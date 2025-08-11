using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Text;

namespace web.app.Controllers;

[Authorize]
public class SseController(SseManager ssemgr, IHostApplicationLifetime lifetime) : Controller
{
    public async Task Index()
    {
        var sid = User.Identity.Name;
        try {
            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.Connection = "keep-alive";
            Response.Headers.CacheControl = "no-cache";
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted, lifetime.ApplicationStopping);
            using var stream = new StreamWriter(Response.Body, Encoding.UTF8, leaveOpen: true);
            if (await ssemgr.TryAddAsync(sid, stream, cts).ConfigureAwait(false)) {
                try {
                    await Task.Delay(Timeout.Infinite, cts.Token).ConfigureAwait(false);
                } finally {
                    ssemgr.Remove(sid);
                }
            }
        } catch (Exception) {
        }
    }
}