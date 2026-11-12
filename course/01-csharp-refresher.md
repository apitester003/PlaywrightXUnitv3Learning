# Part 1: C# Refresher (Days 1–12)

You know basic C#. This part fills the gaps you need for test automation: methods, classes, interfaces, `async`/`await`, and dependency injection. Every day has a small example you can run.

**Setup for this part:** create a scratch console app outside the repo:

```bash
dotnet new console -n CSharpPractice
cd CSharpPractice
dotnet run
```

Replace the contents of `Program.cs` with each day's example, then run it.

---

## Day 1: Variables, Types, and Interpolation

Variables hold values. `var` lets the compiler infer the type. Use `$"..."` to put values inside a string.

```csharp
string username = "standard_user";
int attempts = 3;
bool isLocked = false;
var greeting = $"Logging in as {username} (attempt {attempts})";

Console.WriteLine(greeting);
Console.WriteLine($"Locked: {isLocked}");
```

**Exercise:** declare a `password` variable and print `"User X uses password Y"`.

**Check:** the output matches your sentence exactly.

---

## Day 2: Methods

A method has a return type, a name, and parameters. `void` means it returns nothing.

```csharp
static string BuildGreeting(string name)
{
    return $"Hello, {name}!";
}

static void PrintLine(string text)
{
    Console.WriteLine(text);
}

PrintLine(BuildGreeting("Ana"));
```

**Exercise:** write `static bool IsValidUsername(string name)` that returns `true` when the name isn't empty.

**Check:** `IsValidUsername("")` returns `false`, and `IsValidUsername("bob")` returns `true`.

---

## Day 3: Conditionals and Loops

```csharp
string[] users = { "standard_user", "locked_out_user", "" };

foreach (var user in users)
{
    if (string.IsNullOrWhiteSpace(user))
    {
        Console.WriteLine("Empty username");
    }
    else if (user == "locked_out_user")
    {
        Console.WriteLine($"{user}: locked");
    }
    else
    {
        Console.WriteLine($"{user}: ok");
    }
}

for (int i = 1; i <= 3; i++)
{
    Console.WriteLine($"Attempt {i}");
}
```

**Exercise:** loop over the three users above and use a `switch` expression to print a status for each.

**Check:** you see one line per user.

---

## Day 4: Classes and Properties

A class groups data and behavior. Auto-properties store values. A constructor sets them when you create an object.

```csharp
public class User
{
    public string Username { get; set; }
    public string Password { get; set; }

    public User(string username, string password)
    {
        Username = username;
        Password = password;
    }

    public string Describe() => $"{Username} / {Password}";
}

var user = new User("standard_user", "secret_sauce");
Console.WriteLine(user.Describe());
```

**Exercise:** add a `bool IsLocked` property and a `Describe` that includes it.

**Check:** `new User(...) { IsLocked = true }` prints the lock status.

---

## Day 5: Interfaces

An interface lists what a class must do, without saying how. Tests depend on interfaces so you can swap implementations.

```csharp
public interface IGreeter
{
    string Greet(string name);
}

public class FriendlyGreeter : IGreeter
{
    public string Greet(string name) => $"Hi {name}!";
}

public class FormalGreeter : IGreeter
{
    public string Greet(string name) => $"Good day, {name}.";
}

IGreeter greeter = new FriendlyGreeter();
Console.WriteLine(greeter.Greet("Ana"));

greeter = new FormalGreeter();
Console.WriteLine(greeter.Greet("Ana"));
```

**Exercise:** write a third class `SilentGreeter` that returns an empty string.

**Check:** the same `IGreeter` variable works with all three classes.

**Repo link:** `Pages/ILoginPage.cs` defines the contract; `Pages/LoginPage.cs` implements it.

---

## Day 6: Inheritance and Abstract Classes

A class can inherit from another. An `abstract` class can't be created directly, and it can force subclasses to implement certain members.

```csharp
public abstract class Page
{
    public string Url { get; set; } = "";

    public void Open() => Console.WriteLine($"Opening {Url}");

    public abstract string Title();
}

public class LoginPage : Page
{
    public LoginPage() { Url = "https://www.saucedemo.com"; }

    public override string Title() => "Swag Labs";
}

var login = new LoginPage();
login.Open();
Console.WriteLine(login.Title());
```

