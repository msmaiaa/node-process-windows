var exec = require("child_process").exec;
var path = require("path");

var windowsFocusManagementBinary = path.join(
  __dirname,
  "windows-console-app",
  "windows-console-app",
  "bin",
  "Release",
  "windows-console-app.exe"
);

var isWindows = process.platform === "win32";

/**
 * @typedef {{ProcessId?: number, MainWindowTitle?: string, ProcessName?: string}} ProcessInfo
 */

/**
 * Get list of processes that are currently running
 *
 * @returns {ProcessInfo[]}
 */
function getProcesses() {
  if (!isWindows) {
    throw new Error("Non-Windows platforms are currently not supported");
  }

  executeProcess("--list", callback);
}

function mappingFunction(processes) {
  return processes.map(p => {
    return {
      pid: p.ProcessId,
      mainWindowTitle: p.MainWindowTitle || "",
      processName: p.ProcessName || ""
    };
  });
}

/**
 * Focus a windows
 * Process can be a number (PID), name (process name or window title),
 * or a process object returning from getProcesses
 *
 * @param {number|string|ProcessInfo} process
 */
function focusWindow(process) {
  if (!isWindows) {
    throw "Non-windows platforms are currently not supported";
  }

  if (process === null) return;

  if (typeof process === "number") {
    executeProcess(`--focus --pid ${process.toString()}`);
  } else if (typeof process === "string") {
    executeProcess(`--focus --name ${process.toString()}`);
  } else if (
    process.ProcessId ||
    process.MainWindowTitle ||
    process.ProcessName
  ) {
    let command = "--focus";
    if (process.ProcessId) {
      command + ` --pid ${process.ProcessId}`;
    }
    if (process.MainWindowTitle) {
      command + ` --name ${process.MainWindowTitle}`;
    }
    if (process.ProcessName) {
      command + ` --class ${process.ProcessName}`;
    }
    executeProcess(command);
  }
}

/**
 * Helper method to execute the C# process that wraps the native focus / window APIs
 */
function executeProcess(arg, callback) {
  callback = callback || (() => {});

  exec(windowsFocusManagementBinary + " " + arg, (error, stdout, stderr) => {
    if (error) {
      throw new Error(error);
    }

    if (stderr) {
      throw new Error(stderr);
    }

    var returnObject = JSON.parse(stdout);

    if (returnObject.Error) {
      callback(returnObject.Error, null);
      return;
    }

    var ret = returnObject.Result;
    callback(null, mappingFunction(ret));
  });
}

module.exports = {
  getProcesses: getProcesses,
  focusWindow: focusWindow
};
