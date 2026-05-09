# Step 5 — Eliminazione cronologia: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permettere l'eliminazione di singole partite e l'intera cronologia dalla GUI, dando un Id stabile a `RisultatoQuiz`.

**Architecture:** Tre layer di modifiche. `Wsa.Quiz.Core`: campo `Id` su `RisultatoQuiz`, generazione Guid in `SalvaRisultato`, migrazione lazy in `CaricaCronologia`, due nuovi metodi `EliminaRisultato`/`SvuotaCronologia`. `Wsa.Quiz.App` (Cronologia): bottone "Elimina" inline su riga (pattern Sospesi), bottone "Svuota cronologia" in header con dialog modale, bottone "Elimina questa partita" nel dettaglio. `Wsa.Quiz.Cli`: nessuna modifica.

**Tech Stack:** .NET 8, C# 12, Avalonia 12.0.2, code-behind con `INotifyPropertyChanged`.

**Note sul testing:** Il progetto **non** ha un'infrastruttura di test automatici. Le verifiche di ogni task sono `dotnet build` + verifica manuale con i comandi e gli step descritti. Il piano segue comunque la regola "passi piccoli, commit frequenti".

**Spec di riferimento:** `docs/superpowers/specs/2026-05-09-step5-eliminazione-cronologia-design.md`

**Comando di build per verifica continua:**
```powershell
dotnet build Wsa.Quiz.sln
```
Atteso a ogni task: `Build succeeded.` con 0 errori.

---

## Task 1: Aggiungere il campo Id a RisultatoQuiz

**Files:**
- Modify: `wsa.quiz.core/Models/RisultatoQuiz.cs:6-22`

- [ ] **Step 1: Aggiungere la proprietà `Id` come prima proprietà del modello**

Apri `wsa.quiz.core/Models/RisultatoQuiz.cs` e modifica la classe in:

```csharp
namespace Wsa.Quiz.Core.Models;

/// <summary>
/// Risultato completo di una sessione di quiz. Persistito in cronologia.json.
/// </summary>
public class RisultatoQuiz
{
    /// <summary>
    /// Id stabile generato in <see cref="Services.StorageService.SalvaRisultato"/>.
    /// I record vecchi senza Id ricevono un Guid generato al primo
    /// <see cref="Services.StorageService.CaricaCronologia"/> che li incontra
    /// (migrazione lazy una-tantum).
    /// </summary>
    public string Id { get; set; } = "";

    public DateTime DataOra { get; set; }
    public string Modalita { get; set; } = string.Empty;
    public string MateriaNome { get; set; } = string.Empty;
    public List<string> CategorieSelezionate { get; set; } = new();
    public int TotaleDomande { get; set; }
    public int RisposteCorrette { get; set; }
    public int RisposteErrate { get; set; }
    public double PercentualeCorrette { get; set; }
    public TimeSpan DurataQuiz { get; set; }
    public bool Abbandonato { get; set; }
    public bool ModalitaRotazione { get; set; }
    public bool CronometroAttivo { get; set; }
    public int Punteggio { get; set; }
    public List<DettaglioRisposta> Dettagli { get; set; } = new();
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build Wsa.Quiz.sln
```
Atteso: `Build succeeded.` con 0 errori. (Aggiungere una proprietà non causa breakage perché ha default `""`.)

- [ ] **Step 3: Commit**

```powershell
git add wsa.quiz.core/Models/RisultatoQuiz.cs
git commit -m "feat(step5): add Id field to RisultatoQuiz"
```

---

## Task 2: Generare Guid in SalvaRisultato

**Files:**
- Modify: `wsa.quiz.core/Services/StorageService.cs:224-230`

- [ ] **Step 1: Modificare `SalvaRisultato` per generare l'Id se mancante**

Sostituisci il metodo `SalvaRisultato` (riga 224 circa):

```csharp
public void SalvaRisultato(RisultatoQuiz risultato)
{
    if (string.IsNullOrEmpty(risultato.Id))
        risultato.Id = Guid.NewGuid().ToString();

    var cronologia = CaricaCronologia();
    cronologia.Add(risultato);
    string json = JsonSerializer.Serialize(cronologia, OpzioniScrittura);
    File.WriteAllText(_fileCronologia, json);
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build Wsa.Quiz.sln
```
Atteso: `Build succeeded.` con 0 errori.

- [ ] **Step 3: Verifica manuale che i nuovi quiz ricevano un Id**

Sposta temporaneamente il file di cronologia esistente (per non sporcarlo durante il test):

```powershell
$f = Join-Path $env:APPDATA "WsaQuiz\cronologia.json"
if (Test-Path $f) { Move-Item $f "$f.bak-step5-task2" }
```

Esegui un quiz veloce dalla console (4-5 domande, anche abbandonato va bene):

```powershell
dotnet run --project wsa.quiz.cli
```

A quiz finito (o abbandonato), apri il nuovo file:

```powershell
Get-Content (Join-Path $env:APPDATA "WsaQuiz\cronologia.json") -Raw
```

Atteso: il record contiene un campo `"Id": "<guid>"` non vuoto, formato `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`.

Ripristina il file originale per il prossimo task:

```powershell
$f = Join-Path $env:APPDATA "WsaQuiz\cronologia.json"
Remove-Item $f
if (Test-Path "$f.bak-step5-task2") { Move-Item "$f.bak-step5-task2" $f }
```

