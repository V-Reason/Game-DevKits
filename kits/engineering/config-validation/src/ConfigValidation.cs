using System;
using System.Collections.Generic;
using System.Linq;

namespace GameDevKit.ConfigValidation
{
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
    }

    public readonly struct ConfigValidationIssue
    {
        public ConfigValidationIssue(ValidationSeverity severity, string path, string message)
        {
            Severity = severity;
            Path = path;
            Message = message;
        }

        public ValidationSeverity Severity { get; }

        public string Path { get; }

        public string Message { get; }
    }

    public sealed class ConfigValidationReport
    {
        private readonly List<ConfigValidationIssue> _issues;

        public ConfigValidationReport(IEnumerable<ConfigValidationIssue> issues)
        {
            _issues = issues?.ToList() ?? throw new ArgumentNullException(nameof(issues));
        }

        public IReadOnlyList<ConfigValidationIssue> Issues => _issues;

        public bool IsValid
        {
            get
            {
                for (int i = 0; i < _issues.Count; i++)
                {
                    if (_issues[i].Severity == ValidationSeverity.Error)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }

    public interface IConfigValidator<in TConfig>
    {
        IEnumerable<ConfigValidationIssue> Validate(TConfig config);
    }

    public sealed class ConfigValidationPipeline<TConfig>
    {
        private readonly List<IConfigValidator<TConfig>> _validators = new();

        public void Add(IConfigValidator<TConfig> validator)
        {
            _validators.Add(validator ?? throw new ArgumentNullException(nameof(validator)));
        }

        public ConfigValidationReport Validate(TConfig config)
        {
            var issues = new List<ConfigValidationIssue>();

            for (int i = 0; i < _validators.Count; i++)
            {
                try
                {
                    IEnumerable<ConfigValidationIssue>? validatorIssues = _validators[i].Validate(config);

                    if (validatorIssues == null)
                    {
                        throw new InvalidOperationException("Validator returned null.");
                    }

                    issues.AddRange(validatorIssues);
                }
                catch (Exception exception)
                {
                    issues.Add(new ConfigValidationIssue(
                        ValidationSeverity.Error,
                        $"validator[{i}]",
                        exception.Message));
                }
            }

            return new ConfigValidationReport(issues);
        }
    }

    public sealed class TableIndex<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _values = new();

        public void Add(TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_values.ContainsKey(key))
            {
                throw new ArgumentException($"Duplicate table key: {key}", nameof(key));
            }

            _values.Add(key, value);
        }

        public int Count => _values.Count;

        public bool TryGet(TKey key, out TValue value)
        {
            return _values.TryGetValue(key, out value!);
        }

        public TValue Get(TKey key)
        {
            if (_values.TryGetValue(key, out TValue? value))
            {
                return value;
            }

            throw new KeyNotFoundException($"Unknown table key: {key}");
        }
    }

    public sealed class ConfigManifest
    {
        private readonly string[] _requiredTables;

        public ConfigManifest(IEnumerable<string> requiredTables)
        {
            if (requiredTables == null)
            {
                throw new ArgumentNullException(nameof(requiredTables));
            }

            _requiredTables = requiredTables
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        public ConfigValidationReport ValidatePresence(IEnumerable<string> availableTables)
        {
            if (availableTables == null)
            {
                throw new ArgumentNullException(nameof(availableTables));
            }

            var available = new HashSet<string>(availableTables, StringComparer.Ordinal);
            var issues = new List<ConfigValidationIssue>();

            for (int i = 0; i < _requiredTables.Length; i++)
            {
                string name = _requiredTables[i];

                if (available.Contains(name))
                {
                    continue;
                }

                issues.Add(new ConfigValidationIssue(
                    ValidationSeverity.Error,
                    name,
                    $"Required table '{name}' is missing."));
            }

            return new ConfigValidationReport(issues);
        }
    }

    public interface IValueAdapter<in TSource, out TView>
    {
        TView Adapt(TSource source);
    }

    public sealed class DelegateValueAdapter<TSource, TView> : IValueAdapter<TSource, TView>
    {
        private readonly Func<TSource, TView> _adapt;

        public DelegateValueAdapter(Func<TSource, TView> adapt)
        {
            _adapt = adapt ?? throw new ArgumentNullException(nameof(adapt));
        }

        public TView Adapt(TSource source)
        {
            return _adapt(source);
        }
    }
}
