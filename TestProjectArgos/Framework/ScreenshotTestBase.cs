using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;

namespace TestProjectArgos.Framework
{
    public abstract class ScreenshotTestBase
    {
        /// <summary>
        /// Override this property in Selenium test classes.
        /// Desktop-only tests can leave it as null.
        /// </summary>
        protected virtual IWebDriver? CurrentDriver => null;

        [OneTimeSetUp]
        public void StartScreenshotReport()
        {
            PptScreenshotReport.Initialize(
                reportName: "ADI_Argos_Automation_Run");
        }

        [TearDown]
        public void CaptureScreenshotAfterTest()
        {
            string testName =
                TestContext.CurrentContext.Test.Name;

            TestStatus status =
                TestContext.CurrentContext.Result.Outcome.Status;

            string message =
                TestContext.CurrentContext.Result.Message
                ?? string.Empty;

            try
            {
                if (status == TestStatus.Failed)
                {
                    PptScreenshotReport.CaptureFailureScreenshot(
                        CurrentDriver,
                        testName,
                        message);
                }
                else
                {
                    if (CurrentDriver != null)
                    {
                        PptScreenshotReport.CaptureWebScreenshot(
                            CurrentDriver,
                            $"{testName} - PASSED",
                            "Final test state");
                    }
                    else
                    {
                        PptScreenshotReport.CaptureDesktopScreenshot(
                            $"{testName} - PASSED",
                            "Final test state");
                    }
                }
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    $"Screenshot error: {exception.Message}");
            }
        }

        [OneTimeTearDown]
        public void FinishScreenshotReport()
        {
            PptScreenshotReport.CompleteReport();

            TestContext.Progress.WriteLine(
                $"PowerPoint report: " +
                $"{PptScreenshotReport.PresentationPath}");
        }
    }
}