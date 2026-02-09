using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace MythNote.Web.DTOs
{
    public class NoteProps
    {
        [YamlMember(Order = 1)]
        public string Created { get; set; }

        [YamlMember(Order = 2)]
        public string Modified { get; set; }

        [YamlMember(Order = 3)]
        public string Title { get; set; }

        [YamlMember(Order = 4)]
        public List<string> Tags { get; set; } = [];
        
        [YamlMember(Order = 5)]
        public string? SourceUrl { get; set; }
        
        
        [YamlMember(Order = 6)]
        public bool Pinned { get; set; }

        [YamlMember(Order = 7)]
        public bool Deleted { get; set; }

    }

    public class NoteWriteRequest
    {
        [Required]
        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("props")]
        public NoteProps Props { get; set; }
    }
}