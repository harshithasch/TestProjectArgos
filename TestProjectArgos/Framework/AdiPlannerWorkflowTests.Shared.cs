using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SimulatorAutomation.Tests.Framework;
using SimulatorAutomation.Tests.Pages.AdiPlanner;

namespace SimulatorAutomation.Tests
{
    [TestFixture]
    [NonParallelizable]
    public partial class AdiPlannerWorkflowTests
    {

#pragma warning disable NUnit1032
        protected IWebDriver Driver { get; private set; }
#pragma warning restore NUnit1032
        protected string ExcelPath;

        protected Dictionary<string, string> Config;

        protected List<PatientDataFromExcel> Patients;

        protected List<OrderMeasurementData> Orders;

        protected LoginPage LoginPage;
        protected PatientCreationPage PatientCreationPage;
        protected OrderMeasurementPage OrderMeasurementPage;
        protected string AdiPlannerWindowHandle { get; private set; }

        [OneTimeSetUp]
        public void AdiPlannerOneTimeSetUp()
        {
            TestContext.Progress.WriteLine(
                "================================================");

            TestContext.Progress.WriteLine(
                "Starting ADI Planner test setup.");

            TestContext.Progress.WriteLine(
                "================================================");

            ExcelPath = Path.GetFullPath(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "Test data",
                    "patient_data_template.xlsx"));

            Assert.That(
                File.Exists(ExcelPath),
                Is.True,
                "Excel file was not found: " + ExcelPath);

            LoadAdiPlannerTestData();
            StartBrowser();
            InitializePages();
            LoginToAdiPlanner();

            TestContext.Progress.WriteLine(
                "ADI Planner setup completed successfully.");
        }

        protected void LoadAdiPlannerTestData()
        {
            Config =
                ExcelDataReader.ReadConfig(ExcelPath);

            Assert.That(
                Config,
                Does.ContainKey("base_url"),
                "base_url was not found in Config sheet.");

            Assert.That(
                Config,
                Does.ContainKey("username"),
                "username was not found in Config sheet.");

            Assert.That(
                Config,
                Does.ContainKey("password"),
                "password was not found in Config sheet.");

            Patients =
                ExcelDataReader.ReadPatients(ExcelPath);

            Orders =
                ExcelDataReader.ReadOrderMeasurements(ExcelPath);


            Assert.That(
                Patients,
                Is.Not.Null,
                "Patient collection was not initialized.");

            Assert.That(
                Orders,
                Is.Not.Null,
                "Order measurement collection was not initialized.");

            TestContext.Progress.WriteLine(
                "Patients loaded: " +
                Patients.Count);

            TestContext.Progress.WriteLine(
                "Measurement orders loaded: " +
                Orders.Count);

            Assert.That(
                Patients.Count,
                Is.GreaterThan(0),
                "No patient rows were loaded from Excel.");

            foreach (PatientDataFromExcel patient in Patients)
            {
                TestContext.Progress.WriteLine(
                    "Loaded patient: " +
                    "Run='" + patient.Run +
                    "', FirstName='" + patient.FirstName +
                    "', LastName='" + patient.LastName +
                    "', MRN='" + patient.Mrn + "'");
            }

            List<PatientDataFromExcel> invalidPatients =
                Patients
                    .Where(patient =>
                        string.IsNullOrWhiteSpace(patient.Run) ||
                        string.IsNullOrWhiteSpace(patient.FirstName) ||
                        string.IsNullOrWhiteSpace(patient.LastName) ||
                        string.IsNullOrWhiteSpace(patient.Mrn))
                    .ToList();

            if (invalidPatients.Count > 0)
            {
                foreach (PatientDataFromExcel invalidPatient
                         in invalidPatients)
                {
                    TestContext.Progress.WriteLine(
                        "[INVALID PATIENT DATA] " +
                        "Run='" + invalidPatient.Run +
                        "', FirstName='" +
                        invalidPatient.FirstName +
                        "', LastName='" +
                        invalidPatient.LastName +
                        "', MRN='" +
                        invalidPatient.Mrn +
                        "'");
                }
            }

            Assert.That(
                invalidPatients.Count,
                Is.EqualTo(0),
                "One or more patient rows contain blank Run, " +
                "FirstName, LastName, or MRN values. " +
                "Check the Patients sheet headers and data.");

            List<OrderMeasurementData> invalidOrders =
                Orders
                    .Where(order =>
                        string.IsNullOrWhiteSpace(order.Mrn))
                    .ToList();

            if (invalidOrders.Count > 0)
            {
                TestContext.Progress.WriteLine(
                    "[INVALID ORDER DATA] " +
                    invalidOrders.Count +
                    " order rows contain an empty MRN.");
            }

            Assert.That(
                invalidOrders.Count,
                Is.EqualTo(0),
                "One or more Ordermeasurement rows " +
                "contain a blank MRN.");

            TestContext.Progress.WriteLine(
                "ADI Planner Excel data validation passed.");
        }
        protected void StartBrowser()
        {
            TestContext.Progress.WriteLine(
                "[" + DateTime.Now.ToString("HH:mm:ss.fff") +
                "] Starting Chrome using local ChromeDriver.");

            var stopwatch =
                System.Diagnostics.Stopwatch.StartNew();

            string driverDirectory = Path.Combine(
                AppContext.BaseDirectory,
                "Drivers");

            string driverExecutable = Path.Combine(
                driverDirectory,
                "chromedriver.exe");

            Assert.That(
                File.Exists(driverExecutable),
                Is.True,
                "chromedriver.exe was not found at: " +
                driverExecutable);

            var service =
                ChromeDriverService.CreateDefaultService(
                    driverDirectory);

            service.HideCommandPromptWindow = true;

            var options = new ChromeOptions();

            options.AddArgument("--start-maximized");
            options.AddArgument("--disable-notifications");
            options.AddArgument("--disable-popup-blocking");

            TestContext.Progress.WriteLine(
                "ChromeDriver path: " +
                driverExecutable);

            Driver = new ChromeDriver(
                service,
                options,
                TimeSpan.FromSeconds(30));

            stopwatch.Stop();

            Driver.Manage()
                .Timeouts()
                .ImplicitWait = TimeSpan.Zero;

            Driver.Manage()
                .Timeouts()
                .PageLoad = TimeSpan.FromSeconds(60);

            TestContext.Progress.WriteLine(
                "[" + DateTime.Now.ToString("HH:mm:ss.fff") +
                "] Chrome browser started successfully.");

            TestContext.Progress.WriteLine(
                "ChromeDriver startup time: " +
                stopwatch.Elapsed.TotalSeconds.ToString("F2") +
                " seconds.");
        }

