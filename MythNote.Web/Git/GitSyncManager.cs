using System.Collections.Concurrent;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using MythNote.Web.Models;

namespace MythNote.Web.Git;

public class GitSyncManager
{
    private readonly string _baseLocalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "repo");

    private static readonly ConcurrentDictionary<int, object> UserLocks = new();

    private object GetUserLock(int userId)
    {
        return UserLocks.GetOrAdd(userId, new object());
    }

    public string GetUserFolder(int id)
    {
        return Path.Combine(_baseLocalPath, id.ToString());
    }

    public void DeleteFolder(int userId)
    {
        // 获取该用户专属的锁对象
        lock (GetUserLock(userId))
        {
            string path = Path.Combine(_baseLocalPath, userId.ToString());
            if (!Directory.Exists(path)) return;

            var directory = new DirectoryInfo(path);
            foreach (var file in directory.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
            {
                if ((file.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    file.Attributes = FileAttributes.Normal;
                }
            }

            directory.Delete(true);
        }
    }

    public void FullSync(User user)
    {
        lock (GetUserLock(user.Id))
        {
            // 为每个用户创建单独的仓库路径
            string localPath = Path.Combine(_baseLocalPath, user.Id.ToString());
            string repoUrl = user.GitRepoUrl;
            string authToken = user.GitAuthToken;

            // 验证用户配置
            if (string.IsNullOrEmpty(repoUrl))
            {
                throw new Exception($"用户 {user.Id} 未配置 Git 仓库 URL");
            }

            if (string.IsNullOrEmpty(authToken))
            {
                throw new Exception($"用户 {user.Id} 未配置 Git 认证令牌");
            }

            // 定义凭据处理器
            CredentialsHandler credentialsHandler = (_, _, _) =>
                new UsernamePasswordCredentials { Username = "oauth2", Password = authToken };

            // 1. 克隆逻辑（增强检查）
            if (!Directory.Exists(localPath) || !Repository.IsValid(localPath))
            {
                if (Directory.Exists(localPath)) Directory.Delete(localPath, true);

                var cloneOptions = new CloneOptions { FetchOptions = { CredentialsProvider = credentialsHandler } };
                Repository.Clone(repoUrl, localPath, cloneOptions);
            }

            using var repo = new Repository(localPath);
            var signature = new Signature($"User-{user.Id}", $"user{user.Id}@example.com", DateTimeOffset.Now);

            // 2. 提交本地修改
            Commands.Stage(repo, "*");
            if (repo.RetrieveStatus().IsDirty)
            {
                repo.Commit($"Auto-sync: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", signature, signature);
            }

            // 3. 同步远程状态 (Fetch)
            var remote = repo.Network.Remotes["origin"];
            var fetchOptions = new FetchOptions { CredentialsProvider = credentialsHandler };
            Commands.Fetch(repo, remote.Name, Array.Empty<string>(), fetchOptions, null);

            // 4. 合并远程分支 (替代 Pull)
            var upstreamBranch = repo.Head.TrackedBranch;
            if (upstreamBranch != null)
            {
                var mergeResult = repo.Merge(upstreamBranch, signature, new MergeOptions
                {
                    FastForwardStrategy = FastForwardStrategy.Default
                });

                if (mergeResult.Status == MergeStatus.Conflicts)
                {
                    // 这里可以根据需求：抛出异常、或者强制以远程/本地为准
                    throw new Exception("检测到合并冲突，请手动解决。");
                }
            }

            // 5. 执行 Push
            var pushOptions = new PushOptions { CredentialsProvider = credentialsHandler };
            repo.Network.Push(repo.Head, pushOptions);

            user.GitSyncTime = DateTime.Now;
        }
    }


    /// <summary>
    /// 检查指定用户本地仓库是否有未提交的更改
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>如果有未提交的更改返回 true，否则返回 false</returns>
    public bool HasUncommittedChanges(int userId)
    {
        string localPath = Path.Combine(_baseLocalPath, userId.ToString());

        // 1. 检查目录和仓库有效性
        if (!Directory.Exists(localPath) || !Repository.IsValid(localPath))
        {
            return false;
        }

        using var repo = new Repository(localPath);

        // 2. 获取仓库状态
        // RepositoryStatus 会包含所有 Staged, Unstaged, Untracked 的文件
        RepositoryStatus status = repo.RetrieveStatus(new StatusOptions
        {
            IncludeUntracked = true, // 是否包含未追踪的新文件
            RecurseUntrackedDirs = true
        });

        // 3. 只要 IsDirty 为 true，就表示本地有变动（含新增、修改、删除）
        return status.IsDirty;
    }
}