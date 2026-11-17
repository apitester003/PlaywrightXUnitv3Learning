# Week 1: Setup and First Programs

This week you install the .NET SDK and an editor, create and run your first console program, and learn how a C# project is laid out. You then learn the basic data types (including nullable types), operators and type conversion, and console input/output with string interpolation. Day 6 is a review.

Every code example in this week was compiled and run on .NET 10, and the "Output" comments show what it printed. Examples that read input show a short typed transcript instead.

---

## Day 1: Install the .NET SDK and an editor; run your first `dotnet run`

### What you are installing

- The **.NET SDK** contains the compiler, the `dotnet` command-line tool, the libraries (the Base Class Library) and the runtime that executes your programs. You need the SDK (not only the "runtime") to build code.
- An **editor** to write code. Two common choices:
  - **Visual Studio Code** with the **C# Dev Kit** extension (works on Windows, macOS and Linux).
  - **Visual Studio** (Windows; the free Community edition is enough) with the ".NET desktop development" or "ASP.NET and web development" workload. On macOS use VS Code.

This course targets **.NET 10** (`net10.0`), the same as the Playwright/xUnit repository you will work on later.

### Step 1: install the .NET 10 SDK

Download the installer for your operating system from Microsoft's official .NET download page and choose "SDK" (not "Runtime"), version 10. Alternatives:

- **Windows:** the installer, or the Windows package manager `winget` (the package id is typically `Microsoft.DotNet.SDK.10`; if `winget search dotnet-sdk` shows a different id, use that one).
- **macOS:** the installer (pick the Arm64 build for Apple Silicon, x64 for Intel Macs), or Homebrew.
- **Linux:** use your distribution's package manager (package names look like `dotnet-sdk-10.0`, but the exact name and availability depend on the distribution), or Microsoft's install script/instructions for your distribution.

### Step 2: verify the install

Open a **new** terminal window (an already-open one may not see the new `PATH`). On Windows use PowerShell or Windows Terminal; on macOS/Linux use Terminal.

```bash
dotnet --version
```

You should see something starting with `10.`, for example `10.0.403` (the last numbers differ on your machine). Two more useful commands:

```bash
dotnet --list-sdks      # every SDK installed on this machine
dotnet --info           # full details: SDK, runtimes, OS
```

If you get "command not found" or "'dotnet' is not recognized", the SDK is not on your `PATH`: close and reopen the terminal, or re-run the installer. If `dotnet --version` shows 8 or 9, an older SDK is the default; run `dotnet --list-sdks` to see whether 10 is also installed.

### Step 3: install the editor

For VS Code: install it, open the Extensions panel (Ctrl+Shift+X, or Cmd+Shift+X on macOS), search for **C# Dev Kit** and install it (it pulls in the base "C#" extension). Open a folder, not a single file, so the editor can find the project.

### Step 4: create a console app

Pick a folder for your practice work (for example `~/csharp-course`) and run:

```bash
mkdir HelloWorld
cd HelloWorld
dotnet new console
```

`dotnet new console` creates a project named after the folder. You can also give it a name and a folder explicitly: `dotnet new console -n HelloWorld -o HelloWorld`. Run `dotnet new list` to see all templates. The new folder contains:

```text
HelloWorld/
  HelloWorld.csproj   <- project file (settings)
  Program.cs          <- your code
  obj/                <- build intermediates (created on restore/build)
```

Open `Program.cs`. The template generates a single line:

```csharp
Console.WriteLine("Hello, World!");
```

### Step 5: run it

```bash
dotnet run
```

`dotnet run` restores NuGet packages if needed, compiles the project, and runs the result. You will see `Hello, World!`. The compiled output goes to `bin/Debug/net10.0/`. You can also separate the steps:

```bash
dotnet build    # compile only
dotnet run      # build (if needed) and run
```

Change the text in `Program.cs`, save, and run again to see your own message:

```csharp
Console.WriteLine("Hello, Playwright!");

// Output:
// Hello, Playwright!
```

### Top-level statements

Look at `Program.cs`: there is no `class` and no `Main` method. This is a **top-level statements** program. The compiler wraps your statements in a hidden `Main` method inside a hidden class for you. Only **one** file in a project may use top-level statements. The following program is equivalent; older tutorials and many codebases use this "classic" form:

```csharp
// The same program written the "classic" way, with an explicit class and Main.
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, Playwright!");
    }
}

// Output:
// Hello, Playwright!
```

You can ask the template to generate the classic form with `dotnet new console --use-program-main`. Both forms compile to the same thing; this course uses top-level statements for small examples and normal classes for real code.

Things that work in top-level statements:

- Statements run in order from the top.
- A variable called `args` (a `string[]`) holds the command-line arguments.
- You can `return` an integer: it becomes the process **exit code** (0 means success by convention). Test runners and CI systems use exit codes to decide whether a step passed.
- Type declarations (classes, records) must come after all the statements in the file.

```csharp
// Top-level statements: 'args' exists automatically, and 'return' sets the exit code.
Console.WriteLine($"Number of command-line arguments: {args.Length}");

if (args.Length == 0)
{
    Console.WriteLine("No arguments given, exiting with code 0.");
    return 0;
}

Console.WriteLine($"First argument: {args[0]}");
return 1;

// Output:
// Number of command-line arguments: 0
// No arguments given, exiting with code 0.
```

You can pass arguments with `dotnet run -- first second` (everything after `--` goes to your program, so `args[0]` would be `first`).

### Console basics

`Console.WriteLine` writes text and a line break; `Console.Write` writes without a line break. Inside a normal string, `\n` is a new line, `\t` a tab, `\"` a quote and `\\` a backslash. A string prefixed with `@` (verbatim) treats backslashes literally.

```csharp
Console.Write("Test 1 ... ");
Console.WriteLine("passed");
Console.WriteLine("Name:\tStandard User");
Console.WriteLine("Line one\nLine two");
Console.WriteLine("She said \"hello\"");
Console.WriteLine(@"C:\temp\reports");
Console.WriteLine();
Console.WriteLine("Done");

// Output:
// Test 1 ... passed
// Name:	Standard User
// Line one
// Line two
// She said "hello"
// C:\temp\reports
//
// Done
```

