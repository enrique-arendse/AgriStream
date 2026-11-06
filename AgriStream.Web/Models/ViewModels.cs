using System.ComponentModel.DataAnnotations;
namespace AgriStream.Web.Models;
public class LoginViewModel {
    [Required] public string Username { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
}
public class TelemetryViewModel {
    public string DeviceId { get; set; } = "";
    public double Moisture { get; set; }
    public double Temperature { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}
public class IrrigationViewModel {
    [Required] public string DeviceId { get; set; } = "";
    [Range(1, 120)] public int DurationMinutes { get; set; } = 10;
    public string Reason { get; set; } = "Manual dashboard request";
}
