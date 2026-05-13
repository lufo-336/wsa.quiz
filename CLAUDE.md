# WSA Quiz — Riferimento di progetto

> Documento unico di contesto. Sostituisce e unifica `README_STEP1/2/3.md`,
> `WSA_QUIZ_HANDOFF.md` e le spec/plan in `docs/superpowers/`.

## Cos'è

App di quiz per il corso WsA. Esisteva una versione **console** (C#, .NET 8) che
funziona e che Luca usa per studiare. È stata aggiunta un'app con **interfaccia
grafica** (Avalonia) che condivide la stessa logica e gli stessi dati. Lungo
termine: anche Android/iOS.

Luca sta imparando C# in parallelo a un corso. Il progetto è anche un esercizio
didattico, quindi le scelte privilegiano chiarezza e separazione dei concetti.

## Stato attuale

**Step 7 completo + Step 8 parziale (rimandato a Step 15).**

L'app Avalonia ha navigazione tastiera completa **dentro il quiz**:
`A/B/C/D` rispondono direttamente, `↑/↓` evidenziano una risposta con bordo
giallo `#FFD500` 3px e `Invio` la conferma, `←/→` scorrono le passate già date
in modalità read-only (banner giallo crema in alto + bottone "Torna alla
corrente"), `Invio` in feedback avanza, `ESC` apre la modale pausa unificata
Annulla/Abbandona/Salva e esci. Il ciclo pausa→sospeso→ripresa funziona
end-to-end nella GUI: anche dopo una ripresa le passate pre-pausa restano
navigabili con i 4 bottoni colorati. Cronologia condivisa fra console e GUI
verificata via shared storage in `%APPDATA%\WsaQuiz`.

## Stack

- **.NET 8** (target framework)
- **Avalonia 12.0.2** (release del 28 aprile 2026, .NET 8 minimo, mobile
  improvement notevoli — buono per il futuro Android/iOS)
- **Tema Fluent** + font **Inter**
- **LiveCharts2** prevista per i grafici (step 9, non ancora installata)
- **MVVM no**: code-behind con `INotifyPropertyChanged`. È la strada
  intrapresa, non rifattorizziamo.

## Layout repo

```
wsa.quiz/
├── Wsa.Quiz.sln
├── materie.json                ← editabile, copiato nei bin a ogni build
├── domande/                    ← editabile, idem
│   ├── cpp/, cs/, frontend/, infra/, sql/
├── wsa.quiz.core/              (class library)
│   ├── Wsa.Quiz.Core.csproj
│   ├── Models/   (RisultatoQuiz, DettaglioRisposta, SessionePausa,
│   │              Materia, Domanda, DomandaPreparata, OpzioniQuiz)
│   └── Services/ (QuizService, StorageService)
├── wsa.quiz.cli/               (console)
│   ├── Wsa.Quiz.Cli.csproj
│   ├── Program.cs
│   └── Services/ConsoleUI.cs
└── wsa.quiz.app/               (Avalonia, tre tab popolate)
    ├── Wsa.Quiz.App.csproj
    ├── Program.cs (Avalonia bootstrap)
    ├── App.axaml + App.axaml.cs   ← style Button.accent / Button.danger /
    │                                Focus adorner globale
    ├── QuizColors.cs              ← colori condivisi (verde/ambra/rosso)
    ├── MainWindow.axaml(.cs)      ← shell con TabControl Home/Cronologia/Sospesi
    ├── State/
    │   ├── ObservableObject.cs
    │   ├── MateriaSelezionabile.cs
    │   ├── CategoriaSelezionabile.cs
    │   ├── RispostaItem.cs
    │   ├── SessioneQuiz.cs               ← state machine event-driven
    │   ├── RisultatoCronologiaItem.cs
    │   ├── SessioneSospesaItem.cs
    │   └── DettaglioRispostaItem.cs
    └── Views/
        ├── HomeView.axaml(.cs)
        ├── QuizView.axaml(.cs)
        ├── RiepilogoView.axaml(.cs)
        ├── PlaceholderView.axaml(.cs)    ← ramo "impossibile avviare"
        ├── CronologiaView.axaml(.cs)
        ├── CronologiaDettaglioView.axaml(.cs)
        └── SospesiView.axaml(.cs)
```

### Naming

- Namespace radice: `Wsa.Quiz.*` (sostituisce il vecchio `QuizFdP.*`)
- Tre progetti: `Wsa.Quiz.Core` (libreria), `Wsa.Quiz.Cli` (console),
  `Wsa.Quiz.App` (Avalonia)
- **Importante**: il nome del progetto console NON è `Wsa.Quiz.Console` —
  collide con `System.Console` e rompe la build. (Trappola 1)

## Build & run

```powershell
dotnet build Wsa.Quiz.sln
dotnet run --project wsa.quiz.cli
dotnet run --project wsa.quiz.app
```

## Aggiungere materie/domande

Editi `materie.json` e/o aggiungi file in `domande/<materia>/`. I csproj fanno
`<None Include="..\materie.json">` con `Link` per copiarli nei bin a ogni build,
preservando la struttura di sottocartelle. Funziona per console e GUI insieme.

## Storage condiviso console ↔ GUI

`StorageService` ha due costruttori:
- `StorageService(string cartella)` — legacy
- `StorageService(string cartellaDati, string cartellaUtente)` — quello in uso

`cartellaDati` = output del bin (dove ci sono i JSON copiati). `cartellaUtente`
= cartella per cronologia + sospesi, condivisa fra tutti gli eseguibili.
Default cross-platform via helper statico `StorageService.CartellaUtenteDefault()`:

- Windows: `%APPDATA%\WsaQuiz`
- macOS: `~/Library/Application Support/WsaQuiz`
- Linux: `~/.config/WsaQuiz`

### API

- `storage.CaricaCronologia()` → `List<RisultatoQuiz>` (migrazione lazy: i
  record vecchi senza `Id` ne ricevono uno e il file viene riscritto, una
  volta sola — idempotente)
- `storage.CaricaPause()` → `List<SessionePausa>`
- `storage.SalvaRisultato(RisultatoQuiz)` — append; genera `Guid` se `Id` vuoto
- `storage.SalvaPausa(SessionePausa)` — upsert per `SessioneId`
- `storage.EliminaPausa(string sessioneId)`
- `storage.EliminaRisultato(string id)`
- `storage.SvuotaCronologia()`

### Modello dati — note

- `Domanda.Id` è uno **SHA256 troncato a 12 char hex** calcolato su
  `testo|risposte|indice_corretta`. Stabile a riordini, cambia se cambia il
  contenuto. Utile per matching cronologia/sospesi.
- `RisultatoQuiz.Id` è un **Guid stringa** generato in `SalvaRisultato`.
- `OpzioniQuiz.Categorie` usa la chiave `"NomeMateria/Categoria"` per
  supportare multi-materia.
- `QuizService.PreparaDomanda` mescola le risposte e ricalcola l'indice della
  corretta — usare sempre questo, non leggere `Domanda.RispostaCorretta`
  direttamente.
- `RisultatoQuiz.Dettagli` contiene la sequenza completa di `DettaglioRisposta`
  (id, testo, risposta data, corretta, spiegazione, tentativi). Da step 7 ha
  anche 3 campi additivi (`RisposteShufflate`, `IndiceCorrettoShufflato`,
  `IndiceDataShufflato`) che il `CronologiaDettaglioView` ignora ma che
  servono al `QuizView` in view-mode per ricostruire i 4 bottoni colorati
  anche dopo pausa/ripresa. Default vuoto/-1 → backward compatible coi record
  JSON vecchi.

## Architettura GUI

**Code-behind + INPC, no MVVM.** Le UserControl sono il proprio DataContext.
Le proprietà notificano via `SetField` definito in `ObservableObject`. Il
`DataContext` è assegnato **una sola volta nel costruttore** di ogni view e
mai resettato (anti-pattern dello storico WPF — Trappola 3).

### `SessioneQuiz` — state machine

Rimpiazza i loop bloccanti del console:

| Console (loop in `Program.EseguiSessione*`) | GUI (state machine) |
|---|---|
| `for ... ChiediRisposta` | `Avvia()` → `CaricaProssimaDomanda()` |
| input utente | `RispondiA(int)` da click |
| `MostraFeedback` + ENTER | `Avanza()` da click "Prossima" |
| `break` (-1 abbandona) | `Abbandona()` da bottone |
| return RisultatoQuiz | evento `Concluso` |

L'algoritmo di rotazione (re-coda casuale delle sbagliate, conteggio tentativi,
% basata sul primo colpo) è 1:1 con quello del console. Il punteggio usa
`QuizService.CalcolaPunteggio` esistente.

### Pausa e ripresa

`SessioneQuiz` espone:
- `EsportaPausa()` — produce un `SessionePausa` consistente sia in stato "in
  attesa" sia "feedback pendente" (discriminante: `RispostaInviata`). In
  rotazione + in attesa, la domanda corrente viene rimessa in testa alla
  coda; in classica + in attesa, `EffettuateClassica = _indiceClassica +
  _offsetEffettuate`; in classica + post-feedback è `+ 1`. In view-mode
  chiama internamente `TornaACorrente()` come prima cosa per non catturare
  lo stato view-mode invece del live.
- `static RiprendiDa(SessionePausa, IDictionary<string,Domanda>)` — factory.
  Lancia `InvalidOperationException` se nessuno dei `CodaDomandeIds` è più
  presente nel pool (pausa orfana). La `MainWindow` cattura, mostra errore
  in `ErrorePanel`, ed elimina la pausa.
- `IdSessionePausa` — proprietà pubblica read-only: `_sessioneId` se
  `_avviataDaRipresa==true`, altrimenti `null`. Serve alla `MainWindow` per
  chiamare `EliminaPausa` al termine di una sessione ripresa.

Stato interno aggiunto: `_sessioneId`, `_avviataDaRipresa`, `_offsetCronometro`
(sommato ovunque si usi `_cron.Elapsed`), `_offsetEffettuate` (classica).

A fine sessione (anche per abbandono), `MainWindow.OnQuizConcluso` chiama
`_storage.EliminaPausa(sessione.IdSessionePausa)` se non null.

### Navigazione fra domande passate (view-mode)

Stato in `SessioneQuiz`: `_viewIndex` (`null`=corrente, altrimenti indice in
`Risultato.Dettagli`) + `_indiceHighlight`. Implementazione **Opzione A**:
riusa le proprietà observable correnti con backup/restore via `_StatoLive`
nei metodi privati `SalvaStatoLive()` / `RipristinaStatoLive()`. Ogni nuovo
campo observable di sessione live va aggiunto in entrambi i metodi.

`RispostaItem` ha 2 flag (`IsEnabled`, `IsHighlighted`) + computed
`PuoCliccare = IsEnabled && IsNeutra` che sostituisce `IsNeutra` nel binding
`Button.IsEnabled`. Necessario per disabilitare i bottoni in view-mode senza
cambiare il template XAML.

### Navigazione fra schermate

La `MainWindow` ha tre tab. Dentro la tab Home c'è un `ContentControl` il cui
`Content` viene swappato fra `HomeView`, `QuizView`, `RiepilogoView`. La tab
Cronologia ha `CronologiaView` con swap interno fra lista e
`CronologiaDettaglioView`. La tab Sospesi ha `SospesiView`.

`Ctrl+Tab` / `Ctrl+Shift+Tab` (gestiti in `MainWindow.OnKeyDown`) ciclano fra
le 3 tab principali con re-focus della view attiva.

### Style globali (App.axaml)

Due classi, `accent` (azzurro Fluent) e `danger` (rosso), ognuna con i 4
stati default/pointerover/pressed/disabled definiti sul
`ContentPresenter#PART_ContentPresenter` del template del Button. Risolve la
trappola del bottone Fluent disabled (vedi Trappola 6). Per usarle:
`<Button Classes="accent" Content="..." />` invece di `<Button Background="..." />`.

I valori del `:disabled` sono **espliciti in hex** (`#D6D6D6` su `#7A7A7A`),
non `DynamicResource Color`, per evitare problemi di conversione Color→Brush
nei `Setter` (Trappola 7).

`FocusAdorner` globale (bordo giallo `#FFD500` 3px che compare solo per focus
da tastiera, non da click) è applicato su Button/CheckBox/ListBoxItem/TabItem/
NumericUpDown.

### Colori condivisi — `QuizColors.cs`

Centralizzati nel C#:
- Verde `#1F7A4D`, Ambra `#B8860B`, Rosso `#B85450`
- Focus `#FFD500`, Disabilitato sfondo `#D6D6D6`, testo `#7A7A7A`
- API: `QuizColors.Percentuale(double pct)` (verde≥80, ambra≥50, rosso<50)
  e `QuizColors.Esito(bool corretto)`.

Nei `.axaml` gli stessi hex sono ancora hard-coded (XAML non legge facilmente
costanti C# senza risorse). La centralizzazione vera richiederebbe risorse
XAML — fuori scope finché non serve.

## Decisioni UX consolidate

- **Home unificata** ("Caso A"): materie a sinistra (checkbox), categorie
  filtrate dinamicamente a destra in base alle materie selezionate, opzioni
  in basso (rotazione, cronometro, randomizza, limita a N). Una sola
  schermata copre Quizzone (più materie spuntate), quiz singola materia, per
  categorie, di N domande.
- **Home — barra "Avvia" sticky in alto** (DockPanel.Dock="Top"); resto
  della pagina è una colonna unica scrollabile (Materie, Categorie, Opzioni).
  Solo Categorie ha scroll interno con `MaxHeight=240` (Materie ha 5 voci,
  non serve cap). Niente `BoxShadow` (Trappola 10).
- **Multi-materia con categorie**: chiave `"NomeMateria/Categoria"`. Categorie
  con stesso nome su materie diverse non si confondono.
- **Feedback risposta**: il bottone cliccato si colora rosso/verde, e quello
  con la risposta giusta si colora sempre verde. Pannello con titolo + spiegazione,
  bottone "Prossima domanda".
- **Anti doppio-click**: appena risposto, tutti i bottoni risposta vengono
  disabilitati (`IsEnabled = PuoCliccare`).
- **Cronologia con striscia colorata** a sinistra di ogni riga. Doppio click
  apre il dettaglio domanda-per-domanda; bottone "← Cronologia" per tornare.
  Cronologia auto-refresh dopo un quiz GUI (`MainWindow.OnQuizConcluso`
  chiama `_cronologiaView?.Ricarica()`).
- **Dettaglio cronologia**: bordo verde sulle corrette, rosso sulle sbagliate.
  Per le sbagliate mostra anche la risposta corretta. Per le rotazioni mostra
  "Risposta corretta dopo N tentativi" se >1.
- **Eliminazione cronologia** (step 5):
  - Bottone "Elimina" inline su ogni riga, conferma in-place (max una alla volta).
  - "Svuota cronologia" nell'header con dialog modale (pattern
    `ChiediConfermaAbbandono`, ESC=Annulla, bottone "Sì, cancella tutto"
    `Classes="danger"`, disabilitato quando la lista è vuota).
  - "Elimina questa partita" nel `CronologiaDettaglioView` con conferma
    inline ed evento `EliminazioneRichiesta` che la `CronologiaView` gestisce
    eseguendo l'eliminazione, chiudendo il dettaglio e ricaricando.
- **Conferma Elimina inline (no dialog modali)** sui sospesi: primo click →
  riga mostra "Sicuro? [Sì, elimina] [Annulla]"; secondo conferma. In Avalonia
  12 i `MessageBox.Show` non esistono nativamente.
- **Pausa unificata** (step 6): bottone in alto a destra del Quiz si chiama
  "**Pausa**". Stesso bottone e `ESC` aprono la stessa modale. Tre voci:
  Annulla / Abbandona (`danger`, a sinistra) / Salva e esci (`accent`, a destra).
  Modale 480×210, CenterOwner, no resize, no taskbar. Annulla è il focus di
  default. ESC dentro la modale = Annulla.
- **Tastiera nel quiz** (step 4 + 7):
  - `A/B/C/D` → risponde direttamente (solo se `InAttesaRisposta`).
  - `↑/↓` → highlight su una risposta in attesa (giallo `#FFD500` 3px;
    iterazione UX rispetto alla spec originale che usava bordo accent 2px,
    poco visibile in test pratico).
  - `Invio` → conferma highlight (se presente) o avanza (in feedback).
  - `←/→` → scorre passate già date (banner crema `#FFF8E1` + bottone
    "Torna alla corrente"; i 4 bottoni A/B/C/D ricostruiti con colorazione
    verde/rosso/neutra e `IsEnabled=false`).
  - `Esc` → modale pausa (anche in view-mode).
  - Override `OnKeyDown` con `Focusable=true` e `this.Focus()` in
    `OnAttachedToVisualTree`. Tutti i branch settano `e.Handled = true`.
- **Focus dopo click mouse**: in `QuizView.OnRispostaClick` /
  `OnAvanzaClick` / `OnTornaACorrenteClick` chiama esplicitamente `Focus()`,
  altrimenti dopo un click del mouse il focus resta sul bottone che diventa
  invisibile/disabilitato e i tasti non arrivano più al `OnKeyDown` del Quiz.
- **Modalità "ripasso punti deboli"** (categorie ≥60% errore): **scartata**.

## Roadmap

### ✅ Step 1 — Fondazione
Estrazione Core, riorganizzazione cartelle, rinomina namespace `QuizFdP`→`Wsa.Quiz.*`,
scheletro Avalonia. Cartella utente condivisa cross-platform per cronologia/sospesi.

### ✅ Step 2 — Porting interfaccia in Avalonia
Home (selezione + opzioni + Avvia), Quiz (domanda + 4 risposte + feedback +
progresso), Riepilogo (% + sbagliate + ritorno). INPC corretto, niente reset
di `DataContext`. Quiz dall'inizio alla fine come dal console, finisce in
cronologia condivisa.

### ✅ Step 3 — Cronologia + Sospesi come tab dedicate
Tab Cronologia (data, modalità, %, durata) + dettaglio domanda-per-domanda.
Tab Sospesi con Riprendi (allora disabilitato) / Elimina (conferma inline).
Style globali `Button.accent`/`Button.danger` per fix bottone disabled.

### ✅ Step 4 — Tastiera + Pausa
Invio = avanti, A/B/C/D = risposta, ESC = menu pausa. Pausa salvata dalla GUI
confluisce nei sospesi e Riprendi ricostruisce la sessione (cumulativa:
dettagli, durata e contatori sopravvivono al ciclo). A fine sessione ripresa
la pausa originale viene rimossa.

### ✅ Step 5 — Eliminazione cronologia
`RisultatoQuiz.Id` (Guid) generato in `SalvaRisultato`, migrazione lazy
una-tantum dei record esistenti. `EliminaRisultato(string)` e
`SvuotaCronologia()` su `StorageService`. Vedi sezione "Decisioni UX".

### ✅ Step 6 — UX rifiniture (Home sticky + Pausa unificata)
Home: vedi sezione "Decisioni UX". Pausa: tre voci unificate. Fix laterali
applicati: feedback box con `Foreground="#1F1F1F"` esplicito (Trappola 9);
"Prossima domanda" con `Classes="accent"` per coerenza.

### ✅ Step 7 — Navigazione tra domande
Vedi sezione "Navigazione fra domande passate (view-mode)". Iterazione UX:
highlight giallo `#FFD500` 3px invece di accent 2px. Fix laterale: bottone
"Prossima domanda" testo bianco illeggibile in dark mode → style
`Button.prossima /template/ ContentPresenter` con `TextBlock.Foreground="#1F1F1F"`
(Trappola 12).

### ⚠️ Step 8 — Navigazione tastiera globale (parziale)

**Funziona**:
- `FocusAdorner` globale (bordo giallo `#FFD500` 3px sul focus da tastiera).
- `Ctrl+Tab` / `Ctrl+Shift+Tab` per cambiare tab principale.
- Frecce `←/→` dentro la modale pausa; `Ctrl+Tab` bloccato dentro la modale.
- `Tab` ciclico fra le 4 zone della Home (`KeyboardNavigation.TabNavigation="Once"`
  per zona + `Cycle` sul DockPanel root).
- `IsTabStop="False"` sui bottoni interni dei `ListBoxItem` (Elimina/Riprendi/...)
  e sugli header `TabItem`.
- `SospesiView` convertita da `ItemsControl` a `ListBox` (`SelectionMode="Single"`).

**NON funziona** (rimandato a Step 15):
- Frecce `↑/↓` dentro la zona corrente della Home.
- `Invio` per aprire dettaglio su una riga di Cronologia (visivamente la riga
  diventa selezionata "viola" ma il dettaglio non si apre).
- `Invio` per Riprendere su una riga di Sospesi.
- `Canc` per avviare la conferma elimina inline da tastiera.
- `Esc` sul `CronologiaDettaglioView` per tornare alla lista.

Sintomi: il bordo giallo di evidenza compare e poi sparisce dopo un'azione,
le frecce non spostano la selezione nelle liste, e dopo aver perso il focus
solo un click di mouse lo rimette in carreggiata.

Ipotesi non confermate: la `ScrollViewer` che avvolge la `HomeView` intercetta
`↑/↓` per lo scroll prima del nostro `OnKeyDown` marcandoli `Handled`; il
`ListBoxItem` di Avalonia 12 imposta la selezione su `Enter` e marca
`Handled`. Tentativo di soluzione: convertire da override `OnKeyDown` (bubble)
ad `AddHandler(KeyDownEvent, ..., RoutingStrategies.Tunnel)` — non ha
sbloccato niente nei test pratici. Vedi Trappola 13.

### ⏳ Step 9 — Grafici
LiveCharts2 in nuova tab "Statistiche" (quarta). % corrette per materia
(bar chart), drill-down su categorie. Verificare al momento dell'installazione
una versione di LiveCharts2 compatibile con Avalonia 12.

### ⏳ Step 10 — Dark mode
Toggle Fluent chiaro/scuro. Posizione del toggle da decidere. Persistenza
nelle preferenze utente (step 13).

### ⏳ Step 11 — Esportazione e filtri cronologia
Export CSV/JSON (utile come backup pre-svuotamento). Filtri per materia,
range di date, percentuale. Da definire se export è "tutto" o rispetta i filtri.

### ⏳ Step 12 — Import domande/materie da GUI
Schermata "Gestione contenuti" per aggiungere domande caricando un file `.json`
conforme allo schema del README. Due flussi: aggiungi a materia esistente /
crea nuova materia. Decisioni UX da prendere: dove vive, gestione conflitti
di `id` domanda, anteprima. **Implicazione storage**: oggi `materie.json` e
`domande/` vengono copiati al build da `..\<file>` con `<None Include="...">`;
vanno scritti nella cartella **sorgente** (non in `bin/`) altrimenti le
modifiche si perdono al `dotnet clean`.

### ⏳ Step 13 — Preferenze utente persistite
File `settings.json` nella cartella utente. Ricorda: ultime opzioni quiz,
ultima tab aperta, scelta dark mode.

### ⏳ Step 14 — Rifiniture distribuzione
Icona app, schermata "About", build portable e/o installer (`dotnet publish`
self-contained, o pacchetti per piattaforma).

### ⏳ Step 15 — Riprendere Step 8
Far funzionare `↑/↓` dentro la Home e `Invio`/`Canc`/`Esc` sulle righe di
Cronologia/Sospesi. Da indagare seriamente, **non a tentativi**:

1. Verificare in che fase del routing arriva effettivamente l'evento
   (aggiungere `Debug.WriteLine` o un overlay diagnostico che mostri
   `e.Source`/`e.Route`/`e.Handled` e l'elemento focused — questo passaggio
   non è stato fatto e probabilmente è la radice del nostro avanzare a tentoni).
2. Capire come `ListBoxItem` di Avalonia 12 tratta `Enter`.
3. Verificare se le `ScrollViewer` annidate nella Home consumano davvero
   `↑/↓` (mini-progetto Avalonia di prova fuori dal repo se serve).

Una volta capita la causa, applicare il fix minimo possibile.

## Trappole già scoperte (non rifare)

1. **`System.Console` shadowing**: usare un namespace `Wsa.Quiz.Console`
   rompe tutte le chiamate `Console.WriteLine`. Il compilatore risolve
   `Console` come sotto-namespace di se stesso prima di guardare in
   `System`. Soluzione: `Wsa.Quiz.Cli`.
2. **`Avalonia.Diagnostics` rimosso in v12**: il package non esiste più. Il
   rimpiazzo è `AvaloniaUI.DiagnosticsSupport` (separato), opzionale. Non
   includerlo finché non serve.
3. **`RefreshBinding` con `DataContext = null/this`**: anti-pattern usato
   nella WPF iniziale. Cancellava la selezione delle ListView a ogni
   interazione. Soluzione: INPC corretto su tutto, `DataContext` settato solo
   nel costruttore.
4. **Avalonia desktop = `OutputType=WinExe`** anche su Mac/Linux. Sembra
   Windows-specifico ma è il flag standard.
5. **Compiled bindings richiedono `x:DataType` ovunque**
   (`<AvaloniaUseCompiledBindingsByDefault>true</...>`). Sui `UserControl`
   root e su ogni `<DataTemplate>`. Senza, il build dà errori cripti tipo
   "could not resolve property X on type System.Object".
6. **Bottone Fluent disabled + accent color = "scomparso"** *(risolto step 3)*:
   in Fluent, un bottone con `Background="{DynamicResource SystemAccentColorBrush}"`
   quando va in `IsEnabled=false` diventa molto chiaro su sfondo chiaro e
   sembra assente. Risolto con style globale `Button.accent` in `App.axaml`
   con i 4 stati definiti sul template
   (`/template/ ContentPresenter#PART_ContentPresenter`).
7. **`DynamicResource` di tipo `Color` nei `Setter` di Style**: nella XAML
   "diretta" Avalonia converte automaticamente `Color → SolidColorBrush`
   quando assegna a `Background`/`Foreground`/`BorderBrush`, ma nei `Setter`
   di Style la conversione è meno affidabile e può dare errori a runtime.
   Per i Setter cromatici, usare `DynamicResource SystemAccentColorBrush`
   (versione `Brush`) o un valore esplicito hex. Gli stati `:disabled` di
   `Button.accent`/`Button.danger` usano hex literali (`#D6D6D6` su `#7A7A7A`).
8. **`Key.Enter` == `Key.Return`** *(scoperta step 4)*: in `Avalonia.Input.Key`
   sono lo stesso enum constant. Metterli su due `case` distinti dello stesso
   `switch` è un errore `CS0152`. Tenerne **uno solo** (per coerenza usiamo
   `Key.Enter`).
9. **`SystemBaseHighColor` come `Foreground` su background hardcoded chiaro
   = illeggibile in dark mode** *(scoperta step 6)*: i brush "Color" di
   sistema si invertono a seconda del tema. Se il `Background` del
   contenitore è hardcoded chiaro (es. feedback box `#E5F4EC` / `#F9E7E6`),
   in dark mode il foreground diventa quasi-bianco. Soluzione: `Foreground`
   hex esplicito (es. `#1F1F1F`) quando il background è anch'esso hex.
10. **`BoxShadow` su sticky bar adiacente a `ScrollViewer`** *(scoperta step 6)*:
    aggiungere `BoxShadow="0 -2 8 0 #28000000"` su una `Border` adiacente al
    viewport del `ScrollViewer` proietta l'ombra di 8px che copre
    visivamente il bordo del viewport. Effetto "ultimo elemento tagliato".
    Soluzione: niente shadow, basta `BorderThickness` di 1px sul lato che separa.
11. **Doppio scroll innestato (pagina + pannello con `MaxHeight`)** *(scoperta
    step 6)*: avere un `ScrollViewer` esterno alla pagina + un `ScrollViewer
    MaxHeight=240` su un pannello interno è confondente. Regola: un solo
    scroll dove possibile; se serve un cap interno, deve essere giustificato
    da una vera differenza di volume di dati.
12. **`Foreground` diretto su `Button Classes="accent"` non vince**
    *(scoperta step 7)*: assegnare `Foreground="#1F1F1F"` come attributo non
    sovrascrive il colore del testo se il bottone ha `Classes="accent"`. Lo
    style globale colpisce il `ContentPresenter#PART_ContentPresenter`, che
    ha priorità sul `Foreground` dell'attributo. Soluzione: classe locale
    aggiuntiva (es. `Classes="accent prossima"`) e style con selector
    `Button.prossima /template/ ContentPresenter` che setti
    `TextBlock.Foreground`. Stesso pattern già usato per
    `Button.risposta.corretta/sbagliata` in `QuizView.axaml`.
13. **`OnKeyDown` override su `UserControl` non basta per intercettare
    frecce/Invio quando ci sono `ScrollViewer` o `ListBox` sopra**
    *(scoperta step 8, **non risolta**)*: l'override viene chiamato in fase
    di **bubbling** ed è registrato dalle class-handler interne con
    `handledEventsToo=false`. Se un controllo intermedio (es. `ScrollViewer`
    per `↑/↓`, o `ListBoxItem` per `Enter`) marca l'evento come `Handled`
    durante il bubble, l'override non riceve mai la notifica. Tentativo:
    convertire ad `AddHandler(KeyDownEvent, handler, RoutingStrategies.Tunnel)`
    per intercettare in fase di capture (top-down). In test pratico
    nello step 8 questo NON ha sbloccato le frecce nelle liste/Home — quindi
    la radice è probabilmente altrove. Prima di toccare ancora codice:
    `Debug.WriteLine` o overlay diagnostico per vedere `e.Source`/
    `e.RoutedEvent`/`e.Handled` e l'elemento focused.

## Note per i prossimi step

### Cronologia + Sospesi
- I wrapper observable in `State/` (`RisultatoCronologiaItem`,
  `SessioneSospesaItem`, `DettaglioRispostaItem`) tengono i colori e la
  formattazione fuori dalla XAML.
- `CronologiaView.Ricarica()` è idempotente e thread-safe nel singolo
  dispatcher; viene chiamata al boot, al click di "Aggiorna", e dalla
  `MainWindow` dopo `SalvaRisultato`.
- `SessioneSospesaItem.InAttesaConfermaEliminazione` è observable; la
  SospesiView toggla questo flag per swappare i bottoni. `OnEliminaClick`
  azzera prima eventuali altre conferme aperte (al massimo una alla volta).
- L'`App.axaml` espone le classi `accent` e `danger`. Per usarle:
  `<Button Classes="accent" Content="..." />`.

### Storage atomico (post code-review PR #1)
- `StorageService` valida i path (anti directory-traversal) tramite
  `Path.GetFullPath` + check `StartsWith`.
- Scritture su file (`SalvaRisultato`, `SalvaPausa`, `SvuotaCronologia`,
  `EliminaRisultato`, `EliminaPausa`) usano `ScriviAtomico` (temp + Move
  con `overwrite:true`).
- Letture (`LeggiFileCondiviso`) usano `FileShare.Read` per consentire letture
  concorrenti.
- `JsonSerializerOptions` ha `MaxDepth=64`. Niente `UnsafeRelaxedJsonEscaping`.
- `Random.Shared` (.NET 6+) sostituisce le istanze statiche `Random` non
  thread-safe in `QuizService` e `SessioneQuiz`.

### ⚠️ Regressione UTC (post-merge PR #1)
PR #1 ha cambiato `DateTime.Now` → `DateTime.UtcNow` nei *writer*
(`SessioneQuiz`, `Program.cs`, `ConsoleUI`) ma **non** nei punti di
visualizzazione che continuano a formattare il `DateTime` direttamente:

- `wsa.quiz.app/Views/CronologiaDettaglioView.axaml.cs:83`
- `wsa.quiz.app/State/RisultatoCronologiaItem.cs:53`
- `wsa.quiz.app/State/SessioneSospesaItem.cs:47`
- `wsa.quiz.cli/Services/ConsoleUI.cs:680,734`

Conseguenza: a Roma in ora legale, l'utente vede ogni nuovo quiz con orario
sbagliato di **2 ore**. Inoltre `cronologia.json` ora contiene un mix
permanente di timestamp locali (vecchi) e UTC (nuovi), indistinguibili dopo
il round-trip JSON. **Da fixare** con uno dei due approcci:
- tornare a `DateTime.Now` (annotare il caveat),
- aggiungere `.ToLocalTime()` in tutti i punti di formattazione *e*
  serializzare con `Kind=Utc` esplicito.

## Per ripartire

1. Leggi questa pagina.
2. Conferma stato attuale (sandbox Windows, step 7 completo + step 8 parziale).
3. Si parte dallo **Step 9** (Grafici) o dallo **Step 15** (riprendere step 8
   con un'indagine vera del routing eventi tastiera in Avalonia 12, vedi
   trappola 13). La scelta dipende dalla priorità della navigazione tastiera
   nelle liste rispetto alle statistiche. **Prima**, valutare se chiudere la
   regressione UTC sopra.
