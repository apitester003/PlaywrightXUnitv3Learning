# Week 7: Async and file I/O

This week covers the features every real test project depends on: delegates and lambdas (code you pass around as data), `async`/`await` (how `await page.ClickAsync(...)` really works), file I/O, and JSON with `System.Text.Json` (reading test data from files). Day 42 reviews everything.

All examples target .NET 10 with `Nullable` and implicit usings enabled (the defaults of `dotnet new console`). Each C# block marked as a complete program can be pasted into `Program.cs` of a fresh console project and run with `dotnet run`.

---

## Day 37: Delegates, lambdas, `Func`, `Action`

### 1. The idea: a variable that holds a method

So far, variables hold data: numbers, strings, objects. A **delegate** is a type whose values are *methods*. You can store a method in a variable, pass it to another method, and call it later. This is how you tell a helper "run this code when you are ready" without the helper knowing what the code does.

A delegate type declares a *signature*: the parameter types and the return type of the methods it can hold.

```csharp
// Complete program: Program.cs
int Add(int a, int b) => a + b;
int Multiply(int a, int b) => a * b;

BinaryOp op = Add;                 // store the method Add (no parentheses: we are not calling it)
Console.WriteLine(op(2, 3));       // 5   (calling the variable calls the method)

op = Multiply;                     // same variable, different method
Console.WriteLine(op(2, 3));       // 6

Console.WriteLine(Apply(Add, 10, 20));       // 30
Console.WriteLine(Apply(Multiply, 10, 20));  // 200

static int Apply(BinaryOp operation, int x, int y) => operation(x, y);

delegate int BinaryOp(int a, int b);
```

```text
5
6
30
200
```

Key points:

- `delegate int BinaryOp(int a, int b);` declares a type. Any method that takes two `int`s and returns `int` fits.
- `op = Add;` writes the method name without `()`. With `()` you would be calling it and storing the result.
- `Apply` does not know or care which operation it runs. That is the whole point.

### 2. `Func`, `Action`, `Predicate`: the built-in delegate types

Declaring your own delegate type for every shape is tedious, so .NET ships generic ones. In practice you use these almost exclusively:

| Type | Meaning | Example |
|---|---|---|
| `Action` | no parameters, returns nothing | `Action a = () => Console.WriteLine("hi");` |
| `Action<T1, T2>` | parameters, returns nothing (up to 16 parameters) | `Action<string, int>` |
| `Func<TResult>` | no parameters, returns `TResult` | `Func<int>` |
| `Func<T1, T2, TResult>` | parameters, **last type argument is the return type** | `Func<int, int, int>` |
| `Predicate<T>` | one parameter, returns `bool` (same as `Func<T, bool>`) | `Predicate<string>` |

The rule to memorize: **`Action` returns void; `Func` returns something, and its last type argument is the return type.**

```csharp
// Complete program
Action greet = () => Console.WriteLine("Hello");
Action<string> greetPerson = name => Console.WriteLine($"Hello, {name}");
Func<int> getAnswer = () => 42;
Func<int, int, int> add = (a, b) => a + b;
Func<string, bool> isLong = s => s.Length > 5;
Predicate<string> isEmpty = s => s.Length == 0;

greet();                                   // Hello
greetPerson("Ada");                        // Hello, Ada
Console.WriteLine(getAnswer());            // 42
Console.WriteLine(add(4, 5));              // 9
Console.WriteLine(isLong("Playwright"));   // True
Console.WriteLine(isEmpty(""));            // True
```

```text
Hello
Hello, Ada
42
9
True
True
```

### 3. Lambda expressions

The `=>` syntax above is a **lambda expression**: a method written inline, without a name. Read `=>` as "goes to".

```csharp
// Complete program
Func<int, int> square1 = (int x) => { return x * x; };   // statement lambda, full form
Func<int, int> square2 = (x) => x * x;                   // expression lambda, types inferred
Func<int, int> square3 = x => x * x;                     // one parameter: parentheses optional
Func<int> roll = () => 4;                                // no parameters: empty parentheses required

Console.WriteLine(square1(5));  // 25
Console.WriteLine(square2(6));  // 36
Console.WriteLine(square3(7));  // 49
Console.WriteLine(roll());      // 4

Func<int, int, string> describe = (a, b) =>
{
    var sum = a + b;                 // a statement lambda can have many statements
    return $"{a} + {b} = {sum}";
};
Console.WriteLine(describe(2, 3));   // 2 + 3 = 5
```

```text
25
36
49
4
2 + 3 = 5
```

- **Expression lambda**: `x => x * x`. One expression, its value is the return value.
- **Statement lambda**: `x => { ...; return ...; }`. Use braces when you need several statements.
- You already used lambdas in Week 6: `list.Where(x => x > 3)` passes a `Func<int, bool>` to `Where`.

### 4. Passing behaviour into your own methods

Writing a method that *accepts* a delegate is the useful skill. Here is a method that runs any action several times and another that transforms a list:

```csharp
// Complete program
Repeat(3, () => Console.WriteLine("tick"));

var names = new List<string> { "ada", "linus", "grace" };
var upper = Transform(names, n => n.ToUpper());
Console.WriteLine(string.Join(", ", upper));      // ADA, LINUS, GRACE

var lengths = Transform(names, n => n.Length);
Console.WriteLine(string.Join(", ", lengths));    // 3, 5, 5

static void Repeat(int times, Action body)
{
    for (var i = 0; i < times; i++)
        body();
}

static List<TOut> Transform<TIn, TOut>(List<TIn> source, Func<TIn, TOut> convert)
{
    var result = new List<TOut>();
    foreach (var item in source)
        result.Add(convert(item));
    return result;
}
```

```text
tick
tick
tick
ADA, LINUS, GRACE
3, 5, 5
```

`Transform` is generic (Week 5) and is exactly what LINQ's `Select` does.

### 5. Closures: lambdas capture variables

A lambda can use variables from the method around it. It captures the *variable itself*, not a copy of its value at that moment.

```csharp
// Complete program
var counter = 0;
Action increment = () => counter++;

increment();
increment();
Console.WriteLine(counter);   // 2   (the lambda changed the outer variable)

var message = "first";
Func<string> show = () => message;
message = "second";
Console.WriteLine(show());    // second   (the lambda reads the current value, not "first")
```

```text
2
second
```

This is convenient, but it is also a source of surprises: if a lambda runs later, it sees whatever the variable holds *then*.

### 6. Multicast delegates and events (short introduction)

A delegate can hold more than one method. Calling it calls all of them in order. Add with `+=`, remove with `-=`. The `event` keyword wraps a delegate field so outside code can only subscribe/unsubscribe, not call or replace it.

```csharp
// Complete program
var runner = new StepRunner();
runner.StepFinished += name => Console.WriteLine($"[log] finished {name}");
runner.StepFinished += name => Console.WriteLine($"[report] {name} recorded");

runner.Run("login");
runner.Run("checkout");

class StepRunner
{
    public event Action<string>? StepFinished;   // nullable: nobody may be subscribed

    public void Run(string name)
    {
        Console.WriteLine($"running {name}");
        StepFinished?.Invoke(name);              // ?. avoids a NullReferenceException when there are no subscribers
    }
}
```

```text
running login
[log] finished login
[report] login recorded
running checkout
[log] finished checkout
[report] checkout recorded
```

