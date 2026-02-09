using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MythNote.Web.DTOs
{
    public class NoteCategoryDeleteRequest
    {
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
