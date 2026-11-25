using System;
using System.Collections.Generic;
using Xunit;

namespace GameDevKit.ConfigValidation.Tests
{
    public sealed class ConfigValidationTests
    {
        [Fact]
        public void TableIndexRejectsDuplicateKeys()
        {
            var index = new TableIndex<int, string>();
            index.Add(1, "first");

            Assert.Throws<ArgumentException>(() => index.Add(1, "second"));
            Assert.Equal("first", index.Get(1));
        }

        [Fact]
        public void TableIndexReportsMissingKey()
        {
            var index = new TableIndex<string, int>();

            Assert.False(index.TryGet("missing", out _));
            Assert.Throws<KeyNotFoundException>(() => index.Get("missing"));
        }

        [Fact]
        public void PipelineCollectsAllValidatorIssues()
        {
            var pipeline = new ConfigValidationPipeline<int>();
            pipeline.Add(new DelegateValidator<int>(_ => new[]
            {
                new ConfigValidationIssue(ValidationSeverity.Warning, "a", "warning"),
            }));
            pipeline.Add(new DelegateValidator<int>(_ => new[]
            {
                new ConfigValidationIssue(ValidationSeverity.Error, "b", "error"),
            }));

            ConfigValidationReport report = pipeline.Validate(1);

            Assert.False(report.IsValid);
            Assert.Equal(2, report.Issues.Count);
        }

        [Fact]
        public void ValidatorExceptionBecomesErrorIssue()
        {
            var pipeline = new ConfigValidationPipeline<int>();
            pipeline.Add(new ThrowingValidator());

            ConfigValidationReport report = pipeline.Validate(1);

            Assert.False(report.IsValid);
            Assert.Contains("boom", report.Issues[0].Message);
            Assert.Equal(ValidationSeverity.Error, report.Issues[0].Severity);
        }

        [Fact]
        public void ManifestReportsEveryMissingTable()
        {
            var manifest = new ConfigManifest(new[] { "players", "enemies", "waves" });

            ConfigValidationReport report = manifest.ValidatePresence(new[] { "players" });

            Assert.False(report.IsValid);
            Assert.Equal(2, report.Issues.Count);
            Assert.Contains("enemies", report.Issues[0].Path);
            Assert.Contains("waves", report.Issues[1].Path);
        }

        [Fact]
        public void SpecAdapterReturnsStableView()
        {
            IValueAdapter<SourceRow, ViewRow> adapter =
                new DelegateValueAdapter<SourceRow, ViewRow>(source => new ViewRow(source.Id, source.Name));
            var source = new SourceRow { Id = 7, Name = "enemy", Hidden = 99 };

            ViewRow view = adapter.Adapt(source);

            Assert.Equal(7, view.Id);
            Assert.Equal("enemy", view.Name);
        }

        private sealed class DelegateValidator<T> : IConfigValidator<T>
        {
            private readonly Func<T, IEnumerable<ConfigValidationIssue>> _validate;

            public DelegateValidator(Func<T, IEnumerable<ConfigValidationIssue>> validate)
            {
                _validate = validate;
            }

            public IEnumerable<ConfigValidationIssue> Validate(T config)
            {
                return _validate(config);
            }
        }

        private sealed class ThrowingValidator : IConfigValidator<int>
        {
            public IEnumerable<ConfigValidationIssue> Validate(int config)
            {
                throw new InvalidOperationException("boom");
            }
        }

        private sealed class SourceRow
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public int Hidden { get; set; }
        }

        private readonly struct ViewRow
        {
            public ViewRow(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public int Id { get; }

            public string Name { get; }
        }

    }
}
