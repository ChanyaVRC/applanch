using System.Collections.ObjectModel;

namespace applanch;

internal static class CollectionExtensions
{
    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source) =>
        new(source);
}
