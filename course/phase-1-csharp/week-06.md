# Week 6: LINQ and Exceptions

This week covers two skills that appear in nearly every test project. LINQ (Language Integrated Query) lets you filter, transform, sort, group, join and inspect collections in a few readable lines, which is how you will process test data, API responses and lists of page elements. Exceptions are how .NET reports that something went wrong; you will learn to catch, rethrow, define your own, and release resources reliably.

All examples are complete programs. Create a console project with `dotnet new console -o Week6`, replace `Program.cs` with an example and run `dotnet run`. The project targets `net10.0` with implicit usings (which include `System.Linq`) and nullable reference types enabled.

---

## Day 31: LINQ basics: `Where`, `Select`

### What LINQ is

LINQ is a set of methods (and a query syntax) for working with any sequence of items: arrays, lists, dictionaries, results of other queries. The methods are extension methods (Week 5, Day 29) on `IEnumerable<T>`, living in the `System.Linq` namespace. Implicit usings already imports it in the default console template.

Two operations do most of the work:

- `Where` keeps the items that satisfy a condition (filtering).
- `Select` turns each item into something else (projection, also called mapping).

### A first look at lambdas

Both methods take a small function written as a lambda expression: `parameter => expression`. Read `n => n > 2` as "given n, produce `n > 2`". Lambdas get a full treatment on Day 37; for now this reading is enough.

```csharp
int[] numbers = { 1, 2, 3, 4, 5, 6 };

var big = numbers.Where(n => n > 3);
var doubled = numbers.Select(n => n * 2);
var bigDoubled = numbers.Where(n => n > 3).Select(n => n * 2);

Console.WriteLine(string.Join(", ", big));
Console.WriteLine(string.Join(", ", doubled));
Console.WriteLine(string.Join(", ", bigDoubled));

// Output:
// 4, 5, 6
// 2, 4, 6, 8, 10, 12
// 8, 10, 12
```

The original array is never changed. Each call returns a new sequence. Chaining reads left to right: filter, then transform.

### Working with objects

Most real queries run over objects. These examples use a record for test cases.

```csharp
var tests = TestData.All();

var failed = tests.Where(t => !t.Passed);
foreach (var t in failed)
{
    Console.WriteLine($"FAILED: {t.Name}");
}

var slowNames = tests
    .Where(t => t.DurationMs > 1000)
    .Select(t => t.Name);
Console.WriteLine(string.Join(", ", slowNames));

var seconds = tests.Select(t => t.DurationMs / 1000.0);
Console.WriteLine(string.Join(" ", seconds.Select(s => s.ToString("0.0"))));

record TestCase(string Name, string Area, int DurationMs, bool Passed);

static class TestData
{
    public static List<TestCase> All() => new()
    {
        new("Login_Succeeds", "Auth", 820, true),
        new("Login_BadPassword", "Auth", 640, true),
        new("Logout_ClearsSession", "Auth", 450, false),
        new("Search_FindsItem", "Search", 1300, true),
        new("Search_EmptyQuery", "Search", 300, true),
        new("Checkout_AppliesCoupon", "Checkout", 2310, false),
        new("Checkout_PaysByCard", "Checkout", 3100, true),
    };
}

// Output:
// FAILED: Logout_ClearsSession
// FAILED: Checkout_AppliesCoupon
// Search_FindsItem, Checkout_AppliesCoupon, Checkout_PaysByCard
// 0.8 0.6 0.5 1.3 0.3 2.3 3.1
```

`TestData.All()` is reused in later examples; paste the record and the `TestData` class into each program that needs it.

### Projecting to new shapes

`Select` can return a different type: a single property, a formatted string, a tuple, a new record, or an anonymous type (an unnamed class created on the spot with `new { ... }`).

```csharp
var tests = TestData.All();

var shortInfo = tests
    .Where(t => t.Area == "Auth")
    .Select(t => new { t.Name, Seconds = t.DurationMs / 1000.0 });

foreach (var item in shortInfo)
{
    Console.WriteLine($"{item.Name}: {item.Seconds}s");
}

var labels = tests
    .Select(t => (t.Name, Status: t.Passed ? "pass" : "FAIL"))
    .Where(x => x.Status == "FAIL");

foreach (var (name, status) in labels)
{
    Console.WriteLine($"{status} {name}");
}

var summaries = tests.Take(2).Select(t => new Summary(t.Name, t.Passed));
foreach (var s in summaries)
{
    Console.WriteLine(s);
}

record Summary(string Name, bool Passed);

record TestCase(string Name, string Area, int DurationMs, bool Passed);

static class TestData
{
    public static List<TestCase> All() => new()
    {
        new("Login_Succeeds", "Auth", 820, true),
        new("Login_BadPassword", "Auth", 640, true),
        new("Logout_ClearsSession", "Auth", 450, false),
        new("Search_FindsItem", "Search", 1300, true),
        new("Search_EmptyQuery", "Search", 300, true),
        new("Checkout_AppliesCoupon", "Checkout", 2310, false),
        new("Checkout_PaysByCard", "Checkout", 3100, true),
    };
}

// Output:
// Login_Succeeds: 0.82s
// Login_BadPassword: 0.64s
// Logout_ClearsSession: 0.45s
// FAIL Logout_ClearsSession
// FAIL Checkout_AppliesCoupon
// Summary { Name = Login_Succeeds, Passed = True }
// Summary { Name = Login_BadPassword, Passed = True }
```

### Other everyday operators

```csharp
var tests = TestData.All();

Console.WriteLine(tests.Count());
Console.WriteLine(tests.Count(t => t.Passed));
Console.WriteLine(tests.Sum(t => t.DurationMs));
Console.WriteLine(tests.Average(t => t.DurationMs).ToString("0.0"));
Console.WriteLine(tests.Max(t => t.DurationMs));
Console.WriteLine(tests.Min(t => t.DurationMs));

Console.WriteLine(string.Join(", ", tests.Select(t => t.Area).Distinct()));
Console.WriteLine(string.Join(", ", tests.Skip(5).Select(t => t.Name)));
Console.WriteLine(string.Join(", ", tests.Take(2).Select(t => t.Name)));

var indexed = tests.Take(3).Select((t, i) => $"{i + 1}. {t.Name}");
Console.WriteLine(string.Join(" | ", indexed));

record TestCase(string Name, string Area, int DurationMs, bool Passed);

static class TestData
{
    public static List<TestCase> All() => new()
    {
        new("Login_Succeeds", "Auth", 820, true),
        new("Login_BadPassword", "Auth", 640, true),
        new("Logout_ClearsSession", "Auth", 450, false),
        new("Search_FindsItem", "Search", 1300, true),
        new("Search_EmptyQuery", "Search", 300, true),
        new("Checkout_AppliesCoupon", "Checkout", 2310, false),
        new("Checkout_PaysByCard", "Checkout", 3100, true),
    };
}

// Output:
// 7
// 5
// 8920
// 1274.3
// 3100
// 300
// Auth, Search, Checkout
// Checkout_AppliesCoupon, Checkout_PaysByCard
// Login_Succeeds, Login_BadPassword
// 1. Login_Succeeds | 2. Login_BadPassword | 3. Logout_ClearsSession
```

### Flattening with `SelectMany`

`Select` on a sequence of lists gives a sequence of lists. `SelectMany` flattens them into one sequence.

```csharp
var suites = new List<Suite>
{
    new("Auth", new[] { "Login", "Logout" }),
    new("Search", new[] { "Find", "Empty", "Paging" }),
};

var nested = suites.Select(s => s.Tests);
Console.WriteLine(nested.Count());

var flat = suites.SelectMany(s => s.Tests);
Console.WriteLine(flat.Count());
Console.WriteLine(string.Join(", ", flat));

var qualified = suites.SelectMany(s => s.Tests, (s, test) => $"{s.Name}.{test}");
Console.WriteLine(string.Join(", ", qualified));

record Suite(string Name, string[] Tests);

// Output:
// 2
// 5
// Login, Logout, Find, Empty, Paging
// Auth.Login, Auth.Logout, Search.Find, Search.Empty, Search.Paging
```

### Making a result concrete: `ToList`, `ToArray`, `ToDictionary`

```csharp
var tests = TestData.All();

List<string> names = tests.Where(t => t.Passed).Select(t => t.Name).ToList();
string[] areas = tests.Select(t => t.Area).Distinct().ToArray();
Dictionary<string, int> durations = tests.ToDictionary(t => t.Name, t => t.DurationMs);

Console.WriteLine(names.Count);
Console.WriteLine(areas.Length);
Console.WriteLine(durations["Search_FindsItem"]);

record TestCase(string Name, string Area, int DurationMs, bool Passed);

static class TestData
{
    public static List<TestCase> All() => new()
    {
        new("Login_Succeeds", "Auth", 820, true),
        new("Login_BadPassword", "Auth", 640, true),
        new("Logout_ClearsSession", "Auth", 450, false),
        new("Search_FindsItem", "Search", 1300, true),
        new("Search_EmptyQuery", "Search", 300, true),
        new("Checkout_AppliesCoupon", "Checkout", 2310, false),
        new("Checkout_PaysByCard", "Checkout", 3100, true),
    };
}

// Output:
// 5
// 3
// 1300
```