**Exercise:** create an `InventoryPage` that inherits `Page` and overrides `Title()`.

**Check:** calling `Open()` on it prints its own URL.

**Repo link:** `Core/BaseTest.cs` is `abstract`, and `Tests/LoginTests.cs` inherits from it.

---

## Day 7: Collections

`List<T>` holds an ordered list. `Dictionary<TKey, TValue>` maps keys to values.

```csharp
var usernames = new List<string> { "standard_user", "problem_user" };
usernames.Add("performance_glitch_user");

var passwords = new Dictionary<string, string>
{
    ["standard_user"] = "secret_sauce",
    ["problem_user"] = "secret_sauce",
};

foreach (var name in usernames)
{
    Console.WriteLine($"{name} -> {passwords[name]}");
}
```

**Exercise:** add `locked_out_user` to both collections, then look up its password.

**Check:** the printed password is `secret_sauce`.

---

## Day 8: async and await

`async` methods run without blocking. `await` pauses the method until a `Task` finishes. Browser calls in Playwright are all `async`.

```csharp
static async Task LoadAsync()
{
    Console.WriteLine("Loading...");
    await Task.Delay(1000);
    Console.WriteLine("Done.");
}

await LoadAsync();
```

**Exercise:** write two methods, `FetchA` and `FetchB`, each waiting one second. Call them in sequence with `await`, then run them together with `await Task.WhenAll(...)`.

**Check:** the sequential version takes about two seconds; the `WhenAll` version takes about one.

**Repo link:** every method in `Pages/LoginPage.cs` returns `Task` and uses `await`.

---

## Day 9: Nullable Reference Types

With `<Nullable>enable</Nullable>` (set in your `.csproj`), `string` can't be null, but `string?` can. The compiler warns you if you use a nullable value unsafely.

```csharp
static string Describe(string? name)
{
    return name ?? "unknown";
}

string? maybe = null;
Console.WriteLine(Describe(maybe));
Console.WriteLine(maybe?.Length);   // null-conditional
```

**Exercise:** write a method that returns the length of a `string?`, or `0` when it's null.

**Check:** no compiler warnings appear.

**Repo link:** `Core/BasePageSimple.cs` takes `string? baseUrl`.

---

## Day 10: Dependency Injection

Instead of creating dependencies inside a class, you pass them in. A container builds the object graph for you.

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddScoped<IGreeter, FriendlyGreeter>();

var provider = services.BuildServiceProvider();
var greeter = provider.GetRequiredService<IGreeter>();
Console.WriteLine(greeter.Greet("Ana"));
```

You'll need to add the package: `dotnet add package Microsoft.Extensions.DependencyInjection`.

**Exercise:** create `Welcomer` that takes an `IGreeter` in its constructor. Register it and resolve it.

**Check:** `Welcomer` uses the greeter the container gave it.

**Repo link:** `Tests/LoginTests.cs`, `InitializeAsync`, builds a `ServiceCollection` the same way.

---

## Day 11: Exceptions

Use `try` to run code that might fail, `catch` to handle a specific error, and `finally` to run cleanup no matter what.

```csharp
static void ValidateUsername(string name)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        throw new ArgumentException("Username is required.", nameof(name));
    }
}

try
{
    ValidateUsername("");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Caught: {ex.Message}");
}
finally
{
    Console.WriteLine("Validation finished.");
}
```

**Exercise:** write `ValidatePassword` that throws if the password is shorter than six characters. Catch and print the error.

**Check:** a short password prints the message; a long one prints nothing.

---

## Day 12: Review and Checkpoint 1

Re-type Days 4, 5, 8, and 10 without looking at the examples. Then:

**Checkpoint 1 project:** in `CSharpPractice`, build:
- a `User` class with `Username` and `Password`
- an `IUserValidator` interface with `Task<bool> IsValidAsync(User user)`
- a `UserValidator` that returns `false` for an empty username
- DI registration for `IUserValidator`, resolved in `Main`

**Done when:** it builds with no warnings and rejects an empty username.

---

Next: [Part 2: Playwright Basics](02-playwright-basics.md)
