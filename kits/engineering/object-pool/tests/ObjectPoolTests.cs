using System;
using Xunit;

namespace GameDevKit.ObjectPool.Tests
{
    public sealed class ObjectPoolTests
    {
        [Fact]
        public void PrewarmedItemIsReusedAndResetOnReturn()
        {
            int created = 0;
            int reset = 0;
            var pool = new ObjectPool<Probe>(new PoolOptions<Probe>(() =>
            {
                created++;
                return new Probe();
            })
            {
                Reset = probe =>
                {
                    probe.ResetCount++;
                    reset++;
                },
            });

            pool.Prewarm(2);
            Assert.True(pool.TryRent(out Probe? item));
            item!.Value = 10;
            pool.Return(item);

            Assert.Equal(2, created);
            Assert.Equal(1, reset);
            Assert.Equal(10, item.Value);
            Assert.Equal(2, pool.GetStats().Idle);
            Assert.Equal(0, pool.GetStats().Active);
        }

        [Fact]
        public void DropPolicyReturnsFalseWhenPoolHasNoRetainedItem()
        {
            var pool = new ObjectPool<Probe>(new PoolOptions<Probe>(() => new Probe())
            {
                OverflowPolicy = PoolOverflowPolicy.Drop,
            });

            Assert.False(pool.TryRent(out Probe? item));
            Assert.Null(item);
            Assert.Equal(1, pool.GetStats().Dropped);
        }

        [Fact]
        public void GrowPolicyCreatesBeyondRetainedCapacity()
        {
            var pool = new ObjectPool<Probe>(new PoolOptions<Probe>(() => new Probe())
            {
                MaxRetained = 1,
                OverflowPolicy = PoolOverflowPolicy.Grow,
            });

            Assert.True(pool.TryRent(out Probe? first));
            Assert.True(pool.TryRent(out Probe? second));
            Assert.NotSame(first, second);
            Assert.Equal(2, pool.GetStats().Active);
            Assert.Equal(2, pool.GetStats().PeakActive);
        }

        [Fact]
        public void GrowUpToLimitDropsAtTotalCap()
        {
            var pool = new ObjectPool<Probe>(new PoolOptions<Probe>(() => new Probe())
            {
                MaxRetained = 1,
                MaxTotal = 1,
                OverflowPolicy = PoolOverflowPolicy.GrowUpToLimit,
            });

            Assert.True(pool.TryRent(out _));
            Assert.False(pool.TryRent(out _));
            Assert.Equal(1, pool.GetStats().Dropped);
        }

        [Fact]
        public void ReturningAnItemNotRentedFromPoolIsRejected()
        {
            var pool = new ObjectPool<Probe>(new PoolOptions<Probe>(() => new Probe()));

            Assert.Throws<InvalidOperationException>(() => pool.Return(new Probe()));
        }

        [Fact]
        public void DisposeClearsIdleObjectsAndRejectsFutureRentals()
        {
            int destroyed = 0;
            var pool = new ObjectPool<Probe>(new PoolOptions<Probe>(() => new Probe())
            {
                Destroy = _ => destroyed++,
            });
            pool.Prewarm(2);

            pool.Dispose();
            pool.Dispose();

            Assert.Equal(2, destroyed);
            Assert.Throws<ObjectDisposedException>(() => pool.TryRent(out _));
        }

        private sealed class Probe
        {
            public int Value { get; set; }

            public int ResetCount { get; set; }
        }
    }
}