### Common mistakes

- Running `dotnet run` in the wrong folder. It needs to be in the folder with the `.csproj` (or you must pass `--project path`). The error mentions "Couldn't find a project to run".
- Installing only the .NET Runtime instead of the SDK, or testing `dotnet --version` in a terminal opened before the install.
- Forgetting the semicolon at the end of a statement, or using a different case: `console.WriteLine` fails because C# is case-sensitive (`Console` is the correct name).
- Creating a second file with top-level statements; the compiler reports an error because only one is allowed per project.

### Why this matters for test automation

Everything in this course (and every Playwright/xUnit repo) is driven by the `dotnet` CLI: `dotnet build`, `dotnet test`, `dotnet run`. CI pipelines run exactly these commands, so being comfortable with them on a terminal matters more than any editor button. Exit codes are also how a pipeline knows a run failed: `dotnet test` returns a non-zero exit code when tests fail.

### Interview questions

1. **What is the difference between the .NET SDK and the .NET runtime?** The runtime only runs compiled applications. The SDK includes the runtime plus the compiler and the `dotnet` CLI tools needed to build, test and publish.
2. **What does `dotnet run` do?** It restores dependencies, builds the project if needed, and runs it.
3. **What are top-level statements?** A feature that lets a file contain statements directly, with the compiler generating the `Main` entry point. Only one file per project may use them.
4. **What does a program's exit code mean?** A number returned to the operating system. By convention 0 is success and non-zero is failure; CI uses it to pass or fail a step.

### Exercise

1. Install the SDK and an editor and run `dotnet --version`. Write down the version.
2. Create a console app called `Day01` and make it print three lines: your name, the .NET version you installed, and "Ready to learn Playwright".
3. Change the program so that if you run `dotnet run -- hello`, it prints `Argument: hello`, and with no arguments it prints `No arguments`.
4. Make the program return exit code 3 when there are no arguments. On Linux/macOS run `echo $?` right after `dotnet run`; on PowerShell run `echo $LASTEXITCODE`.

### Check

You are done when `dotnet --version` prints a `10.x` version, `dotnet run` prints your three lines, and you see the exit code 3 in the terminal.

---

## Day 2: Project structure: `.csproj`, solution, namespaces, `using`

### The project file

The `.csproj` file is plain XML that tells the SDK how to build the project. The one `dotnet new console` generates for .NET 10 looks like this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

What each part does:

- `Sdk="Microsoft.NET.Sdk"` brings in all the default build rules. By default every `.cs` file under the project folder is compiled; you do not list them.
- `OutputType` `Exe` makes a runnable program. A library (a reusable DLL) uses `Library` (or omits it). Test projects are built as executables too in xUnit v3.
- `TargetFramework` `net10.0` is the .NET version the code targets.
- `ImplicitUsings` `enable` generates a hidden file of `global using` directives so you do not have to write the common ones in every file. For a console app these are `System`, `System.Collections.Generic`, `System.IO`, `System.Linq`, `System.Net.Http`, `System.Threading` and `System.Threading.Tasks`. This is why `Console` works without `using System;`.
- `Nullable` `enable` turns on nullable reference type warnings (Day 3).

Dependencies are added as `PackageReference` items. You normally add them with the CLI, which edits the `.csproj` for you:

```bash
dotnet add package Microsoft.Playwright
```

(You will do this for real later in the course. The package name is shown only to illustrate the command.) After adding, the project file contains an entry like `<PackageReference Include="Microsoft.Playwright" Version="..." />`.

### Folders you will see

- `bin/` holds compiled output (`bin/Debug/net10.0/`).
- `obj/` holds intermediate files, including the generated global usings.
- Both are generated and should not be committed to Git; the standard `.gitignore` for .NET ignores them. Deleting them is safe; the next build recreates them (`dotnet clean` also removes build output).

### Solutions

A **solution** groups several projects so you can build them together and open them as one in an editor. In .NET 10 the `dotnet new sln` command creates a `.slnx` file (a short XML format); older SDKs created `.sln`. The Playwright repo in this course uses a `.slnx`.

```bash
mkdir Workspace && cd Workspace
dotnet new sln -n Workspace            # creates Workspace.slnx
dotnet new console -n App -o App       # a project in the App folder
dotnet new classlib -n Helpers -o Helpers
dotnet sln add App/App.csproj Helpers/Helpers.csproj
dotnet sln list                        # lists the projects in the solution
dotnet add App/App.csproj reference Helpers/Helpers.csproj   # App can now use Helpers
dotnet build                           # builds everything in the solution
```

A **project reference** (`dotnet add ... reference`) lets one project use the code in another. A typical test repo has a shared library project (page objects, helpers) referenced by one or more test projects.

### Namespaces

A **namespace** is a named container for types (classes, records, enums). It prevents name clashes and organizes code. The full name of a type is `Namespace.TypeName`. By convention namespaces follow the folder structure and the project name, for example `MyTests.Pages`.

```csharp
using Acme.Tools;

Console.WriteLine(Greeter.Hello("QA"));
Console.WriteLine(Acme.Tools.Greeter.Hello("full name"));

namespace Acme.Tools
{
    public static class Greeter
    {
        public static string Hello(string name) => $"Hello, {name}!";
    }
}

// Output:
// Hello, QA!
// Hello, full name!
```

Two spellings of namespace declarations exist:

```csharp
namespace Acme.Tools        // block-scoped: braces, several namespaces per file
{
    public class A { }
}
```

```csharp
namespace Acme.Tools;       // file-scoped: one namespace for the whole file, less indentation

public class A { }
```

Use the file-scoped form in normal class files (it is the common modern style). In a file that also has top-level statements, use the block form after the statements, as above.

### `using` directives

`using Some.Namespace;` lets you write `Greeter` instead of `Acme.Tools.Greeter`. It does not "import code"; it only affects name lookup. Related forms:

- `using static System.Math;` lets you call static members without the class name (`Max(3, 9)` instead of `Math.Max(3, 9)`).
- `using Alias = Some.Long.Type;` creates a short name.
- `global using X;` in any one file applies to the whole project. That is what implicit usings generate.

