# Week 18: Data-Driven Tests

One test body, many inputs. This week you learn `[Theory]` with `[InlineData]`, `[MemberData]`, `[ClassData]`, custom data attributes (JSON and CSV), and skipping and explicit tests.

The examples are in `Tests/Course/Week18/Week18Examples.cs`, with test data in `TestData/login-cases.json`. They were run with `dotnet test --filter "FullyQualifiedName~Week18Examples"` on .NET 10 with `xunit.v3` 3.2.2. Result: 24 passed, 1 skipped.

Two things to know about that result:
- The `[Fact(Explicit = true)]` test is not counted in the VSTest summary. The file defines 26 test cases (counted by hand: 9 `[InlineData]` rows, 7 `[MemberData]` rows, 3 `[ClassData]` rows, 3 JSON rows and 4 plain facts). The run reported a total of 25: 24 passed and 1 skipped, which is 26 minus the explicit test. The console also printed one extra "Skipped" line for the explicit test. I did not test other runners, so check yours if it matters.
- The `.csproj` now copies `TestData/**/*.json` to the output folder. Without that, the JSON test cannot find its file.

**Recap you rely on:** records and collections (Weeks 3 and 5), `System.Text.Json` and file I/O (Week 7), the `ShoppingCart` sample from Week 17.

---

## Day 103: `[Theory]` and `[InlineData]`

A `[Fact]` has no parameters. A `[Theory]` takes parameters and runs once per data row. Each row is reported as its own test.

```csharp
[Theory]
[InlineData(0, 100, 100)]
[InlineData(10, 100, 90)]
[InlineData(50, 80, 40)]
[InlineData(100, 80, 0)]
public void Day103_ApplyDiscount_ReturnsExpected(int percent, int price, int expected)
{
    var cart = new ShoppingCart();
    cart.Add(new CartItem("Item", price));

    Assert.Equal((decimal)expected, cart.ApplyDiscount(percent));
}
```

### Rules
- Each `[InlineData]` value list must match the method parameters in count and (convertible) type.
- Values must be compile-time constants: numbers, strings, `bool`, `null`, `typeof`, enums. You cannot pass `new CartItem(...)` or a `decimal` literal like `29.99m`. That is why the example uses `int` and converts. For anything richer, use `[MemberData]` (Day 104).
- `null` is allowed for reference types: `[InlineData(null, false)]` with `string? user`.

### Boundary values
Theories are the natural place for boundary testing. For `ApplyDiscount`, the boundaries are -1, 0, 100 and 101:

```csharp
[Theory]
[InlineData(-1)]
[InlineData(101)]
public void Day103_ApplyDiscount_OutOfRange_Throws(int percent) { ... }
```

### Why this matters for UI tests
The same technique fits login validation: several invalid user and password pairs, one test body. Each row is its own pass or fail in the report, so you see exactly which input broke.

### Common mistakes
- Putting an `if` inside the test body to handle different rows. If rows need different logic, they need different tests.
- Too many parameters. After four or five, use a data object (Day 104 and Day 106).
- Unreadable rows. Name what each value means in the parameter names.

### Exercise
1. Write a theory for `ShoppingCart.Add` that rejects quantities 0 and -1. You need to build the `CartItem` inside the test from an `int` quantity.
2. Write a theory with four usernames for the string helper `string.IsNullOrWhiteSpace`: `""`, `" "`, `null`, `"a"`.
3. Find out what happens when you pass the wrong number of values to `[InlineData]`. Write down the message you see.

**Solution notes:** (1) `[InlineData(0)]` and `[InlineData(-1)]` with `Assert.Throws<ArgumentOutOfRangeException>(() => cart.Add(new CartItem("X", 1m, quantity)))`. (3) I did not capture the message, so run it. xUnit reports a failure at discovery or run time and the analyzer usually warns at compile time.

---

## Day 104: `[MemberData]`

`[MemberData]` reads rows from a static member (property, field or method) of the test class, or of another class via `MemberType`.

### Untyped: `IEnumerable<object[]>`

```csharp
public static IEnumerable<object[]> PriceRows =>
    new List<object[]>
    {
        new object[] { new[] { 10m, 20m }, 30m },
        new object[] { new[] { 5.5m }, 5.5m },
        new object[] { Array.Empty<decimal>(), 0m },
    };

[Theory]
[MemberData(nameof(PriceRows))]
public void Day104_Total_MemberData_ObjectArrays(decimal[] prices, decimal expected) { ... }
```
This allows arrays, `decimal` and any object. The weakness: types are checked at run time. A wrong type fails only when the test runs.

### Typed: `TheoryData<...>`

```csharp
public static TheoryData<string, int, decimal> TypedRows => new()
{
    { "Backpack", 1, 29.99m },
    { "Onesie", 3, 23.97m },
};
```
The compiler checks each row against the type arguments. Prefer this over `object[]` whenever the values fit.

### Named rows: `TheoryDataRow<...>`

