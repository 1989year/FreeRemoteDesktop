using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace web.app.Controllers;

public class ApiController(IMemoryCache cache) : Controller
{
    [Route("/{rgb:guid}")]
    public IActionResult Apts_Code(Guid rgb)
    {
        //byte[] bytes = rgb.ToByteArray();
        //var a = BitConverter.ToInt32(bytes.AsSpan(0, 4));
        //var b = BitConverter.ToInt16(bytes.AsSpan(4, 2));
        //var timestamp = (((long)a) << 16) + b;
        //var guidTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
        //if (DateTime.UtcNow - guidTime > TimeSpan.FromHours(1)) {
        //    return NotFound();
        //}
        var fb = System.IO.File.OpenRead(Path.Combine(App.CurrentDirectory, "wwwroot", "apts.core"));
        return File(fb, "application/x-msdownload");
    }

    [Route("/cache/")]
    public IActionResult GetCacheValue(Guid id)
    {
        return cache.TryGetValue(id, out string value) ? Ok(value) : NotFound();
    }
}