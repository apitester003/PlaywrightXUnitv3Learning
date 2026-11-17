# Week 3: Strings, arrays, and collections

This week covers how C# handles text and groups of data: `string` and `StringBuilder`, arrays, `List<T>`, `Dictionary<K,V>`, `HashSet<T>`, `Queue<T>`, `Stack<T>` and `LinkedList<T>`. You learn what each collection costs and how to choose between them, then practise the classic interview problems (reverse, palindrome, duplicates, anagram, missing number, two-sum, word frequency).

Every example is a complete program. Create a console project, paste the code into `Program.cs` and run it with `dotnet run`; the output should match the `// Output:` comments.

## Day 13: Strings: methods, `StringBuilder`, immutability

Almost every test you write handles text: page titles, error messages, URLs, locators, test data. Today you learn what a `string` really is and the methods you will use every day.

### 1. What a string is

A `string` is a sequence of characters (`char` values). You can read a character by index, starting at 0, and the property `Length` gives the number of characters.

```csharp
string title = "Login Page";

Console.WriteLine(title.Length);   // number of characters
Console.WriteLine(title[0]);       // first character
Console.WriteLine(title[title.Length - 1]); // last character
Console.WriteLine(title[^1]);      // ^1 means "1 from the end" - same last character

foreach (char c in title)
{
    Console.Write(c + "|");
}
Console.WriteLine();

// Output:
// 10
// L
// e
// e
// L|o|g|i|n| |P|a|g|e|
```

Reading `title[10]` would throw an `IndexOutOfRangeException`, because valid indexes are 0 to `Length - 1`.

### 2. Immutability: strings never change

A string is **immutable**: once created, its characters cannot be modified. Every method that looks like it "changes" a string actually returns a **new** string and leaves the original alone.

```csharp
string name = "  playwright  ";

name.Trim();                 // result thrown away - name is unchanged!
Console.WriteLine($"[{name}]");

string trimmed = name.Trim(); // keep the result
Console.WriteLine($"[{trimmed}]");

name = name.ToUpper();       // reassign the variable to the new string
Console.WriteLine($"[{name}]");

// title[0] = 'X';           // compile error: strings are read-only by index

// Output:
// [  playwright  ]
// [playwright]
// [  PLAYWRIGHT  ]
```

This is the single most common beginner bug with strings: calling `Trim()`, `Replace()` or `ToUpper()` and ignoring the return value.

### 3. Searching inside a string

```csharp
string url = "https://example.com/login?user=alice";

Console.WriteLine(url.Contains("login"));        // True
Console.WriteLine(url.StartsWith("https"));      // True
Console.WriteLine(url.EndsWith(".com"));         // False
Console.WriteLine(url.IndexOf("login"));         // position of first match
Console.WriteLine(url.IndexOf('?'));             // works with a char too
Console.WriteLine(url.IndexOf("missing"));       // -1 when not found
Console.WriteLine(url.LastIndexOf('/'));         // search from the end

// Output:
// True
// True
// False
// 20
// 25
// -1
// 19
```

`IndexOf` returns `-1` when nothing is found. Always check for `-1` before using the result as an index.

### 4. Extracting and changing text

```csharp
string url = "https://example.com/login?user=alice";

int q = url.IndexOf('?');
string path = url.Substring(0, q);      // start index, length
string query = url.Substring(q + 1);    // from index to the end
Console.WriteLine(path);
Console.WriteLine(query);

Console.WriteLine(url.Replace("https", "http"));
Console.WriteLine("Hello".PadLeft(8, '*'));
Console.WriteLine("Hello".PadRight(8, '*'));
Console.WriteLine("a-b-c".Remove(1, 2));      // remove 2 chars starting at index 1
Console.WriteLine("ac".Insert(1, "b"));       // insert at index 1

// Ranges are a shorter way to slice
Console.WriteLine(url[8..19]);                // index 8 up to (not including) 19

// Output:
// https://example.com/login
// user=alice
// http://example.com/login?user=alice
// ***Hello
// Hello***
// a-c
// abc
// example.com
```

### 5. Trim, case, and empty checks

```csharp
string input = "   Admin@Test.com  ";

Console.WriteLine($"[{input.Trim()}]");
Console.WriteLine($"[{input.TrimStart()}]");
Console.WriteLine($"[{input.TrimEnd()}]");
Console.WriteLine(input.Trim().ToLower());
Console.WriteLine(input.Trim().ToUpper());

string? empty = "";
string? spaces = "   ";
string? nothing = null;

Console.WriteLine(string.IsNullOrEmpty(empty));           // True
Console.WriteLine(string.IsNullOrEmpty(spaces));          // False
Console.WriteLine(string.IsNullOrWhiteSpace(spaces));     // True
Console.WriteLine(string.IsNullOrWhiteSpace(nothing));    // True

// Output:
// [Admin@Test.com]
// [Admin@Test.com  ]
// [   Admin@Test.com]
// admin@test.com
// ADMIN@TEST.COM
// True
// False
// True
// True
```

Calling `nothing.Trim()` on a `null` string would throw `NullReferenceException`. `string.IsNullOrWhiteSpace` is safe on `null`.

### 6. Split and Join

`Split` breaks a string into an array of pieces. `string.Join` does the reverse.

```csharp
string csv = "alice,bob,carol";
string[] names = csv.Split(',');
Console.WriteLine(names.Length);
Console.WriteLine(names[1]);

string sentence = "  the quick   brown fox ";
string[] words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
Console.WriteLine(words.Length);

string joined = string.Join(" | ", words);
Console.WriteLine(joined);

string[] pieces = "a=1;b=2".Split(';');
foreach (string piece in pieces)
{
    string[] kv = piece.Split('=');
    Console.WriteLine($"key={kv[0]} value={kv[1]}");
}

// Output:
// 3
// bob
// 4
// the | quick | brown | fox
// key=a value=1
// key=b value=2
```

Without `RemoveEmptyEntries`, consecutive separators produce empty strings in the result.

### 7. Comparing strings

`==` compares the characters, and it is case-sensitive. When you need a different rule, state it explicitly with `StringComparison`.

```csharp
string a = "Login";
string b = "login";

Console.WriteLine(a == b);                                              // False
Console.WriteLine(a.Equals(b));                                         // False
Console.WriteLine(a.Equals(b, StringComparison.OrdinalIgnoreCase));     // True
Console.WriteLine(a.Contains("LOG", StringComparison.OrdinalIgnoreCase)); // True
Console.WriteLine(a.StartsWith("lo", StringComparison.OrdinalIgnoreCase)); // True
Console.WriteLine(string.Compare("apple", "banana", StringComparison.Ordinal) < 0); // True

// Output:
// False
// False
// True
// True
// True
// True
```

`Ordinal` and `OrdinalIgnoreCase` compare raw character values and are the right default for technical text such as URLs, ids and error messages.

### 8. Building strings: interpolation, `Format`, verbatim and raw literals

```csharp
string user = "alice";
int attempts = 3;
double price = 9.5;

Console.WriteLine($"User {user} tried {attempts} times");
Console.WriteLine($"{price:F2}");                  // two decimals
Console.WriteLine($"{attempts,5}|");               // right-aligned in width 5
Console.WriteLine(string.Format("{0} has {1}", user, attempts));

string path = @"C:\tests\data.json";               // verbatim: backslashes are literal
Console.WriteLine(path);

string json = """
    {
      "name": "alice"
    }
    """;                                           // raw string literal
Console.WriteLine(json);

Console.WriteLine("Line1\nLine2\tTabbed \"quoted\"");

// Output:
// User alice tried 3 times
// 9.50
//     3|
// alice has 3
// C:\tests\data.json
// {
//   "name": "alice"
// }
// Line1
// Line2	Tabbed "quoted"
```

Note on `{price:F2}`: number formatting depends on the current culture of the machine. On a machine with a culture that uses a comma as the decimal separator you may see `9,50`.

### 9. Why `StringBuilder` exists

Because strings are immutable, `s += "x"` creates a brand new string every time and copies all the old characters. In a loop that builds a big result, this does a lot of repeated copying. `StringBuilder` (in `System.Text`) holds a growable buffer and appends in place.

```csharp
using System.Text;

var sb = new StringBuilder();
sb.Append("Test run: ");
sb.Append(42);
sb.AppendLine();
sb.AppendLine("Step 1 passed");
sb.Append("Step 2 ").Append("passed").Append('!');   // Append returns the builder, so calls chain

Console.WriteLine(sb.ToString());
Console.WriteLine(sb.Length);

sb.Insert(0, ">> ");
sb.Replace("passed", "OK");
Console.WriteLine(sb.ToString().Contains(">> Test run"));

sb.Clear();
Console.WriteLine(sb.Length);

// Build 1..5 comma separated
var numbers = new StringBuilder();
for (int i = 1; i <= 5; i++)
{
    if (numbers.Length > 0) numbers.Append(',');
    numbers.Append(i);
}
Console.WriteLine(numbers);

// Output:
// Test run: 42
// Step 1 passed
// Step 2 passed!
// 41
// True
// 0
// 1,2,3,4,5
```

(`Length` is 41 on Linux, where `AppendLine` adds one `\n` character. On Windows the newline is two characters, so the number differs there.)

Rule of thumb: a few concatenations or one interpolated string is fine with `+` or `$""`. Use `StringBuilder` when you append in a loop of many iterations.

### 10. A first interview-style example: reverse a string

```csharp
string Reverse(string s)
{
    char[] chars = s.ToCharArray();   // a char[] is mutable, unlike a string
    Array.Reverse(chars);
    return new string(chars);
}

Console.WriteLine(Reverse("playwright"));
Console.WriteLine(Reverse("").Length);

// Output:
// thgirwyalp
// 0
```

