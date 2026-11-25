using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GameDevKit.ObjectPool
{
    public enum PoolOverflowPolicy
    {
        Grow,
        GrowUpToLimit,
        Drop,
        Throw,
    }

    public sealed class PoolOptions<T>
        where T : class
    {
        public PoolOptions(Func<T> factory)
        {
            Factory = factory;
        }

        public Func<T> Factory { get; }

        public Action<T>? Reset { get; set; }

        public Action<T>? Destroy { get; set; }

        public int MaxRetained { get; set; } = 64;

        public int MaxTotal { get; set; } = 256;

        public PoolOverflowPolicy OverflowPolicy { get; set; } = PoolOverflowPolicy.Grow;
    }

    public readonly struct PoolStats
    {
        public PoolStats(int active, int idle, int totalCreated, int peakActive, int dropped)
        {
            Active = active;
            Idle = idle;
            TotalCreated = totalCreated;
            PeakActive = peakActive;
            Dropped = dropped;
        }

        public int Active { get; }

        public int Idle { get; }

        public int TotalCreated { get; }

        public int PeakActive { get; }

        public int Dropped { get; }
    }

    public sealed class ObjectPool<T> : IDisposable
        where T : class
    {
        private readonly Func<T> _factory;
        private readonly Action<T>? _reset;
        private readonly Action<T>? _destroy;
        private readonly int _maxRetained;
        private readonly int _maxTotal;
        private readonly PoolOverflowPolicy _overflowPolicy;
        private readonly Stack<T> _idle = new();
        private readonly HashSet<T> _active = new(ReferenceComparer.Instance);
        private int _totalCreated;
        private int _peakActive;
        private int _dropped;
        private bool _disposed;

        public ObjectPool(PoolOptions<T> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Factory == null)
            {
                throw new ArgumentException("Factory is required.", nameof(options));
            }

            if (options.MaxRetained <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "MaxRetained must be positive.");
            }

            if (options.MaxTotal < options.MaxRetained)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "MaxTotal must be at least MaxRetained.");
            }

            _factory = options.Factory;
            _reset = options.Reset;
            _destroy = options.Destroy;
            _maxRetained = options.MaxRetained;
            _maxTotal = options.MaxTotal;
            _overflowPolicy = options.OverflowPolicy;
        }

        public bool TryRent(out T? item)
        {
            ThrowIfDisposed();

            if (_idle.Count > 0)
            {
                item = _idle.Pop();
                MarkActive(item);
                return true;
            }

            switch (_overflowPolicy)
            {
                case PoolOverflowPolicy.Grow:
                    item = CreateAndRent();
                    return true;

                case PoolOverflowPolicy.GrowUpToLimit:
                    if (_totalCreated < _maxTotal)
                    {
                        item = CreateAndRent();
                        return true;
                    }

                    _dropped++;
                    item = null;
                    return false;

                case PoolOverflowPolicy.Drop:
                    _dropped++;
                    item = null;
                    return false;

                case PoolOverflowPolicy.Throw:
                    _dropped++;
                    throw new InvalidOperationException("Object pool is empty.");

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public T Rent()
        {
            if (!TryRent(out T? item))
            {
                throw new InvalidOperationException("Object pool is empty.");
            }

            return item!;
        }

        public void Return(T item)
        {
            ThrowIfDisposed();

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!_active.Remove(item))
            {
                throw new InvalidOperationException("Object was not rented from this pool or was already returned.");
            }

            _reset?.Invoke(item);

            if (_idle.Count < _maxRetained)
            {
                _idle.Push(item);
            }
            else
            {
                _destroy?.Invoke(item);
            }
        }

        public void Prewarm(int count)
        {
            ThrowIfDisposed();

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            int available = Math.Max(0, _maxRetained - _idle.Count);
            int create = Math.Min(count, available);

            if (_overflowPolicy == PoolOverflowPolicy.GrowUpToLimit)
            {
                create = Math.Min(create, Math.Max(0, _maxTotal - _totalCreated));
            }

            for (int i = 0; i < create; i++)
            {
                _idle.Push(Create());
            }
        }

        public void Clear()
        {
            while (_idle.Count > 0)
            {
                _destroy?.Invoke(_idle.Pop());
            }
        }

        public PoolStats GetStats()
        {
            return new PoolStats(
                _active.Count,
                _idle.Count,
                _totalCreated,
                _peakActive,
                _dropped);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Clear();
            _disposed = true;
        }

        private T CreateAndRent()
        {
            T item = Create();
            MarkActive(item);
            return item;
        }

        private T Create()
        {
            T item = _factory();

            if (item == null)
            {
                throw new InvalidOperationException("Pool factory returned null.");
            }

            _totalCreated++;
            return item;
        }

        private void MarkActive(T item)
        {
            if (!_active.Add(item))
            {
                throw new InvalidOperationException("Pool factory returned an object that is already active.");
            }

            if (_active.Count > _peakActive)
            {
                _peakActive = _active.Count;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<T>
        {
            public static readonly ReferenceComparer Instance = new();

            public bool Equals(T? x, T? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
