using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MythNote.Web.DTOs
{
    public class UploadBase64Request
    {
        [Required]
        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