`ToDictionary` throws if two items produce the same key. Why these conversion methods matter leads directly to the next topic.

### Deferred execution

`Where`, `Select` and most other LINQ operators do not run when you call them. They build a description of the query. The work happens when you enumerate the result: with `foreach`, `ToList()`, `Count()`, `First()` and similar. This is called deferred (or lazy) execution.

```csharp
var numbers = new List<int> { 1, 2, 3 };

var query = numbers.Where(n =>
{
    Console.WriteLine($"  checking {n}");
    return n > 1;
});

Console.WriteLine("Query created, nothing checked yet");

numbers.Add(4);   // changes the source BEFORE the query runs

Console.WriteLine("First enumeration:");
foreach (var n in query)
{
    Console.WriteLine($"  got {n}");
}

Console.WriteLine("Second enumeration:");
Console.WriteLine($"  count = {query.Count()}");

// Output:
// Query created, nothing checked yet
// First enumeration:
//   checking 1
//   checking 2
//   got 2
//   checking 3
//   got 3
//   checking 4
//   got 4
// Second enumeration:
//   checking 1
//   checking 2
//   checking 3
//   checking 4
//   count = 3
```

Three consequences you must remember:

1. The query sees the data as it is when it runs, not when it was written. Item `4` was added after the query was created and still appears.
2. Each enumeration re-runs the whole query. If the query is expensive (for example it calls a service in a `Select`), enumerating twice does the work twice.
3. Items flow through one at a time: "checking 2" and "got 2" are interleaved, not "check everything, then deliver everything".

To run the query once and keep a snapshot, materialise it with `ToList()` or `ToArray()`:

```csharp
var numbers = new List<int> { 1, 2, 3 };

var lazy = numbers.Where(n => n > 1);
var snapshot = numbers.Where(n => n > 1).ToList();

numbers.Add(4);

Console.WriteLine($"lazy:     {string.Join(", ", lazy)}");
Console.WriteLine($"snapshot: {string.Join(", ", snapshot)}");

// Output:
// lazy:     2, 3, 4
// snapshot: 2, 3
```

Operators such as `Count`, `Sum`, `ToList`, `ToArray`, `ToDictionary`, `First` and `Any` execute immediately. Modifying a list while a query over it is mid-`foreach` throws an `InvalidOperationException`.

### Method syntax and query syntax

LINQ has a second, SQL-like syntax. Both compile to the same method calls. Most C# test code uses method syntax, which is what this course uses.

```csharp
int[] numbers = { 5, 12, 8, 3, 20 };

var methodSyntax = numbers.Where(n => n > 4).Select(n => n * 10);

var querySyntax = from n in numbers
                  where n > 4
                  select n * 10;

Console.WriteLine(string.Join(", ", methodSyntax));
Console.WriteLine(string.Join(", ", querySyntax));

// Output:
// 50, 120, 80, 200
// 50, 120, 80, 200
```

### Common mistakes

- Assuming `Where` runs immediately and being surprised when the data changes before enumeration.
- Enumerating an expensive query several times instead of calling `ToList()` once.
- Using `Select` when you wanted `Where`. `Select(t => t.Passed)` gives booleans; it does not filter.
- Forgetting that `Where`/`Select` return a new sequence; statements like `list.Where(...)` on their own do nothing useful.

### Why this matters for test automation

You will constantly turn raw data into the form a test needs: get texts of all table rows (`rows.Select(r => r.Name)`), filter users by role, convert API models into expected UI strings, or find the failed items in a result list. Understanding deferred execution explains confusing behaviour when a query runs against a list that tests are modifying, and when a "query" calls the browser or network repeatedly.

### Interview questions

**Q1. What is LINQ?** A set of query operators (mostly extension methods on `IEnumerable<T>`) for filtering, projecting, ordering, grouping and aggregating sequences, with an optional SQL-like syntax.

**Q2. Difference between `Where` and `Select`?** `Where` filters items by a condition and keeps the same type; `Select` transforms each item into a new value or type.

**Q3. What is deferred execution?** Query operators like `Where` and `Select` only build the query; it runs each time the result is enumerated. Operators like `ToList`, `Count` and `First` force execution.

**Q4. How do you avoid executing a query repeatedly?** Materialise the result with `ToList()` or `ToArray()` once and use that.

**Q5. `Select` vs `SelectMany`?** `Select` maps each item to one result (possibly a collection). `SelectMany` maps each item to a collection and flattens all collections into one sequence.

### Exercise

1. Using `TestData.All()`, list the names of Auth tests that passed.
2. Produce the total duration in seconds (as a `double`) of all tests in the `Checkout` area.
3. Create a list of strings in the form `"1. Login_Succeeds (0.82s)"` for the three slowest tests. (You may use `OrderByDescending(t => t.DurationMs)`; sorting is formally covered tomorrow.)
4. Demonstrate deferred execution of your own: build a query over a `List<string>`, add an item, then enumerate it and show that the new item appears.

### Check

- Which of `Count()`, `Where()`, `ToList()`, `Select()` run immediately?
- In `Select((t, i) => ...)`, what does `i` hold?
- If a `Select` calls a slow method and you enumerate the result twice, how many times does the slow method run per item?

---

## Day 32: `OrderBy`, `GroupBy`, `Join`

### Sorting: `OrderBy`, `ThenBy`

`OrderBy` sorts ascending by a key you choose; `OrderByDescending` sorts descending. `ThenBy` and `ThenByDescending` add tie-breakers. The sort is stable: items with equal keys keep their original relative order. Like `Where`, sorting is deferred.

```csharp
var tests = TestData.All();

Console.WriteLine("By duration, fastest first:");
foreach (var t in tests.OrderBy(t => t.DurationMs))
{
    Console.WriteLine($"  {t.DurationMs,5} {t.Name}");
}

Console.WriteLine("By area, then slowest first:");
foreach (var t in tests.OrderBy(t => t.Area).ThenByDescending(t => t.DurationMs))
{
    Console.WriteLine($"  {t.Area,-9} {t.DurationMs,5} {t.Name}");
}

record TestCase(string Name, string Area, int DurationMs, bool Passed);

static class TestData
{
    public static List<TestCase> All() => new()
    {
        new("Login_Succeeds", "Auth", 820, true),
        new("Login_BadPassword", "Auth", 640, true),
        new("Logout_ClearsSession", "Auth", 450, false),
        new("Search_FindsItem", "Search", 1300, true),
        new("Search_EmptyQuery", "Search", 300, true),
        new("Checkout_AppliesCoupon", "Checkout", 2310, false),
        new("Checkout_PaysByCard", "Checkout", 3100, true),
    };
}

// Output:
// By duration, fastest first:
//     300 Search_EmptyQuery
//     450 Logout_ClearsSession
//     640 Login_BadPassword
//     820 Login_Succeeds
//    1300 Search_FindsItem
//    2310 Checkout_AppliesCoupon
//    3100 Checkout_PaysByCard
// By area, then slowest first:
//   Auth        820 Login_Succeeds
//   Auth        640 Login_BadPassword
//   Auth        450 Logout_ClearsSession
//   Checkout   3100 Checkout_PaysByCard
//   Checkout   2310 Checkout_AppliesCoupon
//   Search     1300 Search_FindsItem
//   Search      300 Search_EmptyQuery
```

A common bug: calling `OrderBy` twice in a row. The second call re-sorts everything, discarding the first order. Use `OrderBy(...).ThenBy(...)` for multi-key sorts.

`OrderBy` on strings uses the current culture's comparison by default. For deterministic ordering regardless of machine settings you can pass `StringComparer.Ordinal`: `names.OrderBy(n => n, StringComparer.Ordinal)`.

`OrderBy` returns an `IOrderedEnumerable<T>`; the original list is untouched. If you want the list itself sorted in place, use `List<T>.Sort()`.

### Grouping: `GroupBy`

`GroupBy` splits a sequence into groups that share a key. Each group is an `IGrouping<TKey, TElement>`: it has a `Key` property and is itself a sequence of the group's items. Groups appear in the order in which their keys first occur in the source.

```csharp
var tests = TestData.All();

var byArea = tests.GroupBy(t => t.Area);

foreach (var group in byArea)
{
    Console.WriteLine($"{group.Key}: {group.Count()} test(s)");
    foreach (var t in group)
    {
        Console.WriteLine($"    {t.Name}");
    }
}

record TestCase(string Name, string Area, int DurationMs, bool Passed);

static class TestData
{
    public static List<TestCase> All() => new()
    {
        new("Login_Succeeds", "Auth", 820, true),
        new("Login_BadPassword", "Auth", 640, true),
        new("Logout_ClearsSession", "Auth", 450, false),
        new("Search_FindsItem", "Search", 1300, true),
        new("Search_EmptyQuery", "Search", 300, true),
        new("Checkout_AppliesCoupon", "Checkout", 2310, false),
        new("Checkout_PaysByCard", "Checkout", 3100, true),
    };
}

// Output:
// Auth: 3 test(s)
//     Login_Succeeds
//     Login_BadPassword
//     Logout_ClearsSession
// Search: 2 test(s)
//     Search_FindsItem
//     Search_EmptyQuery
// Checkout: 2 test(s)
//     Checkout_AppliesCoupon
//     Checkout_PaysByCard
```

