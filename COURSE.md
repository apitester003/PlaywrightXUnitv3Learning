# 60-Day Course: C# + Playwright + xUnit v3 + Azure CI

**Time commitment:** 1 hour per day (about 30 min reading, 30 min coding).
**Rule:** Every day ends with code you ran yourself. Reading alone doesn't count.

**How to use this course**
1. Read the day's lesson.
2. Run the example and do the exercise in this repo.
3. Check your answer against the "Done when" line.
4. Every 10 days, do the checkpoint project.

Official docs (use them when a detail is unclear):
- C#: https://learn.microsoft.com/dotnet/csharp/
- Playwright .NET: https://playwright.dev/dotnet/docs/intro
- xUnit v3: https://xunit.net/docs/getting-started/v3/whats-new
- Azure Pipelines: https://learn.microsoft.com/azure/devops/pipelines/

---

## Setup (Day 0, before Day 1)

1. Install the .NET 10 SDK: `dotnet --version` should print `10.x`.
2. Install PowerShell 7 (`pwsh`). Playwright uses it to install browsers.
3. Clone the repo and build:
   ```bash
   git clone https://github.com/apitester003/PlaywrightXUnitv3Learning.git
   cd PlaywrightXUnitv3Learning
   dotnet build
   ```
4. Install the Chromium browser (run from the project folder):
   ```bash
   pwsh bin/Debug/net10.0/playwright.ps1 install chromium
   ```
5. Run the existing test:
   ```bash
   dotnet test
   ```
6. Create a free Azure DevOps organization at https://dev.azure.com. You'll need it on Day 56.

**Done when:** `dotnet test` runs and `Login_WithValidCredentials_ShouldSucceed` passes.

---

## Part 1: C# Refresher (Days 1–12)

### Day 1: Variables, types, and `string` interpolation
- Lesson: `var`, `string`, `int`, `bool`, and `$"Hello {name}"`.
- Exercise: Write a console app that prints a login message using a username variable.
- Done when: the program prints the message correctly.

### Day 2: Methods and parameters
- Lesson: return types, parameters, `void` vs `Task`.
- Exercise: Write `static string Greet(string name)` and call it.
- Done when: it returns the expected string.

### Day 3: Conditionals and loops
- Lesson: `if`, `else`, `switch`, `for`, `foreach`.
- Exercise: Loop over three usernames and print `"valid"` for `standard_user` and `"invalid"` for the others.
- Done when: the output matches.

### Day 4: Classes and properties
- Lesson: `class`, constructors, auto-properties (`public string Name { get; set; }`).
- Exercise: Create a `User` class with `Username` and `Password`.
- Done when: you can create and print a `User`.

### Day 5: Interfaces
- Lesson: an interface defines a contract; a class implements it.
- Repo link: `Pages/ILoginPage.cs` and `Pages/LoginPage.cs`.
- Exercise: Define `IGreeter` with `string Greet(string name)`, then implement it in `FriendlyGreeter`.
- Done when: `IGreeter g = new FriendlyGreeter();` compiles and runs.

### Day 6: Inheritance and `abstract`
- Lesson: `abstract class` can't be created directly; subclasses must implement its abstract members.
- Repo link: `Core/BaseTest.cs` is `abstract`, and `LoginTests` inherits it.
- Exercise: Create an abstract `Animal` with `Speak()`, then subclass it.
- Done when: subclass instances call their own `Speak()`.

### Day 7: Lists and dictionaries
- Lesson: `List<T>`, `Dictionary<TKey, TValue>`.
- Exercise: Store three test users in a `Dictionary<string, string>` (username → password).
- Done when: you can look up a password by username.

### Day 8: `async` and `await`
- Lesson: `async Task` methods return immediately; `await` pauses until the work finishes.
- Repo link: every `await` in `Pages/LoginPage.cs`.
- Exercise: Write `async Task Wait1Second()` using `await Task.Delay(1000)`.
- Done when: the program waits about one second before printing.

