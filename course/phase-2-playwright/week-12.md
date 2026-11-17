# Week 12: Assertions and Waiting

This week you learn how Playwright decides when to act and when to check. You cover web-first assertions with `Expect`, auto-waiting and actionability checks, explicit waits, timeouts and configuration, and negative assertions with failure messages. Day 72 is a review with interview questions.

The examples are in `Tests/Course/Week12/Week12Examples.cs`. All 10 tests were run with `dotnet test --filter "FullyQualifiedName~Week12Examples"` on .NET 10 with Playwright 1.58 and passed. Most use an HTML string with a `setTimeout` that changes the page after a delay, so you can see waiting happen without depending on a website. Two use saucedemo.

The delayed page used in several examples:

```html
<div id="status">Loading</div>
<script>
  setTimeout(() => {
    document.getElementById('status').textContent = 'Done';
    const b = document.createElement('button');
    b.id = 'late'; b.textContent = 'Late';
    document.body.appendChild(b);
  }, 1000);
</script>
```

---

## Day 67: `Expect` and web-first assertions

### Two kinds of assertion

| | xUnit `Assert` | Playwright `Assertions.Expect` |
|---|---|---|
| Checks | A value you already have | A **locator or page**, live |
| Waits? | No. It checks once | Yes. It retries until it passes or times out |
| Example | `Assert.Equal("Done", text)` | `await Expect(locator).ToHaveTextAsync("Done")` |

### Proof (verified)

On the delayed page, reading the text immediately and using xUnit `Assert` sees the **old** value:

```csharp
string? text = await page.Locator("#status").TextContentAsync();
Assert.Equal("Loading", text);   // passes: the page has not changed yet
```

A web-first assertion waits for the change:

```csharp
await Assertions.Expect(page.Locator("#status")).ToHaveTextAsync("Done");
```

In `Day67_WebFirstAssertionsRetry` the test measures the time and asserts it took between 0.5 and 5 seconds. That is the retry at work. The exact number differs per run, so this course does not quote one.

**Rule:** when the thing you check can change after your last action, use `Expect(...)`. Do not read a value and compare it with `Assert`.

### Where `Expect` comes from

```csharp
using Microsoft.Playwright;

await Assertions.Expect(locator).ToBeVisibleAsync();
```

`Assertions` is a static class in `Microsoft.Playwright`. The xUnit helper base classes from `Microsoft.Playwright.Xunit.v3` also expose an `Expect` method. This repo's `BaseTest` does not use those helper classes, so it uses `Assertions.Expect(...)` directly (see `LoginTests`).

### Assertions on locators (all verified in `Day67_CommonAssertions`)

| Assertion | Checks |
|---|---|
| `ToBeVisibleAsync()` | visible |
| `ToHaveTextAsync("Hello World")` | **full** text (exact, whitespace normalized) |
| `ToContainTextAsync("World")` | substring |
| `ToHaveTextAsync(new Regex("^Hello"))` | regex |
| `ToHaveTextAsync(new[] { "A", "B" })` | list of elements' texts, in order |
| `ToHaveClassAsync("big")` | class attribute |
| `ToHaveValueAsync("abc")` | input value |
| `ToHaveAttributeAsync("href", "/x")` | attribute value |
| `ToBeDisabledAsync()` / `ToBeEnabledAsync()` | state |
| `ToBeCheckedAsync()` | checkbox / radio |
| `ToHaveCountAsync(2)` | number of matches |

Assertions on the page:

```csharp
await Assertions.Expect(page).ToHaveTitleAsync("My Page");
await Assertions.Expect(page).ToHaveURLAsync(new Regex("inventory\\.html$"));
```

`ToHaveTextAsync` vs `ToContainTextAsync`: the first needs the **whole** text to match; the second needs only part of it. Choose the stricter one when exactness matters.

### Exercise

1. On saucedemo, after login, assert the title text of the page header is `Swag Labs` using `Expect`.
2. Assert there are exactly 6 products with `ToHaveCountAsync(6)`.
3. Rewrite this to be reliable and explain why the original is flaky:
   ```csharp
   Assert.Equal(6, await page.Locator(".inventory_item").CountAsync());
   ```

<details>
<summary>Answers</summary>

