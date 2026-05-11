# Step 8 — Navigazione tastiera globale — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Estendere la navigazione tastiera fuori dal `QuizView` (modale pausa, Home, switch tab, liste Cronologia/Sospesi), con un indicatore visivo unico (bordo giallo `#FFD500` 3px) che appare solo per focus da tastiera.

**Architecture:** Approccio ibrido (focus nativo Avalonia + indice custom solo dove serve). Lo stile `FocusAdorner` globale in `App.axaml` produce il bordo giallo automaticamente su tutti i controlli che ricevono focus da tastiera. `Ctrl+Tab` gestito dalla `MainWindow.OnKeyDown`. Modale pausa: `←/→` + `Tab` nativo. Home: `KeyboardNavigation.TabNavigation="Once"` sui 4 contenitori + `OnKeyDown` per `↑/↓` dentro la zona. Liste: `OnKeyDown` per `Invio` e `Canc`.

**Tech Stack:** .NET 8, Avalonia 12.0.2, FluentTheme. Niente test automatici nel progetto — ogni task ha criteri di **verifica manuale** espliciti (avvio app, sequenza di tasti, osservazione visiva). Build con `dotnet build wsa.quiz.app/Wsa.Quiz.App.csproj`.

**Spec di riferimento:** `docs/superpowers/specs/2026-05-11-step8-navigazione-tastiera-globale-design.md`

**Note operative:**
- Tutti i path sono relativi alla root della repo `wsa_quiz_step1/`.
- `dotnet build wsa.quiz.app/Wsa.Quiz.App.csproj` deve completare senza errori prima di ogni commit.
- Avvio app: `dotnet run --project wsa.quiz.app/Wsa.Quiz.App.csproj`.
- Per ogni task la "verifica" è una sequenza precisa di azioni utente con esito atteso. Se non si verifica → NON committare, indaga.

---

### Task 1: Indicatore visivo focus globale (bordo giallo `#FFD500` 3px)

**Files:**
- Modify: `wsa.quiz.app/App.axaml`

In Avalonia 12 ogni controllo focusable ha la proprietà `FocusAdorner` (un `ITemplate<Control>`) che viene mostrata **solo** quando il focus arriva da tastiera, non da click. Ridefinendo lo stile globale di `Button`, `CheckBox`, `ListBoxItem`, `TabItem` con un `FocusAdornerTemplate` personalizzato, otteniamo il bordo giallo ovunque, senza codice extra nelle view.

- [ ] **Step 1: Aggiungere gli stili `FocusAdorner` globali in `App.axaml`**

Aprire `wsa.quiz.app/App.axaml` e aggiungere i seguenti stili **dopo** il blocco `Button.danger:disabled` (riga 64), ma prima di `</Application.Styles>` (riga 65):

```xml
        <!--
          Step 8: indicatore visivo unificato per il focus da tastiera.
          In Avalonia 12 il FocusAdorner viene mostrato SOLO quando il focus
          arriva da tastiera (Tab/frecce), non da click del mouse. Definendolo
          a livello globale otteniamo il bordo giallo 3px ovunque senza codice
          aggiuntivo nelle view. Stesso colore del highlight dello step 7.
        -->
        <Style Selector="Button">
            <Setter Property="FocusAdorner">
                <FocusAdornerTemplate>
                    <Border BorderBrush="#FFD500" BorderThickness="3" CornerRadius="4"/>
                </FocusAdornerTemplate>
            </Setter>
        </Style>
        <Style Selector="CheckBox">
            <Setter Property="FocusAdorner">
                <FocusAdornerTemplate>
                    <Border BorderBrush="#FFD500" BorderThickness="3" CornerRadius="4"/>
                </FocusAdornerTemplate>
            </Setter>
        </Style>
        <Style Selector="ListBoxItem">
            <Setter Property="FocusAdorner">
                <FocusAdornerTemplate>
                    <Border BorderBrush="#FFD500" BorderThickness="3" CornerRadius="4"/>
                </FocusAdornerTemplate>
            </Setter>
        </Style>
        <Style Selector="TabItem">
            <Setter Property="FocusAdorner">
                <FocusAdornerTemplate>
                    <Border BorderBrush="#FFD500" BorderThickness="3" CornerRadius="4"/>
                </FocusAdornerTemplate>
            </Setter>
        </Style>
        <Style Selector="NumericUpDown">
            <Setter Property="FocusAdorner">
                <FocusAdornerTemplate>
                    <Border BorderBrush="#FFD500" BorderThickness="3" CornerRadius="4"/>
                </FocusAdornerTemplate>
            </Setter>
        </Style>
```

- [ ] **Step 2: Build**

```bash
dotnet build wsa.quiz.app/Wsa.Quiz.App.csproj
```

Expected: build OK, niente warning XAML su `FocusAdorner` o `FocusAdornerTemplate`.

- [ ] **Step 3: Verifica manuale**

```bash
dotnet run --project wsa.quiz.app/Wsa.Quiz.App.csproj
```

Procedura:
1. Premi `Tab` dall'app appena aperta. **Atteso:** un controllo della Home (probabilmente il bottone "Avvia quiz" o un altro Button/CheckBox) mostra un bordo giallo 3px attorno.
2. Premi `Tab` più volte. **Atteso:** il bordo segue il controllo focused.
3. Clicca con il mouse su un qualsiasi bottone (es. "Avvia quiz" se disabilitato apri prima una checkbox materia, oppure "Aggiorna" in Cronologia). **Atteso:** NESSUN bordo giallo appare al click. Il bordo appare solo se premi `Tab` dopo.
4. Vai su tab Sospesi (click). Se ci sono pause da console, premi `Tab` finché atterri su un bottone "Riprendi"/"Elimina". **Atteso:** bordo giallo.

