# Week 9: Playwright Basics

This week you learn what Playwright is, how it compares with Selenium, and how its object model fits together (`IPlaywright`, `IBrowser`, `IBrowserContext`, `IPage`). You install the browsers, write your first script, learn navigation, page title and URL, and read this repository's `Core/BaseTest.cs` line by line. Day 54 is a review with interview questions.

The examples for Days 50-52 are in the repository at `Tests/Course/Week09/Week09Examples.cs`. All five tests were run with `dotnet test --filter "FullyQualifiedName~Week09Examples"` on .NET 10 with Playwright 1.58 and passed (Chromium, Firefox and WebKit). Where this file shows an "Output" comment, it shows something the test asserts, not a version number that will differ on your machine.

---

## Day 49: What Playwright is, and how it compares with Selenium

### What Playwright is

Playwright is a browser automation library from Microsoft. It drives real browsers (Chromium, Firefox, WebKit) from code. You use it to open pages, click, type, read text and check results. It has official bindings for .NET, Java, Python and JavaScript/TypeScript. This course uses the .NET one (`Microsoft.Playwright`).

This repository uses the package `Microsoft.Playwright.Xunit.v3`. It pulls in `Microsoft.Playwright` and adds xUnit v3 helpers. Check `PlaywrightXUnitv3Learning.csproj`.

### How it talks to the browser

- Your test process starts a small Playwright **driver** (Node.js based, shipped inside the NuGet package).
- The driver talks to each browser over a persistent connection (for Chromium this is the DevTools protocol).
- Because the connection stays open, Playwright is told about page events as they happen. This is the basis of **auto-waiting** (Week 12).

Selenium WebDriver works differently: it sends one HTTP request per command to a separate driver executable (chromedriver, geckodriver), and each request returns a response.

### Playwright vs Selenium

| Topic | Playwright | Selenium |
|---|---|---|
| Waiting | Auto-waits for elements to be actionable before click/fill | You write explicit or implicit waits yourself |
| Browsers | Chromium, Firefox, WebKit bundled by Playwright | Any browser that has a driver; you manage the driver |
| Isolation | Cheap **browser contexts** (like fresh profiles) | Usually a new browser or a new session |
| Assertions | Built-in web-first assertions that retry | None; use a test framework's asserts plus your own waits |
| Network | Route, mock and observe requests natively | Limited; usually needs a proxy or DevTools access |
| Debugging | Trace Viewer, video, screenshots built in | Mostly third-party tooling |
| Languages | .NET, Java, Python, JS/TS | Many more languages |
| Maturity / ecosystem | Newer, growing fast | Very mature, huge install base, W3C standard |
| Mobile | Browser emulation of devices (not real devices) | Appium extends it to real devices |

Be fair in interviews: Selenium is not "bad". Many companies have large Selenium suites, and for those the right answer is often "keep it". Playwright is a strong choice for new web projects because of auto-waiting and speed.

### Exercise

1. Open `PlaywrightXUnitv3Learning.csproj`. Which package brings in Playwright? Which package is xUnit v3 itself?
2. Write three sentences explaining to a colleague why Playwright tests are usually less flaky than Selenium tests that use `Thread.Sleep`.

<details>
<summary>Answers</summary>

1. `Microsoft.Playwright.Xunit.v3` brings in Playwright; `xunit.v3` is the test framework.
2. Playwright waits until an element is attached, visible, stable, enabled and able to receive events before it acts, so you do not guess a sleep time. Sleeps that are too short cause failures; sleeps that are too long slow every run. Playwright retries assertions until a timeout, so it also tolerates normal page delays.

</details>

---

## Day 50: The object model: `IPlaywright`, `IBrowser`, `IBrowserContext`, `IPage`

Four interfaces form a chain. Each one creates the next:

```text
IPlaywright  ->  IBrowserType (Chromium/Firefox/Webkit)  ->  IBrowser  ->  IBrowserContext  ->  IPage
```

| Interface | What it is | Created by |
|---|---|---|
| `IPlaywright` | Entry point; holds `Chromium`, `Firefox`, `Webkit` | `Playwright.CreateAsync()` |
| `IBrowserType` | One kind of browser | `playwright.Chromium` etc. |
| `IBrowser` | One running browser process | `browserType.LaunchAsync(...)` |
| `IBrowserContext` | An isolated session: its own cookies, storage, cache | `browser.NewContextAsync()` |
| `IPage` | One tab | `context.NewPageAsync()` |

Why contexts matter: launching a browser is slow (about a second or more), creating a context is fast (milliseconds). Two contexts in the same browser do not share cookies or local storage. So one browser can serve many independent tests. In Week 13 you will use this for test isolation.

