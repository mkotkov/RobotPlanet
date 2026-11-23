using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using RobotPlanet.Core.Properties;

namespace RobotPlanet.Core.storage
{
    /// <summary>The file reading and writing shared by the JSON and XML storages.</summary>
    internal static class FileIo
    {
        /// <summary>
        /// Writes to a temporary file first and replaces the real file only when the write succeeded,
        /// so a failed save never destroys the previous file.
        /// </summary>
        public static async Task WriteAtomicAsync(string path, byte[] content, CancellationToken token)
        {
            RequirePath(path);
            string temp = path + ".tmp";
            try
            {
                await File.WriteAllBytesAsync(temp, content, token);
                File.Move(temp, path, overwrite: true);
            }
            catch (OperationCanceledException)
            {
                TryDelete(temp);
                throw;
            }
            catch (Exception ex)
            {
                TryDelete(temp);
                throw new WorldStorageException(string.Format(Resources.Storage_WriteFailed, path, ex.Message), ex);
            }
        }

        public static async Task<byte[]> ReadAsync(string path, CancellationToken token)
        {
            RequirePath(path);
            if (!File.Exists(path))
                throw new WorldStorageException(string.Format(Resources.Storage_FileNotFound, path));

            byte[] content;
            try
            {
                content = await File.ReadAllBytesAsync(path, token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new WorldStorageException(string.Format(Resources.Storage_ReadFailed, path, ex.Message), ex);
            }

            if (content.Length == 0)
                throw new WorldStorageException(Resources.Storage_EmptyFile);
            return content;
        }

        private static void RequirePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new WorldStorageException(Resources.Storage_PathEmpty);
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    /// <summary>Saves and loads the world as JSON.</summary>
    public sealed class JsonWorldStorage : IWorldStorage
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,   // keeps ä, ö, õ, ü readable in the file
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public Task SaveAsync(WorldDto world, string path, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(world);
            byte[] content = JsonSerializer.SerializeToUtf8Bytes(world, Options);
            return FileIo.WriteAtomicAsync(path, content, token);
        }

        public async Task<WorldDto> LoadAsync(string path, CancellationToken token = default)
        {
            byte[] content = await FileIo.ReadAsync(path, token);
            try
            {
                return JsonSerializer.Deserialize<WorldDto>(content, Options)
                       ?? throw new WorldStorageException(string.Format(Resources.Storage_BadFormat, "JSON", Resources.Storage_NoContent));
            }
            catch (JsonException ex)
            {
                throw new WorldStorageException(string.Format(Resources.Storage_BadFormat, "JSON", ex.Message), ex);
            }
        }
    }

    /// <summary>Saves and loads the world as XML (the same WorldDto, so both formats restore the same world).</summary>
    public sealed class XmlWorldStorage : IWorldStorage
    {
        private static readonly XmlSerializer Serializer = new(typeof(WorldDto));

        public Task SaveAsync(WorldDto world, string path, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(world);

            using var buffer = new MemoryStream();
            var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
            using (XmlWriter writer = XmlWriter.Create(buffer, settings))
                Serializer.Serialize(writer, world);

            return FileIo.WriteAtomicAsync(path, buffer.ToArray(), token);
        }

        public async Task<WorldDto> LoadAsync(string path, CancellationToken token = default)
        {
            byte[] content = await FileIo.ReadAsync(path, token);
            try
            {
                // DTD processing is prohibited: a file must not be able to pull in external data.
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
                using var stream = new MemoryStream(content);
                using XmlReader reader = XmlReader.Create(stream, settings);
                return Serializer.Deserialize(reader) as WorldDto
                       ?? throw new WorldStorageException(string.Format(Resources.Storage_BadFormat, "XML", Resources.Storage_NoContent));
            }
            catch (Exception ex) when (ex is InvalidOperationException or XmlException)
            {
                string detail = ex.InnerException?.Message ?? ex.Message;
                throw new WorldStorageException(string.Format(Resources.Storage_BadFormat, "XML", detail), ex);
            }
        }
    }

    /// <summary>
    /// The storage the application registers for files: it picks JSON or XML from the file extension.
    /// The ViewModel still sees one IWorldStorage.
    /// </summary>
    public sealed class FileWorldStorage : IWorldStorage, IFileWorldStorage
    {
        private readonly IWorldStorage _json;
        private readonly IWorldStorage _xml;

        public FileWorldStorage(JsonWorldStorage json, XmlWorldStorage xml)
        {
            _json = json ?? throw new ArgumentNullException(nameof(json));
            _xml = xml ?? throw new ArgumentNullException(nameof(xml));
        }

        public string FileFilter => Resources.Storage_FileFilter;

        public Task SaveAsync(WorldDto world, string path, CancellationToken token = default) =>
            Pick(path).SaveAsync(world, path, token);

        public Task<WorldDto> LoadAsync(string path, CancellationToken token = default) =>
            Pick(path).LoadAsync(path, token);

        private IWorldStorage Pick(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new WorldStorageException(Resources.Storage_PathEmpty);

            string extension = Path.GetExtension(path).ToLowerInvariant();
            return extension switch
            {
                ".json" => _json,
                ".xml" => _xml,
                _ => throw new WorldStorageException(Resources.Storage_UnknownExtension)
            };
        }
    }
}
