using System.Collections.Concurrent;
using Xunit.Sdk;
using Xunit.v3;

namespace PlaywrightXUnitv3Learning.Tests.Course.Week20
{
    // ---------- Day 115: traits ----------

    public class TraitTests
    {
        [Fact]
        [Trait("Category", "Smoke")]
        public void Day115_Smoke() => Assert.True(true);

        [Fact]
        [Trait("Category", "Regression")]
        [Trait("Owner", "team-checkout")]
        public void Day115_Regression() => Assert.True(true);
    }

    // ---------- Day 116: output ----------

    public class OutputTests
    {
        private readonly ITestOutputHelper _output;

        public OutputTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public void Day116_OutputHelper_Injected()
        {
            _output.WriteLine("hello from the injected helper");
            Assert.NotNull(_output);
        }

        [Fact]
        public void Day116_TestContext_Current()
        {
            ITestContext context = TestContext.Current;
            Assert.NotNull(context.Test);
            Assert.Contains("Day116_TestContext_Current", context.Test!.TestDisplayName);
            TestContext.Current.SendDiagnosticMessage("diagnostic from Day116");
            TestContext.Current.TestOutputHelper?.WriteLine("via TestContext");
        }
    }

    // ---------- Day 117: parallelism ----------

    [Collection("Serial A")]
    public class SerialOneTests
    {
        public static readonly ConcurrentQueue<string> Log = new();

        [Fact]
        public async Task Day117_SameCollection_One()
        {
            Log.Enqueue("one-start");
            await Task.Delay(50, TestContext.Current.CancellationToken);
            Log.Enqueue("one-end");
        }
    }

    [Collection("Serial A")]
    public class SerialTwoTests
    {
        [Fact]
        public async Task Day117_SameCollection_Two()
        {
            SerialOneTests.Log.Enqueue("two-start");
            await Task.Delay(50, TestContext.Current.CancellationToken);
            SerialOneTests.Log.Enqueue("two-end");
        }
    }

    // ---------- Day 118: ordering ----------

    public class AlphabeticalOrderer : ITestCaseOrderer
    {
        public IReadOnlyCollection<TTestCase> OrderTestCases<TTestCase>(IReadOnlyCollection<TTestCase> testCases)
            where TTestCase : notnull, ITestCase
            => testCases.OrderBy(t => t.TestMethodName, StringComparer.Ordinal).ToList();
    }

    [TestCaseOrderer(typeof(AlphabeticalOrderer))]
    public class OrderedTests
    {
        public static readonly List<string> Seen = new();

        [Fact] public void Step3_C() => Seen.Add("C");
        [Fact] public void Step1_A() => Seen.Add("A");
        [Fact] public void Step2_B() => Seen.Add("B");
    }

    // ---------- Day 119: flaky tests ----------

    public class FlakyTests
    {
        private static int _attempts;

        [Fact]
        public void Day119_Deterministic_NotFlaky()
        {
            Interlocked.Increment(ref _attempts);
            Assert.True(_attempts >= 1);
        }
    }
}