- [ ] **Step 4: Commit**

```powershell
git add wsa.quiz.core/Services/StorageService.cs
git commit -m "feat(step5): generate Guid in SalvaRisultato when Id missing"
```

---

## Task 3: Migrazione lazy in CaricaCronologia

**Files:**
- Modify: `wsa.quiz.core/Services/StorageService.cs:209-222`

- [ ] **Step 1: Sostituire `CaricaCronologia` con la versione che migra al volo**

Sostituisci il metodo `CaricaCronologia` (riga 209 circa):

```csharp
public List<RisultatoQuiz> CaricaCronologia()
{
    if (!File.Exists(_fileCronologia)) return new List<RisultatoQuiz>();
    List<RisultatoQuiz> lista;
    try
    {
        string json = File.ReadAllText(_fileCronologia);
        lista = JsonSerializer.Deserialize<List<RisultatoQuiz>>(json, OpzioniLettura) ?? new();
    }
    catch
    {
        // File corrotto: non blocco l'app, ricomincio da lista vuota.
        return new List<RisultatoQuiz>();
    }

    // Migrazione lazy una-tantum: i record vecchi senza Id ne ricevono uno
    // e il file viene riscritto. Idempotente: dalla seconda chiamata in poi e' un no-op.
    bool serveRiscrittura = false;
    foreach (var r in lista)
    {
        if (string.IsNullOrEmpty(r.Id))
        {
            r.Id = Guid.NewGuid().ToString();
            serveRiscrittura = true;
        }
    }
    if (serveRiscrittura)
    {
        try
        {
            string json = JsonSerializer.Serialize(lista, OpzioniScrittura);
            File.WriteAllText(_fileCronologia, json);
        }
        catch
        {
            // Se la riscrittura fallisce (file in lock, disco pieno), restituisco
            // comunque la lista in memoria con gli Id assegnati: l'app continua a
            // funzionare, e al prossimo caricamento riproveremo la migrazione.
        }
    }
    return lista;
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build Wsa.Quiz.sln
```
Atteso: `Build succeeded.` con 0 errori.

- [ ] **Step 3: Verifica manuale della migrazione su un file esistente senza Id**

Se hai una cronologia con record vecchi (probabile: ci sono partite di test pre-step5), apri direttamente il file:

```powershell
$f = Join-Path $env:APPDATA "WsaQuiz\cronologia.json"
Get-Content $f -Raw | Select-Object -First 1
```

Se non vedi campi `"Id"` su tutti i record, fai una copia di sicurezza:

```powershell
Copy-Item $f "$f.bak-step5-task3"
```

Avvia la GUI (anche solo per aprire la tab Cronologia, è sufficiente):

```powershell
dotnet run --project wsa.quiz.app
```

Chiudi l'app. Riapri il file:

```powershell
Get-Content (Join-Path $env:APPDATA "WsaQuiz\cronologia.json") -Raw
```

Atteso: ogni record ora ha un `"Id"` valorizzato con un Guid. Il numero di record è invariato.

- [ ] **Step 4: Verifica idempotenza**

Riapri la GUI, chiudila. Il file non deve essere riscritto inutilmente: confronta la dimensione/timestamp con quello dopo il primo run (in pratica: se è già stato migrato una volta, `serveRiscrittura` resta `false` e non si fa I/O di scrittura).

Verifica veloce:

```powershell
$before = (Get-Item (Join-Path $env:APPDATA "WsaQuiz\cronologia.json")).LastWriteTime
dotnet run --project wsa.quiz.app
# chiudi l'app
$after = (Get-Item (Join-Path $env:APPDATA "WsaQuiz\cronologia.json")).LastWriteTime
"$before -> $after"
```

Atteso: `LastWriteTime` invariato (la GUI non riscrive il file in assenza di modifiche).

- [ ] **Step 5: Commit**

```powershell
git add wsa.quiz.core/Services/StorageService.cs
git commit -m "feat(step5): migrate legacy cronologia records to give them an Id"
```

---

## Task 4: Aggiungere EliminaRisultato e SvuotaCronologia

**Files:**
- Modify: `wsa.quiz.core/Services/StorageService.cs` (aggiungere dopo `SalvaRisultato`, prima della sezione PAUSA)

- [ ] **Step 1: Aggiungere i due nuovi metodi**

Subito dopo il metodo `SalvaRisultato` e prima del commento `// ------ PAUSA SESSIONE`, aggiungi:

```csharp
/// <summary>
/// Elimina il risultato con l'Id specificato dalla cronologia.
/// No-op silenzioso se l'Id non esiste o se il file non c'e' (es. cronologia mai creata).
/// </summary>
public void EliminaRisultato(string id)
{
    if (string.IsNullOrEmpty(id)) return;
    if (!File.Exists(_fileCronologia)) return;

    var cronologia = CaricaCronologia();
    int rimossi = cronologia.RemoveAll(r => r.Id == id);
    if (rimossi == 0) return;

    string json = JsonSerializer.Serialize(cronologia, OpzioniScrittura);
    File.WriteAllText(_fileCronologia, json);
}

/// <summary>
/// Svuota completamente la cronologia. Scrive una lista vuota nel file
/// (non lo elimina) per evitare race con scritture concorrenti successive
/// e per coerenza con il pattern di <see cref="CaricaCronologia"/>.
/// </summary>
public void SvuotaCronologia()
{
    string json = JsonSerializer.Serialize(new List<RisultatoQuiz>(), OpzioniScrittura);
    File.WriteAllText(_fileCronologia, json);
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build Wsa.Quiz.sln
```
Atteso: `Build succeeded.` con 0 errori.

