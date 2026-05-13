# WSA Quiz — Stato del progetto e roadmap

> Documento di passaggio tra chat. Da caricare come contesto per riprendere il lavoro.

## Cos'è

App di quiz per il corso WsA. Esisteva una versione **console** (C#, .NET 8) che funziona e che l'utente usa per studiare. Stiamo aggiungendo un'app con **interfaccia grafica** (Avalonia) che condivide la stessa logica e gli stessi dati. Lungo termine: anche Android/iOS.

L'utente (Luca) sta imparando C# in parallelo a un corso. Il progetto è anche un esercizio didattico, quindi le scelte tendono a privilegiare chiarezza e separazione dei concetti.

## Stato attuale: STEP 7 COMPLETO + STEP 8 PARZIALE (rimandato)

L'app Avalonia ha navigazione tastiera completa **dentro il quiz**: `A/B/C/D` rispondono direttamente, `↑/↓` evidenziano una risposta con bordo giallo `#FFD500` 3px e `Invio` la conferma, `←/→` scorrono le passate già date in modalità read-only (banner giallo crema in alto + bottone "Torna alla corrente"), `Invio` in feedback avanza, `ESC` apre la modale pausa unificata Annulla/Abbandona/Salva e esci. Il ciclo pausa→sospeso→ripresa funziona end-to-end nella GUI: anche dopo una ripresa le passate pre-pausa restano navigabili con i 4 bottoni colorati (grazie a 3 campi additivi su `DettaglioRisposta` — vedi sotto). Bottone "Pausa" in alto a destra del Quiz apre la stessa modale. La Home ha la barra "Avvia quiz" sticky **in alto** (al posto del vecchio titolo/sottotitolo, ora rimossi) e scroll globale del resto della pagina (Materie, Categorie, Opzioni). Solo Categorie ha uno scroll interno con `MaxHeight=240` perché può crescere molto quando si selezionano tutte le materie; Materie no, ha poche voci. Il bottone "Riprendi" della SospesiView è attivo: ricostruisce la sessione dalla pausa, naviga al QuizView e — al termine — la pausa originale viene rimossa automaticamente (stesso comportamento del console). Le tre tab restano popolate (Home, Cronologia, Sospesi). Cronologia condivisa fra console e GUI verificata via shared storage in `%APPDATA%\WsaQuiz`.

Lo step 4 introduce due metodi su `SessioneQuiz`: `EsportaPausa()` (snapshot consistente sia in stato "in attesa" sia in stato "feedback") e factory statica `RiprendiDa(SessionePausa, mappaPerId)`. Stato interno aggiunto: `_sessioneId`, `_avviataDaRipresa`, `_offsetCronometro`, `_offsetEffettuate` (classica). Esposta proprietà pubblica `IdSessionePausa` per consentire alla `MainWindow` di eliminare la pausa originale a fine sessione.

**Step 8 (navigazione tastiera globale) è stato fermato a metà.** Funzionano: `FocusAdorner` globale (bordo giallo `#FFD500` 3px sul focus da tastiera), `Ctrl+Tab` / `Ctrl+Shift+Tab` per cambiare tab principale, frecce `←/→` dentro la modale pausa, `Tab` ciclico fra le 4 zone della Home (`KeyboardNavigation.TabNavigation="Once"` per zona + `Cycle` sul DockPanel root), `IsTabStop="False"` sui bottoni interni dei `ListBoxItem` (Elimina/Riprendi/...) e sugli header `TabItem`. Fix correlato in `QuizView`: ripristino esplicito di `Focus()` in `OnRispostaClick`/`OnAvanzaClick`, altrimenti dopo un click del mouse il focus restava sul bottone che spariva e `A/B/C/D`/frecce non arrivavano più al `OnKeyDown` del Quiz. **NON funzionano**: frecce `↑/↓` per navigare dentro la zona corrente della Home, e `Invio`/`Canc`/`Esc` sulle righe della Cronologia e dei Sospesi. Indagine in corso ma ferma: l'ipotesi era che la `ScrollViewer` (Home) e il `ListBoxItem` (liste) intercettassero gli eventi di tastiera prima del nostro override `OnKeyDown` marcandoli `Handled`; abbiamo provato a convertire gli handler in tunneling (`AddHandler(KeyDownEvent, ..., RoutingStrategies.Tunnel)`) ma in test pratico il comportamento non è cambiato. Da capire perché — possibili tracce: il `FocusManager.GetFocusedElement()` non restituisce l'elemento atteso quando si naviga via Tab; la `ListBoxItem` di Avalonia 12 imposta la selezione su `Enter` anche in fase Tunnel; oppure i miei `KeyboardNavigation` su `Once`/`Cycle` interagiscono male con i bottoni interni `IsTabStop=False`. **Decisione**: lasciamo committato lo stato attuale (Tab funziona, frecce/Invio sulle liste no), documentiamo la trappola, e si torna sopra in uno step dedicato (vedi roadmap "Step 15"). Spec di riferimento: `docs/superpowers/specs/2026-05-11-step8-navigazione-tastiera-globale-design.md`. Plan: `docs/superpowers/plans/2026-05-11-step8-navigazione-tastiera-globale.md`.

## Decisioni prese

### Stack
- **.NET 8** (target framework)
- **Avalonia 12.0.2** (release del 28 aprile 2026, .NET 8 minimo, mobile improvement notevoli — buono per il futuro Android/iOS)
- **Tema Fluent** + font **Inter**
- **LiveCharts2** prevista per i grafici (step 6, non ancora installata)
- **MVVM no**: code-behind con `INotifyPropertyChanged`. È la strada intrapresa, non rifattorizziamo.

### Naming
- Namespace radice: `Wsa.Quiz.*` (sostituisce il vecchio `QuizFdP.*`)
- Tre progetti: `Wsa.Quiz.Core` (libreria), `Wsa.Quiz.Cli` (console), `Wsa.Quiz.App` (Avalonia)
- **Importante**: il nome del progetto console NON è `Wsa.Quiz.Console` — collide con `System.Console` e rompe la build. Lezione imparata, non rifare.

### Layout repo
```
wsa.quiz/                       (root della repo, contiene .git originale sul Mac)
├── Wsa.Quiz.sln
├── materie.json                ← editabile, copiato nei bin a ogni build
├── domande/                    ← editabile, idem
│   ├── cpp/, cs/, frontend/, infra/, sql/
├── wsa.quiz.core/              (class library, invariata da step 1)
│   ├── Wsa.Quiz.Core.csproj
│   ├── Models/   (7 classi: Materia, Domanda, DomandaPreparata, OpzioniQuiz, RisultatoQuiz, DettaglioRisposta, SessionePausa)
│   └── Services/ (QuizService, StorageService)
├── wsa.quiz.cli/               (console, invariato)
│   ├── Wsa.Quiz.Cli.csproj
│   ├── Program.cs
│   └── Services/ConsoleUI.cs
└── wsa.quiz.app/               (Avalonia, ora con tre tab popolate)
    ├── Wsa.Quiz.App.csproj
    ├── Program.cs (Avalonia bootstrap)
    ├── App.axaml + App.axaml.cs        ← style globali Button.accent / Button.danger (4 stati)
    ├── app.manifest
    ├── MainWindow.axaml(.cs)             ← shell con TabControl Home/Cronologia/Sospesi
    ├── State/
    │   ├── ObservableObject.cs           ← base INPC con SetField + RaisePropertyChanged
    │   ├── MateriaSelezionabile.cs       ← wrapper observable (IsSelected, NumDomande)
    │   ├── CategoriaSelezionabile.cs     ← idem (IsSelected, ChiaveCombinata "Materia/Categoria")
    │   ├── RispostaItem.cs               ← Lettera/Testo/Stato (Neutra/Corretta/Sbagliata)
    │   ├── SessioneQuiz.cs               ← state machine event-driven (classica + rotazione)
    │   ├── RisultatoCronologiaItem.cs    ← wrapper riga cronologia (data, %, durata, colore) — step 3
    │   ├── SessioneSospesaItem.cs        ← wrapper riga sospesi + flag conferma elimina — step 3
    │   └── DettaglioRispostaItem.cs      ← wrapper riga dettaglio (colore corretto/sbagliato) — step 3
    └── Views/
        ├── HomeView.axaml(.cs)           ← selezione materie + categorie + opzioni + Avvia
        ├── QuizView.axaml(.cs)           ← domanda + 4 risposte + feedback + progresso. Step 4: tastiera + menu pausa modale
        ├── RiepilogoView.axaml(.cs)      ← % corrette + durata + lista sbagliate
        ├── PlaceholderView.axaml(.cs)    ← stub per ramo "impossibile avviare" (residuo step 2)
        ├── CronologiaView.axaml(.cs)             ← tab Cronologia (lista + swap a dettaglio) — step 3
        ├── CronologiaDettaglioView.axaml(.cs)    ← dettaglio domanda-per-domanda — step 3
        └── SospesiView.axaml(.cs)                ← tab Sospesi (lista + Riprendi/Elimina). Step 4: Riprendi attivo
```

### Aggiungere materie/domande
**Resta come prima**: editi `materie.json` e/o aggiungi file in `domande/<materia>/`. Il csproj fa `<None Include="..\materie.json">` che li copia nei bin a ogni build. Funziona per console e GUI insieme.

### Storage condiviso console ↔ GUI
`StorageService` ha due costruttori:
- `StorageService(string cartella)` — legacy (una cartella sola)
- `StorageService(string cartellaDati, string cartellaUtente)` — quello che usiamo

`cartellaDati` = output del bin (dove ci sono i JSON copiati). `cartellaUtente` = cartella per cronologia + sospesi, condivisa fra tutti gli eseguibili. Default cross-platform (helper statico `StorageService.CartellaUtenteDefault()`):
- Windows: `%APPDATA%\WsaQuiz`
- macOS: `~/Library/Application Support/WsaQuiz`
- Linux: `~/.config/WsaQuiz`

## Architettura GUI

**Code-behind + INPC, no MVVM**. Le UserControl sono il proprio DataContext. Le proprietà notificano via `SetField` definito in `ObservableObject`. Il `DataContext` è assegnato **una sola volta nel costruttore** di ogni view e mai resettato (anti-pattern dello storico WPF).

**SessioneQuiz** è la state machine event-driven che rimpiazza i loop bloccanti del console:

| Console (loop in `Program.EseguiSessione*`) | GUI (state machine in `SessioneQuiz`) |
|--------------------------------------------|----------------------------------------|
| `for ... ChiediRisposta`                   | `Avvia()` → `CaricaProssimaDomanda()`  |
| input utente                               | `RispondiA(int)` da click              |
| `MostraFeedback` + ENTER                   | `Avanza()` da click "Prossima"         |
| `break` (-1 abbandona)                     | `Abbandona()` da bottone               |
| return RisultatoQuiz                       | evento `Concluso`                      |

L'algoritmo di rotazione (re-coda casuale delle sbagliate, conteggio tentativi, % basata sul primo colpo) è 1:1 con quello del console. Il punteggio usa `QuizService.CalcolaPunteggio` esistente.

**Navigazione fra schermate**: la `MainWindow` ha tre tab. Dentro la tab Home c'è un `ContentControl` il cui `Content` viene swappato fra `HomeView`, `QuizView`, `RiepilogoView`. La tab Cronologia ha `CronologiaView` con swap interno fra lista e `CronologiaDettaglioView`. La tab Sospesi ha `SospesiView`.

**Style globali (App.axaml)**: due classi, `accent` (azzurro Fluent) e `danger` (rosso), ognuna con i 4 stati default/pointerover/pressed/disabled definiti sul `ContentPresenter#PART_ContentPresenter` del template del Button. Risolve la trappola n.6 della handoff originale: il bottone con `Background={DynamicResource SystemAccentColorBrush}` non aveva uno stato disabled distinguibile dallo sfondo. I valori del :disabled sono **espliciti in hex** (`#D6D6D6` su `#7A7A7A`), non `DynamicResource Color`, per evitare problemi di conversione Color→Brush nei `Setter` (vedi trappola 7).

## Decisioni UX prese

- **Home unificata** (decisione "Caso A"): materie a sinistra (checkbox), categorie filtrate dinamicamente a destra in base alle materie selezionate, opzioni in basso (rotazione, cronometro, randomizza, limita a N), riepilogo testuale + bottone Avvia. Una sola schermata copre Quizzone (più materie spuntate), quiz singola materia, per categorie, di N domande.
- **Multi-materia con categorie**: la chiave salvata in `OpzioniQuiz.Categorie` è `"NomeMateria/Categoria"` (già supportato dal Core). Categorie con stesso nome su materie diverse non si confondono.
- **Feedback risposta**: il bottone cliccato si colora rosso (sbagliato) o verde (corretto), e quello con la risposta giusta si colora sempre verde. Pannello di feedback con titolo (Corretto!/Sbagliato) e spiegazione, bottone "Prossima domanda".
- **Anti doppio-click**: appena risposto, tutti i bottoni risposta vengono disabilitati (`IsEnabled = IsNeutra`).
- **Abbandono**: bottone "Abbandona" in alto a destra del Quiz, conferma modale, salvataggio in cronologia con flag `Abbandonato=true` e schermata di riepilogo che riflette lo stato.
- **Cronologia con striscia colorata** a sinistra di ogni riga (verde ≥80%, ambra 50–79%, rosso <50%). Doppio click sulla riga apre il dettaglio domanda-per-domanda; bottone "← Cronologia" per tornare.
- **Dettaglio cronologia**: bordo verde sulle corrette, rosso sulle sbagliate. Per le sbagliate mostra anche la risposta corretta. Per le rotazioni mostra "Risposta corretta dopo N tentativi" se >1.
- **Cronologia auto-refresh dopo un quiz GUI**: la `MainWindow.OnQuizConcluso` chiama `_cronologiaView?.Ricarica()` dopo il salvataggio, così la nuova sessione è già visibile senza dover premere "Aggiorna".
- **Conferma Elimina inline (no dialog modali)** sui sospesi: primo click su Elimina → la riga mostra "Sicuro? [Sì, elimina] [Annulla]"; secondo click conferma. In Avalonia 12 i `MessageBox.Show` non esistono nativamente, e per uno step "leggi + elimina" il dialog modale sarebbe overkill.
- **Menu pausa = dialog modale** (step 4). Riusa lo stesso pattern di `ChiediConfermaAbbandono` (Window 420×180, CenterOwner, no resize, no taskbar). Tre pulsanti: Annulla / Salva e esci / Riprendi (accent). ESC dentro la modale equivale a Annulla. Il `QuizView` riprende il focus dopo la chiusura via `this.Focus()` per mantenere le scorciatoie tastiera attive.
- **Tastiera nel quiz** (step 4): override `OnKeyDown` sulla `QuizView` (`Focusable=true`, `this.Focus()` in `OnAttachedToVisualTree`). `A/B/C/D` solo se `InAttesaRisposta`; `Invio` solo se `RispostaInviata` (avanza); `Escape` apre menu pausa. Tutti i branch settano `e.Handled = true` per evitare che un Button con focus si auto-clicchi.

## Decisioni UX da implementare nei prossimi step

- **Eliminazione cronologia** (step 5, **nuovo**): l'utente vuole poter cancellare singole partite dalla cronologia e/o svuotarla del tutto. Decisioni UX da prendere all'apertura dello step: posizione del bottone "elimina" sulla riga (alla destra come per i sospesi? icona compatta?), conferma inline o modale, e dove mettere il "Cancella tutto" (header della tab? menu kebab?). Implicazioni storage: serve probabilmente un metodo `StorageService.EliminaRisultato(string id)` e un `SvuotaCronologia()` o equivalente — il `RisultatoQuiz` non ha oggi un Id stabile, va aggiunto (può essere uno SHA256 di `DataOra+TotaleDomande+Materia` oppure semplicemente un `Guid` salvato a `SalvaRisultato`).
- **Tastiera (estensione step 6)**:
  - `↑/↓` → seleziona la risposta da confermare con Invio
  - `←/→` → naviga tra domande già fatte (sola lettura, non si modificano)
- **Domande passate**: sola lettura, mostrano risposta data e corretta. Non si possono cambiare a posteriori.
- **Statistiche**: % corrette per materia → drill-down su categorie. Implementazione con LiveCharts2 (step 7).
- **Tema**: Fluent default, dark mode toggle quando ci arriviamo.
- **Modalità "ripasso punti deboli"** (categorie ≥60% errore): **scartata**. Non più nella roadmap.

## Roadmap

### ✅ Step 1 — Fondazione
Estrazione Core, riorganizzazione cartelle, rinomina namespace, scheletro Avalonia con verifica caricamento dati al boot. **Fatto.**

### ✅ Step 2 — Porting interfaccia in Avalonia
Home (selezione + opzioni + Avvia), Quiz (domanda + 4 risposte + feedback + progresso), Riepilogo (% + sbagliate + ritorno). `INotifyPropertyChanged` corretto, niente reset di `DataContext`. Si fa un quiz dall'inizio alla fine come dal console, e finisce in cronologia condivisa. **Fatto.**

### ✅ Step 3 — Cronologia + Sospesi come sezioni dedicate
Tab "Cronologia" con tabella sessioni passate (data, modalità, % corrette, durata) + dettaglio domanda-per-domanda al doppio-click. Tab "Sospesi" con elenco sessioni in pausa salvate (per ora solo dal console — la pausa GUI arriva nello step 4) e azioni Riprendi (disabilitato fino allo step 4) / Elimina (con conferma inline). Fix cosmetico del bottone "Avvia quiz" disabled tramite style globali `Button.accent`. **Fatto.**

### ✅ Step 4 — Tastiera + Pausa
Invio = avanti, A/B/C/D = risposta, ESC = menu pausa (Riprendi / Salva e esci / Annulla). Pausa salvata dalla GUI confluisce nei sospesi e Riprendi ricostruisce la sessione (cumulativa: dettagli, durata e contatori sopravvivono al ciclo pausa/ripresa). A fine sessione ripresa la pausa originale viene rimossa. Spec: `docs/superpowers/specs/2026-05-08-step4-tastiera-pausa-design.md`. **Fatto.**

### ✅ Step 5 — Eliminazione cronologia
`RisultatoQuiz.Id` (Guid stringa) generato in `SalvaRisultato`, migrazione lazy una-tantum dei record esistenti in `CaricaCronologia` (al primo caricamento, i record senza Id ne ricevono uno e il file viene riscritto; idempotente nelle chiamate successive). Due nuovi metodi su `StorageService`: `EliminaRisultato(string)` e `SvuotaCronologia()`. GUI: bottone "Elimina" inline su ogni riga della Cronologia con conferma in-place (pattern dei Sospesi: prima conferma azzera le altre, max una alla volta), bottone "Svuota cronologia" nell'header con dialog modale di conferma (riusa il pattern di `ChiediConfermaAbbandono` — Window 420×180, CenterOwner, ESC=Annulla, bottone "Sì, cancella tutto" `Classes="danger"`, disabilitato quando la lista è vuota), bottone "Elimina questa partita" nel `CronologiaDettaglioView` con conferma inline ed evento `EliminazioneRichiesta` che la `CronologiaView` gestisce eseguendo l'eliminazione, chiudendo il dettaglio e ricaricando. Spec: `docs/superpowers/specs/2026-05-09-step5-eliminazione-cronologia-design.md`. Plan: `docs/superpowers/plans/2026-05-09-step5-eliminazione-cronologia.md`. **Fatto.**

### ✅ Step 6 — UX rifiniture (Home sticky + Pausa unificata)
Due fix UX raccolti durante il test dello step 5:
1. **Home — barra "Avvia" sticky**. La spec originale prevedeva sticky in basso + `MaxHeight=240` anche su Materie. Dopo iterazione visiva con Luca durante il test, lo schema finale è diverso: barra sticky **in alto** come `DockPanel.Dock="Top"` al posto del vecchio titolo "Configura un nuovo quiz" + sottotitolo (entrambi rimossi); il resto della pagina è una colonna unica scrollabile (Materie, Categorie, Opzioni); solo Categorie ha lo scroll interno con `MaxHeight=240` (Materie ha 5 voci, non serve cap). Niente `BoxShadow` (era stata aggiunta poi rimossa: proiettata verso il viewport del `ScrollViewer` adiacente, copriva visivamente l'ultimo pezzo di contenuto).
2. **Pausa unificata**. Oggi ESC apre il "menu pausa" (Annulla/Salva e esci/Riprendi) e il bottone "Abbandona" apre una conferma separata (Continua/Abbandona). Si fondono in **una sola** modale, raggiungibile sia da ESC sia dal bottone in alto, con tre voci: Annulla / Abbandona (`danger`) / Salva e esci (`accent`). Il bottone in alto si rinomina in "**Pausa**". ESC dentro la modale = Annulla.

Fix UX laterali emersi nel test manuale e applicati nello stesso step:
- Feedback risposta nel `QuizView` (titolo + spiegazione): `Foreground` esplicito a `#1F1F1F` perché il vecchio `{DynamicResource SystemBaseHighColor}` in dark mode rendeva un colore quasi-bianco, illeggibile sui background fissi `#E5F4EC` / `#F9E7E6` del feedback box.
- Bottone "Prossima domanda" ora usa `Classes="accent"` invece di `Background`/`Foreground` hardcoded, per coerenza con gli altri bottoni primari e per ereditare i 4 stati definiti in `App.axaml`.

Spec: `docs/superpowers/specs/2026-05-09-step6-rifiniture-ux-design.md`. Plan: `docs/superpowers/plans/2026-05-10-step6-rifiniture-ux.md`. **Fatto.**

### ✅ Step 7 — Navigazione tra domande
`←/→` scorrono le passate già date dentro la sessione corrente in modalità read-only (banner giallo crema `#FFF8E1` + bottone "Torna alla corrente"; i 4 bottoni A/B/C/D vengono ricostruiti con colorazione verde/rosso/neutra e `IsEnabled=false`). `↑/↓` evidenziano una risposta sulla corrente con bordo giallo `#FFD500` 3px; `Invio` conferma quella evidenziata, o avanza se siamo già in feedback. Modello dati: 3 campi additivi su `DettaglioRisposta` (`RisposteShufflate`, `IndiceCorrettoShufflato`, `IndiceDataShufflato`) popolati in `SessioneQuiz.RispondiA`, necessari per ricostruire i 4 bottoni anche dopo pausa/ripresa (default vuoto/-1 per record JSON vecchi → backward compatible). Stato: `SessioneQuiz._viewIndex` (null=corrente, altrimenti indice in `Risultato.Dettagli`) + `_indiceHighlight`. Opzione A della spec (riusare le proprietà observable correnti con backup/restore via `_StatoLive`) implementata. `EsportaPausa` chiama `TornaACorrente()` come prima cosa per evitare di catturare lo stato view-mode invece del live. `RispostaItem` ha 2 nuovi flag (`IsEnabled` e `IsHighlighted`) + computed `PuoCliccare = IsEnabled && IsNeutra` che sostituisce `IsNeutra` nel binding `Button.IsEnabled`. Iterazione UX rispetto alla spec: l'highlight con bordo accent 2px era poco visibile nel test pratico, sostituito con giallo `#FFD500` 3px (vedi memoria "Iterazione UX su spec"). Spec: `docs/superpowers/specs/2026-05-10-step7-navigazione-domande-design.md`. Plan: `docs/superpowers/plans/2026-05-10-step7-navigazione-domande.md`. **Fatto.**

Fix UX laterale emerso nel test manuale: il bottone "Prossima domanda" (`Classes="accent"`) aveva il testo bianco illeggibile in dark mode. Risolto con uno style `Button.prossima /template/ ContentPresenter` che imposta `TextBlock.Foreground="#1F1F1F"` — il `Foreground` diretto sull'attributo del Button non vinceva perché lo style globale `.accent` lo ridefinisce nel template (vedi trappola 12).

### ⚠️ Step 8 — Navigazione tastiera globale (parziale)
**Fatto e funzionante**: `FocusAdorner` globale (bordo giallo `#FFD500` 3px che compare solo per focus da tastiera) su Button/CheckBox/ListBoxItem/TabItem/NumericUpDown in `App.axaml`; `Ctrl+Tab` / `Ctrl+Shift+Tab` in `MainWindow.OnKeyDown` per ciclare fra le 3 tab principali con re-focus della view attiva; modale pausa con `←/→` ciclici sui 3 bottoni (Abbandona/Annulla/Salva e esci) e `Ctrl+Tab` bloccato dentro la modale; `Tab` ciclico fra le 4 zone della Home (`ZonaAvvia`/`ZonaMaterie`/`ZonaCategorie`/`ZonaOpzioni`, ognuna con `KeyboardNavigation.TabNavigation="Once"` + `Cycle` sul DockPanel root); `TabItem.IsTabStop="False"` globale così il Tab non finisce sugli header; `IsTabStop="False"` sui bottoni interni dei `ListBoxItem` (Elimina/Riprendi/Si elimina/Annulla) per non rubare il Tab stop alla riga; `SospesiView` convertita da `ItemsControl` a `ListBox` (`x:Name="ListaPause"`, `SelectionMode="Single"`). Fix correlato in `QuizView.axaml.cs`: `OnRispostaClick` e `OnAvanzaClick` chiamano esplicitamente `Focus()` (come faceva già `OnTornaACorrenteClick`), altrimenti dopo un click del mouse il focus restava sul bottone che diventava invisibile/disabilitato e `A/B/C/D`/frecce non arrivavano più al `OnKeyDown` del Quiz.

**NON funzionante (rimandato a Step 15)**: frecce `↑/↓` dentro la zona corrente della Home; `Invio` per aprire dettaglio su una riga di Cronologia (visivamente la riga diventa selezionata "viola" ma il dettaglio non si apre); `Invio` per Riprendere su una riga di Sospesi; `Canc` per avviare la conferma elimina inline da tastiera; `Esc` sul `CronologiaDettaglioView` per tornare alla lista (non verificato — la navigazione si rompe prima). I sintomi in pratica: il bordo giallo di evidenza compare e poi sparisce dopo un'azione, le frecce non spostano la selezione nelle liste, e dopo aver perso il focus solo un click di mouse lo rimette in carreggiata.

Ipotesi sul perché (non confermate da debug definitivo): la `ScrollViewer` che avvolge il contenuto della `HomeView` intercetta `↑/↓` per lo scroll prima del nostro `OnKeyDown` marcandoli `Handled`; il `ListBoxItem` di Avalonia 12 imposta la selezione su `Enter` e marca `Handled`. Abbiamo provato a convertire i tre handler (Home/Cronologia/Sospesi) da override `OnKeyDown` (fase bubble) a `AddHandler(KeyDownEvent, ..., RoutingStrategies.Tunnel)` per intercettare gli eventi in fase di capture, prima che `ScrollViewer`/`ListBoxItem` li consumino — il test pratico non ha mostrato miglioramenti, segno che la causa è altrove o la mia comprensione del routing in Avalonia 12 è incompleta. Vedi trappola n.13.

Spec: `docs/superpowers/specs/2026-05-11-step8-navigazione-tastiera-globale-design.md`. Plan: `docs/superpowers/plans/2026-05-11-step8-navigazione-tastiera-globale.md`. **Parziale** — si chiude lo step e si rimanda la finitura allo Step 15.

### ⏳ Step 9 — Grafici
LiveCharts2 in tab "Statistiche" (quarto tab da aggiungere). % corrette per materia (bar chart), drill-down su categorie. Da verificare al momento dell'installazione che esista una versione di LiveCharts2 compatibile con Avalonia 12.

### ⏳ Step 10 — Dark mode
Toggle Fluent chiaro/scuro. Posizione del toggle da decidere (header MainWindow? menu impostazioni?). Persistenza della scelta nelle preferenze utente (vedi step 12).

### ⏳ Step 11 — Esportazione e filtri cronologia
Export della cronologia in CSV e/o JSON (utile come backup prima di "Cancella tutto" dello step 5). Filtri sulla CronologiaView per materia, range di date, percentuale. Da definire se l'export è "tutto" o rispetta i filtri attivi.

### ⏳ Step 12 — Import domande/materie da GUI
Schermata "Gestione contenuti" (nuova tab? menu? dialog?) per aggiungere domande direttamente dall'interfaccia caricando un file `.json` conforme allo schema descritto nel `README.md`. Due flussi: (a) **aggiungi a materia esistente** — l'utente sceglie la materia dal pool corrente, seleziona il file, l'app valida lo schema (4 risposte, indici 0..3, id univoci dentro il file, categoria stringa) e lo copia in `domande/<cartella materia>/<nome file>.json`; (b) **crea nuova materia** — form con `id`/`nome`/`cartella` + selezione file iniziale, l'app crea la cartella, appende la voce a `materie.json` e copia il file. Decisioni UX da prendere: dove vive (tab dedicata "Contenuti"? bottone in Home? menu?), come si gestiscono i conflitti di `id` domanda (rifiuta il file? rinumera in automatico?), se serve un'anteprima del contenuto prima di confermare. Implicazioni storage: oggi `materie.json` e `domande/` vengono copiati al build da `..\<file>` con `<None Include="...">`; vanno scritti nella cartella **sorgente** (non in `bin/`) altrimenti le modifiche si perdono al `dotnet clean`. Da valutare anche un import "incolla JSON da clipboard" oltre al file picker (utile per il flusso generazione-da-IA descritto nel README).

### ⏳ Step 13 — Preferenze utente persistite
File `settings.json` nella cartella utente (`%APPDATA%\WsaQuiz` ecc.) per ricordare: ultime opzioni quiz scelte (rotazione, cronometro, randomizza, N domande), ultima tab aperta, scelta dark mode (step 10). Da decidere se persistere anche le ultime materie/categorie selezionate.

### ⏳ Step 14 — Rifiniture distribuzione
Icona app (.ico per Windows, .icns per macOS), schermata "About" con versione/licenza, build portable e/o installer (es. `dotnet publish` self-contained, oppure pacchetti per piattaforma). Da affrontare quando il resto è stabile.

### ⏳ Step 15 — Riprendere Step 8 (frecce nelle liste e dentro la Home)
Tornare sulla parte di Step 8 lasciata aperta: far funzionare `↑/↓` per navigare la zona corrente della Home, e `Invio`/`Canc`/`Esc` sulle righe di Cronologia e Sospesi (e quindi `Esc` dal `CronologiaDettaglioView`). Lo stato attuale: i bottoni interni delle righe hanno `IsTabStop="False"` (così il Tab atterra sulla riga e non sul bottone), i miei handler sono già scritti come `AddHandler(KeyDownEvent, ..., RoutingStrategies.Tunnel)` in `HomeView`/`CronologiaView`/`SospesiView` ma in pratica non intercettano gli eventi. Da indagare seriamente, **non a tentativi**: (a) verificare in che fase del routing arriva effettivamente l'evento (aggiungere temporaneamente un `Debug.WriteLine` o un overlay diagnostico che mostri `e.Source`/`e.Route`/`e.Handled` e l'elemento focused — non ho fatto questo passaggio e penso sia la radice del nostro avanzare a tentoni); (b) capire come `ListBoxItem` di Avalonia 12 tratta `Enter` (gestione interna che marca handled? gestione via `KeyDown` con strategia di routing diversa?); (c) verificare se le `ScrollViewer` annidate nella Home consumano davvero `↑/↓` o no — fare un mini-progetto Avalonia di prova fuori dal repo se serve. Una volta capita la causa, applicare il fix minimo possibile e non più a strati. Memoria utile: l'utente ha esplicitamente chiesto "fix puntuali, non architetturali" e ha valutato il risultato in **test manuale**, smentendo la spec. Non ricominciare dalla spec dello step 8 prima di aver capito il routing in pratica.

## Trappole già scoperte (non rifare)

1. **`System.Console` shadowing**: usare un namespace `Wsa.Quiz.Console` rompe tutte le chiamate `Console.WriteLine`. Il compilatore risolve `Console` come sotto-namespace di se stesso prima di guardare in `System`. Soluzione: `Wsa.Quiz.Cli`.
2. **`Avalonia.Diagnostics` rimosso in v12**: il package non esiste più. Il rimpiazzo è `AvaloniaUI.DiagnosticsSupport` (separato), opzionale. Non includerlo finché non serve davvero per debug visuale.
3. **`RefreshBinding` con `DataContext = null/this`**: anti-pattern usato nella WPF iniziale per simulare le notifiche. Cancellava la selezione delle ListView a ogni interazione. Soluzione: `INotifyPropertyChanged` corretto su tutto, `DataContext` settato solo nel costruttore.
4. **Avalonia desktop = `OutputType=WinExe`** anche su Mac/Linux. Sembra Windows-specifico ma è il flag standard.
5. **Compiled bindings richiedono `x:DataType` ovunque** (`<AvaloniaUseCompiledBindingsByDefault>true</...>` è abilitato). Sui `UserControl` root e su ogni `<DataTemplate>`. Senza, il build dà errori cripti tipo "could not resolve property X on type System.Object".
6. **Bottone Fluent disabled + accent color = "scomparso"** *(risolto in step 3)*: in Fluent, un bottone con `Background="{DynamicResource SystemAccentColorBrush}"` quando va in `IsEnabled=false` diventa molto chiaro su sfondo chiaro e sembra assente. Risolto con style globale `Button.accent` in `App.axaml` con i 4 stati definiti sul template (`/template/ ContentPresenter#PART_ContentPresenter`). I bottoni primari ora usano `Classes="accent"` invece di `Background=` hardcoded.
7. **`DynamicResource` di tipo `Color` nei `Setter` di Style**: nella XAML "diretta" Avalonia converte automaticamente `Color → SolidColorBrush` quando assegna a `Background`/`Foreground`/`BorderBrush`, ma nei `Setter` di Style questa conversione è meno affidabile e può dare errori a runtime. Per i Setter con valori cromatici, usare `DynamicResource SystemAccentColorBrush` (versione `Brush`) o un valore esplicito hex. Soluzione adottata in step 3: gli stati `:disabled` di `Button.accent`/`Button.danger` usano hex literali (`#D6D6D6` su `#7A7A7A`).
8. **`Key.Enter` == `Key.Return`** *(scoperta in step 4)*: in `Avalonia.Input.Key` i due valori sono lo stesso enum constant (entrambi mappati a 6). Metterli su due `case` distinti dello stesso `switch` è un errore di compilazione `CS0152`. Tenerne **uno solo** (per coerenza usiamo `Key.Enter`).
9. **`SystemBaseHighColor` come `Foreground` su background hardcoded chiaro = illeggibile in dark mode** *(scoperta in step 6)*: i brush "Color" di sistema (`SystemBaseHighColor`, `SystemBaseMediumColor`...) si invertono a seconda del tema. Se il `Background` del contenitore è hardcoded chiaro (es. il feedback box `#E5F4EC` / `#F9E7E6`), in dark mode il foreground diventa quasi-bianco e si fonde con il chiaro. Soluzione: usare un `Foreground` hex esplicito (es. `#1F1F1F`) quando il background è anch'esso hex esplicito.
10. **`BoxShadow` su sticky bar adiacente a `ScrollViewer`** *(scoperta in step 6)*: aggiungere `BoxShadow="0 -2 8 0 #28000000"` su una `Border DockPanel.Dock="Bottom"` proietta l'ombra di 8px verso l'alto, e quei 8px coprono visivamente il bordo inferiore del viewport del `ScrollViewer` adiacente. Effetto "ultimo elemento tagliato" anche con scroll a fondo. Soluzione: niente shadow, basta `BorderThickness` di 1px sul lato che separa.
11. **Doppio scroll innestato (pagina + pannello con `MaxHeight`)** *(scoperta in step 6)*: avere un `ScrollViewer` esterno alla pagina + un `ScrollViewer MaxHeight=240` su un pannello interno è confondente — la rotella del mouse "perde" il pannello interno appena esci con il puntatore, e i pannelli con `MaxHeight` sembrano "tagliati" più che intenzionalmente limitati. Regola: un solo scroll dove possibile; se serve un cap interno, deve essere giustificato da una vera differenza di volume di dati (Categorie può avere 50+ voci → cap sì; Materie ne ha 5 → cap no).
12. **`Foreground` diretto su `Button Classes="accent"` non vince** *(scoperta in step 7)*: assegnare `Foreground="#1F1F1F"` come attributo del Button non sovrascrive il colore del testo se il bottone ha `Classes="accent"`. Lo style globale `Button.accent` definito in `App.axaml` colpisce il `ContentPresenter#PART_ContentPresenter` del template, che ha priorità sul `Foreground` dell'attributo. Soluzione: aggiungere una classe locale (es. `Classes="accent prossima"`) e definire uno style con selector `Button.prossima /template/ ContentPresenter` che setti `TextBlock.Foreground`. Stesso pattern già usato per `Button.risposta.corretta/sbagliata` in `QuizView.axaml`.
13. **`OnKeyDown` override su `UserControl` non basta per intercettare frecce/Invio quando ci sono `ScrollViewer` o `ListBox` sopra** *(scoperta in step 8, **non risolta**)*: il classico `protected override void OnKeyDown(KeyEventArgs e)` su una `UserControl` viene chiamato in fase di **bubbling** ed è registrato dalle class-handler interne con `handledEventsToo=false`. Se un controllo intermedio (es. `ScrollViewer` per `↑/↓` per lo scroll, o `ListBoxItem` per `Enter` per la selezione) marca l'evento come `Handled` durante il bubble, l'override del nostro UserControl non riceve mai la notifica. Tentativo di soluzione: convertire da override a `AddHandler(KeyDownEvent, handler, RoutingStrategies.Tunnel)` per intercettare in fase di capture (top-down) prima che i controlli intermedi processino l'evento. In test pratico nello step 8 questo NON ha sbloccato le frecce nelle liste/Home — quindi la radice è probabilmente altrove (forse `FocusManager.GetFocusedElement()` non restituisce l'elemento atteso quando si naviga via Tab dentro un `ListBox`/`ItemsControl`, o gli eventi non passano davvero dalla mia UserControl in tunneling perché il routing è scoped al focused element e non al sotto-albero atteso). Prima di toccare ancora codice **mettere un `Debug.WriteLine` o un overlay diagnostico** che stampi `e.Source`/`e.RoutedEvent`/`e.Handled` e l'elemento focused, e capire in pratica come fluisce l'evento — non a tentativi.

