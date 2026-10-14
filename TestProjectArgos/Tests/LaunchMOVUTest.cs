using System.IO;
using System.Threading;
using FlaUI.Core.AutomationElements;
using NUnit.Framework;
using SimulatorAutomation.Tests.Framework;

namespace SimulatorAutomation.Tests
{
    public partial class SimulatorWorkflowTests
    {
        [Test]
        [Order(1)]
        [Category(TestSuiteNames.LaunchApplications)]
        public void LaunchMOVUtest()
        {
            RunLaunchMovuWorkflow();
        }

        public void RunLaunchMovuWorkflow()
        {
            TestContext.Progress.WriteLine(
                "================================================");
            TestContext.Progress.WriteLine(
                "Starting MOVU launch workflow.");
            TestContext.Progress.WriteLine(
                "================================================");

            Assert.That(
                File.Exists(_config.MovuShortcutPath),
                Is.True,
                "MOVU shortcut was not found at: " +
                _config.MovuShortcutPath);

            Window movuWindow =
                FindWindowByTitle(MovuWindowTitle);

            if (movuWindow == null)
            {
                TestContext.Progress.WriteLine(
                    "MOVU is not currently running.");

                OpenWindowsShortcut(
                    _config.MovuShortcutPath);

                movuWindow =
                    WaitForWindowByTitle(
                        MovuWindowTitle,
                        MovuWindowSearchTimeoutMilliseconds);
            }
            else
            {
                TestContext.Progress.WriteLine(
                    "MOVU is already running.");
            }

            Assert.That(
                movuWindow,
                Is.Not.Null,
                "MOVU window was not found.");

            FocusWindowSafely(
                movuWindow,
                "MOVU");

            AutomationElement minimizeButton =
                WaitForVisibleElementByAutomationId(
                    MovuWindowTitle,
                    MovuMinimizeButtonAutomationId,
                    MinimizeButtonSearchTimeoutMilliseconds);

            Assert.That(
                minimizeButton,
                Is.Not.Null,
                "MOVU minimize button was not found. AutomationId: " +
                MovuMinimizeButtonAutomationId);

            Thread.Sleep(500);

            ClickElementCenterWithMouse(
                minimizeButton,
                "MOVU minimize button");

            Thread.Sleep(1000);

            TestContext.Progress.WriteLine(
                "MOVU launch workflow completed successfully.");
        }
    }
}