3. `await Assertions.Expect(page.Locator(".inventory_item")).ToHaveCountAsync(6);`. `CountAsync()` is a snapshot, so right after navigation it can return 0 (this happened while writing Week 10).

</details>

---

## Day 68: Auto-waiting and actionability checks

### What happens before an action

When you call `ClickAsync`, Playwright waits until the element passes these checks:

| Check | Meaning |
|---|---|
| **Attached** | It exists in the DOM |
| **Visible** | It has a non-empty size and is not `visibility: hidden` |
| **Stable** | It is not moving (not animating) for two consecutive frames |
| **Receives events** | No other element covers it at the click point |
| **Enabled** | It is not `disabled` |

Which checks run depends on the action. For example `FillAsync` also requires the element to be editable, and `HoverAsync` does not require it to be enabled. See the Playwright documentation's "Actionability" page for the table.

### Verified examples

An element that appears after 1 second:

```csharp
await page.Locator("#late").ClickAsync();   // waits about 1 second, then clicks
```

A button that is `disabled` for 800 ms:

```csharp
await page.Locator("#b").ClickAsync();      // waits until it is enabled
await Assertions.Expect(page.Locator("#o")).ToHaveTextAsync("clicked");
```

No `Thread.Sleep` or `Task.Delay` was needed in either test.

### When waiting fails

If the checks do not pass before the timeout, the action throws. In `Day68_AutoWaitTimesOutWithUsefulMessage`:

```csharp
TimeoutException ex = await Assert.ThrowsAsync<TimeoutException>(
    () => page.Locator("#nope").ClickAsync(new() { Timeout = 1500 }));
Assert.Contains("Timeout 1500ms exceeded", ex.Message);
```

The message names the timeout and describes what Playwright was waiting for. Read it first when a test fails. (The `TimeoutException` here is the Playwright one, which is a kind of `PlaywrightException`. Check which namespace your editor resolves it to, because `System.TimeoutException` also exists.)

### Why not `Thread.Sleep`?

Too short and the test is flaky; too long and the suite is slow. Auto-waiting waits exactly as long as needed, up to the timeout.

### Exercise

1. Build an HTML page where a button becomes enabled after 2 seconds. Click it without any sleep and prove the click works.
2. Make the button become enabled after 60 seconds. Observe what error you get with the default timeout.
3. Name the five actionability checks.

---

## Day 69: Explicit waits: `WaitForAsync`, `WaitForURLAsync`

Auto-waiting covers most cases. Explicit waits exist for situations where you want to wait for something that is **not** the target of your next action.

### Locator `WaitForAsync`

```csharp
await page.Locator("#late").WaitForAsync(new() { State = WaitForSelectorState.Visible });
```

States: `Attached`, `Detached`, `Visible`, `Hidden`. Waiting for an element to disappear (verified):

```csharp
await page.Locator("#x").WaitForAsync(new() { State = WaitForSelectorState.Detached });
```

### Wait for a condition

```csharp
await page.WaitForFunctionAsync("document.getElementById('status').textContent === 'Done'");
```

It runs the JavaScript expression repeatedly until it is truthy.

### `WaitForURLAsync` and load states

```csharp
await page.WaitForURLAsync("**/inventory.html");
await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
```

`LoadState` values: `Load`, `DOMContentLoaded`, `NetworkIdle`. Do not use `NetworkIdle` as a general fix: pages with polling never become idle. Prefer waiting for a **specific element or URL** that proves the page is ready.

All of these are in `Day69_ExplicitWaits` and `Day69_WaitForUrl`.

### Assertion or explicit wait?

If you are about to **check** something, use `Expect` (it waits already). Use an explicit wait when you need to be **synchronized** before something that is not itself a check, for example waiting for a spinner to be `Hidden` before reading a table with `AllTextContentsAsync` (which does not wait).

### Anti-patterns

- `await Task.Delay(3000);` as a wait.
- Waiting for `NetworkIdle` everywhere.
- Waiting for something and then not asserting anything about it.

### Exercise

1. After login on saucedemo, wait for the inventory URL and then for the first `.inventory_item` to be visible.
2. Write a helper that waits until an element is `Hidden`, and use it with a spinner that disappears after 1 second (build the spinner with `SetContentAsync`).
3. Replace `Task.Delay(2000)` in a test you wrote with a proper wait.

