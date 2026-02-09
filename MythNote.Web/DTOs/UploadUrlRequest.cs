using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MythNote.Web.DTOs
{
    public class UploadUrlRequest
    {
        [Required]
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
