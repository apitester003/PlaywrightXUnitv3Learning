# Part 3: xUnit v3 (Days 28–37)

xUnit runs your tests. It finds methods marked with attributes, runs them, and reports results. This repo uses xUnit v3 (`xunit.v3` 3.2.2), which is what `Tests/LoginTests.cs` relies on.

---

## Day 28: Facts and Assertions

A `[Fact]` is a test with no inputs. Inside it, use `Assert` to check results.

```csharp
using Xunit;

public class CalculatorTests
{
    [Fact]
    public void Add_TwoAndThree_ReturnsFive()
    {
        var result = 2 + 3;

        Assert.Equal(5, result);
    }

    [Fact]
    public void IsEmpty_EmptyString_ReturnsTrue()
    {
        Assert.True(string.IsNullOrEmpty(""));
    }
}
```

Naming tests as `Method_Scenario_Expected` makes failures easy to read.

**Exercise:** create `CalculatorTests.cs` in `Tests/` with the code above. Add one failing test on purpose.

**Check:** `dotnet test` reports one failure and shows the expected and actual values.

---

## Day 29: Theories and InlineData

A `[Theory]` runs one test with several inputs. `[InlineData]` supplies each set.

```csharp
[Theory]
[InlineData(1, 2, 3)]
[InlineData(5, 5, 10)]
[InlineData(0, 0, 0)]
public void Add_ReturnsSum(int a, int b, int expected)
{
    Assert.Equal(expected, a + b);
}
```

Each `InlineData` line is a separate test in the results.

**Exercise:** write a theory for `IsValidUsername` from Part 1, Day 2, with three inputs: `"bob"` (true), `""` (false), `"  "` (false).

**Check:** `dotnet test` reports three separate results.

---

## Day 30: MemberData

For more complex data (like objects), use `[MemberData]`. It points to a static method that returns the test data.

```csharp
public static IEnumerable<object[]> Credentials()
{
    yield return new object[] { "standard_user", "secret_sauce" };
    yield return new object[] { "problem_user", "secret_sauce" };
}

[Theory]
[MemberData(nameof(Credentials))]
public void Credentials_AreNotEmpty(string user, string password)
{
    Assert.False(string.IsNullOrWhiteSpace(user));
    Assert.False(string.IsNullOrWhiteSpace(password));
}
```

**Exercise:** add a third set, `("locked_out_user", "secret_sauce")`, to `Credentials()`.

**Check:** the theory runs three times.

---

## Day 31: Setup and Cleanup with IAsyncLifetime

`IAsyncLifetime` gives a test class two methods: `InitializeAsync` runs before each test, and `DisposeAsync` runs after. Use them for browser setup and teardown.

```csharp
public class LifecycleDemo : IAsyncLifetime
{
    public ValueTask InitializeAsync()
    {
        Console.WriteLine("Setup");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Console.WriteLine("Teardown");
        return ValueTask.CompletedTask;
    }

    [Fact]
    public void First() => Console.WriteLine("Test 1");

    [Fact]
    public void Second() => Console.WriteLine("Test 2");
}
```

You'll see `Setup`, `Test 1`, `Teardown`, then `Setup`, `Test 2`, `Teardown`. Each test gets a fresh setup.

**Exercise:** run the test and confirm the output order.

**Check:** you can explain why `Setup` runs twice.

**Repo link:** `Core/BaseTest.cs` implements `IAsyncLifetime`. It launches a browser in `InitializeAsync` and closes it in `DisposeAsync`.

---

## Day 32: Class Fixtures

`IClassFixture<T>` creates one shared object for all tests in a class. Use it for expensive setup, such as starting a browser once for many tests.

```csharp
public class SharedCounter
{
    public int Count;
}

public class CounterTests : IClassFixture<SharedCounter>
{
    private readonly SharedCounter _counter;

    public CounterTests(SharedCounter counter) => _counter = counter;

    [Fact]
    public void FirstTest() => Assert.Equal(1, ++_counter.Count);

    [Fact]
    public void SecondTest() => Assert.Equal(2, ++_counter.Count);
}
```

Test order isn't guaranteed, so the second test may run first. Write tests that don't depend on order.

**Exercise:** change the counter test so it doesn't depend on order.

**Check:** the tests pass when you run them in any order.

**Note:** the repo's `BaseTest` creates a new browser per test, which is slower but more isolated. Keep that in mind when you choose a fixture.

---

## Day 33: Skip and Traits

Use `Skip` to temporarily disable a test with a reason. Use `[Trait]` to group tests.

```csharp
[Fact(Skip = "Known bug #123")]
public void BrokenTest() => Assert.Fail("not run");

[Fact]
[Trait("Category", "Smoke")]
public void QuickCheck() => Assert.True(true);
```

**Exercise:** skip one test with a reason, and add `Trait("Category", "Smoke")` to another.

**Check:** `dotnet test` shows the skipped test in its summary.

---

## Day 34: Running Some Tests

Run everything with `dotnet test`. Narrow it with `--filter`:

```bash
dotnet test --filter "FullyQualifiedName~LoginTests"
dotnet test --filter "FullyQualifiedName~Calculator"
```

`~` means "contains". If your xUnit setup doesn't accept `--filter` with traits, check the version notes at https://xunit.net.

**Exercise:** run only tests whose names contain `Calculator`.

**Check:** only `CalculatorTests` run.

---

## Day 35: Test Output

`ITestOutputHelper` writes messages that show up in the test output, without cluttering the console.

```csharp
public class OutputTests
{
    private readonly ITestOutputHelper _output;

    public OutputTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void LogsSomething()
    {
        _output.WriteLine("Checking login for standard_user");
        Assert.True(true);
    }
}
```

**Exercise:** log each input in a `[Theory]` using `_output.WriteLine`.

**Check:** each input appears in the test output for its case.

---

## Day 36: Review

Re-type the Day 29 theory and the Day 31 lifecycle demo without looking.

**Quick quiz (answer in your notes):**
1. What's the difference between `[Fact]` and `[Theory]`?
2. When does `InitializeAsync` run?
3. Why shouldn't tests depend on each other?

---

## Day 37: Checkpoint 3

Create `Tests/LoginDataTests.cs`. Add a `[Theory]` with `[MemberData]` that logs in with three credential sets from the list below. Use `IAsyncLifetime` for browser setup, and reuse the browser setup from `Core/BaseTest.cs`.

| Username | Password | Expected |
|---|---|---|
| `standard_user` | `secret_sauce` | Inventory page loads |
| `locked_out_user` | `secret_sauce` | Error contains `"locked out"` |
| `problem_user` | `secret_sauce` | Inventory page loads |

**Done when:** all three cases run and report separately.

---

Previous: [Part 2](02-playwright-basics.md) · Next: [Part 4: Page Object Model](04-page-object-model.md)
