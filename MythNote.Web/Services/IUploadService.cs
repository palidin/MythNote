namespace MythNote.Web.Services
{
    public interface IUploadService
    {
        Task<object> UploadSpecificFile(IFormFile file);
    }
}
