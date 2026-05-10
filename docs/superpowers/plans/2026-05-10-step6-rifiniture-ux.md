# Step 6 — UX rifiniture (Home sticky + Pausa unificata) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rendere la barra "Avvia quiz" della Home sempre visibile in basso (sticky) anche con liste lunghe, e fondere le due modali "Abbandona" / "Menu pausa" del Quiz in una sola modale con tre azioni (Annulla / Abbandona / Salva e esci) raggiungibile sia dal bottone "Pausa" sia da ESC.

**Architecture:** Modifiche solo a livello di view Avalonia. `HomeView.axaml` passa da `ScrollViewer` root a `DockPanel` root con barra "AVVIO" come `DockPanel.Dock="Bottom"`. `QuizView.axaml(.cs)` rinomina il bottone "Abbandona" → "Pausa", elimina il metodo `ChiediConfermaAbbandono()`, e riscrive `ApriMenuPausa()` con layout 480×210 a tre bottoni. Nessuna modifica a `Wsa.Quiz.Core`, a `MainWindow`, alla `SessioneQuiz`, allo storage o al flusso di ripresa pausa.

**Tech Stack:** .NET 8, Avalonia 12.0.2 (Fluent), C# 12, code-behind + INPC (no MVVM). Stili globali `Button.accent` / `Button.danger` già definiti in `App.axaml`. Build: `dotnet build Wsa.Quiz.sln`. Run: `dotnet run --project wsa.quiz.app`.

**Spec di riferimento:** `docs/superpowers/specs/2026-05-09-step6-rifiniture-ux-design.md`

**Note sul testing:** Il progetto non ha un framework di test automatici. La spec definisce 13 acceptance test manuali (5 Home + 8 Pausa). Il gate di verifica usato per ogni task è `dotnet build` con 0 errori e 0 warning sui file toccati; gli acceptance test sono raccolti in un'unica Task 6 da eseguire manualmente alla fine.

---

## File Structure

**Modificati:**
- `wsa.quiz.app/Views/HomeView.axaml` — root `ScrollViewer` → `DockPanel`; estrazione barra "AVVIO" come Bottom; wrap di Materie in `ScrollViewer MaxHeight=240`.
- `wsa.quiz.app/Views/QuizView.axaml` — rinomina bottone header `Abbandona` → `Pausa` e relativo `Click`.
- `wsa.quiz.app/Views/QuizView.axaml.cs` — rinomina `OnAbbandonaClick` → `OnPausaClick`; rimozione `ChiediConfermaAbbandono()`; riscrittura `ApriMenuPausa()` (Window 480×210, 3 bottoni, variabile `azione` con valori `"annulla" | "abbandona" | "salva"`).

**Non toccati:** `wsa.quiz.app/Views/HomeView.axaml.cs` (gli handler `OnAvviaClick`, `OnPulisciCategorieClick` mantengono firma e nome), `MainWindow`, `SessioneQuiz`, `StorageService`, `App.axaml`, tutto `wsa.quiz.core` e `wsa.quiz.cli`.

---

## Task 1: Home — passaggio a DockPanel root con barra "AVVIO" sticky

**Files:**
- Modify: `wsa.quiz.app/Views/HomeView.axaml` (intero contenuto del root `<ScrollViewer>`)

**Obiettivo:** sostituire il root `<ScrollViewer Padding="20">` con un `<DockPanel>`. La Grid "AVVIO" (oggi ultimo child dello `StackPanel`) viene estratta e diventa il primo child del `DockPanel`, wrappata in una `<Border>` con `DockPanel.Dock="Bottom"`. Il resto del contenuto (titoli, Materie/Categorie, Opzioni) resta dentro un `<ScrollViewer Padding="20">` che riempie l'area sopra la barra.

- [ ] **Step 1: Sostituire l'intero contenuto di `HomeView.axaml` con la nuova struttura**

