# Week 14: Frames and Dynamic UI

This week you learn how to work with content that is harder to reach: iframes, shadow DOM, dynamic tables and pagination, infinite scroll and lazy loading, and date pickers and autocomplete. Day 84 is a review with interview questions.

The examples are in `Tests/Course/Week14/Week14Examples.cs`. All 6 tests were run with `dotnet test --filter "FullyQualifiedName~Week14Examples"` on .NET 10 with Playwright 1.58 and passed. Saucedemo has none of these features, so every example builds its own small page with `SetContentAsync`. They need no internet connection and do not depend on a public practice site.

---

## Day 79: iframes

### What an iframe is

An `<iframe>` embeds a separate page inside the current page. It has its own DOM. A normal locator on the main page **cannot see** elements inside it. Payment forms, embedded videos and ads are common iframes.

The page used in the example (two iframes, one of them nested):

```html
<h1>Outer</h1>
<iframe id="f1" name="payment" srcdoc="<input id='card' placeholder='Card'>
  <button id='pay' onclick=&quot;document.getElementById('msg').textContent='paid'&quot;>Pay</button>
  <div id='msg'></div>"></iframe>
<iframe id="f2" srcdoc="<iframe id='inner' srcdoc='<p id=deep>Deep text</p>'></iframe>"></iframe>
```

### `FrameLocator`

```csharp
IFrameLocator frame = page.FrameLocator("#f1");
await frame.Locator("#card").FillAsync("4111");
await frame.GetByRole(AriaRole.Button, new() { Name = "Pay" }).ClickAsync();
await Assertions.Expect(frame.Locator("#msg")).ToHaveTextAsync("paid");
```

`FrameLocator` takes a selector for the `<iframe>` element, then everything after it is searched **inside** the frame. It auto-waits like any locator, so it works even when the iframe loads late.

### The main page cannot see inside (verified)

```csharp
await page.Locator("#card").CountAsync();   // 0: it lives inside the iframe
```

If a locator that looks right finds nothing, check whether the element is in an iframe (inspect the page in the browser and look for an `<iframe>` ancestor).

### Nested iframes (verified)

Chain `FrameLocator` calls:

```csharp
IFrameLocator nested = page.FrameLocator("#f2").FrameLocator("#inner");
await Assertions.Expect(nested.Locator("#deep")).ToHaveTextAsync("Deep text");
```

### The `IFrame` object

```csharp
IFrame? byName = page.Frame("payment");        // by the iframe's name attribute
page.Frames.Count;                              // 4 here: main + payment + outer f2 + inner
```

The test asserts the count is 4 (the main frame, `payment`, `f2` and the nested `inner`). `IFrame` is useful when you need the frame's URL or to run something in it. For clicking and filling, prefer `FrameLocator`, because it waits.

### Exercise

1. Build a page with an iframe that contains a text input. Fill it using `FrameLocator`.
2. Show that `page.Locator` cannot find the input, and print the count.
3. Add a second iframe inside the first, and reach an element inside it.

---

## Day 80: Shadow DOM

### What it is

Web components can attach a **shadow root** to an element. The elements inside are hidden from normal DOM queries such as `document.querySelector`, which is why shadow DOM is called encapsulated. Libraries built on web components (for example Lit and Ionic) use it heavily.

### Playwright pierces open shadow roots

Playwright's CSS and role/text locators **see through open shadow roots** automatically. No special syntax is needed. The example attaches an open shadow root to `<my-card>` with a button and a span:

```csharp
await page.Locator("#b").CountAsync();                         // 1: found inside the shadow root
await page.Locator("my-card button").CountAsync();             // 1: the CSS path crosses the boundary
await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Shadow Button" })).ToBeVisibleAsync();
await Assertions.Expect(page.GetByText("inside", new() { Exact = true })).ToBeVisibleAsync();
```

All of these passed in `Day80_ShadowDom`.

### Closed shadow roots

A **closed** shadow root (`attachShadow({ mode: 'closed' })`) hides its content from scripts outside the component. In the same test a `<closed-card>` element has a span inside a closed root, and this assertion passed:

```csharp
await page.Locator("closed-card .t").CountAsync();   // 0: not reachable
```

