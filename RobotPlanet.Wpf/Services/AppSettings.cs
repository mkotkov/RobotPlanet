using System.IO;
using System.Text.Json;
using RobotPlanet.Wpf.Properties;

namespace RobotPlanet.Wpf.Services
{
    /// <summary>
    /// What the application is started with: which storage it uses (File, LocalDb, EfCore) and how to reach LocalDB.
    /// Read once at startup from appsettings.json (next to the exe); the command line wins over the file,
    /// e.g. RobotPlanet.Wpf.exe --storage=EfCore
    /// A wrong setting never stops the application: it falls back to the file storage and tells the user.
    /// </summary>
    public sealed class AppSettings
    {
        public const string FileStorage = "File";
        public const string LocalDbStorage = "LocalDb";
        public const string EfCoreStorage = "EfCore";

        public const string DefaultConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=RobotPlanet;Integrated Security=true;TrustServerCertificate=true";

        public string Storage { get; init; } = FileStorage;
        public string LocalDbConnectionString { get; init; } = DefaultConnectionString;

        /// <summary>Something the user should know about the settings (null when everything was fine).</summary>
        public string? Warning { get; init; }

        public bool UseLocalDb => Storage == LocalDbStorage;
        public bool UseEfCore => Storage == EfCoreStorage;

        /// <param name="args">The command line arguments of the application.</param>
        /// <param name="settingsPath">Only for tests; by default the file next to the exe is used.</param>
        public static AppSettings Load(string[] args, string? settingsPath = null)
        {
            string storage = FileStorage;
            string connection = DefaultConnectionString;
            var warnings = new List<string>();

            string path = settingsPath ?? Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(path))
            {
                try
                {
                    var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
                    using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path), options);
                    JsonElement root = document.RootElement;

                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        if (root.TryGetProperty("Storage", out JsonElement s) && s.ValueKind == JsonValueKind.String)
                            storage = s.GetString() ?? FileStorage;

                        if (root.TryGetProperty("LocalDbConnectionString", out JsonElement c) && c.ValueKind == JsonValueKind.String
                            && !string.IsNullOrWhiteSpace(c.GetString()))
                            connection = c.GetString()!;
                        else if (root.TryGetProperty("ConnectionStrings", out JsonElement cs) && cs.ValueKind == JsonValueKind.Object
                            && cs.TryGetProperty("RobotPlanet", out JsonElement rp) && rp.ValueKind == JsonValueKind.String
                            && !string.IsNullOrWhiteSpace(rp.GetString()))
                            connection = rp.GetString()!;
                    }
                }
                catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
                {
                    warnings.Add(string.Format(Resources.Settings_Unreadable, path, ex.Message));
                }
            }

            // the command line wins over the file
            const string prefix = "--storage=";
            foreach (string arg in args ?? Array.Empty<string>())
            {
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    storage = arg[prefix.Length..];
            }

            if (storage.Equals(FileStorage, StringComparison.OrdinalIgnoreCase)) storage = FileStorage;
            else if (storage.Equals(LocalDbStorage, StringComparison.OrdinalIgnoreCase)) storage = LocalDbStorage;
            else if (storage.Equals(EfCoreStorage, StringComparison.OrdinalIgnoreCase)) storage = EfCoreStorage;
            else
            {
                warnings.Add(string.Format(Resources.Settings_UnknownStorage, storage));
                storage = FileStorage;
            }

            return new AppSettings
            {
                Storage = storage,
                LocalDbConnectionString = connection,
                Warning = warnings.Count == 0 ? null : string.Join(Environment.NewLine, warnings)
            };
        }
    }
}
