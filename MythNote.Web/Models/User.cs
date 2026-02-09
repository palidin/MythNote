using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MythNote.Web.Models;

[Table("user")]
[Index(nameof(Name), IsUnique = true)]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [Required]
    [MaxLength(255)]
    public string Name { get; set; }

    [Column("password")]
    [MaxLength(255)]
    public string Password { get; set; }

    [Column("created_at")]
    public long CreatedAt { get; set; }

    [Column("updated_at")]
    public long UpdatedAt { get; set; }

    [Column("git_sync_time")]
    public DateTime? GitSyncTime { get; set; }

    [Column("git_repo_url")]
    [MaxLength(512)]
    public string? GitRepoUrl { get; set; }

    [Column("git_auth_token")]
    [MaxLength(512)]
    public string? GitAuthToken { get; set; }

    [Column("git_sync_interval")]
    public int GitSyncInterval { get; set; }
}