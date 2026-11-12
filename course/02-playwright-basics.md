# Part 2: Playwright Basics (Days 16–27)

Playwright drives a real browser from C# code. You'll open pages, find elements, type, click, and check results. Every example here runs against https://www.saucedemo.com, a practice site used by this repo.

**Before you start:** make sure `dotnet test` passes in the repo (see Setup in [COURSE.md](../COURSE.md)).

---

## Day 16: What Playwright Is

Playwright has three main objects:

- `IPlaywright`: the entry point. Creates browsers.
- `IBrowser`: a running browser, such as Chromium.
- `IPage`: one tab in that browser. Most of your code uses this.

Browser code is slow, so Playwright waits for elements automatically. You rarely need manual `Thread.Sleep` calls.

**Exercise:** open `Core/BaseTest.cs` and find where each of the three objects is created.

**Check:** you can point to the line that creates each one.

---

## Day 17: Launching a Browser

The minimal script, which you can run from a test or a console app (add the package with `dotnet add package Microsoft.Playwright`):

```csharp
using Microsoft.Playwright;

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false   // false = you can see the window
});
var page = await browser.NewPageAsync();
await page.GotoAsync("about:blank");
Console.WriteLine("Browser opened.");
```

`Headless = false` shows the window, which helps while you learn. `Headless = true` runs without a window and is needed on CI.

**Exercise:** after `GotoAsync`, wait 2 seconds with `await Task.Delay(2000);` so you can see the blank page.

**Check:** a Chromium window opens and closes.

---

## Day 18: Navigation and Page Title

`GotoAsync` opens a URL and waits for the page to load. `TitleAsync` returns the page title.

```csharp
await page.GotoAsync("https://www.saucedemo.com");
var title = await page.TitleAsync();
Console.WriteLine($"Title: {title}");
```

**Exercise:** navigate to `https://www.saucedemo.com` and print the title. Then navigate to `https://www.saucedemo.com/inventory.html` and print that title.

**Check:** the first title is `Swag Labs`. The second may be the same, so note what you see.

**Repo link:** `Core/BasePageSimple.cs` has the `GoTo` method that calls `GotoAsync`.

---

## Day 19: Locators

A locator points at an element. It doesn't find the element right away. Playwright looks it up each time you use it, which is why locators are reliable.

Common ways to write them:

```csharp
// CSS selector: matches the data-test attribute the repo uses
var username = page.Locator("[data-test='username']");

// By role and name: matches what a user sees
var loginButton = page.GetByRole(AriaRole.Button, new() { Name = "Login" });

// By label text
var passwordField = page.GetByLabel("Password");
```

Playwright uses `data-testid` by default. This repo uses `data-test`, so set it once before creating locators:

```csharp
playwright.Selectors.SetTestIdAttribute("data-test");
var field = page.GetByTestId("username");
```

`AriaRole` is in `Microsoft.Playwright`.

**Exercise:** on saucedemo.com, find the username field using `Locator("[data-test='username']")`, then print `await username.CountAsync()`.

**Check:** the count is `1`.

**Repo link:** `Pages/LoginPage.cs` uses `Page.Locator("[data-test='username']")`.

---

## Day 20: Typing and Clicking

`FillAsync` clears an input and types text. `ClickAsync` clicks an element.

```csharp
await page.GotoAsync("https://www.saucedemo.com");
await page.Locator("[data-test='username']").FillAsync("standard_user");
await page.Locator("[data-test='password']").FillAsync("secret_sauce");
await page.Locator("[data-test='login-button']").ClickAsync();
await page.WaitForURLAsync("**/inventory.html");
Console.WriteLine("Logged in.");
```

**Exercise:** log in, then click the burger menu button (`#react-burger-menu-btn`) and print whether the menu opened.

**Check:** you can see the menu open in the browser window.

---

## Day 21: Reading Text

`TextContentAsync` returns the element's text. `InnerTextAsync` returns the visible text. Use `TextContentAsync` for hidden elements.

