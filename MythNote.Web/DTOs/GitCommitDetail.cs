using System.Text.Json.Serialization;

namespace MythNote.Web.DTOs
{
    public class GitCommitDetail
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

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("diff")]
        public string Diff { get; set; } = string.Empty;

        [JsonPropertyName("parentIds")]
        public List<string> ParentIds { get; set; } = new();
    }
}