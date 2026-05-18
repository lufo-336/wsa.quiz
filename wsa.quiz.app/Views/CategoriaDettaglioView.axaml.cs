using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Wsa.Quiz.Core.Models.Statistiche;

namespace Wsa.Quiz.App.Views;

/// <summary>
/// Drill-down della heatmap statistiche: mostra le domande sbagliate
/// di una categoria. Espone <see cref="IndietroRichiesto"/> per tornare
/// alla heatmap.
/// </summary>
public partial class CategoriaDettaglioView : UserControl, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? IndietroRichiesto;

    public ObservableCollection<DomandaSbagliata> Sbagliate { get; } = new();

    private string _titolo = "";
    public string Titolo
    {
        get => _titolo;
        private set { if (_titolo != value) { _titolo = value; Raise(); } }
    }

    public CategoriaDettaglioView()
    {
        InitializeComponent();
        DataContext = this;
        // Stesso pattern Tunnel+handledEventsToo dello step 8/PR#2: tentiamo
        // di intercettare Esc per tornare indietro. Se non funziona, il
        // bottone resta sempre disponibile (vedi Trappola 13 in CLAUDE.md).
        AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    public void Mostra(CategoriaStat stat)
    {
        Titolo = $"{stat.Materia} / {stat.Categoria} — {stat.Errori} errori su {stat.Visti} viste ({stat.PctErrore:P0})";
        Sbagliate.Clear();
        foreach (var d in stat.Sbagliate)
            Sbagliate.Add(d);
    }

    private void OnIndietroClick(object? sender, RoutedEventArgs e)
        => IndietroRichiesto?.Invoke(this, EventArgs.Empty);

    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            IndietroRichiesto?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
