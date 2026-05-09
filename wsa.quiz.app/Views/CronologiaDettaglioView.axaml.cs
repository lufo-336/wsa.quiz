using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Wsa.Quiz.App.State;
using Wsa.Quiz.Core.Models;

namespace Wsa.Quiz.App.Views;

/// <summary>
/// Sub-view della <see cref="CronologiaView"/>: mostra il dettaglio domanda-per-domanda
/// di una singola sessione. Riusa il pattern di <see cref="RiepilogoView"/> per le
/// statistiche in alto e per il template della singola riga, ma:
/// - mostra TUTTE le risposte (corrette e sbagliate, con bordo colorato),
/// - ha un bottone "Indietro" invece di "Torna alla Home".
/// </summary>
public partial class CronologiaDettaglioView : UserControl, INotifyPropertyChanged
{
    /// <summary>Sollevato al click di "Torna alla cronologia". La <see cref="CronologiaView"/> torna alla lista.</summary>
    public event EventHandler? IndietroRichiesto;

    /// <summary>Sollevato quando l'utente conferma l'eliminazione di questa partita
    /// dal dettaglio. La <see cref="CronologiaView"/> esegue l'eliminazione su storage,
    /// torna alla lista e ricarica.</summary>
    public event EventHandler<string>? EliminazioneRichiesta;

    public new event PropertyChangedEventHandler? PropertyChanged;

    private readonly RisultatoQuiz _risultato;

    // ------------------------------------------------------------------ DATI

    public string Titolo { get; }
    public string Sottotitolo { get; }
    public string TitoloLista => Dettagli.Count == 0
        ? "Domande della sessione"
        : $"Domande della sessione ({Dettagli.Count})";

    public string PercentualeFormattata { get; }
    public IBrush ColorePercentuale { get; }
    public int TotaleDomande => _risultato.TotaleDomande;
    public int RisposteCorrette => _risultato.RisposteCorrette;
    public int RisposteErrate => _risultato.RisposteErrate;
    public int Punteggio => _risultato.Punteggio;
    public string DurataFormattata { get; }
    public string Modalita => string.IsNullOrWhiteSpace(_risultato.Modalita) ? "—" : _risultato.Modalita;
    public string MateriaEtichetta => string.IsNullOrWhiteSpace(_risultato.MateriaNome) ? "—" : _risultato.MateriaNome;

    public IList<DettaglioRispostaItem> Dettagli { get; }
    public bool NessunDettaglio => Dettagli.Count == 0;

    // ------------------------------------------------------------------ STATO CONFERMA ELIMINA

    private bool _inAttesaConferma;
    public bool InAttesaConfermaEliminazione
    {
        get => _inAttesaConferma;
        private set
        {
            if (_inAttesaConferma == value) return;
            _inAttesaConferma = value;
            Raise();
            Raise(nameof(NonInAttesaConferma));
        }
    }

    public bool NonInAttesaConferma => !_inAttesaConferma;

    // ------------------------------------------------------------------ COSTRUZIONE

    public CronologiaDettaglioView(RisultatoQuiz risultato)
    {
        InitializeComponent();
        _risultato = risultato;

        Titolo = risultato.Abbandonato
            ? "Sessione abbandonata"
            : "Dettaglio sessione";
        Sottotitolo = $"Avviata il {risultato.DataOra:dd MMM yyyy} alle {risultato.DataOra:HH:mm}.";

        PercentualeFormattata = $"{Math.Round(risultato.PercentualeCorrette)}%";
        ColorePercentuale = ColoreDaPercentuale(risultato.PercentualeCorrette);

        var d = risultato.DurataQuiz;
        DurataFormattata = d.TotalHours >= 1
            ? $"{(int)d.TotalHours}:{d.Minutes:00}:{d.Seconds:00}"
            : $"{d.Minutes:00}:{d.Seconds:00}";

        Dettagli = risultato.Dettagli
            .Select(x => new DettaglioRispostaItem(x))
            .ToList();

        DataContext = this;
    }

    private static IBrush ColoreDaPercentuale(double pct)
    {
        if (pct >= 80) return new SolidColorBrush(Color.Parse("#1F7A4D"));
        if (pct >= 50) return new SolidColorBrush(Color.Parse("#B8860B"));
        return new SolidColorBrush(Color.Parse("#B85450"));
    }

    private void OnIndietroClick(object? sender, RoutedEventArgs e)
    {
        IndietroRichiesto?.Invoke(this, EventArgs.Empty);
    }

    private void OnEliminaClick(object? sender, RoutedEventArgs e)
    {
        InAttesaConfermaEliminazione = true;
    }

    private void OnAnnullaEliminaClick(object? sender, RoutedEventArgs e)
    {
        InAttesaConfermaEliminazione = false;
    }

    private void OnConfermaEliminaClick(object? sender, RoutedEventArgs e)
    {
        EliminazioneRichiesta?.Invoke(this, _risultato.Id);
    }

    private void Raise([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
