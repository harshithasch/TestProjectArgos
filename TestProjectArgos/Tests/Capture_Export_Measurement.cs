using FlaUI.Core.AutomationElements;
using NUnit.Framework;
using SimulatorAutomation.Tests.Framework;
using System;
using System.Threading;
using TestProjectArgos.Framework;

namespace SimulatorAutomation.Tests
{
    public partial class SimulatorWorkflowTests
    {
        [TestCaseSource(nameof(CaptureMeasurementTestCases))]
        [OrderAttribute(5)]
        [Category(TestSuiteNames.CaptureMeasurement)]
        public void Capture_Export_Measurement(
    CaptureMeasurementData testData)
        {
            RunCaptureExportMeasurementWorkflow(testData);
        }

        public void RunCaptureExportMeasurementWorkflow(
            CaptureMeasurementData testData)
        { 
            string result =
                "FAILED: Test did not complete";

            try
            {
                Assert.That(
                    testData,
                    Is.Not.Null,
                    "Capture-measurement test data is null.");

                Assert.That(
                    string.IsNullOrWhiteSpace(
                        testData.PatientMrn),
                    Is.False,
                    "Patient MRN is empty.");

                TestContext.Progress.WriteLine(
                    "================================================");

                TestContext.Progress.WriteLine(
                    "Starting complete capture-measurement workflow.");

                TestContext.Progress.WriteLine(
                    "Excel Row: " +
                    testData.ExcelRowNumber);

                TestContext.Progress.WriteLine(
                    "Patient MRN: " +
                    testData.PatientMrn);

                TestContext.Progress.WriteLine(
                    "================================================");

                // ====================================================
                // STEP 1: VERIFY VISION PLANNER IS OPEN
                // ====================================================

                TestContext.Progress.WriteLine(
     "Waiting for Vision Planner window after surgeon login.");

                Window visionPlannerWindow =
                    WaitForWindowByTitle(
                        VisionPlannerWindowTitle,
                        VisionPlannerWindowSearchTimeoutMilliseconds);

                Assert.That(
                    visionPlannerWindow,
                    Is.Not.Null,
                    "Vision Planner window was not found after surgeon login. " +
                    "Expected title: " +
                    VisionPlannerWindowTitle);

                FocusWindowSafely(
                    visionPlannerWindow,
                    "Vision Planner");

                Thread.Sleep(500);

                // ====================================================
                // STEP 2: SEARCH AND SELECT PATIENT
                // ====================================================

                SearchAndSelectPatientByMrn(
                    testData.PatientMrn);

                Thread.Sleep(1000);

                // ====================================================
                // STEP 3: READ SELECTED PATIENT DETAILS
                // ====================================================

                string selectedPatientName;

                string selectedPatientDateOfBirth;

                ReadSelectedPatientFromVisionPlanner(
                    testData.PatientMrn,
                    out selectedPatientName,
                    out selectedPatientDateOfBirth);

                Assert.That(
                    string.IsNullOrWhiteSpace(
                        selectedPatientName),
                    Is.False,
                    "Selected Patient Name was not captured.");

                Assert.That(
                    string.IsNullOrWhiteSpace(
                        selectedPatientDateOfBirth),
                    Is.False,
                    "Selected Patient Date of Birth was not captured.");

                TestContext.Progress.WriteLine(
                    "Selected Patient Name: " +
                    selectedPatientName);

                TestContext.Progress.WriteLine(
                    "Selected Patient DOB: " +
                    selectedPatientDateOfBirth);
                PptScreenshotReport.CaptureDesktopScreenshot(
                        "Captured screenshot for search patient in Argos",
                        "Patient visible in Argos");

                // ====================================================
                // STEP 4: START IMAGE-GUIDED MEASUREMENT
                // ====================================================

                ClickNewImageGuidedMeasurementButton();

                TestContext.Progress.WriteLine(
                    "New Image Guided Measurement clicked.");

                // ====================================================
                // STEP 5: WAIT FOR MOVU MEASUREMENT SCREEN
                // ====================================================

                Window movuWindow =
                    WaitForMovuMeasurementScreen();

                Assert.That(
                    movuWindow,
                    Is.Not.Null,
                    "MOVU measurement window was not displayed.");

                // ====================================================
                // STEP 6: VERIFY PATIENT IN MOVU
                // ====================================================

                VerifyPatientDisplayedInMovu(
                    movuWindow,
                    selectedPatientName,
                    selectedPatientDateOfBirth);

                // ====================================================
                // STEP 7: CAPTURE OD AND OS
                // ====================================================

                CaptureMeasurementsForBothEyes();

                TestContext.Progress.WriteLine(
                    "OD and OS measurements captured successfully.");
                PptScreenshotReport.CaptureDesktopScreenshot(
                        "Captured screenshot of measurements",
                        "Measurement capture in Argos");
                // ====================================================
                // STEP 8: CLICK FINISH
                // ====================================================

                ClickMovuFinishButton();

                TestContext.Progress.WriteLine(
                    "Finish clicked and confirmation popup displayed.");

                // ====================================================
                // STEP 9: CLICK YES
                // ====================================================

                ClickMovuFinishConfirmationYesButton();

                TestContext.Progress.WriteLine(
                    "Finish confirmation completed.");

                // ====================================================
                // STEP 10: VERIFY EXPORT SUCCESS
                // ====================================================

                VerifyMeasurementsExportedSuccessfully();

                TestContext.Progress.WriteLine(
                    "Measurement export verified successfully.");
                PptScreenshotReport.CaptureDesktopScreenshot(
                        "Captured screenshot for measurement export in Argos",
                        "DX export icon verification");
                // ====================================================
                // STEP 11: RETURN TO PATIENT LIST
                // ====================================================

                ClickBackToPatientListButton();

                TestContext.Progress.WriteLine(
                    "Returned to Patient List successfully.");

                // ====================================================
                // COMPLETE CURRENT EXCEL ROW
                // ====================================================

                result =
                    "PASSED";

                TestContext.Progress.WriteLine(
                    "================================================");

                TestContext.Progress.WriteLine(
                    "CAPTURE-MEASUREMENT ROW PASSED.");

                TestContext.Progress.WriteLine(
                    "Excel Row: " +
                    testData.ExcelRowNumber);

                TestContext.Progress.WriteLine(
                    "Patient MRN: " +
                    testData.PatientMrn);

                TestContext.Progress.WriteLine(
                    "================================================");
            }
            catch (Exception exception)
            {
                result =
                    CreateCaptureMeasurementFailedResult(
                        exception.Message);

                TestContext.Progress.WriteLine(
                    "================================================");

                TestContext.Progress.WriteLine(
                    "CAPTURE-MEASUREMENT ROW FAILED.");

                TestContext.Progress.WriteLine(
                    "Excel Row: " +
                    testData.ExcelRowNumber);

                TestContext.Progress.WriteLine(
                    "Patient MRN: " +
                    testData.PatientMrn);

                TestContext.Progress.WriteLine(
                    "Failure: " +
                    exception.Message);

                TestContext.Progress.WriteLine(
                    "================================================");

                throw;
            }
            finally
            {
                if (testData != null &&
                    testData.ExcelRowNumber > 0)
                {
                    ExcelConfigReader.UpdateTestResult(
                        "Capturemeasurement",
                        testData.ExcelRowNumber,
                        result);

                    TestContext.Progress.WriteLine(
                        "Capture-measurement result updated in Excel. Row: " +
                        testData.ExcelRowNumber);
                }
                else
                {
                    TestContext.Progress.WriteLine(
                        "[INFO] Excel result update skipped. " +
                        "The capture workflow was called from the " +
                        "ADI-Argos integration test and has no Capturemeasurement " +
                        "Excel row.");
                }
            }
        }

        // ===*==================================*=====================
        // CREATE CAPTURE-MEASUREMENT FAILED RESULT
        // ============================================================

        private static string CreateCaptureMeasurementFailedResult(
            string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                return "FAILED";
            }

            string singleLineMessage =
                errorMessage
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Trim();

            const int maximumMessageLength =
                150;

            if (singleLineMessage.Length >
                maximumMessageLength)
            {
                singleLineMessage =
                    singleLineMessage.Substring(
                        0,
                        maximumMessageLength);
            }

            return
                "FAILED: " +
                singleLineMessage;
        }
    }
}