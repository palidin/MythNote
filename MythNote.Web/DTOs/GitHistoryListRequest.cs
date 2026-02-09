using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MythNote.Web.DTOs
{
    public class GitHistoryListRequest
    {
        [Required]
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("page")]
        public int Page { get; set; } = 1;

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 20;
    }
}