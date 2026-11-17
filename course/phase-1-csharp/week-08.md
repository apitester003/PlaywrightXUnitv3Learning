# Week 8: Tooling and Phase 1 wrap-up

This week covers the tools every professional test engineer uses daily: Git and GitHub for version control and code review, NuGet and the `dotnet` CLI for projects and packages, and the debugger. Then you build a complete console tool (Day 47) and finish Phase 1 with interview questions and a self-assessment (Day 48).

Shell output in the Git and CLI lessons was produced by running the commands in scratch folders (Git 2.51, .NET SDK 10). Your commit hashes, dates and paths will differ.

---

## Day 43: Git basics

### 1. What Git is, and the three areas

Git records the history of your project as a chain of **commits**. A commit is a snapshot of all tracked files plus a message, an author, and a link to its parent commit. With Git you can see what changed, go back, work on several ideas in parallel (branches), and share work with a team.

Git is a program on your computer. GitHub (Day 44) is a website that hosts Git repositories. They are different things.

A repository has three areas to understand:

```text
Working directory  --git add-->  Staging area (index)  --git commit-->  Repository (history)
 (your files)                     (what the next commit                   (commits)
                                   will contain)
```

- You edit files in the **working directory**.
- `git add` copies chosen changes to the **staging area**, so you decide exactly what goes into the next commit.
- `git commit` records the staged snapshot permanently in the **repository**.

### 2. One-time setup

Tell Git who you are (this goes into every commit). These commands change your global configuration, which is why they are only shown, not run in the scratch folder:

```shell
git config --global user.name "Your Name"
git config --global user.email "you@example.com"
git config --global init.defaultBranch main
git --version
```

### 3. First repository: `init`, `status`, `add`, `commit`, `log`

```shell
$ mkdir demo && cd demo
$ git init -b main
Initialized empty Git repository in /.../demo/.git/
$ git status
On branch main

No commits yet

nothing to commit (create/copy files and use "git add" to track)
```

Create two files, then look at the status:

```shell
$ echo "# Test Data Tool" > README.md
$ echo 'Console.WriteLine("hi");' > Program.cs
$ git status
On branch main

No commits yet

Untracked files:
  (use "git add <file>..." to include in what will be committed)
	Program.cs
	README.md

nothing added to commit but untracked files present (use "git add" to track)
```

Stage and commit:

```shell
$ git add README.md Program.cs
$ git status --short
A  Program.cs
A  README.md
$ git commit -m "Add README and Program"
[main (root-commit) e78b0e7] Add README and Program
 2 files changed, 2 insertions(+)
 create mode 100644 Program.cs
 create mode 100644 README.md
$ git log --oneline
e78b0e7 Add README and Program
```

Short status codes: `??` untracked, `A` added (staged), `M` modified, `UU` conflict. The left column is the staging area, the right column is the working directory.

Good commit messages: a short imperative summary ("Add login page tests", not "stuff" or "fixed things"). Commit small, related changes together so each commit tells one story.

### 4. Seeing changes: `diff`

```shell
$ echo 'Console.WriteLine("hello");' > Program.cs
$ git diff
diff --git a/Program.cs b/Program.cs
index ec4b103..4a9f396 100644
--- a/Program.cs
+++ b/Program.cs
@@ -1 +1 @@
-Console.WriteLine("hi");
+Console.WriteLine("hello");
$ git add -A
$ git commit -m "Change greeting"
[main e9d13a4] Change greeting
 1 file changed, 1 insertion(+), 1 deletion(-)
```

- `git diff` shows unstaged changes (working directory vs. staging area).
- `git diff --staged` shows what is staged for the next commit.
- Lines starting with `-` were removed, lines starting with `+` were added.
- `git add -A` stages every change (new, modified, deleted). Prefer `git add <path>` when you only want some of them, and **always run `git status` before committing** so you do not commit by accident build output or secrets.

### 5. `.gitignore`: files Git should never track

A .NET project creates `bin/` and `obj/` folders full of generated files. They do not belong in history.

```shell
$ mkdir bin obj; echo x > bin/a.dll; echo x > obj/b.dll
$ git status --short
?? bin/
?? obj/
$ printf 'bin/\nobj/\n*.user\n' > .gitignore
$ git status --short
?? .gitignore
$ git add .gitignore
$ git commit -m "Add .gitignore"
[main 11092d8] Add .gitignore
 1 file changed, 3 insertions(+)
 create mode 100644 .gitignore
```

After the `.gitignore` exists, `bin/` and `obj/` no longer show up. Pattern rules: `bin/` matches a folder named bin; `*.user` matches by extension; `#` starts a comment; `!pattern` re-includes.

The .NET SDK can generate a complete, ready-made file for .NET projects:

```shell
$ dotnet new gitignore
The template "dotnet gitignore file" was created successfully.
$ grep -n "^\[Bb\]in" .gitignore
33:[Bb]in/
```

Important: `.gitignore` only affects **untracked** files. If a file is already committed, ignoring it does nothing; remove it from tracking first (the file stays on disk):

```shell
$ git rm --cached a.txt
rm 'a.txt'
```

Never commit secrets (passwords, tokens, `.env` files with real credentials). If one gets committed, rotating the secret is the real fix; deleting it in a later commit does not remove it from history.

### 6. Undoing and parking work

```shell
$ git restore a.txt                # throw away unstaged edits to a.txt (cannot be undone!)
$ git restore --staged a.txt       # unstage, but keep the edits in the file
$ git stash                        # park all uncommitted work, giving you a clean tree
Saved working directory and index state WIP on main: f4c38ae Add a
$ git stash pop                    # bring the parked work back
Dropped refs/stash@{0} (4d078d02ca11e69369f40e9a01372aa55867027b)
$ git commit --amend -m "Add wip line"    # rewrite the LAST commit (only before sharing it)
$ git revert --no-edit HEAD        # add a NEW commit that undoes the previous one
[main 6bf4390] Revert "Add wip line"
 1 file changed, 1 deletion(-)
```

(Some output lines were omitted for brevity.) The rule of thumb: `restore`, `stash` and `--amend` deal with your local, unshared work. Once a commit is shared, undo it with `revert`, which keeps history honest.

### 7. Branches

A **branch** is a movable label pointing at a commit. Creating one is instant and cheap. You create a branch for each task, work on it, and then merge it back.

```shell
$ git branch feature/login        # create the branch (does not move you)
$ git switch feature/login        # move onto it
Switched to branch 'feature/login'
$ git switch -c feature/b         # create and switch in one step
Switched to a new branch 'feature/b'
$ git branch                      # list local branches; * marks the current one
  feature/login
* main
```

(`git switch` is the modern command; `git checkout` still works and does more things.)

### 8. Merging

**Fast-forward merge.** If `main` has no new commits since you branched, Git just moves the `main` label forward:

```shell
$ git switch main
$ git merge feature/b
Updating a5af66c..6cb10ca
Fast-forward
 b.txt | 1 +
 1 file changed, 1 insertion(+)
 create mode 100644 b.txt
$ git log --oneline --decorate
6cb10ca (HEAD -> main, feature/b) Add b
a5af66c Add a
$ git branch -d feature/b          # delete a merged branch
Deleted branch feature/b (was 6cb10ca).
```

**Three-way merge.** If both branches have new commits, Git creates a **merge commit** with two parents:

```shell
$ git merge feature/login -m "Merge feature/login"
Merge made by the 'ort' strategy.
 login.txt | 1 +
 1 file changed, 1 insertion(+)
 create mode 100644 login.txt
$ git log --oneline --graph
*   fb95a28 Merge feature/login
|\  
| * f2759d3 Add login notes
* | dca5c85 Update README
|/  
* e9d13a4 Change greeting
* 11092d8 Add .gitignore
* e78b0e7 Add README and Program
```

`git branch -d` refuses to delete a branch that has unmerged work (`error: the branch 'feature/q' is not fully merged`). `-D` forces the deletion; use it only when you are sure you want to lose those commits.

### 9. Resolving a merge conflict

A conflict happens when both branches changed the **same lines**. Git cannot decide, so it stops and asks you. Here `main` set `Timeout=45` and `feature/timeout` set `Timeout=60` in the same line:

```shell
$ git merge feature/timeout
Auto-merging settings.txt
CONFLICT (content): Merge conflict in settings.txt
Automatic merge failed; fix conflicts and then commit the result.
$ git status --short
UU settings.txt
$ cat settings.txt
BaseUrl=https://dev.example.com
<<<<<<< HEAD
Timeout=45
=======
Timeout=60
>>>>>>> feature/timeout
```

Read the markers:

- `<<<<<<< HEAD` to `=======`: your current branch's version (`main`).
- `=======` to `>>>>>>> feature/timeout`: the incoming branch's version.

Resolve in four steps: (1) edit the file to the content you actually want and **delete all three marker lines**; (2) `git add` the file; (3) `git commit`; (4) run the tests. Here we keep 60:

```shell
$ cat settings.txt
BaseUrl=https://dev.example.com
Timeout=60
$ git add settings.txt
$ git commit -m "Merge feature/timeout, keep 60"
[main 91fceb2] Merge feature/timeout, keep 60
$ git log --oneline --graph
*   91fceb2 Merge feature/timeout, keep 60
|\  
| * 8daea74 Raise timeout to 60
* | 5fa250c Raise timeout to 45
|/  
* f761f9d Add settings
```

To give up instead, run `git merge --abort`. VS Code and Visual Studio both show conflicts with buttons such as "Accept Current Change" and "Accept Incoming Change", but they only edit the same file for you; you still `add` and `commit`.

### 10. Rebase (concept)

`rebase` is an alternative to merging: it **replays** your branch's commits on top of the latest `main`, giving a straight line without a merge commit. Before: `main` has commit B, and `feature/x` (branched from A) has commit X. After rebasing:

```shell
$ git switch feature/x
$ git rebase main
Rebasing (1/1)Successfully rebased and updated refs/heads/feature/x.
$ git log --oneline --graph --all
* c707d11 X
* a3bf92a B
* a420841 A
```

Notice the hash of `X` changed from its original (commits are immutable; rebase creates new ones). This leads to the golden rule: **never rebase commits that other people already have** (that you have pushed and shared). Rebase your own private branch to tidy it; merge shared work. Many teams use "squash and merge" on GitHub to keep `main` linear; you will see it tomorrow.

### 11. Cheat sheet

| Goal | Command |
|---|---|
| New repo | `git init -b main` |
| What changed? | `git status`, `git diff`, `git diff --staged` |
| Stage / commit | `git add <path>`, `git commit -m "msg"` |
| History | `git log --oneline --graph --decorate` |
| New branch | `git switch -c name` |
| Merge | `git switch main` then `git merge name` |
| Abort a merge | `git merge --abort` |
| Discard unstaged edits | `git restore <path>` |
| Park work | `git stash`, `git stash pop` |
| Undo shared commit | `git revert <commit>` |

### Common mistakes

- Committing `bin/` and `obj/` (no `.gitignore`) or secrets.
- Running `git add .` and `git commit` without checking `git status` first.
- Leaving conflict markers (`<<<<<<<`) in a file and committing it. Search for `<<<<<<<` before you commit.
- Rebasing a branch that others already pulled, or using `git restore` on work you wanted to keep (it is irreversible).

### Why this matters for test automation

Test code is code: it is reviewed, versioned, and run from a repository by CI. Branches let you write new tests without breaking the team's `main`. A correct `.gitignore` keeps `bin/`, `obj/`, Playwright traces/videos/screenshots, and credentials out of the history.

### Interview questions

**Q: What is the difference between Git and GitHub?**
Git is the distributed version control tool that runs locally; GitHub is a hosting service for Git repositories adding pull requests, issues, and CI.

**Q: What is the staging area?**
A holding area where you assemble the next commit; `git add` puts changes there, `git commit` records them.

**Q: Merge vs rebase?**
Merge joins histories with a merge commit and preserves the real branching. Rebase replays commits on top of another branch for a linear history but rewrites those commits, so do not rebase shared commits.

**Q: How do you resolve a merge conflict?**
Open the conflicted files, edit out the markers keeping the correct result, `git add` the files, and `git commit` (or `git merge --abort` to cancel).

**Q: What does `.gitignore` do, and does it affect already tracked files?**
It lists patterns of files Git should not track. It does not affect files already tracked; those need `git rm --cached`.

### Exercise

1. In a scratch folder run `git init -b main`, create a console app with `dotnet new console`, run `dotnet new gitignore`, and check with `git status --short` that `bin/` and `obj/` do not appear. Commit.
2. Create branch `feature/greeting`, change the message in `Program.cs`, commit, switch back to `main`, and merge with a fast-forward. Delete the branch.
3. Create a deliberate conflict: change the same line differently on two branches, merge, and resolve it. Check `git log --oneline --graph`.
4. Make an edit, `git stash`, confirm the tree is clean, and `git stash pop`.

### Check

You can explain the three areas (working directory, staging, repository) from memory, name the command that shows which files are staged, and recover from a conflict without help.

---

## Day 44: GitHub branches and pull requests

### 1. Local versus remote

Your repository lives on your machine. A **remote** is another copy of it on a server, by convention named `origin`. GitHub is the most common host. You send commits to the remote with `push` and get others' commits with `fetch`/`pull`.

The commands below were run against a bare repository in a scratch folder that stands in for GitHub, because the Git side of the workflow is identical: push, fetch, pull, branches. (The folder paths in the output are shortened.)

### 2. Getting a repository: `clone`

```shell
$ git clone remote.git w2
Cloning into 'w2'...
done.
```

With GitHub you clone a URL: `git clone <url>` (HTTPS or SSH, copied from the green "Code" button on the repository page). With the GitHub CLI: `gh repo clone owner/name`. `clone` also sets up `origin` and checks out the default branch.

### 3. The feature-branch workflow

The standard team flow, which every pull request process builds on:

1. Start from an up-to-date `main`: `git switch main` then `git pull`.
2. Create a branch for **one** piece of work: `git switch -c feature/login-tests`.
3. Edit, test, and commit in small steps.
4. Push the branch: `git push -u origin feature/login-tests`.
5. Open a **pull request (PR)** asking to merge your branch into `main`.
6. Teammates review; you push more commits to answer feedback (the PR updates by itself). CI runs the tests.
7. After approval and passing checks, the PR is merged. The branch is deleted.
8. Update your local `main` and delete the old local branch.

```shell
$ git switch -c feature/login-tests
Switched to a new branch 'feature/login-tests'
$ git add . && git commit -m "Add login tests"
$ git push -u origin feature/login-tests
To remote.git
 * [new branch]      feature/login-tests -> feature/login-tests
branch 'feature/login-tests' set up to track 'origin/feature/login-tests'.
```

`-u` (upstream) links your local branch to the remote one, so later plain `git push` and `git pull` know where to go. `git remote -v` lists your remotes:

```shell
$ git remote -v
origin	remote.git (fetch)
origin	remote.git (push)
```

### 4. `fetch` and `pull`

- `git fetch` downloads new commits and updates the remote-tracking branches (like `origin/main`) but does **not** change your files or your branches. It is always safe.
- `git pull` is `fetch` followed by an integration (merge by default; `git pull --rebase` replays your local commits instead).

```shell
$ git fetch
From remote
 * [new branch]      feature/login-tests -> origin/feature/login-tests
$ git branch -r
  origin/HEAD -> origin/main
  origin/feature/login-tests
  origin/main
$ git log --oneline origin/feature/login-tests
8ebf5ca Add login tests
f0c8c09 Initial commit
```

`git status` tells you if you are ahead or behind your remote branch ("Your branch is up to date with 'origin/main'" or "ahead/behind by N commits").

### 5. Opening a pull request

**With the web UI.** After you push a branch, the repository page on GitHub normally offers a prompt to create a pull request for the recently pushed branch, and the "Pull requests" tab has a "New pull request" button where you choose the base branch (usually `main`) and your branch. Fill in a title and a description, and then create the pull request. The page that appears shows the conversation, the commits, the changed files (the diff), and the status of automated checks. Reviewers can comment on specific lines and approve or request changes. Exact button names and layout can change, so rely on the concepts, not on the pixel positions.

**With the GitHub CLI (`gh`).** `gh` is a separate command-line tool; sign in once with `gh auth login`. Then, from your pushed branch:

```shell
gh pr create --base main --title "Add login tests" --body "Adds tests for valid and invalid login. Closes #12"
gh pr create --fill                 # use the commit messages for title and body
gh pr list                          # open pull requests of the repository
gh pr view 15 --web                 # open PR #15 in the browser
gh pr checkout 15                   # fetch and switch to the branch of PR #15 to try it locally
gh pr merge 15 --squash --delete-branch
```

`gh pr create` prints the URL of the new pull request when it succeeds. Writing `Closes #12` (or `Fixes #12`) in the body links the issue and closes it automatically when the PR is merged. `gh pr merge` accepts `--merge`, `--squash`, or `--rebase` to choose the merge method, and `--delete-branch` removes the branch afterwards. If the branch is not pushed yet, `gh pr create` asks where to push it.

### 6. Review and merge methods

What a reviewer checks in a test-code PR: Do the tests assert something meaningful? Are names clear? Are waits and selectors robust (no fixed sleeps)? Is test data separated from logic? No secrets? Do all checks pass?

When merging, GitHub offers three methods (what you may use depends on repository settings):

| Method | Result in `main` |
|---|---|
| Create a merge commit | All branch commits plus a merge commit |
| Squash and merge | One new commit containing all the branch's changes |
| Rebase and merge | Branch commits replayed on `main`, no merge commit |

Teams often protect `main` with **branch protection rules** (required reviews, required passing checks, no direct pushes). If a push to `main` is rejected, that is usually why; the answer is always "make a PR".

### 7. After the merge: clean up

Once the PR is merged on GitHub, update your local repository. Here the merge already happened on the remote, and we sync:

```shell
$ git switch main
Switched to branch 'main'
Your branch is up to date with 'origin/main'.
$ git pull
From remote
   f0c8c09..ca7c2df  main       -> origin/main
Updating f0c8c09..ca7c2df
Fast-forward
 LoginTests.cs | 1 +
 1 file changed, 1 insertion(+)
 create mode 100644 LoginTests.cs
$ git branch -d feature/login-tests
Deleted branch feature/login-tests (was 8ebf5ca).
$ git fetch --prune
$ git branch -a
* main
  remotes/origin/HEAD -> origin/main
  remotes/origin/main
```

`git fetch --prune` removes remote-tracking branches that were deleted on the server. If you choose "squash and merge", your original commits are not in `main` (a new single commit is), so `git branch -d` may refuse with "not fully merged"; after confirming the PR was merged, `git branch -D` is appropriate.

To delete a remote branch yourself (GitHub can also do it in the PR page): `git push origin --delete feature/login-tests`.

### 8. Conflicts in a PR, and keeping a branch current

If `main` moves ahead while your PR is open and touches the same lines, GitHub reports that the branch has conflicts. Fix them locally:

```shell
git switch feature/login-tests
git fetch
git merge origin/main        # or: git rebase origin/main
# resolve conflicts as in Day 43, then:
git add <files>
git commit                   # (a rebase uses 'git rebase --continue' instead)
git push                     # after a rebase you must use: git push --force-with-lease
```

`--force-with-lease` overwrites your own remote branch only if nobody else has pushed to it meanwhile; it is the safe way to push rewritten history. Never force-push `main`.

### 9. Forks (open-source style)

When you cannot push to a repository, you **fork** it (GitHub copies it under your account), push your branch to the fork, and open a pull request from the fork to the original ("upstream"). The mechanics of branch and PR are the same. `gh repo fork` exists for this; check `gh repo fork --help`.

