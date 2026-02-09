using System.Text.Json.Serialization;

namespace MythNote.Web.DTOs
{
    public class ApiResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("data")]
        public object? Data { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }
    }
}
