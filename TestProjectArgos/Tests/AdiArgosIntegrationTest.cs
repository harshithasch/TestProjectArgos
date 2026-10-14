using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimulatorAutomation.Tests.Framework;
using TestProjectArgos.Framework;

namespace SimulatorAutomation.Tests
{
    public partial class AdiPlannerWorkflowTests
    {
        private SimulatorWorkflowTests _simulatorWorkflow = null!;
        public void InitializeIntegrationSimulatorWorkflow()
        {
            _simulatorWorkflow =
                new SimulatorWorkflowTests();
            _simulatorWorkflow.OneTimeSetUp();

            /*
             * Initialize the PowerPoint before the integration
             * workflow begins.
             */
            PptScreenshotReport.Initialize(
                reportName: "ADI_Argos_Integration_Run");

            TestContext.Progress.WriteLine(
                "PowerPoint screenshot report created.");

            TestContext.Progress.WriteLine(
                "PowerPoint report path: " +
                PptScreenshotReport.PresentationPath);
        }

        [Test]
        [NonParallelizable]
        [Category("6_ADIArgosIntegration")]
        public void AdiArgosCompleteIntegrationTest()
        {
            InitializeIntegrationSimulatorWorkflow();
            WriteStep(
                "ADI ARGOS COMPLETE INTEGRATION WORKFLOW");

            List<string> failures =
                new List<string>();

            Dictionary<string, OrderMeasurementData> ordersByMrn =
                Orders
                    .Where(order =>
                        order != null &&
                        !string.IsNullOrWhiteSpace(order.Mrn))
                    .GroupBy(order =>
                        order.Mrn.Trim())
                    .ToDictionary(
                        group => group.Key,
                        group => group.First());

            foreach (PatientDataFromExcel patient in Patients)
            {
                if (patient == null)
                {
                    continue;
                }

                if (!ShouldRun(patient.Run))
                {
                    TestContext.Progress.WriteLine(
                        "Skipping patient because Run is not enabled. " +
                        "MRN: " +
                        patient.Mrn);

                    continue;
                }

                try
                {
                    RunAdiArgosWorkflow(
                        patient,
                        ordersByMrn);
                }
                catch (Exception exception)
                {
                    string patientMrn =
                        string.IsNullOrWhiteSpace(patient.Mrn)
                            ? "Unknown"
                            : patient.Mrn.Trim();

                    string errorMessage =
                        "MRN " +
                        patientMrn +
                        ": " +
                        exception.Message;

                    failures.Add(errorMessage);

                    TestContext.Progress.WriteLine(
                        "[FAILED] " +
                        errorMessage);

                    CaptureScreenshotSafely(
                        "Integration Failed - " +
                        patientMrn,
                        exception.Message);
                }
            }

            Assert.That(
                failures,
                Is.Empty,
                "ADI-Argos integration failures:" +
                Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    failures));
        }