Se il bordo non appare: probabilmente Avalonia 12.0.2 non riconosce `FocusAdornerTemplate` come tag root. Fallback: sostituire `<FocusAdornerTemplate>` con `<Template>` (alcuni docs lo chiamano così). Vedi nota in spec sezione "Trappole anticipate".

- [ ] **Step 4: Commit**

```bash
git add wsa.quiz.app/App.axaml
git commit -m "feat(step8): stile FocusAdorner globale bordo giallo #FFD500 3px"
```

---

### Task 2: `Ctrl+Tab` / `Ctrl+Shift+Tab` per cambiare tab principale

**Files:**
- Modify: `wsa.quiz.app/MainWindow.axaml.cs`

Aggiungiamo un handler `OnKeyDown` su `MainWindow` che intercetta `Ctrl+Tab` e `Ctrl+Shift+Tab` prima dei figli e cambia `Tabs.SelectedIndex` in modo ciclico. Dopo lo switch, mettiamo il focus sulla view attiva (`HomeArea.Content`, `CronologiaArea.Content`, `SospesiArea.Content`) chiamando `Focus()` se è un `Control`.

- [ ] **Step 1: Aggiungere `using Avalonia.Input;` se manca**

Aprire `wsa.quiz.app/MainWindow.axaml.cs`. In cima al file, sotto `using System.Collections.Generic;`, aggiungere:

```csharp
using Avalonia.Input;
```

(Verificare che non sia già presente — se sì, saltare questo step.)

- [ ] **Step 2: Aggiungere override `OnKeyDown` in `MainWindow`**

Inserire il seguente metodo dentro la classe `MainWindow`, **dopo** il costruttore (riga 30) e **prima** del `// ----- BOOT` (riga 32):

```csharp
    // ------------------------------------------------------------------ TASTIERA GLOBALE

    /// <summary>
    /// Step 8: Ctrl+Tab / Ctrl+Shift+Tab cambiano tab principale in modo ciclico.
    /// Funziona ovunque nell'app, anche dentro al quiz (il quiz resta vivo sotto
    /// e si ritrova quando si torna sulla tab Home).
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Tab && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            int n = Tabs.ItemCount;
            if (n <= 0) { base.OnKeyDown(e); return; }
            int delta = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : +1;
            Tabs.SelectedIndex = (Tabs.SelectedIndex + delta + n) % n;
            // Sposto il focus nella view appena attivata cosi' le scorciatoie locali
            // partono immediate senza dover cliccare.
            switch (Tabs.SelectedIndex)
            {
                case 0: (HomeArea.Content as Control)?.Focus(); break;
                case 1: (CronologiaArea.Content as Control)?.Focus(); break;
                case 2: (SospesiArea.Content as Control)?.Focus(); break;
            }
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }
```

- [ ] **Step 3: Build**

```bash
dotnet build wsa.quiz.app/Wsa.Quiz.App.csproj
```

Expected: build OK.

- [ ] **Step 4: Verifica manuale**

```bash
dotnet run --project wsa.quiz.app/Wsa.Quiz.App.csproj
```

Procedura:
1. App aperta su tab Home. Premi `Ctrl+Tab`. **Atteso:** la tab diventa Cronologia.
2. `Ctrl+Tab` di nuovo. **Atteso:** Sospesi.
3. `Ctrl+Tab` di nuovo. **Atteso:** Home (ciclico).
4. `Ctrl+Shift+Tab` da Home. **Atteso:** Sospesi (al contrario).
5. Avvia un quiz (selezione materie + Avvia). Dentro al `QuizView`, premi `Ctrl+Tab`. **Atteso:** vai su Cronologia. `Ctrl+Tab` ancora → Sospesi. `Ctrl+Tab` ancora → Home, e si vede il quiz ancora in corso.
6. Premi `Tab` (senza Ctrl) dentro al quiz. **Atteso:** comportamento normale di focus dentro al quiz, NIENTE cambio tab.

- [ ] **Step 5: Commit**

```bash
git add wsa.quiz.app/MainWindow.axaml.cs
git commit -m "feat(step8): Ctrl+Tab / Ctrl+Shift+Tab ciclico fra tab principali"
```

---

### Task 3: Modale pausa — frecce `←/→` + `Ctrl+Tab` bloccato

**Files:**
- Modify: `wsa.quiz.app/Views/QuizView.axaml.cs`

La modale pausa (metodo `ApriMenuPausa`, righe 142-246) ha già `ESC = Annulla` e `Opened → annullaBtn.Focus()`. Aggiungiamo:
- `←/→`: spostano il focus ciclicamente fra i 3 bottoni (Annulla → Abbandona → Salva → Annulla).
- `Ctrl+Tab` / `Ctrl+Shift+Tab`: ignorati (`e.Handled = true`), così non cambiamo tab con la modale aperta.
- `Tab` / `Shift+Tab`: già funzionano nativi, non tocchiamo.
- `Invio`: già attiva il bottone focused (default Avalonia su `Click`).

Ordine visivo dei bottoni in pulsantiera:
- `abbandonaBtn` (sinistra, danger)
- `annullaBtn` (destra, normale)
- `salvaBtn` (destra, accent)

L'ordine ciclico per le frecce segue questo: Abbandona ↔ Annulla ↔ Salva (con wrap).

- [ ] **Step 1: Estendere l'handler `w.KeyDown` della modale**

In `wsa.quiz.app/Views/QuizView.axaml.cs`, individuare il blocco (righe 208-211):

```csharp
        // ESC dentro la modale = Annulla (chiude senza azione)
        w.KeyDown += (_, ke) =>
        {
            if (ke.Key == Key.Escape) { ke.Handled = true; azione = "annulla"; w.Close(); }
        };
```