        protected void InitializePages()
        {
            LoginPage =
                new LoginPage(Driver);

            PatientCreationPage =
                new PatientCreationPage(Driver);

            OrderMeasurementPage =
                new OrderMeasurementPage(Driver);

        }

        protected void LoginToAdiPlanner()
        {
            string baseUrl = Config["base_url"];
            string username = Config["username"];
            string password = Config["password"];

            LoginPage.Open(baseUrl);
            LoginPage.Login(username, password);

            AdiPlannerWindowHandle =
                Driver.CurrentWindowHandle;

            TestContext.Progress.WriteLine(
                "ADI Planner browser window saved: " +
                AdiPlannerWindowHandle);

            TestContext.Progress.WriteLine(
                "ADI Planner login completed.");
        }

        protected void EnsureBrowserIsAvailable()
        {
            Assert.That(
                Driver,
                Is.Not.Null,
                "WebDriver is not initialized.");

            try
            {
                string currentWindow =
                    Driver.CurrentWindowHandle;

                Assert.That(
                    string.IsNullOrWhiteSpace(currentWindow),
                    Is.False,
                    "Browser window is not available.");
            }
            catch (WebDriverException exception)
            {
                Assert.Fail(
                    "ADI Planner browser is not available. " +
                    exception.Message);
            }
        }

        protected void ReturnToPatientManagement()
        {
            EnsureBrowserIsAvailable();

            PatientCreationPage.GoToPatientManagement();
        }

        protected static bool ShouldRun(string runValue)
        {
            return string.Equals(
                runValue?.Trim(),
                "Y",
                StringComparison.OrdinalIgnoreCase);
        }