## Stato dei file

L'utente sta lavorando **sulla sandbox Windows** (`C:\Users\luca.foglia\Documents\Quiz\...`). La repo originale sul Mac (`wsa.quiz/` con `.git`) è ferma allo stato pre-step-1 e verrà allineata in seguito (probabilmente copiando l'intera sandbox sul Mac e committando in un colpo solo step 1+2+...). Per ora ogni step viene consegnato come zip da estrarre sopra alla sandbox sovrascrivendo i conflitti, e Luca rinomina la cartella di volta in volta o tiene lo stesso nome.

## Note tecniche utili per i prossimi step

### Generale
- I csproj WPF/Avalonia includono `<None Include="..\materie.json">` con `Link` per preservare la struttura di sottocartelle in `domande/`.
- `Domanda.Id` è uno SHA256 troncato a 12 char hex calcolato su `testo|risposte|indice_corretta`. Stabile a riordini, cambia se cambia il contenuto. Utile per matching cronologia/sospesi.
- `QuizService.PreparaDomanda` mescola le risposte e ricalcola l'indice della corretta — usare sempre questo, non leggere `Domanda.RispostaCorretta` direttamente.
- `OpzioniQuiz.Categorie` usa la chiave `"NomeMateria/Categoria"` per supportare multi-materia.