```csharp
await page.GotoAsync("https://www.saucedemo.com");
await page.Locator("[data-test='login-button']").ClickAsync();

var error = await page.Locator("[data-test='error']").TextContentAsync();
Console.WriteLine(error);
```

**Exercise:** trigger the error message above, then print it. Next, log in with `locked_out_user` / `secret_sauce` and print that error.

**Check:** you see `Epic sadface: Username is required` and a lockout message.

---

## Day 22: Assertions with Expect

`Assertions.Expect` checks a condition and retries until it passes or times out. Prefer it over comparing text yourself, because it waits for the page to update.

```csharp
using Microsoft.Playwright;

await Assertions.Expect(page.Locator("[data-test='error']"))
    .ToContainTextAsync("Username is required");
```

Common checks:

- `ToBeVisibleAsync()`
- `ToHaveTextAsync("exact")`
- `ToContainTextAsync("part")`
- `ToHaveCountAsync(3)`

**Exercise:** after a failed login, assert the error contains `"Epic sadface"`.

**Check:** the assertion passes. Change the expected text to something wrong and confirm it fails.

**Repo link:** `Tests/LoginTests.cs` uses `Assertions.Expect(...).ToBeVisibleAsync(...)`.

---

## Day 23: Waiting Correctly

Playwright waits for elements to be ready before clicking or typing. Avoid `Task.Delay` in test code, because it makes tests slow and flaky. Wait for a condition instead:

```csharp
await page.WaitForURLAsync("**/inventory.html");           // wait for a URL
await Assertions.Expect(page.Locator(".inventory_list"))   // wait for an element
    .ToBeVisibleAsync();
```

**Exercise:** log in and wait for the inventory list with `Expect`, not with `Task.Delay`.

**Check:** the test passes without any fixed delay.

**Repo link:** `Tests/LoginTests.cs` uses `WaitForURLAsync("**/inventory.html")`.

---

## Day 24: Dropdowns and Checkboxes

```csharp
await page.GotoAsync("https://the-internet.herokuapp.com/dropdown");

var dropdown = page.Locator("#dropdown");
await dropdown.SelectOptionAsync("2");
Console.WriteLine(await dropdown.InputValueAsync());   // prints 2

await page.GotoAsync("https://the-internet.herokuapp.com/checkboxes");
var box = page.Locator("input[type='checkbox']").First;
await box.CheckAsync();
```

**Exercise:** select option `1` on the dropdown page, then check the first checkbox on the checkboxes page.

**Check:** the dropdown value prints `1`, and the checkbox is checked on screen.

---

## Day 25: Screenshots

Screenshots help you debug failing tests.

```csharp
await page.GotoAsync("https://www.saucedemo.com");
await page.ScreenshotAsync(new PageScreenshotOptions { Path = "login.png" });
```

**Exercise:** take a screenshot after a failed login and open the image.

**Check:** `login.png` exists in the folder where you ran the code and shows the error message.

---

## Day 26: Headless vs Headed

`Headless = false` shows the browser window. `Headless = true` runs without one, which CI agents need.

In the repo, `Core/BaseTest.cs` sets `Headless = false`. Later you'll make this configurable with an environment variable (Day 62). For now, try both:

```csharp
Headless = true   // run once, check that tests still pass
Headless = false  // back to visible for learning
```

**Exercise:** run `dotnet test` with `Headless = true`, then with `false`.

**Check:** the test passes both ways and you can explain why CI needs `true`.

---

## Day 27: Review and Checkpoint 2

Re-type the Day 19, 20, and 22 examples without looking.

**Checkpoint 2 project:** in the repo, add three tests in `Tests/`, each using `Expect` for assertions:
1. Valid login reaches the inventory page.
2. Wrong password shows an error containing `"Username and password do not match"`.
3. Empty username shows `"Username is required"`.

**Done when:** all three pass with `dotnet test`.

---

Previous: [Part 1](01-csharp-refresher.md) · Next: [Part 3: xUnit v3](03-xunit-v3.md)
