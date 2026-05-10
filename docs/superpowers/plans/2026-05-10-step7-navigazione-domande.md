# Step 7 — Navigazione tra domande — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Aggiungere alla `QuizView` la navigazione `←/→` tra domande passate (read-only) e l'evidenziazione `↑/↓ + Invio` come scorciatoia alternativa ad `A/B/C/D`, mantenendo lo stato della domanda corrente intatto durante la "view-mode".

**Architecture:** Estendere il modello `DettaglioRisposta` (Core) con 3 campi additivi che catturano la shufflata delle risposte (necessaria per ricostruire i 4 bottoni colorati anche dopo pausa/ripresa). Aggiungere alla `SessioneQuiz` due indici di stato (`_viewIndex` per la passata visualizzata, `_indiceHighlight` per il highlight) e i metodi di navigazione. Il `QuizView` riusa le proprietà observable correnti (Domanda, Risposte, Feedback) sostituendole temporaneamente con lo snapshot della passata in view-mode (opzione A della spec — unica copia di stato + backup/restore).

**Tech Stack:** .NET 8, Avalonia 12.0.2, code-behind + INPC (no MVVM). Non c'è suite di test automatica: la verifica per ogni task è `dotnet build` + (per i task UI) test manuale eseguendo `dotnet run --project wsa.quiz.app`.

**Spec di riferimento:** `docs/superpowers/specs/2026-05-10-step7-navigazione-domande-design.md`

---

## File toccati (riepilogo)

- Modify `wsa.quiz.core/Models/DettaglioRisposta.cs` — 3 campi additivi
- Modify `wsa.quiz.app/State/RispostaItem.cs` — flag `IsHighlighted`, `IsEnabled`, computed `PuoCliccare`
- Modify `wsa.quiz.app/State/SessioneQuiz.cs` — popolazione 3 nuovi campi in `RispondiA`, stato `_viewIndex` + `_indiceHighlight`, navigation methods, backup/restore stato live, `EtichettaDomanda`, `EsportaPausa` con uscita view-mode forzata
- Modify `wsa.quiz.app/Views/QuizView.axaml` — banner view-mode, style `Button.risposta.highlighted`, binding `IsEnabled` su `PuoCliccare`, classe `.highlighted`, header con `EtichettaDomanda`
- Modify `wsa.quiz.app/Views/QuizView.axaml.cs` — `OnKeyDown` esteso per `Left/Right/Up/Down` e `Enter` con highlight
- Modify `WSA_QUIZ_HANDOFF.md` — stato step 7 done

Nessun file nuovo.

---

### Task 1: Aggiungere 3 campi a `DettaglioRisposta` e popolarli in `SessioneQuiz.RispondiA`

**Files:**
- Modify: `wsa.quiz.core/Models/DettaglioRisposta.cs`
- Modify: `wsa.quiz.app/State/SessioneQuiz.cs:271-282`

I tre campi servono al `QuizView` per ricostruire i 4 bottoni A/B/C/D in view-mode. Sono additivi: i record JSON esistenti caricano con default vuoto/`-1`. Il `CronologiaDettaglioView` non li tocca.

- [ ] **Step 1: aggiungi i 3 campi a `DettaglioRisposta`**

Sostituisci tutto il file `wsa.quiz.core/Models/DettaglioRisposta.cs` con:

```csharp
namespace Wsa.Quiz.Core.Models;

/// <summary>
/// Dettaglio di una singola risposta data durante un quiz. Salvato in cronologia.
/// </summary>
public class DettaglioRisposta
{
    public string IdDomanda { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string MateriaNome { get; set; } = string.Empty;
    public string TestoDomanda { get; set; } = string.Empty;
    public string RispostaData { get; set; } = string.Empty;
    public string RispostaCorretta { get; set; } = string.Empty;
    public bool Corretta { get; set; }
    public string Spiegazione { get; set; } = string.Empty;

    /// <summary>Numero di tentativi prima che fosse data la risposta corretta (modalità rotazione). 1 = al primo colpo.</summary>
    public int Tentativi { get; set; } = 1;

    // ----- Step 7: campi per ricostruire i 4 bottoni A/B/C/D in view-mode -----
    // Additivi: i record JSON esistenti caricano con default vuoto/-1. Ignorati
    // dal CronologiaDettaglioView. Popolati in SessioneQuiz.RispondiA.

    /// <summary>Le 4 risposte nell'ordine mostrato all'utente (post-shuffle).</summary>
    public List<string> RisposteShufflate { get; set; } = new();

    /// <summary>Indice (0..3) della risposta corretta nell'ordine shufflato.</summary>
    public int IndiceCorrettoShufflato { get; set; } = -1;

    /// <summary>Indice (0..3) della risposta data dall'utente nell'ordine shufflato.</summary>
    public int IndiceDataShufflato { get; set; } = -1;
}
```

- [ ] **Step 2: popola i 3 campi in `RispondiA`**

In `wsa.quiz.app/State/SessioneQuiz.cs`, trova il blocco `Risultato.Dettagli.Add(new DettaglioRisposta { ... })` (intorno a riga 271). Aggiungi le tre nuove assegnazioni in coda all'inizializer, dopo `Tentativi = tentativi`:

```csharp
Risultato.Dettagli.Add(new DettaglioRisposta
{
    IdDomanda = _domandaCorrente.Originale.Id,
    Categoria = _domandaCorrente.Originale.Categoria,
    MateriaNome = _domandaCorrente.Originale.MateriaNome,
    TestoDomanda = _domandaCorrente.Originale.DomandaTesto,
    RispostaData = $"[{lettera}] {_domandaCorrente.RisposteShufflate[indiceShufflato]}",
    RispostaCorretta = $"[{_domandaCorrente.LetteraCorretta}] {_domandaCorrente.TestoRispostaCorretta}",
    Corretta = ok,
    Spiegazione = _domandaCorrente.Originale.Spiegazione,
    Tentativi = tentativi,
    RisposteShufflate = _domandaCorrente.RisposteShufflate.ToList(),
    IndiceCorrettoShufflato = _domandaCorrente.IndiceCorrettoShufflato,
    IndiceDataShufflato = indiceShufflato
});
```

(`.ToList()` per sicurezza: copia difensiva.)

- [ ] **Step 3: build**

```
dotnet build Wsa.Quiz.sln
```

Atteso: build succeeded, 0 errori, 0 warning sui file modificati.

- [ ] **Step 4: commit**

```
git add wsa.quiz.core/Models/DettaglioRisposta.cs wsa.quiz.app/State/SessioneQuiz.cs
git commit -m "feat(step7): DettaglioRisposta espone shuffle (4 risposte + indici)"
```

---

### Task 2: Estendere `RispostaItem` con `IsHighlighted`, `IsEnabled`, `PuoCliccare` + aggiornare binding e style in `QuizView.axaml`

**Files:**
- Modify: `wsa.quiz.app/State/RispostaItem.cs`
- Modify: `wsa.quiz.app/Views/QuizView.axaml:127-146` (template del Button risposta), e blocco `<UserControl.Styles>` per la classe `.highlighted`

Il bottone risposta deve poter essere disabilitato indipendentemente dallo stato (in view-mode). E deve poter mostrare un bordo accent quando `IsHighlighted=true` e lo stato è `Neutra`. Vedi spec sezione "RispostaItem — flag aggiuntivi".

- [ ] **Step 1: aggiungi i campi a `RispostaItem`**

Sostituisci tutto `wsa.quiz.app/State/RispostaItem.cs` con:

```csharp
namespace Wsa.Quiz.App.State;

/// <summary>
/// Stato visivo di una singola risposta nella schermata di quiz.
/// Pilotato dalla logica di sessione e tradotto in colori dagli stili XAML.
/// </summary>
public enum StatoRisposta
{
    /// <summary>Non ancora risposto: tutte le risposte sono in questo stato.</summary>
    Neutra,
    /// <summary>Quella che l'utente ha cliccato, e che era sbagliata.</summary>
    Sbagliata,
    /// <summary>La risposta corretta (sempre evidenziata dopo il click).</summary>
    Corretta
}

/// <summary>
/// Singola riga risposta nella QuizView. Lo <see cref="Stato"/> e' observable,
/// cosi' lo style XAML reagisce al cambio senza ricreare la collezione.
/// </summary>
public class RispostaItem : ObservableObject
{
    public int Indice { get; }
    public char Lettera { get; }
    public string Testo { get; }

    public string EtichettaLettera => $"{Lettera}.";

    private StatoRisposta _stato = StatoRisposta.Neutra;
    public StatoRisposta Stato
    {
        get => _stato;
        set
        {
            if (SetField(ref _stato, value))
            {
                RaisePropertyChanged(nameof(IsCorretta));
                RaisePropertyChanged(nameof(IsSbagliata));
                RaisePropertyChanged(nameof(IsNeutra));
                RaisePropertyChanged(nameof(PuoCliccare));
            }
        }
    }

    /// <summary>Helper per binding bool-only (es. selettori di stile).</summary>
    public bool IsNeutra    => _stato == StatoRisposta.Neutra;
    public bool IsCorretta  => _stato == StatoRisposta.Corretta;
    public bool IsSbagliata => _stato == StatoRisposta.Sbagliata;

    // ----- Step 7 -----

    private bool _isEnabled = true;
    /// <summary>
    /// Flag indipendente dallo stato. La <c>SessioneQuiz</c> lo mette a <c>false</c>
    /// quando entra in view-mode (i bottoni vanno disabilitati anche se Neutri).
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetField(ref _isEnabled, value))
                RaisePropertyChanged(nameof(PuoCliccare));
        }
    }

    private bool _isHighlighted;
    /// <summary>
    /// Bottone evidenziato dalle frecce ↑/↓. Lo style XAML applica un bordo accent
    /// quando questa proprieta' e' vera e lo stato e' <see cref="StatoRisposta.Neutra"/>.
    /// </summary>
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetField(ref _isHighlighted, value);
    }

    /// <summary>
    /// Computed: il bottone risposta e' cliccabile solo se enabled E ancora in
    /// stato neutro (anti doppio-click + view-mode).
    /// </summary>
    public bool PuoCliccare => _isEnabled && _stato == StatoRisposta.Neutra;

    public RispostaItem(int indice, string testo)
    {
        Indice = indice;
        Lettera = (char)('A' + indice);
        Testo = testo;
    }
}
```

- [ ] **Step 2: aggiorna binding e style in `QuizView.axaml`**

In `wsa.quiz.app/Views/QuizView.axaml`:

**(a)** Nel blocco `<UserControl.Styles>`, dopo lo style `Button.risposta.sbagliata:disabled` (linea ~52, prima di `<!-- Feedback box ... -->`), aggiungi:

```xml
        <Style Selector="Button.risposta.highlighted">
            <Setter Property="BorderBrush" Value="{DynamicResource SystemAccentColorBrush}"/>
            <Setter Property="BorderThickness" Value="2"/>
        </Style>
```

**(b)** Nel template del Button risposta (intorno alla linea 130), cambia l'attributo `IsEnabled="{Binding IsNeutra}"` in `IsEnabled="{Binding PuoCliccare}"` e aggiungi `Classes.highlighted="{Binding IsHighlighted}"`. Il Button risultante:

```xml
                            <Button Classes="risposta"
                                    Classes.corretta="{Binding IsCorretta}"
                                    Classes.sbagliata="{Binding IsSbagliata}"
                                    Classes.highlighted="{Binding IsHighlighted}"
                                    Click="OnRispostaClick"
                                    IsEnabled="{Binding PuoCliccare}">
```

- [ ] **Step 3: build**

```
dotnet build Wsa.Quiz.sln
```

Atteso: build succeeded.

- [ ] **Step 4: smoke test manuale**

```
dotnet run --project wsa.quiz.app
```

Avvia un quiz qualunque e verifica che:
- I 4 bottoni risposta restano cliccabili come prima
- Dopo aver risposto, vengono colorati verde/rosso e disabilitati come prima

Comportamento identico al pre-step (regressione zero). Lo style `.highlighted` non si vede ancora perché nessuno setta `IsHighlighted=true`.

- [ ] **Step 5: commit**

```
git add wsa.quiz.app/State/RispostaItem.cs wsa.quiz.app/Views/QuizView.axaml
git commit -m "feat(step7): RispostaItem.IsEnabled/IsHighlighted + style highlighted"
```

---

### Task 3: Aggiungere stato view-mode + backup/restore + navigation methods + `EtichettaDomanda` su `SessioneQuiz`

**Files:**
- Modify: `wsa.quiz.app/State/SessioneQuiz.cs`

Cuore tecnico dello step. Aggiunge:
- Campo `_viewIndex` (nullable: null = corrente).
- Proprietà observable `IndiceVisualizzazione`, `InViewMode`, `EtichettaDomanda`, `PuoIndietro`, `PuoAvanti`.
- Struct privata `_StatoLive` per il backup.
- Metodi privati `SalvaStatoLive()`, `RipristinaStatoLive()`, `MostraPassata(int)`.
- Metodi pubblici `VaiAPassataPrecedente()`, `VaiAPassataSuccessiva()`, `TornaACorrente()`.

- [ ] **Step 1: aggiungi i campi privati**

In `wsa.quiz.app/State/SessioneQuiz.cs`, nella sezione `// ------------------------------------------------------------------ STATO COMUNE` (dopo la dichiarazione di `_offsetEffettuate`, intorno a riga 44), aggiungi:

```csharp
    // ----- Step 7: navigazione view-mode -----
    private int? _viewIndex;       // null = corrente, 0..N-1 = passata in Risultato.Dettagli
    private _StatoLive? _backupLive;

    /// <summary>Snapshot dello stato live, salvato all'ingresso in view-mode e ripristinato all'uscita.</summary>
    private record _StatoLive(
        DomandaPreparata? DomandaCorrente,
        string DomandaTesto,
        string CategoriaCorrente,
        string MateriaCorrente,
        int NumeroDomandaCorrente,
        bool RispostaInviata,
        bool UltimaRispostaCorretta,
        string FeedbackTitolo,
        string SpiegazioneTesto,
        string LetteraCorretta,
        string TestoRispostaCorretta,
        List<(string Testo, StatoRisposta Stato)> Risposte);
```

- [ ] **Step 2: aggiungi le proprietà observable**

Sempre in `SessioneQuiz.cs`, nella sezione `// ------------------------------------------------------------------ PROPRIETA' OBSERVABLE` (dopo `TestoRispostaCorretta`, intorno a riga 150), aggiungi:

```csharp
    // ----- Step 7 -----

    /// <summary>Indice della passata visualizzata (0..Dettagli.Count-1) o null se sulla corrente.</summary>
    public int? IndiceVisualizzazione
    {
        get => _viewIndex;
        private set
        {
            if (_viewIndex != value)
            {
                _viewIndex = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(InViewMode));
                RaisePropertyChanged(nameof(EtichettaDomanda));
                RaisePropertyChanged(nameof(PuoIndietro));
                RaisePropertyChanged(nameof(PuoAvanti));
            }
        }
    }

    /// <summary>True quando si sta guardando una passata (read-only).</summary>
    public bool InViewMode => _viewIndex.HasValue;

    /// <summary>Etichetta header: "Domanda X di Y" o "Domanda X di Y (vista)" in view-mode.</summary>
    public string EtichettaDomanda => InViewMode
        ? $"Domanda {_viewIndex!.Value + 1} di {TotalePrevisto} (vista)"
        : $"Domanda {NumeroDomandaCorrente} di {TotalePrevisto}";

    /// <summary>True se ← è significativo: ci sono passate e non sono già sulla prima.</summary>
    public bool PuoIndietro =>
        Risultato.Dettagli.Count > 0 &&
        (_viewIndex ?? Risultato.Dettagli.Count) > 0;

    /// <summary>True se → è significativo: sono in view-mode.</summary>
    public bool PuoAvanti => InViewMode;
```