Sostituirlo interamente con:

```csharp
        // Step 8: gestione tastiera estesa nella modale pausa.
        // ESC = Annulla; frecce sinistra/destra muovono il focus fra i 3 bottoni
        // in modo ciclico (ordine visivo: Abbandona, Annulla, Salva); Ctrl+Tab
        // bloccato per non cambiare tab con la modale aperta.
        var bottoniInOrdine = new Button[] { abbandonaBtn, annullaBtn, salvaBtn };
        w.KeyDown += (_, ke) =>
        {
            if (ke.Key == Key.Escape)
            {
                ke.Handled = true;
                azione = "annulla";
                w.Close();
                return;
            }
            if (ke.Key == Key.Tab && ke.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                // Blocca Ctrl+Tab dentro la modale: niente cambio tab globale.
                ke.Handled = true;
                return;
            }
            if (ke.Key == Key.Left || ke.Key == Key.Right)
            {
                int idx = System.Array.IndexOf(bottoniInOrdine,
                    global::Avalonia.Input.FocusManager.GetFocusManager(w)?.GetFocusedElement() as Button);
                if (idx < 0) idx = 1; // default su Annulla
                int delta = ke.Key == Key.Right ? +1 : -1;
                int nuovo = (idx + delta + bottoniInOrdine.Length) % bottoniInOrdine.Length;
                bottoniInOrdine[nuovo].Focus();
                ke.Handled = true;
            }
        };
```

- [ ] **Step 2: Build**

```bash
dotnet build wsa.quiz.app/Wsa.Quiz.App.csproj
```

Expected: build OK. Se errore "FocusManager.GetFocusManager non esistente", usare il fallback:

```csharp
int idx = System.Array.IndexOf(bottoniInOrdine, w.FocusManager?.GetFocusedElement() as Button);
```

(Avalonia 12 espone `Window.FocusManager` direttamente.)

- [ ] **Step 3: Verifica manuale**

Avvia app, avvia un quiz, dentro al quiz premi `Esc`.

Procedura nella modale aperta:
1. **Atteso all'apertura:** focus su "Annulla" (bordo giallo 3px attorno).
2. Premi `→`. **Atteso:** focus su "Salva e esci".
3. Premi `→`. **Atteso:** focus su "Abbandona" (wrap).
4. Premi `→`. **Atteso:** focus su "Annulla" (wrap).
5. Premi `←` da "Annulla". **Atteso:** focus su "Abbandona".
6. Premi `Tab`. **Atteso:** focus si sposta al bottone successivo nell'ordine di Tab nativo (probabilmente Annulla → Salva → Abbandona, dipende dal tab order di Avalonia).
7. Premi `Invio` con il focus su "Salva e esci". **Atteso:** la sessione viene salvata in Sospesi e la modale si chiude.
8. Riapri un quiz, premi `Esc`, premi `Ctrl+Tab`. **Atteso:** la tab principale NON cambia, la modale resta aperta.
9. Premi `Esc` con la modale aperta. **Atteso:** modale chiude, torni al quiz.

- [ ] **Step 4: Commit**

```bash
git add wsa.quiz.app/Views/QuizView.axaml.cs
git commit -m "feat(step8): modale pausa con frecce <-/-> e Ctrl+Tab bloccato"
```

---

### Task 4: Home — Tab fra zone (4 zone con `KeyboardNavigation.TabNavigation="Once"`)

**Files:**
- Modify: `wsa.quiz.app/Views/HomeView.axaml`

Vogliamo che premendo `Tab` dalla Home si vada **una zona alla volta** (Avvia → Materie → Categorie → Opzioni → Avvia ciclicamente). Avalonia ha la attached property `KeyboardNavigation.TabNavigation` con valore `Once`: applicata a un container, fa sì che `Tab` atterri sul primo elemento focusable del container e poi salti al container successivo.

I 4 contenitori da marcare con `TabNavigation="Once"` sono:
1. La `Border DockPanel.Dock="Top"` che contiene il bottone Avvia (righe 14-41).
2. La `Border` Materie (riga 51-72).
3. La `Border` Categorie (riga 75-123).
4. La `Border` Opzioni (riga 127-163).

Aggiungiamo anche un nome (`x:Name`) a ciascuna zona per individuarla nel task 5.

- [ ] **Step 1: Modificare le 4 Border in `HomeView.axaml`**

Aprire `wsa.quiz.app/Views/HomeView.axaml`.

a) Riga 14: `<Border DockPanel.Dock="Top"` → aggiungere `x:Name="ZonaAvvia" KeyboardNavigation.TabNavigation="Once"`:

```xml
        <Border x:Name="ZonaAvvia"
                KeyboardNavigation.TabNavigation="Once"
                DockPanel.Dock="Top"
                Background="{DynamicResource SystemRegionBrush}"
                BorderBrush="{DynamicResource SystemBaseLowColor}"
                BorderThickness="0,0,0,1"
                Padding="20,12,20,12">
```

b) Riga 51: `<Border Grid.Row="0" Grid.Column="0"` (Materie) → aggiungere `x:Name="ZonaMaterie" KeyboardNavigation.TabNavigation="Once"`:

```xml
                    <Border x:Name="ZonaMaterie"
                            KeyboardNavigation.TabNavigation="Once"
                            Grid.Row="0" Grid.Column="0"
                            Background="{DynamicResource SystemRegionBrush}"
                            BorderBrush="{DynamicResource SystemBaseLowColor}"
                            BorderThickness="1"
                            CornerRadius="6"
                            Padding="14">
```

c) Riga 75: `<Border Grid.Row="0" Grid.Column="2"` (Categorie) → aggiungere `x:Name="ZonaCategorie" KeyboardNavigation.TabNavigation="Once"`:

