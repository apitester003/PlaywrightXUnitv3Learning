# 52-Week Roadmap: C# + Playwright + xUnit v3 (UI and API) + Azure CI/CD

Goal: go from basic C# to senior/framework-architect level, using this repo as the working framework.

**Schedule:** 1 hour a day, 6 days a week, 52 weeks = 312 days.

**Weekly pattern:**
- Days 1–5 of each week: one new topic per day, with explanation, code, and an exercise.
- Day 6 of each week: review, practice, and interview questions for that week.

**Practice sites:** UI on https://www.saucedemo.com, API on https://reqres.in.

## Phases

| Phase | Weeks | Days | Topic |
|---|---|---|---|
| 1 | 1–8 | 1–48 | C# foundations |
| 2 | 9–16 | 49–96 | Playwright UI automation |
| 3 | 17–21 | 97–126 | xUnit v3 |
| 4 | 22–28 | 127–168 | This repo's framework: POM, Flows, DI, config |
| 5 | 29–38 | 169–228 | API automation |
| 6 | 39–44 | 229–264 | Advanced: reporting, parallel runs, database, visual, accessibility, mobile |
| 7 | 45–50 | 265–300 | Docker and Azure CI/CD |
| 8 | 51–52 | 301–312 | Architecture, capstone, interviews |

Each phase ends with a mini-project and an interview Q&A section.

---

## Phase 1: C# Foundations (Days 1–48)

### Week 1: Setup and first programs (Days 1–6)
| Day | Topic |
|---|---|
| 1 | Install the .NET SDK and an editor; run your first `dotnet run` |
| 2 | Project structure: `.csproj`, solution, namespaces, `using` |
| 3 | Variables, data types, `var`, nullable types |
| 4 | Operators, expressions, type conversion |
| 5 | Console input and output, string interpolation |
| 6 | Review and practice |

### Week 2: Control flow and methods (Days 7–12)
| Day | Topic |
|---|---|
| 7 | `if`/`else`, `switch` statements and expressions |
| 8 | Loops: `for`, `foreach`, `while`, `do-while` |
| 9 | Methods: parameters, return values, overloads |
| 10 | Optional and named arguments, `out`, `ref` |
| 11 | Scope, recursion basics |
| 12 | Review and practice |

### Week 3: Strings, arrays, and collections (Days 13–18)
| Day | Topic |
|---|---|
| 13 | Strings: methods, `StringBuilder`, immutability |
| 14 | Arrays and `Array` methods |
| 15 | `List<T>` |
| 16 | `Dictionary<K,V>` and `HashSet<T>` |
| 17 | `Queue`, `Stack`, `LinkedList`, choosing a collection |
| 18 | Review and interview questions on collections |

### Week 4: OOP, part 1 (Days 19–24)
| Day | Topic |
|---|---|
| 19 | Classes, objects, fields, properties |
| 20 | Constructors, `this`, `static` |
| 21 | Encapsulation and access modifiers |
| 22 | Inheritance and `base` |
| 23 | Polymorphism: `virtual`, `override`, `abstract` |
| 24 | Review and interview questions on OOP |

### Week 5: OOP, part 2 (Days 25–30)
| Day | Topic |
|---|---|
| 25 | Interfaces |
| 26 | Abstract class vs. interface |
| 27 | Records, structs, enums |
| 28 | Generics |
| 29 | Extension methods, partial classes |
| 30 | Review and practice |

### Week 6: LINQ and exceptions (Days 31–36)
| Day | Topic |
|---|---|
| 31 | LINQ basics: `Where`, `Select` |
| 32 | `OrderBy`, `GroupBy`, `Join` |
| 33 | `Any`, `First`, `Single`, and their `OrDefault` forms |
| 34 | Exceptions: `try`/`catch`/`finally` |
| 35 | Custom exceptions, `throw`, `using` and `IDisposable` |
| 36 | Review and practice |

### Week 7: Async and file I/O (Days 37–42)
| Day | Topic |
|---|---|
| 37 | Delegates, lambdas, `Func`, `Action` |
| 38 | `Task`, `async`, `await` |
| 39 | `ValueTask` and async pitfalls (`async void`, `.Result`) |
| 40 | File I/O |
| 41 | JSON with `System.Text.Json` |
| 42 | Review and practice |

