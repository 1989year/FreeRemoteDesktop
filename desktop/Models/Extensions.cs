using System.Buffers;
using System.Data;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;

public static class Extensions
{
    public static async Task SendAsync(this ClientWebSocket websocket, Guid token, int action, ReadOnlyMemory<byte> data, CancellationToken stoppingToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(24 + data.Length);
        try {
            token.TryWriteBytes(buffer.AsSpan(0, 16));
            MemoryMarshal.Write(buffer.AsSpan(16, 4), action);
            MemoryMarshal.Write(buffer.AsSpan(20, 4), data.Length);
            if (data.Length > 0) {
                data.CopyTo(buffer.AsMemory(24));
            }
            await websocket.SendAsync(buffer, WebSocketMessageType.Binary, true, stoppingToken).ConfigureAwait(false);
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static void SetDoubleBuffered(this Control control)
    {
        control.GetType().GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(control, true, null);
    }

    public static void SetCenterLocation(this Control control, Form from)
    {
        control.Location = new Point((from.ClientSize.Width - control.Width) / 2, (from.ClientSize.Height - control.Height) / 2);
    }

    public static string Substring(this string _this, string start, string end)
    {
        var st = _this.IndexOf(start);
        if (st == -1) return null;
        st += start.Length;
        var ed = _this.IndexOf(end, st);
        if (ed == -1) return null;
        return _this.Substring(st, ed - st);
    }


    public static string GetArgs(this object _, string name)
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1) {
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == name && args.Length >= i + 1) {
                    return args[i + 1];
                }
            }
        }
        return string.Empty;
    }

    public static T Get<T>(this object obj, string key)
    {
        try {
            if (obj is DataRowView drv) {
                return (T)drv.Row[key];
            }
        } catch (Exception) {
        }
        return default;
    }

    public static string Query(this string[] args, string key, string defaultValue = null)
    {
        try {
            for (int i = 0; i < args.Length; i++) {
                if (args[i] == key) {
                    return args[i + 1];
                }
            }
        } catch (Exception) {
        }
        return defaultValue;
    }

    public static void AddSystemMenu(this Form form, int uFlags = 0, uint uIDNewItem = 0, string lpNewItem = null)
    {
        form.Invoke(() => {
            IntPtr systemMenuHandle = User32.GetSystemMenu(form.Handle, false);
            if (systemMenuHandle == IntPtr.Zero) {
                return;
            }
            if (lpNewItem == null) {
                User32.InsertMenu(systemMenuHandle, 0, 0x00000800 | 0x00000400, 0, string.Empty);
            } else {
                User32.InsertMenu(systemMenuHandle, 0, uFlags, uIDNewItem, lpNewItem);
            }
        });
    }

    public static bool SetSystemMenuCheckState(this Form form, int uIDNewItem, bool enable)
    {
        return form.Invoke(() => {
            IntPtr hwnd = User32.GetSystemMenu(form.Handle, false);
            if (hwnd == IntPtr.Zero) {
                return false;
            }
            User32.CheckMenuItem(hwnd, uIDNewItem, enable ? 0x00000008 : 0x00000000);
            return enable;
        });
    }

    public static bool GetSystemMenuCheckState(this Form form, int uIDNewItem)
    {
        return form.Invoke(() => {
            IntPtr hwnd = User32.GetSystemMenu(form.Handle, false);
            if (hwnd == IntPtr.Zero) {
                return false;
            }
            return (User32.GetMenuState(hwnd, uIDNewItem, 0x00000008) & 0x00000008) == 0x00000008;
        });
    }

    public static void EnableMenuItem(this Form form, int uIDNewItem, bool isenable)
    {
        form.Invoke(() => {
            IntPtr hwnd = User32.GetSystemMenu(form.Handle, false);
            if (hwnd == IntPtr.Zero) {
                return false;
            }
            return User32.EnableMenuItem(hwnd, uIDNewItem, isenable ? 0x00000001 : 0x00000000);
        });
    }
}