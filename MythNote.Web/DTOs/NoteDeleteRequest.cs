using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MythNote.Web.DTOs
{
    public class NoteDeleteRequest
    {
        [Required]
        [JsonPropertyName("paths")]
        public List<string> Paths { get; set; } = [];

        [JsonPropertyName("deleted")]
        public int Deleted { get; set; }
    }
}