- [ ] **Step 3: Commit**

```powershell
git add wsa.quiz.core/Services/StorageService.cs
git commit -m "feat(step5): add EliminaRisultato and SvuotaCronologia to StorageService"
```

---

## Task 5: Esporre Id e InAttesaConfermaEliminazione su RisultatoCronologiaItem

**Files:**
- Modify: `wsa.quiz.app/State/RisultatoCronologiaItem.cs:12-49`

- [ ] **Step 1: Convertire in `ObservableObject` e aggiungere lo stato di conferma**

`RisultatoCronologiaItem` oggi è una classe semplice (non observable). Convertila estendendo `ObservableObject` e aggiungendo le due nuove proprietà.

Sostituisci l'intero contenuto del file:

```csharp
using Avalonia.Media;
using Wsa.Quiz.Core.Models;

namespace Wsa.Quiz.App.State;

/// <summary>
/// Wrapper di una riga della tabella cronologia. Espone proprieta' gia' formattate
/// per il binding XAML (data, durata, etichetta materie, colore della percentuale).
/// E' observable per supportare la conferma inline dell'azione Elimina:
/// <see cref="InAttesaConfermaEliminazione"/> commuta i bottoni della riga
/// fra "Elimina" e "Si', elimina/Annulla" (stesso pattern di <see cref="SessioneSospesaItem"/>).
/// </summary>
public class RisultatoCronologiaItem : ObservableObject
{
    /// <summary>Riferimento al risultato originale, usato per aprire il dettaglio e per Elimina.</summary>
    public RisultatoQuiz Risultato { get; }

    /// <summary>Id del risultato, propagato per comodita' sui binding/handler.</summary>
    public string Id => Risultato.Id;

    public string DataFormattata { get; }
    public string Modalita { get; }
    public string MateriaEtichetta { get; }
    public string PercentualeFormattata { get; }
    public IBrush ColorePercentuale { get; }
    public string DurataFormattata { get; }
    public string Conteggio { get; }
    public bool Abbandonato { get; }
    public string StatoEtichetta { get; }

    // ------------------------------------------------------------------ STATO CONFERMA ELIMINA

    private bool _inAttesaConferma;
    public bool InAttesaConfermaEliminazione
    {
        get => _inAttesaConferma;
        set
        {
            if (SetField(ref _inAttesaConferma, value))
                RaisePropertyChanged(nameof(NonInAttesaConferma));
        }
    }

    /// <summary>Inverso di <see cref="InAttesaConfermaEliminazione"/>, comodo per l'XAML.</summary>
    public bool NonInAttesaConferma => !_inAttesaConferma;

    // ------------------------------------------------------------------ COSTRUZIONE

    public RisultatoCronologiaItem(RisultatoQuiz r)
    {
        Risultato = r;

        DataFormattata = r.DataOra.ToString("dd MMM yyyy · HH:mm");

        Modalita = string.IsNullOrWhiteSpace(r.Modalita) ? "—" : r.Modalita;

        MateriaEtichetta = string.IsNullOrWhiteSpace(r.MateriaNome) ? "—" : r.MateriaNome;

        PercentualeFormattata = $"{System.Math.Round(r.PercentualeCorrette)}%";
        ColorePercentuale = ColoreDaPercentuale(r.PercentualeCorrette);

        var d = r.DurataQuiz;
        DurataFormattata = d.TotalHours >= 1
            ? $"{(int)d.TotalHours}:{d.Minutes:00}:{d.Seconds:00}"
            : $"{d.Minutes:00}:{d.Seconds:00}";

        Conteggio = $"{r.RisposteCorrette} / {r.TotaleDomande}";

        Abbandonato = r.Abbandonato;
        StatoEtichetta = r.Abbandonato ? "Abbandonato" : "Concluso";
    }

    private static IBrush ColoreDaPercentuale(double pct)
    {
        // Stessa scala di RiepilogoView: verde >=80, ambra 50-79, rosso <50.
        if (pct >= 80) return new SolidColorBrush(Color.Parse("#1F7A4D"));
        if (pct >= 50) return new SolidColorBrush(Color.Parse("#B8860B"));
        return new SolidColorBrush(Color.Parse("#B85450"));
    }
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build Wsa.Quiz.sln
```
Atteso: `Build succeeded.` con 0 errori.

- [ ] **Step 3: Commit**

```powershell
git add wsa.quiz.app/State/RisultatoCronologiaItem.cs
git commit -m "feat(step5): make RisultatoCronologiaItem observable for inline delete"
```

---

## Task 6: Aggiungere bottoni Elimina/conferma sulla riga cronologia (XAML)

**Files:**
- Modify: `wsa.quiz.app/Views/CronologiaView.axaml:68-156` (header colonne + template riga)

- [ ] **Step 1: Aggiornare l'header colonne per includere la colonna azioni**

Nell'header colonne (riga 68 circa) cambia la `Grid.ColumnDefinitions` da `Auto,180,*,90,90,90` a `Auto,180,*,90,90,90,160` e aggiungi una settima cella vuota:

