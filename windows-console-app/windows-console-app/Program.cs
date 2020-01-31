using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Text;
using Newtonsoft.Json;

namespace windows_console_app
{
    public class ProcessInfo
    {
        public uint ProcessId { get; set; }

        public string MainWindowTitle { get; set; }

        public string ProcessName { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
                throw new ArgumentException("Please specify some arguments: --active, --list, --focus and then --name <name>, --pid <pid>, --class <classname>");

            List<ProcessInfo> processes = new List<ProcessInfo>();
            bool focus = false;
            try
            {
                switch (args[0].ToLower())
                {
                case "--active":
                    processes.Add(GetActiveProcess());
                    break;
                case "--list":
                    processes.AddRange(GetAllProcesses());
                    break;
                case "--focus":
                    focus = true;
                    break;

                default:
                    throw new ArgumentException("Unknown argument");
                }

                if (focus)
                {
                    new UIPermission(UIPermissionWindow.AllWindows).Demand();
                    new UIPermission(UIPermissionWindow.AllWindows).Assert();

                    uint? pid = null;
                    string partialTitle = "";
                    string partialClass = "";

                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i] == "--pid")
                        {
                            pid = uint.Parse(args[i + 1]);
                        }
                        else if (args[i] == "--name")
                        {
                            partialTitle = args[i + 1];
                        }
                        else if (args[i] == "--class")
                        {
                            partialClass = args[i + 1];
                        }
                    }
                    processes.AddRange(Focus(pid, partialTitle, partialClass));
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return;
            }

            Console.WriteLine(JsonConvert.SerializeObject(processes));
        }

        private static List<ProcessInfo> Focus(uint? pid, string partialTitle, string partialClass)
        {
            List<ProcessInfo> processes = new List<ProcessInfo>();
            NativeMethods.EnumDesktopWindows(IntPtr.Zero, (IntPtr hWnd, int lParam) =>
            {
                var processInfo = GetProcessInfo(hWnd);

                if (pid.HasValue && lParam != pid)
                    return true;
                if (!string.IsNullOrEmpty(partialTitle) && processInfo.MainWindowTitle.IndexOf(partialTitle, StringComparison.OrdinalIgnoreCase) != 0)
                    return true;
                if (!string.IsNullOrEmpty(partialClass) && processInfo.ProcessName.IndexOf(partialClass, StringComparison.OrdinalIgnoreCase) != 0)
                    return true;

                if (NativeMethods.IsWindowVisible(hWnd))
                {
                    processes.Add(processInfo);

                    //https://stackoverflow.com/a/34414846/3492994
                    IntPtr currentForegroundWindow = NativeMethods.GetForegroundWindow();
                    uint currentForegroundThread = NativeMethods.GetWindowThreadProcessId(hWnd, out uint _);
                    uint thisThread = NativeMethods.GetCurrentThreadId();

                    NativeMethods.AttachThreadInput(thisThread, currentForegroundThread, true);
                    const int SWP_ASYNCWINDOWPOS = 0x4000;
                    const int SWP_DEFERERASE = 0x2000;
                    const int SWP_DRAWFRAME = 0x0020;
                    const int SWP_FRAMECHANGED = 0x0020;
                    const int SWP_HIDEWINDOW = 0x0080;
                    const int SWP_NOACTIVATE = 0x0010;
                    const int SWP_NOCOPYBITS = 0x0100;
                    const int SWP_NOMOVE = 0x0002;
                    const int SWP_NOOWNERZORDER = 0x0200;
                    const int SWP_NOREDRAW = 0x0008;
                    const int SWP_NOREPOSITION = 0x0200;
                    const int SWP_NOSENDCHANGING = 0x0400;
                    const int SWP_NOSIZE = 0x0001;
                    const int SWP_NOZORDER = 0x0004;
                    const int SWP_SHOWWINDOW = 0x0040;

                    const int HWND_TOP = 0;
                    const int HWND_BOTTOM = 1;
                    const int HWND_TOPMOST = -1;
                    const int HWND_NOTOPMOST = -2;

                    NativeMethods.SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
                    NativeMethods.SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
                    NativeMethods.SetForegroundWindow(hWnd);

                    const int SW_MAXIMIZE = 3;
                    const int SW_RESTORE = 9;
                    NativeMethods.ShowWindow(hWnd, SW_RESTORE); 
                    NativeMethods.ShowWindow(hWnd, SW_MAXIMIZE);

                    NativeMethods.AttachThreadInput(thisThread, currentForegroundThread, false);
                    NativeMethods.SetFocus(hWnd);
                    NativeMethods.SetActiveWindow(hWnd);

                    NativeMethods.BringWindowToTop(hWnd);
                    NativeMethods.SwitchToThisWindow(hWnd, false);
                }
                return true;
            }, IntPtr.Zero);
            return processes;
        }


        private static ProcessInfo GetActiveProcess()
        {
            var activeForegroundWindow = NativeMethods.GetForegroundWindow();
            return GetProcessInfo(activeForegroundWindow);
        }

        private static List<ProcessInfo> GetAllProcesses()
        {
            // TODO: Multiple desktops https://stackoverflow.com/questions/17321363/how-can-i-get-a-list-of-processes-running-across-multiple-virtual-desktops

            List<ProcessInfo> processes = new List<ProcessInfo>();

            NativeMethods.EnumDesktopWindows(IntPtr.Zero, (IntPtr hWnd, int lParam) =>
            {
                string title = GetTitle(hWnd);
                if (NativeMethods.IsWindowVisible(hWnd) && !string.IsNullOrEmpty(title))
                {
                    processes.Add(GetProcessInfo(hWnd));
                }
                return true;
            }, IntPtr.Zero);

            return processes;
        }

        private static ProcessInfo GetProcessInfo(IntPtr hWnd)
        {
            NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
            return new ProcessInfo()
            {
                ProcessId = processId,
                MainWindowTitle = GetTitle(hWnd),
                ProcessName = GetName(hWnd)
            };
        }

        private static string GetTitle(IntPtr hWnd)
        {
            StringBuilder sb = new StringBuilder(1024);
            int length = NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        private static string GetName(IntPtr hWnd)
        {
            StringBuilder sb = new StringBuilder(1024);
            int length = NativeMethods.GetClassName(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }
    }
}
