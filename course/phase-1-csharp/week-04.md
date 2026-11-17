# Week 4: Object-oriented programming, part 1

This week covers classes and objects, fields and properties, constructors, `this` and `static`, encapsulation and access modifiers, inheritance with `base`, and polymorphism with `virtual`, `override` and `abstract`. Examples use test-automation shapes such as `TestUser` and `LoginPage`-style classes.

Every example is a complete program. In each file the class definitions come after the top-level statements, which is allowed in `Program.cs`. Paste into a console project and run it with `dotnet run`; the output should match the `// Output:` comments.

## Day 19: Classes, objects, fields, properties

So far you have worked with types that already exist: `string`, `int`, `List<T>`. From today you create your own types. A **class** is a blueprint; an **object** (also called an instance) is one thing built from that blueprint. A page object such as `LoginPage`, a `TestUser` holding credentials, a `TestResult` holding an outcome: all are classes.

In every example below, the class definitions sit at the bottom of the file, after the top-level statements. That is allowed in a `Program.cs` that uses top-level statements.

### 1. Your first class: fields and methods

```csharp
var user = new TestUser();          // create an object with 'new'
user.Username = "alice";
user.Password = "secret123";
user.IsAdmin = true;

Console.WriteLine(user.Username);
Console.WriteLine(user.Describe());

var other = new TestUser();         // a second, separate object
other.Username = "bob";
Console.WriteLine(other.Describe());

// Output:
// alice
// alice (admin: True)
// bob (admin: False)

class TestUser
{
    public string Username = "";    // fields: data stored in each object
    public string Password = "";
    public bool IsAdmin;

    public string Describe()        // method: behaviour that works on this object's data
    {
        return $"{Username} (admin: {IsAdmin})";
    }
}
```

- `class TestUser { ... }` defines the blueprint.
- `new TestUser()` builds an object in memory and gives you a reference to it.
- A **field** is a variable that belongs to an object. Each object has its own copy of the fields.
- A **method** inside a class can read and change that object's fields.
- `public` means code outside the class may use the member. (Day 21 explains access modifiers fully.)

Fields that are not assigned get default values: `0` for numbers, `false` for `bool`, `null` for reference types. With nullable reference types enabled, a non-nullable `string` field with no initial value produces a compiler warning, which is why the example writes `= ""`. If a string really may be missing, declare it `string?`.

### 2. Properties: the normal way to expose data

A **property** looks like a field to the caller but is really a pair of methods (a getter and a setter). The simplest form is an **auto-property**: the compiler creates the hidden storage for you.

```csharp
var user = new TestUser
{
    Username = "alice",           // object initializer: set properties right after 'new'
    Role = "Admin"
};

user.Password = "pw";
Console.WriteLine($"{user.Username} / {user.Role} / {user.Password}");

var guest = new TestUser { Username = "guest" };
Console.WriteLine(guest.Role);

// Output:
// alice / Admin / pw
// Viewer

class TestUser
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "Viewer";    // default value for new objects
}
```

Why not just use public fields? Properties can later gain validation, computed values or restricted access without changing the code that uses them. The convention in C# is: fields are private, properties are public, and property names start with a capital letter.

### 3. Different kinds of properties

```csharp
var result = new TestResult
{
    Name = "Login_Valid",
    DurationMs = 1500,
    Passed = true
};

Console.WriteLine(result.Summary);          // computed property
Console.WriteLine(result.DurationSeconds);

// result.Id = 5;                           // compile error: Id is read-only after construction

Console.WriteLine(result.Id);

// Output:
// Login_Valid passed in 1.5s
// 1.5
// 0

class TestResult
{
    public int Id { get; }                           // get-only: can be set only inside the class
    public string Name { get; init; } = "";          // init: set only in an object initializer
    public int DurationMs { get; set; }
    public bool Passed { get; set; }

    public double DurationSeconds => DurationMs / 1000.0;                 // computed, no storage

    public string Summary =>
        $"{Name} {(Passed ? "passed" : "failed")} in {DurationSeconds}s";
}
```

Summary of property forms:

| Form | Meaning |
|---|---|
| `{ get; set; }` | read and write from anywhere allowed by the property's access |
| `{ get; }` | read-only; assigned in the constructor or by an initializer inside the class |
| `{ get; init; }` | can be assigned during object creation (constructor or initializer), then read-only |
| `=> expression` | a computed read-only property, calculated on each read |

### 4. Properties with logic: backing fields and validation

When you need to run code on read or write, write the property out in full with a **backing field**. The setter receives the assigned value in the keyword `value`.

```csharp
var user = new TestUser();
user.Email = "  Alice@Test.COM ";
Console.WriteLine($"[{user.Email}]");

try
{
    user.Email = "";
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
}

user.Age = 30;
Console.WriteLine(user.Age);

// Output:
// [alice@test.com]
// Email cannot be empty
// 30

class TestUser
{
    private string _email = "";               // backing field (private)

    public string Email
    {
        get { return _email; }
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Email cannot be empty");
            }
            _email = value.Trim().ToLowerInvariant();
        }
    }

    public int Age { get; set; }
}
```

(`throw` and exceptions are covered in Week 6. For now read it as "stop with an error".)

### 5. Objects are references

A class is a **reference type**: a variable of a class type holds a reference (like an address) to the object, not the object itself. Copying the variable copies the reference. Both variables then point at the same object.

```csharp
var a = new TestUser { Username = "alice" };
var b = a;                      // b refers to the SAME object
b.Username = "changed";
Console.WriteLine(a.Username);

var c = new TestUser { Username = "changed" };
Console.WriteLine(a == c);      // different objects: compares references, so False
Console.WriteLine(ReferenceEquals(a, b));

TestUser? nobody = null;        // a reference can point at nothing
Console.WriteLine(nobody is null);
Console.WriteLine(nobody?.Username ?? "no user");   // ?. avoids NullReferenceException

Rename(a);                      // the method receives the reference
Console.WriteLine(a.Username);

void Rename(TestUser user)
{
    user.Username = "renamed";
}

// Output:
// changed
// False
// True
// True
// no user
// renamed

class TestUser
{
    public string Username { get; set; } = "";
}
```

Using a member of a `null` reference (`nobody.Username`) throws `NullReferenceException`. It is the most common runtime error in C#. The `?.` (null-conditional) and `??` (null-coalescing) operators handle it safely.

### 6. Lists of objects

```csharp
var users = new List<TestUser>
{
    new TestUser { Username = "alice", IsActive = true },
    new TestUser { Username = "bob", IsActive = false },
    new() { Username = "carol", IsActive = true }       // target-typed new
};

int active = 0;
foreach (TestUser u in users)
{
    if (u.IsActive) active++;
}
Console.WriteLine($"{active} of {users.Count} active");

TestUser? found = users.Find(u => u.Username == "bob");
Console.WriteLine(found?.IsActive);

var byName = new Dictionary<string, TestUser>();
foreach (var u in users) byName[u.Username] = u;
Console.WriteLine(byName["carol"].Username);

// Output:
// 2 of 3 active
// False
// carol

class TestUser
{
    public string Username { get; set; } = "";
    public bool IsActive { get; set; }
}
```

### 7. A page-object flavoured example

Page objects wrap one page of an application: they hold what is needed to find elements and expose actions. Here the "browser" is simulated with a string so you can run it without Playwright. In Phase 2 the same shape holds real locators.

