using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Text;

namespace PowerToFlyBot.Selenium
{
    public class SeleniumSettings
    {
        public string UserDataDir { get; set; } = "./";

        public string ProfileDirectory { get; set; }

        public bool IsDisableExtensions { get; set; } = false;

        public bool IsHeadless { get; set; }
    }

    public class WebDriverStatus
    {
        public int Id { get; set; }

        public ChromeDriver WebDriver { get; set; }

        public SeleniumSettings Settings { get; set; }

        public bool IsActive { get; set; }
    }

    public class SeleniumWebDriver
    {
        public ChromeOptions ChromeWebDriverOptions { get; private set; }

        public ChromeDriver ChromeDriver { get; private set; }

        public SeleniumWebDriver(SeleniumSettings seleniumSettings)
        {
            ChromeWebDriverOptions = new ChromeOptions();

            ChromeWebDriverOptions.AddArguments(new List<string>()
            {
                $"user-data-dir={seleniumSettings.UserDataDir}",
                $"profile-directory={seleniumSettings.ProfileDirectory}",
            });

            if (seleniumSettings.IsDisableExtensions)
                ChromeWebDriverOptions.AddArgument("disable-extensions");

            if (seleniumSettings.IsHeadless)
                ChromeWebDriverOptions.AddArgument("headless");

            var chromeDriverService = ChromeDriverService.CreateDefaultService(seleniumSettings.UserDataDir);

            ChromeDriver = new ChromeDriver(chromeDriverService, ChromeWebDriverOptions);
        }

        public void GoToUrl(string url)
        {
            ChromeDriver.Navigate().GoToUrl(url);
        }
    }
}
