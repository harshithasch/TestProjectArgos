using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using NUnit.Framework;
using SeleniumKeys = OpenQA.Selenium.Keys;

namespace SimulatorAutomation.Tests.Pages.AdiPlanner
{
    /// <summary>
    /// ADI Planner Patient Creation Page Object.
    /// Handles patient creation form and navigation.
    /// Converted from Python pages/patient_creation_page.py
    /// </summary>
    public class PatientCreationPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        private readonly By AddPatientBtn = By.XPath("//adi-button[@data-testid='home-addPatient-btn']");
        private readonly By FirstNameInput = By.XPath("//input[@data-testid='first-name']");
        private readonly By PatientManagementMenuIcon = By.XPath("//div[@role='menuitem' and @title='Patient management']");
        private readonly By SuccessLabel = By.XPath("//label[normalize-space()='Alcon patient ID:']");

        private readonly By LastNameInput = By.XPath("//input[@data-testid='last-name']");
        private readonly By GenderSelect = By.CssSelector("adi-select[name='gender']");
        private readonly By DateOfBirthInput = By.CssSelector("adi-date-input[name='dateOfBirth']");
        private readonly By MrnInput = By.XPath("//input[@data-testid='mrn']");
        private readonly By DiagnosticCenterSelect = By.CssSelector("adi-multi-select[name='clinicLocation']");
        private readonly By SubmitBtn = By.CssSelector("adi-button[data-testid='home-addPatient-btn']");

