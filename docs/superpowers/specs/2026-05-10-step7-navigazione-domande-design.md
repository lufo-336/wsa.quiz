# Step 7 — Navigazione tra domande (design)

> Spec di design. La spec è un'ipotesi: durante l'implementazione e i test manuali può cambiare. Il giudice finale è il test pratico.

## Obiettivo

Permettere all'utente, durante un quiz in corso, di rivedere le risposte già date senza perdere lo stato della domanda corrente. Aggiungere inoltre due scorciatoie (`↑/↓` + `Invio`) per chi preferisce confermare la risposta dopo averla evidenziata, in alternativa al click diretto su `A/B/C/D`.

Tre interazioni nuove:
1. `←/→` scorrono la cronologia delle risposte già date dentro la sessione corrente (entra/esce dalla view-mode read-only).
2. `↑/↓` evidenziano una risposta sulla corrente in attesa; `Invio` conferma l'evidenziata.
3. Bottone "Torna alla corrente" visibile in view-mode per uscire senza usare la tastiera.

## Decisioni UX

### Tastiera

| Tasto | Stato corrente in attesa | Stato corrente feedback | View-mode (passata) |
|-------|------|------|------|
| `A/B/C/D` | risponde direttamente (oggi) | – | – (bottoni disabilitati) |
| `↑/↓` | sposta highlight su una risposta | – | – |
| `Invio` | conferma highlight (se presente) | avanza (oggi) | – |
| `←` | va all'ultima passata (entra view-mode) | va all'ultima passata | va a passata precedente |
| `→` | – (sei già sulla corrente) | – | va a successiva o torna a corrente |
| `Esc` | apre modale pausa (oggi) | apre modale pausa (oggi) | apre modale pausa |

`A/B/C/D` resta come scorciatoia diretta. `↑/↓ + Invio` è una via alternativa, non sostitutiva.

`Esc` mantiene il comportamento attuale (modale pausa) anche in view-mode, per non rompere la familiarità. L'uscita dalla view-mode si fa con `→` ripetuto fino a tornare alla corrente, oppure con il bottone "Torna alla corrente".

### Modello di "passata" in rotazione

Ogni tentativo è una entry separata: `←/→` scorre la cronologia completa di tutte le risposte date, anche quando la stessa domanda compare più volte (rotazione). Coerente col modello "storico cronologico". È equivalente a scorrere `Risultato.Dettagli` in ordine.

### Layout view-mode

Quando l'indice di visualizzazione punta a una passata:

- **Banner** in alto, fascia neutra dentro la `QuizView`: `"Stai rivedendo la domanda 3 di 7 — già risposta"` + bottone `"Torna alla corrente"` (`Classes="accent"`) a destra. Visibile solo se `InViewMode=true`.
- **4 bottoni A/B/C/D** mostrati con la stessa colorazione del feedback live: verde la corretta, rosso quella data se sbagliata, neutri gli altri. `IsEnabled=false` in view-mode.
- **Pannello feedback** identico al post-risposta: titolo (`Corretto!`/`Sbagliato`), spiegazione.
- **Header**: il numero domanda mostrato cambia in `"Domanda X (vista)"` come segnale aggiuntivo. `X` è la posizione della passata in `Risultato.Dettagli` (1-based), che corrisponde a `NumeroDomandaCorrente` nello storico (in entrambe le modalità è già un contatore globale che incrementa per ogni domanda mostrata, comprese ripetute in rotazione).
- Bottone "Pausa" e cronometro nell'header restano visibili e funzionanti.

`↑/↓`, `A/B/C/D`, `Invio` sono no-op in view-mode. Ignoriamoli silenziosamente, niente feedback sonoro o visivo.

### Highlight (↑/↓)

- Stato: `int? IndiceHighlight` su `SessioneQuiz`, nullable. `null` significa nessun highlight (default).
- Inizializzazione: alla prima pressione di `↑` parte da indice 3 (ultima risposta), alla prima pressione di `↓` parte da indice 0. Successivamente clamp [0..3].
- `Invio` su corrente in attesa: se `IndiceHighlight` non null → `RispondiA(IndiceHighlight.Value)`. Se null → niente.
- `RispondiA` resetta `IndiceHighlight = null`.
- `CaricaProssimaDomanda` resetta `IndiceHighlight = null`.
- In view-mode `↑/↓` ignorati, `IndiceHighlight` non viene aggiornato né mostrato.
- Visivo: il `RispostaItem` evidenziato ha una proprietà observable aggiuntiva `IsHighlighted`. Lo style `Button.risposta` (esistente o da definire) applica `BorderThickness=2 BorderBrush="{DynamicResource SystemAccentColorBrush}"` quando `IsHighlighted=true` e lo stato è `Neutra`. In stato `Corretta`/`Sbagliata` il bordo highlight non si mostra (i colori dello stato hanno priorità).