A typical use is to project each group into a summary with `Select`:

```csharp
var tests = TestData.All();

var report = tests
    .GroupBy(t => t.Area)
    .Select(g => new
    {
        Area = g.Key,
        Total = g.Count(),
        Failed = g.Count(t => !t.Passed),
        TotalMs = g.Sum(t => t.DurationMs),
        Slowest = g.MaxBy(t => t.DurationMs)!.Name,
    })
    .OrderByDescending(r => r.TotalMs);

Console.WriteLine("Area       Total Failed  TotalMs Slowest");
foreach (var r in report)
{
    Console.WriteLine($"{r.Area,-10} {r.Total,5} {r.Failed,6} {r.TotalMs,8} {r.Slowest}");
}

record TestCase(string Name, string Area, int DurationMs, bool Passed);

static class TestData
{
    public static List<TestCase> All() => new()
    {
        new("Login_Succeeds", "Auth", 820, true),
        new("Login_BadPassword", "Auth", 640, true),
        new("Logout_ClearsSession", "Auth", 450, false),
        new("Search_FindsItem", "Search", 1300, true),
        new("Search_EmptyQuery", "Search", 300, true),
        new("Checkout_AppliesCoupon", "Checkout", 2310, false),
        new("Checkout_PaysByCard", "Checkout", 3100, true),
    };
}

// Output:
// Area       Total Failed  TotalMs Slowest
// Checkout       2      1     5410 Checkout_PaysByCard
// Auth           3      1     1910 Login_Succeeds
// Search         2      0     1600 Search_FindsItem
```

`MaxBy` (available since .NET 6) returns the item with the largest key, and `null` for an empty sequence of reference types, hence the `!`. Here a group is never empty.

Related tools: `ToLookup` groups immediately (not deferred) and gives a dictionary-like object you can index by key; `ToDictionary` requires unique keys; `GroupBy` does not.

```csharp
var tests = TestData.All();

var lookup = tests.ToLookup(t => t.Area);

Console.WriteLine(lookup["Auth"].Count());
Console.WriteLine(lookup["Missing"].Count());   // an empty group, not an exception
Console.WriteLine(lookup.Count);                // number of keys

record TestCase(string Name, string Area, int DurationMs, bool Passed);

static class TestData
{
    public static List<TestCase> All() => new()
    {
        new("Login_Succeeds", "Auth", 820, true),
        new("Login_BadPassword", "Auth", 640, true),
        new("Logout_ClearsSession", "Auth", 450, false),
        new("Search_FindsItem", "Search", 1300, true),
        new("Search_EmptyQuery", "Search", 300, true),
        new("Checkout_AppliesCoupon", "Checkout", 2310, false),
        new("Checkout_PaysByCard", "Checkout", 3100, true),
    };
}

// Output:
// 3
// 0
// 3
```

### Joining two sequences: `Join`

`Join` combines two sequences by matching a key from each, like a SQL inner join. Items without a match on the other side are dropped. The parameters are: the other sequence, a key selector for the left items, a key selector for the right items, and a function that builds the result from a matching pair.

Worked example: test cases carry an owner id, and a separate list holds the owners.

```csharp
var owners = new List<Owner>
{
    new(1, "Ana", "Auth team"),
    new(2, "Bob", "Search team"),
    new(3, "Cy", "Payments team"),
};

var tests = new List<OwnedTest>
{
    new("Login_Succeeds", 1, true),
    new("Logout_ClearsSession", 1, false),
    new("Search_FindsItem", 2, true),
    new("Checkout_PaysByCard", 3, true),
    new("Orphan_NoOwner", 99, true),
};

var joined = tests.Join(
    owners,
    t => t.OwnerId,
    o => o.Id,
    (t, o) => new { t.Name, Owner = o.Name, o.Team, t.Passed });

foreach (var row in joined)
{
    Console.WriteLine($"{row.Name,-22} {row.Owner,-4} {row.Team,-14} {(row.Passed ? "pass" : "FAIL")}");
}

Console.WriteLine($"Rows joined: {joined.Count()} of {tests.Count}");

record Owner(int Id, string Name, string Team);

record OwnedTest(string Name, int OwnerId, bool Passed);

// Output:
// Login_Succeeds         Ana  Auth team      pass
// Logout_ClearsSession   Ana  Auth team      FAIL
// Search_FindsItem       Bob  Search team    pass
// Checkout_PaysByCard    Cy   Payments team  pass
// Rows joined: 4 of 5
```

`Orphan_NoOwner` has no matching owner, so the inner join drops it.

### `GroupJoin` and left joins

`GroupJoin` pairs each left item with the collection of all matching right items (possibly empty). Combined with `DefaultIfEmpty` or an explicit check, it expresses a left join that keeps unmatched items.

```csharp
var owners = new List<Owner>
{
    new(1, "Ana"),
    new(2, "Bob"),
    new(3, "Cy"),
};

var tests = new List<OwnedTest>
{
    new("Login_Succeeds", 1),
    new("Logout_ClearsSession", 1),
    new("Search_FindsItem", 2),
};

var perOwner = owners.GroupJoin(
    tests,
    o => o.Id,
    t => t.OwnerId,
    (o, ownedTests) => new { o.Name, Tests = ownedTests.Select(t => t.Name).ToList() });

foreach (var row in perOwner)
{
    string list = row.Tests.Count == 0 ? "(none)" : string.Join(", ", row.Tests);
    Console.WriteLine($"{row.Name}: {list}");
}

record Owner(int Id, string Name);

record OwnedTest(string Name, int OwnerId);

// Output:
// Ana: Login_Succeeds, Logout_ClearsSession
// Bob: Search_FindsItem
// Cy: (none)
```

### Putting it together: join, group, sort

This pipeline joins tests to owners, groups by team, summarises and sorts. Read it step by step and match each stage to the output.

```csharp
var owners = new List<Owner>
{
    new(1, "Ana", "Auth team"),
    new(2, "Bob", "Search team"),
    new(3, "Cy", "Payments team"),
};

var tests = new List<OwnedTest>
{
    new("Login_Succeeds", 1, 820, true),
    new("Login_BadPassword", 1, 640, true),
    new("Logout_ClearsSession", 1, 450, false),
    new("Search_FindsItem", 2, 1300, true),
    new("Search_EmptyQuery", 2, 300, true),
    new("Checkout_AppliesCoupon", 3, 2310, false),
    new("Checkout_PaysByCard", 3, 3100, true),
};

var report = tests
    .Join(owners, t => t.OwnerId, o => o.Id, (t, o) => new { o.Team, t.Name, t.DurationMs, t.Passed })
    .GroupBy(x => x.Team)
    .Select(g => new
    {
        Team = g.Key,
        PassRate = 100.0 * g.Count(x => x.Passed) / g.Count(),
        AvgMs = g.Average(x => x.DurationMs),
        Failing = g.Where(x => !x.Passed).Select(x => x.Name).ToList(),
    })
    .OrderBy(r => r.PassRate)
    .ThenBy(r => r.Team);

foreach (var r in report)
{
    string failing = r.Failing.Count == 0 ? "-" : string.Join(", ", r.Failing);
    Console.WriteLine($"{r.Team,-14} pass {r.PassRate,5:0.0}%  avg {r.AvgMs,7:0.0} ms  failing: {failing}");
}

record Owner(int Id, string Name, string Team);

record OwnedTest(string Name, int OwnerId, int DurationMs, bool Passed);

// Output:
// Payments team  pass  50.0%  avg  2705.0 ms  failing: Checkout_AppliesCoupon
// Auth team      pass  66.7%  avg   636.7 ms  failing: Logout_ClearsSession
// Search team    pass 100.0%  avg   800.0 ms  failing: -
```

The report is sorted by pass rate ascending, so the team with the lowest pass rate is listed first.

### Deferred execution again

`OrderBy`, `GroupBy` and `Join` are deferred, but when you start enumerating they must read the whole source before returning the first item (a sort cannot emit anything until it has seen every element). Compare with `Where`, which streams item by item.

```csharp
var numbers = new[] { 3, 1, 2 };

var sorted = numbers.Select(n =>
{
    Console.WriteLine($"  reading {n}");
    return n;
}).OrderBy(n => n);

Console.WriteLine("Query built");

foreach (var n in sorted)
{
    Console.WriteLine($"got {n}");
}

// Output:
// Query built
//   reading 3
//   reading 1
//   reading 2
// got 1
// got 2
// got 3
```

### Common mistakes

- Calling `OrderBy` twice instead of `OrderBy` then `ThenBy`.
- Using `ToDictionary` when keys may repeat (it throws `ArgumentException`); use `GroupBy` or `ToLookup`.
- Assuming `Join` keeps unmatched items. It is an inner join.
- Forgetting that `GroupBy` returns groups; iterating a group yields the items, and `Key` is the grouping value.
- Mixing up the order of the key selectors in `Join` (left key first, then right key).

