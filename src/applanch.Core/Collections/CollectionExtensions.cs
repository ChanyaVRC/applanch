using System.Collections.ObjectModel;

namespace applanch.Core.Collections;

/// <summary>Extension methods for collection types.</summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Creates an <see cref="ObservableCollection{T}"/> from an <see cref="IEnumerable{T}"/>.
    /// </summary>
    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source) =>
        new(source);
}