```csharp
using static System.Math;
using Json = System.Text.Json.JsonSerializer;

Console.WriteLine(Max(3, 9));
Console.WriteLine(Round(2.567, 1));

string text = Json.Serialize(new { Name = "Ana", Age = 30 });
Console.WriteLine(text);

// Output:
// 9
// 2.6
// {"Name":"Ana","Age":30}
```

If two namespaces contain a type with the same name, using both with plain names is ambiguous; use the full name for one (or an alias).

```csharp
using Shop.Models;
using Shop.Services;

var item = new Item("Backpack", 29.99m);
var printer = new ReceiptPrinter();
printer.Print(item);

// Two different classes can share a name if their namespaces differ.
var a = new Shop.Models.Result(true);
var b = new Shop.Services.Result("ok");
Console.WriteLine($"{a.Success} / {b.Message}");

namespace Shop.Models
{
    public record Item(string Name, decimal Price);

    public record Result(bool Success);
}

namespace Shop.Services
{
    public class ReceiptPrinter
    {
        public void Print(Item item) => Console.WriteLine($"{item.Name}: {item.Price}");
    }

    public record Result(string Message);
}

// Output:
// Backpack: 29.99
// True / ok
```

Do not confuse `using` directives (at the top of a file) with the `using` **statement** that disposes resources (`using var file = ...`), which you will meet later; Playwright code uses it often.

### Common mistakes

- Putting top-level statements after a type or namespace declaration (they must come first in the file).
- Thinking `using` adds a reference to a library. The package or project reference makes the code available; `using` only shortens names.
- Editing `bin/` or `obj/` files, or committing them.
- Changing a namespace but not the `using` directives in other files, leading to "type or namespace name could not be found".

### Why this matters for test automation

A Playwright test repo is organized by namespaces and folders: pages, flows, tests, helpers. Knowing how project references, packages and namespaces fit together lets you read an unfamiliar repo quickly. Also, `Microsoft.Playwright` and `Xunit` types are found by `using Microsoft.Playwright;` and `using Xunit;`; missing usings are one of the most common beginner compile errors.

### Interview questions

1. **What is a namespace for?** It groups related types and avoids name collisions; the fully-qualified name is namespace plus type name.
2. **What does `using System;` do?** It lets you refer to types in `System` without the prefix. It does not load anything extra.
3. **What are implicit usings?** A project setting (`ImplicitUsings`) that auto-generates global using directives for common namespaces.
4. **What is the difference between a project and a solution?** A project compiles to one assembly (DLL/EXE) and is described by a `.csproj`; a solution groups projects for building and editing together.
5. **How do you add a NuGet package?** `dotnet add package <name>`, which adds a `PackageReference` to the `.csproj`.

### Exercise

1. Create a folder `Workspace` with a solution, a console project `App` and a class library `Helpers` (use the commands above).
2. In `Helpers`, rename `Class1.cs` (if the template creates it) or add a class `TextTools` in namespace `Helpers` with a public static method `Shout(string text)` that returns the text upper-cased with `!` appended.
3. Reference `Helpers` from `App` and call `TextTools.Shout("ready")` from `App/Program.cs`, using a `using Helpers;` directive. Run `dotnet run --project App`.
4. Open `App.csproj` and find `TargetFramework` and the `ProjectReference` entry.

### Check

`dotnet build` at the solution folder succeeds, `dotnet run --project App` prints `READY!`, and you can point to the `ProjectReference` line in `App.csproj`.

---

## Day 3: Variables, data types, `var`, nullable types

### Variables and types

C# is **statically typed**: every variable has a type that is known at compile time and cannot change. You declare a variable with a type, a name and (usually) an initial value.

Core built-in types:

| Type | Holds | Example |
|---|---|---|
| `int` | whole numbers (32-bit) | `42` |
| `long` | larger whole numbers (64-bit) | `3_000_000_000` |
| `byte` | 0 to 255 | `255` |
| `double` | fractional numbers (binary floating point) | `19.99` |
| `float` | smaller floating point, `f` suffix | `0.5f` |
| `decimal` | exact decimal numbers, `m` suffix | `19.99m` |
| `bool` | `true` or `false` | `true` |
| `char` | one UTF-16 character, single quotes | `'A'` |
| `string` | text, double quotes | `"Login page"` |

The underscore in `3_000_000_000` is only a visual digit separator.

```csharp
int count = 42;
long bigNumber = 3_000_000_000;
double price = 19.99;
decimal money = 19.99m;
float ratio = 0.5f;
bool isVisible = true;
char grade = 'A';
string title = "Login page";
byte small = 255;

Console.WriteLine(count);
Console.WriteLine(bigNumber);
Console.WriteLine(price);
Console.WriteLine(money);
Console.WriteLine(ratio);
Console.WriteLine(isVisible);
Console.WriteLine(grade);
Console.WriteLine(title);
Console.WriteLine(small);

Console.WriteLine($"int range: {int.MinValue} to {int.MaxValue}");
Console.WriteLine($"byte range: {byte.MinValue} to {byte.MaxValue}");
Console.WriteLine($"long max: {long.MaxValue}");

// Output:
// 42
// 3000000000
// 19.99
// 19.99
// 0.5
// True
// A
// Login page
// 255
// int range: -2147483648 to 2147483647
// byte range: 0 to 255
// long max: 9223372036854775807
```

### `var`, `const` and definite assignment

`var` asks the compiler to infer the type from the right-hand side. The variable is still strongly typed; `var` is not "any type". It requires an initializer.

```csharp
var name = "Ana";          // string
var age = 30;              // int
var total = 12.5;          // double
var amount = 12.5m;        // decimal
var flag = true;           // bool

Console.WriteLine(name.GetType().Name);
Console.WriteLine(age.GetType().Name);
Console.WriteLine(total.GetType().Name);
Console.WriteLine(amount.GetType().Name);
Console.WriteLine(flag.GetType().Name);

// Once the type is fixed, it cannot change:
// age = "thirty";   // compile error: cannot convert string to int

const int MaxRetries = 3;
Console.WriteLine(MaxRetries * 2);

int uninitializedLocalsMustBeAssigned;
uninitializedLocalsMustBeAssigned = 5;
Console.WriteLine(uninitializedLocalsMustBeAssigned);

// Output:
// String
// Int32
// Double
// Decimal
// Boolean
// 6
// 5
```

