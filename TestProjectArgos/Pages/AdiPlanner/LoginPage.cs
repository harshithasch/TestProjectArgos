using System;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace SimulatorAutomation.Tests.Pages.AdiPlanner
{
    /// <summary>
    /// ADI Planner Login Page Object.
    /// </summary>
    public class LoginPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        private readonly By LoginButton =
            By.ClassName("_loginButton_1dnrd_114");

        private readonly By EmailInput =
            By.Id("input27");

        private readonly By NextButton =
            By.ClassName("button-primary");

        private readonly By PasswordInput =
            By.Id("input60");

        private readonly By VerifyButton =
            By.ClassName("button-primary");

        private readonly By LandingPageHeading =
            By.ClassName("heading-md-medium");

        public LoginPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(
                driver,
                TimeSpan.FromSeconds(20));

            _wait.IgnoreExceptionTypes(
                typeof(NoSuchElementException),
                typeof(StaleElementReferenceException));
        }

        public void Open(string url)
        {
            _driver.Navigate().GoToUrl(url);

            TestContext.Progress.WriteLine(
                "Navigated to login page");
        }

        public void Login(
            string username,
            string password)
        {
            try
            {
                TestContext.Progress.WriteLine(
                    "Opening login page");

                IWebElement loginBtn =
                    WaitForElementClickable(LoginButton);

                SafeClick(loginBtn);

                TestContext.Progress.WriteLine(
                    "[OK] Login button clicked");

                IWebElement emailField =
                    WaitForElementVisible(EmailInput);

                emailField.Clear();
                emailField.SendKeys(username);

                TestContext.Progress.WriteLine(
                    "[OK] Username entered");

                IWebElement nextBtn =
                    WaitForElementClickable(NextButton);

                SafeClick(nextBtn);

                TestContext.Progress.WriteLine(
                    "[OK] Next button clicked");

                IWebElement passwordField =
                    WaitForElementVisible(PasswordInput);

                passwordField.Clear();
                passwordField.SendKeys(password);

                TestContext.Progress.WriteLine(
                    "[OK] Password entered");

                IWebElement verifyBtn =
                    WaitForElementClickable(VerifyButton);

                SafeClick(verifyBtn);

                TestContext.Progress.WriteLine(
                    "[OK] Verify button clicked");

                WaitForElementPresent(
                    LandingPageHeading);

                TestContext.Progress.WriteLine(
                    "[OK] Login successful");
            }
            catch (Exception ex)
            {
                TestContext.Progress.WriteLine(
                    $"[ERROR] Login failed: {ex.Message}");

                throw;
            }
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
                    IWebElement element =
                        driver.FindElement(locator);

                    return element.Displayed
                        ? element
                        : null;
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
                    IWebElement element =
                        driver.FindElement(locator);

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

        private void SafeClick(IWebElement element)
        {
            try
            {
                element.Click();
            }
            catch
            {
                ((IJavaScriptExecutor)_driver)
                    .ExecuteScript(
                        "arguments[0].click();",
                        element);
            }
        }
    }
}