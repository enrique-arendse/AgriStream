
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;

namespace AgriStream.Web.Services;

public class AzureFarmStorage(IConfiguration config)
{
	private string Conn => config["Azure:StorageConnection"] ?? "";

	private TableClient Table => new(
		Conn,
		config["Azure:TableName"] ?? "FarmTelemetry"
	);

	public async Task<IReadOnlyList<TelemetryRecord>> LatestAsync(int take = 50)
	{
		if (string.IsNullOrWhiteSpace(Conn))
			return Array.Empty<TelemetryRecord>();

		var rows = new List<TelemetryRecord>();

		await foreach (var e in Table.QueryAsync<TableEntity>(maxPerPage: take))
		{
			rows.Add(new TelemetryRecord(
				e.PartitionKey,
				e.RowKey,
				e.TryGetValue("Moisture", out var m) ? Convert.ToDouble(m) : 0,
				e.TryGetValue("Temperature", out var t) ? Convert.ToDouble(t) : 0
			));
		}

		return rows
			.OrderByDescending(x => x.Timestamp)
			.Take(take)
			.ToList();
	}

	public async Task<string> UploadAsync(IFormFile file)
	{
		var service = new BlobServiceClient(Conn);

		var container = service.GetBlobContainerClient(
			config["Azure:BlobContainer"] ?? "farm-media"
		);

		await container.CreateIfNotExistsAsync();

		var safeName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}-{Path.GetFileName(file.FileName)}";

		var blob = container.GetBlobClient(safeName);

		await using var stream = file.OpenReadStream();

		await blob.UploadAsync(stream, overwrite: false);

		return blob.Uri.ToString();
	}
}

public record TelemetryRecord(
	string DeviceId,
	string Timestamp,
	double Moisture,
	double Temperature
);
