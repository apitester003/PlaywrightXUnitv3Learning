# Week 21: Advanced xUnit and Phase 3 Wrap-Up

This week covers custom attributes, assertion libraries, analyzers, organising a larger suite, the Phase 3 mini-project and the phase interview Q&A.

The examples are in `Tests/Course/Week21/`:
- `Week21Examples.cs`: custom attributes and AwesomeAssertions.
- `ShouldlyExamples.cs`: Shouldly.
- `Phase3ProjectTests.cs`: the Day 125 mini-project.

Results from my runs on .NET 10 with `xunit.v3` 3.2.2:
- `--filter "FullyQualifiedName~Week21"`: 7 passed, 0 failed (before the mini-project file was added).
- `--filter "Category=Smoke&FullyQualifiedName~Week21"`: 2 passed. Both custom attributes produce the `Smoke` trait.
- `--filter "FullyQualifiedName~ShoppingCartTests"`: 17 passed, 1 skipped, 0 failed.

I did not rerun the whole suite after the last change, and the Week 9-16 browser tests need internet access. Run `dotnet test` once yourself before you rely on a green full run.

**Packages added to the `.csproj`** (both free, licenses read from the package metadata on this machine):
- `Shouldly` 4.3.0, BSD-3-Clause.
- `AwesomeAssertions` 9.6.0, Apache-2.0.

**Recap you rely on:** attributes (Week 5), traits and filtering (Week 20), `ShoppingCart` (Week 17), data-driven tests (Week 18).

---

## Day 121: Custom attributes and extension points

### A custom trait attribute

Strings like `"Category"` and `"Smoke"` scattered across a suite invite typos. A small attribute replaces them:

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SmokeAttribute : Attribute, ITraitAttribute
{
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() =>
        new[] { new KeyValuePair<string, string>("Category", "Smoke") };
}

[Fact]
[Smoke]
public void Day121_CustomTraitAttribute() { ... }
```
`ITraitAttribute` lives in `Xunit.v3`. This is the v3 shape. It was verified: `--filter "Category=Smoke"` finds the test.

### A custom `[Fact]`

```csharp
public sealed class SmokeFactAttribute : FactAttribute, ITraitAttribute
{
    public SmokeFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber) { }

    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() =>
        new[] { new KeyValuePair<string, string>("Category", "Smoke") };
}

[SmokeFact]
public void Day121_CustomFactAttribute() { ... }
```
Why the constructor looks like that: in v3, `FactAttribute` takes the source file path and line number so the IDE can jump to the test. Your derived attribute passes them through with `[CallerFilePath]` and `[CallerLineNumber]`. Leaving those out may produce an analyzer warning. I did not test the version without them.

### Extension points you have already used

| Extension point | Used in | Purpose |
|---|---|---|
| `DataAttribute` | Day 106 | Custom data sources |
| `ITestCaseOrderer` | Day 118 | Ordering |
| `ITraitAttribute` | this day | Custom tags |
| `IAsyncLifetime` | Days 110-111 | Async set-up and teardown |
| `FactAttribute` / `TheoryAttribute` subclasses | this day | Custom test kinds |

Other extension points exist (test collection orderers, a custom test framework, before/after test attributes). I have not built them in this course. If you need one, look at the current xUnit v3 documentation, since the v3 API differs from v2.

### Retry attribute: the honest answer
A retry attribute is a custom test framework extension, not a simple attribute. It is a real piece of work and I have not built or verified one. The options when you need it:
1. Use a community package, after you check v3 support and its license.
2. Retry at the pipeline level (rerun failed tests only).
3. Fix the flake.

Option 3 is the default recommendation from Day 119.

### Exercise
1. Write `[Layer("Ui")]` with a constructor parameter, as a trait attribute.
2. Write `[SlowFact]` that adds `Category=Slow`.
3. Which of the two forms (a trait attribute next to `[Fact]`, or a custom `[Fact]`) is easier to forget when copying tests? Defend your choice.

**Solution sketch (1):**
```csharp
public sealed class LayerAttribute : Attribute, ITraitAttribute
{
    private readonly string _layer;
    public LayerAttribute(string layer) => _layer = layer;
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() =>
        new[] { new KeyValuePair<string, string>("Layer", _layer) };
}
```
This follows the same pattern as `SmokeAttribute`, but I did not compile this exact class. Add it to the examples file and run it.

---

## Day 122: Assertion libraries: Shouldly and AwesomeAssertions

### Why use one
`Assert.Equal(expected, actual)` works, but reads awkwardly, and its failure messages are plain. Assertion libraries give fluent syntax and richer messages.

### Licensing: a decision you must make
Many tutorials use **FluentAssertions**. Its newer major versions moved to a commercial license for some uses. I know that from general knowledge and I did not check the license text here. Because you asked for free libraries only, this course uses:

| Library | Style | License (read from package) |
|---|---|---|
| Shouldly | `actual.ShouldBe(expected)` | BSD-3-Clause |
| AwesomeAssertions | `actual.Should().Be(expected)` | Apache-2.0 |

AwesomeAssertions has a fluent `Should()` API. Shouldly has a direct `ShouldBe()` style. Both are verified in this repo's examples.

Before adopting any library at work, check its current license and your company's policy. That is a normal part of the job, not just a course rule.

### AwesomeAssertions (verified)
```csharp
cart.Count.Should().Be(2);
cart.Total.Should().BeApproximately(59.98m, 0.001m);
cart.Items.Should().ContainSingle(i => i.Name == "Backpack");
cart.Items.Should().HaveCount(1).And.OnlyContain(i => i.Price > 0);

