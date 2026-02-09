using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MythNote.Web.DTOs;
using MythNote.Web.Services;

namespace MythNote.Web.Controllers;

[ApiController]
[Route("auth/token")]
public class IndexController(IUserService userService) : ControllerBase
{
    [HttpPost("get")]
    public IActionResult Get([FromBody] AuthRequest request)
    {
        try
        {
            var user = userService.Login(request.Username, request.Password);
            var tokenResult = userService.GetTokenResult(user);
            return Ok(new ApiResponse { Status = 0, Data = tokenResult });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(200, new ApiResponse { Status = 401, Msg = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(200, new ApiResponse { Status = 500, Msg = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var tokenResult = userService.RefreshAccessToken(request.RefreshToken);
            return Ok(new ApiResponse { Status = 0, Data = tokenResult });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(200, new ApiResponse { Status = 401, Msg = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(200, new ApiResponse { Status = 500, Msg = ex.Message });
        }
    }

    [HttpPost("destroy")]
    [Authorize]
    public IActionResult Destroy()
    {
        return Ok(new ApiResponse { Status = 0, Data = new { message = "Token destroyed" } });
    }

    [HttpPost("check")]
    [Authorize]
    public IActionResult Check()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

        return Ok(new ApiResponse
        {
            Status = 0,
            Data = new
            {
                access_token = GetAccessToken(),
                is_valid = userId > 0
            }
        });
    }

    [HttpPost("user")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var nameClaim = User.FindFirst(ClaimTypes.Name);

        return Ok(new ApiResponse
        {
            Status = 0,
            Data = new UserResponse
            {
                Id = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0,
                Name = nameClaim?.Value ?? ""
            }
        });
    }

    private string GetAccessToken()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader != null && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length);
        }

        return "";
    }
}