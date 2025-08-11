#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable CA1050 // 在命名空间中声明类型
#pragma warning disable CA1416 // 验证平台兼容性

using System.Management;

internal class Hwid
{
    public static uint GetParentProcessId()
    {
        using var moc = new ManagementClass("Win32_Process");
        using var instance = moc.GetInstances();
        return instance.Cast<ManagementObject>().Select(x => new {
            ProcessId = (uint)x["ProcessId"],
            ParentProcessId = (uint)x["ParentProcessId"]
        }).Single(x => x.ProcessId == Environment.ProcessId).ParentProcessId;
    }

    public static string CPU {
        get {
            try {
                using var mc = new ManagementClass("Win32_Processor");
                foreach (var mo in mc.GetInstances().Cast<ManagementObject>()) {
                    return (string)mo["Name"];
                }
            } catch (Exception) {
            }
            return "unknown";
        }
    }

    public static string[] GPU {
        get {
            List<string> list = [];
            try {
                using var mc = new ManagementClass("Win32_VideoController");
                foreach (var mo in mc.GetInstances().Cast<ManagementObject>()) {
                    list.Add((string)mo["Name"]);
                }
            } catch (Exception) {
            }
            return list.ToArray();
        }
    }
}