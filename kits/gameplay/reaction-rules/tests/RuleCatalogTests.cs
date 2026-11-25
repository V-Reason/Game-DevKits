using System;
using System.Collections.Generic;
using Xunit;

namespace GameDevKit.ReactionRules.Tests
{
    public sealed class RuleCatalogTests
    {
        [Fact]
        public void ExactCompositeKeyReturnsConfiguredResult()
        {
            var catalog = new RuleCatalog<string, string, string>(
                new[]
                {
                    new RuleEntry<string, string, string>("water", "fire", "steam"),
                    new RuleEntry<string, string, string>("earth", "fire", "brick"),
                });

            Assert.Equal(2, catalog.Count);
            Assert.True(catalog.TryGet("water", "fire", out string result));
            Assert.Equal("steam", result);
            Assert.False(catalog.TryGet("fire", "water", out _));
        }

        [Fact]
        public void MissingRuleUsesExplicitFallback()
        {
            var catalog = new RuleCatalog<string, string, string>(
                Array.Empty<RuleEntry<string, string, string>>(),
                (left, right) => $"{left}+{right}");

            Assert.Equal("water+sand", catalog.Resolve("water", "sand"));
        }

        [Fact]
        public void MissingRuleWithoutFallbackThrows()
        {
            var catalog = new RuleCatalog<string, string, string>(
                Array.Empty<RuleEntry<string, string, string>>());

            Assert.Throws<KeyNotFoundException>(() => catalog.Resolve("water", "sand"));
        }

        [Fact]
        public void DuplicateCompositeKeyFailsAtInitialization()
        {
            var rules = new[]
            {
                new RuleEntry<string, string, string>("water", "fire", "steam"),
                new RuleEntry<string, string, string>("water", "fire", "overwrite"),
            };

            Assert.Throws<ArgumentException>(
                () => new RuleCatalog<string, string, string>(rules));
        }
    }
}
