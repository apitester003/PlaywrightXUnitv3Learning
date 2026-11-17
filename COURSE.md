> **New plan:** a full 52-week course (312 days) is in [ROADMAP.md](ROADMAP.md). The files below are the earlier short version and will be replaced phase by phase.

# 70-Day Course: C# + Playwright + xUnit v3 + Azure CI

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
6. Create a free Azure DevOps organization at https://dev.azure.com (needed on Day 59).

**Done when:** `dotnet test` passes `Login_WithValidCredentials_ShouldSucceed`.

## 52-week course: Phase 1 (Days 1–48)

| Week | Days | Topic |
|---|---|---|
| [1](course/phase-1-csharp/week-01.md) | 1–6 | Setup and first programs |
| [2](course/phase-1-csharp/week-02.md) | 7–12 | Control flow and methods |
| [3](course/phase-1-csharp/week-03.md) | 13–18 | Strings, arrays, and collections |
| [4](course/phase-1-csharp/week-04.md) | 19–24 | OOP, part 1 |
| [5](course/phase-1-csharp/week-05.md) | 25–30 | OOP, part 2 |
| [6](course/phase-1-csharp/week-06.md) | 31–36 | LINQ and exceptions |
| [7](course/phase-1-csharp/week-07.md) | 37–42 | Async and file I/O |
| [8](course/phase-1-csharp/week-08.md) | 43–48 | Tooling and Phase 1 wrap-up |

## Earlier short course (being replaced phase by phase)


| Part | Days | Topic |
|---|---|---|
| Old part 1 | | Replaced by Phase 1 of the 52-week course (see below) |
| [2](course/02-playwright-basics.md) | 16–27 | Playwright: browsers, locators, actions, assertions |
| [3](course/03-xunit-v3.md) | 28–37 | xUnit v3: facts, theories, lifecycle, output |
| [4](course/04-page-object-model.md) | 38–47 | Page Object Model |
| [5](course/05-flows-and-di.md) | 48–53 | Flows and dependency injection |
| [6](course/06-own-tests.md) | 54–58 | Your own tests |
| [7](course/07-azure-ci.md) | 59–63 | Azure Pipelines CI |
| [8](course/08-deep-dives-and-interviews.md) | 64–70 | Deep dives (strings, LINQ, Playwright windows, iframes, shadow DOM), framework design, interview prep |

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
