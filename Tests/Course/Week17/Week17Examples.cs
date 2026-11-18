using PlaywrightXUnitv3Learning.Tests.Course.Samples;

namespace PlaywrightXUnitv3Learning.Tests.Course.Week17
{
    public class Week17Examples
    {
        // ---------- Day 98: [Fact] and the Assert API ----------

        [Fact]
        public void Day98_Equality_And_Booleans()
        {
            Assert.Equal(4, 2 + 2);
            Assert.NotEqual(5, 2 + 2);
            Assert.True(10 > 3);
            Assert.False(string.IsNullOrEmpty("abc"));
        }

        [Fact]
        public void Day98_Strings()
        {
            const string text = "Sauce Labs Backpack";
            Assert.Contains("Backpack", text);
            Assert.StartsWith("Sauce", text);
            Assert.EndsWith("Backpack", text);
            Assert.Equal("sauce labs backpack", text, ignoreCase: true);
            Assert.Matches("^Sauce.*pack$", text);
        }

        [Fact]
        public void Day98_Collections()
        {
            var names = new List<string> { "Backpack", "Bike Light", "Onesie" };
            Assert.Equal(3, names.Count);
            Assert.Contains("Onesie", names);
            Assert.DoesNotContain("Jacket", names);
            Assert.Single(names, n => n.StartsWith('B') && n.Contains("Light"));
            Assert.All(names, n => Assert.False(string.IsNullOrWhiteSpace(n)));
            Assert.Equal(new[] { "Backpack", "Bike Light", "Onesie" }, names);
            Assert.NotEmpty(names);
        }

        [Fact]
        public void Day98_Nulls_And_Types()
        {
            object? nothing = null;
            Assert.Null(nothing);

            object value = "text";
            Assert.NotNull(value);
            Assert.IsType<string>(value);
            Assert.IsAssignableFrom<IEnumerable<char>>(value);
        }

        [Fact]
        public void Day98_Numbers_With_Tolerance()
        {
            Assert.Equal(0.3, 0.1 + 0.2, precision: 10);
            Assert.InRange(7, 1, 10);
        }

        [Fact]
        public void Day98_Exceptions()
        {
            var cart = new ShoppingCart();

            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => cart.Add(new CartItem("Bad", -1m)));
            Assert.Equal("item", ex.ParamName);

            Assert.Throws<ArgumentNullException>(() => cart.Add(null!));
        }

        [Fact]
        public async Task Day98_Async_Exceptions()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await Task.Delay(1, TestContext.Current.CancellationToken);
                throw new InvalidOperationException("boom");
            });
        }

        [Fact]
        public void Day98_Collection_Of_Checks()
        {
            var item = new CartItem("Backpack", 29.99m, 2);
            Assert.Collection(
                new[] { item },
                i =>
                {
                    Assert.Equal("Backpack", i.Name);
                    Assert.Equal(2, i.Quantity);
                });
        }

        // ---------- Day 99: naming and Arrange-Act-Assert ----------

        [Fact]
        public void Add_NewItem_IncreasesCount()
        {
            // Arrange
            var cart = new ShoppingCart();

            // Act
            cart.Add(new CartItem("Backpack", 29.99m));

            // Assert
            Assert.Equal(1, cart.Count);
        }

        [Fact]
        public void Add_SameItemTwice_MergesQuantity()
        {
            var cart = new ShoppingCart();

            cart.Add(new CartItem("Backpack", 29.99m));
            cart.Add(new CartItem("Backpack", 29.99m, 2));

            Assert.Single(cart.Items);
            Assert.Equal(3, cart.Count);
        }

        [Fact]
        public void Total_WithTwoItems_SumsPriceTimesQuantity()
        {
            var cart = new ShoppingCart();
            cart.Add(new CartItem("Backpack", 29.99m, 2));
            cart.Add(new CartItem("Onesie", 7.99m));

            Assert.Equal(67.97m, cart.Total);
        }

        [Fact]
        public void Remove_UnknownItem_ReturnsFalse()
        {
            var cart = new ShoppingCart();

            bool removed = cart.Remove("Nothing");

            Assert.False(removed);
        }

        [Fact]
        public void ApplyDiscount_Over100_Throws()
        {
            var cart = new ShoppingCart();

            Assert.Throws<ArgumentOutOfRangeException>(() => cart.ApplyDiscount(101));
        }

        // ---------- Day 101: reading failures (these pass; see the notes in the course file) ----------

        [Fact]
        public void Day101_ExpectedVsActual_Order_Matters()
        {
            int expected = 3;
            int actual = new List<int> { 1, 2, 3 }.Count;

            Assert.Equal(expected, actual);
        }

        [Fact(Skip = "Day 101 exercise: remove Skip, run it, and read the failure message.")]
        public void Day101_DeliberateFailure()
        {
            var cart = new ShoppingCart();
            cart.Add(new CartItem("Backpack", 29.99m));

            Assert.Equal(2, cart.Count);
        }
    }
}