xUnit and Playwright both expose events (for example Playwright's `page.Console` and `page.Request` events) that you subscribe to with `+=` and a lambda.

### 7. A test-automation helper: polling with `Func<bool>`

Real tests wait for conditions. A reusable `WaitUntil` that accepts the condition as a `Func<bool>` is a classic pattern (Playwright has its own auto-waiting, but you will write helpers like this for APIs, files and queues).

```csharp
// Complete program
var start = DateTime.UtcNow;

// the condition becomes true after about 300 ms
var ok = WaitUntil(() => DateTime.UtcNow - start > TimeSpan.FromMilliseconds(300),
                   timeoutMs: 2000, pollMs: 50);
Console.WriteLine($"Condition met: {ok}");        // Condition met: True

var never = WaitUntil(() => false, timeoutMs: 200, pollMs: 50);
Console.WriteLine($"Condition met: {never}");     // Condition met: False

Step("open page", () => Console.WriteLine("  ...opening"));
Step("broken step", () => throw new InvalidOperationException("boom"));

static bool WaitUntil(Func<bool> condition, int timeoutMs, int pollMs)
{
    var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
    while (DateTime.UtcNow < deadline)
    {
        if (condition()) return true;
        Thread.Sleep(pollMs);
    }
    return false;
}

static void Step(string name, Action body)
{
    Console.WriteLine($"STEP: {name}");
    try
    {
        body();
        Console.WriteLine("  PASS");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  FAIL: {ex.Message}");
    }
}
```

```text
Condition met: True
Condition met: False
STEP: open page
  ...opening
  PASS
STEP: broken step
  FAIL: boom
```

(`Thread.Sleep` is fine here because the method is synchronous. Tomorrow you will meet its async replacement, `await Task.Delay`.)

### Common mistakes

- Writing `op = Add();` instead of `op = Add;`. With parentheses you call the method and try to store its result.
- Confusing `Func` and `Action`: if the lambda must return a value, you need `Func<..., TResult>`. `Action` lambdas discard or have no return value.
- Forgetting that lambdas capture variables, not values. Capturing a loop variable or something you later change leads to "why does it print the new value?" bugs.
- Invoking an event or delegate that might be `null` without `?.Invoke(...)`.

### Why this matters for test automation

- Retry/wait helpers, `Step(name, action)` wrappers for reporting, and test-data builders all take lambdas.
- Playwright .NET uses delegates and lambdas in `page.RouteAsync("**/*", route => ...)` and in event handlers (`page.Dialog += (_, dialog) => ...`).
- LINQ, which you use to filter and assert over test data, is built on `Func<T, bool>` and `Func<T, TResult>`.

### Interview questions

**Q: What is a delegate?**
A type-safe reference to a method. It defines a signature, and any method with a matching signature can be assigned to it and invoked through it.

**Q: Difference between `Func` and `Action`?**
`Action` represents a method that returns `void`; `Func<..., TResult>` represents one that returns a value, with the return type as the last type argument.

**Q: What is a closure?**
A lambda (or anonymous method) that uses variables from its enclosing scope. It captures the variable itself, so later changes are visible to the lambda.

**Q: What is the difference between a delegate and an event?**
An event is a delegate field with restricted access: outside code can only subscribe (`+=`) and unsubscribe (`-=`), not invoke it or overwrite the list of subscribers.

### Exercise

1. Write `static List<T> Filter<T>(List<T> items, Func<T, bool> keep)` without using LINQ, and use it to keep test names that start with `"Login"`.
2. Write `static void Retry(int attempts, Action action)` that calls `action`, and if it throws, tries again up to `attempts` times, printing `Attempt n failed` each time, and rethrows the last exception when it runs out of attempts. Test it with a lambda that fails twice and then succeeds (use a captured counter).
3. Create a class `Button` with an event `Action<string> Clicked`. Subscribe two lambdas and raise the event with `?.Invoke`.

### Check

You are done when you can, without looking: (a) write the type of a lambda that takes a `string` and returns its length, (b) say why `Func<int, string>` takes the return type last, (c) explain why `increment()` in the closure example changes `counter`.

---

## Day 38: `Task`, `async`, `await`

### 1. The problem: waiting is slow

A browser test spends nearly all its time waiting: for a page to load, for a server to answer, for an element to appear. While a thread waits, it can do nothing else. If a program blocked a thread for every wait, it would waste threads (and UIs would freeze).

**Asynchronous programming** lets a method say: "start this slow operation, and when it finishes, continue here. Meanwhile, give my thread back." C# makes this readable with two keywords, `async` and `await`, and one type, `Task`.

### 2. `Task`: a promise of work that finishes later

- `Task` represents an operation that is in progress or complete. It has no result value. Think "a `void` method that finishes later".
- `Task<T>` is the same, but when it completes it produces a value of type `T`. Think "an `int` method that finishes later".

You usually do not create tasks yourself. Library methods return them, and the naming convention is that such methods end in `Async`: `File.ReadAllTextAsync`, `Task.Delay`, and in Playwright `page.ClickAsync(...)`, `page.GotoAsync(...)`, `locator.TextContentAsync()`.

### 3. `async` and `await`

```csharp
// Complete program
Console.WriteLine("1. before");
await Task.Delay(500);              // wait half a second WITHOUT blocking a thread
Console.WriteLine("2. after");
```

```text
1. before
2. after
```

Rules:

1. A method containing `await` must be marked `async`.
2. An `async` method returns `Task` (no value), `Task<T>` (a value), or `ValueTask`/`ValueTask<T>` (Day 39). Never `void` (Day 39 explains why).
3. `await someTask` means: if the task is already done, continue immediately. If not, **return to the caller now** and resume the rest of the method when the task completes. The result of `await` is the value of `Task<T>` (or nothing for `Task`).
4. Top-level statements in `Program.cs` may use `await` directly; the compiler makes the entry point async for you.

`await` does **not** create a new thread. It registers "run the rest of this method when that operation is done". While waiting, no thread is tied up.

### 4. A fake page: sequential awaits

This mimics the shape of Playwright code. Each `await` finishes before the next line runs, so the steps happen in order, exactly like synchronous code.

```csharp
// Complete program
var page = new FakePage();

await page.GotoAsync("https://example.test/login");
await page.FillAsync("#user", "ada");
await page.ClickAsync("#submit");
string title = await page.TitleAsync();     // Task<string>: await gives us the string
Console.WriteLine($"Title is: {title}");

class FakePage
{
    public async Task GotoAsync(string url)
    {
        await Task.Delay(100);              // pretend the network is slow
        Console.WriteLine($"navigated to {url}");
    }

    public async Task FillAsync(string selector, string text)
    {
        await Task.Delay(50);
        Console.WriteLine($"filled {selector} with '{text}'");
    }

    public async Task ClickAsync(string selector)
    {
        await Task.Delay(50);
        Console.WriteLine($"clicked {selector}");
    }

    public async Task<string> TitleAsync()
    {
        await Task.Delay(10);
        return "Dashboard";                 // in a Task<string> method, return a plain string
    }
}
```

```text
navigated to https://example.test/login
filled #user with 'ada'
clicked #submit
Title is: Dashboard
```

Notice that inside `async Task<string>` you `return "Dashboard";`, a plain `string`. The compiler wraps it in a `Task<string>`.

### 5. What happens if you forget `await`

Without `await`, you get the *task object* instead of the work being finished. The method continues immediately, and the operation runs "in the background".

```csharp
// Complete program
Console.WriteLine("--- with await ---");
await SaveAsync("A");
Console.WriteLine("after A");

Console.WriteLine("--- without await ---");
Task t = SaveAsync("B");        // starts, but we do not wait
Console.WriteLine("after B (saving not finished yet)");
await t;                        // later we can still await the same task
Console.WriteLine("B really done");

static async Task SaveAsync(string name)
{
    await Task.Delay(100);
    Console.WriteLine($"saved {name}");
}
```

```text
--- with await ---
saved A
after A
--- without await ---
after B (saving not finished yet)
saved B
B really done
```

In a test, `page.ClickAsync("#buy");` with no `await` means the next line runs before the click happens. The compiler warns (CS4014) in `async` methods; treat that warning as an error in your head. Always await, or store the task deliberately.

### 6. Starting several operations and awaiting them together: `Task.WhenAll`

If operations are independent, start them all, then wait for all:

```csharp
// Complete program
using System.Diagnostics;

var sw = Stopwatch.StartNew();
string a = await FetchAsync("A", 300);
string b = await FetchAsync("B", 300);
string c = await FetchAsync("C", 300);
Console.WriteLine($"sequential: {a}{b}{c}, took >= 900 ms: {sw.ElapsedMilliseconds >= 900}");

sw.Restart();
Task<string> ta = FetchAsync("A", 300);     // all three start now
Task<string> tb = FetchAsync("B", 300);
Task<string> tc = FetchAsync("C", 300);
string[] results = await Task.WhenAll(ta, tb, tc);   // wait until all complete
Console.WriteLine($"parallel: {string.Join("", results)}, took < 800 ms: {sw.ElapsedMilliseconds < 800}");

static async Task<string> FetchAsync(string name, int ms)
{
    await Task.Delay(ms);
    return name;
}
```

```text
sequential: ABC, took >= 900 ms: True
parallel: ABC, took < 800 ms: True
```

- `await Task.WhenAll(...)` returns when **all** tasks are done. With `Task<T>` tasks it gives `T[]` in the same order as the arguments.
- `await Task.WhenAny(...)` returns the first task that completes (useful for timeouts).
- Do not run several operations in parallel on one Playwright `IPage`, though: page actions are meant to be awaited one after another. `WhenAll` suits independent things like calling several API endpoints or launching separate browser contexts.

### 7. Exceptions in async code

An exception thrown inside an async method is stored in the returned task and **re-thrown at the `await`**. So an ordinary `try/catch` around the `await` works.

```csharp
// Complete program
try
{
    await ClickMissingAsync("#nope");
    Console.WriteLine("not reached");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Caught: {ex.Message}");
}

// With WhenAll, if several tasks fail, await throws only one of the exceptions.
var all = Task.WhenAll(FailAsync("one"), FailAsync("two"));
try
{
    await all;
}
catch (Exception ex)
{
    Console.WriteLine($"await threw: {ex.GetType().Name}");
    Console.WriteLine($"tasks that failed: {all.Exception!.InnerExceptions.Count}");
}

static async Task ClickMissingAsync(string selector)
{
    await Task.Delay(10);
    throw new InvalidOperationException($"element {selector} not found");
}

static async Task FailAsync(string name)
{
    await Task.Delay(10);
    throw new InvalidOperationException($"{name} failed");
}
```

```text
Caught: element #nope not found
await threw: InvalidOperationException
tasks that failed: 2
```

### 8. Async all the way up

If method A awaits method B, then A must be async and return a task, and whoever calls A must await it, and so on up to the entry point (`Main`, or in xUnit, the test method). This is called "async all the way". Mixing sync and async by blocking (Day 39) causes problems.

```csharp
// Complete program
await RunTestAsync();                      // entry point awaits

static async Task RunTestAsync()           // returns Task, not void
{
    await LoginAsync();
    var count = await CountItemsAsync();
    Console.WriteLine($"items: {count}");
}

static async Task LoginAsync()
{
    await Task.Delay(20);
    Console.WriteLine("logged in");
}

static async Task<int> CountItemsAsync()
{
    await Task.Delay(20);
    return 3;
}
```

```text
logged in
items: 3
```

### 9. How this maps to Playwright and xUnit

```cs
[Fact]
public async Task Login_works()                       // Task return type: xUnit awaits it
{
    await Page.GotoAsync("https://example.test");     // starts navigation, resumes when done
    await Page.GetByLabel("User").FillAsync("ada");
    await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
    var heading = await Page.GetByRole(AriaRole.Heading).TextContentAsync();   // Task<string?>
    Assert.Equal("Dashboard", heading);
}
```

Each `...Async` method sends a command to the browser and returns a `Task`. `await` lets your thread do other things while the browser works. Because the test method itself is `async Task`, xUnit awaits the whole test.

### Common mistakes

- Forgetting `await`: the test continues before the browser action finishes, producing flaky or "passing" tests that verify nothing.
- Writing `async void` (explained tomorrow). Return `Task`.
- Thinking `await` makes code run in parallel. It does not: `await A(); await B();` runs B after A. Parallelism requires starting tasks first, then `WhenAll`.
- Using `Thread.Sleep` in async code. It blocks the thread; use `await Task.Delay`.

### Why this matters for test automation

Every Playwright call and every xUnit v3 lifecycle method is asynchronous. Understanding that a call returns a `Task` that must be awaited, that exceptions surface at the `await`, and that sequential awaits mean ordered steps is the foundation for reliable tests.

### Interview questions

**Q: What does `await` do?**
It suspends the async method until the awaited task completes, without blocking the thread; the rest of the method then continues. If the task failed, `await` re-throws its exception; if it is `Task<T>`, `await` yields the `T`.

**Q: Does `async` create a new thread?**
No. `async` only allows `await` and makes the compiler build a state machine. Waiting for I/O operations does not need a dedicated thread.

**Q: `Task` vs `Task<T>`?**
`Task` is an asynchronous operation with no result; `Task<T>` produces a `T` when it completes.

**Q: How do you run several async operations concurrently?**
Start them without awaiting each one, collect the tasks, then `await Task.WhenAll(tasks)`.

### Exercise

1. Write `static async Task<int> SlowSquareAsync(int n)` that waits 100 ms then returns `n * n`. Call it for 1, 2, 3 sequentially and print the three results.
2. Run the same three calls with `Task.WhenAll` and print the results using `string.Join`.
3. Write an async method that throws after a delay and catch its exception at the call site. Print the message.
4. Extend `FakePage` from section 4 with `IsVisibleAsync(string selector)` returning `Task<bool>` (always `true`) and use it in an `if`.

### Check

You can answer: what is the type of `page.ClickAsync("x")` without `await`, and what is the type of the expression `await page.TitleAsync()`? Why does `after B` print before `saved B` in section 5?

---

## Day 39: `ValueTask` and async pitfalls (`async void`, `.Result`)

### 1. `ValueTask`: a task that is often already finished

Every `Task<T>` is an object allocated on the heap. Some async methods usually finish **synchronously**: for example, a method that returns a cached value, and only occasionally does real I/O. `ValueTask<T>` is a struct that can hold either a ready result (no allocation) or a real `Task<T>`.

```csharp
// Complete program
var repo = new UserRepository();

Console.WriteLine(await repo.GetNameAsync(1));   // slow path: simulates a lookup
Console.WriteLine(await repo.GetNameAsync(1));   // fast path: from cache, no Task allocated
Console.WriteLine(repo.LookupCount);             // 1

class UserRepository
{
    private readonly Dictionary<int, string> _cache = new();
    public int LookupCount { get; private set; }

    public ValueTask<string> GetNameAsync(int id)
    {
        if (_cache.TryGetValue(id, out var cached))
            return new ValueTask<string>(cached);          // already have it: no allocation

        return new ValueTask<string>(LoadAsync(id));       // wrap the real Task
    }

    private async Task<string> LoadAsync(int id)
    {
        await Task.Delay(50);
        LookupCount++;
        var name = $"user-{id}";
        _cache[id] = name;
        return name;
    }
}
```

```text
user-1
user-1
1
```

Rules for `ValueTask`:

- Await it **once**. Do not await the same `ValueTask` twice, do not read its result before it completes, and do not run `WhenAll` on it directly (call `.AsTask()` first if you must).
- Prefer plain `Task` unless you measured a reason. It is a performance optimization.
- You mostly *consume* them: you `await` them the same way as tasks.

Why do you meet `ValueTask` in this course? **xUnit v3 uses it for its lifecycle methods**: `IAsyncLifetime` has `ValueTask InitializeAsync()` and its `DisposeAsync()` returns `ValueTask` too (it extends `IAsyncDisposable`). Test methods themselves may return `void`, `Task`, or `ValueTask` (return `async Task` for tests as the convention).

### 2. `IAsyncLifetime` in xUnit v3

xUnit creates a new instance of the test class for each test. `IAsyncLifetime` lets that instance run async setup and cleanup around the test. This is where Playwright browsers and pages are created and closed.

```cs
// xUnit v3 test project (dotnet new xunit3). Compiled and run with `dotnet test`.
public class LifecycleTests : IAsyncLifetime
{
    private readonly List<string> _log = new();

    public LifecycleTests()
    {
        _log.Add("constructor");              // synchronous setup
    }

    public async ValueTask InitializeAsync()
    {
        await Task.Delay(10);                 // e.g. launch a browser
        _log.Add("InitializeAsync");
    }

    [Fact]
    public async Task Order_of_setup()
    {
        await Task.Delay(10);
        Assert.Equal(new[] { "constructor", "InitializeAsync" }, _log);
    }

    public async ValueTask DisposeAsync()     // e.g. close the browser
    {
        await Task.Delay(10);                 // runs after the test
    }
}
```

Order for every test: constructor, `InitializeAsync`, the test method, `DisposeAsync` (and `Dispose` if you also implement `IDisposable`). In `InitializeAsync` you can `await`; in a constructor you cannot, which is exactly why this interface exists. Playwright's xUnit base classes (`PageTest` etc.) follow this pattern, and so does the `BaseTest` in your project.

If you write `public async ValueTask InitializeAsync()` you `await` inside it exactly as in a `Task` method. If there is nothing to await, return `ValueTask.CompletedTask`:

```cs
public ValueTask InitializeAsync() => ValueTask.CompletedTask;
```

### 3. Pitfall 1: `async void`

`async void` methods compile, but they are dangerous:

1. **The caller cannot await them.** There is no task to wait for, so the caller continues immediately.
2. **Exceptions cannot be caught by the caller.** An exception thrown after the first `await` in an `async void` method is raised on the synchronization context or thread pool; with no handler, it can crash the whole process (for a test run, the test host).
3. **Test frameworks cannot track completion.** xUnit does not support `async void` tests; use `async Task` (or `async ValueTask`).

```csharp
// Complete program
Console.WriteLine("start");
DoWorkVoid();                         // cannot await: returns immediately
Console.WriteLine("caller continues, work not done yet");
await Task.Delay(300);                // we only wait here to let the demo finish
Console.WriteLine("end");

static async void DoWorkVoid()        // BAD: avoid async void
{
    await Task.Delay(100);
    Console.WriteLine("work finished");
}
```

```text
start
caller continues, work not done yet
work finished
end
```

Compare the fix:

```csharp
// Complete program
Console.WriteLine("start");
await DoWorkAsync();                  // we can await the Task
Console.WriteLine("caller continues after the work");

static async Task DoWorkAsync()       // GOOD: returns Task
{
    await Task.Delay(100);
    Console.WriteLine("work finished");
}
```

```text
start
work finished
caller continues after the work
```

The only legitimate use of `async void` is an event handler whose delegate type is `void` (for example, a UI button click). In test code, avoid it entirely. If you pass an `async` lambda to a parameter of type `Action`, the compiler silently creates an `async void` lambda. Pass it to a `Func<Task>` parameter instead.

### 4. Pitfall 2: `.Result` and `.Wait()`

`task.Result` and `task.Wait()` block the current thread until the task finishes. Problems:

- **Deadlocks.** In environments with a synchronization context (classic ASP.NET, UI apps such as WinForms/WPF), the blocked thread is the one the continuation needs to resume on, so each waits for the other forever. Console apps and xUnit generally do not have such a context, so the code often "works" there, which hides the habit.
- **Thread starvation.** Blocking threads wastes threads that async code was designed to free.
- **Wrapped exceptions.** `.Result` and `.Wait()` throw an `AggregateException` wrapping the real one, which makes errors harder to read.

```csharp
// Complete program
try
{
    var value = GetValueAsync().Result;          // blocks, and wraps the failure
    Console.WriteLine(value);
}
catch (AggregateException ex)
{
    Console.WriteLine($"Result threw: {ex.GetType().Name}");
    Console.WriteLine($"Inner: {ex.InnerException!.GetType().Name}: {ex.InnerException.Message}");
}

try
{
    var value = await GetValueAsync();           // preferred: original exception
    Console.WriteLine(value);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"await threw: {ex.GetType().Name}: {ex.Message}");
}

static async Task<int> GetValueAsync()
{
    await Task.Delay(10);
    throw new InvalidOperationException("no value");
}
```

```text
Result threw: AggregateException
Inner: InvalidOperationException: no value
await threw: InvalidOperationException: no value
```

The fix is almost always: make the caller `async` and `await`. If you truly cannot (for example, you are inside a constructor or a property getter), redesign: move the async work into `InitializeAsync` or an async factory method. `task.GetAwaiter().GetResult()` unwraps the exception but still blocks, so it is a last resort, not a fix.

In Playwright tests, `page.ClickAsync("#x").Wait();` is the same mistake: always `await`.

### 5. Cancellation basics

Long operations should be stoppable. .NET uses a cooperative model:

- A `CancellationTokenSource` (the controller) creates a `CancellationToken` (the signal).
- You pass the token to async methods that accept it.
- When `Cancel()` is called (or a timeout elapses), those methods throw `OperationCanceledException` (`Task.Delay` throws the derived `TaskCanceledException`).

```csharp
// Complete program
using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));   // auto-cancel after 200 ms

try
{
    await LongOperationAsync(cts.Token);
    Console.WriteLine("completed");
}
catch (OperationCanceledException ex)
{
    Console.WriteLine($"cancelled (is cancellation exception: {ex is OperationCanceledException})");
}

// Manual cancellation
using var cts2 = new CancellationTokenSource();
var task = LongOperationAsync(cts2.Token);
cts2.Cancel();                                   // request cancellation now
try { await task; }
catch (OperationCanceledException) { Console.WriteLine("cancelled by Cancel()"); }

// Own loop: check the token between steps
using var cts3 = new CancellationTokenSource();
for (var i = 1; i <= 5; i++)
{
    if (i == 3) cts3.Cancel();
    try
    {
        cts3.Token.ThrowIfCancellationRequested();   // throws if cancel was requested
        Console.WriteLine($"step {i}");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"stopped before step {i}");
        break;
    }
}

static async Task LongOperationAsync(CancellationToken token)
{
    await Task.Delay(5000, token);                   // would take 5 s unless cancelled
}
```

```text
cancelled (is cancellation exception: True)
cancelled by Cancel()
step 1
step 2
stopped before step 3
```

Where you will see tokens: many Playwright and `HttpClient` async methods accept timeouts or tokens; xUnit v3 exposes a `TestContext.Current.CancellationToken` that you can pass to async calls so a test run can be cancelled cleanly (it is the recommended token to forward; check the xUnit v3 analyzer hints in your IDE).

### 6. Other pitfalls in a nutshell

| Pitfall | Why it hurts | Fix |
|---|---|---|
| Not awaiting a task | Races; swallowed exceptions | Always `await` |
| `async void` | Uncatchable exceptions; no completion signal | Return `Task` |
| `.Result` / `.Wait()` | Deadlocks, thread starvation, `AggregateException` | `await` |
| `Thread.Sleep` in async code | Blocks a thread | `await Task.Delay` |
| `async` lambda passed as `Action` | Becomes `async void` | Parameter type `Func<Task>` |
| Awaiting a `ValueTask` twice | Undefined behaviour | Await once or call `.AsTask()` |
| Fixed sleeps in UI tests | Slow and still flaky | Playwright auto-waiting and web-first assertions |

`ConfigureAwait(false)` tells `await` not to resume on the captured context. It matters mainly in library code that runs under a synchronization context; you generally do not need it in test code, so do not sprinkle it around.

### Common mistakes

- Using `.Result` "just this once" inside a helper: it will deadlock in some host and cost you hours.
- Writing `public async void Setup()` in a test class.
- Catching `Exception` around code you did not await: the exception is not there yet.
- Awaiting inside `lock`: C# does not allow `await` inside a `lock` statement; use `SemaphoreSlim` when you need async-compatible locking.

### Why this matters for test automation

Flaky tests are very often un-awaited tasks. xUnit v3's lifecycle uses `ValueTask`, and mixing blocking calls into async fixtures causes hangs in CI. Cancellation and timeouts keep a stuck test from freezing the pipeline.

### Interview questions

**Q: Why avoid `async void`?**
The caller cannot await it, cannot catch its exceptions (they can crash the process), and frameworks cannot know when it ends. Return `Task`. The exception is event handlers.

**Q: Why avoid `.Result` and `.Wait()`?**
They block a thread, can deadlock when a synchronization context exists, and wrap exceptions in `AggregateException`. Use `await`.

**Q: When use `ValueTask` over `Task`?**
When an async method frequently completes synchronously and allocations matter. It must be awaited once, and it is not a default choice.

**Q: How does cancellation work in .NET?**
Cooperatively: a `CancellationTokenSource` produces a token that is passed down; operations observe it and throw `OperationCanceledException`.

**Q: What is `IAsyncLifetime` for?**
It lets an xUnit test class (or fixture) do async setup (`InitializeAsync`) and cleanup (`DisposeAsync`), which a constructor cannot do.

### Exercise

1. Take the `UserRepository` example and write a second method `ValueTask<bool> ExistsAsync(int id)` that returns a completed `ValueTask<bool>` for ids 1 to 3 and otherwise delays. Await it from a loop.
2. Create a small xUnit v3 test class implementing `IAsyncLifetime` that fills a list in `InitializeAsync` and asserts it in a `[Fact]`.
3. Write a method `WithTimeoutAsync(Func<CancellationToken, Task> work, int ms)` that creates a `CancellationTokenSource(ms)` and returns `true` if the work completes and `false` if it was cancelled.

### Check

Explain in two sentences why `GetValueAsync().Result` can deadlock in a UI app but "works" in a console app. Explain which method signature xUnit v3's `InitializeAsync` has and why.

---

## Day 40: File I/O

### 1. Why tests touch files

Tests read test data (users, products, expected values), write results (logs, screenshots, reports, downloaded files), and check that downloads were saved correctly. All of that goes through the `System.IO` namespace, which is included by implicit usings.

The easy API is the static class `File`. It has one-line methods for the common jobs:

| Method | What it does |
|---|---|
| `File.WriteAllText(path, text)` | Creates or **overwrites** a file with the text |
| `File.AppendAllText(path, text)` | Adds text to the end (creates the file if missing) |
| `File.ReadAllText(path)` | Reads the whole file into one `string` |
| `File.WriteAllLines(path, lines)` | Writes a collection of strings, one per line |
| `File.ReadAllLines(path)` | Reads all lines into a `string[]` |
| `File.ReadLines(path)` | Reads lines **lazily** as an `IEnumerable<string>` (good for big files) |
| `File.Exists(path)` / `Directory.Exists(path)` | Checks existence |
| `File.Delete(path)` / `File.Copy(src, dest)` / `File.Move(src, dest)` | Manage files |
| `Directory.CreateDirectory(path)` | Creates a folder (and parents); does nothing if it exists |

Each of the read/write methods also has an async twin (`File.ReadAllTextAsync`, `File.WriteAllTextAsync`, ...) that returns a `Task`.

### 2. First worked example: write, append, read

```csharp
// Complete program
const string dir = "scratch-io";
Directory.CreateDirectory(dir);                     // safe if it already exists
string path = Path.Combine(dir, "notes.txt");       // never glue paths with "+" or "/"

File.WriteAllText(path, "first line\n");            // creates / overwrites
File.AppendAllText(path, "second line\n");          // adds to the end
File.AppendAllText(path, "third line\n");

string all = File.ReadAllText(path);
Console.WriteLine(all.Length);                      // 34 here: line ends are written as \n

string[] lines = File.ReadAllLines(path);
Console.WriteLine(lines.Length);                    // 3
Console.WriteLine(lines[1]);                        // second line

File.WriteAllLines(path, new[] { "a", "b", "c" });  // overwrites with 3 new lines
foreach (var line in File.ReadLines(path))          // lazy: one line at a time
    Console.WriteLine($"> {line}");

Console.WriteLine(File.Exists(path));               // True
File.Delete(path);
Console.WriteLine(File.Exists(path));               // False
Directory.Delete(dir);                              // folder is empty now
```

```text
34
3
second line
> a
> b
> c
True
False
```

Beware: `WriteAllText` silently **replaces** an existing file. `AppendAllText` is the one that keeps content. (The length 34 counts one `\n` per line; on Windows, `WriteAllLines` and `Console.WriteLine` use `\r\n`, so lengths of files written that way differ.)

### 3. Paths: use `Path`, not string concatenation

Windows uses `\` and Linux/macOS use `/` as separators. `Path.Combine` picks the right one, and `Path` has helpers to take paths apart:

```csharp
// Complete program
string p = Path.Combine("TestData", "users", "admin.json");
Console.WriteLine(p);                                   // TestData/users/admin.json   (TestData\users\admin.json on Windows)
Console.WriteLine(Path.GetFileName(p));                 // admin.json
Console.WriteLine(Path.GetFileNameWithoutExtension(p)); // admin
Console.WriteLine(Path.GetExtension(p));                // .json
Console.WriteLine(Path.GetDirectoryName(p));            // TestData/users
Console.WriteLine(Path.ChangeExtension(p, ".bak"));     // TestData/users/admin.bak
Console.WriteLine(Path.IsPathRooted(p));                // False (relative path)
```

```text
TestData/users/admin.json
admin.json
admin
.json
TestData/users
TestData/users/admin.bak
False
```

A relative path such as `"TestData/users.json"` is resolved against the **current working directory**, which is not always the folder you expect (see section 6). `Path.GetFullPath(p)` shows the absolute path a relative one resolves to; print it when debugging "file not found".

### 4. Directories: listing and creating

```csharp
// Complete program
const string root = "scratch-dir";
Directory.CreateDirectory(Path.Combine(root, "reports"));
File.WriteAllText(Path.Combine(root, "a.json"), "{}");
File.WriteAllText(Path.Combine(root, "b.json"), "{}");
File.WriteAllText(Path.Combine(root, "readme.txt"), "hi");
File.WriteAllText(Path.Combine(root, "reports", "c.json"), "{}");

// Top directory only, filtered by pattern. Sort: the order is not guaranteed.
var topJson = Directory.EnumerateFiles(root, "*.json").OrderBy(f => f);
foreach (var f in topJson) Console.WriteLine(Path.GetFileName(f));

// Include sub-directories
var allJson = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories)
                       .Select(f => Path.GetRelativePath(root, f))
                       .OrderBy(f => f);