```xml
<Grid ColumnDefinitions="Auto,180,*,90,90,90,160">
    <TextBlock Text="" Width="6"/>
    <TextBlock Grid.Column="1" Text="Data"
               FontSize="11" FontWeight="SemiBold"
               Foreground="{DynamicResource SystemBaseMediumColor}"/>
    <TextBlock Grid.Column="2" Text="Modalita'"
               FontSize="11" FontWeight="SemiBold"
               Foreground="{DynamicResource SystemBaseMediumColor}"/>
    <TextBlock Grid.Column="3" Text="%"
               FontSize="11" FontWeight="SemiBold"
               Foreground="{DynamicResource SystemBaseMediumColor}"
               HorizontalAlignment="Right"/>
    <TextBlock Grid.Column="4" Text="Risposte"
               FontSize="11" FontWeight="SemiBold"
               Foreground="{DynamicResource SystemBaseMediumColor}"
               HorizontalAlignment="Right"/>
    <TextBlock Grid.Column="5" Text="Durata"
               FontSize="11" FontWeight="SemiBold"
               Foreground="{DynamicResource SystemBaseMediumColor}"
               HorizontalAlignment="Right"/>
    <TextBlock Grid.Column="6" Text="" />
</Grid>
```

- [ ] **Step 2: Aggiornare il `DataTemplate` della riga: stessa colonna 7 con i bottoni**

Trova il `DataTemplate DataType="state:RisultatoCronologiaItem"` (riga 109 circa) e sostituisci il `<Grid>` interno (intero contenuto del DataTemplate) con:

```xml
<Grid ColumnDefinitions="6,180,*,90,90,90,160"
      Margin="0,2">
    <Border Grid.Column="0"
            Width="6"
            Background="{Binding ColorePercentuale}"
            CornerRadius="3"/>

    <StackPanel Grid.Column="1" Spacing="2" Margin="8,2,4,2">
        <TextBlock Text="{Binding DataFormattata}"
                   FontSize="13" FontWeight="SemiBold"/>
        <TextBlock Text="Abbandonato"
                   FontSize="10"
                   Foreground="#B85450"
                   IsVisible="{Binding Abbandonato}"/>
    </StackPanel>

    <StackPanel Grid.Column="2" Spacing="2" VerticalAlignment="Center">
        <TextBlock Text="{Binding Modalita}"
                   TextWrapping="NoWrap"
                   TextTrimming="CharacterEllipsis"/>
        <TextBlock Text="{Binding MateriaEtichetta}"
                   FontSize="11"
                   Foreground="{DynamicResource SystemBaseMediumColor}"
                   TextTrimming="CharacterEllipsis"/>
    </StackPanel>

    <TextBlock Grid.Column="3"
               Text="{Binding PercentualeFormattata}"
               FontSize="14" FontWeight="SemiBold"
               Foreground="{Binding ColorePercentuale}"
               HorizontalAlignment="Right"
               VerticalAlignment="Center"/>

    <TextBlock Grid.Column="4"
               Text="{Binding Conteggio}"
               FontSize="12"
               HorizontalAlignment="Right"
               VerticalAlignment="Center"/>

    <TextBlock Grid.Column="5"
               Text="{Binding DurataFormattata}"
               FontSize="12"
               HorizontalAlignment="Right"
               VerticalAlignment="Center"/>

    <!-- Bottoni: stato normale -->
    <StackPanel Grid.Column="6"
                Orientation="Horizontal"
                Spacing="6"
                HorizontalAlignment="Right"
                VerticalAlignment="Center"
                Margin="8,0,0,0"
                IsVisible="{Binding NonInAttesaConferma}">
        <Button Content="Elimina"
                Click="OnEliminaRigaClick"
                Classes="danger"
                Padding="12,4"
                FontSize="11"/>
    </StackPanel>

    <!-- Bottoni: stato conferma -->
    <StackPanel Grid.Column="6"
                Orientation="Horizontal"
                Spacing="6"
                HorizontalAlignment="Right"
                VerticalAlignment="Center"
                Margin="8,0,0,0"
                IsVisible="{Binding InAttesaConfermaEliminazione}">
        <Button Content="Si', elimina"
                Click="OnConfermaEliminaRigaClick"
                Classes="danger"
                Padding="10,4"
                FontSize="11"/>
        <Button Content="Annulla"
                Click="OnAnnullaEliminaRigaClick"
                Padding="10,4"
                FontSize="11"/>
    </StackPanel>
</Grid>
```

- [ ] **Step 3: Build**

```powershell
dotnet build Wsa.Quiz.sln
```
Atteso: errori di compilazione su `OnEliminaRigaClick`, `OnConfermaEliminaRigaClick`, `OnAnnullaEliminaRigaClick` perché non esistono ancora nel code-behind. Procedere comunque al Task 7 che li definisce.

⚠️ Se invece compare un errore XAML come "could not resolve property X on type System.Object", controlla che il `DataType` del `DataTemplate` sia ancora `state:RisultatoCronologiaItem`.

---

## Task 7: Aggiungere handler per Elimina riga (code-behind)

**Files:**
- Modify: `wsa.quiz.app/Views/CronologiaView.axaml.cs` (aggiungere handler dopo `OnSessioneDoubleTapped`)

- [ ] **Step 1: Aggiungere i tre handler**

Nel file `CronologiaView.axaml.cs`, subito dopo il metodo `OnSessioneDoubleTapped` (riga 119 circa) e prima di `ApriDettaglio`, aggiungi:

