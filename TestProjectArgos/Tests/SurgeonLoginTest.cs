using System;
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
        [Order(3)]
        [Category(TestSuiteNames.SurgeonLogin)]
        public void SurgeonLoginTest()
        {
            TestContext.Progress.WriteLine(
                "================================================");

            TestContext.Progress.WriteLine(
                "Starting surgeon login test.");

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
                "Vision Planner is not open.");

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

            Thread.Sleep(15000);

            /*
             * Check if login form is already visible.
             * If Vision Planner is already running from a previous test,
             * the carousel may still be showing the login tiles.
             */
            bool isLoginFormAlreadyVisible =
                IsLoginFormVisible(visionPlannerWindow);

            if (!isLoginFormAlreadyVisible)
            {
                TestContext.Progress.WriteLine(
                    "Login form not yet visible. Attempting to click '" +
                    _config.VisionPlannerLoginTileName +
                    "' tile.");

                bool passwordScreenOpened =
                    ClickOtherUsersWithRetry(
                        LoginItemSearchTimeoutMilliseconds);

                if (!passwordScreenOpened)
                {
                    /*
                     * If clicking failed, but the login form is now visible,
                     * that's okay - just proceed.
                     */
                    if (!IsLoginFormVisible(visionPlannerWindow))
                    {
                        Assert.Fail(
                            "The '" +
                            _config.VisionPlannerLoginTileName +
                            "' tile was found, but the password screen " +
                            "did not appear.");
                    }

                    TestContext.Progress.WriteLine(
                        "Login form is now visible after tile click.");
                }
            }
            else
            {
                TestContext.Progress.WriteLine(
                    "Username/password screen is already visible.");
            }

            bool loginFormVisible =
                WaitForLoginForm(
                    LoginFormSearchTimeoutMilliseconds);

            Assert.That(
                loginFormVisible,
                Is.True,
                "Username/password login form was not detected.");

            string password =
                ReadVisionPlannerPassword();

            EnterVisionPlannerCredentials(
                _config.VisionPlannerUserName,
                password);

            // Remove the reference after entering the password.
            password = null;

            ClickVisionPlannerLoginButton();

            bool loginCompleted =
                WaitForLoginResult(
                    LoginResultTimeoutMilliseconds);

            Assert.That(
                loginCompleted,
                Is.True,
                "Vision Planner surgeon login was not confirmed.");

            TestContext.Progress.WriteLine(
                "Surgeon login completed successfully.");
            Thread.Sleep(6000);
        }
    }
}