        protected static Dictionary<string, string>
            ConvertPatientToDictionary(
                PatientDataFromExcel patient)
        {
            if (patient == null)
            {
                throw new ArgumentNullException(nameof(patient));
            }

            return new Dictionary<string, string>
            {
                ["first_name"] =
                    patient.FirstName ?? string.Empty,

                ["last_name"] =
                    patient.LastName ?? string.Empty,

                ["gender"] =
                    patient.Gender ?? string.Empty,

                ["dob"] =
                    patient.DateOfBirth ?? string.Empty,

                ["mrn"] =
                    patient.Mrn ?? string.Empty,

                ["diagnostic_center"] =
                    patient.DiagnosticCenter ?? string.Empty
            };
        }

        protected static void WriteStep(
            string stepName)
        {
            TestContext.Progress.WriteLine(string.Empty);
            TestContext.Progress.WriteLine(
                "================================================");

            TestContext.Progress.WriteLine(stepName);

            TestContext.Progress.WriteLine(
                "================================================");
        }

        protected void SwitchToAdiPlannerMaximizeAndRefresh()
        {
            EnsureBrowserIsAvailable();

            TestContext.Progress.WriteLine(
                "Switching back to ADI Planner.");

            if (string.IsNullOrWhiteSpace(
                    AdiPlannerWindowHandle))
            {
                throw new InvalidOperationException(
                    "ADI Planner window handle was not saved.");
            }

            if (!Driver.WindowHandles.Contains(
                    AdiPlannerWindowHandle))
            {
                throw new InvalidOperationException(
                    "ADI Planner browser window is no longer available.");
            }

            // Switch Selenium control back to ADI Planner Chrome.
            Driver.SwitchTo()
                .Window(AdiPlannerWindowHandle);

            TestContext.Progress.WriteLine(
                "Switched to ADI Planner Chrome window.");

            // Maximize ADI Planner Chrome.
            Driver.Manage()
                .Window
                .Maximize();

            TestContext.Progress.WriteLine(
                "ADI Planner browser maximized.");

            // Bring Selenium/browser back to foreground.
            Thread.Sleep(500);

            // Refresh ADI Planner.
            Driver.Navigate()
                .Refresh();

            TestContext.Progress.WriteLine(
                "ADI Planner browser refreshed.");

            WaitForAdiPlannerPageToLoad();

            TestContext.Progress.WriteLine(
                "ADI Planner page loaded successfully.");
        }
        protected void WaitForAdiPlannerPageToLoad()
        {
            var wait =
                new OpenQA.Selenium.Support.UI.WebDriverWait(
                    Driver,
                    TimeSpan.FromSeconds(60));

            wait.IgnoreExceptionTypes(
                typeof(WebDriverException));

            wait.Until(driver =>
            {
                try
                {
                    var javaScriptExecutor =
                        (IJavaScriptExecutor)driver;

                    object readyState =
                        javaScriptExecutor.ExecuteScript(
                            "return document.readyState");

                    return string.Equals(
                        readyState?.ToString(),
                        "complete",
                        StringComparison.OrdinalIgnoreCase);
                }
                catch (WebDriverException)
                {
                    return false;
                }
            });
        }
        protected void MinimizeAdiPlanner()
        {
            try
            {
                EnsureBrowserIsAvailable();

                TestContext.Progress.WriteLine(
                    "Minimizing ADI Planner browser...");

                if (!string.IsNullOrWhiteSpace(AdiPlannerWindowHandle) &&
                    Driver.WindowHandles.Contains(AdiPlannerWindowHandle))
                {
                    Driver.SwitchTo()
                        .Window(AdiPlannerWindowHandle);
                }

                Driver.Manage()
                    .Window
                    .Minimize();

                Thread.Sleep(500);

                TestContext.Progress.WriteLine(
                    "ADI Planner browser minimized successfully.");
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "[WARNING] Unable to minimize ADI Planner: " +
                    exception.Message);
            }
        }
        /*
        [OneTimeTearDown]
        public void AdiPlannerOneTimeTearDown()
        {
            try
            {
                Driver?.Quit();
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "Browser Quit warning: " +
                    exception.Message);
            }
            finally
            {
                Driver?.Dispose();
                Driver = null;

                TestContext.Progress.WriteLine(
                    "ADI Planner browser disposed successfully.");
            }
        }
        */

    }
}