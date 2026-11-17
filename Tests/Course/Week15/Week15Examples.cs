using System.Text.Json;
using Microsoft.Playwright;

namespace PlaywrightXUnitv3Learning.Tests.Course.Week15
{
    public class Week15Examples : IAsyncLifetime
    {
        private IPlaywright _playwright = null!;
        private IBrowser _browser = null!;
        private IBrowserContext _context = null!;
        private IPage _page = null!;
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "week15-" + Guid.NewGuid().ToString("N"));

        public async ValueTask InitializeAsync()
        {
            Directory.CreateDirectory(_tempDir);
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            _context = await _browser.NewContextAsync(new() { RecordVideoDir = null });
            _page = await _context.NewPageAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _browser.CloseAsync();
            _playwright.Dispose();
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
        }

        private const string AppHtml = """
            <html><head><title>Fake App</title></head><body>
            <ul id="users"></ul><div id="error"></div><img id="logo" src="https://app.test/logo.png">
            <script>
              fetch('https://app.test/api/users')
                .then(r => { if (!r.ok) throw new Error('HTTP ' + r.status); return r.json(); })
                .then(d => { document.getElementById('users').innerHTML = d.map(u => '<li>' + u.name + '</li>').join(''); })
                .catch(e => { document.getElementById('error').textContent = 'Failed: ' + e.message; });
            </script></body></html>
            """;

        private async Task ServeAppAsync(Func<IRoute, Task> apiHandler)
        {
            await _context.RouteAsync("https://app.test/", r => r.FulfillAsync(new() { ContentType = "text/html", Body = AppHtml }));
            await _context.RouteAsync("https://app.test/logo.png", r => r.FulfillAsync(new() { ContentType = "image/png", Body = "" }));
            await _context.RouteAsync("https://app.test/api/users", apiHandler);
        }

        private static Task Json(IRoute route, string json, int status = 200) =>
            route.FulfillAsync(new()
            {
                Status = status,
                ContentType = "application/json",
                Headers = new Dictionary<string, string> { ["Access-Control-Allow-Origin"] = "*" },
                Body = json
            });

        [Fact]
        public async Task Day85_MockResponse()
        {
            await ServeAppAsync(r => Json(r, """[{"name":"Mock Ann"},{"name":"Mock Bob"}]"""));
            await _page.GotoAsync("https://app.test/");
            await Assertions.Expect(_page.Locator("#users li")).ToHaveTextAsync(new[] { "Mock Ann", "Mock Bob" });
        }

        [Fact]
        public async Task Day85_MockServerError()
        {
            await ServeAppAsync(r => Json(r, """{"error":"boom"}""", 500));
            await _page.GotoAsync("https://app.test/");
            await Assertions.Expect(_page.Locator("#error")).ToHaveTextAsync("Failed: HTTP 500");
        }

        [Fact]
        public async Task Day85_AbortRequest()
        {
            await ServeAppAsync(r => r.AbortAsync());
            await _page.GotoAsync("https://app.test/");
            await Assertions.Expect(_page.Locator("#error")).ToContainTextAsync("Failed");
        }

        [Fact]
        public async Task Day85_BlockImagesByType()
        {
            int blocked = 0;
            await _context.RouteAsync("**/*", async r =>
            {
                if (r.Request.ResourceType == "image") { blocked++; await r.AbortAsync(); }
                else if (r.Request.Url == "https://app.test/") await r.FulfillAsync(new() { ContentType = "text/html", Body = AppHtml });
                else await Json(r, "[]");
            });
            await _page.GotoAsync("https://app.test/");
            await Assertions.Expect(_page.Locator("#users")).ToBeAttachedAsync();
            Assert.True(blocked >= 1);
        }

        [Fact]
        public async Task Day86_ModifyRealResponse()
        {
            await _page.RouteAsync("**/inventory.html", r => r.ContinueAsync());
            await _page.GotoAsync("https://www.saucedemo.com");
            await _page.RouteAsync("**/*.css", async r =>
            {
                IAPIResponse response = await r.FetchAsync();
                string body = await response.TextAsync();
                await r.FulfillAsync(new() { Response = response, Body = body + " body{background:rgb(255,0,0)!important}" });
            });
            await _page.ReloadAsync();
            string color = await _page.EvaluateAsync<string>("getComputedStyle(document.body).backgroundColor");
            Assert.Equal("rgb(255, 0, 0)", color);
        }