Use `var` when the type is obvious from the right side (`var user = new User();`) and an explicit type when it is not (`int count = Compute();`). Follow the style of the codebase. `const` defines a compile-time constant that cannot change. A local variable must be assigned before it is read, or the compiler reports an error.

### Value types and reference types

- **Value types** (`int`, `double`, `bool`, `char`, `decimal`, structs, enums) hold their value directly. Copying one makes an independent copy.
- **Reference types** (`string`, classes, arrays, collections) hold a reference to an object. Copying a variable copies the reference; both variables point to the same object. (`string` is a reference type but immutable, so it behaves like a value in practice; Week 3 covers this.)

### `double` versus `decimal`

`double` stores numbers in binary, so many decimal fractions cannot be represented exactly. `decimal` stores decimal digits and is meant for money. Never compare `double` values with `==` after arithmetic; compare with a tolerance.

```csharp
double d = 0.1 + 0.2;
decimal m = 0.1m + 0.2m;

Console.WriteLine(d == 0.3);
Console.WriteLine(m == 0.3m);
Console.WriteLine(d.ToString("R"));
Console.WriteLine(m);

Console.WriteLine(Math.Abs(d - 0.3) < 0.000001);

decimal price = 19.99m;
int quantity = 3;
Console.WriteLine(price * quantity);

// Output:
// False
// True
// 0.30000000000000004
// 0.3
// True
// 59.97
```

### Nullable value types

A plain `int` can never be `null`. Add `?` to allow it: `int?` is shorthand for `Nullable<int>`. It has `HasValue` and `Value`, and the operator `??` supplies a fallback.

```csharp
int? maybeAge = null;
Console.WriteLine(maybeAge.HasValue);
Console.WriteLine(maybeAge ?? -1);

maybeAge = 25;
Console.WriteLine(maybeAge.HasValue);
Console.WriteLine(maybeAge.Value);
Console.WriteLine(maybeAge ?? -1);

bool? answer = null;      // three states: true, false, unknown
Console.WriteLine(answer is null ? "unknown" : answer.ToString());

int parsedOk = int.TryParse("12", out int n) ? n : -1;
Console.WriteLine(parsedOk);

int? fromText = int.TryParse("abc", out int m) ? m : null;
Console.WriteLine(fromText is null ? "not a number" : fromText.ToString());

// int plainInt = null;   // compile error: int is not nullable

// Output:
// False
// -1
// True
// 25
// 25
// unknown
// 12
// not a number
```

Reading `.Value` on a null `int?` throws an `InvalidOperationException`, so check `HasValue` or use `??`.

### Nullable reference types

With `<Nullable>enable</Nullable>` the compiler tracks null for reference types too. `string` means "should never be null"; `string?` means "may be null". These are **compile-time warnings** only; the runtime does not stop you from putting null in a `string`. The compiler helps you find places where a `NullReferenceException` could happen.

Useful null operators:

- `?.` null-conditional: if the left side is null, the whole expression is null instead of throwing.
- `??` null-coalescing: use the right side when the left is null.
- `??=` assign only when the variable is currently null.
- `is null` / `is not null`: preferred way to test for null.

```csharp
string title = "Home";            // never null (compiler warns if you assign null)
string? subtitle = null;          // may be null

Console.WriteLine(title.Length);

// Console.WriteLine(subtitle.Length);   // warning CS8602: possible null dereference
Console.WriteLine(subtitle?.Length);                 // null-conditional: prints an empty line
Console.WriteLine(subtitle?.Length ?? 0);            // null-coalescing: 0
Console.WriteLine(subtitle ?? "(none)");

subtitle ??= "Default subtitle";                     // assign only if null
Console.WriteLine(subtitle);

if (subtitle is not null)
{
    Console.WriteLine(subtitle.ToUpper());
}

string? env = Environment.GetEnvironmentVariable("SURELY_NOT_SET_12345");
Console.WriteLine(env is null ? "env var missing" : env);
Console.WriteLine(string.IsNullOrEmpty(env));
Console.WriteLine(string.IsNullOrWhiteSpace("   "));

// Output:
// 4
//
// 0
// (none)
// Default subtitle
// DEFAULT SUBTITLE
// env var missing
// True
// True
```

The `!` operator (null-forgiving, `value!`) tells the compiler "trust me, this is not null". It silences the warning but does not check anything at runtime. Use it sparingly.

### Common mistakes

- Using `double` for money and getting results like `0.30000000000000004`.
- Writing `19.99` where a `decimal` is needed. A plain `19.99` is a `double`; you need `19.99m`.
- Ignoring nullable warnings, then getting a `NullReferenceException` at runtime.
- Calling `.Value` on an `int?` that is null.

### Why this matters for test automation

Nullable awareness matters constantly in Playwright code: many APIs return values that can be null, such as `await locator.TextContentAsync()` which returns `string?`, or `Environment.GetEnvironmentVariable`. Config values that may be missing (a base URL, a password from an environment variable) should be `string?` until validated. `decimal` is the correct type when a test asserts on prices.

### Interview questions

1. **What is the difference between `double` and `decimal`?** `double` is binary floating point, fast and approximate; `decimal` is base-10 with higher precision, slower, suited to money.
2. **What is the difference between value and reference types?** Value types store the data directly and copy by value; reference types store a reference to an object, so copies share the same object.
3. **What does `string?` mean?** A nullable reference type annotation: the string may be null. It is enforced by compiler warnings, not at runtime.
4. **What does `var` do?** The compiler infers the static type from the initializer; the variable does not become dynamic.
5. **What are `??` and `?.`?** `??` gives a default when the left side is null; `?.` short-circuits to null when the object is null.

### Exercise

1. Declare variables for a test case: a `string` name, an `int` id, a `decimal` expectedPrice, a `bool` isSmokeTest, and a `string?` notes that starts as null. Print them.
2. Print `notes ?? "no notes"`, then assign `notes ??= "added later"` and print it again.
3. Show with code that `0.1 + 0.2 == 0.3` is `false` for `double` and `true` for `decimal`.
4. Create an `int?` called `retryCount` set to null and print `retryCount ?? 3`.