Action act = () => cart.ApplyDiscount(101);
act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("percent");

actual.Should().BeEquivalentTo(expected);   // compares object graphs
```

### Shouldly (verified)
```csharp
cart.Count.ShouldBe(2);
cart.Total.ShouldBe(59.98m, 0.001m);
cart.Items.ShouldHaveSingleItem().Name.ShouldBe("Backpack");

var ex = Should.Throw<ArgumentOutOfRangeException>(() => cart.ApplyDiscount(101));
ex.ParamName.ShouldBe("percent");
```
Shouldly's selling point is that failure messages include the **source expression** (for example, `cart.Count should be 2 but was 1`). I have not triggered a failure here to show you. Do that in the exercise.

### Name clash to know about
Both libraries add extension methods named `ShouldBe`, `Should`, and so on, and AwesomeAssertions uses `Should()`. Importing both in one file can make the compiler report ambiguous calls. We keep them in separate files (`Week21Examples.cs` and `ShouldlyExamples.cs`) for that reason. I did not test the combined case; a company standard of one library per project avoids the question.

### Mixing with xUnit's `Assert`
You can use `Assert` and a library in one file. Do not mix them inside one test. Pick one style per project.

### Playwright's own assertions
`Assertions.Expect(locator)...` (Week 12) is separate. Use it for anything that must **wait** for the UI. Use a library for plain values after you have read them. A common mistake is reading text with `TextContentAsync()` and asserting at once, which skips the auto-wait.

### How to choose
| Factor | Guidance |
|---|---|
| License and policy | Free and permitted: Shouldly, AwesomeAssertions |
| Team familiarity | Whatever the team can read |
| Message quality | Try the same failing assertion in each |
| Object graph comparison | AwesomeAssertions `BeEquivalentTo` is strong |
| Dependency risk | One library, pinned, reviewed on upgrade |

### Exercise
1. Break `cart.Count.ShouldBe(2)` (use 3) and read the Shouldly message. Do the same in AwesomeAssertions. Write down the two messages and which you prefer.
2. Rewrite five tests from Week 17 with each library.
3. Write one paragraph recommending a library for a team of 15 and the evidence you would collect before deciding.

---

## Day 123: xUnit analyzers

xUnit ships Roslyn analyzers with the `xunit.v3` package. They report mistakes at compile time. I saw two of them in this project's build output while writing this phase:

| Rule | What it said | What we did |
|---|---|---|
| `xUnit2031` | Do not use `Where` before `Assert.Single`; pass the filter to `Assert.Single` | Fixed in `Week17Examples.cs` |
| `xUnit1051` | Calls that accept a `CancellationToken` should pass `TestContext.Current.CancellationToken` | Fixed in my new examples. It is **still reported** in `Week11Examples.cs` (lines 120 and 138) |

The rule list is public at xunit.net/xunit.analyzers. I only list the two I saw. Open the list and read at least the first 20.

### Severity and `.editorconfig`
Analyzer severity can be set per rule:
```ini
# .editorconfig
[*.cs]
dotnet_diagnostic.xUnit1051.severity = warning
dotnet_diagnostic.xUnit2031.severity = error
```
Syntax is standard .NET `.editorconfig`. I did not add this file to the repo. Do it as an exercise, then rebuild and see what changes.

### Treating warnings as errors
`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in the `.csproj` makes every warning fail the build. The repo currently has warnings (for example, `CS8625` in `BaseTest.cs`), so turning that on today would break the build. Fix the warnings first, then enable it. Phase 4 cleans `BaseTest`.