        [Fact]
        public async Task Day86_ObserveRequestsAndResponses()
        {
            List<string> requests = new();
            List<int> statuses = new();
            _page.Request += (_, r) => requests.Add(r.Method + " " + r.Url);
            _page.Response += (_, r) => statuses.Add(r.Status);

            await _page.GotoAsync("https://www.saucedemo.com");
            Assert.Contains(requests, x => x.StartsWith("GET https://www.saucedemo.com"));
            Assert.Contains(200, statuses);
        }

        [Fact]
        public async Task Day86_ModifyRequestHeaders()
        {
            string? seen = null;
            await _page.RouteAsync("https://www.saucedemo.com/**", async r =>
            {
                Dictionary<string, string> headers = new(r.Request.Headers) { ["x-course"] = "week15" };
                seen = headers["x-course"];
                await r.ContinueAsync(new() { Headers = headers });
            });
            await _page.GotoAsync("https://www.saucedemo.com");
            Assert.Equal("week15", seen);
        }

        [Fact]
        public async Task Day87_WaitForResponse()
        {
            await ServeAppAsync(r => Json(r, """[{"name":"Ann"}]"""));
            IResponse response = await _page.RunAndWaitForResponseAsync(
                async () => await _page.GotoAsync("https://app.test/"),
                r => r.Url.EndsWith("/api/users") && r.Status == 200);

            JsonElement json = await response.JsonAsync() ?? default;
            Assert.Equal("Ann", json[0].GetProperty("name").GetString());
        }

        [Fact]
        public async Task Day87_WaitForRequest()
        {
            await ServeAppAsync(r => Json(r, "[]"));
            IRequest request = await _page.RunAndWaitForRequestAsync(
                async () => await _page.GotoAsync("https://app.test/"),
                "**/api/users");
            Assert.Equal("GET", request.Method);
        }

        [Fact]
        public async Task Day88_Trace()
        {
            string zip = Path.Combine(_tempDir, "trace.zip");
            await _context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = false });
            await _page.GotoAsync("https://www.saucedemo.com");
            await _page.Locator("[data-test='username']").FillAsync("standard_user");
            await _context.Tracing.StopAsync(new() { Path = zip });

            Assert.True(File.Exists(zip));
            Assert.True(new FileInfo(zip).Length > 1000);
        }

        [Fact]
        public async Task Day89_ScreenshotsAndConsole()
        {
            string png = Path.Combine(_tempDir, "page.png");
            string elementPng = Path.Combine(_tempDir, "el.png");
            List<string> console = new();
            List<string> errors = new();
            _page.Console += (_, m) => console.Add(m.Type + ":" + m.Text);
            _page.PageError += (_, e) => errors.Add(e);

            await _page.SetContentAsync("<h1 id='h'>Hi</h1><script>console.log('hello log');console.warn('careful');setTimeout(()=>{throw new Error('kaboom')},50)</script>");
            await _page.WaitForFunctionAsync("true");
            await Assertions.Expect(_page.Locator("#h")).ToBeVisibleAsync();
            await _page.WaitForTimeoutAsync(300);

            await _page.ScreenshotAsync(new() { Path = png, FullPage = true });
            await _page.Locator("#h").ScreenshotAsync(new() { Path = elementPng });

            Assert.True(File.Exists(png) && new FileInfo(png).Length > 0);
            Assert.True(File.Exists(elementPng) && new FileInfo(elementPng).Length > 0);
            Assert.Contains("log:hello log", console);
            Assert.Contains("warning:careful", console);
            Assert.Contains(errors, e => e.Contains("kaboom"));
        }

        [Fact]
        public async Task Day89_Video()
        {
            string dir = Path.Combine(_tempDir, "videos");
            IBrowserContext ctx = await _browser.NewContextAsync(new() { RecordVideoDir = dir });
            IPage page = await ctx.NewPageAsync();
            await page.SetContentAsync("<h1>video</h1>");
            await ctx.CloseAsync();

            string[] files = Directory.GetFiles(dir, "*.webm");
            Assert.Single(files);
        }
    }
}