```csharp
/// <summary>Primo click su "Elimina" sulla riga: entra in stato di conferma per quella riga.</summary>
private void OnEliminaRigaClick(object? sender, RoutedEventArgs e)
{
    if (sender is not Button b) return;
    if (b.DataContext is not RisultatoCronologiaItem item) return;
    // azzera eventuali altre conferme aperte (al massimo una alla volta, come SospesiView)
    foreach (var s in Sessioni) s.InAttesaConfermaEliminazione = false;
    item.InAttesaConfermaEliminazione = true;
}

/// <summary>Conferma definitiva: elimina dal disco e dalla lista.</summary>
private void OnConfermaEliminaRigaClick(object? sender, RoutedEventArgs e)
{
    if (sender is not Button b) return;
    if (b.DataContext is not RisultatoCronologiaItem item) return;
    if (_storage == null) return;

    try
    {
        _storage.EliminaRisultato(item.Id);
    }
    catch (Exception ex)
    {
        Sottotitolo = $"Errore nell'eliminazione: {ex.Message}";
        return;
    }

    Sessioni.Remove(item);
    NessunaSessione = Sessioni.Count == 0;
    Sottotitolo = NessunaSessione
        ? "Nessuna sessione registrata."
        : $"{Sessioni.Count} sessioni registrate.";
}

/// <summary>Annulla la conferma e torna al layout normale della riga.</summary>
private void OnAnnullaEliminaRigaClick(object? sender, RoutedEventArgs e)
{
    if (sender is not Button b) return;
    if (b.DataContext is not RisultatoCronologiaItem item) return;
    item.InAttesaConfermaEliminazione = false;
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build Wsa.Quiz.sln
```
Atteso: `Build succeeded.` con 0 errori.

- [ ] **Step 3: Verifica manuale dell'eliminazione di una riga**

```powershell
dotnet run --project wsa.quiz.app
```

In GUI:
1. Vai alla tab Cronologia.
2. Su una riga, click "Elimina" → la riga deve mostrare "Sì, elimina" + "Annulla".
3. Click "Annulla" → la riga torna al normale ("Elimina").
4. Click "Elimina" su una **riga A**, poi su una **riga B**: A deve tornare normale, solo B in conferma.
5. Click "Sì, elimina" su una riga → la riga sparisce, il sottotitolo si aggiorna ("N sessioni registrate.").
6. Chiudi e riapri l'app: la riga eliminata non torna.

⚠️ **Se cliccando "Elimina" si apre anche il dettaglio**: il bottone sta propagando l'evento al `ListBox`. In Avalonia 12 il `Button` non dovrebbe propagare il click al contenitore di default, ma se succede aggiungi `e.Handled = true` come prima riga di `OnEliminaRigaClick`. Verifica anche che il doppio-click sulla riga (non sul bottone) apra il dettaglio come prima.

- [ ] **Step 4: Commit**

```powershell
git add wsa.quiz.app/Views/CronologiaView.axaml wsa.quiz.app/Views/CronologiaView.axaml.cs
git commit -m "feat(step5): inline delete button on cronologia row"
```

---

## Task 8: Aggiungere bottone "Svuota cronologia" nell'header

**Files:**
- Modify: `wsa.quiz.app/Views/CronologiaView.axaml:14-32` (header)

- [ ] **Step 1: Aggiungere il bottone accanto ad "Aggiorna"**

Trova la `Grid` dell'header (riga 14 circa). Cambia `ColumnDefinitions="*,Auto,Auto"` (già fatto, è già così) e aggiungi un secondo `Button` nell'`Grid.Column="2"`:

```xml
<Grid DockPanel.Dock="Top"
      ColumnDefinitions="*,Auto,Auto"
      Margin="20,16,20,8">
    <StackPanel Spacing="2">
        <TextBlock Text="Cronologia"
                   FontSize="22" FontWeight="SemiBold"/>
        <TextBlock Text="{Binding Sottotitolo}"
                   FontSize="12"
                   Foreground="{DynamicResource SystemBaseMediumColor}"/>
    </StackPanel>

    <Button Grid.Column="1"
            Content="Aggiorna"
            Click="OnAggiornaClick"
            Padding="14,6"
            FontSize="12"
            Margin="0,0,8,0"
            ToolTip.Tip="Ricarica la cronologia dal disco. Utile se hai fatto un quiz dal console mentre l'app era aperta."/>

    <Button Grid.Column="2"
            Content="Svuota cronologia"
            Click="OnSvuotaCronologiaClick"
            Classes="danger"
            Padding="14,6"
            FontSize="12"
            IsEnabled="{Binding !NessunaSessione}"
            ToolTip.Tip="Elimina TUTTE le sessioni dalla cronologia. Verra' chiesta conferma."/>
</Grid>
```

- [ ] **Step 2: Build**

```powershell
dotnet build Wsa.Quiz.sln
```
Atteso: errori sull'handler `OnSvuotaCronologiaClick` ancora non definito. Procedere al Task 9.

---

## Task 9: Dialog modale + handler "Svuota cronologia"

**Files:**
- Modify: `wsa.quiz.app/Views/CronologiaView.axaml.cs` (aggiungere `OnSvuotaCronologiaClick` e il metodo dialog)

- [ ] **Step 1: Aggiungere gli `using` necessari per la finestra modale**