Day 18 collects this and the other classic string problems with several solutions each.

### Common mistakes

- Ignoring the return value: `text.Trim();` does nothing useful. Write `text = text.Trim();`.
- Using `IndexOf`'s result without checking for `-1`, which then crashes `Substring`.
- Comparing text with `==` or `Contains` when case should not matter. Pass `StringComparison.OrdinalIgnoreCase`.
- Building a long string with `+=` inside a big loop instead of `StringBuilder`.

### Why this matters for test automation

Assertions on page text, URLs and error messages are string operations. You will trim whitespace from `InnerTextAsync()` results, compare titles ignoring case, split a URL to get the query string, and build readable failure messages with interpolation or `StringBuilder`. Flaky tests are often caused by stray whitespace or case differences, which `Trim` and `StringComparison` fix.

### Interview questions

**Q: Why are strings immutable in C#, and what does that mean in practice?**
A: Once created, a string's content cannot change. Methods like `Replace` return a new string. This makes strings safe to share, but repeated modification in a loop creates many temporary objects.

**Q: When do you use `StringBuilder` instead of `string`?**
A: When building text through many appends, typically in a loop. For a few concatenations, `+` or interpolation is simpler and fine.

**Q: What is the difference between `string.IsNullOrEmpty` and `string.IsNullOrWhiteSpace`?**
A: The first is true for `null` and `""`. The second is also true for strings that contain only whitespace.

**Q: How do you compare two strings ignoring case?**
A: `a.Equals(b, StringComparison.OrdinalIgnoreCase)`.

**Exercise**

1. Given `string email = "  Alice.Smith@Example.COM "`, produce the trimmed, lowercase version, then print the part before `@` and the part after `@` (use `IndexOf` and `Substring`).
2. Given the text `"one two  three   four"`, split it into words with `RemoveEmptyEntries` and print the count and the words joined with `-`.
3. Using `StringBuilder`, build the string `"1-2-3-...-10"` without a trailing dash.
4. Write `bool IsValidUrl(string s)` that returns true when `s` is not blank and starts with `http://` or `https://` (ignore case).

**Check**

- Can you explain why `s.ToUpper();` on its own line has no effect?
- What does `IndexOf` return when the text is missing?
- Which `StringComparison` would you choose for comparing two URLs ignoring case?

## Day 14: Arrays and `Array` methods

An array is the most basic collection: a fixed number of items of the same type, stored one after another. Arrays are everywhere in C# (`string.Split` returns one) and array questions are a staple of interviews. Today is example-heavy on purpose.

### 1. Creating arrays

```csharp
// 1. Size only: every element gets the default value of its type
int[] scores = new int[5];
string[] names = new string[3];
bool[] flags = new bool[2];

Console.WriteLine(string.Join(",", scores));
Console.WriteLine(names[0] is null);       // reference types default to null
Console.WriteLine(string.Join(",", flags));

// 2. Initializer: size is inferred
int[] primes = new int[] { 2, 3, 5, 7 };
string[] browsers = { "chromium", "firefox", "webkit" };   // shortest form
var days = new[] { "Mon", "Tue", "Wed" };                  // type inferred as string[]

// 3. Collection expression (C# 12 and later)
int[] evens = [2, 4, 6, 8];

Console.WriteLine(primes.Length);
Console.WriteLine(browsers.Length);
Console.WriteLine(days.Length);
Console.WriteLine(string.Join(" ", evens));

// Output:
// 0,0,0,0,0
// True
// False,False
// 4
// 3
// 3
// 2 4 6 8
```

The size of an array is fixed when it is created. It cannot grow or shrink; you can only create a new array.

### 2. Reading, writing, looping

```csharp
string[] browsers = { "chromium", "firefox", "webkit" };

Console.WriteLine(browsers[0]);        // first
Console.WriteLine(browsers[^1]);       // last (index from the end)
browsers[1] = "firefox-nightly";       // array elements are mutable (unlike string characters)

for (int i = 0; i < browsers.Length; i++)
{
    Console.WriteLine($"{i}: {browsers[i]}");
}

foreach (string b in browsers)
{
    Console.WriteLine(b.ToUpper());
}

try
{
    Console.WriteLine(browsers[3]);    // valid indexes are 0..2
}
catch (IndexOutOfRangeException)
{
    Console.WriteLine("Index out of range");
}

// Output:
// chromium
// webkit
// 0: chromium
// 1: firefox-nightly
// 2: webkit
// CHROMIUM
// FIREFOX-NIGHTLY
// WEBKIT
// Index out of range
```

Use `for` when you need the index or want to modify elements. `foreach` reads each item; you cannot assign to the loop variable.

### 3. Sum, min, max, average by hand

These loops are the building blocks of many interview questions.

```csharp
int[] data = { 7, -2, 15, 4, 9 };

int sum = 0;
int max = data[0];
int min = data[0];
foreach (int n in data)
{
    sum += n;
    if (n > max) max = n;
    if (n < min) min = n;
}

Console.WriteLine($"sum={sum} max={max} min={min}");
Console.WriteLine($"average={(double)sum / data.Length}");

// The same using LINQ (covered properly in Week 6)
Console.WriteLine($"{data.Sum()} {data.Max()} {data.Min()} {data.Average()}");

// Output:
// sum=33 max=15 min=-2
// average=6.6
// 33 15 -2 6.6
```

Start `max` and `min` from `data[0]`, not from `0`; otherwise an all-negative array gives a wrong max. (This fails with an empty array. Check `Length` first.)

### 4. Searching: `Array.IndexOf`, `Exists`, `Find`, `FindAll`, `BinarySearch`

```csharp
int[] numbers = { 4, 8, 15, 16, 23, 42 };

Console.WriteLine(Array.IndexOf(numbers, 15));          // index of first match
Console.WriteLine(Array.IndexOf(numbers, 99));          // -1 if not found
Console.WriteLine(Array.Exists(numbers, n => n > 40));  // any element matching?
Console.WriteLine(Array.Find(numbers, n => n > 10));    // first match
int[] big = Array.FindAll(numbers, n => n > 15);        // all matches
Console.WriteLine(string.Join(",", big));
Console.WriteLine(Array.FindIndex(numbers, n => n % 2 == 1));

// BinarySearch requires a SORTED array; it is much faster than a linear scan
Console.WriteLine(Array.BinarySearch(numbers, 23));

// Output:
// 2
// -1
// True
// 15
// 16,23,42
// 2
// 4
```

(The `n => n > 40` part is a lambda, a tiny inline function. You will study lambdas on Day 37; for now read `n => n > 40` as "n such that n is greater than 40".)

Searching an unsorted array means checking elements one by one, which takes time proportional to the length (O(n)). `BinarySearch` on a sorted array halves the range each step (O(log n)).

### 5. Sorting and reversing

```csharp
int[] nums = { 5, 3, 9, 1, 7 };
Array.Sort(nums);                       // sorts the array IN PLACE, ascending
Console.WriteLine(string.Join(",", nums));

Array.Reverse(nums);                    // reverses IN PLACE
Console.WriteLine(string.Join(",", nums));

string[] words = { "pear", "apple", "Banana" };
Array.Sort(words, StringComparer.Ordinal);   // by raw char value: uppercase letters come first
Console.WriteLine(string.Join(",", words));
Array.Sort(words, StringComparer.OrdinalIgnoreCase);
Console.WriteLine(string.Join(",", words));

// Sort a copy when you must keep the original order
int[] original = { 3, 1, 2 };
int[] sorted = (int[])original.Clone();
Array.Sort(sorted);
Console.WriteLine($"{string.Join(",", original)} -> {string.Join(",", sorted)}");

// Output:
// 1,3,5,7,9
// 9,7,5,3,1
// Banana,apple,pear
// apple,Banana,pear
// 3,1,2 -> 1,2,3
```

Plain `Array.Sort(words)` on strings uses a culture-sensitive comparison by default, so pass a `StringComparer` when you need a predictable order. `Array.Sort` typically runs in O(n log n) time. It changes the array it is given and returns nothing, so do not write `nums = Array.Sort(nums)`.

### 6. Copying, resizing, filling, clearing

```csharp
int[] a = { 1, 2, 3, 4, 5 };

int[] copy = new int[5];
Array.Copy(a, copy, a.Length);               // source, destination, count
Console.WriteLine(string.Join(",", copy));

int[] part = new int[3];
Array.Copy(a, 1, part, 0, 3);                // srcIndex, dest, destIndex, count
Console.WriteLine(string.Join(",", part));

Array.Resize(ref a, 7);                      // creates a NEW array and reassigns 'a'
Console.WriteLine(string.Join(",", a));

Array.Fill(a, 9, 5, 2);                      // value, startIndex, count
Console.WriteLine(string.Join(",", a));

Array.Clear(a, 0, 3);                        // reset 3 elements to default
Console.WriteLine(string.Join(",", a));

// Output:
// 1,2,3,4,5
// 2,3,4
// 1,2,3,4,5,0,0
// 1,2,3,4,5,9,9
// 0,0,0,4,5,9,9
```

`Array.Resize` does not extend the existing array. It allocates a new one and copies the elements, which is why it takes `ref`. If you resize often, use a `List<T>` (Day 15).

### 7. Arrays are reference types

An array variable holds a reference to the array. Assigning it copies the reference, not the data.

```csharp
int[] first = { 1, 2, 3 };
int[] alias = first;            // same array, two names
alias[0] = 100;
Console.WriteLine(first[0]);

int[] independent = (int[])first.Clone();   // shallow copy
independent[0] = -1;
Console.WriteLine(first[0]);

int[] x = { 1, 2, 3 };
int[] y = { 1, 2, 3 };
Console.WriteLine(x == y);                  // compares references, not contents
Console.WriteLine(x.SequenceEqual(y));      // compares contents (System.Linq)

static void Zero(int[] arr)
{
    for (int i = 0; i < arr.Length; i++) arr[i] = 0;
}
Zero(x);                                    // the method changes the caller's array
Console.WriteLine(string.Join(",", x));

// Output:
// 100
// 100
// False
// True
// 0,0,0
```