This course did not look for a way around closed roots. For testing, ask the developers to use open roots or to expose a test hook, since closed roots are rare in application code.

### XPath does not pierce shadow roots

The Playwright documentation states that XPath locators do not pierce shadow DOM. This course did not test that claim. Another reason to prefer CSS and `GetBy...` locators.

### Exercise

1. Create a custom element with an open shadow root that contains an input, and fill it with `Locator`.
2. Find the same input using `GetByPlaceholder`.
3. Explain the difference between iframe and shadow DOM from a locator's point of view.

<details>
<summary>Answers</summary>

3. An iframe is a separate document, so you must enter it with `FrameLocator`. An open shadow root is part of the same document, so normal locators work.

</details>

---

## Day 81: Dynamic tables and pagination

### Tables

The example table:

```html
<table id="t">
  <thead><tr><th>Name</th><th>Role</th><th>Action</th></tr></thead>
  <tbody>
    <tr><td>Ann</td><td>Admin</td><td><button>Edit</button></td></tr>
    <tr><td>Bob</td><td>User</td><td><button>Edit</button></td></tr>
    <tr><td>Cid</td><td>User</td><td><button>Edit</button></td></tr>
  </tbody>
</table>
```

Verified in `Day81_DynamicTable`:

```csharp
ILocator rows = page.Locator("#t tbody tr");
await Assertions.Expect(rows).ToHaveCountAsync(3);                 // waits for the data

IReadOnlyList<string> names =
    await page.Locator("#t tbody tr td:nth-child(1)").AllTextContentsAsync();
// ["Ann", "Bob", "Cid"]

ILocator bob = rows.Filter(new() { HasText = "Bob" });             // find the row by content
await bob.Locator("td").Nth(1).TextContentAsync();                 // "User"
await bob.GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();   // act inside that row

await rows.Filter(new() { HasText = "User" }).CountAsync();        // 2
```

### The pattern: find the row, then act inside it

Do not click "the third Edit button". Find the **row** by its content, then use a locator **scoped to that row**. The test then keeps working when rows are re-sorted or new rows appear.

### Wait for data first

`CountAsync` and `AllTextContentsAsync` do not wait (Week 10 and Week 12). Before reading a dynamic table, wait with `Expect(rows).ToHaveCountAsync(n)` or `Expect(rows.First).ToBeVisibleAsync()`.

### Pagination

Loop: read the current page, then click Next until it is disabled. Verified in `Day81_Pagination` (7 items, 3 per page):

```csharp
List<string> all = new();
while (true)
{
    all.AddRange(await page.Locator("#list li").AllTextContentsAsync());
    ILocator next = page.GetByRole(AriaRole.Button, new() { Name = "Next" });
    if (await next.IsDisabledAsync()) break;
    await next.ClickAsync();
}
// all.Count == 7, all[^1] == "Item 7"
```

This simple version works because the example re-renders immediately. On a real page where the list loads from a server, wait for the list to change after each click (for example by asserting on the "Page N" label) before reading it again. Otherwise you may read the old page twice.

### Exercise

1. On the example table, click "Edit" for the user "Cid" and assert something changes (add a click handler to your own page).
2. Read the whole table into a `List<(string Name, string Role)>`.
3. Add a safeguard to the pagination loop so it cannot run forever if the Next button never becomes disabled.

---

## Day 82: Infinite scroll and lazy loading

### The idea

Infinite scroll loads more items when the user nears the bottom. The example loads 10 rows at the start, then 10 more each time you reach the bottom, up to 30, with a 200 ms delay to imitate a server.

### Scrolling

```csharp
ILocator rows = page.Locator(".row");
await Assertions.Expect(rows).ToHaveCountAsync(10);

await rows.Last.ScrollIntoViewIfNeededAsync();
await page.Locator("#box").EvaluateAsync("e => e.scrollTop = e.scrollHeight");
```

- `ScrollIntoViewIfNeededAsync` scrolls the element into view.
- Setting `scrollTop` with `EvaluateAsync` scrolls a specific container to the bottom. This was needed here because the rows live in a scrollable `div`, not the main page. For the main page you can use `page.Mouse.WheelAsync(0, 1000)` or `page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)")`.

