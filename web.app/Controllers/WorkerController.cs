using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace web.app.Controllers;

public partial class WorkerController(ObjectCache cache, IMemoryCache mcache, IHostApplicationLifetime lifetime, SseManager sseMgr) : Controller
{
    public static readonly ConcurrentDictionary<Guid, CustomClientContext> _session = [];
    public static readonly ConcurrentDictionary<Guid, TaskCompletionSource<byte[]>> _tasks = [];

    [Authorize]
    public async Task<IActionResult> Control(Guid id, int cmd)
    {
        var taskid = Guid.CreateVersion7();
        var tcs = new TaskCompletionSource<byte[]>();
        try {
            if (_session.TryGetValue(id, out var ctx)) {
                switch (cmd) {
                    case 0x01:
                        await ctx.WebSocket.SendAsync(0x01, taskid.ToByteArray(), CancellationToken.None);
                        if (_tasks.TryAdd(taskid, tcs)) {
                            var bytes = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(15), HttpContext.RequestAborted).ConfigureAwait(false);
                            return Json(new {
                                Url = $"ws2://{Uri.EscapeDataString(Convert.ToBase64String(bytes))}",
                                Code = 200
                            });
                        }
                        break;
                    case 0x03:
                        await ctx.WebSocket.SendAsync(0x03, taskid.ToByteArray(), CancellationToken.None);
                        if (_tasks.TryAdd(taskid, tcs)) {
                            var bytes = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(15), HttpContext.RequestAborted).ConfigureAwait(false);
                            return Json(new {
                                Url = $"ws2://{Uri.EscapeDataString(Convert.ToBase64String(bytes))}",
                                Code = 200
                            });
                        }
                        break;
                    case 0xff:
                        await ctx.WebSocket.SendAsync(0xff, ReadOnlyMemory<byte>.Empty, CancellationToken.None);
                        break;
                }
            }
            return Json(new {
                Code = 200
            });
        } catch (Exception ex) {
            return Json(new {
                code = 500,
                ex.Message
            });
        }
    }

    private async Task ProcessMessageAsync(CustomClientContext ctx, int action, ReadOnlyMemory<byte> data)
    {
        // worker register
        if (action == 0x00) {
            ctx.OSInformation = JsonSerializer.Deserialize<CustomOSInformation>(data.Span);
            if (cache.TryGetValue<CustomClientCacheContext>(ctx.Id, out var value)) {
                ctx.Note = value.Note;
                ctx.Group = value.Group;
            }
        }

        // worker RDP
        if (action == 0x01) {
            var taskid = MemoryMarshal.Read<Guid>(data[..16].Span);
            var cid = Guid.CreateVersion7();
            mcache.Set(cid, Encoding.UTF8.GetString(data[16..].Span), TimeSpan.FromMinutes(1));
            if (_tasks.TryGetValue(taskid, out var tcs)) {
                tcs.TrySetResult(JsonSerializer.SerializeToUtf8Bytes(new {
                    Token = ctx.Id,
                    Route = "desktop",
                    Url = $"http://{HttpContext.Request.Host}/cache?id={cid}"
                }));
            }
        }

        // worker Shell
        if (action == 0x03) {
            var taskid = MemoryMarshal.Read<Guid>(data[..16].Span);
            if (_tasks.TryGetValue(taskid, out var tcs)) {
                tcs.TrySetResult(JsonSerializer.SerializeToUtf8Bytes(new {
                    Token = ctx.Id,
                    Route = "shell",
                    Url = Encoding.UTF8.GetString(data[16..].Span)
                }));
            }
        }

        // worker restart
        if (action == 0xff) {

        }

        await Task.CompletedTask;
    }

    public async Task Index(Guid id)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest) {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        if (!HttpContext.Request.Headers.TryGetValue("aes-key", out var hex)) {
            HttpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
            return;
        }
        var key = Convert.FromBase64String(hex);
        using var websocket = new CustomWebSocket(await HttpContext.WebSockets.AcceptWebSocketAsync(), key);
        if (id == Guid.Empty) {
            id = Guid.CreateVersion7();
        }
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.ApplicationStopping, HttpContext.RequestAborted);
        var ctx = new CustomClientContext {
            Id = id,
            WebSocket = websocket
        };
        if (_session.TryAdd(id, ctx)) {
            try {
                await websocket.SendAsync(0x00, id.ToByteArray(), cts.Token).ConfigureAwait(false);
                await websocket.ReceiveAsync((x, y) => ProcessMessageAsync(ctx, x, y), cts.Token).ConfigureAwait(false);
            } catch (Exception) {
            } finally {
                try {
                    cts.Cancel();
                } catch (Exception) {
                }
                try {
                    _ = sseMgr.PublishAsync(true).ConfigureAwait(false);
                } catch (Exception) {
                }
                _session.TryRemove(id, out _);
            }
        }
    }
}