### Check

The output shows `no notes`, then `added later`, then `False` and `True` for the comparisons, and `3` for the retry count. The build has no warnings.

---

## Day 4: Operators, expressions, type conversion

### Arithmetic operators

`+ - * / %` plus increment `++` and decrement `--`. Two rules cause many bugs:

1. Integer division discards the fraction: `17 / 5` is `3`, not `3.4`. Make at least one operand a `double` or `decimal` to get a fractional result.
2. `%` is the remainder, and it takes the sign of the left operand.

Compound assignment (`+=`, `-=`, `*=`, `/=`, `%=`) combines an operation with assignment. `x++` returns the old value then increments; `++x` increments then returns the new value.

```csharp
int a = 17;
int b = 5;

Console.WriteLine(a + b);     // addition
Console.WriteLine(a - b);     // subtraction
Console.WriteLine(a * b);     // multiplication
Console.WriteLine(a / b);     // integer division: fraction is discarded
Console.WriteLine(a % b);     // remainder
Console.WriteLine(a / (double)b);   // real division

Console.WriteLine(-17 / 5);   // truncates toward zero
Console.WriteLine(-17 % 5);   // remainder keeps the sign of the left operand

int counter = 0;
counter++;
counter += 10;
counter *= 2;
Console.WriteLine(counter);

int x = 5;
int y = x++;     // y gets 5, then x becomes 6
int z = ++x;     // x becomes 7, then z gets 7
Console.WriteLine($"{x} {y} {z}");

Console.WriteLine(2 + 3 * 4);
Console.WriteLine((2 + 3) * 4);

// Output:
// 22
// 12
// 85
// 3
// 2
// 3.4
// -3
// -2
// 22
// 7 5 7
// 14
// 20
```

Precedence: `*`, `/`, `%` bind tighter than `+`, `-`. Use parentheses when in doubt; they cost nothing and help readers.

### Comparison and logical operators

`==  !=  <  >  <=  >=` produce a `bool`. Combine booleans with `&&` (and), `||` (or) and `!` (not). `&&` and `||` **short-circuit**: the right side is not evaluated if the left side already decides the result. This is what makes `text != null && text.Length > 3` safe.

The conditional (ternary) operator `condition ? a : b` chooses between two values. String `==` compares the content, and it is case-sensitive; use `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)` for case-insensitive comparison.

```csharp
int statusCode = 404;
bool isError = statusCode >= 400;
bool isClientError = statusCode >= 400 && statusCode < 500;
bool isOkOrRedirect = statusCode == 200 || statusCode == 302;
Console.WriteLine($"{isError} {isClientError} {isOkOrRedirect} {!isError}");

// && and || short-circuit: the right side runs only when needed.
static bool Check(string label, bool result)
{
    Console.WriteLine($"  evaluating {label}");
    return result;
}

Console.WriteLine("false && ...");
bool r1 = Check("left", false) && Check("right", true);
Console.WriteLine("true || ...");
bool r2 = Check("left", true) || Check("right", false);
Console.WriteLine($"{r1} {r2}");

string? text = null;
bool safe = text != null && text.Length > 3;   // no crash: Length is never read
Console.WriteLine(safe);

string label = statusCode == 200 ? "OK" : "Not OK";   // ternary
Console.WriteLine(label);

Console.WriteLine("abc" == "abc");
Console.WriteLine("abc" == "ABC");
Console.WriteLine(string.Equals("abc", "ABC", StringComparison.OrdinalIgnoreCase));

// Output:
// True True False False
// false && ...
//   evaluating left
// true || ...
//   evaluating left
// False True
// False
// Not OK
// True
// False
// True
```

### Type conversion

**Implicit** conversions happen automatically when no data can be lost (`int` to `long` or `double`). **Explicit** conversions (casts) are required when data may be lost: `(int)19.99` truncates toward zero to 19; it does not round.

Overflow: by default integer arithmetic and casts are **unchecked** and wrap around silently. Wrap an expression in `checked(...)` to get an `OverflowException` instead.

Converting between text and numbers:

- `int.Parse(text)` throws `FormatException` if the text is invalid.
- `int.TryParse(text, out int value)` returns `false` instead of throwing and sets `value` to 0 on failure. Prefer it for user input.
- `Convert.ToInt32(x)` converts many types and rounds doubles (to the nearest even number when exactly halfway).
- `value.ToString()` converts to text.

```csharp
// Implicit conversion: always safe, no data lost.
int small = 100;
long big = small;
double d = small;
Console.WriteLine($"{big} {d}");

// Explicit conversion (cast): may lose data, you must ask for it.
double price = 19.99;
int truncated = (int)price;          // truncates, does not round
Console.WriteLine(truncated);
Console.WriteLine((int)Math.Round(price));
Console.WriteLine((int)-2.9);

long tooBig = 3_000_000_000;
int wrapped = unchecked((int)tooBig);   // silently wraps around
Console.WriteLine(wrapped);

try
{
    int safe = checked((int)tooBig);
    Console.WriteLine(safe);
}
catch (OverflowException ex)
{
    Console.WriteLine($"OverflowException: {ex.Message}");
}

// Converting text to numbers.
int parsed = int.Parse("42");
Console.WriteLine(parsed + 1);

if (int.TryParse("4x2", out int result))
{
    Console.WriteLine(result);
}
else
{
    Console.WriteLine($"Could not parse, result is {result}");
}

try
{
    int.Parse("abc");
}
catch (FormatException ex)
{
    Console.WriteLine($"FormatException: {ex.Message}");
}

// Converting values to text.
Console.WriteLine(Convert.ToString(123) + "!");
Console.WriteLine(123.ToString() + "!");
Console.WriteLine(Convert.ToInt32("77") + Convert.ToInt32(3.7));
Console.WriteLine(Convert.ToInt32(2.5));   // Convert rounds, to the nearest even number on ties
Console.WriteLine(Convert.ToInt32(3.5));

// Output:
// 100 100
// 19
// 20
// -2
// -1294967296
// OverflowException: Arithmetic operation resulted in an overflow.
// 43
// Could not parse, result is 0
// FormatException: The input string 'abc' was not in a correct format.
// 123!
// 123!
// 81
// 2
// 4
```

