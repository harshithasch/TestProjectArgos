using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

namespace SimulatorAutomation.Core
{
    public class SimulatorLauncher : IDisposable
    {
        private UIA3Automation _automation;

        public Application VisionPlannerApplication { get; private set; }

        public Window VisionPlannerWindow { get; private set; }

        public UIA3Automation Automation => _automation;

        /// <summary>
        /// Starts Movu Simulator first, waits for it to become ready,
        /// then starts VisionPlanner and waits for its UI window.
        /// </summary>
        public Window StartSimulatorEnvironment(
            string movuShortcutPath,
            string movuProcessName,
            string visionPlannerShortcutPath,
            string visionPlannerProcessName,
            string visionPlannerWindowTitle = null)
        {
            _automation = new UIA3Automation();

            StartDesktopShortcut(
                shortcutPath: movuShortcutPath,
                displayName: "MovuSimulator");

            WaitForProcess(
                processName: movuProcessName,
                timeout: TimeSpan.FromMinutes(5));

            StartDesktopShortcut(
                shortcutPath: visionPlannerShortcutPath,
                displayName: "VisionPlanner");

            Process visionPlannerProcess = WaitForProcess(
                processName: visionPlannerProcessName,
                timeout: TimeSpan.FromMinutes(5));

            VisionPlannerApplication =
                Application.Attach(visionPlannerProcess);

            VisionPlannerWindow = WaitForApplicationWindow(
                application: VisionPlannerApplication,
                expectedWindowTitle: visionPlannerWindowTitle,
                timeout: TimeSpan.FromMinutes(5));

            VisionPlannerWindow.Focus();

            return VisionPlannerWindow;
        }

        /// <summary>
        /// Opens a Windows desktop shortcut in the same way as
        /// manually opening the shortcut from the desktop.
        /// </summary>
        private static void StartDesktopShortcut(
            string shortcutPath,
            string displayName)
        {
            if (string.IsNullOrWhiteSpace(shortcutPath))
            {
                throw new ArgumentException(
                    $"{displayName} shortcut path cannot be empty.");
            }

            if (!File.Exists(shortcutPath))
            {
                throw new FileNotFoundException(
                    $"{displayName} shortcut was not found at: " +
                    shortcutPath,
                    shortcutPath);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = shortcutPath,

                // Required when opening file types such as .lnk
                // through the Windows graphical shell.
                UseShellExecute = true,

                WorkingDirectory =
                    Path.GetDirectoryName(shortcutPath)
            };

            Process.Start(startInfo);

            Console.WriteLine(
                $"{displayName} shortcut was started: {shortcutPath}");
        }

        /// <summary>
        /// Waits until a process with the given name is running.
        /// The process name must not contain the .exe extension.
        /// </summary>
        private static Process WaitForProcess(
            string processName,
            TimeSpan timeout)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                throw new ArgumentException(
                    "Process name cannot be empty.",
                    nameof(processName));
            }

            var result = Retry.WhileNull(
                () =>
                {
                    return Process
                        .GetProcessesByName(processName)
                        .OrderByDescending(
                            process => GetSafeStartTime(process))
                        .FirstOrDefault();
                },
                timeout: timeout,
                interval: TimeSpan.FromSeconds(2),
                throwOnTimeout: false);

            Process process = result.Result;

            if (process == null)
            {
                throw new InvalidOperationException(
                    $"The process '{processName}' did not start " +
                    $"within {timeout.TotalMinutes} minutes. " +
                    "Check the process name in Task Manager.");
            }

            Console.WriteLine(
                $"Process is running: {process.ProcessName}, " +
                $"PID: {process.Id}");

            return process;
        }

        /// <summary>
        /// Waits for VisionPlanner to expose a top-level UI window.
        /// If a title is provided, the matching window is selected.
        /// </summary>
        private Window WaitForApplicationWindow(
            Application application,
            string expectedWindowTitle,
            TimeSpan timeout)
        {
            var result = Retry.WhileNull(
                () =>
                {
                    Window[] windows =
                        application.GetAllTopLevelWindows(_automation);

                    if (!string.IsNullOrWhiteSpace(
                            expectedWindowTitle))
                    {
                        return windows.FirstOrDefault(
                            window =>
                                !string.IsNullOrWhiteSpace(
                                    window.Title) &&
                                window.Title.IndexOf(
                                    expectedWindowTitle,
                                    StringComparison.OrdinalIgnoreCase)
                                >= 0);
                    }

                    return windows.FirstOrDefault(
                        window =>
                            window.IsAvailable &&
                            !string.IsNullOrWhiteSpace(
                                window.Title));
                },
                timeout: timeout,
                interval: TimeSpan.FromSeconds(2),
                throwOnTimeout: false);

            Window window = result.Result;

            if (window == null)
            {
                string titleInformation =
                    string.IsNullOrWhiteSpace(expectedWindowTitle)
                        ? "any non-empty top-level window"
                        : $"a window containing the title " +
                          $"'{expectedWindowTitle}'";

                throw new InvalidOperationException(
                    "VisionPlanner process started, but FlaUI " +
                    $"could not find {titleInformation} within " +
                    $"{timeout.TotalMinutes} minutes.");
            }

            Console.WriteLine(
                $"VisionPlanner window is ready: {window.Title}");

            return window;
        }

        private static DateTime GetSafeStartTime(Process process)
        {
            try
            {
                return process.StartTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// This cleanup disconnects FlaUI only.
        /// It intentionally does not close Movu or VisionPlanner.
        /// </summary>
        public void Dispose()
        {
            _automation?.Dispose();

            _automation = null;
            VisionPlannerApplication = null;
            VisionPlannerWindow = null;
        }
    }
}