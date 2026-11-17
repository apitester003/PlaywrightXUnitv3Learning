using Microsoft.Playwright;

namespace PlaywrightXUnitv3Learning.Tests.Course.Week09
{
    public class Week09Examples
    {
        [Fact]
        public async Task Day50_ObjectModel_PlaywrightBrowserContextPage()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

            IBrowserContext contextA = await browser.NewContextAsync();
            IBrowserContext contextB = await browser.NewContextAsync();
            IPage pageA = await contextA.NewPageAsync();
            IPage pageB = await contextB.NewPageAsync();

            Assert.Equal(2, browser.Contexts.Count);
            Assert.Single(contextA.Pages);
            Assert.NotSame(pageA, pageB);
            Console.WriteLine($"Browser: {browser.BrowserType.Name} {browser.Version}");
        }

        [Fact]
        public async Task Day51_FirstScript_OpensSauceDemo()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await browser.NewPageAsync();

            await page.GotoAsync("https://www.saucedemo.com");

            Assert.Equal("Swag Labs", await page.TitleAsync());
            Console.WriteLine(await page.TitleAsync());
        }

        [Fact]
        public async Task Day52_Navigation_TitleUrlBackForwardReload()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await browser.NewPageAsync();

            IResponse? response = await page.GotoAsync("https://www.saucedemo.com");
            Assert.NotNull(response);
            Assert.True(response.Ok);
            Assert.Equal(200, response.Status);
            Assert.StartsWith("https://www.saucedemo.com", page.Url);

            await page.Locator("[data-test='username']").FillAsync("standard_user");
            await page.Locator("[data-test='password']").FillAsync("secret_sauce");
            await page.Locator("[data-test='login-button']").ClickAsync();
            await page.WaitForURLAsync("**/inventory.html");
            Assert.EndsWith("/inventory.html", page.Url);

            await page.GoBackAsync();
            Assert.DoesNotContain("inventory", page.Url);

            await page.GoForwardAsync();
            Assert.EndsWith("/inventory.html", page.Url);

            await page.ReloadAsync();
            Assert.Equal("Swag Labs", await page.TitleAsync());
        }

        [Fact]
        public async Task Day52_Navigation_UnknownHostThrows()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await browser.NewPageAsync();

            PlaywrightException ex = await Assert.ThrowsAsync<PlaywrightException>(
                () => page.GotoAsync("https://this-host-does-not-exist.invalid"));
            Console.WriteLine(ex.Message.Split('\n')[0]);
            Assert.Contains("ERR_NAME_NOT_RESOLVED", ex.Message);
        }

        [Fact]
        public async Task Day51_AllThreeBrowsersLaunch()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            foreach (IBrowserType type in new[] { playwright.Chromium, playwright.Firefox, playwright.Webkit })
            {
                await using IBrowser browser = await type.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
                IPage page = await browser.NewPageAsync();
                await page.SetContentAsync("<title>Hi</title><h1>Hello</h1>");
                Assert.Equal("Hi", await page.TitleAsync());
                Console.WriteLine($"{type.Name} {browser.Version}");
            }
        }
    }
}
