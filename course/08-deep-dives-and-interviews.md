# Part 8: Deep Dives and Interview Prep (Days 64–70)

Parts 1–7 build the skills. This part goes deeper on topics that interviewers ask about most, then turns everything into an interview-ready framework. Each day ends with interview questions. Answer them out loud before you read the answers in your notes.

---

## Day 64: Strings and StringBuilder

`string` is **immutable**. Every "change" creates a new string. That matters in loops.

```csharp
using System.Text;

string s = "  standard_user  ";
Console.WriteLine(s.Trim());                           // standard_user
Console.WriteLine(s.Trim().Length);                    // 13
Console.WriteLine(string.IsNullOrWhiteSpace("   "));   // True
Console.WriteLine(string.Join(", ", new[] { "a", "b" })); // a, b
Console.WriteLine("a,b,,c".Split(',', StringSplitOptions.RemoveEmptyEntries).Length); // 3
Console.WriteLine("Login".Contains("log", StringComparison.OrdinalIgnoreCase));      // True
Console.WriteLine($"[{"x",5}]");                       // [    x]  right-aligned in 5 characters
```

**Comparison:** `==` on `string` compares the value, not the reference. For user-facing text, pass a `StringComparison` so the result doesn't depend on the machine's culture.

**StringBuilder** is for building a string piece by piece:

```csharp
var sb = new StringBuilder();
for (int i = 1; i <= 3; i++)
{
    sb.Append($"row {i}").AppendLine();
}
sb.Insert(0, "Report\n");
sb.Replace("row", "line");
Console.Write(sb.ToString());
Console.WriteLine(sb.Length);
```

**Pitfall:** `result += piece` inside a loop copies the whole string each time, which gets slow for large loops. Use `StringBuilder` there.

**Exercise:** given the string `"alice, bob,  , carol"`, split it, trim each part, drop the empty ones, and build a single line `"alice | bob | carol"` with a `StringBuilder`.

**Check:** you can explain why `StringBuilder` is faster in a loop, and why a `string` passed to a method can't be changed by that method.

**Interview questions**
1. Why are strings immutable in .NET? (Safety, thread-safe sharing, and they can be used as dictionary keys.)
2. `string` vs `StringBuilder`: when do you use each?
3. What does `string.Empty` give you over `""`? (Nothing at runtime. It's a readability choice.)

---

## Day 65: LINQ Deeper

Day 8 covered `Where`, `Select`, `Any`, and `First`. This day covers the parts that cause interview questions.

```csharp
var nums = new List<int> { 1, 2, 3, 4 };

// Deferred execution: nothing runs until you enumerate
var big = nums.Where(n => n > 2);
nums.Add(10);
Console.WriteLine(big.Count());     // 3: the query ran now, and it saw the new 10
var snapshot = big.ToList();        // ToList() runs it and stores the result

// Grouping
var users = new[] { "bob", "alice", "carol", "dan" };
foreach (var g in users.GroupBy(u => u.Length))
{
    Console.WriteLine($"{g.Key}: {string.Join(", ", g)}");
}

// Sorting, then tie-breaking
var people = new[] { (Name: "Zoe", Age: 30), (Name: "Adam", Age: 30), (Name: "Bo", Age: 25) };
foreach (var p in people.OrderBy(p => p.Age).ThenBy(p => p.Name))
{
    Console.WriteLine(p.Name);      // Bo, Adam, Zoe
}

// Flatten a list of lists
var rows = new[] { new[] { 1, 2 }, new[] { 3 } };
Console.WriteLine(string.Join(",", rows.SelectMany(r => r)));  // 1,2,3

// Reduce to one value
Console.WriteLine(nums.Aggregate(0, (total, n) => total + n));  // 20 (including the added 10)
```

**Single vs SingleOrDefault:** `Single` throws if there isn't exactly one match. Use it when you expect exactly one.

**Pitfall:** enumerating a deferred query twice runs it twice. If it's expensive or reads a database, call `ToList()` once.

**IEnumerable vs IQueryable:** `IEnumerable` runs in memory (LINQ to Objects). `IQueryable` (from Entity Framework, for example) is translated to SQL and runs on the database. Filtering an `IQueryable` before `ToList()` avoids loading rows you don't need.

**Exercise:** given a list of `(User, Browser, Passed)` results, group by browser and print how many tests passed in each group.

**Check:** you can predict what `Count()` prints in the first example and explain why.

**Interview questions**
1. What is deferred execution, and why can it surprise you?
2. `First` vs `FirstOrDefault` vs `Single` vs `SingleOrDefault`?
3. `IEnumerable` vs `IQueryable`?

---

## Day 66: Playwright Tabs, Popups, and Windows

If you know Selenium, you're used to "window handles" and `SwitchTo().Window(...)`. Playwright doesn't need that. Each tab is a `IPage` object that you already hold, so you use the object directly.

```csharp
using Microsoft.Playwright;

// A link opens a new tab. Start listening before the click, then wait for the popup.
var popup = await Page.RunAndWaitForPopupAsync(async () =>
{
    await Page.GetByRole(AriaRole.Link, new() { Name = "Terms" }).ClickAsync();
});

await popup.WaitForLoadStateAsync();
Console.WriteLine(await popup.TitleAsync());
await popup.CloseAsync();

// Every tab in the same browser context
foreach (var tab in Page.Context.Pages)
{
    Console.WriteLine(tab.Url);
}
```

**Pitfall:** if you click first and listen afterwards, the popup can open before you start listening, and you miss it. `RunAndWaitForPopupAsync` avoids this race.

**Dialogs** (`alert`, `confirm`) are handled with an event:

```csharp
Page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
```

**Exercise:** create a test that opens a popup from a link, checks its title, and closes it. Use a page with a link that opens a new tab. If you don't have one, use a public demo site.

**Check:** you can explain why you don't switch windows in Playwright.

**Interview questions**
1. How do you handle multiple windows in Playwright? (Use the `IPage` object for each tab. Use `RunAndWaitForPopupAsync` to catch new tabs.)
2. Why is listening before the action important?

---

## Day 67: iframes

An iframe is a page inside a page. Playwright's `FrameLocator` lets you locate elements inside one without switching context.

```csharp
// One iframe
var payment = Page.FrameLocator("iframe[title='Payment']");
await payment.GetByLabel("Card number").FillAsync("4242424242424242");

// Nested iframes
var inner = Page.FrameLocator("#outer").FrameLocator("#inner");
await inner.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
```

`FrameLocator` waits and retries like any other locator. If the iframe is missing, you get a timeout that names the frame selector.

**Pitfall:** don't copy the element's selector from the browser's DevTools and forget the frame. The element isn't in the main document, so `Page.Locator(...)` will never find it.

**Exercise:** write a locator for a button inside an iframe on a page you control or a public demo. Then add one assertion that the button text is correct.

**Check:** you can explain why `FrameLocator` is needed.

**Interview questions**
1. How do you handle iframes in Playwright? (`FrameLocator`, which chains for nested frames.)
2. How is that different from Selenium's `SwitchTo().Frame()`? (Playwright doesn't change a "current frame". Every locator states its frame.)