### Week 8: Tooling and Phase 1 wrap-up (Days 43–48)
| Day | Topic |
|---|---|
| 43 | Git basics |
| 44 | GitHub branches and pull requests |
| 45 | NuGet and the `dotnet` CLI |
| 46 | Debugging in your IDE |
| 47 | Phase 1 mini-project: a console test-data tool |
| 48 | Phase 1 interview Q&A and checkpoint |


---

## Phase 2: Playwright UI Automation (Days 49–96)

### Week 9: Playwright basics (Days 49–54)
| Day | Topic |
|---|---|
| 49 | What Playwright is, and how it compares with Selenium |
| 50 | The object model: `IPlaywright`, `IBrowser`, `IBrowserContext`, `IPage` |
| 51 | Install browsers and write your first script |
| 52 | Navigation, page title, URL |
| 53 | Walk through `Core/BaseTest.cs` |
| 54 | Review and practice |

### Week 10: Locators (Days 55–60)
| Day | Topic |
|---|---|
| 55 | CSS and XPath selectors |
| 56 | `GetByRole`, `GetByText`, `GetByLabel`, `GetByPlaceholder` |
| 57 | Test IDs and the repo's `data-test` attribute |
| 58 | Chaining, filtering, `First`, `Last`, `Nth` |
| 59 | Strict mode and choosing good locators |
| 60 | Review and practice |

### Week 11: Actions (Days 61–66)
| Day | Topic |
|---|---|
| 61 | `ClickAsync`, `FillAsync`, `PressAsync` |
| 62 | Checkboxes, radio buttons, dropdowns |
| 63 | Hover, drag and drop, keyboard |
| 64 | File upload and download |
| 65 | Dialogs and alerts |
| 66 | Review and practice |

### Week 12: Assertions and waiting (Days 67–72)
| Day | Topic |
|---|---|
| 67 | `Expect` and web-first assertions |
| 68 | Auto-waiting and actionability checks |
| 69 | Explicit waits: `WaitForAsync`, `WaitForURLAsync` |
| 70 | Timeouts and configuration |
| 71 | Negative assertions and custom messages |
| 72 | Review and practice |

### Week 13: Contexts and sessions (Days 73–78)
| Day | Topic |
|---|---|
| 73 | Browser context isolation |
| 74 | Multiple tabs and popups |
| 75 | Cookies, storage state, reusing login |
| 76 | Viewport, permissions, geolocation |
| 77 | Headless vs. headed, `SlowMo`, Chromium/Firefox/WebKit |
| 78 | Review and practice |

### Week 14: Frames and dynamic UI (Days 79–84)
| Day | Topic |
|---|---|
| 79 | iframes |
| 80 | Shadow DOM |
| 81 | Dynamic tables and pagination |
| 82 | Infinite scroll and lazy loading |
| 83 | Date pickers and autocomplete |
| 84 | Review and practice |

### Week 15: Network and debugging (Days 85–90)
| Day | Topic |
|---|---|
| 85 | Request routing and mocking |
| 86 | Intercepting requests and responses |
| 87 | Waiting for network responses |
| 88 | Trace Viewer |
| 89 | Screenshots, videos, console logs |
| 90 | Review and practice |

### Week 16: End-to-end on saucedemo (Days 91–96)
| Day | Topic |
|---|---|
| 91 | Login scenarios |
| 92 | Inventory: sorting and the cart |
| 93 | Checkout flow |
| 94 | Negative scenarios |
| 95 | Phase 2 project: 10 UI tests |
| 96 | Phase 2 interview Q&A and checkpoint |

---

## Phase 3: xUnit v3 (Days 97–126)

### Week 17: Basics (Days 97–102)
| Day | Topic |
|---|---|
| 97 | xUnit v3 architecture; differences from v2 and NUnit |
| 98 | `[Fact]` and the `Assert` API |
| 99 | Naming and the Arrange-Act-Assert pattern |
| 100 | Running tests: `dotnet test` and your IDE's test explorer |
| 101 | Reading test results and failures |
| 102 | Review and practice |