### Why this matters for test automation

Test reports are groupings (by area, status, browser). Test data setup is often a join (users with their roles, orders with customers). Verifying that a UI list is sorted correctly is `Assert.Equal(expected.OrderBy(x => x), actual)`. Grouping duplicates (`GroupBy(x => x).Where(g => g.Count() > 1)`) finds repeated rows in a table.

### Interview questions

**Q1. How do you sort by two criteria?** `OrderBy(a).ThenBy(b)`. Calling `OrderBy` twice discards the first sort.

**Q2. What does `GroupBy` return?** A sequence of `IGrouping<TKey, TElement>`; each has a `Key` and enumerates its items. Groups appear in order of first occurrence of the key.

**Q3. What is the difference between `Join` and `GroupJoin`?** `Join` is an inner join producing one result per matching pair. `GroupJoin` produces one result per left item with the collection of its matches, which may be empty; it is the basis of left joins.

**Q4. Is `OrderBy` stable?** Yes. Items with equal keys keep their original relative order.

**Q5. `GroupBy` vs `ToDictionary` vs `ToLookup`?** `GroupBy` is deferred and returns groups. `ToLookup` groups immediately into a lookup with an indexer by key. `ToDictionary` needs unique keys and maps each key to one value.

### Exercise

1. Print all tests ordered by `Passed` (failures first), then by name.
2. Group tests by whether they are fast (`DurationMs < 1000`) or slow, and print the count in each group.
3. Using the owners and tests from the worked example, print each team with the names of its tests, using `GroupJoin` or `Join` plus `GroupBy`.
4. Find duplicate names in `string[] names = { "a", "b", "a", "c", "b", "a" }` and print each duplicate with its count.

### Check

- What is the type of an element returned by `GroupBy`, and what are its members?
- What happens to a test with an owner id that is not in the owners list when using `Join`?
- Why does the "reading" output in the last example appear before the first "got"?

---

## Day 33: `Any`, `First`, `Single`, and their `OrDefault` forms

### Questions about a sequence

Some LINQ methods do not return a sequence; they answer a question or pick one element. They execute immediately.

| Method | Returns | Notes |
|---|---|---|
| `Any()` / `Any(predicate)` | `bool` | At least one item (or one matching)? Stops at the first match. |
| `All(predicate)` | `bool` | Do all items match? `true` for an empty sequence. |
| `Contains(value)` | `bool` | Is the value in the sequence? |
| `Count()` / `Count(predicate)` | `int` | Number of items. |

```csharp
var tests = TestData.All();
var empty = new List<TestCase>();

Console.WriteLine(tests.Any());
Console.WriteLine(empty.Any());
Console.WriteLine(tests.Any(t => !t.Passed));
Console.WriteLine(tests.All(t => t.DurationMs < 5000));
Console.WriteLine(tests.All(t => t.Passed));
Console.WriteLine(empty.All(t => t.Passed));   // true: nothing violates the rule
Console.WriteLine(tests.Select(t => t.Area).Contains("Search"));

record TestCase(string Name, string Area, int DurationMs, bool Passed);

static class TestData
{
    public static List<TestCase> All() => new()
    {
        new("Login_Succeeds", "Auth", 820, true),
        new("Login_BadPassword", "Auth", 640, true),
        new("Logout_ClearsSession", "Auth", 450, false),
        new("Search_FindsItem", "Search", 1300, true),
        new("Search_EmptyQuery", "Search", 300, true),
        new("Checkout_AppliesCoupon", "Checkout", 2310, false),
        new("Checkout_PaysByCard", "Checkout", 3100, true),
    };
}

// Output:
// True
// False
// True
// True
// False
// True
// True
```

Prefer `Any()` over `Count() > 0`. `Any()` can stop after the first element; `Count()` on a general sequence must walk everything. (For a `List<T>`, `Count()` is fast, but `Any()` expresses the intent and is safe on every sequence.)

### Picking one element

| Method | Empty sequence | One match | Several matches |
|---|---|---|---|
| `First()` | throws | that item | the first |
| `FirstOrDefault()` | `default` | that item | the first |
| `Single()` | throws | that item | throws |
| `SingleOrDefault()` | `default` | that item | throws |
| `Last()` / `LastOrDefault()` | throws / `default` | that item | the last |

All of these have an overload that takes a predicate: `First(t => t.Name == "x")`. `default` means `null` for reference types, `0` for `int`, `false` for `bool`, and so on.

```csharp
var tests = TestData.All();

var firstFailed = tests.First(t => !t.Passed);
Console.WriteLine(firstFailed.Name);

var lastAuth = tests.Last(t => t.Area == "Auth");
Console.WriteLine(lastAuth.Name);

var unique = tests.Single(t => t.Name == "Search_FindsItem");
Console.WriteLine(unique.DurationMs);

var missing = tests.FirstOrDefault(t => t.Area == "Admin");
Console.WriteLine(missing is null);
Console.WriteLine(missing?.Name ?? "no admin tests");

record TestCase(string Name, string Area, int DurationMs, bool Passed);

static class TestData
{
    public static List<TestCase> All() => new()
    {
        new("Login_Succeeds", "Auth", 820, true),
        new("Login_BadPassword", "Auth", 640, true),
        new("Logout_ClearsSession", "Auth", 450, false),
        new("Search_FindsItem", "Search", 1300, true),
        new("Search_EmptyQuery", "Search", 300, true),
        new("Checkout_AppliesCoupon", "Checkout", 2310, false),
        new("Checkout_PaysByCard", "Checkout", 3100, true),
    };
}

// Output:
// Logout_ClearsSession
// Logout_ClearsSession
// 1300
// True
// no admin tests
```

### What goes wrong: the exceptions

`First` and `Single` signal problems with `InvalidOperationException`. The message tells you which problem occurred.

```csharp
var tests = TestData.All();

Try("First on no match", () => tests.First(t => t.Area == "Admin"));
Try("Single on many matches", () => tests.Single(t => t.Area == "Auth"));
Try("Single on no match", () => tests.Single(t => t.Name == "Nope"));
Try("SingleOrDefault on many", () => tests.SingleOrDefault(t => t.Area == "Search"));
Try("FirstOrDefault on no match", () => tests.FirstOrDefault(t => t.Area == "Admin"));
Try("First on empty list", () => new List<int>().First());

static void Try(string label, Func<object?> action)
{
    try
    {
        var result = action();
        Console.WriteLine($"{label}: returned {(result is null ? "null" : "a value")}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{label}: {ex.GetType().Name} - {ex.Message}");
    }
}

record TestCase(string Name, string Area, int DurationMs, bool Passed);

static class TestData
{
    public static List<TestCase> All() => new()
    {
        new("Login_Succeeds", "Auth", 820, true),
        new("Login_BadPassword", "Auth", 640, true),
        new("Logout_ClearsSession", "Auth", 450, false),
        new("Search_FindsItem", "Search", 1300, true),
        new("Search_EmptyQuery", "Search", 300, true),
        new("Checkout_AppliesCoupon", "Checkout", 2310, false),
        new("Checkout_PaysByCard", "Checkout", 3100, true),
    };
}

// Output:
// First on no match: InvalidOperationException - Sequence contains no matching element
// Single on many matches: InvalidOperationException - Sequence contains more than one matching element
// Single on no match: InvalidOperationException - Sequence contains no matching element
// SingleOrDefault on many: InvalidOperationException - Sequence contains more than one matching element
// FirstOrDefault on no match: returned null
// First on empty list: InvalidOperationException - Sequence contains no elements
```

(The `Try` helper catches exceptions, which Day 34 explains. `Func<object?>` means "code that returns a value".)

### Choosing between them

- Use `First` when you expect at least one match and any of them is fine. If none exists the failure should be loud.
- Use `Single` when exactly one match is a rule of your data or UI, for example "exactly one logged-in user menu". It also fails if there are two, which detects duplicates. This is why it is popular in tests.
- Use the `OrDefault` forms when "not found" is a normal outcome that you handle. With `SingleOrDefault`, "several found" is still an error.
- Use `Any` when you only need to know whether something exists. Do not write `FirstOrDefault(...) != null` for that.

### The `OrDefault` trap with value types

For value types `default` is a real value. `new List<int>().FirstOrDefault()` returns `0`, which is indistinguishable from an element that is actually `0`. .NET 6 added overloads that take your own default:

```csharp
var codes = new List<int>();

Console.WriteLine(codes.FirstOrDefault());          // 0, but is that "not found" or a real 0?
Console.WriteLine(codes.FirstOrDefault(-1));        // explicit default
Console.WriteLine(codes.FirstOrDefault(c => c > 500, -1));

var statuses = new List<int> { 200, 404, 0 };
Console.WriteLine(statuses.FirstOrDefault(s => s == 0));   // found a real 0
Console.WriteLine(statuses.Any(s => s == 0));              // unambiguous

// Output:
// 0
// -1
// -1
// 0
// True
```