### Day 9: Nullable reference types
- Lesson: `string?` can be null; `string` shouldn't be. The repo has `<Nullable>enable</Nullable>`.
- Repo link: `Core/BasePageSimple.cs` uses `string? baseUrl`.
- Exercise: Write `string Describe(string? x)` that returns `"none"` when `x` is null.
- Done when: the compiler gives no warnings.

### Day 10: Dependency injection (basics)
- Lesson: a container creates objects and supplies their dependencies.
- Repo link: `Tests/LoginTests.cs`, `InitializeAsync` method.
- Exercise: Register `IGreeter`/`FriendlyGreeter` in a `ServiceCollection` and resolve it.
- Done when: `GetRequiredService<IGreeter>()` returns your class.

### Day 11: Exceptions
- Lesson: `try`, `catch`, `finally`, and `throw`.
- Exercise: Write a method that throws `ArgumentException` for an empty username.
- Done when: a `try/catch` around it prints the message.

### Day 12: Review
- Re-do Days 4, 5, 8, and 10 without looking at the notes.

### CHECKPOINT 1 (end of Day 12)
Build a console app with a `User` class, an `IUserValidator` interface, and a DI-registered validator. It should reject empty usernames and print results using `async` methods.
**Done when:** it builds with no warnings and handles the empty-username case.

---

## Part 2: Playwright Basics (Days 13–24)

### Day 13: What Playwright is
- Lesson: Playwright controls real browsers (Chromium, Firefox, WebKit) from code.
- Exercise: Read the first page of https://playwright.dev/dotnet/docs/intro.
- Done when: you can explain what `IPlaywright`, `IBrowser`, and `IPage` are.

### Day 14: Launching a browser
- Lesson: `Playwright.CreateAsync()` → `Chromium.LaunchAsync()` → `NewContextAsync()` → `NewPageAsync()`.
- Repo link: `Core/BaseTest.cs`, `InitializeAsync`.
- Exercise: Write a test that opens `about:blank` and checks the title is empty.
- Done when: the test passes.

### Day 15: Navigation
- Lesson: `Page.GotoAsync(url)`.
- Repo link: `Core/BasePageSimple.cs`, `GoTo` method.
- Exercise: Navigate to https://www.saucedemo.com and print the title.
- Done when: the title prints.

### Day 16: Locators
- Lesson: a `Locator` describes an element. Prefer `data-test` and `Role`-based locators.
- Repo link: `Pages/LoginPage.cs`, lines with `Page.Locator("[data-test='username']")`.
- Exercise: Find the username input on saucedemo.com using `Page.Locator("[data-test='username']")`.
- Done when: `CountAsync()` returns 1.

### Day 17: Filling and clicking
- Lesson: `FillAsync(text)` types into an input; `ClickAsync()` clicks.
- Exercise: Log in to saucedemo.com with `standard_user` / `secret_sauce`.
- Done when: the inventory page loads.

### Day 18: Reading text
- Lesson: `TextContentAsync()`, `InnerTextAsync()`.
- Exercise: Read the error message after logging in with a wrong password.
- Done when: your test prints the message.

### Day 19: Assertions with `Expect`
- Lesson: `await Assertions.Expect(locator).ToBeVisibleAsync()` waits and retries automatically.
- Repo link: `Tests/LoginTests.cs`, the `.inventory_list` assertion.
- Exercise: Assert the error message contains `"Epic sadface"`.
- Done when: the assertion passes.

### Day 20: Waiting
- Lesson: avoid fixed delays (`Task.Delay`). Use `WaitForURLAsync` or `Expect`.
- Repo link: `Tests/LoginTests.cs`, `WaitForURLAsync("**/inventory.html")`.
- Exercise: Replace any `Task.Delay` in your code with an `Expect` wait.
- Done when: the test passes without `Task.Delay`.

### Day 21: Dropdowns and checkboxes
- Lesson: `SelectOptionAsync` for `<select>`, `CheckAsync` for checkboxes.
- Exercise: On https://the-internet.herokuapp.com/dropdown, select "Option 2".
- Done when: the selected value is "2".