## Modello dati

### Aggiunte a `DettaglioRisposta` (Core)

Tre campi additivi:

```csharp
public List<string> RisposteShufflate { get; set; } = new();
public int IndiceCorrettoShufflato { get; set; } = -1;
public int IndiceDataShufflato { get; set; } = -1;
```

- I record JSON esistenti caricano con default vuoto/`-1`. Il `CronologiaDettaglioView` non li usa (continua a leggere `RispostaData`/`RispostaCorretta` come oggi). Nessuna migrazione attiva: i nuovi campi vengono popolati alle nuove sessioni e ignorati nei record vecchi.
- Il QuizView in view-mode legge questi campi per ricostruire i 4 bottoni colorati.
- Sono popolati in `SessioneQuiz.RispondiA` quando crea il `DettaglioRisposta` (oggi crea già il record, basta valorizzare i tre nuovi campi da `_domandaCorrente.RisposteShufflate`, `_domandaCorrente.IndiceCorrettoShufflato`, e `indiceShufflato` parametro).

### Stato in `SessioneQuiz`

Nuovi campi privati e proprietà observable:

```csharp
private int? _viewIndex; // null = corrente, 0..N-1 = passata in Risultato.Dettagli
public int? IndiceVisualizzazione { get; private set; }
public bool InViewMode => _viewIndex.HasValue;

private int? _indiceHighlight;
public int? IndiceHighlight { get; private set; }

public bool PuoIndietro => Risultato.Dettagli.Count > 0 && (_viewIndex ?? Risultato.Dettagli.Count) > 0;
public bool PuoAvanti => InViewMode; // dalla corrente non si può andare avanti
```

Nuovi metodi pubblici:

```csharp
public void VaiAPassataPrecedente();
public void VaiAPassataSuccessiva(); // se _viewIndex == ultimo, ritorna a corrente
public void TornaACorrente();
public void HighlightSu();    // ↑
public void HighlightGiu();   // ↓
public void ConfermaHighlight(); // Invio quando IndiceHighlight != null
```

`VaiAPassataPrecedente` quando si è sulla corrente: setta `_viewIndex = Risultato.Dettagli.Count - 1` (entra in view-mode). Se non ci sono passate, no-op.

### Snapshot vista passata

Quando `InViewMode=true`, il `QuizView` non binda direttamente le proprietà live di `SessioneQuiz` (`DomandaTesto`, `Risposte`, ecc.) ma una vista derivata. Due opzioni di implementazione:

**Opzione A (preferita)**: `SessioneQuiz` aggiorna direttamente le proprietà observable correnti (`DomandaTesto`, `Risposte`, `FeedbackTitolo`, `SpiegazioneTesto`, ecc.) ricaricandole dalla `DettaglioRisposta` corrispondente quando si entra in view-mode, e ricaricandole dalla domanda live quando si torna. Lo stato live viene salvato in campi privati di "backup" all'ingresso e ripristinato all'uscita. Pro: nessun cambio al binding XAML esistente. Contro: rischio di stato che si perde se non si bilancia bene save/restore.

**Opzione B**: `SessioneQuiz` espone una seconda set di proprietà observable (`DomandaTestoVista`, `RisposteVista`, ecc.) e il `QuizView` cambia il binding/visibilità tra il "blocco corrente" e il "blocco vista" in base a `InViewMode`. Pro: zero rischio di interferenza. Contro: duplicazione XAML.

**Decisione**: Opzione A. Più semplice, meno duplicazione. Il backup/restore avviene in due metodi privati `SalvaStatoLive()` / `RipristinaStatoLive()` chiamati solo nelle transizioni in/out view-mode.

