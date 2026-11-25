using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GameDevKit.AssetLifecycle.Tests
{
    public sealed class AssetCacheTests
    {
        [Fact]
        public async Task SecondAcquireUsesCachedAsset()
        {
            var source = new FakeSource();
            using var cache = new AssetCache<string, object>(source);

            using AssetLease<string, object> first = await cache.AcquireAsync("a");
            first.Dispose();
            using AssetLease<string, object> second = await cache.AcquireAsync("a");

            Assert.Same(first.Asset, second.Asset);
            Assert.Equal(1, source.LoadCalls);
            Assert.Equal(1, cache.GetStats().CacheHits);
        }

        [Fact]
        public async Task ConcurrentAcquiresCoalesceOneSourceLoad()
        {
            var source = new FakeSource(delay: true);
            using var cache = new AssetCache<string, object>(source);

            Task<AssetLease<string, object>> one = cache.AcquireAsync("a");
            Task<AssetLease<string, object>> two = cache.AcquireAsync("a");
            source.Release();

            using AssetLease<string, object> first = await one;
            using AssetLease<string, object> second = await two;

            Assert.Same(first.Asset, second.Asset);
            Assert.Equal(1, source.LoadCalls);
            Assert.Equal(2, cache.GetStats().ActiveLeases);
        }

        [Fact]
        public async Task FailedLoadRetriesAndUsesFallback()
        {
            var fallback = new object();
            var source = new FakeSource(failures: 10);
            using var cache = new AssetCache<string, object>(
                source,
                new AssetCacheOptions { RetryCount = 2 });
            cache.RegisterFallback("a", fallback);

            using AssetLease<string, object> lease = await cache.AcquireAsync("a");

            Assert.Same(fallback, lease.Asset);
            Assert.Equal(3, source.LoadCalls);
            Assert.Equal(1, cache.GetStats().Failures);
        }

        [Fact]
        public async Task MissingAssetWithoutFallbackThrows()
        {
            var source = new FakeSource(missing: true);
            using var cache = new AssetCache<string, object>(
                source,
                new AssetCacheOptions { RetryCount = 0 });

            await Assert.ThrowsAsync<AssetLoadException>(() => cache.AcquireAsync("a"));
        }

        [Fact]
        public async Task CooldownThenLruEvictsIdleEntry()
        {
            var clock = new ManualAssetClock();
            var source = new FakeSource();
            var unloaded = new List<object>();
            using var cache = new AssetCache<string, object>(
                source,
                new AssetCacheOptions
                {
                    MaxEntries = 1,
                    CooldownSeconds = 2,
                    Unload = asset => unloaded.Add(asset),
                },
                clock);

            AssetLease<string, object> a = await cache.AcquireAsync("a");
            object asset = a.Asset;
            a.Dispose();

            clock.NowSeconds = 1;
            cache.Maintain();
            Assert.Equal(1, cache.GetStats().Entries);

            clock.NowSeconds = 3;
            using AssetLease<string, object> b = await cache.AcquireAsync("b");
            cache.Maintain();

            Assert.Equal(1, cache.GetStats().Entries);
            Assert.Contains(asset, unloaded);
        }

        [Fact]
        public async Task PreloadedEntryIsNotEvicted()
        {
            var clock = new ManualAssetClock();
            var source = new FakeSource();
            using var cache = new AssetCache<string, object>(
                source,
                new AssetCacheOptions { MaxEntries = 1, CooldownSeconds = 0 },
                clock);

            await cache.PreloadAsync("pinned");
            AssetLease<string, object> other = await cache.AcquireAsync("other");
            other.Dispose();
            clock.NowSeconds = 2;
            cache.Maintain();

            Assert.Equal(1, cache.GetStats().Entries);
            Assert.Equal(1, cache.GetStats().Evictions);
        }

        [Fact]
        public async Task CancellationDoesNotPoisonFutureAcquire()
        {
            var source = new FakeSource(delay: true);
            using var cache = new AssetCache<string, object>(source);
            using var cancellation = new CancellationTokenSource();

            Task<AssetLease<string, object>> cancelled = cache.AcquireAsync("a", cancellation.Token);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);

            source.Release();
            await Task.Delay(20);

            using AssetLease<string, object> retry = await cache.AcquireAsync("a");
            Assert.NotNull(retry.Asset);
        }

        private sealed class FakeSource : IAssetSource<string, object>
        {
            private readonly TaskCompletionSource<object?> _gate;
            private readonly bool _delay;
            private readonly bool _missing;
            private int _failures;

            public FakeSource(bool delay = false, bool missing = false, int failures = 0)
            {
                _delay = delay;
                _missing = missing;
                _failures = failures;
                _gate = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public int LoadCalls { get; private set; }

            public IReadOnlyCollection<object> Assets => Array.Empty<object>();

            public void Release()
            {
                _gate.TrySetResult(null);
            }

            public async Task<AssetLoadResult<object>> LoadAsync(
                string key,
                CancellationToken cancellationToken)
            {
                LoadCalls++;

                if (_delay)
                {
                    await _gate.Task.WaitAsync(cancellationToken);
                }

                if (_failures > 0)
                {
                    _failures--;
                    return AssetLoadResult<object>.Failed("failed");
                }

                if (_missing)
                {
                    return AssetLoadResult<object>.Missing("missing");
                }

                return AssetLoadResult<object>.Success(new Asset(key));
            }
        }

        private sealed class Asset
        {
            public Asset(string key)
            {
                Key = key;
            }

            public string Key { get; }
        }
    }
}
