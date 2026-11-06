using AgriStream.Web.Models;
using AgriStream.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
namespace AgriStream.Web.Controllers;
public class AccountController(SqlUserService users) : Controller {
    [HttpGet] public IActionResult Login()=>View(new LoginViewModel());
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Login(LoginViewModel model) {
        if(!ModelState.IsValid) return View(model);
        var (ok,role)=await users.ValidateAsync(model.Username,model.Password);
        if(!ok) { ModelState.AddModelError("","Invalid username or password."); return View(model); }
        var claims=new[]{new Claim(ClaimTypes.Name,model.Username),new Claim(ClaimTypes.Role,role)};
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)));
        return RedirectToAction("Index","Home");
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Logout() {
        await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login));
    }
    public IActionResult Denied()=>View();
}
