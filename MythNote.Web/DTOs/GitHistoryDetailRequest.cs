using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MythNote.Web.DTOs
{
    public class GitHistoryDetailRequest
    {
        [Required]
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("commitId")]
        public string CommitId { get; set; } = string.Empty;
    }
}