`browser.NewPageAsync()` is a shortcut that creates a context and a page in one call. It is fine for small scripts, but you cannot configure the context with it as easily, and the implicit context is closed when the page's browser closes.

### Example (verified)

```csharp
using Microsoft.Playwright;

using IPlaywright playwright = await Playwright.CreateAsync();
await using IBrowser browser = await playwright.Chromium.LaunchAsync(
    new BrowserTypeLaunchOptions { Headless = true });

IBrowserContext contextA = await browser.NewContextAsync();
IBrowserContext contextB = await browser.NewContextAsync();
IPage pageA = await contextA.NewPageAsync();
IPage pageB = await contextB.NewPageAsync();

Console.WriteLine(browser.Contexts.Count);      // 2
Console.WriteLine(contextA.Pages.Count);        // 1
Console.WriteLine(ReferenceEquals(pageA, pageB)); // False
```

This is the test `Day50_ObjectModel_PlaywrightBrowserContextPage` in `Week09Examples.cs`.

### Disposal

- `IPlaywright` implements `IDisposable`, so use `using` (or call `Dispose()`).
- `IBrowser` implements `IAsyncDisposable`, so use `await using` (or call `await browser.CloseAsync()`).
- Closing a browser closes its contexts and pages.

### Headless vs headed

`Headless = true` runs without a visible window (use this in CI). `Headless = false` shows the browser (use this while you write and debug tests).

### Exercise

1. Write a program that launches Chromium, creates **one** context with **two** pages, and prints `context.Pages.Count`.
2. In the first page, call `SetContentAsync("<h1>One</h1>")`; in the second, `SetContentAsync("<h1>Two</h1>")`. Print each page's `<h1>` text using `await page.Locator("h1").TextContentAsync()`.

<details>
<summary>Solution</summary>

```csharp
using Microsoft.Playwright;

using IPlaywright playwright = await Playwright.CreateAsync();
await using IBrowser browser = await playwright.Chromium.LaunchAsync(
    new BrowserTypeLaunchOptions { Headless = true });

IBrowserContext context = await browser.NewContextAsync();
IPage first = await context.NewPageAsync();
IPage second = await context.NewPageAsync();

await first.SetContentAsync("<h1>One</h1>");
await second.SetContentAsync("<h1>Two</h1>");

Console.WriteLine(context.Pages.Count);
Console.WriteLine(await first.Locator("h1").TextContentAsync());
Console.WriteLine(await second.Locator("h1").TextContentAsync());

// Output:
// 2
// One
// Two
```

</details>

---

## Day 51: Install browsers and write your first script

### Step 1: build the project

```bash
dotnet build
```

The build copies a script called `playwright.ps1` into the output folder.

### Step 2: install the browsers

Playwright downloads its own copies of the browsers. It does not use the Chrome installed on your machine by default.

```bash
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

Variations:

```bash
pwsh bin/Debug/net10.0/playwright.ps1 install                 # all default browsers
pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps     # also OS libraries (Linux CI)
```

You need PowerShell 7 (`pwsh`) installed. The path contains `Debug/net10.0`, so build first and adjust if you build in Release.

Browsers are stored in a shared cache folder. On Windows this is `%LOCALAPPDATA%\ms-playwright`. Each browser has a revision in its folder name (for example `chromium-1208`). Each Playwright version expects specific revisions, so after you upgrade the NuGet package, install again.

**Common error:** `Executable doesn't exist at ...` means the browser for this Playwright version is not installed. Run the install command again.

### Step 3: first script as a test

In this repo scripts are written as tests, so you can run them with `dotnet test`:

```csharp
[Fact]
public async Task Day51_FirstScript_OpensSauceDemo()
{
    using IPlaywright playwright = await Playwright.CreateAsync();
    await using IBrowser browser = await playwright.Chromium.LaunchAsync(
        new BrowserTypeLaunchOptions { Headless = true });
    IPage page = await browser.NewPageAsync();

    await page.GotoAsync("https://www.saucedemo.com");

    Assert.Equal("Swag Labs", await page.TitleAsync());
}
```

Run only this test:

```bash
dotnet test --filter "FullyQualifiedName~Day51_FirstScript"
```

### Trying all three engines (verified)

```csharp
foreach (IBrowserType type in new[] { playwright.Chromium, playwright.Firefox, playwright.Webkit })
{
    await using IBrowser browser = await type.LaunchAsync(
        new BrowserTypeLaunchOptions { Headless = true });
    IPage page = await browser.NewPageAsync();
    await page.SetContentAsync("<title>Hi</title><h1>Hello</h1>");
    Console.WriteLine($"{type.Name} {await page.TitleAsync()}");
}

// Output (browser versions are not shown here because they differ per install):
// chromium Hi
// firefox Hi
// webkit Hi
```