```csharp
var page = new LoginPage();
page.Open();
page.EnterCredentials("alice", "secret");
page.Submit();
Console.WriteLine(page.Status);

page.EnterCredentials("alice", "wrong");
page.Submit();
Console.WriteLine(page.Status);
Console.WriteLine(page.AttemptCount);

// Output:
// Logged in as alice
// Login failed
// 2

class LoginPage
{
    public string Url { get; } = "https://example.com/login";
    public string Status { get; private set; } = "Not opened";
    public int AttemptCount { get; private set; }

    private string _username = "";
    private string _password = "";

    public void Open()
    {
        Status = "Opened " + Url;
    }

    public void EnterCredentials(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public void Submit()
    {
        AttemptCount++;
        Status = _password == "secret" ? $"Logged in as {_username}" : "Login failed";
    }
}
```

Notice `{ get; private set; }`: anyone can read `Status`, but only the class itself can change it. Day 21 builds on that idea.

### Common mistakes

- Forgetting `new`: `TestUser user; user.Username = "x";` does not compile (the variable has no object), and `TestUser? user = null; user.Username` throws `NullReferenceException`.
- Writing `b = a` and expecting an independent copy of the object.
- Making fields `public` instead of using properties.
- Forgetting that a computed property (`=>`) is recalculated on every read, and that it has no setter.

### Why this matters for test automation

Your whole framework is classes. `LoginPage`, `CheckoutPage`, `TestUser`, `TestSettings` and `ApiClient` are all classes made of properties and methods. Test data classes with properties are what you will load from JSON files in Week 7. Reference semantics explain surprising bugs when two tests unknowingly share one `TestUser` object and one of them changes it.

### Interview questions

**Q: What is the difference between a class and an object?**
A: A class is the definition (blueprint). An object is an instance created from it with `new`.

**Q: What is the difference between a field and a property?**
A: A field is a variable stored in the object. A property is a get/set accessor pair that can enforce rules and control access; it is usually backed by a private field. Fields are normally private and properties public.

**Q: What does `{ get; init; }` mean?**
A: The property can be set only during object creation (constructor or object initializer) and is read-only afterwards.

**Q: Is a class a value type or a reference type? What follows from that?**
A: A reference type. Assigning one variable to another copies the reference, so both refer to the same object, and a class variable can be `null`.

**Q: What is an auto-property?**
A: A property whose backing field is generated by the compiler: `public string Name { get; set; }`.

**Exercise**

1. Create a class `Product` with properties `Name` (string), `Price` (decimal) and `Quantity` (int), plus a computed read-only property `Total` (`Price * Quantity`). Create three products in a `List<Product>` and print each name with its total.
2. Give `Product` a setter validation: `Price` must not be negative (throw `ArgumentException` otherwise). Test it with a valid and an invalid value.
3. Make a class `Counter` with `public int Value { get; private set; }` and methods `Increment()` and `Reset()`. Show that `Value` can be read but not assigned from outside (write the failing line as a comment).
4. Create two variables pointing at the same `Product`, change one through the other and explain the output.

**Check**

- What is printed by reading an unassigned `int` field? An unassigned `string?` field?
- Which property form lets you assign a value in an object initializer but nowhere else afterwards?
- Why does `a == c` return `false` for two different `TestUser` objects with identical data?

## Day 20: Constructors, `this`, `static`

Yesterday you created objects with `new TestUser()` and then set properties one by one. That leaves a window where the object exists but is half-filled. A **constructor** is a special method that runs when the object is created and makes sure it starts in a valid state.

### 1. Your first constructor

```csharp
var user = new TestUser("alice", "secret123");
Console.WriteLine(user.Username);
Console.WriteLine(user.Password);
Console.WriteLine(user.Role);

// var broken = new TestUser();   // compile error: no constructor takes zero arguments

// Output:
// alice
// secret123
// Viewer

class TestUser
{
    public string Username { get; }
    public string Password { get; }
    public string Role { get; set; } = "Viewer";

    public TestUser(string username, string password)   // same name as the class, no return type
    {
        Username = username;
        Password = password;
    }
}
```

Rules:

- A constructor has the same name as the class and no return type.
- It runs once, when `new` is used.
- If you write no constructor at all, the compiler provides an invisible parameterless one. As soon as you write any constructor, that free one is gone.
- A get-only property can be assigned inside a constructor.

### 2. Several constructors (overloading)

A class may have several constructors with different parameter lists.

```csharp
var a = new TestUser();
var b = new TestUser("bob");
var c = new TestUser("carol", "Admin");

Console.WriteLine(a.Describe());
Console.WriteLine(b.Describe());
Console.WriteLine(c.Describe());

// Output:
// anonymous (Viewer)
// bob (Viewer)
// carol (Admin)

class TestUser
{
    public string Username { get; }
    public string Role { get; }

    public TestUser()
    {
        Username = "anonymous";
        Role = "Viewer";
    }

    public TestUser(string username)
    {
        Username = username;
        Role = "Viewer";
    }

    public TestUser(string username, string role)
    {
        Username = username;
        Role = role;
    }

    public string Describe() => $"{Username} ({Role})";
}
```

The repeated assignments are a smell. The next section removes them.

### 3. `this`: the current object

Inside an instance member, `this` is a reference to the object the code is running on.

**Use 1: disambiguate** a parameter from a field or property with the same name.

```csharp
var page = new LoginPage("https://example.com/login", 5000);
Console.WriteLine(page.Url);
Console.WriteLine(page.TimeoutMs);

// Output:
// https://example.com/login
// 5000

class LoginPage
{
    private readonly string url;
    private readonly int timeoutMs;

    public string Url => url;
    public int TimeoutMs => timeoutMs;

    public LoginPage(string url, int timeoutMs)
    {
        this.url = url;            // left: the field; right: the parameter
        this.timeoutMs = timeoutMs;
    }
}
```

Many teams avoid the need for `this.` by prefixing private fields with an underscore (`_url`); both styles are common.

**Use 2: constructor chaining** with `this(...)`. One constructor calls another, so the real work is written once.

```csharp
var a = new TestUser();
var b = new TestUser("bob");
var c = new TestUser("carol", "Admin");

Console.WriteLine(a.Describe());
Console.WriteLine(b.Describe());
Console.WriteLine(c.Describe());

// Output:
// anonymous (Viewer)
// bob (Viewer)
// carol (Admin)

class TestUser
{
    public string Username { get; }
    public string Role { get; }

    public TestUser() : this("anonymous")
    {
    }

    public TestUser(string username) : this(username, "Viewer")
    {
    }

    public TestUser(string username, string role)   // the one "main" constructor
    {
        Username = username;
        Role = role;
    }

    public string Describe() => $"{Username} ({Role})";
}
```

The `: this(...)` part runs first, then the body of the current constructor.

**Use 3: pass the current object** to another method or return it for chaining (a "fluent" style).

```csharp
var user = new TestUser("alice")
    .WithRole("Admin")
    .WithEmail("alice@test.com");

Console.WriteLine(user.Username + " " + user.Role + " " + user.Email);

// Output:
// alice Admin alice@test.com

class TestUser
{
    public string Username { get; }
    public string Role { get; private set; } = "Viewer";
    public string Email { get; private set; } = "";

    public TestUser(string username)
    {
        Username = username;
    }

    public TestUser WithRole(string role)
    {
        Role = role;
        return this;               // return the same object so calls can chain
    }

    public TestUser WithEmail(string email)
    {
        Email = email;
        return this;
    }
}
```

This fluent style is common in test data builders and in Playwright page objects.

### 4. Constructor, object initializer, or `required`?

```csharp
// 1. Constructor: callers MUST provide the arguments
var a = new TestUser("alice");

// 2. Object initializer: callers set properties by name
var b = new Settings { BaseUrl = "https://example.com", Retries = 3 };

// 3. required: callers MUST set the property in an initializer
var c = new Account { Id = 7 };
// var d = new Account();         // compile error: required member 'Id' must be set

Console.WriteLine($"{a.Username} {b.BaseUrl} {b.Retries} {c.Id}");

// Output:
// alice https://example.com 3 7

class TestUser
{
    public string Username { get; }
    public TestUser(string username) => Username = username;
}

class Settings
{
    public string BaseUrl { get; set; } = "";
    public int Retries { get; set; } = 1;
}

class Account
{
    public required int Id { get; init; }
}
```