```csharp
public static IEnumerable<TheoryDataRow<int, int, int>> RowsWithNames()
{
    yield return new(1, 2, 3) { TestDisplayName = "one plus two" };
    yield return new(0, 0, 0) { TestDisplayName = "zeros" };
}
```
`TestDisplayName` replaces the generated name in reports. In a CI log, `one plus two` is clearer than `Day104(a: 1, b: 2, sum: 3)`. Rows can also carry `Skip` and `Traits`.

### Which to use
| Need | Use |
|---|---|
| Constants only, few rows | `[InlineData]` |
| Objects, arrays, `decimal`, computed values | `[MemberData]` with `TheoryData<...>` |
| Readable names or per-row skips | `TheoryDataRow<...>` |
| Data shared by several test classes | `[ClassData]` or `MemberType` |

### Rule: the data member must be static
An instance member will not work. If rows must be built from expensive set-up, build them once and keep them static.

### Exercise
1. Convert Day 103's `ApplyDiscount` theory to `TheoryData<int, decimal, decimal>` so prices like `29.99m` are allowed.
2. Add a `TestDisplayName` to every row.
3. Move the data into a separate static class and refer to it with `MemberType = typeof(...)`.

**Solution sketch (3):**
```csharp
public static class DiscountData
{
    public static TheoryData<int, decimal, decimal> Rows => new()
    {
        { 10, 29.99m, 26.991m },
    };
}

[Theory]
[MemberData(nameof(DiscountData.Rows), MemberType = typeof(DiscountData))]
```
Add it to the examples file and run it before you rely on it.

---

## Day 105: `[ClassData]`

`[ClassData(typeof(X))]` takes rows from a class. In v3 the simplest way is to derive from `TheoryData<...>`:

```csharp
public class LoginCases : TheoryData<string, string, string>
{
    public LoginCases()
    {
        Add("", "secret_sauce", "Username is required");
        Add("standard_user", "", "Password is required");
        Add("locked_out_user", "secret_sauce", "locked out");
    }
}

[Theory]
[ClassData(typeof(LoginCases))]
public void Day105_ClassData_Shape(string user, string password, string expectedError) { ... }
```

### When `[ClassData]` is the better choice
- The data is reused by several test classes.
- Building the data needs real code: loops, helpers, files.
- You want the data class to be unit-testable by itself.

### The saucedemo error messages
The three cases above match the messages you saw in Week 16 (Day 91). In Phase 4 you will use cases like these in a real data-driven login test through the `LoginPage`. This week they only check the data's shape because the point is the mechanism, not the browser.

### Exercise
1. Create a `CartCases : TheoryData<...>` for the `Total` calculation and use it from two test methods.
2. What is the difference between `[MemberData]` and `[ClassData]` from a reuse point of view? Write three sentences.

---

## Day 106: Custom data attributes; JSON and CSV

When the data lives in a file, write your own source by deriving from `DataAttribute` (namespace `Xunit.v3`, plus `Xunit.Sdk` for `DisposalTracker`).

### A JSON data attribute

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class JsonFileDataAttribute : DataAttribute
{
    private readonly string _fileName;
    public JsonFileDataAttribute(string fileName) => _fileName = fileName;

    public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
        MethodInfo testMethod, DisposalTracker disposalTracker)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", _fileName);
        List<LoginCase> cases = JsonSerializer.Deserialize<List<LoginCase>>(File.ReadAllText(path))!;

        IReadOnlyCollection<ITheoryDataRow> rows = cases
            .Select(c => (ITheoryDataRow)new TheoryDataRow(c) { TestDisplayName = $"{c.Username}/{c.Password}" })
            .ToList();
        return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(rows);
    }

    public override bool SupportsDiscoveryEnumeration() => true;
}

[Theory]
[JsonFileData("login-cases.json")]
public void Day106_JsonData(LoginCase testCase) { ... }
```

The data file `TestData/login-cases.json` is copied to the output folder by the `.csproj` entry:

```xml
<None Update="TestData\**\*.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

### Points to understand
- `GetData` is `ValueTask`-based in v3. In v2 it was a synchronous method returning `IEnumerable<object[]>`. Older tutorials online show the v2 shape and will not compile on v3.
- Each row is one argument (`LoginCase`), so the test method takes one parameter. The JSON property names must match the record's property names. `System.Text.Json` matched them here because the file uses the same casing as the record. If you change casing, set `PropertyNameCaseInsensitive = true`.
- `SupportsDiscoveryEnumeration` returning `true` lets Test Explorer list each row before running. Return `false` if the data is only available at run time.
- File paths: use `AppContext.BaseDirectory`, not the working directory. The working directory differs between your IDE, `dotnet test` and CI.

### CSV
We have a small parsing sample in `Day106_Csv_Parsing_Sample`. For real work use a free library such as CsvHelper (check its current license before adding it; I have not verified it), or write a `CsvFileDataAttribute` in the same shape as the JSON one.

### Where to keep test data
| Data | Where |
|---|---|
| A few rows, simple | `[InlineData]` in the test |
| Medium, typed | `TheoryData` in the test class |
| Large, owned by QA or product | JSON or CSV file |
| Secrets (real passwords) | Never in the repo. Week 27 and Day 262 |

