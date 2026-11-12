# Part 5: Flows and Dependency Injection (Days 48–53)

A **flow** combines several page actions into one business step, such as "log in." Tests call flows, so they read like requirements. Flows get their pages through **dependency injection (DI)**, which keeps them easy to test.

---

## Day 48: What a Flow Is

Look at the repo's `Flows/LoginFlow.cs`:

```csharp
public async Task LoginWithCredentials(string username, string password)
{
    await _loginPage.GoTo("https://www.saucedemo.com");
    await _loginPage.EnterUsername(username);
    await _loginPage.EnterPassword(password);
    await _loginPage.ClickLoginButton();
}
```

The flow knows the steps. The page knows the selectors. The test knows only the goal.

**Exercise:** list the four steps in `LoginWithCredentials` in your notes.

**Check:** you can name each step and the page method behind it.

---

## Day 49: Constructor Injection

A class gets its dependencies through its constructor, not by creating them itself.

```csharp
public class LoginFlow : ILoginFlow
{
    private readonly ILoginPage _loginPage;

    public LoginFlow(ILoginPage loginPage)
    {
        _loginPage = loginPage;
    }
}
```

Without injection, `LoginFlow` would create its own `LoginPage`, which needs a real browser. With injection, a test can pass in a different page.

**Exercise:** create `CheckoutFlow` that takes an `IInventoryPage` in its constructor. Write a method `Task AddItemAsync(string item)` that calls `AddToCart`.

**Check:** the project builds.

---

## Day 50: Registering Services

`ServiceCollection` decides how long each object lives:

- `AddTransient`: a new object every time it's requested.
- `AddScoped`: one object per scope (per test in this repo).
- `AddSingleton`: one object for the whole run.

```csharp
services.AddScoped(sp => Page);             // the browser page for this test
services.AddScoped<ILoginPage, LoginPage>();
services.AddScoped<ILoginFlow, LoginFlow>();
```

**Exercise:** change `ILoginPage` to `AddTransient` and resolve it twice. Then do the same with `AddScoped` inside one scope.

**Check:** `Transient` gives two objects. `Scoped` gives the same object within one scope.

**Repo link:** `Tests/LoginTests.cs`, `InitializeAsync`.

---

## Day 51: Using Flows in Tests

Tests call the flow, then check the result:

```csharp
[Fact]
public async Task Login_WithValidCredentials_ShowsInventory()
{
    await _loginFlow.LoginWithCredentials("standard_user", "secret_sauce");

    await Page.WaitForURLAsync("**/inventory.html");
    await Assertions.Expect(Page.Locator(".inventory_list")).ToBeVisibleAsync();
}
```

**Exercise:** write a second test with `locked_out_user` and assert that the error appears. (Hint: the flow doesn't return the error yet. You'll add that on Day 52.)

**Check:** the test runs and you can see the browser try the login.

---

## Day 52: Returning Results From a Flow

A flow can return data, so tests can check outcomes without reaching into pages.

```csharp
// ILoginFlow.cs
Task<string> LoginAndGetError(string username, string password);

// LoginFlow.cs
public async Task<string> LoginAndGetError(string username, string password)
{
    await LoginWithCredentials(username, password);
    return await _loginPage.GetErrorMessage();
}
```

**Exercise:** write a test that calls `LoginAndGetError("locked_out_user", "secret_sauce")` and asserts the result contains `"locked out"`.

**Check:** the test passes.

---

## Day 53: Review and Checkpoint 5

Read `Flows/LoginFlow.cs` and `Tests/LoginTests.cs` together. Explain in your notes how the test gets its `ILoginFlow`.

**Checkpoint 5 project:** build a `CheckoutFlow` that logs in, adds an item, and returns the cart count:

```csharp
public async Task<int> LoginAndAddToCart(string user, string password, string item)
{
    await _loginFlow.LoginWithCredentials(user, password);
    await _inventoryPage.AddToCart(item);
    return await _inventoryPage.CartCount();
}
```

Write one test that asserts the count is `1`.

**Done when:** the test passes.

---

Previous: [Part 4](04-page-object-model.md) · Next: [Part 6: Your Own Tests](06-own-tests.md)
