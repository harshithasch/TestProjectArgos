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
        [Order(2)]
        [Category(TestSuiteNames.ADIWorkflows)]
        public void OrderMeasurementAdiPlannerTest()
        {
            WriteStep(
                "ADI PLANNER: ORDER MEASUREMENT");

            EnsureBrowserIsAvailable();

            Assert.That(
                Orders,
                Is.Not.Null,
                "Order measurement data was not loaded.");

            if (Orders.Count == 0)
            {
                Assert.Ignore(
                    "No measurement orders were found in Excel.");

                return;
            }

            int successCount = 0;
            int skippedCount = 0;

            var failures =
                new List<string>();

            foreach (OrderMeasurementData order in Orders)
            {
                if (!ShouldRun(order.Run))
                {
                    skippedCount++;

                    TestContext.Progress.WriteLine(
                        "Skipping measurement order because Run='" +
                        order.Run +
                        "'. MRN: " +
                        order.Mrn);

                    continue;
                }

                if (string.IsNullOrWhiteSpace(order.Mrn))
                {
                    skippedCount++;

                    TestContext.Progress.WriteLine(
                        "Skipping measurement order because " +
                        "MRN is blank.");

                    continue;
                }

                try
                {
                    TestContext.Progress.WriteLine(
                        "Processing measurement order. " +
                        "Run='" +
                        order.Run +
                        "', MRN='" +
                        order.Mrn +
                        "'.");

                    string currentUrl =
                        Driver.Url ?? string.Empty;

                    TestContext.Progress.WriteLine(
                        "Current URL before order measurement: " +
                        currentUrl);

                    if (currentUrl.Contains(
                            "/patients/add",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            "The browser is still on the Add Patient " +
                            "page. Patient creation was not fully " +
                            "completed, so order measurement cannot " +
                            "start.");
                    }

                    OrderMeasurementPage.OrderMeasurement(
                        order);

                    successCount++;

                    TestContext.Progress.WriteLine(
                        "Measurement ordered successfully. " +
                        "MRN: " +
                        order.Mrn);
                    PptScreenshotReport.CaptureDesktopScreenshot(
                        "Captured screenshot for measurement order",
                        "Order measurement");
                }
                catch (Exception exception)
                {
                    string failure =
                        "MRN " +
                        order.Mrn +
                        ": " +
                        exception.GetType().Name +
                        ": " +
                        exception.Message;

                    failures.Add(failure);

                    TestContext.Progress.WriteLine(
                        "Measurement order failed: " +
                        failure);

                    TestContext.Progress.WriteLine(
                        exception.StackTrace ??
                        "No stack trace was available.");
                }
            }

            TestContext.Progress.WriteLine(
                "Measurement orders completed: " +
                successCount);

            TestContext.Progress.WriteLine(
                "Measurement orders skipped: " +
                skippedCount);

            TestContext.Progress.WriteLine(
                "Measurement-order failures: " +
                failures.Count);

            if (successCount == 0 &&
                failures.Count == 0)
            {
                Assert.Ignore(
                    "No runnable measurement orders were found.");

                return;
            }

            Assert.Multiple(() =>
            {
                Assert.That(
                    successCount,
                    Is.GreaterThan(0),
                    "No measurement orders were completed.");

                Assert.That(
                    failures.Count,
                    Is.EqualTo(0),
                    "Measurement-order failures:" +
                    Environment.NewLine +
                    string.Join(
                        Environment.NewLine,
                        failures));
            });
        }
    }
}