### The loop used in the test

```csharp
int previous = 0;
for (int i = 0; i < 10; i++)
{
    int count = await rows.CountAsync();
    if (count == previous) break;       // nothing new loaded: we are done
    previous = count;
    await rows.Last.ScrollIntoViewIfNeededAsync();
    await page.Locator("#box").EvaluateAsync("e => e.scrollTop = e.scrollHeight");
    await page.WaitForTimeoutAsync(400);
}
await Assertions.Expect(rows).ToHaveCountAsync(30);
```

Honest note: the test uses `WaitForTimeoutAsync(400)` inside the loop. This course told you in Week 12 to avoid fixed sleeps, and this is a known weak spot of the example: the loop's "stop when the count stops changing" check can stop too early if a load takes longer than the pause. A more reliable version waits for the count to **increase** after each scroll, with a timeout (for example by polling inside `Expect(...)` or by waiting for a specific new row such as `Row 11`). Try this in the exercise.

### Lazy-loaded images

Images that load when they enter the viewport need the same treatment: scroll them into view, then assert on the image (for example that its `naturalWidth` is greater than 0 using `EvaluateAsync`). This course did not run an example for it.

### Exercise

1. Replace `WaitForTimeoutAsync(400)` in the loop with a wait that is tied to the number of rows increasing.
2. Change the page so that loading takes 2 seconds. Does the original loop still reach 30 rows? Does your improved loop?
3. Scroll until a row with the text "Row 25" is visible, then stop.

---

## Day 83: Date pickers and autocomplete

### Native date input

For `<input type="date">`, use `FillAsync` with an ISO date `yyyy-MM-dd`, no clicking in the calendar:

```csharp
await page.Locator("#d").FillAsync("2030-12-25");
await page.Locator("#d").InputValueAsync();   // "2030-12-25"
```

Verified in `Day83_DatePickerAndAutocomplete`.

### Custom date pickers

Many applications use a custom calendar widget made from `div`s, not a native input. You cannot `FillAsync` a date into those unless the widget has a text field that accepts typing. Otherwise open the picker, navigate to the month and click the day. Make the day locator specific (for example a role or an `aria-label` with the full date) and compute the date in your code rather than hard-coding it, because a hard-coded date goes stale.

### Autocomplete

An autocomplete shows suggestions while you type. Use `PressSequentiallyAsync` so the page receives key events, then pick a suggestion. The example's suggestion list contains London, Lisbon, Los Angeles and Paris, and shows cities that start with the typed text:

```csharp
await page.Locator("#ac").PressSequentiallyAsync("Lo");
await Assertions.Expect(page.GetByRole(AriaRole.Option)).ToHaveCountAsync(2);   // London, Los Angeles
await page.GetByRole(AriaRole.Option, new() { Name = "Los Angeles" }).ClickAsync();
await Assertions.Expect(page.Locator("#ac")).ToHaveValueAsync("Los Angeles");
await Assertions.Expect(page.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);   // list closed
```

Notes:
- **Why not `FillAsync`?** It sets the value in one step, and some autocompletes only react to key events.
- **Assert the suggestions** before you click, so the test waits for them to appear.
- **Mistake made while writing this course:** the first version of the test expected 3 suggestions for "Lo" and included Lisbon. Lisbon starts with "Li", so the real count is 2 and the test failed. The test, not Playwright, was wrong. When a count assertion fails, first check your own test data.

### Exercise

1. Write a test for a page with a native date input that fills a date 30 days from today. Compute the date in C# with `DateTime.Today.AddDays(30).ToString("yyyy-MM-dd")`.
2. Extend the autocomplete example so the user can choose a suggestion with the keyboard (`ArrowDown`, `Enter`) and test that.
3. Write the steps you would use for a custom calendar widget.

---

## Day 84: Review and practice

### Recap

