using Avalonia.Media;
using Wsa.Quiz.Core.Models;

namespace Wsa.Quiz.App.State;

/// <summary>
/// Wrapper di una singola riga di dettaglio nella vista di dettaglio cronologia.
/// A differenza di <see cref="DettaglioRisposta"/> (che e' nel Core e quindi
/// non puo' avere riferimenti UI), espone gia' i colori e i flag che servono
/// al template XAML per disegnare correttamente corrette e sbagliate.
/// </summary>
public class DettaglioRispostaItem
{
    public DettaglioRisposta Dettaglio { get; }

    public string MateriaNome => Dettaglio.MateriaNome;
    public string Categoria => Dettaglio.Categoria;
    public string TestoDomanda => Dettaglio.TestoDomanda;
    public string RispostaData => Dettaglio.RispostaData;
    public string RispostaCorretta => Dettaglio.RispostaCorretta;
    public string Spiegazione => Dettaglio.Spiegazione;
    public bool Corretta => Dettaglio.Corretta;
    public int Tentativi => Dettaglio.Tentativi;

    /// <summary>Mostra la riga "Risposta corretta" sotto, solo se la risposta data era sbagliata.</summary>
    public bool MostraRispostaCorretta => !Dettaglio.Corretta;

    /// <summary>Mostra la riga del numero di tentativi, solo se >1 (rotazione).</summary>
    public bool MostraTentativi => Dettaglio.Tentativi > 1;

    public string TentativiEtichetta => $"Risposta corretta dopo {Dettaglio.Tentativi} tentativi";

    public IBrush ColoreBordo { get; }
    public IBrush ColoreRisposta { get; }
    public string EsitoEtichetta { get; }
    public IBrush ColoreEsito { get; }

    public DettaglioRispostaItem(DettaglioRisposta d)
    {
        Dettaglio = d;
        ColoreBordo = QuizColors.Esito(d.Corretta);
        ColoreRisposta = QuizColors.Esito(d.Corretta);
        EsitoEtichetta = d.Corretta ? "Corretto" : "Sbagliato";
        ColoreEsito = QuizColors.Esito(d.Corretta);
    }
}
