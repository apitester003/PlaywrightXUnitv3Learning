using Microsoft.Playwright;

namespace PlaywrightXUnitv3Learning.Tests.Course.Week14
{
    public class Week14Examples : IAsyncLifetime
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
        public async Task Day79_IFrames_FrameLocator()
        {
            await _page.SetContentAsync("""
                <h1>Outer</h1>
                <iframe id="f1" name="payment" srcdoc="<input id='card' placeholder='Card'><button id='pay' onclick=&quot;document.getElementById('msg').textContent='paid'&quot;>Pay</button><div id='msg'></div>"></iframe>
                <iframe id="f2" srcdoc="<iframe id='inner' srcdoc='<p id=deep>Deep text</p>'></iframe>"></iframe>
                """);

            IFrameLocator frame = _page.FrameLocator("#f1");
            await frame.Locator("#card").FillAsync("4111");
            await frame.GetByRole(AriaRole.Button, new() { Name = "Pay" }).ClickAsync();
            await Assertions.Expect(frame.Locator("#msg")).ToHaveTextAsync("paid");

            Assert.Equal(0, await _page.Locator("#card").CountAsync());

            IFrameLocator nested = _page.FrameLocator("#f2").FrameLocator("#inner");
            await Assertions.Expect(nested.Locator("#deep")).ToHaveTextAsync("Deep text");

            IFrame? byName = _page.Frame("payment");
            Assert.NotNull(byName);
            Assert.Equal(1, await byName.Locator("#card").CountAsync());
            Assert.Equal(4, _page.Frames.Count);
        }

        [Fact]
        public async Task Day80_ShadowDom()
        {
            await _page.SetContentAsync("""
                <my-card id="host"></my-card>
                <closed-card id="chost"></closed-card>
                <script>
                  const h = document.getElementById('host').attachShadow({mode:'open'});
                  h.innerHTML = '<button id="b">Shadow Button</button><span class="t">inside</span>';
                  const c = document.getElementById('chost').attachShadow({mode:'closed'});
                  c.innerHTML = '<span class="t">closed inside</span>';
                </script>
                """);

            Assert.Equal(1, await _page.Locator("#b").CountAsync());
            Assert.Equal(1, await _page.Locator("my-card button").CountAsync());
            await Assertions.Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Shadow Button" })).ToBeVisibleAsync();
            await Assertions.Expect(_page.GetByText("inside", new() { Exact = true })).ToBeVisibleAsync();

            Assert.Equal(0, await _page.Locator("closed-card .t").CountAsync());
        }

        private const string TableHtml = """
            <table id="t"><thead><tr><th>Name</th><th>Role</th><th>Action</th></tr></thead>
            <tbody>
              <tr><td>Ann</td><td>Admin</td><td><button>Edit</button></td></tr>
              <tr><td>Bob</td><td>User</td><td><button>Edit</button></td></tr>
              <tr><td>Cid</td><td>User</td><td><button>Edit</button></td></tr>
            </tbody></table>
            """;

        [Fact]
        public async Task Day81_DynamicTable()
        {
            await _page.SetContentAsync(TableHtml);
            ILocator rows = _page.Locator("#t tbody tr");
            await Assertions.Expect(rows).ToHaveCountAsync(3);

            IReadOnlyList<string> names = await _page.Locator("#t tbody tr td:nth-child(1)").AllTextContentsAsync();
            Assert.Equal(new[] { "Ann", "Bob", "Cid" }, names);

            ILocator bob = rows.Filter(new() { HasText = "Bob" });
            Assert.Equal("User", await bob.Locator("td").Nth(1).TextContentAsync());
            await Assertions.Expect(bob.GetByRole(AriaRole.Button, new() { Name = "Edit" })).ToBeVisibleAsync();

            Assert.Equal(2, await rows.Filter(new() { HasText = "User" }).CountAsync());
        }

