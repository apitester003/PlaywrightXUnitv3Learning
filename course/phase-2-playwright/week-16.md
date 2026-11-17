# Week 16: End-to-End on saucedemo

This week you apply everything from Phase 2 to a real site, https://www.saucedemo.com: login scenarios, the inventory (sorting and the cart), the checkout flow, and negative scenarios. Day 95 is the Phase 2 project (10 UI tests). Day 96 is the Phase 2 interview Q&A and checkpoint.

The examples are in `Tests/Course/Week16/Week16Examples.cs`. All 14 tests were run with `dotnet test --filter "FullyQualifiedName~Week16Examples"` on .NET 10 with Playwright 1.58. They passed on two consecutive full runs. They need an internet connection. If saucedemo changes its pages, some locators may stop working. That is a normal part of UI testing, and it is also why you read failure messages carefully.

## How the tests are set up

Each test class instance creates its own Playwright, browser, context and page, and closes them afterwards (the same lifecycle idea as `BaseTest`, with `IAsyncLifetime`). Two small choices make the tests shorter:

```csharp
_playwright.Selectors.SetTestIdAttribute("data-test");   // Week 10, Day 57

private async Task LoginAsync(string user, string password = "secret_sauce")
{
    await _page.GotoAsync(BaseUrl);
    await _page.GetByTestId("username").FillAsync(user);
    await _page.GetByTestId("password").FillAsync(password);
    await _page.GetByTestId("login-button").ClickAsync();
}

private async Task LoginAndWaitAsync()
{
    await LoginAsync("standard_user");
    await Assertions.Expect(_page).ToHaveURLAsync(new Regex("inventory\\.html$"));
}
```

All `data-test` names used below were found by running the tests against the live site and, for the cart page, by listing the page's `data-test` values.

---

## Day 91: Login scenarios

Saucedemo's login page lists its test users on the page. The password is `secret_sauce` for all of them. This week uses `standard_user` and `locked_out_user`.

### Valid login

```csharp
await LoginAndWaitAsync();
await Assertions.Expect(page.GetByTestId("title")).ToHaveTextAsync("Products");
await Assertions.Expect(page.GetByTestId("inventory-item")).ToHaveCountAsync(6);
```

### Wrong password

```csharp
await LoginAsync("standard_user", "wrong");
await Assertions.Expect(page.GetByTestId("error")).ToContainTextAsync("Username and password do not match");
await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("inventory"));
```

### Locked-out user

```csharp
await LoginAsync("locked_out_user");
await Assertions.Expect(page.GetByTestId("error")).ToContainTextAsync("locked out");
```

### Empty fields (validation messages appear one at a time)

```csharp
await page.GotoAsync(BaseUrl);
await page.GetByTestId("login-button").ClickAsync();
await Assertions.Expect(page.GetByTestId("error")).ToContainTextAsync("Username is required");

await page.GetByTestId("username").FillAsync("standard_user");
await page.GetByTestId("login-button").ClickAsync();
await Assertions.Expect(page.GetByTestId("error")).ToContainTextAsync("Password is required");
```

### Logout

```csharp
await LoginAndWaitAsync();
await page.GetByRole(AriaRole.Button, new() { Name = "Open Menu" }).ClickAsync();
await page.GetByTestId("logout-sidebar-link").ClickAsync();
await Assertions.Expect(page.GetByTestId("login-button")).ToBeVisibleAsync();
```

All five tests (`Day91_*`) passed.

### Exercise

1. Add tests for `problem_user` and `performance_glitch_user`. Run them and write down what you observe before you write assertions. Do not assume they behave like `standard_user`.
2. Write a `[Theory]` preview with `[InlineData]` for several invalid logins (xUnit theories come in Phase 3, so copy the pattern from the xUnit documentation).
3. Which of the five tests would you move to a "smoke" group, and why?

---

## Day 92: Inventory: sorting and the cart

### Sorting

The sort dropdown has `data-test="product-sort-container"`. Option values are `az`, `za`, `lohi`, `hilo`. Reading prices then comparing in C# keeps the assertion independent of the actual numbers:

```csharp
await page.GetByTestId("product-sort-container").SelectOptionAsync("lohi");

IReadOnlyList<string> texts = await page.GetByTestId("inventory-item-price").AllTextContentsAsync();
List<decimal> prices = texts
    .Select(t => decimal.Parse(t.TrimStart('$'), CultureInfo.InvariantCulture))
    .ToList();

Assert.Equal(6, prices.Count);
Assert.Equal(prices.OrderBy(p => p).ToList(), prices);
```

