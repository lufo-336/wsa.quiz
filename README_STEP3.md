# Step 3 — Cronologia + Sospesi + fix bottone disabled

## Cosa c'è dentro

Estensione dell'app Avalonia con due tab popolate (Cronologia e Sospesi) e fix
cosmetico del bottone "Avvia quiz" disabled segnalato nello scorso passaggio.

### File NUOVI

```
wsa.quiz.app/
├── State/
│   ├── RisultatoCronologiaItem.cs    ← wrapper riga cronologia (data, %, durata, colore)
│   ├── SessioneSospesaItem.cs        ← wrapper riga sospesi + flag conferma elimina
│   └── DettaglioRispostaItem.cs      ← wrapper riga di dettaglio (con colore corretto/sbagliato)
└── Views/
    ├── CronologiaView.axaml(.cs)             ← tab Cronologia (lista + swap a dettaglio)
    ├── CronologiaDettaglioView.axaml(.cs)    ← dettaglio domanda-per-domanda di una sessione
    └── SospesiView.axaml(.cs)                ← tab Sospesi (lista + Riprendi/Elimina)
```

### File MODIFICATI

```
wsa.quiz.app/
├── App.axaml                          ← aggiunti style Button.accent e Button.danger (4 stati ciascuno)
├── MainWindow.axaml.cs                ← istanzia CronologiaView/SospesiView, ricarica dopo un quiz
└── Views/
    ├── HomeView.axaml                 ← bottone Avvia: Classes="accent" al posto di Background/Foreground
    └── RiepilogoView.axaml            ← bottone Torna alla Home: idem
```

`PlaceholderView` resta nel progetto: è ancora usata dalla MainWindow nel ramo
"Impossibile avviare". Non è incluso nello zip perché non cambia.

## Come applicare

Estrai sopra alla sandbox (`C:\Users\luca.foglia\Documents\Quiz\...`)
sovrascrivendo i conflitti. I file nuovi entrano automaticamente nei csproj
grazie al glob standard di SDK-style projects, non serve toccare il csproj.

## Cosa fa adesso

### Tab Cronologia
- Tabella delle sessioni passate, **più recente in alto**.
- Per ogni riga: data, modalità + materia, % corrette (col colore della scala
  80/50), conteggio risposte (`corrette / totali`), durata.
- Striscia colorata a sinistra (verde/ambra/rosso) per leggere a colpo d'occhio
  com'è andata la sessione.
- Etichetta "Abbandonato" in rosso sotto la data se la sessione era stata
  abbandonata.
- **Doppio click su una riga → vista di dettaglio**: stesse statistiche del
  riepilogo + lista completa delle domande con bordo verde (corrette) o rosso
  (sbagliate). Per le sbagliate mostra anche la risposta corretta. Per le
  rotazioni mostra "Risposta corretta dopo N tentativi".
- "← Cronologia" / "Torna alla cronologia" per tornare alla lista. La
  selezione resta dov'era.
- Bottone "Aggiorna" rilegge il file dal disco — utile se hai fatto un quiz
  dal console mentre l'app era aperta.
- Stato vuoto curato se non c'è ancora cronologia.

