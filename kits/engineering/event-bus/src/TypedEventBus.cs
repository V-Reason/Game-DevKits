using System;

namespace GameDevKit.EventBus
{
    public sealed class EventSubscription : IDisposable
    {
        private Action? _dispose;

        internal EventSubscription(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            Action? dispose = _dispose;
            _dispose = null;
            dispose?.Invoke();
        }
    }

    public sealed class TypedEventBus<TEvent>
        where TEvent : struct
    {
        private const int InitialCapacity = 4;

        private Action<TEvent>[] _handlers = new Action<TEvent>[InitialCapacity];
        private Action<TEvent>[] _snapshot = new Action<TEvent>[InitialCapacity];
        private readonly Action<Exception>? _onHandlerException;
        private int _count;
        private int _publishDepth;

        public TypedEventBus(Action<Exception>? onHandlerException = null)
        {
            _onHandlerException = onHandlerException;
        }

        public int HandlerCount => _count;

        public IDisposable Subscribe(Action<TEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (_count == _handlers.Length)
            {
                Array.Resize(ref _handlers, _handlers.Length * 2);
            }

            _handlers[_count++] = handler;
            return new EventSubscription(() => Unsubscribe(handler));
        }

        public void Publish(in TEvent evt)
        {
            int count = _count;

            if (count == 0)
            {
                return;
            }

            bool reuseSnapshot = _publishDepth == 0;
            Action<TEvent>[] snapshot;

            if (reuseSnapshot)
            {
                if (_snapshot.Length < count)
                {
                    _snapshot = new Action<TEvent>[count];
                }

                snapshot = _snapshot;
                _publishDepth = 1;
            }
            else
            {
                snapshot = new Action<TEvent>[count];
            }

            Array.Copy(_handlers, snapshot, count);

            try
            {
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        snapshot[i](evt);
                    }
                    catch (Exception exception)
                    {
                        _onHandlerException?.Invoke(exception);
                    }
                }
            }
            finally
            {
                if (reuseSnapshot)
                {
                    _publishDepth = 0;
                }
            }
        }

        public void Clear()
        {
            Array.Clear(_handlers, 0, _count);
            _count = 0;
        }

        private void Unsubscribe(Action<TEvent> handler)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_handlers[i] != handler)
                {
                    continue;
                }

                int last = _count - 1;
                _handlers[i] = _handlers[last];
                _handlers[last] = null!;
                _count = last;
                return;
            }
        }
    }
}