```xml
                    <Border x:Name="ZonaCategorie"
                            KeyboardNavigation.TabNavigation="Once"
                            Grid.Row="0" Grid.Column="2"
                            Background="{DynamicResource SystemRegionBrush}"
                            BorderBrush="{DynamicResource SystemBaseLowColor}"
                            BorderThickness="1"
                            CornerRadius="6"
                            Padding="14"
                            MinHeight="180">
```

d) Riga 127: `<Border Background="{DynamicResource SystemRegionBrush}"` (Opzioni) → aggiungere `x:Name="ZonaOpzioni" KeyboardNavigation.TabNavigation="Once"`:

```xml
                <Border x:Name="ZonaOpzioni"
                        KeyboardNavigation.TabNavigation="Once"
                        Background="{DynamicResource SystemRegionBrush}"
                        BorderBrush="{DynamicResource SystemBaseLowColor}"
                        BorderThickness="1"
                        CornerRadius="6"
                        Padding="14">
```

- [ ] **Step 2: Build**

```bash
dotnet build wsa.quiz.app/Wsa.Quiz.App.csproj
```

Expected: build OK. Se errore "KeyboardNavigation non trovato", aggiungere lo xmlns nel root se non c'è: lo namespace di default Avalonia copre `KeyboardNavigation` direttamente, non serve prefisso. In caso di problemi: `xmlns:i="https://github.com/avaloniaui"` è già il default, quindi `KeyboardNavigation.TabNavigation` deve risolvere.

- [ ] **Step 3: Verifica manuale**

```bash
dotnet run --project wsa.quiz.app/Wsa.Quiz.App.csproj
```

Procedura sulla Home:
1. Seleziona almeno una materia con il mouse (es. la prima checkbox in Materie). Questo abilita Avvia.
2. Clicca dentro la `MainWindow` (es. sulla barra del titolo o uno spazio neutro), poi premi `Tab` dall'inizio. **Atteso:** focus atterra sul primo controllo focusable della zona Avvia (= bottone "Avvia quiz") con bordo giallo.
3. Premi `Tab` di nuovo. **Atteso:** focus salta alla prima checkbox di Materie.
4. Premi `Tab` di nuovo. **Atteso:** focus salta alla prima checkbox di Categorie (se ci sono categorie disponibili) oppure salta direttamente a Opzioni se Categorie è vuoto.
5. Premi `Tab` di nuovo. **Atteso:** focus salta al primo controllo di Opzioni (probabilmente la checkbox "Randomizza").
6. Premi `Tab` di nuovo. **Atteso:** focus torna su "Avvia quiz" (ciclo).
7. `Shift+Tab` percorre l'ordine inverso.

Se Tab passa fra TUTTE le singole checkbox di Materie invece di saltare alla zona successiva, `TabNavigation="Once"` non è stato applicato correttamente — verifica spelling e che sia sull'attributo del Border esterno della zona.

- [ ] **Step 4: Commit**

```bash
git add wsa.quiz.app/Views/HomeView.axaml
git commit -m "feat(step8): Home navigabile a 4 zone con Tab (TabNavigation=Once)"
```

---

### Task 5: Home — frecce `↑/↓` dentro la zona corrente

**Files:**
- Modify: `wsa.quiz.app/Views/HomeView.axaml.cs`

Aggiungiamo l'override `OnKeyDown` sulla `HomeView` che, su `↑` o `↓`, identifica la zona di appartenenza del controllo focused (risalendo i visual ancestor fino a trovare un Border con `x:Name="ZonaXxx"`) e sposta il focus al controllo focusable precedente/successivo dentro quella zona.

Filtri:
- Se il focused è un `NumericUpDown`, **non** intercettiamo `↑/↓` (lasciamo che incrementi/decrementi).
- `Spazio` su `CheckBox` resta nativo (toggle).
- Le frecce non wrappano alla zona dopo — solo dentro la stessa zona.

- [ ] **Step 1: Aggiornare gli `using` di `HomeView.axaml.cs`**

In cima a `wsa.quiz.app/Views/HomeView.axaml.cs`, dopo `using Avalonia.Controls;`, aggiungere:

```csharp
using Avalonia.Input;
using Avalonia.VisualTree;
```

(Verificare che non siano già presenti.)

- [ ] **Step 2: Aggiungere override `OnKeyDown` nella `HomeView`**

Inserire il seguente metodo dentro la classe `HomeView`, **dopo** il costruttore `HomeView()` (riga 114) e **prima** del metodo `Inizializza` (riga 120):

```csharp
    // ------------------------------------------------------------------ TASTIERA HOME (step 8)

    /// <summary>
    /// Step 8: dentro una zona (Materie, Categorie, Opzioni, Avvia) le frecce su/giu'
    /// spostano il focus fra i controlli focusable della zona stessa.
    /// Filtro: sul NumericUpDown lasciamo che le frecce incrementino il valore.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key != Key.Up && e.Key != Key.Down)
        {
            base.OnKeyDown(e);
            return;
        }

        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control;
        if (focused == null) { base.OnKeyDown(e); return; }

        // Se il focus e' su un NumericUpDown (o suo interno), lascia il comportamento nativo.
        if (focused is NumericUpDown || focused.FindAncestorOfType<NumericUpDown>() != null)
        {
            base.OnKeyDown(e);
            return;
        }

        // Trova il Border-zona piu' vicino antenato.
        var zona = focused.GetVisualAncestors()
            .OfType<Border>()
            .FirstOrDefault(b => b.Name is "ZonaAvvia" or "ZonaMaterie" or "ZonaCategorie" or "ZonaOpzioni");
        if (zona == null) { base.OnKeyDown(e); return; }

        // Raccogli tutti i controlli focusable dentro la zona, in ordine visivo.
        var focusables = zona.GetVisualDescendants()
            .OfType<Control>()
            .Where(c => c.Focusable && c.IsEffectivelyVisible && c.IsEffectivelyEnabled)
            .Where(c => c is Button || c is CheckBox || c is NumericUpDown)
            .ToList();
        if (focusables.Count == 0) { base.OnKeyDown(e); return; }

        int idx = focusables.IndexOf(focused);
        if (idx < 0)
        {
            // Il focused non e' uno dei nostri candidati: prendi il primo.
            focusables[0].Focus();
            e.Handled = true;
            return;
        }

        int delta = e.Key == Key.Down ? +1 : -1;
        int nuovo = idx + delta;
        // No wrap: se sforiamo, restiamo dove siamo (Tab serve a cambiare zona).
        if (nuovo < 0 || nuovo >= focusables.Count) { e.Handled = true; return; }
        focusables[nuovo].Focus();
        e.Handled = true;
    }
```