### Storage
- `storage.CaricaCronologia()` → `List<RisultatoQuiz>`
- `storage.CaricaPause()` → `List<SessionePausa>`
- `storage.SalvaRisultato(RisultatoQuiz)` — append nel file cronologia
- `storage.SalvaPausa(SessionePausa)` — upsert per `SessioneId`
- `storage.EliminaPausa(string sessioneId)` — già usato dalla SospesiView

### Cronologia + Sospesi (step 3)
- `RisultatoQuiz.Dettagli` contiene la sequenza completa di `DettaglioRisposta` (id, testo, risposta data, corretta, spiegazione, tentativi). È quello che il `CronologiaDettaglioView` legge per il drill-down.
- I wrapper observable in `State/` (`RisultatoCronologiaItem`, `SessioneSospesaItem`, `DettaglioRispostaItem`) tengono i colori e la formattazione fuori dalla XAML (regola dei 3 colori: `#1F7A4D` verde, `#B8860B` ambra, `#B85450` rosso).
- `CronologiaView.Ricarica()` è idempotente e thread-safe nel singolo dispatcher; viene chiamata al boot, al click di "Aggiorna", e dalla `MainWindow` dopo `SalvaRisultato`.
- `SessioneSospesaItem.InAttesaConfermaEliminazione` è observable; la SospesiView toggla questo flag per swappare i bottoni della singola riga fra "Riprendi/Elimina" e "Sì, elimina/Annulla". `OnEliminaClick` azzera prima eventuali altre conferme aperte (al massimo una alla volta).
- L'`App.axaml` espone le classi `accent` (azzurro Fluent) e `danger` (rosso). Per usarle: `<Button Classes="accent" Content="..." />` invece di `<Button Background="..." Foreground="..." />`. I 4 stati sono già coperti.