With reference types, remember to handle the `null` that `FirstOrDefault` can return. The compiler's nullable analysis flags it: the result type is `TestCase?`.

```csharp
var users = new List<User> { new("ana", "Admin"), new("bob", "Guest") };

User? admin = users.FirstOrDefault(u => u.Role == "Admin");
User? owner = users.FirstOrDefault(u => u.Role == "Owner");

Console.WriteLine(admin?.Name);
Console.WriteLine(owner?.Name ?? "(no owner)");

if (owner is null)
{
    Console.WriteLine("Skipping owner checks");
}

record User(string Name, string Role);

// Output:
// ana
// (no owner)
// Skipping owner checks
```

### First and deferred execution

`First`, `Any` and `Take` stop reading the source as soon as they have their answer. That matters when the source is expensive or produces items one at a time.

```csharp
var query = Numbers();

Console.WriteLine($"First even: {query.First(n => n % 2 == 0)}");
Console.WriteLine($"Any > 3: {Numbers().Any(n => n > 3)}");

static IEnumerable<int> Numbers()
{
    for (int i = 1; i <= 10; i++)
    {
        Console.WriteLine($"  producing {i}");
        yield return i;
    }
}

// Output:
//   producing 1
//   producing 2
// First even: 2
//   producing 1
//   producing 2
//   producing 3
//   producing 4
// Any > 3: True
```

`yield return` lets a method produce a sequence lazily, one item at a time. You do not need to write such methods yet, but it shows how streaming works: `First` took only two items, and `Any` four.

### Common mistakes

- Using `First` or `Single` where "not found" can happen, then being surprised by `InvalidOperationException`. Use the `OrDefault` form and handle `null`.
- Using `Single` when several matches are acceptable. It throws if there are two.
- Using `FirstOrDefault` on value types and treating `0` as "not found".
- Writing `list.Count() > 0` or `FirstOrDefault(...) != null` instead of `Any(...)`.
- Ignoring the `null` result and dereferencing it, causing `NullReferenceException`.

### Why this matters for test automation

Finding "the row for user X", "the button with this text" or "the one error message" is `First`/`Single` over a list of elements or models. `Single` doubles as an assertion that your data is unique. xUnit has its own `Assert.Single` for the same idea. Choosing the `OrDefault` form wrongly produces either flaky "sequence contains no matching element" failures or hidden nulls that crash later with unclear messages.

### Interview questions

**Q1. Difference between `First` and `FirstOrDefault`?** `First` throws `InvalidOperationException` if nothing matches. `FirstOrDefault` returns the type's default value (null for reference types).

**Q2. Difference between `First` and `Single`?** `First` returns the first match and ignores further matches. `Single` requires exactly one match and throws if there are none or more than one.

**Q3. When would `SingleOrDefault` throw?** When more than one element matches. It returns default only for zero matches.

**Q4. `Any()` vs `Count() > 0`?** `Any()` stops at the first element and states the intent; `Count()` may enumerate everything.

**Q5. What does `All` return for an empty sequence?** `true`.

### Exercise

1. With `TestData.All()`, write a query that returns the first test in the `Checkout` area slower than 3000 ms, or print `"none"` if there is no such test.
2. Use `Single` to find the test named `"Login_Succeeds"`. Then write code that safely handles the case where a name appears twice, using `SingleOrDefault` inside a `try`/`catch (InvalidOperationException)`.
3. Using `All` and `Any`, print whether every Auth test is under one second, and whether any Search test failed.
4. Show the value-type trap: use `FirstOrDefault` on a `List<int>` that contains no matches and explain why using `Any` or an explicit default is safer.

### Check

- Which method would you use to assert that exactly one "Logout" button exists?
- What does `FirstOrDefault` return for `List<bool>` with no match?
- Why is `Any(predicate)` preferable to `Where(predicate).Count() > 0`?

---

## Day 34: Exceptions: `try`/`catch`/`finally`

### What an exception is

When a method cannot do its job (bad input, missing file, a null where an object was needed), .NET creates an exception object and starts throwing it. The normal flow stops. The runtime looks back up the chain of method calls for a `catch` block that handles it. If it finds none, the program crashes with an unhandled exception and prints the error and stack trace.

```text
Console.WriteLine("Before");

int number = int.Parse("abc");   // throws FormatException

Console.WriteLine("After");      // never reached
```

This program would print `Before` and then terminate with an unhandled `System.FormatException`. Notice that `After` is never written.

### `try` and `catch`

Put the risky code in a `try` block and the recovery in a `catch` block. The exception object (`ex`) tells you what happened.

```csharp
try
{
    Console.WriteLine("Parsing...");
    int number = int.Parse("abc");
    Console.WriteLine($"Parsed {number}");
}
catch (FormatException ex)
{
    Console.WriteLine($"Caught: {ex.GetType().Name}");
    Console.WriteLine($"Message: {ex.Message}");
}

Console.WriteLine("Program continues");

// Output:
// Parsing...
// Caught: FormatException
// Message: The input string 'abc' was not in a correct format.
// Program continues
```

Statements after the throwing line inside `try` are skipped (`Parsed` is not printed). Useful members of every exception: `Message` (human-readable text), `GetType()` (which kind), `StackTrace` (where it was thrown, as text), `InnerException` (the exception that caused this one, if any).

### The exception hierarchy and catch order

All exceptions derive from `System.Exception`. Specific types derive from more general ones, for example `FormatException` and `ArgumentException` both derive (directly or indirectly) from `SystemException`, which derives from `Exception`. You can write several `catch` blocks; the first one whose type matches runs, and the compiler rejects a catch placed after a broader one that already covers it. List specific types first and general ones last.

```csharp
Run("42");
Run("abc");
Run("99999999999");
Run(null);

static void Run(string? input)
{
    try
    {
        int n = int.Parse(input!);
        Console.WriteLine($"Parsed {n}");
    }
    catch (FormatException)
    {
        Console.WriteLine($"'{input}' is not a number");
    }
    catch (OverflowException)
    {
        Console.WriteLine($"'{input}' is too large for int");
    }
    catch (ArgumentNullException)
    {
        Console.WriteLine("input was null");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected: {ex.GetType().Name}");
    }
}

// Output:
// Parsed 42
// 'abc' is not a number
// '99999999999' is too large for int
// input was null
```

### `finally`: code that always runs

A `finally` block runs whether the `try` succeeded, threw and was caught, threw and was not caught, or left through `return`. It is for cleanup: closing files, ending a browser session, releasing locks.

```csharp
Console.WriteLine(Step(false));
Console.WriteLine(Step(true));

static string Step(bool fail)
{
    try
    {
        Console.WriteLine("  try: start");
        if (fail) throw new InvalidOperationException("boom");
        Console.WriteLine("  try: end");
        return "returned normally";
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"  catch: {ex.Message}");
        return "returned from catch";
    }
    finally
    {
        Console.WriteLine("  finally: cleanup");
    }
}

// Output:
//   try: start
//   try: end
//   finally: cleanup
// returned normally
//   try: start
//   catch: boom
//   finally: cleanup
// returned from catch
```

Even though `return` appears inside `try` and `catch`, `finally` runs before the method actually returns. You can use `try`/`finally` with no `catch` at all.

### Exception filters: `catch ... when`

A `when` clause adds a condition. If it is false, the catch block is skipped and the search continues, and the exception is not "unwound" in the process.

```csharp
foreach (var status in new[] { 404, 503, 200 })
{
    try
    {
        Fetch(status);
        Console.WriteLine($"{status}: fine");
    }
    catch (HttpFailure ex) when (ex.StatusCode >= 500)
    {
        Console.WriteLine($"{status}: server error, worth retrying");
    }
    catch (HttpFailure)
    {
        Console.WriteLine($"{status}: client error, do not retry");
    }
}

static void Fetch(int status)
{
    if (status >= 400) throw new HttpFailure(status);
}

class HttpFailure : Exception
{
    public int StatusCode { get; }

    public HttpFailure(int statusCode) : base($"HTTP {statusCode}") => StatusCode = statusCode;
}

// Output:
// 404: client error, do not retry
// 503: server error, worth retrying
// 200: fine
```

### Rethrowing: `throw;` versus `throw ex;`

Sometimes you want to log an exception and let it continue to the caller. Write `throw;` (bare). It preserves the original stack trace. Writing `throw ex;` throws the same object but resets its stack trace to the current line, hiding where the error really started.

```csharp
using System.Runtime.CompilerServices;

Console.WriteLine("throw;     keeps origin: " + OriginSurvives(rethrowBare: true));
Console.WriteLine("throw ex;  keeps origin: " + OriginSurvives(rethrowBare: false));

static bool OriginSurvives(bool rethrowBare)
{
    try
    {
        try
        {
            FailsDeepInside();
        }
        catch (Exception ex)
        {
            if (rethrowBare) throw;
            throw ex;
        }
    }
    catch (Exception outer)
    {
        return outer.StackTrace!.Contains(nameof(FailsDeepInside));
    }

    return false;
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void FailsDeepInside() => throw new InvalidOperationException("deep failure");

// Output:
// throw;     keeps origin: True
// throw ex;  keeps origin: False
```