- [ ] **Step 3: Build**

```bash
dotnet build wsa.quiz.app/Wsa.Quiz.App.csproj
```

Expected: build OK. Se errore `GetVisualAncestors` o `GetVisualDescendants` non trovati, verificare `using Avalonia.VisualTree;`. Se errore `FindAncestorOfType`, `using Avalonia.VisualTree;` lo copre.

- [ ] **Step 4: Verifica manuale**

Avvia app sulla Home.

Procedura:
1. Premi `Tab` finché il focus arriva sulla prima checkbox di Materie. Premi `↓`. **Atteso:** focus passa alla seconda materia (bordo giallo).
2. `↓` ancora. **Atteso:** terza materia.
3. `↓` finché sei sull'ultima. `↓` ancora. **Atteso:** focus resta sull'ultima (no wrap), `Tab` serve a uscire dalla zona.
4. `↑` torna alla prima.
5. `Spazio` sulla checkbox focused. **Atteso:** toggla normalmente (e cambia le categorie visibili).
6. Premi `Tab` per uscire dalla zona Materie → atterri su Categorie (se ci sono) o Opzioni.
7. In Opzioni: `Tab` finché atterri sulla prima checkbox di Opzioni. `↓`/`↑` muove fra le checkbox.
8. Premi `Tab` finché atterri sul `NumericUpDown` "Limita a". Premi `↑`. **Atteso:** il valore numerico aumenta di 5 (`Increment="5"`), il focus NON si sposta.
9. `↓` sul NumericUpDown decrementa.

- [ ] **Step 5: Commit**

```bash
git add wsa.quiz.app/Views/HomeView.axaml.cs
git commit -m "feat(step8): Home navigazione su/giu' dentro la zona corrente"
```

---

### Task 6: CronologiaView — `Invio` apre dettaglio, `Canc` avvia conferma inline

**Files:**
- Modify: `wsa.quiz.app/Views/CronologiaView.axaml.cs`

La `CronologiaView` ha già un `ListBox` (`x:Name="ListaSessioni"`) con frecce native. Aggiungiamo `OnKeyDown` sulla view che:

- `Invio` quando il focus è dentro al `ListaSessioni` e c'è un `SelectedItem` → apre dettaglio (replica di `OnSessioneDoubleTapped`).
- `Canc` quando c'è un `SelectedItem` e la riga NON è in stato di conferma → trasforma la riga in stato conferma (replica di `OnEliminaRigaClick` ma sulla riga selezionata).
- `Canc` quando la riga selezionata è già in conferma → conferma definitiva (replica di `OnConfermaEliminaRigaClick`).
- `Esc` quando la riga selezionata è in conferma → annulla la conferma.

- [ ] **Step 1: Aggiornare gli `using` di `CronologiaView.axaml.cs`**

Verificare che `using Avalonia.Input;` sia già presente (lo è — riga 8). Niente da aggiungere.

- [ ] **Step 2: Aggiungere override `OnKeyDown` nella `CronologiaView`**

Inserire il seguente metodo dentro la classe `CronologiaView`, **dopo** il metodo `OnAnnullaEliminaRigaClick` (riga 161) e **prima** del metodo `ApriDettaglio` (riga 163):

```csharp
    // ------------------------------------------------------------------ TASTIERA (step 8)

    /// <summary>
    /// Step 8: Invio apre il dettaglio della riga selezionata; Canc avvia la
    /// conferma inline (e secondo Canc la conferma definitiva); Esc annulla la
    /// conferma se attiva. Attivo solo quando NON siamo nel dettaglio.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (ModoDettaglio) { base.OnKeyDown(e); return; }

        if (ListaSessioni.SelectedItem is not RisultatoCronologiaItem item)
        {
            base.OnKeyDown(e);
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                ApriDettaglio(item);
                return;

            case Key.Delete:
                e.Handled = true;
                if (item.InAttesaConfermaEliminazione)
                {
                    // Secondo Canc: conferma definitiva.
                    EseguiEliminazione(item);
                }
                else
                {
                    // Primo Canc: avvia conferma inline (replica OnEliminaRigaClick).
                    foreach (var s in Sessioni) s.InAttesaConfermaEliminazione = false;
                    item.InAttesaConfermaEliminazione = true;
                }
                return;

            case Key.Escape:
                if (item.InAttesaConfermaEliminazione)
                {
                    e.Handled = true;
                    item.InAttesaConfermaEliminazione = false;
                }
                return;
        }

        base.OnKeyDown(e);
    }

    /// <summary>Estratto da OnConfermaEliminaRigaClick: serve a poter eliminare anche da tastiera (Canc).</summary>
    private void EseguiEliminazione(RisultatoCronologiaItem item)
    {
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
```

- [ ] **Step 3: Rifattorizzare `OnConfermaEliminaRigaClick` per usare `EseguiEliminazione`**

