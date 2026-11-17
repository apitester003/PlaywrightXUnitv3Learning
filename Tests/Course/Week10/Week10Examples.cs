using Microsoft.Playwright;

namespace PlaywrightXUnitv3Learning.Tests.Course.Week10
{
    public class Week10Examples : IAsyncLifetime
    {
        private IPlaywright _playwright = null!;
        private IBrowser _browser = null!;
        private IPage _page = null!;

        public async ValueTask InitializeAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            _page = await _browser.NewPageAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _browser.CloseAsync();
            _playwright.Dispose();
        }

        private const string Html = """
            <html><body>
              <h1 id="title" class="heading main">Shop</h1>
              <form>
                <label for="email">Email</label>
                <input id="email" type="text" placeholder="you@example.com" data-test="email">
                <label for="pwd">Password</label>
                <input id="pwd" type="password" placeholder="Password" data-test="pwd">
                <button type="submit" data-test="go">Sign in</button>
                <button type="button" data-test="cancel">Cancel</button>
              </form>
              <ul id="items">
                <li class="item">Backpack</li>
                <li class="item">Bike Light</li>
                <li class="item">Fleece Jacket</li>
              </ul>
              <div class="card"><span>Price</span><b>9.99</b></div>
              <div class="card"><span>Price</span><b>29.99</b></div>
              <a href="/about">About us</a>
            </body></html>
            """;

        [Fact]
        public async Task Day55_CssSelectors()
        {
            await _page.SetContentAsync(Html);
            Assert.Equal(1, await _page.Locator("#title").CountAsync());
            Assert.Equal(1, await _page.Locator("h1.heading").CountAsync());
            Assert.Equal(3, await _page.Locator("ul#items > li.item").CountAsync());
            Assert.Equal(1, await _page.Locator("input[type='password']").CountAsync());
            Assert.Equal(1, await _page.Locator("[data-test='go']").CountAsync());
        }

        [Fact]
        public async Task Day55_XPath()
        {
            await _page.SetContentAsync(Html);
            Assert.Equal(3, await _page.Locator("xpath=//ul[@id='items']/li").CountAsync());
            Assert.Equal("Cancel", await _page.Locator("//button[text()='Cancel']").TextContentAsync());
        }

        [Fact]
        public async Task Day56_GetByMethods()
        {
            await _page.SetContentAsync(Html);
            Assert.Equal(1, await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).CountAsync());
            Assert.Equal(1, await _page.GetByRole(AriaRole.Heading, new() { Name = "Shop" }).CountAsync());
            Assert.Equal(1, await _page.GetByRole(AriaRole.Link, new() { Name = "About us" }).CountAsync());
            Assert.Equal(1, await _page.GetByText("Backpack").CountAsync());
            Assert.Equal(1, await _page.GetByLabel("Email").CountAsync());
            Assert.Equal(1, await _page.GetByPlaceholder("you@example.com").CountAsync());
            Assert.Equal(1, await _page.GetByText("backpack", new() { Exact = false }).CountAsync());
            Assert.Equal(0, await _page.GetByText("backpack", new() { Exact = true }).CountAsync());
        }

        [Fact]
        public async Task Day57_TestIds()
        {
            await _page.SetContentAsync(Html);
            _playwright.Selectors.SetTestIdAttribute("data-testid");
            Assert.Equal(0, await _page.GetByTestId("go").CountAsync());

            _playwright.Selectors.SetTestIdAttribute("data-test");
            try
            {
                Assert.Equal(1, await _page.GetByTestId("go").CountAsync());
                await _page.GetByTestId("email").FillAsync("a@b.com");
                Assert.Equal("a@b.com", await _page.GetByTestId("email").InputValueAsync());
            }
            finally
            {
                _playwright.Selectors.SetTestIdAttribute("data-testid");
            }
        }

        [Fact]
        public async Task Day58_ChainingFilteringNth()
        {
            await _page.SetContentAsync(Html);
            ILocator items = _page.Locator("li.item");

            Assert.Equal("Backpack", await items.First.TextContentAsync());
            Assert.Equal("Fleece Jacket", await items.Last.TextContentAsync());
            Assert.Equal("Bike Light", await items.Nth(1).TextContentAsync());

            Assert.Equal(1, await items.Filter(new() { HasText = "Jacket" }).CountAsync());
            Assert.Equal(2, await items.Filter(new() { HasNotText = "Backpack" }).CountAsync());

            ILocator cardWith29 = _page.Locator(".card").Filter(new() { Has = _page.Locator("b", new() { HasText = "29.99" }) });
            Assert.Equal(1, await cardWith29.CountAsync());

            Assert.Equal(3, await _page.Locator("#items").Locator("li").CountAsync());

            IReadOnlyList<string> all = await items.AllTextContentsAsync();
            Assert.Equal(new[] { "Backpack", "Bike Light", "Fleece Jacket" }, all);
        }

        [Fact]
        public async Task Day59_StrictMode()
        {
            await _page.SetContentAsync(Html);
            ILocator items = _page.Locator("li.item");

            PlaywrightException ex = await Assert.ThrowsAsync<PlaywrightException>(
                () => items.TextContentAsync(new() { Timeout = 2000 }));
            Assert.Contains("strict mode violation", ex.Message);

            Assert.Equal("Backpack", await items.First.TextContentAsync());
        }

        [Fact]
        public async Task Day57_SauceDemoLoginUsingLocators()
        {
            await _page.GotoAsync("https://www.saucedemo.com");
            await _page.Locator("[data-test='username']").FillAsync("standard_user");
            await _page.GetByPlaceholder("Password").FillAsync("secret_sauce");
            await _page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();
            await _page.WaitForURLAsync("**/inventory.html");
            await Assertions.Expect(_page.Locator(".inventory_item")).ToHaveCountAsync(6);
        }
    }
}