### Week 18: Data-driven tests (Days 103–108)
| Day | Topic |
|---|---|
| 103 | `[Theory]` and `[InlineData]` |
| 104 | `[MemberData]` |
| 105 | `[ClassData]` |
| 106 | Custom data attributes; reading JSON and CSV data |
| 107 | Skipping and explicit tests |
| 108 | Review and practice |

### Week 19: Lifecycle and fixtures (Days 109–114)
| Day | Topic |
|---|---|
| 109 | Constructor and `IDisposable` |
| 110 | `IAsyncLifetime` |
| 111 | Class fixtures |
| 112 | Collection and assembly fixtures |
| 113 | Choosing the right fixture |
| 114 | Review and practice |

### Week 20: Traits, output, and parallelism (Days 115–120)
| Day | Topic |
|---|---|
| 115 | Traits and filtering |
| 116 | `ITestOutputHelper` and test context |
| 117 | Parallelism and test collections |
| 118 | Test ordering |
| 119 | Handling flaky tests and retries |
| 120 | Review and practice |

### Week 21: Advanced xUnit and Phase 3 wrap-up (Days 121–126)
| Day | Topic |
|---|---|
| 121 | Custom attributes and extension points |
| 122 | Assertion libraries: FluentAssertions, Shouldly |
| 123 | xUnit analyzers |
| 124 | Organising a larger test suite |
| 125 | Phase 3 mini-project |
| 126 | Phase 3 interview Q&A and checkpoint |

---

## Phase 4: This Repo's Framework (Days 127–168)

### Week 22: Framework tour (Days 127–132)
| Day | Topic |
|---|---|
| 127 | Repo walkthrough: `Core`, `Pages`, `Flows`, `Tests` |
| 128 | `BaseTest` in depth |
| 129 | Project layering and why it exists |
| 130 | Why Page Object Model |
| 131 | Run and debug `LoginTests` |
| 132 | Review and practice |

### Week 23: Page Objects, part 1 (Days 133–138)
| Day | Topic |
|---|---|
| 133 | `LoginPage` in depth |
| 134 | Locator properties |
| 135 | Page methods |
| 136 | Fluent returns |
| 137 | `InventoryPage` |
| 138 | Review and practice |

### Week 24: Page Objects, part 2 (Days 139–144)
| Day | Topic |
|---|---|
| 139 | A base page class |
| 140 | Reusable components |
| 141 | `CartPage` and checkout pages |
| 142 | Navigation between pages |
| 143 | POM anti-patterns |
| 144 | Review and refactoring |

### Week 25: Flows (Days 145–150)
| Day | Topic |
|---|---|
| 145 | `LoginFlow` in depth |
| 146 | Flow vs. Page Object |
| 147 | Composing flows |
| 148 | Test data in flows |
| 149 | Checkout flow |
| 150 | Review and practice |

### Week 26: Dependency Injection (Days 151–156)
| Day | Topic |
|---|---|
| 151 | DI concepts |
| 152 | `ServiceCollection` and `ServiceProvider` |
| 153 | Lifetimes: transient, scoped, singleton |
| 154 | Registering pages and flows |
| 155 | Scopes and disposal in tests |
| 156 | Review and practice |

### Week 27: Configuration and test data (Days 157–162)
| Day | Topic |
|---|---|
| 157 | `appsettings.json` and `IConfiguration` |
| 158 | Environment variables and secrets |
| 159 | Test data builders |
| 160 | Fake data with Bogus |
| 161 | Multiple environments |
| 162 | Review and practice |

### Week 28: Hardening and Phase 4 wrap-up (Days 163–168)
| Day | Topic |
|---|---|
| 163 | Headless toggle in `BaseTest` |
| 164 | Logging with Serilog |
| 165 | Helper and wait utilities |
| 166 | Screenshot on failure |
| 167 | Phase 4 project: extend the repo to 20 tests |
| 168 | Phase 4 interview Q&A and checkpoint |

---

## Phase 5: API Automation (Days 169–228)

