using System.Reflection;
using System.Text.Json;
using PlaywrightXUnitv3Learning.Tests.Course.Samples;
using Xunit.Sdk;
using Xunit.v3;


namespace PlaywrightXUnitv3Learning.Tests.Course.Week18
{
    public record LoginCase(string Username, string Password, string ExpectedError);

    public class LoginCases : TheoryData<string, string, string>
    {
        public LoginCases()
        {
            Add("", "secret_sauce", "Username is required");
            Add("standard_user", "", "Password is required");
            Add("locked_out_user", "secret_sauce", "locked out");
        }
    }

    /// <summary>Custom data source: reads a JSON file next to the test assembly.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class JsonFileDataAttribute : DataAttribute
    {
        private readonly string _fileName;

        public JsonFileDataAttribute(string fileName) => _fileName = fileName;

        public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
            MethodInfo testMethod, DisposalTracker disposalTracker)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "TestData", _fileName);
            string json = File.ReadAllText(path);
            List<LoginCase> cases = JsonSerializer.Deserialize<List<LoginCase>>(json)!;

            IReadOnlyCollection<ITheoryDataRow> rows = cases
                .Select(c => (ITheoryDataRow)new TheoryDataRow(c) { TestDisplayName = $"{c.Username}/{c.Password}" })
                .ToList();
            return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(rows);
        }

        public override bool SupportsDiscoveryEnumeration() => true;
    }

    public class Week18Examples
    {
        // ---------- Day 103: [Theory] and [InlineData] ----------

        [Theory]
        [InlineData(0, 100, 100)]
        [InlineData(10, 100, 90)]
        [InlineData(50, 80, 40)]
        [InlineData(100, 80, 0)]
        public void Day103_ApplyDiscount_ReturnsExpected(int percent, int price, int expected)
        {
            var cart = new ShoppingCart();
            cart.Add(new CartItem("Item", price));

            Assert.Equal((decimal)expected, cart.ApplyDiscount(percent));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Day103_ApplyDiscount_OutOfRange_Throws(int percent)
        {
            var cart = new ShoppingCart();

            Assert.Throws<ArgumentOutOfRangeException>(() => cart.ApplyDiscount(percent));
        }

        [Theory]
        [InlineData("standard_user", true)]
        [InlineData("locked_out_user", false)]
        [InlineData(null, false)]
        public void Day103_Null_And_Strings_In_InlineData(string? user, bool allowed)
        {
            bool result = user is "standard_user";
            Assert.Equal(allowed, result);
        }

        // ---------- Day 104: [MemberData] ----------

        public static IEnumerable<object[]> PriceRows =>
            new List<object[]>
            {
                new object[] { new[] { 10m, 20m }, 30m },
                new object[] { new[] { 5.5m }, 5.5m },
                new object[] { Array.Empty<decimal>(), 0m },
            };

        [Theory]
        [MemberData(nameof(PriceRows))]
        public void Day104_Total_MemberData_ObjectArrays(decimal[] prices, decimal expected)
        {
            var cart = new ShoppingCart();
            for (int i = 0; i < prices.Length; i++)
            {
                cart.Add(new CartItem($"Item{i}", prices[i]));
            }

            Assert.Equal(expected, cart.Total);
        }

        public static TheoryData<string, int, decimal> TypedRows => new()
        {
            { "Backpack", 1, 29.99m },
            { "Onesie", 3, 23.97m },
        };

        [Theory]
        [MemberData(nameof(TypedRows))]
        public void Day104_Total_MemberData_TypedTheoryData(string name, int quantity, decimal expectedTotal)
        {
            var cart = new ShoppingCart();
            decimal unit = expectedTotal / quantity;
            cart.Add(new CartItem(name, unit, quantity));

            Assert.Equal(expectedTotal, cart.Total);
        }

        [Theory]
        [MemberData(nameof(RowsWithNames))]
        public void Day104_TheoryDataRow_WithDisplayName(int a, int b, int sum)
        {
            Assert.Equal(sum, a + b);
        }

        public static IEnumerable<TheoryDataRow<int, int, int>> RowsWithNames()
        {
            yield return new(1, 2, 3) { TestDisplayName = "one plus two" };
            yield return new(0, 0, 0) { TestDisplayName = "zeros" };
        }

        // ---------- Day 105: [ClassData] ----------

        [Theory]
        [ClassData(typeof(LoginCases))]
        public void Day105_ClassData_Shape(string user, string password, string expectedError)
        {
            Assert.False(string.IsNullOrEmpty(expectedError));
            Assert.True(user.Length > 0 || password.Length > 0);
        }

        // ---------- Day 106: custom data attributes, JSON ----------

        [Theory]
        [JsonFileData("login-cases.json")]
        public void Day106_JsonData(LoginCase testCase)
        {
            Assert.False(string.IsNullOrWhiteSpace(testCase.ExpectedError));
        }

        [Fact]
        public void Day106_Csv_Parsing_Sample()
        {
            string[] lines = { "user,password", "standard_user,secret_sauce", "locked_out_user,secret_sauce" };

            var rows = lines.Skip(1).Select(l => l.Split(',')).ToList();

            Assert.Equal(2, rows.Count);
            Assert.All(rows, r => Assert.Equal(2, r.Length));
        }

        // ---------- Day 107: skipping and explicit tests ----------

        [Fact(Skip = "Day 107 example: skipped with a reason")]
        public void Day107_Skipped()
        {
            Assert.Fail("Never runs");
        }

        [Fact(Explicit = true)]
        public void Day107_Explicit_RunsOnlyWhenRequested()
        {
            Assert.True(true);
        }

        public static bool IsWindows => OperatingSystem.IsWindows();

        [Fact(SkipUnless = nameof(IsWindows), Skip = "Windows only")]
        public void Day107_Conditional_SkipUnless()
        {
            Assert.True(OperatingSystem.IsWindows());
        }
    }
}
