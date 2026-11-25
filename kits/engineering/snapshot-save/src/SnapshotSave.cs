using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security;
using System.Text;

namespace GameDevKit.SnapshotSave
{
    public interface ISnapshotCodec<T>
        where T : class
    {
        byte[] Encode(T state);

        bool TryDecode(
            ReadOnlySpan<byte> payload,
            [NotNullWhen(true)] out T? state,
            [NotNullWhen(false)] out string? error);
    }

    public interface ISnapshotStore
    {
        bool TryRead(
            string slot,
            [NotNullWhen(true)] out byte[]? data,
            [NotNullWhen(false)] out string? error);

        bool TryWrite(
            string slot,
            ReadOnlySpan<byte> data,
            [NotNullWhen(false)] out string? error);

        bool TryDelete(string slot, [NotNullWhen(false)] out string? error);
    }

    public sealed class Utf8StringCodec : ISnapshotCodec<string>
    {
        private static readonly UTF8Encoding StrictUtf8 = new(false, true);

        public byte[] Encode(string state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return StrictUtf8.GetBytes(state);
        }

        public bool TryDecode(
            ReadOnlySpan<byte> payload,
            [NotNullWhen(true)] out string? state,
            [NotNullWhen(false)] out string? error)
        {
            try
            {
                state = StrictUtf8.GetString(payload);
                error = null;
                return true;
            }
            catch (DecoderFallbackException exception)
            {
                state = null;
                error = exception.Message;
                return false;
            }
        }
    }

    public sealed class AtomicFileSnapshotStore : ISnapshotStore
    {
        private const string Extension = ".snap";
        private readonly string _rootDirectory;

        public AtomicFileSnapshotStore(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("Root directory is required.", nameof(rootDirectory));
            }

            _rootDirectory = Path.GetFullPath(rootDirectory);
            Directory.CreateDirectory(_rootDirectory);
        }

        public bool TryRead(
            string slot,
            [NotNullWhen(true)] out byte[]? data,
            [NotNullWhen(false)] out string? error)
        {
            data = null;

            if (!TryGetPath(slot, out string? path, out error))
            {
                return false;
            }

            try
            {
                data = File.ReadAllBytes(path);
                error = null;
                return true;
            }
            catch (FileNotFoundException)
            {
                error = $"Snapshot slot '{slot}' does not exist.";
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                error = $"Snapshot slot '{slot}' does not exist.";
                return false;
            }
            catch (Exception exception) when (IsRecoverableIoError(exception))
            {
                error = exception.Message;
                return false;
            }
        }

        public bool TryWrite(
            string slot,
            ReadOnlySpan<byte> data,
            [NotNullWhen(false)] out string? error)
        {
            if (!TryGetPath(slot, out string? path, out error))
            {
                return false;
            }

            string temporaryPath = path + ".tmp-" + Guid.NewGuid().ToString("N");

            try
            {
                Directory.CreateDirectory(_rootDirectory);
                File.WriteAllBytes(temporaryPath, data.ToArray());

                if (File.Exists(path))
                {
                    File.Replace(temporaryPath, path, null);
                }
                else
                {
                    File.Move(temporaryPath, path);
                }

                error = null;
                return true;
            }
            catch (Exception exception) when (IsRecoverableIoError(exception))
            {
                TryDeleteTemporary(temporaryPath);
                error = exception.Message;
                return false;
            }
        }

        public bool TryDelete(string slot, [NotNullWhen(false)] out string? error)
        {
            if (!TryGetPath(slot, out string? path, out error))
            {
                return false;
            }

            try
            {
                File.Delete(path);
                error = null;
                return true;
            }
            catch (Exception exception) when (IsRecoverableIoError(exception))
            {
                error = exception.Message;
                return false;
            }
        }

        private bool TryGetPath(
            string slot,
            [NotNullWhen(true)] out string? path,
            [NotNullWhen(false)] out string? error)
        {
            path = null;

            if (!IsSafeSlot(slot))
            {
                error = "Snapshot slot must contain only letters, digits, '.', '_' or '-'.";
                return false;
            }

            path = Path.Combine(_rootDirectory, slot + Extension);
            error = null;
            return true;
        }

        private static bool IsSafeSlot(string? slot)
        {
            if (string.IsNullOrEmpty(slot) || slot == "." || slot == "..")
            {
                return false;
            }

            for (int i = 0; i < slot.Length; i++)
            {
                char character = slot[i];

                if (!char.IsLetterOrDigit(character)
                    && character != '.'
                    && character != '_'
                    && character != '-')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsRecoverableIoError(Exception exception)
        {
            return exception is IOException
                || exception is UnauthorizedAccessException
                || exception is ArgumentException
                || exception is NotSupportedException
                || exception is SecurityException;
        }

        private static void TryDeleteTemporary(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception) when (IsRecoverableIoError(exception))
            {
                // The original write failure remains the actionable error.
            }
        }
    }

    public sealed class SnapshotService<T>
        where T : class
    {
        private const int HeaderSize = sizeof(int);
        private readonly ISnapshotStore _store;
        private readonly ISnapshotCodec<T> _codec;
        private readonly int _currentSchemaVersion;

        public SnapshotService(
            ISnapshotStore store,
            ISnapshotCodec<T> codec,
            int currentSchemaVersion)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));

            if (currentSchemaVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentSchemaVersion));
            }

            _currentSchemaVersion = currentSchemaVersion;
        }

        public bool TrySave(string slot, T state, out string? error)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            byte[] payload;

            try
            {
                payload = _codec.Encode(state);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            byte[] framed = new byte[HeaderSize + payload.Length];
            BinaryPrimitives.WriteInt32LittleEndian(framed, _currentSchemaVersion);
            Array.Copy(payload, 0, framed, HeaderSize, payload.Length);
            return _store.TryWrite(slot, framed, out error);
        }

        public bool TryLoad(
            string slot,
            [NotNullWhen(true)] out T? state,
            [NotNullWhen(false)] out string? error)
        {
            state = null;

            if (!_store.TryRead(slot, out byte[]? framed, out error))
            {
                return false;
            }

            if (framed == null || framed.Length < HeaderSize)
            {
                error = "Snapshot header is truncated.";
                return false;
            }

            int schemaVersion = BinaryPrimitives.ReadInt32LittleEndian(framed);

            if (schemaVersion != _currentSchemaVersion)
            {
                error = $"Unsupported schema version {schemaVersion}; expected {_currentSchemaVersion}.";
                return false;
            }

            var payload = new ReadOnlySpan<byte>(framed, HeaderSize, framed.Length - HeaderSize);
            return _codec.TryDecode(payload, out state, out error);
        }

        public bool TryDelete(string slot, out string? error)
        {
            return _store.TryDelete(slot, out error);
        }
    }
}
