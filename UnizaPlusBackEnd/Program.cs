using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using UnizaPlus.Services;
using UnizaPlusBackEnd.Models;
using UnizaPlusBackEnd.Services;

namespace UnizaPlusBackEnd
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("UnizaPlus - Schedule scraper");
            var scheduleItems = new List<ScheduleItem>();

            string? username = null;
            string? password = null;

            bool autoMode = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--username" && i + 1 < args.Length)
                    username = args[i + 1];
                else if (args[i] == "--password" && i + 1 < args.Length)
                    password = args[i + 1];
                else if (args[i] == "--auto")
                    autoMode = true;
            }

            string solutionDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
            string outputPath = Path.Combine(solutionDir, "schedule.csv");

            if (File.Exists(outputPath) && !autoMode)
            {
                Console.WriteLine($"Schedule file already exists at {outputPath}.");
                Console.WriteLine("Do you want to re-scrape the data? (y/n)");
                string answer = Console.ReadLine() ?? "";

                if (answer.Trim().ToLower() != "y")
                {
                    Console.WriteLine("Using existing schedule data. Exiting...");
                    return;
                }
            }
            else if (File.Exists(outputPath) && autoMode)
            {
                Console.WriteLine($"Schedule file already exists. Using existing data in auto mode.");
                return;
            }
            else
            {
                Console.WriteLine($"Schedule file not found. Will now scrape data.");
            }

            try
            {
               
                if (string.IsNullOrEmpty(username))
                {
                    Console.Write("Enter username: ");
                    username = Console.ReadLine() ?? "";
                }

                if (string.IsNullOrEmpty(password))
                {
                    Console.Write("Enter password: ");
                    password = ReadPassword() ?? "";
                }

                var options = new ChromeOptions();
                //options.AddArgument("--headless"); 

                using (var driver = new ChromeDriver(options))
                {
                    driver.Navigate().GoToUrl("https://vzdelavanie.uniza.sk/vzdelavanie/index.php");
                    Console.WriteLine("Navigated to the main page");

                    var loginButton = driver.FindElement(By.CssSelector("a[href='https://vzdelavanie.uniza.sk/vzdelavanie/login.php']"));
                    loginButton.Click();
                    Console.WriteLine("Clicked on login button");

                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                    wait.Until(d => d.FindElement(By.Id("meno")));

                    var usernameField = driver.FindElement(By.Id("meno"));
                    var passwordField = driver.FindElement(By.Id("heslo"));

                    usernameField.SendKeys(username);
                    passwordField.SendKeys(password);
                    Console.WriteLine("Filled in credentials");

                    var submitButton = driver.FindElement(By.Id("login"));
                    submitButton.Click();
                    Console.WriteLine("Submitted login form");

                    await Task.Delay(2000);
                    Console.WriteLine("Current URL after login attempt: " + driver.Url);

                    wait.Until(d => d.FindElement(By.Id("id2-dropbtn")));
                    var generalInfoDropdown = driver.FindElement(By.Id("id2-dropbtn"));
                    generalInfoDropdown.Click();
                    Console.WriteLine("Clicked on general info dropdown");

                   
                    wait.Until(d => d.FindElement(By.Id("display-rozvrh-desk")));
                    var scheduleLink = driver.FindElement(By.Id("display-rozvrh-desk"));
                    scheduleLink.Click();
                    Console.WriteLine("Clicked on schedule link");

                    await Task.Delay(2000);
                    Console.WriteLine("Navigated to schedule page: " + driver.Url);
                    Console.WriteLine("Starting detailed schedule extraction - this may take several minutes...");

                    var scraper = new ScheduleScraper();
                    scheduleItems = scraper.ExtractScheduleData(driver);
                    Console.WriteLine($"Extracted {scheduleItems.Count} schedule items with detailed information");

                    var fileService = new FileService();
                    fileService.SaveScheduleToFile(scheduleItems, outputPath);
                    Console.WriteLine($"Schedule data saved to {outputPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static string ReadPassword()
        {
            var password = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password[0..^1];
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password;
        }
    }
}