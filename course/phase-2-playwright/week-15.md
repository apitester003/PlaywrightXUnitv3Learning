# Week 15: Network and Debugging

This week you learn to control and observe the network (routing, mocking, intercepting, waiting for responses) and to debug failed tests (Trace Viewer, screenshots, videos, console logs). Day 90 is a review with interview questions.

The examples are in `Tests/Course/Week15/Week15Examples.cs`. All 12 tests were run with `dotnet test --filter "FullyQualifiedName~Week15Examples"` on .NET 10 with Playwright 1.58 and passed.

Most examples need no internet: they serve a tiny fake app from the made-up address `https://app.test/` by answering every request with `RouteAsync`. Three examples (`Day86_*`, `Day88_Trace`) use https://www.saucedemo.com.

The fake app page:

```html
<ul id="users"></ul><div id="error"></div><img id="logo" src="https://app.test/logo.png">
<script>
  fetch('https://app.test/api/users')
    .then(r => { if (!r.ok) throw new Error('HTTP ' + r.status); return r.json(); })
    .then(d => { document.getElementById('users').innerHTML = d.map(u => '<li>' + u.name + '</li>').join(''); })
    .catch(e => { document.getElementById('error').textContent = 'Failed: ' + e.message; });
</script>
```

The test helper that serves it:

```csharp
private async Task ServeAppAsync(Func<IRoute, Task> apiHandler)
{
    await context.RouteAsync("https://app.test/", r => r.FulfillAsync(new() { ContentType = "text/html", Body = AppHtml }));
    await context.RouteAsync("https://app.test/logo.png", r => r.FulfillAsync(new() { ContentType = "image/png", Body = "" }));
    await context.RouteAsync("https://app.test/api/users", apiHandler);
}
```

Because the page and the API are on the same fake host, the example adds a CORS header on the API answers (`Access-Control-Allow-Origin: *`) so the browser accepts them.

---

## Day 85: Request routing and mocking

### What routing is

`RouteAsync(pattern, handler)` tells the browser: "for every request matching this URL pattern, call my handler instead of going to the network". The handler receives an `IRoute` and must do exactly one of:

| Call | Effect |
|---|---|
| `FulfillAsync(...)` | Answer with a response you build (a mock) |
| `ContinueAsync()` | Let the request go to the real server (optionally modified) |
| `AbortAsync()` | Make the request fail |
| `FetchAsync()` | Send the real request yourself and get the real response (Day 86) |

If the handler does none of them, the request hangs. Routes can be set on the page (`page.RouteAsync`) or on the context (`context.RouteAsync`, covers every page in it). Set routes **before** the navigation or action that makes the request.

URL patterns are globs: `**/api/users`, `**/*.png`, `https://app.test/**`. A predicate or a regex also works.

### Mock a successful response (verified, `Day85_MockResponse`)

```csharp
await ServeAppAsync(r => r.FulfillAsync(new()
{
    Status = 200,
    ContentType = "application/json",
    Headers = new Dictionary<string, string> { ["Access-Control-Allow-Origin"] = "*" },
    Body = """[{"name":"Mock Ann"},{"name":"Mock Bob"}]"""
}));
await page.GotoAsync("https://app.test/");
await Assertions.Expect(page.Locator("#users li")).ToHaveTextAsync(new[] { "Mock Ann", "Mock Bob" });
```

### Mock a server error (verified, `Day85_MockServerError`)

```csharp
await ServeAppAsync(r => r.FulfillAsync(new() { Status = 500, ContentType = "application/json", Body = """{"error":"boom"}""",
    Headers = new Dictionary<string, string> { ["Access-Control-Allow-Origin"] = "*" } }));
await page.GotoAsync("https://app.test/");
await Assertions.Expect(page.Locator("#error")).ToHaveTextAsync("Failed: HTTP 500");
```

This is the biggest reason to mock: you can test how the UI behaves when the server fails, which is hard to cause on a real server.

### Abort (verified, `Day85_AbortRequest`)

```csharp
await ServeAppAsync(r => r.AbortAsync());
// the page's fetch fails, #error contains "Failed"
```

### Block by resource type (verified, `Day85_BlockImagesByType`)

```csharp
await context.RouteAsync("**/*", async r =>
{
    if (r.Request.ResourceType == "image") await r.AbortAsync();
    else ... 
});
```

