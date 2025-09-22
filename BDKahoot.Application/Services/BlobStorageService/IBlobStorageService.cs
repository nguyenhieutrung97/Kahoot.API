using BDKahoot.Domain.Models;

namespace BDKahoot.Application.Services.BlobStorageServices
{
    public interface IBlobStorageService
    {
        Task UploadFileAsync(string blobName, Stream fileStream);
        Task DeleteFileAsync(string blobName);
        Task<MemoryStream?> GetFileAsync(string blobName);
        Task<GameAudio> GetAudioFileUrlsAsync();
    }
}
