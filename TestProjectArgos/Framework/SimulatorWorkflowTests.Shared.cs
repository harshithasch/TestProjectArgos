using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using NUnit.Framework;
using SimulatorAutomation.Tests.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Collections.Generic;

namespace SimulatorAutomation.Tests
{
    [TestFixture]
    [NonParallelizable]
    public partial class SimulatorWorkflowTests
    {

        // ============================================================
        // WAIT CONFIGURATION
        // ============================================================

        protected const int MovuStartupWaitMilliseconds =
            60000;

        protected const int MovuWindowSearchTimeoutMilliseconds =
            60000;

        protected const int MinimizeButtonSearchTimeoutMilliseconds =
            50000;

        protected const int AfterMovuMinimizeWaitMilliseconds =
            5000;

        protected const int VisionPlannerWindowSearchTimeoutMilliseconds =
            100000;

        protected const int LoginItemSearchTimeoutMilliseconds =
            120000;

        protected const int LoginFormSearchTimeoutMilliseconds =
            120000;

        protected const int LoginResultTimeoutMilliseconds =
            120000;

        protected const int PollingIntervalMilliseconds =
            1000;
        protected const int NewpatientButtoninterval =
            200000;

        // ============================================================
        // STABLE APPLICATION DETAILS
        // ============================================================

        protected const string MovuWindowTitle =
            "Argos Biometer Simulator";

        protected const string MovuMinimizeButtonAutomationId =
            "MinimizeBtn";

        protected const string VisionPlannerWindowTitle =
            "Vision Planner";

        protected const string RecentUsersListAutomationId =
            "LoginRecentUsersList";

        protected const string UserNameTextBoxAutomationId =
            "ParamTxtBox";

        protected const string PasswordTextBoxAutomationId =
            "PasswordEditInLogin";

        protected const string LoginButtonAutomationId =
            "LoginButton";

        protected const string LoginErrorTextAutomationId =
            "ErrorTextBox";

        protected const string NewPatientButtonName =
            "+ New Patient";

        // SHARED OBJECTS
        protected UIA3Automation _automation;

        protected AutomationConfig _config;
        // ============================================================
        // PATIENT VERIFICATION DETAILS
        // ============================================================

        protected const string CreatedPatientNameAutomationId =
            "MarkedNameText";

        protected const int PatientCreationVerificationTimeoutMilliseconds =
            120000;

        // ============================================================
        // PATIENT SEARCH DETAILS
        // ============================================================

        protected const string PatientSearchTextBoxAutomationId =
            "Search Item";

        protected const string PatientSearchResetAutomationId =
            "Reset Search";

        protected const int PatientSearchTimeoutMilliseconds =
            120000;

        // ============================================================
        // CAPTURE MEASUREMENT CONFIGURATION
        // ============================================================

        protected const string NewImageGuidedMeasurementButtonName =
            "New Image Guided Measurement";

        protected const string MovuMeasurementControlAutomationId =
            "Mainy";

        protected const int CaptureMeasurementTimeoutMilliseconds =
            180000;
        // ============================================================
        // MOVU TASKBAR DETAILS
        // ============================================================

        protected const string MovuTaskbarButtonPartialName =
            "MovuSimulator";

        protected const string MovuTaskbarButtonAutomationIdPartialText =
            "ArgosBiometerSimulator.exe";

        protected const int MovuTaskbarButtonTimeoutMilliseconds =
            30000;
        // ============================================================
        // MOVU CAPTURE-MEASUREMENT CONFIGURATION
        // ============================================================

        protected const string MovuTaskbarButtonNamePartialText =
            "MovuSimulator";

        protected const string MovuTaskbarAutomationIdPartialText =
            "ArgosBiometerSimulator.exe";

        protected const int MovuTaskbarSearchTimeoutMilliseconds =
            30000;
        // ============================================================
        // MOVU MEASUREMENT CAPTURE CONTROLS
        // ============================================================

        protected const string MovuOdButtonAutomationId =
            "SwitchToODArrow";

        protected const string MovuOsButtonAutomationId =
            "SwitchToOSArrow";

        protected const string MovuJoystickButtonAutomationId =
            "Joystick1";

        protected const string MovuAcceptButtonAutomationId =
            "btnAcceptRI";

        protected const string MovuFinishButtonAutomationId =
            "FinishBtn";

        protected const int MovuCaptureControlTimeoutMilliseconds =
            60000;

        protected const int MovuMeasurementProcessingWaitMilliseconds =
            2000;

        protected const int MovuAcceptProcessingWaitMilliseconds =
            1500;

        protected const int MeasurementsPerEye =
            1;
        protected const string MovuFinishConfirmationWindowTitle =
    "Vision Planner Confirmation";

        protected const string MovuFinishConfirmationYesAutomationId =
            "ConfirmationBox::Yes";

        protected const int MovuFinishConfirmationTimeoutMilliseconds =
            60000;
        // ============================================================
        // BACK TO PATIENT LIST
        // ============================================================

        protected const string BackToPatientListAutomationId =
            "BackToPatientList";

        protected const int BackToPatientListTimeoutMilliseconds =
            60000;
        // ============================================================
        // ONE-TIME SETUP
        // ============================================================

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestContext.Progress.WriteLine(
                "Loading automation configuration from Excel.");

            _config =
                ExcelConfigReader.Load();

            Assert.That(
                _config,
                Is.Not.Null,
                "Automation configuration could not be loaded.");

            _automation =
                new UIA3Automation();

            TestContext.Progress.WriteLine(
                "Excel configuration loaded successfully.");

            TestContext.Progress.WriteLine(
                "UIA3Automation initialized successfully.");
        }

        // ============================================================
        // READ VISION PLANNER PASSWORD
        // ============================================================

