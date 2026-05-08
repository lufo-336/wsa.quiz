# Step 4 — Tastiera + Pausa nella GUI Avalonia

> Data: 2026-05-08
> Scope: porting nella GUI delle scorciatoie da tastiera del console e della funzione "metti in pausa"; abilitazione del bottone "Riprendi" nei sospesi (chiusura del cerchio aperto nello step 3).

## Contesto

Lo step 3 ha completato Cronologia e Sospesi come tab dedicate. Il bottone "Riprendi" della `SospesiView` è oggi visibile ma `IsEnabled=False` con tooltip "Disponibile dallo step 4". Lo step 4 lo attiva e introduce la pausa lato GUI, così la pipeline pausa→sospeso→ripresa funziona in entrambi gli eseguibili (console e GUI) usando lo stesso storage.

Le decisioni UX sono già fissate dall'handoff:
- **Trigger pausa**: solo `ESC` dalla tastiera (no nuovo bottone in header del Quiz).
- **Menu pausa**: dialog modale come `ChiediConfermaAbbandono`, tre pulsanti "Riprendi", "Salva e esci", "Annulla".
- **Riprendi dai Sospesi**: switch automatico alla tab Home con `QuizView` caricato.

Decisioni tecniche di sfondo:
- Code-behind + INPC, niente MVVM.
- `SessioneQuiz` è la state machine event-driven già esistente (vedi `wsa.quiz.app/State/SessioneQuiz.cs`).
- Lo storage condiviso è `StorageService` (cartella utente cross-platform).

## Obiettivi

1. **Tastiera nel quiz**: `A/B/C/D` selezionano la risposta, `Invio` avanza alla domanda successiva, `ESC` apre il menu pausa.
2. **Menu pausa**: modale Riprendi/Salva e esci/Annulla. "Salva e esci" persiste un `SessionePausa` e torna alla Home.
3. **Riprendi dai Sospesi**: il bottone della `SospesiView` ricostruisce una `SessioneQuiz` dalla pausa e naviga al `QuizView`.
4. **Pausa orfana**: se i `CodaDomandeIds` di una pausa non hanno più corrispondenza con il pool corrente (es. domande rimosse da `materie.json`), la pausa viene eliminata e l'utente vede un messaggio.

Fuori scope (rimangono per gli step successivi):
- Navigazione `←/→` fra domande passate (step 5).
- Ripasso punti deboli (scartato dalla roadmap).

## Architettura

Niente cambiamenti strutturali. Lo step 4 estende `SessioneQuiz` con due metodi, aggiunge gestione tastiera + dialog modale alla `QuizView`, abilita il bottone Riprendi della `SospesiView`, e arrotonda gli handler della `MainWindow`.

```
QuizView (UserControl, Focusable=true)
 ├── OnAttachedToVisualTree → Avvia + Focus()
 ├── OnKeyDown                ← nuovo
 │    ├── A/B/C/D → SessioneQuiz.RispondiA(idx)
 │    ├── Enter   → SessioneQuiz.Avanza()
 │    └── Escape  → ApriMenuPausa() (modale)
 ├── ChiediConfermaAbbandono   (esistente)
 └── ApriMenuPausa             ← nuovo
       ├── Riprendi  → chiudi modale, niente
       ├── Salva esci → storage.SalvaPausa(_sessione.EsportaPausa())
       │              → evento QuizMessoInPausa
       └── Annulla   → chiudi modale

SessioneQuiz
 ├── Avvia()                              ← modificato: salta shuffle se _avviataDaRipresa
 ├── EsportaPausa() → SessionePausa       ← nuovo
 └── static RiprendiDa(SessionePausa,
        IDictionary<string,Domanda>)
        → SessioneQuiz                    ← nuovo

SospesiView
 └── OnRiprendiClick → evento RiprendiRichiesto(SessionePausa)   ← nuovo

MainWindow
 ├── Caricamento(): aggancia _sospesiView.RiprendiRichiesto
 ├── OnRiprendiSospesa(p): RiprendiDa, monta QuizView, tab Home
 └── OnQuizMessoInPausa(): torna a HomeView, _sospesiView.Ricarica()
```

## Componenti — dettaglio

### 1. Tastiera in `QuizView`

