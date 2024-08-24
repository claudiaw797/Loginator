
namespace Loginator.Model {

    public record SearchOptions {

        /// <summary>
        /// The search criteria, i.e. text to search.
        /// </summary>
        public string? Criteria { get; init; }
        /// <summary>
        /// A value indicating whether to invert the search, i.e. search for entries not matching <see cref="Criteria"/>.
        /// </summary>
        public bool IsInverted { get; init; }
    }
}