Blocking images, fonts and analytics speeds up tests and removes noise.

### When to mock and when not

| Mock | Do not mock |
|---|---|
| The UI's reaction to errors, empty lists, slow or odd data | The thing you are actually testing end-to-end |
| Third-party services you do not control | Your own critical flow in a full end-to-end test |
| Slow or unstable dependencies | |

A UI suite that mocks everything proves only that the UI works with your mocks. Keep some real end-to-end tests.

### Exercise

1. Mock the users API with an empty list `[]` and assert the page shows no `li` elements.
2. Mock a 404 and a 500 and assert both show an error.
3. Count how many image requests the page makes by routing and counting, without blocking them.

---

## Day 86: Intercepting requests and responses

### Observe without changing

Events let you watch traffic (verified, `Day86_ObserveRequestsAndResponses`):

```csharp
List<string> requests = new();
List<int> statuses = new();
page.Request += (_, r) => requests.Add(r.Method + " " + r.Url);
page.Response += (_, r) => statuses.Add(r.Status);

await page.GotoAsync("https://www.saucedemo.com");
// requests contains an entry starting with "GET https://www.saucedemo.com"
// statuses contains 200
```

Events cannot change anything. They only watch.

### Modify a request (verified, `Day86_ModifyRequestHeaders`)

```csharp
await page.RouteAsync("https://www.saucedemo.com/**", async r =>
{
    Dictionary<string, string> headers = new(r.Request.Headers) { ["x-course"] = "week15" };
    await r.ContinueAsync(new() { Headers = headers });
});
```

`ContinueAsync` can change the headers, method, URL and body, then send the request on to the real server.

### Modify a real response (verified, `Day86_ModifyRealResponse`)

`FetchAsync` sends the real request, so you can take the real response, change it and fulfill with the changed version:

```csharp
await page.RouteAsync("**/*.css", async r =>
{
    IAPIResponse response = await r.FetchAsync();
    string body = await response.TextAsync();
    await r.FulfillAsync(new() { Response = response, Body = body + " body{background:rgb(255,0,0)!important}" });
});
await page.ReloadAsync();
string color = await page.EvaluateAsync<string>("getComputedStyle(document.body).backgroundColor");
// "rgb(255, 0, 0)"
```

The example makes the effect visible by injecting CSS. The same technique is used to change JSON from the real API (for example to force a "no items" case while keeping the rest real).

Note that in .NET the type of `FetchAsync`'s result is `IAPIResponse`. The first version of this example did not compile with `APIResponse`.

### Route order

When several routes match a request, the most recently registered one runs first. If it calls `ContinueAsync`, the request goes on to the network, not to the older routes (the docs have a fallback mechanism for that, which this course did not test).

### Exercise

1. Log all failed requests (status 400 or higher) during a saucedemo run.
2. Add a custom header to every request and verify it with the `Request` event.
3. Use `FetchAsync` to change a real JSON response (you will need a site with a JSON API; mock one with `ServeAppAsync` if you do not have one).

---

## Day 87: Waiting for network responses

### The problem

Sometimes the right signal that "the page is ready" is a specific API call finishing. Wait for it explicitly, and start waiting **before** the action that triggers it.

### Wait for a response (verified, `Day87_WaitForResponse`)

```csharp
IResponse response = await page.RunAndWaitForResponseAsync(
    async () => await page.GotoAsync("https://app.test/"),
    r => r.Url.EndsWith("/api/users") && r.Status == 200);

JsonElement json = await response.JsonAsync() ?? default;
json[0].GetProperty("name").GetString();   // "Ann"
```

The first argument is the action, the second is a predicate that picks the response you want. `response.JsonAsync()` returns a `JsonElement?`.

### Wait for a request (verified, `Day87_WaitForRequest`)

```csharp
IRequest request = await page.RunAndWaitForRequestAsync(
    async () => await page.GotoAsync("https://app.test/"),
    "**/api/users");
// request.Method == "GET"
```

You can use it to check what the UI sent: `request.PostData`, `request.Headers`, and so on.

### Use it to check what the UI sends

For example, after clicking "Save", wait for the request to `**/api/orders`, then assert on its body. This tests the contract between UI and API without a database.