Console.WriteLine(string.Join(" | ", allJson));

Console.WriteLine(string.Join(",", Directory.GetDirectories(root).Select(Path.GetFileName)));

Directory.Delete(root, recursive: true);   // delete the folder and everything inside
```

```text
a.json
b.json
a.json | b.json | reports/c.json
reports
```

(On Windows the relative path prints with a backslash.) `Directory.Delete(path, true)` is permanent: there is no recycle bin. Double-check the path before using it.

### 5. Streams: `StreamReader`/`StreamWriter` and `using`

`File.ReadAllText` loads everything at once. For large files, or when you write line by line, use a `StreamWriter`/`StreamReader`. They hold an operating-system file handle, so they implement `IDisposable` (Week 6): wrap them in `using` so the file is closed even if an exception happens.

```csharp
// Complete program
const string path = "scratch-stream.log";

using (var writer = new StreamWriter(path))                 // overwrites
{
    writer.WriteLine("run started");
    writer.WriteLine($"tests: {3}");
    writer.Write("no newline here");
}                                                           // closed and flushed here

using (var appender = new StreamWriter(path, append: true)) // keep existing content
{
    appender.WriteLine();
    appender.WriteLine("run finished");
}

using (var reader = new StreamReader(path))
{
    string? line;
    int number = 1;
    while ((line = reader.ReadLine()) != null)              // ReadLine returns null at end of file
    {
        Console.WriteLine($"{number++}: {line}");
    }
}

