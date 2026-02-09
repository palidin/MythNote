using System.Security.Cryptography;

namespace MythNote.Web.Services;

public class UploadService(IConfiguration configuration, ILogger<UploadService> logger)
    : IUploadService
{
    private readonly ILogger<UploadService> _logger = logger;

    public async Task<object> UploadSpecificFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("没有文件被上传");
        }

        var extensions = configuration["Upload:Extensions:Image"] ?? "jpg|jpeg|png|gif|webp";
        var allowedExtensions = extensions.Split('|').Select(e => e.ToLower()).ToList();

        var ext = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
        if (!allowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException($"文件类型不合法，必须以 {string.Join(" ", allowedExtensions)} 为后缀");
        }

        using var stream = file.OpenReadStream();
        using var md5 = MD5.Create();
        var hashBytes = await md5.ComputeHashAsync(stream);
        var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        stream.Seek(0, SeekOrigin.Begin);

        var filename = CreateKey(ext, hash);
        var folder = configuration["Upload:Folder"] ?? "./uploads";
        var filePath = Path.Combine(folder, filename);

        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await stream.CopyToAsync(fileStream);
        }

        var title = Path.GetFileNameWithoutExtension(file.FileName);
        var host = configuration["Upload:Host"] ?? "/uploads";
        var url = $"{host}/{filename.Replace("\\", "/")}";

        return new
        {
            title = title,
            url = url
        };
    }

    private string CreateKey(string ext, string hash)
    {
        var datePath = DateTime.Now.ToString("yyyy/MM/dd");
        var name = $"{datePath}/{hash}";
        if (!string.IsNullOrEmpty(ext))
        {
            name = $"{name}.{ext}";
        }

        return name;
    }
}