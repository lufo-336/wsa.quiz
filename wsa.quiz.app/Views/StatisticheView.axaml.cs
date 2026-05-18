using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Wsa.Quiz.App.State;
using Wsa.Quiz.Core.Models.Statistiche;
using Wsa.Quiz.Core.Services;

namespace Wsa.Quiz.App.Views;

/// <summary>
/// Tab "Statistiche": heatmap dei punti deboli per categoria.
/// I task successivi aggiungono filtro periodo (Tutto/Ultime N), drill-down
/// e empty state.
/// </summary>
public partial class StatisticheView : UserControl, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;

    private StorageService? _storage;
    private readonly StatisticheService _servizio = new();

    public ObservableCollection<MateriaRigaItem> Righe { get; } = new();

    private bool _modalitaTutto = false;
    public bool ModalitaTutto
    {
        get => _modalitaTutto;
        set
        {
            if (_modalitaTutto != value)
            {
                _modalitaTutto = value;
                Raise();
                Ricarica();
            }
        }
    }

    private int _limiteUltimeN = 30;
    public int LimiteUltimeN
    {
        get => _limiteUltimeN;
        set
        {
            if (_limiteUltimeN != value)
            {
                _limiteUltimeN = value;
                Raise();
            }
        }
    }

    private bool _modoDettaglio;
    public bool ModoDettaglio
    {
        get => _modoDettaglio;
        private set
        {
            if (_modoDettaglio != value)
            {
                _modoDettaglio = value;
                Raise();
                Raise(nameof(HeatmapVisibile));
            }
        }
    }

    private bool _nessunDato = true;
    public bool NessunDato
    {
        get => _nessunDato;
        private set
        {
            if (_nessunDato != value)
            {
                _nessunDato = value;
                Raise();
                Raise(nameof(HeatmapVisibile));
            }
        }
    }

    private bool _filtroTroppoStretto;
    public bool FiltroTroppoStretto
    {
        get => _filtroTroppoStretto;
        private set
        {
            if (_filtroTroppoStretto != value)
            {
                _filtroTroppoStretto = value;
                Raise();
                Raise(nameof(HeatmapVisibile));
            }
        }
    }

    public bool HeatmapVisibile => !ModoDettaglio && !NessunDato && !FiltroTroppoStretto;

    private CategoriaDettaglioView? _dettaglio;

    public StatisticheView()
    {
        InitializeComponent();
        DataContext = this;
    }

    public void Inizializza(StorageService storage)
    {
        _storage = storage;
        Ricarica();
    }

    public void Ricarica()
    {
        if (_storage == null) return;
        Righe.Clear();
        try
        {
            var cronologia = _storage.CaricaCronologia();
            int? limite = ModalitaTutto ? null : LimiteUltimeN;
            var stat = _servizio.Calcola(cronologia, limite);
            foreach (var r in stat)
                Righe.Add(new MateriaRigaItem(r));

            NessunDato = cronologia.Count == 0;
            FiltroTroppoStretto = !NessunDato && Righe.Count == 0;
        }
        catch
        {
            NessunDato = true;
            FiltroTroppoStretto = false;
        }
    }

    private void OnAggiornaClick(object? sender, RoutedEventArgs e) => Ricarica();

    private void OnLimiteChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (!ModalitaTutto) Ricarica();
    }

    private void OnCellaPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border b) return;
        if (b.DataContext is not CategoriaCellaItem cella) return;
        ApriDettaglio(cella.Stat);
    }

    private void ApriDettaglio(CategoriaStat stat)
    {
        if (_dettaglio == null)
        {
            _dettaglio = new CategoriaDettaglioView();
            _dettaglio.IndietroRichiesto += (_, _) => ChiudiDettaglio();
            DettaglioArea.Content = _dettaglio;
        }
        _dettaglio.Mostra(stat);
        ModoDettaglio = true;
    }

    private void ChiudiDettaglio() => ModoDettaglio = false;

    private void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