In cima al file, dopo gli `using` esistenti, assicurati che ci siano:

```csharp
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Wsa.Quiz.App.State;
using Wsa.Quiz.Core.Services;
```

(Sono già tutti presenti — è solo una conferma; non rimuovere niente.)

- [ ] **Step 2: Aggiungere handler e dialog**

In fondo alla classe `CronologiaView`, prima del metodo `Raise` (INPC), aggiungi:

```csharp
private void OnSvuotaCronologiaClick(object? sender, RoutedEventArgs e)
{
    ChiediConfermaSvuotaCronologia();
}

/// <summary>
/// Dialog modale di conferma per "Svuota cronologia". Stesso pattern di
/// <c>QuizView.ChiediConfermaAbbandono</c>: Window 420x180, CenterOwner,
/// no resize, no taskbar. ESC chiude come Annulla.
/// </summary>
private async void ChiediConfermaSvuotaCronologia()
{
    if (_storage == null) return;

    int n = Sessioni.Count;
    var w = new Window
    {
        Title = "Svuota cronologia",
        Width = 420,
        Height = 180,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        CanResize = false,
        ShowInTaskbar = false
    };

    bool conferma = false;
    var contenuto = new StackPanel
    {
        Margin = new global::Avalonia.Thickness(20),
        Spacing = 14
    };
    contenuto.Children.Add(new TextBlock
    {
        Text = "Svuota tutta la cronologia?",
        FontSize = 14,
        FontWeight = global::Avalonia.Media.FontWeight.SemiBold
    });
    contenuto.Children.Add(new TextBlock
    {
        Text = $"Verranno eliminate {n} partite dalla cronologia. L'azione non e' reversibile.",
        FontSize = 12,
        TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
        Foreground = global::Avalonia.Media.Brushes.DimGray
    });

    var pulsantiera = new StackPanel
    {
        Orientation = global::Avalonia.Layout.Orientation.Horizontal,
        Spacing = 8,
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right
    };
    var annullaBtn = new Button
    {
        Content = "Annulla",
        Padding = new global::Avalonia.Thickness(14, 5)
    };
    var siBtn = new Button
    {
        Content = "Si', cancella tutto",
        Padding = new global::Avalonia.Thickness(14, 5)
    };
    siBtn.Classes.Add("danger");
    annullaBtn.Click += (_, _) => w.Close();
    siBtn.Click += (_, _) => { conferma = true; w.Close(); };

    // ESC = Annulla (stesso pattern del menu pausa)
    w.KeyDown += (_, ev) =>
    {
        if (ev.Key == Key.Escape)
        {
            ev.Handled = true;
            w.Close();
        }
    };

    pulsantiera.Children.Add(annullaBtn);
    pulsantiera.Children.Add(siBtn);
    contenuto.Children.Add(pulsantiera);
    w.Content = contenuto;

    var owner = TopLevel.GetTopLevel(this) as Window;
    if (owner != null)
        await w.ShowDialog(owner);
    else
        w.Show();

    if (!conferma) return;

    try
    {
        _storage.SvuotaCronologia();
    }
    catch (Exception ex)
    {
        Sottotitolo = $"Errore nello svuotamento: {ex.Message}";
        return;
    }

    Ricarica();
}
```

- [ ] **Step 3: Build**

```powershell
dotnet build Wsa.Quiz.sln
```
Atteso: `Build succeeded.` con 0 errori.

- [ ] **Step 4: Verifica manuale**

```powershell
dotnet run --project wsa.quiz.app
```

In GUI, tab Cronologia:
1. Verifica che il bottone "Svuota cronologia" sia visibile in alto a destra.
2. Verifica che sia disabilitato se la lista è vuota (per testarlo, prima fai dei quiz veloci se la cronologia è popolata, oppure usa un file vuoto).
3. Click "Svuota cronologia" → dialog con il numero N corretto e testo "L'azione non è reversibile.".
4. ESC nel dialog → si chiude senza svuotare.
5. Click "Annulla" → idem.
6. Riapri il dialog → click "Sì, cancella tutto" → la lista si svuota, lo stato vuoto compare, il bottone "Svuota cronologia" diventa disabilitato.
7. Chiudi e riapri l'app: la cronologia resta vuota.

- [ ] **Step 5: Commit**

```powershell
git add wsa.quiz.app/Views/CronologiaView.axaml wsa.quiz.app/Views/CronologiaView.axaml.cs
git commit -m "feat(step5): clear-all button with modal confirm in cronologia header"
```

---

## Task 10: Bottone "Elimina questa partita" nel dettaglio

**Files:**
- Modify: `wsa.quiz.app/Views/CronologiaDettaglioView.axaml:192-201` (top bar e bottom bar)
- Modify: `wsa.quiz.app/Views/CronologiaDettaglioView.axaml.cs`

- [ ] **Step 1: Sostituire il bottone in basso "Torna alla cronologia" con una pulsantiera**

Nel file `CronologiaDettaglioView.axaml`, trova il `<Button Grid.Row="3" Content="Torna alla cronologia" ... />` (riga 192 circa). Sostituiscilo con uno `StackPanel` orizzontale che contiene il bottone Elimina (a sinistra) e il bottone Indietro (a destra), con stato di conferma swappabile via XAML:

