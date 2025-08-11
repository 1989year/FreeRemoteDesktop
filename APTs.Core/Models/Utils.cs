#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable CA1050 // 在命名空间中声明类型
#pragma warning disable CA1416 // 验证平台兼容性

using System.Diagnostics;
using System.Runtime.InteropServices;

internal class Utils
{
    public static IntPtr DumpWinlogonToken()
    {
        foreach (var explorer in Process.GetProcessesByName("explorer")) {
            foreach (var winlogon in Process.GetProcessesByName("winlogon")) {
                if (winlogon.SessionId == explorer.SessionId) {
                    if (OpenProcessToken(winlogon.Handle, 0x0002, out IntPtr hToken)) {
                        try {
                            SECURITY_ATTRIBUTES sa = new() {
                                Length = Marshal.SizeOf<SECURITY_ATTRIBUTES>()
                            };
                            IntPtr hDumpToken = IntPtr.Zero;
                            if (DuplicateTokenEx(hToken, 0x2000000, ref sa, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, TOKEN_TYPE.TokenPrimary, ref hDumpToken)) {
                                return hDumpToken;
                            }
                        } finally {
                            CloseHandle(hToken);
                        }
                    }
                }
            }
        }
        return IntPtr.Zero;
    }

    public static bool CreateProcessAsCurrentUser(string str)
    {
        var hToken = DumpWinlogonToken();
        if (hToken == IntPtr.Zero) {
            return false;
        }
        try {
            var si = new STARTUPINFO {
                cb = Marshal.SizeOf<STARTUPINFO>(),
                dwFlags = 0x00000001,
                wShowWindow = 0,
                lpDesktop = "winsta0\\Default"
            };
            bool success = CreateProcessAsUser(hToken, null, str, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out PROCESS_INFORMATION pi);
            if (success) {
                CloseHandle(pi.hThread);
                CloseHandle(pi.hProcess);
            }
            return success;
        } finally {
            CloseHandle(hToken);
        }
    }

    [DllImport("kernel32.dll")]
    public static extern bool ProcessIdToSessionId(int dwProcessId, out uint pSessionId);

    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(IntPtr hObject);

    public enum TOKEN_TYPE : uint
    {
        TokenPrimary = 1,
        TokenImpersonation = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public uint wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [DllImport("Advapi32.dll")]
    extern static bool CreateProcessAsUser(IntPtr hToken,
        string lpApplicationName,
        string lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandle,
        int dwCreationFlags,
        IntPtr lpEnvironment,
        string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    [DllImport("advapi32.dll")]
    static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [StructLayout(LayoutKind.Sequential)]
    struct SECURITY_ATTRIBUTES
    {
        public int Length;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }

    [DllImport("advapi32.dll")]
    extern static bool DuplicateTokenEx(IntPtr hExistingToken,
        uint dwDesiredAccess,
        ref SECURITY_ATTRIBUTES lpTokenAttributes,
        SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
        TOKEN_TYPE TokenType,
        ref IntPtr phNewToken);

    enum SECURITY_IMPERSONATION_LEVEL
    {
        SecurityAnonymous,
        SecurityIdentification,
        SecurityImpersonation,
        SecurityDelegation
    }
}