This needs all three browsers installed (`install` without a browser name).

### Exercise

1. Install Chromium with the script. Confirm a `chromium-*` folder exists in your Playwright cache.
2. Write a test that opens `https://www.saucedemo.com`, and asserts the title is `Swag Labs`.
3. Change `Headless = true` to `false` and watch the browser. Then change it back.

---

## Day 52: Navigation, page title, URL

### The methods

| Method / property | What it does |
|---|---|
| `await page.GotoAsync(url)` | Navigates; returns `IResponse?` |
| `page.Url` | Current URL (a property, not a method, no `await`) |
| `await page.TitleAsync()` | Page title (a method; it asks the browser) |
| `await page.GoBackAsync()` | Browser Back button |
| `await page.GoForwardAsync()` | Browser Forward button |
| `await page.ReloadAsync()` | Reload |
| `await page.WaitForURLAsync(pattern)` | Wait until the URL matches (glob, regex or predicate) |

`GotoAsync` returns an `IResponse?`. It is `null` for navigations that have no response (for example `about:blank` or navigating to the same URL with only a changed hash). For normal pages you can check `response.Ok` and `response.Status`.

### What `GotoAsync` waits for

By default `GotoAsync` waits for the `load` event. You can change this:

```csharp
await page.GotoAsync("https://www.saucedemo.com", new PageGotoOptions
{
    WaitUntil = WaitUntilState.DOMContentLoaded,
    Timeout = 30000
});
```

`WaitUntilState` values: `Load` (default), `DOMContentLoaded`, `NetworkIdle` and `Commit`. Avoid `NetworkIdle` in your own tests: Playwright's documentation discourages it, because pages with polling or analytics never go idle. Rely on auto-waiting and web-first assertions instead.

### Example (verified)

```csharp
IResponse? response = await page.GotoAsync("https://www.saucedemo.com");
Console.WriteLine(response!.Status);   // 200
Console.WriteLine(page.Url.StartsWith("https://www.saucedemo.com")); // True

await page.Locator("[data-test='username']").FillAsync("standard_user");
await page.Locator("[data-test='password']").FillAsync("secret_sauce");
await page.Locator("[data-test='login-button']").ClickAsync();
await page.WaitForURLAsync("**/inventory.html");

await page.GoBackAsync();       // page.Url no longer contains "inventory"
await page.GoForwardAsync();    // page.Url ends with "/inventory.html" again
await page.ReloadAsync();
```

This is `Day52_Navigation_TitleUrlBackForwardReload`. The `**` in `**/inventory.html` is a glob: it matches any characters, including `/`.

### Navigation errors (verified)

A navigation to a host that does not exist throws a `PlaywrightException`:

```csharp
PlaywrightException ex = await Assert.ThrowsAsync<PlaywrightException>(
    () => page.GotoAsync("https://this-host-does-not-exist.invalid"));
Assert.Contains("ERR_NAME_NOT_RESOLVED", ex.Message);
```

A server that answers with 404 or 500 does **not** throw. You get a response with `Ok == false`. Know the difference: a thrown exception means the browser could not load anything; an HTTP error status is still a loaded page.

### `page.Url` vs `WaitForURLAsync`

`page.Url` reads the URL right now. After a click that triggers navigation, the URL may not have changed yet. Use `WaitForURLAsync` (or the assertion `Expect(page).ToHaveURLAsync(...)`, Week 12) so the test waits for the change.

### Exercise

1. Navigate to saucedemo, log in, and assert that `page.Url` ends with `/inventory.html` after `WaitForURLAsync`.
2. Assert that `GotoAsync` returned a response with `Status` 200.
3. Navigate to `https://www.saucedemo.com/does-not-exist`. Print the status code. Does it throw? (Run it and see, then explain what you observed.)

---

## Day 53: Walk through `Core/BaseTest.cs`

This is the base class every test in this repo derives from. Here is the file as it exists:

```csharp
using Microsoft.Playwright;

namespace PlaywrightXUnitv3Learning.Core
{
    public abstract class BaseTest : IAsyncLifetime
    {
        protected IPage Page { get; set; } = null;
        protected IBrowser Browser { get; set; } = null;
        protected IPlaywright Playwright { get; set; } = null;

        public virtual async ValueTask InitializeAsync()
        {
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false
            });
            IBrowserContext context = await Browser.NewContextAsync();
            Page = await context.NewPageAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Page.CloseAsync();
            await Browser.CloseAsync();
            Playwright.Dispose();
        }
    }
}
```

