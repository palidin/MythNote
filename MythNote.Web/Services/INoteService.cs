using MythNote.Web.DTOs;

namespace MythNote.Web.Services
{
    public interface INoteService
    {
        object Index(NoteIndexRequest request);
        MarkdownFile ReadFileMatter(string path);
        bool SafeSaveFile(string path, string body, NoteProps props, bool ignoreWriteFile);
        int DeleteAll(List<string> paths, bool deleted);
        bool Cleanup();
        object CategoryIndex();
        bool CategoryRename(string oldName, string newName);
        bool CategoryDelete(string name);
        int SystemRebuild();
        bool SystemStatus();
        void RunRebuildTask();
        object GetGitHistoryList(GitHistoryListRequest request);
        GitCommitDetail GetGitHistoryDetail(GitHistoryDetailRequest request);
    }
}
