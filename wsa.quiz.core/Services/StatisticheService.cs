using Wsa.Quiz.Core.Models;
using Wsa.Quiz.Core.Models.Statistiche;

namespace Wsa.Quiz.Core.Services;

/// <summary>
/// Aggrega la cronologia in una "mappa di calore" per categoria.
/// Funzione pura su <see cref="RisultatoQuiz"/>: non fa I/O.
/// </summary>
public class StatisticheService
{
    /// <summary>
    /// Calcola le statistiche per materia/categoria. Errore =
    /// !(Corretta &amp;&amp; Tentativi == 1): unifica modalita' classica
    /// e rotazione (in rotazione, "sbagliata al primo colpo" = Tentativi&gt;1).
    /// </summary>
    /// <param name="sessioni">Cronologia completa (anche abbandonati).</param>
    /// <param name="limiteUltimeN">Se valorizzato, considera solo le ultime N
    /// sessioni per DataOra decrescente.</param>
    public IReadOnlyList<StatistichePerMateria> Calcola(
        IEnumerable<RisultatoQuiz> sessioni,
        int? limiteUltimeN = null)
    {
        var filtrate = sessioni;
        if (limiteUltimeN is int n && n > 0)
        {
            filtrate = sessioni
                .OrderByDescending(s => s.DataOra)
                .Take(n);
        }

        // Tutti i dettagli, appiattiti.
        var dettagli = filtrate.SelectMany(s => s.Dettagli).ToList();
        if (dettagli.Count == 0) return Array.Empty<StatistichePerMateria>();

        // Raggruppa per (materia, categoria).
        var gruppi = dettagli
            .GroupBy(d => (d.MateriaNome, d.Categoria))
            .Select(g => CostruisciCategoriaStat(g.Key.MateriaNome, g.Key.Categoria, g.ToList()))
            .ToList();

        // Raggruppa per materia, ordina materie alfabetico, categorie per
        // PctErrore desc, poi alfabetico a parita'.
        return gruppi
            .GroupBy(c => c.Materia)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new StatistichePerMateria(
                g.Key,
                g.OrderByDescending(c => c.PctErrore)
                 .ThenBy(c => c.Categoria, StringComparer.OrdinalIgnoreCase)
                 .ToList()))
            .ToList();
    }

    private static CategoriaStat CostruisciCategoriaStat(
        string materia, string categoria, List<DettaglioRisposta> dettagli)
    {
        int visti = dettagli.Count;
        int errori = dettagli.Count(d => !(d.Corretta && d.Tentativi == 1));
        double pct = visti == 0 ? 0.0 : (double)errori / visti;

        var sbagliate = dettagli
            .Where(d => !(d.Corretta && d.Tentativi == 1))
            .GroupBy(d => d.IdDomanda)
            .Select(g =>
            {
                var ultimo = g.Last();
                int volteSbagliata = g.Count();
                int volteVista = dettagli.Count(d => d.IdDomanda == g.Key);
                return new DomandaSbagliata(
                    IdDomanda: g.Key,
                    Testo: ultimo.TestoDomanda,
                    RispostaCorretta: ultimo.RispostaCorretta,
                    Spiegazione: ultimo.Spiegazione,
                    VolteSbagliata: volteSbagliata,
                    VolteVista: volteVista);
            })
            .OrderByDescending(d => d.VolteSbagliata)
            .ThenBy(d => d.Testo, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new CategoriaStat(materia, categoria, visti, errori, pct, sbagliate);
    }
}