---

## Day 68: Shadow DOM

A shadow root hides part of the DOM from normal queries. Playwright's CSS selectors pierce **open** shadow roots automatically, so you usually don't need extra syntax.

```csharp
// Works when the button lives inside an open shadow root of <my-login-card>
await Page.Locator("my-login-card button").ClickAsync();

// Chain explicitly when you want to step through each level
await Page.Locator("my-login-card >> button.submit").ClickAsync();

// Role and text locators also pierce open shadow roots
await Page.GetByRole(AriaRole.Button, new() { Name = "Submit" }).ClickAsync();
```

**Closed** shadow roots can't be reached by selectors. If you hit one, ask the developers for a `data-testid` attribute on an element outside the shadow root, or for an open shadow root in test builds.

**Exercise:** write a test that uses a web component on a public demo site. Show that the same button can be found with a CSS selector and with `GetByRole`.

**Check:** you can say what "open" and "closed" shadow roots mean for testing.

**Interview questions**
1. How do you locate elements inside shadow DOM in Playwright? (Selectors pierce open roots. Closed roots need developer help.)
2. Why does a shadow root break many Selenium locators? (Selenium's CSS engine doesn't pierce shadow roots, so you need `executeScript` to reach them.)

---

## Day 69: Building an Enterprise-Style Framework

A framework is a set of layers, and each layer has one job:

| Layer | Folder in this repo | Job |
|---|---|---|
| Core | `Core/` | Browser setup, base classes, configuration |
| Pages | `Pages/` | Page Objects: locators and actions for one screen |
| Flows | `Flows/` | Multi-step user journeys shared by tests |
| Tests | `Tests/` | Assertions and test cases only |

**Production practices to add next:**

- **Configuration:** read `BASE_URL`, `HEADLESS`, and browser choice from environment variables or `appsettings.json`, with the defaults in one place.
- **Tracing for failures:** record traces on failure so you can open them later.

```csharp
await Context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
// ... test steps ...
await Context.Tracing.StopAsync(new() { Path = "trace.zip" });
```

- **Test data:** keep users and expected messages in data files, not inside test code.
- **Parallel runs:** make each test create its own browser context, so tests don't share cookies or state.
- **Reports and CI:** publish results and traces from the pipeline (Day 63).

**Exercise:** compare this repo with the table above. Write a one-page list of what is missing, then pick one item (configuration or tracing) and add it.

**Check:** you can say which layer each class in this repo belongs to.

**Interview questions**
1. How do you structure a Playwright framework for a large team? (Layers, page objects, flows, shared config, data separation.)
2. How do you debug a test that fails only in CI? (Traces, screenshots, console logs, and checking the environment differences.)

---

## Day 70: Interview Prep Capstone

**Question bank.** Answer each question out loud in under two minutes.

**C#**
1. Value types vs reference types.
2. `string` vs `StringBuilder`.
3. Deferred execution in LINQ.
4. `async`/`await`: what happens to the thread? Why avoid `.Result` on a `Task`?
5. `IDisposable` and `using`.
6. Constructor injection and why it helps tests.

**Playwright**
1. Auto-waiting: what does Playwright wait for before a click?
2. Locator vs element handle: why are locators preferred?
3. How do you handle popups, iframes, and shadow DOM? (Days 66–68.)
4. How do you record a trace and use it?

**xUnit v3**
1. Fact vs Theory.
2. Lifecycle: what runs before and after each test?
3. `IClassFixture` vs `ICollectionFixture`.

**Framework and CI**
1. Page Object Model: what belongs in a page class and what doesn't?
2. How does a pipeline run your tests and publish the results?

**Mock interview (exercise).** Ask a friend, or record yourself, to ask you 5 random questions from the bank. Score each answer 0–2 (0: missed, 1: partly right, 2: clear with an example). Repeat on any question you scored below 2.

**Check:** you can answer at least 12 of the 15 questions with a score of 2.

**Done when:** you've finished the mock interview and written down your three weakest topics. Review those topics first.
