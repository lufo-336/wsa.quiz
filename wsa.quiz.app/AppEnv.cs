namespace Wsa.Quiz.App;

/// <summary>
/// Ambiente di esecuzione corrente, deciso una sola volta all'avvio in
/// <see cref="App.OnFrameworkInitializationCompleted"/>. Unico punto di verità
/// per distinguere desktop da touch (Android) senza #if sparsi nelle view.
/// </summary>
public static class AppEnv
{
    /// <summary>True su Android/single-view: abilita dimensioni e layout touch.</summary>
    public static bool TouchMode { get; set; }
}
