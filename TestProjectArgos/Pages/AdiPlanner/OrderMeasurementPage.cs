using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SimulatorAutomation.Tests.Framework;
using SeleniumKeys = OpenQA.Selenium.Keys;

namespace SimulatorAutomation.Tests.Pages.AdiPlanner
{
    public class OrderMeasurementPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        private readonly By MeasurementTab = By.XPath(
            "//div[@role='menuitem' and @title='Measurement']//measurement-icon");

        private readonly By OrderMeasurementButton = By.CssSelector(
            "[data-testid='ordermeasurementButton']");

        private readonly By OrderPanel = By.CssSelector(
            "[data-testid='order-measurement-panel']");

        private readonly By SidePanelContents = By.CssSelector(
            "[data-testid='sidepanel-contents']");

        private readonly By DeviceDropdown = By.CssSelector(
            "[data-testid='order-measurement-device']");

        private readonly By DateHost = By.CssSelector(
            "[data-testid='order-measurement-date']");

        private readonly By TimeHost = By.CssSelector(
            "adi-time-input[name='scheduleTime']");

        private readonly By SubmitButton = By.CssSelector(
            "[data-testid='place-order-button']");

        private readonly By SuccessToast = By.XPath(
            "//*[contains(translate(normalize-space()," +
            "'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'success')" +
            " or contains(translate(normalize-space()," +
            "'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'order created')" +
            " or contains(translate(normalize-space()," +
            "'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'order placed')]");

        public OrderMeasurementPage(IWebDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            _wait.IgnoreExceptionTypes(
                typeof(NoSuchElementException),
                typeof(StaleElementReferenceException),
                typeof(ElementNotInteractableException));
        }

        public void OrderMeasurement(OrderMeasurementData order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (string.IsNullOrWhiteSpace(order.Mrn))
                throw new ArgumentException("Order measurement MRN is empty.", nameof(order));

            if (string.IsNullOrWhiteSpace(order.Device))
                throw new ArgumentException("Order measurement device is empty.", nameof(order));

            if (string.IsNullOrWhiteSpace(order.ScheduledDate))
                throw new ArgumentException("Order measurement schedule date is empty.", nameof(order));

            TestContext.Progress.WriteLine("Ordering measurement for MRN: " + order.Mrn);

            OpenMeasurementTab();
            ClickOrderMeasurement();
            SelectDevice(order.Device);
            SetScheduleDate(order.ScheduledDate);

            if (!string.IsNullOrWhiteSpace(order.ScheduledTime))
                SetScheduleTime(order.ScheduledTime);
            else
                TestContext.Progress.WriteLine(
                    "[WARNING] Scheduled time is empty. Time selection skipped.");

            SubmitOrder();
            VerifySuccess();

            TestContext.Progress.WriteLine(
                "[OK] Measurement order workflow completed. MRN: " + order.Mrn);
        }

        public void OpenMeasurementTab()
        {
            TestContext.Progress.WriteLine("Navigating to Measurement tab...");
            WaitForInvisibleLoader();

            IWebElement measurementIcon = WaitForElementClickable(MeasurementTab);
            ScrollIntoView(measurementIcon);
            SafeClick(measurementIcon);

            WaitForInvisibleLoader();

            bool opened = _wait.Until(driver =>
            {
                string url = driver.Url ?? string.Empty;
                return url.Contains("/measurements", StringComparison.OrdinalIgnoreCase) ||
                       IsAnyElementVisible(driver, OrderMeasurementButton);
            });

            if (!opened)
                throw new WebDriverTimeoutException("Measurement page did not open.");

            TestContext.Progress.WriteLine("[OK] Measurement page opened.");
            TestContext.Progress.WriteLine("Measurement URL: " + _driver.Url);
        }

        public void ClickOrderMeasurement()
        {
            TestContext.Progress.WriteLine("Opening Order Measurement dialog...");
            WaitForInvisibleLoader();

            IWebElement button = WaitForElementClickable(OrderMeasurementButton);
            ScrollIntoView(button);
            SafeClick(button);

            bool opened = _wait.Until(driver =>
                IsAnyElementVisible(driver, OrderPanel) ||
                IsAnyElementVisible(driver, SidePanelContents) ||
                IsAnyElementVisible(driver, DeviceDropdown));

            if (!opened)
                throw new WebDriverTimeoutException(
                    "Order Measurement dialog did not open.");

            TestContext.Progress.WriteLine("[OK] Order Measurement dialog opened.");
        }

