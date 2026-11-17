# Week 5: OOP, Part 2

This week finishes the object-oriented toolkit you need for test automation code: interfaces, the choice between abstract classes and interfaces, records, structs and enums, generics, and extension methods with partial classes. By the end you can read and design the kind of code found in a Playwright test framework: page objects behind interfaces, immutable test data, reusable generic helpers and fluent extension methods.

All examples are complete programs. Create a console project with `dotnet new console -o Week5`, replace `Program.cs` with the example, and run `dotnet run`. The project targets `net10.0` with implicit usings and nullable reference types enabled, which is the default template.

---

## Day 25: Interfaces

### What an interface is

A class answers the question "what is this thing, and how does it work?". An interface answers a different question: "what can this thing do?". An interface is a contract: a list of members (methods, properties, events, indexers) that a class promises to provide. The interface contains no state and, normally, no implementation. Any class that says it implements the interface must supply every member.

```csharp
IGreeter greeter = new FormalGreeter();
Console.WriteLine(greeter.Greet("Ana"));

greeter = new FriendlyGreeter();
Console.WriteLine(greeter.Greet("Ana"));

interface IGreeter
{
    string Greet(string name);
}

class FormalGreeter : IGreeter
{
    public string Greet(string name) => $"Good day, {name}.";
}

class FriendlyGreeter : IGreeter
{
    public string Greet(string name) => $"Hey {name}!";
}

// Output:
// Good day, Ana.
// Hey Ana!
```

Points to notice:

- By convention interface names start with `I`: `IGreeter`, `IDisposable`, `IEnumerable<T>`.
- Interface members have no access modifier and no body. They are implicitly public.
- The implementing class must declare its members `public` (unless it uses explicit implementation, shown later).
- The variable `greeter` has the type `IGreeter`. You can assign any object whose class implements it. This is polymorphism, the same idea as with base classes, but without needing a shared parent.
- You cannot write `new IGreeter()`. An interface is not an object; it describes one.

### Why bother? Code that depends on the contract

The real value appears when other code depends on the interface instead of the concrete class. Suppose a login test needs to write log messages. It does not care whether messages go to the console, a file or memory.

```csharp
var consoleLogger = new ConsoleLogger();
var memoryLogger = new MemoryLogger();

new LoginFlow(consoleLogger).Run("ana");
new LoginFlow(memoryLogger).Run("bob");

Console.WriteLine($"Captured {memoryLogger.Lines.Count} line(s):");
foreach (var line in memoryLogger.Lines)
{
    Console.WriteLine($"  {line}");
}

interface ITestLogger
{
    void Info(string message);
}

class ConsoleLogger : ITestLogger
{
    public void Info(string message) => Console.WriteLine($"[INFO] {message}");
}

class MemoryLogger : ITestLogger
{
    public List<string> Lines { get; } = new();
    public void Info(string message) => Lines.Add(message);
}

class LoginFlow
{
    private readonly ITestLogger _logger;

    public LoginFlow(ITestLogger logger) => _logger = logger;

    public void Run(string user)
    {
        _logger.Info($"Opening login page for {user}");
        _logger.Info($"Submitting credentials for {user}");
    }
}

// Output:
// [INFO] Opening login page for ana
// [INFO] Submitting credentials for ana
// Captured 2 line(s):
//   Opening login page for bob
//   Submitting credentials for bob
```

`LoginFlow` never mentions `ConsoleLogger` or `MemoryLogger`. Passing a dependency in through the constructor, typed as an interface, is called constructor injection (done by hand here). It makes code testable: in a unit test you pass a `MemoryLogger` and inspect what was logged. A hand-written stand-in object like `MemoryLogger` is often called a fake.

### Interface members: methods, properties and more

Interfaces may declare properties (get-only, or get and set), methods, events and indexers. The implementing class can implement a get-only interface property with a read/write property; it just has to provide at least what the contract requires.

```csharp
ITestEnvironment env = new StagingEnvironment();
Console.WriteLine(env.Name);
Console.WriteLine(env.BaseUrl);
Console.WriteLine(env.BuildUrl("/login"));

interface ITestEnvironment
{
    string Name { get; }
    string BaseUrl { get; }
    string BuildUrl(string path);
}

class StagingEnvironment : ITestEnvironment
{
    public string Name => "staging";
    public string BaseUrl { get; } = "https://staging.example.test";

    public string BuildUrl(string path) => BaseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
}

// Output:
// staging
// https://staging.example.test
// https://staging.example.test/login
```

### Implementing several interfaces

A class can inherit from only one base class, but it can implement any number of interfaces. List them after the base class, separated by commas.

```csharp
var user = new TestUser("ana", "Admin");
Console.WriteLine(user.Describe());
user.Reset();
Console.WriteLine(user.Describe());

interface IDescribable
{
    string Describe();
}

interface IResettable
{
    void Reset();
}

class TestUser : IDescribable, IResettable
{
    private readonly string _initialRole;

    public string Name { get; }
    public string Role { get; private set; }

    public TestUser(string name, string role)
    {
        Name = name;
        Role = role;
        _initialRole = role;
    }

    public string Describe() => $"{Name} ({Role})";

    public void Reset() => Role = "Guest";
}

// Output:
// ana (Admin)
// ana (Guest)
```

### Interfaces as parameter types, and testing the type at run time

A method that accepts an interface accepts every class that implements it. You can also ask an object at run time whether it supports an interface, using `is` with a pattern.

```csharp
object[] things = { new TestUser("ana", "Admin"), "plain string", new ScratchPad() };

foreach (var thing in things)
{
    if (thing is IDescribable d)
    {
        Console.WriteLine($"Describable: {d.Describe()}");
    }
    else
    {
        Console.WriteLine($"Not describable: {thing.GetType().Name}");
    }
}

PrintAll(new IDescribable[] { new TestUser("bob", "Guest"), new ScratchPad() });

static void PrintAll(IEnumerable<IDescribable> items)
{
    foreach (var item in items)
    {
        Console.WriteLine(item.Describe());
    }
}

interface IDescribable
{
    string Describe();
}

class TestUser : IDescribable
{
    private readonly string _name;
    private readonly string _role;

    public TestUser(string name, string role)
    {
        _name = name;
        _role = role;
    }

    public string Describe() => $"{_name} ({_role})";
}

class ScratchPad : IDescribable
{
    public string Describe() => "a scratch pad";
}

// Output:
// Describable: ana (Admin)
// Not describable: String
// Describable: a scratch pad
// bob (Guest)
// a scratch pad
```

Note that `IEnumerable<IDescribable>` is itself an interface. Arrays and `List<T>` implement it, which is why `PrintAll` accepts an array here and would accept a `List<IDescribable>` too.

### Explicit interface implementation

If two interfaces declare a member with the same name, or you want a member to be reachable only through the interface, implement it explicitly: no access modifier, and the interface name in front.

```csharp
var report = new Report();

Console.WriteLine(report.Title);                 // the class's own member
Console.WriteLine(((IPrintable)report).Print()); // reachable only via the interface
Console.WriteLine(((IExportable)report).Print());

interface IPrintable
{
    string Print();
}

interface IExportable
{
    string Print();
}

class Report : IPrintable, IExportable
{
    public string Title => "Q3 results";

    string IPrintable.Print() => "printing " + Title;
    string IExportable.Print() => "exporting " + Title;
}

// Output:
// Q3 results
// printing Q3 results
// exporting Q3 results
```

Writing `report.Print()` would not compile, because `Print` is not a public member of `Report`. Explicit implementation is uncommon in day-to-day code but is used when names collide, and you will see it in library code.

### Default interface members (brief)

Since C# 8 an interface may provide a default body for a member. Classes that do not implement it get the default. Such members are only reachable through the interface type, not through the class type. Use this sparingly: it exists mainly so library authors can add members to a published interface without breaking existing implementers.

