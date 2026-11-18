# Week 20: Traits, Output, and Parallelism

This week covers filtering with traits, test output and `TestContext`, parallel execution and collections, test ordering, and the first look at flaky tests. Deeper parallel and flake work comes in Phase 6 (Days 235-239).

The examples are in `Tests/Course/Week20/Week20Examples.cs`. With Week 19 they were run with `dotnet test --filter "FullyQualifiedName~Week19|FullyQualifiedName~Week20"`: 19 passed, 0 failed. The trait filters were also run:
- `--filter "Category=Smoke&FullyQualifiedName~Week20"` ran 1 test.
- `--filter "Owner=team-checkout"` ran 1 test.

What is not verified: the observed order of events in the parallelism and ordering examples (the tests compile and pass, but they do not assert the order). Where a day says "run it and record", do that. I have not seen your output.

**Recap you rely on:** attributes (Week 5), `async` and `ValueTask` (Week 7), fixtures and collections (Week 19).

---

## Day 115: Traits and filtering

A trait is a name and value tag on a test.

```csharp
[Fact]
[Trait("Category", "Smoke")]
public void Day115_Smoke() { ... }

[Fact]
[Trait("Category", "Regression")]
[Trait("Owner", "team-checkout")]
public void Day115_Regression() { ... }
```

You can put `[Trait]` on a method or on a class. Several traits on one test are allowed.

### Filtering from the CLI (verified)
```bash
dotnet test --filter "Category=Smoke"
dotnet test --filter "Owner=team-checkout"
dotnet test --filter "Category=Smoke&FullyQualifiedName~Week20"   # AND
dotnet test --filter "Category=Smoke|Category=Regression"         # OR
dotnet test --filter "Category!=Slow"                              # NOT
```
Quote the filter in PowerShell, and in a pipeline YAML file, so the `&` and `|` characters are not interpreted by the shell.

### Custom trait attributes
You can make your own attribute, so strings are not scattered through the suite. Not verified here; the v3 shape is an attribute that implements `ITraitAttribute` with a `GetTraits()` method. Check the xUnit docs for the exact signature and add it to the examples file.

### A trait taxonomy that works on the job
| Trait | Values | Used for |
|---|---|---|
| `Category` | `Smoke`, `Regression`, `Slow` | What to run, and when |
| `Layer` | `Unit`, `Api`, `Ui` | Test pyramid reporting |
| `Owner` | team name | Who to call when it breaks |
| `Quarantine` | ticket id | Known-flaky, kept out of the gate |

Keep the list short and documented. A trait nobody understands does not help.

### Exercise
1. Add `Category` and `Layer` traits to ten existing tests from Weeks 17-20.
2. Write the filter that runs smoke tests that are not UI tests.
3. Which traits would you require in code review, and why?

**Solutions:** (2) `--filter "Category=Smoke&Layer!=Ui"`. (3) Opinion: `Layer` and `Owner`, because they enable the pyramid report and failure routing.

---

## Day 116: `ITestOutputHelper` and test context

### `ITestOutputHelper`
In xUnit, `Console.WriteLine` does not reliably show up in test results. Use `ITestOutputHelper`, injected through the constructor:

```csharp
public class OutputTests
{
    private readonly ITestOutputHelper _output;
    public OutputTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Day116_OutputHelper_Injected()
    {
        _output.WriteLine("hello from the injected helper");
    }
}
```
The output is attached to that test's result. To see it with the CLI, use `--logger "console;verbosity=detailed"`.

### `TestContext.Current` (new in v3)
```csharp
ITestContext context = TestContext.Current;
context.Test!.TestDisplayName;                           // the test's name
TestContext.Current.SendDiagnosticMessage("...");        // runner-level message
TestContext.Current.TestOutputHelper?.WriteLine("...");  // output without injection
TestContext.Current.CancellationToken;                   // the test's cancellation token
```
Our example asserts that the display name contains the method name, and it passes. Note that `TestContext.Current` returns `ITestContext`. In an early draft I assigned it to `TestContext` and the compiler rejected it.

### Why `TestContext.Current` matters for a framework
Helpers that are deep inside your framework (a screenshot helper, a logger) can write to the test's output or know the test's name **without** passing `ITestOutputHelper` through every constructor. This is how you will name screenshot and trace files after the test in Phase 4.

### Logging pattern
Wrap `ITestOutputHelper` in a small logger so framework code does not depend on xUnit directly. Phase 4 connects this to Microsoft.Extensions.Logging, which the project already references.

