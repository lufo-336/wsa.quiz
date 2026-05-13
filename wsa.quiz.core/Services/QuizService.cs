using Wsa.Quiz.Core.Models;

namespace Wsa.Quiz.Core.Services;

/// <summary>
/// Logica di business: filtri, shuffle, preparazione domande, rotazione delle sbagliate.
/// </summary>
public static class QuizService
{
    // Random.Shared e' thread-safe (.NET 6+) — sostituisce l'istanza statica non thread-safe.

    // --------------------------------------------------------- SHUFFLE / FILTRI

    public static List<Domanda> DomandeRandomizzate(IEnumerable<Domanda> sorgente)
    {
        var lista = sorgente.ToList();
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
        return lista;
    }

    public static List<Domanda> FiltraPerCategorie(IEnumerable<Domanda> sorgente, List<string> categorie)
    {
        if (categorie == null || categorie.Count == 0) return sorgente.ToList();
        var set = new HashSet<string>(categorie, StringComparer.OrdinalIgnoreCase);
        return sorgente.Where(d => set.Contains(d.Categoria)).ToList();
    }

    public static List<Domanda> FiltraPerMaterie(IEnumerable<Domanda> sorgente, List<string> materieId)
    {
        if (materieId == null || materieId.Count == 0) return sorgente.ToList();
        var set = new HashSet<string>(materieId, StringComparer.OrdinalIgnoreCase);
        return sorgente.Where(d => set.Contains(d.MateriaId)).ToList();
    }

    public static List<Domanda> PrendiN(IEnumerable<Domanda> sorgente, int n)
    {
        if (n <= 0) return sorgente.ToList();
        var rand = DomandeRandomizzate(sorgente);
        return rand.Take(n).ToList();
    }

    // ---------------------------------------------------------- CATEGORIE

    /// <summary>
    /// Restituisce un dizionario ordinato: categoria → numero di domande disponibili.
    /// </summary>
    public static List<(string Categoria, int Conteggio)> RiepilogoCategorie(IEnumerable<Domanda> domande)
    {
        return domande
            .GroupBy(d => d.Categoria)
            .Select(g => (g.Key, g.Count()))
            .OrderBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ---------------------------------------------------------- PREPARA DOMANDA

    /// <summary>
    /// Restituisce una DomandaPreparata con le risposte mescolate e l'indice della
    /// corretta ricalcolato rispetto al nuovo ordine.
    /// </summary>
    public static DomandaPreparata PreparaDomanda(Domanda d)
    {
        int idxCorretta = d.RispostaCorretta;
        string testoCorretto = (idxCorretta >= 0 && idxCorretta < d.Risposte.Count)
            ? d.Risposte[idxCorretta]
            : d.Risposte.FirstOrDefault() ?? "";

        var shuffled = d.Risposte.ToList();
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        int nuovoIdx = shuffled.IndexOf(testoCorretto);
        if (nuovoIdx < 0) nuovoIdx = 0; // fallback difensivo

        return new DomandaPreparata
        {
            Originale = d,
            RisposteShufflate = shuffled,
            IndiceCorrettoShufflato = nuovoIdx
        };
    }

    // ---------------------------------------------------------- PUNTEGGIO

    /// <summary>
    /// Punteggio semplice: 10 punti per ogni risposta corretta al primo tentativo,
    /// 4 per quelle corrette dopo qualche errore (modalità rotazione),
    /// 0 per quelle errate o non risposte.
    /// </summary>
    public static int CalcolaPunteggio(RisultatoQuiz r)
    {
        int punti = 0;
        foreach (var d in r.Dettagli)
        {
            if (!d.Corretta) continue;
            punti += d.Tentativi <= 1 ? 10 : 4;
        }
        return punti;
    }
}
