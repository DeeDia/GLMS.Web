using Microsoft.EntityFrameworkCore;
using GLMS.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Keep existing DbContext
builder.Services.AddDbContext<GlmsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Register the API base URL as a singleton so every controller uses it
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
                 ?? "http://localhost:5043";

// Register named HttpClient with SSL bypass
builder.Services.AddHttpClient("GLMSApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Bypass SSL for localhost development
    ServerCertificateCustomValidationCallback =
        (message, cert, chain, errors) => true
});

// Also register a default HttpClient with same SSL bypass
// This covers any controller that creates a plain HttpClient
builder.Services.AddHttpClient("default")
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback =
        (message, cert, chain, errors) => true
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();