        private void RunAdiArgosWorkflow(
            PatientDataFromExcel patient,
            Dictionary<string, OrderMeasurementData> ordersByMrn)
        {
            Assert.That(
                patient,
                Is.Not.Null,
                "Patient data is null.");

            Assert.That(
                patient.Mrn,
                Is.Not.Null.And.Not.Empty,
                "Patient MRN is empty.");

            string mrn =
                patient.Mrn.Trim();

            /*
             * STEP 1:
             * Create patient in ADI Planner.
             */
            WriteStep(
                "STEP 1 - CREATE PATIENT IN ADI PLANNER: " +
                mrn);

            EnsureBrowserIsAvailable();

            PatientCreationPage.CreatePatient(
                ConvertPatientToDictionary(patient));

            TestContext.Progress.WriteLine(
                "Patient created successfully. MRN: " +
                mrn);

            CaptureScreenshotSafely(
                "Step 1 - Patient Created - " + mrn,
                "Patient created successfully in ADI Planner.");

            /*
             * STEP 2:
             * Create measurement order in ADI Planner.
             */
            WriteStep(
                "STEP 2 - ORDER MEASUREMENT IN ADI PLANNER: " +
                mrn);

            OrderMeasurementData order;

            bool orderFound =
                ordersByMrn.TryGetValue(
                    mrn,
                    out order);

            Assert.That(
                orderFound,
                Is.True,
                "Order measurement data was not found for MRN: " +
                mrn);

            Assert.That(
                order,
                Is.Not.Null,
                "Order measurement data is null for MRN: " +
                mrn);

            OrderMeasurementPage.OrderMeasurement(
                order);

            TestContext.Progress.WriteLine(
                "Measurement order completed. MRN: " +
                mrn);

            CaptureScreenshotSafely(
                "Step 2 - Measurement Ordered - " + mrn,
                "Measurement order completed in ADI Planner.");

            /*
             * STEP 3:
             * Launch MOVU.
             */
            WriteStep(
                "STEP 3 - LAUNCH MOVU");

            _simulatorWorkflow
                .RunLaunchMovuWorkflow();

            TestContext.Progress.WriteLine(
                "MOVU launched successfully.");

   
            /*
             * STEP 4:
             * Launch Vision Planner.
             */
            WriteStep(
                "STEP 4 - LAUNCH VISION PLANNER");

            _simulatorWorkflow
                .RunLaunchVisionPlannerWorkflow();

            TestContext.Progress.WriteLine(
                "Vision Planner launched successfully.");

            CaptureScreenshotSafely(
                "Step 4 - Vision Planner Launched - " + mrn,
                "Vision Planner launched successfully.");

            /*
             * STEP 5:
             * Surgeon login.
             */
            WriteStep(
                "STEP 5 - SURGEON LOGIN");

            _simulatorWorkflow
                .RunArgosSurgeonLoginWorkflow();

            TestContext.Progress.WriteLine(
                "ADI Surgeon login completed successfully in Argos.");

            CaptureScreenshotSafely(
                "Step 5 - Surgeon Login Completed - " + mrn,
                "Surgeon login completed successfully.");

            /*
             * STEP 6:
             * Capture measurement for the ADI-created patient.
             */
            WriteStep(
                "STEP 6 - CAPTURE MEASUREMENT: " +
                mrn);

            CaptureMeasurementData captureMeasurementData =
                new CaptureMeasurementData
                {
                    PatientMrn = mrn
                };

            _simulatorWorkflow
                .RunCaptureExportMeasurementWorkflow(
                    captureMeasurementData);

            TestContext.Progress.WriteLine(
                "Measurement captured successfully. MRN: " +
                mrn);

            CaptureScreenshotSafely(
                "Step 6 - Measurement Completed - " + mrn,
                "Measurement capture and export completed successfully.");

            _simulatorWorkflow.MinimizeVisionPlanner();

            _simulatorWorkflow.MinimizeMovu();

            TestContext.Progress.WriteLine(
                "================================================");

            TestContext.Progress.WriteLine(
                "ADI-ARGOS INTEGRATION WORKFLOW PASSED.");

            TestContext.Progress.WriteLine(
                "Patient MRN: " +
                mrn);

            TestContext.Progress.WriteLine(
                "================================================");
            /*
    * STEP 7:
    * Return to ADI Planner and refresh the browser.
    */
            WriteStep(
                "STEP 7 - RETURN TO ADI PLANNER AND REFRESH: " +
                mrn);

            SwitchToAdiPlannerMaximizeAndRefresh();

            CaptureScreenshotSafely(
                "Step 7 - ADI Planner Refreshed - " + mrn,
                "Returned to ADI Planner and refreshed the browser.");

            TestContext.Progress.WriteLine(
                "Returned to ADI Planner successfully. MRN: " +
                mrn);
            //MinimizeAdiPlanner();


        }
       

        private static void CaptureScreenshotSafely(
            string title,
            string description)
        {
            try
            {
                PptScreenshotReport.CaptureDesktopScreenshot(
                    title,
                    description);

                TestContext.Progress.WriteLine(
                    "Screenshot added to PowerPoint: " +
                    title);
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "[SCREENSHOT ERROR] " +
                    exception.Message);
            }
        }

        [OneTimeTearDown]
        public void CompleteIntegrationPowerPointReport()
        {
            try
            {
                PptScreenshotReport.CompleteReport();

                TestContext.Progress.WriteLine(
                    "PowerPoint screenshot report saved.");

                TestContext.Progress.WriteLine(
                    "PowerPoint report path: " +
                    PptScreenshotReport.PresentationPath);
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "[POWERPOINT REPORT ERROR] " +
                    exception.Message);
            }
            finally
            {
                if (_simulatorWorkflow != null)
                {
                    _simulatorWorkflow.OneTimeTearDown();
                    _simulatorWorkflow = null!;
                }
                 
            }
        }
    }
}