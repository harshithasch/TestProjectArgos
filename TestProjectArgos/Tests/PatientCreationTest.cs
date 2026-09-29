using System;
using System.Globalization;
using System.Threading;

using FlaUI.Core.AutomationElements;

using NUnit.Framework;

using SimulatorAutomation.Tests.Framework;

namespace SimulatorAutomation.Tests
{
    public partial class SimulatorWorkflowTests
    {
        [TestCaseSource(nameof(PatientCreationTestCases))]
        [Order(4)]
        [Category(TestSuiteNames.PatientCreation)]
        public void PatientCreationTest(
    PatientCreationData testData)
        {
            string result =
                string.Empty;

            try
            {
                TestContext.Progress.WriteLine(
                    "================================================");

                TestContext.Progress.WriteLine(
                    "Starting patient creation for Excel row: " +
                    testData.ExcelRowNumber);

                TestContext.Progress.WriteLine(
                    "Patient ID: " +
                    testData.PatientID);

                TestContext.Progress.WriteLine(
                    "================================================");

                ValidatePatientCreationData(
                    testData);

                Window visionPlannerWindow =
                    FindWindowByTitle(
                        VisionPlannerWindowTitle);

                Assert.That(
                    visionPlannerWindow,
                    Is.Not.Null,
                    "Vision Planner window is not open. " +
                    "Run SurgeonLoginTest first.");

                FocusWindowSafely(
                    visionPlannerWindow,
                    "Vision Planner");

                Thread.Sleep(500);

                ClickNewPatientButton();

                Thread.Sleep(1000);

                string dobDay;
                string dobMonth;
                string dobYear;

                SplitDateOfBirth(
                    testData.PatientDateOfBirth,
                    out dobDay,
                    out dobMonth,
                    out dobYear);

                EnterPatientDetails(
                    testData.PatientFirstName,
                    testData.PatientLastName,
                    dobDay,
                    dobMonth,
                    dobYear,
                    testData.PatientID,
                    testData.PatientGender);

                ClickSavePatientButton();

                /*
                 * Verify the newly created patient before returning to the
                 * patient-list screen.
                 */
                VerifyPatientCreatedSuccessfully(
                    testData.PatientFirstName,
                    testData.PatientLastName,
                    dobDay,
                    dobMonth,
                    dobYear,
                    testData.PatientID);

                /*
                 * Return to the patient list so the next Excel row can create
                 * the next patient.
                 */
                ClickBackToPatientListButton();

                result =
                    "PASSED";

                TestContext.Progress.WriteLine(
                    "Patient creation passed for row " +
                    testData.ExcelRowNumber +
                    ".");
            }
            catch (Exception exception)
            {
                result =
                    CreateFailedResult(
                        exception.Message);

                TestContext.Progress.WriteLine(
                    "Patient creation failed for row " +
                    testData.ExcelRowNumber +
                    ": " +
                    exception.Message);

                throw;
            }
            finally
            {
                ExcelConfigReader.UpdateTestResult(
                    "Patientcreation",
                    testData.ExcelRowNumber,
                    result);
            }
        }

        private static void ValidatePatientCreationData(
            PatientCreationData testData)
        {
            Assert.That(
                testData,
                Is.Not.Null,
                "Patient test data is null.");

            Assert.That(
                string.IsNullOrWhiteSpace(
                    testData.PatientLastName),
                Is.False,
                "PatientLastName is empty.");

            Assert.That(
                string.IsNullOrWhiteSpace(
                    testData.PatientFirstName),
                Is.False,
                "PatientFirstName is empty.");

            Assert.That(
                string.IsNullOrWhiteSpace(
                    testData.PatientDateOfBirth),
                Is.False,
                "PatientDateOfBirth is empty.");

            Assert.That(
                string.IsNullOrWhiteSpace(
                    testData.PatientID),
                Is.False,
                "PatientID is empty.");

            Assert.That(
                string.IsNullOrWhiteSpace(
                    testData.PatientGender),
                Is.False,
                "PatientGender is empty.");
        }

        private static void SplitDateOfBirth(
            string dateOfBirthText,
            out string day,
            out string month,
            out string year)
        {
            string[] formats =
            {
                "dd-MM-yyyy",
                "d-M-yyyy",
                "dd/MM/yyyy",
                "d/M/yyyy",
                "MM-dd-yyyy",
                "M-d-yyyy"
            };

            DateTime parsedDate;

            bool parsed =
                DateTime.TryParseExact(
                    dateOfBirthText.Trim(),
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out parsedDate);

            Assert.That(
                parsed,
                Is.True,
                "Invalid PatientDateOfBirth value: '" +
                dateOfBirthText +
                "'. Expected format such as 06-02-1987.");

            day =
                parsedDate.Day.ToString(
                    "00",
                    CultureInfo.InvariantCulture);

            month =
                parsedDate.Month.ToString(
                    "00",
                    CultureInfo.InvariantCulture);

            year =
                parsedDate.Year.ToString(
                    CultureInfo.InvariantCulture);
        }

        private static string CreateFailedResult(
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