Sostituire il corpo di `OnConfermaEliminaRigaClick` (righe 132-153) con la chiamata al nuovo metodo:

```csharp
    /// <summary>Conferma definitiva: elimina dal disco e dalla lista.</summary>
    private void OnConfermaEliminaRigaClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        if (b.DataContext is not RisultatoCronologiaItem item) return;
        EseguiEliminazione(item);
    }
```

- [ ] **Step 4: Build**

```bash
dotnet build wsa.quiz.app/Wsa.Quiz.App.csproj
```

Expected: build OK.

- [ ] **Step 5: Verifica manuale**

Avvia app. Vai su tab Cronologia (Ctrl+Tab x1 o click). Serve almeno una sessione in cronologia per testare — se vuota, fai prima un mini-quiz e torna.

Procedura:
1. Premi `Tab` finché il focus arriva in `ListaSessioni` (bordo giallo su un `ListBoxItem`). Premi `↓`/`↑`. **Atteso:** la selezione (background azzurro nativo) si muove fra le righe; il bordo giallo segue.
2. Premi `Invio` con una riga selezionata. **Atteso:** si apre il `CronologiaDettaglioView`.
3. Click su "← Cronologia" (o vedi Task 8 per `Esc`). Torni alla lista.
4. Seleziona di nuovo una riga con `↓/↑`. Premi `Canc`. **Atteso:** la riga mostra "Si', elimina / Annulla" (= conferma inline).
5. Premi `Esc`. **Atteso:** la conferma annullata, la riga torna in stato normale.
6. Premi `Canc` di nuovo. La riga è in conferma. Premi `Canc` ancora. **Atteso:** la riga viene eliminata dalla lista (e da disco).
7. Verifica che cliccando "Si', elimina" col mouse l'eliminazione funzioni ancora (non abbiamo rotto il flusso esistente).

- [ ] **Step 6: Commit**

```bash
git add wsa.quiz.app/Views/CronologiaView.axaml.cs
git commit -m "feat(step8): Cronologia Invio=apri dettaglio, Canc=conferma elimina"
```

---

### Task 7: SospesiView — convertire a `ListBox` + `Invio` Riprende + `Canc` conferma

**Files:**
- Modify: `wsa.quiz.app/Views/SospesiView.axaml`
- Modify: `wsa.quiz.app/Views/SospesiView.axaml.cs`

`SospesiView` usa `ItemsControl`, non `ListBox` — quindi non c'è oggi una "selezione" da tastiera. Convertiamo `ItemsControl` → `ListBox` con `SelectionMode="Single"`. Poi aggiungiamo `OnKeyDown` simile a Cronologia: `Invio` = Riprendi, `Canc` = conferma elimina, `Esc` = annulla conferma.

- [ ] **Step 1: Convertire `ItemsControl` → `ListBox` in `SospesiView.axaml`**

In `wsa.quiz.app/Views/SospesiView.axaml`, righe 59-168 (il blocco `<ItemsControl>...</ItemsControl>`):

Cambiare l'apertura (riga 59):

```xml
            <ItemsControl ItemsSource="{Binding Sessioni}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate DataType="state:SessioneSospesaItem">
```

in:

```xml
            <ListBox x:Name="ListaPause"
                     ItemsSource="{Binding Sessioni}"
                     SelectionMode="Single"
                     Background="Transparent"
                     BorderThickness="0">
                <ListBox.ItemTemplate>
                    <DataTemplate DataType="state:SessioneSospesaItem">
```

E la chiusura (righe 167-168):

```xml
                </ItemsControl.ItemTemplate>
            </ItemsControl>
```

in:

```xml
                </ListBox.ItemTemplate>
            </ListBox>
```

- [ ] **Step 2: Aggiungere override `OnKeyDown` in `SospesiView.axaml.cs`**

Inserire il seguente metodo dentro la classe `SospesiView`, **dopo** il metodo `OnAnnullaEliminaClick` (riga 147) e **prima** del commento `// ------- INPC`:

```csharp
    // ------------------------------------------------------------------ TASTIERA (step 8)

    /// <summary>
    /// Step 8: Invio sulla riga selezionata = Riprendi; Canc avvia conferma
    /// inline (secondo Canc = conferma definitiva); Esc annulla la conferma.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (ListaPause.SelectedItem is not SessioneSospesaItem item)
        {
            base.OnKeyDown(e);
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                RiprendiRichiesto?.Invoke(this, item.Pausa);
                return;

            case Key.Delete:
                e.Handled = true;
                if (item.InAttesaConfermaEliminazione)
                {
                    EseguiEliminazione(item);
                }
                else
                {
                    foreach (var s in Sessioni) s.InAttesaConfermaEliminazione = false;
                    item.InAttesaConfermaEliminazione = true;
                }
                return;

            case Key.Escape:
                if (item.InAttesaConfermaEliminazione)
                {
                    e.Handled = true;
                    item.InAttesaConfermaEliminazione = false;
                }
                return;
        }

        base.OnKeyDown(e);
    }

    /// <summary>Estratto da OnConfermaEliminaClick: serve a poter eliminare anche da tastiera.</summary>
    private void EseguiEliminazione(SessioneSospesaItem item)
    {
        if (_storage == null) return;
        try
        {
            _storage.EliminaPausa(item.SessioneId);
        }
        catch (Exception ex)
        {
            Sottotitolo = $"Errore nell'eliminazione: {ex.Message}";
            return;
        }

        Sessioni.Remove(item);
        NessunaPausa = Sessioni.Count == 0;
        Sottotitolo = NessunaPausa
            ? "Nessuna sessione in pausa."
            : $"{Sessioni.Count} sessioni in pausa.";
    }
```

