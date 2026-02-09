using MythNote.Web.Models;
using static BCrypt.Net.BCrypt;

namespace MythNote.Web.Services;

public static class DatabaseInitializer
{
    public static void Initialize(AppDbContext context, IConfiguration configuration)
    {
        context.Database.EnsureCreated();

        var existNames = context.Users.Select(a => a.Name).ToList();

        var users = configuration.GetSection("Users").Get<List<UserDto>>()!;
        foreach (var v in users)
        {
            if (existNames.Contains(v.Name))
            {
                continue;
            }

            context.Users.Add(new User
            {
                Name = v.Name,
                Password = HashPassword(v.Password),
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
        }

        context.SaveChanges();
    }
}

public class UserDto
{
    public string Name { get; set; }
    public string Password { get; set; }
}