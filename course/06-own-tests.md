# Part 6: Your Own Tests (Days 54–58)

Now you'll write tests without step-by-step instructions. Each day gives a goal and a hint. Use what you learned in Parts 1–5.

---

## Day 54: Negative Login Tests

A negative test checks that the system rejects bad input. Write three tests in `Tests/LoginNegativeTests.cs`:

| Test | Username | Password | Expected error contains |
|---|---|---|---|
| Wrong password | `standard_user` | `wrong` | `"do not match"` |
| Empty username | `""` | `secret_sauce` | `"Username is required"` |
| Locked-out user | `locked_out_user` | `secret_sauce` | `"locked out"` |

**Hint:** use `ILoginFlow.LoginAndGetError` from Day 52.

**Done when:** all three pass.

---

## Day 55: Data-Driven Negative Tests

Combine the three cases into one `[Theory]` with `[MemberData]`:

```csharp
public static IEnumerable<object[]> NegativeCases()
{
    yield return new object[] { "standard_user", "wrong", "do not match" };
    yield return new object[] { "", "secret_sauce", "Username is required" };
    yield return new object[] { "locked_out_user", "secret_sauce", "locked out" };
}

[Theory]
[MemberData(nameof(NegativeCases))]
public async Task Login_WithBadInput_ShowsError(string user, string password, string expected)
{
    var error = await _loginFlow.LoginAndGetError(user, password);
    Assert.Contains(expected, error);
}
```

**Done when:** the theory runs three times and all pass.

---

## Day 56: Smoke Tests

A smoke test is a short check that the app basically works. Tag your valid-login test:

```csharp
[Fact]
[Trait("Category", "Smoke")]
public async Task Login_WithValidCredentials_ShowsInventory()
{
    // ...
}
```

Run only smoke tests:

```bash
dotnet test --filter "Category=Smoke"
```

**Done when:** only the smoke test runs.

---

## Day 57: Remove Duplicate Setup

If several tests build the same services or log in the same way, move the shared code into `BaseTest` or a flow.

Before: every test has its own `ServiceCollection` setup. After: the setup lives in one place.

**Hint:** see `Core/BaseTest.cs` and `Tests/LoginTests.cs`.

**Done when:** no test repeats the same setup block.

---

## Day 58: Checkpoint 6

Write a suite with at least six tests:
- three positive tests (valid login, inventory loads, cart count is 1)
- three negative tests (from Day 54)

All tests use the POM and flows. None use `Page.Locator` directly.

**Done when:** `dotnet test` shows six passing tests.

---

Previous: [Part 5](05-flows-and-di.md) · Next: [Part 7: Azure CI](07-azure-ci.md)