File.Delete(path);
```

```text
1: run started
2: tests: 3
3: no newline here
4: run finished
```

Note `string? line`: `ReadLine()` returns `null` at the end, which is the loop's stop signal.

### 6. Where is my file? Working directory vs. output folder

This is the number one beginner problem in test projects.

- `dotnet run` and `dotnet test` copy your compiled program into `bin/Debug/net10.0/`. The **working directory** may be the project folder or the output folder depending on how the program is started (command line, IDE, test runner), so do not rely on it.
- A data file such as `TestData/users.json` that sits in your project folder is **not** automatically in the output folder.
- You must tell the build to copy it, with this in the `.csproj`:

```xml
<ItemGroup>
  <None Update="TestData\**\*.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

- Then build the path from `AppContext.BaseDirectory` (the folder that holds your compiled assembly), which does not depend on the working directory:

```csharp
// Complete program
string baseDir = AppContext.BaseDirectory;
Console.WriteLine(new DirectoryInfo(baseDir).Name);     // net10.0   (when started with dotnet run)

string dataPath = Path.Combine(baseDir, "TestData", "users.json");
Console.WriteLine(Path.GetFileName(dataPath));          // users.json
Console.WriteLine(File.Exists(dataPath));               // False (this demo has no such file or csproj entry)
```

