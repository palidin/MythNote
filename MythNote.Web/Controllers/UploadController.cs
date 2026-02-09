using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MythNote.Web.DTOs;
using MythNote.Web.Services;

namespace MythNote.Web.Controllers;

[ApiController]
[Authorize]
public class UploadController(IUploadService uploadService) : ControllerBase
{
    [HttpPost("upload/image")]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromForm] string name)
    {
        var result = await uploadService.UploadSpecificFile(file);
        return Ok(new ApiResponse { Status = 0, Data = result });
    }

    [HttpPost("upload/image/base64")]
    public async Task<IActionResult> UploadBase64([FromBody] UploadBase64Request request)
    {
        var file = TranslateBase64(request.Data, request.Name);
        var result = await uploadService.UploadSpecificFile(file);
        return Ok(new ApiResponse { Status = 0, Data = result });
    }

    [HttpPost("upload/image/url")]
    public async Task<IActionResult> UploadUrl([FromBody] UploadUrlRequest request)
    {
        var file = await TranslateUrl(request.Url, request.Name);
        var result = await uploadService.UploadSpecificFile(file);
        return Ok(new ApiResponse { Status = 0, Data = result });
    }

    private IFormFile TranslateBase64(string data, string name)
    {
        var pattern = @"^data:(.*);base64,";
        var match = System.Text.RegularExpressions.Regex.Match(data, pattern);
        if (!match.Success)
        {
            throw new InvalidOperationException("不合法的base64格式");
        }

        var base64Data = data.Substring(match.Length);
        var binary = Convert.FromBase64String(base64Data);

        var stream = new MemoryStream(binary);
        var contentType = match.Groups[1].Value;

        return new FormFile(stream, 0, stream.Length, name, name)
        {
            Headers = new HeaderDictionary
            {
                { "Content-Type", contentType }
            }
        };
    }

    private async Task<IFormFile> TranslateUrl(string url, string name)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(url);
        var binary = await response.Content.ReadAsByteArrayAsync();

        var stream = new MemoryStream(binary);
        var filename = string.IsNullOrEmpty(name) ? Path.GetFileName(new Uri(url).LocalPath) : name;

        return new FormFile(stream, 0, stream.Length, filename, filename)
        {
            Headers = new HeaderDictionary
            {
                { "Content-Type", "image/png" }
            }
        };
    }
}