`QuizView.axaml` riceve `Focusable="True"`. `QuizView.axaml.cs` aggiunge:
- `OnAttachedToVisualTree`: dopo `_sessione.Avvia()`, `this.Focus()`.
- Override `OnKeyDown(KeyEventArgs)`:
  - `Key.A/B/C/D` → calcola indice, se `_sessione.InAttesaRisposta && idx < _sessione.Risposte.Count` chiama `_sessione.RispondiA(idx)`. `e.Handled = true`.
  - `Key.Enter` → se `_sessione.RispostaInviata` chiama `_sessione.Avanza()`. `e.Handled = true` (impedisce a un eventuale Button con focus di "cliccarsi").
  - `Key.Escape` → `ApriMenuPausa()`. `e.Handled = true`.
  - Altri tasti: nessun handling, lascia bubble.

Nota Avalonia: la `UserControl` deve avere `Focusable=true` e devi chiamare `Focus()` perché `OnKeyDown` venga invocato a livello root. In alternativa si può sottoscrivere `KeyDown += ...`; uso `OnKeyDown` (override) per simmetria con `OnAttachedToVisualTree`. Il dialog modale `Abbandona` apre comunque una `Window` separata che cattura il proprio focus, quindi i tasti dentro la modale non arrivano qui.

### 2. Menu pausa modale

Pattern identico a `ChiediConfermaAbbandono`:

```
Window 380x180, CenterOwner, no resize, no taskbar
StackPanel
  TextBlock "Quiz in pausa. Cosa vuoi fare?"
  StackPanel orizzontale right-aligned, spacing 8
    Button "Annulla"        (default)
    Button "Salva e esci"   (Classes="danger" o neutro)
    Button "Riprendi"       (Classes="accent")
```

`Riprendi` chiude la finestra senza azione. `Annulla` idem. `Salva e esci`:
1. costruisce `SessionePausa` via `_sessione.EsportaPausa()`,
2. `_storage.SalvaPausa(pausa)`,
3. solleva un evento pubblico `QuizMessoInPausa(this, EventArgs.Empty)` su `QuizView`,
4. chiude la modale.

`MainWindow` riceve l'evento e:
- ferma il riferimento al `QuizView` (esce di scope),
- monta `HomeView`,
- chiama `_sospesiView?.Ricarica()` (così la nuova pausa è già visibile alla prossima apertura del tab).

ESC dentro la modale: gestito con `KeyDown` sulla finestra, equivale ad "Annulla". Non passa al `QuizView` sotto perché la modale è in foreground.

Lo `_storage` non è oggi accessibile da `QuizView`. Due opzioni:
1. (scelta) Passare `StorageService` al costruttore di `QuizView`. È un'aggiunta minima alla signature, e la `MainWindow` ce l'ha già.
2. Sollevare l'evento "voglio mettere in pausa, dammi la sessione esportata" e farlo gestire dalla `MainWindow`. Più elaborato.

Vado con opzione 1: la `QuizView(SessioneQuiz, StorageService)` rende esplicita la dipendenza e l'evento `QuizMessoInPausa` segnala solo "ho già salvato, switcha a Home".

### 3. `SessioneQuiz.EsportaPausa()`

Restituisce un `SessionePausa` consistente in entrambi gli stati possibili al momento dell'ESC:

| Campo | Classica, in attesa | Classica, post-feedback | Rotazione, in attesa | Rotazione, post-feedback |
|---|---|---|---|---|
| `CodaDomandeIds` | `_ordineClassica.Skip(_indiceClassica).Select(d=>d.Id)` | `Skip(_indiceClassica + 1)` | `[_domandaCorrente.Originale.Id] + _codaRotazione.Select(Id)` | `_codaRotazione.Select(Id)` |
| `EffettuateClassica` | `_indiceClassica + _offsetEffettuate` | `+1` | n/a | n/a |
| `CorretteClassica` | `_correttoClassica` | `_correttoClassica` | n/a | n/a |
| `IndicePosizioneRotazione` | n/a | n/a | `_indicePosizioneRotazione - 1` | `_indicePosizioneRotazione` |
| `TotaleUnicheRotazione` | n/a | n/a | `_totUnicoRotazione` | idem |
| `SbagliateContatore` | n/a | n/a | `_sbagliateContatoreRotazione` | idem |
| `TentativiPerId` | n/a | n/a | `new(_tentativiPerId)` | idem |
| `CorretteIds` | n/a | n/a | `_corretteIds.ToList()` | idem |
| `Dettagli` | `Risultato.Dettagli.ToList()` | idem | idem | idem |
| `TempoTrascorso` | `_cron.Elapsed + _offsetCronometro` | idem | idem | idem |
| `Opzioni` | `Opzioni` | idem | idem | idem |
| `ModalitaRotazione` | `Opzioni.Rotazione` | idem | idem | idem |
| `SessioneId` | `_sessioneId` (nuovo campo) | idem | idem | idem |
| `DataOraPausa` | `DateTime.Now` | idem | idem | idem |