### SessioneQuiz e ripresa pausa (step 4 — fatto)
- `SessioneQuiz` espone ora: `Avvia/RispondiA/Avanza/Abbandona/EsportaPausa()` + factory statica `RiprendiDa(SessionePausa, IDictionary<string,Domanda>)` + proprietà pubblica `IdSessionePausa`.
- `EsportaPausa()` produce uno `SessionePausa` consistente sia in stato "in attesa di risposta" sia "feedback pendente": discriminante `RispostaInviata`. In rotazione + in attesa, la domanda corrente viene rimessa in testa alla coda; in classica + in attesa, `EffettuateClassica = _indiceClassica + _offsetEffettuate`; in classica + post-feedback è `+ 1`.
- `RiprendiDa()` lancia `InvalidOperationException` se nessuno dei `CodaDomandeIds` è più presente nel pool (pausa orfana). La `MainWindow` cattura, mostra errore in `ErrorePanel`, ed elimina la pausa.
- Cronometro nelle sessioni riprese: `_offsetCronometro = pausa.TempoTrascorso`, e ovunque si usi `_cron.Elapsed` adesso si somma `_offsetCronometro` (`AggiornaTempo`, `EsportaPausa`, `Concludi`).
- A fine sessione (anche per abbandono), `MainWindow.OnQuizConcluso` chiama `_storage.EliminaPausa(sessione.IdSessionePausa)` se non null. Stesso comportamento del console.

