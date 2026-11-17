# Week 10: Locators

This week you learn how to find elements. You start with CSS and XPath selectors, then the user-facing `GetBy...` methods, test IDs (including this repo's `data-test` attribute), chaining and filtering, and strict mode. Day 60 is a review with interview questions.

The examples are in `Tests/Course/Week10/Week10Examples.cs`. All 7 tests were run with `dotnet test --filter "FullyQualifiedName~Week10Examples"` on .NET 10 with Playwright 1.58 and passed. Most examples use a small HTML string loaded with `SetContentAsync`, so they do not depend on a website. One example uses saucedemo.

The HTML used in the examples:

```html
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
```

---

## Day 55: CSS and XPath selectors

### What a locator is

`page.Locator(selector)` returns an `ILocator`. A locator is **not** an element. It is a recipe for finding elements, and Playwright runs the recipe each time you use the locator. That is why a locator keeps working after the page re-renders. Creating a locator does not touch the page, so it does not need `await`.

### CSS selectors

| Selector | Meaning | Example |
|---|---|---|
| `#title` | by `id` | `Locator("#title")` |
| `.item` | by class | `Locator(".item")` |
| `h1.heading` | tag plus class | |
| `ul#items > li.item` | direct child | |
| `div li` | any descendant | |
| `input[type='password']` | attribute equals | |
| `[data-test='go']` | any element with that attribute | |
| `a[href^='/ab']` | attribute starts with | |
| `a[href$='out']` | attribute ends with | |
| `a[href*='bo']` | attribute contains | |

Verified:

```csharp
await page.Locator("#title").CountAsync();                 // 1
await page.Locator("h1.heading").CountAsync();             // 1
await page.Locator("ul#items > li.item").CountAsync();     // 3
await page.Locator("input[type='password']").CountAsync(); // 1
await page.Locator("[data-test='go']").CountAsync();       // 1
```

### XPath

XPath is useful when you need to move **up** the tree or match by text. Playwright detects a selector that starts with `//` or `..` as XPath, or you can write the `xpath=` prefix.

```csharp
await page.Locator("xpath=//ul[@id='items']/li").CountAsync();            // 3
await page.Locator("//button[text()='Cancel']").TextContentAsync();       // "Cancel"
```

### Which one should you use?

CSS is shorter, faster and easier to read. Use XPath only when CSS cannot express what you need. Both are tied to the page structure, so they break when developers change the markup. Day 56 and Day 57 show more stable options.

### Exercise

Using the HTML above, write one locator for each of these, and check the count:
1. the password input, by `id`
2. the second `li` (using CSS `:nth-child`)
3. all `li` elements that are children of `#items`
4. the `<b>` inside the first `.card`

<details>
<summary>Answers</summary>

1. `#pwd`
2. `#items li:nth-child(2)`
3. `#items > li` (count 3)
4. `.card:first-child b` is **not** reliable here because the first `.card` is not the first child of `body`. Use `.card >> nth=0 >> b`, or in C# `page.Locator(".card").First.Locator("b")`.

</details>

---

## Day 56: `GetByRole`, `GetByText`, `GetByLabel`, `GetByPlaceholder`

Playwright recommends locators that match what a **user** sees and what assistive technology reads, because those change less often than CSS classes.

| Method | Finds by | Example |
|---|---|---|
| `GetByRole` | ARIA role plus accessible name | `GetByRole(AriaRole.Button, new() { Name = "Sign in" })` |
| `GetByText` | visible text | `GetByText("Backpack")` |
| `GetByLabel` | the `<label>` of a form control | `GetByLabel("Email")` |
| `GetByPlaceholder` | the placeholder text | `GetByPlaceholder("you@example.com")` |
| `GetByAltText` | `alt` of an image | |
| `GetByTitle` | `title` attribute | |
| `GetByTestId` | test id attribute (Day 57) | |

Verified (each returns count 1 on the HTML above):

```csharp
page.GetByRole(AriaRole.Button, new() { Name = "Sign in" })
page.GetByRole(AriaRole.Heading, new() { Name = "Shop" })
page.GetByRole(AriaRole.Link, new() { Name = "About us" })
page.GetByText("Backpack")
page.GetByLabel("Email")
page.GetByPlaceholder("you@example.com")
```

### Roles

A role is the meaning of an element: `<button>` has role `button`, `<a href>` has `link`, `<h1>`-`<h6>` have `heading`, `<input type="checkbox">` has `checkbox`. You get these roles for free from correct HTML, with no extra attributes. The `Name` option matches the accessible name (button text, label text, and so on).

### Text matching rules

By default `GetByText` and `Name` do a **case-insensitive substring** match. Use `Exact = true` for a full, case-sensitive match.

```csharp
page.GetByText("backpack", new() { Exact = false })  // count 1 (matches "Backpack")
page.GetByText("backpack", new() { Exact = true })   // count 0
```

### Why this matters

If your tests use `GetByRole(Button, "Sign in")`, a developer can rename a CSS class without breaking the test. If the button stops being a real button, the test fails, and that is a useful failure because accessibility broke too.

### Exercise

On saucedemo's login page, locate the password field using `GetByPlaceholder`, and the login button using `GetByRole`. Fill and click them, then assert the URL ends with `/inventory.html`. (The button's accessible name on saucedemo is `Login`.)