### Day 22: Screenshots
- Lesson: `Page.ScreenshotAsync(new PageScreenshotOptions { Path = "shot.png" })`.
- Exercise: Take a screenshot after login.
- Done when: `shot.png` exists.

### Day 23: Headless vs headed
- Lesson: `Headless = false` shows the browser (good for learning). `Headless = true` runs without a window (needed for CI).
- Exercise: Run the existing test with `Headless = true` in `BaseTest.cs` and confirm it passes. Then set it back to `false` for learning.
- Done when: both settings pass.

### Day 24: Review
- Re-do Days 16–20 on a different site.

### CHECKPOINT 2 (end of Day 24)
Write three tests against saucedemo.com: valid login, wrong password (check the error message), and empty username (check the error message). Use `Expect` for all assertions.
**Done when:** all three pass.

---

## Part 3: xUnit v3 (Days 25–34)

### Day 25: `[Fact]` and assertions
- Lesson: `[Fact]` marks a test. Use `Assert.Equal`, `Assert.True`.
- Repo link: `Tests/LoginTests.cs`, `[Fact]` attribute.
- Exercise: Write three `[Fact]` tests on a small `Calculator` class.
- Done when: `dotnet test` shows three passing tests.

### Day 26: `[Theory]` and `[InlineData]`
- Lesson: one test, many inputs.
- Exercise: `[Theory] [InlineData(1,2,3)] [InlineData(5,5,10)]` on an `Add` method.
- Done when: both cases run and pass.

### Day 27: `[MemberData]`
- Lesson: pull test data from a static method or property.
- Exercise: Move your login credentials into a `MemberData` method.
- Done when: the theory runs once per credential set.

### Day 28: Test lifecycle with `IAsyncLifetime`
- Lesson: `InitializeAsync` runs before each test; `DisposeAsync` runs after.
- Repo link: `Core/BaseTest.cs`.
- Exercise: Add a `Console.WriteLine` in both methods of a test class, then check the output order.
- Done when: you can explain the order.

### Day 29: Class fixtures
- Lesson: `IClassFixture<T>` shares one object across all tests in a class.
- Exercise: Create a fixture that holds a counter; check it's shared across tests.
- Done when: the counter increments across tests.

### Day 30: Skipping and traits
- Lesson: `[Fact(Skip = "reason")]` and `[Trait("Category", "Smoke")]`.
- Exercise: Mark one test as skipped with a reason.
- Done when: it shows as skipped in `dotnet test`.

### Day 31: Running a subset
- Lesson: `dotnet test --filter` runs matching tests.
- Exercise: Run only `LoginTests` with `dotnet test --filter FullyQualifiedName~LoginTests`.
- Done when: only those tests run.

### Day 32: Test output
- Lesson: `ITestOutputHelper` writes output to the test runner.
- Exercise: Log the username used in a test with `ITestOutputHelper`.
- Done when: it appears in test output.

### Day 33: Review
- Re-do Days 26–28.

### CHECKPOINT 3 (end of Day 34)
Build a `[Theory]`-driven login test that runs three credential sets from `MemberData`. Use `IAsyncLifetime` for browser setup.
**Done when:** all three cases run and report separately.

---

## Part 4: Page Object Model (Days 35–44)

### Day 35: What POM is
- Lesson: one class per page. Tests call page methods; they don't touch locators directly.
- Repo link: `Pages/LoginPage.cs`.
- Exercise: List the locators and actions on the saucedemo.com login page.
- Done when: you have a list of 3 locators and 3 actions.

### Day 36: Locators stay inside the page class
- Lesson: `protected ILocator` keeps selectors private. The repo does this in `LoginPage`.
- Exercise: Move a locator out of a test file into a page class.
- Done when: the test no longer uses `Page.Locator`.

### Day 37: Base page classes
- Lesson: shared code (like `GoTo`) goes in a base class.
- Repo link: `Core/BasePageSimple.cs`.
- Exercise: Add a `GetTitle()` method to `BasePageSimple`.
- Done when: `LoginPage` can call it.

