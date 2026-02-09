using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MythNote.Web.DTOs
{
    public class NoteReadRequest
    {
        [Required]
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;
    }
}
