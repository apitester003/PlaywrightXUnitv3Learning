# Week 11: Actions

This week you learn how to interact with a page: click, fill and press keys; checkboxes, radio buttons and dropdowns; hover, drag and drop and the keyboard; file upload and download; and dialogs. Day 66 is a review with interview questions.

The examples are in `Tests/Course/Week11/Week11Examples.cs`. All 7 tests were run with `dotnet test --filter "FullyQualifiedName~Week11Examples"` on .NET 10 with Playwright 1.58 and passed. They use small HTML strings loaded with `SetContentAsync`, so they do not depend on a website.

Every action on a locator first waits for the element to be ready (attached, visible, stable, enabled, and able to receive events). Week 12 explains this in detail. This week the focus is on which action to use.

---

## Day 61: `ClickAsync`, `FillAsync`, `PressAsync`

### The core actions

| Action | What it does |
|---|---|
| `ClickAsync()` | Clicks the center of the element |
| `DblClickAsync()` | Double click |
| `ClickAsync(new() { Button = MouseButton.Right })` | Right click |
| `FillAsync("text")` | Clears the field and sets the value in one step |
| `ClearAsync()` | Empties the field |
| `PressAsync("Enter")` | Presses a key (or combination) while the element is focused |
| `PressSequentiallyAsync("abc")` | Types one key at a time, firing key events for each character |
| `InputValueAsync()` | Reads the value of an input |

Verified:

```csharp
await page.Locator("#name").FillAsync("Alice");
await page.Locator("#name").InputValueAsync();        // "Alice"

await page.Locator("#name").ClearAsync();
await page.Locator("#name").InputValueAsync();        // ""

await page.Locator("#b").ClickAsync();                 // runs the button's onclick

await page.Locator("#search").FillAsync("shoes");
await page.Locator("#search").PressAsync("Enter");     // key handler sees Enter

await page.Locator("#name").PressSequentiallyAsync("abc");
await page.Locator("#name").InputValueAsync();        // "abc"
```

### `FillAsync` vs `PressSequentiallyAsync`

`FillAsync` sets the value directly and fires `input` events once. It is fast and is what you want almost always. Use `PressSequentiallyAsync` only when the page reacts to each key press (autocomplete, live search, input masks).

### Key names

`PressAsync` accepts names like `Enter`, `Tab`, `Escape`, `Backspace`, `ArrowDown`, `F5`, and combinations like `Control+A` or `Shift+Home`.

### Double and right click (verified)

```csharp
await page.Locator("#b").DblClickAsync();
await page.Locator("#b").ClickAsync(new() { Button = MouseButton.Right });
```

### Click options you will meet

- `Force = true` skips actionability checks. Avoid it. It hides real problems (for example an overlay covering the button).
- `Timeout` overrides the default for this one call.
- `Modifiers` for Ctrl/Shift click.
- `Position` clicks at an offset instead of the center.

### Exercise

1. On saucedemo's login page, log in using `FillAsync` and `ClickAsync`.
2. Do it again, but submit the form by pressing `Enter` in the password field instead of clicking the button.
3. What is the observable difference between `FillAsync("abc")` and `PressSequentiallyAsync("abc")`?

<details>
<summary>Answers</summary>

3. The final value is the same. `PressSequentiallyAsync` fires key down/press/up and input events for each character; `FillAsync` fires one input event with the full value.

</details>

---

## Day 62: Checkboxes, radio buttons, dropdowns

### Checkboxes and radios

```csharp
await page.Locator("#c1").CheckAsync();       // ensures checked
await page.Locator("#c2").UncheckAsync();     // ensures unchecked
bool on = await page.Locator("#c1").IsCheckedAsync();
```

`CheckAsync` is **idempotent**: if the box is already checked it does nothing. `ClickAsync` toggles, so calling it twice undoes the first click. Prefer `CheckAsync` and `UncheckAsync`, because they state the desired result.

Radio buttons use `CheckAsync()` too. There is no "uncheck" for radios; you select another one:

```csharp
await page.Locator("#m").CheckAsync();
await Assertions.Expect(page.Locator("#m")).ToBeCheckedAsync();
await Assertions.Expect(page.Locator("#s")).Not.ToBeCheckedAsync();
```

### Dropdowns (`<select>`)

```csharp
await page.Locator("#country").SelectOptionAsync("in");                           // by value
await page.Locator("#country").SelectOptionAsync(new SelectOptionValue { Label = "UK" }); // by visible text
await Assertions.Expect(page.Locator("#country")).ToHaveValueAsync("uk");
```

`SelectOptionAsync` works on real `<select>` elements only. Custom dropdowns built from `div`s are not `<select>`; for those you click the control to open it, then click the option, like any other element.

All of this is verified in `Day62_CheckboxRadioDropdown`.

### Exercise

On saucedemo's inventory page, select the sort option "Price (low to high)" using `SelectOptionAsync` (the select has `data-test='product-sort-container'`), then assert that the first product price is the smallest of the list. Describe how you read the prices.

