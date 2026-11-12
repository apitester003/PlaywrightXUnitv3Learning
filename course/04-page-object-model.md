# Part 4: Page Object Model (Days 38–47)

The Page Object Model (POM) puts each page's locators and actions in one class. Tests call those actions and never touch selectors. When the site changes, you fix one class instead of many tests.

---

## Day 38: The Problem POM Solves

Without POM, a test mixes selectors and logic:

```csharp
// Hard to maintain: selectors live inside the test
await page.Locator("[data-test='username']").FillAsync("standard_user");
await page.Locator("[data-test='password']").FillAsync("secret_sauce");
await page.Locator("[data-test='login-button']").ClickAsync();
```

With POM, the test reads like a user story:

```csharp
await loginPage.EnterUsername("standard_user");
await loginPage.EnterPassword("secret_sauce");
await loginPage.ClickLoginButton();
```

**Exercise:** count how many tests in the repo would need an edit if the username selector changed.

**Check:** the answer should be "one class" once the POM is in place.

---

## Day 39: Keeping Locators Private

Locators go in a `protected` property, so only the page class and its subclasses can use them.

```csharp
public class LoginPage
{
    private readonly IPage _page;

    public LoginPage(IPage page) => _page = page;

    protected ILocator UsernameInput => _page.Locator("[data-test='username']");

    public async Task EnterUsername(string username)
    {
        await UsernameInput.FillAsync(username);
    }
}
```

**Exercise:** open `Pages/LoginPage.cs` and find the three `protected ILocator` properties.

**Check:** you can explain why they're `protected` and not `public`.

---

## Day 40: Base Page Classes

Shared code goes in a base class. The repo's `BasePageSimple` holds the page object, the URL, and the `GoTo` method.

```csharp
public abstract class BasePageSimple : IBasePage
{
    protected IPage Page { get; }
    protected string PageUrl { get; set; } = string.Empty;

    protected BasePageSimple(IPage page) => Page = page;

    public virtual async Task GoTo(string? baseUrl = null)
    {
        var url = baseUrl != null ? $"{baseUrl}/{PageUrl}" : PageUrl;
        await Page.GotoAsync(url);
    }
}
```

**Exercise:** add `public async Task<string> GetTitle() => await Page.TitleAsync();` to `BasePageSimple`.

**Check:** `LoginPage` can call `GetTitle()` without writing it again.

---

## Day 41: Interfaces for Pages

The repo's `ILoginPage` lists what the login page can do. Flows depend on the interface, not the class, so you can swap in a fake page later.

```csharp
public interface ILoginPage : IBasePage
{
    Task EnterUsername(string username);
    Task EnterPassword(string password);
    Task ClickLoginButton();
    Task<string> GetErrorMessage();
}
```

**Exercise:** add `GetErrorMessage` to `ILoginPage`, then implement it in `LoginPage`:

```csharp
public async Task<string> GetErrorMessage()
{
    return await _page.Locator("[data-test='error']").TextContentAsync() ?? "";
}
```

**Check:** the project builds. Fix any missing-member errors.

---

## Day 42: Methods That Return Pages

When a click leads to a new page, return that page's object. The test can then keep going on the new page.

```csharp
public class InventoryPage : BasePageSimple
{
    public InventoryPage(IPage page) : base(page) { PageUrl = "inventory.html"; }

    protected ILocator InventoryList => Page.Locator(".inventory_list");
}

// in LoginPage
public async Task<InventoryPage> ClickLoginAndGoToInventory()
{
    await LoginButton.ClickAsync();
    await Page.WaitForURLAsync("**/inventory.html");
    return new InventoryPage(Page);
}
```

**Exercise:** create `InventoryPage` with the code above. Use it in a test that logs in and checks the returned page is not null.

**Check:** the test passes.

---

## Day 43: Checking That You're on the Right Page

Each page class should have a method that confirms it loaded.

```csharp
public async Task<bool> IsLoaded()
{
    return await InventoryList.IsVisibleAsync();
}
```

**Exercise:** add `IsLoaded()` to `InventoryPage` and call it after logging in.

**Check:** `IsLoaded()` returns `true` on the inventory page and `false` on the login page.

---

## Day 44: Keep Assertions Out of Page Classes

Pages perform actions and read state. Tests make assertions. This keeps pages reusable.

```csharp
// Page class: returns data
public Task<string> GetErrorMessage() => ...;

// Test: makes the assertion
var error = await loginPage.GetErrorMessage();
Assert.Contains("Username is required", error);
```

**Exercise:** find any `Assertions.Expect` or `Assert.` call in your page classes and move it into the test.

**Check:** page classes contain no assertions.

---

## Day 45: Naming

Name page classes after the screen (`LoginPage`, `InventoryPage`). Name methods after what a user does (`EnterUsername`, `AddToCart`).

Bad: `DoStuff()`, `Click1()`. Good: `ClickLoginButton()`, `AddItemToCart()`.

**Exercise:** rename one vague method in your code.

**Check:** the name says what a user does.

---

## Day 46: Review

Re-type the `InventoryPage` example from Day 42 without looking. Then explain in your own words why tests call `loginPage.EnterUsername` instead of using `Page.Locator`.

---

## Day 47: Checkpoint 4

Build a POM for the saucedemo inventory page:

```csharp
public class InventoryPage : BasePageSimple
{
    public InventoryPage(IPage page) : base(page) { PageUrl = "inventory.html"; }

    public async Task AddToCart(string itemName)
    {
        // Verify the exact button locator in browser dev tools (F12)
        await Page.Locator($"[data-test='add-to-cart-{itemName}']").ClickAsync();
    }

    public async Task<int> CartCount()
    {
        var badge = Page.Locator(".shopping_cart_badge");
        if (await badge.CountAsync() == 0) return 0;
        return int.Parse(await badge.TextContentAsync() ?? "0");
    }
}
```

Write a test that logs in, adds `sauce-labs-backpack`, and asserts `CartCount()` is `1`. The test file must contain no selectors.

**Done when:** the test passes and the test file has no `Page.Locator` calls.

---

Previous: [Part 3](03-xunit-v3.md) · Next: [Part 5: Flows and Dependency Injection](05-flows-and-di.md)
