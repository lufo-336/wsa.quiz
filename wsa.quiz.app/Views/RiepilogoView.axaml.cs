using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Wsa.Quiz.Core.Models;

namespace Wsa.Quiz.App.Views;

/// <summary>
/// Schermata di riepilogo a fine quiz. Espone proprieta' formattate
/// (percentuale, durata, sottotitolo, lista sbagliate) per il binding XAML.
/// </summary>
public partial class RiepilogoView : UserControl, INotifyPropertyChanged
{
    public event EventHandler? TornaAllaHome;

    public new event PropertyChangedEventHandler? PropertyChanged;

    private readonly RisultatoQuiz _risultato;

    // ------------------------------------------------------------------ DATI

    public string Titolo { get; }
    public string Sottotitolo { get; }
    public string TitoloLista => Sbagliate.Count == 0
        ? "Risposte sbagliate"
        : $"Risposte sbagliate ({Sbagliate.Count})";

    public string PercentualeFormattata { get; }
    public IBrush ColorePercentuale { get; }
    public int TotaleDomande => _risultato.TotaleDomande;
    public int RisposteCorrette => _risultato.RisposteCorrette;
    public int RisposteErrate => _risultato.RisposteErrate;
    public int Punteggio => _risultato.Punteggio;
    public string DurataFormattata { get; }
    public string Modalita => _risultato.Modalita;

    public IList<DettaglioRisposta> Sbagliate { get; }
    public bool NessunaSbagliata => Sbagliate.Count == 0;

    // ------------------------------------------------------------------ COSTRUZIONE

    public RiepilogoView(RisultatoQuiz risultato)
    {
        InitializeComponent();
        _risultato = risultato;

        Titolo = risultato.Abbandonato ? "Quiz abbandonato" : "Quiz concluso";
        Sottotitolo = risultato.Abbandonato
            ? "Hai abbandonato la sessione. Le risposte gia' date sono state comunque salvate."
            : "Ecco com'e' andata.";

        PercentualeFormattata = $"{Math.Round(risultato.PercentualeCorrette)}%";
        ColorePercentuale = ColoreDaPercentuale(risultato.PercentualeCorrette);

        var d = risultato.DurataQuiz;
        DurataFormattata = d.TotalHours >= 1
            ? $"{(int)d.TotalHours}:{d.Minutes:00}:{d.Seconds:00}"
            : $"{d.Minutes:00}:{d.Seconds:00}";

        Sbagliate = risultato.Dettagli.Where(x => !x.Corretta).ToList();

        DataContext = this;
    }

    private static IBrush ColoreDaPercentuale(double pct)
    {
        // verde >= 80, ambra 50-79, rosso < 50
        if (pct >= 80) return new SolidColorBrush(Color.Parse("#1F7A4D"));
        if (pct >= 50) return new SolidColorBrush(Color.Parse("#B8860B"));
        return new SolidColorBrush(Color.Parse("#B85450"));
    }

    private void OnTornaClick(object? sender, RoutedEventArgs e)
    {
        TornaAllaHome?.Invoke(this, EventArgs.Empty);
    }
}