### Day 38: Interfaces for pages
- Lesson: `ILoginPage` lets tests (or fakes) depend on a contract.
- Repo link: `Pages/ILoginPage.cs`.
- Exercise: Add `Task<string> GetErrorMessage()` to `ILoginPage` and implement it.
- Done when: it returns the error text.

### Day 39: Returning page objects
- Lesson: a method can return the next page object, so tests read like steps.
- Exercise: Make `ClickLoginButton()` return an `InventoryPage` (create a small class for the inventory page).
- Done when: `await loginPage.ClickLoginButton()` returns an `InventoryPage`.

### Day 40: Page verification methods
- Lesson: each page has a method that confirms you're on it.
- Exercise: Add `Task<bool> IsLoaded()` to `InventoryPage`.
- Done when: it returns true on the inventory page.

### Day 41: Keep assertions out of pages
- Lesson: pages perform actions and read state. Tests make assertions.
- Exercise: Remove any `Expect` call from your page classes and move it to the test.
- Done when: all `Expect` calls are in test files.

### Day 42: Naming
- Lesson: name pages after what they represent (`LoginPage`, `InventoryPage`), and methods after user actions (`EnterUsername`).
- Exercise: Rename one vague method in your code.
- Done when: all names match their purpose.

### Day 43: Review
- Re-do Days 36–39 without looking at notes.

### CHECKPOINT 4 (end of Day 44)
Make a POM for the saucedemo.com inventory page with `AddToCart(string itemName)` and `CartCount`. Write a test that adds one item and checks the count is 1.
**Done when:** the test passes and the test file contains no locators.

---

## Part 5: Flows and Dependency Injection (Days 45–50)

### Day 45: What a flow is
- Lesson: a flow is several page actions combined into one business step (like "log in").
- Repo link: `Flows/LoginFlow.cs`.
- Exercise: Read `LoginFlow.LoginWithCredentials` and list the steps.
- Done when: you can name all four.

### Day 46: Constructor injection
- Lesson: a class receives its dependencies in the constructor.
- Repo link: `Flows/LoginFlow.cs`, constructor takes `ILoginPage`.
- Exercise: Create a `CheckoutFlow` that takes `IInventoryPage`.
- Done when: DI resolves it.

### Day 47: Registering services
- Lesson: `AddScoped`, `AddTransient`, `AddSingleton`. Scoped = one per test here.
- Repo link: `Tests/LoginTests.cs`, `services.AddScoped`.
- Exercise: Change one registration to `AddTransient` and explain the difference.
- Done when: you can say what changes.

### Day 48: Flows in tests
- Lesson: tests call flows, not raw pages.
- Exercise: Write a test that uses `ILoginFlow` to log in and checks the inventory page.
- Done when: the test passes.

### Day 49: Flow for failed login
- Lesson: flows can return results so tests can check errors.
- Exercise: Add `Task<string> LoginAndGetError(string u, string p)` to `ILoginFlow`.
- Done when: a test checks the error message.

### Day 50: Review
- Re-read `LoginFlow.cs` and `LoginTests.cs` and explain how they connect.

### CHECKPOINT 5 (end of Day 50)
Build a `CheckoutFlow` that logs in, adds an item, and returns the cart count. Write one test for it.
**Done when:** the test passes.

---

## Part 6: Your Own Tests (Days 51–55)

### Day 51: Negative tests
- Exercise: Write tests for wrong password, empty username, and locked-out user (`locked_out_user`).
- Done when: all three pass.

### Day 52: Data-driven tests
- Exercise: Use `[Theory]` with `MemberData` for the negative cases.
- Done when: one theory covers all three.

### Day 53: Smoke test tag
- Exercise: Add `[Trait("Category", "Smoke")]` to your login test and run `dotnet test --filter Category=Smoke`.
- Done when: only the smoke test runs.

### Day 54: Cleanup
- Exercise: Remove duplicate code from your tests by moving it into `BaseTest` or a flow.
- Done when: no test has repeated setup.

