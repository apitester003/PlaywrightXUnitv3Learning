# Week 19: Lifecycle and Fixtures

Who creates your test objects, when, and who cleans up? This week covers the constructor and `IDisposable`, `IAsyncLifetime`, class fixtures, collection fixtures, assembly fixtures, and how to choose between them. This is the foundation for the Playwright browser setup you will design in Phase 4.

The examples are in `Tests/Course/Week19/Week19Examples.cs`. They were run with `dotnet test --filter "FullyQualifiedName~Week19"` on .NET 10 with `xunit.v3` 3.2.2. Result: 9 passed, 0 failed.

What is verified and what is not:
- Verified: the constructor, `IDisposable`, `IAsyncLifetime`, `IClassFixture`, `ICollectionFixture` and assembly fixture examples below.
- Not verified: the Playwright browser fixture sketch (Day 113). Phase 4 builds and tests the real one.

**Recap you rely on:** `IDisposable` and `using` (Week 6), `async`/`ValueTask` (Week 7), `BaseTest : IAsyncLifetime` (Week 9), `ShoppingCart` (Week 17).

---

## Day 109: Constructor and `IDisposable`

### The rule: one new instance per test

xUnit creates a new instance of the test class for every test method. So the constructor is your per-test setup and `Dispose` is your per-test teardown.

```csharp
public class ConstructorLifecycleTests : IDisposable
{
    private readonly ShoppingCart _cart = new();

    public ConstructorLifecycleTests()
    {
        _cart.Add(new CartItem("Backpack", 10m));   // runs before EVERY test
    }

    public void Dispose() { /* runs after EVERY test */ }

    [Fact]
    public void Day109_EachTest_GetsFreshCart_A()
    {
        _cart.Add(new CartItem("Onesie", 5m));
        Assert.Equal(2, _cart.Items.Count);
    }

    [Fact]
    public void Day109_EachTest_GetsFreshCart_B()
    {
        Assert.Single(_cart.Items);   // still 1: the Onesie from test A is gone
    }
}
```

Both tests pass in any order. That is the point: state set in one test does not reach another.

### Order of events for one test
1. Constructor
2. Test method
3. `Dispose` (if implemented)

Then the next test starts again at step 1 with a new instance.

### Comparison
| xUnit | NUnit | MSTest |
|---|---|---|
| Constructor | `[SetUp]` | `[TestInitialize]` |
| `Dispose()` | `[TearDown]` | `[TestCleanup]` |

### Pitfalls
- **Heavy work in a constructor.** It runs once per test. A browser launch here means 1,000 launches for 1,000 tests.
- **Constructors cannot be `async`.** For async set-up use `IAsyncLifetime` (Day 110).
- **Static fields are shared.** A `static` counter or list survives across instances and across tests. That breaks the isolation xUnit gives you, and in parallel runs it becomes a race. Use `Interlocked` or avoid statics.
- **If the constructor throws, `Dispose` is not called.** Whatever the constructor created before the exception is not cleaned up automatically.

### Exercise
1. Add a `static int` counter incremented in the constructor and read it in a test. Run the class twice in a row and compare. What does the number tell you?
2. Make the constructor throw. Observe whether `Dispose` runs. Write down what you see (I did not run this experiment).
3. List three resources in UI testing that must be disposed.

**Solution notes:** (3) Browser, browser context, `IPlaywright`. These are what `BaseTest.DisposeAsync` closes.

---

## Day 110: `IAsyncLifetime`

Playwright is async, so set-up and teardown are async. xUnit's answer is `IAsyncLifetime`:

```csharp
public class AsyncLifetimeTests : IAsyncLifetime
{
    private ShoppingCart _cart = null!;

    public async ValueTask InitializeAsync()
    {
        await Task.Delay(1, TestContext.Current.CancellationToken);
        _cart = new ShoppingCart();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

### Order of events
1. Constructor
2. `InitializeAsync()`
3. Test method
4. `DisposeAsync()` (and `Dispose()` too if the class implements `IDisposable`)

This is what `BaseTest` does: `InitializeAsync` launches Playwright, the browser, a context and a page; `DisposeAsync` closes them.

### v3 detail: `ValueTask`
In v2 these methods returned `Task`. In v3 they return `ValueTask`. Code copied from older blogs will not compile unchanged.

### Where `null!` comes from
`_cart` is assigned in `InitializeAsync`, not the constructor, so the compiler cannot prove it is non-null. `= null!` tells the compiler you take responsibility. That is the same reason `BaseTest` initializes its properties with `null`.

### Looking at the repo's `BaseTest`
Reading `BaseTest.cs` with this week's rules in mind:
- `Page`, `Browser` and `Playwright` are set to `null` and the build warns (CS8625) because nullable annotations are on. The `null!` style used in the Week 16 examples avoids that warning.
- `DisposeAsync` closes the page and browser but not the context. It also throws a `NullReferenceException` if `InitializeAsync` failed before `Page` was assigned.
- Phase 4 will fix these on purpose. Note them now.

### Making teardown safe
```csharp
public async ValueTask DisposeAsync()
{
    if (_context is not null) await _context.CloseAsync();
    if (_browser is not null) await _browser.CloseAsync();
    _playwright?.Dispose();
}
```
Teardown must tolerate a half-finished set-up, because a failure in `InitializeAsync` is exactly when you most need clean-up.

### Exercise
1. Add `_log` entries in the constructor, `InitializeAsync`, the test and `DisposeAsync`. Print them with `ITestOutputHelper` (Week 20) or inspect them in a debugger. Write the order you observe.
2. Make `InitializeAsync` throw. What happens to the test? Does `DisposeAsync` run? Run it and record the result rather than guessing.
3. Rewrite the repo's `BaseTest.DisposeAsync` in the safe form above (do it in a copy, not in the repo yet).

---

## Day 111: Class fixtures

Sometimes setup is expensive and safe to share: a database seed, a config load, a logged-in session file. A class fixture is created once and shared by all tests in one class.

```csharp
public class CounterFixture : IAsyncLifetime
{
    public static int Created;
    public Guid Id { get; } = Guid.NewGuid();

