using MythNote.Web.DTOs;
using MythNote.Web.Models;

namespace MythNote.Web.Services
{
    public interface IUserService
    {
        User Login(string username, string password);
        TokenResponse GetTokenResult(User user);
        TokenResponse RefreshAccessToken(string refreshToken);
    }
}
