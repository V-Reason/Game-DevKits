using System.Collections.Generic;
using Xunit;

namespace GameDevKit.EffectOrchestrator.Tests
{
    public sealed class EffectRouterTests
    {
        private readonly struct Context
        {
            public Context(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }

        [Fact]
        public void PlayRoutesToRegisteredDriver()
        {
            var driver = new FakeDriver();
            var router = new EffectRouter<int, Context>();
            router.Register(1, driver);

            EffectHandle handle = router.Play(1, new Context(42));

            Assert.True(handle.IsValid);
            Assert.Equal(new[] { 42 }, driver.PlayedValues);
        }

        [Fact]
        public void UnknownDriverReturnsNoneAndReportsIssue()
        {
            var issues = new List<EffectIssue>();
            var router = new EffectRouter<int, Context>(reportIssue: issues.Add);

            EffectHandle handle = router.Play(99, new Context(1));

            Assert.Equal(EffectHandle.None, handle);
            Assert.Single(issues);
            Assert.Equal("unknown-driver", issues[0].Code);
        }

        [Fact]
        public void PendingPlayReplaysFifoWhenDriverBecomesReady()
        {
            var driver = new FakeDriver { IsReady = false };
            var router = new EffectRouter<int, Context>();
            router.Register(1, driver);

            router.Play(1, new Context(1));
            router.Play(1, new Context(2));
            Assert.Empty(driver.PlayedValues);

            driver.IsReady = true;
            router.Tick(0.016);

            Assert.Equal(new[] { 1, 2 }, driver.PlayedValues);
            Assert.Equal(0, router.GetStats().Pending);
        }

        [Fact]
        public void PendingQueueDropsOldestAtLimit()
        {
            var driver = new FakeDriver { IsReady = false };
            var router = new EffectRouter<int, Context>(
                new EffectRouterOptions { MaxPendingPerEffect = 2 });
            router.Register(1, driver);

            router.Play(1, new Context(1));
            router.Play(1, new Context(2));
            router.Play(1, new Context(3));
            driver.IsReady = true;
            router.Tick(0);

            Assert.Equal(new[] { 2, 3 }, driver.PlayedValues);
        }

        [Fact]
        public void StaleHandleIsNotForwardedAfterClearAll()
        {
            var driver = new FakeDriver();
            var router = new EffectRouter<int, Context>();
            router.Register(1, driver);
            EffectHandle handle = router.Play(1, new Context(1));

            router.ClearAll();

            Assert.False(router.Update(handle, new Context(2)));
            router.Stop(handle);
            Assert.Equal(0, driver.UpdateCalls);
            Assert.Equal(0, driver.StopCalls);
        }

        [Fact]
        public void TickAdvancesEveryDriverAndAggregatesStats()
        {
            var first = new FakeDriver { Active = 2, Pooled = 3 };
            var second = new FakeDriver { Active = 1, Pooled = 4 };
            var router = new EffectRouter<int, Context>();
            router.Register(1, first);
            router.Register(2, second);

            router.Tick(0.05);

            Assert.Equal(0.05, first.Ticked, 6);
            Assert.Equal(0.05, second.Ticked, 6);
            Assert.Equal(3, router.GetStats().Active);
            Assert.Equal(7, router.GetStats().Pooled);
        }

        private sealed class FakeDriver : IEffectDriver<int, Context>
        {
            private readonly object _owner = new();
            private readonly List<int> _played = new();
            private int _generation = 1;

            public bool IsReady { get; set; } = true;

            public int Active { get; set; }

            public int Pooled { get; set; }

            public int UpdateCalls { get; private set; }

            public int StopCalls { get; private set; }

            public double Ticked { get; private set; }

            public IReadOnlyList<int> PlayedValues => _played;

            public bool IsHandleValid(EffectHandle handle)
            {
                return ReferenceEquals(handle.Owner, _owner) && handle.Generation == _generation;
            }

            public EffectHandle Play(int id, in Context context)
            {
                _played.Add(context.Value);
                return new EffectHandle(context.Value, _generation, _owner);
            }

            public void Update(EffectHandle handle, in Context context)
            {
                UpdateCalls++;
            }

            public void Stop(EffectHandle handle)
            {
                StopCalls++;
            }

            public void Tick(double deltaTime)
            {
                Ticked = deltaTime;
            }

            public void ClearAll()
            {
                _generation++;
            }

            public EffectDriverStats GetStats()
            {
                return new EffectDriverStats(Active, Pooled);
            }
        }
    }
}