```csharp
ITestLogger logger = new QuietLogger();
logger.Info("hello");
logger.Warn("careful");

interface ITestLogger
{
    void Info(string message);

    void Warn(string message) => Info("WARN: " + message);
}

class QuietLogger : ITestLogger
{
    public void Info(string message) => Console.WriteLine($"> {message}");
}

// Output:
// > hello
// > WARN: careful
```

### Built-in interfaces you will meet constantly

| Interface | Purpose |
|---|---|
| `IEnumerable<T>` | "I can be looped over with `foreach`." Implemented by arrays, `List<T>`, `Dictionary<K,V>` and the results of LINQ queries. |
| `IComparable<T>` | "I know how to order myself against another T." Used by `List<T>.Sort()`. |
| `IEquatable<T>` | "I know how to compare myself for equality with another T." |
| `IDisposable` | "I hold a resource that must be released." Covered on Day 35. |

Here is `IComparable<T>` in action. `CompareTo` returns a negative number if this object sorts before the other, zero if equal, positive if after.

```csharp
var results = new List<TestResult>
{
    new("Checkout", 1200),
    new("Login", 300),
    new("Search", 750),
};

results.Sort();

foreach (var r in results)
{
    Console.WriteLine($"{r.Name}: {r.DurationMs} ms");
}

class TestResult : IComparable<TestResult>
{
    public string Name { get; }
    public int DurationMs { get; }

    public TestResult(string name, int durationMs)
    {
        Name = name;
        DurationMs = durationMs;
    }

    public int CompareTo(TestResult? other)
    {
        if (other is null) return 1;
        return DurationMs.CompareTo(other.DurationMs);
    }
}

// Output:
// Login: 300 ms
// Search: 750 ms
// Checkout: 1200 ms
```

`Sort()` has no idea what a `TestResult` is. It only knows that the element type implements `IComparable<TestResult>` and calls `CompareTo`. That is the whole point of an interface.

### Common mistakes

- Forgetting `public` on the implementing member. The compiler reports that the class does not implement the interface member, or that the member must be public.
- Trying to put fields or constructors in an interface. Interfaces describe behaviour, not stored state.
- Writing `new IGreeter()`. Create an instance of a class that implements it instead.
- Making huge "god" interfaces with 20 methods. Prefer several small, focused interfaces (`IResettable`, `IDescribable`) that classes can combine.

### Why this matters for test automation

Test frameworks live on interfaces. A page object that depends on `ITestLogger` or `IClock` rather than on concrete classes can be tested without a browser. Test data providers (`ITestUserProvider`) let you swap a hard-coded list for a database or API call without touching tests. xUnit itself uses interfaces such as `IAsyncLifetime` (setup and teardown hooks) that your test classes implement; you will meet it in Phase 2.

### Interview questions

**Q1. What is an interface?** A contract that declares members without (normally) implementing them. A class that implements the interface must provide all of them. It lets code depend on behaviour rather than on a concrete type.

**Q2. Can a class implement multiple interfaces? Can it inherit multiple classes?** It can implement any number of interfaces but inherit from only one class.

**Q3. What is explicit interface implementation and when do you use it?** Implementing a member as `string IPrintable.Print()` with no access modifier. It is reachable only through the interface type. Used to resolve name collisions between interfaces or to hide members from the class's public surface.

