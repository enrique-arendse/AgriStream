using AgriStream.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => { o.LoginPath = "/Account/Login"; o.AccessDeniedPath = "/Account/Denied"; o.SlidingExpiration = true; });
builder.Services.AddAuthorization();
builder.Services.AddScoped<SqlUserService>();
builder.Services.AddSingleton<AzureFarmStorage>();
builder.Services.AddHttpClient<FunctionApi>();
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection(); app.UseStaticFiles(); app.UseRouting();
app.UseAuthentication(); app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
