using Azure.Storage.Blobs;
using BDKahoot.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace BDKahoot.Application.Services.BlobStorageServices
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly ILogger<BlobStorageService> _logger;
        private readonly BlobServiceClient _blobServiceClient;
        private string backgroundContainerName = string.Empty;
        private string audioContainerName = string.Empty;

        private const string BDKahoot_File_Lobby = "BDKahoot_Lobby.mp3";
        private const string BDKahoot_File_10sec = "BDKahoot_10sec.mp3";
        private const string BDKahoot_File_20sec = "BDKahoot_20sec.mp3";
        private const string BDKahoot_File_30sec = "BDKahoot_30sec.mp3";

        public BlobStorageService(ILogger<BlobStorageService> logger, BlobServiceClient blobServiceClient, IOptions<BlobStorageOptions> options)
        {
            _logger = logger;
            _blobServiceClient = blobServiceClient;
            backgroundContainerName = options.Value.BackgroundContainerName;
            audioContainerName = options.Value.AudioContainerName;
        }

        public async Task<MemoryStream?> GetFileAsync(string blobName)
        {
            if (_blobServiceClient == null)
            {
                _logger.LogInformation("Cannot initialize BlobServiceClient");
                return null;
            }

            // Get a reference to the container
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(backgroundContainerName);

            // Get a reference to the blob
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
                
            // Check if blob exists before trying to download
            var exists = await blobClient.ExistsAsync();
            if (!exists.Value)
            {
                _logger.LogInformation($"Blob {blobName} does not exist");
                return null;
            }

            if (Debugger.IsAttached) _logger.LogInformation($"Get blob {blobName} successfully!");

            var downloadStream = new MemoryStream();
            await blobClient.DownloadToAsync(downloadStream);
            return downloadStream;
        }

        public async Task DeleteFileAsync(string blobName)
        {
            if (_blobServiceClient == null)
            {
                _logger.LogInformation("Cannot initialize BlobServiceClient");
                return;
            }

            // Get a reference to the container
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(backgroundContainerName);

            // Get a reference to the blob
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync();
            if (Debugger.IsAttached) _logger.LogInformation($"Deleted blob {blobName} successfully");
        }

        public async Task UploadFileAsync(string blobName, Stream fileStream)
        {
            if (_blobServiceClient == null)
            {
                _logger.LogInformation("Cannot initialize BlobServiceClient");
                return;
            }

            // Create a BlobContainerClient
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(backgroundContainerName);

            // Create a BlobClient
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            // Upload the file
            if (fileStream == null)
            {
                _logger.LogWarning($"Upload {blobName} to Blob Storage failed because of File Stream is null.");
                return;
            }
            await blobClient.UploadAsync(fileStream);
            if (Debugger.IsAttached) _logger.LogInformation($"DONE! Uploaded file to blob \"{blobName}\" of container \"{backgroundContainerName}\".");
        }

        public async Task<GameAudio> GetAudioFileUrlsAsync()
        {
            if (_blobServiceClient == null)
            {
                _logger.LogInformation("Cannot initialize BlobServiceClient");
                throw new InvalidOperationException("Cannot initialize BlobServiceClient.");
            }

            GameAudio gameAudio = new GameAudio()
            {
                BDKahoot_Lobby = await GetAudioFileUrlAsync(BDKahoot_File_Lobby),
                BDKahoot_10sec = await GetAudioFileUrlAsync(BDKahoot_File_10sec),
                BDKahoot_20sec = await GetAudioFileUrlAsync(BDKahoot_File_20sec),
                BDKahoot_30sec = await GetAudioFileUrlAsync(BDKahoot_File_30sec)
            };
            return gameAudio;
        }

        private async Task<string> GetAudioFileUrlAsync(string blobName, int validMinutes = 60)
        {
            if (_blobServiceClient == null)
            {
                _logger.LogInformation("Cannot initialize BlobServiceClient");
                throw new InvalidOperationException("Cannot initialize BlobServiceClient.");
            }
            // Create a BlobContainerClient
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(audioContainerName);
            // Create a BlobClient
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            // Check if blob exists before trying to grant read access
            var exists = await blobClient.ExistsAsync();
            if (!exists.Value)
            {
                _logger.LogInformation($"Blob {blobName} does not exist");
                throw new FileNotFoundException($"Blob {blobName} not found in container {audioContainerName}");
            }
            // Create SAS token to grant read access, expired after validMinutes
            var uri = (blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(validMinutes))).AbsoluteUri;
            return uri;
        }
    }
}