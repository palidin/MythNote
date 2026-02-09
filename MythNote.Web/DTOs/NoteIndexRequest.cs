using System.Text.Json.Serialization;

namespace MythNote.Web.DTOs;

public class NoteIndexRequest
{
    [JsonPropertyName("folder")]
    public string? Folder { get; set; }

    [JsonPropertyName("keywords")]
    public string? Keywords { get; set; }

    [JsonPropertyName("order")]
    public PageOrder Order { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 10;
}


public class PageOrder
{
    public string column { get; set; }
    public string direction { get; set; }
}