| Day | Topic | Key points |
|---|---|---|
| 79 | iframes | Separate document; main-page locators cannot see inside; `FrameLocator`; chain for nested; `page.Frame(name)` / `page.Frames` |
| 80 | Shadow DOM | Open shadow roots are pierced by CSS and `GetBy...`; closed roots are not reachable; XPath does not pierce (per Playwright docs) |
| 81 | Tables and pagination | Find the row by content with `Filter`, act inside it; wait before reading; loop until Next is disabled |
| 82 | Infinite scroll | Scroll the right container; wait for new items; avoid fixed pauses |
| 83 | Date and autocomplete | `FillAsync` on `type=date`; `PressSequentiallyAsync` for suggestions; assert before clicking |

### Practice questions

1. Why does `page.Locator("#card").CountAsync()` return 0 for an input inside an iframe?
2. How do you reach an element inside a nested iframe?
3. Do you need special syntax for an open shadow root?
4. What is the safest way to click "Edit" on Bob's row?
5. How do you detect the last page of a paginated list?
6. How do you scroll a specific container to the bottom?
7. What format does `FillAsync` need for `<input type="date">`?
8. Why use `PressSequentiallyAsync` for an autocomplete?

<details>
<summary>Answers</summary>

1. The main page's locators do not enter an iframe; use `FrameLocator`.
2. Chain `FrameLocator` calls: `page.FrameLocator("#outer").FrameLocator("#inner")`.
3. No. CSS and `GetBy...` locators pierce open shadow roots.
4. Filter the row by its text, then click the button scoped to that row.
5. The Next button is disabled (or absent).
6. `locator.EvaluateAsync("e => e.scrollTop = e.scrollHeight")`.
7. ISO format: `yyyy-MM-dd`.
8. It fires key events for each character, which many autocompletes rely on.

</details>

### Mini-program: a components lab

Create one HTML page that contains an iframe with a form, a web component with an open shadow root, a table of at least 5 rows, a "Load more" button that appends rows, and an autocomplete. Write a test for each, then one end-to-end test that uses all of them. Do not use `WaitForTimeoutAsync` anywhere. If you feel you need it, find the condition that would tell you the page is ready.

### Interview questions

**1. A locator works in the browser console but finds nothing in Playwright. What do you check?**
Answer: Whether the element is inside an iframe (use `FrameLocator`), whether it appears after a delay (use an assertion or auto-wait instead of `CountAsync`), whether it is in a closed shadow root, and whether the locator is correct for the page state at that moment.

**2. How does Playwright handle shadow DOM?**
Answer: CSS and role/text locators pierce open shadow roots automatically. Closed roots cannot be reached. XPath does not pierce them (stated in Playwright's documentation).
**Follow-up:** How is it different from iframes? Answer: An iframe is a separate document that needs `FrameLocator`; an open shadow root is in the same document.

**3. How do you test a table whose rows change order?**
Answer: Locate the row by its content (`Filter` with `HasText`), then act on elements inside that row. Do not rely on row index.

**4. How would you test pagination?**
Answer: Loop: read the current page, click Next, wait until the page indicator or content changes, and stop when Next is disabled. Assert on the combined data. Add a maximum iteration count as a safeguard.

**5. How do you handle infinite scroll?**
Answer: Scroll the container (or the page) to the bottom, wait until more items appear, and repeat until the count stops increasing or the target item is visible. Avoid fixed sleeps. Wait for the new items.

**6. How do you test an autocomplete?**
Answer: Type with `PressSequentiallyAsync`, assert the suggestions appear (this waits), pick one by role/name or the keyboard, and assert the input value and that the list closed.

**7. How would you test a date picker?**
Answer: For a native input, `FillAsync` with `yyyy-MM-dd`. For a custom widget, open it, navigate to the month and click a day with a specific locator. Compute dates from `DateTime`, never hard-code them.

**8. Why is `WaitForTimeoutAsync` discouraged?**
Answer: It is a fixed sleep: too short is flaky, too long is slow. Wait for a condition instead.

**9. Code:** Click the "Edit" button in the row that contains "Cid".
```csharp
await page.Locator("#t tbody tr")
    .Filter(new() { HasText = "Cid" })
    .GetByRole(AriaRole.Button, new() { Name = "Edit" })
    .ClickAsync();
```

**10. Code:** Type into an input inside `<iframe id="f1">`.
```csharp
await page.FrameLocator("#f1").Locator("#card").FillAsync("4111");
```