### Exercise
1. Add two rows to `login-cases.json` and confirm that two more tests appear.
2. Break the JSON on purpose (remove a comma). Read the error. Where does it surface: at discovery or at run time? Write down what you observe.
3. Write a `CsvFileDataAttribute` that reads `TestData/login-cases.csv` and compare it with the JSON one.

---

## Day 107: Skipping and explicit tests

### Skip
```csharp
[Fact(Skip = "Day 107 example: skipped with a reason")]
```
A skipped test is reported as skipped. `Skip` needs a reason. It also works on `[Theory]`, and on a single `TheoryDataRow`.

### Conditional skip
```csharp
public static bool IsWindows => OperatingSystem.IsWindows();

[Fact(SkipUnless = nameof(IsWindows), Skip = "Windows only")]
```
`SkipUnless` (and `SkipWhen`) name a static `bool` member. The test runs only when it is `true`. This is a good fit for browser-specific tests (for example, WebKit only on some OSes) or tests that need a secret that only exists in CI.

### Explicit
```csharp
[Fact(Explicit = true)]
```
Explicit tests are not run in normal runs. They run only when you ask for them. Use them for slow or destructive tests, like a full data reset.

Observed here: the explicit test printed as skipped in the VSTest console output and did not appear in the summary count. Behavior may differ with other runners, so check yours.

### Skip policy (the part that matters on the job)
A `Skip` with no owner becomes permanent. Suggested rules:
- Every skip reason names a ticket: `Skip = "BUG-1234: flaky on checkout"`.
- A skip has an owner and a review date.
- Count skips on the dashboard. Rising skips are a smell.
- Prefer fixing, quarantining with a trait (Week 20) or deleting.

### Exercise
1. Add a `SkipWhen` test that skips on Linux.
2. Mark a slow test `Explicit` and run it by itself. Find out how to run it from the CLI and write down the command that worked for you.
3. Write a one-paragraph skip policy for a team of ten.

---

## Day 108: Review, practice and interview questions

### Mini-project: a data-driven suite
Write `ShoppingCartDataTests` with:
1. an `[InlineData]` theory for discount boundaries,
2. a typed `[MemberData]` theory for totals with `decimal` values,
3. a `[ClassData]` source shared by two test methods,
4. a JSON-driven theory with at least five rows, including one that is skipped with a ticket reference,
5. a display name on every row so the CI log reads well.

### Review checklist
- [ ] I know what values `[InlineData]` can hold, and what it cannot.
- [ ] I choose `TheoryData<...>` over `object[]` when types allow.
- [ ] I can write a custom `DataAttribute` using the v3 `GetData` signature.
- [ ] I know the JSON file must be copied to the output folder.
- [ ] I can skip, conditionally skip and mark tests explicit.

### Interview questions

**Junior**
1. When do you use `[Theory]` instead of `[Fact]`? *(Same logic, several inputs.)*
2. What kinds of values can go in `[InlineData]`? *(Compile-time constants.)*

**Mid**
3. `[MemberData]` or `[ClassData]`? *(Member for local data, class for reuse or complex construction.)*
4. How do you give a data row a readable name? *(`TheoryDataRow` with `TestDisplayName`.)*
5. Your JSON-driven test cannot find its file in CI. Why? *(Not copied to the output folder, or relative to the wrong directory. Use `AppContext.BaseDirectory` and the `CopyToOutputDirectory` item.)*

**Senior**
6. What changed in custom data attributes between v2 and v3? *(`GetData` returns `ValueTask<IReadOnlyCollection<ITheoryDataRow>>`, takes a `DisposalTracker`, and has `SupportsDiscoveryEnumeration`.)*
7. A theory has 400 rows and takes 20 minutes. What do you do? *(Check what each row costs. Move pure logic to a unit-level theory, cut redundant rows with equivalence classes, and run UI rows in parallel only if they are isolated.)*
8. How do you keep data-driven UI tests from hiding which input failed? *(Display names that include the input, one assertion focus per row, and attach screenshots or traces on failure.)*

**Principal / architect**
9. Where should test data live in a large organisation, and who owns it? *(Source-controlled with the tests for stability. Separate files where non-developers contribute. Secrets in a vault. Clear ownership and review rules.)*
10. How do you decide between boundary tests, equivalence classes and exhaustive data? *(Risk-based. Cover each equivalence class and each boundary. Exhaustive data only where the domain is tiny or the cost is near zero.)*

### On-the-job scenarios
1. **The 200-row theory.** A teammate generated a theory with 200 near-identical rows. Say how you review it. Hint: ask which behavior each row proves.
2. **The data file that broke the build.** A product owner edits a JSON file and the suite fails at discovery with a vague error. Decide what guardrails you add: a schema or a validation test that loads the file, and clear error messages in the data attribute.
3. **The skipped theory row.** One row of a theory is flaky. You want to keep the others. Show how you skip only that row and track it.

### Checkpoint
You are ready for Week 19 if you can write a custom data attribute from memory and explain the trade-offs between the four data sources.

**Up next:** Week 19, lifecycle and fixtures: constructor and `IDisposable`, `IAsyncLifetime`, class, collection and assembly fixtures.