Path esatto: `wsa.quiz.app/Views/HomeView.axaml`. Sostituisci dalle righe 11-165 (l'intero blocco da `<ScrollViewer Padding="20">` fino a `</ScrollViewer>`) con:

```xml
    <DockPanel>

        <!-- ============================== AVVIO (sticky) ============================== -->
        <Border DockPanel.Dock="Bottom"
                Background="{DynamicResource SystemRegionBrush}"
                BorderBrush="{DynamicResource SystemBaseLowColor}"
                BorderThickness="0,1,0,0"
                Padding="20,12,20,12">
            <Grid ColumnDefinitions="*,Auto">
                <StackPanel>
                    <TextBlock Text="{Binding RiepilogoSelezione}"
                               FontWeight="SemiBold"/>
                    <TextBlock Text="{Binding AvvisoSelezione}"
                               Foreground="#B85450"
                               IsVisible="{Binding HaAvviso}"
                               FontSize="12"
                               TextWrapping="Wrap"
                               Margin="0,4,0,0"/>
                </StackPanel>

                <Button Grid.Column="1"
                        Content="Avvia quiz"
                        Click="OnAvviaClick"
                        IsEnabled="{Binding PuoAvviare}"
                        Classes="accent"
                        Padding="22,10"
                        FontSize="14"
                        FontWeight="SemiBold"
                        VerticalAlignment="Center"/>
            </Grid>
        </Border>

        <!-- ============================== CONTENUTO SCROLLABILE ============================== -->
        <ScrollViewer Padding="20">
            <StackPanel Spacing="16" MaxWidth="980" HorizontalAlignment="Stretch">

                <TextBlock Text="Configura un nuovo quiz"
                           FontSize="22" FontWeight="SemiBold"/>
                <TextBlock Text="Scegli una o piu' materie. Se vuoi, restringi a categorie specifiche e regola le opzioni della sessione."
                           Foreground="{DynamicResource SystemBaseMediumColor}"
                           TextWrapping="Wrap"/>

                <!-- Materie + Categorie -->
                <Grid ColumnDefinitions="*,16,2*" RowDefinitions="Auto,Auto" Margin="0,4,0,0">

                    <!-- ============================== MATERIE ============================== -->
                    <Border Grid.Row="0" Grid.Column="0"
                            Background="{DynamicResource SystemRegionBrush}"
                            BorderBrush="{DynamicResource SystemBaseLowColor}"
                            BorderThickness="1"
                            CornerRadius="6"
                            Padding="14">
                        <DockPanel>
                            <TextBlock DockPanel.Dock="Top"
                                       Text="Materie"
                                       FontWeight="SemiBold"
                                       Margin="0,0,0,8"/>
                            <ScrollViewer MaxHeight="240"
                                          VerticalScrollBarVisibility="Auto">
                                <ItemsControl ItemsSource="{Binding Materie}">
                                    <ItemsControl.ItemTemplate>
                                        <DataTemplate DataType="state:MateriaSelezionabile">
                                            <CheckBox Content="{Binding Etichetta}"
                                                      IsChecked="{Binding IsSelected, Mode=TwoWay}"
                                                      Margin="0,3"/>
                                        </DataTemplate>
                                    </ItemsControl.ItemTemplate>
                                </ItemsControl>
                            </ScrollViewer>
                        </DockPanel>
                    </Border>

                    <!-- ============================== CATEGORIE ============================== -->
                    <Border Grid.Row="0" Grid.Column="2"
                            Background="{DynamicResource SystemRegionBrush}"
                            BorderBrush="{DynamicResource SystemBaseLowColor}"
                            BorderThickness="1"
                            CornerRadius="6"
                            Padding="14"
                            MinHeight="180">
                        <DockPanel>
                            <Grid DockPanel.Dock="Top" ColumnDefinitions="*,Auto" Margin="0,0,0,8">
                                <TextBlock Text="Categorie  (lascia vuoto per includerle tutte)"
                                           FontWeight="SemiBold"
                                           VerticalAlignment="Center"/>
                                <Button Grid.Column="1"
                                        Content="Pulisci"
                                        Click="OnPulisciCategorieClick"
                                        Padding="10,3"
                                        FontSize="11"
                                        IsEnabled="{Binding HaCategorieSelezionate}"/>
                            </Grid>

                            <TextBlock x:Name="EmptyCategorieText"
                                       Text="Seleziona una materia per vedere le categorie disponibili."
                                       Foreground="{DynamicResource SystemBaseMediumColor}"
                                       FontStyle="Italic"
                                       TextWrapping="Wrap"
                                       IsVisible="{Binding NessunaCategoriaDisponibile}"/>

                            <ScrollViewer MaxHeight="240"
                                          VerticalScrollBarVisibility="Auto"
                                          IsVisible="{Binding !NessunaCategoriaDisponibile}">
                                <ItemsControl ItemsSource="{Binding CategorieVisibili}">
                                    <ItemsControl.ItemTemplate>
                                        <DataTemplate DataType="state:CategoriaSelezionabile">
                                            <Grid ColumnDefinitions="*,Auto" Margin="0,2">
                                                <CheckBox Content="{Binding Etichetta}"
                                                          IsChecked="{Binding IsSelected, Mode=TwoWay}"/>
                                                <TextBlock Grid.Column="1"
                                                           Text="{Binding MateriaNome}"
                                                           FontSize="11"
                                                           Foreground="{DynamicResource SystemBaseMediumColor}"
                                                           VerticalAlignment="Center"
                                                           Margin="8,0,0,0"/>
                                            </Grid>
                                        </DataTemplate>
                                    </ItemsControl.ItemTemplate>
                                </ItemsControl>
                            </ScrollViewer>
                        </DockPanel>
                    </Border>
                </Grid>

                <!-- ============================== OPZIONI ============================== -->
                <Border Background="{DynamicResource SystemRegionBrush}"
                        BorderBrush="{DynamicResource SystemBaseLowColor}"
                        BorderThickness="1"
                        CornerRadius="6"
                        Padding="14">
                    <StackPanel Spacing="8">
                        <TextBlock Text="Opzioni"
                                   FontWeight="SemiBold"/>

                        <CheckBox Content="Randomizza l'ordine delle domande"
                                  IsChecked="{Binding RandomizzaOrdine, Mode=TwoWay}"
                                  ToolTip.Tip="Se disattivato, le domande seguono l'ordine dei file JSON."/>

                        <CheckBox Content="Modalita' rotazione (le sbagliate rientrano in coda)"
                                  IsChecked="{Binding Rotazione, Mode=TwoWay}"
                                  ToolTip.Tip="Il quiz termina solo quando tutte le domande sono state risposte correttamente almeno una volta."/>

                        <CheckBox Content="Mostra cronometro durante il quiz"
                                  IsChecked="{Binding Cronometro, Mode=TwoWay}"/>

                        <Grid ColumnDefinitions="Auto,Auto,Auto" Margin="0,4,0,0">
                            <CheckBox Content="Limita a"
                                      IsChecked="{Binding LimitaDomande, Mode=TwoWay}"
                                      VerticalAlignment="Center"/>
                            <NumericUpDown Grid.Column="1"
                                           Minimum="1" Maximum="9999"
                                           Value="{Binding LimiteN, Mode=TwoWay}"
                                           Increment="5"
                                           Width="120"
                                           Margin="10,0,8,0"
                                           IsEnabled="{Binding LimitaDomande}"/>
                            <TextBlock Grid.Column="2"
                                       Text="domande (estratte casualmente)"
                                       VerticalAlignment="Center"/>
                        </Grid>
                    </StackPanel>
                </Border>

            </StackPanel>
        </ScrollViewer>

    </DockPanel>
```

Cose importanti che cambiano rispetto all'originale:
- Il root non è più `<ScrollViewer>` ma `<DockPanel>`.
- La Grid "AVVIO" non è più dentro lo `StackPanel` interno; è il primo child del `DockPanel` (Bottom), wrappata in una `<Border>` con bordo superiore a 1px e Background `SystemRegionBrush`.
- Il pannello "Materie" ora ha un `<ScrollViewer MaxHeight="240" VerticalScrollBarVisibility="Auto">` attorno all'`ItemsControl` (prima era nudo). Il `<DockPanel>` interno con `TextBlock DockPanel.Dock="Top"` resta, ma l'`ItemsControl` è dentro lo `ScrollViewer`.
- Il pannello "Categorie" è invariato (aveva già il suo `ScrollViewer MaxHeight=240`).
- Il blocco "OPZIONI" è invariato.
- `MaxWidth="980"` resta sullo `StackPanel` interno (limita il contenuto su monitor larghi). La barra sticky non ha `MaxWidth` (occupa tutta la tab).

- [ ] **Step 2: Build di verifica**

Run: `dotnet build Wsa.Quiz.sln`
Expected: `Build succeeded.` con `0 Error(s)`. Se ci sono warning XAML (es. binding non risolto), tornare al file e correggere.

- [ ] **Step 3: Avvio rapido per verificare che la Home apra senza eccezioni**

Run: `dotnet run --project wsa.quiz.app`
Expected: la finestra si apre, la tab Home mostra titolo + materie + categorie + opzioni, e in basso (sticky) la barra con il bottone "Avvia quiz". Niente eccezioni in console. Chiudi la finestra.

- [ ] **Step 4: Commit**

```bash
git add wsa.quiz.app/Views/HomeView.axaml
git commit -m "feat(step6): home sticky avvio bar + materie scrollable"
```

---

## Task 2: Quiz — rinomina bottone header "Abbandona" → "Pausa"

**Files:**
- Modify: `wsa.quiz.app/Views/QuizView.axaml:104-108` (il `Button` nell'header con `Content="Abbandona"`)

**Obiettivo:** cambiare etichetta e nome del click handler nel solo XAML. L'handler C# verrà rinominato nel Task 3.

- [ ] **Step 1: Sostituire il bottone "Abbandona" del header con "Pausa"**

Cerca in `wsa.quiz.app/Views/QuizView.axaml` il blocco (intorno a riga 104):

```xml
                <Button Grid.Column="2"
                        Content="Abbandona"
                        Click="OnAbbandonaClick"
                        Padding="12,5"
                        FontSize="12"/>
```

Sostituiscilo con:

```xml
                <Button Grid.Column="2"
                        Content="Pausa"
                        Click="OnPausaClick"
                        Padding="12,5"
                        FontSize="12"/>
```

Solo `Content` e `Click` cambiano; `Grid.Column`, `Padding`, `FontSize` restano identici.

- [ ] **Step 2: Verifica visiva senza compilare (XAML è statico)**

Apri `wsa.quiz.app/Views/QuizView.axaml` e conferma che:
- nel file compare esattamente una occorrenza di `Content="Pausa"` (la nuova)
- non c'è più `Content="Abbandona"` né `Click="OnAbbandonaClick"`

(Non fare ancora `dotnet build` qui: il code-behind referenzia ancora `OnAbbandonaClick`, quindi il build fallirà finché non si completa il Task 3. Il commit di questo task viene incluso nel commit del Task 3.)

---

## Task 3: Quiz — rinomina handler C# `OnAbbandonaClick` → `OnPausaClick`, rimozione `ChiediConfermaAbbandono`

**Files:**
- Modify: `wsa.quiz.app/Views/QuizView.axaml.cs:100-149` (handler `OnAbbandonaClick` + intero metodo `ChiediConfermaAbbandono`)
- Modify: `wsa.quiz.app/Views/QuizView.axaml.cs:16-18` (commento di summary del file: la riga sulla pausa va aggiornata in Task 4)

**Obiettivo:** rinominare l'handler agganciato dal nuovo XAML, eliminare il vecchio percorso "conferma abbandono separata" perché ora è fuso nel menu pausa.

- [ ] **Step 1: Rinomina dell'handler e rimozione di `ChiediConfermaAbbandono`**

In `wsa.quiz.app/Views/QuizView.axaml.cs` cerca il blocco (righe 100-149):

```csharp
    private void OnAbbandonaClick(object? sender, RoutedEventArgs e)
    {
        // Conferma rapida via finestra modale
        ChiediConfermaAbbandono();
    }

    private async void ChiediConfermaAbbandono()
    {
        var w = new Window
        {
            Title = "Abbandona quiz",
            Width = 380,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false
        };
        bool conferma = false;
        var contenuto = new StackPanel { Margin = new global::Avalonia.Thickness(20), Spacing = 14 };
        contenuto.Children.Add(new TextBlock
        {
            Text = "Sei sicuro di voler abbandonare il quiz? Le risposte gia' date verranno comunque salvate in cronologia.",
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
        });
        var pulsantiera = new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right
        };
        var siBtn = new Button { Content = "Abbandona", Padding = new global::Avalonia.Thickness(14, 5) };
        var noBtn = new Button { Content = "Continua", Padding = new global::Avalonia.Thickness(14, 5) };
        siBtn.Click += (_, _) => { conferma = true; w.Close(); };
        noBtn.Click += (_, _) => w.Close();
        pulsantiera.Children.Add(noBtn);
        pulsantiera.Children.Add(siBtn);
        contenuto.Children.Add(pulsantiera);
        w.Content = contenuto;

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        // Riprendo focus per le scorciatoie
        Focus();

        if (conferma) _sessione.Abbandona();
    }
```

Sostituiscilo con:

```csharp
    private void OnPausaClick(object? sender, RoutedEventArgs e)
    {
        ApriMenuPausa();
    }
```

Cioè: rinomina `OnAbbandonaClick` in `OnPausaClick`, fai chiamare `ApriMenuPausa()` invece di `ChiediConfermaAbbandono()`, e cancella interamente il metodo `ChiediConfermaAbbandono()`.

- [ ] **Step 2: Build di verifica**

Run: `dotnet build Wsa.Quiz.sln`
Expected: `Build succeeded.` con `0 Error(s)`. Il vecchio metodo `ApriMenuPausa()` esistente regge ancora questa fase: il build deve passare anche prima di Task 4 (l'app a runtime mostra la modale 3-bottoni vecchia, riscritta nel Task 4). Se invece compaiono errori del tipo `CS0103: The name 'OnAbbandonaClick' does not exist`, controlla che il Task 2 abbia effettivamente sostituito anche `Click="OnAbbandonaClick"` in `QuizView.axaml`.

- [ ] **Step 3: Commit (include la rinomina XAML del Task 2)**

```bash
git add wsa.quiz.app/Views/QuizView.axaml wsa.quiz.app/Views/QuizView.axaml.cs
git commit -m "refactor(step6): bottone Pausa unificato, rimossa conferma abbandono separata"
```

---

## Task 4: Quiz — riscrittura `ApriMenuPausa()` come modale unificata 480×210

**Files:**
- Modify: `wsa.quiz.app/Views/QuizView.axaml.cs:151-235` (intero metodo `ApriMenuPausa`)
- Modify: `wsa.quiz.app/Views/QuizView.axaml.cs:16-18` (commento di summary)

**Obiettivo:** sostituire la modale a 2 bottoni (Annulla / Salva e esci / Riprendi) con una modale a 3 azioni che fonde anche l'abbandono. Layout: bottone "Abbandona" (`danger`) a sinistra; "Annulla" + "Salva e esci" (`accent`) a destra. Default focus su Annulla. ESC = Annulla.

- [ ] **Step 1: Aggiornare il commento XML di classe per riflettere il nuovo comportamento**

In `wsa.quiz.app/Views/QuizView.axaml.cs` cerca:

```csharp
/// Tastiera (step 4): A/B/C/D selezionano la risposta, Invio avanza, ESC apre il
/// menu pausa modale (Riprendi / Salva e esci / Annulla).
```

Sostituiscilo con:

```csharp
/// Tastiera (step 4): A/B/C/D selezionano la risposta, Invio avanza, ESC apre il
/// menu pausa modale unificato (Annulla / Abbandona / Salva e esci) — anche il
/// bottone "Pausa" del header apre la stessa modale (step 6).
```

- [ ] **Step 2: Sostituire l'intero metodo `ApriMenuPausa()` con la nuova versione**

In `wsa.quiz.app/Views/QuizView.axaml.cs` localizza il metodo `ApriMenuPausa()` (sotto al commento `/// <summary>` "Menu pausa: ..."). Sostituisci tutto il blocco — dal commento `/// <summary>` di sopra fino alla `}` di chiusura del metodo (incluse) — con questo:

```csharp
    /// <summary>
    /// Modale pausa unificata (step 6). Tre uscite possibili veicolate da una
    /// variabile locale <c>azione</c>: <c>"annulla"</c> (default + ESC), <c>"abbandona"</c>,
    /// <c>"salva"</c>. Layout: Abbandona (danger) a sinistra, Annulla + Salva e esci
    /// (accent) a destra. Default focus su Annulla.
    /// </summary>
    private async void ApriMenuPausa()
    {
        var w = new Window
        {
            Title = "Quiz in pausa",
            Width = 480,
            Height = 210,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false
        };

        string azione = "annulla";

        var contenuto = new StackPanel { Margin = new global::Avalonia.Thickness(20), Spacing = 12 };
        contenuto.Children.Add(new TextBlock
        {
            Text = "Quiz in pausa. Cosa vuoi fare?",
            FontSize = 14,
            FontWeight = global::Avalonia.Media.FontWeight.SemiBold
        });
        contenuto.Children.Add(new TextBlock
        {
            Text = "Annulla riprende il quiz. Abbandona lo chiude e lo registra in cronologia come abbandonato. Salva e esci lo mette nei Sospesi: lo riprenderai dalla relativa tab.",
            FontSize = 12,
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
            Foreground = global::Avalonia.Media.Brushes.DimGray
        });

        // Pulsantiera: Abbandona (sx, danger) | spazio | Annulla, Salva e esci (dx, accent)
        var pulsantiera = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Margin = new global::Avalonia.Thickness(0, 4, 0, 0)
        };

        var abbandonaBtn = new Button { Content = "Abbandona", Padding = new global::Avalonia.Thickness(14, 5) };
        abbandonaBtn.Classes.Add("danger");
        Grid.SetColumn(abbandonaBtn, 0);

        var destra = new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right
        };
        Grid.SetColumn(destra, 2);

        var annullaBtn = new Button { Content = "Annulla", Padding = new global::Avalonia.Thickness(14, 5) };
        var salvaBtn = new Button { Content = "Salva e esci", Padding = new global::Avalonia.Thickness(14, 5) };
        salvaBtn.Classes.Add("accent");

        abbandonaBtn.Click += (_, _) => { azione = "abbandona"; w.Close(); };
        annullaBtn.Click += (_, _) => { azione = "annulla"; w.Close(); };
        salvaBtn.Click += (_, _) => { azione = "salva"; w.Close(); };

        destra.Children.Add(annullaBtn);
        destra.Children.Add(salvaBtn);

        pulsantiera.Children.Add(abbandonaBtn);
        pulsantiera.Children.Add(destra);

        contenuto.Children.Add(pulsantiera);
        w.Content = contenuto;

        // ESC dentro la modale = Annulla (chiude senza azione)
        w.KeyDown += (_, ke) =>
        {
            if (ke.Key == Key.Escape) { ke.Handled = true; azione = "annulla"; w.Close(); }
        };

        // Focus di default su Annulla quando la finestra si apre
        w.Opened += (_, _) => annullaBtn.Focus();

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner != null) await w.ShowDialog(owner);
        else w.Show();

        // Riprendo focus per le scorciatoie del Quiz
        Focus();

        switch (azione)
        {
            case "abbandona":
                _sessione.Abbandona();
                return;

            case "salva":
                try
                {
                    var pausa = _sessione.EsportaPausa();
                    _storage.SalvaPausa(pausa);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SalvaPausa failed: {ex}");
                    return;
                }
                QuizMessoInPausa?.Invoke(this, EventArgs.Empty);
                return;

            default: // "annulla"
                return;
        }
    }
```

Note di mappatura rispetto al codice precedente:
- la modale era 420×180; ora 480×210 (più larga per ospitare il terzo bottone, leggermente più alta per il testo descrittivo più lungo).
- la pulsantiera passa da `StackPanel` orizzontale a `Grid 3-colonne (Auto,*,Auto)` per separare visivamente l'Abbandona (sx) dal blocco Annulla+Salva (dx).
- la classe `danger` si applica al bottone Abbandona (già definita in `App.axaml`).
- la variabile booleana `bool salva` è sostituita da `string azione` con tre valori: `"annulla"` (default), `"abbandona"`, `"salva"`.
- Il vecchio bottone "Riprendi" è eliminato: la sua azione (chiude la modale senza fare nulla) è già coperta da "Annulla". La spec lo specifica esplicitamente.
- `w.Opened += (_, _) => annullaBtn.Focus();` imposta il focus di default sul bottone Annulla (la spec menziona questo come metodo preferito rispetto a `IsDefault`).

- [ ] **Step 3: Build di verifica**

Run: `dotnet build Wsa.Quiz.sln`
Expected: `Build succeeded.` con `0 Error(s)` e `0 Warning(s)` sui file toccati. Se `Grid` o `ColumnDefinitions` non risolvono, controlla che `using Avalonia.Controls;` sia già presente in cima al file (lo è — riga 2 dell'originale).

- [ ] **Step 4: Smoke run dell'app**

Run: `dotnet run --project wsa.quiz.app`
Expected: l'app si apre, vai in Home, avvia un quiz qualsiasi (selezione una materia → Avvia). Sul Quiz, conferma che il bottone in alto a destra dice "**Pausa**". Click su "Pausa" → si apre una modale 480×210 con tre bottoni nel layout previsto. Premi "Annulla" o ESC → la modale chiude e i tasti A/B/C/D rispondono di nuovo. Chiudi l'app.

- [ ] **Step 5: Commit**

```bash
git add wsa.quiz.app/Views/QuizView.axaml.cs
git commit -m "feat(step6): modale pausa unificata 480x210 con Abbandona/Annulla/Salva"
```

---

## Task 5: Aggiornamento WSA_QUIZ_HANDOFF.md

**Files:**
- Modify: `WSA_QUIZ_HANDOFF.md` (sezione "Stato attuale", la voce dello step 6 nella roadmap, e la voce nel changelog dei test)

**Obiettivo:** segnare lo step 6 come completato nel documento di passaggio, in linea con il pattern usato dagli step precedenti (step 5 è già marcato `**Fatto.**`).

- [ ] **Step 1: Marcare lo step 6 completato nella roadmap**

In `WSA_QUIZ_HANDOFF.md` cerca la riga (intorno a 146):

```markdown
### ⏳ Step 6 — UX rifiniture (Home sticky + Pausa unificata)
```

Sostituiscila con:

```markdown
### ✅ Step 6 — UX rifiniture (Home sticky + Pausa unificata)
```

Poi, alla fine del paragrafo dello step 6 (dopo la riga sull'ESC dentro la modale = Annulla), aggiungi alla fine `Plan: docs/superpowers/plans/2026-05-10-step6-rifiniture-ux.md. **Fatto.**` come fatto per gli step precedenti.

Nello specifico cambia:

```markdown
2. **Pausa unificata**. Oggi ESC apre il "menu pausa" (Annulla/Salva e esci/Riprendi) e il bottone "Abbandona" apre una conferma separata (Continua/Abbandona). Si fondono in **una sola** modale, raggiungibile sia da ESC sia dal bottone in alto, con tre voci: Annulla / Abbandona (`danger`) / Salva e esci (`accent`). Il bottone in alto si rinomina in "**Pausa**". ESC dentro la modale = Annulla.
```

In:

```markdown
2. **Pausa unificata**. Oggi ESC apre il "menu pausa" (Annulla/Salva e esci/Riprendi) e il bottone "Abbandona" apre una conferma separata (Continua/Abbandona). Si fondono in **una sola** modale, raggiungibile sia da ESC sia dal bottone in alto, con tre voci: Annulla / Abbandona (`danger`) / Salva e esci (`accent`). Il bottone in alto si rinomina in "**Pausa**". ESC dentro la modale = Annulla. Spec: `docs/superpowers/specs/2026-05-09-step6-rifiniture-ux-design.md`. Plan: `docs/superpowers/plans/2026-05-10-step6-rifiniture-ux.md`. **Fatto.**
```

- [ ] **Step 2: Aggiornare la sezione "Stato attuale"**

In `WSA_QUIZ_HANDOFF.md` cerca la riga 11:

```markdown
## Stato attuale: STEP 5 COMPLETO
```

Sostituiscila con:

```markdown
## Stato attuale: STEP 6 COMPLETO
```

Poi nella riga 13 (paragrafo sotto), aggiorna l'ultimo aggiornamento dello stato. Cerca:

```markdown
L'app Avalonia ha ora tastiera completa nel Quiz (`A/B/C/D` selezionano la risposta, `Invio` avanza, `ESC` apre il menu pausa modale Riprendi/Salva e esci/Annulla) e il ciclo pausa→sospeso→ripresa funziona end-to-end nella GUI.
```

Sostituiscila con:

```markdown
L'app Avalonia ha ora tastiera completa nel Quiz (`A/B/C/D` selezionano la risposta, `Invio` avanza, `ESC` apre la modale pausa unificata Annulla/Abbandona/Salva e esci — step 6) e il ciclo pausa→sospeso→ripresa funziona end-to-end nella GUI. Bottone "Pausa" in alto a destra del Quiz apre la stessa modale. La Home ha la barra "Avvia quiz" sticky in basso e il pannello Materie con scroll interno (max 240px), come Categorie.
```

- [ ] **Step 3: Build (sanity check, niente di compilato cambia)**

Run: `dotnet build Wsa.Quiz.sln`
Expected: `Build succeeded.` con `0 Error(s)`. Il file modificato è solo Markdown, ma il build conferma che lo stato del codice non è regredito.

- [ ] **Step 4: Commit**

```bash
git add WSA_QUIZ_HANDOFF.md
git commit -m "docs(step6): mark UX refinements as done in handoff"
```

---

## Task 6: Acceptance test manuali

**Files:** nessuno (test manuali sull'app in esecuzione).

**Obiettivo:** eseguire i 13 test manuali della spec sezione "Test manuali (acceptance)" e verificare che tutti passino. Se qualcuno fallisce, tornare indietro al task corrispondente, correggere e ri-eseguire la batteria.

- [ ] **Step 1: Avvia l'app**

Run: `dotnet run --project wsa.quiz.app`
Expected: la finestra si apre sulla tab Home.

- [ ] **Step 2: Home — Test 1 (sticky con finestra alta 600px, niente selezioni)**

Ridimensiona la finestra a circa 600px di altezza (trascina il bordo inferiore). Senza selezionare alcuna materia, la barra "Avvia quiz" deve essere visibile in basso, con bordo superiore a 1px che la separa dal contenuto.

- [ ] **Step 3: Home — Test 2 (sticky con tutte le materie selezionate)**

Seleziona tutte le materie disponibili (cinque: cpp, cs, frontend, infra, sql). Le categorie appaiono a destra. La barra "Avvia quiz" deve restare visibile in fondo senza bisogno di scrollare. Il bottone "Avvia quiz" si abilita.

- [ ] **Step 4: Home — Test 3 (scroll interno su Materie e Categorie con liste lunghe)**

Continua con tutte le materie selezionate. Il pannello Categorie scorre internamente quando supera ~240px (era già così). Verifica che ora **anche il pannello Materie** scorra internamente se la finestra è abbastanza piccola da forzare il `MaxHeight=240`.

- [ ] **Step 5: Home — Test 4 (PuoAvviare e riepilogo)**

Deseleziona tutto: il bottone "Avvia quiz" è disabilitato e il riepilogo a sinistra dice "0 materie selezionate" (o equivalente). Seleziona una materia: il bottone si abilita; il riepilogo si aggiorna. Comportamento identico allo step 5.

- [ ] **Step 6: Home — Test 5 (finestra molto larga, contenuto a 980)**

Massimizza la finestra (o portala a 1600px di larghezza). Il contenuto centrale (titoli, materie/categorie, opzioni) resta limitato a 980px di larghezza; la barra sticky in basso occupa tutta la larghezza della tab. Il bottone "Avvia quiz" resta allineato a destra rispetto al contenuto da 980px.

- [ ] **Step 7: Pausa — Test 6 (bottone si chiama "Pausa")**

Avvia un quiz qualsiasi (es. una materia, ~5 domande limitate). Il bottone in alto a destra del Quiz dice "**Pausa**" (non più "Abbandona").

- [ ] **Step 8: Pausa — Test 7 (apertura modale dal bottone)**

Click sul bottone "Pausa". Si apre una modale 480×210 con titolo "Quiz in pausa". Layout pulsantiera: sinistra **Abbandona** (rosso), destra **Annulla** + **Salva e esci** (Salva e esci accent). Bottone Annulla con focus visibile (default).

- [ ] **Step 9: Pausa — Test 8 (apertura modale da ESC)**

Chiudi la modale (Annulla). Premi ESC: si apre la stessa identica modale.

- [ ] **Step 10: Pausa — Test 9 (ESC dentro la modale = Annulla)**

Modale aperta. Premi ESC: la modale chiude. Tornato sul Quiz, A/B/C/D rispondono normalmente (focus restituito al `QuizView`).

- [ ] **Step 11: Pausa — Test 10 (Annulla esplicito)**

Apri la modale, click su "Annulla". Identico a ESC: chiude la modale, tasti tornano attivi.

- [ ] **Step 12: Pausa — Test 11 (Abbandona)**

Apri la modale, click su "Abbandona". Vai dritto al `RiepilogoView`. La cronologia mostra la sessione con tag "Abbandonato". Torna alla Home.

- [ ] **Step 13: Pausa — Test 12 (Salva e esci → Sospesi)**

Avvia un nuovo quiz. Apri la modale, click su "Salva e esci". Torni alla tab Home. Vai alla tab Sospesi: la pausa nuova compare nella lista (data/ora attuale, materia/numero corretti).

- [ ] **Step 14: Pausa — Test 13 (focus tastiera dopo modale)**

Avvia un nuovo quiz. Apri la modale (ESC), chiudila (Annulla). Premi A/B/C/D: rispondi. Premi Invio: avanza. Tutte le scorciatoie continuano a funzionare. Chiudi l'app.

- [ ] **Step 15: Se tutti i test passano, niente da committare. Se qualcuno fallisce, tornare al task pertinente**

Se Test 1-5 falliscono → rivedere Task 1.
Se Test 6 fallisce → rivedere Task 2/3.
Se Test 7-13 falliscono → rivedere Task 4.
Se Test 14 fallisce → controllare che `Focus()` dopo `ShowDialog` sia ancora presente in `ApriMenuPausa()` (lo è — non l'abbiamo tolto).

---

## Riepilogo commit attesi

Alla fine dei task in ordine:

1. `feat(step6): home sticky avvio bar + materie scrollable` (Task 1)
2. `refactor(step6): bottone Pausa unificato, rimossa conferma abbandono separata` (Task 2 + 3)
3. `feat(step6): modale pausa unificata 480x210 con Abbandona/Annulla/Salva` (Task 4)
4. `docs(step6): mark UX refinements as done in handoff` (Task 5)

Totale: 4 commit. Task 6 non genera commit (solo verifica).
