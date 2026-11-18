using PlaywrightXUnitv3Learning.Tests.Course.Samples;

[assembly: AssemblyFixture(typeof(PlaywrightXUnitv3Learning.Tests.Course.Week19.GlobalFixture))]

namespace PlaywrightXUnitv3Learning.Tests.Course.Week19
{
    public class GlobalFixture
    {
        public static int Created;
        public GlobalFixture() => Interlocked.Increment(ref Created);
    }

    public class AssemblyFixtureTests
    {
        public AssemblyFixtureTests(GlobalFixture fixture) => Assert.NotNull(fixture);

        [Fact]
        public void Day112_AssemblyFixture_CreatedOnce() => Assert.Equal(1, GlobalFixture.Created);
    }

    // ---------- Day 109: constructor and IDisposable ----------

    public class ConstructorLifecycleTests : IDisposable
    {
        private static int _constructed;
        private static int _disposed;
        private readonly ShoppingCart _cart = new();
        private readonly Guid _instanceId = Guid.NewGuid();

        public ConstructorLifecycleTests()
        {
            Interlocked.Increment(ref _constructed);
            _cart.Add(new CartItem("Backpack", 10m));
        }

        public void Dispose() => Interlocked.Increment(ref _disposed);

        [Fact]
        public void Day109_EachTest_GetsFreshCart_A()
        {
            _cart.Add(new CartItem("Onesie", 5m));
            Assert.Equal(2, _cart.Items.Count);
        }

        [Fact]
        public void Day109_EachTest_GetsFreshCart_B()
        {
            Assert.Single(_cart.Items);
        }

        [Fact]
        public void Day109_InstanceId_IsNotEmpty()
        {
            Assert.NotEqual(Guid.Empty, _instanceId);
        }
    }

    // ---------- Day 110: IAsyncLifetime ----------

    public class AsyncLifetimeTests : IAsyncLifetime
    {
        private readonly List<string> _log = new();
        private ShoppingCart _cart = null!;

        public async ValueTask InitializeAsync()
        {
            await Task.Delay(1, TestContext.Current.CancellationToken);
            _cart = new ShoppingCart();
            _log.Add("init");
        }

        public ValueTask DisposeAsync()
        {
            _log.Add("dispose");
            return ValueTask.CompletedTask;
        }

        [Fact]
        public void Day110_InitializeAsync_RanBeforeTest()
        {
            Assert.NotNull(_cart);
            Assert.Equal(new[] { "init" }, _log);
        }
    }

    // ---------- Day 111: class fixtures ----------

    public class CounterFixture : IAsyncLifetime
    {
        public static int Created;
        public Guid Id { get; } = Guid.NewGuid();
        public int Calls;

        public ValueTask InitializeAsync()
        {
            Interlocked.Increment(ref Created);
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    public class ClassFixtureTests : IClassFixture<CounterFixture>
    {
        private readonly CounterFixture _fixture;

        public ClassFixtureTests(CounterFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Day111_Fixture_IsShared_A()
        {
            Interlocked.Increment(ref _fixture.Calls);
            Assert.NotEqual(Guid.Empty, _fixture.Id);
        }

        [Fact]
        public void Day111_Fixture_IsShared_B()
        {
            Interlocked.Increment(ref _fixture.Calls);
            Assert.Equal(1, CounterFixture.Created);
        }
    }

    // ---------- Day 112: collection fixtures ----------

    public class SharedResourceFixture
    {
        public static int Created;
        public SharedResourceFixture() => Interlocked.Increment(ref Created);
        public string Name => "shared";
    }

    [CollectionDefinition("Shared resource")]
    public class SharedResourceCollection : ICollectionFixture<SharedResourceFixture>
    {
    }

    [Collection("Shared resource")]
    public class CollectionFixtureOneTests
    {
        private readonly SharedResourceFixture _fixture;
        public CollectionFixtureOneTests(SharedResourceFixture fixture) => _fixture = fixture;

        [Fact]
        public void Day112_ClassOne_SeesFixture() => Assert.Equal("shared", _fixture.Name);
    }

    [Collection("Shared resource")]
    public class CollectionFixtureTwoTests
    {
        private readonly SharedResourceFixture _fixture;
        public CollectionFixtureTwoTests(SharedResourceFixture fixture) => _fixture = fixture;

        [Fact]
        public void Day112_ClassTwo_SeesSameFixtureInstanceCount()
        {
            Assert.Equal("shared", _fixture.Name);
            Assert.Equal(1, SharedResourceFixture.Created);
        }
    }
}
