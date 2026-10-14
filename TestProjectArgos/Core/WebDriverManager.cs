using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace SimulatorAutomation.Tests.Core
{
    public class WebDriverManager : IDisposable
    {
        private IWebDriver _driver;
        private WebDriverWait _wait;

        public IWebDriver Driver => _driver;
        public WebDriverWait Wait => _wait;

        public WebDriverManager(int timeoutSeconds = 10)
        {
            InitializeDriver(timeoutSeconds);
        }

        private void InitializeDriver(int timeoutSeconds)
        {
            try
            {
                // Configure Chrome options
                var options = new ChromeOptions();
                
                // Optional: Run headless (no GUI window)
                // options.AddArgument("--headless");
                
                // Optional: Disable notifications, popups
                options.AddArgument("--disable-notifications");
                options.AddArgument("--disable-popup-blocking");
                
                // Optional: Start maximized
                options.AddArgument("--start-maximized");
                
                // Create driver
                _driver = new ChromeDriver(options);
                
                // Set timeouts
                _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(timeoutSeconds);
                _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
                
                // Create wait object for explicit waits
                _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
                
                Console.WriteLine("ChromeDriver initialized successfully.");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Failed to initialize ChromeDriver: " + exception.Message);
                throw;
            }
        }

        public void NavigateTo(string url)
        {
            try
            {
                _driver.Navigate().GoToUrl(url);
                Console.WriteLine("Navigated to: " + url);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Navigation failed: " + exception.Message);
                throw;
            }
        }

        public void Close()
        {
            try
            {
                _driver?.Quit();
                Console.WriteLine("ChromeDriver closed.");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error closing ChromeDriver: " + exception.Message);
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
