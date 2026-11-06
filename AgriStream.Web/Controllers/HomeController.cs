using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AgriStream.Web.Services;
namespace AgriStream.Web.Controllers;
[Authorize]
public class HomeController(AzureFarmStorage storage, FunctionApi functions) : Controller {
    public async Task<IActionResult> Index() {
        try {
            var data=await functions.GetAsync<List<TelemetryDto>>("telemetry");
            return View(data ?? new());
        } catch { return View(new List<TelemetryDto>()); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Irrigate(string deviceId,int durationMinutes,string reason) {
        var response=await functions.PostAsync("irrigation",new { deviceId,durationMinutes,reason });
        TempData["Message"]=response.IsSuccessStatusCode ? "Irrigation command queued." : "Could not queue command. Check Function App configuration.";
        return RedirectToAction(nameof(Index));
    }
    public record TelemetryDto(string deviceId,string timestamp,double moisture,double temperature);
}