Il discriminante "in attesa" vs "post-feedback" è il flag esistente `RispostaInviata`.

Side effects: `EsportaPausa()` ferma `_cron` e `_timer` (la sessione non viene più usata).

### 4. `SessioneQuiz.RiprendiDa(SessionePausa pausa, IDictionary<string,Domanda> mappaPerId)` (factory statica)

1. Mappa `pausa.CodaDomandeIds` a `Domanda`, scartando gli id che non sono più presenti (`mappaPerId.TryGetValue`).
2. Se la lista risultante è vuota, lancia `InvalidOperationException("Le domande di questa pausa non sono più disponibili.")`. La `MainWindow` cattura, mostra l'errore, e chiama `EliminaPausa`.
3. Costruisce `new SessioneQuiz(pool: domandeRicostruite, opzioni: pausa.Opzioni)`.
4. Imposta i campi privati (tramite parametri o metodi privati di setup, vedi sotto):
   - `_sessioneId = pausa.SessioneId` (per upsert su re-pausa).
   - `_offsetCronometro = pausa.TempoTrascorso`.
   - `Risultato.Dettagli` ← `pausa.Dettagli.ToList()` (cumulativo).
   - Classica: `_ordineClassica = domandeRicostruite`, `_indiceClassica = 0`, `_correttoClassica = pausa.CorretteClassica`, `_offsetEffettuate = pausa.EffettuateClassica`. `Corrette = pausa.CorretteClassica; Errate = pausa.EffettuateClassica - pausa.CorretteClassica`.
   - Rotazione: `_codaRotazione = new LinkedList<Domanda>(domandeRicostruite)`, `_tentativiPerId = new Dictionary<string,int>(pausa.TentativiPerId)`, `_corretteIds = new HashSet<string>(pausa.CorretteIds)`, `_totUnicoRotazione = pausa.TotaleUnicheRotazione`, `_sbagliateContatoreRotazione = pausa.SbagliateContatore`, `_indicePosizioneRotazione = pausa.IndicePosizioneRotazione`. `Corrette = _corretteIds.Count; Errate = _sbagliateContatoreRotazione`.
   - `_avviataDaRipresa = true`.
5. Restituisce la `SessioneQuiz` non ancora avviata. La `QuizView` la avvia normalmente.

### 5. `Avvia()` — modifica leggera

