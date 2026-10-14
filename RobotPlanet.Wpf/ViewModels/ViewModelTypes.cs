using RobotPlanet.Core.@interface;

namespace RobotPlanet.Wpf.ViewModels
{
    public enum SortOption
    {
        Name,
        Battery,
        Integrity,
        Id
    }

    /// <summary>One entry of the sort drop-down: what to sort by + the text the user sees.</summary>
    public record SortChoice(SortOption Option, string Label);

    /// <summary>One row of the GroupBy summary (one robot type).</summary>
    public record GroupSummary(string Title, int Count, int AverageBattery, int AverageIntegrity);

    /// <summary>
    /// Wraps a repository for the repository list: the Count updates automatically
    /// whenever robots are added or removed.
    /// </summary>
    public class RepositoryItem : ViewModelBase
    {
        public RepositoryItem(IRobotRepository repository)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            Repository.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Count));
        }

        public IRobotRepository Repository { get; }
        public string Title => Repository.Title;
        public int Count => Repository.Count;
    }
}