**Q4. Can an interface contain fields or constructors?** No instance fields and no constructors. It can declare properties, methods, events, indexers, and (C# 8+) members with default bodies.

**Q5. Why program to an interface instead of a concrete class?** It reduces coupling, allows substituting implementations (for example a fake in tests), and makes code easier to change and test.

### Exercise

1. Define `ITestUserProvider` with a method `TestUser GetUser(string role)` (reuse a small `TestUser` class with `Name` and `Role`). Write two implementations: `InMemoryUserProvider` that returns users from a `Dictionary<string, TestUser>`, and `FixedUserProvider` that always returns the same user.
2. Write a class `LoginTest` whose constructor takes an `ITestUserProvider`. Its `Run()` method gets the "Admin" user and prints `Logging in as <name>`.
3. Run `LoginTest` once with each provider.
4. Make `TestUser` implement `IComparable<TestUser>` ordering by `Name`, then sort a list of five users.

### Check

- Can you explain, in one sentence, the difference between a class and an interface?
- Without looking: what happens if a class lists an interface but forgets one of its methods?
- Why does `LoginTest` become easier to test when it takes `ITestUserProvider` instead of `InMemoryUserProvider`?

---

## Day 26: Abstract class vs. interface

### Two tools for related jobs

You met abstract classes on Day 23 and interfaces on Day 25. Both let you write code against an abstraction and have several implementations. They differ in what they can carry.

| | Abstract class | Interface |
|---|---|---|
| Fields (state) | Yes | No instance fields |
| Constructors | Yes | No |
| Implemented methods | Yes | Only default interface members (C# 8+) |
| Abstract members | Yes | Yes (all members are abstract unless given a default) |
| Access modifiers on members | Yes | Members are public by default |
| How many can a class use | One (single inheritance) | Many |
| Can be instantiated | No | No |
| Models | "is a" relationship with shared code | "can do" capability |

The shorthand: use an abstract class when subclasses share code and state; use an interface when unrelated types must share a capability.

### An abstract class: shared state and shared behaviour

An abstract class can contain a partly finished implementation. Subclasses fill in the abstract parts. This is a common design for page objects: every page opens a URL and logs, but each page has its own URL.

```csharp
var login = new LoginPage("https://app.test");
var dashboard = new DashboardPage("https://app.test");

login.Open();
dashboard.Open();

Console.WriteLine($"Pages opened: {BasePage.OpenCount}");

abstract class BasePage
{
    public static int OpenCount { get; private set; }

    private readonly string _baseUrl;

    protected BasePage(string baseUrl) => _baseUrl = baseUrl;

    protected abstract string Path { get; }

    public void Open()
    {
        OpenCount++;
        Console.WriteLine($"Navigating to {_baseUrl}{Path}");
        OnOpened();
    }

    protected virtual void OnOpened() { }
}

class LoginPage : BasePage
{
    public LoginPage(string baseUrl) : base(baseUrl) { }

    protected override string Path => "/login";
}

class DashboardPage : BasePage
{
    public DashboardPage(string baseUrl) : base(baseUrl) { }

    protected override string Path => "/dashboard";

    protected override void OnOpened() => Console.WriteLine("Dashboard widgets loaded");
}

// Output:
// Navigating to https://app.test/login
// Navigating to https://app.test/dashboard
// Dashboard widgets loaded
// Pages opened: 2
```

`BasePage` holds a field (`_baseUrl`), a static counter, a constructor, a finished method (`Open`), one abstract member (`Path`) that every subclass must supply, and one virtual hook (`OnOpened`) that subclasses may override. An interface cannot do any of that. The `Open` method is a small example of the template method pattern: the base class defines the sequence of steps and subclasses customise individual steps.

An abstract class cannot be instantiated:

```text
var page = new BasePage("https://app.test");   // error CS0144: Cannot create an instance of the abstract type
```

### An interface: a capability across unrelated types

Now suppose several unrelated classes can be "screenshot-able": a page, a single component, a failed test result. They share no common base class and should not. An interface fits.

```csharp
var things = new List<ICapturable>
{
    new LoginPage(),
    new ErrorBanner("Invalid password"),
};

foreach (var thing in things)
{
    Console.WriteLine(thing.CaptureName());
}

interface ICapturable
{
    string CaptureName();
}

class LoginPage : ICapturable
{
    public string CaptureName() => "login-page.png";
}

class ErrorBanner : ICapturable
{
    private readonly string _text;

    public ErrorBanner(string text) => _text = text;

    public string CaptureName() => $"error-{_text.Replace(' ', '-').ToLowerInvariant()}.png";
}

// Output:
// login-page.png
// error-invalid-password.png
```

### The deciding factor: single inheritance

A class gets exactly one base class. If you spend that slot on an abstract class, you cannot also inherit another one. Interfaces do not use up the slot. That is why a class often has one base class and several interfaces:

```csharp
var page = new CheckoutPage();
page.Open();
Console.WriteLine(page.CaptureName());
page.Reset();

abstract class BasePage
{
    public void Open() => Console.WriteLine($"Open {GetType().Name}");
}

interface ICapturable
{
    string CaptureName();
}

interface IResettable
{
    void Reset();
}

class CheckoutPage : BasePage, ICapturable, IResettable
{
    public string CaptureName() => "checkout.png";

    public void Reset() => Console.WriteLine("Cart cleared");
}

// Output:
// Open CheckoutPage
// checkout.png
// Cart cleared
```

### Using both together

A very common design is an interface that defines what callers rely on, plus an abstract class that provides shared code for most implementations. Callers and tests depend on the interface. If someone needs a different base class, they can implement the interface directly.

```csharp
IPage page = new SearchPage();
Describe(page);

IPage fake = new FakePage();
Describe(fake);

static void Describe(IPage page)
{
    Console.WriteLine($"{page.Name} -> {page.Url}");
}

interface IPage
{
    string Name { get; }
    string Url { get; }
}

abstract class PageBase : IPage
{
    public string Name => GetType().Name;
    public abstract string Url { get; }
}

class SearchPage : PageBase
{
    public override string Url => "/search";
}

class FakePage : IPage
{
    public string Name => "FakePage (no base class)";
    public string Url => "about:blank";
}

// Output:
// SearchPage -> /search
// FakePage (no base class) -> about:blank
```

### Checklist for choosing

Ask these questions in order:

1. Do the types need to share fields, constructor logic or finished methods? If yes, an abstract class (possibly behind an interface).
2. Do unrelated types need to share a capability, or could one type need several such capabilities? Use interfaces.
3. Do you need to substitute a fake in tests? Make callers depend on an interface.
4. Are you unsure? Start with an interface. It is easier to add an abstract base class later than to remove a base class that callers depend on.

### A note on default interface members

Default interface members (Day 25) blur the line a little: an interface can now contain code. They still cannot hold instance state, still cannot have constructors, and are only accessible through the interface type. They do not replace abstract classes.

```csharp
IShape shape = new Square(3);
Console.WriteLine(shape.Describe());

var square = new Square(3);
// square.Describe();  // does not compile: Describe is not a member of Square
Console.WriteLine(((IShape)square).Describe());

interface IShape
{
    double Area { get; }

    string Describe() => $"Shape with area {Area}";
}

class Square : IShape
{
    private readonly double _side;

    public Square(double side) => _side = side;

    public double Area => _side * _side;
}

// Output:
// Shape with area 9
// Shape with area 9
```

### Common mistakes

- Using an abstract class when no code or state is shared. A pure list of method signatures belongs in an interface; the abstract class just uses up the single inheritance slot.
- Forgetting that an abstract member forces every non-abstract subclass to override it. A new abstract member added to a base class breaks all existing subclasses, whereas a new default interface member does not.
- Treating "has-a capability" as inheritance. A `FailedTestResult` is not a kind of `Screenshot`; it can be `ICapturable`.
- Omitting `override` or changing the accessibility when implementing an abstract member. The subclass must write `override` and match the base member's accessibility (for example `protected override`).

### Why this matters for test automation

Page object frameworks usually have an abstract `BasePage` holding the Playwright page, a base URL and helper methods, plus small interfaces for capabilities such as `IHasUrl`. Choosing correctly keeps page classes small and lets unit tests substitute fakes. Many frameworks also start with a deep inheritance chain and regret it; preferring composition and interfaces keeps your design flexible.

### Interview questions

**Q1. Abstract class vs. interface: what are the differences?** An abstract class can have fields, constructors and implemented members and supports only single inheritance. An interface defines a contract, has no instance state or constructors, and a class can implement many. Use the abstract class for shared code in a family of types; use an interface for a capability across unrelated types.

**Q2. Can an abstract class implement an interface?** Yes. It can implement some members and mark others `abstract` to force subclasses to implement them.

**Q3. Can an abstract class have a constructor?** Yes. It runs when a subclass is constructed and is used to initialise base state. It is typically `protected`.

**Q4. When would you pick an interface even though you share code?** When callers must be able to substitute implementations (tests, different back ends) or when implementers already have a different base class. Shared code can be placed in an optional abstract base class or in a helper.

**Q5. Does an interface let you simulate multiple inheritance?** Multiple inheritance of type, yes: a class can be treated as many interface types. Multiple inheritance of state, no.

### Exercise

1. Create an abstract class `ApiClientBase` with a constructor taking `baseUrl`, a protected method `string BuildUrl(string path)`, and an abstract property `string ServiceName`. Derive `UsersClient` and `OrdersClient`.
2. Create an interface `IHealthCheckable` with `bool IsHealthy()`. Make `UsersClient` implement it but not `OrdersClient`.
3. In a loop over `new ApiClientBase[] { ... }`, print each `ServiceName` and, using `is IHealthCheckable h`, the health result when available.
4. Write two sentences explaining why `IHealthCheckable` is an interface and not a second base class.

### Check

- Which of the two can hold a private field?
- Why can a class implement five interfaces but derive from only one class?
- You need `FailedTest`, `Screenshot` and `Report` to share a `Save()` capability and nothing else. Which tool, and why?

---

## Day 27: Records, structs, enums

### Records: classes designed for data

A lot of test code is "just data": a test user, an API response, a viewport size. Writing a class with properties, a constructor, `Equals`, `GetHashCode` and `ToString` by hand is repetitive. A record does it for you.

First, the problem. Two class instances with identical contents are not equal by default, because classes compare by reference (whether they are the same object in memory):

```csharp
var a = new UserClass("ana", "Admin");
var b = new UserClass("ana", "Admin");

Console.WriteLine(a == b);
Console.WriteLine(a.Equals(b));
Console.WriteLine(a);

class UserClass
{
    public string Name { get; }
    public string Role { get; }

    public UserClass(string name, string role)
    {
        Name = name;
        Role = role;
    }
}

// Output:
// False
// False
// UserClass
```

The same thing as a record:

```csharp
var a = new User("ana", "Admin");
var b = new User("ana", "Admin");
var c = new User("bob", "Guest");

Console.WriteLine(a == b);
Console.WriteLine(a.Equals(c));
Console.WriteLine(ReferenceEquals(a, b));
Console.WriteLine(a);
Console.WriteLine(a.Name);

record User(string Name, string Role);

// Output:
// True
// False
// False
// User { Name = ana, Role = Admin }
// ana
```

`record User(string Name, string Role);` is a positional record. The compiler generates a constructor, init-only properties `Name` and `Role`, value-based `Equals` and `==`, `GetHashCode`, and a readable `ToString`. Two records are equal when their types and all property values are equal. `ReferenceEquals` still shows they are different objects.

A `record` declared this way is a reference type (a class under the hood). Since C# 10 you can write `record class` to say so explicitly.

### Non-destructive mutation with `with`

Record properties from a positional declaration are `init`-only: you can set them in a constructor or an object initializer, not afterwards. To get a modified copy, use a `with` expression.

```csharp
var admin = new User("ana", "Admin");
var guest = admin with { Role = "Guest" };

Console.WriteLine(admin);
Console.WriteLine(guest);
Console.WriteLine(admin == guest);

// admin.Role = "x";   // does not compile: init-only property

record User(string Name, string Role);

// Output:
// User { Name = ana, Role = Admin }
// User { Name = ana, Role = Guest }
// False
```

`admin` is unchanged; `guest` is a new object with one property replaced. Immutable test data is safer in parallel tests because no test can change an object another test is using.

### Deconstruction and extra members

Positional records can be deconstructed into variables. Records can also have extra properties, methods and validation.

```csharp
var response = new ApiResponse(404, "Not found");

var (status, body) = response;
Console.WriteLine($"{status}: {body}");
Console.WriteLine(response.IsSuccess);
Console.WriteLine(new ApiResponse(200, "ok").IsSuccess);

record ApiResponse(int StatusCode, string Body)
{
    public bool IsSuccess => StatusCode is >= 200 and < 300;
}

// Output:
// 404: Not found
// False
// True
```

### Records with `init` properties

Instead of positional syntax you can declare properties yourself. This is better when there are many optional properties.

```csharp
var settings = new BrowserSettings
{
    Name = "chromium",
    Headless = true,
};

var debug = settings with { Headless = false };

Console.WriteLine(settings);
Console.WriteLine(debug);
Console.WriteLine(settings.ViewportWidth);

record BrowserSettings
{
    public required string Name { get; init; }
    public bool Headless { get; init; } = true;
    public int ViewportWidth { get; init; } = 1280;
}

// Output:
// BrowserSettings { Name = chromium, Headless = True, ViewportWidth = 1280 }
// BrowserSettings { Name = chromium, Headless = False, ViewportWidth = 1280 }
// 1280
```

`required` forces the caller to set `Name` when creating the object; leaving it out is a compile error.

Records can inherit from other records, and equality takes the exact type into account. Records are the best default for DTOs (data transfer objects), such as the models you will deserialise from API responses.

### Structs: value types

Classes and records are reference types: a variable holds a reference to an object on the heap, and assigning copies the reference. A struct is a value type: a variable holds the data itself, and assigning copies the data.

```csharp
var p1 = new PointStruct { X = 1, Y = 1 };
var p2 = p1;          // copies the data
p2.X = 99;

Console.WriteLine($"struct: p1.X={p1.X}, p2.X={p2.X}");

var c1 = new PointClass { X = 1, Y = 1 };
var c2 = c1;          // copies the reference; both names point at one object
c2.X = 99;

Console.WriteLine($"class:  c1.X={c1.X}, c2.X={c2.X}");

struct PointStruct
{
    public int X { get; set; }
    public int Y { get; set; }
}

class PointClass
{
    public int X { get; set; }
    public int Y { get; set; }
}

// Output:
// struct: p1.X=1, p2.X=99
// class:  c1.X=99, c2.X=99
```

Structs also behave differently when passed to methods: the method receives a copy, so changes it makes do not affect the caller's variable (unless you use `ref`).

```csharp
var size = new Size(800, 600);
Grow(size);
Console.WriteLine($"{size.Width}x{size.Height}");

var grown = Grown(size);
Console.WriteLine($"{grown.Width}x{grown.Height}");

static void Grow(Size s)
{
    s.Width += 100;   // modifies the copy only
}

static Size Grown(Size s) => new Size(s.Width + 100, s.Height);

struct Size
{
    public int Width { get; set; }
    public int Height { get; set; }

    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }
}

// Output:
// 800x600
// 900x600
```

This "mutating a copy" surprise is why the guidance is to keep structs small and immutable. Make them `readonly struct` (every member is read-only) or use a `readonly record struct`, which also gives you value equality and `ToString`:

```csharp
var a = new Viewport(1280, 720);
var b = new Viewport(1280, 720);

Console.WriteLine(a == b);
Console.WriteLine(a);
Console.WriteLine(a with { Height = 1024 });

readonly record struct Viewport(int Width, int Height);

// Output:
// True
// Viewport { Width = 1280, Height = 720 }
// Viewport { Width = 1280, Height = 1024 }
```

When to choose a struct: the type is small (the usual guidance is around 16 bytes or less), represents a single value (a point, a size, a duration), and is immutable. Otherwise use a class or record. Structs cannot inherit from other types (they can implement interfaces), and a struct always has a parameterless default (`default(Viewport)` is all zeros).

### Enums: named constants

An enum is a type with a fixed set of named values. It replaces "magic" strings and numbers.

```csharp
Browser b = Browser.Firefox;
Console.WriteLine(b);
Console.WriteLine((int)b);

Browser fromInt = (Browser)2;
Console.WriteLine(fromInt);

Console.WriteLine(Enum.Parse<Browser>("Webkit"));
Console.WriteLine(Enum.TryParse<Browser>("opera", ignoreCase: true, out var opera));
Console.WriteLine(Enum.TryParse<Browser>("firefox", ignoreCase: true, out var ff));
Console.WriteLine(ff);

foreach (var value in Enum.GetValues<Browser>())
{
    Console.WriteLine($"{(int)value} = {value}");
}

Console.WriteLine(Describe(Browser.Chromium));
Console.WriteLine(Describe(Browser.Webkit));

static string Describe(Browser browser) => browser switch
{
    Browser.Chromium => "Chrome and Edge engine",
    Browser.Firefox => "Gecko engine",
    Browser.Webkit => "Safari engine",
    _ => "unknown",
};

enum Browser
{
    Chromium,
    Firefox,
    Webkit,
}

// Output:
// Firefox
// 1
// Webkit
// Webkit
// False
// True
// Firefox
// 0 = Chromium
// 1 = Firefox
// 2 = Webkit
// Chrome and Edge engine
// Safari engine
```

Facts about enums:

- Each name maps to an integer, starting at 0 and increasing by 1 unless you assign values (`Passed = 1, Failed = 2`). The underlying type is `int` by default.
- A cast such as `(Browser)42` is allowed even if 42 is not a defined value. Use `Enum.IsDefined` or `TryParse` plus a check when input is untrusted.
- `Enum.TryParse` returns `false` for unknown names without throwing. `Enum.Parse` throws.
- Always give an enum member the value 0 meaning something sensible, because `default(Browser)` is 0.

### `[Flags]` enums: combining values

When options can be combined, give each member a distinct bit (1, 2, 4, 8...) and add `[Flags]`. Combine with `|` and test with `HasFlag` or `&`.

```csharp
var enabled = Features.Screenshots | Features.Video;

Console.WriteLine(enabled);
Console.WriteLine(enabled.HasFlag(Features.Video));
Console.WriteLine(enabled.HasFlag(Features.Trace));

enabled |= Features.Trace;
Console.WriteLine(enabled);
Console.WriteLine((int)enabled);

enabled &= ~Features.Screenshots;
Console.WriteLine(enabled);

[Flags]
enum Features
{
    None = 0,
    Screenshots = 1,
    Video = 2,
    Trace = 4,
}

// Output:
// Screenshots, Video
// True
// False
// Screenshots, Video, Trace
// 7
// Video, Trace
```

### Choosing between class, record, struct and enum

| Need | Use |
|---|---|
| Object with behaviour and identity (a page object, a service) | `class` |
| Data carrier with value equality (DTO, test data, API model) | `record` |
| Small immutable single value (point, size, duration) | `readonly record struct` or `readonly struct` |
| One of a fixed set of choices | `enum` |
| Combinable options | `[Flags] enum` |

### Common mistakes

- Expecting `==` on two class instances to compare contents. It compares references unless the class overrides equality (records do).
- Mutating a struct stored in a `List<T>` or returned from a property. You edit a copy and the change is lost. Prefer immutable structs.
- Using `with` on a record and expecting the original to change. It returns a new object.
- Casting an integer to an enum without checking it is a defined value, then getting a value that prints as a number.

### Why this matters for test automation

Records suit test data (`record TestUser(string Name, string Password)`) and deserialised API responses because equality in assertions "just works": `Assert.Equal(expected, actual)` compares contents. Enums describe browsers, environments and user roles instead of error-prone strings. `[Flags]` enums model feature toggles. Understanding value versus reference semantics prevents subtle bugs where one test changes data another test shares.

### Interview questions

**Q1. What is the difference between a class and a struct?** A class is a reference type: variables hold references, assignment copies the reference and instances live on the heap (usually). A struct is a value type: assignment copies the data, it cannot inherit from another type and is best used for small immutable values.

**Q2. What does a record give you?** Compiler-generated value-based equality, `GetHashCode`, `ToString`, `with` expressions, and for positional records a constructor, init-only properties and a deconstructor.

**Q3. Is a record a value type?** `record` and `record class` are reference types. `record struct` is a value type.

**Q4. What does `[Flags]` do on an enum?** It marks the enum as a bit set, so values can be combined and `ToString` prints combinations by name. You still must assign powers of two yourself.

**Q5. What is the default value of an enum, and why does it matter?** `default` is the value 0 even if no member is named for it, so define a meaningful member with value 0.

### Exercise

1. Create `record TestUser(string Name, string Password, UserRole Role)` and `enum UserRole { Guest, Standard, Admin }`. Create two identical users, compare them with `==`, and print both. Create a copy with a different `Role` using `with`.
2. Write a `readonly record struct Viewport(int Width, int Height)` with a property `bool IsMobile => Width < 768`. Test it with two sizes.
3. Create a `[Flags] enum TestTags { None = 0, Smoke = 1, Regression = 2, Slow = 4 }`. Combine `Smoke | Slow`, then print whether `Regression` is set.
4. Write a `static string Describe(UserRole role)` using a switch expression.

### Check

- After `var b = a with { Name = "x" };`, does `a.Name` change?
- Why is a mutable struct risky?
- What does `(UserRole)10` produce when `UserRole` has only three members?

---

## Day 28: Generics

### The problem generics solve

Suppose you need a method that returns the first element of a list. Without generics you would write one version per type, or use `object` and cast everywhere (losing type safety):

```csharp
var names = new List<string> { "ana", "bob" };
var codes = new List<int> { 200, 404 };

Console.WriteLine(FirstString(names));
Console.WriteLine(FirstInt(codes));

static string FirstString(List<string> items) => items[0];
static int FirstInt(List<int> items) => items[0];

// Output:
// ana
// 200
```

Generics let you write the logic once, with a placeholder for the type. The placeholder is written in angle brackets and by convention called `T` (or `TKey`, `TValue`, `TResult` when there are several).

### Generic methods

```csharp
var names = new List<string> { "ana", "bob" };
var codes = new List<int> { 200, 404 };

Console.WriteLine(First(names));
Console.WriteLine(First(codes));
Console.WriteLine(First<string>(new List<string> { "explicit" }));

int x = 1, y = 2;
Swap(ref x, ref y);
Console.WriteLine($"{x} {y}");

string s1 = "left", s2 = "right";
Swap(ref s1, ref s2);
Console.WriteLine($"{s1} {s2}");

static T First<T>(List<T> items) => items[0];

static void Swap<T>(ref T a, ref T b)
{
    T temp = a;
    a = b;
    b = temp;
}

// Output:
// ana
// 200
// explicit
// 2 1
// right left
```

The compiler normally infers `T` from the arguments (type inference), so you can write `First(names)`. You can also give it explicitly with `First<string>(...)`. The generated code is type-safe: `First(names)` is known to return a `string`, no cast required.

### Generic classes

A class can have type parameters. Every use of the class supplies the actual type.

```csharp
var ids = new Box<int>(42);
var label = new Box<string>("login");

Console.WriteLine(ids.Value + 1);
Console.WriteLine(label.Value.ToUpper());
Console.WriteLine(ids);
Console.WriteLine(label);

// ids.Value = "text";   // does not compile: Value is an int

class Box<T>
{
    public T Value { get; set; }

    public Box(T value) => Value = value;

    public override string ToString() => $"Box<{typeof(T).Name}>({Value})";
}

// Output:
// 43
// LOGIN
// Box<Int32>(42)
// Box<String>(login)
```

You already use generic classes every day: `List<T>`, `Dictionary<TKey, TValue>`, `Queue<T>`, `HashSet<T>`. Each combination such as `Box<int>` and `Box<string>` is a distinct (constructed) type.

### A practical generic: `Result<T>`

A common pattern in framework code is returning either a value or an error message instead of throwing.

```csharp
var ok = Result<int>.Success(200);
var bad = Result<int>.Failure("timeout");

Print(ok);
Print(bad);
Print(Parse("17"));
Print(Parse("abc"));

static Result<int> Parse(string text) =>
    int.TryParse(text, out var n)
        ? Result<int>.Success(n)
        : Result<int>.Failure($"'{text}' is not a number");

static void Print<T>(Result<T> result)
{
    if (result.IsSuccess)
    {
        Console.WriteLine($"OK: {result.Value}");
    }
    else
    {
        Console.WriteLine($"ERROR: {result.Error}");
    }
}

class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}

// Output:
// OK: 200
// ERROR: timeout
// OK: 17
// ERROR: 'abc' is not a number
```

`default` gives the default value of `T`: `0` for `int`, `null` for reference types. The `T?` on `Value` means "may be null or default".

### Constraints: telling the compiler what `T` can do

Inside a generic method the compiler knows nothing about `T`; you cannot call `T`'s methods or compare values. A `where` clause adds constraints.

| Constraint | Meaning |
|---|---|
| `where T : class` | `T` must be a reference type |
| `where T : struct` | `T` must be a non-nullable value type |
| `where T : new()` | `T` must have a public parameterless constructor |
| `where T : SomeBase` | `T` must be `SomeBase` or derive from it |
| `where T : ISomeInterface` | `T` must implement the interface |
| `where T : notnull` | `T` must be non-nullable |

```csharp
Console.WriteLine(Max(3, 9));
Console.WriteLine(Max("apple", "pear"));

var page = Create<LoginPage>();
Console.WriteLine(page.Url);

var pages = new List<IPage> { new LoginPage(), new HomePage() };
PrintUrls(pages);
PrintUrls(new List<LoginPage> { new LoginPage() });

static T Max<T>(T a, T b) where T : IComparable<T>
{
    return a.CompareTo(b) >= 0 ? a : b;
}

static T Create<T>() where T : IPage, new()
{
    return new T();
}

static void PrintUrls<T>(IEnumerable<T> pages) where T : IPage
{
    foreach (var p in pages)
    {
        Console.WriteLine(p.Url);
    }
}

interface IPage
{
    string Url { get; }
}

class LoginPage : IPage
{
    public string Url => "/login";
}

class HomePage : IPage
{
    public string Url => "/home";
}

// Output:
// 9
// pear
// /login
// /login
// /home
// /login
```

`Max` can call `CompareTo` because the constraint says `T` implements `IComparable<T>`. `Create<T>` can write `new T()` because of `new()`. Remember that `PrintUrls` also accepts a `List<LoginPage>`: `List<LoginPage>` is an `IEnumerable<LoginPage>`, and the constraint is satisfied by `LoginPage`.

### Generic interfaces

Interfaces can be generic. This is how repositories, providers and handlers are written.

```csharp
IRepository<TestUser> users = new InMemoryRepository<TestUser>(u => u.Id);
users.Add(new TestUser(1, "ana"));
users.Add(new TestUser(2, "bob"));

Console.WriteLine(users.GetById(2)?.Name);
Console.WriteLine(users.GetById(99) is null);
Console.WriteLine(users.GetAll().Count);

IRepository<string> words = new InMemoryRepository<string>(w => w.Length);
words.Add("hi");
words.Add("hello");
Console.WriteLine(words.GetById(5));

record TestUser(int Id, string Name);

interface IRepository<T>
{
    void Add(T item);
    T? GetById(int id);
    IReadOnlyList<T> GetAll();
}

class InMemoryRepository<T> : IRepository<T>
{
    private readonly Dictionary<int, T> _items = new();
    private readonly Func<T, int> _getId;

    public InMemoryRepository(Func<T, int> getId) => _getId = getId;

    public void Add(T item) => _items[_getId(item)] = item;

    public T? GetById(int id) => _items.TryGetValue(id, out var item) ? item : default;

    public IReadOnlyList<T> GetAll() => _items.Values.ToList();
}

// Output:
// bob
// True
// 2
// hello
```

`Func<T, int>` is a type for "a function that takes a `T` and returns an `int`". You will study `Func` properly in Week 7; for now read `u => u.Id` as "given a user, produce its id".

### A reusable helper: retry

Generics shine in helpers. Flaky operations (waiting for an element, calling a slow API) are often retried. This helper works with any return type. (`Func<T>` means "a piece of code that returns a `T`"; covered in Week 7.)

```csharp
int attempts = 0;

string token = Retry(() =>
{
    attempts++;
    if (attempts < 3) throw new InvalidOperationException("not ready");
    return "token-abc";
}, maxAttempts: 5);

Console.WriteLine($"{token} after {attempts} attempts");

int number = Retry(() => 7, maxAttempts: 2);
Console.WriteLine(number);

static T Retry<T>(Func<T> action, int maxAttempts)
{
    for (int attempt = 1; ; attempt++)
    {
        try
        {
            return action();
        }
        catch (Exception) when (attempt < maxAttempts)
        {
            Console.WriteLine($"Attempt {attempt} failed, retrying");
        }
    }
}

// Output:
// Attempt 1 failed, retrying
// Attempt 2 failed, retrying
// token-abc after 3 attempts
// 7
```

The `try`/`catch` syntax is explained on Day 34. The `when` filter makes the catch apply only while attempts remain; on the last attempt the exception escapes to the caller.

### Static members and `typeof(T)`

Each constructed generic type has its own static members, and you can inspect `T` with `typeof(T)`.

```csharp
Counter<int>.Increment();
Counter<int>.Increment();
Counter<string>.Increment();

Console.WriteLine(Counter<int>.Count);
Console.WriteLine(Counter<string>.Count);
Console.WriteLine(TypeName<List<int>>());
Console.WriteLine(TypeName<double>());

static string TypeName<T>() => typeof(T).Name;

class Counter<T>
{
    public static int Count { get; private set; }

    public static void Increment() => Count++;
}

// Output:
// 2
// 1
// List`1
// Double
```

`List`1` is the runtime name of a generic type with one type parameter.

### Common mistakes

- Using `object` where a generic would do. You lose compile-time checking and add casts and boxing.
- Calling a method on `T` without a constraint. The compiler error says `T` does not contain a definition; add `where T : ...`.
- Writing `new T()` without the `new()` constraint.
- Assuming `T` can be compared with `>`; operators are not available unless you use `IComparable<T>` or the newer generic math interfaces.
- Over-generalising. If only one concrete type will ever be used, a generic adds noise.

### Why this matters for test automation

Frameworks lean on generics: `List<T>`, `Dictionary<K,V>`, `Task<T>` (Week 7), `IEnumerable<T>` (LINQ), `Assert.Equal<T>`, `IClassFixture<T>` and `TheoryData<T>` in xUnit. Writing your own generic helpers (retry, polling, `Result<T>`, page factories like `Create<T>() where T : BasePage, new()`) removes duplicate code across hundreds of tests.

### Interview questions

**Q1. What are generics and why use them?** Type parameters that let you write a class or method once for many types while keeping compile-time type safety and avoiding casts and boxing.

**Q2. What is the difference between `List<object>` and `List<T>`?** `List<object>` can hold anything but needs casts to read and boxes value types. `List<int>` is checked at compile time and stores ints directly.

**Q3. What are generic constraints? Give examples.** The `where` clause that restricts type arguments: `class`, `struct`, `new()`, a base class, an interface, `notnull`. They allow the generic code to use members of the constrained type.

**Q4. What does `default(T)` return?** The default value: `null` for reference types, zero or the all-zero value for value types.

**Q5. Does a generic class share static fields across `Box<int>` and `Box<string>`?** No. Each constructed type has its own statics.

### Exercise

1. Write `static T LastOrFallback<T>(List<T> items, T fallback)` that returns the last item or the fallback if the list is empty. Test with `int` and `string`.
2. Write `class Pair<TFirst, TSecond>` with two get-only properties and a method `Pair<TSecond, TFirst> Swap()`. Print the result.
3. Write `static T CreatePage<T>() where T : IPage, new()` and use it with two page classes.
4. Write `static T Poll<T>(Func<T> read, Func<T, bool> isReady, int maxTries)` that returns the first value for which `isReady` is true, or throws `TimeoutException` after `maxTries`. Test it with a counter that becomes ready on the fourth call.

### Check

- What does the compiler infer for `T` in `First(new List<string>())`?
- Why does `Max<T>` need `where T : IComparable<T>`?
- How many distinct types are created by `Counter<int>`, `Counter<string>` and `Counter<int>`?

---

## Day 29: Extension methods, partial classes

### Extension methods: adding methods you do not own

You cannot edit `string` or `List<T>`, but you can write methods that look like they belong to them. An extension method is a normal static method in a static class whose first parameter is marked with `this`. The caller writes `value.Method()` and the compiler translates it into `StaticClass.Method(value)`.

```csharp
string raw = "  Hello World  ";

Console.WriteLine(raw.Shout());
Console.WriteLine(StringExtensions.Shout(raw));   // the same call, written the long way
Console.WriteLine("login page".ToSlug());
Console.WriteLine("abc".Repeat(3));

static class StringExtensions
{
    public static string Shout(this string text) => text.Trim().ToUpperInvariant() + "!";

    public static string ToSlug(this string text) => text.Trim().ToLowerInvariant().Replace(' ', '-');

    public static string Repeat(this string text, int times) => string.Concat(Enumerable.Repeat(text, times));
}

// Output:
// HELLO WORLD!
// HELLO WORLD!
// login-page
// abcabcabc
```

Rules:

- The class must be `static` and non-generic and not nested.
- The method must be `static` and its first parameter carries `this`.
- The extension is only visible where the class's namespace is imported with `using` (the default template has no namespace here, so it is always in scope).
- Extension methods cannot access private members of the type they extend; they only use its public API.

### Extending generic and interface types

Extending an interface such as `IEnumerable<T>` makes the method available on arrays, lists and query results at once. This is how LINQ (Week 6) is built: `Where`, `Select` and others are extension methods on `IEnumerable<T>`.

```csharp
var browsers = new List<string> { "chromium", "firefox", "webkit" };
int[] codes = { 200, 404, 500 };

Console.WriteLine(browsers.JoinWith(" | "));
Console.WriteLine(codes.JoinWith(","));
Console.WriteLine(browsers.IsEmpty());
Console.WriteLine(new List<int>().IsEmpty());
Console.WriteLine(browsers.SecondOrDefault());

static class CollectionExtensions
{
    public static string JoinWith<T>(this IEnumerable<T> items, string separator)
        => string.Join(separator, items);

    public static bool IsEmpty<T>(this IEnumerable<T> items) => !items.Any();

    public static T? SecondOrDefault<T>(this IEnumerable<T> items)
        => items.Skip(1).FirstOrDefault();
}

// Output:
// chromium | firefox | webkit
// 200,404,500
// False
// True
// firefox
```

### Fluent helpers and null handling

An extension method is a static call, so it can run even when the receiver is `null`. That is occasionally useful, and a classic source of confusion.

```csharp
string? a = null;
string? b = "  ";
string? c = "ok";

Console.WriteLine(a.IsBlank());
Console.WriteLine(b.IsBlank());
Console.WriteLine(c.IsBlank());
Console.WriteLine(a.OrDefault("n/a"));
Console.WriteLine(c.OrDefault("n/a"));

static class NullSafe
{
    public static bool IsBlank(this string? text) => string.IsNullOrWhiteSpace(text);

    public static string OrDefault(this string? text, string fallback) => text.IsBlank() ? fallback : text!;
}

// Output:
// True
// True
// False
// n/a
// ok
```

The `!` after `text` is the "null-forgiving" operator: it tells the compiler you know the value is not null. It does nothing at run time.

### Instance methods always win

If a type already has an instance method with the same signature, the instance method is called and your extension is ignored. Do not try to override behaviour with extensions.

```csharp
var tool = new Tool();
Console.WriteLine(tool.Run());       // instance method
Console.WriteLine(tool.Run(2));      // extension method (different signature)
Console.WriteLine(ToolExtensions.Run(tool));   // forces the extension

class Tool
{
    public string Run() => "instance Run";
}

static class ToolExtensions
{
    public static string Run(this Tool tool) => "extension Run";

    public static string Run(this Tool tool, int times) => $"extension Run x{times}";
}

// Output:
// instance Run
// extension Run x2
// extension Run
```

### Extensions on your own types and enums

Extensions are handy to add formatting or conversions without cluttering a model class.

```csharp
var result = new TestResult("Login", TestStatus.Failed, 1250);

Console.WriteLine(result.Summary());
Console.WriteLine(TestStatus.Passed.Icon());
Console.WriteLine(TestStatus.Skipped.Icon());
Console.WriteLine(result.DurationMs.ToSeconds());

record TestResult(string Name, TestStatus Status, int DurationMs);

enum TestStatus
{
    Passed,
    Failed,
    Skipped,
}

static class TestExtensions
{
    public static string Icon(this TestStatus status) => status switch
    {
        TestStatus.Passed => "[PASS]",
        TestStatus.Failed => "[FAIL]",
        _ => "[SKIP]",
    };

    public static string Summary(this TestResult r) => $"{r.Status.Icon()} {r.Name} ({r.DurationMs.ToSeconds()})";

    public static string ToSeconds(this int milliseconds) => $"{milliseconds / 1000.0:0.00}s";
}

// Output:
// [FAIL] Login (1.25s)
// [PASS]
// [SKIP]
// 1.25s
```

### Partial classes: one class, several files

A `partial` class is a single class whose definition is split across several declarations, which the compiler merges. All parts need the `partial` keyword, the same name, the same namespace and the same assembly. Typical uses:

- Separating hand-written code from generated code (designers, source generators), so regeneration does not overwrite your code.
- Splitting a very large class into logical files.

For a demo, the parts can sit in one file. In a real project they would normally live in separate files such as `LoginPage.cs` and `LoginPage.Locators.cs`.

```csharp
var page = new LoginPage();
page.Fill("ana", "secret");
Console.WriteLine(page.Describe());

// First part: behaviour
partial class LoginPage
{
    public void Fill(string user, string password)
    {
        UserName = user;
        Password = password;
    }
}

// Second part: data (for example, locators or generated members)
partial class LoginPage
{
    public string UserName { get; private set; } = "";
    public string Password { get; private set; } = "";

    public string Describe() => $"user={UserName}, password length={Password.Length}";
}

// Output:
// user=ana, password length=6
```

Parts can implement different interfaces and one part may name the base class; the combined class gets all of them.

### Partial methods

A partial method has a declaration in one part and, optionally, an implementation in another. The classic form (no access modifier, returns `void`) is removed by the compiler entirely when no implementation exists. A partial method with an access modifier, a non-void return type, or `out` parameters must have an implementation.

```csharp
var report = new Report();
report.Save();

partial class Report
{
    partial void OnSaving();          // optional hook, no implementation required

    public void Save()
    {
        OnSaving();
        Console.WriteLine("Saved");
    }
}

partial class Report
{
    partial void OnSaving() => Console.WriteLine("About to save");
}

// Output:
// About to save
// Saved
```

If you delete the second part, the program still compiles and prints only `Saved`.

### Where you will see `partial` in modern .NET

Source generators produce code into the other half of a partial class. One that ships with .NET is `[GeneratedRegex]`: you declare a `partial` method and the compiler generates a fast regular expression for it.

```csharp
using System.Text.RegularExpressions;

Console.WriteLine(OrderIds.IsOrderId("ORD-1234"));
Console.WriteLine(OrderIds.IsOrderId("order 12"));

partial class OrderIds
{
    [GeneratedRegex(@"^ORD-\d{4}$")]
    private static partial Regex OrderIdPattern();

    public static bool IsOrderId(string text) => OrderIdPattern().IsMatch(text);
}

// Output:
// True
// False
```

You do not need to understand how the generator works. The takeaway is that `partial` is how your code and generated code share one class.

### Common mistakes

- Forgetting `static` on the extension class or method, or forgetting `this` on the first parameter. The method then is not an extension.
- Expecting an extension to override an instance method. The instance method always wins.
- Using extensions to reach into private state; they only see the public API.
- Declaring the parts of a partial class with different accessibility conflicts (`public partial class X` and `internal partial class X`), or leaving out `partial` on one part. Both are compile errors.
- Putting extensions in a namespace but not importing it, then wondering why the method "does not exist".

### Why this matters for test automation

Extension methods let you build a readable test vocabulary: `result.Status.Icon()`, `text.ToSlug()`, `responses.JoinWith("\n")`, or helpers on Playwright types in your own framework (for example waiting or retry helpers on a locator, added without modifying the library). Partial classes appear when generated code is involved: source generators, designer files and `[GeneratedRegex]`. Recognising `partial` stops you being confused when "half" of a class is in another file.

### Interview questions

**Q1. What is an extension method?** A static method in a static class, with `this` on the first parameter, that can be called as if it were an instance method of that type. It does not modify the type and can only use its public members.

**Q2. What happens if an instance method and an extension method have the same signature?** The instance method is chosen.

**Q3. Why does LINQ work on arrays and lists?** `Where`, `Select` and the rest are extension methods on `IEnumerable<T>`, which arrays and lists implement.

**Q4. What is a partial class and why would you use one?** A class split across multiple declarations merged at compile time. Used to separate generated code from hand-written code or to organise large classes.

**Q5. Can you call an extension method on a null reference?** Yes; it is a static call, so no `NullReferenceException` occurs at the call itself. The method gets `null` as its first argument and must handle it.

### Exercise

1. Write a static class `StringExtensions` with `Truncate(this string text, int max)` (adds `...` when longer than `max`) and `IsBlank(this string? text)`. Test with three values.
2. Write `static string Describe<T>(this IEnumerable<T> items)` that returns `"3 item(s): a, b, c"`.
3. Write an enum `Severity { Low, Medium, High }` and an extension `bool IsUrgent(this Severity s)`.
4. Split a `ReportBuilder` class into two `partial` parts: one with the fields and constructor, one with a `Build()` method. Show it works.

### Check

- What are the three syntactic requirements of an extension method?
- What does the compiler do with `text.Shout()`?
- Name two reasons to use `partial`.

---

## Day 30: Review and practice

### Recap

| Day | Topic | Key points |
|---|---|---|
| 25 | Interfaces | A contract of members; classes implement many; depend on interfaces to substitute fakes; `is IFoo f` tests support; explicit implementation; default members; `IComparable<T>`, `IEnumerable<T>`. |
| 26 | Abstract class vs. interface | Abstract: shared state and code, single inheritance. Interface: capability, many per class. Combine: interface for callers, abstract base for shared code. |
| 27 | Records, structs, enums | Records: value equality, `with`, concise data types. Structs: value types, copied on assignment, keep small and immutable. Enums: named constants; `[Flags]` for combinations; `default` is 0. |
| 28 | Generics | Type parameters `T`; generic methods, classes, interfaces; constraints with `where`; `default`; one statics set per constructed type. |
| 29 | Extension methods, partial classes | `static` class, `static` method, `this` first parameter; instance methods win; `partial` splits one class across declarations. |

### Practice questions

Try each before opening the answers.

1. A class `A` implements `IFoo` and `IBar`, both declaring `void Run()`. How do you give each a different implementation?
2. `record P(int X);` Then `var a = new P(1); var b = a; var c = new P(1);`. What do `a == b`, `a == c` and `ReferenceEquals(a, c)` give?
3. A `struct Counter { public int N; }` is stored in a `List<Counter>`. Why does `list[0].N++` not compile, and what does `var c = list[0]; c.N++;` do to the list?
4. What is wrong with `static T Add<T>(T a, T b) => a + b;`?
5. Write the declaration line of an extension method `Double` for `int`.
6. Which is correct for a `[Flags]` enum with `A = 1, B = 2, C = 4`: `A | C` equals 3, 5 or 6?
7. Can an interface have a constructor? Can an abstract class?
8. What is the output of `Console.WriteLine((Level)1);` for `enum Level { Low, Medium, High }`?
9. When two record instances of different record types have the same property values, are they equal?
10. Why prefer `IPage` over `LoginPage` as a parameter type in a helper method?

<details>
<summary>Answers</summary>

1. Explicit implementation: `void IFoo.Run() { ... }` and `void IBar.Run() { ... }`. Callers must cast to the interface to call them.
2. `a == b` is `True` (same object and same values), `a == c` is `True` (value equality), `ReferenceEquals(a, c)` is `False`.
3. `list[0]` returns a copy of the struct, so the compiler refuses to modify the temporary copy. `var c = list[0]; c.N++;` modifies the local copy only; the element in the list is unchanged. Replace the element with `list[0] = c;` or use an immutable struct.
4. The compiler does not know that `T` supports `+`. Without a constraint (such as the generic math interface `INumber<T>`) it does not compile.
5. `public static int Double(this int value) => value * 2;` inside a non-generic `static class`.
6. `A | C` is `5` (1 + 4); `3` would be `A | B`, and `6` would be `B | C`.
7. An interface cannot have a constructor; an abstract class can (used by derived constructors).
8. `Medium`.
9. No. Record equality also requires the same runtime type.
10. The helper then works with any implementation, including fakes and other page classes, and does not depend on a concrete class.

```csharp
Console.WriteLine((Level)1);
Console.WriteLine(Level.High | Level.Low);

[Flags]
enum Level
{
    Low = 1,
    Medium = 2,
    High = 4,
}

// Output:
// Low
// Low, High
```

Note that adding `[Flags]` changes the numbering in this snippet (`Low = 1`), so `(Level)1` prints `Low`. In question 8 the enum has no explicit values, so 1 is `Medium`.

</details>

### Combined mini-program: a test run reporter

This program uses everything from the week: an interface, a record, enums, a generic class with a constraint and an extension method. Read it, run it, then do the exercise below.

```csharp
var store = new ResultStore<TestResult>();
store.Add(new TestResult("Login_Succeeds", TestStatus.Passed, 820));
store.Add(new TestResult("Login_BadPassword_ShowsError", TestStatus.Passed, 640));
store.Add(new TestResult("Checkout_AppliesCoupon", TestStatus.Failed, 2310));
store.Add(new TestResult("Search_EmptyQuery", TestStatus.Skipped, 0));

IResultSink sink = new ConsoleSink();

foreach (var result in store.Items.OrderByDescending(r => r.DurationMs))
{
    sink.Write(result);
}

Console.WriteLine();
Console.WriteLine(store.Items.Summarize());
Console.WriteLine($"Failed tests: {store.CountWhere(r => r.Status == TestStatus.Failed)}");

interface IResultSink
{
    void Write(TestResult result);
}

class ConsoleSink : IResultSink
{
    public void Write(TestResult result) =>
        Console.WriteLine($"{result.Status.Icon()} {result.Name,-32} {result.DurationMs,5} ms");
}

record TestResult(string Name, TestStatus Status, int DurationMs);

enum TestStatus
{
    Passed,
    Failed,
    Skipped,
}

class ResultStore<T> where T : class
{
    private readonly List<T> _items = new();

    public IReadOnlyList<T> Items => _items;

    public void Add(T item) => _items.Add(item);

    public int CountWhere(Func<T, bool> predicate) => _items.Count(predicate);
}

static class ResultExtensions
{
    public static string Icon(this TestStatus status) => status switch
    {
        TestStatus.Passed => "[PASS]",
        TestStatus.Failed => "[FAIL]",
        _ => "[SKIP]",
    };

    public static string Summarize(this IEnumerable<TestResult> results)
    {
        var list = results.ToList();
        int passed = list.Count(r => r.Status == TestStatus.Passed);
        int failed = list.Count(r => r.Status == TestStatus.Failed);
        int skipped = list.Count(r => r.Status == TestStatus.Skipped);
        return $"Total {list.Count}: {passed} passed, {failed} failed, {skipped} skipped";
    }
}

// Output:
// [FAIL] Checkout_AppliesCoupon            2310 ms
// [PASS] Login_Succeeds                     820 ms
// [PASS] Login_BadPassword_ShowsError       640 ms
// [SKIP] Search_EmptyQuery                    0 ms
//
// Total 4: 2 passed, 1 failed, 1 skipped
// Failed tests: 1
```

`OrderByDescending`, `Count` and `ToList` are LINQ methods; they are the topic of Week 6, and you can read them as "sort", "count" and "make a list" for now. `{result.Name,-32}` left-aligns the name in a 32-character column, and `{result.DurationMs,5}` right-aligns the number in 5 characters.

**Exercise (combined)**

Extend the program:

1. Add `IResultSink` implementation `MemorySink` that stores formatted lines in a `List<string>`; write all results to both sinks and print the memory sink's line count.
2. Add a `[Flags] enum TestTags { None = 0, Smoke = 1, Regression = 2 }` property to `TestResult` (as a fourth positional parameter) and add an extension method `bool HasTag(this TestResult r, TestTags tag)`.
3. Create a failed copy of the first result with `with { Status = TestStatus.Failed }` and show that the original is unchanged.
4. Add a generic method `static T? FirstOrNothing<T>(IEnumerable<T> items, Func<T, bool> test)` that returns the first match or `default`.

### More interview questions

**Q1. Why would you depend on `IFoo` rather than `Foo`?** Looser coupling: you can replace implementations, use fakes in tests and change internals without changing callers.

**Q2. Give one design where you use an abstract class and an interface together.** `interface IPage` for callers; `abstract class PageBase : IPage` with shared navigation code; concrete pages derive from `PageBase`, and a fake page implements `IPage` directly.

**Q3. Record vs. class: when each?** A record for data with value equality and immutability (DTOs, test data, API models). A class for objects with identity and behaviour (services, page objects) or when reference equality is wanted.

**Q4. Why can a mutable struct produce confusing bugs?** Copies are made on assignment, when passing to methods and when reading from collections or properties, so modifications hit a copy. Keep structs immutable.

**Q5. What are generics constraints, and what does `where T : class, new()` mean?** Restrictions on type arguments. This one requires `T` to be a reference type with a public parameterless constructor.

**Q6. How do extension methods differ from inheritance?** They add no members and cannot override; they are static methods with call syntax sugar, resolved at compile time, and see only the public API.

**Q7. What is the difference between `Enum.Parse` and `Enum.TryParse`?** `Parse` throws on invalid input; `TryParse` returns `false`.

**Q8. Can a class have an `abstract` member without being abstract?** No. A class with an abstract member must itself be declared `abstract`.