### Why this matters on the job
Analyzers are the cheapest reviewers you have: they never get tired, and they enforce rules without a human arguing. A principal-level move is to encode team conventions as analyzer severities and a shared `.editorconfig`, so reviews focus on design.

### Exercise
1. Fix the two `xUnit1051` warnings in `Week11Examples.cs` (lines 120 and 138). Check the warning is gone.
2. Create `.editorconfig` with one analyzer set to `error`. Prove it fails the build. Then relax it.
3. List five conventions from your own team (or an imagined one) that an analyzer or `.editorconfig` could enforce.

---

## Day 124: Organising a larger test suite

### Where we are
Your repo has two kinds of tests:
- **Course examples** in `Tests/Course/WeekNN/`: teaching code.
- **Real framework tests** in `Tests/` (`LoginTests`).

Real suites need a structure that scales. Phase 4 builds it for this repo. The principles first:

### Structure principles
1. **Organise by what you test, not by test type**, until the suite is big. For example: `Tests/Login/`, `Tests/Cart/`, `Tests/Checkout/`.
2. **Separate layers**: `Tests/Unit/`, `Tests/Api/`, `Tests/Ui/`, so each can run alone (`Layer` trait or separate projects).
3. **Keep helpers out of test classes**: builders, pages, flows and fixtures live in their own folders (this repo already has `Core/`, `Pages/`, `Flows/`, `Fixtures/`).
4. **One test class per unit of behavior**, with a name that matches (`ShoppingCartTests`).
5. **A shared base class only for set-up**, never for hidden assertions.

### Separate projects or one project?
| Option | Pros | Cons |
|---|---|---|
| One project, many folders | Simple, one build | Slow to build, cannot run layers in different pipelines easily |
| One project per layer | Independent runs and dependencies | More project files and shared-code projects |

For this single-project repo we keep one project through Phase 5, using traits (`Layer`) to select what runs. Whether to split is a Phase 8 design decision (Day 303), and I will raise it there instead of deciding now.

### Naming conventions that scale
- Class: `<Subject>Tests` (`LoginTests`).
- Method: `Method_Condition_Expected` or `Scenario_Expected`.
- Traits: `Layer`, `Category`, `Owner`, `Quarantine` (Day 115).
- Files: one primary class per file.

### Keeping the suite healthy
- Delete dead tests. Skipped tests have owners and dates (Day 107).
- Treat test code like production code: reviews, analyzers, refactoring.
- Watch the numbers: run time, flake rate, skips, tests per layer.

### Exercise
1. Sketch the folder tree for a suite with 40 UI, 60 API and 200 unit tests against saucedemo and reqres.in.
2. List what moves out of `Tests/Course/` when Phase 4 starts and what stays.
3. Decide the `Layer` trait values and write the three CLI filters (PR gate, nightly, UI-only).

---

## Day 125: Phase 3 mini-project

### The task
Write a complete suite for `ShoppingCart` using what you learned in Phase 3. A worked version is in `Tests/Course/Week21/Phase3ProjectTests.cs`. It was run: 17 passed, 1 skipped, 0 failed. Try your own version **before** reading it.

