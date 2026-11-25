using System;
using System.Collections.Generic;
using Xunit;

namespace GameDevKit.DiagnosticsSnapshot.Tests
{
    public sealed class DiagnosticsHubTests
    {
        [Fact]
        public void CaptureSortsSourcesAndMetrics()
        {
            var hub = new DiagnosticsHub();
            hub.Register(new Source("z", Metric("b", 2), Metric("a", 1)));
            hub.Register(new Source("a", Metric("z", 3)));

            DiagnosticsSnapshot snapshot = hub.Capture();

            Assert.Equal(new[] { "a/z", "z/a", "z/b" }, Names(snapshot));
        }

        [Fact]
        public void DuplicateSourceNameIsRejected()
        {
            var hub = new DiagnosticsHub();
            hub.Register(new Source("a"));

            Assert.Throws<ArgumentException>(() => hub.Register(new Source("a")));
        }

        [Fact]
        public void SourceFailureBecomesIssueWithoutStoppingOthers()
        {
            var hub = new DiagnosticsHub();
            hub.Register(new ThrowingSource("broken"));
            hub.Register(new Source("healthy", Metric("ok", 1)));

            DiagnosticsSnapshot snapshot = hub.Capture();

            Assert.Single(snapshot.Entries);
            Assert.Single(snapshot.Issues);
            Assert.Equal("broken", snapshot.Issues[0].Source);
        }

        [Fact]
        public void UnregisterRemovesFutureSource()
        {
            var hub = new DiagnosticsHub();
            hub.Register(new Source("a", Metric("value", 1)));

            Assert.True(hub.Unregister("a"));
            Assert.Empty(hub.Capture().Entries);
            Assert.False(hub.Unregister("a"));
        }

        [Fact]
        public void SnapshotIsCopyOfCapturedValues()
        {
            var source = new MutableSource();
            var hub = new DiagnosticsHub();
            hub.Register(source);

            DiagnosticsSnapshot snapshot = hub.Capture();
            source.Value = 99;

            Assert.True(snapshot.TryGetValue("mutable", "value", out double captured));
            Assert.Equal(1, captured);
        }

        private static string[] Names(DiagnosticsSnapshot snapshot)
        {
            var names = new string[snapshot.Entries.Count];

            for (int i = 0; i < names.Length; i++)
            {
                names[i] = snapshot.Entries[i].Source + "/" + snapshot.Entries[i].Name;
            }

            return names;
        }

        private static DiagnosticMetric Metric(string name, double value)
        {
            return new DiagnosticMetric(name, value, DiagnosticKind.Gauge);
        }

        private sealed class Source : IDiagnosticsSource
        {
            private readonly DiagnosticMetric[] _metrics;

            public Source(string name, params DiagnosticMetric[] metrics)
            {
                Name = name;
                _metrics = metrics;
            }

            public string Name { get; }

            public IEnumerable<DiagnosticMetric> Capture()
            {
                return _metrics;
            }
        }

        private sealed class ThrowingSource : IDiagnosticsSource
        {
            public ThrowingSource(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public IEnumerable<DiagnosticMetric> Capture()
            {
                throw new InvalidOperationException("capture failed");
            }
        }

        private sealed class MutableSource : IDiagnosticsSource
        {
            public string Name => "mutable";

            public double Value { get; set; } = 1;

            public IEnumerable<DiagnosticMetric> Capture()
            {
                yield return Metric("value", Value);
            }
        }
    }
}