---

## Day 70: Timeouts and configuration

### The timeouts

| Timeout | Default | Applies to |
|---|---|---|
| Default (general) | 30 seconds | actions (click, fill...) and waits |
| Navigation timeout | 30 seconds | `GotoAsync`, `GoBackAsync`, `ReloadAsync`... |
| `Expect` timeout | 5 seconds | web-first assertions |

(The defaults above are Playwright's documented values. This course measured only the behaviours in the next section.)

### Setting them

Per call:

```csharp
await locator.ClickAsync(new() { Timeout = 1500 });
await Assertions.Expect(locator).ToBeVisibleAsync(new() { Timeout = 700 });
```

For a page (or context):

```csharp
page.SetDefaultTimeout(1000);
page.SetDefaultNavigationTimeout(5000);
```

Verified in `Day70_Timeouts`: with `SetDefaultTimeout(1000)` a click on a missing element failed after roughly one second (the test asserts it took between 0.9 and 3 seconds), and an assertion with `Timeout = 700` failed in about 0.7 seconds.

Note that `SetDefaultTimeout` does not change the `Expect` timeout. Assertions have their own default, and you set it per assertion (as above).

### Choosing values

- Keep defaults for normal UI.
- Raise the timeout for one known slow step (a report that takes 20 seconds), not for the whole suite.
- Lowering the default can make failures faster to see while you write a test.
- Never solve flakiness by simply raising every timeout. It hides the cause and slows failures.

### Where this repo stands

`BaseTest` does not set any timeouts, so every test uses the defaults. `LoginTests` sets `Timeout = 15000` on one assertion, which is an example of the per-call approach. Central configuration comes in Phase 4.

### Exercise

1. Set a 1 second default timeout on a page and try to click a missing element. Time it with `Stopwatch`.
2. Give one assertion a 10-second timeout and show it passing on a page that changes after 6 seconds.
3. Explain why raising the global timeout to 2 minutes is a bad fix for a flaky test.

---

## Day 71: Negative assertions and failure messages

### `Not`

```csharp
await Assertions.Expect(page.Locator("#a")).Not.ToBeVisibleAsync();
await Assertions.Expect(page.Locator("#b")).Not.ToHaveTextAsync("other");
await Assertions.Expect(page.Locator("#gone")).Not.ToBeAttachedAsync();
```

All three are verified in `Day71_NegativeAssertionsAndMessages`. A negative assertion **also waits**: `Not.ToBeVisibleAsync()` keeps retrying until the element is not visible. So it is the right tool to check that a spinner has gone, and it is better than `Assert.False(await locator.IsVisibleAsync())`, which checks once.

### Careful: a negative assertion can pass for the wrong reason

`Not.ToHaveTextAsync("other")` passes if the element has any text other than "other", or does not exist. If you want to say "the element exists and shows X", assert that positively. Use negative assertions for absence and disappearance.

### Failure messages

When an assertion times out, the exception contains the expected value, the actual value found, and a call log of the retries. The test asserts that the message contains the expected text `wrong`:

```csharp
PlaywrightException ex = await Assert.ThrowsAsync<PlaywrightException>(
    () => Assertions.Expect(page.Locator("#b")).ToHaveTextAsync("wrong", new() { Timeout = 700 }));
Assert.Contains("wrong", ex.Message);
```

Read the message top to bottom when you debug: the first lines say what was expected and what was received. I did not capture the full message text for this course, so run the test and print `ex.Message` to see your version's format.

Custom assertion messages: this course did not verify a way to attach your own message to a Playwright assertion in the .NET API, so do not rely on one. Make failures readable instead by using clear test names and specific locators.

### Soft assertions

Not covered here because the .NET API does not give you a built-in soft-assertion mode that this course has verified. If you need to collect several failures in one test, check the Playwright documentation for your version before relying on it.

### Exercise

1. Assert that a spinner is not visible, on a page where it disappears after 2 seconds.
2. Write a failing assertion on purpose, with a 1 second timeout, and print the whole `ex.Message`. List what information it contains.
3. Rewrite `Assert.False(await spinner.IsVisibleAsync())` as a web-first assertion and explain the difference.

---

## Day 72: Review and practice

### Recap

| Day | Topic | Key points |
|---|---|---|
| 67 | Web-first assertions | `Expect(locator)` retries; xUnit `Assert` checks once; use `Expect` for anything that can still change |
| 68 | Auto-waiting | Attached, visible, stable, receives events, enabled; no `Thread.Sleep`; timeouts include a useful message |
| 69 | Explicit waits | `WaitForAsync` states; `WaitForURLAsync`; `WaitForFunctionAsync`; avoid `NetworkIdle` as a habit |
| 70 | Timeouts | Per call, `SetDefaultTimeout`, `SetDefaultNavigationTimeout`; `Expect` has its own timeout |
| 71 | Negative assertions | `Not.` also waits; messages show expected and actual; do not use `Not` as a substitute for a positive check |

### Practice questions

1. Why is `Assert.Equal("Done", await locator.TextContentAsync())` risky?
2. Does `ToHaveTextAsync("World")` pass for the text "Hello World"?
3. Name three actionability checks.
4. What does `WaitForSelectorState.Detached` wait for?
5. Which timeout does `SetDefaultTimeout` not change?
6. Does `Not.ToBeVisibleAsync()` wait?
7. Why is `Task.Delay` a bad wait?
8. Does `CountAsync()` wait?

<details>
<summary>Answers</summary>

1. It reads the text once. If the page has not updated yet, the test fails even though the page will be right a moment later.
2. No. `ToHaveTextAsync` needs the full text; use `ToContainTextAsync("World")`.
3. Any three of: attached, visible, stable, receives events, enabled.
4. For the element to be removed from the DOM.
5. The `Expect` assertion timeout.
6. Yes. It retries until the element is not visible or the timeout expires.
7. It waits a fixed time: too short is flaky, too long wastes time.
8. No, it is a snapshot.

</details>

### Mini-program: slow page toolkit

Build a local HTML page with: a spinner that disappears after 2 seconds, a button that is enabled after 3 seconds, a result `div` that appears after the click with a 1 second delay, and an element that is removed after 4 seconds. Write one test per behaviour using the correct waiting technique, with **no** `Task.Delay` and no `Thread.Sleep`. Then change the delays to be longer than the default `Expect` timeout (5 seconds) and observe which tests fail and why. Fix them with a per-call timeout.

### Interview questions

**1. What is auto-waiting?**
Answer: Before an action, Playwright waits until the element is attached, visible, stable, not covered and enabled, retrying until the timeout. You do not write sleeps.
**Follow-up:** Does auto-waiting remove the need for assertions? Answer: No. It makes actions reliable; assertions verify the result.

**2. Difference between `Assert.Equal` and `Expect(...).ToHaveTextAsync`?**
Answer: `Assert` compares values once. `Expect` takes a locator and retries until the condition is true or times out.

**3. What is a web-first assertion?**
Answer: An assertion that auto-retries against the live page, so it handles asynchronous updates.

**4. When do you need an explicit wait?**
Answer: When you must be synchronized with something that is not the target of the next action or assertion, for example waiting for a loading overlay to detach before reading data with a non-waiting method.

**5. Why is `NetworkIdle` a poor default?**
Answer: Pages with polling, websockets or analytics never become idle, so tests hang until the timeout, and idle does not prove the UI is ready. Wait for specific UI states instead.

**6. A test fails with "Timeout 30000ms exceeded". What do you do?**
Answer: Read the call log in the message to see which check was not satisfied (not visible, covered, disabled, not found). Check the locator, check for overlays or a changed page, then fix the cause. Do not just increase the timeout.

**7. How do you give one slow assertion more time?**
Answer: Pass `new() { Timeout = 10000 }` to that assertion, as `LoginTests` does for the inventory list.

**8. Is `Not.ToBeVisibleAsync()` the same as `Assert.False(await x.IsVisibleAsync())`?**
Answer: No. The first retries until the element is hidden; the second checks once.

**9. Code:** Wait for a spinner with id `spinner` to disappear, then assert the table has 5 rows.
```csharp
await Assertions.Expect(page.Locator("#spinner")).Not.ToBeVisibleAsync();
await Assertions.Expect(page.Locator("table tr")).ToHaveCountAsync(5);
```

**10. Code:** Make a single click wait up to 10 seconds.
```csharp
await page.Locator("#slow").ClickAsync(new() { Timeout = 10000 });
```
