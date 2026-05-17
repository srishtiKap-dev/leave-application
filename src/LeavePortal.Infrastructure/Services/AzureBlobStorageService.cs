using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using LeavePortal.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LeavePortal.Infrastructure.Services;

public sealed class AzureBlobStorageService(IConfiguration config) : IBlobStorageService
{
    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var connectionString = config["FileStorage:ConnectionString"] ?? config.GetConnectionString("Storage");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");

        var containerName = config["FileStorage:ContainerName"] ?? config["Storage:ReceiptsContainer"] ?? "receipts";
        var container = new BlobContainerClient(connectionString, containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: ct);

        var blob = container.GetBlobClient($"{Guid.NewGuid():N}-{Path.GetFileName(fileName)}");
        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
        return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1)).ToString();
    }

    public Task<string> CreateReadSasAsync(string blobUrl, TimeSpan ttl, CancellationToken ct = default)
    {
        var blob = new BlobClient(new Uri(blobUrl));
        return Task.FromResult(blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(ttl)).ToString());
    }
}
