using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameDevKit.UiStack
{
    public enum UiLayer
    {
        Transition = 0,
        Background = 1,
        Standard = 2,
        Overlay = 3,
        System = 4,
    }

    public enum UiPanelState
    {
        Hidden,
        Loading,
        Visible,
    }

    public interface IUiPanel
    {
        void Show();

        void Hide();

        void Dispose();
    }

    public interface IUiPanelLoader<TPanelId, TPanel>
        where TPanelId : notnull
        where TPanel : class, IUiPanel
    {
        Task<TPanel?> LoadAsync(TPanelId id, CancellationToken cancellationToken);
    }

    public sealed class UiPanelSpec<TPanelId>
        where TPanelId : notnull
    {
        public UiPanelSpec(TPanelId id, UiLayer layer, bool canCloseByBack = true)
        {
            Id = id;
            Layer = layer;
            CanCloseByBack = canCloseByBack;
        }

        public TPanelId Id { get; }

        public UiLayer Layer { get; }

        public bool CanCloseByBack { get; }
    }

    public sealed class UiPanelStack<TPanelId, TPanel> : IDisposable
        where TPanelId : notnull
        where TPanel : class, IUiPanel
    {
        private sealed class Entry
        {
            public Entry(UiPanelSpec<TPanelId> spec)
            {
                Spec = spec;
            }

            public UiPanelSpec<TPanelId> Spec { get; }

            public TPanel? Panel { get; set; }

            public UiPanelState State { get; set; }

            public bool CloseRequested { get; set; }

            public Task<bool>? LoadTask { get; set; }
        }

        private readonly object _gate = new();
        private readonly IUiPanelLoader<TPanelId, TPanel> _loader;
        private readonly Dictionary<TPanelId, Entry> _entries = new();
        private readonly Dictionary<UiLayer, List<TPanelId>> _stacks = new();
        private Action<TPanelId, UiPanelState>? _stateChanged;
        private bool _disposed;

        public UiPanelStack(
            IUiPanelLoader<TPanelId, TPanel> loader,
            System.Collections.Generic.IEnumerable<UiPanelSpec<TPanelId>> specs)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));

            if (specs == null)
            {
                throw new ArgumentNullException(nameof(specs));
            }

            foreach (UiLayer layer in Enum.GetValues(typeof(UiLayer)))
            {
                _stacks.Add(layer, new List<TPanelId>());
            }

            foreach (UiPanelSpec<TPanelId> spec in specs)
            {
                if (_entries.ContainsKey(spec.Id))
                {
                    throw new ArgumentException($"Duplicate UI panel spec: {spec.Id}", nameof(specs));
                }

                _entries.Add(spec.Id, new Entry(spec));
            }
        }

        public event Action<TPanelId, UiPanelState>? StateChanged
        {
            add => _stateChanged += value;
            remove => _stateChanged -= value;
        }

        public Task<bool> OpenAsync(
            TPanelId id,
            CancellationToken cancellationToken = default)
        {
            Task<bool> operation;
            bool immediateResult = false;

            lock (_gate)
            {
                ThrowIfDisposed();

                if (!_entries.TryGetValue(id, out Entry? entry))
                {
                    throw new ArgumentException($"UI panel is not registered: {id}", nameof(id));
                }

                if (entry.State == UiPanelState.Visible)
                {
                    return Task.FromResult(true);
                }

                if (entry.State == UiPanelState.Hidden && entry.Panel != null)
                {
                    entry.Panel.Show();
                    entry.State = UiPanelState.Visible;
                    _stacks[entry.Spec.Layer].Add(id);
                    immediateResult = true;
                    operation = Task.FromResult(true);
                }
                else if (entry.State == UiPanelState.Loading && entry.LoadTask != null)
                {
                    operation = entry.LoadTask;
                }
                else
                {
                    entry.State = UiPanelState.Loading;
                    entry.CloseRequested = false;
                    entry.LoadTask = LoadAndShowAsync(id, entry);
                    operation = entry.LoadTask;
                }
            }

            if (immediateResult)
            {
                _stateChanged?.Invoke(id, UiPanelState.Visible);
            }

            return WaitWithCancellation(operation, cancellationToken);
        }

        public Task<bool> CloseAsync(TPanelId id)
        {
            lock (_gate)
            {
                ThrowIfDisposed();

                if (!_entries.TryGetValue(id, out Entry? entry))
                {
                    return Task.FromResult(false);
                }

                if (entry.State == UiPanelState.Loading)
                {
                    entry.CloseRequested = true;
                    return Task.FromResult(true);
                }

                return Task.FromResult(CloseVisible(id, entry));
            }
        }

        public Task<bool> CloseTopmostAsync()
        {
            lock (_gate)
            {
                ThrowIfDisposed();

                UiLayer[] layers =
                {
                    UiLayer.System,
                    UiLayer.Overlay,
                    UiLayer.Standard,
                    UiLayer.Background,
                    UiLayer.Transition,
                };

                for (int i = 0; i < layers.Length; i++)
                {
                    List<TPanelId> stack = _stacks[layers[i]];

                    if (stack.Count == 0)
                    {
                        continue;
                    }

                    TPanelId id = stack[stack.Count - 1];
                    Entry entry = _entries[id];

                    if (!entry.Spec.CanCloseByBack)
                    {
                        return Task.FromResult(false);
                    }

                    return Task.FromResult(CloseVisible(id, entry));
                }

                return Task.FromResult(false);
            }
        }

        public Task CloseAllAsync()
        {
            lock (_gate)
            {
                ThrowIfDisposed();

                UiLayer[] layers =
                {
                    UiLayer.System,
                    UiLayer.Overlay,
                    UiLayer.Standard,
                    UiLayer.Background,
                    UiLayer.Transition,
                };

                for (int l = 0; l < layers.Length; l++)
                {
                    List<TPanelId> stack = _stacks[layers[l]];

                    for (int i = stack.Count - 1; i >= 0; i--)
                    {
                        TPanelId id = stack[i];
                        CloseVisible(id, _entries[id]);
                    }
                }

                return Task.CompletedTask;
            }
        }

        public bool IsOpen(TPanelId id)
        {
            lock (_gate)
            {
                return _entries.TryGetValue(id, out Entry? entry)
                    && entry.State == UiPanelState.Visible;
            }
        }

        public void Dispose()
        {
            List<TPanel> panels = new();

            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                foreach (Entry entry in _entries.Values)
                {
                    if (entry.State == UiPanelState.Loading)
                    {
                        entry.CloseRequested = true;
                    }

                    if (entry.Panel != null)
                    {
                        panels.Add(entry.Panel);
                        entry.Panel = null;
                    }

                    entry.State = UiPanelState.Hidden;
                }

                foreach (List<TPanelId> stack in _stacks.Values)
                {
                    stack.Clear();
                }
            }

            for (int i = 0; i < panels.Count; i++)
            {
                panels[i].Dispose();
            }
        }

        private async Task<bool> LoadAndShowAsync(TPanelId id, Entry entry)
        {
            TPanel? panel = null;

            try
            {
                panel = await _loader.LoadAsync(id, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                panel = null;
            }

            lock (_gate)
            {
                entry.LoadTask = null;

                if (_disposed || entry.CloseRequested)
                {
                    panel?.Dispose();
                    entry.Panel = null;
                    entry.State = UiPanelState.Hidden;
                    entry.CloseRequested = false;
                    _stateChanged?.Invoke(id, UiPanelState.Hidden);
                    return false;
                }

                if (panel == null)
                {
                    entry.Panel = null;
                    entry.State = UiPanelState.Hidden;
                    _stateChanged?.Invoke(id, UiPanelState.Hidden);
                    return false;
                }

                entry.Panel = panel;
                entry.State = UiPanelState.Visible;
                _stacks[entry.Spec.Layer].Add(id);
            }

            panel.Show();
            _stateChanged?.Invoke(id, UiPanelState.Visible);
            return true;
        }

        private bool CloseVisible(TPanelId id, Entry entry)
        {
            if (entry.State != UiPanelState.Visible || entry.Panel == null)
            {
                return false;
            }

            entry.Panel.Hide();
            entry.State = UiPanelState.Hidden;
            _stacks[entry.Spec.Layer].Remove(id);
            _stateChanged?.Invoke(id, UiPanelState.Hidden);
            return true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private static async Task<bool> WaitWithCancellation(
            Task<bool> task,
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
    }
}
