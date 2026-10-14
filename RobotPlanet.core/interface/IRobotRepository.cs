using System.Collections.Specialized;
using RobotPlanet.Core.entities;

namespace RobotPlanet.Core.@interface
{
    /// <summary>
    /// Non-generic, read-only view of a Repository&lt;T&gt;. It lets the world and the UI
    /// keep ONE list of different repositories (Repository&lt;CleanerBot&gt;, Repository&lt;GuardBot&gt;, ...)
    /// without knowing T. Nothing can be added or removed through it.
    /// </summary>
    public interface IRobotRepository : INotifyCollectionChanged
    {
        string Title { get; }
        int Count { get; }
        IReadOnlyList<Robot> Robots { get; }
    }
}
