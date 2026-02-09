using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MythNote.Web.DTOs;
using MythNote.Web.Services;

namespace MythNote.Web.Controllers;

[ApiController]
[Authorize]
public class FileController(INoteService noteService) : ControllerBase
{
    [HttpPost("file/list")]
    public IActionResult List([FromBody] NoteIndexRequest request)
    {
        return Ok(new ApiResponse { Status = 0, Data = noteService.Index(request) });
    }

    [HttpPost("file/read")]
    public IActionResult Read([FromBody] NoteReadRequest request)
    {
        return Ok(new ApiResponse { Status = 0, Data = noteService.ReadFileMatter(request.Path) });
    }

    [HttpPost("file/write")]
    public IActionResult Write([FromBody] NoteWriteRequest request)
    {
        return Ok(new ApiResponse { Status = 0, Data = noteService.SafeSaveFile(request.Path, request.Content, request.Props, false) });
    }

    [HttpPost("file/delete")]
    public IActionResult Delete([FromBody] NoteDeleteRequest request)
    {
        return Ok(new ApiResponse { Status = 0, Data = noteService.DeleteAll(request.Paths, request.Deleted > 0) });
    }

    [HttpPost("file/cleanup")]
    public IActionResult Cleanup()
    {
        return Ok(new ApiResponse { Status = 0, Data = noteService.Cleanup() });
    }

    [HttpPost("category/list")]
    public IActionResult CategoryIndex()
    {
        return Ok(new ApiResponse { Status = 0, Data = noteService.CategoryIndex() });
    }

    [HttpPost("category/rename")]
    public IActionResult CategoryRename([FromBody] NoteCategoryRenameRequest request)
    {
        return Ok(new ApiResponse { Status = 0, Data = noteService.CategoryRename(request.Old, request.New) });
    }

    [HttpPost("category/delete")]
    public IActionResult CategoryDelete([FromBody] NoteCategoryDeleteRequest request)
    {
        return Ok(new ApiResponse { Status = 0, Data = noteService.CategoryDelete(request.Name) });
    }

    [HttpPost("system/rebuild")]
    public IActionResult SystemRebuild()
    {
        return Ok(new ApiResponse { Status = 0, Data = noteService.SystemRebuild() });
    }

    [HttpPost("system/status")]
    public IActionResult SystemStatus()
    {
        return Ok(new ApiResponse { Status = 0, Data = new { @lock = noteService.SystemStatus() } });
    }

    [HttpPost("git/history/list")]
    public IActionResult GitHistoryList([FromBody] GitHistoryListRequest request)
    {
        return Ok(new ApiResponse { Status = 0, Data = noteService.GetGitHistoryList(request) });
    }

    [HttpPost("git/history/detail")]
    public IActionResult GitHistoryDetail([FromBody] GitHistoryDetailRequest request)
    {
        return Ok(new ApiResponse { Status = 0, Data = noteService.GetGitHistoryDetail(request) });
    }
}