(The compiler may show a warning about rethrowing `ex`; that is the point of the demonstration.) The practical rule: use `throw;` to rethrow. If you want to add context, wrap the original as an inner exception:

```csharp
try
{
    LoadUser("ana");
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.InnerException!.GetType().Name);
    Console.WriteLine(ex.InnerException.Message);
}

static void LoadUser(string name)
{
    try
    {
        int.Parse("not-a-number");
    }
    catch (FormatException inner)
    {
        throw new InvalidOperationException($"Could not load user '{name}'", inner);
    }
}

// Output:
// Could not load user 'ana'
// FormatException
// The input string 'not-a-number' was not in a correct format.
```

### Common exception types

```csharp
Show("Index out of range", () => { int[] a = new int[2]; return a[5]; });
Show("Null reference", () => { string? s = null; return s!.Length; });
Show("Divide by zero", () => { int zero = 0; return 10 / zero; });
Show("Key not found", () => new Dictionary<string, int>()["missing"]);
Show("Invalid cast", () => (int)(object)"text");
Show("Invalid operation", () => new List<int>().First());
Show("Argument", () => new string('x', -1).Length);

static void Show(string label, Func<object> action)
{
    try
    {
        action();
        Console.WriteLine($"{label}: no exception");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{label}: {ex.GetType().Name}");
    }
}

// Output:
// Index out of range: IndexOutOfRangeException
// Null reference: NullReferenceException
// Divide by zero: DivideByZeroException
// Key not found: KeyNotFoundException
// Invalid cast: InvalidCastException
// Invalid operation: InvalidOperationException
// Argument: ArgumentOutOfRangeException
```

| Exception | Typical cause |
|---|---|
| `NullReferenceException` | Using a member of a `null` reference |
| `ArgumentException`, `ArgumentNullException`, `ArgumentOutOfRangeException` | A method received an invalid argument |
| `InvalidOperationException` | The object is in a state where the call is not allowed (including LINQ `First` on empty) |
| `FormatException` | Text could not be parsed |
| `IndexOutOfRangeException`, `KeyNotFoundException` | Bad array index, missing dictionary key |
| `TimeoutException` | An operation exceeded its time limit |

### Exceptions versus normal checks

Exceptions are for unexpected failures, not for ordinary control flow. Throwing is slow compared with a check, and code that uses exceptions to decide between two normal outcomes is harder to read. Prefer `TryParse`, `TryGetValue` and `FirstOrDefault` when "it did not work" is a normal possibility.

```csharp
string[] inputs = { "10", "x", "7" };
int total = 0;

foreach (var text in inputs)
{
    if (int.TryParse(text, out int value))
    {
        total += value;
    }
    else
    {
        Console.WriteLine($"Skipping '{text}'");
    }
}

Console.WriteLine($"Total: {total}");

// Output:
// Skipping 'x'
// Total: 17
```

### What not to do

- Do not write an empty `catch { }` or `catch (Exception) { }`. It hides bugs, and in tests it can make a failing test pass.
- Do not catch `Exception` far from where you can do something useful. Catch the specific exceptions you can handle; let others propagate.
- Do not use `catch` just to print and swallow. Either handle it, rethrow with `throw;`, or let it pass.

### Common mistakes

- Putting a general `catch (Exception)` before specific ones (compile error).
- Using `throw ex;` and losing the stack trace.
- Swallowing exceptions silently.
- Doing the cleanup in `try` instead of `finally`, so it is skipped when an exception occurs.
- Using exceptions for expected situations that `TryParse`-style methods handle.

### Why this matters for test automation

A test fails by throwing an exception: xUnit assertions throw, and Playwright throws a timeout exception when an element is not found within its wait limit. You need to read stack traces, distinguish a real application failure from a test bug (`NullReferenceException` in your page object), and use `finally` to make sure cleanup runs when a test fails halfway. Retry helpers use `catch ... when` to retry only transient errors.

### Interview questions

**Q1. What is the difference between `throw;` and `throw ex;`?** `throw;` rethrows the current exception preserving its stack trace. `throw ex;` resets the stack trace to the point of the rethrow.

**Q2. Does `finally` always run?** Almost always: after normal completion, after a caught or uncaught exception, and when returning from `try`. It does not run if the process is terminated abruptly (for example `Environment.FailFast` or a power loss) or if the `try` block never finishes.

**Q3. In which order should multiple `catch` blocks be written?** From most specific to most general; the first matching block runs, and the compiler errors if a later block is unreachable.

**Q4. What is an exception filter?** A `when (condition)` clause on a catch. The block runs only if the condition is true.

**Q5. Why not use exceptions for flow control?** They are meant for unexpected failures, they are slower than normal checks, and they obscure the intended logic. Use `TryParse` and similar patterns for expected failures.

### Exercise

1. Write a method `int ParsePort(string text)` that returns the port, or throws `ArgumentException` with a clear message if the text is not an integer from 1 to 65535. Call it with `"8080"`, `"abc"`, `"70000"` inside `try`/`catch`.
2. Write a method that opens a pretend resource (print `Opened`), throws an exception, and uses `finally` to print `Closed`. Show that `Closed` prints even though the exception is caught by the caller.
3. Write a loop over statuses 200, 404, 500, 503 and a method `Fetch(int)` that throws `HttpRequestException`-like failures (use your own class). Use `catch ... when` so only 5xx are "retried" (print a message).
4. Rewrite the code in task 1 using `int.TryParse` so no exception is thrown for the non-numeric case. Compare readability.

### Check

- If an exception is thrown in a `try` block with no matching `catch`, does `finally` still run?
- Which line in `try` is skipped after an exception, and which lines after the whole `try` statement run?
- Why is `catch (Exception) { }` dangerous in test code?

---

## Day 35: Custom exceptions, `throw`, `using` and `IDisposable`

### Why define your own exception

Built-in exceptions describe generic problems. A custom exception names a problem in your domain and can carry extra data: `ElementNotFoundException` with the selector, `LoginFailedException` with the user name. Callers can then catch precisely that type, and failure messages are meaningful.

Rules of thumb:

- Derive from `Exception` (or a more specific built-in type when it fits).
- Name it with the `Exception` suffix.
- Provide at least the usual constructors: parameterless, message, and message plus inner exception.
- Add properties for the extra data, set in the constructor.

```csharp
try
{
    Page.Find("#submit", waitedMs: 5000);
}
catch (ElementNotFoundException ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine($"Selector: {ex.Selector}");
    Console.WriteLine($"Waited: {ex.WaitedMs} ms");
}

static class Page
{
    public static void Find(string selector, int waitedMs)
    {
        throw new ElementNotFoundException(selector, waitedMs);
    }
}

class ElementNotFoundException : Exception
{
    public string Selector { get; }
    public int WaitedMs { get; }

    public ElementNotFoundException(string selector, int waitedMs)
        : base($"Element '{selector}' was not found after {waitedMs} ms")
    {
        Selector = selector;
        WaitedMs = waitedMs;
    }

    public ElementNotFoundException(string selector, int waitedMs, Exception inner)
        : base($"Element '{selector}' was not found after {waitedMs} ms", inner)
    {
        Selector = selector;
        WaitedMs = waitedMs;
    }
}

// Output:
// Element '#submit' was not found after 5000 ms
// Selector: #submit
// Waited: 5000 ms
```

### A small exception hierarchy

A base exception for your framework lets callers catch "anything from my framework" or one specific kind.

```csharp
foreach (var action in new Action[] { FailLogin, FailConfig, FailOther })
{
    try
    {
        action();
    }
    catch (LoginFailedException ex)
    {
        Console.WriteLine($"Login problem for {ex.User}");
    }
    catch (FrameworkException ex)
    {
        Console.WriteLine($"Framework problem: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Not ours: {ex.GetType().Name}");
    }
}

static void FailLogin() => throw new LoginFailedException("ana");
static void FailConfig() => throw new FrameworkException("Missing base URL");
static void FailOther() => throw new TimeoutException();

class FrameworkException : Exception
{
    public FrameworkException() { }
    public FrameworkException(string message) : base(message) { }
    public FrameworkException(string message, Exception inner) : base(message, inner) { }
}

class LoginFailedException : FrameworkException
{
    public string User { get; }

    public LoginFailedException(string user) : base($"Login failed for '{user}'") => User = user;
}

// Output:
// Login problem for ana
// Framework problem: Missing base URL
// Not ours: TimeoutException
```

`Action` is a type for "code that returns nothing", covered on Day 37. Here it lets us put three methods in an array.

### Throwing: `throw` statements and expressions

`throw new X(...)` is a statement. Since C# 7 you can also use `throw` as an expression in places that need a value: after `??`, in the false branch of `?:`, and in expression-bodied members.

