# Part 7: Azure CI (Days 56–60)

Continuous integration (CI) runs your tests automatically when you push code. Azure Pipelines runs them on a Microsoft-hosted machine and shows the results.

---

## Day 56: Azure DevOps Setup

1. Go to https://dev.azure.com and sign in with a Microsoft account. Create it if you don't have one.
2. Create an **organization**. This holds your projects.
3. Create a **project** inside it.
4. Go to **Pipelines → New pipeline → GitHub**. Authorize Azure Pipelines and pick `apitester003/PlaywrightXUnitv3Learning`.

Azure DevOps offers a free tier. Check https://azure.microsoft.com/pricing/details/devops/azure-devops-services/ for the current limits before you rely on them.

**Exercise:** choose "Starter pipeline" and don't save it yet. Look at the template.

**Check:** you see the YAML editor.

---

## Day 57: Your First Pipeline

A pipeline is defined in `azure-pipelines.yml` at the repo root. Replace the starter template with:

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

Save and run it. The `trigger` line starts the pipeline on every push to `main`.

**Exercise:** add a `displayName` to each step and push a change to `README` or `COURSE.md`.

**Check:** the build finishes green in Azure DevOps.

---

## Day 58: Installing Browsers on the Agent

CI machines don't have Playwright browsers. Install them in a step. The script is built into the output folder by the Playwright package:

```yaml
  - pwsh: |
      dotnet build --configuration Release
      pwsh bin/Release/net10.0/playwright.ps1 install --with-deps chromium
    displayName: Build and install browsers
```

`--with-deps` also installs the Linux libraries that Chromium needs.

**Exercise:** replace the `Build` step with the step above.

**Check:** the step finishes without errors.

---

## Day 59: Headless Mode for CI

CI has no screen, so tests must run headless. Make `Core/BaseTest.cs` read an environment variable instead of hard-coding `Headless = false`:

```csharp
Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = Environment.GetEnvironmentVariable("HEADLESS") == "true"
});
```

Locally, `HEADLESS` isn't set, so the browser shows. In the pipeline, set it to `true`:

```yaml
variables:
  HEADLESS: 'true'
```

Add the test step:

```yaml
  - script: dotnet test --configuration Release --no-build
    displayName: Run tests
```

**Exercise:** push the change and watch the test step.

**Check:** the tests pass in the pipeline.

---

## Day 60: Publishing Test Results and Checkpoint 7

Add this step so the results show in the **Tests** tab:

```yaml
  - task: PublishTestResults@2
    condition: succeededOrFailed()
    inputs:
      testResultsFormat: VSTest
      testResultsFiles: '**/*.trx'
```

Add `--logger trx` to your `dotnet test` line so it writes the `.trx` file the task reads.

**Full pipeline for reference:**

```yaml
trigger:
  - main

pool:
  vmImage: ubuntu-latest

variables:
  HEADLESS: 'true'

steps:
  - task: UseDotNet@2
    inputs:
      packageType: sdk
      version: '10.x'

  - pwsh: |
      dotnet build --configuration Release
      pwsh bin/Release/net10.0/playwright.ps1 install --with-deps chromium
    displayName: Build and install browsers

  - script: dotnet test --configuration Release --no-build --logger trx
    displayName: Run tests

  - task: PublishTestResults@2
    condition: succeededOrFailed()
    inputs:
      testResultsFormat: VSTest
      testResultsFiles: '**/*.trx'
```

**Checkpoint 7:** make a test fail on purpose, push it, and confirm the pipeline goes red and shows the failure in the Tests tab. Then fix it and confirm the pipeline goes green.

**Done when:** you've seen both the red and green runs.

---

Previous: [Part 6](06-own-tests.md) · Back to [COURSE.md](../COURSE.md)