```xml
<!-- BARRA IN BASSO -->
<Grid Grid.Row="3"
      ColumnDefinitions="Auto,Auto,*,Auto"
      Margin="0,16,0,0">

    <!-- Stato normale -->
    <Button Grid.Column="0"
            Content="Elimina questa partita"
            Click="OnEliminaClick"
            Classes="danger"
            Padding="14,8"
            FontSize="12"
            IsVisible="{Binding NonInAttesaConferma}"/>

    <!-- Stato conferma -->
    <StackPanel Grid.Column="0" Grid.ColumnSpan="2"
                Orientation="Horizontal"
                Spacing="6"
                VerticalAlignment="Center"
                IsVisible="{Binding InAttesaConfermaEliminazione}">
        <TextBlock Text="Sicuro?"
                   VerticalAlignment="Center"
                   FontSize="12"
                   Foreground="#B85450"
                   FontWeight="SemiBold"
                   Margin="0,0,4,0"/>
        <Button Content="Si', elimina"
                Click="OnConfermaEliminaClick"
                Classes="danger"
                Padding="14,6"
                FontSize="12"/>
        <Button Content="Annulla"
                Click="OnAnnullaEliminaClick"
                Padding="14,6"
                FontSize="12"/>
    </StackPanel>

    <Button Grid.Column="3"
            Content="Torna alla cronologia"
            Click="OnIndietroClick"
            Classes="accent"
            Padding="22,10"
            FontSize="14"
            FontWeight="SemiBold"/>
</Grid>
```

- [ ] **Step 2: Aggiungere stato observable + evento "Elimina richiesto" nel code-behind**

Apri `wsa.quiz.app/Views/CronologiaDettaglioView.axaml.cs`. La classe ha già `INotifyPropertyChanged` (riga 20). Aggiungi:

A. Un nuovo evento accanto a `IndietroRichiesto`:

```csharp
/// <summary>Sollevato quando l'utente conferma l'eliminazione di questa partita
/// dal dettaglio. La <see cref="CronologiaView"/> esegue l'eliminazione su storage,
/// torna alla lista e ricarica.</summary>
public event EventHandler<string>? EliminazioneRichiesta;
```

B. Le due proprietà di stato conferma (sotto `Modalita`/`MateriaEtichetta`, prima di `Dettagli`):

```csharp
// ------------------------------------------------------------------ STATO CONFERMA ELIMINA

private bool _inAttesaConferma;
public bool InAttesaConfermaEliminazione
{
    get => _inAttesaConferma;
    private set
    {
        if (_inAttesaConferma == value) return;
        _inAttesaConferma = value;
        Raise();
        Raise(nameof(NonInAttesaConferma));
    }
}

public bool NonInAttesaConferma => !_inAttesaConferma;
```

C. I tre handler accanto a `OnIndietroClick`:

```csharp
private void OnEliminaClick(object? sender, RoutedEventArgs e)
{
    InAttesaConfermaEliminazione = true;
}

private void OnAnnullaEliminaClick(object? sender, RoutedEventArgs e)
{
    InAttesaConfermaEliminazione = false;
}

private void OnConfermaEliminaClick(object? sender, RoutedEventArgs e)
{
    EliminazioneRichiesta?.Invoke(this, _risultato.Id);
}
```