Backup necessario:
- `_domandaCorrente` (riferimento a `DomandaPreparata`)
- `RispostaInviata` e tutto il blocco feedback (`UltimaRispostaCorretta`, `FeedbackTitolo`, `SpiegazioneTesto`, `LetteraCorretta`, `TestoRispostaCorretta`)
- Lo `Stato` di ognuno dei 4 `RispostaItem` corrente (per ripristinare l'eventuale rosso/verde se ero in feedback)
- `IndiceHighlight`

In view-mode i 4 `RispostaItem` vengono ricostruiti con `IsEnabled=false` (nuovo flag observable, vedi sotto).

### `RispostaItem` — flag aggiuntivi

`RispostaItem` oggi espone `Lettera`, `Testo`, `Stato` (Neutra/Corretta/Sbagliata). Aggiunte:

```csharp
public bool IsHighlighted { get; set; } // observable, per il bordo accent in attesa
public bool IsEnabled { get; set; } = true; // observable, false in view-mode
```

`IsEnabled` permette di disabilitare i bottoni in view-mode senza dover cambiare il template XAML del bottone. La `QuizView` binda `IsEnabled` del `Button` a `{Binding IsEnabled}` invece che a `{Binding IsNeutra}` (oggi è IsNeutra; vediamo sotto).

Importante: oggi i bottoni risposta vengono disabilitati post-risposta tramite `IsEnabled="{Binding IsNeutra}"` (anti doppio-click). In view-mode dobbiamo sovra-imporre `IsEnabled=false` indipendentemente dallo stato. Sostituire `IsNeutra` con un nuovo computed `bool PuoCliccare => IsEnabled && Stato == StatoRisposta.Neutra` su `RispostaItem`. Mantenere `IsNeutra` se serve a XAML esistente per altri scopi (es. mostrare/nascondere testo); o riusare `PuoCliccare` per il binding `Button.IsEnabled` e lasciare `IsNeutra` per il resto.

## Pausa e ripresa

Le passate accumulate restano in `Risultato.Dettagli` come oggi, e con i tre nuovi campi popolati. Quando una pausa viene esportata (`EsportaPausa`), i campi `RisposteShufflate`, `IndiceCorrettoShufflato`, `IndiceDataShufflato` vengono salvati nel JSON insieme al resto. Alla ripresa (`RiprendiDa`), `pausa.Dettagli` ricostruisce tutto compresi i tre nuovi campi → in view-mode anche le passate pre-pausa hanno i 4 bottoni.

`_viewIndex` NON viene persistito nella pausa: alla ripresa si parte sempre sulla corrente. La modale pausa apribile via ESC, anche dalla view-mode, esporta lo stato attivo (la `RispostaInviata` e il blocco feedback live, NON la passata che si stava guardando). Per essere sicuri, `EsportaPausa` chiama internamente `TornaACorrente()` (no-op se già sulla corrente) prima di leggere lo stato.

## QuizView XAML/CS — modifiche puntuali

- Banner view-mode: nuovo `<Border IsVisible="{Binding Sessione.InViewMode}" ...>` in cima alla view, sopra l'header esistente. Contiene `TextBlock Text="{Binding Sessione.BannerVista}"` (proprietà computed lato C#) e bottone "Torna alla corrente" che chiama `Sessione.TornaACorrente()`.
- I 4 bottoni risposta: `IsEnabled` rebindato su `PuoCliccare` (vedi sopra). Aggiunto `Classes.highlighted="{Binding IsHighlighted}"` per attivare lo stile bordo accent.
- Header: `NumeroDomandaCorrente` resta, ma può essere affiancato da una label "(vista)" via converter o semplicemente concatenando in C# in una nuova proprietà `EtichettaNumeroDomanda`.
- `OnKeyDown` (in `QuizView.axaml.cs`): aggiunti case per `Key.Left`, `Key.Right`, `Key.Up`, `Key.Down`. `Invio` esteso: se `Sessione.IndiceHighlight` non null e siamo in attesa, conferma highlight; altrimenti il comportamento esistente (avanza in feedback). `A/B/C/D`, `Up/Down`, `Enter` ignorati se `InViewMode=true`. `Left/Right` consentiti sempre tranne quando una modale (es. pausa) è aperta — ma siccome la pausa toglie il focus alla `QuizView`, questo è già gestito implicitamente.

## App.axaml — style aggiuntivo

Nuova classe `Button.risposta-highlighted` (o riusare logica `:focus`). Bordo:

```xaml
<Style Selector="Button.highlighted">
    <Setter Property="BorderThickness" Value="2"/>
    <Setter Property="BorderBrush" Value="{DynamicResource SystemAccentColorBrush}"/>
</Style>
```

Da rifinire in implementazione (potrebbe servire un selector più specifico per non collidere con altri stati del bottone).

## Out-of-scope dichiarati

- `Esc` resta sulla pausa anche in view-mode. Niente shortcut "Esc = esci da view-mode".
- View-mode è solo lettura. Nessuna modifica possibile delle risposte già date.
- `←/→` non escono dal Quiz e non navigano nella cronologia globale (CronologiaView). Operano solo sui dettagli della sessione corrente.
- `↑/↓` in view-mode: ignorati. Non evidenziano la risposta data (sarebbe ridondante: è già colorata).
- Persistenza dell'ultimo `_viewIndex` in pausa: scartata. Alla ripresa si torna sempre sulla corrente.
- Riepilogo (`RiepilogoView`): nessun cambio. Le passate sono già visibili lì come oggi (nelle sbagliate).

## Trappole previste

- **Backup/restore stato live**: la trappola principale dell'opzione A (riusare le proprietà observable correnti). Se entrando in view-mode dimentichiamo di salvare un campo, all'uscita resta lo stato della passata invece della corrente. Disciplina: due metodi privati `SalvaStatoLive()` / `RipristinaStatoLive()` con un singolo struct/record `_backupStatoLive` che li tiene insieme; ogni nuovo campo observable di sessione live va aggiunto in entrambi i metodi.
- **`IsEnabled` vs `IsNeutra`**: cambiare il binding del bottone risposta richiede attenzione a non rompere il comportamento anti doppio-click attuale. Il computed `PuoCliccare = IsEnabled && IsNeutra` deve essere observable e rinotificato sia quando cambia `IsEnabled` sia quando cambia `Stato`.
- **`Key.Up`/`Key.Down` con `Focusable=true`**: in Avalonia, i tasti freccia su un controllo focusable possono spostare il focus verso il primo controllo accanto. Vincolare `e.Handled = true` per tutte le frecce, e verificare manualmente che il `QuizView.Focus()` resti attivo dopo ogni evento.
- **Highlight + risposta diretta**: se l'utente ha highlightato indice 2 con `↑/↓` e poi clicca direttamente sul bottone A (indice 0), `RispondiA(0)` deve essere eseguito (ignorando l'highlight) e `IndiceHighlight` deve essere resettato. Coerente con la logica esistente di `RispondiA`.
- **Numero domanda in rotazione**: assumiamo che `NumeroDomandaCorrente` allo step `i` di una passata combaci con `i+1` (1-based) nell'array `Dettagli`, sia in classica sia in rotazione. Verificare nel test manuale 5 (rotazione con tentativi multipli) che l'header in view-mode mostri il numero coerente con quello che era visibile al momento della risposta.