Use a constructor for values the object cannot work without. Use initializers for optional settings.

### 5. Primary constructors (C# 12)

A class can declare its constructor parameters right after its name. The parameters are available throughout the class body.

```csharp
var page = new LoginPage("https://example.com/login");
Console.WriteLine(page.Describe());

// Output:
// Page at https://example.com/login

class LoginPage(string url)
{
    public string Url { get; } = url;        // copy the parameter into a property

    public string Describe() => $"Page at {Url}";
}
```

A primary-constructor parameter is not a property by itself. If you want one, as above, assign it to a property. You will see both styles in real code; the classic constructor is always fine.

### 6. `static`: members that belong to the class

A normal member belongs to each object. A `static` member belongs to the class itself: one copy shared by everyone, and you reach it through the class name, not through an object.

```csharp
Console.WriteLine(TestUser.Count);         // 0 objects so far

var a = new TestUser("alice");
var b = new TestUser("bob");
Console.WriteLine(TestUser.Count);         // accessed through the class name
Console.WriteLine(a.Username);             // instance member: through an object

Console.WriteLine(TestUser.IsValidName("al"));
Console.WriteLine(TestUser.IsValidName("alice"));

// Output:
// 0
// 2
// alice
// False
// True

class TestUser
{
    public static int Count { get; private set; }     // shared by all TestUser objects
    public string Username { get; }

    public TestUser(string username)
    {
        Username = username;
        Count++;                                       // every new object increases the shared counter
    }

    public static bool IsValidName(string name)       // static method: no 'this' available
    {
        return name.Length >= 3;
    }
}
```

A static method cannot use instance members (no `this`), because it is not running on any particular object.

You have already used static members: `Console.WriteLine`, `string.IsNullOrEmpty`, `Array.Sort`, `Math.Max`.

### 7. Static classes, `const`, and `static readonly`

A `static class` cannot be instantiated and can contain only static members. It suits helper methods and configuration.

```csharp
Console.WriteLine(Config.BaseUrl);
Console.WriteLine(Config.DefaultTimeoutMs);
Console.WriteLine(Config.MaxRetries);
Console.WriteLine(TextHelper.Truncate("playwright automation", 10));
Console.WriteLine(Config.StartedAt <= DateTime.UtcNow);

// Config.MaxRetries = 5;     // compile error: const cannot change

// Output:
// https://example.com
// 30000
// 3
// playwrigh...
// True

static class Config
{
    public const int MaxRetries = 3;                          // compile-time constant
    public const string BaseUrl = "https://example.com";
    public static readonly int DefaultTimeoutMs = 30 * 1000;  // set once; may be computed at run time
    public static readonly DateTime StartedAt = DateTime.UtcNow;
}

static class TextHelper
{
    public static string Truncate(string text, int max)
    {
        return text.Length <= max ? text : text.Substring(0, max - 1) + "...";
    }
}
```

- `const`: value must be known at compile time (numbers, strings, `bool`, etc.). It is implicitly static.
- `static readonly`: assigned once, either at the declaration or in a static constructor; the value can be computed at run time.
- `readonly` (non-static) field: assigned once per object, in the declaration or in a constructor.

```csharp
var r = new TestRun("smoke");
Console.WriteLine(r.Name);
Console.WriteLine(r.StartedAt <= DateTime.UtcNow);
// r.Name = "other";         // compile error

// Output:
// smoke
// True

class TestRun
{
    public readonly string Name;
    public readonly DateTime StartedAt = DateTime.UtcNow;

    public TestRun(string name)
    {
        Name = name;           // allowed: inside the constructor
    }
}
```

### 8. Static constructor and initialization order

A static constructor runs once, automatically, before the class is first used. It has no access modifier and no parameters.

```csharp
Console.WriteLine("Program starts");
Console.WriteLine(Registry.Count);
Console.WriteLine(Registry.Count);

// Output:
// Program starts
// Registry static constructor
// 3
// 3

static class Registry
{
    private static readonly List<string> _names;

    static Registry()
    {
        Console.WriteLine("Registry static constructor");
        _names = new List<string> { "a", "b", "c" };
    }

    public static int Count => _names.Count;
}
```

Use it sparingly; most of the time a `static readonly` field initializer is enough.

### Common mistakes

- Writing a constructor with arguments and then calling `new TestUser()`. The free parameterless constructor no longer exists.
- Using a `static` field for data that should differ per object, or the reverse.
- Trying to use instance members from a `static` method.
- Using `static` fields for shared mutable test state (counters, current user). When tests run in parallel, they will interfere with each other.

### Why this matters for test automation

Page objects take the `IPage` in their constructor so they cannot be created without a browser page. Test data builders use `this` chaining. Static classes hold constants such as base URLs, timeouts and locators. xUnit v3 itself creates a new instance of your test class for every test, calling its constructor each time, so what you write in a constructor is per-test setup (Phase 3). Shared `static` mutable state is a common source of flaky parallel tests.

### Interview questions

**Q: What is a constructor, and when does the compiler provide one?**
A: A special method that runs when an object is created. If a class declares no constructors, the compiler provides a public parameterless one; once you declare any, it does not.

**Q: What does `this` refer to?**
A: The current object instance. It is used to disambiguate names, to call another constructor (`: this(...)`) and to pass or return the current object.

**Q: What is the difference between a static and an instance member?**
A: An instance member belongs to each object. A static member belongs to the type, exists once and is accessed through the type name.

**Q: `const` versus `static readonly`?**
A: `const` is substituted at compile time and limited to simple types. `static readonly` is assigned at run time (once) and can hold any type.

**Q: Can a static method access instance fields?**
A: Not directly, because there is no current object. It would need an object reference passed in.

**Exercise**

1. Create `TestUser` with get-only `Username`, `Email` and `Role`. Write a main constructor taking all three, and two chained overloads: `(username)`, which uses the email `username + "@test.com"` and the role `"Viewer"`, and `(username, role)`, which uses the same generated email.
2. Add a static property `Created` counting how many `TestUser` objects have been constructed. Create four users and print the count.
3. Write a static class `Urls` with a `const string Base`, and a static method `Build(string path)` that returns `Base + "/" + path.TrimStart('/')`. Test with `"login"` and `"/cart"`.
4. Create a class `Browser` with `readonly string Name`, set in the constructor, and show the compile error you get from trying to change it later (as a comment).

**Check**

- What does `: this(username, "Viewer")` do, and in what order do the two constructor bodies run?
- Why can't a `static` method read `Username`?
- Name one reason shared static state is risky for parallel tests.

## Day 21: Encapsulation and access modifiers

**Encapsulation** means bundling data with the code that works on it, and hiding the internal details so that outside code can only use the object through a safe, small set of members. The object is responsible for keeping itself valid. Access modifiers are the tool C# gives you for this.

### 1. The problem: anyone can break a public field

```csharp
var cart = new BadCart();
cart.Items.Add("book");
cart.Total = -500;                 // nothing stops nonsense values
cart.Items = null!;                // or removes the list entirely

Console.WriteLine(cart.Total);

// Output:
// -500

class BadCart
{
    public List<string> Items = new();
    public decimal Total;
}
```

If every piece of code can change anything, you can never be sure the object is in a valid state, and a bug can come from anywhere.

### 2. Access modifiers

| Modifier | Who can use the member |
|---|---|
| `public` | any code |
| `private` | only code inside the same class (the default for class members) |
| `protected` | the class and classes that inherit from it (Day 22) |
| `internal` | any code in the same project (assembly) (the default for top-level types) |
| `protected internal` | same project, or inheriting classes anywhere |
| `private protected` | inheriting classes, but only within the same project |

