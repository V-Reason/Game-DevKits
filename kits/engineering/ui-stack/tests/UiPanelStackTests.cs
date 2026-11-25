using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GameDevKit.UiStack.Tests
{
    public sealed class UiPanelStackTests
    {
        [Fact]
        public async Task OpenLoadsShowsAndPushesPanel()
        {
            var loader = new FakeLoader();
            using var stack = CreateStack(loader, "main");

            Assert.True(await stack.OpenAsync("main"));

            Assert.True(stack.IsOpen("main"));
            Assert.Equal(1, loader.Calls);
            Assert.True(loader.Panels["main"].Shown);
        }

        [Fact]
        public async Task ConcurrentOpenSharesOneLoad()
        {
            var loader = new FakeLoader(delay: true);
            using var stack = CreateStack(loader, "main");

            Task<bool> first = stack.OpenAsync("main");
            Task<bool> second = stack.OpenAsync("main");
            loader.Release("main");

            Assert.True(await first);
            Assert.True(await second);
            Assert.Equal(1, loader.Calls);
        }

        [Fact]
        public async Task CloseDuringLoadDisposesWithoutShowing()
        {
            var loader = new FakeLoader(delay: true);
            using var stack = CreateStack(loader, "main");

            Task<bool> opening = stack.OpenAsync("main");
            Assert.True(await stack.CloseAsync("main"));
            loader.Release("main");

            Assert.False(await opening);
            Assert.False(stack.IsOpen("main"));
            Assert.True(loader.Panels["main"].Disposed);
            Assert.False(loader.Panels["main"].Shown);
        }

        [Fact]
        public async Task CloseTopmostUsesHighestLayer()
        {
            var loader = new FakeLoader();
            using var stack = CreateStack(
                loader,
                new UiPanelSpec<string>("standard", UiLayer.Standard),
                new UiPanelSpec<string>("overlay", UiLayer.Overlay));

            await stack.OpenAsync("standard");
            await stack.OpenAsync("overlay");

            Assert.True(await stack.CloseTopmostAsync());
            Assert.False(stack.IsOpen("overlay"));
            Assert.True(stack.IsOpen("standard"));
        }

        [Fact]
        public async Task NonCloseableTopPanelBlocksBack()
        {
            var loader = new FakeLoader();
            using var stack = CreateStack(
                loader,
                new UiPanelSpec<string>("modal", UiLayer.Overlay, canCloseByBack: false));
            await stack.OpenAsync("modal");

            Assert.False(await stack.CloseTopmostAsync());
            Assert.True(stack.IsOpen("modal"));
        }

        [Fact]
        public async Task LoadFailureCleansState()
        {
            var loader = new FakeLoader(fail: true);
            using var stack = CreateStack(loader, "broken");

            Assert.False(await stack.OpenAsync("broken"));
            Assert.False(stack.IsOpen("broken"));
        }

        [Fact]
        public async Task CloseAllHidesPanelsInLifoOrder()
        {
            var loader = new FakeLoader();
            using var stack = CreateStack(
                loader,
                new UiPanelSpec<string>("a", UiLayer.Standard),
                new UiPanelSpec<string>("b", UiLayer.Overlay));
            await stack.OpenAsync("a");
            await stack.OpenAsync("b");

            await stack.CloseAllAsync();

            Assert.False(stack.IsOpen("a"));
            Assert.False(stack.IsOpen("b"));
            Assert.Equal(new[] { "b.Hide", "a.Hide" }, loader.HideLog);
        }

        private static UiPanelStack<string, FakePanel> CreateStack(
            FakeLoader loader,
            params string[] ids)
        {
            var specs = new List<UiPanelSpec<string>>();

            foreach (string id in ids)
            {
                specs.Add(new UiPanelSpec<string>(id, UiLayer.Standard));
            }

            return new UiPanelStack<string, FakePanel>(loader, specs);
        }

        private static UiPanelStack<string, FakePanel> CreateStack(
            FakeLoader loader,
            params UiPanelSpec<string>[] specs)
        {
            return new UiPanelStack<string, FakePanel>(loader, specs);
        }

        private sealed class FakeLoader : IUiPanelLoader<string, FakePanel>
        {
            private readonly bool _delay;
            private readonly bool _fail;
            private readonly Dictionary<string, TaskCompletionSource<bool>> _gates = new();

            public FakeLoader(bool delay = false, bool fail = false)
            {
                _delay = delay;
                _fail = fail;
            }

            public int Calls { get; private set; }

            public Dictionary<string, FakePanel> Panels { get; } = new();

            public List<string> HideLog { get; } = new();

            public void Release(string id)
            {
                _gates[id].TrySetResult(true);
            }

            public async Task<FakePanel?> LoadAsync(
                string id,
                CancellationToken cancellationToken)
            {
                Calls++;

                if (_delay)
                {
                    var gate = new TaskCompletionSource<bool>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    _gates[id] = gate;
                    await gate.Task;
                }

                if (_fail)
                {
                    return null;
                }

                var panel = new FakePanel(id, HideLog);
                Panels[id] = panel;
                return panel;
            }
        }

        private sealed class FakePanel : IUiPanel
        {
            private readonly string _id;
            private readonly List<string> _hideLog;

            public FakePanel(string id, List<string> hideLog)
            {
                _id = id;
                _hideLog = hideLog;
            }

            public bool Shown { get; private set; }

            public bool Disposed { get; private set; }

            public void Show()
            {
                Shown = true;
            }

            public void Hide()
            {
                Shown = false;
                _hideLog.Add(_id + ".Hide");
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