---

## Day 63: Hover, drag and drop, keyboard

### Hover

```csharp
await page.Locator("#hover").HoverAsync();
await Assertions.Expect(page.Locator("#menu")).ToBeVisibleAsync();
```

In the example, a CSS rule `#hover:hover + #menu { display: block }` shows the menu only while the mouse is over the element. Hover is how you test menus that open on mouse over.

### Drag and drop

```csharp
await page.Locator("#drag").DragToAsync(page.Locator("#drop"));
await Assertions.Expect(page.Locator("#res")).ToHaveTextAsync("dropped");
```

`DragToAsync` moves the mouse from the source to the target. The page needs `dragover` and `drop` handlers (the example adds them). If a site uses a custom drag library and `DragToAsync` does not work, use the low-level mouse: `page.Mouse.MoveAsync`, `DownAsync`, `MoveAsync`, `UpAsync`.

### Keyboard

`page.Keyboard` sends keys to whatever element has focus:

```csharp
await page.Locator("#t").FocusAsync();
await page.Keyboard.TypeAsync("Hello");
await page.Keyboard.PressAsync("Shift+Home");   // select to the start of the line
await page.Keyboard.PressAsync("Backspace");    // delete the selection
await page.Locator("#t").InputValueAsync();     // ""
```

Verified in `Day63_HoverDragKeyboard`. Use `locator.PressAsync` when you have the element, and `page.Keyboard` when you need to send keys to the current focus or send several in a row.

### Exercise

1. Write a test for a page with a hover menu: hover, then click an item in the menu.
2. Press `Control+A` in a text field, then `Delete`, and assert the field is empty.
3. Explain when you would use `page.Mouse` instead of `DragToAsync`.

---

## Day 64: File upload and download

### Upload

For `<input type="file">`, give the file path to `SetInputFilesAsync`. No file dialog opens:

```csharp
await page.Locator("#f").SetInputFilesAsync(@"C:\temp\week11-upload.txt");
await Assertions.Expect(page.Locator("#name")).ToHaveTextAsync("week11-upload.txt");
```

To remove the selection: `SetInputFilesAsync(Array.Empty<string>())`. Multiple files: pass an array of paths.

### Download

Start waiting for the download **before** you trigger it, then trigger it inside the callback:

```csharp
IDownload download = await page.RunAndWaitForDownloadAsync(async () =>
{
    await page.Locator("#dl").ClickAsync();
});

Console.WriteLine(download.SuggestedFilename);   // report.txt
await download.SaveAsAsync(@"C:\temp\report.txt");
```

`RunAndWaitForDownloadAsync` avoids a race: if you click first and wait afterwards, the download event can fire before you start listening.

The example page uses a `data:` link with a `download` attribute so the test needs no server. In `Day64_UploadAndDownload` the saved file content was read back and compared to `hello download`.

### Where do files go?

Playwright stores downloads in a temporary folder and deletes them when the context closes. Always call `SaveAsAsync` if you need to keep or read the file.

### Exercise

1. Write a test that uploads a small text file to a page with a file input and checks the displayed file name.
2. Write a test that downloads a file and checks its content. (Use a public site that offers a download link, or build a `data:` link like the example.)
3. Why must `RunAndWaitForDownloadAsync` start waiting before the click?

---

## Day 65: Dialogs and alerts

Browser dialogs are `alert`, `confirm`, `prompt` (and `beforeunload`). They are **not** part of the page's DOM, so you cannot locate them. You handle them with the `Dialog` event.

### Important default

If you do **not** register a handler, Playwright automatically dismisses the dialog. Verified in `Day65_UnhandledDialogIsAutoDismissed`: clicking a button that calls `confirm('Sure?')` without any handler produces `false` (the page shows `no`).

### Handling

```csharp
string? alertMessage = null;
EventHandler<IDialog> onAlert = (_, d) => { alertMessage = d.Message; _ = d.AcceptAsync(); };

page.Dialog += onAlert;
await page.Locator("#a").ClickAsync();
page.Dialog -= onAlert;

// alertMessage == "Saved!"
```

Accept or dismiss:

```csharp
page.Dialog += (_, d) => { _ = d.DismissAsync(); };      // confirm returns false
page.Dialog += (_, d) => { _ = d.AcceptAsync("Bob"); };  // prompt returns "Bob"
```

`d.Message` is the text, `d.Type` is `alert`, `confirm`, `prompt` or `beforeunload`, `d.DefaultValue` is the prompt's default text.

### Pitfalls

- Register the handler **before** the action that opens the dialog.
- The event handler is not async. `_ = d.AcceptAsync();` starts the call without waiting, which is why the code discards the task. This is acceptable here, but be aware that exceptions from the discarded task are not observed.
- Remove the handler (`-=`) when you are done, or it will answer later dialogs too. A handler registered with `page.Dialog +=` stays active for the life of the page.
- Use `page.Dialog +=` for repeated dialogs, and remember there is no built-in "once" in the .NET API; remove it yourself.