---

## Day 57: Test IDs and the repo's `data-test` attribute

### What a test id is

A test id is an attribute that developers add only for tests, for example `data-test="login-button"`. It does not change when the text, translation or styling changes. This repo's `LoginPage` uses it:

```csharp
protected ILocator UsernameInput => Page.Locator("[data-test='username']");
```

### `GetByTestId`

`GetByTestId("go")` looks for the attribute `data-testid` by default. Saucedemo (and this repo) use `data-test`, so by default it finds nothing:

```csharp
await page.GetByTestId("go").CountAsync();   // 0 (the HTML uses data-test, not data-testid)
```

You change the attribute name once, on the `IPlaywright` object:

```csharp
playwright.Selectors.SetTestIdAttribute("data-test");

await page.GetByTestId("go").CountAsync();   // 1
await page.GetByTestId("email").FillAsync("a@b.com");
await page.GetByTestId("email").InputValueAsync();  // "a@b.com"
```

Verified in `Day57_TestIds`.

**Warning (found by running the whole suite):** the test id attribute is shared by the whole test process, not only by one `IPlaywright` instance. Week 16's tests set it to `data-test` and did not reset it, so when this test ran afterwards its first check (expecting 0 matches) failed. The fix in the example code is to set the value you expect at the start of the test (`SetTestIdAttribute("data-testid")`) and to reset it when a test that changes it finishes. I first tried an xUnit collection to stop parallel runs, and that did not fix it, so the cause was leaking state, not timing. In real projects, set it once for the whole run and never change it per test. In `BaseTest` (Day 53) the best place is right after `CreateAsync()`, but this course does not change `BaseTest` yet.

### When to use which

