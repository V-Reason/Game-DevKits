using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameDevKit.AssetLifecycle
{
    public enum AssetLoadStatus
    {
        Success,
        Missing,
        Failed,
        Cancelled,
    }

    public sealed class AssetLoadResult<TAsset>
        where TAsset : class
    {
        private AssetLoadResult(AssetLoadStatus status, TAsset? asset, string? error)
        {
            Status = status;
            Asset = asset;
            Error = error;
        }

        public AssetLoadStatus Status { get; }

        public TAsset? Asset { get; }

        public string? Error { get; }

        public static AssetLoadResult<TAsset> Success(TAsset asset)
        {
            return new AssetLoadResult<TAsset>(AssetLoadStatus.Success, asset, null);
        }

        public static AssetLoadResult<TAsset> Missing(string error)
        {
            return new AssetLoadResult<TAsset>(AssetLoadStatus.Missing, null, error);
        }

        public static AssetLoadResult<TAsset> Failed(string error)
        {
            return new AssetLoadResult<TAsset>(AssetLoadStatus.Failed, null, error);
        }
    }

    public interface IAssetSource<TKey, TAsset>
        where TKey : notnull
        where TAsset : class
    {
        Task<AssetLoadResult<TAsset>> LoadAsync(TKey key, CancellationToken cancellationToken);
    }

    public interface IAssetClock
    {
        double NowSeconds { get; }
    }

    public sealed class ManualAssetClock : IAssetClock
    {
        public double NowSeconds { get; set; }
    }

    public sealed class AssetCacheOptions
    {
        public int MaxEntries { get; set; } = 64;

        public int MaxConcurrentLoads { get; set; } = 4;

        public int RetryCount { get; set; } = 1;

        public double CooldownSeconds { get; set; } = 2;

        public int MaxEvictionsPerMaintenance { get; set; } = 8;

        public Action<object>? Unload { get; set; }
    }

    public readonly struct AssetCacheStats
    {
        public AssetCacheStats(
            int entries,
            int activeLeases,
            int cacheHits,
            int cacheMisses,
            int loads,
            int failures,
            int evictions)
        {
            Entries = entries;
            ActiveLeases = activeLeases;
            CacheHits = cacheHits;
            CacheMisses = cacheMisses;
            Loads = loads;
            Failures = failures;
            Evictions = evictions;
        }

        public int Entries { get; }

        public int ActiveLeases { get; }

        public int CacheHits { get; }

        public int CacheMisses { get; }

        public int Loads { get; }

        public int Failures { get; }

        public int Evictions { get; }
    }

    public sealed class AssetLoadException : Exception
    {
        public AssetLoadException(string message)
            : base(message)
        {
        }
    }

    public sealed class AssetLease<TKey, TAsset> : IDisposable
        where TKey : notnull
        where TAsset : class
    {
        private Action<TKey>? _release;

        internal AssetLease(TKey key, TAsset asset, Action<TKey> release)
        {
            Key = key;
            Asset = asset;
            _release = release;
        }

        public TKey Key { get; }

        public TAsset Asset { get; }

        public void Dispose()
        {
            Action<TKey>? release = _release;
            _release = null;
            release?.Invoke(Key);
        }
    }

    public sealed class AssetCache<TKey, TAsset> : IDisposable
        where TKey : notnull
        where TAsset : class
    {
        private sealed class Entry
        {
            public Entry(TAsset asset, double now)
            {
                Asset = asset;
                LastAccess = now;
            }

            public TAsset Asset { get; }

            public int RefCount { get; set; }

            public double LastAccess { get; set; }

            public double EvictAfter { get; set; }

            public bool IsPinned { get; set; }
        }

        private readonly object _gate = new();
        private readonly IAssetSource<TKey, TAsset> _source;
        private readonly AssetCacheOptions _options;
        private readonly IAssetClock _clock;
        private readonly Dictionary<TKey, Entry> _entries = new();
        private readonly Dictionary<TKey, Task<TAsset>> _pending = new();
        private readonly Dictionary<TKey, TAsset> _fallbacks = new();
        private readonly SemaphoreSlim _loadSlots;
        private int _activeLeases;
        private int _hits;
        private int _misses;
        private int _loads;
        private int _failures;
        private int _evictions;
        private bool _disposed;

        public AssetCache(
            IAssetSource<TKey, TAsset> source,
            AssetCacheOptions? options = null,
            IAssetClock? clock = null)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _options = options ?? new AssetCacheOptions();
            _clock = clock ?? new StopwatchAssetClock();

            if (_options.MaxEntries <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "MaxEntries must be positive.");
            }

            if (_options.MaxConcurrentLoads <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "MaxConcurrentLoads must be positive.");
            }

            if (_options.RetryCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "RetryCount cannot be negative.");
            }

            if (_options.CooldownSeconds < 0
                || double.IsNaN(_options.CooldownSeconds)
                || double.IsInfinity(_options.CooldownSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(options), "CooldownSeconds must be finite and non-negative.");
            }

            if (_options.MaxEvictionsPerMaintenance <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    "MaxEvictionsPerMaintenance must be positive.");
            }

            _loadSlots = new SemaphoreSlim(
                _options.MaxConcurrentLoads,
                _options.MaxConcurrentLoads);
        }

        public Task<AssetLease<TKey, TAsset>> AcquireAsync(
            TKey key,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            Task<TAsset> pending;

            lock (_gate)
            {
                if (_entries.TryGetValue(key, out Entry? entry))
                {
                    entry.RefCount++;
                    entry.LastAccess = _clock.NowSeconds;
                    _activeLeases++;
                    _hits++;
                    return Task.FromResult(CreateLease(key, entry.Asset));
                }

                _misses++;
                pending = GetOrStartLoad(key);
            }

            return AcquirePendingAsync(key, pending, cancellationToken);
        }

        public void RegisterFallback(TKey key, TAsset asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            lock (_gate)
            {
                ThrowIfDisposed();
                _fallbacks[key] = asset;
            }
        }

        public async Task PreloadAsync(TKey key, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            Task<TAsset> pending;

            lock (_gate)
            {
                if (_entries.TryGetValue(key, out Entry? entry))
                {
                    entry.IsPinned = true;
                    entry.EvictAfter = 0;
                    return;
                }

                pending = GetOrStartLoad(key);
            }

            await WaitWithCancellation(pending, cancellationToken).ConfigureAwait(false);

            lock (_gate)
            {
                if (_entries.TryGetValue(key, out Entry? entry))
                {
                    entry.IsPinned = true;
                    entry.EvictAfter = 0;
                }
            }
        }

        public void Maintain()
        {
            List<Entry> unload;

            lock (_gate)
            {
                ThrowIfDisposed();
                int overflow = _entries.Count - _options.MaxEntries;

                if (overflow <= 0)
                {
                    return;
                }

                double now = _clock.NowSeconds;

                List<KeyValuePair<TKey, Entry>> candidates = _entries
                    .Where(pair => pair.Value.RefCount == 0
                        && !pair.Value.IsPinned
                        && pair.Value.EvictAfter <= now)
                    .OrderBy(pair => pair.Value.LastAccess)
                    .Take(Math.Min(overflow, _options.MaxEvictionsPerMaintenance))
                    .ToList();

                if (candidates.Count == 0)
                {
                    return;
                }

                unload = new List<Entry>(candidates.Count);

                for (int i = 0; i < candidates.Count; i++)
                {
                    _entries.Remove(candidates[i].Key);
                    unload.Add(candidates[i].Value);
                    _evictions++;
                }
            }

            for (int i = 0; i < unload.Count; i++)
            {
                _options.Unload?.Invoke(unload[i].Asset);
            }
        }

        public AssetCacheStats GetStats()
        {
            lock (_gate)
            {
                return new AssetCacheStats(
                    _entries.Count,
                    _activeLeases,
                    _hits,
                    _misses,
                    _loads,
                    _failures,
                    _evictions);
            }
        }

        public void Dispose()
        {
            List<TAsset> unload;

            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                unload = _entries.Values.Select(entry => entry.Asset).ToList();
                _entries.Clear();
                _pending.Clear();
                _fallbacks.Clear();
            }

            for (int i = 0; i < unload.Count; i++)
            {
                _options.Unload?.Invoke(unload[i]);
            }

            _loadSlots.Dispose();
        }

        private async Task<AssetLease<TKey, TAsset>> AcquirePendingAsync(
            TKey key,
            Task<TAsset> pending,
            CancellationToken cancellationToken)
        {
            TAsset asset = await WaitWithCancellation(pending, cancellationToken).ConfigureAwait(false);

            lock (_gate)
            {
                ThrowIfDisposed();

                if (!_entries.TryGetValue(key, out Entry? entry))
                {
                    throw new AssetLoadException("Asset was loaded but not cached.");
                }

                entry.RefCount++;
                entry.LastAccess = _clock.NowSeconds;
                _activeLeases++;
                return CreateLease(key, asset);
            }
        }

        private Task<TAsset> GetOrStartLoad(TKey key)
        {
            if (_pending.TryGetValue(key, out Task<TAsset>? task))
            {
                return task;
            }

            task = LoadAndStoreAsync(key);
            _pending[key] = task;
            return task;
        }

        private async Task<TAsset> LoadAndStoreAsync(TKey key)
        {
            await Task.Yield();
            await _loadSlots.WaitAsync().ConfigureAwait(false);

            try
            {
                string? lastError = null;

                for (int attempt = 0; attempt <= _options.RetryCount; attempt++)
                {
                    AssetLoadResult<TAsset> result = await _source
                        .LoadAsync(key, CancellationToken.None)
                        .ConfigureAwait(false);

                    if (result.Status == AssetLoadStatus.Success && result.Asset != null)
                    {
                        lock (_gate)
                        {
                            if (_disposed)
                            {
                                throw new ObjectDisposedException(GetType().Name);
                            }

                            _entries[key] = new Entry(result.Asset, _clock.NowSeconds);
                            _loads++;
                        }

                        return result.Asset;
                    }

                    lastError = result.Error ?? result.Status.ToString();
                }

                lock (_gate)
                {
                    if (_fallbacks.TryGetValue(key, out TAsset? fallback))
                    {
                        _entries[key] = new Entry(fallback, _clock.NowSeconds);
                        _failures++;
                        return fallback;
                    }

                    _failures++;
                }

                throw new AssetLoadException($"Failed to load asset '{key}': {lastError}");
            }
            finally
            {
                lock (_gate)
                {
                    _pending.Remove(key);
                }

                _loadSlots.Release();
            }
        }

        private AssetLease<TKey, TAsset> CreateLease(TKey key, TAsset asset)
        {
            return new AssetLease<TKey, TAsset>(key, asset, Release);
        }

        private void Release(TKey key)
        {
            lock (_gate)
            {
                if (!_entries.TryGetValue(key, out Entry? entry))
                {
                    return;
                }

                if (entry.RefCount <= 0)
                {
                    throw new InvalidOperationException("Asset lease released more than once.");
                }

                entry.RefCount--;
                _activeLeases--;

                if (entry.RefCount == 0)
                {
                    entry.EvictAfter = _clock.NowSeconds + _options.CooldownSeconds;
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private static async Task<T> WaitWithCancellation<T>(
            Task<T> task,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return await task.ConfigureAwait(false);
            }

            var cancelled = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            using (cancellationToken.Register(() => cancelled.TrySetResult(true)))
            {
                Task completed = await Task.WhenAny(task, cancelled.Task).ConfigureAwait(false);

                if (completed != task)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task.ConfigureAwait(false);
        }

        private sealed class StopwatchAssetClock : IAssetClock
        {
            private readonly System.Diagnostics.Stopwatch _stopwatch = System.Diagnostics.Stopwatch.StartNew();

            public double NowSeconds => _stopwatch.Elapsed.TotalSeconds;
        }
    }
}