D. Aggiungi il metodo `Raise` (l'attuale code-behind ha `PropertyChanged` ma non un helper `Raise`). Subito prima della chiusura della classe:

```csharp
private void Raise([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
```

- [ ] **Step 3: Build**

```powershell
dotnet build Wsa.Quiz.sln
```
Atteso: `Build succeeded.` con 0 errori.

- [ ] **Step 4: Commit (parziale, l'integrazione con CronologiaView arriva al Task 11)**

```powershell
git add wsa.quiz.app/Views/CronologiaDettaglioView.axaml wsa.quiz.app/Views/CronologiaDettaglioView.axaml.cs
git commit -m "feat(step5): inline delete button in cronologia detail view"
```

---

## Task 11: CronologiaView gestisce l'evento di eliminazione dal dettaglio

**Files:**
- Modify: `wsa.quiz.app/Views/CronologiaView.axaml.cs:121-133` (`ApriDettaglio` e `OnIndietroDalDettaglio`)

- [ ] **Step 1: Sottoscrivere il nuovo evento e gestirlo**

Nel file `CronologiaView.axaml.cs`, modifica `ApriDettaglio` per sottoscrivere anche `EliminazioneRichiesta`:

```csharp
private void ApriDettaglio(RisultatoCronologiaItem item)
{
    var dettaglio = new CronologiaDettaglioView(item.Risultato);
    dettaglio.IndietroRichiesto += OnIndietroDalDettaglio;
    dettaglio.EliminazioneRichiesta += OnEliminaDalDettaglio;
    DettaglioArea.Content = dettaglio;
    ModoDettaglio = true;
}
```

E aggiungi il nuovo handler subito sotto `OnIndietroDalDettaglio`:

```csharp
private void OnEliminaDalDettaglio(object? sender, string id)
{
    if (_storage == null) return;
    try
    {
        _storage.EliminaRisultato(id);
    }
    catch (Exception ex)
    {
        Sottotitolo = $"Errore nell'eliminazione: {ex.Message}";
        return;
    }

    // chiudi il dettaglio e torna alla lista, ricaricata da disco
    DettaglioArea.Content = null;
    ModoDettaglio = false;
    Ricarica();
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build Wsa.Quiz.sln
```
Atteso: `Build succeeded.` con 0 errori.

- [ ] **Step 3: Verifica manuale**

```powershell
dotnet run --project wsa.quiz.app
```

In GUI, tab Cronologia:
1. Doppio click su una riga → si apre il dettaglio.
2. In basso a sinistra deve esserci "Elimina questa partita".
3. Click → swap a "Sì, elimina" + "Annulla". Click "Annulla" → torna al bottone normale.
4. Click di nuovo "Elimina questa partita" → "Sì, elimina" → torno alla lista, la partita non c'è più. Sottotitolo aggiornato.
5. "Torna alla cronologia" funziona ancora normalmente sulle altre partite.

- [ ] **Step 4: Commit**

```powershell
git add wsa.quiz.app/Views/CronologiaView.axaml.cs
git commit -m "feat(step5): handle delete-from-detail event in CronologiaView"
```

---

## Task 12: Test di accettazione end-to-end

Esegui tutti i test manuali della spec in un'unica passata, sull'app costruita dall'ultima HEAD.

**Files:** nessuno

- [ ] **Step 1: Build pulita**

```powershell
dotnet build Wsa.Quiz.sln
```
Atteso: `Build succeeded.`, 0 warning relativi al nostro codice.

- [ ] **Step 2: Esegui la lista di test della spec**

Apri la GUI:

```powershell
dotnet run --project wsa.quiz.app
```

Spunta una a una:

1. **Migrazione**: file `cronologia.json` esistente con record vecchi → tab Cronologia → niente errori → ogni record ha ora un campo `"Id"` (controllabile aprendo il file dopo).
2. **Elimina riga**: click "Elimina" su una riga → swap a conferma. "Annulla" ripristina. Nuovo click "Elimina" + "Sì, elimina" rimuove la riga.
3. **Conferma esclusiva**: aprire conferma su riga A, poi su riga B → la conferma di A si chiude.
4. **Svuota cronologia**: click "Svuota cronologia" → dialog con N corretto. "Annulla"/ESC → niente cambia. Riprovo → "Sì, cancella tutto" → lista vuota, stato vuoto visibile.
5. **Disabled quando vuota**: dopo svuotamento, "Svuota cronologia" è disabilitato.
6. **Elimina dal dettaglio**: doppio click su riga → dettaglio → "Elimina questa partita" → conferma → torno alla lista, partita assente.
7. **Persistenza**: chiudo e riapro l'app, le eliminazioni sono persistite.
8. **Sospesi intatti**: dopo svuotamento cronologia, tab Sospesi non ha perso pause.
9. **Console+GUI**: console finisce un quiz mentre GUI ha la tab aperta. Aggiorna → la nuova partita appare con `Id` valorizzato.

Se uno qualsiasi fallisce, fixarlo nello stesso task con un commit separato.

- [ ] **Step 3: Aggiorna `WSA_QUIZ_HANDOFF.md`**

Cambia lo stato dello Step 5 da `⏳` a `✅` e aggiungi un paragrafo riassuntivo (puoi modellarlo su quello dello Step 4):

```markdown
### ✅ Step 5 — Eliminazione cronologia
`RisultatoQuiz.Id` (Guid stringa) generato in `SalvaRisultato`, migrazione lazy una-tantum dei record esistenti in `CaricaCronologia`. Due nuovi metodi: `StorageService.EliminaRisultato(string)` e `StorageService.SvuotaCronologia()`. GUI: bottone "Elimina" inline su ogni riga della Cronologia (pattern dei Sospesi), bottone "Svuota cronologia" nell'header con dialog modale di conferma (riusa il pattern di `ChiediConfermaAbbandono`), bottone "Elimina questa partita" nel `CronologiaDettaglioView` con conferma inline. Spec: `docs/superpowers/specs/2026-05-09-step5-eliminazione-cronologia-design.md`. **Fatto.**
```

E aggiorna anche la sezione "Stato attuale" in cima al documento se vuoi (opzionale: "Stato attuale: STEP 5 COMPLETO").

- [ ] **Step 4: Commit finale**

```powershell
git add WSA_QUIZ_HANDOFF.md
git commit -m "docs(step5): mark history deletion as done in handoff"
```

---

## Riepilogo dei file toccati

| File | Cosa |
| --- | --- |
| `wsa.quiz.core/Models/RisultatoQuiz.cs` | + `Id` |
| `wsa.quiz.core/Services/StorageService.cs` | Guid in `SalvaRisultato`, migrazione in `CaricaCronologia`, nuovi `EliminaRisultato`/`SvuotaCronologia` |
| `wsa.quiz.app/State/RisultatoCronologiaItem.cs` | observable + `Id` + `InAttesaConfermaEliminazione` |
| `wsa.quiz.app/Views/CronologiaView.axaml` | colonna azioni nel template riga + bottone "Svuota cronologia" header |
| `wsa.quiz.app/Views/CronologiaView.axaml.cs` | handler riga + dialog svuota + handler delete-from-detail |
| `wsa.quiz.app/Views/CronologiaDettaglioView.axaml` | bottone "Elimina questa partita" + barra in basso |
| `wsa.quiz.app/Views/CronologiaDettaglioView.axaml.cs` | stato observable + evento `EliminazioneRichiesta` + handler |
| `WSA_QUIZ_HANDOFF.md` | step 5 a ✅ |
