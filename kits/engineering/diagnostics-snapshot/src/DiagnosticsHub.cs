using System;
using System.Collections.Generic;
using System.Linq;

namespace GameDevKit.DiagnosticsSnapshot
{
    public enum DiagnosticKind
    {
        Counter,
        Gauge,
    }

    public readonly struct DiagnosticMetric
    {
        public DiagnosticMetric(string name, double value, DiagnosticKind kind)
        {
            Name = name;
            Value = value;
            Kind = kind;
        }

        public string Name { get; }

        public double Value { get; }

        public DiagnosticKind Kind { get; }
    }

    public interface IDiagnosticsSource
    {
        string Name { get; }

        IEnumerable<DiagnosticMetric> Capture();
    }

    public readonly struct DiagnosticEntry
    {
        public DiagnosticEntry(string source, in DiagnosticMetric metric)
        {
            Source = source;
            Name = metric.Name;
            Value = metric.Value;
            Kind = metric.Kind;
        }

        public string Source { get; }

        public string Name { get; }

        public double Value { get; }

        public DiagnosticKind Kind { get; }
    }

    public readonly struct DiagnosticsIssue
    {
        public DiagnosticsIssue(string source, string message)
        {
            Source = source;
            Message = message;
        }

        public string Source { get; }

        public string Message { get; }
    }

    public sealed class DiagnosticsSnapshot
    {
        private readonly List<DiagnosticEntry> _entries;
        private readonly List<DiagnosticsIssue> _issues;

        public DiagnosticsSnapshot(
            IEnumerable<DiagnosticEntry> entries,
            IEnumerable<DiagnosticsIssue> issues)
        {
            _entries = entries?.ToList() ?? throw new ArgumentNullException(nameof(entries));
            _issues = issues?.ToList() ?? throw new ArgumentNullException(nameof(issues));
        }

        public IReadOnlyList<DiagnosticEntry> Entries => _entries;

        public IReadOnlyList<DiagnosticsIssue> Issues => _issues;

        public bool TryGetValue(string source, string name, out double value)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                DiagnosticEntry entry = _entries[i];

                if (string.Equals(entry.Source, source, StringComparison.Ordinal)
                    && string.Equals(entry.Name, name, StringComparison.Ordinal))
                {
                    value = entry.Value;
                    return true;
                }
            }

            value = 0;
            return false;
        }
    }

    public sealed class DiagnosticsHub
    {
        private readonly SortedDictionary<string, IDiagnosticsSource> _sources =
            new(StringComparer.Ordinal);

        public void Register(IDiagnosticsSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (string.IsNullOrWhiteSpace(source.Name))
            {
                throw new ArgumentException("Diagnostics source name is required.", nameof(source));
            }

            if (_sources.ContainsKey(source.Name))
            {
                throw new ArgumentException(
                    $"Diagnostics source '{source.Name}' is already registered.",
                    nameof(source));
            }

            _sources.Add(source.Name, source);
        }

        public bool Unregister(string sourceName)
        {
            return sourceName != null && _sources.Remove(sourceName);
        }

        public DiagnosticsSnapshot Capture()
        {
            var entries = new List<DiagnosticEntry>();
            var issues = new List<DiagnosticsIssue>();

            foreach (KeyValuePair<string, IDiagnosticsSource> pair in _sources)
            {
                try
                {
                    IEnumerable<DiagnosticMetric>? metrics = pair.Value.Capture();

                    if (metrics == null)
                    {
                        throw new InvalidOperationException("Capture returned null.");
                    }

                    var captured = metrics
                        .OrderBy(metric => metric.Name, StringComparer.Ordinal)
                        .ToArray();

                    for (int i = 0; i < captured.Length; i++)
                    {
                        entries.Add(new DiagnosticEntry(pair.Key, in captured[i]));
                    }
                }
                catch (Exception exception)
                {
                    issues.Add(new DiagnosticsIssue(pair.Key, exception.Message));
                }
            }

            return new DiagnosticsSnapshot(entries, issues);
        }
    }
}
