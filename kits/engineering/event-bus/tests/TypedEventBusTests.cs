using System;
using Xunit;

namespace GameDevKit.EventBus.Tests
{
    public sealed class TypedEventBusTests
    {
        private readonly struct Probe
        {
            public Probe(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }

        [Fact]
        public void PublishedEventReachesSubscribers()
        {
            var bus = new TypedEventBus<Probe>();
            int total = 0;
            bus.Subscribe(value => total += value.Value);

            bus.Publish(new Probe(7));

            Assert.Equal(7, total);
            Assert.Equal(1, bus.HandlerCount);
        }

        [Fact]
        public void SubscriptionDisposeUnsubscribesOnce()
        {
            var bus = new TypedEventBus<Probe>();
            int calls = 0;
            IDisposable subscription = bus.Subscribe(_ => calls++);

            subscription.Dispose();
            subscription.Dispose();
            bus.Publish(new Probe(1));

            Assert.Equal(0, calls);
            Assert.Equal(0, bus.HandlerCount);
        }

        [Fact]
        public void SubscriberAddedDuringPublishStartsWithNextEvent()
        {
            var bus = new TypedEventBus<Probe>();
            int lateCalls = 0;
            Action<Probe> late = _ => lateCalls++;

            bus.Subscribe(_ => bus.Subscribe(late));
            bus.Publish(new Probe(1));
            Assert.Equal(0, lateCalls);

            bus.Publish(new Probe(2));
            Assert.Equal(1, lateCalls);
        }

        [Fact]
        public void HandlerFailureIsIsolated()
        {
            Exception? captured = null;
            var bus = new TypedEventBus<Probe>(exception => captured = exception);
            int survivorCalls = 0;

            bus.Subscribe(_ => throw new InvalidOperationException("boom"));
            bus.Subscribe(_ => survivorCalls++);

            bus.Publish(new Probe(1));

            Assert.IsType<InvalidOperationException>(captured);
            Assert.Equal(1, survivorCalls);
        }

        [Fact]
        public void RepeatedPublishDoesNotAllocateAfterWarmup()
        {
            var bus = new TypedEventBus<Probe>();
            int sink = 0;
            bus.Subscribe(value => sink += value.Value);
            bus.Publish(new Probe(1));

            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 1000; i++)
            {
                bus.Publish(new Probe(1));
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.True(allocated < 1024, $"Expected < 1024 bytes, got {allocated}.");
            Assert.Equal(1001, sink);
        }
    }
}
