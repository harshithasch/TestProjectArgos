using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProjectArgos.Models;
using TestProjectArgos.TestInfrastructure;

namespace TestProjectArgos.TestSuites
{
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("ADI-To-ARGOS")]
    [DoNotParallelize]
    public class Suite08_AdiToArgosE2ETests : BaseE2ETest
    {
        [TestMethod]
        [TestCategory("Smoke")]
        [TestProperty("ExecutionOrder", "01")]
        public void E2E_AdiPatient_OrderMeasurement_Capture_Export()
        {
            var context = new WorkflowContext
            {
                WorkflowName =
                    "ADI Patient To ARGOS Measurement Export",

                CreationSource =
                    PatientCreationSource.AdiPlanner,

                Patient = new PatientData
                {
                    FirstName =
                        $"Auto{DateTime.Now:MMddHHmmss}",

                    LastName = "E2EPatient",

                    Mrn =
                        $"MRN{DateTime.Now:MMddHHmmss}",

                    DateOfBirth =
                        new DateTime(1991, 2, 3),

                    Gender = "Female",

                    Eye = "OU",

                    DiagnosticFacility =
                        "Your Diagnostic Facility",

                    MeasurementDevice = "ARGOS"
                }
            };

            E2EWorkflow.Execute(context);

            Assert.IsTrue(
                context.PatientCreated,
                "Patient creation failed.");

            Assert.IsTrue(
                context.MeasurementOrdered,
                "Measurement ordering failed.");

            Assert.IsTrue(
                context.MovuLaunched,
                "MOVU launch failed.");

            Assert.IsTrue(
                context.VisionPlannerLaunched,
                "Vision Planner launch failed.");

            Assert.IsTrue(
                context.SurgeonLoggedIn,
                "Surgeon login failed.");

            Assert.IsTrue(
                context.PatientFoundInVisionPlanner,
                "Patient search failed.");

            Assert.IsTrue(
                context.MeasurementCaptured,
                "Measurement capture failed.");

            Assert.IsTrue(
                context.MeasurementExported,
                "Measurement export failed.");

            Assert.IsTrue(
                context.ExportVerifiedInAdiPlanner,
                "Measurement verification in ADI Planner failed.");
        }
    }
}