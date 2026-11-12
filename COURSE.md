# 60-Day Course: C# + Playwright + xUnit v3 + Azure CI

A step-by-step tutorial. Each day teaches one concept with working code, then gives an exercise. Work through it in order, about 1 hour a day.

**Daily routine:** read the day's section (about 30 minutes), then do the exercise in this repo (about 30 minutes).

## Setup

1. Install the .NET 10 SDK. Check with `dotnet --version`.
2. Install PowerShell 7 (`pwsh`).
3. Clone and build:
   ```bash
   git clone https://github.com/apitester003/PlaywrightXUnitv3Learning.git
   cd PlaywrightXUnitv3Learning
   dotnet build
   ```
4. Install Chromium:
   ```bash
   pwsh bin/Debug/net10.0/playwright.ps1 install chromium
   ```
5. Run the existing test: `dotnet test`
6. Create a free Azure DevOps organization at https://dev.azure.com (needed on Day 56).

**Done when:** `dotnet test` passes `Login_WithValidCredentials_ShouldSucceed`.

## Parts

| Part | Days | Topic |
|---|---|---|
| [1](course/01-csharp-refresher.md) | 1–12 | C# refresher: methods, classes, interfaces, `async`/`await`, DI |
| [2](course/02-playwright-basics.md) | 13–24 | Playwright: browsers, locators, actions, assertions |
| [3](course/03-xunit-v3.md) | 25–34 | xUnit v3: facts, theories, lifecycle, output |
| [4](course/04-page-object-model.md) | 35–44 | Page Object Model |
| [5](course/05-flows-and-di.md) | 45–50 | Flows and dependency injection |
| [6](course/06-own-tests.md) | 51–55 | Your own tests |
| [7](course/07-azure-ci.md) | 56–60 | Azure Pipelines CI |

## Quick Reference

| Task | Code |
|---|---|
| Open a page | `await Page.GotoAsync(url)` |
| Find element | `Page.Locator("[data-test='x']")` |
| Type | `await locator.FillAsync("text")` |
| Click | `await locator.ClickAsync()` |
| Read text | `await locator.TextContentAsync()` |
| Assert text | `await Assertions.Expect(locator).ToContainTextAsync("x")` |
| Wait for URL | `await Page.WaitForURLAsync("**/inventory.html")` |
| Screenshot | `await Page.ScreenshotAsync(new PageScreenshotOptions { Path = "a.png" })` |
| Install browsers | `pwsh bin/Debug/net10.0/playwright.ps1 install chromium` |
| Run all tests | `dotnet test` |
| Run by name | `dotnet test --filter "FullyQualifiedName~LoginTests"` |
| Run smoke tests | `dotnet test --filter "Category=Smoke"` |

## Troubleshooting

- **Browser not found:** run the `playwright.ps1 install` command again.
- **Test hangs:** open the page in a browser, press F12, and check that your selector matches an element.
- **Passes locally, fails in CI:** check `HEADLESS` and confirm browsers are installed in the pipeline.
- **Null warning:** read the `?` in the signature and handle `null` before using the value.