Passing an array to a method gives the method access to the same array. If the method changes elements, the caller sees it.

### 8. Slices and ranges

```csharp
int[] a = { 10, 20, 30, 40, 50 };

int[] firstThree = a[..3];      // from start up to (not including) index 3
int[] lastTwo = a[^2..];        // last two
int[] middle = a[1..4];         // indexes 1,2,3

Console.WriteLine(string.Join(",", firstThree));
Console.WriteLine(string.Join(",", lastTwo));
Console.WriteLine(string.Join(",", middle));

// Output:
// 10,20,30
// 40,50
// 20,30,40
```

On an array, a range creates a new array holding a copy of those elements.

### 9. Multi-dimensional arrays: rectangular and jagged

```csharp
// Rectangular: every row has the same length
int[,] grid = new int[2, 3]
{
    { 1, 2, 3 },
    { 4, 5, 6 }
};

Console.WriteLine(grid[1, 2]);
Console.WriteLine(grid.GetLength(0));   // rows
Console.WriteLine(grid.GetLength(1));   // columns

for (int r = 0; r < grid.GetLength(0); r++)
{
    for (int c = 0; c < grid.GetLength(1); c++)
    {
        Console.Write(c == 0 ? grid[r, c] : " " + grid[r, c]);
    }
    Console.WriteLine();
}

// Jagged: an array of arrays, rows may differ in length
int[][] jagged = new int[3][];
jagged[0] = new[] { 1 };
jagged[1] = new[] { 2, 3 };
jagged[2] = new[] { 4, 5, 6 };

foreach (int[] row in jagged)
{
    Console.WriteLine($"{row.Length}: {string.Join(",", row)}");
}

// Output:
// 6
// 2
// 3
// 1 2 3
// 4 5 6
// 1: 1
// 2: 2,3
// 3: 4,5,6
```

Jagged arrays (`int[][]`) are more common in everyday code. Rectangular arrays suit fixed tables such as a game board.

### 10. Worked problems

**Reverse an array in place (two pointers).**

```csharp
int[] a = { 1, 2, 3, 4, 5 };

int left = 0;
int right = a.Length - 1;
while (left < right)
{
    (a[left], a[right]) = (a[right], a[left]);   // tuple swap
    left++;
    right--;
}

Console.WriteLine(string.Join(",", a));

// Output:
// 5,4,3,2,1
```

This is O(n) time and O(1) extra space.

**Rotate an array left by k positions.**

```csharp
int[] Rotate(int[] source, int k)
{
    int n = source.Length;
    int[] result = new int[n];
    for (int i = 0; i < n; i++)
    {
        result[i] = source[(i + k) % n];
    }
    return result;
}

Console.WriteLine(string.Join(",", Rotate(new[] { 1, 2, 3, 4, 5 }, 2)));

// Output:
// 3,4,5,1,2
```

**Move all zeros to the end, keeping the order of the others.**

```csharp
int[] a = { 0, 1, 0, 3, 12 };

int write = 0;
foreach (int n in a)
{
    if (n != 0) a[write++] = n;     // compact non-zero values to the front
}
while (write < a.Length) a[write++] = 0;

Console.WriteLine(string.Join(",", a));

// Output:
// 1,3,12,0,0
```

**Second largest value.**

```csharp
int SecondLargest(int[] a)
{
    int largest = int.MinValue;
    int second = int.MinValue;
    foreach (int n in a)
    {
        if (n > largest)
        {
            second = largest;
            largest = n;
        }
        else if (n > second && n < largest)
        {
            second = n;
        }
    }
    return second;
}

Console.WriteLine(SecondLargest(new[] { 5, 9, 2, 9, 7 }));

// Output:
// 7
```

### Common mistakes

- Off-by-one errors: the last valid index is `Length - 1`. Writing `i <= arr.Length` in a loop throws `IndexOutOfRangeException`.
- Thinking `b = a` copies an array. It copies the reference; use `Clone()`, `Array.Copy` or a range for a real copy.
- Comparing arrays with `==` or `Equals` and expecting a content comparison. Use `SequenceEqual`.
- Forgetting `Array.Sort` and `Array.Reverse` change the array in place and return nothing.

### Why this matters for test automation