### Week 29: HTTP fundamentals (Days 169–174)
| Day | Topic |
|---|---|
| 169 | HTTP: methods and status codes |
| 170 | Headers, body, JSON |
| 171 | REST principles |
| 172 | Exploring an API with `curl` |
| 173 | Exploring reqres.in |
| 174 | Review and practice |

### Week 30: HttpClient (Days 175–180)
| Day | Topic |
|---|---|
| 175 | `HttpClient` basics |
| 176 | GET and POST |
| 177 | PUT, PATCH, DELETE |
| 178 | `IHttpClientFactory` |
| 179 | Serialization and deserialization |
| 180 | Review and practice |

### Week 31: Playwright API testing (Days 181–186)
| Day | Topic |
|---|---|
| 181 | `APIRequestContext` |
| 182 | GET and POST requests |
| 183 | Headers and authentication |
| 184 | Query parameters |
| 185 | Sharing state between UI and API |
| 186 | Review and practice |

### Week 32: API assertions (Days 187–192)
| Day | Topic |
|---|---|
| 187 | Status codes and headers |
| 188 | JSON body assertions |
| 189 | `JsonDocument` navigation |
| 190 | JSON schema validation |
| 191 | Response time checks |
| 192 | Review and practice |

### Week 33: Models and API clients (Days 193–198)
| Day | Topic |
|---|---|
| 193 | DTOs and models |
| 194 | An API client layer |
| 195 | Request builders |
| 196 | Endpoint constants |
| 197 | Registering API clients with DI |
| 198 | Review and practice |

### Week 34: Authentication (Days 199–204)
| Day | Topic |
|---|---|
| 199 | API keys |
| 200 | Basic authentication |
| 201 | Bearer tokens and JWT |
| 202 | OAuth 2.0 overview |
| 203 | Token management and refresh |
| 204 | Review and practice |

### Week 35: CRUD test suites (Days 205–210)
| Day | Topic |
|---|---|
| 205 | Create and read tests |
| 206 | Update tests |
| 207 | Delete tests |
| 208 | Chaining requests |
| 209 | Negative tests: 400, 401, 404 |
| 210 | Review and practice |

### Week 36: Data-driven API tests (Days 211–216)
| Day | Topic |
|---|---|
| 211 | Theories for API tests |
| 212 | JSON test data |
| 213 | Boundary and equivalence testing |
| 214 | Pagination |
| 215 | Filtering and sorting |
| 216 | Review and practice |

### Week 37: Hybrid UI + API (Days 217–222)
| Day | Topic |
|---|---|
| 217 | API setup, UI verification |
| 218 | Login through the API and reuse storage state |
| 219 | Mocking vs. real services |
| 220 | Test data cleanup through the API |
| 221 | Contract testing basics |
| 222 | Review and practice |

### Week 38: API advanced and Phase 5 wrap-up (Days 223–228)
| Day | Topic |
|---|---|
| 223 | GraphQL basics |
| 224 | Rate limits and retries |
| 225 | Mock servers |
| 226 | API performance basics |
| 227 | Phase 5 project: full API suite for reqres.in |
| 228 | Phase 5 interview Q&A and checkpoint |

---

## Phase 6: Advanced Topics (Days 229–264)

### Week 39: Reporting (Days 229–234)
| Day | Topic |
|---|---|
| 229 | TRX and JUnit result files |
| 230 | HTML reports |
| 231 | Allure reports |
| 232 | Code coverage |
| 233 | Attaching screenshots and traces |
| 234 | Review and practice |

### Week 40: Parallelism and stability (Days 235–240)
| Day | Topic |
|---|---|
| 235 | Parallel execution in xUnit v3 |
| 236 | Test isolation |
| 237 | Diagnosing flaky tests |
| 238 | Retry strategies |
| 239 | Test data isolation |
| 240 | Review and practice |

### Week 41: Database testing (Days 241–246)
| Day | Topic |
|---|---|
| 241 | SQL basics |
| 242 | ADO.NET and Dapper |
| 243 | Asserting database state |
| 244 | Test data setup and cleanup |
| 245 | Practice with SQLite |
| 246 | Review and practice |

