using RobotPlanet.Core.entities;

namespace RobotPlanet.Core.storage
{
    /// <summary>
    /// The contract every storage implements (JSON, XML, LocalDB ADO.NET, EF Core).
    /// The ViewModel only knows this interface.
    /// </summary>
    public interface IWorldStorage
    {
        Task SaveAsync(WorldDto world, string path, CancellationToken token = default);

        Task<WorldDto> LoadAsync(string path, CancellationToken token = default);
    }

    /// <summary>
    /// Implemented by storages (such as EF Core TPH) that persist and materialize
    /// domain Robot entities directly rather than intermediate DTOs.
    /// </summary>
    public interface IEntityWorldStorage : IWorldStorage
    {
        Task SaveWorldAsync(IReadOnlyList<Robot> robots, CancellationToken token = default);

        Task<IReadOnlyList<Robot>> LoadWorldAsync(CancellationToken token = default);
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
