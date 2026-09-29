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
        public void LaunchMOVUTest()
        {
            TestContext.Progress.WriteLine(
                "================================================");

            TestContext.Progress.WriteLine(
                "Starting MOVU launch test.");

            TestContext.Progress.WriteLine(
                "================================================");

            Assert.That(
                File.Exists(
                    _config.MovuShortcutPath),
                Is.True,
                "MOVU shortcut was not found at: " +
                _config.MovuShortcutPath);

            Window movuWindow =
                FindWindowByTitle(
                    MovuWindowTitle);

            if (movuWindow == null)
            {
                TestContext.Progress.WriteLine(
                    "MOVU is not currently running.");

                OpenWindowsShortcut(
                    _config.MovuShortcutPath);

                TestContext.Progress.WriteLine(
                    "MOVU shortcut opened.");

                /*
                 * Do not use a fixed 60-second Thread.Sleep here.
                 *
                 * WaitForWindowByTitle checks repeatedly and returns
                 * immediately when the MOVU window is available.
                 */
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

            TestContext.Progress.WriteLine(
                "MOVU window detected.");

            FocusWindowSafely(
                movuWindow,
                "MOVU");

            /*
             * Wait for the actual minimize button instead of waiting
             * for a fixed period.
             */
            AutomationElement minimizeButton =
                WaitForVisibleElementByAutomationId(
                    MovuWindowTitle,
                    MovuMinimizeButtonAutomationId,
                    MinimizeButtonSearchTimeoutMilliseconds);

            Assert.That(
                minimizeButton,
                Is.Not.Null,
                "MOVU minimize button was not found. " +
                "AutomationId: " +
                MovuMinimizeButtonAutomationId);

            TestContext.Progress.WriteLine(
                "MOVU minimize button is ready.");

            /*
             * A short stabilization wait is sufficient after the
             * real MOVU control has been detected.
             */
            Thread.Sleep(500);

            ClickElementCenterWithMouse(
                minimizeButton,
                "MOVU minimize button");

            /*
             * Reduce the post-minimize wait.
             * Vision Planner can be launched once MOVU is minimized.
             */
            Thread.Sleep(1000);

            TestContext.Progress.WriteLine(
                "MOVU launch test completed successfully.");
        }
    }
}