```text
net10.0
users.json
False
```

Tomorrow's lesson uses exactly this pattern with a real file.

### 7. Errors you will meet

```csharp
// Complete program
try
{
    File.ReadAllText("does-not-exist.txt");
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"FileNotFound: {Path.GetFileName(ex.FileName)}");
}

try
{
    File.WriteAllText(Path.Combine("no-such-folder", "x.txt"), "data");
}
catch (DirectoryNotFoundException)
{
    Console.WriteLine("DirectoryNotFound: create the folder first with Directory.CreateDirectory");
}

// A file opened with FileShare.None cannot be opened again: IOException
File.WriteAllText("locked.txt", "x");
using (var stream = new FileStream("locked.txt", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
{
    try
    {
        File.ReadAllText("locked.txt");
    }
    catch (IOException)
    {
        Console.WriteLine("IOException: file is in use");
    }
}
File.Delete("locked.txt");
```

```text
FileNotFound: does-not-exist.txt
DirectoryNotFound: create the folder first with Directory.CreateDirectory
IOException: file is in use
```

Prefer `try/catch` around the I/O call over `if (File.Exists(...))` followed by a read: the file can disappear between the check and the read. On Windows, `IOException` is also what you get when another program (for example Excel) has the file open.

### 8. A realistic job: parse a CSV of test data

```csharp
// Complete program
const string path = "scratch-users.csv";
File.WriteAllLines(path, new[]
{
    "username,password,shouldSucceed",
    "standard_user,secret_sauce,true",
    "locked_out_user,secret_sauce,false",
    "bad line without commas",
    "problem_user,secret_sauce,true"
});

var cases = new List<LoginCase>();
foreach (var line in File.ReadLines(path).Skip(1))       // skip the header
{
    var parts = line.Split(',');
    if (parts.Length != 3 || !bool.TryParse(parts[2], out var expected))
    {
        Console.WriteLine($"skipping bad line: {line}");
        continue;
    }
    cases.Add(new LoginCase(parts[0], parts[1], expected));
}

foreach (var c in cases)
    Console.WriteLine($"{c.Username,-16} success={c.ShouldSucceed}");

// Write a results file
File.WriteAllLines("scratch-results.txt", cases.Select(c => $"{c.Username}: {(c.ShouldSucceed ? "PASS" : "FAIL")}"));
Console.WriteLine(File.ReadAllText("scratch-results.txt").TrimEnd());

File.Delete(path);
File.Delete("scratch-results.txt");

record LoginCase(string Username, string Password, bool ShouldSucceed);
```

```text
skipping bad line: bad line without commas
standard_user    success=True
locked_out_user  success=False
problem_user     success=True
standard_user: PASS
locked_out_user: FAIL
problem_user: PASS
```

(A naive `Split(',')` breaks on quoted fields that contain commas. For your own simple test data, JSON, covered tomorrow, is usually the better format.)

### 9. Async file I/O

```csharp
// Complete program
const string path = "scratch-async.txt";
await File.WriteAllTextAsync(path, "alpha\nbeta\n");
string text = await File.ReadAllTextAsync(path);
Console.WriteLine(text.Replace("\n", "|"));            // alpha|beta|

string[] lines = await File.ReadAllLinesAsync(path);
Console.WriteLine(lines.Length);                        // 2

File.Delete(path);
```

```text
alpha|beta|
2
```

