using System.Runtime.CompilerServices;
using AwesomeAssertions;
using PlaywrightXUnitv3Learning.Tests.Course.Samples;
using Xunit.v3;

namespace PlaywrightXUnitv3Learning.Tests.Course.Week21
{
    // ---------- Day 121: custom attributes ----------

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SmokeAttribute : Attribute, ITraitAttribute
    {
        public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() =>
            new[] { new KeyValuePair<string, string>("Category", "Smoke") };
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SmokeFactAttribute : FactAttribute, ITraitAttribute
    {
        public SmokeFactAttribute(
            [CallerFilePath] string? sourceFilePath = null,
            [CallerLineNumber] int sourceLineNumber = -1)
            : base(sourceFilePath, sourceLineNumber)
        {
        }

        public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() =>
            new[] { new KeyValuePair<string, string>("Category", "Smoke") };
    }

    public class CustomAttributeTests
    {
        [Fact]
        [Smoke]
        public void Day121_CustomTraitAttribute() => Assert.True(true);

        [SmokeFact]
        public void Day121_CustomFactAttribute() => Assert.True(true);
    }

    // ---------- Day 122: AwesomeAssertions ----------

    public class AwesomeAssertionsTests
    {
        [Fact]
        public void Day122_Awesome_Basics()
        {
            var cart = new ShoppingCart();
            cart.Add(new CartItem("Backpack", 29.99m, 2));

            cart.Count.Should().Be(2);
            cart.Total.Should().BeApproximately(59.98m, 0.001m);
            cart.Items.Should().ContainSingle(i => i.Name == "Backpack");
            cart.Items.Should().HaveCount(1).And.OnlyContain(i => i.Price > 0);
        }

        [Fact]
        public void Day122_Awesome_Exceptions()
        {
            var cart = new ShoppingCart();

            Action act = () => cart.ApplyDiscount(101);

            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("percent");
        }

        [Fact]
        public void Day122_Awesome_ObjectGraph()
        {
            var expected = new CartItem("Backpack", 29.99m, 2);
            var actual = new CartItem("Backpack", 29.99m, 2);

            actual.Should().BeEquivalentTo(expected);
        }
    }
}
