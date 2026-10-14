using System.Collections.ObjectModel;
using System.Collections.Specialized;
using RobotPlanet.Core.entities;
using RobotPlanet.Core.events;
using RobotPlanet.Core.@interface;
using RobotPlanet.Core.Properties;

namespace RobotPlanet.Core.repository
{
    /// <summary>
    /// A repository for ONE robot type. Robots inside it react to each other's events.
    /// Robots in other repositories never hear about them.
    /// </summary>
    public class Repository<T> : IRepository<T>, IRobotRepository where T : Robot
    {
        private readonly ObservableCollection<T> _items = new();
        private readonly Dictionary<T, Subscription> _subscriptions = new();

        public Repository()
        {
            Items = new ReadOnlyObservableCollection<T>(_items);
            _items.CollectionChanged += (_, e) => CollectionChanged?.Invoke(this, e);
        }

        public string Title => typeof(T).Name;
        public int Count => _items.Count;

        /// <summary>Read-only observable view for the UI (the UI can't add or remove directly).</summary>
        public ReadOnlyObservableCollection<T> Items { get; }

        /// <summary>The same items seen as the base type (IReadOnlyList is covariant).</summary>
        public IReadOnlyList<Robot> Robots => Items;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        // ---- Events (domain logic, not WPF button handlers) ----
        public event EventHandler<RobotEventArgs>? ItemAdded;
        public event EventHandler<RobotEventArgs>? ItemRemoved;
        public event EventHandler<RobotEventArgs>? CrazyActionPerformed;

        /// <summary>Raised for every reaction, so the ViewModel can write it to the log.</summary>
        public event EventHandler<ReactionEventArgs>? ReactionOccurred;

        // ---- IRepository<T> ----
        public void Add(T item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (_items.Any(x => x.Id == item.Id))
                throw new InvalidOperationException(string.Format(Resources.Repo_DuplicateId, item.Id));

            _items.Add(item);
            Subscribe(item);
            ItemAdded?.Invoke(this, new RobotEventArgs(item, RobotEventKind.Added));
        }

        public bool Remove(T item)
        {
            if (item is null || !_items.Remove(item))
                return false;

            // Unsubscribe FIRST: the removed robot must not react to its own removal
            // or to anything that happens later.
            Unsubscribe(item);
            ItemRemoved?.Invoke(this, new RobotEventArgs(item, RobotEventKind.Removed));
            return true;
        }

        public IEnumerable<T> GetAll() => _items.ToList();

        public T? Find(Func<T, bool> predicate) => _items.FirstOrDefault(predicate);

        /// <summary>
        /// Makes the actor do its own CrazyAction() and tells the others about it.
        /// </summary>
        public string TriggerCrazyAction(T actor)
        {
            ArgumentNullException.ThrowIfNull(actor);
            if (!_items.Contains(actor))
                return string.Format(Resources.Repo_NotInRepository, actor.Name);

            string message = actor.CrazyAction();
            CrazyActionPerformed?.Invoke(this, new RobotEventArgs(actor, RobotEventKind.CrazyAction));
            return message;
        }

        // ---- Subscription management ----
        private sealed class Subscription
        {
            public required EventHandler<RobotEventArgs> OnAdded { get; init; }
            public required EventHandler<RobotEventArgs> OnRemoved { get; init; }
            public required EventHandler<RobotEventArgs> OnCrazyAction { get; init; }
        }

        private void Subscribe(T item)
        {
            var subscription = new Subscription
            {
                OnAdded = (_, e) => React(item, e, item.ReactToAdded),
                OnRemoved = (_, e) => React(item, e, item.ReactToRemoved),
                OnCrazyAction = (_, e) => React(item, e, item.ReactToCrazyAction)
            };

            ItemAdded += subscription.OnAdded;
            ItemRemoved += subscription.OnRemoved;
            CrazyActionPerformed += subscription.OnCrazyAction;
            _subscriptions[item] = subscription;
        }

        private void Unsubscribe(T item)
        {
            if (!_subscriptions.Remove(item, out Subscription? subscription))
                return;

            ItemAdded -= subscription.OnAdded;
            ItemRemoved -= subscription.OnRemoved;
            CrazyActionPerformed -= subscription.OnCrazyAction;
        }

        private void React(T reactor, RobotEventArgs cause, Func<Robot, string> reaction)
        {
            // A robot does not react to its own event.
            if (ReferenceEquals(cause.Source, reactor))
                return;

            string message = reaction(cause.Source);
            ReactionOccurred?.Invoke(this, new ReactionEventArgs(reactor, cause, message));
        }
    }
}