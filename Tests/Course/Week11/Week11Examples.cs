using Microsoft.Playwright;

namespace PlaywrightXUnitv3Learning.Tests.Course.Week11
{
    public class Week11Examples : IAsyncLifetime
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

        [Fact]
        public async Task Day61_ClickFillPress()
        {
            await _page.SetContentAsync("""
                <input id="name"><button id="b" onclick="document.getElementById('out').textContent='clicked'">Go</button>
                <div id="out"></div><input id="search" onkeydown="if(event.key==='Enter')document.getElementById('out').textContent='enter:'+this.value">
                """);
            await _page.Locator("#name").FillAsync("Alice");
            Assert.Equal("Alice", await _page.Locator("#name").InputValueAsync());

            await _page.Locator("#name").ClearAsync();
            Assert.Equal("", await _page.Locator("#name").InputValueAsync());

            await _page.Locator("#b").ClickAsync();
            await Assertions.Expect(_page.Locator("#out")).ToHaveTextAsync("clicked");

            await _page.Locator("#search").FillAsync("shoes");
            await _page.Locator("#search").PressAsync("Enter");
            await Assertions.Expect(_page.Locator("#out")).ToHaveTextAsync("enter:shoes");

            await _page.Locator("#name").PressSequentiallyAsync("abc");
            Assert.Equal("abc", await _page.Locator("#name").InputValueAsync());
        }

        [Fact]
        public async Task Day61_DoubleAndRightClick()
        {
            await _page.SetContentAsync("""
                <button id="b" ondblclick="document.getElementById('out').textContent='dbl'" oncontextmenu="document.getElementById('out').textContent='right';return false">X</button>
                <div id="out"></div>
                """);
            await _page.Locator("#b").DblClickAsync();
            await Assertions.Expect(_page.Locator("#out")).ToHaveTextAsync("dbl");
            await _page.Locator("#b").ClickAsync(new() { Button = MouseButton.Right });
            await Assertions.Expect(_page.Locator("#out")).ToHaveTextAsync("right");
        }

        [Fact]
        public async Task Day62_CheckboxRadioDropdown()
        {
            await _page.SetContentAsync("""
                <input type="checkbox" id="c1"> <input type="checkbox" id="c2" checked>
                <input type="radio" name="size" id="s" value="S"><input type="radio" name="size" id="m" value="M">
                <select id="country"><option value="us">USA</option><option value="in">India</option><option value="uk">UK</option></select>
                """);
            await _page.Locator("#c1").CheckAsync();
            await Assertions.Expect(_page.Locator("#c1")).ToBeCheckedAsync();
            await _page.Locator("#c2").UncheckAsync();
            await Assertions.Expect(_page.Locator("#c2")).Not.ToBeCheckedAsync();
            await _page.Locator("#c1").CheckAsync();
            Assert.True(await _page.Locator("#c1").IsCheckedAsync());

            await _page.Locator("#m").CheckAsync();
            await Assertions.Expect(_page.Locator("#m")).ToBeCheckedAsync();
            await Assertions.Expect(_page.Locator("#s")).Not.ToBeCheckedAsync();

            await _page.Locator("#country").SelectOptionAsync("in");
            await Assertions.Expect(_page.Locator("#country")).ToHaveValueAsync("in");
            await _page.Locator("#country").SelectOptionAsync(new SelectOptionValue { Label = "UK" });
            await Assertions.Expect(_page.Locator("#country")).ToHaveValueAsync("uk");
        }

