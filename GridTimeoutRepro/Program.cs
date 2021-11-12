using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace GridTimeoutRepro
{
	class Program
	{
		private static string _gridUrl;
		private const int Concurrency = 50;
		private const double DelayBetweenCreatingSessions = 0.1;

		static void Main(string[] args)
		{
			if (!SetGridUrl(args)) return;

			// Spin up x number of tasks where x is determined by the Concurrency field that each creates a browser session on the grid and does stuff to generate some load
			var sessions = CreateSessionsOnGrid();

			// See if any sessions failed and, if we find any, write the exceptions encountered to the console.
			var failed = sessions.Where(t => t.Result.Failed).ToList();

			if (!failed.Any())
			{
				Console.WriteLine($"{Environment.NewLine}All sessions succeeded!");
				return;
			}

			Console.WriteLine($"{Environment.NewLine}{failed.Count} sessions failed{Environment.NewLine}");

			foreach (var failedRun in failed)
			{
				Console.WriteLine($"Session [{failedRun.Result.SessionId}] failed with {failedRun.Result.Exception.Message}: {failedRun.Result.Exception.StackTrace}{Environment.NewLine}");
			}

			// Run with local browser
			//DoStuffThatGeneratesSomeLoad(null);
		}

		private static Task<RunResult>[] CreateSessionsOnGrid()
		{
			Console.WriteLine($"Creating {Concurrency} sessions on grid with url {_gridUrl}{Environment.NewLine}");

			var tasks = new Task<RunResult>[Concurrency];

			for (var i = 0; i < tasks.Length; i++)
			{
				Console.WriteLine($"Starting session {i + 1}");

				tasks[i] = Task.Factory.StartNew(() => DoStuffThatGeneratesSomeLoad(_gridUrl));

				Thread.Sleep(TimeSpan.FromSeconds(DelayBetweenCreatingSessions));
			}

			Console.WriteLine($"{Environment.NewLine}Waiting for all sessions to end");

			Task.WaitAll(tasks);

			return tasks;
		}

		private static bool SetGridUrl(string[] args)
		{
			if (args.Length <= 0 && string.IsNullOrWhiteSpace(_gridUrl))
			{
				Console.WriteLine("No grid url found. Supply it as an argument, e.g. 'dotnet run http://mygridurl' or initialize the static field holding the grid url");
				return false;
			}

			if (string.IsNullOrWhiteSpace(_gridUrl))
			{
				_gridUrl ??= args[0];
			}

			return true;
		}

		private static RunResult DoStuffThatGeneratesSomeLoad(string gridUrl)
		{
			var result = new RunResult();

			IWebDriver driver = null;

			try
			{
				// Establish a session and go to w3.org and find the WebDriver spec in an inefficient way
				driver = GetDriver(gridUrl);

				if (driver is RemoteWebDriver remoteWebDriver)
				{
					result.SessionId = remoteWebDriver.SessionId.ToString();
				}

				driver.Manage().Window.Maximize();
				driver.Navigate().GoToUrl("https://www.w3.org/TR/");

				var summaryHeaderText = driver.FindElement(By.Id("summary")).Text;
				var select = new SelectElement(driver.FindElement(By.Id("tag")));
				select.SelectByText("Web API");

				// Wait for the filtering to complete by expecting the header text to change
				var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10)) { PollingInterval = TimeSpan.FromMilliseconds(500) };
				wait.Until((d) => d.FindElement(By.Id("summary")).Text != summaryHeaderText);

				// now let's proceed to find the link to the webdriver spec in a very roundabout way that generates some load
				var webApiStandards = driver.FindElements(By.CssSelector("ul#container > li[data-version*='upcoming']")).Where(e => e.Displayed);

				IWebElement linkToClick = null;

				foreach (var standard in webApiStandards)
				{
					var standardElements = standard.FindElements(By.XPath("./*"));

					foreach (var standardElement in standardElements)
					{
						if (standardElement.GetAttribute("class").Equals("WorkingDraft"))
						{
							var link = standardElement.FindElement(By.TagName("a"));

							if (link.GetAttribute("href").Contains("webdriver2"))
							{
								linkToClick = link;
							}
						}
					}
				}

				linkToClick.Click();
			}
			catch (Exception e)
			{
				result.Exception = e;
			}
			finally
			{
				driver?.Quit();
			}

			return result;
		}

		private static IWebDriver GetDriver(string gridUrl)
		{
			ICapabilities GetCapabilities()
			{
				var options = new ChromeOptions();
				//options.AddArgument("--headless");
				options.AddArgument("--window-size=1920,1080");
				options.AddArgument("--disable-gpu");
				options.AddArgument("--disable-extensions");
				options.AddArgument("--disable-dev-shm-usage");
				return options.ToCapabilities();
			}

			return gridUrl != null ? new RemoteWebDriver(new Uri(gridUrl), GetCapabilities(), TimeSpan.FromSeconds(120)) : new ChromeDriver();
		}

		private class RunResult
		{
			public string SessionId { get; set; }

			public Exception Exception { get; set; }

			public bool Failed => Exception != null;
		}
	}
}