```csharp
Console.WriteLine(GetBaseUrl("https://app.test"));

try
{
    GetBaseUrl(null);
}
catch (ArgumentNullException ex)
{
    Console.WriteLine(ex.Message);
}

try
{
    new Config(null!);
}
catch (ArgumentNullException ex)
{
    Console.WriteLine(ex.ParamName);
}

try
{
    Console.WriteLine(Describe(-5));
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine(ex.ParamName);
}

static string GetBaseUrl(string? url) =>
    url ?? throw new ArgumentNullException(nameof(url), "Base URL is required");

static string Describe(int retries) =>
    retries >= 0 ? $"{retries} retries" : throw new ArgumentOutOfRangeException(nameof(retries));

class Config
{
    public string Url { get; }

    public Config(string url)
    {
        ArgumentNullException.ThrowIfNull(url);
        Url = url;
    }
}

// Output:
// https://app.test
// Base URL is required (Parameter 'url')
// url
// retries
```

Guard clauses like these validate arguments at the top of a method and fail early with a precise message. `ArgumentNullException.ThrowIfNull(value)` (available since .NET 6) is the concise form. The `nameof` operator produces the parameter's name as a string and stays correct when you rename the parameter.

Which built-in exception to throw: `ArgumentNullException` for a null argument, `ArgumentException` for an invalid value, `ArgumentOutOfRangeException` for a value outside the allowed range, `InvalidOperationException` when the object's current state does not allow the call, `NotSupportedException` or `NotImplementedException` for missing features.

### Wrapping and adding context

A catch block can wrap a low-level exception in a domain exception, keeping the original in `InnerException`:

```csharp
try
{
    Login("ana", "pw");
}
catch (LoginFailedException ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine($"Caused by: {ex.InnerException!.Message}");
}

static void Login(string user, string password)
{
    try
    {
        CallApi();
    }
    catch (TimeoutException inner)
    {
        throw new LoginFailedException(user, inner);
    }
}

static void CallApi() => throw new TimeoutException("auth service did not answer in 5 s");

class LoginFailedException : Exception
{
    public string User { get; }

    public LoginFailedException(string user, Exception inner)
        : base($"Login failed for '{user}'", inner) => User = user;
}

// Output:
// Login failed for 'ana'
// Caused by: auth service did not answer in 5 s
```

### Resources and `IDisposable`

Some objects hold something outside the managed memory that the garbage collector tracks: files, network connections, browser processes. The garbage collector frees memory at an unpredictable time, so those resources must be released deterministically. The convention is the `IDisposable` interface (Week 5): it has one method, `Dispose()`, which releases the resource.

```csharp
var session = new BrowserSession("chromium");
session.Open("/login");
session.Dispose();

Console.WriteLine("done");

class BrowserSession : IDisposable
{
    private readonly string _name;

    public BrowserSession(string name)
    {
        _name = name;
        Console.WriteLine($"{_name}: started");
    }

    public void Open(string path) => Console.WriteLine($"{_name}: opened {path}");

    public void Dispose() => Console.WriteLine($"{_name}: closed");
}

// Output:
// chromium: started
// chromium: opened /login
// chromium: closed
// done
```

The danger: if `Open` threw, `Dispose` would never run and the browser would stay open. You could use `try`/`finally`, but C# has a shorter form.

### The `using` statement and `using` declaration

`using` guarantees `Dispose()` is called when the block ends, even when an exception occurs. It is shorthand for `try`/`finally`.

```csharp
using (var session = new BrowserSession("firefox"))
{
    session.Open("/home");
}

Console.WriteLine("after block");

try
{
    using var failing = new BrowserSession("webkit");   // using declaration
    failing.Open("/boom");
    throw new InvalidOperationException("test failed");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"caught: {ex.Message}");
}

class BrowserSession : IDisposable
{
    private readonly string _name;

    public BrowserSession(string name)
    {
        _name = name;
        Console.WriteLine($"{_name}: started");
    }

    public void Open(string path) => Console.WriteLine($"{_name}: opened {path}");

    public void Dispose() => Console.WriteLine($"{_name}: closed");
}

// Output:
// firefox: started
// firefox: opened /home
// firefox: closed
// after block
// webkit: started
// webkit: opened /boom
// webkit: closed
// caught: test failed
```

