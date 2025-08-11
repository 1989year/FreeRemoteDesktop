#pragma warning disable SYSLIB1054 // 使用 “LibraryImportAttribute” 而不是 “DllImportAttribute” 在编译时生成 P/Invoke 封送代码
#pragma warning disable CA2101 // 指定对 P/Invoke 字符串参数进行封送处理
using System.Runtime.InteropServices;

public static class PMQ
{
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int Y, int cx, int cy, uint uFlags);

    const uint SWP_SHOWWINDOW = 0x0040;
    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int Width, int Height, bool Repaint);
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    public static void Start(int windowWidth = 250, int windowHeight = 250)
    {
        List<IntPtr> targetWindows = [];
        EnumWindows((hWnd, lParam) => {
            if (IsWindowVisible(hWnd)) {
                var sb = new System.Text.StringBuilder(1024);
                _ = GetWindowText(hWnd, sb, sb.Capacity);
                string windowTitle = sb.ToString();
                if (Guid.TryParse(windowTitle, out _)) {
                    targetWindows.Add(hWnd);
                }
            }
            return true;
        }, IntPtr.Zero);


        int gap = 0;
        int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        // int screenHeight = Screen.PrimaryScreen.Bounds.Height;

        // 计算每行显示的窗口数量
        int columns = (int)Math.Floor((double)screenWidth / (windowWidth + gap));
        if (columns < 1) columns = 1;

        // 计算行数
        int rows = (targetWindows.Count + columns - 1) / columns;

        // 调整窗口位置和大小
        for (int i = 0; i < targetWindows.Count; i++) {
            int col = i % columns;
            int row = i / columns;

            // 计算窗口坐标
            int x = 5 + col * (windowWidth + gap);
            int y = 10 + row * (windowHeight + gap);

            // 调整窗口大小和位置
            MoveWindow(targetWindows[i], x, y, windowWidth, windowHeight, true);

            // 激活窗口（可选）
            ShowWindow(targetWindows[i], 9); // SW_SHOWDEFAULT
        }
    }
}