### Exercise

1. Write a test for a page that shows `confirm('Delete?')`: first accept it and check the result, then do it again and dismiss it.
2. Capture the message of an `alert` and assert on it.
3. What happens in a test that triggers `alert()` if you have not registered a handler?

<details>
<summary>Answers</summary>

3. Playwright dismisses it automatically, so the test does not hang. If your test needs the message, you must register a handler.

</details>

---

## Day 66: Review and practice

### Recap

| Day | Topic | Key points |
|---|---|---|
| 61 | Click, fill, press | `FillAsync` for values; `PressSequentiallyAsync` only when per-key events matter; avoid `Force` |
| 62 | Checkbox, radio, select | `CheckAsync`/`UncheckAsync` are idempotent; `SelectOptionAsync` by value or label |
| 63 | Hover, drag, keyboard | `HoverAsync`, `DragToAsync`, `page.Keyboard`; `page.Mouse` as fallback |
| 64 | Upload, download | `SetInputFilesAsync`; `RunAndWaitForDownloadAsync` starts waiting before the click |
| 65 | Dialogs | Not in the DOM; `Dialog` event; unhandled dialogs are auto-dismissed; register before the trigger |

### Practice questions

1. What is the difference between `CheckAsync()` and `ClickAsync()` on a checkbox?
2. How do you select a dropdown option by its visible text?
3. Which method sets a value faster, `FillAsync` or `PressSequentiallyAsync`?
4. How do you upload a file without a file dialog?
5. Why does the download wait start inside `RunAndWaitForDownloadAsync`?
6. What happens to a `confirm` dialog if no handler is registered?
7. Can you find an `alert` with a locator?
8. What does `Force = true` do, and why is it risky?

<details>
<summary>Answers</summary>

1. `CheckAsync` ensures the box ends up checked (does nothing if it already is); `ClickAsync` toggles.
2. `SelectOptionAsync(new SelectOptionValue { Label = "UK" })`.
3. `FillAsync`.
4. `SetInputFilesAsync(path)` on the `<input type="file">`.
5. To avoid a race where the download starts before you listen.
6. It is dismissed automatically, so `confirm` returns `false`.
7. No. Dialogs are not part of the DOM; use the `Dialog` event.
8. It skips the actionability checks, so it can click an element that is covered or disabled and hide a real bug.

</details>

### Mini-program: form filler

Create a local HTML form (text field, email field, checkbox "I agree", radio group, a `<select>` of countries, a file input, and a submit button that shows the entered values in a `<div>`). Write one test that fills every control using the most specific action for each, submits, and asserts the result text. Then add a second test that submits the form **without** ticking "I agree" and asserts that the submit shows an error (design the page to show one).

### Interview questions

**1. What does Playwright do before it clicks an element?**
Answer: It runs actionability checks: the element is attached, visible, stable (not animating), enabled, and not covered by another element. It retries until they pass or the timeout expires.

**2. Difference between `FillAsync` and `PressSequentiallyAsync`?**
Answer: `FillAsync` sets the value at once. `PressSequentiallyAsync` types key by key with real key events; use it only for pages that react to each key.

**3. Why prefer `CheckAsync` over `ClickAsync` for checkboxes?**
Answer: It states the desired end state and is idempotent, so a test does not break when the box starts checked.

**4. How do you handle a JavaScript `confirm` dialog?**
Answer: Subscribe to the page's `Dialog` event before the action that opens it, then call `AcceptAsync` or `DismissAsync`. Without a handler Playwright dismisses it.
**Follow-up:** Why register before the click? Answer: The dialog blocks the page; if nobody is listening when it appears, it is dismissed with no chance to inspect it.

**5. How do you test a file download?**
Answer: Use `RunAndWaitForDownloadAsync` with the click inside the callback, then read `SuggestedFilename` and save with `SaveAsAsync` to check the content.

**6. When would you use `Force = true`?**
Answer: Almost never. Only when you have proven the element is really clickable and the check is wrong (for example, a known overlay animation). Otherwise the check is telling you something useful.

**7. How do you test a hover menu?**
Answer: `HoverAsync` on the trigger, then assert the menu is visible and click the item. Use `Expect(...).ToBeVisibleAsync()` so the assertion waits for the menu.

**8. Code:** Select "Price (low to high)" on saucedemo's sort dropdown.
```csharp
await page.Locator("[data-test='product-sort-container']").SelectOptionAsync("lohi");
```
(The `lohi` value comes from saucedemo's HTML; this snippet was not run in the Week 11 tests. Check it in the Day 62 exercise and tell me if the value differs.)

**9. Code:** Accept every dialog automatically and record its message.
```csharp
List<string> messages = new();
page.Dialog += (_, d) => { messages.Add(d.Message); _ = d.AcceptAsync(); };
```