### Prefer UI signals when you can

Waiting for a response ties the test to an implementation detail (the URL of an API). If a UI change proves the same thing (a row appears), assert on the UI. Use response waits when there is no visible signal, or when you want to verify the request itself.

### Exercise

1. Click a button that triggers a POST and assert on the request body.
2. Wait for a response with status 500 (mock it) and assert the UI shows an error.
3. Replace a `WaitForTimeoutAsync` in an older test with a wait for the response it was really waiting for.

---

## Day 88: Trace Viewer

### What a trace is

A trace is a `.zip` file with a recording of a test: a timeline of every Playwright action, DOM snapshots before and after each action, screenshots, network calls and console messages. It is the best tool for understanding why a test failed, especially in CI where you cannot watch the browser.

### Record one (verified, `Day88_Trace`)

```csharp
await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = false });

await page.GotoAsync("https://www.saucedemo.com");
await page.Locator("[data-test='username']").FillAsync("standard_user");

await context.Tracing.StopAsync(new() { Path = "trace.zip" });
```

The test checks that the file exists and is larger than 1000 bytes. `Sources = true` also bundles your C# source, which this course did not try.

### Open it

```bash
pwsh bin/Debug/net10.0/playwright.ps1 show-trace trace.zip
```

I did not run this command while writing the course (it opens a window). The Playwright documentation also offers an online viewer at trace.playwright.dev where you drop the zip file.

In the viewer you can: step through actions, see the page before and after each one, hover the timeline, look at the network and console tabs, and see which locator was used and how long it waited.

### Tracing every failing test

The usual practice is to start tracing in test setup and, in teardown, **save the trace only when the test failed**. Doing this properly needs knowledge of xUnit v3's test context (Phase 3) and fixtures (Phase 4), so this course comes back to it there.

### Exercise

1. Record a trace of a login on saucedemo and open it in the viewer. Find the step where `FillAsync` ran and look at the "before" and "after" snapshots.
2. Make the test fail on purpose (wrong locator), record the trace and find the failing step.
3. Find the network call for the main page in the trace.

---

## Day 89: Screenshots, videos, console logs

### Screenshots (verified, `Day89_ScreenshotsAndConsole`)

```csharp
await page.ScreenshotAsync(new() { Path = "page.png", FullPage = true });   // whole page
await page.Locator("#h").ScreenshotAsync(new() { Path = "el.png" });        // one element
```

The test checks both files exist and are not empty.

### Console messages and page errors (verified)

```csharp
List<string> console = new();
List<string> errors = new();
page.Console += (_, m) => console.Add(m.Type + ":" + m.Text);
page.PageError += (_, e) => errors.Add(e);
```

For a page that runs `console.log('hello log'); console.warn('careful');` and then throws an uncaught `Error('kaboom')`, the test found:

- `console` contains `log:hello log` and `warning:careful` (the type of `console.warn` is `warning`, not `warn`).
- `errors` contains an entry with `kaboom` (uncaught JavaScript errors arrive through `PageError`, not `Console`).

Use this to fail tests when the app logs JavaScript errors you did not expect. The example uses `WaitForTimeoutAsync(300)` to let the delayed error happen, a fixed sleep that this course discourages. A better approach is to wait until `errors` has an element.

### Video (verified, `Day89_Video`)

```csharp
IBrowserContext ctx = await browser.NewContextAsync(new() { RecordVideoDir = "videos" });
IPage page = await ctx.NewPageAsync();
// ... run the test ...
await ctx.CloseAsync();    // the video file is finalized when the context closes
```

The folder then contains one `.webm` file per page. Videos are written when the context closes, so you must close it before reading the file.

### Choosing the right tool

| Tool | Best for | Cost |
|---|---|---|
| Screenshot on failure | A quick look at the last state | Small |
| Video | Seeing the whole flow in CI | Medium, slows runs |
| Trace | Understanding exactly why it failed (snapshots, network, console) | Medium |
| Console/page error events | Catching JavaScript errors | Very small |

Common practice: trace and screenshot on failure; video only when you need it.

### Exercise

1. Take a full-page screenshot of the saucedemo inventory page and an element screenshot of the first product.
2. Record a video of a login test and play it.
3. Fail a test whenever `PageError` fires, with a message containing the error.

---

