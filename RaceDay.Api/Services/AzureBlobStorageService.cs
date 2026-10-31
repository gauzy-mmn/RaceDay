
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Services;

public class AzureBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<string> UploadImageAsync(
        IFormFile file,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException(
                "Please select an image.");
        }

        // Maximum image size: 5 MB
        const long maxFileSize = 5 * 1024 * 1024;

        if (file.Length > maxFileSize)
        {
            throw new ArgumentException(
                "Image size must not exceed 5 MB.");
        }

        // Only allow our designated image containers
        if (containerName != "profile-pictures" &&
            containerName != "event-banners")
        {
            throw new ArgumentException(
                "Invalid storage container.");
        }

        // Restrict file extensions
        var extension = Path
            .GetExtension(file.FileName)
            .ToLowerInvariant();

        var allowedTypes = new Dictionary<string, string>
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

        if (!allowedTypes.TryGetValue(
                extension, out var contentType) ||
            !string.Equals(
                file.ContentType,
                contentType,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only JPG, PNG and WebP images are allowed.");
        }

        // Verify the actual file signature
        await using (var signatureStream = file.OpenReadStream())
        {
            var header = new byte[12];
            var bytesRead = 0;

            while (bytesRead < header.Length)
            {
                var count = await signatureStream.ReadAsync(
                    header.AsMemory(bytesRead),
                    cancellationToken);

                if (count == 0)
                    break;

                bytesRead += count;
            }

            bool validSignature = extension switch
            {
                ".jpg" or ".jpeg" =>
                    bytesRead >= 3 &&
                    header[0] == 0xFF &&
                    header[1] == 0xD8 &&
                    header[2] == 0xFF,

                ".png" =>
                    bytesRead >= 8 &&
                    header.AsSpan(0, 8).SequenceEqual(
                        new byte[]
                        {
                     0x89, 0x50, 0x4E, 0x47,
                     0x0D, 0x0A, 0x1A, 0x0A
                        }),

                ".webp" =>
                    bytesRead >= 12 &&
                    System.Text.Encoding.ASCII
                        .GetString(header, 0, 4) == "RIFF" &&
                    System.Text.Encoding.ASCII
                        .GetString(header, 8, 4) == "WEBP",

                _ => false
            };

            if (!validSignature)
            {
                throw new ArgumentException(
                    "The uploaded file is not a supported image.");
            }
        }


        // Retrieve or create a private Azure container
        var container = _blobServiceClient
            .GetBlobContainerClient(containerName);

        await container.CreateIfNotExistsAsync(
            publicAccessType: PublicAccessType.None,
            cancellationToken: cancellationToken);

        // Generate a unique, safe filename
        var blobName =
            $"{Guid.NewGuid():N}{extension}";

        var blob = container.GetBlobClient(blobName);

        // Upload the image
        await using var stream = file.OpenReadStream();

        await blob.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            },
            cancellationToken);

        // Return the reference for database storage
        return blobName;
    }

    public async Task<(Stream Content, string ContentType)>
        GetProfilePictureAsync(
            string blobName,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException(
                "Profile picture filename is required.");
        }

        // Retrieve the private profile-pictures container
        var container = _blobServiceClient
            .GetBlobContainerClient("profile-pictures");

        // Locate the user's stored image
        var blob = container.GetBlobClient(blobName);

        // Download the image as a stream
        var response = await blob.DownloadStreamingAsync(
            cancellationToken: cancellationToken);

        // Return the stream and its content type

        return (
            response.Value.Content,
            response.Value.Details.ContentType
                ?? "application/octet-stream"
        );
    }

    // Retrieve an event banner from Azure Blob Storage
    public async Task<(Stream Content, string ContentType)>
        GetEventBannerAsync(
            string blobName,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException(
                "Event banner filename is required.");
        }

        // Access the private event-banners container
        var container = _blobServiceClient
            .GetBlobContainerClient("event-banners");

        // Find the event's uploaded banner
        var blob = container.GetBlobClient(blobName);

        // Download the banner
        var response = await blob.DownloadStreamingAsync(
            cancellationToken: cancellationToken);

        // Return the image and its content type
        return (
            response.Value.Content,
            response.Value.Details.ContentType
                ?? "application/octet-stream"
        );
    }

    public async Task<bool> DeleteImageAsync(
        string blobName,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        // Validate the blob filename
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return false;
        }

        // Only allow our designated containers
        if (containerName != "profile-pictures" &&
            containerName != "event-banners")
        {
            throw new ArgumentException(
                "Invalid storage container.",
                nameof(containerName));
        }

        // Prevent blob path traversal or unexpected names
        if (blobName != Path.GetFileName(blobName) ||
            blobName.Contains('\\'))
        {
            throw new ArgumentException(
                "Invalid blob filename.",
                nameof(blobName));
        }

        // Locate the container
        var container = _blobServiceClient
            .GetBlobContainerClient(containerName);

        // Locate the stored image
        var blob = container.GetBlobClient(blobName);

        // Delete the image if it exists
        var result = await blob.DeleteIfExistsAsync(
            cancellationToken: cancellationToken);

        // True if deleted, false if already missing
        return result.Value;
    }

}




