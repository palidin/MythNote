using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MythNote.Web.DTOs;
using MythNote.Web.Git;
using MythNote.Web.Models;

namespace MythNote.Web.Controllers;

[ApiController]
public class UserController(
    AppDbContext dbContext,
    SessionUser sessionUser,
    GitSyncManager gitSyncManager)
    : ControllerBase
{
    [HttpPost("/git/config/save")]
    [Authorize]
    public IActionResult SetGitConfig([FromBody] GitConfigRequest request)
    {
        try
        {
            // 从JWT获取用户ID
            int userId = sessionUser.Id;
            var user = dbContext.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return StatusCode(200, new ApiResponse { Status = 404, Msg = "用户不存在" });
            }

            // 更新用户的Git配置
            user.GitRepoUrl = request.RepoUrl;
            user.GitAuthToken = request.AuthToken;
            user.GitSyncInterval = request.SyncInterval;
            user.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();


            gitSyncManager.DeleteFolder(userId);

            gitSyncManager.FullSync(user);

            dbContext.SaveChanges();

            return Ok(new ApiResponse { Status = 0, Msg = "Git配置更新成功" });
        }
        catch (Exception ex)
        {
            return StatusCode(200, new ApiResponse { Status = 500, Msg = ex.Message });
        }
    }

    [HttpPost("/git/config/get")]
    [Authorize]
    public IActionResult GetGitConfig()
    {
        int userId = sessionUser.Id;
        var user = dbContext.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            return StatusCode(200, new ApiResponse { Status = 404, Msg = "用户不存在" });
        }

        // 返回用户的Git配置
        var gitConfig = new GitConfigResponse
        {
            RepoUrl = user.GitRepoUrl,
            AuthToken = user.GitAuthToken,
            SyncInterval = user.GitSyncInterval
        };

        return Ok(new ApiResponse { Status = 0, Data = gitConfig });
    }

    [HttpPost("/git/sync")]
    [Authorize]
    public IActionResult GitSync()
    {
        var user = dbContext.Users.First(u => u.Id == sessionUser.Id);
        gitSyncManager.FullSync(user);
        dbContext.SaveChanges();
        return Ok(new ApiResponse { Status = 0, Data = "" });
    }


    [HttpPost("/git/sync/status")]
    [Authorize]
    public IActionResult GitStatus()
    {
        var user = dbContext.Users.First(u => u.Id == sessionUser.Id);
        return Ok(new ApiResponse
        {
            Status = 0, Data = new
            {
                last_sync_time = user.GitSyncTime,
                has_changed = gitSyncManager.HasUncommittedChanges(sessionUser.Id),
            }
        });
    }
}

public class GitConfigRequest
{
    [JsonPropertyName("repoUrl")]
    public string RepoUrl { get; set; }

    [JsonPropertyName("authToken")]
    public string AuthToken { get; set; }

    [JsonPropertyName("syncInterval")]
    public int SyncInterval { get; set; }
}

public class GitConfigResponse
{
    [JsonPropertyName("repoUrl")]
    public string RepoUrl { get; set; }

    [JsonPropertyName("authToken")]
    public string AuthToken { get; set; }

    [JsonPropertyName("syncInterval")]
    public int SyncInterval { get; set; }
}