### Line by line

- `abstract class BaseTest : IAsyncLifetime`: you cannot instantiate it; test classes inherit from it. `IAsyncLifetime` is an xUnit interface with two methods, `InitializeAsync` and `DisposeAsync`. xUnit creates a **new instance of the test class for every test method**, calls `InitializeAsync` before the test and `DisposeAsync` after it. So every test gets its own browser.
- `ValueTask`: in xUnit v3 `IAsyncLifetime` uses `ValueTask` (v2 used `Task`). You met `ValueTask` in Week 7.
- The three properties hold the Playwright objects so derived tests can use `Page`.
- `Microsoft.Playwright.Playwright.CreateAsync()` is written with the full name because the **property** named `Playwright` hides the **class** named `Playwright` inside this type. Writing `Playwright.CreateAsync()` would try to call a method on the `IPlaywright` property and fail to compile.
- `InitializeAsync` is `virtual`, so `LoginTests` can override it, call `base.InitializeAsync()` first, and then add its own setup (it builds the DI container there).
- `DisposeAsync` is not `virtual`, so a derived class cannot extend it.

### Known weaknesses (not fixed on purpose)

You asked for the course to build on the repo. These issues are real, but they are the subject of Phase 4, so this week only names them:

1. `= null` on non-nullable properties produces compiler warning `CS8625` (you see it in every build). The properties are assigned in `InitializeAsync`, so `null!` or `required`-style design would be cleaner.
2. `Headless = false` is hardcoded, so the tests open a visible window. CI needs headless; this should come from configuration (Phase 4, configuration weeks).
3. The `IBrowserContext` is a local variable and never disposed explicitly. Closing the browser does close it, but you lose the chance to save traces or videos from the context.
4. If `InitializeAsync` fails half way, `DisposeAsync` still runs and may throw a `NullReferenceException` on `Page`, which hides the real error.
5. One browser launch per test is simple but slow. Sharing the browser and creating one context per test is faster. Phase 3 (fixtures) and Phase 4 revisit this.

### Exercise

1. Run `dotnet build` and find the three `CS8625` warnings for `BaseTest.cs`. Which lines are they on?
2. Without editing `BaseTest.cs`, write a new test class that inherits from `BaseTest`, overrides `InitializeAsync`, calls the base, navigates to saucedemo, and asserts the title.
3. Explain in two sentences why `LoginTests` calls `await base.InitializeAsync()` **first**.

<details>
<summary>Answers</summary>

1. Lines 10, 11 and 12 (the three property initialisers).
2. ```csharp
   public class TitleTests : BaseTest
   {
       public override async ValueTask InitializeAsync()
       {
           await base.InitializeAsync();
           await Page.GotoAsync("https://www.saucedemo.com");
       }

       [Fact]
       public async Task Title_IsSwagLabs()
       {
           Assert.Equal("Swag Labs", await Page.TitleAsync());
       }
   }
   ```
3. `Page` is created inside the base method. If the derived class used `Page` before calling the base, `Page` would still be `null`.

</details>

---

## Day 54: Review and practice

### Recap

| Day | Topic | Key points |
|---|---|---|
| 49 | What Playwright is | Drives real browsers; auto-waiting; contexts; built-in assertions, tracing; vs Selenium trade-offs |
| 50 | Object model | `IPlaywright` -> `IBrowserType` -> `IBrowser` -> `IBrowserContext` -> `IPage`; contexts are isolated and cheap; `using`/`await using` |
| 51 | Install and first script | `playwright.ps1 install`; browsers live in a cache keyed by revision; `Headless` flag |
| 52 | Navigation | `GotoAsync` returns `IResponse?`; `page.Url` vs `WaitForURLAsync`; `WaitUntilState`; network errors throw, HTTP errors do not |
| 53 | `BaseTest` | One instance per test; `IAsyncLifetime`; `virtual InitializeAsync`; name hiding of `Playwright`; known weaknesses |

### Practice questions

1. Which object represents one tab?
2. Does `browser.NewContextAsync()` start a new browser process?
3. Is `page.Url` a method or a property? Is `page.TitleAsync()`?
4. What does `GotoAsync` return, and when can it be `null`?
5. Does `GotoAsync("https://www.saucedemo.com/missing")` throw on a 404?
6. What does `**/inventory.html` match?
7. Why does `BaseTest` write `Microsoft.Playwright.Playwright.CreateAsync()`?
8. How many browsers does xUnit launch for 10 tests that derive from `BaseTest`?