### Day 55: Review
- Run `dotnet test` and fix anything that fails.

### CHECKPOINT 6 (end of Day 55)
Write a test suite with at least 6 tests: 3 positive, 3 negative, all using POM and flows. Every test should pass.
**Done when:** `dotnet test` shows 6 passing tests.

---

## Part 7: Azure CI (Days 56–60)

### Day 56: Azure DevOps setup
- Lesson: Azure DevOps organizations hold projects; a project holds pipelines.
- Exercise: Sign in at https://dev.azure.com, create an organization and project, then connect your GitHub repo under **Pipelines → New pipeline → GitHub**.
- Done when: Azure DevOps can see your repo.

### Day 57: Your first pipeline file
- Lesson: `azure-pipelines.yml` in the repo root defines the build.
- Exercise: Create a pipeline that builds the project:
  ```yaml
  trigger:
    - main

  pool:
    vmImage: ubuntu-latest

  steps:
    - task: UseDotNet@2
      inputs:
        packageType: sdk
        version: '10.x'

    - script: dotnet build --configuration Release
      displayName: Build
  ```
- Done when: the build succeeds in Azure DevOps.

### Day 58: Installing browsers in CI
- Lesson: CI agents need Playwright's browsers. The official option is the Playwright CLI.
- Exercise: Add this step before `dotnet test`:
  ```yaml
    - pwsh: |
        dotnet build --configuration Release
        pwsh bin/Release/net10.0/playwright.ps1 install --with-deps chromium
      displayName: Install Playwright browsers
  ```
- Done when: the step finishes without errors.

### Day 59: Running tests in CI
- Lesson: CI needs headless browsers. Change `Headless = false` to `Headless = true` in `Core/BaseTest.cs` (or read it from an environment variable).
- Exercise: Add a test step:
  ```yaml
    - script: dotnet test --configuration Release --logger trx
      displayName: Run tests
  ```
- Done when: the tests pass in the pipeline.

### Day 60: Publishing results
- Lesson: `PublishTestResults@2` shows results in the Azure DevOps Tests tab.
- Exercise: Add:
  ```yaml
    - task: PublishTestResults@2
      condition: succeededOrFailed()
      inputs:
        testResultsFormat: VSTest
        testResultsFiles: '**/*.trx'
  ```
- Done when: the test results appear in the pipeline's Tests tab.

### CHECKPOINT 7 (end of Day 60)
Make one change to a test, push it, and watch the pipeline run from start to finish. Break a test on purpose, confirm the pipeline fails, then fix it.
**Done when:** the pipeline goes red, then green.

---

## Quick Reference

| Task | Code |
|---|---|
| Open a page | `await Page.GotoAsync(url)` |
| Find element | `Page.Locator("[data-test='x']")` |
| Type | `await locator.FillAsync("text")` |
| Click | `await locator.ClickAsync()` |
| Read text | `await locator.TextContentAsync()` |
| Assert visible | `await Assertions.Expect(locator).ToBeVisibleAsync()` |
| Assert text | `await Assertions.Expect(locator).ToHaveTextAsync("x")` |
| Wait for URL | `await Page.WaitForURLAsync("**/inventory.html")` |
| Screenshot | `await Page.ScreenshotAsync(new PageScreenshotOptions { Path = "a.png" })` |
| Install browsers | `pwsh bin/Debug/net10.0/playwright.ps1 install chromium` |
| Run all tests | `dotnet test` |
| Run one class | `dotnet test --filter FullyQualifiedName~LoginTests` |
| Run by trait | `dotnet test --filter Category=Smoke` |

## Troubleshooting

- **Browser not found:** run the `playwright.ps1 install` command again.
- **Test hangs:** check your locator in the browser dev tools (F12) and make sure it exists.
- **Passes locally, fails in CI:** check `Headless` and confirm browsers are installed in the pipeline.
- **Compiler warning about null:** read the `?` in the signature and handle null before use.