- [ ] **Step 3: rinotifica `EtichettaDomanda` quando cambiano `NumeroDomandaCorrente` o `TotalePrevisto`**

In `SessioneQuiz.cs`, modifica i setter di `TotalePrevisto` e `NumeroDomandaCorrente` per notificare anche `EtichettaDomanda` e `PuoIndietro`/`PuoAvanti`:

```csharp
    private int _numeroDomandaCorrente;
    public int NumeroDomandaCorrente
    {
        get => _numeroDomandaCorrente;
        private set
        {
            if (SetField(ref _numeroDomandaCorrente, value))
            {
                RaisePropertyChanged(nameof(EtichettaDomanda));
                RaisePropertyChanged(nameof(ProgressoPercentuale));
            }
        }
    }

    private int _totalePrevisto;
    public int TotalePrevisto
    {
        get => _totalePrevisto;
        private set
        {
            if (SetField(ref _totalePrevisto, value))
            {
                RaisePropertyChanged(nameof(ProgressoPercentuale));
                RaisePropertyChanged(nameof(EtichettaDomanda));
            }
        }
    }
```

(Nota: `NumeroDomandaCorrente` ora notifica anche `ProgressoPercentuale`. Prima non lo faceva ma `ProgressoPercentuale` lo usa: era un baco silenzioso. Se preferisci stretto YAGNI, togli `ProgressoPercentuale` qui — il chiamante in `CaricaProssimaDomanda` raise esplicitamente.)

Inoltre, ogni volta che `Risultato.Dettagli` cambia (cioè in `RispondiA` dopo `Risultato.Dettagli.Add(...)`), bisogna notificare `PuoIndietro`. Aggiungi questa riga subito dopo l'`Add` in `RispondiA`:

```csharp
        RaisePropertyChanged(nameof(PuoIndietro));
```

- [ ] **Step 4: aggiungi i metodi backup/restore + MostraPassata**

