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
        [Order(2)]
        [Category(TestSuiteNames.LaunchApplications)]
        public void LaunchVPTest()
        {
            TestContext.Progress.WriteLine(
                "================================================");

            TestContext.Progress.WriteLine(
                "Starting Vision Planner launch test.");

            TestContext.Progress.WriteLine(
                "================================================");

            Window visionPlannerWindow =
                FindWindowByTitle(
                   VisionPlannerWindowTitle);

            if (visionPlannerWindow == null)
            {
                Assert.That(
                    File.Exists(
                        _config.VisionPlannerShortcutPath),
                    Is.True,
                    "Vision Planner shortcut was not found at: " +
                    _config.VisionPlannerShortcutPath);

                OpenWindowsShortcut(
                    _config.VisionPlannerShortcutPath);

                TestContext.Progress.WriteLine(
                    "Vision Planner shortcut opened.");

                TestContext.Progress.WriteLine(
                    "Approve the UAC prompt manually if displayed.");

                visionPlannerWindow =
                    WaitForWindowByTitle(
                       VisionPlannerWindowTitle,
                        VisionPlannerWindowSearchTimeoutMilliseconds);
            }
            else
            {
                TestContext.Progress.WriteLine(
                    "Vision Planner is already running.");
            }

            Assert.That(
                visionPlannerWindow,
                Is.Not.Null,
                "Vision Planner window was not found.");

            FocusWindowSafely(
                visionPlannerWindow,
                "Vision Planner");

            /*
             * Important for the full workflow.
             * Vision Planner can expose its window before the WPF
             * carousel is ready to accept clicks.
             */
            TestContext.Progress.WriteLine(
                "Waiting for Vision Planner carousel to become ready.");

            Thread.Sleep(20000);

            TestContext.Progress.WriteLine(
                "Vision Planner launch test completed successfully.");
        }
    }
}