### Requirements
- Cover every public member: `Items`, `Count`, `Total`, `Add`, `Remove`, `ApplyDiscount`.
- Both exception paths on `Add`, and `null`.
- Boundaries on `ApplyDiscount`: -1, 0, 100, 101.
- At least one `[InlineData]` theory, one `[MemberData]` theory, and one ticketed skip.
- A `[Smoke]` attribute on the single most important test.
- Naming convention applied everywhere. `Layer` trait on the class.
- No logic in tests beyond what a `switch` in a data-driven test needs (see the worked version's `Total_ForScenario_ReturnsExpected`, and consider whether you would write it differently).

### Self-review checklist
- [ ] Each test has one reason to fail.
- [ ] I can read a failure in the CI log and know what broke.
- [ ] Data-driven rows have meaningful values.
- [ ] No test depends on another.
- [ ] I can state which behaviors are **not** covered.

### Stretch goals
1. Rewrite the assertions with Shouldly, then with AwesomeAssertions. Which reads better?
2. Add a coverage gap on purpose and find it. Code coverage tooling arrives in Week 39 (Day 232), so for now check by reading the code.
3. Write one test that proves two instances of the cart are independent.

### Worth noticing in the worked version
`Total_ForScenario_ReturnsExpected` takes a `scenario` string and uses a `switch` to set up the cart. That is a compromise: it keeps one theory but puts logic in the test. A cleaner approach passes the items as data. Think about why, and rewrite it. This is the kind of detail a principal reviewer comments on.

---

## Day 126: Phase 3 interview Q&A and checkpoint

### Core xUnit questions

**1.** *Why is a v3 test project an executable?*
The runner is built in. The project has its own entry point (generated into `obj`), which supports running the test assembly directly and the Microsoft Testing Platform.

**2.** *Order of events for a test class that implements `IAsyncLifetime` and `IDisposable`?*
Constructor, `InitializeAsync`, test, `DisposeAsync`. I verified this order with a probe. When a class implements both `IAsyncLifetime` and `IDisposable`, `Dispose()` did not run in my test, so put clean-up in `DisposeAsync`.

**3.** *How do `[InlineData]`, `[MemberData]`, `[ClassData]` differ?*
Constants only; any static member (typed with `TheoryData`); a reusable class. Custom `DataAttribute` for files.

**4.** *What does `Assert.Equal(expected, actual)` print on failure, and why does order matter?*
Both values, labelled. Swapping them mislabels the message.

**5.** *Class fixture vs collection fixture vs assembly fixture?*
Per class; across classes (serialising them); across the assembly.

**6.** *What runs in parallel by default?*
Different test collections. Tests inside one class run in sequence.

### Design and judgement questions

**7.** *How do you stop a suite from becoming flaky?*
Isolation (a new context and test data per test), web-first assertions, no order dependence, measured retries with reporting, quarantine with owners and dates.

**8.** *When would you add a retry?*
As a time-limited measure after classifying the cause, never silently. Report retried passes.

**9.** *How do you choose an assertion library?*
License and policy first, then readability, message quality and team familiarity. Standardise on one.

**10.** *What do analyzers add that code review does not?*
Consistent, tireless enforcement of rules, so reviews can focus on design.

**11.** *Where does shared mutable state hide in test suites?*
Static fields, class and collection fixtures, shared accounts or data on a test environment, files on disk, and shared browser pages.

**12.** *How would you structure test code for a team of 15?*
By behavior and layer, with helpers in separate folders, traits for run selection, a documented naming convention and analyzers enforcing the rest. (Day 124.)

### Principal-level discussion prompts
Think through your answers aloud, and write them down.
1. "We have 4,000 tests and the pipeline takes 90 minutes. What is your plan for the first two weeks?" Use traits, layers, isolation, and measurement.
2. "A senior engineer wants to disable parallelism because tests are flaky. What do you say?"
3. "How do you decide what NOT to automate?"
4. "Convince a team to replace `Thread.Sleep` with web-first assertions."
5. "Walk me through how you would introduce xUnit v3 to a team on v2."

### Known gaps in Phase 3 (be honest in interviews)
- I verified the examples compile and pass. I did not verify behavior under the Microsoft Testing Platform runner, only the VSTest path.
- Retry handling is described, not built.
- Several "run it and record" exercises ask you to observe behavior I have not seen, including ordering output and constructor-failure behavior.
- Custom test framework extensions are not covered.
- `.editorconfig` and `TreatWarningsAsErrors` are described, not applied.

### Phase 3 checkpoint
You are ready for Phase 4 if you can, without notes:
- [ ] Write a data-driven theory from each of the four sources.
- [ ] Draw the lifecycle of a test with a class fixture and `IAsyncLifetime`.
- [ ] Explain what runs in parallel and how to change it.
- [ ] Write a trait filter for any run you describe.
- [ ] Defend a position on retries and on assertion libraries.
- [ ] Answer questions 1-12 above in your own words.

**Up next:** Phase 4, this repo's framework: the tour, page objects, flows, dependency injection, configuration and hardening. We will read the repo's `Core`, `Pages`, `Flows` and `Tests` folders line by line, then improve them.