The second form, `using var x = ...;`, is the using declaration (C# 8). The object is disposed at the end of the enclosing scope. Here the scope is the `try` block, so `webkit: closed` prints before the `catch` runs.

With several using declarations, disposal happens in reverse order of declaration:

```csharp
using var first = new Resource("first");
using var second = new Resource("second");

Console.WriteLine("working");

class Resource : IDisposable
{
    private readonly string _name;

    public Resource(string name)
    {
        _name = name;
        Console.WriteLine($"open {_name}");
    }

    public void Dispose() => Console.WriteLine($"dispose {_name}");
}

// Output:
// open first
// open second
// working
// dispose second
// dispose first
```

### What `using` expands to

```csharp
var session = new BrowserSession("manual");
try
{
    session.Open("/x");
}
finally
{
    session.Dispose();
}

class BrowserSession : IDisposable
{
    private readonly string _name;

    public BrowserSession(string name)
    {
        _name = name;
        Console.WriteLine($"{_name}: started");
    }

    public void Open(string path) => Console.WriteLine($"{_name}: opened {path}");

    public void Dispose() => Console.WriteLine($"{_name}: closed");
}

// Output:
// manual: started
// manual: opened /x
// manual: closed
```

### Writing `Dispose` carefully

For a simple class that owns a resource, make `Dispose` safe to call more than once and guard use after disposal:

```csharp
var temp = new TempFolder("run-1");
temp.Dispose();
temp.Dispose();   // second call does nothing

try
{
    temp.Write("late");
}
catch (ObjectDisposedException ex)
{
    Console.WriteLine(ex.GetType().Name);
}

class TempFolder : IDisposable
{
    private readonly string _name;
    private bool _disposed;

    public TempFolder(string name)
    {
        _name = name;
        Console.WriteLine($"created {_name}");
    }

    public void Write(string text)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Console.WriteLine($"{_name}: wrote {text}");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Console.WriteLine($"deleted {_name}");
    }
}

// Output:
// created run-1
// deleted run-1
// ObjectDisposedException
```

Classes that wrap unmanaged resources directly need the fuller dispose pattern with a finalizer; classes that just own other `IDisposable` objects (the common case) can use the simple form above. There is also `IAsyncDisposable` with `await using` for resources that close asynchronously; Playwright objects use it, and it is covered in Week 7 after `async`.

### Common mistakes

- Deriving custom exceptions from `ApplicationException` (not recommended); use `Exception`.
- Forgetting to pass the original exception as `inner` when wrapping, which loses the root cause.
- Throwing `Exception` directly instead of a specific type.
- Creating an `IDisposable` object and never disposing it. Prefer `using var`.
- Putting a `using` around an object you did not create and that someone else owns, so you close it too early.

### Why this matters for test automation

Custom exceptions give failures a clear name and data (selector, URL, user), so a CI log immediately says what broke. Browsers, pages, HTTP clients and file streams are disposable resources; leaking them leaves browser processes running and tests interfering with each other. xUnit test classes can implement `IDisposable` (it calls `Dispose` after each test) and `IAsyncLifetime`, so you will use the same cleanup idea from this lesson.

### Interview questions

**Q1. How do you create a custom exception?** Derive from `Exception`, name it with the `Exception` suffix, provide standard constructors, and add properties for extra context.

**Q2. What does `using` do?** For an `IDisposable` it calls `Dispose()` when the scope ends, even if an exception occurs; it is equivalent to `try`/`finally`.

**Q3. Why is `IDisposable` needed given the garbage collector?** The GC reclaims memory at unpredictable times and knows nothing about external resources like handles and processes. `Dispose` releases them deterministically.

**Q4. What is the difference between `throw new X(...)` inside `catch` and `throw;`?** The first creates a new exception (pass the original as inner exception to keep the cause); `throw;` rethrows the original with its stack trace.

**Q5. Which exception should a method throw for a `null` argument it cannot accept?** `ArgumentNullException`, commonly via `ArgumentNullException.ThrowIfNull(arg)`.

### Exercise

1. Create `TestDataException : Exception` with a `FileName` property and the three standard constructors (plus one taking the file name). Throw it from a method `LoadUsers(string fileName)` when the name does not end in `.json`, and catch it printing the file name.
2. Write `class LogScope : IDisposable` whose constructor prints `Begin <name>` and whose `Dispose` prints `End <name>`. Use it with `using var` in a method that throws; show that `End` prints.
3. Nest two `using` statements and predict, then verify, the order of `Dispose` messages.
4. Write a method with a guard clause using `ArgumentNullException.ThrowIfNull` and `ArgumentOutOfRangeException` for a negative `retries`. Call it with bad values and print `ParamName`.

### Check

- Why should a wrapping `throw new X(..., inner)` include `inner`?
- When exactly is a `using var` object disposed?
- What does `using` expand to?

---

## Day 36: Review and practice

### Recap

| Day | Topic | Key points |
|---|---|---|
| 31 | LINQ basics | `Where` filters, `Select` projects, `SelectMany` flattens; chain left to right; query is deferred and re-runs on each enumeration; `ToList`/`ToArray` snapshot. |
| 32 | `OrderBy`, `GroupBy`, `Join` | `OrderBy`/`ThenBy` stable sort; `GroupBy` gives `IGrouping` with `Key`; `Join` is an inner join; `GroupJoin` for left-join style; sorting and grouping read the whole source. |
| 33 | `Any`, `First`, `Single` | `Any`/`All` for questions; `First` vs `Single`; `OrDefault` returns default instead of throwing on no match; `SingleOrDefault` still throws on several. |
| 34 | `try`/`catch`/`finally` | Specific catches first; `finally` always runs; `catch ... when`; `throw;` keeps the stack trace; do not swallow exceptions. |
| 35 | Custom exceptions, `using` | Derive from `Exception`, add context, wrap with inner exception; guard clauses; `IDisposable` and `using` for deterministic cleanup. |

### Practice questions

Try each before opening the answers.

1. What does this print? `var q = new List<int>{1,2,3}.Where(n => n > 1); Console.WriteLine(q.Count());` (assuming nothing else touches the list)
2. A list `L` has 3 items. You write `var q = L.Where(x => x.Active);` and then `L.Add(newActiveItem);` and then `q.Count()`. Does the new item count?
3. Which method throws if there are two matches: `First`, `FirstOrDefault`, `Single`, `SingleOrDefault`?
4. What is the result of `new int[0].All(n => n > 100)`?
5. What does `OrderBy(a).OrderBy(b)` do compared with `OrderBy(a).ThenBy(b)`?
6. Which output order does `GroupBy` use for groups?
7. What is the difference between `throw;` and `throw ex;`?
8. Does `finally` run when the `try` block contains `return`?
9. A `using var db = new Db();` is declared in a method. When is `Dispose` called?
10. Write a `catch` that handles only `HttpFailure` exceptions with `StatusCode >= 500`.

<details>
<summary>Answers</summary>

1. `2`.
2. Yes. The query is deferred, so it runs at `Count()` and sees all three original items plus the new one (assuming they are active).
3. `Single` and `SingleOrDefault`.
4. `true`. There is no element that violates the condition.
5. `OrderBy(a).OrderBy(b)` sorts by `b` only (the second sort replaces the first, although equal `b` values keep the earlier order because the sort is stable). `OrderBy(a).ThenBy(b)` sorts by `a` and uses `b` to break ties.
6. In order of first occurrence of each key in the source sequence.
7. `throw;` rethrows preserving the stack trace; `throw ex;` resets the stack trace to the rethrow point.
8. Yes. `finally` runs before the method returns.
9. When the enclosing scope (the method, here) ends, including when an exception leaves it, and in reverse order relative to other using declarations.
10. `catch (HttpFailure ex) when (ex.StatusCode >= 500) { ... }`.

```csharp
var items = new List<int> { 1, 2, 3 };
var q = items.Where(n => n > 1);
items.Add(10);
Console.WriteLine(q.Count());
Console.WriteLine(new int[0].All(n => n > 100));

// Output:
// 3
// True
```

</details>

### Combined mini-program: parse and summarise a test run

The program reads result lines in the format `name|status|durationMs`, uses a custom exception for bad lines, groups and summarises the good ones with LINQ, and disposes a report writer with `using`.

```csharp
string[] lines =
{
    "Login_Succeeds|Passed|820",
    "Login_BadPassword|Passed|640",
    "Logout_ClearsSession|Failed|450",
    "Search_FindsItem|Passed|1300",
    "Search_EmptyQuery|Skipped|0",
    "Broken line without separators",
    "Checkout_AppliesCoupon|Failed|2310",
    "Checkout_PaysByCard|Exploded|3100",
    "Checkout_Refund|Passed|abc",
};

var results = new List<TestResult>();
var errors = new List<string>();

foreach (var line in lines)
{
    try
    {
        results.Add(ResultParser.Parse(line));
    }
    catch (ResultFormatException ex)
    {
        errors.Add($"{ex.Message} [{ex.Line}]");
    }
}

using var writer = new ReportWriter("run-001");

writer.WriteLine($"Parsed {results.Count} of {lines.Length} lines");

var byStatus = results
    .GroupBy(r => r.Status)
    .OrderBy(g => g.Key)
    .Select(g => $"{g.Key}: {g.Count()}");
writer.WriteLine(string.Join(", ", byStatus));

var slowest = results.OrderByDescending(r => r.DurationMs).First();
writer.WriteLine($"Slowest: {slowest.Name} ({slowest.DurationMs} ms)");

var firstFailure = results.FirstOrDefault(r => r.Status == TestStatus.Failed);
writer.WriteLine($"First failure: {firstFailure?.Name ?? "none"}");
writer.WriteLine($"All under 5 s: {results.All(r => r.DurationMs < 5000)}");

foreach (var error in errors)
{
    writer.WriteLine($"Rejected: {error}");
}

enum TestStatus
{
    Passed,
    Failed,
    Skipped,
}

record TestResult(string Name, TestStatus Status, int DurationMs);

class ResultFormatException : Exception
{
    public string Line { get; }

    public ResultFormatException(string message, string line, Exception? inner = null)
        : base(message, inner) => Line = line;
}

static class ResultParser
{
    public static TestResult Parse(string line)
    {
        var parts = line.Split('|');
        if (parts.Length != 3)
        {
            throw new ResultFormatException("Expected 3 fields", line);
        }

        if (!Enum.TryParse<TestStatus>(parts[1], out var status) || !Enum.IsDefined(status))
        {
            throw new ResultFormatException($"Unknown status '{parts[1]}'", line);
        }

        if (!int.TryParse(parts[2], out var duration))
        {
            throw new ResultFormatException($"Bad duration '{parts[2]}'", line);
        }

        return new TestResult(parts[0], status, duration);
    }
}

class ReportWriter : IDisposable
{
    private readonly string _name;

    public ReportWriter(string name)
    {
        _name = name;
        Console.WriteLine($"== {_name} begin ==");
    }

    public void WriteLine(string text) => Console.WriteLine(text);

    public void Dispose() => Console.WriteLine($"== {_name} end ==");
}

// Output:
// == run-001 begin ==
// Parsed 6 of 9 lines
// Passed: 3, Failed: 2, Skipped: 1
// Slowest: Checkout_AppliesCoupon (2310 ms)
// First failure: Logout_ClearsSession
// All under 5 s: True
// Rejected: Expected 3 fields [Broken line without separators]
// Rejected: Unknown status 'Exploded' [Checkout_PaysByCard|Exploded|3100]
// Rejected: Bad duration 'abc' [Checkout_Refund|Passed|abc]
// == run-001 end ==
```

Note that `== run-001 end ==` prints last, when `writer` goes out of scope at the end of the program.

**Exercise (combined)**

Extend the program:

1. Add a `GroupBy` on the test name prefix (text before the first `_`) and print the number of tests and total duration per prefix, sorted by total duration descending.
2. Add a `Dictionary<string, int>` built with `ToDictionary` from name to duration, and look up a missing name safely with `TryGetValue`.
3. Make the parser throw `ResultFormatException` with the original `FormatException` as the inner exception, by replacing `int.TryParse` with `int.Parse` inside a `try`/`catch (FormatException)`. Print the inner exception type for rejected lines.
4. Add a second `using var` object (for example a `ReportFile`), and confirm from the output that disposal happens in reverse order.

### More interview questions

**Q1. Explain deferred execution with an example where it gives a surprising result.** A query like `list.Where(...)` runs when enumerated. If items are added to `list` after the query is defined but before it is enumerated, they appear in the results. Each enumeration re-runs the query, so use `ToList()` to snapshot.

**Q2. When would you use `Single` instead of `First` in a test?** When the data or UI rule is that exactly one match exists; `Single` then also detects accidental duplicates.

**Q3. How do you do a left join in LINQ?** Use `GroupJoin` (each left item with a possibly empty collection of matches), then flatten with `SelectMany` and `DefaultIfEmpty()` if you want one row per left item, or keep the group as in the Day 32 example.

**Q4. What does the `Select` + `GroupBy` + `OrderBy` pipeline in a report do step by step?** `GroupBy` partitions items by key; `Select` over the groups computes one summary per group (`Key`, `Count`, `Sum`); `OrderBy` sorts the summaries.

**Q5. When is catching `Exception` acceptable?** At a boundary that can do something meaningful with any failure, such as logging at the top of a test runner or wrapping into a domain exception with the original as `InnerException`. Not deep in code, and never silently.

**Q6. What is the difference between `Dispose` and a finalizer?** `Dispose` is called explicitly (or by `using`) at a predictable time. A finalizer runs when the garbage collector eventually collects the object, at an unpredictable time, and is only a safety net for unmanaged resources.

**Q7. Why should custom exceptions carry extra data as properties?** So handlers and logs can use structured information (selector, status code, user) instead of parsing the message text.

**Q8. What does `finally` guarantee, and what does it not?** It runs after the try/catch regardless of how control leaves, unless the process terminates abruptly or the thread never leaves the block. It does not undo state changes made in `try`.