### Exercise
1. Log "navigating to ..." and the URL from a UI test using the output helper. Run with detailed verbosity and find the lines.
2. Write a static helper `TestInfo.Name()` that returns `TestContext.Current.Test?.TestDisplayName`.
3. When would `TestContext.Current.Test` be null? Look up the xUnit docs; do not guess.

---

## Day 117: Parallelism and test collections

### The default rules
- **Test classes** (collections) run in parallel with each other.
- **Tests inside one class** run one after another.
- Each class without `[Collection]` is its own collection.

So by default, 20 test classes can run at the same time, but the 10 tests in one class cannot.

### Collections serialise their members
```csharp
[Collection("Serial A")] public class SerialOneTests { ... }
[Collection("Serial A")] public class SerialTwoTests { ... }
```
Classes that share a collection name never run at the same time. Use this when two classes need the same non-shareable resource (a file, a port, an account). The example logs `one-start`, `one-end`, `two-start`, `two-end` into a `ConcurrentQueue`. It does not assert the order, so run it yourself and print the queue to see it.

### Turning parallelism off or tuning it
At assembly level:
```csharp
[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: CollectionBehavior(MaxParallelThreads = 4)]
```
These are standard xUnit attributes; I have not added them to the repo, because turning off parallelism for the whole assembly would slow the other weeks' tests. Try them in a scratch copy. v3 also supports a configuration file (`xunit.runner.json`); check the docs for the exact keys before relying on them.

### What parallel UI tests need
- Own browser context (and page) per test. Shared pages are not safe.
- Own test data. Two tests that log in with `standard_user` and edit the same cart on a real shared backend can collide. Saucedemo keeps its cart in browser storage, so contexts isolate it. A real application often does not.
- Unique values (names, emails) for anything created on a server.

### Cost and benefit
Parallel runs cut wall-clock time but raise the cost of shared state mistakes, and can overload a test environment. Measure both time and failure rate when you change the setting.

### Exercise
1. Run the two `Serial A` tests and print the queue. Then remove the collection attribute and run again. Describe how the order changes. Record what you see.
2. Write a test class with two `Task.Delay(2000)` tests. How long does the class take? Why?
3. List the shared resources in this repo that would break parallel UI tests today.

**Solution notes:** (2) About 4 seconds, since tests inside one class run in sequence. (3) Check `BaseTest`: it creates its own browser per test, so it is isolated, but the `Headless = false` setting would open many windows at once.

---

## Day 118: Test ordering

### The principle
Tests should not depend on order. Each should pass alone and in any order. xUnit does not promise a particular order by default, and relying on it is a bug waiting to happen.

### When ordering is legitimate
- A long end-to-end journey where each step is a separate test case for reporting. (Better: one test with several steps.)
- Making a run deterministic for debugging.
- Running fast tests before slow ones for earlier feedback.

### A custom orderer (compiles and runs)
```csharp
public class AlphabeticalOrderer : ITestCaseOrderer
{
    public IReadOnlyCollection<TTestCase> OrderTestCases<TTestCase>(IReadOnlyCollection<TTestCase> testCases)
        where TTestCase : notnull, ITestCase
        => testCases.OrderBy(t => t.TestMethodName, StringComparer.Ordinal).ToList();
}

[TestCaseOrderer(typeof(AlphabeticalOrderer))]
public class OrderedTests { ... }
```
In v3 the orderer interface is generic and returns an `IReadOnlyCollection`; v2 examples on the web use a different shape and will not compile. The class built and its tests passed. The example collects the order in a static list but does not assert it, so print `OrderedTests.Seen` to confirm.

You can also order test collections (`ITestCollectionOrderer`) and apply an orderer at assembly level. Not verified here.

### The better answer: remove the dependency
If step 2 needs step 1's data, make step 2 create it (through an API call or a helper) instead of relying on step 1. Hybrid UI plus API set-up in Week 37 is built on this idea.

### Exercise
1. Run `OrderedTests` and print the order. Does it match the alphabetical order?
2. Rewrite a three-step "add to cart, check out, verify order" chain as one test with three clearly commented sections. Which version reports failures better? Argue both sides.
3. Write a rule for your team's guide on ordering. One sentence.

---

## Day 119: Handling flaky tests and retries

### What flaky means
A flaky test passes and fails on the same code. In UI automation the usual causes are:

| Cause | Example | Direction of the fix |
|---|---|---|
| Timing | Reading text before it updates | Web-first assertions (Week 12) |
| Shared state | Two tests edit the same data | Isolation (Days 117, 239) |
| Environment | Slow CI machine, network | Timeouts as config, stable test environment |
| Test data | A user was locked by a previous run | Create data per test, clean up |
| Order dependence | Passes only after test X | Remove the dependency (Day 118) |
| Real product bug | Race condition in the app | Report it. Do not hide it |

### Retries: what they do and do not do
A retry runs a failed test again. It hides flakiness, it does not fix it. Teams use retries as a **temporary** measure, with these safeguards:
- Report that a test needed a retry. A retried pass is not a clean pass.
- Count retries on a dashboard.
- Quarantine, fix, then remove the retry.

### Retries in xUnit
xUnit has no built-in retry attribute. Options are a community package or your own custom attribute (Week 21, Day 121). I have not verified any package against v3. If you consider one, check three things first: that it supports xUnit v3, its license (free only for this course), and that it reports retried tests visibly.

### A starting policy
1. Fail fast in the gate. No silent retries on pull request builds.
2. A test that fails once and passes on re-run is logged as flaky with the run id.
3. A flaky test gets a ticket, a `Quarantine` trait and an owner. It is moved out of the blocking set.
4. Quarantined tests still run on a schedule so you see progress.
5. A quarantine over 30 days old is reviewed: fix or delete.

You can filter quarantined tests out with a trait: `--filter "Quarantine!=true"` style, using the trait scheme from Day 115. Check the exact value you choose.

### Exercise
1. Take a flaky-prone locator from Week 16 and describe how it could race. Fix it with a web-first assertion.
2. Write the one-page flaky-test policy for your team.
3. Reproduce a timing flake on purpose: read `TextContentAsync` immediately after a click that triggers a delayed update. Describe what you observed.

---

## Day 120: Review, practice and interview questions

### Mini-project: a tagged, observable, parallel-safe suite
1. Tag every test in Weeks 17-20 with `Layer` and `Category`.
2. Write two CLI filters: a fast gate (unit, smoke) and a nightly set (everything except quarantined).
3. Add output logging that prints the test name at the start of three UI tests.
4. Prove with two timed classes that collections serialise.
5. Write a half-page flaky-test policy.

### Review checklist
- [ ] I can tag and filter tests with traits.
- [ ] I know `TestContext.Current` and what it replaces.
- [ ] I can state the default parallelism rules.
- [ ] I know when ordering is appropriate, and the better alternative.
- [ ] I can describe a retry policy without hiding real bugs.

### Interview questions

**Junior**
1. How do you run only smoke tests? *(Trait plus `--filter`.)*
2. How do you print output from an xUnit test? *(`ITestOutputHelper`.)*

**Mid**
3. What runs in parallel in xUnit by default? *(Test classes/collections yes, tests within a class no.)*
4. Why shouldn't tests depend on order? *(Isolation, parallelism, debuggability.)*
5. What is `TestContext.Current` for? *(Access to the current test's info, output and cancellation token without injection.)*

**Senior**
6. A test fails one time in twenty. What is your process? *(Reproduce, classify the cause, check shared state and timing, look at traces, then fix. Quarantine if it blocks the team.)*
7. Argue for and against automatic retries. *(For: unblocks delivery on infra noise. Against: hides real defects, grows the suite's cost, erodes trust. Mitigation: report and cap them.)*
8. Two test classes use the same account on a shared environment. How do you make them safe in parallel? *(Separate accounts or data, or a collection to serialise them. Prefer separate data.)*

**Principal / architect**
9. How do you measure suite health? *(Flake rate, mean time to diagnose, run time, quarantine age, skip counts, and failures per layer.)*
10. How would you design the parallelism strategy for a 3,000-test suite? *(Isolation first, then shard across agents (Day 296), keep shared resources behind collections or per-worker data, and measure to find the real bottleneck.)*

### On-the-job scenarios
1. **The Friday flake.** The release build fails on one UI test. Re-running passes. A manager says to add a retry. What do you say? Write a short reply with a plan.
2. **The slow gate.** Pull request checks take 35 minutes. Use traits and layers to propose a 10-minute gate. What do you keep and what do you move to nightly?
3. **The shared account.** Parallel runs fail randomly after you turn parallelism on. Explain how you prove the account is the cause.

### Checkpoint
You are ready for Week 21 if you can write the filter for any run you describe, and defend your position on retries in two minutes.

**Up next:** Week 21, advanced xUnit and the Phase 3 wrap-up: custom attributes, assertion libraries, analyzers, organising a large suite, the mini-project and the phase interview.