Start with the most restrictive option (`private`) and open up only what callers truly need. A `public` member is a promise: other code will depend on it.

```csharp
var counter = new Counter();
counter.Increment();
counter.Increment();
Console.WriteLine(counter.Value);

// Console.WriteLine(counter._value);   // compile error: '_value' is inaccessible due to its protection level
// counter.Value = 10;                  // compile error: the set accessor is private

// Output:
// 2

class Counter
{
    private int _value;                       // hidden storage

    public int Value => _value;               // read access for everyone

    public void Increment()                   // the only way to change it
    {
        _value++;
    }
}
```

### 3. A properly encapsulated class

```csharp
var cart = new ShoppingCart();
cart.AddItem("book", 12.50m);
cart.AddItem("pen", 1.25m);

Console.WriteLine(cart.Count);
Console.WriteLine(cart.Total);

try
{
    cart.AddItem("free gift", -1m);
}
catch (ArgumentException ex)
{
    Console.WriteLine(ex.Message);
}

Console.WriteLine(cart.Total);

// cart.Total = 0;               // compile error: set accessor is private

// Output:
// 2
// 13.75
// Price cannot be negative
// 13.75

class ShoppingCart
{
    private readonly List<(string Name, decimal Price)> _items = new();

    public int Count => _items.Count;

    public decimal Total { get; private set; }

    public void AddItem(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required");
        if (price < 0)
            throw new ArgumentException("Price cannot be negative");

        _items.Add((name, price));
        Total += price;               // the class keeps Total consistent with the items
    }
}
```

What encapsulation gives you here:

- Validation in one place: `AddItem` rejects bad input.
- Consistency: `Total` can never disagree with the items, because only `AddItem` changes it.
- Freedom to change the inside: you could later compute `Total` differently without breaking callers.

### 4. Do not leak internal collections

Returning your private `List<T>` hands callers the keys to your internals.

```csharp
var team = new Team();
team.Add("alice");
team.Add("bob");

// A caller can cast back and mutate the leaked list
if (team.LeakyMembers is List<string> leaked)
{
    leaked.Clear();
}
Console.WriteLine(team.Count);        // the team was emptied from outside

var team2 = new Team();
team2.Add("alice");
Console.WriteLine(team2.Members is List<string>);   // a read-only wrapper is not a List<string>
Console.WriteLine(team2.Members.Count);

// team2.Members.Add("x");            // compile error: no Add on IReadOnlyList<string>

// Output:
// 0
// False
// 1

class Team
{
    private readonly List<string> _members = new();

    public IReadOnlyList<string> LeakyMembers => _members;               // returns the real list
    public IReadOnlyList<string> Members => _members.AsReadOnly();      // returns a read-only wrapper

    public int Count => _members.Count;

    public void Add(string name) => _members.Add(name);
}
```

Exposing the collection as `IReadOnlyList<T>` stops accidental calls to `Add`; wrapping with `AsReadOnly()` also stops deliberate casts. The same applies to arrays: returning a private array lets callers change its elements, so return a copy (`ToArray()`) when that matters.

```csharp
var config = new Config();
string[] browsers = config.GetBrowsers();
browsers[0] = "hacked";
Console.WriteLine(config.GetBrowsers()[0]);

// Output:
// chromium

class Config
{
    private readonly string[] _browsers = { "chromium", "firefox" };

    public string[] GetBrowsers() => (string[])_browsers.Clone();   // return a copy
}
```

### 5. Private helper methods

Not only data: methods that are implementation details should be `private` too.

```csharp
var page = new LoginPage();
Console.WriteLine(page.Login(" Alice ", "secret"));
Console.WriteLine(page.Login("", "secret"));

// Output:
// OK: alice
// Invalid input

class LoginPage
{
    public string Login(string username, string password)
    {
        string name = Normalize(username);
        if (!IsValid(name, password))
        {
            return "Invalid input";
        }
        return "OK: " + name;
    }

    private static string Normalize(string input) => input.Trim().ToLowerInvariant();

    private static bool IsValid(string name, string password) =>
        name.Length > 0 && password.Length > 0;
}
```

Callers see one public method, `Login`. How the work is split inside is free to change.

### 6. Controlled state changes

An object that can be in different states should only let you move between them through methods.

```csharp
var run = new TestRun("smoke");
Console.WriteLine(run.Status);

run.Start();
run.Complete(passed: true);
Console.WriteLine(run.Status);

try
{
    run.Start();                     // cannot start a finished run
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);
}

// Output:
// NotStarted
// Passed
// Cannot start a run in state Passed

class TestRun
{
    public string Name { get; }
    public string Status { get; private set; } = "NotStarted";

    public TestRun(string name) => Name = name;

    public void Start()
    {
        if (Status != "NotStarted")
            throw new InvalidOperationException($"Cannot start a run in state {Status}");
        Status = "Running";
    }

    public void Complete(bool passed)
    {
        if (Status != "Running")
            throw new InvalidOperationException($"Cannot complete a run in state {Status}");
        Status = passed ? "Passed" : "Failed";
    }
}
```

(In real code an `enum` is better than a string for the status; you will learn enums on Day 27.)

### 7. `internal` and where code lives

`internal` means "visible inside this project only". A solution usually has several projects (for example a test project and a framework library). Marking helpers `internal` keeps them out of the public surface of a library. In a single-project console app `internal` and `public` behave the same in practice, which is why the examples today concentrate on `public` versus `private`.

A top-level class with no modifier is `internal`. A member with no modifier is `private`.

```csharp
var o = new Outer();
Console.WriteLine(o.Show());

// Output:
// 42

class Outer                      // internal by default
{
    int _hidden = 42;            // private by default

    public int Show() => _hidden;
}
```

### Common mistakes

- Making everything `public` "to be safe". Start private and widen on demand.
- Exposing a mutable `List<T>` or array through a public property, so outside code can change your internal state.
- Using a `public` setter on a property that should only change through a method.
- Thinking `private` means secret from other objects of the same class: `private` is per class, not per object, so one `Counter` can read another `Counter`'s private field.

### Why this matters for test automation

Page objects are encapsulation in practice. Tests call `loginPage.LoginAs(user)`; they do not know which locators or how many clicks it takes. If the page changes, you fix one private locator, not 200 tests. Keep locators `private` and expose intent-revealing public actions. Test data classes use `init` or get-only properties so a test cannot accidentally alter shared data used by another test.

### Interview questions

**Q: What is encapsulation?**
A: Hiding an object's internal state and exposing a controlled interface, so the object keeps itself in a valid state.

**Q: What is the default access of class members and of top-level classes?**
A: Members default to `private`; top-level types default to `internal`.

**Q: Difference between `private`, `protected` and `internal`?**
A: `private`: the declaring class only. `protected`: the class and its subclasses. `internal`: any code in the same assembly.

**Q: Why use a property with a `private set` instead of a public field?**
A: The value stays readable to everyone while only the class can change it, and the property can later add logic without breaking callers.

**Q: How do you stop callers modifying a private collection exposed by a property?**
A: Expose `IReadOnlyList<T>` (ideally via `AsReadOnly()`), or return a copy.

**Exercise**

1. Write a class `BankAccount` with a private balance, a read-only `Balance` property, `Deposit(decimal)` and `Withdraw(decimal)`. Reject non-positive amounts and overdrafts with `ArgumentException` or `InvalidOperationException`. Test valid and invalid calls.
2. Create `Playlist` holding a private `List<string>`. Expose `IReadOnlyList<string> Songs` via `AsReadOnly()`, an `Add` method that ignores duplicates and a `Remove` method.
3. Write `LoginPage` with private string fields for three fake locators (`"#user"`, `"#pass"`, `"#submit"`) and one public method `Describe()` that prints how the page would log in. Explain which members tests should be able to see.
4. Add the access-modifier errors you would expect, as comments, for three ways of misusing your `BankAccount` from outside.

