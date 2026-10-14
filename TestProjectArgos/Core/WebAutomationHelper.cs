using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace SimulatorAutomation.Tests.Core
{
    public class WebAutomationHelper
    {
        private readonly WebDriverManager _webDriverManager;

        public WebAutomationHelper(
            WebDriverManager webDriverManager)
        {
            _webDriverManager =
                webDriverManager ??
                throw new ArgumentNullException(
                    nameof(webDriverManager));
        }

        private IWebDriver Driver
        {
            get
            {
                if (_webDriverManager.Driver == null)
                {
                    throw new InvalidOperationException(
                        "WebDriver is not initialized. " +
                        "Start the browser before using " +
                        "WebAutomationHelper.");
                }

                return _webDriverManager.Driver;
            }
        }

        /// <summary>
        /// Finds a clickable element by ID.
        /// </summary>
        public IWebElement FindElementById(
            string elementId,
            int timeoutSeconds = 10)
        {
            ValidateText(
                elementId,
                nameof(elementId));

            return FindClickableElement(
                By.Id(elementId),
                timeoutSeconds,
                "ID",
                elementId);
        }

        /// <summary>
        /// Finds a clickable element by Name.
        /// </summary>
        public IWebElement FindElementByName(
            string elementName,
            int timeoutSeconds = 10)
        {
            ValidateText(
                elementName,
                nameof(elementName));

            return FindClickableElement(
                By.Name(elementName),
                timeoutSeconds,
                "Name",
                elementName);
        }

        /// <summary>
        /// Finds a clickable element by XPath.
        /// </summary>
        public IWebElement FindElementByXPath(
            string xpath,
            int timeoutSeconds = 10)
        {
            ValidateText(
                xpath,
                nameof(xpath));

            return FindClickableElement(
                By.XPath(xpath),
                timeoutSeconds,
                "XPath",
                xpath);
        }

        /// <summary>
        /// Finds a clickable element by CSS selector.
        /// </summary>
        public IWebElement FindElementByCSS(
            string cssSelector,
            int timeoutSeconds = 10)
        {
            ValidateText(
                cssSelector,
                nameof(cssSelector));

            return FindClickableElement(
                By.CssSelector(cssSelector),
                timeoutSeconds,
                "CSS selector",
                cssSelector);
        }

        /// <summary>
        /// Finds a visible element using any Selenium locator.
        /// </summary>
        public IWebElement FindVisibleElement(
            By by,
            int timeoutSeconds = 10)
        {
            if (by == null)
            {
                throw new ArgumentNullException(
                    nameof(by));
            }

            ValidateTimeout(timeoutSeconds);

            var wait = CreateWait(timeoutSeconds);

            try
            {
                IWebElement element =
                    wait.Until(driver =>
                    {
                        try
                        {
                            IWebElement candidate =
                                driver.FindElement(by);

                            return candidate.Displayed
                                ? candidate
                                : null;
                        }
                        catch (NoSuchElementException)
                        {
                            return null;
                        }
                        catch (StaleElementReferenceException)
                        {
                            return null;
                        }
                    });

                Console.WriteLine(
                    "Visible element found: " + by);

                return element;
            }
            catch (WebDriverTimeoutException exception)
            {
                string message =
                    "Timed out after " +
                    timeoutSeconds +
                    " seconds waiting for visible element: " +
                    by;

                Console.WriteLine(message);

                throw new NoSuchElementException(
                    message,
                    exception);
            }
        }

        /// <summary>
        /// Finds a clickable element using any Selenium locator.
        /// </summary>
        public IWebElement FindClickableElement(
            By by,
            int timeoutSeconds = 10)
        {
            if (by == null)
            {
                throw new ArgumentNullException(
                    nameof(by));
            }

            return FindClickableElement(
                by,
                timeoutSeconds,
                "locator",
                by.ToString());
        }

        /// <summary>
        /// Types text into an element after clearing it.
        /// </summary>
        public void TypeText(
            IWebElement element,
            string text)
        {
            if (element == null)
            {
                throw new ArgumentNullException(
                    nameof(element));
            }

            try
            {
                element.Clear();

                if (!string.IsNullOrEmpty(text))
                {
                    element.SendKeys(text);
                }

                Console.WriteLine(
                    "Text entered successfully.");
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    "Failed to type text: " +
                    exception.Message);

                throw;
            }
        }

        /// <summary>
        /// Selects an element.
        /// </summary>
        public void ClickElement(
            IWebElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(
                    nameof(element));
            }

            try
            {
                element.Click();

                Console.WriteLine(
                    "Element selected successfully.");
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    "Failed to select element: " +
                    exception.Message);

                throw;
            }
        }

        /// <summary>
        /// Waits until at least one matching element is visible.
        /// </summary>
        public bool WaitForElementVisible(
            By by,
            int timeoutSeconds = 10)
        {
            if (by == null)
            {
                throw new ArgumentNullException(
                    nameof(by));
            }

            ValidateTimeout(timeoutSeconds);

            var wait = CreateWait(timeoutSeconds);

            try
            {
                bool visible =
                    wait.Until(driver =>
                    {
                        try
                        {
                            IReadOnlyCollection<IWebElement> elements =
                                driver.FindElements(by);

                            foreach (IWebElement element in elements)
                            {
                                try
                                {
                                    if (element.Displayed)
                                    {
                                        return true;
                                    }
                                }
                                catch (StaleElementReferenceException)
                                {
                                    // Continue checking other elements.
                                }
                            }

                            return false;
                        }
                        catch (NoSuchElementException)
                        {
                            return false;
                        }
                        catch (StaleElementReferenceException)
                        {
                            return false;
                        }
                    });

                Console.WriteLine(
                    "Element is visible: " + by);

                return visible;
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine(
                    "Timed out waiting for element visibility: " +
                    by);

                return false;
            }
        }

        /// <summary>
        /// Waits until an element becomes invisible or is removed.
        /// </summary>
        public bool WaitForElementInvisible(
            By by,
            int timeoutSeconds = 10)
        {
            if (by == null)
            {
                throw new ArgumentNullException(
                    nameof(by));
            }

            ValidateTimeout(timeoutSeconds);

            var wait = CreateWait(timeoutSeconds);

            try
            {
                bool invisible =
                    wait.Until(driver =>
                    {
                        try
                        {
                            IReadOnlyCollection<IWebElement> elements =
                                driver.FindElements(by);

                            if (elements.Count == 0)
                            {
                                return true;
                            }

                            foreach (IWebElement element in elements)
                            {
                                try
                                {
                                    if (element.Displayed)
                                    {
                                        return false;
                                    }
                                }
                                catch (StaleElementReferenceException)
                                {
                                    // A stale element is no longer visible.
                                }
                            }

                            return true;
                        }
                        catch (NoSuchElementException)
                        {
                            return true;
                        }
                        catch (StaleElementReferenceException)
                        {
                            return true;
                        }
                    });

                Console.WriteLine(
                    "Element is invisible: " + by);

                return invisible;
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine(
                    "Timed out waiting for element invisibility: " +
                    by);

                return false;
            }
        }

        /// <summary>
        /// Gets the current browser URL.
        /// </summary>
        public string GetCurrentUrl()
        {
            return Driver.Url;
        }

        /// <summary>
        /// Gets the current page title.
        /// </summary>
        public string GetPageTitle()
        {
            return Driver.Title;
        }

        /// <summary>
        /// Executes JavaScript without arguments.
        /// </summary>
        public object ExecuteScript(
            string script)
        {
            return ExecuteScript(
                script,
                Array.Empty<object>());
        }

        /// <summary>
        /// Executes JavaScript with optional arguments.
        /// </summary>
        public object ExecuteScript(
            string script,
            params object[] arguments)
        {
            ValidateText(
                script,
                nameof(script));

            try
            {
                IJavaScriptExecutor javaScriptExecutor =
                    Driver as IJavaScriptExecutor;

                if (javaScriptExecutor == null)
                {
                    throw new NotSupportedException(
                        "The active WebDriver does not support " +
                        "JavaScript execution.");
                }

                return javaScriptExecutor.ExecuteScript(
                    script,
                    arguments ?? Array.Empty<object>());
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    "Failed to execute JavaScript: " +
                    exception.Message);

                throw;
            }
        }

        /// <summary>
        /// Saves a browser screenshot.
        /// </summary>
        public void TakeScreenshot(
            string filePath)
        {
            ValidateText(
                filePath,
                nameof(filePath));

            try
            {
                ITakesScreenshot screenshotDriver =
                    Driver as ITakesScreenshot;

                if (screenshotDriver == null)
                {
                    throw new NotSupportedException(
                        "The active WebDriver does not support " +
                        "screenshots.");
                }

                Screenshot screenshot =
                    screenshotDriver.GetScreenshot();

                screenshot.SaveAsFile(filePath);

                Console.WriteLine(
                    "Screenshot saved: " + filePath);
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    "Failed to take screenshot: " +
                    exception.Message);

                throw;
            }
        }

        /// <summary>
        /// Switches to a frame using its index.
        /// </summary>
        public void SwitchToFrame(
            int frameIndex)
        {
            if (frameIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(frameIndex),
                    "Frame index cannot be negative.");
            }

            try
            {
                Driver.SwitchTo().Frame(frameIndex);

                Console.WriteLine(
                    "Switched to frame: " + frameIndex);
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    "Failed to switch to frame: " +
                    exception.Message);

                throw;
            }
        }

        /// <summary>
        /// Switches to a frame using its element.
        /// </summary>
        public void SwitchToFrame(
            IWebElement frameElement)
        {
            if (frameElement == null)
            {
                throw new ArgumentNullException(
                    nameof(frameElement));
            }

            try
            {
                Driver.SwitchTo().Frame(frameElement);

                Console.WriteLine(
                    "Switched to frame element.");
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    "Failed to switch to frame element: " +
                    exception.Message);

                throw;
            }
        }

        /// <summary>
        /// Switches back to the main page.
        /// </summary>
        public void SwitchToDefaultContent()
        {
            try
            {
                Driver.SwitchTo().DefaultContent();

                Console.WriteLine(
                    "Switched to default content.");
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    "Failed to switch to default content: " +
                    exception.Message);

                throw;
            }
        }

        /// <summary>
        /// Gets all elements matching a locator.
        /// </summary>
        public IList<IWebElement> FindElements(
            By by)
        {
            if (by == null)
            {
                throw new ArgumentNullException(
                    nameof(by));
            }

            return Driver.FindElements(by);
        }

        private IWebElement FindClickableElement(
            By by,
            int timeoutSeconds,
            string locatorType,
            string locatorValue)
        {
            ValidateTimeout(timeoutSeconds);

            var wait = CreateWait(timeoutSeconds);

            try
            {
                IWebElement element =
                    wait.Until(driver =>
                    {
                        try
                        {
                            IWebElement candidate =
                                driver.FindElement(by);

                            if (candidate.Displayed &&
                                candidate.Enabled)
                            {
                                return candidate;
                            }

                            return null;
                        }
                        catch (NoSuchElementException)
                        {
                            return null;
                        }
                        catch (StaleElementReferenceException)
                        {
                            return null;
                        }
                    });

                Console.WriteLine(
                    "Clickable element found by " +
                    locatorType +
                    ": " +
                    locatorValue);

                return element;
            }
            catch (WebDriverTimeoutException exception)
            {
                string message =
                    "Timed out after " +
                    timeoutSeconds +
                    " seconds waiting for clickable element by " +
                    locatorType +
                    ": " +
                    locatorValue;

                Console.WriteLine(message);

                throw new NoSuchElementException(
                    message,
                    exception);
            }
        }

        private WebDriverWait CreateWait(
            int timeoutSeconds)
        {
            var wait = new WebDriverWait(
                Driver,
                TimeSpan.FromSeconds(timeoutSeconds));

            wait.PollingInterval =
                TimeSpan.FromMilliseconds(250);

            wait.IgnoreExceptionTypes(
                typeof(NoSuchElementException),
                typeof(StaleElementReferenceException));

            return wait;
        }

        private static void ValidateText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Value cannot be null, empty, or whitespace.",
                    parameterName);
            }
        }

        private static void ValidateTimeout(
            int timeoutSeconds)
        {
            if (timeoutSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeoutSeconds),
                    "Timeout must be greater than zero.");
            }
        }
    }
}