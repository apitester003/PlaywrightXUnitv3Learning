using System.Diagnostics;
using Microsoft.Playwright;

namespace PlaywrightXUnitv3Learning.Tests.Course.Week12
{
    public class Week12Examples : IAsyncLifetime
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

        private const string DelayedHtml = """
            <div id="status">Loading</div>
            <script>setTimeout(()=>{document.getElementById('status').textContent='Done';
              const b=document.createElement('button');b.id='late';b.textContent='Late';document.body.appendChild(b);},1000);</script>
            """;

        [Fact]
        public async Task Day67_WebFirstAssertionsRetry()
        {
            await _page.SetContentAsync(DelayedHtml);
            Stopwatch sw = Stopwatch.StartNew();
            await Assertions.Expect(_page.Locator("#status")).ToHaveTextAsync("Done");
            sw.Stop();
            Assert.InRange(sw.ElapsedMilliseconds, 500, 5000);
            Console.WriteLine($"Waited about {sw.ElapsedMilliseconds} ms");
        }

        [Fact]
        public async Task Day67_PlainAssertDoesNotRetry()
        {
            await _page.SetContentAsync(DelayedHtml);
            string? text = await _page.Locator("#status").TextContentAsync();
            Assert.Equal("Loading", text);
        }

        [Fact]
        public async Task Day67_CommonAssertions()
        {
            await _page.SetContentAsync("""
                <title>My Page</title>
                <h1 class="big">Hello World</h1>
                <input id="i" value="abc" placeholder="p">
                <button id="d" disabled>Off</button>
                <button id="e">On</button>
                <ul><li>A</li><li>B</li></ul>
                <a id="l" href="/x">link</a>
                """);
            ILocator h1 = _page.Locator("h1");
            await Assertions.Expect(h1).ToBeVisibleAsync();
            await Assertions.Expect(h1).ToHaveTextAsync("Hello World");
            await Assertions.Expect(h1).ToContainTextAsync("World");
            await Assertions.Expect(h1).ToHaveClassAsync("big");
            await Assertions.Expect(h1).ToHaveTextAsync(new System.Text.RegularExpressions.Regex("^Hello"));
            await Assertions.Expect(_page.Locator("#i")).ToHaveValueAsync("abc");
            await Assertions.Expect(_page.Locator("#d")).ToBeDisabledAsync();
            await Assertions.Expect(_page.Locator("#e")).ToBeEnabledAsync();
            await Assertions.Expect(_page.Locator("li")).ToHaveCountAsync(2);
            await Assertions.Expect(_page.Locator("li")).ToHaveTextAsync(new[] { "A", "B" });
            await Assertions.Expect(_page.Locator("#l")).ToHaveAttributeAsync("href", "/x");
            await Assertions.Expect(_page).ToHaveTitleAsync("My Page");
            await Assertions.Expect(_page.Locator("#missing")).ToHaveCountAsync(0);
        }

        [Fact]
        public async Task Day68_AutoWaitsForElementToAppear()
        {
            await _page.SetContentAsync(DelayedHtml);
            Stopwatch sw = Stopwatch.StartNew();
            await _page.Locator("#late").ClickAsync();
            sw.Stop();
            Assert.InRange(sw.ElapsedMilliseconds, 500, 5000);
        }

        [Fact]
        public async Task Day68_AutoWaitTimesOutWithUsefulMessage()
        {
            await _page.SetContentAsync("<div>nothing</div>");
            TimeoutException ex = await Assert.ThrowsAsync<TimeoutException>(
                () => _page.Locator("#nope").ClickAsync(new() { Timeout = 1500 }));
            Console.WriteLine(ex.Message.Split('\n')[0]);
            Assert.Contains("Timeout 1500ms exceeded", ex.Message);
        }

        [Fact]
        public async Task Day68_DisabledButtonIsWaitedFor()
        {
            await _page.SetContentAsync("""
                <button id="b" disabled onclick="document.getElementById('o').textContent='clicked'">Go</button>
                <div id="o"></div>
                <script>setTimeout(()=>document.getElementById('b').disabled=false,800)</script>
                """);
            await _page.Locator("#b").ClickAsync();
            await Assertions.Expect(_page.Locator("#o")).ToHaveTextAsync("clicked");
        }

        [Fact]
        public async Task Day69_ExplicitWaits()
        {
            await _page.SetContentAsync(DelayedHtml);
            await _page.Locator("#late").WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await _page.WaitForFunctionAsync("document.getElementById('status').textContent === 'Done'");

            await _page.SetContentAsync("<div id='x'>here</div><script>setTimeout(()=>document.getElementById('x').remove(),500)</script>");
            await _page.Locator("#x").WaitForAsync(new() { State = WaitForSelectorState.Detached });
            Assert.Equal(0, await _page.Locator("#x").CountAsync());
        }

        [Fact]
        public async Task Day69_WaitForUrl()
        {
            await _page.GotoAsync("https://www.saucedemo.com");
            await _page.Locator("[data-test='username']").FillAsync("standard_user");
            await _page.Locator("[data-test='password']").FillAsync("secret_sauce");
            await _page.Locator("[data-test='login-button']").ClickAsync();
            await _page.WaitForURLAsync("**/inventory.html");
            await Assertions.Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("inventory\\.html$"));
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        [Fact]
        public async Task Day70_Timeouts()
        {
            await _page.SetContentAsync("<div>x</div>");
            _page.SetDefaultTimeout(1000);
            Stopwatch sw = Stopwatch.StartNew();
            await Assert.ThrowsAsync<TimeoutException>(() => _page.Locator("#nope").ClickAsync());
            sw.Stop();
            Assert.InRange(sw.ElapsedMilliseconds, 900, 3000);
            Console.WriteLine($"Default timeout 1000 -> failed after {sw.ElapsedMilliseconds} ms");

            sw.Restart();
            await Assert.ThrowsAsync<PlaywrightException>(() => Assertions.Expect(_page.Locator("#nope")).ToBeVisibleAsync(new() { Timeout = 700 }));
            sw.Stop();
            Assert.InRange(sw.ElapsedMilliseconds, 600, 2500);

            _page.SetDefaultNavigationTimeout(5000);
        }

        [Fact]
        public async Task Day71_NegativeAssertionsAndMessages()
        {
            await _page.SetContentAsync("<div id='a' style='display:none'>hidden</div><div id='b'>shown</div>");
            await Assertions.Expect(_page.Locator("#a")).Not.ToBeVisibleAsync();
            await Assertions.Expect(_page.Locator("#b")).Not.ToHaveTextAsync("other");
            await Assertions.Expect(_page.Locator("#gone")).Not.ToBeAttachedAsync();

            PlaywrightException ex = await Assert.ThrowsAsync<PlaywrightException>(
                () => Assertions.Expect(_page.Locator("#b")).ToHaveTextAsync("wrong", new() { Timeout = 700 }));
            Console.WriteLine(ex.Message.Split('\n')[0]);
            Assert.Contains("wrong", ex.Message);
        }
    }
}