**Check**

- If a member has no access modifier, what is it?
- Why is `public List<string> Items` a risk compared with `IReadOnlyList<string>`?
- Which keyword lets a property be read by everyone but changed only by its own class?

## Day 22: Inheritance and `base`

**Inheritance** lets one class (the **derived** or child class) reuse and extend another (the **base** or parent class). The relationship should read as "is a": a `LoginPage` is a `BasePage`; an `AdminUser` is a `TestUser`. The derived class gets the base class's accessible members for free and adds or changes its own.

### 1. The first inheritance example

```csharp
var user = new TestUser("alice");
Console.WriteLine(user.Describe());

var admin = new AdminUser("root", "all-access");
Console.WriteLine(admin.Describe());      // inherited from TestUser
Console.WriteLine(admin.Username);        // inherited property
Console.WriteLine(admin.Permissions);     // its own property
Console.WriteLine(admin.IsActive);

// Output:
// User alice
// User root
// root
// all-access
// True

class TestUser
{
    public string Username { get; }
    public bool IsActive { get; set; } = true;

    public TestUser(string username)
    {
        Username = username;
    }

    public string Describe() => $"User {Username}";
}

class AdminUser : TestUser          // ": TestUser" means "inherits from TestUser"
{
    public string Permissions { get; }

    public AdminUser(string username, string permissions) : base(username)   // call the base constructor
    {
        Permissions = permissions;
    }
}
```

Three things to see:

1. `class AdminUser : TestUser` makes `AdminUser` inherit `Username`, `IsActive` and `Describe()`.
2. `: base(username)` calls the base class constructor. A derived constructor must make sure a base constructor runs, because the base part of the object has to be initialised too.
3. If the base class has a parameterless constructor, `: base()` is implicit and may be left out. Here `TestUser` has none, so you must write `: base(username)`.

### 2. Constructor order

When you create a derived object, the base constructor runs first, then the derived one.

```csharp
Console.WriteLine("Creating LoginPage");
var page = new LoginPage();
Console.WriteLine("Done");

// Output:
// Creating LoginPage
// BasePage constructor
// LoginPage constructor
// Done

class BasePage
{
    public BasePage()
    {
        Console.WriteLine("BasePage constructor");
    }
}

class LoginPage : BasePage
{
    public LoginPage()
    {
        Console.WriteLine("LoginPage constructor");
    }
}
```

### 3. `base.` to reach the parent's members

Inside a derived class, `base.Member` refers to the parent's version of a member. Day 23 shows how it is used when you override; here it is used to call a parent method that you extend.

```csharp
var page = new LoginPage("https://example.com");
page.Open();
Console.WriteLine(page.Title);

// Output:
// Opening https://example.com/login
// Login

class BasePage
{
    public string BaseUrl { get; }
    public string Title { get; protected set; } = "";

    public BasePage(string baseUrl)
    {
        BaseUrl = baseUrl;
    }

    public void NavigateTo(string path)
    {
        Console.WriteLine($"Opening {BaseUrl}{path}");
    }
}

class LoginPage : BasePage
{
    public LoginPage(string baseUrl) : base(baseUrl)
    {
    }

    public void Open()
    {
        base.NavigateTo("/login");     // 'base.' is optional here; shown for clarity
        Title = "Login";               // protected setter is usable in a derived class
    }
}
```

### 4. `protected`: visible to the family

`protected` members are visible inside the class and in classes that derive from it, but not to outside code.

```csharp
var page = new DashboardPage();
page.Load();
Console.WriteLine(page.LoadCount);

// page.Log("x");            // compile error: Log is protected
// page.LoadCount = 5;       // compile error: private set

// Output:
// [Dashboard] loading
// 1

class BasePage
{
    public int LoadCount { get; private set; }

    protected void Log(string message)
    {
        Console.WriteLine($"[{GetType().Name.Replace("Page", "")}] {message}");
    }

    protected void MarkLoaded()
    {
        LoadCount++;
    }
}

class DashboardPage : BasePage
{
    public void Load()
    {
        Log("loading");           // inherited protected method
        MarkLoaded();
    }
}
```

(`GetType().Name` returns the actual class name at run time, here `"DashboardPage"`.)

Keep protected members to a minimum. Every protected member is part of the contract with all future subclasses.

### 5. Everything inherits from `object`

Every class, even one with no `: Something`, ultimately derives from `System.Object`. That is where `ToString()`, `Equals()`, `GetHashCode()` and `GetType()` come from.

```csharp
var user = new TestUser { Username = "alice" };
Console.WriteLine(user.ToString());           // default: the type name
Console.WriteLine(user.GetType().Name);
Console.WriteLine(user is object);

// Output:
// TestUser
// TestUser
// True

class TestUser
{
    public string Username { get; set; } = "";
}
```

### 6. Upcasting, type tests and downcasting

A variable of the base type can hold a derived object. Going from derived to base is always safe (an **upcast**). Going back needs a check.

```csharp
TestUser user = new AdminUser("root");           // upcast: AdminUser is a TestUser

Console.WriteLine(user.Username);
// Console.WriteLine(user.Permissions);          // compile error: TestUser has no Permissions

if (user is AdminUser admin)                     // safe downcast with a type test
{
    Console.WriteLine(admin.Permissions);
}

AdminUser? maybe = user as AdminUser;            // 'as' gives null if the cast fails
Console.WriteLine(maybe is not null);

TestUser plain = new TestUser("bob");
Console.WriteLine(plain is AdminUser);
Console.WriteLine(plain as AdminUser is null);

List<TestUser> everyone = new() { new TestUser("a"), new AdminUser("b"), new TestUser("c") };
int admins = 0;
foreach (TestUser u in everyone)
{
    if (u is AdminUser) admins++;
}
Console.WriteLine(admins);

// Output:
// root
// all-access
// True
// False
// True
// 1

class TestUser
{
    public string Username { get; }
    public TestUser(string username) => Username = username;
}

class AdminUser : TestUser
{
    public string Permissions { get; } = "all-access";
    public AdminUser(string username) : base(username) { }
}
```

A plain cast `(AdminUser)user` throws `InvalidCastException` if the object is not really an `AdminUser`. Prefer `is` or `as`.

### 7. Building a hierarchy of pages

```csharp
var pages = new List<BasePage>
{
    new LoginPage(),
    new SearchPage(),
};

foreach (BasePage p in pages)
{
    Console.WriteLine($"{p.Name}: {p.Url}");
}

// Output:
// Login: /login
// Search: /search

class BasePage
{
    public string Name { get; }
    public string Url { get; }

    public BasePage(string name, string url)
    {
        Name = name;
        Url = url;
    }
}

class LoginPage : BasePage
{
    public LoginPage() : base("Login", "/login") { }
}

class SearchPage : BasePage
{
    public SearchPage() : base("Search", "/search") { }
}
```

Both derived classes reuse the base fields and constructor logic and add their own specifics.

### 8. `sealed`, single inheritance, and composition

- A class can inherit from only one base class (single inheritance). It may implement many interfaces (Day 25).
- `sealed class X` cannot be inherited from. Use it when a class is not designed for extension. Several .NET classes, `string` among them, are sealed.
- Inheritance creates tight coupling: a change in the base class affects every derived class. When the relationship is "has a" rather than "is a", prefer **composition**: hold the other object in a field.