Se `_avviataDaRipresa`:
- non costruisce `_ordineClassica` né `_codaRotazione` (già fatti),
- non re-shuffla,
- imposta comunque `Risultato.DataOra/Modalita/MateriaNome/CategorieSelezionate/...` come oggi (la cronologia mostra l'ora di completamento, non quella della prima sessione — coerente con il console che fa `risultato.DataOra = DateTime.Now`),
- `_cron.Start()`, `_timer.Start()` se cronometro.

`AggiornaTempo()` mostra `(_cron.Elapsed + _offsetCronometro)` invece di `_cron.Elapsed`.

`Concludi()` salva `Risultato.DurataQuiz = _cron.Elapsed + _offsetCronometro`.

`Concludi()` continua a non eliminare la pausa: lo fa la `MainWindow` (vedi sotto), in modo da seguire il pattern esistente per cui `MainWindow` è l'unica a parlare con `_storage` (a parte la pausa salvata in `QuizView`, dove serve la dipendenza diretta).

### 6. `SospesiView`

Cambi minimi:
- `OnRiprendiClick` nuovo handler. Solleva evento pubblico `RiprendiRichiesto(SessionePausa)` con `item.Pausa`.
- XAML: `<Button Content="Riprendi" Classes="accent" Click="OnRiprendiClick" Padding="14,6" FontSize="12"/>` (rimuovo `IsEnabled="False"` e il tooltip; uso `Classes="accent"` per coerenza con Avvia/Torna alla Home, dato che è l'azione primaria della card).

Il blocco "Stato vuoto" ha un testo che cita "step 4"; lo aggiorno a una versione neutra.

### 7. `MainWindow`

Aggiunte:
- `_homeView.QuizAvviato`: oggi passa `SessioneQuiz` a una nuova `QuizView`. Diventa `new QuizView(sessione, _storage!)`.
- Aggancio `quizView.QuizMessoInPausa += OnQuizMessoInPausa`.
- Nel `Caricamento()`: `_sospesiView.RiprendiRichiesto += OnRiprendiSospesa`.
- `OnRiprendiSospesa(SessionePausa p)`:
  ```
  try {
      var mappa = _tutteDomande.ToDictionary(d => d.Id);
      var sessione = SessioneQuiz.RiprendiDa(p, mappa);
      var quizView = new QuizView(sessione, _storage!);
      quizView.QuizConcluso += OnQuizConcluso;
      quizView.QuizMessoInPausa += OnQuizMessoInPausa;
      HomeArea.Content = quizView;
      Tabs.SelectedIndex = 0;
  } catch (InvalidOperationException ex) {
      ErrorePanel.IsVisible = true;
      ErroreText.Text = ex.Message + " La pausa è stata rimossa.";
      _storage!.EliminaPausa(p.SessioneId);
      _sospesiView!.Ricarica();
  }
  ```
- `OnQuizMessoInPausa(...)`: torna a `HomeView`, `_sospesiView?.Ricarica()`.
- `OnQuizConcluso`: alla normale conclusione di una sessione che era stata ripresa, va comunque eliminata l'eventuale pausa preesistente con quel `SessioneId`. Subito dopo `SalvaRisultato`: se `sessione.IdSessionePausa != null`, chiamare `_storage!.EliminaPausa(sessione.IdSessionePausa)` e poi `_sospesiView?.Ricarica()`. (Stesso comportamento di `EseguiSessione*` console che a fine sessione chiama `EliminaPausa(stato.SessioneId)`.)

## Modello dati

Nessuna modifica a `SessionePausa`. Tutti i campi necessari ci sono già.

`SessioneQuiz` aggiunge come stato privato:
- `string _sessioneId = Guid.NewGuid().ToString("N")` (riassegnato da `RiprendiDa`).
- `bool _avviataDaRipresa`.
- `int _offsetEffettuate` (classica).
- `TimeSpan _offsetCronometro`.

Espone una proprietà pubblica read-only `string? IdSessionePausa` che restituisce `_sessioneId` se `_avviataDaRipresa == true`, altrimenti `null`. Serve alla `MainWindow` per chiamare `EliminaPausa` al termine di una sessione ripresa. Il resto è internal bookkeeping.

Eventi pubblici:
- `QuizView.QuizConcluso` (esistente): `EventHandler<SessioneQuiz>`.
- `QuizView.QuizMessoInPausa` (nuovo): `EventHandler` (no payload — la `MainWindow` fa solo navigation).
- `SospesiView.RiprendiRichiesto` (nuovo): `EventHandler<SessionePausa>`.

## Errori e casi limite

| Caso | Comportamento |
|---|---|
| Pausa con tutte le domande rimosse dal pool | `RiprendiDa` lancia, `MainWindow` elimina pausa e mostra errore. |
| Pausa con alcune domande rimosse | Quelle presenti vengono usate. La sessione è più corta. Coerente col console. |
| ESC durante il feedback (post risposta) | `EsportaPausa` salva la coda partendo dalla *successiva*, dettaglio già in `Dettagli`. La ripresa parte dalla domanda nuova. |
| ESC mentre il modale Abbandona è aperto | Annulla l'Abbandona (comportamento Avalonia di default), il quiz resta in stato attuale. Il `KeyDown` di `QuizView` non viene chiamato perché la modale ha catturato il focus. |
| Click "Annulla" sul menu pausa | Modale chiusa, nessuna modifica, focus torna su `QuizView`. Verificare con `this.Focus()` post-modale (probabilmente non serve, ma metto un `Focus()` di sicurezza). |
| Quiz ripreso → utente lo finisce normalmente | `Concludi` salva `RisultatoQuiz` con `Dettagli` cumulativi e `DurataQuiz` cumulativa, `MainWindow` elimina la pausa originale. |
| Quiz ripreso → utente lo abbandona | Idem (anche l'abbandono va in cronologia con `Abbandonato=true` e i dettagli accumulati fino a quel momento). |
| Quiz ripreso → utente preme di nuovo "Salva e esci" | `EsportaPausa` produce un `SessionePausa` con lo *stesso* `SessioneId`. `StorageService.SalvaPausa` fa upsert. Niente duplicati. |

## Test plan manuale

1. **Tastiera base**: dalla Home avvia un quiz. Premi `B` → la risposta B viene "cliccata", feedback visibile. Premi `Invio` → avanza. Ripeti su tutta la sessione.
2. **Salva pausa, classica**: avvia un quiz classico, rispondi alla 2/10, premi `ESC` → menu pausa. Click "Salva e esci" → torno alla Home. Tab Sospesi → c'è una nuova card con "2/10 risposte" e tempo coerente.
3. **Riprendi, classica**: dalla card sospesi click "Riprendi" → switch automatico alla tab Home con la domanda 3/10 caricata. Tempo continua dal punto in cui era. Completa la sessione → Cronologia mostra una sola riga 10/10.
4. **ESC sul feedback**: avvia quiz, rispondi, durante il feedback (prima di Avanza) premi `ESC` → Salva e esci. Riprendi: la prossima domanda è la *successiva*, non quella appena risposta. `Cronologia.Dettagli` finale contiene tutte le risposte una sola volta.
5. **Rotazione**: avvia quiz rotazione, sbaglia 1, rispondi giusta a 2, ESC → Salva e esci. Sospesi: "0/N padroneggiate · 1 sbagliata" coerente. Riprendi: padroneggiate e sbagliate ripartono dai contatori giusti.
6. **Pausa orfana**: chiudi l'app, edita `materie.json` rimuovendo una materia che era nella pausa, riapri l'app. Sospesi: la card è ancora lì. Riprendi: errore "Le domande di questa pausa non sono più disponibili. La pausa è stata rimossa.", la card sparisce.
7. **Annulla pausa**: ESC → Annulla → torno al quiz, focus tastiera funziona ancora (premi `B` → seleziona).
8. **Abbandona vs Pausa**: durante un quiz, click "Abbandona" → conferma → cronologia con flag Abbandonato. La sessione *non* finisce nei sospesi.
9. **Tab Cronologia**: dopo un quiz ripreso e completato, una sola riga in Cronologia con la durata totale (pre-pausa + post-pausa).

## File toccati

```
wsa.quiz.app/
├── State/SessioneQuiz.cs                ← +EsportaPausa, +RiprendiDa, +Avvia mod, +offset cronometro, +offset effettuate, +sessioneId, +flag avviataDaRipresa
├── Views/QuizView.axaml                  ← Focusable=true (e basta)
├── Views/QuizView.axaml.cs               ← +OnKeyDown, +ApriMenuPausa, +evento QuizMessoInPausa, costruttore (+StorageService), Focus() in OnAttachedToVisualTree
├── Views/SospesiView.axaml               ← Riprendi: rimosso IsEnabled=False, rimosso tooltip, aggiunto Click; testo stato vuoto neutralizzato
├── Views/SospesiView.axaml.cs            ← +OnRiprendiClick, +evento RiprendiRichiesto
└── MainWindow.axaml.cs                    ← passaggio _storage al QuizView, gestione QuizMessoInPausa, gestione RiprendiRichiesto, EliminaPausa al termine sessione ripresa
```

Nessun file nuovo. Nessuna modifica a `wsa.quiz.core` o `wsa.quiz.cli`.

## Rischi noti

- **Focus tastiera dopo la modale**: in alcuni scenari Avalonia non riporta automaticamente il focus al `QuizView` dopo la chiusura di `Window.ShowDialog`. Mitigato con `await ShowDialog(...)` seguito da `this.Focus()`.
- **Conflitto Invio + Button focus**: se un Button ha focus quando arriva `Enter`, Avalonia di default genera un click. `e.Handled = true` nel `OnKeyDown` del UserControl interrompe l'evento *se* il bubbling parte dalla UserControl — ma se il bottone è figlio e riceve l'evento prima, no. Soluzione: nel `OnKeyDown` controllo che il `e.KeyModifiers == None` e `Handled` lo metto sempre prima di fare cose. Se restano problemi in test, si valuta `KeyboardNavigation.IsTabStop="False"` sui bottoni risposta o l'override di `OnPreviewKeyDown` (in Avalonia: handler con `RoutingStrategies.Tunnel`).
- **Trappola DataContext step 3**: ovviamente non resetto MAI `DataContext` durante la modale o il cambio focus.
- **Compiled bindings**: la `QuizView` ha già `x:DataType` e niente nuovo binding viene aggiunto.

## Pronto per lo step 5

Lo step 5 (`←/→` per navigare fra domande passate, sola lettura) si appoggia sullo stesso `OnKeyDown` introdotto qui — basterà aggiungere due rami. Lo stato "view-index ≠ answering-index" sarà un nuovo campo observable in `SessioneQuiz`.