### Mixed-type expressions

When you mix numeric types, the smaller type is widened. `int * decimal` gives `decimal`. There is no implicit conversion between `double` and `decimal`, so you need a cast.

```csharp
// A tiny "price with tax" calculation that mixes types on purpose.
int quantity = 3;
decimal unitPrice = 19.99m;
double taxRate = 0.2;

decimal subtotal = quantity * unitPrice;           // int is widened to decimal
decimal tax = subtotal * (decimal)taxRate;         // double -> decimal needs a cast
decimal total = subtotal + tax;

Console.WriteLine(subtotal);
Console.WriteLine(tax);
Console.WriteLine(total);
Console.WriteLine(Math.Round(total, 2));

// Integer division trap when computing percentages.
int passed = 7;
int totalTests = 9;
Console.WriteLine(passed / totalTests * 100);          // 0 * 100
Console.WriteLine(passed * 100 / totalTests);          // multiply first
Console.WriteLine((double)passed / totalTests * 100);  // real division
Console.WriteLine(passed * 100.0 / totalTests);

// The "is" operator and pattern checks on values.
int code = 503;
Console.WriteLine(code is >= 500 and <= 599);
Console.WriteLine(code is 200 or 201 or 204);
Console.WriteLine(code is not 404);

// Null-coalescing in an expression.
int? timeout = null;
int effective = timeout ?? 30;
Console.WriteLine(effective);

// Output:
// 59.97
// 11.994
// 71.964
// 71.96
// 0
// 77
// 77.77777777777779
// 77.77777777777777
// True
// False
// True
// 30
```

The last lines use **patterns**: `is >= 500 and <= 599`, `is 200 or 201 or 204`, `is not 404`. They read like English and are handy for checking HTTP status codes.

### Common mistakes

- Integer division when you wanted a percentage: `passed / total * 100` gives 0 for any `passed < total`. Multiply first or cast to `double`.
- Using `(int)` and expecting rounding. Use `Math.Round` if you want rounding.
- Using `int.Parse` on user or file input without handling bad data.
- Confusing `=` (assignment) with `==` (comparison). In an `if`, the compiler usually flags this for non-bool types.

### Why this matters for test automation

Tests do arithmetic on scraped text all the time: parse "$29.99" to a `decimal`, compare it to an expected total, compute the count of rows. Choosing `decimal`, using `TryParse`, and avoiding the integer-division trap prevent false passes and flaky failures. Status-code checks (`code is >= 200 and < 300`) show up in API tests.

### Interview questions

1. **What is the result of `7 / 2` in C#?** `3`, because both operands are `int`. Use `7 / 2.0` for `3.5`.
2. **What is the difference between `int.Parse` and `int.TryParse`?** `Parse` throws on invalid input; `TryParse` returns `false` and sets the out value to 0.
3. **What does `&&` short-circuiting mean?** If the left operand is false, the right operand is not evaluated.
4. **What is the difference between implicit and explicit conversion?** Implicit is automatic and lossless; explicit needs a cast because data may be lost or an exception may occur.
5. **What does `checked` do?** It makes integer arithmetic or conversion throw `OverflowException` on overflow instead of wrapping.

### Exercise

1. Write a program with `int total = 17; int groups = 5;` that prints the quotient, the remainder, and the real (double) result of dividing them.
2. Convert the text `"199.90"` to a `decimal` with `decimal.TryParse`, print it multiplied by 3, and print what happens for `"abc"` (a clear message, no crash).
3. Print `(int)7.8`, `Math.Round(7.8)` and `Convert.ToInt32(7.5)` and explain in a comment why the results differ.
4. Write a boolean expression that is true when a status code is a client error (400-499), using the pattern syntax.

### Check

Your results are `3`, `2`, `3.4`; `599.70` (the exact trailing digits depend on the string you parsed); `7`, `8`, `8`; and your boolean is `true` for 404 and `false` for 500.

---

## Day 5: Console input and output, string interpolation

### Reading input

`Console.ReadLine()` waits for the user to type a line and press Enter. It returns `string?`: `null` when input has ended (for example, when input is piped from a file or the user presses Ctrl+Z on Windows or Ctrl+D on Linux/macOS). That is why the compiler wants you to handle null.

This program needs interactive input. Run it with `dotnet run` and type the answers:

```csharp
Console.Write("Enter your name: ");
string? name = Console.ReadLine();

Console.Write("Enter your age: ");
string? ageText = Console.ReadLine();

if (string.IsNullOrWhiteSpace(name))
{
    name = "stranger";
}

if (int.TryParse(ageText, out int age))
{
    Console.WriteLine($"Hello, {name}. Next year you will be {age + 1}.");
}
else
{
    Console.WriteLine($"Hello, {name}. '{ageText}' is not a valid age.");
}
```

A typical session (what you type is after each prompt):

```text
Enter your name: Ana
Enter your age: 29
Hello, Ana. Next year you will be 30.
```

If you type `abc` for the age, the program prints `Hello, Ana. 'abc' is not a valid age.` instead of crashing.

### A validation loop

User input is unreliable, so loop until it is valid. This also previews the `while` loop you will study in Week 2.

```csharp
// Reads numbers until the user enters a valid one between 1 and 10.
int choice;
while (true)
{
    Console.Write("Pick a number from 1 to 10: ");
    string? line = Console.ReadLine();

    if (line is null)
    {
        Console.WriteLine("Input ended, using 1.");
        choice = 1;
        break;
    }

    if (!int.TryParse(line, out choice))
    {
        Console.WriteLine($"'{line}' is not a number.");
        continue;
    }

    if (choice < 1 || choice > 10)
    {
        Console.WriteLine("Out of range.");
        continue;
    }

    break;
}

Console.WriteLine($"You chose {choice}");
```

With `abc`, `42` and `7` typed one per line, the session looks like this:

```text
Pick a number from 1 to 10: abc
'abc' is not a number.
Pick a number from 1 to 10: 42
Out of range.
Pick a number from 1 to 10: 7
You chose 7
```