```csharp
var page = new CheckoutPage();
page.PayWithCard("4111");

// Output:
// Logging: paying
// Paid with card 4111

class Logger
{
    public void Info(string message) => Console.WriteLine($"Logging: {message}");
}

// A page HAS a logger. It is not a kind of logger, so it does not inherit from one.
class CheckoutPage
{
    private readonly Logger _logger = new();

    public void PayWithCard(string number)
    {
        _logger.Info("paying");
        Console.WriteLine($"Paid with card {number}");
    }
}

sealed class FinalPage { }          // no class can derive from FinalPage
```

### Common mistakes

- Forgetting the base constructor call when the base class has no parameterless constructor (compile error: no argument given that corresponds to the required parameter).
- Using inheritance only to reuse code when the "is a" relationship does not hold. `LoginPage : Logger` is wrong; give the page a `Logger`.
- Deep hierarchies (five or six levels) that are hard to follow. Keep them shallow.
- Casting with `(Derived)obj` without checking the type first.

### Why this matters for test automation

A `BasePage` class with common behaviour (navigation, waiting for load, shared locators like a header) is the standard starting point of a page-object framework. Every page inherits from it. Test classes can also inherit from a base test class with shared setup. Be aware that xUnit prefers composition and fixtures for sharing setup, which you will study in Phase 3; inheritance is still useful for a small shared base.

### Interview questions

**Q: What is inheritance and what does `:` mean in `class B : A`?**
A: A mechanism to reuse and extend a class. `B` derives from `A`, getting its accessible members.

**Q: In what order do constructors run in an inheritance chain?**
A: The base class constructor runs first, then the derived one.

**Q: What does `base(...)` do in a constructor, and what does `base.Method()` do?**
A: `base(...)` selects which base constructor to run. `base.Method()` calls the base class's version of a method from within the derived class.

**Q: Can a C# class inherit from multiple classes?**
A: No, only one class; it can implement many interfaces.

**Q: What is the difference between `is`, `as` and a direct cast?**
A: `is` tests the type (and can declare a variable). `as` returns `null` on failure. A direct cast throws `InvalidCastException` on failure.

**Q: What does `sealed` do?**
A: It prevents a class from being inherited (or, on an override, from being overridden further).

**Exercise**

1. Create a base class `Vehicle` with `Make`, a constructor taking it, and a method `Describe()`. Create `Car : Vehicle` with an extra `Doors` property and `Motorcycle : Vehicle`. Print the description of one of each through a `List<Vehicle>`.
2. Add constructor `Console.WriteLine` calls to all three classes and predict the printed order before running.
3. Create `BasePage` (with `protected void Log(string)`) and `ProfilePage : BasePage`; call `Log` from a `ProfilePage` method and show (as a comment) that the same call from outside is a compile error.
4. Given a `List<TestUser>` containing `TestUser` and `AdminUser` objects, print the names of only the admins using `is`.

**Check**

- Which constructor runs first when `new AdminUser(...)` is called?
- What happens if you write `class Sub : Base` and `Base` has only a constructor with parameters?
- Why would you choose composition over inheritance for `CheckoutPage` and `Logger`?

## Day 23: Polymorphism: `virtual`, `override`, `abstract`

**Polymorphism** means "many forms": code written against a base type works with any derived type, and each derived type can respond in its own way. You call `step.Execute()` on a list of different steps; each one does its own thing.

### 1. The idea without polymorphism: why it is needed

```csharp
var users = new List<TestUser> { new TestUser("alice"), new AdminUser("root") };

foreach (TestUser u in users)
{
    Console.WriteLine(u.Greet());       // Greet is not virtual: the base version always runs
}

// Output:
// Hello, alice
// Hello, root

class TestUser
{
    public string Name { get; }
    public TestUser(string name) => Name = name;

    public string Greet() => $"Hello, {Name}";
}

class AdminUser : TestUser
{
    public AdminUser(string name) : base(name) { }

    public new string Greet() => $"Welcome back, administrator {Name}";   // hides the base method
}
```

`AdminUser.Greet` only hides the inherited method (the `new` keyword, explained in section 5, tells the compiler this is intended). Because the variable's type is `TestUser`, the base version runs. We want the admin's own greeting even though the variable type is `TestUser`. That is what `virtual` and `override` do.

### 2. `virtual` and `override`

- Mark the base method `virtual`: "derived classes may replace this".
- Mark the derived method `override`: "this replaces the base version".
- Which version runs is decided at run time by the actual type of the object, not the type of the variable.

```csharp
var users = new List<TestUser> { new TestUser("alice"), new AdminUser("root"), new GuestUser() };

foreach (TestUser u in users)
{
    Console.WriteLine(u.Greet());       // the actual object decides which Greet runs
}

// Output:
// Hello, alice
// Welcome back, administrator root
// Hello, guest

class TestUser
{
    public string Name { get; }
    public TestUser(string name) => Name = name;

    public virtual string Greet() => $"Hello, {Name}";
}

class AdminUser : TestUser
{
    public AdminUser(string name) : base(name) { }

    public override string Greet() => $"Welcome back, administrator {Name}";
}

class GuestUser : TestUser
{
    public GuestUser() : base("guest") { }
    // does not override Greet: it inherits the base version
}
```

An override can still use the base version through `base.`:

```csharp
Console.WriteLine(new AdminUser("root").Greet());

// Output:
// Hello, root (admin)

class TestUser
{
    public string Name { get; }
    public TestUser(string name) => Name = name;

    public virtual string Greet() => $"Hello, {Name}";
}

class AdminUser : TestUser
{
    public AdminUser(string name) : base(name) { }

    public override string Greet() => base.Greet() + " (admin)";
}
```

### 3. `abstract` classes and members

An **abstract class** is incomplete on purpose. You cannot create an instance of it with `new`; it exists to be inherited. It may contain **abstract members**: declarations with no body that every concrete derived class must implement.

```csharp
var steps = new List<TestStep>
{
    new NavigateStep("/login"),
    new ClickStep("#submit"),
};

// var s = new TestStep("x");        // compile error: cannot create an instance of an abstract class

foreach (TestStep step in steps)
{
    step.Run();
}

// Output:
// [Navigate] go to /login
// [Click] click #submit

abstract class TestStep
{
    public string Name { get; }

    protected TestStep(string name)
    {
        Name = name;
    }

    protected abstract string Describe();         // no body: derived classes must provide it

    public void Run()                             // shared, non-abstract behaviour
    {
        Console.WriteLine($"[{Name}] {Describe()}");
    }
}

class NavigateStep : TestStep
{
    private readonly string _path;
    public NavigateStep(string path) : base("Navigate") => _path = path;

    protected override string Describe() => $"go to {_path}";
}

class ClickStep : TestStep
{
    private readonly string _selector;
    public ClickStep(string selector) : base("Click") => _selector = selector;

    protected override string Describe() => $"click {_selector}";
}
```

This is the **template method** idea: the base class fixes the overall flow (`Run`) and the derived classes fill in the steps (`Describe`).

Rules:

- An abstract member can only live in an abstract class and cannot be `private`.
- A derived class that is not itself abstract must override every abstract member, or it will not compile.
- An abstract class can also have normal fields, properties, constructors and fully implemented methods.

### 4. Classic example: shapes

```csharp
var shapes = new List<Shape>
{
    new Rectangle(3, 4),
    new Circle(1),
    new Rectangle(2, 2)
};

double total = 0;
foreach (Shape s in shapes)
{
    Console.WriteLine($"{s.Name}: {s.Area():F2}");
    total += s.Area();
}
Console.WriteLine($"Total: {total:F2}");

// Output:
// Rectangle: 12.00
// Circle: 3.14
// Rectangle: 4.00
// Total: 19.14

abstract class Shape
{
    public abstract string Name { get; }          // abstract property
    public abstract double Area();
}

class Rectangle : Shape
{
    private readonly double _w, _h;
    public Rectangle(double w, double h) { _w = w; _h = h; }

    public override string Name => "Rectangle";
    public override double Area() => _w * _h;
}

class Circle : Shape
{
    private readonly double _r;
    public Circle(double r) => _r = r;

    public override string Name => "Circle";
    public override double Area() => Math.PI * _r * _r;
}
```