### Common mistakes

- Working directly on `main` instead of a feature branch.
- Huge pull requests that mix unrelated changes: reviewers miss problems. One purpose per PR.
- Forgetting `git pull` before starting, then fighting avoidable conflicts.
- Force-pushing with plain `--force`, overwriting someone else's commits.
- Pushing credentials or large generated files and then thinking a follow-up commit removes them.

### Why this matters for test automation

Pull requests are where test quality is enforced: reviews catch flaky patterns, and CI (Azure Pipelines or GitHub Actions, later in the course) runs your test suite on every PR so a broken test never reaches `main`. In job interviews, "describe your PR workflow" is a common question.

### Interview questions

**Q: What is a pull request?**
A request to merge one branch into another on the hosting service, providing a place for code review, discussion, and automated checks before the merge.

**Q: `git fetch` vs `git pull`?**
`fetch` only downloads remote changes; `pull` is `fetch` plus merge (or rebase) into your current branch.

**Q: What does `git push -u origin branch` do?**
Pushes the branch to `origin` and sets it as the upstream of the local branch so future `git push`/`git pull` need no arguments.

**Q: Squash vs merge commit?**
Squash combines the PR's commits into one commit on `main` (clean history, loses the individual commits). A merge commit keeps them and adds a merge commit.

**Q: How do you keep a long-running branch up to date?**
Regularly merge (or rebase) `main` into it and resolve conflicts early; use `--force-with-lease` after a rebase.

### Exercise

1. Create a scratch bare remote (`git init --bare -b main remote.git`), clone it twice into folders `a` and `b`, and act as two developers: in `a` create a branch, commit, and push; in `b` `fetch` and inspect `git branch -r` and `git log origin/<branch>`.
2. Merge that branch into `main` from `a` and push `main`; in `b` run `git pull` and confirm the file arrives.
3. If you have a GitHub account, create a private practice repository, push a branch, open a PR from the web UI, then from the terminal with `gh pr create --fill`, and merge one with each method to see the difference in `git log`.
4. Write down the 8-step feature-branch workflow in your own words.

### Check

You can list the commands from "start a task" to "clean up after merge" in order, and explain why you should not push directly to `main`.

---

## Day 45: NuGet and the `dotnet` CLI

### 1. Concepts

- **The .NET SDK** contains the compiler, the runtime and the `dotnet` command-line tool.
- A **project** is a folder with a `.csproj` file (XML) describing how to build it. A **solution** (`.sln` or `.slnx`) groups several projects.
- **NuGet** is .NET's package manager. A **package** is a ZIP (`.nupkg`) of compiled libraries and metadata, published on a feed (the default one is nuget.org). Your test project gets xUnit, Playwright and others as packages.
- A **package reference** is a line in the `.csproj`. **Restore** downloads the referenced packages (and their dependencies) into a cache on your machine. `dotnet build`/`run`/`test` run a restore automatically when needed.

### 2. Checking your tools

```shell
$ dotnet --version
10.0.403
$ dotnet --list-sdks
10.0.403 [/usr/share/dotnet/sdk]
```

`dotnet --info` prints more detail (SDK, runtimes, OS). A `global.json` file next to your solution can pin the SDK version or select the test runner (below).

### 3. Creating projects: `dotnet new`

`dotnet new list` shows the installed templates (shortened):

```shell
$ dotnet new list
Template Name                     Short Name   Language    Tags
--------------------------------  -----------  ----------  ----------------
Class Library                     classlib     [C#],F#,VB  Common/Library
Console App                       console      [C#],F#,VB  Common/Console
dotnet gitignore file             gitignore    ...         Config
...
```

Create a solution with two projects in a typical layout:

```shell
$ dotnet new sln -n Demo
The template "Solution File" was created successfully.

$ dotnet new console -n Demo.App -o src/Demo.App
The template "Console App" was created successfully.
...
Restore succeeded.
```

The new `console` project's `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

The xUnit v3 project template is not built into the SDK. Install it once, then use it:

```shell
$ dotnet new install xunit.v3.templates
Success: xunit.v3.templates@4.0.3 installed the following templates:
Template Name                   Short Name        Language    Tags
------------------------------  ----------------  ----------  ----------------------
xUnit.net v3 Extension Project  xunit3-extension  [C#],F#,VB  Test/xUnit/Desktop/Web
xUnit.net v3 Test Project       xunit3            [C#],F#,VB  Test/xUnit/Desktop/Web

$ dotnet new xunit3 -n Demo.Tests -o tests/Demo.Tests -f net10.0
The template "xUnit.net v3 Test Project" was created successfully.
...
Successfully modified global.json.
```

Two observations from running it with this version of the template: without `-f net10.0` the project targeted `net8.0` (and failed to run on a machine that only has the .NET 10 runtime), so check `TargetFramework` after generating; and the template wrote a `global.json` at the solution root selecting the Microsoft.Testing.Platform runner:

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

Template versions change, so always open the generated `.csproj` and look.

### 4. Solutions and project references

```shell
$ dotnet sln add src/Demo.App tests/Demo.Tests
Project `src/Demo.App/Demo.App.csproj` added to the solution.
Project `tests/Demo.Tests/Demo.Tests.csproj` added to the solution.
$ dotnet add tests/Demo.Tests reference src/Demo.App
Reference `..\..\src\Demo.App\Demo.App.csproj` added to the project.
$ dotnet sln list
Project(s)
----------
src/Demo.App/Demo.App.csproj
tests/Demo.Tests/Demo.Tests.csproj
$ dotnet list tests/Demo.Tests reference
Project reference(s)
--------------------
../../src/Demo.App/Demo.App.csproj
```

The test project's `.csproj` now has `<ProjectReference Include="..\..\src\Demo.App\Demo.App.csproj" />`, so tests can use the classes of the app project.

### 5. NuGet packages: add, list, update, remove

```shell
$ dotnet add src/Demo.App package Newtonsoft.Json --version 13.0.3
info : Adding PackageReference for package 'Newtonsoft.Json' into project '.../Demo.App.csproj'.
...
info : PackageReference for package 'Newtonsoft.Json' version '13.0.3' added to file '.../Demo.App.csproj'.
```

(The real command also prints many `GET`/`OK` lines for the feed; they were omitted.) Without `--version`, the latest stable version is chosen. The `.csproj` gets:

```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
</ItemGroup>
```

You can also edit that line by hand and run `dotnet restore`. Now inspect:

```shell
$ dotnet list src/Demo.App package
Project 'Demo.App' has the following package references
   [net10.0]: 
   Top-level Package      Requested   Resolved
   > Newtonsoft.Json      13.0.3      13.0.3  

$ dotnet list src/Demo.App package --outdated
Project `Demo.App` has the following updates to its packages
   [net10.0]: 
   Top-level Package      Requested   Resolved   Latest
   > Newtonsoft.Json      13.0.3      13.0.3     13.0.4
```

Other useful variations: `dotnet list package --include-transitive` (also show the dependencies of your dependencies), `dotnet list package --vulnerable` (known security advisories), and `dotnet remove src/Demo.App package Newtonsoft.Json`. To update, run `dotnet add package <name>` again with the version you want (or edit the `Version`).

Versions use **semantic versioning**: `Major.Minor.Patch`. A major bump may contain breaking changes; minor and patch bumps should not. A suffix like `-beta.1` marks a prerelease. For test projects, pin versions explicitly, and keep related packages in step (for example, all `Microsoft.Extensions.*` packages at the same version, and the Playwright package versions consistent).