    public ValueTask InitializeAsync() { Interlocked.Increment(ref Created); return ValueTask.CompletedTask; }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public class ClassFixtureTests : IClassFixture<CounterFixture>
{
    private readonly CounterFixture _fixture;
    public ClassFixtureTests(CounterFixture fixture) => _fixture = fixture;

    [Fact]
    public void Day111_Fixture_IsShared_B()
    {
        Assert.Equal(1, CounterFixture.Created);   // created once for the whole class
    }
}
```

### How it works
- Implement `IClassFixture<T>` on the test class.
- Add a constructor parameter of type `T`. xUnit passes the shared instance.
- The fixture is created before the first test in the class and disposed after the last.
- The fixture can implement `IDisposable`, `IAsyncDisposable` or `IAsyncLifetime`.

### The danger: shared mutable state
A fixture is shared, so mutations made by one test are visible to the next. In the example `Calls` is incremented with `Interlocked` to stay safe. Rules:
- Prefer **read-only** fixtures: configuration, immutable data, a connection pool.
- If a fixture holds mutable data, each test must create and clean its own data inside it.
- Tests within one class run one after another by default, but still avoid hidden order dependencies.

### Fixture constructor parameters
A fixture can request `IMessageSink` for diagnostic output. I have not verified the v3 variant of this, so look it up in the xUnit docs if you need it.

### Exercise
1. Create a `ConfigFixture` that reads `TestData/login-cases.json` once and exposes it. Use it from two tests.
2. What breaks if your fixture exposes a `List<T>` that tests add to?
3. Why is a single shared Playwright **page** a bad fixture? Name two failure modes.

**Solutions:** (2) Tests become order-dependent and flaky in parallel. (3) State leaks between tests (cookies, cart, URL), and one failure leaves the page in a broken state for the rest.

---

## Day 112: Collection and assembly fixtures

### Collection fixtures
A class fixture is shared inside one class. A collection fixture is shared across several classes.

```csharp
public class SharedResourceFixture { public string Name => "shared"; }

[CollectionDefinition("Shared resource")]
public class SharedResourceCollection : ICollectionFixture<SharedResourceFixture> { }

[Collection("Shared resource")]
public class CollectionFixtureOneTests
{
    public CollectionFixtureOneTests(SharedResourceFixture fixture) { ... }
}

[Collection("Shared resource")]
public class CollectionFixtureTwoTests
{
    public CollectionFixtureTwoTests(SharedResourceFixture fixture) { ... }
}
```

Steps: (1) define the fixture, (2) define a collection class marked `[CollectionDefinition]` that implements `ICollectionFixture<T>`, (3) mark each test class with `[Collection("name")]`. The test in `CollectionFixtureTwoTests` asserts that only one fixture instance was created, and it passed.

### Important side effect: collections are not parallel
Test classes in the **same collection run one after another**, not in parallel. Putting classes in a collection to share a fixture therefore reduces parallelism. That is a real trade-off, and Week 20 (Day 117) covers it in detail.

### Assembly fixtures
An assembly fixture is created once for the whole test assembly. In v3, you register it with an assembly-level attribute:

```csharp
[assembly: AssemblyFixture(typeof(GlobalFixture))]
```
and take it as a constructor parameter in any test class. Verified in `Week19Examples.cs`: `AssemblyFixtureTests` asserts it is created once. Use it for truly global, read-only things: loading configuration, starting a mock server, or a one-time login that saves a storage-state file.

### Exercise
1. Add a second test class to the collection and prove that the fixture is still created once.
2. Make two classes in the same collection each sleep 2 seconds in a test. Measure the total time with and without the collection attribute. Record the numbers from your run.
3. Create your own assembly fixture and verify it is built exactly once per run. Record what you see.

---

## Day 113: Choosing the right fixture

| Need | Use | Scope | Parallel impact |
|---|---|---|---|
| Fresh objects for each test | Constructor / `IDisposable` | Test | None |
| Async set-up per test | `IAsyncLifetime` on the class | Test | None |
| Share expensive read-only data in one class | `IClassFixture<T>` | Class | None across classes |
| Share across several classes | `ICollectionFixture<T>` | Collection | Classes in the collection serialise |
| Share across the whole run | Assembly fixture | Assembly | None |

### Decision questions
1. Is the state **mutable**? If yes, do not share it, or isolate it per test.
2. Is it **expensive**? If not, a constructor is simpler.
3. Do other classes need it? If yes, assembly or collection.
4. Does sharing it force **serial** execution? Weigh the saved set-up time against lost parallelism.

### Applying it to Playwright
| Resource | Share? | Why |
|---|---|---|
| `IPlaywright` | Yes (assembly or collection) | Starting the driver is slow; the object is safe to share |
| `IBrowser` | Often yes | Launching a browser is slow; contexts give isolation |
| `IBrowserContext` | **No**, one per test | It holds cookies and storage; this is your isolation boundary |
| `IPage` | **No**, one per test | Holds navigation state |

A common architecture is: share the browser, create a fresh context and page per test. This is faster than the repo's current `BaseTest`, which launches a new browser for every test. I have not benchmarked the difference on your machine. Phase 4 measures it before changing `BaseTest`.

### Sketch (not verified, Phase 4 will build and test the real one)
```csharp
public class BrowserFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        await Browser.CloseAsync();
        Playwright.Dispose();
    }
}
```
Each test class then takes the fixture and creates its own context and page in `InitializeAsync`.

### Exercise
1. For each of these, choose a scope and justify it: a database connection string, a JWT token valid for 1 hour, a saucedemo cart, a mock server on port 5000, a Bogus `Faker`.
2. Draw the lifecycle of one UI test class using a browser fixture and a per-test context. Mark create and dispose points.

**Solution notes (1):** Connection string: assembly (read-only). Token: assembly, but handle expiry (Day 203). Cart: test (mutable). Mock server: assembly or collection (shared port, start once). Bogus `Faker`: per test if you need a repeatable seed, otherwise treat it as not thread-safe unless the docs say otherwise (I have not checked).

---

## Day 114: Review, practice and interview questions

### Mini-project: a layered lifecycle
Build, in `Tests/Course/Week19/`, a test class pair that proves, with assertions, each of these:
1. A new instance is created per test (use a per-instance GUID).
2. A class fixture is created once per class.
3. A collection fixture is created once across two classes.
4. `DisposeAsync` runs even when the test fails (log to a static `ConcurrentQueue` and assert in a later, ordered check, or inspect it in the debugger).
5. A throwing `InitializeAsync` is reported clearly.

### Review checklist
- [ ] I can state the order: constructor, `InitializeAsync`, test, `DisposeAsync` (and that `Dispose` is skipped when `DisposeAsync` exists).
- [ ] I know why constructors cannot do async work.
- [ ] I can pick between class, collection and assembly fixtures.
- [ ] I know sharing through a collection costs parallelism.
- [ ] I can explain which Playwright objects to share and which not.

### Interview questions

**Junior**
1. How do you do set-up before each test in xUnit? *(Constructor; async: `IAsyncLifetime`.)*
2. How do you do teardown? *(`IDisposable` / `IAsyncDisposable` / `DisposeAsync`.)*

**Mid**
3. What is a class fixture and when would you use one? *(Shared, expensive, usually read-only per class.)*
4. Why does xUnit create a new instance per test? *(Isolation.)*
5. What is the danger of static fields in test classes? *(Shared across tests and threads; ordering and race problems.)*

**Senior**
6. Compare class, collection and assembly fixtures. *(Scope, and the parallelism cost of collections.)*
7. Your suite launches a browser per test and takes 40 minutes. What do you change? *(Share the browser through a fixture and create a context per test. Measure first, then change.)*
8. `InitializeAsync` throws halfway. What do you worry about? *(Leaked resources, because teardown may not run the way you expect. Write teardown that tolerates null.)*

**Principal / architect**
9. Design the lifecycle for a framework with UI, API and DB tests in one repo. *(Assembly: config, logging, mock servers. Collection or class: browser, DB connection. Test: context, page, test data. State the isolation guarantees and the cost of each share.)*
10. How do you prevent teams from sharing mutable state through fixtures? *(Convention and review, read-only fixture interfaces, and per-test data creation helpers.)*

### On-the-job scenarios
1. **The cart that remembers.** A test fails only when run after another one. You find a shared fixture holding a mutable cart. Describe the fix and how you will prevent a repeat.
2. **The 40-minute suite.** Your lead asks you to halve run time. Before you touch code, list the numbers you will measure.
3. **The leaked browsers.** CI machines run out of memory after many runs because browser processes stay alive. Say where you look first. Hint: failed set-up with no safe teardown.

### Checkpoint
You are ready for Week 20 if you can justify the lifecycle of every Playwright object in a test without notes.

**Up next:** Week 20, traits, output, parallelism, ordering and flaky tests.