        public PatientCreationPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
        }

        /// <summary>
        /// Checks if element is visible (with timeout).
        /// </summary>
        private bool IsElementVisible(By locator, int timeoutSeconds = 5)
        {
            try
            {
                new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds)).Until(
    driver =>
    {
        try
        {
            return driver.FindElement(locator).Displayed;
        }
        catch
        {
            return false;
        }
    });
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Waits for invisible loader overlay.
        /// </summary>
        private void WaitForInvisibleLoader()
        {
            var wait =
                new WebDriverWait(
                    _driver,
                    TimeSpan.FromSeconds(60));

            wait.IgnoreExceptionTypes(
                typeof(NoSuchElementException),
                typeof(StaleElementReferenceException));

            wait.Until(driver =>
            {
                var loaders =
                    driver.FindElements(
                        By.CssSelector(
                            "#loader, " +
                            "[data-testid='loading-dialog'], " +
                            "[data-testid='loader'], " +
                            ".loader, " +
                            ".spinner"));

                foreach (IWebElement loader in loaders)
                {
                    try
                    {
                        if (loader.Displayed)
                        {
                            return false;
                        }
                    }
                    catch (StaleElementReferenceException)
                    {
                        // The loader disappeared from the DOM.
                    }
                }

                return true;
            });
        }
        private void SubmitPatientForm()
        {
            TestContext.Progress.WriteLine(
                "Waiting for patient form loader to disappear...");

            WaitForInvisibleLoader();

            IWebElement submitButton =
                WaitForElementClickable(
                    SubmitBtn);

            if (submitButton == null)
            {
                throw new NoSuchElementException(
                    "Patient form Add button was not found.");
            }

            ScrollIntoView(submitButton);

            TestContext.Progress.WriteLine(
                "Clicking patient form Add button...");

            /*
             * Use a normal Selenium click.
             * Do not bypass a loading overlay with JavaScript.
             */
            submitButton.Click();

            TestContext.Progress.WriteLine(
                "[OK] Patient form Add button clicked.");

            WaitForInvisibleLoader();
        }
        private void WaitForPatientCreationSuccess(
    string expectedMrn)
        {
            if (string.IsNullOrWhiteSpace(expectedMrn))
            {
                throw new ArgumentException(
                    "Expected MRN cannot be blank.",
                    nameof(expectedMrn));
            }

            expectedMrn =
                expectedMrn.Trim();

            TestContext.Progress.WriteLine(
                "Waiting for patient creation success. MRN: " +
                expectedMrn);

            var successWait =
                new WebDriverWait(
                    _driver,
                    TimeSpan.FromSeconds(60));

            bool patientCreated =
                successWait.Until(driver =>
                {
                    try
                    {
                        string currentUrl =
                            driver.Url ??
                            string.Empty;

                        bool stillOnAddPage =
                            currentUrl.Contains(
                                "/patients/add",
                                StringComparison.OrdinalIgnoreCase);

                        bool patientViewOpened =
                            currentUrl.Contains(
                                "/patients/view",
                                StringComparison.OrdinalIgnoreCase);

                        bool measurementMenuVisible =
                            driver.FindElements(
                                    By.XPath(
                                        "//div[@role='menuitem' " +
                                        "and @title='Measurement']"))
                                .Any(element =>
                                {
                                    try
                                    {
                                        return element.Displayed;
                                    }
                                    catch
                                    {
                                        return false;
                                    }
                                });

                        TestContext.Progress.WriteLine(
                            "Patient creation check: " +
                            "URL='" +
                            currentUrl +
                            "', StillOnAddPage=" +
                            stillOnAddPage +
                            ", PatientViewOpened=" +
                            patientViewOpened +
                            ", MeasurementMenuVisible=" +
                            measurementMenuVisible);

                        /*
                         * Patient creation must leave the Add Patient page.
                         */
                        return
                            !stillOnAddPage &&
                            (patientViewOpened ||
                             measurementMenuVisible);
                    }
                    catch (StaleElementReferenceException)
                    {
                        return false;
                    }
                });

            if (!patientCreated)
            {
                throw new WebDriverTimeoutException(
                    "Patient creation was not confirmed. MRN: " +
                    expectedMrn);
            }

            TestContext.Progress.WriteLine(
                "[OK] Patient creation confirmed. MRN: " +
                expectedMrn);

            TestContext.Progress.WriteLine(
                "Current URL after patient creation: " +
                _driver.Url);
        }

        /// <summary>
        /// Navigates to Patient Management page.
        /// </summary>
        public void GoToPatientManagement()
        {
            TestContext.Progress.WriteLine(
                "Navigating to Patient Management page...");

            WaitForInvisibleLoader();

            string currentUrl =
                _driver.Url ?? string.Empty;

            TestContext.Progress.WriteLine(
                "Current URL: " + currentUrl);

            /*
             * We are on the patient list only when:
             * 1. The URL is not the Add Patient page.
             * 2. The patient search component is available.
             */
            bool onAddPatientPage =
                currentUrl.Contains(
                    "/patients/add",
                    StringComparison.OrdinalIgnoreCase);

            bool searchAvailable =
                IsElementVisible(
                    By.CssSelector(
                        "adi-search-input[data-testid='patient-search-input']"),
                    3);

            if (!onAddPatientPage && searchAvailable)
            {
                TestContext.Progress.WriteLine(
                    "[OK] Already on Patient Management list page.");

                return;
            }

            /*
             * Click the Patient Management sidebar icon.
             */
            try
            {
                IWebElement patientManagementButton =
                    WaitForElementClickable(
                        PatientManagementMenuIcon);

                Assert.That(
                    patientManagementButton,
                    Is.Not.Null,
                    "Patient Management sidebar button was not found.");

                SafeClick(patientManagementButton);

                WaitForInvisibleLoader();

                var navigationWait =
                    new WebDriverWait(
                        _driver,
                        TimeSpan.FromSeconds(30));

                bool patientListLoaded =
                    navigationWait.Until(driver =>
                    {
                        try
                        {
                            string newUrl =
                                driver.Url ?? string.Empty;

                            bool leftAddPage =
                                !newUrl.Contains(
                                    "/patients/add",
                                    StringComparison.OrdinalIgnoreCase);

                            var searchComponents =
                                driver.FindElements(
                                    By.CssSelector(
                                        "adi-search-input" +
                                        "[data-testid='patient-search-input']"));

                            bool searchDisplayed =
                                searchComponents.Any(element =>
                                {
                                    try
                                    {
                                        return element.Displayed;
                                    }
                                    catch
                                    {
                                        return false;
                                    }
                                });

                            return leftAddPage &&
                                   searchDisplayed;
                        }
                        catch (
                            StaleElementReferenceException)
                        {
                            return false;
                        }
                    });

                Assert.That(
                    patientListLoaded,
                    Is.True,
                    "Patient Management list page was not displayed.");

                TestContext.Progress.WriteLine(
                    "[OK] Patient Management list page displayed.");

                TestContext.Progress.WriteLine(
                    "Patient Management URL: " +
                    _driver.Url);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "Unable to navigate to Patient Management. " +
                    "Current URL: " +
                    _driver.Url +
                    ". Error: " +
                    exception.Message,
                    exception);
            }
        }

        /// <summary>
        /// Clicks the Add Patient button and waits for form to open.
        /// </summary>
        public void ClickAddPatient()
        {
            IWebElement addPatientBtn =
    WaitForElementClickable(AddPatientBtn);
            SafeClick(addPatientBtn);

            WaitForElementVisible(FirstNameInput);
            TestContext.Progress.WriteLine("✅ Add Patient form opened");
        }

        /// <summary>
        /// Creates a patient from data dictionary.
        /// </summary>
        public string CreatePatient(
    Dictionary<string, string> patient)
        {
            if (patient == null)
            {
                throw new ArgumentNullException(
                    nameof(patient));
            }

            string firstName =
                patient["first_name"]?.Trim() ??
                string.Empty;

            string lastName =
                patient["last_name"]?.Trim() ??
                string.Empty;

            string mrn =
                patient["mrn"]?.Trim() ??
                string.Empty;

            TestContext.Progress.WriteLine(
                "Creating patient: " +
                firstName +
                " " +
                lastName +
                ", MRN: " +
                mrn);

            GoToPatientManagement();
            ClickAddPatient();

            SetFirstName(firstName);
            SetLastName(lastName);
            SetGender(patient["gender"]);
            SetDateOfBirth(patient["dob"]);
            SetMrn(mrn);

            SelectDiagnosticCenter(
                patient["diagnostic_center"]);

            TestContext.Progress.WriteLine(
                "[OK] Patient form filled.");

            SubmitPatientForm();

            WaitForPatientCreationSuccess(
                mrn);

            TestContext.Progress.WriteLine(
                "[OK] Patient created successfully. MRN: " +
                mrn);

            return mrn;
        }
        /// <summary>
        /// Sets first name field.
        /// </summary>
        private void SetFirstName(string value)
        {
            IWebElement field =
     WaitForElementVisible(
         FirstNameInput);
            field.Clear();
            field.SendKeys(value);
        }

        /// <summary>
        /// Sets last name field.
        /// </summary>
        private void SetLastName(string value)
        {
            IWebElement field =
    WaitForElementVisible(
        LastNameInput);
            field.Clear();
            field.SendKeys(value);
        }

        /// <summary>
        /// Selects gender from dropdown (handles shadow DOM).
        /// </summary>
        private void SetGender(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Gender value is empty.",
                    nameof(value));
            }

            value = value.Trim();

            TestContext.Progress.WriteLine(
                "Selecting gender: " + value);

            IWebElement genderHost =
                WaitForElementPresent(GenderSelect);

            Assert.That(
                genderHost,
                Is.Not.Null,
                "Gender component was not found.");

            ScrollIntoView(genderHost);

            ISearchContext shadowRoot;

            try
            {
                shadowRoot =
                    genderHost.GetShadowRoot();
            }
            catch (Exception exception)
            {
                throw new WebDriverException(
                    "Unable to access the gender Shadow DOM. " +
                    exception.Message,
                    exception);
            }

            IWebElement dropdown = null;

            var possibleDropdowns =
                shadowRoot.FindElements(
                    By.CssSelector(
                        "div.input, " +
                        "div.select, " +
                        "[role='combobox']"));

            foreach (IWebElement candidate in possibleDropdowns)
            {
                try
                {
                    if (candidate.Displayed &&
                        candidate.Enabled)
                    {
                        dropdown = candidate;
                        break;
                    }
                }
                catch (StaleElementReferenceException)
                {
                    // Try the next candidate.
                }
            }

            if (dropdown != null)
            {
                SafeClick(dropdown);
            }
            else
            {
                SafeClick(genderHost);
            }

            var optionWait =
                new WebDriverWait(
                    _driver,
                    TimeSpan.FromSeconds(10));

            IWebElement selectedOption =
                optionWait.Until(driver =>
                {
                    try
                    {
                        ISearchContext currentShadowRoot =
                            genderHost.GetShadowRoot();

                        var options =
                            currentShadowRoot.FindElements(
                                By.CssSelector(
                                    "div.option, " +
                                    "[role='option']"));

                        TestContext.Progress.WriteLine(
                            "Gender options found: " +
                            options.Count);

                        foreach (IWebElement option in options)
                        {
                            string optionText =
                                option.Text?.Trim() ??
                                string.Empty;

                            TestContext.Progress.WriteLine(
                                "Gender option: '" +
                                optionText +
                                "'");

                            if (option.Displayed &&
                                string.Equals(
                                    optionText,
                                    value,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                return option;
                            }
                        }

                        return null;
                    }
                    catch (StaleElementReferenceException)
                    {
                        return null;
                    }
                    catch (NoSuchShadowRootException)
                    {
                        return null;
                    }
                });

            if (selectedOption == null)
            {
                throw new NoSuchElementException(
                    "Gender option was not found: " +
                    value);
            }

            ScrollIntoView(selectedOption);
            SafeClick(selectedOption);

            TestContext.Progress.WriteLine(
                "[OK] Gender selected: " +
                value);
        }

        /// <summary>
        /// Sets date of birth field (handles date input).
        /// </summary>
        private void SetDateOfBirth(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Date of birth is empty.",
                    nameof(value));
            }

            value = value.Trim();

            TestContext.Progress.WriteLine(
                "Entering date of birth: " +
                value);

            IWebElement dobHost =
                WaitForElementPresent(
                    DateOfBirthInput);

            Assert.That(
                dobHost,
                Is.Not.Null,
                "Date of birth component was not found.");

            ScrollIntoView(dobHost);

            IWebElement dobInput;

            try
            {
                ISearchContext shadowRoot =
                    dobHost.GetShadowRoot();

                dobInput =
                    shadowRoot.FindElement(
                        By.CssSelector("input"));
            }
            catch (Exception exception)
            {
                throw new WebDriverException(
                    "Unable to locate the date input inside " +
                    "the Date of Birth Shadow DOM. " +
                    exception.Message,
                    exception);
            }

            Assert.That(
                dobInput,
                Is.Not.Null,
                "Date of birth input was not found.");

            SafeClick(dobInput);

            try
            {
                dobInput.Clear();
            }
            catch (InvalidElementStateException)
            {
                dobInput.SendKeys(
                    SeleniumKeys.Control + "a");

                dobInput.SendKeys(
                    SeleniumKeys.Delete);
            }

            dobInput.SendKeys(value);
            dobInput.SendKeys(SeleniumKeys.Tab);

            TestContext.Progress.WriteLine(
                "[OK] Date of birth entered: " +
                value);
        }

        /// <summary>
        /// Sets MRN field.
        /// </summary>
        private void SetMrn(string value)
        {
            IWebElement field =
    WaitForElementVisible(
        MrnInput);
            field.Clear();
            field.SendKeys(value);
        }

        /// <summary>
        /// Selects diagnostic center from multi-select.
        /// </summary>
        private void SelectDiagnosticCenter(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Diagnostic center value is empty.",
                    nameof(value));
            }

            value = value.Trim();

            TestContext.Progress.WriteLine(
                "Selecting diagnostic center: " + value);

            IWebElement clinicHost =
                WaitForElementPresent(
                    DiagnosticCenterSelect);

            Assert.That(
                clinicHost,
                Is.Not.Null,
                "Diagnostic center component was not found.");

            ScrollIntoView(clinicHost);

            ISearchContext shadowRoot;

            try
            {
                shadowRoot =
                    clinicHost.GetShadowRoot();
            }
            catch (Exception exception)
            {
                throw new WebDriverException(
                    "Unable to access the Diagnostic Center " +
                    "Shadow DOM. " +
                    exception.Message,
                    exception);
            }

            IWebElement dropdown =
                shadowRoot.FindElement(
                    By.CssSelector("div.input"));

            Assert.That(
                dropdown,
                Is.Not.Null,
                "Diagnostic center dropdown was not found.");

            SafeClick(dropdown);

            var optionWait =
                new WebDriverWait(
                    _driver,
                    TimeSpan.FromSeconds(10));

            IWebElement selectedOption =
                optionWait.Until(driver =>
                {
                    try
                    {
                        ISearchContext currentShadowRoot =
                            clinicHost.GetShadowRoot();

                        var options =
                            currentShadowRoot.FindElements(
                                By.CssSelector(
                                    "label.option, " +
                                    "[role='option']"));

                        foreach (IWebElement option in options)
                        {
                            string optionText =
                                option.Text?.Trim() ??
                                string.Empty;

                            TestContext.Progress.WriteLine(
                                "Diagnostic center option: '" +
                                optionText +
                                "'");

                            if (option.Displayed &&
                                string.Equals(
                                    optionText,
                                    value,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                return option;
                            }
                        }

                        return null;
                    }
                    catch (StaleElementReferenceException)
                    {
                        return null;
                    }
                    catch (NoSuchShadowRootException)
                    {
                        return null;
                    }
                });

            if (selectedOption == null)
            {
                throw new NoSuchElementException(
                    "Diagnostic Center was not found: " +
                    value);
            }

            ScrollIntoView(selectedOption);
            SafeClick(selectedOption);

            TestContext.Progress.WriteLine(
                "[OK] Diagnostic center selected: " +
                value);
        }

        /// <summary>
        /// Safe click helper - handles stale elements and JS clicks.
        /// </summary>
        private void SafeClick(IWebElement element)
        {
            try
            {
                element.Click();
            }
            catch (Exception ex)
            {
                TestContext.Progress.WriteLine($"Standard click failed, trying JS click: {ex.Message}");
                var jsExecutor = (IJavaScriptExecutor)_driver;
                jsExecutor.ExecuteScript("arguments[0].click();", element);
            }
        }
        private void ScrollIntoView(IWebElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            IJavaScriptExecutor javaScript =
                (IJavaScriptExecutor)_driver;

            javaScript.ExecuteScript(
                "arguments[0].scrollIntoView(" +
                "{block:'center', inline:'nearest'});",
                element);
        }
        private IWebElement WaitForElementPresent(By locator)
        {
            return _wait.Until(driver =>
            {
                try
                {
                    return driver.FindElement(locator);
                }
                catch
                {
                    return null;
                }
            });
        }

        private IWebElement WaitForElementVisible(By locator)
        {
            return _wait.Until(driver =>
            {
                try
                {
                    var element = driver.FindElement(locator);
                    return element.Displayed ? element : null;
                }
                catch
                {
                    return null;
                }
            });
        }

        private IWebElement WaitForElementClickable(By locator)
        {
            return _wait.Until(driver =>
            {
                try
                {
                    var element = driver.FindElement(locator);

                    return element.Displayed &&
                           element.Enabled
                        ? element
                        : null;
                }
                catch
                {
                    return null;
                }
            });
        }
    }
}