<details>
<summary>Answers</summary>

1. `IPage`.
2. No. It creates an isolated session inside the existing browser process.
3. `Url` is a property (no `await`); `TitleAsync()` is an async method.
4. An `IResponse?`; it is `null` when there is no network response (for example `about:blank`).
5. No. You get a response with `Ok == false` and `Status == 404`. It throws only when the browser cannot load the page at all (for example DNS failure).
6. Any URL that ends in `/inventory.html`, for example `https://www.saucedemo.com/inventory.html`.
7. The property `Playwright` of type `IPlaywright` hides the class name inside `BaseTest`.
8. Ten, one per test, because xUnit creates a new instance of the class per test and `InitializeAsync` launches a browser.

</details>

### Mini-program: page health check

Write a test that visits three URLs (`https://www.saucedemo.com`, `https://www.saucedemo.com/inventory.html` without logging in, and a host that does not exist) and prints for each one either `status` and `title`, or the first line of the exception message. Do not hard-code what you expect: print what you find, then explain each result.

Extension: make it a `[Theory]` with `[InlineData]` (xUnit data-driven tests come in Phase 3, so treat this as a preview).

### Interview questions

Model answers follow each question. Follow-up questions are marked **Follow-up**, and coding tasks are marked **Code**.

**1. What is Playwright and why would you choose it over Selenium?**
Answer: A library that drives Chromium, Firefox and WebKit. Main reasons: auto-waiting reduces flakiness, browser contexts give cheap isolation, built-in web-first assertions, network interception, and Trace Viewer. I would not say Selenium is bad: if a team has a large stable Selenium suite, migrating has a cost.
**Follow-up:** When would Selenium still be the better choice? Answer: Languages Playwright does not support, real-device mobile testing through Appium, or an existing large investment.

**2. Explain the object model.**
Answer: `IPlaywright` creates browser types; a browser type launches an `IBrowser`; a browser creates isolated `IBrowserContext`s; a context creates `IPage`s (tabs).
**Follow-up:** Why use several contexts instead of several browsers? Answer: Launching a browser is slow and memory heavy; contexts give isolated cookies and storage at a fraction of the cost.

**3. What is a browser context?**
Answer: An isolated, incognito-like session with its own cookies, local storage and cache. Tests that each use their own context cannot affect each other through browser state.

**4. How does xUnit v3 manage the lifecycle in this repo?**
Answer: `BaseTest` implements `IAsyncLifetime`. xUnit creates a new class instance per test, runs `InitializeAsync` (launch browser, create page), the test, then `DisposeAsync` (close everything).
**Follow-up:** What are the downsides? Answer: Slow because each test launches a browser; hardcoded headed mode; no context disposal or tracing hook. (Phase 4 fixes these.)

**5. What is the difference between `page.Url` and `WaitForURLAsync`?**
Answer: `Url` is a snapshot of the current URL. `WaitForURLAsync` waits until the URL matches, which is what you need right after an action that triggers navigation.

**6. Does Playwright throw for an HTTP 500?**
Answer: No. `GotoAsync` returns an `IResponse` with `Ok == false`. It throws for failures such as DNS errors, connection refused or timeouts.

**7. Why do you install browsers separately, and what breaks after a package upgrade?**
Answer: Playwright uses its own pinned browser builds so tests behave the same everywhere. A new Playwright version expects new browser revisions; until you re-run `playwright.ps1 install`, tests fail with "Executable doesn't exist".

**8. Headless vs headed?**
Answer: Headless has no visible window: faster, needed for CI. Headed is for developing and debugging. It should be a configuration value, not hardcoded.

**9. Code:** Write a test that opens saucedemo and asserts the title.
```csharp
[Fact]
public async Task Title_IsSwagLabs()
{
    using IPlaywright playwright = await Playwright.CreateAsync();
    await using IBrowser browser = await playwright.Chromium.LaunchAsync(
        new BrowserTypeLaunchOptions { Headless = true });
    IPage page = await browser.NewPageAsync();
    await page.GotoAsync("https://www.saucedemo.com");
    Assert.Equal("Swag Labs", await page.TitleAsync());
}
```

**10. Code:** In `BaseTest`, `Page = await context.NewPageAsync();` is the last line of `InitializeAsync`. What happens if `Browser.NewContextAsync()` throws?
Answer: `Page` stays `null`, xUnit then calls `DisposeAsync`, and `Page.CloseAsync()` throws a `NullReferenceException` that hides the original failure. A safer `DisposeAsync` would null-check each object.
