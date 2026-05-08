namespace Wsa.Quiz.App.State;

/// <summary>
/// Stato visivo di una singola risposta nella schermata di quiz.
/// Pilotato dalla logica di sessione e tradotto in colori dagli stili XAML.
/// </summary>
public enum StatoRisposta
{
    /// <summary>Non ancora risposto: tutte le risposte sono in questo stato.</summary>
    Neutra,
    /// <summary>Quella che l'utente ha cliccato, e che era sbagliata.</summary>
    Sbagliata,
    /// <summary>La risposta corretta (sempre evidenziata dopo il click).</summary>
    Corretta
}

/// <summary>
/// Singola riga risposta nella QuizView. Lo <see cref="Stato"/> e' observable,
/// cosi' lo style XAML reagisce al cambio senza ricreare la collezione.
/// </summary>
public class RispostaItem : ObservableObject
{
    public int Indice { get; }
    public char Lettera { get; }
    public string Testo { get; }

    public string EtichettaLettera => $"{Lettera}.";

    private StatoRisposta _stato = StatoRisposta.Neutra;
    public StatoRisposta Stato
    {
        get => _stato;
        set
        {
            if (SetField(ref _stato, value))
            {
                RaisePropertyChanged(nameof(IsCorretta));
                RaisePropertyChanged(nameof(IsSbagliata));
                RaisePropertyChanged(nameof(IsNeutra));
            }
        }
    }

    /// <summary>Helper per binding bool-only (es. selettori di stile).</summary>
    public bool IsNeutra    => _stato == StatoRisposta.Neutra;
    public bool IsCorretta  => _stato == StatoRisposta.Corretta;
    public bool IsSbagliata => _stato == StatoRisposta.Sbagliata;

    public RispostaItem(int indice, string testo)
    {
        Indice = indice;
        Lettera = (char)('A' + indice);
        Testo = testo;
    }
}
