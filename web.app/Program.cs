using Microsoft.AspNetCore.Authentication;
var builder = WebApplication.CreateBuilder(args);
//builder.Logging.AddFilter("Microsoft.AspNetCore.*", LogLevel.None);
builder.Configuration.AddJsonFile("license.json", true, true);
builder.Configuration.AddJsonFile("users.json", true, true);
builder.Services.AddSingleton<SseManager>();
builder.Services.AddSingleton<ObjectCache>();
builder.Services.AddAuthentication(options => options.AddScheme<CustomAuthorizeHandler>(CustomAuthorizeHandler.AuthenticationScheme, null));
builder.Services.AddControllersWithViews();
var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseRouting();
app.UseWebSockets(new WebSocketOptions {
    KeepAliveInterval = TimeSpan.FromSeconds(15),
    KeepAliveTimeout = TimeSpan.FromSeconds(30)
});
app.UseStaticFiles(new StaticFileOptions {
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/x-msdownload"
}); 
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();