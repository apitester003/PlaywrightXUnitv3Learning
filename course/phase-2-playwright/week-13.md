# Week 13: Contexts and Sessions

This week you learn how browser contexts isolate tests, how to work with several tabs and popups, how to use cookies and storage state to reuse a login, how to set viewport, locale, geolocation and permissions, and how to choose headless or headed mode, `SlowMo` and the three browser engines. Day 78 is a review with interview questions.

The examples are in `Tests/Course/Week13/Week13Examples.cs`. All 9 tests were run with `dotnet test --filter "FullyQualifiedName~Week13Examples"` on .NET 10 with Playwright 1.58 and passed. Most of them use https://www.saucedemo.com, because cookies and storage need a real website origin. They need an internet connection.

Shared helper used in several examples:

```csharp
private static async Task LoginAsync(IPage page)
{
    await page.GotoAsync("https://www.saucedemo.com");
    await page.Locator("[data-test='username']").FillAsync("standard_user");
    await page.Locator("[data-test='password']").FillAsync("secret_sauce");
    await page.Locator("[data-test='login-button']").ClickAsync();
    await page.WaitForURLAsync("**/inventory.html");
}
```

---

## Day 73: Browser context isolation

A context is an isolated session inside one browser: its own cookies, local storage, session storage and cache. Two contexts do not see each other's data.

### Proof (verified in `Day73_ContextsAreIsolated`)

```csharp
IBrowserContext a = await browser.NewContextAsync();
IBrowserContext b = await browser.NewContextAsync();
IPage pageA = await a.NewPageAsync();
IPage pageB = await b.NewPageAsync();

await LoginAsync(pageA);                                              // logged in, context A only
await pageB.GotoAsync("https://www.saucedemo.com/inventory.html");    // context B is not logged in

Assert.EndsWith("/inventory.html", pageA.Url);
await Assertions.Expect(pageB.Locator("[data-test='error']")).ToBeVisibleAsync();   // saucedemo shows an error

Assert.NotEmpty(await a.CookiesAsync());   // A has cookies
Assert.Empty(await b.CookiesAsync());      // B has none
```

Context B is not logged in: saucedemo shows its error message instead of the inventory.

### Why this matters for test design

- **Isolation:** one test's login, cart or cookies cannot leak into another test.
- **Speed:** creating a context takes much less time than launching a browser.
- **Parallelism:** tests can run side by side in separate contexts without interfering.
- **Multi-user scenarios:** two contexts can be two different users in the same test (for example a chat between two users, or admin and customer).

### Closing

`await context.CloseAsync()` closes its pages. Closing the browser closes all contexts. In `BaseTest` (Day 53) the context is never closed explicitly; closing the browser takes care of it.

### Exercise

1. Write a test where context A is logged in as `standard_user` and context B is not, and prove the difference.
2. Open two contexts and add an item to the cart in the first one. Open the cart in the second and check that it is empty.
3. Explain why one browser with many contexts is usually better than many browsers.

---

## Day 74: Multiple tabs and popups

### A link that opens a new tab

Wait for the new page **while** you click:

```csharp
IPage newTab = await context.RunAndWaitForPageAsync(async () =>
{
    await page.Locator("#l").ClickAsync();
});
await newTab.WaitForLoadStateAsync();

Assert.Equal(2, context.Pages.Count);
Assert.Equal("Swag Labs", await newTab.TitleAsync());
await newTab.CloseAsync();
Assert.Single(context.Pages);
```

Here the link was `<a target="_blank" href="https://www.saucedemo.com">`. `RunAndWaitForPageAsync` is on the **context**: it catches any new page opened in that context.

### A page opened with `window.open`

```csharp
IPage popup = await page.RunAndWaitForPopupAsync(async () =>
{
    await page.Locator("#b").ClickAsync();
});
await popup.WaitForLoadStateAsync();
```

`RunAndWaitForPopupAsync` is on the **page**: it catches popups opened by that page.

Both patterns are verified in `Day74_NewTabAndPopup` and `Day74_WindowOpenPopup`.