### Tab Sospesi
- Card per ogni sessione in pausa con: data + tipo (Classica/Rotazione),
  modalità + materia, avanzamento parziale (es. "3/10 risposte" o "2/15
  padroneggiate · 4 sbagliate"), tempo trascorso al momento della pausa.
- **Riprendi disabilitato** (tooltip: "Disponibile dallo step 4. Per ora puoi
  riprendere la sessione dal console").
- **Elimina con conferma inline**: primo click → la riga mostra
  "Sicuro? [Sì, elimina] [Annulla]"; il secondo click conferma. Niente
  dialog modali.
- Bottone "Aggiorna" come per la Cronologia.
- Stato vuoto curato.

### Fix bottone Avvia disabled
Risolve la trappola n.6 della handoff. Approccio:
- L'`App.axaml` definisce due classi di stile, `accent` e `danger`, ognuna
  con i 4 stati (default, pointerover, pressed, **disabled**).
- I bottoni primari nell'app usano `Classes="accent"` invece di
  `Background="{DynamicResource SystemAccentColorBrush}"`. Questo permette a
  `:disabled` di sovrascrivere correttamente lo sfondo con un grigio neutro
  (`#D6D6D6` su foreground `#7A7A7A`) che si distingue da bianco e non è
  confondibile con l'azzurro accent.
- I valori dei colori del disabled sono espliciti (non `DynamicResource` di
  tipo `Color`) per evitare problemi di conversione Color→Brush nei `Setter`
  degli Style.

### Aggiornamento automatico cronologia dopo un quiz
La `MainWindow.OnQuizConcluso` ora chiama `_cronologiaView?.Ricarica()` dopo il
salvataggio. Significa che chiudere un quiz dalla GUI e poi cliccare la tab
Cronologia mostra subito la nuova sessione, senza dover premere "Aggiorna".

## Decisioni prese che vale la pena ricordare

- **No dialog modali per l'Elimina.** In Avalonia 12 i `MessageBox.Show` non
  esistono nativamente — servirebbe un `Window` separato + `ShowDialog` async,
  oppure una libreria. Per uno step "leggi + elimina", la conferma inline a
  due click è più semplice, accessibile da tastiera in futuro, e non rompe il
  flusso. Se in seguito serve davvero una modale (es. per uscire da un quiz
  in corso allo step 4), valutiamo allora.
- **Riprendi disabilitato (non nascosto)** così l'utente vede che esiste e che
  arriverà; il tooltip rimanda al console nel frattempo.
- **Doppio click sulla lista** invece di un bottone "Apri" per ogni riga:
  meno rumore visivo. Ho lasciato un hint testuale sotto la lista così non è
  un'azione invisibile.
- **Cronologia non auto-refresh continuo.** Si aggiorna al boot, dopo ogni
  quiz GUI, e on-demand col bottone "Aggiorna". Niente file watcher —
  sarebbe over-engineered per il caso d'uso e introdurrebbe race con la
  scrittura.
- **DataContext = this nel costruttore, niente reset.** Stesso pattern di
  tutto lo step 2 (anti trappola n.3 della handoff).

## Test rapido suggerito

1. **Boot**: l'app carica come prima, il footer mostra ancora le due cartelle.
   Le tab Cronologia / Sospesi non sono più placeholder.
2. **Cronologia con dati pregressi**: aprendo la tab Cronologia, le sessioni
   fatte dal console (e dalle GUI dello step 2) sono lì.
3. **Quiz GUI → cronologia**: avvia un quiz dalla Home, fallo a metà,
   abbandona. Click su Cronologia → la nuova riga c'è, etichetta
   "Abbandonato" visibile.
4. **Dettaglio**: doppio click su una riga → vedi le domande coi bordi
   colorati. Clicca "← Cronologia" → torni alla lista, la selezione è
   preservata.
5. **Sospesi**: se hai messo in pausa qualcosa dal console, lo vedi qui.
   Clicca Elimina → vedi conferma inline. Annulla → torna normale. Sì,
   elimina → la card sparisce e il file `quiz_in_pausa.json` viene aggiornato.
6. **Bottone Avvia disabled**: deseleziona tutte le materie. Il bottone
   "Avvia quiz" diventa grigio scuro su grigio chiaro, leggibile.

## Pronto per lo step 4

Per la pausa GUI lo step 4 dovrà:
- Aggiungere un bottone "Pausa" nel `QuizView` (o trigger via ESC come nelle
  decisioni UX) → costruisce e salva una `SessionePausa` da `SessioneQuiz`.
  Già abbiamo tutti i dati interni (`_codaRotazione`, `_tentativiPerId`,
  `_corretteIds`, etc.) — basta esporre un metodo `EsportaPausa()`.
- Aggiungere il **costruttore/factory** `SessioneQuiz.RiprendiDa(SessionePausa,
  mappaDomandeId)` che ricostruisce lo stato come fa
  `RiprendiSessioneInPausa` del console (vedi `wsa.quiz.cli/Program.cs:505`).
- Cambiare lo `IsEnabled` del bottone Riprendi nello SospesiView in `True`,
  agganciando `Click="OnRiprendiClick"` che fa: `MainWindow` → costruisce
  `SessioneQuiz` dalla pausa → naviga al `QuizView` come fa già per un quiz
  nuovo.

L'infrastruttura UI è già pronta a riceverlo: niente cambia nel layout di
Sospesi, basta abilitare il bottone e agganciare l'handler.

## Cose da tenere d'occhio dopo l'estrazione

Non ho potuto compilare in questo ambiente. Le cose più a rischio sono:

1. Lo style selector `Button.accent /template/ ContentPresenter#PART_ContentPresenter`
   — sintassi giusta in Avalonia 11/12, ma se per qualche motivo il template
   `Button` di Fluent in 12.0.2 ha cambiato il `Name` del part, il fix non si
   applica e il bottone disabled resta come prima (non rompe niente,
   semplicemente non si vede l'effetto).
2. Il binding `IBrush` su `Border.Background` nei DataTemplate con compiled
   bindings (uso lo stesso pattern di `RiepilogoView` esistente, dovrebbe
   filare).
3. Il `DoubleTapped` sul `ListBox` (è l'event corretto in Avalonia 12).
