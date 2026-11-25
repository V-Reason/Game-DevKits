using System;
using System.Collections.Generic;

namespace GameDevKit.CompositionRoot
{
    public interface IRuntimeService
    {
        int Order { get; }

        void Start();

        void Tick(double deltaTime, double unscaledDeltaTime);

        void Stop();
    }

    public sealed class RuntimeBootstrap
    {
        private readonly List<IRuntimeService> _services = new();

        public bool IsRunning { get; private set; }

        public void Add(IRuntimeService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (IsRunning)
            {
                throw new InvalidOperationException("Services cannot be added after startup.");
            }

            for (int i = 0; i < _services.Count; i++)
            {
                if (_services[i].Order == service.Order)
                {
                    throw new ArgumentException(
                        $"A service with order {service.Order} is already registered.",
                        nameof(service));
                }
            }

            int index = 0;

            while (index < _services.Count && _services[index].Order < service.Order)
            {
                index++;
            }

            _services.Insert(index, service);
        }

        public void Start()
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Runtime is already running.");
            }

            int started = 0;

            try
            {
                for (; started < _services.Count; started++)
                {
                    _services[started].Start();
                }

                IsRunning = true;
            }
            catch
            {
                for (int i = started - 1; i >= 0; i--)
                {
                    _services[i].Stop();
                }

                throw;
            }
        }

        public void Tick(double deltaTime, double unscaledDeltaTime)
        {
            if (!IsRunning)
            {
                return;
            }

            for (int i = 0; i < _services.Count; i++)
            {
                _services[i].Tick(deltaTime, unscaledDeltaTime);
            }
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            for (int i = _services.Count - 1; i >= 0; i--)
            {
                _services[i].Stop();
            }

            IsRunning = false;
        }
    }
}