### Eliminazione cronologia (step 5 prossimo)
- Lo `StorageService` non ha oggi metodi per eliminare singoli risultati. Servirà aggiungerne almeno due: `EliminaRisultato(string id)` e `SvuotaCronologia()`. Il file di cronologia oggi è una lista JSON serializzata di `RisultatoQuiz`: leggi-filtra-riscrivi è ok (non c'è concorrenza significativa).
- `RisultatoQuiz` non ha un Id. Decisione da prendere: aggiungere un `Guid` generato a `SalvaRisultato` (semplice, retrocompatibile: per i record vecchi senza Id si genera al primo caricamento e si riscrive una volta) o un hash deterministico (`SHA256(DataOra+TotaleDomande+MateriaNome)`, troncato a 12 char come `Domanda.Id`).
- UX da definire: dove mettere "Cancella tutto" (header tab? menu kebab?), come si elimina la singola riga (icona compatta a destra come per i sospesi, o context menu su doppio-click?), conferma inline (coerente con i sospesi) o modale.

## Per ripartire

1. Apri questo MD in chat
2. Conferma stato attuale (sandbox Windows, step 7 completo + step 8 parziale: `Ctrl+Tab`, modale pausa con frecce e `Tab` ciclico fra le 4 zone della Home funzionano; frecce dentro la zona e `Invio`/`Canc` sulle righe Cronologia/Sospesi NON funzionano)
3. Si parte dallo **Step 9** (Grafici LiveCharts2 in nuova tab Statistiche, da verificare versione compatibile con Avalonia 12) oppure dallo **Step 15** (riprendere la parte fallita dello step 8 sulle liste/Home con un'indagine vera del routing degli eventi tastiera in Avalonia 12, vedi trappola 13). La scelta dipende da quanto è prioritaria la navigazione tastiera nelle liste rispetto alle statistiche.