### A thing that went wrong while writing this week

The first version of these two tests opened a `data:` URL (a page defined inline in the link). Both tests failed with `Timeout 30000ms exceeded while waiting for event "Page"` (and `"Popup"`). Switching the link to a real `https://` URL made them pass. This course did not investigate the cause, so the lesson is practical: if a new-tab test times out, check what URL the link points to, and use a real, loadable URL in your examples.

### Always start waiting before the click

As with downloads (Week 11), if you click first and then wait, the page may open before you start listening. The `RunAndWait...` methods start listening first.

### Switching between tabs

Each tab is its own `IPage` object. Keep the reference you get from `RunAndWaitForPageAsync` and call methods on it. `context.Pages` lists all open pages. `await page.BringToFrontAsync()` activates a tab.

### Exercise

1. On any page, click a link that opens in a new tab and assert the new tab's title.
2. Close the new tab and assert you are back to one page.
3. When do you use `RunAndWaitForPopupAsync` and when `RunAndWaitForPageAsync`?

<details>
<summary>Answers</summary>

3. Popup: the page itself opened the new window (`window.open`, a `target=_blank` link from that page). Page: any new page in the whole context, regardless of who opened it.

</details>

---

## Day 75: Cookies, storage state, reusing login

### Cookies

```csharp
await context.AddCookiesAsync(new[]
{
    new Cookie { Name = "session-username", Value = "standard_user", Url = "https://www.saucedemo.com" }
});
IPage page = await context.NewPageAsync();
await page.GotoAsync("https://www.saucedemo.com/inventory.html");
await Assertions.Expect(page.Locator(".inventory_list")).ToBeVisibleAsync();   // logged in without the form

await context.ClearCookiesAsync();
Assert.Empty(await context.CookiesAsync());
```

This works because the test set a cookie named `session-username` that saucedemo accepts as a login. That is a property of this practice site, not a general rule. Never assume a real application can be logged in this way.

### Storage state: log in once, reuse many times

Logging in through the form in every test is slow. Instead, log in once, save the **storage state** (cookies and local storage) to a file, and start other contexts from that file.

Save it (verified in `Day75_StorageStateReuseLogin`):

```csharp
IBrowserContext first = await browser.NewContextAsync();
IPage p1 = await first.NewPageAsync();
await LoginAsync(p1);
await first.StorageStateAsync(new() { Path = "state.json" });
await first.CloseAsync();
```

Reuse it:

```csharp
IBrowserContext second = await browser.NewContextAsync(new() { StorageStatePath = "state.json" });
IPage p2 = await second.NewPageAsync();
await p2.GotoAsync("https://www.saucedemo.com/inventory.html");
await Assertions.Expect(p2.Locator(".inventory_list")).ToBeVisibleAsync();
```

The test passes: the second context never touched the login form.

### Rules for storage state files

- They contain **session secrets**. Do not commit them to Git. Add `*.json` state files (or a `.auth/` folder) to `.gitignore`.
- They expire. If the app's session expires, regenerate the file.
- One file per user role (admin, customer, ...).
- The file is only valid for the application and environment where you created it.

In Phase 4 you will use this in the framework (a fixture that creates the state once per run). For now it is just the mechanism.

### Exercise

1. Save the storage state after login, then start a new context from it and open the cart page directly.
2. Delete the state file and show what happens when `StorageStatePath` points to a missing file. (Run it and note the exception.)
3. Why should a state file never be committed to a repository?

---

## Day 76: Viewport, permissions, geolocation

### Viewport, locale and timezone (verified in `Day76_ViewportAndEmulation`)

```csharp
IBrowserContext context = await browser.NewContextAsync(new()
{
    ViewportSize = new ViewportSize { Width = 375, Height = 667 },
    Locale = "de-DE",
    TimezoneId = "Europe/Berlin"
});
IPage page = await context.NewPageAsync();
await page.GotoAsync("https://www.saucedemo.com");

await page.EvaluateAsync<int>("window.innerWidth");      // 375
await page.EvaluateAsync<int>("window.innerHeight");     // 667
await page.EvaluateAsync<string>("navigator.language");   // starts with "de"

await page.SetViewportSizeAsync(1280, 720);                // change it later
await page.EvaluateAsync<int>("window.innerWidth");       // 1280
```

