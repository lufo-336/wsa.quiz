namespace Wsa.Quiz.Core.Models.Statistiche;

/// <summary>
/// Aggregato di tutte le categorie di una materia, ordinate per debolezza
/// decrescente (calcolato da <see cref="Services.StatisticheService"/>).
/// </summary>
public record StatistichePerMateria(
    string Materia,
    IReadOnlyList<CategoriaStat> Categorie);
