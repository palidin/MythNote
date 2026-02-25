using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MythNote.Web.DTOs;
using MythNote.Web.Git;
using MythNote.Web.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using AppTag = MythNote.Web.Models.Tag;
using AppNote = MythNote.Web.Models.Note;

namespace MythNote.Web.Services;

public class NoteService(
    AppDbContext context,
    IMemoryCache cache,
    IServiceProvider serviceProvider,
    SessionUser sessionUser,
    GitSyncManager gitSyncManager,
    ILogger<NoteService> logger)
    : INoteService
{
    public object Index(NoteIndexRequest request)
    {
        var query = context.Notes.AsQueryable();

        if (!string.IsNullOrEmpty(request.Folder))
        {
            if (request.Folder == "//trash")
            {
                query = query.Where(n => n.Deleted);
            }
            else
            {
                query = query.Where(n => !n.Deleted);
                if (request.Folder == "//untagged")
                {
                    query = query.Where(n => string.IsNullOrEmpty(n.Tags));
                }
                else
                {
                    var tag = context.Tags.FirstOrDefault(t => t.Fullname == request.Folder);
                    if (tag != null)
                    {
                        var noteIds = context.NoteTags
                            .Where(nt => nt.TagId == tag.Id)
                            .Select(nt => nt.NoteId)
                            .Distinct()
                            .ToList();
                        query = query.Where(n => noteIds.Contains(n.Id));
                    }
                    else
                    {
                        query = query.Where(n => false);
                    }
                }
            }
        }
        else
        {
            query = query.Where(n => !n.Deleted);
        }

        if (!string.IsNullOrEmpty(request.Keywords))
        {
            // 1. 提取唯一的关键词，过滤空白
            var keywords = request.Keywords
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct() 
                .ToList(); // 立即求值，避免后续多次迭代

            // 2. 构建查询
            foreach (var keyword in keywords)
            {
                query = query.Where(n => n.Title.Contains(keyword) || n.Body.Contains(keyword));
            }
        }


        var orderedQuery = query.OrderByDescending(a => a.Pinned);

        var col = request.Order.column;
        var isDesc = request.Order.direction == "desc";

        orderedQuery = col switch
        {
            "title" => isDesc ? orderedQuery.ThenByDescending(n => n.Title) : orderedQuery.ThenBy(n => n.Title),
            "created" => isDesc ? orderedQuery.ThenByDescending(n => n.Created) : orderedQuery.ThenBy(n => n.Created),
            "modified" => isDesc
                ? orderedQuery.ThenByDescending(n => n.Modified)
                : orderedQuery.ThenBy(n => n.Modified),
            _ => orderedQuery // 默认不加额外排序或加默认排序
        };

        query = orderedQuery;


        var totalCount = query.Count();
        var items = query
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToList()
            .Select(n => new
            {
                title = n.Title,
                path = n.Path,
                pinned = n.Pinned,
                filepath = GetFilePath(n.Path)
            })
            .ToList();

        return new
        {
            total = totalCount,
            page = request.Page,
            limit = request.Limit,
            items = items
        };
    }


    public MarkdownFile ReadFileMatter(string path)
    {
        var text = ReadFile(path);
        return ParseMarkdown(text);
    }

    public bool SafeSaveFile(string path, string body, NoteProps props, bool ignoreWriteFile)
    {
        var markdownFile = ReadFileMatter(path);

        if (!string.IsNullOrEmpty(markdownFile.props.Created) && markdownFile.props.Created != props.Created)
        {
            logger.LogWarning($"{path} 创建日期不一致");
            return false;
        }


        // 如果修改元信息，更新文件
        if (!ignoreWriteFile)
        {
            WriteFile(path, GenerateMarkdown(new MarkdownFile()
            {
                body = body,
                props = props,
            }));
        }

        // 默认更新数据库
        return SaveFile(path, body, props);
    }

    private bool SaveFile(string path, string body, NoteProps props)
    {
        var tags = props.Tags;
        var row = new NoteRowDto
        {
            Path = path,
            Body = body,
            Title = props.Title,
            Tags = string.Join(",", props.Tags),
            Deleted = props.Deleted,
            Pinned = props.Pinned,
            Created = props.Created,
            Modified = props.Modified
        };

        var existingNote = context.Notes.FirstOrDefault(n => n.Path == path);
        long noteId;

        if (existingNote != null)
        {
            existingNote.Title = row.Title;
            existingNote.Body = row.Body;
            existingNote.Tags = row.Tags;
            existingNote.Deleted = row.Deleted;
            existingNote.Pinned = row.Pinned;
            existingNote.Modified = row.Modified;
            existingNote.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            noteId = existingNote.Id;
        }
        else
        {
            var newNote = new AppNote
            {
                Path = row.Path,
                Title = row.Title,
                Body = row.Body,
                Tags = row.Tags,
                Deleted = row.Deleted,
                Pinned = row.Pinned,
                Created = row.Created,
                Modified = row.Modified,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            context.Notes.Add(newNote);
            context.SaveChanges();
            noteId = newNote.Id;
        }

        if (row.Deleted)
        {
            tags = [];
        }

        UpdateFileTags(path, tags, noteId);
        return true;
    }

    public int DeleteAll(List<string> paths, bool deleted)
    {
        int count = 0;
        foreach (var path in paths)
        {
            if (Delete(path, deleted))
            {
                count++;
            }
        }

        return count;
    }

    public bool Cleanup()
    {
        var txn = context.Database.BeginTransaction();
        var notes = context.Notes.Where(a => a.Deleted).ToList();
        foreach (var note in notes)
        {
            DeleteFile(note.Path);
            context.Notes.Remove(note);
        }

        context.SaveChanges();

        txn.Commit();
        return true;
    }


    private bool Delete(string path, bool deleted)
    {
        var content = ReadFile(path);
        var markdownFile = ParseMarkdown(content);

        var tags = markdownFile.props.Tags;

        var modified = GetCurrentDateTime();
        var newContent = RewriteMarkdown(content, props =>
        {
            props.Deleted = deleted;
            props.Modified = modified;
        });

        WriteFile(path, newContent);

        if (deleted)
        {
            tags = new List<string>();
        }

        UpdateFileTags(path, tags);

        var note = context.Notes.FirstOrDefault(n => n.Path == path);
        if (note != null)
        {
            note.Deleted = deleted;
            note.Modified = modified;
            note.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            context.SaveChanges();
        }

        return true;
    }

    public object CategoryIndex()
    {
        var tags = context.Tags.ToList();
        var tree = BuildTagTree(tags, 0);

        return new object[]
        {
            new
            {
                name = "全部笔记",
                fullname = "",
                expand = true,
                count = context.Notes.Count(n => !n.Deleted),
                children = tree
            },
            new
            {
                name = "草稿本",
                fullname = "//untagged",
                count = context.Notes.Count(n => !n.Deleted && string.IsNullOrEmpty(n.Tags)),
                children = Array.Empty<object>()
            },
            new
            {
                name = "回收站",
                fullname = "//trash",
                count = context.Notes.Count(n => n.Deleted),
                children = Array.Empty<object>()
            }
        };
    }


    public bool CategoryDelete(string name)
    {
        return CategoryRename(name, "");
    }


    public bool SystemStatus()
    {
        return cache.TryGetValue("update_lock", out _);
    }


    private void Rebuild()
    {
        var noteFolder = GetNoteFolder();

        if (!Directory.Exists(noteFolder))
        {
            logger.LogWarning("笔记文件夹不存在: {NoteFolder}", noteFolder);
            return;
        }

        var files = Directory.GetFiles(noteFolder, "*.md", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var fileName = Path.GetFileName(file);

            var markdownFile = ParseMarkdown(content);
            SafeSaveFile(fileName, markdownFile.body, markdownFile.props, true);
        }
    }


    private MarkdownFile ParseMarkdown(string content)
    {
        string body = content;
        NoteProps props = new();

        // 匹配开头和结尾都是 --- 的块，且只匹配一次
        // ^--- 表示开头，$--- 表示结尾，使用 Multiline 模式处理换行
        var match = Regex.Match(content, @"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline);

        if (match.Success)
        {
            var yamlContent = match.Groups[1].Value;
            // 这里的 Index 是匹配项的起始位置，Length 是整个 ---...--- 的长度
            body = content.Substring(match.Index + match.Length).TrimStart();

            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties() // 建议加上，增强健壮性
                    .Build();

                props = deserializer.Deserialize<NoteProps>(yamlContent);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"YAML Parsing Error: {ex.Message}");
            }
        }

        return new MarkdownFile { props = props, body = body };
    }


    private string GenerateMarkdown(MarkdownFile file)
    {
        var serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull |
                                            DefaultValuesHandling.OmitDefaults |
                                            DefaultValuesHandling.OmitEmptyCollections)
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithDefaultScalarStyle(style: ScalarStyle.SingleQuoted)
            .WithIndentedSequences()
            .Build();

        var yamlContent = serializer.Serialize(file.props).Trim();

        var yamlHeader = $"---\n{yamlContent}\n---\n";

        return yamlHeader + file.body.TrimStart();
    }


    private string RewriteMarkdown(string content, Action<NoteProps> fn)
    {
        var md = ParseMarkdown(content);
        fn(md.props);
        return GenerateMarkdown(md);
    }


    private void UpdateFileTags(string path, List<string> tags, long? noteId = null)
    {
        int retryCount = 3;
        while (retryCount > 0)
        {
            try
            {
                UpdateFileTagsInner(path, tags, noteId);
                break;
            }
            catch (DbUpdateException _) // 捕获唯一索引冲突
            {
                retryCount--;
                if (retryCount == 0) throw;
                // 刷新上下文，重新从数据库读取最新状态
                context.ChangeTracker.Clear();
            }
        }
    }

    private void UpdateFileTagsInner(string path, List<string> tags, long? noteId = null)
    {
        if (!noteId.HasValue)
        {
            var note = context.Notes.FirstOrDefault(n => n.Path == path);
            noteId = note?.Id;
        }

        if (!noteId.HasValue) return;

        var splitTags = SplitTags(tags);

        var existingTags = context.Tags
            .Where(t => splitTags.Contains(t.Fullname))
            .ToDictionary(t => t.Fullname, t => t.Id);

        // 记录修改前该笔记关联的标签，用于后续刷新它们的计数
        var previousNoteTags = context.NoteTags
            .Where(nt => nt.NoteId == noteId.Value)
            .Select(nt => nt.TagId)
            .ToList();

        // 清除旧关系
        context.NoteTags.RemoveRange(context.NoteTags.Where(nt => nt.NoteId == noteId.Value));

        // 更新/创建标签结构
        var idMap = UpdateTags(splitTags, existingTags);

        // 建立新关系
        foreach (var tagId in idMap.Values)
        {
            context.NoteTags.Add(new NoteTag
            {
                NoteId = (int)noteId.Value,
                TagId = tagId,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
        }

        // 处理孤儿标签（没有笔记关联的标签）
        var currentTagIds = idMap.Values.ToList();
        var removeTagIds = previousNoteTags.Except(currentTagIds).ToList();
        if (removeTagIds.Any())
        {
            var tagsToRemove = context.Tags.Where(t =>
                removeTagIds.Contains(t.Id) &&
                !context.NoteTags.Any(nt => nt.TagId == t.Id && nt.NoteId != noteId.Value)).ToList();
            context.Tags.RemoveRange(tagsToRemove);
        }

        // 先保存 NoteTags 的变更，否则 Count 统计不准
        context.SaveChanges();

        // --- 核心修复：刷新所有受影响标签的 Count ---
        // 受影响的标签包括：当前笔记新加的标签，以及之前关联但现在可能减少了数量的标签
        var tagsToRefresh = currentTagIds.Concat(previousNoteTags).Distinct().ToList();

        foreach (var tid in tagsToRefresh)
        {
            var tag = context.Tags.FirstOrDefault(t => t.Id == tid);
            if (tag != null)
            {
                // 重新计算该标签被多少笔记引用
                tag.Count = context.NoteTags.Count(nt => nt.TagId == tid);
            }
        }

        context.SaveChanges();
    }

    private List<string> SplitTags(List<string> tags)
    {
        var tagCounts = new HashSet<string>();
        foreach (var tag in tags)
        {
            var parts = tag.Split('/');
            var names = new List<string>();
            foreach (var part in parts)
            {
                names.Add(part);
                var tagName = string.Join("/", names);
                tagCounts.Add(tagName);
            }
        }

        return tagCounts.ToList();
    }

    private Dictionary<string, int> UpdateTags(List<string> tags, Dictionary<string, int> existingTags)
    {
        var idMap = new Dictionary<string, int>();
        // 排序确保父级先处理
        var sortedTags = tags.OrderBy(t => t.Length).ToList();

        foreach (var tag in sortedTags)
        {
            var parentName = GetParentName(tag);
            int parentId = 0;
            string ancestorIds = "0";

            if (!string.IsNullOrEmpty(parentName))
            {
                if (idMap.TryGetValue(parentName, out var pId) || existingTags.TryGetValue(parentName, out pId))
                {
                    parentId = pId;
                    var parentTag = context.Tags.AsNoTracking().FirstOrDefault(t => t.Id == parentId);
                    if (parentTag != null)
                    {
                        ancestorIds = $"{parentTag.AncestorIds},{parentId}";
                    }
                }
            }

            if (existingTags.TryGetValue(tag, out var existingTagId))
            {
                var existingTag = context.Tags.FirstOrDefault(t => t.Id == existingTagId);
                if (existingTag != null)
                {
                    existingTag.ParentId = parentId;
                    existingTag.AncestorIds = ancestorIds;
                    existingTag.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    idMap[tag] = existingTag.Id;
                }
            }
            else
            {
                var newTag = new AppTag
                {
                    Fullname = tag,
                    Name = GetBaseName(tag),
                    ParentId = parentId,
                    AncestorIds = ancestorIds,
                    Count = 0, // 先设为0，统一刷新
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                context.Tags.Add(newTag);
                context.SaveChanges();
                idMap[tag] = newTag.Id;
            }
        }

        return idMap;
    }

    private string GetBaseName(string tag)
    {
        var parts = tag.Split('/');
        return parts.Last();
    }

    private string GetParentName(string tag)
    {
        var parts = tag.Split('/');
        if (parts.Length <= 1)
        {
            return "";
        }

        return string.Join("/", parts.Take(parts.Length - 1));
    }

    private List<TreeNode> BuildTagTree(List<AppTag> tags, int parentId)
    {
        var children = tags.Where(t => t.ParentId == parentId).ToList();
        var result = new List<TreeNode>();

        foreach (var tag in children)
        {
            // 递归获取子节点的树
            var childNodes = BuildTagTree(tags, tag.Id);

            // 计算当前节点及其所有子节点的总计数 (如果你的 Count 存的是直接关联数)
            // 或者直接在这里使用内存中的 tags 列表计算
            int totalCount = tag.Count;

            // 如果你想要的是“父级包含子级”的数量，则取消下面这行的注释：
            totalCount += childNodes.Sum(c => (int)((dynamic)c).count);

            result.Add(new TreeNode
            {
                name = tag.Name,
                fullname = tag.Fullname,
                count = totalCount,
                children = childNodes.OrderBy(a => a.name).ToList(),
            });
        }

        return result;
    }

    private string GetNoteFolder()
    {
        return gitSyncManager.GetUserFolder(sessionUser.Id);
    }

    private string GetFilePath(string path)
    {
        var prefix = path.Length >= 2 ? path.Substring(0, 2) : path;
        var dir = Path.Combine(GetNoteFolder(), "data", prefix);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        return Path.Combine(dir, path);
    }

    private string ReadFile(string path)
    {
        var filePath = GetFilePath(path);
        if (File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }

        return "";
    }

    private void WriteFile(string path, string content)
    {
        var filePath = GetFilePath(path);
        File.WriteAllText(filePath, content);
    }

    private void DeleteFile(string path)
    {
        var filePath = GetFilePath(path);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private string GetCurrentDateTime()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public object GetGitHistoryList(GitHistoryListRequest request)
    {
        var noteFolder = GetNoteFolder();
        var filePath = GetFilePath(request.Path);

        if (!File.Exists(filePath))
        {
            return new
            {
                total = 0,
                page = request.Page,
                limit = request.Limit,
                items = Array.Empty<object>()
            };
        }

        if (!Repository.IsValid(noteFolder))
        {
            return new
            {
                total = 0,
                page = request.Page,
                limit = request.Limit,
                items = Array.Empty<object>()
            };
        }

        using var repo = new Repository(noteFolder);
        var relativePath = Path.GetRelativePath(noteFolder, filePath).Replace("\\", "/");

        var commits = repo.Commits
            .QueryBy(relativePath)
            .Select(c => new GitCommitInfo
            {
                Id = c.Commit.Id.ToString(),
                ShortId = c.Commit.Id.ToString(7),
                Message = c.Commit.MessageShort,
                Author = c.Commit.Author.Name,
                AuthorEmail = c.Commit.Author.Email,
                Date = c.Commit.Author.When.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss")
            })
            .ToList();

        var totalCount = commits.Count;
        var items = commits
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToList();

        return new
        {
            total = totalCount,
            page = request.Page,
            limit = request.Limit,
            items = items
        };
    }

    public GitCommitDetail GetGitHistoryDetail(GitHistoryDetailRequest request)
    {
        var noteFolder = GetNoteFolder();
        var filePath = GetFilePath(request.Path);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {request.Path}");
        }

        if (!Repository.IsValid(noteFolder))
        {
            throw new InvalidOperationException("Not a valid git repository");
        }

        using var repo = new Repository(noteFolder);
        var relativePath = Path.GetRelativePath(noteFolder, filePath).Replace("\\", "/");

        var commit = repo.Lookup<Commit>(new ObjectId(request.CommitId));
        if (commit == null)
        {
            throw new ArgumentException($"Commit not found: {request.CommitId}");
        }

        var treeEntry = commit[relativePath];
        if (treeEntry == null)
        {
            throw new FileNotFoundException($"File not found in commit: {relativePath}");
        }

        var blob = treeEntry.Target as Blob;
        var content = blob?.GetContentText() ?? string.Empty;

        var diffContent = string.Empty;
        var parent = commit.Parents.FirstOrDefault();
        if (parent != null)
        {
            var parentTreeEntry = parent[relativePath];
            var parentBlob = parentTreeEntry?.Target as Blob;
            var parentContent = parentBlob?.GetContentText() ?? string.Empty;

            var diff = repo.Diff.Compare(parentBlob, blob);
            diffContent = diff.Patch;
        }

        return new GitCommitDetail
        {
            Id = commit.Id.ToString(),
            ShortId = commit.Id.ToString(7),
            Message = commit.MessageShort,
            Author = commit.Author.Name,
            AuthorEmail = commit.Author.Email,
            Date = commit.Author.When.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            Content = content,
            Diff = diffContent,
            ParentIds = commit.Parents.Select(p => p.Id.ToString()).ToList()
        };
    }


    public bool CategoryRename(string oldName, string newName)
    {
        if (oldName == newName) return true;

        var oldTag = context.Tags.FirstOrDefault(t => t.Fullname == oldName);
        if (oldTag == null) return false;

        var noteIds = context.NoteTags
            .Where(nt => nt.TagId == oldTag.Id)
            .Select(nt => nt.NoteId)
            .Distinct()
            .ToList();

        var modified = GetCurrentDateTime();

        foreach (var id in noteIds)
        {
            var note = context.Notes.FirstOrDefault(n => n.Id == id);
            if (note == null) continue;

            var md = ParseMarkdown(ReadFile(note.Path));
            var tags = md.props.Tags
                .Select(t => t == oldName ? newName : t.StartsWith(oldName + "/") ? newName + t[oldName.Length..] : t)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct()
                .ToList();

            WriteFile(note.Path, RewriteMarkdown(ReadFile(note.Path), p =>
            {
                p.Tags = tags;
                p.Modified = modified;
            }));

            UpdateFileTags(note.Path, tags, note.Id);
        }

        context.Tags.Remove(oldTag);
        context.SaveChanges();
        return true;
    }


    public int SystemRebuild()
    {
        if (!cache.TryGetValue("update_lock", out _))
        {
            cache.Set("update_lock", 1, TimeSpan.FromHours(1));
            Task.Run(() =>
            {
                using var scope = serviceProvider.CreateScope();

                var s = scope.ServiceProvider.GetRequiredService<SessionUser>();
                s.Id = sessionUser.Id;
                scope.ServiceProvider.GetRequiredService<INoteService>().RunRebuildTask();
            });
        }

        return 1;
    }

    public void RunRebuildTask()
    {
        if (cache.TryGetValue("update_lock", out var v) && (int)v == 1)
        {
            cache.Set("update_lock", 2, TimeSpan.FromHours(12));
            try
            {
                RebuildOptimized();
            }
            finally
            {
                cache.Remove("update_lock");
            }
        }
    }

    private void RebuildOptimized()
    {
        var folder = GetNoteFolder();
        if (!Directory.Exists(folder)) return;

        context.Tags.ExecuteDelete();
        context.Notes.ExecuteDelete();
        context.SaveChanges();

        context.ChangeTracker.AutoDetectChangesEnabled = false;
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;


        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var files = Directory.GetFiles(folder, "*.md", SearchOption.AllDirectories);

        var notes = new List<AppNote>();
        var rawTags = new List<(string path, string tag)>();
        var tagSet = new HashSet<string>();

        foreach (var f in files)
        {
            var md = ParseMarkdown(File.ReadAllText(f));
            var path = Path.GetFileName(f);

            notes.Add(new AppNote
            {
                Path = path,
                Title = md.props.Title,
                Body = md.body,
                Tags = string.Join(",", md.props.Tags),
                Created = md.props.Created,
                Modified = md.props.Modified,
                Deleted = md.props.Deleted,
                Pinned = md.props.Pinned,
                CreatedAt = now,
                UpdatedAt = now
            });

            if (!md.props.Deleted) // 只有未删除的笔记才参与标签计数
            {
                foreach (var t in SplitTags(md.props.Tags))
                {
                    tagSet.Add(t);
                    rawTags.Add((path, t));
                }
            }
        }

        context.Notes.AddRange(notes);
        context.SaveChanges();

        var noteIdMap = notes.ToDictionary(n => n.Path, n => n.Id);

        var tags = new Dictionary<string, AppTag>();
        foreach (var name in tagSet.OrderBy(t => t.Length))
        {
            var parent = GetParentName(name);
            var parentId = tags.TryGetValue(parent, out var p) ? p.Id : 0;

            var tag = new AppTag
            {
                Fullname = name,
                Name = GetBaseName(name),
                ParentId = parentId,
                AncestorIds = parentId == 0 ? "0" : p!.AncestorIds + "," + parentId,
                CreatedAt = now,
                UpdatedAt = now
            };

            context.Tags.Add(tag);
            context.SaveChanges();
            tags[name] = tag;
        }

        var noteTags = new List<NoteTag>();
        var countMap = new Dictionary<int, int>();

        foreach (var (path, tag) in rawTags)
        {
            var nt = new NoteTag
            {
                NoteId = (int)noteIdMap[path],
                TagId = tags[tag].Id,
                CreatedAt = now,
                UpdatedAt = now
            };
            noteTags.Add(nt);
            countMap[nt.TagId] = countMap.GetValueOrDefault(nt.TagId) + 1;
        }

        context.NoteTags.AddRange(noteTags);
        context.SaveChanges();

        foreach (var kv in countMap)
            tags.Values.First(t => t.Id == kv.Key).Count = kv.Value;

        context.ChangeTracker.DetectChanges();
        context.SaveChanges();
    }


    private class NoteRowDto
    {
        public string Path { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public bool Deleted { get; set; }
        public bool Pinned { get; set; }
        public string Created { get; set; } = string.Empty;
        public string Modified { get; set; } = string.Empty;
    }
}

public class MarkdownFile
{
    public string body { get; set; }
    public NoteProps props { get; set; }
}

public class TreeNode
{
    public string name { get; set; }
    public string fullname { get; set; }
    public int count { get; set; }
    public List<TreeNode> children { get; set; }
}