- [ ] **Step 3: Aggiungere `using Avalonia.Input;` se manca**

Verificare in cima a `SospesiView.axaml.cs`. Se l'`using Avalonia.Input;` non è presente, aggiungerlo sotto `using Avalonia.Controls;` (riga 6).

- [ ] **Step 4: Rifattorizzare `OnConfermaEliminaClick` per usare `EseguiEliminazione`**

Sostituire il corpo di `OnConfermaEliminaClick` (righe 117-139) con:

```csharp
    /// <summary>Conferma definitiva: elimina dal disco e dalla lista.</summary>
    private void OnConfermaEliminaClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        if (b.DataContext is not SessioneSospesaItem item) return;
        EseguiEliminazione(item);
    }
```

- [ ] **Step 5: Build**

```bash
dotnet build wsa.quiz.app/Wsa.Quiz.App.csproj
```

Expected: build OK. Se errore "ListBox non riconosce SelectionMode", verificare che il namespace di default sia Avalonia (`xmlns="https://github.com/avaloniaui"`).

- [ ] **Step 6: Verifica manuale**

Avvia app. Avvia un quiz, premi `Esc`, scegli "Salva e esci" (per avere almeno una pausa). Vai su tab Sospesi (`Ctrl+Tab` x2).

Procedura:
1. Premi `Tab` finché il focus entra nella lista delle pause (bordo giallo). `↓`/`↑` muovono la selezione.
2. Premi `Invio` con una riga selezionata. **Atteso:** la sessione viene ripresa, l'app naviga al `QuizView`.
3. Torna ai Sospesi (chiudi il quiz con Esc → Abbandona, oppure rimettilo in pausa per ricreare lo scenario).
4. Seleziona una pausa con `↓`. Premi `Canc`. **Atteso:** la riga mostra "Sicuro? Sì, elimina / Annulla".
5. Premi `Esc`. **Atteso:** conferma annullata.
6. Premi `Canc` di nuovo. Premi `Canc` ancora. **Atteso:** la pausa eliminata.
7. Verifica che i bottoni "Riprendi" / "Elimina" / "Sì, elimina" / "Annulla" col mouse continuano a funzionare.

- [ ] **Step 7: Commit**

```bash
git add wsa.quiz.app/Views/SospesiView.axaml wsa.quiz.app/Views/SospesiView.axaml.cs
git commit -m "feat(step8): Sospesi convertito a ListBox + Invio/Canc da tastiera"
```

---

### Task 8: CronologiaDettaglioView — `Esc` chiude e torna alla lista

**Files:**
- Modify: `wsa.quiz.app/Views/CronologiaDettaglioView.axaml.cs`

Quando l'utente è dentro al `CronologiaDettaglioView`, premendo `Esc` deve tornare alla lista (= stesso effetto del bottone "Indietro"/"← Cronologia"). Inoltre se è in stato di conferma eliminazione, `Esc` annulla la conferma (senza chiudere il dettaglio).

- [ ] **Step 1: Aggiornare gli `using`**

In cima a `wsa.quiz.app/Views/CronologiaDettaglioView.axaml.cs`, verificare la presenza di `using Avalonia.Input;`. Se manca, aggiungerlo sotto `using Avalonia.Controls;` (riga 5).

- [ ] **Step 2: Aggiungere override `OnKeyDown`**

Inserire il seguente metodo dentro la classe `CronologiaDettaglioView`, **dopo** il metodo `OnConfermaEliminaClick` (riga 124) e **prima** del metodo `Raise` (riga 126):

```csharp
    // ------------------------------------------------------------------ TASTIERA (step 8)

    /// <summary>
    /// Step 8: Esc chiude il dettaglio e torna alla lista (= bottone Indietro).
    /// Se la conferma di eliminazione e' attiva, Esc la annulla invece di chiudere.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            if (InAttesaConfermaEliminazione)
            {
                InAttesaConfermaEliminazione = false;
            }
            else
            {
                IndietroRichiesto?.Invoke(this, EventArgs.Empty);
            }
            return;
        }
        base.OnKeyDown(e);
    }
```

- [ ] **Step 3: Rendere la view `Focusable` e prenderne il focus all'apertura**

Per ricevere `KeyDown` come `UserControl`, la view deve essere `Focusable=true` e prendere il focus quando viene attaccata. Aggiungere queste righe alla **fine** del costruttore `CronologiaDettaglioView`, dopo `DataContext = this;` (riga 96):

```csharp
        Focusable = true;
        AttachedToVisualTree += (_, _) => Focus();
```

- [ ] **Step 4: Build**

```bash
dotnet build wsa.quiz.app/Wsa.Quiz.App.csproj
```

Expected: build OK.

- [ ] **Step 5: Verifica manuale**

Avvia app, vai su Cronologia, apri il dettaglio di una sessione (Invio dal Task 6, o doppio click).

Procedura:
1. Dentro al dettaglio, premi `Esc`. **Atteso:** torni alla lista cronologia.
2. Riapri il dettaglio. Clicca su "Elimina questa partita" (o naviga col Tab). La view mostra "Sì, elimina / Annulla" inline.
3. Premi `Esc`. **Atteso:** la conferma viene annullata, ma resti dentro al dettaglio (NON torna alla lista).
4. Premi `Esc` di nuovo. **Atteso:** torni alla lista.

- [ ] **Step 6: Commit**

```bash
git add wsa.quiz.app/Views/CronologiaDettaglioView.axaml.cs
git commit -m "feat(step8): CronologiaDettaglio chiude con Esc"
```

---

### Task 9: Aggiornare `WSA_QUIZ_HANDOFF.md` con stato finale step 8

**Files:**
- Modify: `WSA_QUIZ_HANDOFF.md`