        [Fact]
        public async Task Day63_HoverDragKeyboard()
        {
            await _page.SetContentAsync("""
                <style>#menu{display:none}#hover:hover+#menu{display:block}</style>
                <span id="hover">Hover me</span><div id="menu">Menu</div>
                <div id="drag" draggable="true" style="width:50px;height:50px;background:red"></div>
                <div id="drop" style="width:100px;height:100px;background:blue"></div>
                <div id="res"></div>
                <script>
                  const drop=document.getElementById('drop');
                  drop.addEventListener('dragover',e=>e.preventDefault());
                  drop.addEventListener('drop',e=>{e.preventDefault();document.getElementById('res').textContent='dropped'});
                </script>
                <input id="t">
                """);
            await Assertions.Expect(_page.Locator("#menu")).Not.ToBeVisibleAsync();
            await _page.Locator("#hover").HoverAsync();
            await Assertions.Expect(_page.Locator("#menu")).ToBeVisibleAsync();

            await _page.Locator("#drag").DragToAsync(_page.Locator("#drop"));
            await Assertions.Expect(_page.Locator("#res")).ToHaveTextAsync("dropped");

            await _page.Locator("#t").FocusAsync();
            await _page.Keyboard.TypeAsync("Hello");
            await _page.Keyboard.PressAsync("Shift+Home");
            await _page.Keyboard.PressAsync("Backspace");
            Assert.Equal("", await _page.Locator("#t").InputValueAsync());
        }

        [Fact]
        public async Task Day64_UploadAndDownload()
        {
            string temp = Path.Combine(Path.GetTempPath(), "week11-upload.txt");
            await File.WriteAllTextAsync(temp, "hello upload");
            try
            {
                await _page.SetContentAsync("""
                    <input type="file" id="f"><div id="name"></div>
                    <script>document.getElementById('f').addEventListener('change',e=>document.getElementById('name').textContent=e.target.files[0].name)</script>
                    <a id="dl" download="report.txt" href="data:text/plain;charset=utf-8,hello%20download">Download</a>
                    """);
                await _page.Locator("#f").SetInputFilesAsync(temp);
                await Assertions.Expect(_page.Locator("#name")).ToHaveTextAsync("week11-upload.txt");

                IDownload download = await _page.RunAndWaitForDownloadAsync(async () =>
                {
                    await _page.Locator("#dl").ClickAsync();
                });
                Assert.Equal("report.txt", download.SuggestedFilename);
                string saved = Path.Combine(Path.GetTempPath(), "week11-" + download.SuggestedFilename);
                await download.SaveAsAsync(saved);
                Assert.Equal("hello download", await File.ReadAllTextAsync(saved));
                File.Delete(saved);
            }
            finally
            {
                File.Delete(temp);
            }
        }

        [Fact]
        public async Task Day65_Dialogs()
        {
            await _page.SetContentAsync("""
                <button id="a" onclick="alert('Saved!')">Alert</button>
                <button id="c" onclick="document.getElementById('out').textContent=confirm('Sure?')?'yes':'no'">Confirm</button>
                <button id="p" onclick="document.getElementById('out').textContent=prompt('Name?')">Prompt</button>
                <div id="out"></div>
                """);

            string? alertMessage = null;
            EventHandler<IDialog> onAlert = (_, d) => { alertMessage = d.Message; _ = d.AcceptAsync(); };
            _page.Dialog += onAlert;
            await _page.Locator("#a").ClickAsync();
            _page.Dialog -= onAlert;
            Assert.Equal("Saved!", alertMessage);

            EventHandler<IDialog> onConfirm = (_, d) => { _ = d.DismissAsync(); };
            _page.Dialog += onConfirm;
            await _page.Locator("#c").ClickAsync();
            _page.Dialog -= onConfirm;
            await Assertions.Expect(_page.Locator("#out")).ToHaveTextAsync("no");

            EventHandler<IDialog> onPrompt = (_, d) => { _ = d.AcceptAsync("Bob"); };
            _page.Dialog += onPrompt;
            await _page.Locator("#p").ClickAsync();
            _page.Dialog -= onPrompt;
            await Assertions.Expect(_page.Locator("#out")).ToHaveTextAsync("Bob");
        }

        [Fact]
        public async Task Day65_UnhandledDialogIsAutoDismissed()
        {
            await _page.SetContentAsync("""
                <button id="c" onclick="document.getElementById('out').textContent=confirm('Sure?')?'yes':'no'">Confirm</button>
                <div id="out"></div>
                """);
            await _page.Locator("#c").ClickAsync();
            await Assertions.Expect(_page.Locator("#out")).ToHaveTextAsync("no");
        }
    }
}