### String interpolation

Put `$` before a string and write expressions inside `{}`. After a colon you can add a **format string**; after a comma, an alignment.

- `{value:F2}` fixed-point with 2 decimals. `{value:0.0}` is a custom format. `{n:D3}` pads an integer with zeros to 3 digits.
- `{value,10}` right-aligns in a 10-character field; `{value,-10}` left-aligns.
- `{{` and `}}` print literal braces.
- A conditional expression inside `{}` must be wrapped in parentheses, because `:` would otherwise start a format string.
- Other standard format codes include `N2` (thousands separators), `C` (currency) and `P` (percent). Their exact output depends on the **culture** (language/region settings) of the machine unless you pass a culture such as `CultureInfo.InvariantCulture`.

```csharp
using System.Globalization;

string user = "standard_user";
int attempts = 3;
double elapsed = 1.23456;
decimal price = 1234.5m;

Console.WriteLine($"User {user} needed {attempts} attempts");
Console.WriteLine($"Elapsed: {elapsed:F2} seconds");
Console.WriteLine($"Elapsed: {elapsed:0.0} seconds");
Console.WriteLine($"Attempts padded: [{attempts:D3}]");
Console.WriteLine($"Math inside braces: {attempts * 2 + 1}");
Console.WriteLine($"Ternary needs parentheses: {(attempts > 2 ? "retry" : "ok")}");
Console.WriteLine($"Braces: {{literal}} and {attempts}");

// Alignment: {value,width} right-aligns, {value,-width} left-aligns.
Console.WriteLine($"[{user,15}]");
Console.WriteLine($"[{user,-15}]");
Console.WriteLine($"{"Name",-10}|{"Qty",5}");
Console.WriteLine($"{"Pen",-10}|{12,5}");

// Number and date formatting depends on the culture. InvariantCulture is stable.
var inv = CultureInfo.InvariantCulture;
Console.WriteLine(price.ToString("N2", inv));
var when = new DateTime(2025, 3, 7, 14, 5, 9);
Console.WriteLine(when.ToString("yyyy-MM-dd HH:mm:ss", inv));
Console.WriteLine(string.Format(inv, "{0} scored {1:F1}", user, 88.456));
Console.WriteLine(string.Format("{0} + {1} = {2}", 1, 2, 3));

// Output:
// User standard_user needed 3 attempts
// Elapsed: 1.23 seconds
// Elapsed: 1.2 seconds
// Attempts padded: [003]
// Math inside braces: 7
// Ternary needs parentheses: retry
// Braces: {literal} and 3
// [  standard_user]
// [standard_user  ]
// Name      |  Qty
// Pen       |   12
// 1,234.50
// 2025-03-07 14:05:09
// standard_user scored 88.5
// 1 + 2 = 3
```

`string.Format("{0} + {1}", a, b)` is the older, index-based form that interpolation replaces. You will still see it in older code and in logging APIs.

### Raw strings, verbatim strings and output streams

- Verbatim `@"..."` ignores backslash escapes (good for Windows paths).
- **Raw string literals** `"""..."""` can span lines and contain quotes and backslashes without escaping. The closing `"""` position decides how much leading whitespace is removed. Add `$` in front to interpolate (`$"""..."""`).
- `Console.Error.WriteLine` writes to the error stream; `Console.WriteLine` writes to standard output. CI systems and shells can capture them separately.

```csharp
string path = @"C:\Users\qa\reports";
string json = """
    {
      "user": "standard_user",
      "attempts": 3
    }
    """;
string name = "Ana";
string multi = $"""
    Hello {name},
    you have "3" new messages.
    """;

Console.WriteLine(path);
Console.WriteLine(json);
Console.WriteLine(multi);

Console.WriteLine("This line goes to standard output.");

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Colored text (colors only show in a real terminal).");
Console.ResetColor();

// Output:
// C:\Users\qa\reports
// {
//   "user": "standard_user",
//   "attempts": 3
// }
// Hello Ana,
// you have "3" new messages.
// This line goes to standard output.
// Colored text (colors only show in a real terminal).
```

### Common mistakes

- Using `Console.ReadLine()` result directly in arithmetic: it is a string, and may be null.
- Putting a ternary inside `{}` without parentheses.
- Forgetting the `$` so the braces print literally.
- Expecting `F2` and `N2` output to look the same on every machine. Decimal separators and grouping depend on the culture, so use `CultureInfo.InvariantCulture` when output must be identical everywhere (for example when comparing text or writing files).

### Why this matters for test automation

Test output is how you diagnose failures. Good interpolated messages ("Expected 3 items in cart but found 2 on page {url}") save time. Console input is rare in automated tests (they must run unattended), but reading configuration from arguments and environment variables follows the same null-checking and parsing habits. Culture-dependent number parsing is a classic source of "passes on my machine, fails in CI".

### Interview questions

1. **What does `$"..."` do?** It creates an interpolated string: expressions in `{}` are evaluated and inserted, with optional alignment and format strings.
2. **Why does `Console.ReadLine()` return `string?`?** It returns null when there is no more input, so callers must handle null.
3. **What is a verbatim string versus a raw string literal?** Verbatim (`@""`) disables backslash escapes. A raw string (`"""`) allows multi-line text and embedded quotes without escaping and trims common indentation.
4. **Why use `CultureInfo.InvariantCulture`?** To format and parse numbers/dates in a fixed, machine-independent way.

### Exercise

1. Write a program that asks for a product name, a unit price and a quantity, validates the price (`decimal.TryParse`) and quantity (`int.TryParse`), and prints a receipt line such as `Pen        x3 @ 1.50 = 4.50` using alignment.
2. If a number is invalid, print a clear message and stop (use `return 1;`).
3. Print the receipt header using a raw string literal.
4. Run it twice: once with valid input and once with a bad quantity, and check the exit code of both runs.

### Check

With input `Pen`, `1.50`, `3` the receipt shows total `4.50`. With a bad quantity the program prints a message and the exit code is 1. Run it with an input redirect if you like (`dotnet run < input.txt`, where `input.txt` has three lines).

---

## Day 6: Review and practice

### Recap

