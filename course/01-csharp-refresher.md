# Part 1: C# Refresher (Days 1–15)

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

## Day 7: Arrays

A collection holds several values. In test automation you use them constantly: a list of usernames to test, a dictionary of expected error messages, a set of visible product names. Interviewers ask about these often, so this day goes deeper than the others.

### Part A: Arrays

An array has a **fixed size**. Once created, its length doesn't change. Index starts at `0`.

```csharp
// Declare with a size: all values start at their default (0, null, false)
int[] scores = new int[3];
scores[0] = 90;
scores[1] = 75;
scores[2] = 88;

// Declare with values
string[] users = { "standard_user", "problem_user", "locked_out_user" };

Console.WriteLine(users[0]);        // standard_user
Console.WriteLine(users[^1]);       // locked_out_user (^1 = last item)
Console.WriteLine(users.Length);    // 3
```

**Common array operations:**

```csharp
int[] numbers = { 5, 2, 9, 1 };

Array.Sort(numbers);                       // numbers is now 1, 2, 5, 9
Array.Reverse(numbers);                    // 9, 5, 2, 1
int index = Array.IndexOf(numbers, 5);     // 1
bool has9 = Array.Exists(numbers, n => n == 9);
int[] copy = (int[])numbers.Clone();       // a separate copy
```

**Multidimensional and jagged arrays** (interviews like to ask the difference):

```csharp
// Multidimensional: a fixed rectangle (2 rows, 3 columns)
int[,] grid = new int[2, 3];
grid[1, 2] = 7;

// Jagged: an array of arrays, each row can have a different length
int[][] rows =
{
    new[] { 1, 2 },
    new[] { 3, 4, 5 },
};
Console.WriteLine(rows[1][2]);   // 5
```

**Pitfall:** reading past the end throws an exception.

```csharp
string[] users = { "a", "b" };
Console.WriteLine(users[2]);   // IndexOutOfRangeException
```

**Exercise:** create an array of the four saucedemo usernames (`standard_user`, `locked_out_user`, `problem_user`, `performance_glitch_user`). Print each one using a `for` loop with an index, then print the last one using `^1`.

**Check:** you see four lines, then the last username again.

---

## Day 8: `List<T>` and LINQ

### `List<T>`

A `List<T>` is an array that grows and shrinks. It's the collection you'll use most.

```csharp
var usernames = new List<string> { "standard_user", "problem_user" };

usernames.Add("performance_glitch_user");     // add to end
usernames.Insert(0, "locked_out_user");       // add at index 0
usernames.Remove("problem_user");             // remove first match, returns true/false
usernames.RemoveAt(0);                        // remove by index

Console.WriteLine(usernames.Count);                 // 2 (use Count, not Length)
Console.WriteLine(usernames.Contains("standard_user")); // True
Console.WriteLine(usernames[0]);                    // first item
```

