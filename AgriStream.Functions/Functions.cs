using System.Net;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AgriStream.Functions;

public class Functions(IConfiguration config, ILogger<Functions> logger) {
    private string Storage => config["FarmStorage"] ?? config["AzureWebJobsStorage"] ?? throw new InvalidOperationException("FarmStorage missing.");
    private TableClient TelemetryTable => new(Storage, config["TelemetryTable"] ?? "FarmTelemetry");
    private TableClient AuditTable => new(Storage, config["AuditTable"] ?? "ProcessingAudit");

    // 1 of exactly 4: Blob trigger - audit uploaded drone media in Table Storage.
    [Function("DroneImageAudit")]
    public async Task DroneImageAudit([BlobTrigger("farm-media/{name}", Connection="FarmStorage")] Stream image, string name) {
        var row=new TableEntity("BlobUpload",Guid.NewGuid().ToString("N")) {
            ["BlobName"]=name, ["Bytes"]=image.Length, ["Status"]="Received",
            ["ProcessedUtc"]=DateTimeOffset.UtcNow, ["ContentType"]="image/*"
        };
        await AuditTable.CreateIfNotExistsAsync();
        await AuditTable.AddEntityAsync(row);
        logger.LogInformation("Audited blob {BlobName}, {Bytes} bytes",name,image.Length);
    }

    // 2 of exactly 4: HTTP GET trigger - latest sensor records.
    [Function("GetTelemetry")]
    public async Task<HttpResponseData> GetTelemetry([HttpTrigger(AuthorizationLevel.Function,"get",Route="telemetry")] HttpRequestData req) {
        var response=req.CreateResponse(HttpStatusCode.OK);
        var rows=new List<object>();
        await foreach(var e in TelemetryTable.QueryAsync<TableEntity>(maxPerPage:100)) {
            rows.Add(new { deviceId=e.PartitionKey, timestamp=e.RowKey,
                moisture=e.TryGetValue("Moisture",out var m)?Convert.ToDouble(m):0,
                temperature=e.TryGetValue("Temperature",out var t)?Convert.ToDouble(t):0 });
        }
        var latest=rows.TakeLast(50).ToList();
        await response.WriteAsJsonAsync(latest);
        return response;
    }

    // 3 of exactly 4: HTTP POST trigger - enqueue irrigation command.
    [Function("PostIrrigation")]
    public async Task<HttpResponseData> PostIrrigation([HttpTrigger(AuthorizationLevel.Function,"post",Route="irrigation")] HttpRequestData req) {
        var command=await JsonSerializer.DeserializeAsync<IrrigationCommand>(req.Body,new JsonSerializerOptions{PropertyNameCaseInsensitive=true});
        if(command is null || string.IsNullOrWhiteSpace(command.DeviceId) || command.DurationMinutes is <1 or >120) {
            var bad=req.CreateResponse(HttpStatusCode.BadRequest); await bad.WriteStringAsync("A deviceId and durationMinutes from 1 to 120 are required."); return bad;
        }
        var queue=new QueueClient(Storage,config["IrrigationQueue"] ?? "irrigation-commands");
        await queue.CreateIfNotExistsAsync();
        await queue.SendMessageAsync(JsonSerializer.Serialize(command));
        var ok=req.CreateResponse(HttpStatusCode.Accepted);
        await ok.WriteAsJsonAsync(new { status="Queued", command.DeviceId, commandId=command.CommandId });
        return ok;
    }

    // 4 of exactly 4: Queue trigger - persist command result in Azure SQL registry.
    [Function("ProcessIrrigation")]
    public async Task ProcessIrrigation([QueueTrigger("irrigation-commands",Connection="FarmStorage")] string message) {
        var json=message; // host.json sets messageEncoding to none; the trigger supplies the JSON text.
        var command=JsonSerializer.Deserialize<IrrigationCommand>(json,new JsonSerializerOptions{PropertyNameCaseInsensitive=true})
            ?? throw new InvalidOperationException("Invalid irrigation message.");
        var cs=config["SqlConnection"] ?? throw new InvalidOperationException("SqlConnection missing.");
        await using var connection=new SqlConnection(cs); await connection.OpenAsync();
        const string sql=@"
MERGE dbo.DeviceRegistry AS target
USING (SELECT @DeviceId AS DeviceId) AS source ON target.DeviceId=source.DeviceId
WHEN MATCHED THEN UPDATE SET OperationalStatus='IrrigationQueued', LastCommandUtc=SYSUTCDATETIME(), LastCommandId=@CommandId
WHEN NOT MATCHED THEN INSERT(DeviceId,OperationalStatus,LastCommandUtc,LastCommandId)
VALUES(@DeviceId,'IrrigationQueued',SYSUTCDATETIME(),@CommandId);";
        await using var cmd=new SqlCommand(sql,connection);
        cmd.Parameters.AddWithValue("@DeviceId",command.DeviceId);
        cmd.Parameters.AddWithValue("@CommandId",command.CommandId);
        await cmd.ExecuteNonQueryAsync();
        logger.LogInformation("Updated registry for irrigation command {CommandId}",command.CommandId);
    }
}
public record IrrigationCommand(string DeviceId,int DurationMinutes,string Reason) {
    public string CommandId { get; init; } = Guid.NewGuid().ToString("N");
}