Typical package references of a Playwright + xUnit v3 test project (see the `.csproj` of this course's project): `xunit.v3`, `Microsoft.Playwright.Xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`. Package IDs and versions change over time, so copy the exact names from the package page or from your course project, not from memory.

### 6. Restore, build, run, clean

```shell
$ dotnet restore
  Determining projects to restore...
  All projects are up-to-date for restore.
$ dotnet build
  Determining projects to restore...
  All projects are up-to-date for restore.
  TestDataTool -> /.../TestDataTool/bin/Debug/net10.0/TestDataTool.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:00.98
$ dotnet run
Hello, World!
```

- `dotnet build` compiles to `bin/<Configuration>/<framework>/`. The default configuration is **Debug** (extra debugging information, fewer optimizations). `-c Release` builds optimized output.
- `dotnet run` builds (if needed) and starts the app. Arguments for your program go after `--`: `dotnet run -- generate --count 5`.
- `dotnet clean` removes build output. If strange stale errors appear, delete the `bin` and `obj` folders and rebuild.
- `--no-restore` and `--no-build` skip steps that were already done (useful in CI).

### 7. Testing: `dotnet test`

With xUnit v3 and the Microsoft.Testing.Platform runner, the test project is itself an executable (note `OutputType` `Exe` in the csproj) and `dotnet test` runs it:

```shell
$ dotnet test
Running tests from /.../tests/Demo.Tests/bin/Debug/net10.0/Demo.Tests.dll (net10.0|x64)
/.../Demo.Tests.dll (net10.0|x64) passed (333ms)

Test run summary: Passed!
  total: 1
  failed: 0
  succeeded: 1
  skipped: 0
  duration: 517ms
```

When a test fails, the output names the test, the assertion message and the line, and the process exits with a non-zero code (this is how CI knows the run failed). Here the test asserts `Assert.Equal(5, 2 + 2)`:

```shell
$ dotnet test
Running tests from /.../tests/Demo.Tests/bin/Debug/net10.0/Demo.Tests.dll (net10.0|x64)
failed Demo.Tests.UnitTest1.Fails (20ms)
  from /.../Demo.Tests.dll (net10.0|x64)
  Assert.Equal() Failure: Values differ
  Expected: 5
  Actual:   4
    at Demo.Tests.UnitTest1.Fails() in /.../UnitTest1.cs:9
...
Test run summary: Failed!
  total: 2
  failed: 1
  succeeded: 1
  skipped: 0
  duration: 589ms
Test run completed with non-success exit code: 2
```

Filtering by name worked in this setup:

```shell
$ dotnet test --filter "FullyQualifiedName~Passes"
Test run summary: Passed!
  total: 1
  failed: 0
  succeeded: 1
  skipped: 0
  duration: 577ms
```

(Filter syntax and extra runner options depend on the SDK and the runner in use; `dotnet test --help` and the runner's own `--help` list what your versions accept.) Also useful: `dotnet test --no-build` and `dotnet test -c Release`. Run `dotnet test` from the folder of the project (or solution) you want to test.

### 8. Publishing: `dotnet publish`

`publish` produces the deployable output of an app: your compiled code, the package DLLs it needs, and the config files.

```shell
$ dotnet publish -c Release -o out
  TestDataTool -> /.../bin/Release/net10.0/TestDataTool.dll
  TestDataTool -> /.../out/
$ ls out
Humanizer.dll
TestDataTool
TestDataTool.deps.json
TestDataTool.dll
TestDataTool.pdb
TestDataTool.runtimeconfig.json
```

(That run had the `Humanizer.Core` package referenced, which is why `Humanizer.dll` is there.) The `TestDataTool` executable launches the app on a machine that has the .NET runtime installed. Self-contained publishing (`--self-contained`, with a `-r` runtime identifier) bundles the runtime too; for test code, you mostly use `build` and `test`, and `publish` shows up when you ship a tool.

### 9. Where packages live

Restored packages are cached once per user in a global packages folder (on Linux, `~/.nuget/packages`; on Windows, `%USERPROFILE%\.nuget\packages`), and the installed package folders include the version number, so many projects share one download. Restore also writes `obj/project.assets.json` listing everything resolved. Package sources are configured in `NuGet.config` files (the default is nuget.org); companies often add a private feed there. `dotnet nuget list source` shows the configured sources.

### 10. Cheat sheet

| Goal | Command |
|---|---|
| New console app | `dotnet new console -n Name` |
| New solution / add project | `dotnet new sln -n Name`, `dotnet sln add path` |
| Reference another project | `dotnet add <proj> reference <other>` |
| Add package | `dotnet add <proj> package Name [--version X]` |
| List / outdated | `dotnet list package`, `dotnet list package --outdated` |
| Remove package | `dotnet remove <proj> package Name` |
| Restore / build / clean | `dotnet restore`, `dotnet build [-c Release]`, `dotnet clean` |
| Run | `dotnet run [-- args]` |
| Test | `dotnet test` |
| Publish | `dotnet publish -c Release -o out` |

### Common mistakes

- Typing `dotnet run` in a folder with several projects (it asks you to specify one); use `--project path` or `cd` into the project.
- Mixing versions of related packages, causing runtime `MissingMethodException`-style errors or restore warnings.
- Editing `bin/` or `obj/` content or committing it.
- Assuming a template's target framework matches your SDK: always check `TargetFramework` after `dotnet new`.

### Why this matters for test automation

CI pipelines run exactly these commands: `dotnet restore`, `dotnet build`, `dotnet test`. If you can run the full chain locally and read its output, you can debug pipeline failures. Understanding package references and versions prevents the "works on my machine" dependency problems that hit Playwright and xUnit projects.

### Interview questions

**Q: What is NuGet?**
.NET's package manager and the public feed (nuget.org) of packages; projects reference packages in the `.csproj` and `dotnet restore` downloads them.

**Q: Difference between `dotnet build` and `dotnet publish`?**
`build` compiles into `bin/`; `publish` produces the complete, deployable set of files for running the app elsewhere.

**Q: What is in a `.csproj` for packages?**
`<PackageReference Include="Name" Version="x.y.z" />` items inside an `ItemGroup`.

**Q: How does CI know a test run failed?**
`dotnet test` returns a non-zero exit code when any test fails (or the run fails).

**Q: What is a transitive dependency?**
A package your package depends on. `dotnet list package --include-transitive` shows them.

### Exercise

1. Create a solution `Shop` with a class library `Shop.Core` and a console `Shop.App`; reference the library from the app. Run `dotnet build` from the solution folder.
2. Add `Newtonsoft.Json` (or another small package) to `Shop.Core` with a pinned version, list it, check for updates with `--outdated`, and then remove it.
3. Install `xunit.v3.templates`, create a test project with `-f net10.0`, add a failing test, run `dotnet test`, read the failure message, fix it, and rerun.
4. Publish `Shop.App` in Release mode to `out` and run the produced executable.

### Check

You can explain what `dotnet restore` does and where packages end up, and name the command sequence a CI job runs to build and test a solution.

---

## Day 46: Debugging in your IDE

### 1. Why debug instead of printing

`Console.WriteLine` debugging tells you what you decided to print in advance. A **debugger** lets you pause a running program at any line, look at all variables, and move forward one step at a time. For test failures ("why is this value empty at line 42?") it is much faster. The ideas are the same in VS Code and Visual Studio; the shortcut keys are the same too.

### 2. A buggy program to practice with

The program below has two bugs. Run it first:

```csharp
// Complete program (contains deliberate bugs)
var prices = new List<decimal> { 19.99m, 5.00m, 12.50m, 0m, 7.25m };

Console.WriteLine($"Total:   {CalculateTotal(prices)}");     // expected 44.74
Console.WriteLine($"Average: {CalculateAverage(prices)}");

for (int orderId = 1; orderId <= 100; orderId++)
{
    decimal discount = Discount(orderId);
    if (discount < 0)
        Console.WriteLine($"Negative discount for order {orderId}: {discount}");
}

static decimal CalculateTotal(List<decimal> items)
{
    decimal sum = 0;
    for (int i = 1; i < items.Count; i++)      // bug 1
        sum += items[i];
    return sum;
}

static decimal CalculateAverage(List<decimal> items)
{
    return CalculateTotal(items) / items.Count;
}

static decimal Discount(int orderId)
{
    decimal percent = orderId % 10 * 2;        // 0..18 for normal orders
    if (orderId == 73) percent = 150;          // bug 2: bad data for one order
    return 30 - percent;                       // should never be negative
}
```

Expected output (a total below 44.74 and one negative discount):

```text
Total:   24.75
Average: 4.95
Negative discount for order 73: -120
```

Observe: `Total` skips the first price (the loop starts at `i = 1`), and the average inherits the error. Order 73 produces a negative discount, but only that one order out of 100, so stopping on every loop iteration would be painful.

### 3. Core concepts

| Concept | Meaning | Keys (VS Code and Visual Studio) |
|---|---|---|
| Start debugging | Build and run with the debugger attached | `F5` |
| Stop | End the debug session | `Shift+F5` |
| Breakpoint | A line where execution pauses | `F9` toggles on the current line (or click left of the line number) |
| Continue | Run until the next breakpoint | `F5` |
| Step Over | Run the current line; do not enter methods it calls | `F10` |
| Step Into | If the line calls a method, go inside it | `F11` |
| Step Out | Run until the current method returns | `Shift+F11` |

Note: Starting debugging from VS Code needs the C# extension (it supplies the .NET debugger). When you press `F5` the first time, VS Code may ask you to select the environment and generate `launch.json`/`tasks.json`; accepting is fine. Menu names differ between versions, so if something looks different from what is described, search the editor's command palette (`Ctrl+Shift+P` in VS Code) for "Debug".

### 4. Walkthrough 1: find bug 1 with breakpoints and stepping

1. Set a breakpoint on the line `sum += items[i];` (the red dot appears).
2. Press `F5`. Execution stops there before the line runs. The line is highlighted.
3. Look at the variables panel: in VS Code the **Variables** view (with Locals); in Visual Studio the **Locals** and **Autos** windows. You see `i = 1`, `sum = 0` and `items`. Expand `items` to see its five elements.
4. Why is `i` 1 on the first stop? That is the bug: the first element (index 0) was never added.
5. Press `F5` (Continue) a few times and watch `sum` grow (5.00, 17.50, 17.50, 24.75).
6. Fix the loop to `int i = 0` and rerun: `Total: 44.74`.

### 5. Walkthrough 2: stepping over, into, out

Put a breakpoint on `Console.WriteLine($"Average: ...")`. When it stops:

- `F10` (Step Over) runs the whole line, including the call to `CalculateAverage`, and pauses on the next line.
- Rerun and use `F11` (Step Into) instead: the debugger enters `CalculateAverage`, and `F11` again enters `CalculateTotal`.
- `Shift+F11` (Step Out) finishes `CalculateTotal` and returns you to its caller.
- The **Call Stack** view shows the chain of calls that led to the current line (`CalculateTotal` called by `CalculateAverage` called by the top-level code). Click a frame to see that frame's variables.

Stepping into library code (Console.WriteLine, Playwright) is not useful; use Step Over for those lines. VS Code and Visual Studio have a "Just My Code" setting that normally skips stepping into framework code.

### 6. Watch expressions

A **watch** is an expression the debugger re-evaluates at every stop: for example `sum`, `items.Count`, `items[i] * 2`, `orderId % 10`. Add one in the **Watch** view (VS Code: Watch panel, "+" button; Visual Studio: Watch window, type in an empty row). You can also hover over a variable in the editor to see its value, and use the **Debug Console** (VS Code) or **Immediate Window** (Visual Studio) to evaluate or call things while paused, for example `items.Sum()` (a LINQ call may need the `using` in scope).

### 7. Walkthrough 3: conditional breakpoints (bug 2)

A breakpoint inside the `for` loop would stop 100 times. Instead make it stop only when it matters:

- **Conditional breakpoint**: set a breakpoint on `decimal discount = Discount(orderId);`, then edit the breakpoint to add a **condition** `orderId == 73`. In VS Code, right-click in the gutter and choose "Add Conditional Breakpoint" (or right-click an existing breakpoint and choose "Edit Breakpoint"), then type the expression. In Visual Studio, right-click the breakpoint and choose Conditions (or hover and use the settings gear), then enter the expression. The program now runs at full speed and pauses only when `orderId` is 73.
- Press `F11` to step into `Discount` and watch `percent` become 150. There is the bad value.
- **Hit count**: pause on the 50th iteration only (VS Code: edit breakpoint, choose "Hit Count"; Visual Studio: breakpoint settings, Conditions > Hit Count).
- **Logpoint / tracepoint**: print a message when the line is reached *without pausing* and without editing code (VS Code: "Add Logpoint", with expressions in braces such as `order {orderId}`; Visual Studio calls it an action/tracepoint in breakpoint settings).

Conditions are ordinary C# expressions, so `orderId == 73`, `name == null`, or `items.Count > 10 && i == items.Count - 1` all work. Conditional breakpoints slow the program somewhat, so avoid them in extremely hot loops.

### 8. Breaking on exceptions

Both editors can pause when an exception is thrown rather than only when it is unhandled. In VS Code, the **Breakpoints** view has options such as "Uncaught Exceptions" and "All Exceptions" (use the latter sparingly: .NET throws many handled exceptions internally). In Visual Studio use the Exception Settings window and tick the exception types you care about. When a test crashes, the stack trace in the failure message plus a break on the exception shows you the exact state.

### 9. Code-based helpers

```csharp
// Complete program
using System.Diagnostics;

Debug.WriteLine("only in Debug builds; visible in the debugger's output window");   // removed from Release builds
Debug.Assert(1 + 1 == 2, "math is broken");                                          // checked only in Debug builds

if (Debugger.IsAttached)
{
    Console.WriteLine("a debugger is attached");
}
else
{
    Console.WriteLine("no debugger attached");            // plain 'dotnet run' prints this
}

// Debugger.Break();   // uncomment to force a pause when a debugger is attached
```

```text
no debugger attached
```

### 10. Debugging tests

- **xUnit tests**: set breakpoints in the test or the code under test and start the test in debug mode from the editor's test UI. In VS Code, the Test Explorer (with the C# Dev Kit extension) has a debug action for tests; in Visual Studio, right-click a test in Test Explorer and choose Debug. Details depend on the extension versions and on how the runner is configured. If the editor tooling gives trouble with the new test runner, a fallback is to run the test project's executable under the debugger or put `Debugger.Break()` in the test.
- **Playwright**: you can set `PWDEBUG=1` as an environment variable before running tests to open the Playwright Inspector (a stepping tool for browser actions), and `await page.PauseAsync();` pauses the script at that point with the browser open. These are Playwright features that you will use in Phase 2; they complement the C# debugger rather than replace it.
- **Async code**: stepping over an `await` line behaves like stepping over any line; the debugger continues after the awaited operation completes. The Call Stack for async methods can look different from synchronous code, but your own frames are shown.

### 11. A debugging routine that works

1. **Reproduce** the failure reliably.
2. **Read the message and stack trace**: which line and which exception?
3. **Form a hypothesis** ("the list is empty here").
4. **Place a breakpoint** just before the suspect line; add a condition if it is hit many times.
5. **Inspect** values (Variables, Watch). Compare with what you assumed.
6. **Fix**, rerun, and **add a test** that would have caught it.
7. Remove temporary logging and breakpoints.

### Common mistakes

- Setting a breakpoint, but debugging a different configuration or an older build (rebuild; make sure the file you edit is the one that runs).
- Stepping into everything with `F11` and getting lost in framework code: use `F10`, and `Shift+F11` to get out.
- Leaving `Debugger.Break()` or noisy `Debug.WriteLine` calls in committed code.
- Using breakpoints in timing-sensitive UI code and then concluding a test "works under the debugger": pausing changes timing.

### Why this matters for test automation

Failing tests are your daily work. The skills that save most time are: reading the stack trace, stopping at the failing line with a condition, and inspecting the real values (the locator text, the response body, the parsed JSON), and combining the debugger with Playwright's own tools (trace viewer, inspector) later in the course.

### Interview questions

**Q: Step over vs step into vs step out?**
Over runs the current line without entering called methods; into enters the called method; out runs until the current method returns.

**Q: What is a conditional breakpoint and when do you use it?**
A breakpoint with an expression that must be true to pause, used for loops or frequently called code where only one case is interesting.

**Q: What is a logpoint?**
A breakpoint that logs a message and continues instead of pausing, so you can trace values without editing code.

**Q: What does the Call Stack show?**
The chain of method calls that led to the current point, letting you inspect each caller's variables.

**Q: Debug vs Release builds?**
Debug includes extra debugging information and has few optimizations; Release is optimized. `Debug.WriteLine` and `Debug.Assert` only run in Debug builds.

### Exercise

1. Create the buggy program (section 2), run it, and confirm the output. Use a breakpoint, step, and the Variables view to find the cause of the wrong total. Fix it.
2. Use a conditional breakpoint `orderId == 73` and step into `Discount`. Write down the values of `percent` at each line.
3. Change `Discount` so it never returns less than 0 (`Math.Max(0, 30 - percent)`) and add a watch to verify the results for orders 1, 10 and 73.
4. Use a logpoint (or temporary `Console.WriteLine`) to print `orderId` whenever `discount < 0`, without stopping.

### Check

You can set a conditional breakpoint, step into a method and out again, add a watch, and explain what the Call Stack panel shows. (You can also do all of this with the keyboard: `F5`, `F9`, `F10`, `F11`, `Shift+F11`.)

---

## Day 47: Phase 1 mini-project: a console test-data tool

### 1. The goal

You will build `TestDataTool`, a command-line program that creates, checks, and summarizes the user test data that automated tests need. It uses nearly everything from Phase 1: classes and records, enums, generics and collections, LINQ, exceptions, delegates and lambdas, async file I/O, JSON, and the `dotnet` CLI. Plan about one hour for the first working version of two or three commands, and use the reference solution to compare and finish. Put it in Git, work on a branch, and (if you have GitHub) open a pull request for it.

### 2. Specification

The program is run as `dotnet run -- <command> [--option value ...]`.

| Command | Options | Behaviour |
|---|---|---|
| `generate` | `--count N` (default 10), `--seed S` (default 1), `--out FILE` (required) | Creates `N` random users with a `Random(seed)` so the same seed gives the same data, and saves them as indented JSON (camelCase names, enums as text). |
| `validate` | `--file FILE` (required) | Loads the file and checks every user. Prints each problem; prints `OK: N users valid` when there are none. |
| `stats` | `--file FILE` (required) | Prints total users, active users, average age (1 decimal), the oldest user, and the count per role. |
| `filter` | `--file FILE` (required), `--role ROLE`, `--min-age N`, `--out FILE` | Keeps users matching all given criteria (a missing option means "no restriction"). Prints a table; if `--out` is given, also saves the result as JSON. |
| `export-csv` | `--file FILE` (required), `--out FILE` (required) | Writes a CSV with a header row; values containing a comma or a quote are quoted. |

The `User` data:

| Field | Type | Rules for `validate` |
|---|---|---|
| `id` | int | unique, greater than 0 |
| `firstName`, `lastName` | string | not empty |
| `email` | string | contains `@`, unique (case-insensitive) |
| `age` | int | from 18 to 120 |
| `role` | enum `Admin`, `Tester`, `Viewer` | any |
| `isActive` | bool | any |

Error handling and exit codes:

| Situation | Message goes to | Exit code |
|---|---|---|
| Success | stdout | 0 |
| `validate` finds problems | stdout | 1 |
| Missing file, unreadable or invalid JSON | stderr, one clear line starting with `Error:` | 1 |
| Unknown command, missing or bad option | stderr, `Error: ...` plus the usage text | 2 |

Program requirements: no crashes with a stack trace for expected problems; file I/O must be asynchronous (`async Task`); use a single shared `JsonSerializerOptions`; keep the code in small classes with one job each.

Acceptance checks (all should hold):

1. `generate --count 20 --seed 7 --out data/users.json` creates the folder `data` if needed, and running it twice produces identical files.
2. `validate` on that file says OK; after you edit the file to break an email or duplicate an id, it lists the problems and the exit code is 1.
3. `filter --role Tester --min-age 30` returns only testers aged 30 or more.
4. `stats` role counts add up to the total.
5. `export-csv` output has one header line plus one line per user.
6. `dotnet run -- nonsense` prints a usage error and exits with 2.

### 3. Starter structure

```text
TestDataTool/
  TestDataTool.csproj            (dotnet new console, net10.0, Nullable + ImplicitUsings enabled)
  Program.cs                     entry point: parse, dispatch, exit codes
  Models/User.cs                 record User + enum Role
  Cli/Options.cs                 parses "--name value" pairs; UsageException
  Services/UserGenerator.cs      Generate(count, seed)
  Services/UserStore.cs          SaveAsync / LoadAsync (JSON)
  Services/UserValidator.cs      Validate(users) -> list of error messages
  Services/UserReports.cs        Stats(users) and ToCsv(users)
```

Set it up:

```shell
dotnet new console -n TestDataTool
cd TestDataTool
dotnet new gitignore
git init -b main
git add . && git commit -m "Create console project"
git switch -c feature/test-data-tool
mkdir Models Cli Services
```

Starter code with the signatures to fill in (this compiles; every method throws until you implement it):

```cs
// Starter: Models/User.cs   (complete, use as is)
namespace TestDataTool.Models;

public enum Role { Admin, Tester, Viewer }

public record User(int Id, string FirstName, string LastName, string Email, int Age, Role Role, bool IsActive);
```

```cs
// Starter: Services/UserGenerator.cs
using TestDataTool.Models;

namespace TestDataTool.Services;

public static class UserGenerator
{
    public static List<User> Generate(int count, int seed) => throw new NotImplementedException();
}
```

```cs
// Starter: Services/UserStore.cs
using TestDataTool.Models;

namespace TestDataTool.Services;

public static class UserStore
{
    public static Task SaveAsync(string path, IEnumerable<User> users) => throw new NotImplementedException();

    public static Task<List<User>> LoadAsync(string path) => throw new NotImplementedException();
}
```

```cs
// Starter: Services/UserValidator.cs
using TestDataTool.Models;

namespace TestDataTool.Services;

public static class UserValidator
{
    public static List<string> Validate(IReadOnlyList<User> users) => throw new NotImplementedException();
}
```

```cs
// Starter: Services/UserReports.cs
using TestDataTool.Models;

namespace TestDataTool.Services;

public static class UserReports
{
    public static List<string> Stats(IReadOnlyList<User> users) => throw new NotImplementedException();

    public static List<string> ToCsv(IEnumerable<User> users) => throw new NotImplementedException();
}
```

Suggested order of work (commit after each step): (1) `Options` and `Program` skeleton that prints the command name; (2) `UserGenerator` + `UserStore` + `generate`; (3) `validate`; (4) `stats`; (5) `filter` (build a `Func<User, bool>` from the options); (6) `export-csv`; (7) error handling and exit codes. Try to write each part yourself before reading the reference.

### 4. Reference solution

Create each file with the exact path shown. This is a complete solution; it was compiled and run for this lesson.

```cs
// File: Models/User.cs
namespace TestDataTool.Models;

public enum Role { Admin, Tester, Viewer }

public record User(int Id, string FirstName, string LastName, string Email, int Age, Role Role, bool IsActive);
```

```cs
// File: Cli/Options.cs
namespace TestDataTool.Cli;

public class UsageException : Exception
{
    public UsageException(string message) : base(message) { }
}

// Parses: <command> --name value --name value ...
public class Options
{
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

    public string Command { get; }

    public Options(string[] args)
    {
        if (args.Length == 0)
            throw new UsageException("No command given.");

        Command = args[0].ToLowerInvariant();

        for (int i = 1; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--"))
                throw new UsageException($"Unexpected argument '{args[i]}'.");

            string name = args[i][2..];
            if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
                throw new UsageException($"Option --{name} needs a value.");

            _values[name] = args[++i];
        }
    }

    public string? Get(string name) => _values.GetValueOrDefault(name);

    public string Require(string name) =>
        Get(name) ?? throw new UsageException($"Missing required option --{name}.");

    public int GetInt(string name, int defaultValue)
    {
        string? raw = Get(name);
        if (raw is null) return defaultValue;
        return int.TryParse(raw, out int value)
            ? value
            : throw new UsageException($"Option --{name} must be a whole number, got '{raw}'.");
    }
}
```

```cs
// File: Services/UserGenerator.cs
using TestDataTool.Models;

namespace TestDataTool.Services;

public static class UserGenerator
{
    private static readonly string[] FirstNames =
        { "Ada", "Grace", "Linus", "Margaret", "Alan", "Edsger", "Barbara", "Dennis" };

    private static readonly string[] LastNames =
        { "Lovelace", "Hopper", "Torvalds", "Hamilton", "Turing", "Dijkstra", "Liskov", "Ritchie" };

    public static List<User> Generate(int count, int seed)
    {
        var random = new Random(seed);          // same seed, same sequence
        var roles = Enum.GetValues<Role>();
        var users = new List<User>();

        for (int id = 1; id <= count; id++)
        {
            string first = FirstNames[random.Next(FirstNames.Length)];
            string last = LastNames[random.Next(LastNames.Length)];
            string email = $"{first}.{last}.{id}@example.test".ToLowerInvariant();   // the id keeps emails unique
            int age = random.Next(18, 66);
            Role role = roles[random.Next(roles.Length)];
            bool active = random.Next(10) < 8;                                       // about 80% active

            users.Add(new User(id, first, last, email, age, role, active));
        }

        return users;
    }
}
```

```cs
// File: Services/UserStore.cs
using System.Text.Json;
using System.Text.Json.Serialization;
using TestDataTool.Models;

namespace TestDataTool.Services;

public static class UserStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task SaveAsync(string path, IEnumerable<User> users)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null)
            Directory.CreateDirectory(directory);

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, users, Options);
    }

    public static async Task<List<User>> LoadAsync(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}", path);

        await using var stream = File.OpenRead(path);
        try
        {
            return await JsonSerializer.DeserializeAsync<List<User>>(stream, Options) ?? new List<User>();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"'{path}' does not contain valid user data: {ex.Message}", ex);
        }
    }
}
```

```cs
// File: Services/UserValidator.cs
using TestDataTool.Models;

namespace TestDataTool.Services;

public static class UserValidator
{
    public static List<string> Validate(IReadOnlyList<User> users)
    {
        var errors = new List<string>();

        foreach (var user in users)
        {
            if (user.Id <= 0)
                errors.Add($"User {user.Id}: id must be greater than 0");
            if (string.IsNullOrWhiteSpace(user.FirstName))
                errors.Add($"User {user.Id}: first name is empty");
            if (string.IsNullOrWhiteSpace(user.LastName))
                errors.Add($"User {user.Id}: last name is empty");
            if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains('@'))
                errors.Add($"User {user.Id}: invalid email '{user.Email}'");
            if (user.Age < 18 || user.Age > 120)
                errors.Add($"User {user.Id}: age {user.Age} is outside 18-120");
        }

        foreach (var group in users.GroupBy(u => u.Id).Where(g => g.Count() > 1))
            errors.Add($"Duplicate id {group.Key} ({group.Count()} users)");

        var emails = users
            .Where(u => !string.IsNullOrWhiteSpace(u.Email))
            .GroupBy(u => u.Email, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);
        foreach (var group in emails)
            errors.Add($"Duplicate email '{group.Key}' ({group.Count()} users)");

        return errors;
    }
}
```

```cs
// File: Services/UserReports.cs
using TestDataTool.Models;

namespace TestDataTool.Services;

public static class UserReports
{
    public static List<string> Stats(IReadOnlyList<User> users)
    {
        var lines = new List<string> { $"Total users: {users.Count}" };
        if (users.Count == 0)
            return lines;

        lines.Add($"Active users: {users.Count(u => u.IsActive)}");
        lines.Add($"Average age: {users.Average(u => u.Age):F1}");

        var oldest = users.MaxBy(u => u.Age)!;
        lines.Add($"Oldest: {oldest.FirstName} {oldest.LastName} ({oldest.Age})");

        foreach (Role role in Enum.GetValues<Role>())
            lines.Add($"Role {role}: {users.Count(u => u.Role == role)}");

        return lines;
    }

    public static List<string> ToCsv(IEnumerable<User> users)
    {
        var lines = new List<string> { "id,firstName,lastName,email,age,role,isActive" };
        foreach (var u in users)
        {
            lines.Add(string.Join(",",
                u.Id, Quote(u.FirstName), Quote(u.LastName), Quote(u.Email), u.Age, u.Role, u.IsActive));
        }
        return lines;
    }

    private static string Quote(string value) =>
        value.Contains(',') || value.Contains('"')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}
```

```cs
// File: Program.cs
using TestDataTool.Cli;
using TestDataTool.Models;
using TestDataTool.Services;

try
{
    var options = new Options(args);
    return await RunAsync(options);
}
catch (UsageException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.Error.WriteLine();
    PrintUsage();
    return 2;
}
catch (Exception ex) when (ex is IOException or InvalidDataException)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static async Task<int> RunAsync(Options options)
{
    switch (options.Command)
    {
        case "generate":
            return await GenerateAsync(options);
        case "validate":
            return await ValidateAsync(options);
        case "stats":
            return await StatsAsync(options);
        case "filter":
            return await FilterAsync(options);
        case "export-csv":
            return await ExportCsvAsync(options);
        default:
            throw new UsageException($"Unknown command '{options.Command}'.");
    }
}

static async Task<int> GenerateAsync(Options options)
{
    int count = options.GetInt("count", 10);
    int seed = options.GetInt("seed", 1);
    string output = options.Require("out");
    if (count < 1)
        throw new UsageException("Option --count must be at least 1.");

    var users = UserGenerator.Generate(count, seed);
    await UserStore.SaveAsync(output, users);
    Console.WriteLine($"Generated {users.Count} users (seed {seed}) into {output}");
    return 0;
}

static async Task<int> ValidateAsync(Options options)
{
    var users = await UserStore.LoadAsync(options.Require("file"));
    var errors = UserValidator.Validate(users);

    if (errors.Count == 0)
    {
        Console.WriteLine($"OK: {users.Count} users valid");
        return 0;
    }

    foreach (var error in errors)
        Console.WriteLine(error);
    Console.WriteLine($"{errors.Count} problem(s) found");
    return 1;
}

static async Task<int> StatsAsync(Options options)
{
    var users = await UserStore.LoadAsync(options.Require("file"));
    foreach (var line in UserReports.Stats(users))
        Console.WriteLine(line);
    return 0;
}

static async Task<int> FilterAsync(Options options)
{
    var users = await UserStore.LoadAsync(options.Require("file"));

    Func<User, bool> filter = _ => true;                    // start with "match everything"

    string? roleText = options.Get("role");
    if (roleText is not null)
    {
        if (!Enum.TryParse<Role>(roleText, ignoreCase: true, out var role))
            throw new UsageException($"Unknown role '{roleText}'. Use Admin, Tester or Viewer.");
        var previous = filter;
        filter = u => previous(u) && u.Role == role;        // add one criterion
    }

    if (options.Get("min-age") is not null)
    {
        int minAge = options.GetInt("min-age", 0);
        var previous = filter;
        filter = u => previous(u) && u.Age >= minAge;
    }

    var matches = users.Where(filter).OrderBy(u => u.Id).ToList();

    Console.WriteLine($"{"Id",3}  {"Name",-18} {"Age",3}  {"Role",-7} Active");
    foreach (var u in matches)
        Console.WriteLine($"{u.Id,3}  {u.FirstName + " " + u.LastName,-18} {u.Age,3}  {u.Role,-7} {u.IsActive}");
    Console.WriteLine($"{matches.Count} of {users.Count} users matched");

    string? output = options.Get("out");
    if (output is not null)
    {
        await UserStore.SaveAsync(output, matches);
        Console.WriteLine($"Saved to {output}");
    }
    return 0;
}

static async Task<int> ExportCsvAsync(Options options)
{
    var users = await UserStore.LoadAsync(options.Require("file"));
    string output = options.Require("out");

    string? directory = Path.GetDirectoryName(Path.GetFullPath(output));
    if (directory is not null)
        Directory.CreateDirectory(directory);

    await File.WriteAllLinesAsync(output, UserReports.ToCsv(users));
    Console.WriteLine($"Wrote {users.Count} rows to {output}");
    return 0;
}

static void PrintUsage()
{
    Console.Error.WriteLine("Usage: dotnet run -- <command> [options]");
    Console.Error.WriteLine("  generate    --out FILE [--count N] [--seed S]");
    Console.Error.WriteLine("  validate    --file FILE");
    Console.Error.WriteLine("  stats       --file FILE");
    Console.Error.WriteLine("  filter      --file FILE [--role ROLE] [--min-age N] [--out FILE]");
    Console.Error.WriteLine("  export-csv  --file FILE --out FILE");
}
```

How it fits together: `Program.cs` only parses arguments, calls services, and maps problems to exit codes. `UsageException` represents "the user typed something wrong" (exit code 2) and is kept separate from I/O problems (exit code 1). The filter is built by chaining lambdas: each criterion wraps the previous `Func<User, bool>`, which is a closure over `previous` and the parsed value (Day 37).

### 5. Sample session

Real output from the reference solution (the random data depends on the seed and on the .NET version's `Random` implementation, so your numbers may differ if you change the code that draws random values). `dotnet run --` is shown; the program was run with the project already built.

```shell
$ dotnet run -- generate --count 20 --seed 7 --out data/users.json
Generated 20 users (seed 7) into data/users.json
$ head -10 data/users.json
[
  {
    "id": 1,
    "firstName": "Margaret",
    "lastName": "Liskov",
    "email": "margaret.liskov.1@example.test",
    "age": 49,
    "role": "Admin",
    "isActive": true
  },
$ dotnet run -- validate --file data/users.json
OK: 20 users valid
$ dotnet run -- stats --file data/users.json
Total users: 20
Active users: 16
Average age: 36.6
Oldest: Barbara Hopper (65)
Role Admin: 7
Role Tester: 6
Role Viewer: 7
$ dotnet run -- filter --file data/users.json --role Viewer --min-age 40 --out data/viewers.json
 Id  Name               Age  Role    Active
  2  Edsger Lovelace     63  Viewer  False
 10  Dennis Dijkstra     45  Viewer  True
 11  Grace Liskov        51  Viewer  True
 15  Alan Dijkstra       56  Viewer  True
 20  Barbara Hopper      65  Viewer  True
5 of 20 users matched
Saved to data/viewers.json
$ dotnet run -- export-csv --file data/users.json --out data/users.csv
Wrote 20 rows to data/users.csv
$ head -3 data/users.csv
id,firstName,lastName,email,age,role,isActive
1,Margaret,Liskov,margaret.liskov.1@example.test,49,Admin,True
2,Edsger,Lovelace,edsger.lovelace.2@example.test,63,Viewer,False
```

Running `generate` a second time with the same seed produced an identical file (`cmp` reported no difference). With the file edited by hand to have a bad email, a duplicated id, and an age of 17:

```shell
$ dotnet run -- validate --file data/bad.json
User 2: invalid email 'not-an-email'
User 6: age 17 is outside 18-120
Duplicate id 3 (2 users)
3 problem(s) found
$ echo $?
1
```

Error cases:

```shell
$ dotnet run -- validate --file data/missing.json
Error: File not found: data/missing.json
$ dotnet run -- filter --file data/users.json --role Boss
Error: Unknown role 'Boss'. Use Admin, Tester or Viewer.
...
$ dotnet run -- nonsense
Error: Unknown command 'nonsense'.

Usage: dotnet run -- <command> [options]
  generate    --out FILE [--count N] [--seed S]
  validate    --file FILE
  stats       --file FILE
  filter      --file FILE [--role ROLE] [--min-age N] [--out FILE]
  export-csv  --file FILE --out FILE
$ echo $?
2
```

(For `filter --role Boss` the usage text is printed after the error as well; it is shortened here with `...`.) An invalid JSON file such as `{ nope` gives `Error: 'data/broken.json' does not contain valid user data: ...` followed by the library's description of the parse problem, with exit code 1.

### 6. Stretch goals

1. Add an xUnit v3 test project `TestDataTool.Tests` (Day 45 commands) with tests for `UserValidator` (valid data, duplicate id, bad email, age 17) and `UserGenerator` (same seed gives equal lists; different seeds differ; emails are unique). Run `dotnet test`.
2. Add `--active-only true` to `filter`, and an `--sort age|name` option using a `Func<User, object>` key selector or a `switch` expression.
3. Add a `merge --file A --file2 B --out C` command that combines two files and renumbers ids.
4. Use `CancellationToken` in `SaveAsync`/`LoadAsync` and pass a token that cancels after 5 seconds.
5. Finish the work as a pull request: commit in small steps, push `feature/test-data-tool`, open a PR with a description of the commands, and merge it (squash) once the tests pass.

### Common mistakes

- Putting everything into `Program.cs`. Small classes are easier to test and to review.
- Forgetting `--` in `dotnet run -- generate ...`, so `dotnet run` treats `generate` as its own argument.
- Catching `Exception` everywhere and hiding real bugs; catch the specific exceptions you can explain to the user.
- Writing error messages to `Console.WriteLine` instead of `Console.Error`: scripts and CI cannot separate results from errors.
- Forgetting that `File.Create` overwrites, and not disposing the stream (use `await using`).

### Why this matters for test automation

Almost every framework you will build needs a data layer like this: generate or load data, validate it, hand it to tests. Writing it as small, testable classes with clear errors and exit codes is the same discipline as a good page object or fixture. It is also a good portfolio item: a small, tested, documented tool in a Git repository.

### Exercise

1. Build the tool following the suggested order, committing after each step on a feature branch.
2. Run all six acceptance checks and note the results.
3. Break your own data file in three different ways (duplicate id, bad email, wrong age) and confirm `validate` reports each.
4. Write a README with usage examples for each command.

### Check

All acceptance checks pass; `git log --oneline` shows several small commits; you can explain what each class does and why `Program.cs` contains no business logic.

---

## Day 48: Phase 1 interview Q&A and checkpoint

### 1. How to use this lesson

Cover the answers and say each one out loud in 20 to 40 seconds. A good interview answer has three parts: a definition, a short example or a contrast, and (when it fits) why it matters in test automation. Where you do not know an answer, go back to the day named in brackets.

### 2. Interview questions (30 questions, Weeks 1 to 8)

#### Foundations (Weeks 1 to 3)

**1. What is the difference between a value type and a reference type?** [Days 3, 19, 27]
A variable of a value type (`int`, `bool`, `struct`, `enum`) holds the data itself, and assignment copies it. A variable of a reference type (`class`, `string`, arrays, `record`) holds a reference to an object, and assignment copies the reference, so two variables can point to the same object.

**2. Strings are reference types, so why does changing one not affect another?** [Day 13]
Strings are immutable: every "change" creates a new string. Use `StringBuilder` when you build text in a loop.

**3. What does `var` do? Is C# dynamically typed because of it?** [Day 3]
`var` lets the compiler infer the type from the right-hand side at compile time; the variable is still statically typed. It needs an initializer.

**4. What do nullable reference types (`string?`) do?** [Day 3]
They are compile-time annotations that let the compiler warn about possible `null` dereferences. They do not change runtime behaviour; a `null` can still arrive from outside (JSON, other libraries).

**5. `ref` versus `out`?** [Day 10]
Both pass by reference. `ref` requires the variable to be initialized before the call; `out` requires the method to assign it before returning and is used to return extra values (`int.TryParse(text, out var n)`).

**6. Array, `List<T>`, `Dictionary<K,V>`, `HashSet<T>`: when to use each?** [Days 14 to 17]
Array: fixed size. `List<T>`: ordered, growable, index access. `Dictionary<K,V>`: look up values by key in roughly constant time. `HashSet<T>`: unique items and fast "contains" checks. Queue is first in first out; Stack is last in first out.

**7. `==` versus `.Equals` on strings and objects?** [Days 13, 27]
For `string`, `==` compares content. For most classes, `==` compares references unless the type overloads it. Records compare by value automatically.

#### OOP (Weeks 4 and 5)

**8. What are the four pillars of OOP?** [Days 21 to 26]
Encapsulation (hide state behind members and access modifiers), inheritance (reuse via a base class), polymorphism (one call, behaviour depends on the actual type), abstraction (expose what, hide how, through interfaces and abstract classes).

**9. `virtual`/`override` versus `new` (hiding)?** [Day 23]
`override` replaces a virtual method, and calls through a base-typed variable run the derived version (polymorphism). `new` hides a member; which version runs depends on the compile-time type of the variable, so it is rarely what you want.

**10. Abstract class versus interface?** [Day 26]
An interface is a contract with no state (default implementations exist but are an advanced feature), and a class can implement many. An abstract class can hold fields, constructors and shared code, but a class can inherit only one. Use an interface for a capability ("can be logged"), an abstract class for a shared base ("is a page").

**11. Class versus struct versus record?** [Day 27]
Class: reference type. Struct: value type, copied on assignment, suited to small data. Record: a class (by default) with value-based equality, a readable `ToString`, and `with` expressions, built for immutable data such as DTOs and test data.

**12. What are generics and why use them?** [Day 28]
Types and methods with type parameters (`List<T>`, `Func<T, TResult>`), giving type safety without casting and without duplicating code. Constraints such as `where T : class, new()` limit what `T` can be.

**13. What is an extension method?** [Day 29]
A static method in a static class whose first parameter has `this`, called as if it were an instance method (`text.ToSlug()`). LINQ is built from them.

**14. `const` versus `readonly`?** [Days 19 to 21]
`const` is a compile-time constant (primitives and strings) and implicitly static. `readonly` is assigned at declaration or in a constructor and can differ per instance.

#### LINQ and exceptions (Week 6)

**15. What is deferred execution in LINQ?** [Days 31, 32]
A query such as `list.Where(...)` does not run when written; it runs when you enumerate it (`foreach`, `ToList()`, `Count()`). If the source changes before that, the results reflect the change, and iterating twice runs the query twice.

**16. `First`, `FirstOrDefault`, `Single`, `SingleOrDefault`?** [Day 33]
`First` throws if there is no match; `FirstOrDefault` returns the default (`null` for references). `Single` throws if there are zero or more than one match; `SingleOrDefault` throws on more than one. Use `Single` when exactly one is a rule of your test data.

**17. How do `try`, `catch`, `finally` work, and does `finally` always run?** [Day 34]
The `try` code runs; a matching `catch` handles an exception; `finally` runs whether or not an exception occurred (even on `return`), used for cleanup. It will not run if the process is killed.

**18. `throw;` versus `throw ex;`?** [Day 35]
`throw;` rethrows the current exception and keeps the original stack trace; `throw ex;` resets the stack trace to the current line, hiding where it began.

**19. What does `using` do, and what is `IDisposable`?** [Day 35]
`IDisposable` marks types that hold resources (files, connections, browsers). `using` calls `Dispose()` when the scope ends, even if an exception occurs. For async cleanup there is `IAsyncDisposable` and `await using`.

#### Async, files, JSON (Week 7)

**20. Explain `async`/`await`. Does it create threads?** [Days 38, 39]
`await` suspends the method until a task completes without blocking the thread, then resumes it with the result (or re-throws its exception). It does not itself create threads. Every Playwright call such as `await page.ClickAsync(...)` is a `Task` you must await.

**21. Why are `async void` and `.Result`/`.Wait()` dangerous?** [Day 39]
`async void` cannot be awaited or have its exceptions caught by the caller (it can crash the process) and test frameworks cannot track it; return `Task`. `.Result`/`.Wait()` block threads, can deadlock where a synchronization context exists, and wrap errors in `AggregateException`; use `await`.

**22. `Task` versus `ValueTask`?** [Day 39]
`ValueTask` avoids an allocation when the result is available synchronously. It must be awaited once. xUnit v3's `IAsyncLifetime` uses `ValueTask InitializeAsync()` and `DisposeAsync()`.

**23. How do you run tasks in parallel and wait for all?** [Day 38]
Start them without awaiting, then `await Task.WhenAll(tasks)`. Do not use it for steps that must be ordered, such as actions on one page.

**24. What is a delegate? `Func` versus `Action`? What is a closure?** [Day 37]
A delegate is a type-safe method reference. `Func<..., TResult>` returns a value; `Action` returns nothing. A closure is a lambda that captures variables from its enclosing scope.

**25. How do you read test data from a JSON file reliably?** [Days 40, 41]
Keep it under `TestData/`, copy it to the output folder via `CopyToOutputDirectory`, build the path with `Path.Combine(AppContext.BaseDirectory, ...)`, and deserialize into records with `System.Text.Json` using a shared `JsonSerializerOptions` (camelCase, case-insensitive or `[JsonPropertyName]`). Fail loudly on missing or malformed data.

#### Tooling (Week 8)

**26. Explain `git add`, `commit`, `branch`, `merge`.** [Day 43]
`add` stages changes, `commit` records the staged snapshot, a branch is a movable pointer to a commit used to isolate work, `merge` joins another branch into the current one (fast-forward or with a merge commit).

**27. Merge versus rebase?** [Day 43]
Merge preserves history with a merge commit. Rebase replays your commits on a new base for linear history but rewrites them, so never rebase commits others already have.

**28. How do you resolve a merge conflict?** [Day 43]
Open the files with `<<<<<<<` markers, choose or combine the correct content and delete the markers, `git add` the files, `git commit`, then run the tests.

**29. Describe your pull request workflow.** [Day 44]
Update `main`, create a feature branch, commit small changes, push with `-u`, open a PR (web UI or `gh pr create`), wait for review and CI checks, address feedback with more commits, merge (merge/squash/rebase per team policy), delete the branch, pull `main`.

**30. What do `dotnet restore`, `build`, `test` and `publish` do, and how do you add a NuGet package?** [Day 45]
`restore` downloads packages; `build` compiles; `test` runs the tests and returns a non-zero exit code on failure; `publish` produces deployable output. `dotnet add <project> package <Name> --version X` adds a `PackageReference` to the `.csproj`.

**Bonus: How do you debug a failing test?** [Day 46]
Read the message and stack trace, set a breakpoint before the failing line (conditional if in a loop), step with F10/F11, inspect locals and watch expressions, form and test a hypothesis, fix it, and keep a test that covers it.

### 3. Mini code questions

Be ready to answer these without running them.

```csharp
// Complete program: what does it print?
var list = new List<int> { 1, 2, 3, 4 };
var query = list.Where(x => x > 2);
list.Add(5);
Console.WriteLine(string.Join(",", query));                       // 1

string a = "hi";
string b = a;
a += "!";
Console.WriteLine($"{a} {b}");                                    // 2

Func<int, int> twice = x => x * 2;
Action<int> show = x => Console.WriteLine(twice(x));
show(21);                                                         // 3

try
{
    Console.WriteLine("try");
    throw new InvalidOperationException("boom");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"catch {ex.Message}");
}
finally
{
    Console.WriteLine("finally");                                 // 4
}

await Task.Delay(10);
Console.WriteLine("done");                                        // 5
```

<details>
<summary>Answers</summary>

```text
3,4,5
hi! hi
42
try
catch boom
finally
done
```

1. The query is deferred: it runs when `string.Join` enumerates it, after `5` was added. 2. Strings are immutable: `a += "!"` creates a new string and `b` still refers to the old one. 3. `twice(21)` is 42. 4. `finally` runs after `catch`. 5. Top-level `await` is allowed in `Program.cs`.

</details>

### 4. Checkpoint self-assessment

Be honest. A checked box means you can do it from memory, without looking, in a new project. Anything unchecked is your revision list; go back to the day in brackets.

**Setup and basics (Weeks 1 and 2)**
- [ ] I can create, build and run a console project with the `dotnet` CLI and explain the `.csproj`, namespaces and `using`. [1, 2]
- [ ] I can declare variables with the right types, use `var`, nullable types and string interpolation. [3, 5]
- [ ] I can write `if`/`else`, `switch` statements and expressions, and all four loops. [7, 8]
- [ ] I can write methods with parameters, return values, overloads, optional and named arguments, `out` and `ref`. [9, 10]

**Collections and strings (Week 3)**
- [ ] I can use `List<T>`, arrays, `Dictionary<K,V>`, `HashSet<T>`, `Queue<T>`, `Stack<T>` and choose between them. [14 to 17]
- [ ] I know why strings are immutable and when to use `StringBuilder`. [13]

**OOP (Weeks 4 and 5)**
- [ ] I can write classes with properties, constructors, access modifiers, and use inheritance, `virtual`/`override`, and abstract classes. [19 to 24]
- [ ] I can define and implement interfaces and explain abstract class versus interface. [25, 26]
- [ ] I can use records, structs, enums, generics (with constraints), extension methods and partial classes. [27 to 29]

**LINQ and exceptions (Week 6)**
- [ ] I can filter, project, sort, group and join with LINQ, and use `Any`, `First`, `Single` and their `OrDefault` forms correctly. [31 to 33]
- [ ] I can use `try`/`catch`/`finally`, custom exceptions, `throw`, and `using`/`IDisposable`. [34, 35]

**Async, files, JSON (Week 7)**
- [ ] I can use `Func`, `Action` and lambdas, and explain closures. [37]
- [ ] I can write `async Task` and `async Task<T>` methods, use `Task.WhenAll`, and always `await`. [38]
- [ ] I can explain `ValueTask`, `async void`, `.Result` deadlocks and cancellation tokens. [39]
- [ ] I can read and write files, build paths safely, and copy data files to the output folder. [40]
- [ ] I can serialize and deserialize JSON with `System.Text.Json` and load test data from a file. [41]

**Tooling (Week 8)**
- [ ] I can use Git: init, add, commit, branch, merge, resolve a conflict, `.gitignore`. [43]
- [ ] I can push a branch and open a pull request (web UI or `gh`), then clean up after the merge. [44]
- [ ] I can manage solutions, project references and NuGet packages and run restore/build/test/publish. [45]
- [ ] I can debug with breakpoints, stepping, watches and conditional breakpoints. [46]
- [ ] I built the Day 47 tool (or a version of it) and can explain its structure. [47]

Scoring (there are 21 boxes): 18 or more checked means you are ready for Phase 2 (Playwright). Between 13 and 17: spend one or two extra days revisiting the unchecked topics with fresh exercises before moving on. Fewer than 13: repeat the weekly review days (6, 12, 18, 24, 30, 36, 42) for those weeks first; Playwright code uses all of these skills on every line, and the time spent now is saved later.

### 5. A last exercise: teach it back

1. Pick five questions from section 2 that you answered poorly and write a 3-sentence answer for each in your own words.
2. Answer them out loud while recording yourself, and listen for filler and vague words.
3. Add each unchecked checklist item to a "revision" file in your Day 47 repository and commit it on a branch.

### Check

You are done with Phase 1 when the checklist has at most three unchecked items and you can answer at least 25 of the 30 questions fluently. Phase 2 starts with Playwright and browsers; every `await page...` line will use what you practiced here.