The test `Day92_SortByNameZToA` does the same for names with `OrderByDescending(n => n, StringComparer.Ordinal)`.

A caution that Week 12 taught: `AllTextContentsAsync()` does not wait. These tests passed because `SelectOptionAsync` and the earlier `LoginAndWaitAsync` give the page time, but a stricter test would first assert `ToHaveCountAsync(6)`. Try adding it.

### Add and remove from the cart

The add and remove buttons have predictable ids: `add-to-cart-sauce-labs-backpack`, `remove-sauce-labs-backpack`. The badge is `shopping-cart-badge`:

```csharp
ILocator badge = page.GetByTestId("shopping-cart-badge");

await page.GetByTestId("add-to-cart-sauce-labs-backpack").ClickAsync();
await Assertions.Expect(badge).ToHaveTextAsync("1");

await page.GetByTestId("add-to-cart-sauce-labs-bike-light").ClickAsync();
await Assertions.Expect(badge).ToHaveTextAsync("2");

await page.GetByTestId("remove-sauce-labs-backpack").ClickAsync();
await Assertions.Expect(badge).ToHaveTextAsync("1");

await page.GetByTestId("remove-sauce-labs-bike-light").ClickAsync();
await Assertions.Expect(badge).Not.ToBeVisibleAsync();   // the badge disappears when the cart is empty
```

### Adding by filtering the item card (Week 10 technique)

```csharp
await page.GetByTestId("inventory-item")
    .Filter(new() { HasText = "Sauce Labs Backpack" })
    .GetByRole(AriaRole.Button, new() { Name = "Add to cart" })
    .ClickAsync();
await page.GetByTestId("shopping-cart-link").ClickAsync();

await Assertions.Expect(page).ToHaveURLAsync(new Regex("cart\\.html$"));
await Assertions.Expect(page.GetByTestId("cart-list")).ToBeVisibleAsync();
await Assertions.Expect(page.GetByTestId("inventory-item-name")).ToHaveTextAsync("Sauce Labs Backpack");
```