`EvaluateAsync<T>` runs a JavaScript expression in the page and returns the result as `T`. Use it sparingly: prefer locators and assertions for normal checks.

### Geolocation and permissions (verified in `Day76_GeolocationAndPermissions`)

```csharp
IBrowserContext context = await browser.NewContextAsync(new()
{
    Geolocation = new Geolocation { Latitude = 52.52f, Longitude = 13.405f },
    Permissions = new[] { "geolocation" }
});
IPage page = await context.NewPageAsync();
await page.GotoAsync("https://www.saucedemo.com");

double lat = await page.EvaluateAsync<double>(
    "new Promise(r => navigator.geolocation.getCurrentPosition(p => r(p.coords.latitude)))");
// lat is about 52.52
```

Without `Permissions = ["geolocation"]` the browser would not grant location access. Without a `Geolocation` value, there is nothing to report.

### Why test with different viewports?

Responsive layouts hide or move elements on small screens (menus become a hamburger icon). A test that passes at 1280 px can fail at 375 px.

### Exercise

1. Open saucedemo with a 375 x 667 viewport and take a screenshot (`await page.ScreenshotAsync(new() { Path = "mobile.png" })`). Compare it with a 1280 x 720 screenshot.
2. Set a context with locale `fr-FR` and print `navigator.language`.
3. Explain what happens if you ask for geolocation without granting the permission.

---

## Day 77: Headless vs. headed, `SlowMo`, Chromium/Firefox/WebKit

### Launch options

```csharp
await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false,   // show the window (default is true)
    SlowMo = 500        // wait 500 ms between every Playwright operation
});
```

- **Headless** (default): no window. Use this in CI and for normal runs.
- **Headed**: use while writing and debugging. `BaseTest` is currently hard-coded to `Headless = false`, which is why a window opens when you run the tests (Day 53).
- **SlowMo**: slows every operation so you can watch what happens. It is for debugging only; never commit a test run with it enabled.

### The three engines

| Engine | Property | Real-world match |
|---|---|---|
| Chromium | `playwright.Chromium` | Chrome, Edge |
| Firefox | `playwright.Firefox` | Firefox |
| WebKit | `playwright.Webkit` | Safari's engine |

Note the capitalization: `Webkit`, not `WebKit`, in the .NET API.

The test `Day77_SlowMoAndOtherBrowsers` launches Firefox and WebKit headless with `SlowMo = 50`, opens saucedemo and asserts the login button is visible. It passed on this machine.

### Device emulation (verified in `Day77_DeviceEmulation`)

Playwright has a catalog of device descriptors:

```csharp
IBrowserContext context = await browser.NewContextAsync(playwright.Devices["iPhone 13"]);
IPage page = await context.NewPageAsync();
await page.GotoAsync("https://www.saucedemo.com");
// navigator.userAgent contains "iPhone"
```

A descriptor sets viewport, user agent, touch support and device scale factor. This is **emulation** inside the browser, not a real phone.

### Using the installed Chrome or Edge instead

`LaunchAsync(new() { Channel = "chrome" })` uses a locally installed Chrome. This course did not test it, so confirm it works on your machine before relying on it.

### Exercise

1. Run the same test on Chromium, Firefox and WebKit. Does it behave the same everywhere?
2. Use `SlowMo = 1000` with `Headless = false` and watch a login.
3. Run a test with the "iPhone 13" descriptor and take a screenshot.

---

## Day 78: Review and practice

### Recap

