using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgriStream.Web.Services;
namespace AgriStream.Web.Controllers;
[Authorize]
public class MediaController(AzureFarmStorage storage) : Controller {
    [HttpGet] public IActionResult Upload()=>View();
    [HttpPost,ValidateAntiForgeryToken,RequestSizeLimit(25_000_000)]
    public async Task<IActionResult> Upload(IFormFile file) {
        if(file==null || file.Length==0) { ModelState.AddModelError("","Choose an image."); return View(); }
        var url=await storage.UploadAsync(file); ViewBag.UploadedUrl=url; return View();
    }
}