### Week 42: Visual and accessibility testing (Days 247–252)
| Day | Topic |
|---|---|
| 247 | Screenshot comparison |
| 248 | Visual regression |
| 249 | Accessibility testing with axe |
| 250 | Responsive layouts |
| 251 | Internationalization basics |
| 252 | Review and practice |

### Week 43: Mobile and cross-browser (Days 253–258)
| Day | Topic |
|---|---|
| 253 | Device emulation |
| 254 | Geolocation and permissions |
| 255 | Firefox and WebKit |
| 256 | Cross-browser matrix |
| 257 | Touch events |
| 258 | Review and practice |

### Week 44: Performance, security, and quality (Days 259–264)
| Day | Topic |
|---|---|
| 259 | Page timing and Lighthouse basics |
| 260 | Load testing overview (k6) |
| 261 | OWASP basics for testers |
| 262 | Handling secrets safely |
| 263 | Code quality and analyzers |
| 264 | Phase 6 interview Q&A and checkpoint |

---

## Phase 7: Docker and Azure CI/CD (Days 265–300)

### Week 45: Docker (Days 265–270)
| Day | Topic |
|---|---|
| 265 | Containers explained |
| 266 | Writing a Dockerfile |
| 267 | The Playwright Docker image |
| 268 | Running tests in a container |
| 269 | Docker Compose |
| 270 | Review and practice |

### Week 46: Azure DevOps setup (Days 271–276)
| Day | Topic |
|---|---|
| 271 | Create an Azure DevOps account, organization, and project |
| 272 | Azure Repos |
| 273 | Azure Pipelines concepts |
| 274 | Your first YAML pipeline |
| 275 | Agents |
| 276 | Review and practice |

### Week 47: Pipelines (Days 277–282)
| Day | Topic |
|---|---|
| 277 | Triggers and pull request validation |
| 278 | Variables and secrets |
| 279 | Stages and jobs |
| 280 | Caching |
| 281 | Publishing results |
| 282 | Review and practice |

### Week 48: Running tests in the pipeline (Days 283–288)
| Day | Topic |
|---|---|
| 283 | Headless runs in CI |
| 284 | Publishing test results |
| 285 | Saving traces and screenshots as artifacts |
| 286 | Browser matrix |
| 287 | Scheduled runs |
| 288 | Review and practice |

### Week 49: Environments and releases (Days 289–294)
| Day | Topic |
|---|---|
| 289 | Environments and approvals |
| 290 | Variable groups and Key Vault |
| 291 | Gates |
| 292 | Azure Boards and Test Plans overview |
| 293 | Reusable pipeline templates |
| 294 | Review and practice |

### Week 50: Scaling CI and Phase 7 wrap-up (Days 295–300)
| Day | Topic |
|---|---|
| 295 | Parallel jobs |
| 296 | Test sharding |
| 297 | Triaging flaky CI runs |
| 298 | Notifications |
| 299 | Free-tier limits and cost |
| 300 | Phase 7 interview Q&A and checkpoint |

---

## Phase 8: Architecture, Capstone, Interviews (Days 301–312)

### Week 51: Framework architecture (Days 301–306)
| Day | Topic |
|---|---|
| 301 | SOLID principles in test code |
| 302 | Design patterns: factory, builder, strategy, singleton |
| 303 | Framework design decisions |
| 304 | Test strategy and the test pyramid |
| 305 | Metrics and test reporting to stakeholders |
| 306 | Review and practice |

### Week 52: Capstone and interviews (Days 307–312)
| Day | Topic |
|---|---|
| 307 | Capstone: build and extend the framework |
| 308 | Capstone: CI/CD pipeline |
| 309 | Capstone: README and documentation |
| 310 | Mock interview, part 1: C# and OOP |
| 311 | Mock interview, part 2: Playwright, API, CI |
| 312 | Final review and portfolio |

---

## About the existing `course/` files

The earlier 70-day files in `course/` stay in place. As each phase of this roadmap is written, I'll reuse and expand the matching material and replace those files. Until then, treat this roadmap as the plan and the old files as a preview.