Aggiornare il documento di handoff: marcare lo step 8 come completo, riassumere quanto fatto, e aggiungere eventuali trappole scoperte durante l'implementazione.

- [ ] **Step 1: Aggiornare la sezione "Stato attuale" (riga 11)**

Cambiare il titolo da `## Stato attuale: STEP 7 COMPLETO` a `## Stato attuale: STEP 8 COMPLETO`.

Sostituire il paragrafo descrittivo (righe 13-15) con un nuovo paragrafo che riassume lo step 8: navigazione tastiera globale ovunque (Ctrl+Tab fra tab, frecce dentro la modale pausa, Tab fra zone + frecce dentro la zona in Home, Invio/Canc nelle liste Cronologia/Sospesi, Esc per chiudere il dettaglio cronologia), indicatore visivo unico bordo giallo `#FFD500` 3px tramite `FocusAdorner` globale in `App.axaml`. Coerente con lo step 7.

- [ ] **Step 2: Cambiare lo step 8 da ⏳ a ✅ nella Roadmap**

Trovare la riga `### ⏳ Step 8 — Navigazione tastiera globale` (riga 162). Cambiarla in `### ✅ Step 8 — Navigazione tastiera globale`.

Sostituire il paragrafo descrittivo dello step 8 (riga 163) con un riassunto di quanto effettivamente implementato (4 task chiave: FocusAdorner globale, Ctrl+Tab in MainWindow, modale pausa con frecce, Home a 4 zone, liste Invio/Canc, dettaglio cronologia Esc). Citare spec e plan:

```
### ✅ Step 8 — Navigazione tastiera globale
Indicatore visivo unificato `#FFD500` 3px via `FocusAdorner` globale in `App.axaml` (compare solo per focus da tastiera, non da click). `Ctrl+Tab` / `Ctrl+Shift+Tab` in `MainWindow.OnKeyDown` cambia tab ciclicamente (Home → Cronologia → Sospesi → Home), funziona anche dentro al quiz. Modale pausa con `←/→` ciclici sui 3 bottoni (Abbandona/Annulla/Salva e esci), `Ctrl+Tab` bloccato dentro la modale. Home navigata a 4 zone (`ZonaAvvia`, `ZonaMaterie`, `ZonaCategorie`, `ZonaOpzioni`) con `KeyboardNavigation.TabNavigation="Once"` per saltare fra zone con Tab + `↑/↓` dentro la zona via `OnKeyDown` custom (skip se focused è NumericUpDown). `CronologiaView` (già `ListBox`) e `SospesiView` (convertita da `ItemsControl` a `ListBox`) con `Invio` apre dettaglio/Riprende, `Canc` avvia conferma inline (secondo `Canc` conferma definitiva, `Esc` annulla). `CronologiaDettaglioView` chiude con `Esc` (o annulla la conferma se attiva). Spec: `docs/superpowers/specs/2026-05-11-step8-navigazione-tastiera-globale-design.md`. Plan: `docs/superpowers/plans/2026-05-11-step8-navigazione-tastiera-globale.md`. **Fatto.**
```

- [ ] **Step 3: Aggiungere eventuali nuove trappole**

Se durante l'implementazione sono emerse trappole nuove (es. comportamenti inattesi di `FocusAdorner`, di `KeyboardNavigation.TabNavigation`, di `FocusManager` in Avalonia 12), aggiungerle in fondo alla sezione "Trappole già scoperte" (righe 183-196) come voci numerate 13+ con stesso stile (titolo bold + descrizione + soluzione adottata).

Se nessuna trappola nuova è emersa, saltare questo step.

- [ ] **Step 4: Aggiornare la sezione "Per ripartire" (righe 236-240)**

Aggiornare il punto 2 e 3:

```markdown
1. Apri questo MD in chat
2. Conferma stato attuale (sandbox Windows, step 8 funzionante: navigazione tastiera completa in tutta l'app)
3. Si parte dallo step 9 (Grafici LiveCharts2 in nuova tab Statistiche) — da verificare al momento dell'installazione una versione compatibile con Avalonia 12.
```

- [ ] **Step 5: Commit**

```bash
git add WSA_QUIZ_HANDOFF.md
git commit -m "docs(step8): handoff aggiornato — step 8 completo"
```

---

## Self-Review note

Il plan implementa tutte le sezioni della spec:

- **Architettura visiva** → Task 1 (FocusAdorner globale)
- **Sezione 1 (Switch tab Ctrl+Tab)** → Task 2
- **Sezione 2 (Modale pausa frecce + focus iniziale + Ctrl+Tab bloccato)** → Task 3 (focus iniziale su Annulla già esistente, le frecce e il blocco sono i nuovi)
- **Sezione 3 (Home 4 zone)** → Task 4 + Task 5
- **Sezione 4 (Liste Cronologia/Sospesi + CronologiaDettaglio Esc)** → Task 6, 7, 8

Nessuna sezione scoperta. Il test manuale (sezione 7 della spec, 10 punti) è coperto distribuendo i 10 punti fra i task — ciascuna verifica manuale di task copre la sua porzione di test end-to-end.

Tipologie e nomi consistenti fra task:
- `ListaSessioni` (Cronologia, già esistente in XAML) ↔ `ListaPause` (Sospesi, nuovo). Nomi diversi ma documentati.
- `EseguiEliminazione(...)` esiste sia in `CronologiaView` sia in `SospesiView` come metodo privato — naming identico, signature diversa (`RisultatoCronologiaItem` vs `SessioneSospesaItem`). Coerente.
- Zone Home: `ZonaAvvia`, `ZonaMaterie`, `ZonaCategorie`, `ZonaOpzioni`. Riferimenti consistenti fra Task 4 (XAML) e Task 5 (C# match in `OnKeyDown`).

Nessun placeholder.
