# Week 2: Control Flow and Methods

This week you learn to make decisions (`if`, `switch`), repeat work (`for`, `foreach`, `while`, `do-while`), and organize code into methods with parameters, return values, overloads, optional and named arguments, and `out`/`ref`. You also learn scope and the basics of recursion. Day 12 is a review.

Every code example was compiled and run on .NET 10, and the "Output" comments show the real output. All examples use the default console template settings (top-level statements, implicit usings, nullable enabled).

---

## Day 7: `if`/`else`, `switch` statements and expressions

### Making decisions with `if`

An `if` statement runs a block only when its condition is `true`. The condition must be a `bool` (C# does not treat 0 or null as false). Add `else if` for more branches and `else` for the fallback. Branches are tested from top to bottom and only the first true one runs.

```csharp
int score = 72;

if (score >= 90)
{
    Console.WriteLine("Grade A");
}
else if (score >= 80)
{
    Console.WriteLine("Grade B");
}
else if (score >= 70)
{
    Console.WriteLine("Grade C");
}
else
{
    Console.WriteLine("Grade F");
}

// Conditions must be bool. Combine them with && || !
int age = 17;
bool hasTicket = true;
if (age >= 18 && hasTicket)
{
    Console.WriteLine("Entry allowed");
}
else if (age < 18 && hasTicket)
{
    Console.WriteLine("Entry needs an adult");
}
else
{
    Console.WriteLine("No ticket");
}

// Guard clauses keep the "happy path" unindented.
string? input = "  ";
if (string.IsNullOrWhiteSpace(input))
{
    Console.WriteLine("Nothing to process");
    return;
}

Console.WriteLine($"Processing {input}");

// Output:
// Grade C
// Entry needs an adult
// Nothing to process
```

Notes:

- Always use braces `{}`, even for one-line bodies. Without them only the next statement is conditional, which causes bugs when someone adds a second line.
- The last example is a **guard clause**: handle the bad case first and `return`, so the rest of the code is not nested inside an `else`. This keeps test helper code easy to read.
- Order matters in an `else if` chain: put the most specific condition first.

### The `switch` statement

When you compare one value against many constants, `switch` is clearer than a long `else if` chain. Each `case` section must end with `break`, `return`, `throw` (or `continue` inside a loop). C# does not allow "falling through" into the next section, but you can stack several labels on one body. `default` handles everything else. A `when` clause adds an extra condition.

```csharp
string browser = "firefox";

switch (browser)
{
    case "chromium":
        Console.WriteLine("Blink engine");
        break;
    case "firefox":
        Console.WriteLine("Gecko engine");
        break;
    case "webkit":
    case "safari":                      // two labels share one body
        Console.WriteLine("WebKit engine");
        break;
    default:
        Console.WriteLine("Unknown browser");
        break;
}

// 'when' adds a condition to a case. Cases are checked from top to bottom.
int statusCode = 404;
switch (statusCode)
{
    case 200:
        Console.WriteLine("OK");
        break;
    case int code when code >= 500:
        Console.WriteLine("Server error");
        break;
    case int code when code >= 400:
        Console.WriteLine($"Client error {code}");
        break;
    default:
        Console.WriteLine("Other");
        break;
}

// A case section must not "fall through" into the next one.
// case 1:
//     Console.WriteLine("one");
//     case 2:     // compile error: Control cannot fall through from one case label
//
// Use 'goto case 2;' only if you really want that, or stack labels as shown above.

// Output:
// Gecko engine
// Client error 404
```

### The `switch` expression

A switch **expression** is not a statement; it produces a value. Each arm is `pattern => result`, arms are separated by commas, and `_` (the discard) matches anything. It is the preferred style when you are choosing a value.

Useful patterns in arms:

- Constants: `404 => ...`, `"firefox" => ...`
- Relational: `>= 500`, `< 300`
- Logical: `and`, `or`, `not` (`>= 200 and < 300`, `400 or 401 or 403`)
- Tuples: `(true, false) => ...` to match several values at once

Arms are evaluated top to bottom; the first match wins. Put specific arms before general ones (in the example 404 comes before `>= 400 and < 500`). If an arm can never match because an earlier arm already covers it, the compiler reports an error.

```csharp
static string Classify(int statusCode) => statusCode switch
{
    >= 200 and < 300 => "success",
    >= 300 and < 400 => "redirect",
    400 or 401 or 403 => "auth or bad request",
    404 => "not found",
    >= 400 and < 500 => "client error",
    >= 500 => "server error",
    _ => "unknown"
};

foreach (int code in new[] { 200, 301, 403, 404, 418, 503, 99 })
{
    Console.WriteLine($"{code} -> {Classify(code)}");
}

// A switch expression produces a value, so you can assign it directly.
string browser = "webkit";
string engine = browser switch
{
    "chromium" => "Blink",
    "firefox" => "Gecko",
    "webkit" => "WebKit",
    _ => throw new ArgumentException($"Unsupported browser: {browser}")
};
Console.WriteLine(engine);

// Switching on more than one value with a tuple pattern.
static string Describe(bool isLoggedIn, bool isAdmin) => (isLoggedIn, isAdmin) switch
{
    (false, _) => "guest",
    (true, false) => "user",
    (true, true) => "admin"
};
Console.WriteLine(Describe(true, true));
Console.WriteLine(Describe(false, true));

// Output:
// 200 -> success
// 301 -> redirect
// 403 -> auth or bad request
// 404 -> not found
// 418 -> client error
// 503 -> server error
// 99 -> unknown
// WebKit
// admin
// guest
```

### Enums in a switch

An `enum` is a set of named constants backed by integers. Switching on an enum is very common. If a switch expression does not cover all possible values and none matches at runtime, it throws a `SwitchExpressionException` (a subclass of `InvalidOperationException`). The compiler also warns when it can see the switch is not exhaustive: building the example below gives warning CS8524 for the `risky` switch, which is intentional here. Include a `_` arm unless you want an unexpected value to fail loudly.

```csharp
Priority p = Priority.High;

string label = p switch
{
    Priority.Low => "can wait",
    Priority.Medium => "this sprint",
    Priority.High => "now",
    _ => "unknown"
};
Console.WriteLine($"{p} ({(int)p}): {label}");

// An enum value that matches no arm: the discard arm saves us.
Priority invalid = (Priority)42;
Console.WriteLine(invalid);
Console.WriteLine(invalid switch { Priority.Low => "low", _ => "no match" });

// Without a discard arm, an unmatched value throws at runtime.
try
{
    string risky = invalid switch
    {
        Priority.Low => "low",
        Priority.Medium => "medium",
        Priority.High => "high"
    };
    Console.WriteLine(risky);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"{ex.GetType().Name} caught");
}

// 'is' patterns inside if statements.
object value = 42;
if (value is int number && number > 40)
{
    Console.WriteLine($"Big int {number}");
}

string? text = null;
if (text is null or "")
{
    Console.WriteLine("empty or null");
}

enum Priority { Low = 1, Medium = 2, High = 3 }

// Output:
// High (3): now
// 42
// no match
// SwitchExpressionException caught
// Big int 42
// empty or null
```

`is` patterns work inside `if` too: `value is int number && number > 40` tests the type and creates a typed variable in one step. `text is null or ""` checks for two cases at once.

### Common mistakes

- Using `=` instead of `==` in a condition. For `bool` values just write `if (flag)`.
- Forgetting `break` in a `switch` section, which is a compile error in C#.
- Placing a general arm before a specific one in a chain or switch expression, hiding the specific case.
- Omitting the `_` arm in a switch expression and getting a runtime exception on unexpected input.
- Comparing strings with different case and expecting a match; normalize or use `StringComparison.OrdinalIgnoreCase`.

### Why this matters for test automation

Tests and helpers branch constantly: which browser to launch (`"chromium" => ...`), which base URL for which environment, whether an HTTP response is a success or an error. A switch expression with an `_ => throw new ArgumentException(...)` arm is the standard way to turn a configuration string into an object and to fail early when someone mistypes a setting.

### Interview questions

1. **What is the difference between a `switch` statement and a `switch` expression?** A statement executes code per case; an expression evaluates to a value, each arm being `pattern => value`, and the compiler checks that arms are exhaustive (warning if not).
2. **Does C# `switch` fall through like C or Java?** No. Each section must end with `break`, `return`, `throw` or similar; multiple case labels can share one body.
3. **What is the discard `_` in a switch expression?** The catch-all pattern that matches any value not matched by earlier arms.
4. **What happens if no arm of a switch expression matches?** A `SwitchExpressionException` is thrown at runtime.
5. **What is a guard clause?** An early `return` (or `throw`) for invalid or trivial cases so that the main logic is not nested.

### Exercise

1. Write `static string Grade(int score)` using a switch expression: 90+ is "A", 80-89 "B", 70-79 "C", 60-69 "D", below 60 "F", and anything under 0 or above 100 throws `ArgumentOutOfRangeException`. Print the grade for 95, 80, 59 and 100.
2. Write an `enum Environment { Dev, Staging, Prod }` and a switch expression that maps each to a base URL string (use fake URLs like `https://dev.example.test`).
3. Write an `if`/`else if` chain that prints "Fizz" for multiples of 3, "Buzz" for multiples of 5, "FizzBuzz" for both, and the number otherwise, for the numbers 1 to 15 (you may use the `for` loop from tomorrow, or call your code with a few separate values).

### Check

`Grade(95)` is `A`, `Grade(59)` is `F`, `Grade(100)` is `A`, and a bad value throws. FizzBuzz prints `1 2 Fizz 4 Buzz Fizz 7 8 Fizz Buzz 11 Fizz 13 14 FizzBuzz` (one per line is fine).

---

## Day 8: Loops: `for`, `foreach`, `while`, `do-while`

### Choosing a loop

| Loop | Use when |
|---|---|
| `for` | you need an index or a known number of repetitions |
| `foreach` | you want every item of a collection, no index needed |
| `while` | you repeat until a condition changes, possibly zero times |
| `do-while` | the body must run at least once |

Quick vocabulary used here: an **array** is a fixed-size sequence of values of one type (`int[]`, `string[]`), indexed from 0, with a `.Length`. A `List<string>` is a growable sequence. Week 3 covers both in detail; this week they only serve as something to loop over.

### `for`

`for (initializer; condition; iterator)`. The initializer runs once, the condition is checked before each pass, and the iterator runs after each pass. A variable declared in the initializer exists only inside the loop.

```csharp
int[] numbers = { 10, 20, 30 };

for (int i = 0; i < numbers.Length; i++)
{
    Console.WriteLine($"index {i} = {numbers[i]}");
}

// All three parts of 'for' are optional pieces: initializer; condition; iterator.
for (int i = 10; i > 0; i -= 3)
{
    Console.Write($"{i} ");
}
Console.WriteLine();

// Count by two and build a sum.
int sum = 0;
for (int i = 2; i <= 10; i += 2)
{
    sum += i;
}
Console.WriteLine($"Sum of evens up to 10: {sum}");

// 'i' only exists inside the loop; it can be reused afterwards.
for (int i = 0; i < 2; i++) { }
for (int i = 5; i < 7; i++) { Console.Write($"[{i}]"); }
Console.WriteLine();

// Nested loops: a small multiplication table.
for (int row = 1; row <= 3; row++)
{
    for (int col = 1; col <= 4; col++)
    {
        Console.Write($"{row * col,3}");
    }
    Console.WriteLine();
}

// Output:
// index 0 = 10
// index 1 = 20
// index 2 = 30
// 10 7 4 1
// Sum of evens up to 10: 30
// [5][6]
//   1  2  3  4
//   2  4  6  8
//   3  6  9 12
```

The `row * col,3` in the last example right-aligns each number in a field of width 3 (Day 5).

### `foreach`

`foreach (Type item in collection)` visits every element. You cannot change the loop variable, and you must not add or remove items of the collection you are looping over: the collection throws an `InvalidOperationException` on the next step. In the example the removal of `"b"` happened, and then the loop failed; that is why the second loop prints only `c` after removing `a`.

```csharp
string[] browsers = { "chromium", "firefox", "webkit" };

foreach (string browser in browsers)
{
    Console.WriteLine($"Running on {browser}");
}

// foreach works on anything enumerable, including strings (a sequence of chars).
int vowels = 0;
foreach (char c in "playwright")
{
    if ("aeiou".Contains(c))
    {
        vowels++;
    }
}
Console.WriteLine($"Vowels: {vowels}");

// The loop variable is read-only:
// foreach (string b in browsers) { b = "x"; }   // compile error

// You cannot add or remove items of a collection while foreach is reading it.
var names = new List<string> { "a", "b", "c" };
try
{
    foreach (string n in names)
    {
        if (n == "b")
        {
            names.Remove(n);
        }
    }
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"InvalidOperationException: {ex.Message}");
}

// Fix: loop over a copy, or build a new list.
foreach (string n in names.ToList())
{
    if (n == "a")
    {
        names.Remove(n);
    }
}
Console.WriteLine(string.Join(",", names));

// Output:
// Running on chromium
// Running on firefox
// Running on webkit
// Vowels: 2
// InvalidOperationException: Collection was modified; enumeration operation may not execute.
// c
```

### `while` and `do-while`

`while` checks the condition first; `do-while` checks it last. The polling example mirrors what test code does when it waits for a page or service to become ready.

```csharp
// while: the condition is checked BEFORE each pass; the body may run zero times.
int countdown = 3;
while (countdown > 0)
{
    Console.WriteLine($"countdown {countdown}");
    countdown--;
}

// do-while: the body runs at least once, the condition is checked AFTER.
int attempts = 0;
do
{
    attempts++;
    Console.WriteLine($"attempt {attempts}");
} while (attempts < 2);

int zeroTimes = 10;
while (zeroTimes < 5)
{
    Console.WriteLine("never printed");
}

// Typical test-automation pattern: poll until a condition is true, with a limit.
int readyAfter = 4;               // pretend the page is ready on the 4th check
int checks = 0;
bool isReady = false;
while (!isReady && checks < 10)
{
    checks++;
    isReady = checks >= readyAfter;
    Console.WriteLine($"check {checks}: ready={isReady}");
}
Console.WriteLine(isReady ? "Page ready" : "Timed out");

// An intentional infinite loop with an explicit exit.
int n = 0;
while (true)
{
    n++;
    if (n == 3) break;
}
Console.WriteLine($"n = {n}");

// Output:
// countdown 3
// countdown 2
// countdown 1
// attempt 1
// attempt 2
// check 1: ready=False
// check 2: ready=False
// check 3: ready=False
// check 4: ready=True
// Page ready
// n = 3
```

Make sure something inside the loop eventually changes the condition. A loop with a condition that never becomes false runs forever (press Ctrl+C in the terminal to stop it). Always give polling loops a limit such as `checks < 10` or a time limit.

### `break` and `continue`

- `continue` jumps to the next iteration.
- `break` leaves the innermost loop. To leave nested loops, use a flag, or move the loops into a method and `return` from it.

```csharp
// continue skips to the next iteration; break leaves the loop completely.
for (int i = 1; i <= 8; i++)
{
    if (i % 2 == 0) continue;      // skip even numbers
    if (i > 5) break;              // stop after 5
    Console.Write($"{i} ");
}
Console.WriteLine();

// break only leaves the innermost loop. Use a flag (or a method with return) to leave both.
bool found = false;
for (int row = 0; row < 3 && !found; row++)
{
    for (int col = 0; col < 3; col++)
    {
        if (row * col == 2)
        {
            Console.WriteLine($"found at row {row}, col {col}");
            found = true;
            break;
        }
    }
}

// Searching a sequence: the first match wins.
string[] logLines =
{
    "INFO start",
    "INFO step 1",
    "ERROR step 2 failed",
    "INFO step 3",
    "ERROR step 4 failed"
};

string? firstError = null;
foreach (string line in logLines)
{
    if (line.StartsWith("ERROR"))
    {
        firstError = line;
        break;
    }
}
Console.WriteLine(firstError ?? "no errors");

// Counting and summing with a loop.
int errorCount = 0;
foreach (string line in logLines)
{
    if (line.StartsWith("ERROR")) errorCount++;
}
Console.WriteLine($"{errorCount} errors out of {logLines.Length} lines");

// Output:
// 1 3 5
// found at row 1, col 2
// ERROR step 2 failed
// 2 errors out of 5 lines
```

### Common mistakes

- Off-by-one errors: `i <= array.Length` instead of `i < array.Length` throws an `IndexOutOfRangeException`. Valid indexes are `0` to `Length - 1`.
- Modifying a collection inside `foreach` (see above). Loop over a copy (`names.ToList()`) or build a new list.
- Forgetting to update the loop condition in a `while`, producing an infinite loop.
- Declaring a variable inside the loop when you need its value afterwards. Declare it before the loop.
- Using `break` and expecting it to leave an outer loop too.

### Why this matters for test automation

Loops appear when you check every row of a table, every link in a menu, or retry an operation. `foreach` over a list of elements, a counter for assertions, and a bounded `while` for polling are everyday patterns. Playwright has built-in auto-waiting and assertion retries, so you should prefer those over your own polling loops, but understanding the loop makes it clear what the library does for you and helps when you must write a custom wait.

### Interview questions

1. **When do you use `for` versus `foreach`?** `foreach` when you only need each element; `for` when you need the index, want to step differently, or must modify elements by position.
2. **What is the difference between `while` and `do-while`?** `while` may run zero times; `do-while` always runs at least once.
3. **What happens if you add an item to a `List<T>` inside a `foreach` over it?** An `InvalidOperationException` ("Collection was modified") is thrown on the next iteration.
4. **What do `break` and `continue` do?** `break` exits the loop; `continue` skips to the next iteration.
5. **How would you avoid an infinite polling loop?** Add an attempt or time limit and a clear failure (throw or report timeout).

### Exercise

1. Using `for`, print the numbers 1 to 20 on one line, but replace multiples of 3 with `Fizz`.
2. Using `foreach` over `string[] words = { "alpha", "beta", "gamma", "delta" }`, find the longest word and print it with its length.
3. Using `while`, simulate retrying: start with `int attempt = 0;` and keep going until `attempt == 3` or a simulated check succeeds (succeed when `attempt == 2`), printing each attempt.
4. Using nested `for` loops, print a 5x5 grid where the diagonal is `X` and everything else is `.`.

### Check

Exercise 2 prints `alpha` with length 5: `alpha`, `gamma` and `delta` all have 5 letters, and with a strict `>` comparison the first one found stays. Exercise 3 stops after printing attempts 1 and 2 (or 0, 1, 2, depending on where you increment). The grid has `X` on the main diagonal.

---

## Day 9: Methods: parameters, return values, overloads

### Anatomy of a method

A method is a named block of code you can call. It has:

```text
static  int  Add  ( int a, int b )  { ... }
  |      |    |        |
  |      |    |        parameters (inputs)
  |      |    name
  |      return type (void means nothing is returned)
  modifier
```

`return value;` ends the method and gives the value back. In a method with a non-`void` return type **every** code path must return a value (or throw). A method with a single expression can be written with `=>` (expression-bodied).

Inside a top-level statements file, methods you declare are **local functions** and need no class (marking them `static` is optional but good practice). In normal code, methods live inside classes; a `static` method belongs to the class itself and is called with the class name (`Math.Max`).

```csharp
// Parameters: values the caller passes in. Return type: what comes back.
static int Add(int a, int b)
{
    return a + b;
}

// Expression-bodied method: a single expression after =>
static int Square(int n) => n * n;

// void: no return value.
static void PrintHeader(string title)
{
    Console.WriteLine($"== {title} ==");
}

// bool-returning helpers read well in conditions.
static bool IsValidPort(int port) => port is >= 1 and <= 65535;

static string Describe(int number)
{
    if (number < 0)
    {
        return "negative";     // return leaves the method immediately
    }

    if (number == 0)
    {
        return "zero";
    }

    return "positive";         // every path must return a value
}

PrintHeader("Methods");
Console.WriteLine(Add(2, 3));
Console.WriteLine(Square(Add(2, 3)));      // a call can be an argument
Console.WriteLine(IsValidPort(8080));
Console.WriteLine(IsValidPort(70000));
Console.WriteLine(Describe(-4));
Console.WriteLine(Describe(0));
Console.WriteLine(Describe(9));

// Output:
// == Methods ==
// 5
// 25
// True
// False
// negative
// zero
// positive
```

A few rules:

- Name methods with a verb in PascalCase (`CalculateTotal`, `IsValidPort`). Methods that return `bool` often start with `Is`, `Has`, `Can` or `Try`.
- A method should do one job. If you struggle to name it, it probably does too much.
- Parameters are the names in the method declaration; **arguments** are the values you pass in a call.

### Passing arguments

Arguments are passed **by value**: the method receives a copy.

- For value types (`int`, `bool`, `decimal`), changes inside the method do not affect the caller.
- For reference types (`List<T>`, classes, arrays), the copy is a copy of the reference. The method can change the object they both point to, but assigning a new object to the parameter does not change the caller's variable.
- Strings are immutable. Methods like `ToUpper()` return a new string; you must keep the result.

A method can return several values at once by returning a **tuple** `(int Min, int Max)`. The caller reads `result.Min` or deconstructs into variables.

```csharp
// Arguments are passed BY VALUE by default: the method gets a copy.
static void TryToChange(int number)
{
    number = 999;
    Console.WriteLine($"inside: {number}");
}

int original = 5;
TryToChange(original);
Console.WriteLine($"outside: {original}");

// For a reference type, the copy is a copy of the REFERENCE, so both see the same object.
static void AddItem(List<string> list)
{
    list.Add("added inside");
}

static void Replace(List<string> list)
{
    list = new List<string> { "a brand new list" };     // only changes the local copy of the reference
    Console.WriteLine($"inside count: {list.Count}");
}

var items = new List<string> { "first" };
AddItem(items);
Replace(items);
Console.WriteLine(string.Join(", ", items));

// Strings are immutable: methods return new strings.
static string Shout(string s) => s.ToUpper();

string name = "ana";
Shout(name);                        // result ignored: nothing changes
Console.WriteLine(name);
name = Shout(name);                 // keep the result
Console.WriteLine(name);

// Returning more than one value with a tuple.
static (int Min, int Max) MinMax(int[] values)
{
    int min = values[0];
    int max = values[0];
    foreach (int v in values)
    {
        if (v < min) min = v;
        if (v > max) max = v;
    }
    return (min, max);
}

var result = MinMax(new[] { 4, 9, -2, 7 });
Console.WriteLine($"min={result.Min} max={result.Max}");

(int lowest, int highest) = MinMax(new[] { 1, 2, 3 });     // deconstruct
Console.WriteLine($"{lowest}-{highest}");

// Output:
// inside: 999
// outside: 5
// inside count: 1
// first, added inside
// ana
// ANA
// min=-2 max=9
// 1-3
```

### Overloads

**Overloading** means several methods with the same name but different parameter lists (different number or types of parameters). The compiler picks the best match at compile time from the arguments you pass. The return type is not part of the distinction.

Overloads must be declared in a class. (Local functions in a top-level statements file cannot be overloaded; the compiler reports that the name is already defined, so the example puts them in a static class.)

```csharp
Console.WriteLine(Printer.Format(5));
Console.WriteLine(Printer.Format(5.5));
Console.WriteLine(Printer.Format("five"));
Console.WriteLine(Printer.Format(1, 2));
Console.WriteLine(Printer.Format(5L));        // long: no exact match, the compiler picks the best implicit conversion
Console.WriteLine(Printer.Format('c'));       // char converts implicitly to int and double; int is the better match

Console.WriteLine(Printer.Log("started"));
Console.WriteLine(Printer.Log("WARN", "slow response"));

// The Base Class Library is full of overloads:
Console.WriteLine(Math.Max(3, 7));
Console.WriteLine(Math.Max(3.5, 1.2));
Console.WriteLine(Math.Round(2.567));
Console.WriteLine(Math.Round(2.567, 2));

static class Printer
{
    // Overloads: same name, different parameter lists.
    public static string Format(int value) => $"int:{value}";
    public static string Format(double value) => $"double:{value}";
    public static string Format(string value) => $"string:{value}";
    public static string Format(int a, int b) => $"pair:{a},{b}";

    // The return type alone does not distinguish overloads:
    // public static int Parse(string s) => 0;
    // public static long Parse(string s) => 0;   // compile error: already defines a member 'Parse' with the same parameter types

    // Convenience overload that calls the fuller one.
    public static string Log(string message) => Log("INFO", message);
    public static string Log(string level, string message) => $"[{level}] {message}";
}

// Output:
// int:5
// double:5.5
// string:five
// pair:1,2
// double:5
// int:99
// [INFO] started
// [WARN] slow response
// 7
// 3.5
// 3
// 2.57
```

If no overload is an exact match, the compiler uses implicit conversions and picks the best one: a `long` argument goes to the `double` overload because `long` converts implicitly to `double` but not to `int`; a `char` goes to `int`. When two overloads are equally good the call is ambiguous and does not compile.

### Static methods in a class and local functions

Put reusable helpers in a static class. A method you only need inside one other method can be a local function declared inside it.

```csharp
// Static methods inside a class, called with the class name.
Console.WriteLine(StringTools.Truncate("The quick brown fox", 9));
Console.WriteLine(StringTools.Truncate("short", 9));
Console.WriteLine(StringTools.IsPalindrome("Level"));
Console.WriteLine(StringTools.IsPalindrome("Playwright"));

// A local function: a method declared inside another method.
double Average(params double[] values)
{
    double total = 0;
    foreach (double v in values) total += v;
    return values.Length == 0 ? 0 : total / values.Length;
}
Console.WriteLine(Average(2, 4, 9));

static class StringTools
{
    public static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength) + "...";
    }

    public static bool IsPalindrome(string text)
    {
        string lower = text.ToLowerInvariant();
        for (int i = 0, j = lower.Length - 1; i < j; i++, j--)
        {
            if (lower[i] != lower[j]) return false;
        }

        return true;
    }
}

// Output:
// The quick...
// short
// True
// False
// 5
```

### Common mistakes

- Ignoring a return value: `Shout(name);` does nothing useful for an immutable string; write `name = Shout(name);`.
- Missing `return` on some path: error "not all code paths return a value".
- Expecting a method to change an `int` argument (it gets a copy; see `ref` tomorrow).
- Writing very long methods. Split them.
- Creating overloads that differ only in return type, which does not compile.

### Why this matters for test automation

Page objects and test helpers are collections of methods: `LoginAsync(string user, string password)`, `GetCartTotal()`. Good naming and small single-purpose methods make tests read like requirements. Overloads provide convenient variants (for example `ClickAsync()` and `ClickAsync(options)` in Playwright itself). Understanding by-value passing explains why a helper that changes a string or number "does nothing" for the caller.

### Interview questions

1. **What is method overloading?** Multiple methods with the same name and different parameter lists, resolved at compile time. Return type alone is not enough.
2. **Are arguments passed by value or by reference in C#?** By value by default. For reference types the value is the reference, so the callee can mutate the object but cannot reassign the caller's variable without `ref`.
3. **What is an expression-bodied method?** A method whose body is a single expression written with `=>`.
4. **What does `static` mean on a method?** It belongs to the type, not to an instance; call it with the type name and it cannot use instance members.
5. **How can a method return more than one value?** A tuple, an `out` parameter (Day 10), or a small class/record.

### Exercise

1. Write `static int Max(int a, int b, int c)` without using `Math.Max`, and call it with three different orderings of 4, 9, 6.
2. Write overloads in a static class `Area`: `Rectangle(double width, double height)` and `Circle(double radius)` (use `Math.PI`), plus a `Square(double side)` that calls `Rectangle`. Print the three areas rounded to 2 decimals.
3. Write `static (int Even, int Odd) CountEvenOdd(int[] values)` that returns a tuple, and print the result for `{ 1, 2, 3, 4, 5 }`.
4. Show by code that a method that does `number = number * 2` on an `int` parameter does not change the caller's variable.

### Check

`Max` returns 9 in every ordering. Areas are `12` (for 3 x 4), `78.54` (radius 5) and `25` (side 5). `CountEvenOdd` returns `(2, 3)`. The caller's number is unchanged.

---

## Day 10: Optional and named arguments, `out`, `ref`

### Optional parameters

Give a parameter a default value and callers may leave it out. Optional parameters must come after required ones. The default must be a compile-time constant.

### Named arguments

Write `name: value` in the call. You can then pass arguments in any order and skip optional parameters in the middle. Named arguments are very useful for `bool` flags and numbers whose meaning is not obvious at the call site: `Connect("host", secure: false)` is clearer than `Connect("host", 443, false, 60)`. After a positional argument you may continue with named ones.

### `params`

`params` on the last parameter lets callers pass any number of values (or an array).

```csharp
// Optional parameters have a default value and must come after the required ones.
static string Connect(string host, int port = 443, bool secure = true, int timeoutSeconds = 30)
{
    string scheme = secure ? "https" : "http";
    return $"{scheme}://{host}:{port} (timeout {timeoutSeconds}s)";
}

Console.WriteLine(Connect("example.com"));
Console.WriteLine(Connect("example.com", 8080));

// Named arguments: say which parameter you mean. Order no longer matters.
Console.WriteLine(Connect("example.com", secure: false));
Console.WriteLine(Connect(host: "example.com", timeoutSeconds: 5));
Console.WriteLine(Connect(timeoutSeconds: 10, host: "localhost", port: 5000, secure: false));

// Named arguments make boolean flags readable:
Console.WriteLine(Connect("example.com", 443, false, 60));            // what do false and 60 mean?
Console.WriteLine(Connect("example.com", secure: false, timeoutSeconds: 60));

// 'params' lets the caller pass any number of values.
static int Sum(params int[] numbers)
{
    int total = 0;
    foreach (int n in numbers) total += n;
    return total;
}

Console.WriteLine(Sum());
Console.WriteLine(Sum(1));
Console.WriteLine(Sum(1, 2, 3, 4));
Console.WriteLine(Sum(new[] { 10, 20 }));

// Optional parameter versus overload: both can give "defaults".
static string Greet(string name, string greeting = "Hello") => $"{greeting}, {name}!";
Console.WriteLine(Greet("Ana"));
Console.WriteLine(Greet("Ana", greeting: "Welcome"));

// Output:
// https://example.com:443 (timeout 30s)
// https://example.com:8080 (timeout 30s)
// http://example.com:443 (timeout 30s)
// https://example.com:443 (timeout 5s)
// http://localhost:5000 (timeout 10s)
// http://example.com:443 (timeout 60s)
// http://example.com:443 (timeout 60s)
// 0
// 1
// 10
// 30
// Hello, Ana!
// Welcome, Ana!
```

Optional parameters versus overloads: optional parameters keep one method and are compact. Overloads are safer when the default changes over time, because the default value of an optional parameter is baked into the *calling* code at compile time, so callers in other projects only see a changed default after they are recompiled. (This detail is an interview favorite.)

### `out` parameters

An `out` parameter lets a method hand back an extra value. Rules:

- The method must assign every `out` parameter before it returns.
- The caller passes the variable with the `out` keyword. The variable does not need a value beforehand.
- You can declare the variable inline: `out int value`, or ignore it with the discard `out _`.

This is the "Try" pattern: `bool TryXxx(input, out result)`. `int.TryParse` and `Dictionary.TryGetValue` (Week 3) work this way. Write your own for operations that may fail in normal use.

```csharp
// 'out' returns an extra value. The method MUST assign it before returning.
static bool TryParsePrice(string text, out decimal price)
{
    if (decimal.TryParse(text.TrimStart('$'), System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out price))
    {
        return true;
    }

    price = 0;
    return false;
}

// The caller can declare the variable right in the call (out variable).
if (TryParsePrice("$29.99", out decimal p1))
{
    Console.WriteLine($"Parsed {p1}");
}

if (!TryParsePrice("free", out decimal p2))
{
    Console.WriteLine($"Could not parse, p2 = {p2}");
}

// Discard with _ when you do not need the value.
bool looksNumeric = int.TryParse("123", out _);
Console.WriteLine(looksNumeric);

// An out parameter in a "find" method.
static bool TryFindFirstNegative(int[] values, out int index)
{
    for (int i = 0; i < values.Length; i++)
    {
        if (values[i] < 0)
        {
            index = i;
            return true;
        }
    }

    index = -1;
    return false;
}

if (TryFindFirstNegative(new[] { 5, 3, -1, 8 }, out int where))
{
    Console.WriteLine($"First negative at index {where}");
}

// Divide and get both quotient and remainder.
static void DivRem(int a, int b, out int quotient, out int remainder)
{
    quotient = a / b;
    remainder = a % b;
}

DivRem(17, 5, out int q, out int r);
Console.WriteLine($"17 / 5 = {q} remainder {r}");

// Output:
// Parsed 29.99
// Could not parse, p2 = 0
// True
// First negative at index 2
// 17 / 5 = 3 remainder 2
```

### `ref` and `in`

`ref` passes the caller's **variable** itself, so the method can read and change it. Unlike `out`, the variable must be assigned before the call, and the method is not required to assign it. With `ref` a method can even replace the caller's reference to an object, which plain by-value passing cannot do. `in` passes a variable by reference as read-only, mainly a performance tool for large structs, so you will rarely write it.

Use `ref`/`out` sparingly: they make code harder to follow. A tuple return, or a small class/record, is often cleaner for multiple results.

```csharp
// 'ref' passes the variable itself, so the method can change the caller's variable.
// The variable must already be assigned before the call.
static void Increment(ref int counter)
{
    counter++;
}

int visits = 0;
Increment(ref visits);
Increment(ref visits);
Console.WriteLine(visits);

static void Swap(ref int a, ref int b)
{
    int temp = a;
    a = b;
    b = temp;
}

int x = 1, y = 2;
Swap(ref x, ref y);
Console.WriteLine($"x={x} y={y}");

// ref lets a method replace the caller's reference too (compare with Day 9).
static void ReplaceList(ref List<string> list)
{
    list = new List<string> { "new list" };
}

var items = new List<string> { "old" };
ReplaceList(ref items);
Console.WriteLine(items[0]);

// 'in' passes by reference but read-only (useful for large structs; rarely needed).
static double Length(in double x, in double y) => Math.Sqrt(x * x + y * y);
double a = 3, b = 4;
Console.WriteLine(Length(in a, in b));

// Tuples are often a simpler alternative to out and ref.
static (bool Success, int Value) TryParseInt(string text) =>
    int.TryParse(text, out int v) ? (true, v) : (false, 0);

var parsed = TryParseInt("15");
Console.WriteLine($"{parsed.Success} {parsed.Value}");

// Output:
// 2
// x=2 y=1
// new list
// 5
// True 15
```

### Common mistakes

- Putting an optional parameter before a required one (compile error).
- Forgetting `ref` or `out` at the call site; the compiler requires it, so write it both in the declaration and the call.
- Returning from a method without assigning an `out` parameter on some path (compile error).
- Passing a `ref` variable that has not been initialized.
- Using `out`/`ref` when a return value or a tuple would be simpler.

### Why this matters for test automation

Playwright's C# API uses optional parameters and options objects heavily, and named arguments make calls readable (`timeout: 5000`). The `Try...` pattern with `out` is the standard way to say "this lookup may legitimately fail" without throwing exceptions in test helpers. You will see `TryGetValue` and `TryParse` constantly in test data and configuration code.

### Interview questions

1. **What is the difference between `out` and `ref`?** `ref` requires an initialized variable and may or may not be changed by the method; `out` need not be initialized by the caller but must be assigned by the method before it returns.
2. **What is a named argument and why use it?** Passing `name: value` so the order does not matter and the call documents itself.
3. **What is the `params` keyword?** It lets a method accept a variable number of arguments as an array.
4. **What is the Try pattern?** A method named `TryX` returns `bool` for success and gives the result via `out`, avoiding exceptions for expected failures.
5. **What is a pitfall of optional parameters?** The default is compiled into the caller; changing the default in a library does not affect already-compiled callers until they are rebuilt.

### Exercise

1. Write `static string BuildUrl(string host, string path = "/", int port = 443, bool secure = true)` that returns `https://host:443/path`. Call it four ways: only host; host and path; host with `secure: false`; all arguments by name in a different order.
2. Write `static bool TryDivide(int a, int b, out double result)` that returns false (and sets result to 0) when `b` is 0. Use it with `10, 4` and `10, 0`.
3. Write `static void Normalize(ref string text)` that trims the text and lowercases it. Call it with `"  Hello "` and print the result.
4. Write `static int Sum(params int[] numbers)` and call it with 0, 1 and 5 arguments.

### Check

The URL calls output sensible URLs (e.g. `http://example.com:443/` for the `secure: false` call). `TryDivide(10, 4, ...)` gives `True` and `2.5`; `TryDivide(10, 0, ...)` gives `False` and `0`. `Normalize` prints `hello`. `Sum()` is 0.

---

## Day 11: Scope, recursion basics

### Scope

The **scope** of a name is the region of code where it can be used. In C#, a local variable exists from its declaration to the end of the block `{ }` it is declared in. Rules to remember:

- Inner blocks can see variables of outer blocks, not the other way around.
- A nested block may not declare a variable with the same name as an enclosing local (error CS0136), and two variables in the same block may not share a name (error CS0128). Sibling blocks, such as two separate `for` loops, can reuse names.
- A variable declared in a `for` initializer or a `foreach` header lives only in that loop.
- Declare a variable in the smallest scope that works, as close to first use as possible.
- **Fields** (variables declared directly in a class) have class-wide scope and live as long as the class or object. Locals live only for one call of a method.

```csharp
// Scope: where a name can be used. A variable lives in the innermost { } that declares it.
int outer = 1;

{
    int inner = 2;
    Console.WriteLine(outer + inner);   // both visible here
}

// Console.WriteLine(inner);            // compile error: 'inner' does not exist in this scope

if (outer == 1)
{
    string message = "inside if";
    Console.WriteLine(message);
}
// message is gone here as well.

// C# does not allow a nested scope to reuse the name of a local from an enclosing scope:
// int outer = 5;       // error CS0128 (same scope), or CS0136 when declared inside a nested block

// Sibling scopes may reuse names:
for (int i = 0; i < 2; i++) { int temp = i * 10; Console.Write($"{temp} "); }
for (int i = 0; i < 2; i++) { int temp = i * 100; Console.Write($"{temp} "); }
Console.WriteLine();

// A variable declared before a loop keeps its value after the loop.
int lastSeen = -1;
foreach (int n in new[] { 4, 8, 15 })
{
    lastSeen = n;
}
Console.WriteLine(lastSeen);

// Variables in a method are separate from variables in other methods:
static void Demo()
{
    int outer = 100;                    // a different variable, not the one above
    Console.WriteLine(outer);
}

Demo();
Console.WriteLine(outer);

// Class-level state: a static field is shared by all calls.
Counter.Increment();
Counter.Increment();
Console.WriteLine(Counter.Value);

static class Counter
{
    public static int Value;            // field: scope is the whole class

    public static void Increment()
    {
        int step = 1;                   // local: scope is this method call only
        Value += step;
    }
}

// Output:
// 3
// inside if
// 0 10 0 100
// 15
// 100
// 1
// 2
```

### Lifetime

Each call to a method gets fresh locals. After the method returns, its locals are gone. A `static` field, by contrast, keeps its value between calls, which is why `Counter.Value` was 2 after two increments. Shared mutable static state is a common cause of flaky tests: when tests run in parallel, they all see the same field.

### Recursion

A **recursive** method calls itself. Every correct recursive method has:

1. A **base case** that returns without recursing, so the calls stop.
2. A **recursive case** that calls itself with a *smaller* version of the problem, moving toward the base case.

Think of it as: "to solve for n, solve for n - 1 and combine". Each call is placed on the **call stack**. In the countdown example, the "enter" lines print on the way down and the "leave" lines on the way back up, after the deeper calls have finished.

```csharp
Console.WriteLine(Recursion.Factorial(5));
Console.WriteLine(Recursion.Factorial(0));
Console.WriteLine(Recursion.SumDigits(4821));
Console.WriteLine(Recursion.Power(2, 10));
Console.WriteLine(Recursion.Reverse("abc"));
Console.WriteLine(Recursion.Fibonacci(10));

// Watch the calls happen: each call waits for the one below it.
Recursion.Countdown(3);

static class Recursion
{
    // Base case: stop. Recursive case: call yourself with a SMALLER problem.
    public static long Factorial(int n)
    {
        if (n <= 1) return 1;                  // base case
        return n * Factorial(n - 1);           // recursive case
    }

    public static int SumDigits(int n)
    {
        if (n < 10) return n;
        return n % 10 + SumDigits(n / 10);
    }

    public static long Power(int baseValue, int exponent)
    {
        if (exponent == 0) return 1;
        return baseValue * Power(baseValue, exponent - 1);
    }

    public static string Reverse(string s)
    {
        if (s.Length <= 1) return s;
        return Reverse(s.Substring(1)) + s[0];
    }

    // Two recursive calls per step: correct but slow for big n (the same work repeats).
    public static int Fibonacci(int n)
    {
        if (n < 2) return n;
        return Fibonacci(n - 1) + Fibonacci(n - 2);
    }

    public static void Countdown(int n)
    {
        if (n == 0)
        {
            Console.WriteLine("liftoff");
            return;
        }

        Console.WriteLine($"enter {n}");
        Countdown(n - 1);
        Console.WriteLine($"leave {n}");
    }
}

// Output:
// 120
// 1
// 15
// 1024
// cba
// 55
// enter 3
// enter 2
// enter 1
// liftoff
// leave 1
// leave 2
// leave 3
```

### Recursion on tree data

Recursion shines on tree-shaped data: folders containing folders, a page's element tree, nested JSON. The recursive structure of the code mirrors the structure of the data. (The example uses a `record` and an array of children; classes and records come in Phase 2. Focus on how `CountAll` works: one for this node plus the count of every child.)

```csharp
// Recursion fits naturally on tree-shaped data such as folders or a page's element tree.
var root = new Node("html",
[
    new Node("head", []),
    new Node("body",
    [
        new Node("form",
        [
            new Node("input", []),
            new Node("input", []),
            new Node("button", [])
        ]),
        new Node("footer", [])
    ])
]);

Console.WriteLine($"Total nodes: {root.CountAll()}");
Console.WriteLine($"Inputs: {root.CountByName("input")}");
root.Print(0);

// The same sum written with a loop: iteration is often simpler and cannot overflow the stack.
static long FactorialLoop(int n)
{
    long result = 1;
    for (int i = 2; i <= n; i++) result *= i;
    return result;
}
Console.WriteLine(FactorialLoop(5));

record Node(string Name, Node[] Children)
{
    public int CountAll()
    {
        int total = 1;                       // this node
        foreach (Node child in Children)
        {
            total += child.CountAll();       // plus everything below it
        }
        return total;
    }

    public int CountByName(string name)
    {
        int total = Name == name ? 1 : 0;
        foreach (Node child in Children)
        {
            total += child.CountByName(name);
        }
        return total;
    }

    public void Print(int depth)
    {
        Console.WriteLine($"{new string(' ', depth * 2)}{Name}");
        foreach (Node child in Children)
        {
            child.Print(depth + 1);
        }
    }
}

// Output:
// Total nodes: 8
// Inputs: 2
// html
//   head
//   body
//     form
//       input
//       input
//       button
//     footer
// 120
```

### The danger: stack overflow, and iteration as an alternative

Each call uses stack memory, which is limited. A recursive method with a missing or unreachable base case eventually crashes the process with a `StackOverflowException`, which you cannot catch. Very deep (but correct) recursion can do the same. Many recursive algorithms can be written with a loop, which is safe for large inputs (see `FactorialLoop` above). Prefer a loop when it is just as clear; use recursion when the data is naturally recursive.

The naive `Fibonacci` example recomputes the same values many times and becomes very slow for large `n`. Storing earlier results (memoization, usually with a dictionary, covered in Week 3) or using a loop fixes that.

```csharp
// Missing base case: this would never stop. DO NOT run it: the process crashes with a
// StackOverflowException, which cannot be caught with try/catch.
//
// static int Broken(int n) => Broken(n + 1);

// Safer: a depth guard turns an endless recursion into a normal, catchable error.
static int CountDown(int n, int depth = 0)
{
    if (depth > 1000)
    {
        throw new InvalidOperationException("Recursion too deep");
    }

    if (n == 0) return 0;
    return 1 + CountDown(n - 1, depth + 1);
}

Console.WriteLine(CountDown(10));

try
{
    CountDown(-1);          // n never reaches 0, so the guard fires
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);
}

// Output:
// 10
// Recursion too deep
```

### Common mistakes

- Forgetting the base case, or writing one that is never reached (for example recursing on `n + 1` while the base case waits for 0).
- Recursing without making the problem smaller.
- Trying to reuse a variable name inside a nested block and getting CS0136.
- Declaring a variable inside a loop body and using it after the loop.
- Using static fields as hidden shared state.

### Why this matters for test automation

Scope bugs show up as "variable does not exist in this context" and as accidental state sharing between tests. Recursion appears when you walk a tree: collecting all links under a menu, flattening nested JSON test data, or searching a folder of fixtures. Know the base case rule so a recursive helper never hangs a CI run.

### Interview questions

1. **What is scope in C#?** The region where a name is accessible; for locals it is the enclosing block from the declaration onward.
2. **Can an inner block declare a variable with the same name as an outer local?** No, the compiler reports error CS0136. Sibling blocks can reuse names.
3. **What are the two parts of every recursive method?** A base case that stops the recursion, and a recursive case that moves toward it.
4. **What is a stack overflow?** Running out of call-stack space, usually from infinite or very deep recursion. In .NET it terminates the process and cannot be caught.
5. **When would you choose iteration over recursion?** When the problem is a simple repetition or the depth can be large; recursion is better for naturally nested data.

### Exercise

1. Write a recursive `static int SumTo(int n)` that returns 1 + 2 + ... + n. Check `SumTo(10)` is 55.
2. Write a recursive `static bool IsPalindrome(string s)` that compares the first and last characters and recurses on the middle.
3. Write `static int CountDigits(int n)` recursively (use `n / 10`).
4. Write a loop-based `Fibonacci(int n)` and compare with the recursive version for `n = 10`.
5. Write a small program that tries to declare `int total` twice in the same block, read the error message, and fix it. Then declare it in an inner block while an outer `total` exists and read the second error.

### Check

`SumTo(10)` is 55, `IsPalindrome("level")` is true and `IsPalindrome("level up")` is false, `CountDigits(90210)` is 5, and both Fibonacci versions give 55. You can name both compile errors (CS0128 and CS0136).

---

## Day 12: Review and practice

### Recap

| Day | Topic | Key points |
|---|---|---|
| 7 | `if`/`else`, `switch` | conditions are `bool`; guard clauses; `switch` sections end with `break`/`return`; switch expressions with `=>`, relational and logical patterns, `_` discard |
| 8 | Loops | `for` (index), `foreach` (each item, no modifying the collection), `while` (zero or more), `do-while` (at least once); `break`, `continue`; bound polling loops |
| 9 | Methods | parameters vs arguments; return types; every path returns; pass by value; tuples for several results; overloads (not by return type; must be in a class) |
| 10 | Optional, named, `out`, `ref` | defaults after required parameters; `name: value`; `params`; `out` must be assigned; `ref` needs initialized variable; Try pattern |
| 11 | Scope and recursion | block scope; CS0128 and CS0136; fields vs locals; base case plus smaller problem; stack overflow; iteration alternative |

### Practice questions

Answer on paper first, then open the answers.

1. What does `for (int i = 0; i < 3; i++) Console.Write(i);` print?
2. How many times does `do { runs++; } while (false);` run its body?
3. What does `404 switch { >= 500 => "server", >= 400 => "client", _ => "other" }` produce?
4. Given `static int Sub(int a, int b) => a - b;`, what is `Sub(b: 1, a: 5)`?
5. After `int.TryParse("x9", out int parsed)`, what is `parsed`, and what does the call return?
6. What is `Fact(4)` for `static int Fact(int n) => n <= 1 ? 1 : n * Fact(n - 1);`?
7. For `static int Sum(params int[] values)`, what are `Sum(1, 2, 3)` and `Sum()`?
8. In `for (int i = 1; i <= 10; i++) { if (i % 2 == 1) continue; if (i > 8) break; evens++; }` what is `evens` at the end?
9. With `static void Bump(ref int c) => c += 10;` and `static void BumpCopy(int c) => c += 10;` and `int counter = 0;`, what is `counter` after `BumpCopy(counter);` and then after `Bump(ref counter);`?
10. Why does a recursive method need a base case, and why is `int x = 1; { int x = 2; }` an error?

<details>
<summary>Answers</summary>

1. `012`.
2. Once; `do-while` checks the condition after the body.
3. `"client"` (the first arm `>= 500` does not match, `>= 400` does).
4. `4` (named arguments: a is 5, b is 1).
5. The call returns `false`, and `parsed` is `0`.
6. `24`.
7. `6` and `0`.
8. `4` (counted 2, 4, 6, 8; at 10 the `break` fires before the increment).
9. `0` after `BumpCopy` (a copy was changed), then `10` after `Bump(ref counter)`.
10. Without a base case the calls never stop and the stack overflows. In `int x = 1; { int x = 2; }` the inner `x` clashes with the enclosing local (error CS0136).

The code below prints the answers to questions 1-9 in order:

```csharp
for (int i = 0; i < 3; i++) Console.Write(i);
Console.WriteLine();

int runs = 0;
do { runs++; } while (false);
Console.WriteLine(runs);

int code = 404;
string kind = code switch { >= 500 => "server", >= 400 => "client", _ => "other" };
Console.WriteLine(kind);

static int Sub(int a, int b) => a - b;
Console.WriteLine(Sub(b: 1, a: 5));

if (int.TryParse("x9", out int parsed)) Console.WriteLine("ok");
else Console.WriteLine($"failed, parsed={parsed}");

static int Fact(int n) => n <= 1 ? 1 : n * Fact(n - 1);
Console.WriteLine(Fact(4));

static int Sum(params int[] values)
{
    int total = 0;
    foreach (int v in values) total += v;
    return total;
}
Console.WriteLine(Sum(1, 2, 3));
Console.WriteLine(Sum());

int evens = 0;
for (int i = 1; i <= 10; i++)
{
    if (i % 2 == 1) continue;
    if (i > 8) break;
    evens++;
}
Console.WriteLine(evens);

int counter = 0;
static void Bump(ref int c) => c += 10;
static void BumpCopy(int c) => c += 10;
BumpCopy(counter);
Console.WriteLine(counter);
Bump(ref counter);
Console.WriteLine(counter);

// Output:
// 012
// 1
// client
// 4
// failed, parsed=0
// 24
// 6
// 0
// 4
// 0
// 10
```

</details>

### Mini-program: HTTP response analyzer

Write a program that works on `int[] statusCodes = { 200, 200, 404, 500, 201, 302, 503, 200, 429 };` and does the following:

1. A method `Classify(int code)` (switch expression) returns `success`, `redirect`, `client error`, `server error` or `unknown`.
2. A loop counts how many responses fall into each category and prints them in an aligned table.
3. A method `TryFindFirstServerError(int[] codes, out int index, out int code)` finds the first 5xx code and its position.
4. Print the failure rate (4xx plus 5xx) as a percentage with 1 decimal.
5. A recursive method `BackoffSeconds(int attempt, int maxSeconds = 60)` returns 1, 2, 4, 8 ... seconds (doubling), capped at `maxSeconds`. Print the delays for attempts 1 to 4 and the capped value for attempt 10 with `maxSeconds: 30`.

Try it yourself first.

<details>
<summary>Solution</summary>

```csharp
int[] statusCodes = { 200, 200, 404, 500, 201, 302, 503, 200, 429 };

int success = 0, redirects = 0, clientErrors = 0, serverErrors = 0;

foreach (int code in statusCodes)
{
    switch (Classify(code))
    {
        case "success": success++; break;
        case "redirect": redirects++; break;
        case "client error": clientErrors++; break;
        case "server error": serverErrors++; break;
    }
}

Console.WriteLine($"Responses: {statusCodes.Length}");
Console.WriteLine($"{"success",-14}{success,3}");
Console.WriteLine($"{"redirect",-14}{redirects,3}");
Console.WriteLine($"{"client error",-14}{clientErrors,3}");
Console.WriteLine($"{"server error",-14}{serverErrors,3}");

if (TryFindFirstServerError(statusCodes, out int position, out int badCode))
{
    Console.WriteLine($"First server error: {badCode} at position {position}");
}

double failureRate = (double)(clientErrors + serverErrors) / statusCodes.Length * 100;
Console.WriteLine($"Failure rate: {failureRate:F1}%");

for (int attempt = 1; attempt <= 4; attempt++)
{
    Console.WriteLine($"Retry {attempt}: wait {BackoffSeconds(attempt)}s");
}
Console.WriteLine($"Capped: wait {BackoffSeconds(10, maxSeconds: 30)}s");

static string Classify(int code) => code switch
{
    >= 200 and < 300 => "success",
    >= 300 and < 400 => "redirect",
    >= 400 and < 500 => "client error",
    >= 500 and < 600 => "server error",
    _ => "unknown"
};

static bool TryFindFirstServerError(int[] codes, out int index, out int code)
{
    for (int i = 0; i < codes.Length; i++)
    {
        if (codes[i] >= 500)
        {
            index = i;
            code = codes[i];
            return true;
        }
    }

    index = -1;
    code = 0;
    return false;
}

// 1, 2, 4, 8 ... seconds, written recursively; maxSeconds is an optional parameter.
static int BackoffSeconds(int attempt, int maxSeconds = 60)
{
    if (attempt <= 1) return 1;
    return Math.Min(BackoffSeconds(attempt - 1, maxSeconds) * 2, maxSeconds);
}

// Output:
// Responses: 9
// success         4
// redirect        1
// client error    2
// server error    2
// First server error: 500 at position 3
// Failure rate: 44.4%
// Retry 1: wait 1s
// Retry 2: wait 2s
// Retry 3: wait 4s
// Retry 4: wait 8s
// Capped: wait 30s
```

</details>

Extension: take the status codes from `args` (use `int.TryParse` and skip invalid values with a message), and return exit code 1 when any server error is found.

### Interview questions

1. **What is the difference between `foreach` and `for`, and when can't you use `foreach`?** `foreach` gives each element and no index; you cannot reassign the loop variable or add/remove items of the collection while iterating. Use `for` when you need an index or in-place changes.
2. **Explain `out`, `ref` and a normal parameter.** A normal parameter is a copy; `ref` aliases an initialized caller variable; `out` aliases a variable the method must assign before returning.
3. **What is the difference between overloading and optional parameters?** Overloads are separate methods chosen at compile time; optional parameters are one method whose defaults are filled in by the compiler at the call site. Overloads are more robust across library versions.
4. **How does a recursive method work and what can go wrong?** It calls itself on a smaller problem until a base case; without a base case or with too much depth the stack overflows and the process ends.
5. **Why is `switch` with `when` or a switch expression often better than a chain of `if`/`else if`?** It states the relationship between one value and many cases clearly, and the compiler can warn about missing cases.
6. **What does it mean that C# passes arguments by value for reference types?** The reference itself is copied. The callee can change the object, but assigning the parameter to a new object does not affect the caller's variable.
7. **What is the Try pattern and why use it?** `bool TryX(..., out T result)` reports expected failure through the return value instead of an exception, which is cheaper and clearer.
8. **What is scope, and why does C# reject a nested block declaring the same name as an outer local?** Scope is where a name is visible; the rule avoids confusion between two different variables with the same name in overlapping regions.
