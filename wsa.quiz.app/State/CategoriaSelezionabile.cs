namespace Wsa.Quiz.App.State;

/// <summary>
/// Wrapper observable di una categoria appartenente a una specifica materia.
/// Conserva sia il nome "nudo" che la coppia <c>"Materia/Categoria"</c>
/// (utile per il salvataggio in cronologia, come da convenzione progetto).
/// </summary>
public class CategoriaSelezionabile : ObservableObject
{
    public string MateriaId { get; }
    public string MateriaNome { get; }
    public string Categoria { get; }
    public int NumDomande { get; }

    /// <summary>Chiave usata per salvare in <c>OpzioniQuiz.Categorie</c> (compatibile multi-materia).</summary>
    public string ChiaveCombinata => $"{MateriaNome}/{Categoria}";

    /// <summary>Etichetta mostrata a video, con conteggio domande.</summary>
    public string Etichetta => $"{Categoria}  ({NumDomande})";

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public CategoriaSelezionabile(string materiaId, string materiaNome, string categoria, int numDomande)
    {
        MateriaId = materiaId;
        MateriaNome = materiaNome;
        Categoria = categoria;
        NumDomande = numDomande;
    }
}
