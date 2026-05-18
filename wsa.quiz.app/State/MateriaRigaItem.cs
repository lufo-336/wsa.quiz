using System.Linq;
using Wsa.Quiz.Core.Models.Statistiche;

namespace Wsa.Quiz.App.State;

/// <summary>
/// Wrapper di riga heatmap: nome materia + lista celle categoria
/// gia' ordinate per debolezza.
/// </summary>
public class MateriaRigaItem
{
    public string Materia { get; }
    public IReadOnlyList<CategoriaCellaItem> Celle { get; }

    public MateriaRigaItem(StatistichePerMateria stat)
    {
        Materia = stat.Materia;
        Celle = stat.Categorie.Select(c => new CategoriaCellaItem(c)).ToList();
    }
}
