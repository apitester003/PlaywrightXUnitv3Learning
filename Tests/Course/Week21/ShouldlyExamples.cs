using PlaywrightXUnitv3Learning.Tests.Course.Samples;
using Shouldly;

namespace PlaywrightXUnitv3Learning.Tests.Course.Week21
{
    public class ShouldlyTests
    {
        [Fact]
        public void Day122_Shouldly_Basics()
        {
            var cart = new ShoppingCart();
            cart.Add(new CartItem("Backpack", 29.99m, 2));

            cart.Count.ShouldBe(2);
            cart.Total.ShouldBe(59.98m, 0.001m);
            cart.Items.ShouldHaveSingleItem().Name.ShouldBe("Backpack");
        }

        [Fact]
        public void Day122_Shouldly_Exceptions()
        {
            var cart = new ShoppingCart();

            var ex = Should.Throw<ArgumentOutOfRangeException>(() => cart.ApplyDiscount(101));

            ex.ParamName.ShouldBe("percent");
        }
    }
}