`Split` results, lists of expected menu items, rows of a table scraped from a page and `[InlineData]` values in xUnit are all arrays or array-like. Comparing the actual texts on a page with an expected array is a daily job. Remember that two arrays with identical content are not `==`; compare contents explicitly (xUnit's `Assert.Equal` on two collections does compare element by element).

### Interview questions

**Q: What is the difference between an array and a `List<T>`?**
A: An array has a fixed size set at creation; a `List<T>` grows and shrinks as needed. Both give fast access by index. Arrays are slightly lighter; lists are more convenient.

**Q: What is the time complexity of reading `arr[i]`, and of searching an unsorted array?**
A: Reading by index is O(1). Searching an unsorted array is O(n), because you may have to check every element.

**Q: Is an array a value type or a reference type?**
A: A reference type, even when its elements are value types such as `int`. Assigning it copies the reference.

**Q: What is the difference between a rectangular `int[,]` and a jagged `int[][]` array?**
A: A rectangular array is one block with equal-length rows. A jagged array is an array of separate arrays, so rows can differ in length.

**Q: How do you compare the contents of two arrays?**
A: Loop and compare element by element, or use `SequenceEqual`.

**Exercise**

1. Create `int[] temps = { 21, 25, 19, 30, 24 }`. Print the sum, average, max, min and the index of the max (use `Array.IndexOf`).
2. Write `bool IsSorted(int[] a)` that returns true if the array is in ascending order. Test with sorted, unsorted, single-element and empty arrays.
3. Write `int[] MergeSorted(int[] a, int[] b)` that merges two sorted arrays into one sorted array using two index variables, without calling `Array.Sort`.
4. Given `string[] expected = { "Home", "Products", "Contact" }` and `string[] actual = { "Home", "Products", "About" }`, print the positions where they differ.

**Check**

- Why does `Array.Resize` need `ref`?
- What does `new string[3]` contain before you assign anything?
- After `int[] b = a; b[0] = 5;` what is `a[0]`?

## Day 15: `List<T>`

`List<T>` is the collection you will use most. It behaves like an array that can grow and shrink. The `T` is a placeholder for the item type: `List<string>` holds strings, `List<int>` holds ints. (This `<T>` syntax is called generics; Day 28 covers how to write your own. For now you only need to use it.)

`List<T>` lives in `System.Collections.Generic`, which implicit usings already import in a default console project.

### 1. Creating and filling a list

```csharp
var browsers = new List<string>();          // empty list
browsers.Add("chromium");
browsers.Add("firefox");
browsers.Add("webkit");

List<int> numbers = new() { 4, 8, 15 };     // collection initializer, target-typed new
List<int> evens = [2, 4, 6];                // collection expression (C# 12 and later)

Console.WriteLine(browsers.Count);          // Count, not Length
Console.WriteLine(numbers.Count);
Console.WriteLine(evens.Count);
Console.WriteLine(browsers[0]);             // index access works like an array
browsers[1] = "firefox-nightly";
Console.WriteLine(string.Join(", ", browsers));

// Output:
// 3
// 3
// 3
// chromium
// chromium, firefox-nightly, webkit
```

Arrays use `Length`; lists use `Count`. Reading an index that does not exist (`browsers[3]` here) throws `ArgumentOutOfRangeException`.

### 2. Adding items

```csharp
var list = new List<int> { 10, 20, 30 };

list.Add(40);                       // at the end
list.AddRange(new[] { 50, 60 });    // add many
list.Insert(0, 5);                  // at index 0, shifting others right
list.InsertRange(1, new[] { 7, 8 });

Console.WriteLine(string.Join(",", list));
Console.WriteLine(list.Count);

// Output:
// 5,7,8,10,20,30,40,50,60
// 9
```

### 3. Removing items

```csharp
var list = new List<string> { "a", "b", "c", "b", "d" };

bool removed = list.Remove("b");        // removes the FIRST match only
Console.WriteLine($"{removed}: {string.Join(",", list)}");

Console.WriteLine(list.Remove("zzz"));  // False when nothing was removed

list.RemoveAt(0);                       // by index
Console.WriteLine(string.Join(",", list));

int count = list.RemoveAll(x => x == "b");   // by condition; returns how many removed
Console.WriteLine($"{count}: {string.Join(",", list)}");

list.Clear();
Console.WriteLine(list.Count);

// Output:
// True: a,c,b,d
// False
// c,b,d
// 1: c,d
// 0
```

### 4. Searching

```csharp
var list = new List<string> { "alice", "bob", "carol", "bob" };

Console.WriteLine(list.Contains("bob"));         // True
Console.WriteLine(list.IndexOf("bob"));          // 1 (first)
Console.WriteLine(list.LastIndexOf("bob"));      // 3
Console.WriteLine(list.IndexOf("zed"));          // -1
Console.WriteLine(list.Exists(n => n.StartsWith("c")));
Console.WriteLine(list.Find(n => n.Length > 3));        // first match, or null
Console.WriteLine(list.FindAll(n => n.Length == 3).Count);
Console.WriteLine(list.FindIndex(n => n == "carol"));
Console.WriteLine(list.Find(n => n == "nobody") is null);

// Output:
// True
// 1
// 3
// -1
// True
// alice
// 2
// 2
// True
```

### 5. Sorting, reversing, copying

```csharp
var nums = new List<int> { 5, 3, 9, 1 };
nums.Sort();                                // in place, ascending
Console.WriteLine(string.Join(",", nums));
nums.Reverse();                             // in place
Console.WriteLine(string.Join(",", nums));

var names = new List<string> { "pear", "apple", "Banana" };
names.Sort(StringComparer.OrdinalIgnoreCase);
Console.WriteLine(string.Join(",", names));

// Copies
var original = new List<int> { 1, 2, 3 };
var alias = original;                       // same list
var copy = new List<int>(original);         // independent copy
alias.Add(4);
copy.Add(99);
Console.WriteLine(string.Join(",", original));
Console.WriteLine(string.Join(",", copy));

int[] asArray = original.ToArray();
List<int> fromArray = new List<int>(asArray);
Console.WriteLine($"{asArray.Length} {fromArray.Count}");
Console.WriteLine(string.Join(",", original.GetRange(1, 2)));

// Output:
// 1,3,5,9
// 9,5,3,1
// apple,Banana,pear
// 1,2,3,4
// 1,2,3,99
// 4 4
// 2,3
```

`List<T>` is a reference type, exactly like arrays: `alias = original` shares one list.

### 6. Iterating, and the "collection was modified" trap

```csharp
var items = new List<int> { 1, 2, 3, 4, 5, 6 };

foreach (int n in items)
{
    Console.WriteLine(n * n);
}

// Wrong: changing the list while foreach is reading it
try
{
    foreach (int n in items)
    {
        if (n % 2 == 0) items.Remove(n);
    }
}
catch (InvalidOperationException)
{
    Console.WriteLine("InvalidOperationException: list modified during foreach");
}

// Right option 1: RemoveAll
items = new List<int> { 1, 2, 3, 4, 5, 6 };
items.RemoveAll(n => n % 2 == 0);
Console.WriteLine(string.Join(",", items));

// Right option 2: a backwards for loop (removals do not shift unvisited items)
items = new List<int> { 1, 2, 3, 4, 5, 6 };
for (int i = items.Count - 1; i >= 0; i--)
{
    if (items[i] % 2 == 0) items.RemoveAt(i);
}
Console.WriteLine(string.Join(",", items));

// Output:
// 1
// 4
// 9
// 16
// 25
// 36
// InvalidOperationException: list modified during foreach
// 1,3,5
// 1,3,5
```

You may change an element's value in a `for` loop, but adding or removing inside `foreach` on the same list throws.

### 7. Lists of lists, and lists of tuples

```csharp
var table = new List<List<string>>
{
    new() { "Name", "Role" },
    new() { "Alice", "Admin" },
    new() { "Bob", "Viewer" }
};

foreach (List<string> row in table)
{
    Console.WriteLine(string.Join(" | ", row));
}
Console.WriteLine(table[2][1]);

var users = new List<(string Name, int Age)>
{
    ("Alice", 30),
    ("Bob", 25)
};
users.Add(("Carol", 41));
foreach (var (name, age) in users)
{
    Console.WriteLine($"{name} is {age}");
}

// Output:
// Name | Role
// Alice | Admin
// Bob | Viewer
// Viewer
// Alice is 30
// Bob is 25
// Carol is 41
```

### 8. Capacity versus Count

A list keeps its items in an internal array. `Count` is how many items you stored. `Capacity` is how large that internal array currently is. When the array is full, the list allocates a bigger one and copies the items. You can avoid repeated growth by passing an expected size.

```csharp
var list = new List<int>(100);              // capacity hint
Console.WriteLine(list.Count);              // 0 items stored
Console.WriteLine(list.Capacity >= 100);    // True: room for at least 100

list.Add(1);
Console.WriteLine(list.Count);

// Output:
// 0
// True
// 1
```

Complexity, all of which you can rely on: `list[i]` is O(1). `Add` at the end is O(1) on average (the occasional growth copy is spread over many adds). `Insert` and `RemoveAt` at an arbitrary index are O(n) because items shift. `Contains`, `IndexOf` and `Remove(value)` are O(n) because they scan.

### 9. Worked problems

**Remove duplicates while keeping order.**

```csharp
var input = new List<int> { 3, 1, 3, 2, 1, 4 };
var result = new List<int>();

foreach (int n in input)
{
    if (!result.Contains(n)) result.Add(n);   // O(n) check, so the whole thing is O(n^2)
}

Console.WriteLine(string.Join(",", result));

// Output:
// 3,1,2,4
```

On Day 16 you will do this in O(n) with a `HashSet<T>`.

**Split into even and odd lists.**

```csharp
int[] source = { 1, 2, 3, 4, 5, 6, 7 };
var evens = new List<int>();
var odds = new List<int>();

foreach (int n in source)
{
    (n % 2 == 0 ? evens : odds).Add(n);
}

Console.WriteLine($"evens: {string.Join(",", evens)}");
Console.WriteLine($"odds: {string.Join(",", odds)}");

// Output:
// evens: 2,4,6
// odds: 1,3,5,7
```

**Find the longest string.**

```csharp
var words = new List<string> { "test", "playwright", "xunit", "page" };

string longest = words[0];
foreach (string w in words)
{
    if (w.Length > longest.Length) longest = w;
}

Console.WriteLine(longest);

// Output:
// playwright
```

**Run-length summary of a list (consecutive groups).**

```csharp
var results = new List<string> { "pass", "pass", "fail", "fail", "fail", "pass" };
var sb = new System.Text.StringBuilder();

int i = 0;
while (i < results.Count)
{
    int j = i;
    while (j < results.Count && results[j] == results[i]) j++;
    sb.Append($"{results[i]}x{j - i} ");
    i = j;
}

Console.WriteLine(sb.ToString().TrimEnd());

// Output:
// passx2 failx3 passx1
```

### Common mistakes

- Using `Length` on a list or `Count` on an array. Arrays use `Length`; lists use `Count`.
- Adding or removing inside a `foreach` over the same list.
- Calling `Remove(x)` and expecting every `x` to disappear. It removes only the first match; use `RemoveAll`.
- Assuming `var copy = original;` makes an independent copy.

### Why this matters for test automation

`await page.Locator("li").AllInnerTextsAsync()` returns an `IReadOnlyList<string>`. You will collect results, filter them, compare them with the expected list and build test data as lists. Knowing that a list is a reference type avoids bugs where one test mutates shared data and breaks another test.

### Interview questions

**Q: Array or `List<T>`: when do you choose each?**
A: Use an array when the size is fixed and known. Use `List<T>` when the number of items changes.

**Q: What does `List<T>` do internally when it becomes full?**
A: It allocates a larger internal array and copies the items over. That is why adding is O(1) on average but one individual add can be slower.

**Q: What is the complexity of `Insert(0, x)` on a list?**
A: O(n), because every existing item shifts one position.

**Q: Why does removing items inside `foreach` throw?**
A: The enumerator detects that the list changed during enumeration and throws `InvalidOperationException`. Use `RemoveAll` or loop backwards with `for`.

**Q: What is the difference between `Count` and `Capacity`?**
A: `Count` is the number of items stored; `Capacity` is the size of the internal array.

**Exercise**

1. Create a `List<string>` of five test names. Add one at position 2, remove one by value, sort the list and print it with indexes.
2. Write `List<int> Squares(int n)` that returns the squares of 1..n.
3. Given `List<int> a = { 1, 2, 3, 4, 5 }`, remove all odd numbers using both approaches shown above (`RemoveAll` and a backwards loop).
4. Write `List<string> Intersect(List<string> a, List<string> b)` returning items present in both lists without duplicates (use `Contains`).

**Check**

- What is the difference between `Remove` and `RemoveAt`?
- What happens when you read `list[list.Count]`?
- Why is `Contains` on a list O(n)?

## Day 16: `Dictionary<K,V>` and `HashSet<T>`

Today's two collections are built for fast lookup. A `Dictionary<TKey, TValue>` maps a unique key to a value (like a phone book: name to number). A `HashSet<T>` stores a set of unique items and answers "is this item in the set?" quickly.

Both are hash-based. The details of hashing are not needed to use them, but you need to know the result: looking up a key, adding and removing are O(1) on average, compared with O(n) for scanning a `List<T>`.

A note on ordering: do not rely on the iteration order of a `Dictionary` or `HashSet`. In the examples below we sort the keys before printing so that the output is predictable. (`OrderBy` and `Order` come from LINQ, covered in Week 6; read `keys.Order()` as "the keys sorted".)

### 1. Creating a dictionary and reading values

```csharp
var ages = new Dictionary<string, int>();
ages["alice"] = 30;          // add (or overwrite) using the indexer
ages["bob"] = 25;
ages.Add("carol", 41);       // Add: throws if the key already exists

Console.WriteLine(ages["alice"]);
Console.WriteLine(ages.Count);
Console.WriteLine(ages.ContainsKey("bob"));
Console.WriteLine(ages.ContainsValue(41));

// Initializer syntax
var urls = new Dictionary<string, string>
{
    ["dev"] = "https://dev.example.com",
    ["qa"] = "https://qa.example.com"
};
Console.WriteLine(urls["qa"]);

// Output:
// 30
// 3
// True
// True
// https://qa.example.com
```

### 2. Missing keys: indexer, `TryGetValue`, `GetValueOrDefault`

Reading a key that does not exist with the indexer throws `KeyNotFoundException`. Use `TryGetValue` when the key may be missing.

```csharp
var ages = new Dictionary<string, int> { ["alice"] = 30 };

try
{
    Console.WriteLine(ages["zed"]);
}
catch (KeyNotFoundException)
{
    Console.WriteLine("KeyNotFoundException");
}

if (ages.TryGetValue("alice", out int age))
{
    Console.WriteLine($"found {age}");
}

if (!ages.TryGetValue("zed", out int missing))
{
    Console.WriteLine($"not found, default is {missing}");
}

Console.WriteLine(ages.GetValueOrDefault("zed", -1));

// Output:
// KeyNotFoundException
// found 30
// not found, default is 0
// -1
```

`TryGetValue` does the lookup once and gives you the value. The pattern `if (ContainsKey(k)) use dict[k]` does two lookups.

### 3. Adding, overwriting, removing

```csharp
var stock = new Dictionary<string, int>();

stock["apple"] = 5;
stock["apple"] = 8;                       // indexer overwrites
Console.WriteLine(stock["apple"]);

try
{
    stock.Add("apple", 1);                // Add throws for an existing key
}
catch (ArgumentException)
{
    Console.WriteLine("ArgumentException: duplicate key");
}

Console.WriteLine(stock.TryAdd("apple", 1));   // False, no exception, no change
Console.WriteLine(stock.TryAdd("pear", 2));    // True

Console.WriteLine(stock.Remove("apple"));      // True: removed
Console.WriteLine(stock.Remove("apple"));      // False: it was not there
Console.WriteLine(stock.Count);

// Output:
// 8
// ArgumentException: duplicate key
// False
// True
// True
// False
// 1
```

### 4. Iterating

```csharp
var scores = new Dictionary<string, int>
{
    ["carol"] = 70,
    ["alice"] = 90,
    ["bob"] = 80
};

foreach (KeyValuePair<string, int> pair in scores.OrderBy(p => p.Key))
{
    Console.WriteLine($"{pair.Key} = {pair.Value}");
}

Console.WriteLine(string.Join(" ", scores.Keys.Order()));

int total = 0;
foreach (int value in scores.Values) total += value;
Console.WriteLine(total);

foreach (var (name, score) in scores.OrderBy(p => p.Key))   // deconstruction
{
    Console.WriteLine($"{name}:{score}");
}

// Output:
// alice = 90
// bob = 80
// carol = 70
// alice bob carol
// 240
// alice:90
// bob:80
// carol:70
```

As with lists, do not add or remove entries while looping over the same dictionary.

### 5. The counting pattern

This is the most common dictionary idiom: count how many times each thing appears.

```csharp
string text = "to be or not to be";
var counts = new Dictionary<string, int>();

foreach (string word in text.Split(' '))
{
    counts[word] = counts.GetValueOrDefault(word) + 1;
}

foreach (var (word, count) in counts.OrderBy(p => p.Key))
{
    Console.WriteLine($"{word}: {count}");
}

// Output:
// be: 2
// not: 1
// or: 1
// to: 2
```

`GetValueOrDefault(word)` returns 0 for a missing int, so the first time a word is seen it becomes `0 + 1`.

The alternative using `TryGetValue`:

```csharp
var counts = new Dictionary<char, int>();

foreach (char c in "banana")
{
    if (counts.TryGetValue(c, out int current))
        counts[c] = current + 1;
    else
        counts[c] = 1;
}

foreach (var (ch, n) in counts.OrderBy(p => p.Key))
{
    Console.WriteLine($"{ch} -> {n}");
}

// Output:
// a -> 3
// b -> 1
// n -> 2
```

### 6. Grouping: a dictionary of lists

```csharp
string[] tests = { "Login_Valid", "Login_Invalid", "Cart_Add", "Login_Locked", "Cart_Remove" };
var byFeature = new Dictionary<string, List<string>>();

foreach (string test in tests)
{
    string feature = test.Split('_')[0];
    if (!byFeature.TryGetValue(feature, out List<string>? list))
    {
        list = new List<string>();
        byFeature[feature] = list;
    }
    list.Add(test);
}

foreach (var (feature, names) in byFeature.OrderBy(p => p.Key))
{
    Console.WriteLine($"{feature}: {string.Join(", ", names)}");
}

// Output:
// Cart: Cart_Add, Cart_Remove
// Login: Login_Valid, Login_Invalid, Login_Locked
```

### 7. Key comparison and key rules

- Keys must be unique, and a key cannot be `null`.
- By default, string keys are case-sensitive. Pass a comparer to change that.

```csharp
var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["Content-Type"] = "application/json"
};

Console.WriteLine(headers["content-type"]);
Console.WriteLine(headers.ContainsKey("CONTENT-TYPE"));

var strict = new Dictionary<string, int> { ["A"] = 1 };
Console.WriteLine(strict.ContainsKey("a"));

// Output:
// application/json
// True
// False
```

Values may be anything, including `null` (for reference types) and duplicates.

### 8. `HashSet<T>`: unique items

```csharp
var seen = new HashSet<string>();

Console.WriteLine(seen.Add("alice"));   // True: it was added
Console.WriteLine(seen.Add("bob"));     // True
Console.WriteLine(seen.Add("alice"));   // False: already present, set unchanged

Console.WriteLine(seen.Count);
Console.WriteLine(seen.Contains("bob"));
Console.WriteLine(seen.Remove("bob"));
Console.WriteLine(seen.Contains("bob"));

// Remove duplicates from an array
int[] numbers = { 3, 1, 3, 2, 1, 4 };
var unique = new HashSet<int>(numbers);
Console.WriteLine(string.Join(",", unique.Order()));

// Output:
// True
// True
// False
// 2
// True
// True
// False
// 1,2,3,4
```

`Add` returns `false` when the item was already there, which is handy for detecting duplicates in one pass.

```csharp
int[] data = { 5, 2, 8, 2, 9, 5 };
var seen = new HashSet<int>();
var duplicates = new HashSet<int>();

foreach (int n in data)
{
    if (!seen.Add(n)) duplicates.Add(n);
}

Console.WriteLine(string.Join(",", duplicates.Order()));
Console.WriteLine(data.Length == seen.Count ? "all unique" : "has duplicates");

// Output:
// 2,5
// has duplicates
```

### 9. Set operations

```csharp
var a = new HashSet<int> { 1, 2, 3, 4 };
var b = new HashSet<int> { 3, 4, 5 };

var union = new HashSet<int>(a);
union.UnionWith(b);
Console.WriteLine(string.Join(",", union.Order()));

var intersection = new HashSet<int>(a);
intersection.IntersectWith(b);
Console.WriteLine(string.Join(",", intersection.Order()));

var difference = new HashSet<int>(a);
difference.ExceptWith(b);                 // in a but not in b
Console.WriteLine(string.Join(",", difference.Order()));

var sym = new HashSet<int>(a);
sym.SymmetricExceptWith(b);               // in exactly one of them
Console.WriteLine(string.Join(",", sym.Order()));

Console.WriteLine(new HashSet<int> { 3, 4 }.IsSubsetOf(a));
Console.WriteLine(a.Overlaps(b));
Console.WriteLine(new HashSet<int> { 1, 2 }.SetEquals(new[] { 2, 1, 1 }));

// Output:
// 1,2,3,4,5
// 3,4
// 1,2
// 1,2,5
// True
// True
// True
```

These methods change the set they are called on, which is why we copy `a` first.

### 10. Dictionary versus List performance, in words

To find an item in a `List<T>` you scan until you hit it: O(n). A dictionary or hash set computes where the item should be and goes straight there: O(1) on average. For a few items you will not notice. For thousands of lookups, the hash-based collection wins by a wide margin. The trade-off is memory, and the loss of a meaningful order.

```csharp
using System.Diagnostics;

var list = new List<int>();
var set = new HashSet<int>();
for (int i = 0; i < 20000; i++)
{
    list.Add(i);
    set.Add(i);
}

int hits = 0;
var sw = Stopwatch.StartNew();
for (int i = 0; i < 20000; i++) if (list.Contains(i)) hits++;
long listMs = sw.ElapsedMilliseconds;

sw.Restart();
for (int i = 0; i < 20000; i++) if (set.Contains(i)) hits++;
long setMs = sw.ElapsedMilliseconds;

Console.WriteLine(hits);
Console.WriteLine($"list: {listMs} ms, set: {setMs} ms");   // timings differ on every machine

// Output:
// 40000
// list: 73 ms, set: 0 ms
```

### Common mistakes

- Reading `dict[key]` for a key that may not exist. Use `TryGetValue` or `GetValueOrDefault`.
- Using `Add` for a key that might already be there (throws). Use the indexer or `TryAdd`.
- Relying on dictionary or set iteration order.
- Searching a big `List<T>` repeatedly with `Contains` when a `HashSet<T>` would do.

### Why this matters for test automation

Dictionaries hold environment URLs, HTTP headers, test data by name and expected-versus-actual counts. Sets check "no duplicate rows in this table" and "these two lists contain the same items regardless of order". Case-insensitive comparers are useful for headers and for text that the UI changes the case of.

### Interview questions

**Q: How does a `Dictionary` achieve fast lookup?**
A: It hashes the key to find a bucket where the entry should be. On average that is O(1), though a pathological set of keys that collide can be slower.

**Q: What is the difference between `dict[key] = v` and `dict.Add(key, v)`?**
A: The indexer adds or overwrites. `Add` throws `ArgumentException` if the key already exists.

**Q: When would you pick `HashSet<T>` over `List<T>`?**
A: When you need uniqueness or many fast "contains" checks and the order does not matter.

**Q: Can a dictionary have duplicate keys? Duplicate values? Null keys?**
A: Keys must be unique and non-null. Values may repeat.

**Q: Is the iteration order of a `Dictionary` guaranteed?**
A: Do not rely on it. If you need sorted keys, sort them or use a sorted collection.

**Exercise**

1. Build a `Dictionary<string, string>` of three environments to base URLs. Look up `"staging"` safely with `TryGetValue` and print a fallback when it is missing.
2. Count how many times each character appears in `"mississippi"` and print the results sorted by character.
3. Given `string[] emails`, group them by domain (the part after `@`) into a `Dictionary<string, List<string>>`.
4. Given two `List<string>` of expected and actual menu items, use `HashSet<string>` operations to print the missing items and the unexpected items.

**Check**

- What is the difference between `Add` and the indexer on a dictionary?
- What does `HashSet<T>.Add` return and why is it useful?
- Why is `HashSet.Contains` faster than `List.Contains` for large collections?

## Day 17: `Queue`, `Stack`, `LinkedList`, choosing a collection

Today covers three collections defined by how you add and remove items: a `Queue<T>` (first in, first out), a `Stack<T>` (last in, first out) and a `LinkedList<T>` (a chain of nodes). Then you will learn to choose the right collection for a job, which is a favourite interview topic.

### 1. `Queue<T>`: first in, first out (FIFO)

Think of people waiting in line: whoever arrived first is served first.

```csharp
var queue = new Queue<string>();

queue.Enqueue("test-1");      // add to the back
queue.Enqueue("test-2");
queue.Enqueue("test-3");

Console.WriteLine(queue.Count);
Console.WriteLine(queue.Peek());      // look at the front WITHOUT removing
Console.WriteLine(queue.Dequeue());   // remove and return the front
Console.WriteLine(queue.Dequeue());
Console.WriteLine(queue.Count);
Console.WriteLine(queue.Contains("test-3"));

// Output:
// 3
// test-1
// test-1
// test-2
// 1
// True
```

`Dequeue` and `Peek` on an empty queue throw `InvalidOperationException`. Use the `Try` versions when the queue might be empty:

```csharp
var queue = new Queue<int>();
queue.Enqueue(10);

if (queue.TryDequeue(out int first))
{
    Console.WriteLine($"got {first}");
}

if (!queue.TryDequeue(out int second))
{
    Console.WriteLine($"empty, default is {second}");
}

try
{
    queue.Dequeue();
}
catch (InvalidOperationException)
{
    Console.WriteLine("InvalidOperationException on empty queue");
}

// Output:
// got 10
// empty, default is 0
// InvalidOperationException on empty queue
```

Enqueue and Dequeue are both O(1) (Enqueue is O(1) amortized, as with a list's `Add`). Iterating with `foreach` visits items from front to back and does not remove them.

**Worked example: process a queue of jobs until it is empty.**

```csharp
var jobs = new Queue<string>(new[] { "login", "search", "checkout" });

int step = 1;
while (jobs.Count > 0)
{
    string job = jobs.Dequeue();
    Console.WriteLine($"{step++}. running {job}");
    if (job == "search" && step < 4)
    {
        jobs.Enqueue("search-retry");   // a retry goes to the back of the line
    }
}

// Output:
// 1. running login
// 2. running search
// 3. running checkout
// 4. running search-retry
```

### 2. `Stack<T>`: last in, first out (LIFO)

Think of a stack of plates: you take the one you put on last.

```csharp
var stack = new Stack<string>();

stack.Push("page1");        // add to the top
stack.Push("page2");
stack.Push("page3");

Console.WriteLine(stack.Count);
Console.WriteLine(stack.Peek());     // top, not removed
Console.WriteLine(stack.Pop());      // remove and return the top
Console.WriteLine(stack.Pop());
Console.WriteLine(stack.Count);

foreach (string s in stack)          // enumerates from the top down
{
    Console.WriteLine(s);
}

// Output:
// 3
// page3
// page3
// page2
// 1
// page1
```

`Pop` and `Peek` on an empty stack throw `InvalidOperationException`; use `TryPop` and `TryPeek` to avoid that.

**Worked example: browser back button.**

```csharp
var history = new Stack<string>();
string current = "home";

void Visit(string page)
{
    history.Push(current);
    current = page;
}

bool Back()
{
    if (!history.TryPop(out string? previous)) return false;
    current = previous;
    return true;
}

Visit("login");
Visit("dashboard");
Console.WriteLine(current);
Back();
Console.WriteLine(current);
Back();
Console.WriteLine(current);
Console.WriteLine(Back());

// Output:
// dashboard
// login
// home
// False
```

**Worked example: balanced brackets (a classic interview question).**

```csharp
bool IsBalanced(string text)
{
    var stack = new Stack<char>();
    foreach (char c in text)
    {
        if (c == '(' || c == '[' || c == '{')
        {
            stack.Push(c);
        }
        else if (c == ')' || c == ']' || c == '}')
        {
            if (stack.Count == 0) return false;
            char open = stack.Pop();
            bool matches = (open == '(' && c == ')')
                        || (open == '[' && c == ']')
                        || (open == '{' && c == '}');
            if (!matches) return false;
        }
    }
    return stack.Count == 0;
}

Console.WriteLine(IsBalanced("{[()]}"));
Console.WriteLine(IsBalanced("([)]"));
Console.WriteLine(IsBalanced("(("));
Console.WriteLine(IsBalanced(""));

// Output:
// True
// False
// False
// True
```

**Worked example: reverse a string with a stack.**

```csharp
var stack = new Stack<char>("hello");
Console.WriteLine(new string(stack.ToArray()));

// Output:
// olleh
```

(`ToArray` on a stack returns items in pop order, top first.)

### 3. `LinkedList<T>`: a chain of nodes

A linked list stores each item in a node that knows the previous and next node. There is no index; you move along the chain.

```csharp
var list = new LinkedList<string>();

list.AddLast("B");
list.AddLast("C");
list.AddFirst("A");

Console.WriteLine(string.Join("->", list));
Console.WriteLine(list.First!.Value);
Console.WriteLine(list.Last!.Value);
Console.WriteLine(list.Count);

LinkedListNode<string>? nodeB = list.Find("B");
list.AddAfter(nodeB!, "B2");           // O(1) once you hold the node
list.AddBefore(nodeB!, "A2");
Console.WriteLine(string.Join("->", list));

list.Remove("C");                      // by value: must search first
list.RemoveFirst();
list.RemoveLast();
Console.WriteLine(string.Join("->", list));

// walk manually
for (var node = list.First; node != null; node = node.Next)
{
    Console.WriteLine($"node {node.Value}");
}

// Output:
// A->B->C
// A
// C
// 3
// A->A2->B->B2->C
// A2->B
// node A2
// node B
```

`First` and `Last` are `null` for an empty list, which is why the compiler asks for `!` when we know the list is not empty.

Cost summary: adding or removing at either end, or next to a node you already hold, is O(1). Finding a value is O(n). There is no `list[i]`. In everyday code a `List<T>` is usually the better default, because items sit together in memory and index access is O(1). Reach for `LinkedList<T>` when you really do many insertions and removals in the middle of a sequence at positions you already hold nodes for.

### 4. Other collections you may meet

- `SortedDictionary<K,V>`, `SortedSet<T>`: keep keys or items in sorted order. Lookups are O(log n).
- `SortedList<K,V>`: also sorted by key, stored in arrays.
- `ConcurrentQueue<T>`, `ConcurrentDictionary<K,V>` (in `System.Collections.Concurrent`): safe to use from several threads at once. You will meet them when tests run in parallel and share state.
- `ImmutableList<T>` and friends (in `System.Collections.Immutable`): never change after creation.

A short example of a sorted collection:

```csharp
var sorted = new SortedDictionary<string, int>
{
    ["carol"] = 3,
    ["alice"] = 1,
    ["bob"] = 2
};

foreach (var (name, value) in sorted)
{
    Console.WriteLine($"{name}={value}");
}

var set = new SortedSet<int> { 5, 1, 3, 1 };
Console.WriteLine(string.Join(",", set));
Console.WriteLine(set.Min);
Console.WriteLine(set.Max);

// Output:
// alice=1
// bob=2
// carol=3
// 1,3,5
// 1
// 5
```

### 5. Choosing a collection

| You need | Use | Why |
|---|---|---|
| A fixed number of items, fast index access | `T[]` | Simple, no overhead |
| A growing list, access by index | `List<T>` | The default choice |
| Lookup by a key | `Dictionary<K,V>` | O(1) average |
| Unique items, fast "contains" | `HashSet<T>` | O(1) average |
| Sorted keys or sorted unique items | `SortedDictionary<K,V>` / `SortedSet<T>` | Kept in order; O(log n) |
| First come, first served | `Queue<T>` | FIFO |
| Undo, back button, matching brackets | `Stack<T>` | LIFO |
| Many inserts and removes at known nodes | `LinkedList<T>` | O(1) at a held node |
| Shared between threads | `Concurrent*` collections | Thread-safe |

Complexity at a glance (n is the number of items):

| Operation | `List<T>` | `Dictionary` / `HashSet` | `Queue` / `Stack` | `LinkedList` |
|---|---|---|---|---|
| Access by index | O(1) | not available | not available | not available |
| Search for a value | O(n) | O(1) average | O(n) | O(n) |
| Add at the end | O(1) amortized | O(1) average | O(1) amortized | O(1) |
| Insert at the front | O(n) | not applicable | not applicable | O(1) |
| Remove by value | O(n) | O(1) average | not applicable | O(n) to find, then O(1) |

A practical decision process: start with `List<T>`. If you keep searching it by a field, move to a `Dictionary`. If you only care about uniqueness, use a `HashSet`. If the order of removal matters, use a queue or stack.

### 6. Pick the right one

```csharp
// Task 1: check 1000 ids against a blocklist many times -> HashSet
var blocklist = new HashSet<int> { 13, 42, 99 };
Console.WriteLine(blocklist.Contains(42));

// Task 2: look up a user's email by id -> Dictionary
var emails = new Dictionary<int, string> { [1] = "a@test.com", [2] = "b@test.com" };
Console.WriteLine(emails[2]);

// Task 3: keep the order tests were added, show them by position -> List
var tests = new List<string> { "t1", "t2", "t3" };
Console.WriteLine(tests[1]);

// Task 4: run work items in arrival order -> Queue
var work = new Queue<string>(tests);
Console.WriteLine(work.Dequeue());

// Task 5: undo the last action first -> Stack
var undo = new Stack<string>(tests);
Console.WriteLine(undo.Pop());

// Output:
// True
// b@test.com
// t2
// t1
// t3
```

### Common mistakes

- Calling `Dequeue`, `Pop` or `Peek` on an empty collection without checking `Count` or using a `Try` method.
- Mixing up the order: a queue gives back the oldest item, a stack the newest.
- Picking `LinkedList<T>` "because it is faster to insert" and then needing index access, which it does not have.
- Using a `List<T>` as a queue with `RemoveAt(0)`, which shifts every item (O(n)); use `Queue<T>`.

### Why this matters for test automation

A `Queue<T>` suits a list of URLs to visit (crawling a site, breadth first). A `Stack<T>` mirrors browser history and nested page or frame navigation. A `ConcurrentQueue<T>` or `ConcurrentDictionary` appears when parallel tests share test accounts or results. Choosing a `Dictionary` or `HashSet` over repeated list scans keeps data-heavy helpers fast.

### Interview questions

**Q: Queue versus stack?**
A: A queue is first in, first out (`Enqueue`, `Dequeue`). A stack is last in, first out (`Push`, `Pop`).

**Q: When would you use a `LinkedList<T>` instead of a `List<T>`?**
A: When you frequently insert or remove at known positions in the middle and do not need index access. In most code `List<T>` is the better default.

**Q: What are the complexities of `Enqueue`, `Dequeue`, `Push`, `Pop`?**
A: O(1) each (amortized for the operations that may need to grow internal storage).

**Q: How would you implement a queue with two stacks?**
A: Push new items on an `in` stack. To dequeue, if the `out` stack is empty, pop everything from `in` and push it onto `out`; then pop from `out`. This reverses the order twice, giving FIFO.

**Q: How would you check for balanced brackets?**
A: Push opening brackets on a stack; for each closing bracket pop and check it matches. The string is balanced if no mismatch occurs and the stack is empty at the end.

**Exercise**

1. Simulate a printer queue: enqueue five document names, dequeue and print them with a counter until the queue is empty.
2. Write `string ReverseWords(string sentence)` using a `Stack<string>`: `"one two three"` becomes `"three two one"`.
3. Create a `LinkedList<int>` with 1, 2, 3, 4. Insert 99 after the node holding 2, then print the list forwards and (using `Last` and `Previous`) backwards.
4. For each of these, say which collection you would choose and why: (a) unique visited URLs, (b) test results by test name, (c) retry attempts in order, (d) pages to go back through.

**Check**

- Which method on `Stack<T>` looks at the top without removing it?
- Why can't you write `linkedList[2]`?
- What does `queue.TryDequeue(out var x)` return when the queue is empty?

## Day 18: Review and interview questions on collections

Today you consolidate Week 3 and practise the coding problems that interviewers ask about strings, arrays and collections. For each problem, first try to solve it yourself on paper or in an editor, then compare with the solutions. Each solution states its time complexity (n is the input length).

### Recap

| Topic | Key facts |
|---|---|
| `string` | Immutable; methods return new strings. `Trim`, `Split`, `Substring`, `IndexOf` (-1 if missing), `Replace`. Compare with `StringComparison.OrdinalIgnoreCase`. |
| `StringBuilder` | Mutable buffer; use for many appends in a loop. |
| Array `T[]` | Fixed size, `Length`, O(1) index access, reference type. `Array.Sort`, `Reverse`, `IndexOf`, `Copy`, `Resize`. |
| `List<T>` | Growable, `Count`, O(1) index, O(n) `Contains`/`Insert`/`Remove`. Do not modify inside `foreach`. |
| `Dictionary<K,V>` | Unique keys, O(1) average lookup. `TryGetValue`, `TryAdd`, indexer overwrites, `Add` throws on duplicates. |
| `HashSet<T>` | Unique items, O(1) average `Add`/`Contains`. Set operations: `UnionWith`, `IntersectWith`, `ExceptWith`. |
| `Queue<T>` | FIFO: `Enqueue`, `Dequeue`, `Peek`. |
| `Stack<T>` | LIFO: `Push`, `Pop`, `Peek`. |
| `LinkedList<T>` | Nodes, no index, O(1) insert next to a held node. |

### Part 1: Classic coding problems with solutions

#### Problem 1: Reverse a string

Approach A: convert to a `char[]` and reverse it. Approach B: build the result from the end. Both are O(n).

```csharp
using System.Text;

string ReverseA(string s)
{
    char[] chars = s.ToCharArray();
    Array.Reverse(chars);
    return new string(chars);
}

string ReverseB(string s)
{
    var sb = new StringBuilder(s.Length);
    for (int i = s.Length - 1; i >= 0; i--)
    {
        sb.Append(s[i]);
    }
    return sb.ToString();
}

string ReverseC(string s)
{
    char[] chars = s.ToCharArray();
    int left = 0, right = chars.Length - 1;
    while (left < right)
    {
        (chars[left], chars[right]) = (chars[right], chars[left]);
        left++;
        right--;
    }
    return new string(chars);
}

Console.WriteLine(ReverseA("playwright"));
Console.WriteLine(ReverseB("xunit"));
Console.WriteLine(ReverseC("abcd"));

// Output:
// thgirwyalp
// tinux
// dcba
```

Mistake to avoid: `s = s + c` in a loop (or prepending) creates a new string each step; this is O(n^2) in total. The three versions above are what interviewers usually expect to see.

#### Problem 2: Palindrome check

A palindrome reads the same forwards and backwards. Compare characters from both ends moving inward (O(n) time, O(1) extra space). Real tests usually ignore case and non-letters, so skip those characters.

```csharp
bool IsPalindrome(string s)
{
    int left = 0;
    int right = s.Length - 1;

    while (left < right)
    {
        if (!char.IsLetterOrDigit(s[left])) { left++; continue; }
        if (!char.IsLetterOrDigit(s[right])) { right--; continue; }

        if (char.ToLowerInvariant(s[left]) != char.ToLowerInvariant(s[right]))
        {
            return false;
        }
        left++;
        right--;
    }
    return true;
}

Console.WriteLine(IsPalindrome("racecar"));
Console.WriteLine(IsPalindrome("A man, a plan, a canal: Panama"));
Console.WriteLine(IsPalindrome("playwright"));
Console.WriteLine(IsPalindrome(""));

// Output:
// True
// True
// False
// True
```

#### Problem 3: Count duplicates

Given an array, find which values appear more than once and how many times. Brute force compares every pair (O(n^2)). A dictionary does it in one pass (O(n)).

```csharp
int[] data = { 4, 2, 4, 7, 2, 4, 9 };

var counts = new Dictionary<int, int>();
foreach (int n in data)
{
    counts[n] = counts.GetValueOrDefault(n) + 1;
}

foreach (var (value, count) in counts.OrderBy(p => p.Key))
{
    if (count > 1)
    {
        Console.WriteLine($"{value} appears {count} times");
    }
}

// Alternative: only detect duplicates, using a HashSet
var seen = new HashSet<int>();
bool hasDuplicate = false;
foreach (int n in data)
{
    if (!seen.Add(n)) { hasDuplicate = true; break; }
}
Console.WriteLine(hasDuplicate);

// Output:
// 2 appears 2 times
// 4 appears 3 times
// True
```

#### Problem 4: Anagram check

Two words are anagrams if they use exactly the same letters the same number of times (`"listen"` and `"silent"`). Approach A: sort both and compare, O(n log n). Approach B: count letters, O(n).

```csharp
bool IsAnagramSort(string a, string b)
{
    if (a.Length != b.Length) return false;
    char[] x = a.ToLowerInvariant().ToCharArray();
    char[] y = b.ToLowerInvariant().ToCharArray();
    Array.Sort(x);
    Array.Sort(y);
    return x.SequenceEqual(y);
}

bool IsAnagramCount(string a, string b)
{
    if (a.Length != b.Length) return false;

    var counts = new Dictionary<char, int>();
    foreach (char c in a.ToLowerInvariant())
    {
        counts[c] = counts.GetValueOrDefault(c) + 1;
    }
    foreach (char c in b.ToLowerInvariant())
    {
        if (!counts.TryGetValue(c, out int n) || n == 0) return false;
        counts[c] = n - 1;
    }
    return true;
}

Console.WriteLine(IsAnagramSort("Listen", "Silent"));
Console.WriteLine(IsAnagramCount("Listen", "Silent"));
Console.WriteLine(IsAnagramSort("hello", "world"));
Console.WriteLine(IsAnagramCount("aab", "abb"));

// Output:
// True
// True
// False
// False
```

Because the lengths are equal, if every character of `b` can be matched against the counts of `a`, no counts are left over.

#### Problem 5: Find the missing number

An array holds the numbers 1..n with exactly one number missing. Find it. Approach A: the expected sum of 1..n is n*(n+1)/2; subtract the actual sum. O(n) time, O(1) space. Approach B: a `HashSet`.

```csharp
int MissingBySum(int[] numbers)
{
    int n = numbers.Length + 1;          // one number is missing, so the full range is longer
    int expected = n * (n + 1) / 2;
    int actual = 0;
    foreach (int x in numbers) actual += x;
    return expected - actual;
}

int MissingBySet(int[] numbers)
{
    var set = new HashSet<int>(numbers);
    for (int i = 1; i <= numbers.Length + 1; i++)
    {
        if (!set.Contains(i)) return i;
    }
    return -1;
}

int[] input = { 1, 2, 4, 5, 6 };
Console.WriteLine(MissingBySum(input));
Console.WriteLine(MissingBySet(input));
Console.WriteLine(MissingBySum(new[] { 2, 3, 1, 5 }));

// Output:
// 3
// 3
// 4
```

For very large n the sum could overflow `int`; use `long` if that matters.

#### Problem 6: Two-sum

Given an array and a target, return the indexes of two different elements that add up to the target. Brute force checks all pairs: O(n^2) time. With a dictionary from value to index you need one pass: O(n) time, O(n) space.

```csharp
int[]? TwoSumBrute(int[] nums, int target)
{
    for (int i = 0; i < nums.Length; i++)
    {
        for (int j = i + 1; j < nums.Length; j++)
        {
            if (nums[i] + nums[j] == target) return new[] { i, j };
        }
    }
    return null;
}

int[]? TwoSum(int[] nums, int target)
{
    var indexByValue = new Dictionary<int, int>();
    for (int i = 0; i < nums.Length; i++)
    {
        int needed = target - nums[i];
        if (indexByValue.TryGetValue(needed, out int j))
        {
            return new[] { j, i };
        }
        indexByValue[nums[i]] = i;
    }
    return null;
}

int[] nums = { 2, 7, 11, 15 };
Console.WriteLine(string.Join(",", TwoSumBrute(nums, 9)!));
Console.WriteLine(string.Join(",", TwoSum(nums, 26)!));
Console.WriteLine(TwoSum(nums, 100) is null);
Console.WriteLine(string.Join(",", TwoSum(new[] { 3, 3 }, 6)!));

// Output:
// 0,1
// 2,3
// True
// 0,1
```

The dictionary is filled as you go, so an element is never paired with itself; the check for `needed` happens before the current value is stored.

#### Problem 7: Word frequency

Count how often each word appears, ignoring case and punctuation, and print the most frequent first.

```csharp
string text = "The cat and the hat. The cat sat! And the end.";

char[] separators = { ' ', '.', ',', '!', '?' };
string[] words = text.ToLowerInvariant().Split(separators, StringSplitOptions.RemoveEmptyEntries);

var freq = new Dictionary<string, int>();
foreach (string word in words)
{
    freq[word] = freq.GetValueOrDefault(word) + 1;
}

// highest count first, ties broken alphabetically (OrderByDescending/ThenBy are LINQ)
foreach (var (word, count) in freq.OrderByDescending(p => p.Value).ThenBy(p => p.Key))
{
    Console.WriteLine($"{word}: {count}");
}

// Output:
// the: 4
// and: 2
// cat: 2
// end: 1
// hat: 1
// sat: 1
```

#### Problem 8: Remove duplicates

Approach A (unsorted, keep first occurrences in order): a `HashSet` plus a result list, O(n). Approach B (already sorted array, in place, two pointers): O(n) time, O(1) extra space.

```csharp
List<int> DistinctKeepOrder(IEnumerable<int> source)
{
    var seen = new HashSet<int>();
    var result = new List<int>();
    foreach (int n in source)
    {
        if (seen.Add(n)) result.Add(n);
    }
    return result;
}

// For a SORTED array: returns the new length; the first 'length' items are unique
int RemoveDuplicatesSorted(int[] a)
{
    if (a.Length == 0) return 0;
    int write = 1;
    for (int read = 1; read < a.Length; read++)
    {
        if (a[read] != a[write - 1])
        {
            a[write++] = a[read];
        }
    }
    return write;
}

Console.WriteLine(string.Join(",", DistinctKeepOrder(new[] { 3, 1, 3, 2, 1, 4 })));

int[] sorted = { 1, 1, 2, 2, 2, 3, 5, 5 };
int length = RemoveDuplicatesSorted(sorted);
Console.WriteLine(length);
Console.WriteLine(string.Join(",", sorted.Take(length)));

string noDupChars = new string("programming".Distinct().ToArray());
Console.WriteLine(noDupChars);

// Output:
// 3,1,2,4
// 4
// 1,2,3,5
// progamin
```

`Distinct()` is the LINQ shortcut. Interviewers often want to see the manual version first.

### Part 2: Mixed practice questions

1. What does the line `"report".ToUpper();` (result not assigned) do to the variable it is called on?
2. After `int[] a = {1, 2, 3}; int[] b = a; b[0] = 9;` what is `a[0]`?
3. What does `"a,b,,c".Split(',').Length` return, and what if you pass `StringSplitOptions.RemoveEmptyEntries`?
4. Which exception does `dict["missing"]` throw, and what is the safe alternative?
5. What are the complexities of `List<T>.Contains` and `HashSet<T>.Contains`?
6. You push 1, 2, 3 onto a `Stack<int>` and pop twice. What is left, and what does `Peek()` return?
7. What does this print? `var q = new Queue<int>(); q.Enqueue(1); q.Enqueue(2); q.Dequeue(); q.Enqueue(3); Console.WriteLine(string.Join(",", q));`
8. Why does `foreach (var x in list) { list.Remove(x); }` fail, and how do you fix it?
9. What does `HashSet<int>.Add` return when the value is already in the set?
10. Which collection would you use to look up a user object by its id many times?

<details>
<summary>Answers</summary>

1. Nothing useful. Strings are immutable, so `ToUpper` returns a new string that is discarded. Write `report = report.ToUpper();`.
2. `9`. `b` and `a` refer to the same array.
3. `4` (the items are `a`, `b`, empty string, `c`). With `RemoveEmptyEntries` it returns `3`.
4. `KeyNotFoundException`. Use `TryGetValue` or `GetValueOrDefault`.
5. `List<T>.Contains` is O(n). `HashSet<T>.Contains` is O(1) on average.
6. One item (`1`) is left and `Peek()` returns `1`.
7. `2,3`
8. The list is modified while being enumerated, which throws `InvalidOperationException`. Use `RemoveAll` with a condition, a backwards `for` loop, or iterate over a copy.
9. `false`, and the set is unchanged.
10. A `Dictionary<int, User>`, for O(1) average lookup by key.

</details>

### Part 3: Mini-program

Write a console program that processes this array of test results, each in the form `Feature:TestName:Status`:

```text
Login:ValidUser:Pass
Login:BadPassword:Fail
Cart:AddItem:Pass
Cart:RemoveItem:Fail
Cart:Checkout:Pass
Search:EmptyQuery:Pass
Login:Locked:Fail
```

Requirements:

1. Parse each line with `Split(':')`.
2. Count results per status in a `Dictionary<string, int>`.
3. Collect the distinct feature names in a `HashSet<string>` and print them sorted.
4. Put the names of failing tests into a `Queue<string>` and "re-run" them, printing `Re-running Feature.TestName` in order.
5. Print the pass rate as a percentage with one decimal place.

<details>
<summary>Reference solution</summary>

```csharp
string[] lines =
{
    "Login:ValidUser:Pass",
    "Login:BadPassword:Fail",
    "Cart:AddItem:Pass",
    "Cart:RemoveItem:Fail",
    "Cart:Checkout:Pass",
    "Search:EmptyQuery:Pass",
    "Login:Locked:Fail"
};

var statusCounts = new Dictionary<string, int>();
var features = new HashSet<string>();
var failed = new Queue<string>();

foreach (string line in lines)
{
    string[] parts = line.Split(':');
    string feature = parts[0];
    string test = parts[1];
    string status = parts[2];

    statusCounts[status] = statusCounts.GetValueOrDefault(status) + 1;
    features.Add(feature);
    if (status == "Fail") failed.Enqueue($"{feature}.{test}");
}

foreach (var (status, count) in statusCounts.OrderBy(p => p.Key))
{
    Console.WriteLine($"{status}: {count}");
}

Console.WriteLine("Features: " + string.Join(", ", features.Order()));

while (failed.Count > 0)
{
    Console.WriteLine($"Re-running {failed.Dequeue()}");
}

double passRate = 100.0 * statusCounts.GetValueOrDefault("Pass") / lines.Length;
Console.WriteLine($"Pass rate: {passRate:F1}%");

// Output:
// Fail: 3
// Pass: 4
// Features: Cart, Login, Search
// Re-running Login.BadPassword
// Re-running Cart.RemoveItem
// Re-running Login.Locked
// Pass rate: 57.1%
```

</details>

### Part 4: Interview questions

**Q: Why is string concatenation in a loop considered slow, and what do you do instead?**
A: Each `+=` creates a new string and copies the old content, so the total work grows quadratically with the number of iterations. Use `StringBuilder`.

**Q: Explain the difference between `==` and `Equals` for strings, and how to do a case-insensitive comparison.**
A: For strings both compare the characters (case-sensitive). For case-insensitive comparison use `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)`.

**Q: When is a `Dictionary` preferable to a `List`?**
A: When you look items up by a key. A dictionary is O(1) on average; a list needs an O(n) scan.

**Q: How would you find duplicates in an array in O(n) time?**
A: Walk the array with a `HashSet`; if `Add` returns false the value is a duplicate. Or count with a dictionary.

**Q: What happens to the iteration order of a `Dictionary` or `HashSet`?**
A: Do not depend on it. Sort the keys when you need a defined order, or use `SortedDictionary` / `SortedSet`.

**Q: What is the difference between an array and a `List<T>` in memory terms?**
A: An array has a fixed size. A `List<T>` wraps an array and replaces it with a larger one when it runs out of room.

**Q: Which collection is best for implementing undo?**
A: A `Stack<T>`: push each action, pop to undo the most recent.

**Q: What is the time complexity of two-sum with a dictionary, and why is it better than brute force?**
A: O(n) time and O(n) space. Brute force is O(n^2) time; the dictionary turns the inner search into an O(1) average lookup.

**Check**

- Can you write the reverse, palindrome and two-sum solutions without looking?
- Can you say the complexity of each solution and what makes it that?
- Without notes, can you pick a collection for: lookup by key, uniqueness, FIFO, LIFO, ordered growing list?

