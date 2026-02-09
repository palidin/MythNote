using System.Text.Json.Serialization;

namespace MythNote.Web.DTOs
{
    public class GitCommitInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        [JsonPropertyName("authorEmail")]
        public string AuthorEmail { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("shortId")]
        public string ShortId { get; set; } = string.Empty;
    }
}