| Day | Topic | Key points |
|---|---|---|
| 1 | Setup and first run | .NET SDK vs runtime; `dotnet --version`, `dotnet new console`, `dotnet run`; top-level statements; `args`; exit codes |
| 2 | Project structure | `.csproj` (`TargetFramework`, `Nullable`, `ImplicitUsings`); `.slnx` solutions; project references; namespaces; `using`, `using static`, aliases |
| 3 | Types and nullability | `int`, `long`, `double`, `decimal`, `bool`, `char`, `string`; `var`, `const`; value vs reference types; `int?`, `string?`; `?.`, `??`, `??=` |
| 4 | Operators and conversion | integer division, `%`, short-circuit `&&` and `\|\|`; implicit vs explicit conversion; `Parse` vs `TryParse`; `checked`; patterns (`is >= 500 and <= 599`) |
| 5 | Console I/O | `ReadLine` returns `string?`; validation loops; `$""` with `:F2`, `,10`; raw strings; culture |

### Practice questions

Try each question on paper first, then check in the answers block.

1. What does `7 / 2` print? And `7 / 2.0`?
2. What does `10 % 3` give?
3. What does `(int)3.99` give?
4. Is `0.1 + 0.2 == 0.3` true or false for `double`?
5. What does `s?.Length ?? -1` give when `string? s = null;`?
6. After `bool ok = int.TryParse("12a", out int v);`, what are `ok` and `v`?
7. What does `$"{5,4}|{"ab",-4}|"` produce?
8. After `int x = 5; int y = x++ + ++x;`, what are `x` and `y`?
9. What are `"5" + 3`, `5 + 3 + "x"` and `"x" + 5 + 3`?
10. Which type should you use for a price, `double` or `decimal`, and why?

<details>
<summary>Answers</summary>

1. `3` and `3.5` (integer division versus double division).
2. `1`.
3. `3` (the cast truncates).
4. `false`, because of binary floating-point representation.
5. `-1`.
6. `ok` is `False` and `v` is `0`.
7. `   5|ab  |` (the number is right-aligned in 4 characters, the string left-aligned in 4).
8. `x` is 7 and `y` is 12 (`x++` yields 5 and makes x 6, then `++x` makes x 7 and yields 7; 5 + 7).
9. `"53"`, `"8x"`, `"x53"`. Evaluation is left to right; once a string is involved, `+` concatenates.
10. `decimal`, because it represents base-10 fractions exactly.

The code below prints the answers to questions 1-9 in order (question 10 is conceptual):

```csharp
Console.WriteLine(7 / 2);
Console.WriteLine(7 / 2.0);
Console.WriteLine(10 % 3);
Console.WriteLine((int)3.99);
Console.WriteLine(0.1 + 0.2 == 0.3);

string? s = null;
Console.WriteLine(s?.Length ?? -1);

bool ok = int.TryParse("12a", out int v);
Console.WriteLine($"{ok} {v}");

Console.WriteLine($"{5,4}|{"ab",-4}|");

int x = 5;
int y = x++ + ++x;
Console.WriteLine($"x={x} y={y}");

Console.WriteLine("5" + 3);
Console.WriteLine(5 + 3 + "x");
Console.WriteLine("x" + 5 + 3);

// Output:
// 3
// 3.5
// 1
// 3
// False
// -1
// False 0
//    5|ab  |
// x=7 y=12
// 53
// 8x
// x53
```

</details>

### Mini-program: test run summary

Write a console program with these variables: `int totalTests = 48; int passed = 45; int skipped = 2; double durationSeconds = 93.4;`. Print a report that shows total, passed, failed (computed), skipped, the pass rate to 1 decimal place, the average time per test to 2 decimals, the duration as minutes and seconds, and `Result: PASSED` or `Result: FAILED`. Align the numbers in a column. Try to do it without looking at the solution.

<details>
<summary>Solution</summary>

```csharp
int totalTests = 48;
int passed = 45;
int skipped = 2;
double durationSeconds = 93.4;

int failed = totalTests - passed - skipped;
double passRate = (double)passed / totalTests * 100;
double averageSeconds = durationSeconds / totalTests;
int minutes = (int)(durationSeconds / 60);
double remainingSeconds = durationSeconds % 60;

Console.WriteLine("=== Test run summary ===");
Console.WriteLine($"{"Total",-10}{totalTests,6}");
Console.WriteLine($"{"Passed",-10}{passed,6}");
Console.WriteLine($"{"Failed",-10}{failed,6}");
Console.WriteLine($"{"Skipped",-10}{skipped,6}");
Console.WriteLine($"Pass rate: {passRate:F1}%");
Console.WriteLine($"Average per test: {averageSeconds:F2}s");
Console.WriteLine($"Duration: {minutes}m {remainingSeconds:F1}s");
Console.WriteLine(failed > 0 ? "Result: FAILED" : "Result: PASSED");

// Output:
// === Test run summary ===
// Total         48
// Passed        45
// Failed         1
// Skipped        2
// Pass rate: 93.8%
// Average per test: 1.95s
// Duration: 1m 33.4s
// Result: FAILED
```

</details>

Extension: read the numbers from `args` using `int.TryParse`/`double.TryParse` and return exit code 1 if the report shows failures.

### Interview questions

1. **Explain what happens when you run `dotnet run`.** It restores NuGet packages, compiles the project to `bin/`, and starts the program (an exit code is returned to the shell).
2. **What are the roles of `.csproj` and `.slnx`?** The project file configures one project (target framework, packages, references); the solution file groups projects.
3. **Why prefer `TryParse` over `Parse` for user input?** Invalid input is expected, not exceptional; `TryParse` avoids exceptions and lets you handle the failure with an `if`.
4. **When would you use `decimal` over `double`?** For money and anything that needs exact decimal results; `double` is for scientific or approximate values.
5. **What does enabling nullable reference types change?** The compiler warns about possible null dereferences and about assigning null to non-nullable references. There is no runtime change.
6. **What is the difference between a namespace and an assembly?** A namespace is a logical grouping of type names in code; an assembly is the compiled unit (DLL/EXE) that contains types.
7. **What is the difference between `==` on strings and `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)`?** `==` compares content case-sensitively; the second ignores case.