In fondo a `SessioneQuiz.cs`, prima della chiusura della classe (cioè prima dell'ultima `}`), aggiungi:

```csharp
    // ------------------------------------------------------------------ STEP 7: VIEW-MODE

    /// <summary>
    /// Salva lo stato live (domanda corrente + risposte + feedback) prima di
    /// sostituirlo con quello della passata. Chiamato solo quando si entra in
    /// view-mode, NON quando si naviga fra una passata e un'altra.
    /// </summary>
    private void SalvaStatoLive()
    {
        _backupLive = new _StatoLive(
            DomandaCorrente: _domandaCorrente,
            DomandaTesto: _domandaTesto,
            CategoriaCorrente: _categoriaCorrente,
            MateriaCorrente: _materiaCorrente,
            NumeroDomandaCorrente: _numeroDomandaCorrente,
            RispostaInviata: _rispostaInviata,
            UltimaRispostaCorretta: _ultimaRispostaCorretta,
            FeedbackTitolo: _feedbackTitolo,
            SpiegazioneTesto: _spiegazioneTesto,
            LetteraCorretta: _letteraCorretta,
            TestoRispostaCorretta: _testoRispostaCorretta,
            Risposte: Risposte.Select(r => (r.Testo, r.Stato)).ToList());
    }

    /// <summary>
    /// Ripristina lo stato live salvato da <see cref="SalvaStatoLive"/>. Idempotente
    /// se chiamato senza un backup attivo (no-op).
    /// </summary>
    private void RipristinaStatoLive()
    {
        if (_backupLive == null) return;
        var b = _backupLive;
        _domandaCorrente = b.DomandaCorrente;
        DomandaTesto = b.DomandaTesto;
        CategoriaCorrente = b.CategoriaCorrente;
        MateriaCorrente = b.MateriaCorrente;
        NumeroDomandaCorrente = b.NumeroDomandaCorrente;

        Risposte.Clear();
        for (int i = 0; i < b.Risposte.Count; i++)
        {
            var (testo, stato) = b.Risposte[i];
            var item = new RispostaItem(i, testo) { Stato = stato, IsEnabled = true };
            Risposte.Add(item);
        }

        FeedbackTitolo = b.FeedbackTitolo;
        SpiegazioneTesto = b.SpiegazioneTesto;
        LetteraCorretta = b.LetteraCorretta;
        TestoRispostaCorretta = b.TestoRispostaCorretta;
        UltimaRispostaCorretta = b.UltimaRispostaCorretta;
        RispostaInviata = b.RispostaInviata;

        _backupLive = null;
    }

    /// <summary>
    /// Sovrascrive lo stato osservabile con quello della passata indicata.
    /// Non salva il backup (lo fa il chiamante solo all'ingresso in view-mode).
    /// </summary>
    private void MostraPassata(int indice)
    {
        var d = Risultato.Dettagli[indice];

        DomandaTesto = d.TestoDomanda;
        CategoriaCorrente = d.Categoria;
        MateriaCorrente = d.MateriaNome;
        // L'header "Domanda X (vista)" usa _viewIndex via EtichettaDomanda;
        // NumeroDomandaCorrente live resta nel backup.

        Risposte.Clear();
        for (int i = 0; i < d.RisposteShufflate.Count; i++)
        {
            StatoRisposta stato;
            if (i == d.IndiceCorrettoShufflato) stato = StatoRisposta.Corretta;
            else if (i == d.IndiceDataShufflato && !d.Corretta) stato = StatoRisposta.Sbagliata;
            else stato = StatoRisposta.Neutra;

            Risposte.Add(new RispostaItem(i, d.RisposteShufflate[i])
            {
                Stato = stato,
                IsEnabled = false  // disabilitato in view-mode, indipendente dallo Stato
            });
        }

        // Pannello feedback: riproduco lo stato post-risposta
        UltimaRispostaCorretta = d.Corretta;
        FeedbackTitolo = d.Corretta ? "Corretto!" : "Sbagliato";
        SpiegazioneTesto = d.Spiegazione ?? string.Empty;
        // Estraggo lettera + testo da "[X] testo" usando il prefisso che genera RispondiA
        LetteraCorretta = d.IndiceCorrettoShufflato >= 0
            ? ((char)('A' + d.IndiceCorrettoShufflato)).ToString()
            : string.Empty;
        TestoRispostaCorretta = d.IndiceCorrettoShufflato >= 0 && d.IndiceCorrettoShufflato < d.RisposteShufflate.Count
            ? d.RisposteShufflate[d.IndiceCorrettoShufflato]
            : string.Empty;
        RispostaInviata = true;
    }

    /// <summary>← : entra in view-mode (se non c'è già) o scorre alla passata precedente.</summary>
    public void VaiAPassataPrecedente()
    {
        if (Risultato.Dettagli.Count == 0) return;

        int target;
        if (_viewIndex == null)
        {
            // Sto sulla corrente: salvo backup ed entro in view-mode sull'ultima passata
            SalvaStatoLive();
            target = Risultato.Dettagli.Count - 1;
        }
        else if (_viewIndex.Value > 0)
        {
            target = _viewIndex.Value - 1;
        }
        else
        {
            return; // già sulla prima passata, no-op
        }

        IndiceVisualizzazione = target;
        MostraPassata(target);
    }

    /// <summary>→ : scorre alla passata successiva o torna alla corrente.</summary>
    public void VaiAPassataSuccessiva()
    {
        if (_viewIndex == null) return; // già sulla corrente, no-op

        int next = _viewIndex.Value + 1;
        if (next >= Risultato.Dettagli.Count)
        {
            TornaACorrente();
        }
        else
        {
            IndiceVisualizzazione = next;
            MostraPassata(next);
        }
    }

    /// <summary>Esce dalla view-mode e torna alla domanda corrente live.</summary>
    public void TornaACorrente()
    {
        if (_viewIndex == null) return;
        IndiceVisualizzazione = null;
        RipristinaStatoLive();
    }
```

- [ ] **Step 5: aggiungi gli using necessari in `SessioneQuiz.cs`**

In testa al file, verifica che ci sia `using System.Linq;` (già presente) e `using System.Collections.Generic;` (già presente). Niente da aggiungere.

- [ ] **Step 6: build**

```
dotnet build Wsa.Quiz.sln
```

Atteso: build succeeded. Se compila, lo stato è coerente. La UI ancora non sa nulla del view-mode (nessun keybinding); test manuale a Task 5/6.

- [ ] **Step 7: commit**

```
git add wsa.quiz.app/State/SessioneQuiz.cs
git commit -m "feat(step7): SessioneQuiz view-mode + navigation + backup/restore"
```

---

### Task 4: Aggiungere stato highlight + sicurezza pausa su `SessioneQuiz`

**Files:**
- Modify: `wsa.quiz.app/State/SessioneQuiz.cs`

Aggiunge `_indiceHighlight` con metodi `HighlightSu`/`HighlightGiu`/`ConfermaHighlight`. Inoltre forza l'uscita da view-mode all'inizio di `EsportaPausa` (per non catturare lo stato view-mode invece del live) e al `RispondiA`/`Avanza` (sicurezza in caso di chiamata diretta).

- [ ] **Step 1: aggiungi il campo highlight**

In `SessioneQuiz.cs`, sotto i campi step 7 aggiunti al Task 3 (subito dopo `private _StatoLive? _backupLive;`), aggiungi:

```csharp
    private int? _indiceHighlight;
```

- [ ] **Step 2: aggiungi la proprietà observable**

In `SessioneQuiz.cs`, sotto `PuoAvanti` (Task 3 step 2), aggiungi:

```csharp
    /// <summary>
    /// Indice (0..3) della risposta evidenziata da ↑/↓, o null se nessuna.
    /// Settato da <see cref="HighlightSu"/>/<see cref="HighlightGiu"/>, resettato a
    /// null da <see cref="RispondiA"/>, <see cref="CaricaProssimaDomanda"/> e
    /// quando si entra/esce dalla view-mode.
    /// </summary>
    public int? IndiceHighlight
    {
        get => _indiceHighlight;
        private set
        {
            if (_indiceHighlight != value)
            {
                int? old = _indiceHighlight;
                _indiceHighlight = value;
                RaisePropertyChanged();
                // Sincronizza il flag IsHighlighted sui RispostaItem
                if (old.HasValue && old.Value < Risposte.Count)
                    Risposte[old.Value].IsHighlighted = false;
                if (value.HasValue && value.Value < Risposte.Count)
                    Risposte[value.Value].IsHighlighted = true;
            }
        }
    }
```

- [ ] **Step 3: aggiungi i metodi pubblici di highlight**

Sotto `TornaACorrente()` (fondo del file, dentro la classe), aggiungi:

```csharp
    /// <summary>↑ : sposta highlight in su (clamp 0). Inizializza a 3 se null.</summary>
    public void HighlightSu()
    {
        if (InViewMode || RispostaInviata) return;
        if (Risposte.Count == 0) return;
        int next = _indiceHighlight.HasValue
            ? Math.Max(0, _indiceHighlight.Value - 1)
            : Risposte.Count - 1;
        IndiceHighlight = next;
    }

    /// <summary>↓ : sposta highlight in giù (clamp 3). Inizializza a 0 se null.</summary>
    public void HighlightGiu()
    {
        if (InViewMode || RispostaInviata) return;
        if (Risposte.Count == 0) return;
        int next = _indiceHighlight.HasValue
            ? Math.Min(Risposte.Count - 1, _indiceHighlight.Value + 1)
            : 0;
        IndiceHighlight = next;
    }

    /// <summary>Invio quando c'è un highlight: conferma quella risposta.</summary>
    public bool ConfermaHighlight()
    {
        if (InViewMode || RispostaInviata) return false;
        if (!_indiceHighlight.HasValue) return false;
        RispondiA(_indiceHighlight.Value);
        return true;
    }
```

- [ ] **Step 4: reset highlight nei punti chiave**

In `SessioneQuiz.cs`:

**(a)** in `RispondiA` (riga ~254), all'inizio del metodo dopo i due `if (...) return;`, aggiungi:

```csharp
        IndiceHighlight = null;
```

**(b)** in `CaricaProssimaDomanda` (riga ~211), in fondo al metodo dopo `RaisePropertyChanged(nameof(ProgressoPercentuale));`, aggiungi:

```csharp
        IndiceHighlight = null;
```

**(c)** in `MostraPassata` e `RipristinaStatoLive` (Task 3 step 4), aggiungi `IndiceHighlight = null;` come prima riga, per sicurezza.

- [ ] **Step 5: forza uscita view-mode in `EsportaPausa`**

In `SessioneQuiz.cs`, modifica l'inizio di `EsportaPausa` (riga ~397). Subito dopo `public SessionePausa EsportaPausa() {` e prima di `_cron.Stop();`, aggiungi:

```csharp
        if (InViewMode) TornaACorrente();
```

- [ ] **Step 6: build**

```
dotnet build Wsa.Quiz.sln
```

Atteso: build succeeded.

- [ ] **Step 7: commit**

```
git add wsa.quiz.app/State/SessioneQuiz.cs
git commit -m "feat(step7): SessioneQuiz highlight + uscita view-mode in EsportaPausa"
```

---

### Task 5: Banner view-mode + nuova etichetta header in `QuizView.axaml`

**Files:**
- Modify: `wsa.quiz.app/Views/QuizView.axaml`
- Modify: `wsa.quiz.app/Views/QuizView.axaml.cs` (handler nuovo bottone "Torna a corrente")

Aggiunge una riga al `<Grid RowDefinitions>` per il banner sopra l'header. Cambia il TextBlock dell'header per usare `EtichettaDomanda`. Aggiunge il bottone "Torna alla corrente" nel banner.

- [ ] **Step 1: aggiungi la riga banner al Grid principale**

In `wsa.quiz.app/Views/QuizView.axaml`, trova la `<Grid>` principale (linea ~65):

```xml
    <Grid RowDefinitions="Auto,*,Auto,Auto" Margin="20">
```

Cambiala in:

```xml
    <Grid RowDefinitions="Auto,Auto,*,Auto,Auto" Margin="20">
```

Poi shifta tutti i `Grid.Row` esistenti di +1:
- Header `<Border Grid.Row="0">` → `Grid.Row="1"`
- ScrollViewer `<ScrollViewer Grid.Row="1">` → `Grid.Row="2"`
- Border feedback `<Border Grid.Row="2">` → `Grid.Row="3"`
- ProgressBar `<ProgressBar Grid.Row="3">` → `Grid.Row="4"`

- [ ] **Step 2: aggiungi il banner come `Grid.Row="0"`**

In testa alla `<Grid>` principale, prima dell'header (che ora è `Grid.Row="1"`), aggiungi:

```xml
        <!-- BANNER VIEW-MODE (step 7): visibile solo quando si sta rivedendo una passata -->
        <Border Grid.Row="0"
                Background="#FFF8E1"
                BorderBrush="#E0C36C"
                BorderThickness="1"
                CornerRadius="6"
                Padding="14,8"
                Margin="0,0,0,10"
                IsVisible="{Binding InViewMode}">
            <Grid ColumnDefinitions="*,Auto" VerticalAlignment="Center">
                <TextBlock Text="{Binding EtichettaDomanda}"
                           FontSize="13"
                           FontWeight="SemiBold"
                           Foreground="#5C4A12"
                           VerticalAlignment="Center"/>
                <Button Grid.Column="1"
                        Content="Torna alla corrente"
                        Click="OnTornaACorrenteClick"
                        Classes="accent"
                        Padding="14,5"
                        FontSize="12"/>
            </Grid>
        </Border>
```

(Colori: gialletto crema sicuro su tema chiaro e scuro, foreground hex esplicito per evitare la trappola n.9 del handoff.)

- [ ] **Step 3: cambia l'header per usare `EtichettaDomanda`**

Nell'header (ora `Grid.Row="1"`), trova il TextBlock con i 4 `Run`:

```xml
                    <TextBlock FontSize="13" FontWeight="SemiBold">
                        <Run Text="Domanda"/>
                        <Run Text="{Binding NumeroDomandaCorrente}"/>
                        <Run Text="di"/>
                        <Run Text="{Binding TotalePrevisto}"/>
                    </TextBlock>
```

Sostituiscilo con:

```xml
                    <TextBlock Text="{Binding EtichettaDomanda}"
                               FontSize="13"
                               FontWeight="SemiBold"/>
```

- [ ] **Step 4: aggiungi l'handler `OnTornaACorrenteClick` in `QuizView.axaml.cs`**

In `wsa.quiz.app/Views/QuizView.axaml.cs`, sotto `OnPausaClick` (linea ~104), aggiungi:

```csharp
    private void OnTornaACorrenteClick(object? sender, RoutedEventArgs e)
    {
        _sessione.TornaACorrente();
        Focus();
    }
```

- [ ] **Step 5: build**

```
dotnet build Wsa.Quiz.sln
```

Atteso: build succeeded.

- [ ] **Step 6: smoke test manuale**

```
dotnet run --project wsa.quiz.app
```

Avvia un quiz e verifica:
- L'header mostra ancora "Domanda 1 di N" — l'`EtichettaDomanda` funziona via binding.
- Il banner giallo NON è visibile (perché `InViewMode=false`).
- Niente regressioni.

(Il banner si vedrà davvero quando il keybinding sarà attivo nel Task 6. Per ora possiamo forzare un test: temporaneamente in `OnAttachedToVisualTree` aggiungere `_sessione.VaiAPassataPrecedente();` dopo `_sessione.Avvia();` e poi rispondere a una domanda → al successivo refresh il banner si vede. Rimuovere il test temporaneo prima del commit.)

- [ ] **Step 7: commit**

```
git add wsa.quiz.app/Views/QuizView.axaml wsa.quiz.app/Views/QuizView.axaml.cs
git commit -m "feat(step7): banner view-mode + EtichettaDomanda nell'header"
```

---

### Task 6: Estendere `OnKeyDown` con `←/→/↑/↓` + `Invio` con highlight

**Files:**
- Modify: `wsa.quiz.app/Views/QuizView.axaml.cs:56-88`

Aggiunge i case per le 4 frecce ed estende il case `Enter` per gestire la conferma del highlight quando presente.

- [ ] **Step 1: sostituisci `OnKeyDown`**

In `wsa.quiz.app/Views/QuizView.axaml.cs`, sostituisci tutto il metodo `OnKeyDown` (linee 56-88) con:

```csharp
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.None)
        {
            base.OnKeyDown(e);
            return;
        }

        switch (e.Key)
        {
            case Key.A: case Key.B: case Key.C: case Key.D:
                if (!_sessione.InViewMode && _sessione.InAttesaRisposta)
                {
                    int idx = e.Key - Key.A;
                    if (idx < _sessione.Risposte.Count)
                        _sessione.RispondiA(idx);
                }
                e.Handled = true;
                return;

            case Key.Up:
                if (!_sessione.InViewMode) _sessione.HighlightSu();
                e.Handled = true;
                return;

            case Key.Down:
                if (!_sessione.InViewMode) _sessione.HighlightGiu();
                e.Handled = true;
                return;

            case Key.Left:
                _sessione.VaiAPassataPrecedente();
                e.Handled = true;
                return;

            case Key.Right:
                _sessione.VaiAPassataSuccessiva();
                e.Handled = true;
                return;

            case Key.Enter:
                // Priorita': se c'e' un highlight in attesa, conferma quella risposta;
                // altrimenti se siamo in feedback, avanza. In view-mode: niente.
                if (!_sessione.InViewMode)
                {
                    if (!_sessione.ConfermaHighlight() && _sessione.RispostaInviata)
                        _sessione.Avanza();
                }
                e.Handled = true;
                return;

            case Key.Escape:
                e.Handled = true;
                ApriMenuPausa();
                return;
        }

        base.OnKeyDown(e);
    }
```

- [ ] **Step 2: build**

```
dotnet build Wsa.Quiz.sln
```

Atteso: build succeeded.

- [ ] **Step 3: commit**

```
git add wsa.quiz.app/Views/QuizView.axaml.cs
git commit -m "feat(step7): tastiera Left/Right/Up/Down + Invio con highlight"
```

---

### Task 7: Test manuali end-to-end + aggiornamento handoff

**Files:**
- Modify: `WSA_QUIZ_HANDOFF.md`

Verifica tutti i 9 scenari elencati nella spec. Documenta eventuali aggiustamenti applicati. Aggiorna il handoff per marcare lo step 7 come completato.

- [ ] **Step 1: avvia l'app**

```
dotnet run --project wsa.quiz.app
```

- [ ] **Step 2: esegui i test manuali della spec**

Esegui in ordine i test 1-9 della sezione "Test manuali (golden path + edge case)" della spec `docs/superpowers/specs/2026-05-10-step7-navigazione-domande-design.md`. Per ognuno annota PASS/FAIL e se FAIL il sintomo. I 9 test in breve:

1. Classica 5 domande, ←← scorre indietro, →→ torna a corrente con stato intatto.
2. Classica, sbaglia 1, ← rivede la 1 in feedback con la sbagliata in rosso e la corretta in verde, → torna alla 2 in feedback con stato preservato.
3. ↓↓ + Invio conferma C.
4. Highlight su B, click su A → invia A, highlight resettato (nessun bordo accent residuo).
5. Rotazione, sbaglia 2 volte la stessa domanda. ← mostra 2 entry separate.
6. View-mode + ESC → modale pausa. "Salva e esci" salva la sessione live (verifica nei Sospesi che la pausa contenga lo stato corrente, non la passata).
7. Pausa con 3 risposte, riprendi, ←←← rivede tutte e 3 con i 4 bottoni colorati (le shufflate sono state preservate nel JSON).
8. Bottone "Torna alla corrente" sul banner riporta al live.
9. Quiz appena partito, ← no-op, banner non visibile.

- [ ] **Step 3: applica eventuali fix UX emersi**

Se durante i test emergono problemi (banner brutto, etichette confuse, comportamento sorprendente), applicali come modifiche puntuali ai file già toccati. Ogni fix è un commit separato con messaggio `fix(step7): <descrizione>`. Non rifare la spec se il pratico la smentisce: aggiorna il handoff.

- [ ] **Step 4: aggiorna `WSA_QUIZ_HANDOFF.md`**

Le sezioni da aggiornare:

**(a)** Sostituisci la sezione "Stato attuale: STEP 6 COMPLETO" con "Stato attuale: STEP 7 COMPLETO" e riformula il primo paragrafo per includere la navigazione step 7.

**(b)** Nella roadmap, cambia `### ⏳ Step 7 — Navigazione tra domande` in `### ✅ Step 7 — Navigazione tra domande` e aggiungi una descrizione di una-due frasi della soluzione finale (riusa stile step 5/6: cosa è stato fatto, dove vivono i pezzi, eventuali iterazioni rispetto alla spec). Includi i riferimenti a spec e plan:
> Spec: `docs/superpowers/specs/2026-05-10-step7-navigazione-domande-design.md`. Plan: `docs/superpowers/plans/2026-05-10-step7-navigazione-domande.md`. **Fatto.**

**(c)** Aggiungi alla sezione "Trappole già scoperte" eventuali nuove trappole emerse durante l'implementazione/test, numerandole 12+.

**(d)** Cambia "Per ripartire" punto 3 per puntare al prossimo step (8 — Grafici).

- [ ] **Step 5: commit finale**

```
git add WSA_QUIZ_HANDOFF.md
git commit -m "docs(step7): aggiorna handoff con stato finale Navigazione tra domande"
```

---

## Self-Review (post-stesura piano)

**Spec coverage** — verificato sezione per sezione:

- ✅ Tastiera ←/→/↑/↓/Invio: Task 6
- ✅ "Ogni tentativo = entry" in rotazione: implicito perché `Risultato.Dettagli` accumula entry per ogni RispondiA (verificato in Task 1 step 2 e in test 5)
- ✅ Layout view-mode (banner + 4 bottoni colorati e disabilitati + feedback): Task 5 + Task 3 step 4 (`MostraPassata`)
- ✅ Highlight (campo + clamp + visivo + reset): Task 4 + Task 2 (style highlighted) + Task 6
- ✅ Modello dati `DettaglioRisposta` aggiunte: Task 1
- ✅ Stato `_viewIndex`, `_indiceHighlight`, navigation methods, backup/restore: Task 3 + Task 4
- ✅ Snapshot vista (opzione A: backup/restore): Task 3
- ✅ `RispostaItem` `IsHighlighted`/`IsEnabled`/`PuoCliccare`: Task 2
- ✅ Pausa esce da view-mode prima di esportare: Task 4 step 5
- ✅ Pausa NON persiste `_viewIndex`: implicito (campi step 7 non aggiunti a `SessionePausa`)
- ✅ XAML banner + binding aggiornato: Task 5
- ✅ XAML style `Button.risposta.highlighted`: Task 2 step 2 (a)
- ✅ `OnKeyDown` esteso: Task 6
- ✅ App.axaml: la spec menzionava potenziale style globale, ma in realtà conviene tenerlo dentro `QuizView.axaml` (specifico al Quiz) — scelta coerente, plan aggiornato.

**Placeholder scan:** nessun TBD/TODO/"add appropriate". Codice completo in ogni step.

**Type consistency:** verificato — `EtichettaDomanda`, `InViewMode`, `IndiceVisualizzazione`, `IndiceHighlight`, `VaiAPassataPrecedente`, `VaiAPassataSuccessiva`, `TornaACorrente`, `HighlightSu`, `HighlightGiu`, `ConfermaHighlight`, `IsHighlighted`, `IsEnabled`, `PuoCliccare`, `RisposteShufflate`, `IndiceCorrettoShufflato`, `IndiceDataShufflato` — usati con identico nome ovunque appaiono.

**Note di sicurezza:**
- Il `record _StatoLive` in C# 10 con .NET 8 è supportato (positional record, file-private, nessun problema).
- `DomandaPreparata` è una classe del Core già usata da `SessioneQuiz`: il backup ne tiene un riferimento, niente clonazione (è immutabile per uso quiz).
- `Risposte` è una `ObservableCollection<RispostaItem>` con binding diretto: `.Clear() + .Add()` è il pattern standard, già usato in `CaricaProssimaDomanda`. Niente trucchi.
