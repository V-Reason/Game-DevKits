using System;
using System.IO;
using Xunit;

namespace GameDevKit.SnapshotSave.Tests
{
    public sealed class SnapshotSaveTests
    {
        [Fact]
        public void SnapshotRoundTripsThroughAtomicStore()
        {
            using var directory = new TemporaryDirectory();
            var store = new AtomicFileSnapshotStore(directory.Path);
            var service = new SnapshotService<string>(store, new Utf8StringCodec(), 3);

            Assert.True(service.TrySave("slot-1", "hello", out string? saveError), saveError);
            Assert.True(service.TryLoad("slot-1", out string? state, out string? loadError), loadError);
            Assert.Equal("hello", state);
        }

        [Fact]
        public void SchemaMismatchIsRejectedBeforeDecode()
        {
            using var directory = new TemporaryDirectory();
            var store = new AtomicFileSnapshotStore(directory.Path);
            var codec = new Utf8StringCodec();
            var writer = new SnapshotService<string>(store, codec, 2);
            var reader = new SnapshotService<string>(store, codec, 1);

            Assert.True(writer.TrySave("slot", "hello", out _));
            Assert.False(reader.TryLoad("slot", out _, out string? error));
            Assert.Contains("schema", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CorruptPayloadFailsWithoutReturningState()
        {
            using var directory = new TemporaryDirectory();
            var store = new AtomicFileSnapshotStore(directory.Path);
            var service = new SnapshotService<string>(store, new Utf8StringCodec(), 1);

            Assert.True(store.TryWrite("slot", new byte[] { 1, 0, 0, 0, 0xff }, out string? writeError), writeError);
            Assert.False(service.TryLoad("slot", out string? state, out string? error));
            Assert.Null(state);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        [Fact]
        public void OverwriteKeepsOnlyFinalFileAndLeavesNoTemporaryFiles()
        {
            using var directory = new TemporaryDirectory();
            var store = new AtomicFileSnapshotStore(directory.Path);
            var service = new SnapshotService<string>(store, new Utf8StringCodec(), 1);

            Assert.True(service.TrySave("slot", "first", out _));
            Assert.True(service.TrySave("slot", "second", out _));
            Assert.True(service.TryLoad("slot", out string? state, out _));

            Assert.Equal("second", state);
            string[] files = Directory.GetFiles(directory.Path);
            Assert.Single(files);
            Assert.EndsWith(".snap", files[0], StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("../escape")]
        [InlineData("..\\escape")]
        [InlineData(".")]
        [InlineData("..")]
        [InlineData("")]
        public void UnsafeSlotNamesAreRejected(string slot)
        {
            using var directory = new TemporaryDirectory();
            var store = new AtomicFileSnapshotStore(directory.Path);

            Assert.False(store.TryWrite(slot, new byte[] { 1 }, out string? error));
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        [Fact]
        public void MissingSnapshotReturnsFailure()
        {
            using var directory = new TemporaryDirectory();
            var store = new AtomicFileSnapshotStore(directory.Path);
            var service = new SnapshotService<string>(store, new Utf8StringCodec(), 1);

            Assert.False(service.TryLoad("missing", out string? state, out string? error));
            Assert.Null(state);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            public TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "gamedevkit-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public void Dispose()
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, true);
                }
            }
        }
    }
}
