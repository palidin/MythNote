using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MythNote.Web.DTOs
{
    public class RefreshTokenRequest
    {
        [Required]
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
