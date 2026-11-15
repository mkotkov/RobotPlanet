using RobotPlanet.Core.entities;
using RobotPlanet.Core.Properties;

namespace RobotPlanet.Core.repository
{
    // Vol.4: the part of Repository<T> that is only used when a saved world is loaded.
    public partial class Repository<T> where T : Robot
    {
        /// <summary>
        /// Adds a robot that was loaded from storage. It is subscribed to the events again
        /// (so the reactions between neighbours work), but NO event is raised:
        /// loading must not trigger "welcome" reactions that would change the loaded states.
        /// </summary>
        internal void Restore(T item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (_items.Any(x => x.Id == item.Id))
                throw new InvalidOperationException(string.Format(Resources.Repo_DuplicateId, item.Id));

            _items.Add(item);
            Subscribe(item);
        }

        /// <summary>
        /// Empties the repository without raising events. The old robots are unsubscribed first,
        /// so they never react again.
        /// </summary>
        internal void ClearSilently()
        {
            foreach (T item in _items.ToList())
                Unsubscribe(item);

            _items.Clear();
        }
    }
}