## Day 90: Review and practice

### Recap

| Day | Topic | Key points |
|---|---|---|
| 85 | Routing and mocking | `RouteAsync` + `FulfillAsync` / `ContinueAsync` / `AbortAsync`; set routes before the action; mock to test error paths |
| 86 | Intercepting | Events observe, routes change; `FetchAsync` then `FulfillAsync` to alter a real response; type is `IAPIResponse` |
| 87 | Waiting for network | `RunAndWaitForResponseAsync` / `RunAndWaitForRequestAsync`; start before the trigger |
| 88 | Trace Viewer | `Tracing.StartAsync` / `StopAsync(Path)`; open with `show-trace` or trace.playwright.dev |
| 89 | Screenshots, videos, console | `ScreenshotAsync`, `RecordVideoDir`, `Console` and `PageError` events |

### Practice questions

1. What must a route handler always do?
2. Which method lets a request continue to the real server?
3. Why set a route before `GotoAsync`?
4. What is the difference between the `Response` event and `RouteAsync`?
5. How do you change a real response's body?
6. When is a video file complete?
7. Where do uncaught JavaScript errors show up?
8. Name two things you can see in a trace that you cannot see in a screenshot.

<details>
<summary>Answers</summary>

1. Fulfill, continue or abort the request (otherwise it hangs).
2. `ContinueAsync()`.
3. The requests happen during navigation; a route added afterwards misses them.
4. The event only observes; a route can change or stop the request.
5. `FetchAsync()` the real response, then `FulfillAsync` with a new `Body` and `Response = response`.
6. After the context is closed.
7. The `PageError` event.
8. Any two of: DOM snapshots over time, network calls, console messages, action timings and locators.

</details>

### Mini-program: failure lab

Using `ServeAppAsync`-style routing, build a test class for the fake users page with these cases: normal data (3 users), empty data, a 500, a request that never answers (use a very short timeout and describe what you see), and a very slow answer (use `Task.Delay` inside the route handler to delay the answer, and assert on a loading state if you add one to the page). Record a trace for the "never answers" test and say what the trace shows.

### Interview questions

**1. How do you mock an API response in Playwright?**
Answer: Register a route for the URL pattern before the action, and call `FulfillAsync` with status, headers and body. Remember CORS headers if the page is on a different origin.
**Follow-up:** When should you not mock? Answer: In the end-to-end flow you are meant to verify; mocks only prove the UI works with the mocks.

**2. What is the difference between `ContinueAsync`, `FulfillAsync`, `AbortAsync` and `FetchAsync`?**
Answer: Continue sends the (optionally modified) request to the real server; fulfill answers without the network; abort fails the request; fetch sends the real request and gives you the response so you can modify it before fulfilling.

**3. How do you test that the UI handles a 500 error?**
Answer: Route the API call, fulfill with status 500, and assert on the error message in the UI.

**4. How do you wait for a specific API call to finish?**
Answer: `RunAndWaitForResponseAsync` with the action and a predicate, so waiting starts before the action.

**5. What is the Trace Viewer, and when do you use it?**
Answer: A recording with timeline, DOM snapshots, network and console. Use it to debug failures, especially from CI. Record with `Tracing.StartAsync` and `StopAsync(Path)`.
**Follow-up:** Would you record a trace for every test? Answer: Usually keep traces only for failed tests to save time and space.

**6. How do you catch JavaScript errors in the app during a test?**
Answer: Subscribe to `page.PageError` (and `Console` for logged errors) and fail the test if the list is not empty.

**7. Screenshot, video or trace?**
Answer: Screenshot for a quick look, video to see a whole run, trace for the full detail. Traces are usually the most useful for debugging.

**8. How can network control make tests faster?**
Answer: Block images, fonts and analytics; mock slow third-party calls.

**9. Code:** Block all images.
```csharp
await context.RouteAsync("**/*", async r =>
{
    if (r.Request.ResourceType == "image") await r.AbortAsync();
    else await r.ContinueAsync();
});
```
(In the verified example the `else` branch fulfilled fake content because the host does not exist; with real hosts use `ContinueAsync`.)

**10. Code:** Save a trace around a test body.
```csharp
await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true });
// ... test steps ...
await context.Tracing.StopAsync(new() { Path = "trace.zip" });
```