        [Fact]
        public async Task Day81_Pagination()
        {
            await _page.SetContentAsync("""
                <ul id="list"></ul><button id="prev">Prev</button><span id="info"></span><button id="next">Next</button>
                <script>
                  const data = Array.from({length: 7}, (_, i) => 'Item ' + (i + 1));
                  let page = 0; const size = 3;
                  function render() {
                    const start = page * size;
                    document.getElementById('list').innerHTML = data.slice(start, start + size).map(x => '<li>' + x + '</li>').join('');
                    document.getElementById('info').textContent = 'Page ' + (page + 1);
                    document.getElementById('prev').disabled = page === 0;
                    document.getElementById('next').disabled = (page + 1) * size >= data.length;
                  }
                  document.getElementById('next').onclick = () => { page++; render(); };
                  document.getElementById('prev').onclick = () => { page--; render(); };
                  render();
                </script>
                """);

            List<string> all = new();
            while (true)
            {
                all.AddRange(await _page.Locator("#list li").AllTextContentsAsync());
                ILocator next = _page.GetByRole(AriaRole.Button, new() { Name = "Next" });
                if (await next.IsDisabledAsync()) break;
                await next.ClickAsync();
            }
            Assert.Equal(7, all.Count);
            Assert.Equal("Item 7", all[^1]);
        }

        [Fact]
        public async Task Day82_InfiniteScroll()
        {
            await _page.SetContentAsync("""
                <style>#box{height:200px;overflow:auto}.row{height:50px}</style>
                <div id="box"><div id="rows"></div></div>
                <script>
                  const box = document.getElementById('box'), rows = document.getElementById('rows');
                  let n = 0;
                  function load() { for (let i = 0; i < 10 && n < 30; i++) { const d = document.createElement('div'); d.className = 'row'; d.textContent = 'Row ' + (++n); rows.appendChild(d); } }
                  box.addEventListener('scroll', () => { if (box.scrollTop + box.clientHeight >= box.scrollHeight - 5) setTimeout(load, 200); });
                  load();
                </script>
                """);

            ILocator rowsLocator = _page.Locator(".row");
            await Assertions.Expect(rowsLocator).ToHaveCountAsync(10);

            int previous = 0;
            for (int i = 0; i < 10; i++)
            {
                int count = await rowsLocator.CountAsync();
                if (count == previous) break;
                previous = count;
                await rowsLocator.Last.ScrollIntoViewIfNeededAsync();
                await _page.Locator("#box").EvaluateAsync("e => e.scrollTop = e.scrollHeight");
                await _page.WaitForTimeoutAsync(400);
            }
            await Assertions.Expect(rowsLocator).ToHaveCountAsync(30);
        }

        [Fact]
        public async Task Day83_DatePickerAndAutocomplete()
        {
            await _page.SetContentAsync("""
                <input type="date" id="d">
                <input id="ac" autocomplete="off"><ul id="sug"></ul>
                <script>
                  const cities = ['London', 'Lisbon', 'Los Angeles', 'Paris'];
                  const ac = document.getElementById('ac'), sug = document.getElementById('sug');
                  ac.addEventListener('input', () => {
                    const v = ac.value.toLowerCase();
                    sug.innerHTML = v ? cities.filter(c => c.toLowerCase().startsWith(v)).map(c => '<li role="option">' + c + '</li>').join('') : '';
                  });
                  sug.addEventListener('click', e => { ac.value = e.target.textContent; sug.innerHTML = ''; });
                </script>
                """);

            await _page.Locator("#d").FillAsync("2030-12-25");
            Assert.Equal("2030-12-25", await _page.Locator("#d").InputValueAsync());

            await _page.Locator("#ac").PressSequentiallyAsync("Lo");
            await Assertions.Expect(_page.GetByRole(AriaRole.Option)).ToHaveCountAsync(2);
            await _page.GetByRole(AriaRole.Option, new() { Name = "Los Angeles" }).ClickAsync();
            await Assertions.Expect(_page.Locator("#ac")).ToHaveValueAsync("Los Angeles");
            await Assertions.Expect(_page.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);
        }
    }
}
