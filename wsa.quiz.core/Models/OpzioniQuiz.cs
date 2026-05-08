namespace Wsa.Quiz.Core.Models;

/// <summary>
/// Opzioni scelte dall'utente prima di iniziare una sessione.
/// </summary>
public class OpzioniQuiz
{
    /// <summary>
    /// Se vera, le domande sbagliate rientrano in coda (riproposte più avanti, ordine casuale)
    /// e il quiz termina solo quando tutte sono state risposte correttamente almeno una volta.
    /// </summary>
    public bool Rotazione { get; set; }

    /// <summary>Se vero, mostra il tempo trascorso in alto e il tempo per domanda.</summary>
    public bool Cronometro { get; set; }

    /// <summary>Se vero, randomizza l'ordine delle domande all'avvio.</summary>
    public bool RandomizzaOrdineDomande { get; set; } = true;

    /// <summary>Se >0, limita a questo numero di domande (estratte casualmente).</summary>
    public int LimiteDomande { get; set; }

    /// <summary>Filtro per categorie. Vuoto = nessun filtro.</summary>
    public List<string> Categorie { get; set; } = new();

    /// <summary>Filtro per materie. Vuoto = tutte.</summary>
    public List<string> Materie { get; set; } = new();

    public string NomeModalita { get; set; } = string.Empty;
    public string MateriaNomeLabel { get; set; } = string.Empty;
}