        public void SelectDevice(string deviceName)
        {
            deviceName = deviceName?.Trim();

            if (string.IsNullOrWhiteSpace(deviceName))
                throw new ArgumentException("Device name is empty.", nameof(deviceName));

            TestContext.Progress.WriteLine("Selecting device: " + deviceName);

            IWebElement deviceHost = WaitForElementClickable(DeviceDropdown);
            ScrollIntoView(deviceHost);
            SafeClick(deviceHost);

            var optionWait = new WebDriverWait(
                _driver,
                TimeSpan.FromSeconds(15));

            List<IWebElement> options = optionWait.Until(_ =>
            {
                List<IWebElement> found = FindDeviceOptions(deviceHost);
                return found.Count > 0 ? found : null;
            });

            TestContext.Progress.WriteLine(
                "Available device options: " +
                string.Join(" | ", options.Select(GetTextSafely)));

            IWebElement matchingOption = options.FirstOrDefault(option =>
                string.Equals(
                    GetTextSafely(option),
                    deviceName,
                    StringComparison.OrdinalIgnoreCase));

            if (matchingOption == null)
            {
                matchingOption = options.FirstOrDefault(option =>
                    GetTextSafely(option).IndexOf(
                        deviceName,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (matchingOption == null)
                throw new NoSuchElementException("Device was not found: " + deviceName);

            string selectedText = GetTextSafely(matchingOption);
            ScrollIntoView(matchingOption);
            SafeClick(matchingOption);

            // Allow the component to register the selection, then close only with Escape.
            // Do not click the panel here because that can cancel the selected option.
            Thread.Sleep(750);

            try
            {
                _driver.SwitchTo().ActiveElement().SendKeys(SeleniumKeys.Escape);
            }
            catch
            {
                // The option click may already have closed the dropdown.
            }

            Thread.Sleep(500);

            TestContext.Progress.WriteLine("[OK] Device selected: " + selectedText);
        }

        private List<IWebElement> FindDeviceOptions(IWebElement host)
        {
            const string selector =
                "option, div.option, li, .option, [role='option']";

            try
            {
                var options = host.GetShadowRoot().FindElements(By.CssSelector(selector));
                if (options.Count > 0)
                    return options.Where(IsDisplayedSafely).ToList();
            }
            catch
            {
                // Continue with panel/global lookup.
            }

            foreach (IWebElement panel in _driver.FindElements(OrderPanel))
            {
                try
                {
                    var options = panel.FindElements(By.CssSelector(selector));
                    if (options.Count > 0)
                        return options.Where(IsDisplayedSafely).ToList();
                }
                catch (StaleElementReferenceException)
                {
                    // Try next panel.
                }
            }

            return _driver.FindElements(By.CssSelector(selector))
                .Where(IsDisplayedSafely)
                .ToList();
        }

        public void SetScheduleDate(string rawDate)
        {
            if (!TryParseDate(rawDate, out DateTime parsedDate))
            {
                throw new ArgumentException(
                    "Schedule date is invalid: " + rawDate,
                    nameof(rawDate));
            }

            string uiDate = parsedDate.ToString(
                "MM/dd/yyyy",
                CultureInfo.InvariantCulture);

            TestContext.Progress.WriteLine("Excel schedule date: " + rawDate);
            TestContext.Progress.WriteLine("ADI Planner schedule date: " + uiDate);

            IWebElement dateHost = WaitForElementClickable(DateHost);
            ScrollIntoView(dateHost);
            SafeClick(dateHost);
            Thread.Sleep(500);

            IWebElement calendarDay = FindCalendarDay(dateHost, parsedDate.Day);

            if (calendarDay != null)
            {
                ScrollIntoView(calendarDay);
                SafeClick(calendarDay);
                TestContext.Progress.WriteLine("[OK] Schedule date selected: " + uiDate);
            }
            else
            {
                SetComponentValue(dateHost, uiDate);
                TestContext.Progress.WriteLine("[OK] Schedule date entered: " + uiDate);
            }

            ClosePopup(dateHost);
            Thread.Sleep(500);
        }

        private bool TryParseDate(string value, out DateTime parsedDate)
        {
            parsedDate = default;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            string normalized = value.Trim().Replace("-", "/");

            string[] formats =
            {
                "MM/dd/yyyy",
                "M/d/yyyy",
                "yyyy/MM/dd"
            };

            return DateTime.TryParseExact(
                normalized,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsedDate);
        }

        private IWebElement FindCalendarDay(IWebElement dateHost, int day)
        {
            string dayText = day.ToString(CultureInfo.InvariantCulture);

            try
            {
                ISearchContext shadowRoot = dateHost.GetShadowRoot();
                IWebElement shadowDay = shadowRoot
                    .FindElements(By.CssSelector(".t-day"))
                    .FirstOrDefault(element =>
                        IsDisplayedSafely(element) &&
                        string.Equals(
                            GetTextSafely(element),
                            dayText,
                            StringComparison.Ordinal) &&
                        !GetAttributeSafely(element, "class")
                            .Contains("disabled", StringComparison.OrdinalIgnoreCase));

                if (shadowDay != null)
                    return shadowDay;
            }
            catch
            {
                // Continue with global lookup.
            }

            return _driver.FindElements(By.XPath(
                    "//*[contains(@class,'t-day') and normalize-space()='" +
                    dayText +
                    "' and not(contains(@class,'disabled'))]"))
                .FirstOrDefault(IsDisplayedSafely);
        }

        private void SetComponentValue(IWebElement host, string value)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                @"
                const host = arguments[0];
                const value = arguments[1];
                let input = host.shadowRoot
                    ? host.shadowRoot.querySelector('input')
                    : host.querySelector('input');
                const target = input || host;
                target.value = value;
                target.dispatchEvent(new Event('input', { bubbles: true }));
                target.dispatchEvent(new Event('change', { bubbles: true }));
                target.dispatchEvent(new Event('blur', { bubbles: true }));
                ",
                host,
                value);
        }

        private void ClosePopup(IWebElement host)
        {
            try
            {
                host.SendKeys(SeleniumKeys.Escape);
            }
            catch
            {
                try
                {
                    _driver.SwitchTo().ActiveElement().SendKeys(SeleniumKeys.Escape);
                }
                catch
                {
                    // Popup may already be closed.
                }
            }
        }

        private static string GetAttributeSafely(IWebElement element, string name)
        {
            try
            {
                return element.GetAttribute(name) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public void SetScheduleTime(string value)
        {
            value = value.Trim();
            TestContext.Progress.WriteLine("Setting schedule time: " + value);

            IWebElement host = WaitForElementPresent(TimeHost);
            ScrollIntoView(host);
            SafeClick(host);

            IWebElement editor = null;

            try
            {
                editor = _driver.SwitchTo().ActiveElement();
                if (!string.Equals(editor.TagName, "input", StringComparison.OrdinalIgnoreCase))
                    editor = null;
            }
            catch
            {
                editor = null;
            }

            if (editor != null)
            {
                editor.SendKeys(SeleniumKeys.Control + "a");
                editor.SendKeys(SeleniumKeys.Delete);
                editor.SendKeys(value);
                editor.SendKeys(SeleniumKeys.Tab);
            }
            else
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "arguments[0].value=arguments[1];" +
                    "arguments[0].dispatchEvent(new Event('input',{bubbles:true,composed:true}));" +
                    "arguments[0].dispatchEvent(new Event('change',{bubbles:true,composed:true}));",
                    host,
                    value);
            }

            TestContext.Progress.WriteLine("[OK] Schedule time entered: " + value);
        }

        public void SubmitOrder()
        {
            TestContext.Progress.WriteLine("Submitting measurement order...");
            WaitForInvisibleLoader();

            IWebElement button = WaitForElementClickable(SubmitButton);

            if (!button.Enabled ||
                string.Equals(button.GetAttribute("aria-disabled"), "true",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Place Order button is disabled. Check the schedule date and required fields.");
            }

            ScrollIntoView(button);
            SafeClick(button);

            TestContext.Progress.WriteLine("[OK] Place Order button clicked.");
            Thread.Sleep(3000);
            WaitForInvisibleLoader();
        }

        public void VerifySuccess()
        {
            TestContext.Progress.WriteLine("Validating measurement order creation...");

            bool success = _wait.Until(driver =>
            {
                bool toastVisible = IsAnyElementVisible(driver, SuccessToast);
                bool panelVisible = IsAnyElementVisible(driver, SidePanelContents);
                bool orderButtonVisible = IsAnyElementVisible(driver, OrderMeasurementButton);

                return toastVisible || (!panelVisible && orderButtonVisible);
            });

            if (!success)
                throw new WebDriverTimeoutException(
                    "Measurement order success was not confirmed.");

            TestContext.Progress.WriteLine(
                "[OK] Measurement order created successfully.");
        }

        private IWebElement WaitForElementPresent(By locator)
        {
            return _wait.Until(driver =>
            {
                try
                {
                    return driver.FindElements(locator).FirstOrDefault();
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
                    return driver.FindElements(locator)
                        .FirstOrDefault(e => IsDisplayedSafely(e) && e.Enabled);
                }
                catch
                {
                    return null;
                }
            });
        }

        private void WaitForInvisibleLoader()
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));

            wait.Until(driver => driver.FindElements(By.CssSelector(
                    "#loader, [data-testid='loading-dialog'], " +
                    "[data-testid='loader'], .loader, .spinner"))
                .All(loader =>
                {
                    try { return !loader.Displayed; }
                    catch { return true; }
                }));
        }

        private void SafeClick(IWebElement element)
        {
            try
            {
                element.Click();
            }
            catch (WebDriverException)
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "arguments[0].click();",
                    element);
            }
        }

        private void ScrollIntoView(IWebElement element)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                "arguments[0].scrollIntoView({block:'center',inline:'nearest'});",
                element);
        }

        private static bool IsAnyElementVisible(IWebDriver driver, By locator)
        {
            try
            {
                return driver.FindElements(locator).Any(IsDisplayedSafely);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsDisplayedSafely(IWebElement element)
        {
            try
            {
                return element != null && element.Displayed;
            }
            catch
            {
                return false;
            }
        }

        private static string GetTextSafely(IWebElement element)
        {
            try
            {
                return element.Text?.Trim() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
