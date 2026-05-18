namespace Wsa.Quiz.Core.Models.Statistiche;

/// <summary>
/// Aggregato per una singola categoria all'interno di una materia.
/// </summary>
public record CategoriaStat(
    string Materia,
    string Categoria,
    int Visti,
    int Errori,
    double PctErrore,
    IReadOnlyList<DomandaSbagliata> Sbagliate);