public partial class WorkerController
{
    private static readonly ConcurrentDictionary<Guid, RelaySessionContext> _map = [];

    [Authorize]
    public IActionResult Device(Guid id, string note, string group, bool recyclebin)
    {
        if (_session.TryGetValue(id, out var ctx)) {
            if (Request.Method == "GET") {
                return View(ctx);
            }
            if (recyclebin) {
                group = "回收站";
            } else if (string.IsNullOrWhiteSpace(group)) {
                group = "默认分组";
            }
            try {
                ctx.Note = note;
                ctx.Group = group;
                cache.Set(id, new CustomClientCacheContext {
                    Group = group,
                    Note = note
                });
                return Ok(new {
                    Code = 200,
                    Message = "操作成功"
                });
            } catch (Exception ex) {
                return Json(new {
                    code = 500,
                    ex.Message
                });
            }
        }
        return BadRequest();
    }

    [Authorize]
    public IActionResult List(string group, int page = 1, int size = 20)
    {
        int skip = (page - 1) * size;
        if (string.IsNullOrWhiteSpace(group)) {
            return View(_session.Values.Where(x => x.Group != "回收站" && x.OSInformation != null)
               .OrderByDescending(x => x.Time).Skip(skip).Take(size));
        }
        return View(_session.Values.Where(x => x.OSInformation != null && x.Group == group)
            .OrderByDescending(x => x.Time).Skip(skip).Take(size));
    }

    [Route("/worker/relay"), Route("/v1/bridge")]
    public async Task Relay(Guid token)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest) {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        var websocket = await HttpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.ApplicationStopping, HttpContext.RequestAborted);
        if (token == Guid.Empty) {
            token = Guid.CreateVersion7();
            await websocket.SendAsync(token.ToByteArray(), WebSocketMessageType.Binary, true, cts.Token).ConfigureAwait(false);
            using var ctx = new RelaySessionContext(websocket);
            if (_map.TryAdd(token, ctx)) {
                try {
                    var target = await ctx.TaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(30), cts.Token).ConfigureAwait(false);
                    await target.CopyToAsync(websocket, cts.Token).ConfigureAwait(false);
                } finally {
                    _map.TryRemove(token, out _);
                }
            }
        } else if (_map.TryRemove(token, out var target) && target.TaskCompletionSource.TrySetResult(websocket)) {
            await target.WebSocket.CopyToAsync(websocket, cts.Token).ConfigureAwait(false);
        }
    }

    private class RelaySessionContext(WebSocket websocket) : IDisposable
    {
        private bool _disposed;
        public readonly TaskCompletionSource<WebSocket> TaskCompletionSource = new TaskCompletionSource<WebSocket>(TaskCreationOptions.RunContinuationsAsynchronously);

        public WebSocket WebSocket { get; } = websocket;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, true)) {
                return;
            }
            try {
                TaskCompletionSource.TrySetCanceled();
            } catch (Exception) {
            }
            GC.SuppressFinalize(this);
        }
    }
}