The loop knows nothing about rectangles or circles. Adding a `Triangle` later requires no change to that loop. That is the practical benefit.

### 5. Overriding versus hiding with `new`

If a derived class declares a method with the same name but without `override`, it **hides** the base method. The version that runs then depends on the variable's type, not the object's. This is rarely what you want.

```csharp
Base viaBase = new Derived();
Derived viaDerived = new Derived();

Console.WriteLine(viaBase.Overridden());      // run-time type decides: Derived version
Console.WriteLine(viaDerived.Overridden());
Console.WriteLine(viaBase.Hidden());          // variable type decides: Base version
Console.WriteLine(viaDerived.Hidden());       // Derived version

// Output:
// Derived.Overridden
// Derived.Overridden
// Base.Hidden
// Derived.Hidden

class Base
{
    public virtual string Overridden() => "Base.Overridden";
    public string Hidden() => "Base.Hidden";
}

class Derived : Base
{
    public override string Overridden() => "Derived.Overridden";
    public new string Hidden() => "Derived.Hidden";    // 'new' says the hiding is intentional
}
```

Without the `new` keyword the compiler gives a warning. Prefer `virtual`/`override`.

### 6. Overriding `ToString()`

`ToString` is a virtual method of `object`, so you can override it. `Console.WriteLine`, string interpolation and `string.Join` call it for you.

```csharp
var user = new TestUser("alice", "Admin");
Console.WriteLine(user);                       // calls ToString
Console.WriteLine($"Logged in: {user}");
Console.WriteLine(string.Join("; ", new[] { user, new TestUser("bob", "Viewer") }.Select(u => u.ToString())));

// Output:
// alice (Admin)
// Logged in: alice (Admin)
// alice (Admin); bob (Viewer)

class TestUser
{
    public string Name { get; }
    public string Role { get; }

    public TestUser(string name, string role)
    {
        Name = name;
        Role = role;
    }

    public override string ToString() => $"{Name} ({Role})";
}
```

A readable `ToString()` makes test failure messages and debugging output far easier to read.

### 7. `sealed override`, and a page-object hierarchy

`sealed override` stops further overriding. Below, a base page defines a virtual wait and an abstract "is it loaded" check.

```csharp
var pages = new List<BasePage> { new LoginPage(), new DashboardPage() };

foreach (BasePage page in pages)
{
    page.WaitUntilLoaded();
}

// Output:
// Login: checking marker '#login-form'
// Login: loaded
// Dashboard: checking marker '#welcome'
// Dashboard: waiting an extra 500 ms
// Dashboard: loaded

abstract class BasePage
{
    protected abstract string Name { get; }
    protected abstract string LoadedMarker { get; }

    public void WaitUntilLoaded()
    {
        Console.WriteLine($"{Name}: checking marker '{LoadedMarker}'");
        ExtraWait();
        Console.WriteLine($"{Name}: loaded");
    }

    protected virtual void ExtraWait() { }           // default: do nothing
}

class LoginPage : BasePage
{
    protected override string Name => "Login";
    protected override string LoadedMarker => "#login-form";
}

class DashboardPage : BasePage
{
    protected override string Name => "Dashboard";
    protected override string LoadedMarker => "#welcome";

    protected override void ExtraWait()
    {
        Console.WriteLine($"{Name}: waiting an extra 500 ms");
    }
}
```

### 8. Pattern matching on types

Sometimes you do need to treat subtypes differently. A `switch` expression with type patterns is a clean way.

```csharp
Shape[] shapes = { new Square(2), new Circle(1), new Square(3) };

foreach (Shape s in shapes)
{
    string text = s switch
    {
        Square sq => $"square with side {sq.Side}",
        Circle => "a circle",
        _ => "unknown shape"
    };
    Console.WriteLine(text);
}

// Output:
// square with side 2
// a circle
// square with side 3

abstract class Shape { }
class Square : Shape
{
    public double Side { get; }
    public Square(double side) => Side = side;
}
class Circle : Shape
{
    public Circle(double r) { }
}
```

If you find yourself writing many type checks, consider a virtual method instead.

### Common mistakes

- Forgetting `virtual` on the base method (then `override` does not compile) or forgetting `override` in the derived class (then the method is hidden and the base version may run).
- Trying `new` on an abstract class.
- Forgetting to implement an abstract member in a concrete subclass.
- Calling a virtual method from a constructor. The derived override can run before the derived constructor has finished setting up its own fields.

### Why this matters for test automation

Base page classes often declare an abstract property such as the URL or a "page is loaded" marker, and let each page provide it. Custom steps, reporters or data providers use the same idea: the framework code works with the base type and each concrete class supplies the detail. A readable `ToString` makes `Assert` failure messages helpful.

### Interview questions

**Q: What is polymorphism?**
A: The ability to treat objects of derived types through a base type reference and have the correct overridden behaviour run at run time.

**Q: `virtual` versus `abstract`?**
A: A `virtual` member has a default implementation that derived classes may override. An `abstract` member has no implementation and derived (non-abstract) classes must override it.

**Q: Can you instantiate an abstract class?**
A: No. You instantiate a concrete derived class and may hold it in a variable of the abstract type.

**Q: Overriding versus overloading?**
A: Overriding replaces a virtual/abstract method in a derived class (same signature, decided at run time). Overloading is several methods with the same name and different parameters in the same type (decided at compile time).

**Q: What does the `new` modifier on a method do, and why is it discouraged?**
A: It hides a base member. The version called depends on the variable's compile-time type, which is surprising. Prefer `virtual`/`override`.

**Q: What is the difference between an abstract class with all abstract members and an interface?**
A: This is Day 26's topic. In short, a class can inherit only one abstract class but implement many interfaces.

**Exercise**

1. Create an abstract class `Notifier` with an abstract `string Send(string message)` and a concrete `Notify(string message)` that prints the result of `Send`. Implement `EmailNotifier` and `SlackNotifier`. Call them through a `List<Notifier>`.
2. Create `Employee` with a virtual `decimal MonthlyPay()` returning a fixed salary, and `Manager : Employee` overriding it to add a bonus using `base.MonthlyPay()`. Loop over a mixed list and print the total.
3. Give `TestUser` and `AdminUser` a `ToString()` override and print a list of them using `string.Join`.
4. Write a short demo that shows the difference between `override` and `new` for the same method name (like section 5) and explain the output in two sentences.

**Check**

- What decides which override runs: the variable type or the object type?
- Why must a non-abstract class override all abstract members?
- Which method of `object` does `Console.WriteLine(user)` call?

## Day 24: Review and interview questions on OOP

This day consolidates Days 19 to 23. Read the recap table, answer the practice questions before opening the answers, build the mini-program, then practise saying the interview answers aloud.

### Recap

| Concept | Key facts |
|---|---|
| Class and object | A class is the blueprint, an object is an instance made with `new`. Classes are reference types. |
| Field | A variable stored in each object; normally `private`. |
| Property | Get/set accessors; forms: `{ get; set; }`, `{ get; }`, `{ get; init; }`, `=> expr`, `{ get; private set; }`. |
| Constructor | Same name as the class, no return type; runs on `new`. `: this(...)` chains to another constructor of the same class. |
| `this` | The current object. |
| `static` | Belongs to the type, one shared copy, accessed via the type name; cannot use instance members. |
| `const` / `static readonly` / `readonly` | Compile-time constant / run-time constant per type / run-time constant per object. |
| Encapsulation | Hide internals, expose a small safe interface; start `private`. |
| Access modifiers | `public`, `private` (default for members), `protected`, `internal` (default for top-level types), `protected internal`, `private protected`. |
| Inheritance | `class B : A`; single class inheritance; base constructor runs first; `base(...)`, `base.Member`; `sealed` stops inheritance. |
| Polymorphism | `virtual` in base, `override` in derived; the object's real type decides which runs. |
| `abstract` | Cannot be instantiated; abstract members have no body and must be overridden. |
| `new` (hiding) | Hides a base member; the variable's type decides. Prefer `override`. |
| Type tests | `is`, `as`, direct cast (throws `InvalidCastException`). |