## File toccati (stima)

- `wsa.quiz.core/Models/DettaglioRisposta.cs` — 3 nuovi campi
- `wsa.quiz.app/State/SessioneQuiz.cs` — `_viewIndex`, `_indiceHighlight`, metodi navigazione, backup/restore stato live, popolare i 3 nuovi campi in `RispondiA`
- `wsa.quiz.app/State/RispostaItem.cs` — flag `IsHighlighted`, `IsEnabled`, computed `PuoCliccare`
- `wsa.quiz.app/Views/QuizView.axaml` — banner view-mode, binding `PuoCliccare`, classe `.highlighted`
- `wsa.quiz.app/Views/QuizView.axaml.cs` — `OnKeyDown` esteso (`Left/Right/Up/Down`), gestione `Invio` con highlight
- `wsa.quiz.app/App.axaml` — style `Button.highlighted`

Stima: ~150-250 righe modificate/aggiunte. Nessun nuovo file.

## Test manuali (golden path + edge case)

1. Quiz classica, 5 domande. Rispondi a 2. Premi `←`: vedi domanda 2 (passata). Premi `←`: vedi domanda 1. Premi `→` `→`: torni alla 3 corrente, in stato in attesa. Stato corrente intatto.
2. Quiz classica, rispondi alla 1 (sbagli). Premi `←`: vedi 1 in feedback. Premi `→`: torni alla 2 in feedback. Lo stato post-risposta della 2 è preservato (verde/rosso, spiegazione visibile).
3. ↑/↓ + Invio: in attesa sulla 1, premi `↓` 2 volte → highlight su C. Premi `Invio` → conferma C come risposta.
4. ↑/↓ poi click diretto: highlight su B, click su A → invia A, highlight resettato.
5. Rotazione, sbaglia 2 volte la stessa domanda: `←` mostra 2 entry separate per la stessa domanda.
6. View-mode + Pausa: su una passata premi `Esc` → modale pausa. "Salva e esci" salva la sessione (la passata che stavi guardando NON è "la corrente" — verifica che la pausa contenga lo stato live).
7. Pausa e ripresa: pausa una sessione con 3 risposte già date. Riprendi. Premi `←` 3 volte → vedi le 3 passate con i 4 bottoni colorati (le shufflate sono state preservate).
8. Bottone "Torna alla corrente" cliccabile dal banner: porta alla corrente.
9. Quiz appena partito (0 risposte): `←` no-op (bottone "Torna alla corrente" comunque non visibile, banner non visibile).

## Differenze dal handoff

Il handoff descriveva lo step 7 in due righe: "←/→ tra domanda corrente e domande passate (sola lettura), ↑/↓ per selezionare risposta sulla corrente. Richiede di mantenere un view-index separato dal answering-index."

Questa spec aggiunge:
- Decisione esplicita "ogni tentativo = entry" per la rotazione.
- Layout view-mode: banner + 4 bottoni colorati e disabilitati (non formato compatto).
- Modello dati: 3 campi additivi su `DettaglioRisposta` per ricostruire i 4 bottoni anche dopo pausa/ripresa.
- Highlight con `↑/↓ + Invio` come alternativa a `A/B/C/D`, mai sostitutiva.
- ESC mantiene il comportamento attuale (modale pausa) anche in view-mode.
- Out-of-scope esplicitati per evitare scope creep.
