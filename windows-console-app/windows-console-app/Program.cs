using System;
using System.Collections.Generic;
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
                    NativeMethods.SwitchToThisWindow(hWnd, true);
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
