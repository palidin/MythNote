using Microsoft.AspNetCore.Mvc;
using MythNote.Web.DTOs;
using MythNote.Web.Services;

namespace MythNote.Web.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] AuthRequest request)
    {
        try
        {
            var user = _userService.Login(request.Username, request.Password);
            var tokenResult = _userService.GetTokenResult(user);
            return Ok(new ApiResponse { Status = 0, Data = tokenResult });
        }
        catch (Exception ex)
        {
            return StatusCode(200, new ApiResponse { Status = 500, Msg = ex.Message });
        }
    }
}