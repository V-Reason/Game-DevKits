using System;
using System.Collections.Generic;
using System.Numerics;

namespace GameDevKit.InputBuffer
{
    public readonly struct InputSnapshot
    {
        public InputSnapshot(Vector2 move)
        {
            Move = move;
        }

        public Vector2 Move { get; }

        public static InputSnapshot Empty => new(Vector2.Zero);
    }

    public readonly struct InputWindow
    {
        public InputWindow(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            Seconds = seconds;
        }

        public double Seconds { get; }

        public bool Contains(double pressedAt, double now)
        {
            return pressedAt <= now && pressedAt >= now - Seconds;
        }
    }

    public sealed class InputBuffer<TCommand>
        where TCommand : struct, Enum
    {
        private readonly Dictionary<TCommand, double> _pending = new();
        private double _lastSampleTime = double.NegativeInfinity;

        public InputSnapshot Latest { get; private set; }

        public void Push(TCommand command, double now)
        {
            EnsureMonotonic(now);
            _pending[command] = now;
        }

        public void PushSnapshot(InputSnapshot snapshot, double now)
        {
            EnsureMonotonic(now);
            Latest = snapshot;
        }

        public bool CanConsume(TCommand command, double now, double windowSeconds)
        {
            EnsureFinite(now);
            var window = new InputWindow(windowSeconds);

            return _pending.TryGetValue(command, out double pressedAt)
                && window.Contains(pressedAt, now);
        }

        public bool TryConsume(TCommand command, double now, double windowSeconds)
        {
            if (!CanConsume(command, now, windowSeconds))
            {
                return false;
            }

            return _pending.Remove(command);
        }

        public void Clear()
        {
            _pending.Clear();
            Latest = InputSnapshot.Empty;
            _lastSampleTime = double.NegativeInfinity;
        }

        private void EnsureMonotonic(double now)
        {
            EnsureFinite(now);

            if (now < _lastSampleTime)
            {
                throw new InvalidOperationException("Input timestamps must be monotonic.");
            }

            _lastSampleTime = now;
        }

        private static void EnsureFinite(double now)
        {
            if (double.IsNaN(now) || double.IsInfinity(now))
            {
                throw new ArgumentOutOfRangeException(nameof(now));
            }
        }
    }
}