Order of preference (from Playwright's own guidance):
1. `GetByRole` (what users and screen readers see)
2. `GetByLabel`, `GetByPlaceholder`, `GetByText`
3. `GetByTestId` when the element has no good role or text
4. CSS/XPath as the last resort

This repo uses `[data-test='...']` CSS selectors directly. That is a valid, stable choice, and it is equivalent to `GetByTestId` once the attribute is configured.

### Exercise

1. Log in to saucedemo using only `[data-test=...]` locators.
2. Do the same with `GetByTestId` after calling `SetTestIdAttribute("data-test")`.
3. Explain in one sentence why a test id is more stable than `.btn.btn-primary`.

<details>
<summary>Answers</summary>

3. Class names describe styling and change in redesigns; a test id exists only for tests, so nobody changes it by accident.

</details>

---

## Day 58: Chaining, filtering, `First`, `Last`, `Nth`

### Picking one of many

```csharp
ILocator items = page.Locator("li.item");

await items.First.TextContentAsync();      // "Backpack"
await items.Last.TextContentAsync();       // "Fleece Jacket"
await items.Nth(1).TextContentAsync();     // "Bike Light"   (zero-based)
```

`First` and `Last` are properties; `Nth(int)` is a method. Index numbers are zero-based.

### Counting and reading lists

```csharp
await items.CountAsync();                  // 3
IReadOnlyList<string> all = await items.AllTextContentsAsync();
// ["Backpack", "Bike Light", "Fleece Jacket"]
```

Important: `CountAsync()` and `AllTextContentsAsync()` are **snapshots**. They do **not** wait for elements to appear. If you call them while the page is still loading, you can get 0. When a count matters, use the retrying assertion:

```csharp
await Assertions.Expect(page.Locator(".inventory_item")).ToHaveCountAsync(6);
```

This was found while writing this week's examples: right after `WaitForURLAsync("**/inventory.html")`, `CountAsync()` on saucedemo returned 0 because the items had not rendered yet. The `Expect(...).ToHaveCountAsync` version passed. More on this in Week 12.

### Chaining

Calling `Locator` on a locator searches **inside** it:

```csharp
page.Locator("#items").Locator("li")     // 3 elements
```

### Filtering

```csharp
items.Filter(new() { HasText = "Jacket" })       // 1 element
items.Filter(new() { HasNotText = "Backpack" })  // 2 elements
```

`Filter` with `Has` keeps elements that contain a matching child. For example, the card that contains the price 29.99:

```csharp
ILocator card = page.Locator(".card")
    .Filter(new() { Has = page.Locator("b", new() { HasText = "29.99" }) });
await card.CountAsync();   // 1
```

All of this is verified in `Day58_ChainingFilteringNth`.

### Prefer filtering over `Nth`

`Nth(3)` breaks when the order changes. `Filter(HasText = "Fleece")` states **what** you want. Use `Nth` only when order is the point of the test (for example, checking sorting).

### Exercise

On saucedemo's inventory page (after login):
1. Get the name of the first product and the last product.
2. Find the product card that contains the text "Backpack" and click its "Add to cart" button, using chaining and `Filter`.
3. Assert the cart badge shows `1`.

---

## Day 59: Strict mode and choosing good locators

### What strict mode is

When an action needs exactly one element (`ClickAsync`, `FillAsync`, `TextContentAsync`...) and the locator matches **more than one**, Playwright throws instead of guessing. This is **strict mode**.

```csharp
ILocator items = page.Locator("li.item");   // 3 matches

PlaywrightException ex = await Assert.ThrowsAsync<PlaywrightException>(
    () => items.TextContentAsync(new() { Timeout = 2000 }));
// ex.Message contains "strict mode violation"
```

Verified in `Day59_StrictMode`. The error message lists the matching elements, which helps you refine the locator.

Fixes, in order of preference:
1. Make the locator more specific (a role and name, or a test id).
2. Narrow the scope by chaining from a parent.
3. Filter by text or by a child.
4. `First`/`Nth` if the order is really the intent.

Methods like `CountAsync`, `AllTextContentsAsync` and `Filter` work with many elements and do not trigger strict mode.

### Checklist for a good locator

- Does it describe what the user sees (role, label, text)?
- Does it survive a style change? A text change? A layout change?
- Is it unique on the page? (If not, strict mode will tell you.)
- Does it depend on position (`Nth`, `:nth-child`, long CSS chains)? Avoid it.
- Does it need text that is translated or changes often? Use a test id.

### Debugging locators

- In the browser, press F12 and type `document.querySelectorAll("li.item")` in the console to test CSS.
- Use `await locator.CountAsync()` while developing to see how many elements match.
- Playwright's codegen tool can suggest locators: `pwsh bin/Debug/net10.0/playwright.ps1 codegen https://www.saucedemo.com`.

### Exercise

1. On the HTML above, write a locator for "Cancel" that does **not** use `Nth` or text, only a test id.
2. Write a locator that triggers a strict mode violation, catch the exception and print its first line.
3. Fix the violation in two different ways.

---

## Day 60: Review and practice

### Recap

| Day | Topic | Key points |
|---|---|---|
| 55 | CSS and XPath | A locator is a recipe, not an element; CSS first; XPath for text or moving up |
| 56 | `GetBy...` | `GetByRole` + `Name` first; substring and case-insensitive unless `Exact = true` |
| 57 | Test ids | `GetByTestId` reads `data-testid`; `SetTestIdAttribute("data-test")` for this repo |
| 58 | Chain and filter | `First`, `Last`, `Nth` (zero-based); `Filter` with `HasText`, `Has`; `CountAsync` is a snapshot |
| 59 | Strict mode | More than one match throws "strict mode violation"; narrow the locator |

### Practice questions

1. Does `page.Locator("#title")` need `await`?
2. What is the difference between a locator and an element handle?
3. What does `GetByText("backpack")` match by default?
4. What attribute does `GetByTestId` read by default? How do you change it?
5. Is `Nth(1)` the first or the second element?
6. What happens when `ClickAsync` is called on a locator that matches three elements?
7. Does `CountAsync()` wait for elements to appear?
8. Name two ways to make a too-broad locator unique.

<details>
<summary>Answers</summary>

1. No. It only creates the locator. The `await` happens on the action (click, fill, and so on).
2. A locator re-queries the page every time it is used and auto-waits; a handle points to one specific DOM node that can become stale. Prefer locators.
3. Any element whose text contains "backpack", ignoring case.
4. `data-testid`; call `playwright.Selectors.SetTestIdAttribute("data-test")`.
5. The second (zero-based).
6. A strict mode violation (`PlaywrightException`).
7. No, it is a snapshot. Use `Expect(locator).ToHaveCountAsync(n)` to wait.
8. Chain from a parent, filter by text or a child, use a role with a name, use a test id.

</details>

### Mini-program: locator audit of saucedemo

Log in to saucedemo. For each of these elements write **two** different locators and check that both find exactly one element (use `Expect(...).ToHaveCountAsync(1)`), then say which one you would keep in a page object and why:
- the page title text "Products"
- the shopping cart link
- the product sort dropdown
- the "Add to cart" button of "Sauce Labs Backpack"

### Interview questions

**1. What is a locator in Playwright?**
Answer: A lazily evaluated description of how to find elements. It does not hold an element, it re-resolves on each action and auto-waits, so it survives re-renders.
**Follow-up:** Why is that better than holding an element reference? Answer: A held reference can go stale after the DOM changes; a locator never does.

**2. Which locators do you prefer and why?**
Answer: Role and text based ones first, because they mirror what users see and survive markup changes. Test ids when there is no good role or text. CSS or XPath last.
**Follow-up:** When would you pick a test id? Answer: Icon-only controls, text that is translated or dynamic, or elements with no semantic role.

**3. What is strict mode?**
Answer: Actions on a locator that matches more than one element throw, instead of silently acting on the first. It prevents tests from clicking the wrong element.
**Follow-up:** How do you fix a violation? Answer: Make the locator more specific, scope it, or filter it; `First`/`Nth` only when order matters.

**4. What is the difference between `Locator("text=Foo")`, `GetByText("Foo")` and `GetByRole(..., Name = "Foo")`?**
Answer: Text locators match visible text in any element; the role version only matches elements with that role whose accessible name matches, so it is more precise and checks accessibility too.

**5. How do you select an element by position, and what is the risk?**
Answer: `First`, `Last`, `Nth(i)`. The risk is that a change in ordering silently breaks the test or, worse, makes it act on the wrong item.

**6. `CountAsync()` returns 0 right after navigation but the elements are there a second later. Why?**
Answer: `CountAsync()` is a snapshot and does not wait. Use `Expect(locator).ToHaveCountAsync(n)`, which retries until the timeout.

**7. How does this repo locate the login fields?**
Answer: With CSS on the `data-test` attribute, for example `Page.Locator("[data-test='username']")`, as protected properties in `LoginPage`.
**Follow-up:** How could `GetByTestId` replace them? Answer: Call `Selectors.SetTestIdAttribute("data-test")` once and use `GetByTestId("username")`.

**8. Code:** Click "Add to cart" on the product named "Sauce Labs Backpack" on saucedemo, without `Nth`.
```csharp
await page.Locator(".inventory_item")
    .Filter(new() { HasText = "Sauce Labs Backpack" })
    .GetByRole(AriaRole.Button, new() { Name = "Add to cart" })
    .ClickAsync();
```
(This exact snippet was not run as part of the Week 10 tests; try it in the Day 58 exercise and tell me if the button name differs.)

**9. Code:** Given `<ul id="items"><li>A</li><li>B</li></ul>`, return the texts of all items as a list.
```csharp
IReadOnlyList<string> texts = await page.Locator("#items li").AllTextContentsAsync();
```
