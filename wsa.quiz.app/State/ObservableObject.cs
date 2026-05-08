using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Wsa.Quiz.App.State;

/// <summary>
/// Base class minimale che implementa INotifyPropertyChanged.
/// Non e' MVVM (non usiamo ICommand ne' DI), ma serve per fare binding
/// stabili senza dover riassegnare il DataContext a ogni cambiamento
/// (errore della prima versione WPF: vedere handoff).
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Imposta il campo se diverso, e solleva PropertyChanged. Ritorna true
    /// se il valore e' effettivamente cambiato.
    /// </summary>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    /// <summary>Solleva PropertyChanged a mano (utile per proprieta' calcolate).</summary>
    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
