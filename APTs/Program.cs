using Microsoft.Extensions.Options;
using System.Net;
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService();
builder.Services.AddTransient<CustomAssemblyLoadContext>();
if (args.Length > 0) {
    builder.Logging.ClearProviders();
}
builder.Services.AddHttpClient(Options.DefaultName, cfg => {
    cfg.BaseAddress = new Uri("");//https://xxxxx.tun.pub/
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler {
    AutomaticDecompression = DecompressionMethods.All,
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
});
builder.Services.AddHostedService<Service>();
var host = builder.Build();
host.Run();