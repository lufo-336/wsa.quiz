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
                RaisePropertyChanged(nameof(PuoCliccare));
            }
        }
    }

    /// <summary>Helper per binding bool-only (es. selettori di stile).</summary>
    public bool IsNeutra    => _stato == StatoRisposta.Neutra;
    public bool IsCorretta  => _stato == StatoRisposta.Corretta;
    public bool IsSbagliata => _stato == StatoRisposta.Sbagliata;

    // ----- Step 7 -----

    private bool _isEnabled = true;
    /// <summary>
    /// Flag indipendente dallo stato. La <c>SessioneQuiz</c> lo mette a <c>false</c>
    /// quando entra in view-mode (i bottoni vanno disabilitati anche se Neutri).
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetField(ref _isEnabled, value))
                RaisePropertyChanged(nameof(PuoCliccare));
        }
    }

    private bool _isHighlighted;
    /// <summary>
    /// Bottone evidenziato dalle frecce ↑/↓. Lo style XAML applica un bordo accent
    /// quando questa proprieta' e' vera e lo stato e' <see cref="StatoRisposta.Neutra"/>.
    /// </summary>
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetField(ref _isHighlighted, value);
    }

    /// <summary>
    /// Computed: il bottone risposta e' cliccabile solo se enabled E ancora in
    /// stato neutro (anti doppio-click + view-mode).
    /// </summary>
    public bool PuoCliccare => _isEnabled && _stato == StatoRisposta.Neutra;

    public RispostaItem(int indice, string testo)
    {
        Indice = indice;
        Lettera = (char)('A' + indice);
        Testo = testo;
    }
}