This confirms that the `Filter` + `GetByRole` snippet from Week 10 works on saucedemo (the button's accessible name is "Add to cart").

### A failure worth learning from

The first version of this test had no `cart-list` assertion, and failed with a strict mode violation: `inventory-item-name` matched **6** elements (the whole product list). The URL assertion had passed, but the page was still showing the inventory. Saucedemo is a single-page app: the URL changes **before** the new content is on screen. The fix was to wait for something that only exists on the cart page (`cart-list`) before asserting on content. The earlier lesson holds: a URL change does not prove the page content has changed.

### Exercise

1. Add the first and last products by `Nth`, then explain why you would rather use names.
2. Assert that after adding two items and removing one, the cart page shows exactly one item.
3. Write the `hilo` version of the price test.

---

## Day 93: Checkout flow

The flow: inventory -> cart -> checkout step one (your information) -> step two (overview) -> finish -> complete.

Helper used by the checkout tests:

```csharp
private async Task GoToCheckoutStepOneAsync()
{
    await LoginAndWaitAsync();
    await page.GetByTestId("add-to-cart-sauce-labs-backpack").ClickAsync();
    await page.GetByTestId("shopping-cart-link").ClickAsync();
    await page.GetByTestId("checkout").ClickAsync();
    await Assertions.Expect(page).ToHaveURLAsync(new Regex("checkout-step-one\\.html$"));
}
```

### Complete checkout

```csharp
await GoToCheckoutStepOneAsync();
await page.GetByTestId("firstName").FillAsync("Ada");
await page.GetByTestId("lastName").FillAsync("Lovelace");
await page.GetByTestId("postalCode").FillAsync("12345");
await page.GetByTestId("continue").ClickAsync();

await Assertions.Expect(page).ToHaveURLAsync(new Regex("checkout-step-two\\.html$"));
await Assertions.Expect(page.GetByTestId("inventory-item-name")).ToHaveTextAsync("Sauce Labs Backpack");
await Assertions.Expect(page.GetByTestId("subtotal-label")).ToContainTextAsync("29.99");

await page.GetByTestId("finish").ClickAsync();
await Assertions.Expect(page.GetByTestId("complete-header")).ToHaveTextAsync("Thank you for your order!");
await page.GetByTestId("back-to-products").ClickAsync();
await Assertions.Expect(page.GetByTestId("title")).ToHaveTextAsync("Products");
```

### Totals add up

Rather than hard-coding the tax and total, the test checks the **relationship**: total equals subtotal plus tax.

```csharp
static decimal Money(string text) =>
    decimal.Parse(Regex.Match(text, @"\d+\.\d+").Value, CultureInfo.InvariantCulture);

decimal subtotal = Money((await page.GetByTestId("subtotal-label").TextContentAsync())!);
decimal tax      = Money((await page.GetByTestId("tax-label").TextContentAsync())!);
decimal total    = Money((await page.GetByTestId("total-label").TextContentAsync())!);

Assert.Equal(39.98m, subtotal);        // backpack + bike light
Assert.Equal(subtotal + tax, total);
```

Use `decimal` for money (Week 1). `TextContentAsync()` does not wait; the test is safe because the earlier `Continue` click was followed by page content that was already there when these labels were read. If you see flakiness, add an `Expect(...).ToBeVisibleAsync()` before reading.

### Exercise

1. Add a test that checks the overview page lists **two** items when two were added.
2. Check that the complete page has the order image or a "pony express" element (find its `data-test` yourself).
3. Why is it better to assert `subtotal + tax == total` than to hard-code the total?

---

## Day 94: Negative scenarios

### Required checkout fields, one at a time

```csharp
await GoToCheckoutStepOneAsync();
await page.GetByTestId("continue").ClickAsync();
await Assertions.Expect(page.GetByTestId("error")).ToContainTextAsync("First Name is required");

await page.GetByTestId("firstName").FillAsync("Ada");
await page.GetByTestId("continue").ClickAsync();
await Assertions.Expect(page.GetByTestId("error")).ToContainTextAsync("Last Name is required");

await page.GetByTestId("lastName").FillAsync("Lovelace");
await page.GetByTestId("continue").ClickAsync();
await Assertions.Expect(page.GetByTestId("error")).ToContainTextAsync("Postal Code is required");
```

### Opening a protected page without logging in

```csharp
await page.GotoAsync(BaseUrl + "/inventory.html");
await Assertions.Expect(page.GetByTestId("error")).ToContainTextAsync("only access '/inventory.html' when you are logged in");
```

### Dismissing an error

```csharp
await LoginAsync("standard_user", "wrong");
await Assertions.Expect(page.GetByTestId("error")).ToBeVisibleAsync();
await page.GetByTestId("error-button").ClickAsync();
await Assertions.Expect(page.GetByTestId("error")).Not.ToBeVisibleAsync();
```

### What makes a good negative test

- It checks a **specific** message or state, not just "something failed".
- It proves the bad thing did **not** happen (for example the URL did not change).
- It is independent: each test logs in by itself.

### Exercise

1. Add a negative test for an empty cart checkout. Run it first and write down what saucedemo actually does, then decide whether that is a bug.
2. Add a test that the error message disappears after fixing the input and trying again.
3. Think of three more negative scenarios for the login page (long input, spaces, case sensitivity) and try them.

---

## Day 95: Phase 2 project: 10 UI tests

### The task

Create your own test class (for example `Tests/Course/Week16/Phase2Project.cs`) with **ten** tests of your own design, on saucedemo, that together cover login, inventory, cart and checkout. Requirements:

1. No `Task.Delay`, `Thread.Sleep` or `WaitForTimeoutAsync`.
2. No `Nth` unless the order is the point of the test.
3. Every assertion that depends on the page uses `Assertions.Expect`.
4. Use `GetByRole`, `GetByLabel`, `GetByPlaceholder` or test ids, not long CSS chains.
5. Each test logs in by itself or uses a storage state (Week 13).
6. At least two tests use a mocked response or blocked requests (Week 15).
7. At least one test runs in Firefox or WebKit (Week 13).
8. At least one test records a trace (Week 15).
9. Share setup through one helper or a base class, but do not hide assertions in helpers.
10. Run the whole class three times. If any run fails, find the cause. Do not retry blindly.

### A self-check list

- Can another person read each test name and know what is tested?
- Does each test have exactly one reason to fail?
- If saucedemo adds a banner, which tests break? Why?

### Suggested coverage

| Area | Ideas |
|---|---|
| Login | valid, invalid, locked out, empty fields |
| Inventory | sort by price, sort by name, product count |
| Cart | add, remove, badge count, cart page contents |
| Checkout | happy path, missing field, totals |
| Cross-cutting | mobile viewport, blocked images, a trace |

---

## Day 96: Phase 2 interview Q&A and checkpoint

### Checkpoint

You are ready for Phase 3 when you can do all of these **without looking at the course**:

- [ ] Explain `IPlaywright`, `IBrowser`, `IBrowserContext`, `IPage` and why contexts matter.
- [ ] Write a login test with locators chosen for stability.
- [ ] Explain strict mode and fix a violation.
- [ ] Explain why `Expect(...)` and not `Assert` for page state.
- [ ] Explain what auto-waiting does and when you still need an explicit wait.
- [ ] Reuse a login with storage state.
- [ ] Handle a new tab, a dialog, a download and an iframe.
- [ ] Mock an API failure and assert the UI.
- [ ] Record a trace and say what you would look at first.
- [ ] Complete the Day 95 project with all ten tests green three times in a row.

### Interview questions (Playwright, UI)

**1. Walk me through how Playwright finds and clicks a button.**
Answer: A locator describes the element. On the action, Playwright resolves it (strict mode: exactly one match), waits until the element is attached, visible, stable, enabled and not covered, then clicks. It retries until the timeout.

**2. A test passes locally and fails in CI. What do you check?**
Answer: Headless vs headed differences, viewport size, timing on a slower machine, missing browsers or fonts, environment data, parallelism sharing state, and traces or screenshots from the CI run. Fix the cause, not the timeout.

**3. How do you avoid flaky tests?**
Answer: Web-first assertions instead of reading values; stable locators; no fixed sleeps; isolated contexts and independent data; waiting for specific signals; mocking unstable third parties.

**4. How do you structure locators in a framework?**
Answer: As properties in page objects (this repo's `LoginPage` uses protected `ILocator` properties), preferring role, label and test id; tests call page methods, not raw locators.

**5. How would you speed up a UI suite of 500 tests?**
Answer: Log in once with storage state; one browser with a context per test; parallel execution with isolated data; block unnecessary resources; mock slow third parties; keep end-to-end tests for the critical paths and test the rest at lower levels.

**6. What is the difference between `Locator`, `GetByRole` and `GetByTestId`?**
Answer: `Locator` takes a CSS/XPath selector; `GetByRole` matches by ARIA role and accessible name; `GetByTestId` matches a test attribute (configurable with `SetTestIdAttribute`).

**7. How do you test something that depends on a slow or broken backend?**
Answer: Route the API call and fulfill with controlled data or errors.

**8. How do you debug a failing test you cannot reproduce?**
Answer: Record a trace on failure and open it: look at the failing action, its snapshots before and after, the network tab and the console. Add screenshots or video if needed.

**9. How do you test across browsers?**
Answer: Launch each engine (`Chromium`, `Firefox`, `Webkit`) by configuration; install them; be ready for engine-specific differences.

**10. What would you improve in `BaseTest.cs`?**
Answer: Remove the warnings from `= null`; read headless/browser from configuration; keep the context and dispose it; null-check in `DisposeAsync`; share the browser across tests and create a context per test; capture a trace on failure. (These are the Phase 3 and Phase 4 topics.)

**11. Code:** Click the "Add to cart" button of "Sauce Labs Bike Light".
```csharp
await page.GetByTestId("inventory-item")
    .Filter(new() { HasText = "Sauce Labs Bike Light" })
    .GetByRole(AriaRole.Button, new() { Name = "Add to cart" })
    .ClickAsync();
```

**12. Code:** Assert the cart badge is not shown.
```csharp
await Assertions.Expect(page.GetByTestId("shopping-cart-badge")).Not.ToBeVisibleAsync();
```

**13. Code:** Mock a 500 on any request to `**/api/**` and assert an error banner (adapt to your own page).
```csharp
await context.RouteAsync("**/api/**", r => r.FulfillAsync(new() { Status = 500, Body = "{}" }));
```

---

## Phase 2 summary

| Week | Topic | Examples |
|---|---|---|
| 9 | Basics, object model, `BaseTest` | `Tests/Course/Week09/Week09Examples.cs` (5 tests) |
| 10 | Locators | `Tests/Course/Week10/Week10Examples.cs` (7 tests) |
| 11 | Actions | `Tests/Course/Week11/Week11Examples.cs` (7 tests) |
| 12 | Assertions and waiting | `Tests/Course/Week12/Week12Examples.cs` (10 tests) |
| 13 | Contexts and sessions | `Tests/Course/Week13/Week13Examples.cs` (9 tests) |
| 14 | Frames and dynamic UI | `Tests/Course/Week14/Week14Examples.cs` (6 tests) |
| 15 | Network and debugging | `Tests/Course/Week15/Week15Examples.cs` (12 tests) |
| 16 | End-to-end | `Tests/Course/Week16/Week16Examples.cs` (14 tests) |

Phase 3 (xUnit v3) comes next. You will return to `BaseTest`'s weaknesses in Phase 4.
