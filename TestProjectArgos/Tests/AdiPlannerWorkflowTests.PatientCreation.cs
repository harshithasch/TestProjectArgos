using NUnit.Framework;
using SimulatorAutomation.Tests.Framework;
using System;
using System.Collections.Generic;
using TestProjectArgos.Framework;

namespace SimulatorAutomation.Tests
{
    public partial class AdiPlannerWorkflowTests
    {
        [Test]
        [Order(1)]
        [Category(TestSuiteNames.ADIWorkflows)]
        public void PatientCreationAdiPlannerTest()
        {
            WriteStep("ADI PLANNER: PATIENT CREATION");

            EnsureBrowserIsAvailable();

            Assert.That(
                Patients,
                Is.Not.Null,
                "Patient data was not loaded.");

            Assert.That(
                Patients.Count,
                Is.GreaterThan(0),
                "No patients were found in Excel.");

            int successCount = 0;
            var failures = new List<string>();

            foreach (PatientDataFromExcel patient in Patients)
            {
                if (!ShouldRun(patient.Run))
                {
                    TestContext.Progress.WriteLine(
                        "Skipping patient: " +
                        patient.FirstName + " " +
                        patient.LastName);

                    continue;
                }

                try
                {
                    Dictionary<string, string> patientData =
                        ConvertPatientToDictionary(patient);

                    string createdMrn =
                        PatientCreationPage.CreatePatient(
                            patientData);
                    Thread.Sleep(500);
                    Assert.That(
                        createdMrn,
                        Is.EqualTo(patient.Mrn),
                        "Created patient MRN does not match Excel.");
                    Thread.Sleep(500);
                    successCount++;

                    TestContext.Progress.WriteLine(
                        "Patient created successfully: " +
                        patient.FirstName + " " +
                        patient.LastName);
                    Thread.Sleep(1000);
                    PptScreenshotReport.CaptureDesktopScreenshot(
                        "Captured patient creation screenshot in Adi Planner",
                        "Patient creation page.");
                }
                catch (Exception exception)
                {
                    string failure =
                        patient.FirstName + " " +
                        patient.LastName + ": " +
                        exception.Message;

                    failures.Add(failure);

                    TestContext.Progress.WriteLine(
                        "Patient creation failed: " +
                        failure);
                }
            }

            Assert.Multiple(() =>
            {
                Assert.That(
                    successCount,
                    Is.GreaterThan(0),
                    "No patients were created.");

                Assert.That(
     failures.Count,
     Is.EqualTo(0),
     "Patient creation failures:" +
     Environment.NewLine +
     string.Join(
         Environment.NewLine,
         failures));
            });
        }
    }
}