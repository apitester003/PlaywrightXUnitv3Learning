using Microsoft.Playwright;

namespace PlaywrightXUnitv3Learning.Tests.Course.Week13
{
    public class Week13Examples : IAsyncLifetime
    {
        private IPlaywright _playwright = null!;
        private IBrowser _browser = null!;

        public async ValueTask InitializeAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }

        public async ValueTask DisposeAsync()
        {
            await _browser.CloseAsync();
            _playwright.Dispose();
        }

        private static async Task LoginAsync(IPage page)
        {
            await page.GotoAsync("https://www.saucedemo.com");
            await page.Locator("[data-test='username']").FillAsync("standard_user");
            await page.Locator("[data-test='password']").FillAsync("secret_sauce");
            await page.Locator("[data-test='login-button']").ClickAsync();
            await page.WaitForURLAsync("**/inventory.html");
        }

        [Fact]
        public async Task Day73_ContextsAreIsolated()
        {
            IBrowserContext a = await _browser.NewContextAsync();
            IBrowserContext b = await _browser.NewContextAsync();
            IPage pageA = await a.NewPageAsync();
            IPage pageB = await b.NewPageAsync();

            await LoginAsync(pageA);
            await pageB.GotoAsync("https://www.saucedemo.com/inventory.html");

            Assert.EndsWith("/inventory.html", pageA.Url);
            await Assertions.Expect(pageB.Locator("[data-test='error']")).ToBeVisibleAsync();

            IReadOnlyList<BrowserContextCookiesResult> cookiesA = await a.CookiesAsync();
            IReadOnlyList<BrowserContextCookiesResult> cookiesB = await b.CookiesAsync();
            Assert.NotEmpty(cookiesA);
            Assert.Empty(cookiesB);
            Console.WriteLine(string.Join(",", cookiesA.Select(c => c.Name)));
        }

        [Fact]
        public async Task Day74_NewTabAndPopup()
        {
            IBrowserContext context = await _browser.NewContextAsync();
            IPage page = await context.NewPageAsync();
            await page.SetContentAsync("""<a id="l" target="_blank" href="https://www.saucedemo.com">open</a>""");

            IPage newTab = await context.RunAndWaitForPageAsync(async () =>
            {
                await page.Locator("#l").ClickAsync();
            });
            await newTab.WaitForLoadStateAsync();

            Assert.Equal(2, context.Pages.Count);
            Assert.Equal("Swag Labs", await newTab.TitleAsync());
            await newTab.CloseAsync();
            Assert.Single(context.Pages);
        }

        [Fact]
        public async Task Day74_WindowOpenPopup()
        {
            IBrowserContext context = await _browser.NewContextAsync();
            IPage page = await context.NewPageAsync();
            await page.SetContentAsync("""<button id="b" onclick="window.open('https://www.saucedemo.com')">pop</button>""");

            IPage popup = await page.RunAndWaitForPopupAsync(async () =>
            {
                await page.Locator("#b").ClickAsync();
            });
            await popup.WaitForLoadStateAsync();
            Assert.Equal("Swag Labs", await popup.TitleAsync());
        }

        [Fact]
        public async Task Day75_CookiesAddAndRead()
        {
            IBrowserContext context = await _browser.NewContextAsync();
            await context.AddCookiesAsync(new[]
            {
                new Cookie { Name = "session-username", Value = "standard_user", Url = "https://www.saucedemo.com" }
            });
            IPage page = await context.NewPageAsync();
            await page.GotoAsync("https://www.saucedemo.com/inventory.html");
            await Assertions.Expect(page.Locator(".inventory_list")).ToBeVisibleAsync();

            await context.ClearCookiesAsync();
            Assert.Empty(await context.CookiesAsync());
        }

        [Fact]
        public async Task Day75_StorageStateReuseLogin()
        {
            string file = Path.Combine(Path.GetTempPath(), "week13-state.json");
            try
            {
                IBrowserContext first = await _browser.NewContextAsync();
                IPage p1 = await first.NewPageAsync();
                await LoginAsync(p1);
                await first.StorageStateAsync(new() { Path = file });
                await first.CloseAsync();

                Assert.True(File.Exists(file));

                IBrowserContext second = await _browser.NewContextAsync(new() { StorageStatePath = file });
                IPage p2 = await second.NewPageAsync();
                await p2.GotoAsync("https://www.saucedemo.com/inventory.html");
                await Assertions.Expect(p2.Locator(".inventory_list")).ToBeVisibleAsync();
                Assert.EndsWith("/inventory.html", p2.Url);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public async Task Day76_ViewportAndEmulation()
        {
            IBrowserContext context = await _browser.NewContextAsync(new()
            {
                ViewportSize = new ViewportSize { Width = 375, Height = 667 },
                Locale = "de-DE",
                TimezoneId = "Europe/Berlin"
            });
            IPage page = await context.NewPageAsync();
            await page.GotoAsync("https://www.saucedemo.com");

            Assert.Equal(375, await page.EvaluateAsync<int>("window.innerWidth"));
            Assert.Equal(667, await page.EvaluateAsync<int>("window.innerHeight"));
            Assert.StartsWith("de", await page.EvaluateAsync<string>("navigator.language"));

            await page.SetViewportSizeAsync(1280, 720);
            Assert.Equal(1280, await page.EvaluateAsync<int>("window.innerWidth"));
        }

        [Fact]
        public async Task Day76_GeolocationAndPermissions()
        {
            IBrowserContext context = await _browser.NewContextAsync(new()
            {
                Geolocation = new Geolocation { Latitude = 52.52f, Longitude = 13.405f },
                Permissions = new[] { "geolocation" }
            });
            IPage page = await context.NewPageAsync();
            await page.GotoAsync("https://www.saucedemo.com");

            double lat = await page.EvaluateAsync<double>(
                "new Promise(r => navigator.geolocation.getCurrentPosition(p => r(p.coords.latitude)))");
            Assert.InRange(lat, 52.5, 52.6);
        }

        [Fact]
        public async Task Day77_DeviceEmulation()
        {
            IBrowserContext context = await _browser.NewContextAsync(_playwright.Devices["iPhone 13"]);
            IPage page = await context.NewPageAsync();
            await page.GotoAsync("https://www.saucedemo.com");
            Assert.Contains("iPhone", await page.EvaluateAsync<string>("navigator.userAgent"));
        }

        [Fact]
        public async Task Day77_SlowMoAndOtherBrowsers()
        {
            foreach (IBrowserType type in new[] { _playwright.Firefox, _playwright.Webkit })
            {
                await using IBrowser b = await type.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true, SlowMo = 50 });
                IPage p = await b.NewPageAsync();
                await p.GotoAsync("https://www.saucedemo.com");
                await Assertions.Expect(p.Locator("[data-test='login-button']")).ToBeVisibleAsync();
            }
        }
    }
}