### Part 1: Mixed practice questions

1. What is the difference between a class and an object?
2. What does this print?
   ```text
   class Counter { public static int Total; public Counter() { Total++; } }
   new Counter(); new Counter(); new Counter();
   Console.WriteLine(Counter.Total);
   ```
3. What accessibility does a field declared as `int _x;` inside a class have? What about a class declared with no modifier at namespace level?
4. A `Base` class has only a constructor `Base(string name)`. What must a derived class constructor do, and what error do you get otherwise?
5. In what order do the constructors run for `new Derived()` where `Derived : Base`?
6. What is the difference between `{ get; private set; }` and `{ get; init; }`?
7. Given `Base b = new Derived();` and a method `Hello()` that is `virtual` in `Base` and `override` in `Derived`, which version runs? What if `Derived` declares `public new string Hello()` instead?
8. Why is exposing `public List<string> Items { get; }` risky even though there is no setter?
9. Why can a `static` method not read an instance property?
10. A class `Report` is declared `abstract`. Which line fails to compile: `Report r = new Report();` or `Report r = new PdfReport();` (where `PdfReport : Report` is concrete)?

<details>
<summary>Answers</summary>

1. A class is the definition of a type (its data and behaviour). An object is one instance of it created with `new`; each object has its own field values.
2. `3`. `Total` is static, shared by all objects, and the constructor runs three times.
3. The field is `private`. The class is `internal`.
4. It must call a base constructor with `: base(someName)`. Otherwise the compiler reports that there is no argument for the required parameter of the base constructor.
5. `Base` constructor first, then `Derived` constructor.
6. `private set` lets the class change the value at any time. `init` allows assignment only during object creation (constructor or object initializer), after which the property is read-only.
7. With `override`, the `Derived` version runs (decided by the object's real type). With `new` (hiding), the `Base` version runs, because the variable's type decides.
8. The reference cannot be replaced, but callers can still call `Add`, `Remove` and `Clear` on the list and change the object's internal state. Expose `IReadOnlyList<T>` via `AsReadOnly()` instead.
9. A static method does not run on a particular object, so there is no `this`, and so no instance state to read.
10. `Report r = new Report();` fails, because an abstract class cannot be instantiated. The second line is fine.

</details>

### Part 2: Mini-program: a tiny test runner

Build a console program that models a small test run. It combines encapsulation, constructors, static members, inheritance and polymorphism.

Requirements:

1. An abstract class `TestCase` with a get-only `Name` set by the constructor, an abstract method `bool Execute()` and a public method `Run()` that calls `Execute()`, prints `PASS Name` or `FAIL Name` and reports the outcome to the run log.
2. Two concrete tests: `LoginTest` (takes a `TestUser` and passes when the password is not empty) and `TitleTest` (takes an actual and an expected title and passes when they are equal ignoring case).
3. A class `TestUser` with a constructor, `Username` and `Password` properties and an overridden `ToString`.
4. A static class `RunLog` that counts passed and failed tests and exposes a read-only `Summary` string. Its mutating members must be `internal` or `private`, so callers cannot set the counts directly.
5. In the main program, create a `List<TestCase>` with at least four tests (including one failing), run them in a loop and print the summary.

<details>
<summary>Reference solution</summary>

```csharp
var user = new TestUser("alice", "secret");
var emptyPassword = new TestUser("bob", "");

var tests = new List<TestCase>
{
    new LoginTest(user),
    new LoginTest(emptyPassword),
    new TitleTest("Home", "home"),
    new TitleTest("Home", "Products")
};

foreach (TestCase test in tests)
{
    test.Run();
}

Console.WriteLine(RunLog.Summary);

// Output:
// PASS Login alice
// FAIL Login bob
// PASS Title Home
// FAIL Title Home
// 2 passed, 2 failed

abstract class TestCase
{
    public string Name { get; }

    protected TestCase(string name)
    {
        Name = name;
    }

    protected abstract bool Execute();

    public void Run()
    {
        bool passed = Execute();
        Console.WriteLine($"{(passed ? "PASS" : "FAIL")} {Name}");
        RunLog.Record(passed);
    }
}

class LoginTest : TestCase
{
    private readonly TestUser _user;

    public LoginTest(TestUser user) : base($"Login {user.Username}")
    {
        _user = user;
    }

    protected override bool Execute() => _user.Password.Length > 0;
}

class TitleTest : TestCase
{
    private readonly string _actual;
    private readonly string _expected;

    public TitleTest(string actual, string expected) : base($"Title {actual}")
    {
        _actual = actual;
        _expected = expected;
    }

    protected override bool Execute() =>
        _actual.Equals(_expected, StringComparison.OrdinalIgnoreCase);
}

class TestUser
{
    public string Username { get; }
    public string Password { get; }

    public TestUser(string username, string password)
    {
        Username = username;
        Password = password;
    }

    public override string ToString() => Username;
}

static class RunLog
{
    private static int _passed;
    private static int _failed;

    public static string Summary => $"{_passed} passed, {_failed} failed";

    internal static void Record(bool passed)
    {
        if (passed) _passed++;
        else _failed++;
    }
}
```

</details>

### Part 3: Interview questions

**Q: Explain the four pillars of OOP in your own words, with one test-automation example each.**
A: Encapsulation: a page object hides locators behind methods like `LoginAs`. Inheritance: every page derives from `BasePage` and reuses navigation code. Polymorphism: a list of `TestStep` objects runs through one `Run()` call and each step does its own work. Abstraction: callers depend on the essential members (for example an abstract `IsLoaded`) and not the details.

**Q: What is the difference between a field and a property, and why prefer properties?**
A: A field is raw storage. A property is a pair of accessors that can validate, compute and restrict access, and can change implementation later without breaking callers.

**Q: What is the difference between `abstract` and `virtual`?**
A: `virtual` has a default implementation that may be overridden. `abstract` has none and must be overridden by non-abstract derived classes, and it is only allowed in an abstract class.

**Q: What is the difference between `static` and instance members, and when is static state a problem?**
A: Static belongs to the type and exists once; instance belongs to each object. Mutable static state is shared by everything in the process, which makes tests that run in parallel interfere with each other.

**Q: Explain constructor chaining.**
A: A constructor can call another constructor of the same class with `: this(...)` or of the base class with `: base(...)`, so initialisation logic is written once. The called constructor runs first.

**Q: What is the difference between method overriding and hiding?**
A: Overriding (`virtual`/`override`) is resolved at run time using the object's real type. Hiding (`new`) is resolved at compile time using the variable's type.

**Q: When would you choose composition over inheritance?**
A: When the relationship is "has a" rather than "is a", or when you want to avoid tight coupling to a base class. For example, a page object has a logger; it is not a logger.

**Q: What does encapsulating a collection mean, and how do you do it?**
A: Keep the collection private and expose only safe operations, either methods like `Add`, or a read-only view such as `IReadOnlyList<T>` through `AsReadOnly()`.

**Check**

- Can you explain in a minute, with an example, why `override` and `new` behave differently through a base-type variable?
- Can you write an abstract base class with one abstract member and two derived classes from memory?
- Can you list, from the most open to the most closed, the access modifiers you know?

