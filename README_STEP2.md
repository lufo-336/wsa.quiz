# WSA Quiz — Step 2: porting interfaccia in Avalonia

## Cosa contiene questo zip

Tutto il progetto allo stato dello **step 2 completo**. Va estratto **sopra** alla
sandbox dello step 1 (`C:\Users\luca.foglia\Documents\Quiz\wsa_quiz_step1\`)
sovrascrivendo i file in conflitto.

## Cosa cambia rispetto allo step 1

### File rimossi
- `MainWindow.axaml` / `MainWindow.axaml.cs` — quelli vecchi erano il check di
  boot, vengono rimpiazzati da una shell con TabControl.
- `wsa.quiz.console/` (cartella residua dello step 1, mai usata, rimossa per
  pulizia — la console "vera" e' `wsa.quiz.cli/`).
- `cleanup.ps1`, `step1_cleanup.sh`, `README_STEP1.md` (artefatti di setup,
  non piu' necessari).

### File nuovi
```
wsa.quiz.app/
├── State/
│   ├── ObservableObject.cs         (base INPC con SetField + RaisePropertyChanged)
│   ├── MateriaSelezionabile.cs     (wrapper per checkbox materia)
│   ├── CategoriaSelezionabile.cs   (wrapper per checkbox categoria)
│   ├── RispostaItem.cs             (singola risposta nel quiz, con stato visivo)
│   └── SessioneQuiz.cs             (state machine event-driven della sessione)
└── Views/
    ├── HomeView.axaml(.cs)         (configurazione sessione)
    ├── QuizView.axaml(.cs)         (esecuzione quiz)
    ├── RiepilogoView.axaml(.cs)    (fine quiz: stats + sbagliate)
    └── PlaceholderView.axaml(.cs)  (stub per Cronologia/Sospesi, step 3)
```

### File modificati
- `MainWindow.axaml(.cs)` — riscritto da zero, e' la shell con TabControl
  (Home, Cronologia, Sospesi). La transizione Home -> Quiz -> Riepilogo avviene
  swappando il `Content` del `ContentControl` dentro la tab Home.

### File invariati
- `wsa.quiz.core/**` — tutta la libreria condivisa (la GUI riusa al 100%
  `QuizService.PreparaDomanda`, `QuizService.CalcolaPunteggio`, `StorageService`).
- `wsa.quiz.cli/**` — il console gira identico a prima.
- `materie.json`, `domande/**` — invariati.
- `Wsa.Quiz.sln`, `Wsa.Quiz.App.csproj`, `App.axaml(.cs)`, `Program.cs`,
  `app.manifest`.

## Architettura

**No MVVM** come da decisione del progetto: code-behind + `INotifyPropertyChanged`.
Le UserControl sono il proprio DataContext, le proprieta' notificano via
`SetField` (in `ObservableObject`).

**Niente `RefreshBinding` con `DataContext = null/this`** (trappola dello
storico WPF): il `DataContext` viene assegnato una sola volta nel costruttore
di ogni view e mai resettato durante l'uso.

**SessioneQuiz** e' la riformulazione event-driven dei loop bloccanti del
console (`Program.EseguiSessioneClassica` / `EseguiSessioneRotazione`):

| Console (loop)              | GUI (state machine)       |
|-----------------------------|----------------------------|
| `for ... ChiediRisposta`    | `Avvia()` → primo `CaricaProssimaDomanda()` |
| input utente                | `RispondiA(int)` da click  |
| feedback                    | proprieta' observable cambiano, XAML aggiorna |
| `MostraFeedback` + ENTER    | `Avanza()` da click "Prossima domanda" |
| `break` (-1 abbandona)      | `Abbandona()` da bottone   |
| return RisultatoQuiz        | evento `Concluso`          |

Il punteggio, la cronologia salvata, la modalita' rotazione con re-coda
casuale: tutto identico al console (vedi commenti in `SessioneQuiz.cs`).

## UX

**Home (unificata, Caso A)**: a sinistra checkbox materie, a destra le
categorie filtrate dinamicamente in base alle materie selezionate, in basso
opzioni (rotazione, cronometro, randomizzazione, limita a N), sotto un
riepilogo testuale e il bottone "Avvia quiz". Una sola schermata copre tutti
i casi del console (Quizzone = piu' materie spuntate, quiz singola materia,
quiz per categoria, quiz di N domande estratte).

**Quiz**: header con numero domanda / totale / materia · categoria, eventuale
cronometro, bottone "Abbandona". Domanda grande, 4 risposte come bottoni
larghi. Click su una → si colora verde la corretta, rosso (eventuale) la
sbagliata, compare il pannello di feedback con spiegazione e bottone
"Prossima domanda". Tutti i bottoni risposta vengono disabilitati dopo
il primo click → niente doppi click.

**Riepilogo**: percentuale grossa colorata (verde >=80, ambra >=50, rosso <50),
totali, durata, modalita', e lista delle sbagliate con la tua risposta vs
quella corretta + spiegazione. Bottone "Torna alla Home".

**Tastiera**: niente, come da roadmap. Arriva nello step 4. La struttura e'
gia' pronta per agganciare i tasti senza modificare la state machine.

**Pausa**: niente per ora. C'e' solo "Abbandona" con conferma. La pausa con
ESC arriva nello step 4.

## Verifica step 2 (cose da fare)

1. **Build pulito**: `dotnet build` dalla root della soluzione. Deve
   compilare senza errori. Avvisi: tollerabili.
2. **Console immutato**: `dotnet run --project wsa.quiz.cli` parte e funziona
   come prima.
3. **GUI parte**: `dotnet run --project wsa.quiz.app` apre la finestra,
   header blu, "5 materie · 968 domande" in alto a destra (o quel che sono),
   tre tab (Home/Cronologia/Sospesi). Le ultime due hanno il placeholder.
4. **Quiz dall'inizio alla fine**:
   - spunti una o piu' materie (vedi le categorie apparire);
   - opzionalmente spunti delle categorie;
   - leggi che il riepilogo cambia (es. "C# — 3 categorie — 27 domande");
   - clicchi Avvia;
   - rispondi alle domande, vedi feedback corretto/sbagliato;
   - arrivi al riepilogo, controlli percentuale e lista sbagliate;
   - "Torna alla Home" → torni e puoi farne un altro.
5. **Cronologia condivisa**: dopo aver finito un quiz dalla GUI, lancia il
   console. Il menu "Cronologia globale" deve mostrare la sessione appena
   fatta dall'app.
6. **Modalita' rotazione**: spunta "Modalita' rotazione" e fai un quiz su un
   pool piccolo (es. 5 domande). Sbaglia di proposito — la stessa domanda
   ricompare dopo qualche turno, il "totale previsto" si aggiorna.
7. **Cronometro**: spunta "Mostra cronometro" e parti — appare in alto a
   destra dell'header del quiz, formato `mm:ss` o `hh:mm:ss`.
8. **Limita a N**: spunta "Limita a 10 domande", scegli un pool grosso, parti.
   Il quiz finisce a 10.
9. **Abbandona**: a meta' quiz clicca Abbandona → conferma → riepilogo
   mostra "Quiz abbandonato". Le domande gia' risposte sono in cronologia.

## Trappole gia' note ed evitate

1. **Compiled bindings richiedono `x:DataType`** — messo su tutti i
   `UserControl` e `DataTemplate`. Senza, build fallisce con errori cripti.
2. **Niente reset di `DataContext` per refreshare** — INPC fatto a regola,
   `DataContext` settato solo nel costruttore.
3. **Doppi click sulla stessa risposta** — bloccati: il bottone si disabilita
   appena lo `Stato` esce da `Neutra`, e `RispondiA()` ignora chiamate
   successive a `RispostaInviata=true`.
4. **Cronometro che continua dopo Concludi** — `Stopwatch` e `DispatcherTimer`
   stoppati esplicitamente in `Concludi()`.

## Cose che potrebbero richiedere un aggiustamento al primo build

Non ho potuto compilare dall'ambiente in cui sono stati scritti i file
(SDK .NET non disponibile dietro il proxy). Se al primo `dotnet build` esce
qualche errore, i punti piu' probabili sono:

- **`NumericUpDown.Value`** in Avalonia 12 e' tipato come `decimal?` (nullable).
  Il binding TwoWay verso `decimal LimiteN` (non-nullable) in `HomeView.axaml.cs`
  dovrebbe funzionare grazie alla coercion implicita, ma se da' fastidio
  basta rendere `LimiteN` di tipo `decimal?` e gestire il null nel calcolo
  (`(int)(LimiteN ?? 10)`).
- **Selettori multi-classe `Button.risposta.corretta:disabled /template/ ContentPresenter`**:
  in Fluent l'override del background al disabled potrebbe richiedere una
  specificita' diversa. Se i bottoni risposta diventano grigi invece che
  verdi/rossi quando si clicca, e' qui da ritoccare. **Cosmetico**, non rompe
  il flusso.
- **Negation nei compiled bindings** (`{Binding !UltimaRispostaCorretta}`,
  `{Binding !NessunaCategoriaDisponibile}`, `{Binding !RispostaInviata}`):
  supportata in Avalonia 11+. Se 12 ha cambiato qualcosa, basta sostituire
  con due property bool separate.

Nessuno di questi e' bloccante.
