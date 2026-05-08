using Wsa.Quiz.Core.Models;

namespace Wsa.Quiz.App.State;

/// <summary>
/// Wrapper observable di una <see cref="Materia"/> per la lista a checkbox della Home.
/// Mantiene anche il numero di domande disponibili per visualizzazione.
/// </summary>
public class MateriaSelezionabile : ObservableObject
{
    public Materia Materia { get; }
    public int NumDomande { get; }

    public string Nome => Materia.Nome;
    public string Id => Materia.Id;
    public string Etichetta => $"{Nome}  ({NumDomande})";

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public MateriaSelezionabile(Materia materia, int numDomande)
    {
        Materia = materia;
        NumDomande = numDomande;
    }
}
