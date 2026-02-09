using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MythNote.Web.DTOs
{
    public class NoteCategoryRenameRequest
    {
        [Required]
        [JsonPropertyName("old")]
        public string Old { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("new")]
        public string New { get; set; } = string.Empty;
    }
}