In test code, prefer the async versions inside `async Task` tests and fixtures: it is consistent and avoids blocking threads. For tiny files it makes little difference in speed.

### Common mistakes

- Hard-coding absolute paths like `C:\Users\me\data.json`. They break on every other machine and in CI. Use relative paths plus `AppContext.BaseDirectory`.
- Forgetting `CopyToOutputDirectory`, then getting `FileNotFoundException` for a file you can clearly see in the project.
- Using `WriteAllText` when you wanted to append, and wiping a log file.
- Not disposing a `StreamReader`/`StreamWriter`: data may not be flushed, and the file stays locked.

### Why this matters for test automation

Test data files, saved screenshots, traces and videos, downloaded files (`page.WaitForDownloadAsync` then `SaveAsAsync`), and report output are all files. In CI, working directories differ from your laptop, so path handling decides whether the test is portable.

### Interview questions

**Q: Why use `Path.Combine` instead of string concatenation?**
It uses the correct directory separator for the operating system and handles missing or doubled separators.

**Q: `File.ReadAllLines` vs `File.ReadLines`?**
`ReadAllLines` reads everything into an array up front; `ReadLines` returns a lazy sequence that reads while you enumerate, so it uses less memory for large files.

**Q: Why wrap a `StreamWriter` in `using`?**
It owns an unmanaged file handle and a buffer; disposing flushes the buffer and releases the handle even when an exception occurs.

**Q: A test cannot find `TestData/users.json` in CI. Why?**
The file was not copied to the output directory (`CopyToOutputDirectory` missing) or the code used a path relative to a different working directory. Use `AppContext.BaseDirectory` and copy the file in the `.csproj`.

### Exercise

1. Write a program that creates a folder `scratch-logs`, writes three log files `run1.log` to `run3.log` each with a different number of lines, then prints each file name with its line count (use `ReadLines(...).Count()`), then deletes the folder.
2. Parse the CSV in section 8 again, but print only the users where `ShouldSucceed` is `false`.
3. Write a method `static async Task AppendLogAsync(string path, string message)` that appends one line using `File.AppendAllTextAsync`, call it three times with `await`, then read the file back and print the line count.
4. Make `File.ReadAllText("missing.txt")` fail and print a friendly message by catching `FileNotFoundException`.

### Check

Without running it: what does `Path.Combine("a", "b.txt")` return on Linux? Which API reads a large file one line at a time without loading it all? What must you add to the `.csproj` so `TestData/users.json` is available next to the compiled program?

---

## Day 41: JSON with `System.Text.Json`

### 1. What JSON is, and why tests use it

JSON is a text format for structured data. API responses are JSON, and it is a convenient way to store test data. It has objects (`{ }` with `"name": value` pairs), arrays (`[ ]`), strings, numbers, `true`/`false`, and `null`.

```json
{ "username": "ada", "age": 36, "roles": ["admin", "tester"], "manager": null }
```

`System.Text.Json` is the JSON library built into .NET (namespace `System.Text.Json`, no NuGet package needed). Two words you need:

- **Serialize**: turn a C# object into JSON text.
- **Deserialize**: turn JSON text into a C# object.

### 2. Serialize

```csharp
// Complete program
using System.Text.Json;

var user = new User { Username = "ada", Age = 36, Roles = new List<string> { "admin", "tester" } };

string compact = JsonSerializer.Serialize(user);
Console.WriteLine(compact);

string pretty = JsonSerializer.Serialize(user, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(pretty);

class User
{
    public string Username { get; set; } = "";
    public int Age { get; set; }
    public List<string> Roles { get; set; } = new();
    public string? Manager { get; set; }          // null stays in the output as null
}
```

```text
{"Username":"ada","Age":36,"Roles":["admin","tester"],"Manager":null}
{
  "Username": "ada",
  "Age": 36,
  "Roles": [
    "admin",
    "tester"
  ],
  "Manager": null
}
```

Defaults worth knowing: output is compact, property names are written exactly as in C# (PascalCase), and only **public properties** are included (public fields are ignored unless you set `IncludeFields = true`).

### 3. Deserialize

`JsonSerializer.Deserialize<T>(json)` builds an object of type `T`. The type needs a public parameterless constructor (or a constructor the library can match, as records provide) and settable properties.

```csharp
// Complete program
using System.Text.Json;

string json = """
{ "Username": "grace", "Age": 45, "Roles": ["admin"], "Manager": null }
""";

User? user = JsonSerializer.Deserialize<User>(json);     // may return null if the JSON is literally: null
Console.WriteLine(user!.Username);                        // grace
Console.WriteLine(user.Age);                              // 45
Console.WriteLine(user.Roles[0]);                         // admin
Console.WriteLine(user.Manager is null);                  // True

// JSON array into a List
var many = JsonSerializer.Deserialize<List<User>>("""[{"Username":"a"},{"Username":"b"}]""")!;
Console.WriteLine(many.Count);                            // 2

// Names are CASE-SENSITIVE by default: "username" does not match Username
var lower = JsonSerializer.Deserialize<User>("""{"username":"x"}""")!;
Console.WriteLine(lower.Username == "");                  // True (the property kept its default value)

class User
{
    public string Username { get; set; } = "";
    public int Age { get; set; }
    public List<string> Roles { get; set; } = new();
    public string? Manager { get; set; }
}
```

```text
grace
45
admin
True
2
True
```