        protected string ReadVisionPlannerPassword()
        {
            string passwordValue =
                _config.PasswordEnvironmentVariable;

            if (string.IsNullOrWhiteSpace(passwordValue))
            {
                throw new InvalidOperationException(
                    "Password configuration is not set in the Excel workbook.");
            }

            /*
             * If it contains @ or ., it's likely a direct password value.
             */
            if (passwordValue.Contains("@") || passwordValue.Contains("."))
            {
                TestContext.Progress.WriteLine(
                    "Using password from Excel configuration.");

                return passwordValue;
            }

            /*
             * Otherwise, treat it as an environment variable name.
             */
            string password =
                Environment.GetEnvironmentVariable(passwordValue);

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Environment variable '" +
                    passwordValue +
                    "' is not set. Please either: " +
                    "1) Create the environment variable with the password, or " +
                    "2) Update the Excel PasswordEnvironmentVariable field " +
                    "to contain the password directly (must include @ or .)");
            }

            TestContext.Progress.WriteLine(
                "Password read from environment variable '" +
                passwordValue +
                "'.");

            return password;
        }

        // ============================================================
        // CLICK OTHER USERS WITH RETRY
        // ============================================================

        protected bool ClickOtherUsersWithRetry(
            int totalTimeoutMilliseconds)
        {
            /*
             * Wait for the "Other Users" tile to be present and visible
             * before attempting to click it.
             */
            TestContext.Progress.WriteLine(
                "Waiting for '" +
                _config.VisionPlannerLoginTileName +
                "' tile to become available and visible.");

            AutomationElement otherUsersTile =
                WaitForLoginListItemByChildText(
                    _config.VisionPlannerLoginTileName,
                    totalTimeoutMilliseconds);

            if (otherUsersTile == null)
            {
                TestContext.Progress.WriteLine(
                    "The '" +
                    _config.VisionPlannerLoginTileName +
                    "' tile was not found within the timeout period.");

                return false;
            }

            if (!IsElementVisible(otherUsersTile))
            {
                TestContext.Progress.WriteLine(
                    "The '" +
                    _config.VisionPlannerLoginTileName +
                    "' tile was found but is not visible.");

                return false;
            }

            TestContext.Progress.WriteLine(
                "The '" +
                _config.VisionPlannerLoginTileName +
                "' tile is now available and visible.");

            Stopwatch totalStopwatch =
                Stopwatch.StartNew();

            int attempt = 0;

            while (totalStopwatch.ElapsedMilliseconds <
                   totalTimeoutMilliseconds)
            {
                Window visionPlannerWindow =
                    FindWindowByTitle(
                        VisionPlannerWindowTitle);

                if (visionPlannerWindow == null)
                {
                    TestContext.Progress.WriteLine(
                        "Vision Planner window is temporarily " +
                        "unavailable.");

                    Thread.Sleep(1000);

                    continue;
                }

                /*
                 * A previous click may already have opened the
                 * username/password screen.
                 */
                if (IsPasswordScreenVisible(
                        visionPlannerWindow))
                {
                    TestContext.Progress.WriteLine(
                        "Password screen is already visible.");

                    return true;
                }

                FocusWindowSafely(
                    visionPlannerWindow,
                    "Vision Planner");

                Thread.Sleep(1000);

                /*
                 * Re-find the tile before every attempt.
                 * WPF can recreate the ListItem after focus changes.
                 */
                AutomationElement currentTile =
                    FindLoginListItemByChildText(
                        visionPlannerWindow,
                        _config.VisionPlannerLoginTileName);

                if (currentTile == null)
                {
                    TestContext.Progress.WriteLine(
                        "The '" +
                        _config.VisionPlannerLoginTileName +
                        "' tile is not currently available.");

                    Thread.Sleep(1000);

                    continue;
                }

                if (!IsElementVisible(currentTile))
                {
                    TestContext.Progress.WriteLine(
                        "The login tile exists but is not ready " +
                        "for clicking.");

                    Thread.Sleep(1000);

                    continue;
                }

                var rectangle =
                    currentTile.BoundingRectangle;

                /*
                 * Reject stale UI Automation coordinates.
                 *
                 * WPF carousel elements can report very large
                 * negative coordinates even when IsOffscreen is false.
                 */
                if (rectangle.Left < 0 ||
                    rectangle.Top < 0 ||
                    rectangle.Width <= 0 ||
                    rectangle.Height <= 0)
                {
                    TestContext.Progress.WriteLine(
                        "Login tile has invalid or stale coordinates. " +
                        "Left=" +
                        rectangle.Left +
                        ", Top=" +
                        rectangle.Top +
                        ", Width=" +
                        rectangle.Width +
                        ", Height=" +
                        rectangle.Height);

                    Thread.Sleep(1000);

                    continue;
                }

                attempt++;

                TestContext.Progress.WriteLine(
                    "Login tile click attempt " +
                    attempt +
                    ".");

                TestContext.Progress.WriteLine(
                    "Login tile bounds: Left=" +
                    rectangle.Left +
                    ", Top=" +
                    rectangle.Top +
                    ", Width=" +
                    rectangle.Width +
                    ", Height=" +
                    rectangle.Height);

                var centerPoint =
                    rectangle.Center();

                TestContext.Progress.WriteLine(
                    "Login tile click coordinates: X=" +
                    centerPoint.X +
                    ", Y=" +
                    centerPoint.Y);

                try
                {
                    Mouse.MoveTo(centerPoint);

                    Thread.Sleep(700);

                    Mouse.Click(
                        MouseButton.Left);

                    TestContext.Progress.WriteLine(
                        "Physical mouse click sent to the login tile.");
                }
                catch (Exception exception)
                {
                    TestContext.Progress.WriteLine(
                        "Login tile click warning: " +
                        exception.Message);
                }

                bool passwordScreenVisible =
                    WaitForPasswordScreen(
                        10000);

                if (passwordScreenVisible)
                {
                    TestContext.Progress.WriteLine(
                        "Password screen appeared after clicking '" +
                        _config.VisionPlannerLoginTileName +
                        "'.");

                    return true;
                }

                TestContext.Progress.WriteLine(
                    "Password screen did not appear after attempt " +
                    attempt +
                    ". The tile will be found again and retried.");

                Thread.Sleep(2000);
            }

            return false;
        }

        // ============================================================
        // WAIT FOR PASSWORD SCREEN
        // ============================================================

        protected bool WaitForPasswordScreen(
            int timeoutMilliseconds)
        {
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   timeoutMilliseconds)
            {
                Window visionPlannerWindow =
                    FindWindowByTitle(
                        VisionPlannerWindowTitle);

                if (visionPlannerWindow != null &&
                    IsPasswordScreenVisible(
                        visionPlannerWindow))
                {
                    return true;
                }

                TestContext.Progress.WriteLine(
                    "Waiting for password field AutomationId '" +
                    PasswordTextBoxAutomationId +
                    "'. Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(500);
            }

            return false;
        }

        // ============================================================
        // CHECK WHETHER PASSWORD SCREEN IS VISIBLE
        // ============================================================

        protected bool IsPasswordScreenVisible(
            Window visionPlannerWindow)
        {
            if (visionPlannerWindow == null)
            {
                return false;
            }

            try
            {
                AutomationElement passwordField =
                    visionPlannerWindow.FindFirstDescendant(
                        _automation.ConditionFactory
                            .ByAutomationId(
                                PasswordTextBoxAutomationId));

                if (!IsElementVisible(passwordField))
                {
                    return false;
                }

                TestContext.Progress.WriteLine(
                    "Visible password field detected.");

                TestContext.Progress.WriteLine(
                    "Password field AutomationId: " +
                    GetAutomationIdSafely(passwordField));

                return true;
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "Password screen detection warning: " +
                    exception.Message);

                return false;
            }
        }

        // ============================================================
        // WAIT FOR COMPLETE LOGIN FORM
        // ============================================================

        protected bool WaitForLoginForm(
            int timeoutMilliseconds)
        {
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   timeoutMilliseconds)
            {
                Window visionPlannerWindow =
                    FindWindowByTitle(
                        VisionPlannerWindowTitle);

                if (visionPlannerWindow != null &&
                    IsLoginFormVisible(
                        visionPlannerWindow))
                {
                    TestContext.Progress.WriteLine(
                        "Username/password login form detected.");

                    return true;
                }

                TestContext.Progress.WriteLine(
                    "Waiting for username/password form. " +
                    "Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(500);
            }

            return false;
        }

        // ============================================================
        // CHECK WHETHER COMPLETE LOGIN FORM IS VISIBLE
        // ============================================================

        protected bool IsLoginFormVisible(
            Window visionPlannerWindow)
        {
            if (visionPlannerWindow == null)
            {
                return false;
            }

            try
            {
                AutomationElement userNameField =
                    visionPlannerWindow.FindFirstDescendant(
                        _automation.ConditionFactory
                            .ByAutomationId(
                                UserNameTextBoxAutomationId));

                AutomationElement passwordField =
                    visionPlannerWindow.FindFirstDescendant(
                        _automation.ConditionFactory
                            .ByAutomationId(
                                PasswordTextBoxAutomationId));

                AutomationElement loginButton =
                    visionPlannerWindow.FindFirstDescendant(
                        _automation.ConditionFactory
                            .ByAutomationId(
                                LoginButtonAutomationId));

                bool userNameVisible =
                    IsElementVisible(userNameField);

                bool passwordVisible =
                    IsElementVisible(passwordField);

                bool loginButtonVisible =
                    IsElementVisible(loginButton);

                TestContext.Progress.WriteLine(
                    "Login form element visibility - " +
                    "Username: " + userNameVisible +
                    ", Password: " + passwordVisible +
                    ", LoginButton: " + loginButtonVisible);

                return
                    userNameVisible &&
                    passwordVisible &&
                    loginButtonVisible;
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "Login form visibility warning: " +
                    exception.Message);

                return false;
            }
        }

        // ============================================================
        // ENTER USERNAME AND PASSWORD
        // ============================================================

        protected void EnterVisionPlannerCredentials(
            string userName,
            string password)
        {
            Assert.That(
                string.IsNullOrWhiteSpace(userName),
                Is.False,
                "Vision Planner username is empty.");

            Assert.That(
                string.IsNullOrWhiteSpace(password),
                Is.False,
                "Vision Planner password is empty.");

            Window visionPlannerWindow =
                FindWindowByTitle(
                    VisionPlannerWindowTitle);

            Assert.That(
                visionPlannerWindow,
                Is.Not.Null,
                "Vision Planner window was not found while " +
                "entering credentials.");

            FocusWindowSafely(
                visionPlannerWindow,
                "Vision Planner");

            Thread.Sleep(500);

            // ========================================================
            // USERNAME FIELD
            // ========================================================

            AutomationElement userNameField =
                WaitForVisibleElementByAutomationId(
                   VisionPlannerWindowTitle,
                   UserNameTextBoxAutomationId,
                    LoginFormSearchTimeoutMilliseconds);

            Assert.That(
                userNameField,
                Is.Not.Null,
                "Username field was not found. AutomationId: " +
               UserNameTextBoxAutomationId);

            LogElementDetails(
                "Username field",
                userNameField);

            SetTextUsingKeyboard(
                userNameField,
                userName,
                "Username field",
                false);

            /*
             * Re-find the password field after entering username.
             * WPF can regenerate controls after a value changes.
             */

            // ========================================================
            // PASSWORD FIELD
            // ========================================================

            AutomationElement passwordField =
                WaitForVisibleElementByAutomationId(
                    VisionPlannerWindowTitle,
                    PasswordTextBoxAutomationId,
                    LoginFormSearchTimeoutMilliseconds);

            Assert.That(
                passwordField,
                Is.Not.Null,
                "Password field was not found. AutomationId: " +
                PasswordTextBoxAutomationId);

            LogElementDetails(
                "Password field",
                passwordField);

            SetTextUsingKeyboard(
                passwordField,
                password,
                "Password field",
                true);

            TestContext.Progress.WriteLine(
                "Username and password entered successfully.");
        }

        // ============================================================
        // ENTER TEXT USING KEYBOARD
        // ============================================================

        protected static void SetTextUsingKeyboard(
            AutomationElement element,
            string value,
            string elementDescription,
            bool sensitive)
        {
            TestContext.Progress.WriteLine(
                "Starting text entry for: " +
                elementDescription);

            Assert.That(
                element,
                Is.Not.Null,
                elementDescription +
                " is null.");

            Assert.That(
                string.IsNullOrEmpty(value),
                Is.False,
                elementDescription +
                " value is empty.");

            Assert.That(
                IsElementVisible(element),
                Is.True,
                elementDescription +
                " is not visible or enabled.");

            var rectangle =
                element.BoundingRectangle;

            TestContext.Progress.WriteLine(
                elementDescription +
                " coordinates - Left: " +
                rectangle.Left +
                ", Top: " +
                rectangle.Top +
                ", Width: " +
                rectangle.Width +
                ", Height: " +
                rectangle.Height);

            Assert.That(
                rectangle.Left,
                Is.GreaterThanOrEqualTo(0),
                elementDescription +
                " has an invalid left coordinate.");

            Assert.That(
                rectangle.Top,
                Is.GreaterThanOrEqualTo(0),
                elementDescription +
                " has an invalid top coordinate.");

            Assert.That(
                rectangle.Width,
                Is.GreaterThan(0),
                elementDescription +
                " has an invalid width.");

            Assert.That(
                rectangle.Height,
                Is.GreaterThan(0),
                elementDescription +
                " has an invalid height.");

            var centerPoint =
                rectangle.Center();

            TestContext.Progress.WriteLine(
                "Clicking " +
                elementDescription +
                " at: X=" +
                centerPoint.X +
                ", Y=" +
                centerPoint.Y);

            Mouse.MoveTo(centerPoint);

            Thread.Sleep(300);

            Mouse.Click(
                MouseButton.Left);

            Thread.Sleep(300);

            TestContext.Progress.WriteLine(
                "Selecting all text in " +
                elementDescription);

            /*
             * Select and clear any existing value.
             */
            Keyboard.TypeSimultaneously(
                VirtualKeyShort.CONTROL,
                VirtualKeyShort.KEY_A);

            Thread.Sleep(200);

            Keyboard.Press(
                VirtualKeyShort.BACK);

            Thread.Sleep(200);

            TestContext.Progress.WriteLine(
                "Typing text into " +
                elementDescription);

            /*
             * Enter the new value.
             */
            Keyboard.Type(value);

            Thread.Sleep(500);

            if (sensitive)
            {
                TestContext.Progress.WriteLine(
                    elementDescription +
                    " populated. Sensitive text was not logged.");
            }
            else
            {
                TestContext.Progress.WriteLine(
                    elementDescription +
                    " populated with: " +
                    value);
            }
        }

        // ============================================================
        // CLICK VISION PLANNER LOGIN BUTTON
        // ============================================================

        protected void ClickVisionPlannerLoginButton()
        {
            Window visionPlannerWindow =
                FindWindowByTitle(
                    VisionPlannerWindowTitle);

            Assert.That(
                visionPlannerWindow,
                Is.Not.Null,
                "Vision Planner window was not found before " +
                "clicking Login.");

            FocusWindowSafely(
                visionPlannerWindow,
                "Vision Planner");

            Thread.Sleep(500);

            AutomationElement loginButton =
                WaitForVisibleElementByAutomationId(
                   VisionPlannerWindowTitle,
               LoginButtonAutomationId,
                    LoginFormSearchTimeoutMilliseconds);

            Assert.That(
                loginButton,
                Is.Not.Null,
                "Login button was not found. AutomationId: " +
              LoginButtonAutomationId);

            Assert.That(
                loginButton.IsEnabled,
                Is.True,
                "Vision Planner Login button is disabled.");

            LogElementDetails(
                "Vision Planner Login button",
                loginButton);

            ClickElementCenterWithMouse(
                loginButton,
                "Vision Planner Login button");

            TestContext.Progress.WriteLine(
                "Vision Planner Login button clicked.");
        }

        // ============================================================
        // WAIT FOR LOGIN RESULT
        // ============================================================

        protected bool WaitForLoginResult(
            int timeoutMilliseconds)
        {
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   timeoutMilliseconds)
            {
                Window visionPlannerWindow =
                    FindWindowByTitle(
                       VisionPlannerWindowTitle);

                /*
                 * Vision Planner can replace or rename the login
                 * window after successful authentication.
                 */
                if (visionPlannerWindow == null)
                {
                    TestContext.Progress.WriteLine(
                        "Original Vision Planner login window is no " +
                        "longer available. Login transition confirmed.");

                    return true;
                }

                string errorMessage =
                    GetVisibleLoginError(
                        visionPlannerWindow);

                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    Assert.Fail(
                        "Vision Planner login failed. " +
                        "Application error: " +
                        errorMessage);
                }

                AutomationElement passwordField =
                    visionPlannerWindow.FindFirstDescendant(
                        _automation.ConditionFactory
                            .ByAutomationId(
                                PasswordTextBoxAutomationId));

                AutomationElement loginButton =
                    visionPlannerWindow.FindFirstDescendant(
                        _automation.ConditionFactory
                            .ByAutomationId(
                              LoginButtonAutomationId));

                bool passwordVisible =
                    IsElementVisible(passwordField);

                bool loginButtonVisible =
                    IsElementVisible(loginButton);

                /*
                 * WPF may retain the controls in the automation tree.
                 * Therefore, check visibility rather than null only.
                 */
                if (!passwordVisible &&
                    !loginButtonVisible)
                {
                    TestContext.Progress.WriteLine(
                        "Password field and Login button are no longer " +
                        "visible. Login navigation confirmed.");

                    return true;
                }

                TestContext.Progress.WriteLine(
                    "Waiting for login result. Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(1000);
            }

            return false;
        }

        // ============================================================
        // GET VISIBLE LOGIN ERROR
        // ============================================================

        protected string GetVisibleLoginError(
            Window visionPlannerWindow)
        {
            if (visionPlannerWindow == null)
            {
                return string.Empty;
            }

            try
            {
                AutomationElement errorElement =
                    visionPlannerWindow.FindFirstDescendant(
                        _automation.ConditionFactory
                            .ByAutomationId(
                              LoginErrorTextAutomationId));

                if (!IsElementVisible(errorElement))
                {
                    return string.Empty;
                }

                string errorMessage =
                    GetElementNameSafely(
                        errorElement);

                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    return string.Empty;
                }

                TestContext.Progress.WriteLine(
                    "Visible login error detected: " +
                    errorMessage);

                return errorMessage;
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "Login error read warning: " +
                    exception.Message);

                return string.Empty;
            }
        }

        // ============================================================
        // FIND LOGIN LIST ITEM BY CHILD TEXT
        // ============================================================

        protected AutomationElement FindLoginListItemByChildText(
            Window parentWindow,
            string partialTileName)
        {
            if (parentWindow == null)
            {
                return null;
            }

            try
            {
                AutomationElement recentUsersList =
                    parentWindow.FindFirstDescendant(
                        _automation.ConditionFactory
                            .ByAutomationId(
                            RecentUsersListAutomationId));

                if (recentUsersList == null)
                {
                    TestContext.Progress.WriteLine(
                        "Recent-users list was not found. " +
                        "AutomationId: " +
                    RecentUsersListAutomationId);

                    return null;
                }

                AutomationElement[] descendants =
                    recentUsersList.FindAllDescendants();

                AutomationElement[] listItems =
                    descendants
                        .Where(element =>
                            GetControlTypeSafely(element)
                                .IndexOf(
                                    "ListItem",
                                    StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToArray();

                TestContext.Progress.WriteLine(
                    "Login ListItems found: " +
                    listItems.Length);

                foreach (AutomationElement listItem in listItems)
                {
                    if (!IsElementVisible(listItem))
                    {
                        continue;
                    }

                    string listItemName =
                        GetElementNameSafely(listItem);

                    TestContext.Progress.WriteLine(
                        "ListItem candidate Name: " +
                        listItemName);

                    if (ContainsIgnoreCase(
                            listItemName,
                            partialTileName))
                    {
                        TestContext.Progress.WriteLine(
                            "Login tile matched by ListItem Name.");

                        return listItem;
                    }

                    AutomationElement[] children =
                        listItem.FindAllDescendants();

                    foreach (AutomationElement child in children)
                    {
                        string childName =
                            GetElementNameSafely(child);

                        if (string.IsNullOrWhiteSpace(childName))
                        {
                            continue;
                        }

                        TestContext.Progress.WriteLine(
                            "Login child candidate Name: " +
                            childName);

                        if (ContainsIgnoreCase(
                                childName,
                                partialTileName))
                        {
                            TestContext.Progress.WriteLine(
                                "Login tile matched by child Name.");

                            TestContext.Progress.WriteLine(
                                "Matching child Name: " +
                                childName);

                            return listItem;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "Login tile search warning: " +
                    exception.Message);
            }

            return null;
        }

        // ============================================================
        // WAIT FOR LOGIN LIST ITEM
        // ============================================================

        protected AutomationElement WaitForLoginListItemByChildText(
            string partialTileName,
            int timeoutMilliseconds)
        {
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   timeoutMilliseconds)
            {
                Window parentWindow =
                    FindWindowByTitle(
                      VisionPlannerWindowTitle);

                AutomationElement listItem =
                    FindLoginListItemByChildText(
                        parentWindow,
                        partialTileName);

                if (listItem != null)
                {
                    return listItem;
                }

                TestContext.Progress.WriteLine(
                    "Waiting for login tile '" +
                    partialTileName +
                    "'. Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(
                    PollingIntervalMilliseconds);
            }

            return null;
        }

        // ============================================================
        // WAIT FOR VISIBLE ELEMENT BY AUTOMATION ID
        // ============================================================

        protected AutomationElement WaitForVisibleElementByAutomationId(
            string windowTitle,
            string automationId,
            int timeoutMilliseconds)
        {
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   timeoutMilliseconds)
            {
                try
                {
                    Window window =
                        FindWindowByTitle(
                            windowTitle);

                    if (window != null)
                    {
                        AutomationElement element =
                            window.FindFirstDescendant(
                                _automation.ConditionFactory
                                    .ByAutomationId(
                                        automationId));

                        if (IsElementVisible(element))
                        {
                            return element;
                        }
                    }
                }
                catch (Exception exception)
                {
                    TestContext.Progress.WriteLine(
                        "Element search warning for '" +
                        automationId +
                        "': " +
                        exception.Message);
                }

                TestContext.Progress.WriteLine(
                    "Waiting for AutomationId '" +
                    automationId +
                    "'. Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(500);
            }

            return null;
        }

        // ============================================================
        // CLICK ELEMENT CENTER WITH MOUSE
        // ============================================================

        protected static void ClickElementCenterWithMouse(
            AutomationElement element,
            string elementDescription)
        {
            Assert.That(
                element,
                Is.Not.Null,
                elementDescription +
                " cannot be null.");

            var rectangle =
                element.BoundingRectangle;

            Assert.That(
                rectangle.Left,
                Is.GreaterThanOrEqualTo(0),
                elementDescription +
                " has an invalid left coordinate: " +
                rectangle.Left);

            Assert.That(
                rectangle.Top,
                Is.GreaterThanOrEqualTo(0),
                elementDescription +
                " has an invalid top coordinate: " +
                rectangle.Top);

            Assert.That(
                rectangle.Width,
                Is.GreaterThan(0),
                elementDescription +
                " has an invalid width.");

            Assert.That(
                rectangle.Height,
                Is.GreaterThan(0),
                elementDescription +
                " has an invalid height.");

            var centerPoint =
                rectangle.Center();

            TestContext.Progress.WriteLine(
                elementDescription +
                " click coordinates: X=" +
                centerPoint.X +
                ", Y=" +
                centerPoint.Y);

            Mouse.MoveTo(centerPoint);

            Thread.Sleep(500);

            Mouse.Click(
                MouseButton.Left);

            Thread.Sleep(1000);
        }

        // ============================================================
        // CHECK ELEMENT VISIBILITY AND SCREEN POSITION
        // ============================================================

        protected static bool IsElementVisible(
            AutomationElement element)
        {
            if (element == null)
            {
                return false;
            }

            try
            {
                if (!element.IsEnabled)
                {
                    return false;
                }

                if (element.IsOffscreen)
                {
                    return false;
                }

                var rectangle =
                    element.BoundingRectangle;

                if (rectangle.Width <= 0 ||
                    rectangle.Height <= 0)
                {
                    return false;
                }

                /*
                 * Reject stale WPF coordinates.
                 */
                if (rectangle.Right <= 0 ||
                    rectangle.Bottom <= 0)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // OPEN WINDOWS SHORTCUT
        // ============================================================

        protected static void OpenWindowsShortcut(
            string shortcutPath)
        {
            Assert.That(
                string.IsNullOrWhiteSpace(shortcutPath),
                Is.False,
                "Shortcut path is empty.");

            Assert.That(
                File.Exists(shortcutPath),
                Is.True,
                "Shortcut was not found at: " +
                shortcutPath);

            ProcessStartInfo startInfo =
                new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "\"" + shortcutPath + "\"",
                    UseShellExecute = true
                };

            Process.Start(startInfo);
        }

        // ============================================================
        // FIND WINDOW BY TITLE
        // ============================================================

        protected Window FindWindowByTitle(
            string expectedWindowTitle)
        {
            if (string.IsNullOrWhiteSpace(
                    expectedWindowTitle))
            {
                return null;
            }

            try
            {
                AutomationElement desktop =
                    _automation.GetDesktop();

                AutomationElement[] desktopChildren =
                    desktop.FindAllChildren();

                foreach (AutomationElement child in desktopChildren)
                {
                    string windowName =
                        GetElementNameSafely(child);

                    if (!ContainsIgnoreCase(
                            windowName,
                            expectedWindowTitle))
                    {
                        continue;
                    }

                    try
                    {
                        Window window =
                            child.AsWindow();

                        if (window != null)
                        {
                            return window;
                        }
                    }
                    catch
                    {
                        /*
                         * The matching desktop item was not a Window.
                         */
                    }
                }
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "Window search warning: " +
                    exception.Message);
            }

            return null;
        }

        // ============================================================
        // WAIT FOR WINDOW BY TITLE
        // ============================================================

        protected Window WaitForWindowByTitle(
            string expectedWindowTitle,
            int timeoutMilliseconds)
        {
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   timeoutMilliseconds)
            {
                Window window =
                    FindWindowByTitle(
                        expectedWindowTitle);

                if (window != null)
                {
                    return window;
                }

                TestContext.Progress.WriteLine(
                    "Waiting for window '" +
                    expectedWindowTitle +
                    "'. Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(
                    PollingIntervalMilliseconds);
            }

            return null;
        }

        // ============================================================
        // FOCUS WINDOW SAFELY
        // ============================================================

        protected static void FocusWindowSafely(
            Window window,
            string windowDescription)
        {
            if (window == null)
            {
                return;
            }

            try
            {
                window.Focus();

                TestContext.Progress.WriteLine(
                    windowDescription +
                    " window focused.");
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    windowDescription +
                    " focus warning: " +
                    exception.Message);
            }
        }

        // ============================================================
        // CASE-INSENSITIVE CONTAINS
        // ============================================================

        protected static bool ContainsIgnoreCase(
            string completeText,
            string partialText)
        {
            if (string.IsNullOrWhiteSpace(completeText) ||
                string.IsNullOrWhiteSpace(partialText))
            {
                return false;
            }

            return completeText.IndexOf(
                       partialText,
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ============================================================
        // LOG ELEMENT DETAILS
        // ============================================================

        protected static void LogElementDetails(
            string description,
            AutomationElement element)
        {
            if (element == null)
            {
                return;
            }

            TestContext.Progress.WriteLine(
                description +
                " Name: " +
                GetElementNameSafely(element));

            TestContext.Progress.WriteLine(
                description +
                " AutomationId: " +
                GetAutomationIdSafely(element));

            TestContext.Progress.WriteLine(
                description +
                " ClassName: " +
                GetClassNameSafely(element));

            TestContext.Progress.WriteLine(
                description +
                " ControlType: " +
                GetControlTypeSafely(element));

            try
            {
                var rectangle =
                    element.BoundingRectangle;

                TestContext.Progress.WriteLine(
                    description +
                    " Bounds: Left=" +
                    rectangle.Left +
                    ", Top=" +
                    rectangle.Top +
                    ", Width=" +
                    rectangle.Width +
                    ", Height=" +
                    rectangle.Height);
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    description +
                    " bounds warning: " +
                    exception.Message);
            }
        }

        // ============================================================
        // SAFE ELEMENT PROPERTY METHODS
        // ============================================================

        protected static string GetElementNameSafely(
            AutomationElement element)
        {
            if (element == null)
            {
                return string.Empty;
            }

            try
            {
                return element.Name ??
                       string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        protected static string GetAutomationIdSafely(
            AutomationElement element)
        {
            if (element == null)
            {
                return string.Empty;
            }

            try
            {
                return element.AutomationId ??
                       string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        protected static string GetClassNameSafely(
            AutomationElement element)
        {
            if (element == null)
            {
                return string.Empty;
            }

            try
            {
                return element.ClassName ??
                       string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        protected static string GetControlTypeSafely(
            AutomationElement element)
        {
            if (element == null)
            {
                return string.Empty;
            }

            try
            {
                return element.ControlType.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        // ============================================================
        // PATIENT CREATION HELPERS
        // ============================================================

        protected void ClickNewPatientButton()
        {
            TestContext.Progress.WriteLine(
                "Searching for the New Patient button.");

            Window visionPlannerWindow =
                FindWindowByTitle(
                    VisionPlannerWindowTitle);

            Assert.That(
                visionPlannerWindow,
                Is.Not.Null,
                "Vision Planner window was not found before clicking " +
                "the New Patient button.");

            FocusWindowSafely(
                visionPlannerWindow,
                "Vision Planner");

            Thread.Sleep(1000);

            AutomationElement newPatientButton =
                WaitForVisibleButtonByExactName(
                    VisionPlannerWindowTitle,
                    NewPatientButtonName,
                    NewpatientButtoninterval);

            Assert.That(
                newPatientButton,
                Is.Not.Null,
                "The New Patient button was not found. " +
                "Expected Name: '" +
                NewPatientButtonName +
                "'.");

            Assert.That(
                newPatientButton.IsEnabled,
                Is.True,
                "The New Patient button was found, but it is disabled.");

            Assert.That(
                newPatientButton.IsOffscreen,
                Is.False,
                "The New Patient button was found, but it is offscreen.");

            LogElementDetails(
                "New Patient button",
                newPatientButton);

            ClickElementCenterWithMouse(
                newPatientButton,
                "New Patient button");

            TestContext.Progress.WriteLine(
                "New Patient button clicked successfully.");

            Thread.Sleep(2000);
        }

        // ============================================================
        // WAIT FOR VISIBLE BUTTON BY EXACT NAME
        // ============================================================

        protected AutomationElement WaitForVisibleButtonByExactName(
            string windowTitle,
            string expectedButtonName,
            int timeoutMilliseconds)
        {
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   timeoutMilliseconds)
            {
                try
                {
                    Window parentWindow =
                        FindWindowByTitle(
                            windowTitle);

                    if (parentWindow != null)
                    {
                        AutomationElement button =
                            FindVisibleButtonByExactName(
                                parentWindow,
                                expectedButtonName);

                        if (button != null)
                        {
                            return button;
                        }
                    }
                }
                catch (Exception exception)
                {
                    TestContext.Progress.WriteLine(
                        "Button search warning for '" +
                        expectedButtonName +
                        "': " +
                        exception.Message);
                }

                TestContext.Progress.WriteLine(
                    "Waiting for button '" +
                    expectedButtonName +
                    "'. Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(500);
            }

            return null;
        }

        // ============================================================
        // FIND VISIBLE BUTTON BY EXACT NAME
        // ============================================================

        protected AutomationElement FindVisibleButtonByExactName(
            Window parentWindow,
            string expectedButtonName)
        {
            if (parentWindow == null ||
                string.IsNullOrWhiteSpace(expectedButtonName))
            {
                return null;
            }

            try
            {
                AutomationElement[] allElements =
                    parentWindow.FindAllDescendants();

                foreach (AutomationElement element in allElements)
                {
                    if (!IsElementVisible(element))
                    {
                        continue;
                    }

                    string elementName =
                        GetElementNameSafely(element);

                    if (!string.Equals(
                            elementName,
                            expectedButtonName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string controlType =
                        GetControlTypeSafely(element);

                    if (controlType.IndexOf(
                            "Button",
                            StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    TestContext.Progress.WriteLine(
                        "Matching visible button found.");

                    TestContext.Progress.WriteLine(
                        "Button Name: " +
                        elementName);

                    TestContext.Progress.WriteLine(
                        "Button AutomationId: " +
                        GetAutomationIdSafely(element));

                    TestContext.Progress.WriteLine(
                        "Button ControlType: " +
                        controlType);

                    return element;
                }
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "FindVisibleButtonByExactName warning: " +
                    exception.Message);
            }

            return null;
        }

        protected void EnterPatientDetails(
    string firstName,
    string lastName,
    string dobDay,
    string dobMonth,
    string dobYear,
    string patientID,
    string gender)
        {
            Assert.That(
                string.IsNullOrWhiteSpace(lastName),
                Is.False,
                "Patient Last Name is empty in Excel.");

            Assert.That(
                string.IsNullOrWhiteSpace(firstName),
                Is.False,
                "Patient First Name is empty in Excel.");

            Assert.That(
                string.IsNullOrWhiteSpace(dobDay),
                Is.False,
                "Patient Date of Birth Day is empty in Excel.");
            Assert.That(
                string.IsNullOrWhiteSpace(dobMonth),
                Is.False,
                "Patient Date of Birth Moth is empty in Excel.");
            Assert.That(
                string.IsNullOrWhiteSpace(dobYear),
                Is.False,
                "Patient Date of Birth Year is empty in Excel.");

            Assert.That(
                string.IsNullOrWhiteSpace(patientID),
                Is.False,
                "Patient ID is empty in Excel.");

            Assert.That(
                string.IsNullOrWhiteSpace(gender),
                Is.False,
                "Patient Gender is empty in Excel.");

            Window visionPlannerWindow =
                FindWindowByTitle(
                    VisionPlannerWindowTitle);

            Assert.That(
                visionPlannerWindow,
                Is.Not.Null,
                "Vision Planner window is not open.");

            FocusWindowSafely(
                visionPlannerWindow,
                "Vision Planner");

            Thread.Sleep(1000);

            /*
             * Enter Last Name
             *
             * Important:
             * FlaUInspect shows two colons before Last Name.
             */
            AutomationElement lastNameField =
                WaitForVisibleElementByAutomationId(
                    VisionPlannerWindowTitle,
                    "Add or Edit a patient::Last Name",
                    LoginFormSearchTimeoutMilliseconds);

            Assert.That(
                lastNameField,
                Is.Not.Null,
                "Last Name field was not found using AutomationId " +
                "'Add or Edit a patient::Last Name'.");

            TestContext.Progress.WriteLine(
                "Last Name field found.");

            SetTextUsingKeyboard(
                lastNameField,
                lastName.Trim(),
                "Last Name field",
                sensitive: false);

            Thread.Sleep(500);

            /*
             * Enter First Name
             *
             * FlaUInspect also shows two colons before First Name.
             */
            AutomationElement firstNameField =
                WaitForVisibleElementByAutomationId(
                    VisionPlannerWindowTitle,
                    "Add or Edit a patient::First Name",
                    LoginFormSearchTimeoutMilliseconds);

            Assert.That(
                firstNameField,
                Is.Not.Null,
                "First Name field was not found using AutomationId " +
                "'Add or Edit a patient::First Name'.");

            TestContext.Progress.WriteLine(
                "First Name field found.");

            SetTextUsingKeyboard(
                firstNameField,
                firstName.Trim(),
                "First Name field",
                sensitive: false);

            Thread.Sleep(500);

            /*
             * Enter Date of Birth - Day
             *
             * FlaUInspect shows one colon for the DOB AutomationId.
             */
            AutomationElement dobDayField =
                WaitForVisibleElementByAutomationId(
                    VisionPlannerWindowTitle,
                    "Add or Edit a patient::Date of birth. Select day",
                    LoginFormSearchTimeoutMilliseconds);

            Assert.That(
                dobDayField,
                Is.Not.Null,
                "Date of Birth Day field was not found.");

            SetTextUsingKeyboard(
                dobDayField,
                dobDay.Trim(),
                "Date of Birth Day field",
                sensitive: false);

            Thread.Sleep(500);

            /*
             * Find Date of Birth - Month
             */
            AutomationElement dobMonthField =
                WaitForVisibleElementByAutomationId(
                    VisionPlannerWindowTitle,
                    "Add or Edit a patient::Date of birth. Select month",
                    LoginFormSearchTimeoutMilliseconds);

            Assert.That(
                dobMonthField,
                Is.Not.Null,
                "Date of Birth Month field was not found.");
            SetTextUsingKeyboard(
               dobMonthField,
               dobMonth.Trim(),
               "Date of Birth Month field",
               sensitive: false);

            Thread.Sleep(500);

            /*
             * Find Date of Birth - Year
             */
            AutomationElement dobYearField =
                WaitForVisibleElementByAutomationId(
                    VisionPlannerWindowTitle,
                    "Add or Edit a patient::Date of birth. Select year",
                    LoginFormSearchTimeoutMilliseconds);
            Assert.That(
                dobYearField,
                Is.Not.Null,
                "Date of Birth Month field was not found.");
            SetTextUsingKeyboard(
               dobYearField,
               dobYear.Trim(),
               "Date of Birth Month field",
               sensitive: false);


            Assert.That(
                dobYearField,
                Is.Not.Null,
                "Date of Birth Year field was not found.");

            /*
             * Enter Patient ID
             */
            AutomationElement patientIDField =
                WaitForVisibleElementByAutomationId(
                    VisionPlannerWindowTitle,
                    "Add or Edit a patient::Patient ID",
                    LoginFormSearchTimeoutMilliseconds);

            Assert.That(
                patientIDField,
                Is.Not.Null,
                "Patient ID field was not found.");

            SetTextUsingKeyboard(
                patientIDField,
                patientID.Trim(),
                "Patient ID field",
                sensitive: false);

            Thread.Sleep(500);

            /*
             * Select Gender
             */
            SelectPatientGender(
                gender.Trim());

            TestContext.Progress.WriteLine(
                "All patient details entered successfully.");
        }


        protected void SelectPatientGender(
            string gender)
        {
            string genderAutomationId = null;

            if (gender.Equals("Male",
                    StringComparison.OrdinalIgnoreCase))
            {
                genderAutomationId =
                    "Add or Edit a patient::Gender Male";
            }
            else if (gender.Equals("Female",
                    StringComparison.OrdinalIgnoreCase))
            {
                genderAutomationId =
                    "Add or Edit a patient::Gender Female";
            }
            else if (gender.Equals("Other",
                    StringComparison.OrdinalIgnoreCase) ||
                     gender.Equals("Unknown",
                    StringComparison.OrdinalIgnoreCase))
            {
                genderAutomationId =
                    "Add or Edit a patient::Gender Unknown";
            }

            Assert.That(
                string.IsNullOrWhiteSpace(genderAutomationId),
                Is.False,
                "Invalid gender value: " +
                gender);

            AutomationElement genderButton =
                WaitForVisibleElementByAutomationId(
                    VisionPlannerWindowTitle,
                    genderAutomationId,
                    LoginFormSearchTimeoutMilliseconds);

            Assert.That(
                genderButton,
                Is.Not.Null,
                "Gender button for '" +
                gender +
                "' was not found.");

            var rectangle =
                genderButton.BoundingRectangle;

            var centerPoint =
                rectangle.Center();

            Mouse.MoveTo(centerPoint);

            Thread.Sleep(300);

            Mouse.Click(MouseButton.Left);

            Thread.Sleep(500);

            TestContext.Progress.WriteLine(
                "Gender '" +
                gender +
                "' selected.");
        }

        protected void ClickSavePatientButton()
        {
            AutomationElement saveButton =
                WaitForVisibleElementByAutomationId(
                    VisionPlannerWindowTitle,
                    "Add or Edit a patient::Save",
                    LoginFormSearchTimeoutMilliseconds);

            Assert.That(
                saveButton,
                Is.Not.Null,
                "Save button was not found.");

            var rectangle =
                saveButton.BoundingRectangle;

            var centerPoint =
                rectangle.Center();

            Mouse.MoveTo(centerPoint);

            Thread.Sleep(300);

            Mouse.Click(MouseButton.Left);

            Thread.Sleep(1000);

            TestContext.Progress.WriteLine(
                "Save button clicked. Patient creation completed.");
        }
        // ============================================================
        // WAIT FOR CREATED PATIENT
        // ============================================================

        protected AutomationElement WaitForCreatedPatient(
            string expectedPatientName,
            int timeoutMilliseconds)
        {
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   timeoutMilliseconds)
            {
                try
                {
                    Window visionPlannerWindow =
                        FindWindowByTitle(
                            VisionPlannerWindowTitle);

                    if (visionPlannerWindow != null)
                    {
                        AutomationElement[] allElements =
                            visionPlannerWindow.FindAllDescendants();

                        foreach (AutomationElement element in allElements)
                        {
                            string automationId =
                                GetAutomationIdSafely(
                                    element);

                            if (!string.Equals(
                                    automationId,
                                    CreatedPatientNameAutomationId,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            if (!IsElementVisible(element))
                            {
                                continue;
                            }

                            string actualPatientName =
                                GetElementNameSafely(
                                    element);

                            TestContext.Progress.WriteLine(
                                "Patient candidate found: '" +
                                actualPatientName +
                                "'.");

                            if (string.Equals(
                                    actualPatientName.Trim(),
                                    expectedPatientName.Trim(),
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                TestContext.Progress.WriteLine(
                                    "Expected created patient found.");

                                return element;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    TestContext.Progress.WriteLine(
                        "Created patient search warning: " +
                        exception.Message);
                }

                TestContext.Progress.WriteLine(
                    "Waiting for created patient '" +
                    expectedPatientName +
                    "'. Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(1000);
            }

            return null;
        }


        // ============================================================
        // WAIT FOR VISIBLE TEXT BY EXACT NAME
        // ============================================================

        protected AutomationElement WaitForVisibleElementByExactName(
            string expectedName,
            int timeoutMilliseconds)
        {
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   timeoutMilliseconds)
            {
                try
                {
                    Window visionPlannerWindow =
                        FindWindowByTitle(
                            VisionPlannerWindowTitle);

                    if (visionPlannerWindow != null)
                    {
                        AutomationElement[] allElements =
                            visionPlannerWindow.FindAllDescendants();

                        foreach (AutomationElement element in allElements)
                        {
                            if (!IsElementVisible(element))
                            {
                                continue;
                            }

                            string actualName =
                                GetElementNameSafely(
                                    element);

                            if (string.Equals(
                                    actualName.Trim(),
                                    expectedName.Trim(),
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                TestContext.Progress.WriteLine(
                                    "Expected visible text found: '" +
                                    actualName +
                                    "'.");

                                return element;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    TestContext.Progress.WriteLine(
                        "Visible text search warning for '" +
                        expectedName +
                        "': " +
                        exception.Message);
                }

                TestContext.Progress.WriteLine(
                    "Waiting for visible text '" +
                    expectedName +
                    "'. Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(1000);
            }

            return null;
        }


        // ============================================================
        // CREATE SAFE SCREENSHOT FILE NAME
        // ============================================================

        protected static string CreateSafeFileName(
            string originalName)
        {
            if (string.IsNullOrWhiteSpace(originalName))
            {
                return "UnknownPatient";
            }

            char[] invalidCharacters =
                Path.GetInvalidFileNameChars();

            string safeName =
                originalName;

            foreach (char invalidCharacter in invalidCharacters)
            {
                safeName =
                    safeName.Replace(
                        invalidCharacter,
                        '_');
            }

            safeName =
                safeName.Replace(
                    " ",
                    "_");

            return safeName.Trim();
        }


        // ============================================================
        // TAKE PATIENT VERIFICATION SCREENSHOT
        // ============================================================

        protected void TakePatientVerificationScreenshot(
            string patientName,
            string verificationResult)
        {
            try
            {
                Window visionPlannerWindow =
                    FindWindowByTitle(
                        VisionPlannerWindowTitle);

                if (visionPlannerWindow == null)
                {
                    TestContext.Progress.WriteLine(
                        "Screenshot was not captured because " +
                        "Vision Planner was not found.");

                    return;
                }

                string screenshotDirectory =
                    Path.Combine(
                        TestContext.CurrentContext.TestDirectory,
                        "Screenshots");

                Directory.CreateDirectory(
                    screenshotDirectory);

                string safePatientName =
                    CreateSafeFileName(
                        patientName);

                string timestamp =
                    DateTime.Now.ToString(
                        "yyyyMMdd_HHmmss");

                string screenshotFileName =
                    "PatientCreation_" +
                    safePatientName +
                    "_" +
                    verificationResult +
                    "_" +
                    timestamp +
                    ".png";

                string screenshotPath =
                    Path.Combine(
                        screenshotDirectory,
                        screenshotFileName);

                var screenshot =
                    Capture.Element(
                        visionPlannerWindow);

                screenshot.ToFile(
                    screenshotPath);

                if (!File.Exists(screenshotPath))
                {
                    TestContext.Progress.WriteLine(
                        "Screenshot file was not created.");

                    return;
                }

                TestContext.Progress.WriteLine(
                    "Patient verification screenshot saved: " +
                    screenshotPath);

                try
                {
                    TestContext.AddTestAttachment(
                        screenshotPath,
                        "Patient creation verification screenshot");

                    TestContext.Progress.WriteLine(
                        "Screenshot attached to NUnit test result.");
                }
                catch (Exception attachmentException)
                {
                    TestContext.Progress.WriteLine(
                        "Screenshot was saved, but attachment warning: " +
                        attachmentException.Message);
                }
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "Screenshot capture warning: " +
                    exception.Message);
            }
        }


        // ============================================================
        // VERIFY PATIENT CREATED SUCCESSFULLY
        // ============================================================

        protected void VerifyPatientCreatedSuccessfully(
            string firstName,
            string lastName,
            string dobDay,
            string dobMonth,
            string dobYear,
            string patientID)
        {
            Assert.That(
                string.IsNullOrWhiteSpace(firstName),
                Is.False,
                "First Name is empty in Excel.");

            Assert.That(
                string.IsNullOrWhiteSpace(lastName),
                Is.False,
                "Last Name is empty in Excel.");

            Assert.That(
                string.IsNullOrWhiteSpace(dobDay),
                Is.False,
                "Date of Birth Day is empty in Excel.");

            Assert.That(
                string.IsNullOrWhiteSpace(dobMonth),
                Is.False,
                "Date of Birth Month is empty in Excel.");

            Assert.That(
                string.IsNullOrWhiteSpace(dobYear),
                Is.False,
                "Date of Birth Year is empty in Excel.");

            Assert.That(
                string.IsNullOrWhiteSpace(patientID),
                Is.False,
                "Patient ID is empty in Excel.");

            string expectedPatientName =
                 lastName.Trim() +
                     " " +
                 firstName.Trim();

            string expectedPatientId =
                "Id: " +
                patientID.Trim();

            string expectedDateOfBirth =
                dobDay.Trim().PadLeft(2, '0') +
                "/" +
                dobMonth.Trim().PadLeft(2, '0') +
                "/" +
                dobYear.Trim();

            TestContext.Progress.WriteLine(
                "================================================");

            TestContext.Progress.WriteLine(
                "Starting patient creation verification.");

            TestContext.Progress.WriteLine(
                "Expected Patient Name: " +
                expectedPatientName);

            TestContext.Progress.WriteLine(
                "Expected Patient ID: " +
                expectedPatientId);

            TestContext.Progress.WriteLine(
                "Expected Date of Birth: " +
                expectedDateOfBirth);

            TestContext.Progress.WriteLine(
                "================================================");

            bool verificationPassed = false;

            try
            {
                // ----------------------------------------------------
                // VERIFY PATIENT NAME
                // ----------------------------------------------------

                AutomationElement patientNameElement =
                    WaitForCreatedPatient(
                        expectedPatientName,
                        PatientCreationVerificationTimeoutMilliseconds);

                Assert.That(
                    patientNameElement,
                    Is.Not.Null,
                    "Created patient was not found. " +
                    "Expected Patient Name: '" +
                    expectedPatientName +
                    "'.");

                string actualPatientName =
                    GetElementNameSafely(
                        patientNameElement);

                Assert.That(
                    actualPatientName,
                    Is.EqualTo(expectedPatientName)
                        .IgnoreCase,
                    "Patient Name does not match Excel data.");

                TestContext.Progress.WriteLine(
                    "Patient Name verified: " +
                    actualPatientName);

                // ----------------------------------------------------
                // VERIFY PATIENT ID
                // ----------------------------------------------------

                AutomationElement patientIdElement =
                    WaitForVisibleElementByExactName(
                        expectedPatientId,
                        PatientCreationVerificationTimeoutMilliseconds);

                Assert.That(
                    patientIdElement,
                    Is.Not.Null,
                    "Created Patient ID was not found. " +
                    "Expected Patient ID: '" +
                    expectedPatientId +
                    "'.");

                string actualPatientId =
                    GetElementNameSafely(
                        patientIdElement);

                Assert.That(
                    actualPatientId,
                    Is.EqualTo(expectedPatientId)
                        .IgnoreCase,
                    "Patient ID does not match Excel data.");

                TestContext.Progress.WriteLine(
                    "Patient ID verified: " +
                    actualPatientId);

                // ----------------------------------------------------
                // VERIFY DATE OF BIRTH
                // ----------------------------------------------------

                AutomationElement dateOfBirthElement =
                    WaitForVisibleElementByExactName(
                        expectedDateOfBirth,
                        PatientCreationVerificationTimeoutMilliseconds);

                Assert.That(
                    dateOfBirthElement,
                    Is.Not.Null,
                    "Created Patient Date of Birth was not found. " +
                    "Expected Date of Birth: '" +
                    expectedDateOfBirth +
                    "'.");

                string actualDateOfBirth =
                    GetElementNameSafely(
                        dateOfBirthElement);

                Assert.That(
                    actualDateOfBirth,
                    Is.EqualTo(expectedDateOfBirth),
                    "Patient Date of Birth does not match Excel data.");

                TestContext.Progress.WriteLine(
                    "Patient Date of Birth verified: " +
                    actualDateOfBirth);

                verificationPassed = true;

                TestContext.Progress.WriteLine(
                    "================================================");

                TestContext.Progress.WriteLine(
                    "PATIENT CREATION VERIFICATION PASSED.");

                TestContext.Progress.WriteLine(
                    "================================================");
            }
            finally
            {
                string verificationResult =
                    verificationPassed
                        ? "PASSED"
                        : "FAILED";

                /*
                 * Screenshot is taken whether the verification
                 * passes or fails.
                 */
                TakePatientVerificationScreenshot(
                    expectedPatientName,
                    verificationResult);
            }
        }

        // ============================================================
        // SEARCH AND SELECT PATIENT BY MRN
        // ============================================================

        protected void SearchAndSelectPatientByMrn(
            string patientMrn)
        {
            Assert.That(
                string.IsNullOrWhiteSpace(patientMrn),
                Is.False,
                "Capture-measurement Patient MRN is empty in Excel.");

            patientMrn =
                patientMrn.Trim();

            TestContext.Progress.WriteLine(
                "================================================");

            TestContext.Progress.WriteLine(
                "Starting patient search.");

            TestContext.Progress.WriteLine(
                "Patient MRN from Excel: " +
                patientMrn);

            TestContext.Progress.WriteLine(
                "================================================");

            Window visionPlannerWindow =
                FindWindowByTitle(
                    VisionPlannerWindowTitle);

            Assert.That(
                visionPlannerWindow,
                Is.Not.Null,
                "Vision Planner window is not open.");

            FocusWindowSafely(
                visionPlannerWindow,
                "Vision Planner");

            Thread.Sleep(1000);

            /*
             * Locate the search textbox.
             *
             * FlaUInspect:
             * AutomationId = Search Item
             * ControlType  = Edit
             */
            AutomationElement searchTextBox =
                WaitForVisibleElementByAutomationId(
                    VisionPlannerWindowTitle,
                    PatientSearchTextBoxAutomationId,
                    PatientSearchTimeoutMilliseconds);

            Assert.That(
                searchTextBox,
                Is.Not.Null,
                "Patient search field was not found. " +
                "AutomationId: '" +
                PatientSearchTextBoxAutomationId +
                "'.");

            LogElementDetails(
                "Patient search field",
                searchTextBox);

            /*
             * Clear any old search value and enter the MRN.
             */
            SetTextUsingKeyboard(
                searchTextBox,
                patientMrn,
                "Patient search field",
                sensitive: false);

            TestContext.Progress.WriteLine(
                "Patient MRN entered in search field: " +
                patientMrn);

            /*
             * Allow Vision Planner to filter the patient list.
             */
            Thread.Sleep(1500);

            /*
             * Vision Planner displays the MRN as:
             *
             * Id: ARP6
             */
            AutomationElement patientResult =
                WaitForPatientSearchResultByMrn(
                    patientMrn,
                    PatientSearchTimeoutMilliseconds);

            Assert.That(
                patientResult,
                Is.Not.Null,
                "No patient was found for MRN '" +
                patientMrn +
                "'.");

            string actualPatientIdText =
                GetElementNameSafely(
                    patientResult);

            TestContext.Progress.WriteLine(
                "Patient search result found: " +
                actualPatientIdText);

            /*
             * Click the visible MRN text inside the patient result tile.
             * A physical click is used because the Text control does not
             * expose InvokePattern in the attached FlaUInspect view.
             */
            ClickElementCenterWithMouse(
                patientResult,
                "Patient search result for MRN " +
                patientMrn);

            Thread.Sleep(1500);

            /*
             * Verify that the result still exists after selection.
             */
            AutomationElement selectedPatientResult =
                WaitForPatientSearchResultByMrn(
                    patientMrn,
                    10000);

            Assert.That(
                selectedPatientResult,
                Is.Not.Null,
                "Patient search result disappeared after clicking. " +
                "MRN: '" +
                patientMrn +
                "'.");

            TestContext.Progress.WriteLine(
                "================================================");

            TestContext.Progress.WriteLine(
                "PATIENT SEARCH AND SELECTION PASSED.");

            TestContext.Progress.WriteLine(
                "Selected Patient MRN: " +
                patientMrn);

            TestContext.Progress.WriteLine(
                "================================================");
        }


        // ============================================================
        // WAIT FOR PATIENT SEARCH RESULT BY MRN
        // ============================================================

        protected AutomationElement WaitForPatientSearchResultByMrn(
            string patientMrn,
            int timeoutMilliseconds)
        {
            Assert.That(
                string.IsNullOrWhiteSpace(patientMrn),
                Is.False,
                "Patient MRN cannot be empty.");

            string expectedPatientIdText =
                "Id: " +
                patientMrn.Trim();

            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   timeoutMilliseconds)
            {
                try
                {
                    Window visionPlannerWindow =
                        FindWindowByTitle(
                            VisionPlannerWindowTitle);

                    if (visionPlannerWindow != null)
                    {
                        AutomationElement[] allElements =
                            visionPlannerWindow.FindAllDescendants();

                        foreach (AutomationElement element in allElements)
                        {
                            if (!IsElementVisible(element))
                            {
                                continue;
                            }

                            string actualElementName =
                                GetElementNameSafely(
                                    element);

                            if (string.Equals(
                                    actualElementName.Trim(),
                                    expectedPatientIdText.Trim(),
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                TestContext.Progress.WriteLine(
                                    "Matching patient result found: '" +
                                    actualElementName +
                                    "'.");

                                return element;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    TestContext.Progress.WriteLine(
                        "Patient-result search warning: " +
                        exception.Message);
                }

                TestContext.Progress.WriteLine(
                    "Waiting for patient MRN '" +
                    patientMrn +
                    "'. Expected UI text: '" +
                    expectedPatientIdText +
                    "'. Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(1000);
            }

            return null;
        }
        // ============================================================
        // READ SELECTED PATIENT FROM VISION PLANNER
        // ============================================================

        protected void ReadSelectedPatientFromVisionPlanner(
            string patientMrn,
            out string selectedPatientName,
            out string selectedPatientDateOfBirth)
        {
            selectedPatientName =
                string.Empty;

            selectedPatientDateOfBirth =
                string.Empty;

            Assert.That(
                string.IsNullOrWhiteSpace(patientMrn),
                Is.False,
                "Patient MRN is empty.");

            Window visionPlannerWindow =
                FindWindowByTitle(
                    VisionPlannerWindowTitle);

            Assert.That(
                visionPlannerWindow,
                Is.Not.Null,
                "Vision Planner window was not found.");

            string expectedPatientId =
                "Id: " +
                patientMrn.Trim();

            AutomationElement patientIdElement =
                WaitForPatientSearchResultByMrn(
                    patientMrn,
                    PatientSearchTimeoutMilliseconds);

            Assert.That(
                patientIdElement,
                Is.Not.Null,
                "Selected patient MRN was not found. Expected: '" +
                expectedPatientId +
                "'.");

            AutomationElement[] allElements =
                visionPlannerWindow.FindAllDescendants();

            foreach (AutomationElement element in allElements)
            {
                if (!IsElementVisible(element))
                {
                    continue;
                }

                string automationId =
                    GetAutomationIdSafely(
                        element);

                string elementName =
                    GetElementNameSafely(
                        element)
                    .Trim();

                /*
                 * Selected patient name:
                 *
                 * AutomationId = MarkedNameText
                 */
                if (string.Equals(
                        automationId,
                        CreatedPatientNameAutomationId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    selectedPatientName =
                        elementName;
                }

                /*
                 * Selected patient DOB:
                 *
                 * Vision Planner displays a Text element such as:
                 * 04/11/1988
                 */
                if (IsPatientDateText(
                        elementName))
                {
                    selectedPatientDateOfBirth =
                        elementName;
                }

                if (!string.IsNullOrWhiteSpace(
                        selectedPatientName) &&
                    !string.IsNullOrWhiteSpace(
                        selectedPatientDateOfBirth))
                {
                    break;
                }
            }

            Assert.That(
                string.IsNullOrWhiteSpace(
                    selectedPatientName),
                Is.False,
                "Selected patient name was not found in Vision Planner.");

            Assert.That(
                string.IsNullOrWhiteSpace(
                    selectedPatientDateOfBirth),
                Is.False,
                "Selected patient Date of Birth was not found " +
                "in Vision Planner.");

            TestContext.Progress.WriteLine(
                "Selected patient details read from Vision Planner.");

            TestContext.Progress.WriteLine(
                "Selected Patient MRN: " +
                patientMrn);

            TestContext.Progress.WriteLine(
                "Selected Patient Name: " +
                selectedPatientName);

            TestContext.Progress.WriteLine(
                "Selected Patient DOB: " +
                selectedPatientDateOfBirth);
        }


        // ============================================================
        // CHECK WHETHER TEXT IS A PATIENT DATE
        // ============================================================

        protected static bool IsPatientDateText(
            string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string[] acceptedFormats =
            {
        "dd/MM/yyyy",
        "d/M/yyyy",
        "dd/M/yyyy",
        "d/MM/yyyy",
        "MM/dd/yyyy",
        "M/d/yyyy",
        "MM/d/yyyy",
        "M/dd/yyyy"
    };

            DateTime parsedDate;

            return DateTime.TryParseExact(
                text.Trim(),
                acceptedFormats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out parsedDate);
        }


        // ============================================================
        // CLICK NEW IMAGE GUIDED MEASUREMENT
        // ============================================================

        protected void ClickNewImageGuidedMeasurementButton()
        {
            Window visionPlannerWindow =
                FindWindowByTitle(
                    VisionPlannerWindowTitle);

            Assert.That(
                visionPlannerWindow,
                Is.Not.Null,
                "Vision Planner window was not found before " +
                "clicking New Image Guided Measurement.");

            FocusWindowSafely(
                visionPlannerWindow,
                "Vision Planner");

            Thread.Sleep(500);

            AutomationElement measurementButton =
                null;

            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   CaptureMeasurementTimeoutMilliseconds)
            {
                try
                {
                    visionPlannerWindow =
                        FindWindowByTitle(
                            VisionPlannerWindowTitle);

                    if (visionPlannerWindow != null)
                    {
                        AutomationElement[] allElements =
                            visionPlannerWindow.FindAllDescendants();

                        foreach (AutomationElement element in allElements)
                        {
                            if (!IsElementVisible(element))
                            {
                                continue;
                            }

                            string elementName =
                                GetElementNameSafely(
                                    element);

                            string controlType =
                                GetControlTypeSafely(
                                    element);

                            bool correctName =
                                string.Equals(
                                    elementName.Trim(),
                                    NewImageGuidedMeasurementButtonName,
                                    StringComparison.OrdinalIgnoreCase);

                            bool isButton =
                                controlType.IndexOf(
                                    "Button",
                                    StringComparison.OrdinalIgnoreCase) >= 0;

                            if (correctName &&
                                isButton)
                            {
                                measurementButton =
                                    element;

                                break;
                            }
                        }
                    }

                    if (measurementButton != null)
                    {
                        break;
                    }
                }
                catch (Exception exception)
                {
                    TestContext.Progress.WriteLine(
                        "Measurement-button search warning: " +
                        exception.Message);
                }

                TestContext.Progress.WriteLine(
                    "Waiting for '" +
                    NewImageGuidedMeasurementButtonName +
                    "'. Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(1000);
            }

            Assert.That(
                measurementButton,
                Is.Not.Null,
                "New Image Guided Measurement button was not found.");

            Assert.That(
                measurementButton.IsEnabled,
                Is.True,
                "New Image Guided Measurement button is disabled.");

            LogElementDetails(
                "New Image Guided Measurement button",
                measurementButton);

            /*
             * Use the physical mouse click because this already works
             * reliably with the other Vision Planner controls.
             */
            ClickElementCenterWithMouse(
                measurementButton,
                "New Image Guided Measurement button");

            TestContext.Progress.WriteLine(
                "New Image Guided Measurement button clicked.");
        }

        // ============================================================
        // WAIT FOR MOVU MEASUREMENT SCREEN
        // ============================================================

        protected Window WaitForMovuMeasurementScreen()
        {
            TestContext.Progress.WriteLine(
                "Waiting for Vision Planner to transfer the " +
                "selected patient to MOVU.");

            /*
             * Allow Vision Planner to complete the handoff.
             */
            Thread.Sleep(5000);

            /*
             * MOVU is already running but is behind Vision Planner
             * or Visual Studio. Click the visible MOVU taskbar button.
             */
            ClickMovuTaskbarButton();

            Stopwatch stopwatch =
                Stopwatch.StartNew();

            Window readyMovuWindow =
                null;

            while (stopwatch.ElapsedMilliseconds <
                   CaptureMeasurementTimeoutMilliseconds)
            {
                try
                {
                    Window movuWindow =
                        FindWindowByTitle(
                            MovuWindowTitle);

                    if (movuWindow == null)
                    {
                        TestContext.Progress.WriteLine(
                            "MOVU window has not been found.");

                        Thread.Sleep(1000);

                        continue;
                    }

                    /*
                     * FlaUInspect shows the measurement screen under:
                     *
                     * AutomationId = Mainy
                     */
                    AutomationElement measurementControl =
                        movuWindow.FindFirstDescendant(
                            _automation.ConditionFactory
                                .ByAutomationId(
                                    MovuMeasurementControlAutomationId));

                    bool measurementControlFound =
                        measurementControl != null;

                    bool measurementControlVisible =
                        IsElementVisible(
                            measurementControl);

                    TestContext.Progress.WriteLine(
                        "MOVU Mainy control found: " +
                        measurementControlFound);

                    TestContext.Progress.WriteLine(
                        "MOVU Mainy control visible: " +
                        measurementControlVisible);

                    if (measurementControlFound &&
                        measurementControlVisible)
                    {
                        readyMovuWindow =
                            movuWindow;

                        break;
                    }
                }
                catch (Exception exception)
                {
                    TestContext.Progress.WriteLine(
                        "MOVU measurement-screen warning: " +
                        exception.Message);
                }

                TestContext.Progress.WriteLine(
                    "Waiting for MOVU measurement screen. " +
                    "Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(1000);
            }

            Assert.That(
                readyMovuWindow,
                Is.Not.Null,
                "MOVU measurement screen was not displayed after " +
                "clicking the MOVU taskbar button.");

            /*
             * Focus after the physical taskbar click.
             */
            FocusWindowSafely(
                readyMovuWindow,
                "MOVU");

            Thread.Sleep(1000);

            TestContext.Progress.WriteLine(
                "MOVU measurement screen is displayed.");

            return readyMovuWindow;
        }
        // ============================================================
        // VERIFY THE SELECTED PATIENT IS DISPLAYED IN MOVU
        // ============================================================

        protected void VerifyPatientDisplayedInMovu(
            Window movuWindow,
            string expectedPatientName,
            string visionPlannerDateOfBirth)
        {
            Assert.That(
                movuWindow,
                Is.Not.Null,
                "MOVU window is null.");

            Assert.That(
                string.IsNullOrWhiteSpace(
                    expectedPatientName),
                Is.False,
                "Expected patient name is empty.");

            Assert.That(
                string.IsNullOrWhiteSpace(
                    visionPlannerDateOfBirth),
                Is.False,
                "Expected patient Date of Birth is empty.");

            string alternateDateOfBirth =
                ReversePatientDate(
                    visionPlannerDateOfBirth);

            TestContext.Progress.WriteLine(
                "Starting MOVU patient verification.");

            TestContext.Progress.WriteLine(
                "Expected Patient Name: " +
                expectedPatientName);

            TestContext.Progress.WriteLine(
                "Expected DOB option 1: " +
                visionPlannerDateOfBirth);

            TestContext.Progress.WriteLine(
                "Expected DOB option 2: " +
                alternateDateOfBirth);

            string actualPatientName =
                string.Empty;

            string actualPatientDateOfBirth =
                string.Empty;

            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   CaptureMeasurementTimeoutMilliseconds)
            {
                try
                {
                    /*
                     * Re-find MOVU because its automation tree may be
                     * recreated when measurement mode opens.
                     */
                    movuWindow =
                        FindWindowByTitle(
                            MovuWindowTitle);

                    if (movuWindow != null)
                    {
                        AutomationElement[] movuElements =
                            movuWindow.FindAllDescendants();

                        foreach (AutomationElement element
                                 in movuElements)
                        {
                            string elementText =
                                GetElementNameSafely(
                                    element)
                                .Trim();

                            if (string.IsNullOrWhiteSpace(
                                    elementText))
                            {
                                continue;
                            }

                            /*
                             * Match the patient name.
                             */
                            if (string.Equals(
                                    elementText,
                                    expectedPatientName.Trim(),
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                actualPatientName =
                                    elementText;
                            }

                            /*
                             * Vision Planner and MOVU can display DOB
                             * with day and month reversed.
                             */
                            bool originalDateMatches =
                                string.Equals(
                                    elementText,
                                    visionPlannerDateOfBirth.Trim(),
                                    StringComparison.OrdinalIgnoreCase);

                            bool alternateDateMatches =
                                string.Equals(
                                    elementText,
                                    alternateDateOfBirth.Trim(),
                                    StringComparison.OrdinalIgnoreCase);

                            if (originalDateMatches ||
                                alternateDateMatches)
                            {
                                actualPatientDateOfBirth =
                                    elementText;
                            }
                        }
                    }

                    bool patientNameFound =
                        !string.IsNullOrWhiteSpace(
                            actualPatientName);

                    bool patientDateOfBirthFound =
                        !string.IsNullOrWhiteSpace(
                            actualPatientDateOfBirth);

                    TestContext.Progress.WriteLine(
                        "MOVU verification status. Name found: " +
                        patientNameFound +
                        ", DOB found: " +
                        patientDateOfBirthFound);

                    if (patientNameFound &&
                        patientDateOfBirthFound)
                    {
                        break;
                    }
                }
                catch (Exception exception)
                {
                    TestContext.Progress.WriteLine(
                        "MOVU patient verification warning: " +
                        exception.Message);
                }

                Thread.Sleep(1000);
            }

            Assert.That(
                string.IsNullOrWhiteSpace(
                    actualPatientName),
                Is.False,
                "Selected patient name was not displayed in MOVU. " +
                "Expected Patient Name: '" +
                expectedPatientName +
                "'.");

            Assert.That(
                actualPatientName,
                Is.EqualTo(expectedPatientName)
                    .IgnoreCase,
                "MOVU displays a different patient than the " +
                "patient selected in Vision Planner.");

            Assert.That(
                string.IsNullOrWhiteSpace(
                    actualPatientDateOfBirth),
                Is.False,
                "Selected patient Date of Birth was not displayed " +
                "in MOVU. Expected either '" +
                visionPlannerDateOfBirth +
                "' or '" +
                alternateDateOfBirth +
                "'.");

            TestContext.Progress.WriteLine(
                "================================================");

            TestContext.Progress.WriteLine(
                "SELECTED PATIENT VERIFIED IN MOVU.");

            TestContext.Progress.WriteLine(
                "Expected Patient Name: " +
                expectedPatientName);

            TestContext.Progress.WriteLine(
                "Actual MOVU Patient Name: " +
                actualPatientName);

            TestContext.Progress.WriteLine(
                "Vision Planner DOB: " +
                visionPlannerDateOfBirth);

            TestContext.Progress.WriteLine(
                "Actual MOVU DOB: " +
                actualPatientDateOfBirth);

            TestContext.Progress.WriteLine(
                "================================================");
        }


        // ============================================================
        // REVERSE DATE DAY AND MONTH
        // ============================================================

        protected static string ReversePatientDate(
            string dateOfBirth)
        {
            if (string.IsNullOrWhiteSpace(dateOfBirth))
            {
                return string.Empty;
            }

            string[] dateParts =
                dateOfBirth
                    .Trim()
                    .Split('/');

            if (dateParts.Length != 3)
            {
                return dateOfBirth.Trim();
            }

            string firstPart =
                dateParts[0]
                    .PadLeft(
                        2,
                        '0');

            string secondPart =
                dateParts[1]
                    .PadLeft(
                        2,
                        '0');

            string yearPart =
                dateParts[2];

            return secondPart +
                   "/" +
                   firstPart +
                   "/" +
                   yearPart;
        }

        // ============================================================
        // CLICK MOVU BUTTON ON WINDOWS TASKBAR
        // ============================================================

        protected void ClickMovuTaskbarButton()
        {
            TestContext.Progress.WriteLine(
                "Searching for MOVU button on the Windows taskbar.");

            AutomationElement movuTaskbarButton =
                null;

            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   MovuTaskbarSearchTimeoutMilliseconds)
            {
                try
                {
                    AutomationElement desktop =
                        _automation.GetDesktop();

                    AutomationElement[] allDesktopElements =
                        desktop.FindAllDescendants();

                    foreach (AutomationElement element
                             in allDesktopElements)
                    {
                        string controlType =
                            GetControlTypeSafely(
                                element);

                        /*
                         * Ignore every element that is not a button.
                         */
                        if (controlType.IndexOf(
                                "Button",
                                StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        string elementName =
                            GetElementNameSafely(
                                element);

                        string automationId =
                            GetAutomationIdSafely(
                                element);

                        bool nameMatches =
                            ContainsIgnoreCase(
                                elementName,
                                MovuTaskbarButtonNamePartialText);

                        bool automationIdMatches =
                            ContainsIgnoreCase(
                                automationId,
                                MovuTaskbarAutomationIdPartialText);

                        if (!nameMatches &&
                            !automationIdMatches)
                        {
                            continue;
                        }

                        if (!IsElementVisible(element))
                        {
                            continue;
                        }

                        movuTaskbarButton =
                            element;

                        TestContext.Progress.WriteLine(
                            "MOVU taskbar button found.");

                        TestContext.Progress.WriteLine(
                            "Taskbar button Name: " +
                            elementName);

                        TestContext.Progress.WriteLine(
                            "Taskbar button AutomationId: " +
                            automationId);

                        break;
                    }

                    if (movuTaskbarButton != null)
                    {
                        break;
                    }
                }
                catch (Exception exception)
                {
                    TestContext.Progress.WriteLine(
                        "MOVU taskbar search warning: " +
                        exception.Message);
                }

                TestContext.Progress.WriteLine(
                    "Waiting for MOVU taskbar button. " +
                    "Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(1000);
            }

            Assert.That(
                movuTaskbarButton,
                Is.Not.Null,
                "MOVU taskbar button was not found. " +
                "Expected the taskbar button Name to contain '" +
                MovuTaskbarButtonNamePartialText +
                "' or AutomationId to contain '" +
                MovuTaskbarAutomationIdPartialText +
                "'.");

            Assert.That(
                movuTaskbarButton.IsEnabled,
                Is.True,
                "MOVU taskbar button was found, but it is disabled.");

            var rectangle =
                movuTaskbarButton.BoundingRectangle;

            Assert.That(
                rectangle.Left,
                Is.GreaterThanOrEqualTo(0),
                "MOVU taskbar button has an invalid left coordinate.");

            Assert.That(
                rectangle.Top,
                Is.GreaterThanOrEqualTo(0),
                "MOVU taskbar button has an invalid top coordinate.");

            Assert.That(
                rectangle.Width,
                Is.GreaterThan(0),
                "MOVU taskbar button has an invalid width.");

            Assert.That(
                rectangle.Height,
                Is.GreaterThan(0),
                "MOVU taskbar button has an invalid height.");

            var centerPoint =
                rectangle.Center();

            TestContext.Progress.WriteLine(
                "Clicking MOVU taskbar button at X=" +
                centerPoint.X +
                ", Y=" +
                centerPoint.Y);

            Mouse.MoveTo(
                centerPoint);

            Thread.Sleep(500);

            Mouse.Click(
                MouseButton.Left);

            Thread.Sleep(3000);

            TestContext.Progress.WriteLine(
                "MOVU taskbar button clicked.");
        }
        // ============================================================
        // CAPTURE ONE MEASUREMENT FOR OD AND ONE MEASUREMENT FOR OS
        // ============================================================

        protected void CaptureMeasurementsForBothEyes()
        {
            TestContext.Progress.WriteLine(
                "================================================");

            TestContext.Progress.WriteLine(
                "Starting MOVU measurement capture.");

            TestContext.Progress.WriteLine(
                "Required capture: one OD measurement and one OS measurement.");

            TestContext.Progress.WriteLine(
                "================================================");

            Window movuWindow =
                FindWindowByTitle(
                    MovuWindowTitle);

            Assert.That(
                movuWindow,
                Is.Not.Null,
                "MOVU window was not found before measurement capture.");

            FocusWindowSafely(
                movuWindow,
                "MOVU");

            Thread.Sleep(1000);

            /*
             * OD sequence:
             *
             * 1. Click OD once.
             * 2. Click Joystick once.
             * 3. Click Accept once.
             * 4. MOVU automatically shifts to OS.
             */
            CaptureSingleOdMeasurement();

            /*
             * OS sequence:
             *
             * 1. Do not click OS.
             * 2. Click Joystick twice.
             * 3. Click Accept once.
             */
            CaptureSingleOsMeasurement();


            TestContext.Progress.WriteLine(
                "================================================");

            TestContext.Progress.WriteLine(
                "MOVU MEASUREMENT CAPTURE COMPLETED.");

            TestContext.Progress.WriteLine(
                "OD measurements completed: 1");

            TestContext.Progress.WriteLine(
                "OS measurements completed: 1");

            TestContext.Progress.WriteLine(
                "================================================");
        }

        // ============================================================
        // CAPTURE ONE OD MEASUREMENT
        // ============================================================

        protected void CaptureSingleOdMeasurement()
        {
            TestContext.Progress.WriteLine(
                "------------------------------------------------");

            TestContext.Progress.WriteLine(
                "Starting OD measurement.");

            TestContext.Progress.WriteLine(
                "------------------------------------------------");

            Window movuWindow =
                FindWindowByTitle(
                    MovuWindowTitle);

            Assert.That(
                movuWindow,
                Is.Not.Null,
                "MOVU window was not found before OD measurement.");

            FocusWindowSafely(
                movuWindow,
                "MOVU");

            Thread.Sleep(500);

            // ========================================================
            // STEP 1: CLICK OD ONCE
            // ========================================================

            ClickMovuButtonByAutomationId(
                MovuOdButtonAutomationId,
                "OD button");

            TestContext.Progress.WriteLine(
                "OD button clicked once.");

            Thread.Sleep(700);

            // ========================================================
            // STEP 2: CLICK JOYSTICK ONCE
            // ========================================================

            ClickMovuButtonByAutomationId(
                MovuJoystickButtonAutomationId,
                "Joystick button for OD");

            TestContext.Progress.WriteLine(
                "Joystick clicked once for OD.");

            /*
             * Wait for MOVU to generate the OD measurement result.
             */
            Thread.Sleep(
                MovuMeasurementProcessingWaitMilliseconds);

            // ========================================================
            // STEP 3: CLICK ACCEPT ONCE
            // ========================================================

            ClickMovuAcceptButton(
                "OD",
                1);

            Thread.Sleep(
                MovuAcceptProcessingWaitMilliseconds);

            TestContext.Progress.WriteLine(
                "OD measurement accepted.");

            TestContext.Progress.WriteLine(
                "MOVU should now automatically shift to OS.");

            /*
             * Give MOVU time to switch from OD to OS.
             */
            Thread.Sleep(1500);

            TestContext.Progress.WriteLine(
                "------------------------------------------------");
        }
        // ============================================================
        // CAPTURE ONE OS MEASUREMENT
        // ============================================================

        protected void CaptureSingleOsMeasurement()
        {
            TestContext.Progress.WriteLine(
                "------------------------------------------------");

            TestContext.Progress.WriteLine(
                "Starting OS measurement.");

            TestContext.Progress.WriteLine(
                "MOVU should already be on the OS tab.");

            TestContext.Progress.WriteLine(
                "------------------------------------------------");

            Window movuWindow =
                FindWindowByTitle(
                    MovuWindowTitle);

            Assert.That(
                movuWindow,
                Is.Not.Null,
                "MOVU window was not found before OS measurement.");

            FocusWindowSafely(
                movuWindow,
                "MOVU");

            Thread.Sleep(500);

            /*
             * Do not click the OD button.
             * Do not click the OS button.
             *
             * MOVU automatically moved to OS after accepting OD.
             */

            // ========================================================
            // STEP 1: CLICK JOYSTICK FIRST TIME
            // ========================================================

            ClickMovuButtonByAutomationId(
                MovuJoystickButtonAutomationId,
                "Joystick button for OS, first click");

            TestContext.Progress.WriteLine(
                "Joystick first click completed for OS.");

            Thread.Sleep(700);

            // ========================================================
            // STEP 2: CLICK JOYSTICK SECOND TIME
            // ========================================================

            /*
             * ClickMovuButtonByAutomationId re-finds the Joystick
             * element, which avoids using a stale WPF element after
             * the first click.
             */
            ClickMovuButtonByAutomationId(
                MovuJoystickButtonAutomationId,
                "Joystick button for OS, second click");

            TestContext.Progress.WriteLine(
                "Joystick second click completed for OS.");

            /*
             * Wait for MOVU to generate the OS measurement result.
             */
            Thread.Sleep(
                MovuMeasurementProcessingWaitMilliseconds);

            // ========================================================
            // STEP 3: CLICK ACCEPT ONCE
            // ========================================================

            ClickMovuAcceptButton(
                "OS",
                1);

            Thread.Sleep(
                MovuAcceptProcessingWaitMilliseconds);

            TestContext.Progress.WriteLine(
                "OS measurement accepted.");

            TestContext.Progress.WriteLine(
                "------------------------------------------------");
        }

        // ============================================================
        // CLICK MOVU BUTTON BY AUTOMATION ID
        // ============================================================

        protected void ClickMovuButtonByAutomationId(
            string automationId,
            string buttonDescription)
        {
            Assert.That(
                string.IsNullOrWhiteSpace(
                    automationId),
                Is.False,
                buttonDescription +
                " AutomationId is empty.");

            Window movuWindow =
                FindWindowByTitle(
                    MovuWindowTitle);

            Assert.That(
                movuWindow,
                Is.Not.Null,
                "MOVU window was not found while locating " +
                buttonDescription +
                ".");

            AutomationElement button =
                WaitForMovuElementByAutomationId(
                    automationId,
                    MovuCaptureControlTimeoutMilliseconds);

            Assert.That(
                button,
                Is.Not.Null,
                buttonDescription +
                " was not found. AutomationId: '" +
                automationId +
                "'.");

            Assert.That(
                button.IsEnabled,
                Is.True,
                buttonDescription +
                " was found, but it is disabled.");

            Assert.That(
                button.IsOffscreen,
                Is.False,
                buttonDescription +
                " was found, but it is offscreen.");

            LogElementDetails(
                buttonDescription,
                button);

            /*
             * The screenshots show that OD, OS, and Joystick are parent
             * Button elements. Physical clicking is used because the
             * child Text elements do not expose InvokePattern.
             */
            ClickElementCenterWithMouse(
                button,
                buttonDescription);

            TestContext.Progress.WriteLine(
                buttonDescription +
                " clicked successfully.");
        }


        // ============================================================
        // WAIT FOR MOVU ELEMENT BY AUTOMATION ID
        // ============================================================

        protected AutomationElement WaitForMovuElementByAutomationId(
            string automationId,
            int timeoutMilliseconds)
        {
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   timeoutMilliseconds)
            {
                try
                {
                    Window movuWindow =
                        FindWindowByTitle(
                            MovuWindowTitle);

                    if (movuWindow != null)
                    {
                        AutomationElement element =
                            movuWindow.FindFirstDescendant(
                                _automation.ConditionFactory
                                    .ByAutomationId(
                                        automationId));

                        if (element != null &&
                            IsElementVisible(element))
                        {
                            return element;
                        }
                    }
                }
                catch (Exception exception)
                {
                    TestContext.Progress.WriteLine(
                        "MOVU element search warning for '" +
                        automationId +
                        "': " +
                        exception.Message);
                }

                TestContext.Progress.WriteLine(
                    "Waiting for MOVU AutomationId '" +
                    automationId +
                    "'. Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(500);
            }

            return null;
        }


        // ============================================================
        // CLICK MOVU ACCEPT BUTTON
        // ============================================================

        protected void ClickMovuAcceptButton(
            string eyeDescription,
            int measurementNumber)
        {
            TestContext.Progress.WriteLine(
                "Waiting for Accept button after " +
                eyeDescription +
                " measurement " +
                measurementNumber +
                ".");

            AutomationElement acceptButton =
                WaitForMovuElementByAutomationId(
                    MovuAcceptButtonAutomationId,
                    MovuCaptureControlTimeoutMilliseconds);

            Assert.That(
                acceptButton,
                Is.Not.Null,
                "Accept button was not found after " +
                eyeDescription +
                " measurement " +
                measurementNumber +
                ". Expected AutomationId: '" +
                MovuAcceptButtonAutomationId +
                "'.");

            Assert.That(
                acceptButton.IsEnabled,
                Is.True,
                "Accept button is disabled after " +
                eyeDescription +
                " measurement " +
                measurementNumber +
                ".");

            LogElementDetails(
                "Accept button",
                acceptButton);

            ClickElementCenterWithMouse(
                acceptButton,
                "Accept button");

            TestContext.Progress.WriteLine(
                "Accept button clicked for " +
                eyeDescription +
                " measurement " +
                measurementNumber +
                ".");
        }

        // ============================================================
        // CLICK MOVU FINISH BUTTON
        // ============================================================

        protected void ClickMovuFinishButton()
        {
            TestContext.Progress.WriteLine(
                "Waiting for the MOVU Finish button.");

            Window movuWindow =
                FindWindowByTitle(
                    MovuWindowTitle);

            Assert.That(
                movuWindow,
                Is.Not.Null,
                "MOVU window was not found before clicking Finish.");

            FocusWindowSafely(
                movuWindow,
                "MOVU");

            Thread.Sleep(500);

            AutomationElement finishButton =
                WaitForMovuElementByAutomationId(
                    MovuFinishButtonAutomationId,
                    MovuCaptureControlTimeoutMilliseconds);

            Assert.That(
                finishButton,
                Is.Not.Null,
                "Finish button was not found. " +
                "Expected AutomationId: '" +
                MovuFinishButtonAutomationId +
                "'.");

            Assert.That(
                finishButton.IsEnabled,
                Is.True,
                "Finish button was found, but it is disabled. " +
                "Confirm that the OD and OS measurements were accepted.");

            Assert.That(
                finishButton.IsOffscreen,
                Is.False,
                "Finish button was found, but it is offscreen.");

            LogElementDetails(
                "Finish button",
                finishButton);

            ClickElementCenterWithMouse(
                finishButton,
                "Finish button");

            TestContext.Progress.WriteLine(
                "Finish button clicked successfully.");

            /*
             * Wait only until the confirmation popup appears.
             * Do not click Yes inside this method.
             */
            AutomationElement yesButton =
                WaitForFinishConfirmationYesButton(
                    MovuFinishConfirmationTimeoutMilliseconds);

            Assert.That(
                yesButton,
                Is.Not.Null,
                "Finish was clicked, but the confirmation popup " +
                "was not displayed. Expected Yes button AutomationId: '" +
                MovuFinishConfirmationYesAutomationId +
                "'.");

            TestContext.Progress.WriteLine(
                "Finish confirmation popup displayed successfully.");
        }

        // ============================================================
        // CLICK YES ON MOVU FINISH CONFIRMATION
        // ============================================================

        protected void ClickMovuFinishConfirmationYesButton()
        {
            TestContext.Progress.WriteLine(
                "Waiting for the Finish confirmation popup.");

            AutomationElement yesButton =
                WaitForFinishConfirmationYesButton(
                    MovuFinishConfirmationTimeoutMilliseconds);

            Assert.That(
                yesButton,
                Is.Not.Null,
                "The Finish confirmation Yes button was not found. " +
                "Expected AutomationId: '" +
                MovuFinishConfirmationYesAutomationId +
                "'.");

            Assert.That(
                yesButton.IsEnabled,
                Is.True,
                "The Finish confirmation Yes button is disabled.");

            Assert.That(
                yesButton.IsOffscreen,
                Is.False,
                "The Finish confirmation Yes button is offscreen.");

            LogElementDetails(
                "Finish confirmation Yes button",
                yesButton);

            ClickElementCenterWithMouse(
                yesButton,
                "Finish confirmation Yes button");

            TestContext.Progress.WriteLine(
                "Yes clicked on the Finish confirmation popup.");

            bool confirmationClosed =
                WaitForFinishConfirmationToClose(
                    MovuFinishConfirmationTimeoutMilliseconds);

            Assert.That(
                confirmationClosed,
                Is.True,
                "The Finish confirmation popup remained visible after " +
                "clicking Yes.");

            TestContext.Progress.WriteLine(
                "Finish confirmation popup closed successfully.");
        }
        // ============================================================
        // WAIT FOR FINISH CONFIRMATION YES BUTTON
        // ============================================================

        protected AutomationElement WaitForFinishConfirmationYesButton(
            int timeoutMilliseconds)
        {
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   timeoutMilliseconds)
            {
                try
                {
                    /*
                     * The confirmation popup is displayed under the MOVU
                     * application. Search the complete desktop because modal
                     * windows may not be returned as descendants of the
                     * original MOVU window.
                     */
                    AutomationElement desktop =
                        _automation.GetDesktop();

                    AutomationElement[] allElements =
                        desktop.FindAllDescendants();

                    foreach (AutomationElement element in allElements)
                    {
                        string automationId =
                            GetAutomationIdSafely(element);

                        if (!string.Equals(
                                automationId,
                                MovuFinishConfirmationYesAutomationId,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string controlType =
                            GetControlTypeSafely(element);

                        if (controlType.IndexOf(
                                "Button",
                                StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        if (!IsElementVisible(element))
                        {
                            continue;
                        }

                        TestContext.Progress.WriteLine(
                            "Finish confirmation Yes button detected.");

                        TestContext.Progress.WriteLine(
                            "Yes button AutomationId: " +
                            automationId);

                        return element;
                    }
                }
                catch (Exception exception)
                {
                    TestContext.Progress.WriteLine(
                        "Finish confirmation search warning: " +
                        exception.Message);
                }

                TestContext.Progress.WriteLine(
                    "Waiting for Finish confirmation Yes button. " +
                    "Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(500);
            }

            return null;
        }
        // ============================================================
        // WAIT FOR FINISH CONFIRMATION TO CLOSE
        // ============================================================

        protected bool WaitForFinishConfirmationToClose(
            int timeoutMilliseconds)
        {
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   timeoutMilliseconds)
            {
                try
                {
                    AutomationElement yesButton =
                        FindFinishConfirmationYesButton();

                    /*
                     * WPF can keep an old control in the automation tree
                     * after the popup closes.
                     *
                     * The confirmation is considered closed when:
                     *
                     * 1. The Yes button no longer exists, or
                     * 2. The Yes button exists but is no longer visible.
                     */
                    if (yesButton == null ||
                        !IsElementVisible(yesButton))
                    {
                        TestContext.Progress.WriteLine(
                            "Finish confirmation popup is no longer visible.");

                        return true;
                    }
                }
                catch (Exception exception)
                {
                    /*
                     * A stale or unavailable Yes button normally means
                     * the confirmation popup has closed.
                     */
                    TestContext.Progress.WriteLine(
                        "Finish confirmation popup is no longer readable: " +
                        exception.Message);

                    return true;
                }

                TestContext.Progress.WriteLine(
                    "Waiting for Finish confirmation popup to close. " +
                    "Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(500);
            }

            TestContext.Progress.WriteLine(
                "Finish confirmation popup did not close within the timeout.");

            return false;
        }
        // ============================================================
        // FIND FINISH CONFIRMATION YES BUTTON
        // ============================================================

        protected AutomationElement FindFinishConfirmationYesButton()
        {
            try
            {
                AutomationElement desktop =
                    _automation.GetDesktop();

                AutomationElement[] allElements =
                    desktop.FindAllDescendants();

                foreach (AutomationElement element in allElements)
                {
                    string automationId =
                        GetAutomationIdSafely(element);

                    if (!string.Equals(
                            automationId,
                            MovuFinishConfirmationYesAutomationId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string controlType =
                        GetControlTypeSafely(element);

                    if (controlType.IndexOf(
                            "Button",
                            StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    if (!IsElementVisible(element))
                    {
                        continue;
                    }

                    return element;
                }
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "Confirmation Yes button search warning: " +
                    exception.Message);
            }

            return null;
        }
        // ============================================================
        // MEASUREMENT EXPORT VERIFICATION
        // ============================================================

       
        protected const int MeasurementExportTimeoutMilliseconds =
            180000;

        protected const int RequiredDxIconCount =
            2;

        protected const int MeasurementExportPollingMilliseconds =
            1000;
        // ============================================================
        // MEASUREMENT EXPORT VERIFICATION
        // ============================================================

        protected const string MeasurementExportDetailsButtonAutomationId =
            "DiagnosticAvsDnaExportDetailsBtn";

        protected const int RequiredExportDetailsButtonCount =
            2;

        // ============================================================
        // VERIFY MEASUREMENTS EXPORTED SUCCESSFULLY
        // ============================================================

        // ============================================================
        // VERIFY MEASUREMENTS EXPORTED SUCCESSFULLY
        // ============================================================

        protected void VerifyMeasurementsExportedSuccessfully()
        {
            TestContext.Progress.WriteLine(
                "================================================");

            TestContext.Progress.WriteLine(
                "Starting Vision Planner measurement export verification.");

            TestContext.Progress.WriteLine(
                "Expected export-success buttons: " +
                RequiredExportDetailsButtonCount);

            TestContext.Progress.WriteLine(
                "Expected AutomationId: " +
                MeasurementExportDetailsButtonAutomationId);

            TestContext.Progress.WriteLine(
                "================================================");

            Window visionPlannerWindow =
                WaitForWindowByTitle(
                    VisionPlannerWindowTitle,
                    MeasurementExportTimeoutMilliseconds);

            Assert.That(
                visionPlannerWindow,
                Is.Not.Null,
                "Vision Planner window was not displayed after " +
                "confirming Finish.");

            FocusWindowSafely(
                visionPlannerWindow,
                "Vision Planner");

            bool exportSucceeded =
                WaitForMeasurementExportSuccess(
                    RequiredExportDetailsButtonCount,
                    MeasurementExportTimeoutMilliseconds);

            Assert.That(
                exportSucceeded,
                Is.True,
                "Measurement export was not confirmed. " +
                "Expected two visible buttons with AutomationId '" +
                MeasurementExportDetailsButtonAutomationId +
                "', one for OD and one for OS.");

            TestContext.Progress.WriteLine(
                "================================================");

            TestContext.Progress.WriteLine(
                "MEASUREMENT EXPORT VERIFICATION PASSED.");

            TestContext.Progress.WriteLine(
                "Export-success controls detected for OD and OS.");

            TestContext.Progress.WriteLine(
                "================================================");
        }
        // ============================================================
        // WAIT FOR MEASUREMENT EXPORT SUCCESS
        // ============================================================

        protected bool WaitForMeasurementExportSuccess(
            int requiredButtonCount,
            int timeoutMilliseconds)
        {
            Assert.That(
                requiredButtonCount,
                Is.GreaterThan(0),
                "Required export-success button count must be greater " +
                "than zero.");

            Stopwatch stopwatch =
                Stopwatch.StartNew();

            int previousDetectedCount =
                -1;

            while (stopwatch.ElapsedMilliseconds <
                   timeoutMilliseconds)
            {
                try
                {
                    Window visionPlannerWindow =
                        FindWindowByTitle(
                            VisionPlannerWindowTitle);

                    if (visionPlannerWindow == null)
                    {
                        TestContext.Progress.WriteLine(
                            "Vision Planner window is temporarily unavailable.");

                        Thread.Sleep(
                            MeasurementExportPollingMilliseconds);

                        continue;
                    }

                    int detectedCount =
                        CountVisibleMeasurementExportButtons(
                            visionPlannerWindow);

                    if (detectedCount != previousDetectedCount)
                    {
                        TestContext.Progress.WriteLine(
                            "Visible export-success buttons detected: " +
                            detectedCount +
                            " of " +
                            requiredButtonCount);

                        previousDetectedCount =
                            detectedCount;
                    }

                    if (detectedCount >= requiredButtonCount)
                    {
                        TestContext.Progress.WriteLine(
                            "Required export-success buttons detected.");

                        return true;
                    }
                }
                catch (Exception exception)
                {
                    TestContext.Progress.WriteLine(
                        "Measurement export verification warning: " +
                        exception.Message);
                }

                TestContext.Progress.WriteLine(
                    "Waiting for measurement export to complete. " +
                    "Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(
                    MeasurementExportPollingMilliseconds);
            }

            TestContext.Progress.WriteLine(
                "Measurement export was not confirmed within the timeout.");

            return false;
        }
        // ============================================================
        // COUNT VISIBLE MEASUREMENT EXPORT BUTTONS
        // ============================================================

        protected int CountVisibleMeasurementExportButtons(
            Window visionPlannerWindow)
        {
            if (visionPlannerWindow == null)
            {
                return 0;
            }

            int matchingButtonCount =
                0;

            try
            {
                AutomationElement[] allElements =
                    visionPlannerWindow.FindAllDescendants();

                foreach (AutomationElement element in allElements)
                {
                    string automationId =
                        GetAutomationIdSafely(
                            element);

                    if (!string.Equals(
                            automationId,
                            MeasurementExportDetailsButtonAutomationId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string controlType =
                        GetControlTypeSafely(
                            element);

                    if (controlType.IndexOf(
                            "Button",
                            StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    if (!IsElementVisible(element))
                    {
                        continue;
                    }

                    matchingButtonCount++;

                    TestContext.Progress.WriteLine(
                        "Export-success button " +
                        matchingButtonCount +
                        " found.");

                    TestContext.Progress.WriteLine(
                        "AutomationId: " +
                        automationId);

                    try
                    {
                        var rectangle =
                            element.BoundingRectangle;

                        TestContext.Progress.WriteLine(
                            "Bounds: Left=" +
                            rectangle.Left +
                            ", Top=" +
                            rectangle.Top +
                            ", Width=" +
                            rectangle.Width +
                            ", Height=" +
                            rectangle.Height);
                    }
                    catch (Exception boundsException)
                    {
                        TestContext.Progress.WriteLine(
                            "Unable to read export button bounds: " +
                            boundsException.Message);
                    }
                }
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "Export-success button search warning: " +
                    exception.Message);

                return 0;
            }

            return matchingButtonCount;
        }

        // ============================================================
        // PATIENT CREATION EXCEL TEST CASES
        // ============================================================

        public static IEnumerable<TestCaseData>
            PatientCreationTestCases()
        {
            List<PatientCreationData> rows =
                ExcelConfigReader
                    .LoadAllPatientCreationRows();

            foreach (PatientCreationData row in rows)
            {
                yield return
                    new TestCaseData(row)
                        .SetName(
                            "PatientCreation_Row_" +
                            row.ExcelRowNumber +
                            "_" +
                            row.PatientID);
            }
        }

        // ============================================================
        // CAPTURE-MEASUREMENT EXCEL TEST CASES
        // ============================================================

        public static IEnumerable<TestCaseData>
            CaptureMeasurementTestCases()
        {
            List<CaptureMeasurementData> rows =
                ExcelConfigReader
                    .LoadAllCaptureMeasurementRows();

            foreach (CaptureMeasurementData row in rows)
            {
                yield return
                    new TestCaseData(row)
                        .SetName(
                            "CaptureMeasurement_Row_" +
                            row.ExcelRowNumber +
                            "_" +
                            row.PatientMrn);
            }
        }
        // ============================================================
        // CLICK BACK TO PATIENT LIST
        // ============================================================

        protected void ClickBackToPatientListButton()
        {
            TestContext.Progress.WriteLine(
                "Waiting for the Back to Patient List button.");

            Window visionPlannerWindow =
                FindWindowByTitle(
                    VisionPlannerWindowTitle);

            Assert.That(
                visionPlannerWindow,
                Is.Not.Null,
                "Vision Planner window was not found before clicking Back.");

            FocusWindowSafely(
                visionPlannerWindow,
                "Vision Planner");

            Thread.Sleep(500);

            AutomationElement backButton =
                WaitForVisibleElementByAutomationId(
                    VisionPlannerWindowTitle,
                    BackToPatientListAutomationId,
                    BackToPatientListTimeoutMilliseconds);

            Assert.That(
                backButton,
                Is.Not.Null,
                "Back to Patient List button was not found. " +
                "Expected AutomationId: '" +
                BackToPatientListAutomationId +
                "'.");

            Assert.That(
                backButton.IsEnabled,
                Is.True,
                "Back to Patient List button was found, but it is disabled.");

            Assert.That(
                backButton.IsOffscreen,
                Is.False,
                "Back to Patient List button was found, but it is offscreen.");

            LogElementDetails(
                "Back to Patient List button",
                backButton);

            ClickElementCenterWithMouse(
                backButton,
                "Back to Patient List button");

            TestContext.Progress.WriteLine(
                "Back to Patient List button clicked.");

            bool patientListDisplayed =
                WaitForPatientListScreen(
                    BackToPatientListTimeoutMilliseconds);

            Assert.That(
                patientListDisplayed,
                Is.True,
                "Back was clicked, but the Patient List screen was not " +
                "confirmed.");

            TestContext.Progress.WriteLine(
                "Patient List screen displayed successfully.");
        }
        // ============================================================
        // WAIT FOR PATIENT LIST SCREEN
        // ============================================================

        protected bool WaitForPatientListScreen(
            int timeoutMilliseconds)
        {
            Stopwatch stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds <
                   timeoutMilliseconds)
            {
                try
                {
                    Window visionPlannerWindow =
                        FindWindowByTitle(
                            VisionPlannerWindowTitle);

                    if (visionPlannerWindow == null)
                    {
                        Thread.Sleep(500);

                        continue;
                    }

                    /*
                     * The patient-list screen exposes the patient-search
                     * field with AutomationId "Search Item".
                     */
                    AutomationElement searchField =
                        visionPlannerWindow.FindFirstDescendant(
                            _automation.ConditionFactory
                                .ByAutomationId(
                                    PatientSearchTextBoxAutomationId));

                    if (searchField != null &&
                        IsElementVisible(searchField))
                    {
                        TestContext.Progress.WriteLine(
                            "Patient search field detected.");

                        return true;
                    }

                    /*
                     * Fallback verification:
                     * the Back button should no longer be visible.
                     */
                    AutomationElement backButton =
                        visionPlannerWindow.FindFirstDescendant(
                            _automation.ConditionFactory
                                .ByAutomationId(
                                    BackToPatientListAutomationId));

                    if (backButton == null ||
                        !IsElementVisible(backButton))
                    {
                        TestContext.Progress.WriteLine(
                            "Back button is no longer visible. " +
                            "Patient-list navigation confirmed.");

                        return true;
                    }
                }
                catch (Exception exception)
                {
                    TestContext.Progress.WriteLine(
                        "Patient-list verification warning: " +
                        exception.Message);
                }

                TestContext.Progress.WriteLine(
                    "Waiting for Patient List screen. " +
                    "Elapsed seconds: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("0"));

                Thread.Sleep(500);
            }

            return false;
        }
        // ============================================================
        // ONE-TIME CLEANUP
        // ============================================================

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_automation != null)
            {
                _automation.Dispose();

                _automation = null;

                TestContext.Progress.WriteLine(
                    "UIA3Automation disposed.");
            }
        }
    }
}