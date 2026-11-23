namespace RobotPlanet.Core.storage
{
    /// <summary>
    /// The contract every storage implements (JSON, XML, LocalDB). The ViewModel only knows this interface.
    /// All reading and writing happens inside the implementation.
    /// </summary>
    public interface IWorldStorage
    {
        Task SaveAsync(WorldDto world, string path, CancellationToken token = default);

        Task<WorldDto> LoadAsync(string path, CancellationToken token = default);
    }

    /// <summary>
    /// Implemented by storages that work with files. The ViewModel asks the dialog service for a file
    /// only when the storage can do this (a capability check with "is", like IChargeable).
    /// </summary>
    public interface IFileWorldStorage
    {
        /// <summary>The filter of the file dialog, e.g. "JSON (*.json)|*.json|XML (*.xml)|*.xml".</summary>
        string FileFilter { get; }
    }

    /// <summary>
    /// Thrown by a storage when saving or loading fails. The message is already understandable for the user,
    /// the technical cause is in InnerException.
    /// </summary>
    public sealed class WorldStorageException : Exception
    {
        public WorldStorageException(string message) : base(message)
        {
        }

        public WorldStorageException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
