using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace applanch.Core.ViewModels;

/// <summary>
/// Base class for objects that implement <see cref="INotifyPropertyChanged"/>.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets <paramref name="field"/> to <paramref name="value"/> and raises
    /// <see cref="PropertyChanged"/> if the value changed.
    /// </summary>
    /// <returns><see langword="true"/> if the value changed; otherwise <see langword="false"/>.</returns>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>Raises <see cref="PropertyChanged"/> for <paramref name="propertyName"/>.</summary>
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
