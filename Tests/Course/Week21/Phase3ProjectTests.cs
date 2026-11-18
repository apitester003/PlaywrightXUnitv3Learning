using PlaywrightXUnitv3Learning.Tests.Course.Samples;

namespace PlaywrightXUnitv3Learning.Tests.Course.Week21
{
    /// <summary>Day 125: Phase 3 mini-project. A complete suite for ShoppingCart.</summary>
    [Trait("Layer", "Unit")]
    public class ShoppingCartTests
    {
        private static CartItem Item(string name = "Backpack", decimal price = 10m, int quantity = 1) =>
            new(name, price, quantity);

        // ----- Add -----

        [Smoke]
        [Fact]
        public void Add_NewItem_AppearsInItems()
        {
            var cart = new ShoppingCart();

            cart.Add(Item());

            Assert.Single(cart.Items, i => i.Name == "Backpack");
        }

        [Fact]
        public void Add_SameNameTwice_MergesQuantity()
        {
            var cart = new ShoppingCart();

            cart.Add(Item(quantity: 1));
            cart.Add(Item(quantity: 2));

            Assert.Single(cart.Items);
            Assert.Equal(3, cart.Count);
        }

        [Fact]
        public void Add_Null_Throws()
        {
            var cart = new ShoppingCart();

            Assert.Throws<ArgumentNullException>(() => cart.Add(null!));
        }

        [Fact]
        public void Add_NegativePrice_Throws()
        {
            var cart = new ShoppingCart();

            Assert.Throws<ArgumentOutOfRangeException>(() => cart.Add(Item(price: -0.01m)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Add_NonPositiveQuantity_Throws(int quantity)
        {
            var cart = new ShoppingCart();

            Assert.Throws<ArgumentOutOfRangeException>(() => cart.Add(Item(quantity: quantity)));
        }

        [Fact]
        public void Add_ZeroPrice_IsAllowed()
        {
            var cart = new ShoppingCart();

            cart.Add(Item(price: 0m));

            Assert.Equal(0m, cart.Total);
        }

        // ----- Remove -----

        [Fact]
        public void Remove_ExistingItem_ReturnsTrueAndEmptiesCart()
        {
            var cart = new ShoppingCart();
            cart.Add(Item());

            bool removed = cart.Remove("Backpack");

            Assert.True(removed);
            Assert.Empty(cart.Items);
        }

        [Fact]
        public void Remove_UnknownItem_ReturnsFalse()
        {
            var cart = new ShoppingCart();

            Assert.False(cart.Remove("Nothing"));
        }

        // ----- Total -----

        public static TheoryData<string, decimal> TotalCases => new()
        {
            { "empty", 0m },
            { "one", 10m },
            { "mixed", 35.50m },
        };

        [Theory]
        [MemberData(nameof(TotalCases))]
        public void Total_ForScenario_ReturnsExpected(string scenario, decimal expected)
        {
            var cart = new ShoppingCart();
            switch (scenario)
            {
                case "one":
                    cart.Add(Item());
                    break;
                case "mixed":
                    cart.Add(Item("A", 10m, 2));
                    cart.Add(Item("B", 15.50m));
                    break;
            }

            Assert.Equal(expected, cart.Total);
        }

        // ----- Discount -----

        [Theory]
        [InlineData(0, 100, 100)]
        [InlineData(25, 100, 75)]
        [InlineData(100, 100, 0)]
        public void ApplyDiscount_InRange_ReturnsExpected(int percent, int price, int expected)
        {
            var cart = new ShoppingCart();
            cart.Add(Item(price: price));

            Assert.Equal((decimal)expected, cart.ApplyDiscount(percent));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void ApplyDiscount_OutOfRange_Throws(int percent)
        {
            var cart = new ShoppingCart();

            Assert.Throws<ArgumentOutOfRangeException>(() => cart.ApplyDiscount(percent));
        }

        [Fact(Skip = "BUG-0000 (example): shows the skip-with-ticket policy from Day 107")]
        public void ApplyDiscount_OnEmptyCart_ExampleOfTicketedSkip()
        {
            Assert.Fail("never runs");
        }
    }
}
