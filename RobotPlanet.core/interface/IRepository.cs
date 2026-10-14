using RobotPlanet.Core.entities;

namespace RobotPlanet.Core.@interface
{
    /// <summary>
    /// Generic repository. The constraint "where T : Robot" makes the COMPILER reject
    /// anything that is not a robot, e.g. Repository&lt;string&gt; does not compile.
    /// </summary>
    public interface IRepository<T> where T : Robot
    {
        void Add(T item);
        bool Remove(T item);
        IEnumerable<T> GetAll();
        T? Find(Func<T, bool> predicate);
    }
}