The raw string literal `""" ... """` (C# 11) lets you write JSON without escaping quotes.

Notes:

- Unknown JSON properties are ignored by default. Missing JSON properties leave the C# property at its default value. That silent behavior is why **a typo in a JSON key does not throw**; the value just stays empty. Validate your test data.
- A `List<User>` property initialised to `new()` (as above) is safer than leaving it `null`.

### 4. Matching names: `JsonPropertyName` and options

JSON files usually use camelCase (`userName`) while C# properties use PascalCase (`UserName`). Two ways to bridge that gap.

**A. Per property: `[JsonPropertyName]`** (namespace `System.Text.Json.Serialization`). The attribute wins over any option and is precise and explicit:

```csharp
// Complete program
using System.Text.Json;
using System.Text.Json.Serialization;

var account = new Account { UserName = "ada", PasswordHash = "x1", Notes = "internal" };
Console.WriteLine(JsonSerializer.Serialize(account));

var back = JsonSerializer.Deserialize<Account>("""{"user_name":"zed","Notes":"ignored on read"}""")!;
Console.WriteLine($"{back.UserName} / {back.Notes ?? "(null)"}");

class Account
{
    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = "";

    [JsonPropertyName("password_hash")]
    public string PasswordHash { get; set; } = "";

    [JsonIgnore]                                 // never written or read
    public string? Notes { get; set; }
}
```

```text
{"user_name":"ada","password_hash":"x1"}
zed / (null)
```

**B. For the whole type: `JsonSerializerOptions`.** Create **one** options object and reuse it (it caches type information; creating a new one for every call is slow).

```csharp
// Complete program
using System.Text.Json;
using System.Text.Json.Serialization;

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,           // write userName, read userName
    PropertyNameCaseInsensitive = true,                          // read USERNAME, UserName, username
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // skip null properties when writing
    Converters = { new JsonStringEnumConverter() }               // enums as text instead of numbers
};

var dto = new TestCase("Login works", Priority.High, null);
string json = JsonSerializer.Serialize(dto, options);
Console.WriteLine(json);

var parsed = JsonSerializer.Deserialize<TestCase>("""{ "NAME": "Checkout", "priority": "Low", "ticket": "QA-7" }""", options)!;
Console.WriteLine($"{parsed.Name} | {parsed.Priority} | {parsed.Ticket}");

enum Priority { Low, Medium, High }

record TestCase(string Name, Priority Priority, string? Ticket);
```

```text
{
  "name": "Login works",
  "priority": "High"
}
Checkout | Low | QA-7
```

- Records work with `System.Text.Json`: it matches JSON names to the constructor parameters.
- `JsonSerializerDefaults.Web` is a preset (`new JsonSerializerOptions(JsonSerializerDefaults.Web)`) that gives camelCase names, case-insensitive reading, and allows numbers written as strings. It is the usual choice for APIs.
- Without `JsonStringEnumConverter`, enums are written as their numbers (`2`).

### 5. Collections, nested objects, dictionaries

```csharp
// Complete program
using System.Text.Json;

var suite = new Suite
{
    Name = "Smoke",
    Tags = new[] { "fast", "ui" },
    Settings = new Dictionary<string, string> { ["browser"] = "chromium", ["headless"] = "true" },
    Cases = new List<Case> { new() { Id = 1, Title = "Open home" }, new() { Id = 2, Title = "Search" } }
};

string json = JsonSerializer.Serialize(suite);
Console.WriteLine(json);

var copy = JsonSerializer.Deserialize<Suite>(json)!;
Console.WriteLine(copy.Cases.Count);                 // 2
Console.WriteLine(copy.Cases[1].Title);              // Search
Console.WriteLine(copy.Settings["browser"]);         // chromium
Console.WriteLine(string.Join("+", copy.Tags));      // fast+ui

class Suite
{
    public string Name { get; set; } = "";
    public string[] Tags { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> Settings { get; set; } = new();
    public List<Case> Cases { get; set; } = new();
}

class Case
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
}
```

```text
{"Name":"Smoke","Tags":["fast","ui"],"Settings":{"browser":"chromium","headless":"true"},"Cases":[{"Id":1,"Title":"Open home"},{"Id":2,"Title":"Search"}]}
2
Search
chromium
fast+ui
```

### 6. Bad input: `JsonException`, `required`

Malformed JSON throws `JsonException`. A property marked `required` (C# 11) must be present in the JSON, otherwise deserialization throws too, which turns silent typos into loud failures.

```csharp
// Complete program
using System.Text.Json;

try
{
    JsonSerializer.Deserialize<Item>("{ this is not json }");
}
catch (JsonException)
{
    Console.WriteLine("malformed JSON");
}

try
{
    JsonSerializer.Deserialize<Item>("""{"Name":"pen"}""");      // "Price" is missing
}
catch (JsonException ex)
{
    Console.WriteLine("missing required member");
    Console.WriteLine(ex.Message.Contains("Price"));              // True: the message names the member
}

try
{
    JsonSerializer.Deserialize<Item>("""{"Name":"pen","Price":"cheap"}""");   // wrong type
}
catch (JsonException)
{
    Console.WriteLine("wrong value type");
}

var ok = JsonSerializer.Deserialize<Item>("""{"Name":"pen","Price":1.5}""")!;
Console.WriteLine($"{ok.Name} costs {ok.Price}");

class Item
{
    public required string Name { get; set; }
    public required decimal Price { get; set; }
}
```

```text
malformed JSON
missing required member
True
wrong value type
pen costs 1.5
```

### 7. Reading without a class: `JsonNode`

Sometimes you only want one value from a response. `JsonNode` navigates JSON like a dictionary, without defining a class.

```csharp
// Complete program
using System.Text.Json.Nodes;

JsonNode root = JsonNode.Parse("""
{ "id": 7, "user": { "name": "ada", "emails": ["a@x.test", "b@x.test"] } }
""")!;

int id = root["id"]!.GetValue<int>();
string name = root["user"]!["name"]!.GetValue<string>();
string secondEmail = root["user"]!["emails"]![1]!.GetValue<string>();
Console.WriteLine($"{id} {name} {secondEmail}");           // 7 ada b@x.test

Console.WriteLine(root["missing"] is null);                // True (a missing key gives null)
```

```text
7 ada b@x.test
True
```

Use classes for stable, known shapes (test data, DTOs). Use `JsonNode` for quick, throwaway inspection.

### 8. Reading test data from a JSON file

Project layout:

```text
MyTests/
  MyTests.csproj
  TestData/
    login-cases.json
  LoginCase.cs
  TestDataLoader.cs
  Program.cs
```

`TestData/login-cases.json`:

```json
[
  { "username": "standard_user",   "password": "secret_sauce", "expectedSuccess": true  },
  { "username": "locked_out_user", "password": "secret_sauce", "expectedSuccess": false },
  { "username": "",                "password": "x",            "expectedSuccess": false }
]
```

`MyTests.csproj` (add the `ItemGroup` that copies the data):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <None Update="TestData\**\*.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

`LoginCase.cs`:

```cs
using System.Text.Json.Serialization;

public record LoginCase(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("expectedSuccess")] bool ExpectedSuccess);
```

`TestDataLoader.cs`:

```cs
using System.Text.Json;

public static class TestDataLoader
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static async Task<List<T>> LoadListAsync<T>(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Test data file not found: {path}", path);

        await using var stream = File.OpenRead(path);
        var items = await JsonSerializer.DeserializeAsync<List<T>>(stream, Options);
        return items ?? throw new InvalidDataException($"{fileName} contains JSON null");
    }
}
```

`Program.cs`:

```cs
var cases = await TestDataLoader.LoadListAsync<LoginCase>("login-cases.json");
foreach (var c in cases)
    Console.WriteLine($"'{c.Username}' expects success={c.ExpectedSuccess}");
```

Output of `dotnet run`:

```text
'standard_user' expects success=True
'locked_out_user' expects success=False
'' expects success=False
```

Points to notice: `await using` disposes the stream asynchronously; `DeserializeAsync` reads straight from the stream; the generic loader works for any `T`; and a clear error message with the full path saves debugging time.

### 9. Feeding JSON data into xUnit tests

xUnit's `[Theory]` runs one test method for many data rows. `[MemberData]` points to a static member that returns the rows. Keep rows to simple types (`string`, `int`, `bool`) so each row is individually reported.

```cs
using Xunit;

public class LoginTests
{
    public static TheoryData<string, string, bool> Cases()
    {
        var data = new TheoryData<string, string, bool>();
        var cases = TestDataLoader.LoadListAsync<LoginCase>("login-cases.json").GetAwaiter().GetResult();
        foreach (var c in cases)
            data.Add(c.Username, c.Password, c.ExpectedSuccess);
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Credentials_are_classified(string username, string password, bool expectedSuccess)
    {
        bool actual = username != "" && username != "locked_out_user";   // stand-in for a real login
        Assert.Equal(expectedSuccess, actual);
    }
}
```

Here `GetAwaiter().GetResult()` is used deliberately because a `MemberData` member must return data synchronously. It runs at discovery time, not inside a test body, so this is an accepted exception to "never block"; keep such code tiny and out of tests. (Alternatively load data with synchronous `File.ReadAllText` plus `JsonSerializer.Deserialize`.)

### Common mistakes

- Expecting case-insensitive matching by default. `"userName"` in JSON does not fill `UserName` unless you set `PropertyNameCaseInsensitive` or a naming policy, or use `[JsonPropertyName]`.
- Creating `new JsonSerializerOptions()` inside a loop or per call. Create it once and store it in a `static readonly` field.
- Forgetting that properties need setters (or a matching constructor) and public visibility; otherwise values stay at their defaults.
- Not checking for `null` from `Deserialize<T>` (nullable annotation `T?`), or forgetting `CopyToOutputDirectory` for the JSON file.

### Why this matters for test automation

API tests send and receive JSON all the time (`HttpClient`, and Playwright's `APIRequestContext` for API testing). Data-driven UI tests read their cases from JSON. A small, typed loader makes the data reviewable in pull requests, and strictness (`required`, clear errors) makes bad data fail fast.

### Interview questions

**Q: How do you map a JSON property `user_name` to a C# property `UserName`?**
Use `[JsonPropertyName("user_name")]` on the property, or a naming policy in `JsonSerializerOptions` when all names follow one convention.

**Q: Is `System.Text.Json` case-sensitive?**
Yes by default when reading. Set `PropertyNameCaseInsensitive = true` (or use `JsonSerializerDefaults.Web`).

**Q: Why reuse a `JsonSerializerOptions` instance?**
It caches serialization metadata, so creating a new instance per call is wasteful.

**Q: What happens with extra or missing JSON properties?**
Extra ones are ignored; missing ones leave default values, unless the member is `required`, in which case a `JsonException` is thrown.

**Q: How do you read a JSON value without declaring a class?**
`JsonNode.Parse` or `JsonDocument.Parse` and navigate by key and index.

### Exercise

1. Create a `Product` class (`Id`, `Name`, `Price`, `Tags`), serialize a list of three products to `products.json` with indented output, then read it back and print the names where `Price > 10`.
2. Make a JSON file whose keys use `snake_case`, and deserialize it into PascalCase properties using `[JsonPropertyName]`.
3. Add a `required` property and show the exception message when it is missing.
4. Copy the loader project from section 8, add a second file `browsers.json` (an array of strings such as `["chromium","firefox"]`), and load it with `LoadListAsync<string>`.

### Check

Without running it: what does `JsonSerializer.Serialize(new { A = 1 })` produce? Why might `Deserialize<User>("{\"name\":\"x\"}")` leave `Name` empty, and give two fixes?

---

## Day 42: Review and practice

### 1. Recap table

| Day | Topic | Key points |
|---|---|---|
| 37 | Delegates, lambdas | A delegate type holds methods. `Action` returns nothing; `Func<..., TResult>` returns a value (last type argument). Lambdas use `=>`. Lambdas capture variables (closures). Events are restricted delegates (`+=`, `-=`, `?.Invoke`). |
| 38 | `Task`, `async`, `await` | `Task` = work finishing later; `Task<T>` also yields a `T`. `await` suspends without blocking a thread and re-throws exceptions. `Task.WhenAll` for concurrency. Forgetting `await` is a bug. Async all the way up. |
| 39 | `ValueTask`, pitfalls | `ValueTask` avoids allocation when results are often synchronous; await once. xUnit v3 `IAsyncLifetime` uses `ValueTask InitializeAsync()`/`DisposeAsync()`. Avoid `async void`, `.Result`, `.Wait()`. Cancellation via `CancellationToken`. |
| 40 | File I/O | `File`, `Directory`, `Path`. `ReadAllText`, `WriteAllText` (overwrites), `AppendAllText`, `ReadLines` (lazy). `using` for streams. Use `AppContext.BaseDirectory` and `CopyToOutputDirectory`. |
| 41 | `System.Text.Json` | `Serialize`/`Deserialize<T>`. Case-sensitive by default; `[JsonPropertyName]`, `JsonSerializerOptions` (reuse one), `required`, `JsonException`, `JsonNode`. Load test data through a typed loader. |

### 2. Practice questions

Try each on paper or in a scratch project before opening the answers.

1. What does this print?
   ```cs
   var actions = new List<Action>();
   for (int i = 0; i < 3; i++)
   {
       int copy = i;
       actions.Add(() => Console.Write(copy));
   }
   foreach (var a in actions) a();
   ```
2. What is the difference between `Func<int, string>` and `Action<int, string>`?
3. `await A(); await B();` versus `await Task.WhenAll(A(), B());` where each takes one second. How long does each take, and when is the second form not appropriate?
4. A teammate wrote `public async void ClickLoginAsync() { await Page.ClickAsync("#login"); }`. List two problems and show the fix.
5. What exception type do you catch when you write `try { var x = GetAsync().Result; }` and `GetAsync` throws `InvalidOperationException`?
6. Why is `ValueTask InitializeAsync()` used in xUnit v3, and why can a constructor not do the same job?
7. What is the difference between `File.WriteAllText` and `File.AppendAllText`? Which one can destroy existing data?
8. Your test passes locally but in CI throws `FileNotFoundException` for `TestData/users.json`. Give two likely causes.
9. What does `JsonSerializer.Serialize(new Person("Ann", 30))` print for `record Person(string Name, int Age);` with default options?
10. A `CancellationTokenSource` is created with a 200 ms timeout and passed to `Task.Delay(5000, token)`. What happens, and what do you catch?

<details>
<summary>Answers</summary>

1. `012`. Each lambda captures its own `copy` variable. (If it captured `i` directly, a classic `for` loop would share one variable and print `333`.)
2. `Func<int, string>` takes an `int` and **returns** a `string`. `Action<int, string>` takes an `int` and a `string` and returns nothing.
3. Sequential: about 2 seconds. `WhenAll`: about 1 second, because both start before either is awaited. Not appropriate when step B depends on step A's result, or when both operate on the same Playwright `IPage` whose actions should happen in order.
4. The caller cannot await it (so the test finishes before the click is done), and exceptions cannot be caught by the caller and may crash the process. Fix: `public async Task ClickLoginAsync()` and `await ClickLoginAsync();` at the call site.
5. `AggregateException`, whose `InnerException` is the `InvalidOperationException`. With `await` you would catch `InvalidOperationException` directly.
6. Setup often needs `await` (launching a browser), which a constructor cannot do. `ValueTask` avoids allocating a `Task` when setup completes synchronously and is the type xUnit v3 chose for `IAsyncLifetime`.
7. `WriteAllText` creates or **overwrites**; `AppendAllText` adds to the end. `WriteAllText` can destroy existing content.
8. (a) The file was not copied to the output directory (missing `CopyToOutputDirectory`). (b) The code used a path relative to the working directory, which differs in CI, instead of `AppContext.BaseDirectory`. (Also possible: case-sensitive file names on Linux agents, or backslashes in a hard-coded path.)
9. `{"Name":"Ann","Age":30}`
10. After about 200 ms the delay is cancelled and throws `TaskCanceledException`, which derives from `OperationCanceledException`. Catch `OperationCanceledException`.

</details>

### 3. Combined mini-program exercise: test result summarizer

Build a console program that combines the whole week:

1. Define `record TestResult(string Name, string Status, int DurationMs)` where `Status` is `"Passed"`, `"Failed"` or `"Skipped"`.
2. Create 6 sample results in code, and **save** them to `results.json` as indented JSON with camelCase names using `JsonSerializerOptions` (reused from one `static readonly` field).
3. **Load** the file back **asynchronously** (`File.OpenRead` + `JsonSerializer.DeserializeAsync`), using a `CancellationTokenSource` with a 2 second timeout.
4. Write a method `static List<TestResult> Select(List<TestResult> all, Func<TestResult, bool> filter)` and call it with two different lambdas (all failed tests; all tests slower than 200 ms).
5. Group by status with LINQ and print `Status: count (total ms)` in alphabetical order.
6. Write a `summary.txt` with `File.WriteAllLines` and print its content. Delete both files at the end.

Expected shape of the output (your numbers depend on your sample data):

```text
Failed tests: 2
Slow tests (>200ms): 3
Failed: 2 (800 ms)
Passed: 3 (440 ms)
Skipped: 1 (0 ms)
summary.txt written with 3 lines
```

<details>
<summary>Reference solution</summary>

```csharp
// Complete program
using System.Text.Json;

var all = new List<TestResult>
{
    new("Login_works", "Passed", 120),
    new("Login_wrong_password", "Failed", 350),
    new("Search_returns_items", "Passed", 90),
    new("Checkout_total", "Failed", 450),
    new("Profile_edit", "Passed", 230),
    new("Legacy_report", "Skipped", 0)
};

const string resultsFile = "results.json";
const string summaryFile = "summary.txt";

await File.WriteAllTextAsync(resultsFile, JsonSerializer.Serialize(all, Json.Options));

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
List<TestResult> loaded;
try
{
    await using var stream = File.OpenRead(resultsFile);
    loaded = await JsonSerializer.DeserializeAsync<List<TestResult>>(stream, Json.Options, cts.Token)
             ?? new List<TestResult>();
}
catch (OperationCanceledException)
{
    Console.WriteLine("Loading timed out");
    return;
}

var failed = Select(loaded, r => r.Status == "Failed");
var slow = Select(loaded, r => r.DurationMs > 200);
Console.WriteLine($"Failed tests: {failed.Count}");
Console.WriteLine($"Slow tests (>200ms): {slow.Count}");

var summary = loaded
    .GroupBy(r => r.Status)
    .OrderBy(g => g.Key)
    .Select(g => $"{g.Key}: {g.Count()} ({g.Sum(r => r.DurationMs)} ms)")
    .ToList();

foreach (var line in summary) Console.WriteLine(line);

File.WriteAllLines(summaryFile, summary);
Console.WriteLine($"{summaryFile} written with {File.ReadAllLines(summaryFile).Length} lines");

File.Delete(resultsFile);
File.Delete(summaryFile);

static List<TestResult> Select(List<TestResult> all, Func<TestResult, bool> filter)
{
    var result = new List<TestResult>();
    foreach (var item in all)
        if (filter(item)) result.Add(item);
    return result;
}

record TestResult(string Name, string Status, int DurationMs);

static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
```

```text
Failed tests: 2
Slow tests (>200ms): 3
Failed: 2 (800 ms)
Passed: 3 (440 ms)
Skipped: 1 (0 ms)
summary.txt written with 3 lines
```

</details>

### 4. Interview questions (Week 7 review)

**Q: Explain `async`/`await` in one minute.**
`async` marks a method that can `await`. `await` pauses that method until a `Task` finishes and gives the thread back meanwhile; then the method continues, and the result or exception of the task is delivered at the `await`. No new thread is created by `await` itself.

**Q: What are the dangers of `.Result` and `.Wait()`?**
Blocking a thread, possible deadlocks with a synchronization context, thread starvation, and `AggregateException` wrapping. Use `await`.

**Q: `Task.WhenAll` vs `Task.WhenAny`?**
`WhenAll` completes when every task completes (results returned in argument order). `WhenAny` completes when the first one does and returns that task; useful for races and timeouts.

**Q: What is the difference between `Func<T>` and `Action<T>`, and what is a closure?**
`Func` returns a value, `Action` returns `void`. A closure is a lambda that captures variables from its surrounding scope.

**Q: How would you load test data from a JSON file so it works locally and in CI?**
Store it in a `TestData` folder, copy it with `CopyToOutputDirectory` in the `.csproj`, build the path with `Path.Combine(AppContext.BaseDirectory, ...)`, and deserialize it into typed records with a shared `JsonSerializerOptions`.

**Q: Why does xUnit v3's `IAsyncLifetime` return `ValueTask`?**
`ValueTask` is allocation-free when the work completes synchronously and still awaitable when it does not; xUnit v3 adopted it for its async lifecycle members.

### Check

You are ready for Week 8 if you can write, from memory: an `async Task<T>` method with a `try/catch` around `await`, a `Func<T, bool>` parameter use, a `File.ReadLines` loop, and a `JsonSerializer.Deserialize<List<T>>` call.