| Day | Topic | Key points |
|---|---|---|
| 73 | Context isolation | Separate cookies and storage; cheap to create; basis of test independence |
| 74 | Tabs and popups | `RunAndWaitForPageAsync` on the context; `RunAndWaitForPopupAsync` on the page; start waiting before the click |
| 75 | Cookies and storage state | `AddCookiesAsync`; `StorageStateAsync(Path)` and `StorageStatePath`; never commit state files |
| 76 | Viewport and emulation | `ViewportSize`, `Locale`, `TimezoneId`; `Geolocation` needs `Permissions` |
| 77 | Launch modes and engines | `Headless`, `SlowMo`, Chromium/Firefox/Webkit, `Devices[...]` |

### Practice questions

1. What does a browser context isolate?
2. Which object has `RunAndWaitForPageAsync`, and which has `RunAndWaitForPopupAsync`?
3. How do you reuse a login across tests?
4. Why shouldn't you commit a storage state file?
5. Which option shows the browser window?
6. What must you set besides `Geolocation` to make location available to a page?
7. What is the property name for the Safari engine in the .NET API?
8. Is device emulation the same as testing on a real phone?

<details>
<summary>Answers</summary>

1. Cookies, local and session storage, cache, permissions.
2. Context has `RunAndWaitForPageAsync`; page has `RunAndWaitForPopupAsync`.
3. Save `StorageStateAsync(Path = file)` once after login, then create contexts with `StorageStatePath = file`.
4. It contains session cookies and tokens, which are secrets.
5. `Headless = false`.
6. `Permissions = new[] { "geolocation" }`.
7. `playwright.Webkit`.
8. No. It emulates viewport, user agent and touch in a desktop browser engine.

</details>

### Mini-program: two users, one test

Use two contexts to simulate two users on saucedemo: `standard_user` in one context and `locked_out_user` in the other. In one test, log both in and assert that the first reaches the inventory page while the second sees the locked-out error message (find the `data-test` attribute of the error message yourself). Then save the storage state of the first user to a file and prove that a third context started from that file lands on the inventory page without using the form.

### Interview questions

**1. What is a browser context, and why do tests use one per test?**
Answer: An isolated session in one browser with its own cookies, storage and cache. One per test gives independence without paying for a new browser launch.
**Follow-up:** How would you test a chat between two users? Answer: Two contexts, one per user, in the same test.

**2. How do you speed up tests that all need a logged-in user?**
Answer: Log in once, save the storage state to a file, and create each test's context from that file. Keep the file out of source control and regenerate it when the session expires.
**Follow-up:** What can go wrong? Answer: The session expires, the file is for the wrong environment, or tests that change user state affect each other.

**3. How do you handle a link that opens in a new tab?**
Answer: Start waiting for the new page and click inside `RunAndWaitForPageAsync` (or `RunAndWaitForPopupAsync` on the page), then use the returned `IPage`.

**4. How would you run the same suite on three browsers?**
Answer: Parameterize the browser type (Chromium, Firefox, Webkit) through configuration or a test data source. Be aware that some behaviour differs between engines, and the browsers must be installed.

**5. How do you test a responsive layout?**
Answer: Set `ViewportSize` on the context (or use a device descriptor) and assert on the elements that change, for example the visible menu button.

**6. How do you test a feature that needs the user's location?**
Answer: Set `Geolocation` and `Permissions = ["geolocation"]` on the context.

**7. What is `SlowMo` for, and where should it never be used?**
Answer: It adds a delay between operations so a human can follow a test while debugging. Never in committed configuration or CI.

**8. Why does `BaseTest` open a visible browser, and why is that a problem?**
Answer: It hard-codes `Headless = false`. CI machines usually have no display, and visible windows slow the run. It should be configurable.

**9. Code:** Create a context with a 375 x 667 viewport and the "iPhone 13" descriptor is not needed; just the viewport.
```csharp
IBrowserContext context = await browser.NewContextAsync(new()
{
    ViewportSize = new ViewportSize { Width = 375, Height = 667 }
});
```

**10. Code:** Reuse a saved login.
```csharp
IBrowserContext context = await browser.NewContextAsync(new() { StorageStatePath = "state.json" });
IPage page = await context.NewPageAsync();
await page.GotoAsync("https://www.saucedemo.com/inventory.html");
```