**Filtering and transforming with LINQ** (you'll use these in tests all the time):

```csharp
var all = new List<string> { "standard_user", "locked_out_user", "problem_user" };

var customers = all.Where(u => u.EndsWith("_user")).ToList();
var upper = all.Select(u => u.ToUpper()).ToList();
bool anyLocked = all.Any(u => u.Contains("locked"));
int count = all.Count(u => u.Contains("user"));
string first = all.First(u => u.StartsWith("p"));   // "problem_user"
```

- `Where` filters.
- `Select` transforms each item.
- `Any` checks if at least one item matches.
- `First` returns the first match, and throws if there is none. `FirstOrDefault` returns `null` instead.

**Exercise:** start with a list of six usernames. Use LINQ to print only the ones that contain `"user"`, and only the count of ones that contain `"locked"`.

**Check:** you can explain why `Count` is a property and `Length` is an array property.

---

## Day 9: Dictionary, HashSet, Queue, and Stack

### `Dictionary<TKey, TValue>`

A dictionary maps a unique key to a value. Lookups by key are very fast, so dictionaries are ideal for "find the password for this user."

```csharp
var passwords = new Dictionary<string, string>
{
    ["standard_user"] = "secret_sauce",
    ["problem_user"] = "secret_sauce",
};

passwords["locked_out_user"] = "secret_sauce";    // add or update

// Safe lookup: avoid KeyNotFoundException
if (passwords.TryGetValue("ghost_user", out var pw))
{
    Console.WriteLine(pw);
}
else
{
    Console.WriteLine("No such user");
}

Console.WriteLine(passwords.ContainsKey("standard_user"));   // True

foreach (var (user, password) in passwords)
{
    Console.WriteLine($"{user} -> {password}");
}
```

**Pitfall:** `passwords["ghost_user"]` throws `KeyNotFoundException`. Use `TryGetValue` when the key might be missing.

**Exercise:** write a dictionary of three users to their expected error messages. Use `TryGetValue` to look up a fourth user that doesn't exist, and print `"No such user"`.

**Check:** the program doesn't crash on the missing key.

### `HashSet<T>`, `Queue<T>`, and `Stack<T>`

```csharp
// HashSet: unique values only, fast Contains
var seen = new HashSet<string>();
Console.WriteLine(seen.Add("standard_user"));   // True
Console.WriteLine(seen.Add("standard_user"));   // False: already there

// Queue: first in, first out
var queue = new Queue<string>();
queue.Enqueue("step 1");
queue.Enqueue("step 2");
Console.WriteLine(queue.Dequeue());              // step 1

// Stack: last in, first out
var stack = new Stack<string>();
stack.Push("page A");
stack.Push("page B");
Console.WriteLine(stack.Pop());                  // page B (like browser "back")
```

**Exercise:** use a `Stack<string>` to simulate navigating three pages, then going back twice. Print each page you return to.

**Check:** the order is last-in, first-out.

---

## Day 10: Choosing Collections and Interview Questions

### Choosing a collection and returning one from a method

| Need | Use |
|---|---|
| Fixed size, known at creation | Array |
| Growing ordered list | `List<T>` |
| Look up by key | `Dictionary<TKey, TValue>` |
| Unique items, fast membership check | `HashSet<T>` |
| First in, first out | `Queue<T>` |
| Last in, first out | `Stack<T>` |

When a method returns a collection, return the most general type that works, usually `IEnumerable<T>` or `IReadOnlyList<T>`. This stops callers from changing your internal data.

```csharp
public IReadOnlyList<string> GetUsers() => _users;   // callers can read, not add
```

### Interview questions

Try to answer these in your own words before checking the answers in your notes:

1. **Array vs `List<T>`?** Arrays have a fixed size. `List<T>` grows. Use arrays when the size is known and won't change.
2. **Why is `Dictionary` lookup fast?** It uses a hash of the key to jump straight to the value, roughly O(1), instead of checking each item.
3. **What's the difference between `Count` and `Length`?** `Count` is on collections like `List`. `Length` is on arrays.
4. **What happens if you add a duplicate key to a `Dictionary`?** Using `Add` throws `ArgumentException`. Using the indexer `[key] = value` overwrites.
5. **`HashSet` vs `List` for checking membership?** `HashSet.Contains` is roughly O(1). `List.Contains` scans every item, roughly O(n).
6. **`First` vs `FirstOrDefault`?** `First` throws when there is no match. `FirstOrDefault` returns the default value (usually `null`).

**Checkpoint:** write a small console app that stores users in a `Dictionary<string, string>`, filters them with LINQ, and shows a `Stack` of visited pages. Explain each collection choice in a comment.

**Done when:** it runs and you can answer all six questions without looking.

---

## Day 11: async and await

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

## Day 12: Nullable Reference Types

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

## Day 13: Dependency Injection

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

## Day 14: Exceptions

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

## Day 15: Review and Checkpoint 1

Re-type Days 4, 5, 11, and 13 without looking at the examples. Then:

**Checkpoint 1 project:** in `CSharpPractice`, build:
- a `User` class with `Username` and `Password`
- an `IUserValidator` interface with `Task<bool> IsValidAsync(User user)`
- a `UserValidator` that returns `false` for an empty username
- DI registration for `IUserValidator`, resolved in `Main`

**Done when:** it builds with no warnings and rejects an empty username.

---

Next: [Part 2: Playwright Basics](02-playwright-basics.md)
