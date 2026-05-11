# WSA Quiz — Manuale documentativo

> Documento didattico che racconta come è nato e come si è evoluto questo progetto. Scritto per persone che non hanno mai programmato (o che hanno appena cominciato): non si presuppone che tu sappia cos'è una "classe" o un "namespace"; lo spiego strada facendo. Se sai già queste cose, salta avanti — la struttura a capitoli è pensata per essere navigata.

## Indice

- [Introduzione](#introduzione)
- [Parte I — Cose da sapere prima di leggere il resto](#parte-i--cose-da-sapere-prima-di-leggere-il-resto)
  - [1.1 Mini glossario](#11-mini-glossario)
  - [1.2 Da cosa siamo partiti: il quiz console preesistente](#12-da-cosa-siamo-partiti-il-quiz-console-preesistente)
  - [1.3 L'idea: aggiungere un'interfaccia grafica](#13-lidea-aggiungere-uninterfaccia-grafica)
- [Parte II — Lo stack tecnico](#parte-ii--lo-stack-tecnico)
  - [2.1 Perché C# e .NET 8](#21-perché-c-e-net-8)
  - [2.2 Console contro interfaccia grafica](#22-console-contro-interfaccia-grafica)
  - [2.3 Avalonia: cos'è e perché l'abbiamo scelta](#23-avalonia-cosè-e-perché-labbiamo-scelta)
  - [2.4 Niente MVVM: cosa significa](#24-niente-mvvm-cosa-significa)
  - [2.5 JSON come "database" dei contenuti](#25-json-come-database-dei-contenuti)
- [Parte III — La struttura del progetto](#parte-iii--la-struttura-del-progetto)
  - [3.1 Tre progetti, una soluzione](#31-tre-progetti-una-soluzione)
  - [3.2 Il Core (Wsa.Quiz.Core)](#32-il-core-wsaquizcore)
  - [3.3 La console (Wsa.Quiz.Cli)](#33-la-console-wsaquizcli)
  - [3.4 L'app grafica (Wsa.Quiz.App)](#34-lapp-grafica-wsaquizapp)
  - [3.5 I dati](#35-i-dati)
- [Parte IV — La storia in 7 step](#parte-iv--la-storia-in-7-step)
  - [Step 1 — Fondazione](#step-1--fondazione)
  - [Step 2 — Porting in Avalonia](#step-2--porting-in-avalonia)
  - [Step 3 — Cronologia e Sospesi](#step-3--cronologia-e-sospesi)
  - [Step 4 — Tastiera e Pausa](#step-4--tastiera-e-pausa)
  - [Step 5 — Eliminazione cronologia](#step-5--eliminazione-cronologia)
  - [Step 6 — Rifiniture UX](#step-6--rifiniture-ux)
  - [Step 7 — Navigazione tra domande](#step-7--navigazione-tra-domande)
- [Parte V — Le trappole che ci hanno insegnato qualcosa](#parte-v--le-trappole-che-ci-hanno-insegnato-qualcosa)
- [Parte VI — Come si rifà da zero](#parte-vi--come-si-rifà-da-zero)
- [Parte VII — Come abbiamo lavorato con l'IA](#parte-vii--come-abbiamo-lavorato-con-lia)
- [Parte VIII — Cosa c'è dopo](#parte-viii--cosa-cè-dopo)

---

## Introduzione

Questo documento racconta la storia di un piccolo software: un'**app di quiz** per ripassare il programma del corso WSA (Web & Software Architect). Non è solo "come si usa" — quello è scritto nel `README.md`. Qui ti spiego **come è nato, perché abbiamo fatto certe scelte e non altre, dove abbiamo sbagliato e cosa abbiamo imparato**.

Lo scopo è doppio:

1. **Per me che ho scritto il codice**: avere un appiglio narrativo che spieghi il "perché" — il "cosa" lo dice il codice stesso, e fra sei mesi senza un perché scritto altrove rischio di non ricordare come ci sono arrivato.
2. **Per chi legge senza esperienza di programmazione**: capire come si costruisce un software passo dopo passo. Non è magia, non è genio: è una sequenza di scelte piccole, ognuna delle quali si può capire.

Se non hai mai programmato, leggi i capitoli in ordine. Se invece sai già cose, usa l'indice e salta dove ti serve.

---

## Parte I — Cose da sapere prima di leggere il resto

### 1.1 Mini glossario

Per non interrompere il discorso più avanti, ecco i termini che useremo. Salta pure le voci che già conosci.

**Linguaggio di programmazione.** Un linguaggio (inventato) che il computer capisce dopo essere stato "tradotto" in istruzioni macchina. Esempi: C#, Python, JavaScript, Java. Noi useremo C#.

**Codice sorgente.** I file di testo scritti nel linguaggio. Hanno estensioni come `.cs` (C#) o `.json` (dati). Sono leggibili da un essere umano.

**Compilatore.** Il programma che traduce il codice sorgente in qualcosa che il computer può eseguire (un `.exe` su Windows, per esempio). Quando dici "compilo" intendi "lancio il compilatore".

**Build.** L'atto di compilare. "Faccio il build" = "compilo".

**Eseguibile / binario.** Il file finale che si lancia per far partire il programma. Si trova nella cartella `bin/` dopo un build.

**Libreria.** Codice già scritto da qualcun altro che il tuo programma usa. Per esempio Avalonia è una libreria che ti dà finestre, bottoni, eccetera. Tu non li riscrivi: li importi.

**Dipendenza.** Sinonimo pratico di "libreria che il mio progetto usa". Le dipendenze si dichiarano in un file di configurazione (per C# è il `.csproj`) e il sistema le scarica automaticamente.

**SDK (Software Development Kit).** Il pacchetto che contiene il compilatore + le librerie standard + gli strumenti. Per noi è il **.NET 8 SDK** di Microsoft. Una volta installato, hai il comando `dotnet` disponibile in terminale.

**Terminale / shell.** La finestra nera con il testo dove si scrivono comandi. Su Windows è PowerShell o CMD; su Mac/Linux è bash o zsh. Non è obbligatoria per chi usa un IDE, ma è utile saperla.

**IDE (Integrated Development Environment).** Un programma per scrivere codice che integra editor, compilatore, debugger, esplora-file. Esempi: Visual Studio, Rider, VS Code. Si può fare tutto da terminale + editor di testo, ma con un IDE è più comodo.

**Repository (repo) / Git.** Un repository è una cartella di progetto sotto controllo di versione: significa che ogni modifica ai file viene registrata con uno snapshot, e puoi tornare indietro nel tempo o vedere "cosa è cambiato fra ieri e oggi". Git è il programma che fa tutto questo. **GitHub** è un sito che ospita repository Git online.

**Commit.** Un singolo snapshot delle modifiche, con un messaggio che dice cosa è stato fatto. Esempio: `feat(step7): aggiungo navigazione tra domande`.

**Branch.** Una linea di sviluppo parallela. Quando vuoi provare qualcosa senza rovinare quello che funziona, crei un branch. Noi qui abbiamo lavorato sempre sul branch principale (`main`) perché è un progetto di una persona sola.

**Classe.** Un "stampo" che descrive un certo tipo di cosa nel codice. Per esempio una classe `Domanda` descrive cos'è una domanda (un testo, 4 risposte, un indice corretto). Quando esegui il programma puoi creare tante "istanze" di questa classe (= domande concrete con valori reali). Sintatticamente in C# si scrive `class Domanda { ... }`.

**Oggetto / istanza.** Una "cosa concreta" creata a partire da una classe. La classe `Domanda` è lo stampo, "la domanda numero 302 sulla console" è un oggetto di tipo `Domanda`.

**Metodo.** Una "azione" che una classe sa fare. Per esempio la classe `SessioneQuiz` ha un metodo `RispondiA(indice)` che registra la risposta dell'utente. I metodi si chiamano usando il punto: `sessione.RispondiA(2)`.

**Proprietà / campo / variabile.** Sono "dati" che una classe contiene. La classe `Domanda` ha la proprietà `TestoDomanda` (una stringa), `RisposteShufflate` (una lista di 4 stringhe), eccetera. La differenza tecnica fra proprietà e campo non ci interessa qui.

**Namespace.** Un "cassetto" logico per organizzare classi. Serve a non avere collisioni di nomi: se due librerie diverse hanno una classe `Lista`, basta che siano in due namespace diversi e non si pestano i piedi. In C# si scrive `namespace Wsa.Quiz.Core;` in cima al file.

**JSON.** Un formato di testo per rappresentare dati strutturati. Lo usiamo per i contenuti del quiz (domande, materie, cronologia salvata). Vedere il `README.md` per esempi.

**INotifyPropertyChanged (INPC).** È una "interfaccia" che le classi possono implementare per dire al sistema: "quando uno dei miei dati cambia, avvisa chi è interessato". Serve per aggiornare automaticamente l'interfaccia grafica quando cambia un valore nel codice. Lo spieghiamo meglio quando arriva il momento.

**MVVM (Model-View-ViewModel).** Un pattern architetturale per app con interfaccia grafica. Diviso in tre strati. Noi NON lo usiamo (vedi 2.4).

### 1.2 Da cosa siamo partiti: il quiz console preesistente

Prima di questo progetto esisteva già un'applicazione di quiz **console** scritta in C#. "Console" significa: nessuna finestra grafica, nessun bottone, solo testo nel terminale. Tu lanci `wsa.quiz.cli.exe`, lui ti scrive "Premi A, B, C o D" e tu rispondi col tasto. Funziona benissimo, è veloce, e Luca (l'autore) la usa davvero per studiare.

Questa applicazione console contiene tutta la logica del quiz: caricare le domande dai file, mescolare le risposte, gestire la modalità "rotazione" (le sbagliate tornano in coda), calcolare il punteggio, salvare la cronologia. **Tutta questa logica è preziosa**: funziona, è collaudata, non ha senso riscriverla.

### 1.3 L'idea: aggiungere un'interfaccia grafica

L'obiettivo del progetto è **aggiungere una versione con interfaccia grafica** (con finestre, bottoni, click) accanto a quella console. Punti chiave del progetto:

1. **Le due versioni devono condividere la stessa logica**. Se cambio l'algoritmo del punteggio, non voglio doverlo modificare in due posti.
2. **Devono condividere gli stessi dati**. Se rispondo a un quiz dalla console, la sessione finisce nella stessa cronologia che vede l'app grafica, e viceversa.
3. **Lungo termine: anche mobile** (Android, iOS). Quindi va scelto un framework grafico che supporti Windows, Mac, Linux, Android, iOS.

Per soddisfare 1 e 2 abbiamo separato il **Core** (la logica) dai due **front-end** (console e GUI). È la prima vera decisione architetturale del progetto, e da lì discende tutto il resto.

---

## Parte II — Lo stack tecnico

### 2.1 Perché C# e .NET 8

C# è il linguaggio del quiz console esistente. Riscriverlo in un altro linguaggio (Python, Java...) sarebbe stato un costo enorme senza un vero motivo. Quindi: **manteniamo C#**.

.NET 8 è la versione corrente della piattaforma Microsoft per C#. È **multipiattaforma** (Windows, macOS, Linux) e **gratuita**. Si scarica da [dotnet.microsoft.com](https://dotnet.microsoft.com/). Una volta installato hai:

- Il **compilatore** C# (chiamato implicitamente dai comandi `dotnet`).
- Le **librerie standard** (`System.*`).
- Il **CLI** `dotnet`: il programma da terminale con cui crei progetti (`dotnet new`), li compili (`dotnet build`), li esegui (`dotnet run`).

Se non hai mai installato un SDK: scarica l'installer per il tuo sistema operativo, click click click, fine. Poi apri un terminale nuovo e digita `dotnet --version`. Se ti risponde `8.0.qualcosa`, è installato.

### 2.2 Console contro interfaccia grafica

Lo stesso software in versione console e in versione grafica funziona molto diversamente "sotto il cofano".

**Console**: il programma è un grande loop. "Stampa la domanda. Aspetta input. Quando l'utente digita qualcosa, decidi cosa fare. Stampa il feedback. Aspetta Invio. Avanti." Il programma "blocca" la sua esecuzione in attesa di input. È un modello **sincrono**.

**Grafica**: il programma non aspetta nessuno. Disegna la finestra, poi sta lì. Quando l'utente clicca un bottone, il sistema gli manda un **evento** ("ehi, hanno cliccato il bottone B"), e il programma reagisce. Fra un evento e l'altro, il programma è "fermo ma vivo" — la finestra resta interattiva, lo si può ridimensionare, eccetera. È un modello **event-driven**.

Tradurre il loop console in un'app grafica significa **smontare il loop** in tanti pezzi, ognuno dei quali reagisce a un evento. Lo abbiamo fatto creando una classe `SessioneQuiz` che è una "state machine": ha uno **stato** corrente (sto mostrando la domanda? sto mostrando il feedback?) e dei metodi che lo fanno cambiare quando arriva un evento. Esempi:

- `Avvia()` — porta la prima domanda a video.
- `RispondiA(indice)` — l'utente ha cliccato. Passa allo stato "feedback".
- `Avanza()` — l'utente ha cliccato "Prossima". Passa alla domanda successiva, o conclude.

Questo è uno dei concetti centrali del progetto. Ne riparliamo allo Step 2 nella Parte IV.

### 2.3 Avalonia: cos'è e perché l'abbiamo scelta

**Avalonia** è una libreria che ti permette di scrivere un'interfaccia grafica in C# che gira su Windows, macOS, Linux **e** (dalla versione 11+) anche Android e iOS. Si scarica come dipendenza del progetto (cioè la dichiari nel `.csproj` e si installa da sé).

Alternative che abbiamo valutato:

- **WPF (Windows Presentation Foundation)**: storico, ottimo, ma solo Windows. Bocciato perché vogliamo macOS.
- **MAUI**: la soluzione "ufficiale" Microsoft, multipiattaforma. Sulla carta sembra ovvio. In pratica, al momento delle scelte (fine 2025) MAUI è ancora con bordi spigolosi: build complicati, debugging zoppicante su mobile, comunità più piccola.
- **Avalonia**: comunità attiva, API molto vicine a WPF (quindi se conosci una, l'altra si impara in fretta), supporto desktop ottimo e mobile decente. La versione 12 (uscita ad aprile 2026) ha migliorato molto il supporto mobile.

Scelta: **Avalonia 12.0.2**.

In Avalonia, le finestre si descrivono in un linguaggio chiamato **XAML** (che è un dialetto di XML). Esempio molto piccolo:

```xml
<Window>
    <StackPanel>
        <TextBlock Text="Ciao!"/>
        <Button Content="Cliccami"/>
    </StackPanel>
</Window>
```

Questo XAML disegna una finestra con un testo e un bottone. Il "comportamento" del bottone (cosa succede al click) si scrive in C# in un file separato chiamato **code-behind**. Per ogni file `.axaml` (la versione Avalonia di XAML) c'è un file `.axaml.cs` accanto, con lo stesso nome — quello è il code-behind.

Inoltre Avalonia ha:

- **Stili** (`<Style>`): regole CSS-like che dicono "i bottoni con la classe X hanno sfondo verde". Si definiscono una volta e si applicano a tutti i bottoni con quella classe.
- **Binding**: meccanismo per dire "il testo di questo TextBlock viene da questa proprietà del codice C#, e si aggiorna automaticamente quando la proprietà cambia". Funziona se la classe C# implementa `INotifyPropertyChanged`.
- **Tema Fluent**: il look-and-feel di Windows 11. Avalonia lo include di default.

### 2.4 Niente MVVM: cosa significa

In molte app con interfaccia grafica si usa il pattern **MVVM (Model-View-ViewModel)**, che divide il codice in tre strati:

1. **Model** — i dati puri (la classe `Domanda`, per esempio).
2. **View** — il file XAML, cioè cosa si vede.
3. **ViewModel** — una classe "intermedia" che traduce il Model in cose comode da mostrare nella View, e gestisce gli eventi.

Il vantaggio: la View non sa niente del Model, e si possono fare test del ViewModel senza creare finestre. Lo svantaggio: scrivi più classi, più boilerplate, più indirezione. Per progetti grandi vale la pena; per progetti piccoli è un costo che non ti ripaga.

**Decisione presa**: NON usiamo MVVM. Usiamo "**code-behind + INPC**". Significa: il file `.axaml` ha accanto un `.axaml.cs` che è anche il **DataContext** del file XAML (cioè la View si "lega" direttamente al code-behind). Le proprietà osservabili (quelle a cui la View è agganciata via binding) implementano `INotifyPropertyChanged`. Quando cambiano, la View si aggiorna automaticamente.

Per non ripetere il codice di `INPC` in ogni classe, abbiamo una classe base **`ObservableObject`** in `wsa.quiz.app/State/ObservableObject.cs` con i metodi `SetField` e `RaisePropertyChanged`. Tutte le classi che hanno proprietà osservabili ereditano da `ObservableObject`. Esempio semplificato:

```csharp
public class SessioneQuiz : ObservableObject
{
    private string _domandaTesto = "";
    public string DomandaTesto
    {
        get => _domandaTesto;
        set => SetField(ref _domandaTesto, value);
    }
}
```

Il setter chiama `SetField`, che fa due cose: assegna il valore E avvisa eventuali "ascoltatori" (la View) che il valore è cambiato. La View, agganciata via `{Binding DomandaTesto}`, si aggiorna da sola.

Tutto questo è importantissimo perché è il "motore" che fa apparire le domande e cambiare gli stati nella finestra senza che noi scriviamo codice esplicito di "aggiorna la View adesso".

### 2.5 JSON come "database" dei contenuti

I contenuti del quiz (materie, domande, cronologia) **non** sono in un vero database. Sono in file JSON.

**Perché**: un database vero (SQLite, Postgres...) richiede installazione, gestione di schema e migrazioni, e in più qui i dati sono pochissimi (qualche centinaio di domande totali, una cronologia di qualche decina di sessioni). Per questa scala, file JSON sono comodi: editabili a mano, leggibili da un essere umano, facili da generare con un'IA, facili da committare in git.

I file di contenuto stanno nella root del progetto, vengono **copiati negli output `bin/`** a ogni build (è il `.csproj` che lo fa, con una direttiva `<None Include="..\materie.json">`), e l'eseguibile li legge da lì.

I file di stato dell'utente (cronologia delle sessioni giocate, sessioni in pausa) NON stanno nella root del progetto, perché altrimenti seguirebbero il codice del progetto e si perderebbero quando lo aggiorni. Stanno in una **cartella utente** dedicata, diversa per sistema operativo:

- Windows: `%APPDATA%\WsaQuiz`
- macOS: `~/Library/Application Support/WsaQuiz`
- Linux: `~/.config/WsaQuiz`

Questa è una convenzione standard. La cartella si crea automaticamente al primo avvio.

---

## Parte III — La struttura del progetto

### 3.1 Tre progetti, una soluzione

Nella radice del repository c'è un file `Wsa.Quiz.sln`. Quel `.sln` è una **"soluzione"** Visual Studio (per chi viene da Visual Studio): un file di configurazione che dice "questi progetti C# vanno gestiti insieme".

Dentro la soluzione ci sono **tre** progetti, ognuno in una sua cartella:

```
wsa_quiz_step1/
├── Wsa.Quiz.sln               ← la "soluzione"
├── materie.json               ← dati: elenco materie
├── domande/                   ← dati: una cartella per materia, dentro file .json
│   ├── cpp/
│   ├── cs/
│   ├── frontend/
│   ├── infra/
│   └── sql/
├── wsa.quiz.core/             ← Progetto 1: libreria con la logica condivisa
├── wsa.quiz.cli/              ← Progetto 2: app console (usa Core)
└── wsa.quiz.app/              ← Progetto 3: app grafica (usa Core)
```

Ogni progetto ha il suo file `.csproj` (è il file di configurazione del progetto C#: dice il tipo di progetto, le dipendenze, il target framework). E ogni progetto ha la sua cartella `bin/` (dove finiscono gli output del build) e `obj/` (cartella di lavoro intermedia del compilatore — non si tocca a mano).

### 3.2 Il Core (Wsa.Quiz.Core)

**Tipo**: libreria (`<OutputType>` non viene specificato → di default è `Library`). Significa: questo progetto non si esegue da solo. Compilando produce un file `Wsa.Quiz.Core.dll` che **viene importato** dagli altri due progetti.

**Contenuto**:

```
wsa.quiz.core/
├── Wsa.Quiz.Core.csproj
├── Models/
│   ├── Domanda.cs
│   ├── DomandaPreparata.cs       ← Domanda con risposte già mescolate
│   ├── Materia.cs
│   ├── OpzioniQuiz.cs            ← config di una sessione (rotazione, cronometro, ecc.)
│   ├── RisultatoQuiz.cs          ← l'esito di una sessione conclusa
│   ├── DettaglioRisposta.cs      ← singola risposta dentro un Risultato
│   └── SessionePausa.cs          ← snapshot serializzabile di sessione interrotta
└── Services/
    ├── QuizService.cs            ← algoritmi: shuffle, punteggio, preparazione
    └── StorageService.cs         ← lettura/scrittura JSON
```

I **Modelli** sono classi-dati semplici: contengono proprietà, niente logica. La classe `Domanda`, per esempio, ha `TestoDomanda` (stringa), `Risposte` (lista di stringhe), `IndiceRispostaCorretta` (int), eccetera.

I **Servizi** sono classi con logica vera. `QuizService` ha metodi statici come `PreparaDomanda(Domanda)` che mescola le risposte e ricalcola l'indice corretto, oppure `CalcolaPunteggio(RisultatoQuiz)` che applica la formula del punteggio. `StorageService` legge e scrive i JSON di cronologia e pause.

Questo Core è ciò che la versione console usa già da prima del progetto attuale. **Non l'abbiamo riscritto**: l'abbiamo solo "estratto" dalla console nello Step 1 (vedi Parte IV).

### 3.3 La console (Wsa.Quiz.Cli)

**Tipo**: applicazione console (`<OutputType>Exe`).

```
wsa.quiz.cli/
├── Wsa.Quiz.Cli.csproj
├── Program.cs                ← punto di ingresso, contiene il main loop
└── Services/
    └── ConsoleUI.cs          ← funzioni per disegnare a video
```

Il file `Program.cs` ha il **metodo statico `Main`**, che è quello che il sistema operativo chiama quando lanci l'eseguibile. Dentro `Main` c'è il loop: presenta il menu, l'utente sceglie la materia, parte una sessione, eccetera. Il loop usa il `QuizService` del Core per la logica e lo `StorageService` per leggere/scrivere.

**Curiosità**: il progetto NON si chiama `Wsa.Quiz.Console`. Si chiama `Wsa.Quiz.Cli`. Il motivo è una trappola (la numero 1 nella Parte V): se chiami il namespace `Wsa.Quiz.Console`, il compilatore C# si confonde tra il tuo namespace e il namespace di sistema `System.Console`, e tutte le chiamate `Console.WriteLine` smettono di compilare. È una di quelle cose che impari solo sbattendoci la testa.

### 3.4 L'app grafica (Wsa.Quiz.App)

**Tipo**: applicazione "Windows executable" (`<OutputType>WinExe`). Nonostante il nome, questo flag è quello giusto **anche** per Mac e Linux per app desktop Avalonia.

```
wsa.quiz.app/
├── Wsa.Quiz.App.csproj
├── Program.cs                       ← bootstrap di Avalonia
├── App.axaml + App.axaml.cs         ← stili globali, configurazione tema
├── app.manifest
├── MainWindow.axaml + MainWindow.axaml.cs   ← finestra principale con tre tab
├── State/
│   ├── ObservableObject.cs          ← base INPC
│   ├── MateriaSelezionabile.cs      ← wrapper per la lista materie nella Home
│   ├── CategoriaSelezionabile.cs    ← idem categorie
│   ├── RispostaItem.cs              ← singola riga risposta nel Quiz
│   ├── SessioneQuiz.cs              ← state machine principale
│   ├── RisultatoCronologiaItem.cs   ← riga della tabella Cronologia
│   ├── SessioneSospesaItem.cs       ← riga della tabella Sospesi
│   └── DettaglioRispostaItem.cs     ← riga del dettaglio cronologia
└── Views/
    ├── HomeView.axaml(.cs)
    ├── QuizView.axaml(.cs)
    ├── RiepilogoView.axaml(.cs)
    ├── CronologiaView.axaml(.cs)
    ├── CronologiaDettaglioView.axaml(.cs)
    └── SospesiView.axaml(.cs)
```

Il `Program.cs` di questa app è molto piccolo: si limita a "avviare" Avalonia. Il vero punto di ingresso visivo è `MainWindow.axaml`, che descrive la finestra principale. Dentro la `MainWindow` c'è un `TabControl` con tre tab: **Home**, **Cronologia**, **Sospesi**. Ogni tab contiene una `UserControl` (una "view") con il suo contenuto.

La cartella `State/` contiene le classi C# che hanno il vero "stato" dell'applicazione mentre gira (la sessione corrente, le materie selezionate, eccetera). Sono tutte classi che implementano `INotifyPropertyChanged` ereditando da `ObservableObject`.

La cartella `Views/` contiene le coppie `.axaml` + `.axaml.cs`. Ogni `UserControl` è un pezzo di interfaccia.

### 3.5 I dati

I dati statici (materie e domande) vivono nella root:

```
materie.json                  ← elenco materie con id, nome, cartella
domande/
├── cs/
│   ├── csharp_domande.json
│   ├── csharp_domande_2.json
│   └── csharp_domande_3.json
├── cpp/
│   └── ...
└── ...
```

Le materie sono dichiarate in `materie.json` (vedi `README.md` per lo schema). Le domande sono in file `.json` dentro la cartella di ciascuna materia. Un caricatore le legge tutte all'avvio.

I dati dell'utente (le sessioni giocate, le pause) NON stanno nella root. Stanno nella cartella utente (`%APPDATA%\WsaQuiz` su Windows, eccetera). Questo evita che si mescolino con il codice e che vengano cancellati quando aggiorni l'app.

---

## Parte IV — La storia in 7 step

Il progetto è stato sviluppato in 7 "step" incrementali. Ogni step ha avuto:

1. **Una spec di design**: cosa voglio fare, perché, decisioni UX, modello dati, file toccati. Discussa prima di scrivere codice.
2. **Un plan implementativo**: tradotto la spec in una sequenza di task bite-sized ognuno con i suoi step di implementazione e verifica.
3. **L'esecuzione**: scrivere il codice seguendo il plan, build dopo ogni task, commit con messaggio coerente.
4. **Un test manuale finale**: avviare l'app, provare gli scenari della spec, aggiustare quello che si rivela strano. La spec è un'ipotesi; il test pratico è il giudice.
5. **Un aggiornamento del documento di handoff** (`WSA_QUIZ_HANDOFF.md`): scrivere cosa è stato fatto e quali decisioni sono state prese, in modo che la prossima sessione di lavoro possa riprendere senza dover ricostruire il contesto.

Le spec e i plan stanno in `docs/superpowers/specs/` e `docs/superpowers/plans/`, con nomi datati.

### Step 1 — Fondazione

**Obiettivo**: separare la logica dal front-end console esistente, in modo da poterla riusare nell'app grafica.

**Cosa è stato fatto**:

- Estratti i modelli e i servizi dalla vecchia struttura del console, e messi in un nuovo progetto C# di tipo libreria (**`Wsa.Quiz.Core`**).
- Riorganizzate le cartelle: prima c'era una struttura piatta, ora ci sono tre cartelle (`wsa.quiz.core/`, `wsa.quiz.cli/`, `wsa.quiz.app/`) sotto un'unica soluzione `Wsa.Quiz.sln`.
- Rinominati i namespace: prima erano `QuizFdP.*` (vecchio nome del progetto), ora sono `Wsa.Quiz.*`.
- Creato lo scheletro vuoto del progetto Avalonia (`wsa.quiz.app`), con verifica che all'avvio carichi i dati dal Core. Una finestra bianca, ma il caricamento funziona.

**Concetti nuovi introdotti**:

- **Libreria** (output `.dll`) vs **eseguibile** (output `.exe`). Il Core è una libreria; gli altri due la usano.
- **Reference fra progetti**: nel `.csproj` di `Wsa.Quiz.Cli` e `Wsa.Quiz.App` c'è una riga `<ProjectReference Include="..\wsa.quiz.core\Wsa.Quiz.Core.csproj"/>`. Significa "io dipendo da quel progetto, importa il suo `.dll`".

**Decisione strategica**: non rifattorizzare il Core. Era già funzionante e collaudato dalla console. Toccarlo qui significa rischiare di rompere quello che gira, senza guadagnare nulla per lo Step 1.

**Trappola scoperta**: la già citata `System.Console` (vedi Parte V, punto 1) — il nome del progetto console NON può essere `Wsa.Quiz.Console`, va chiamato `Wsa.Quiz.Cli`.

### Step 2 — Porting in Avalonia

**Obiettivo**: portare l'esperienza di gioco del quiz dalla console alla finestra grafica. L'utente deve poter selezionare materie/categorie, fare un quiz, vedere il riepilogo finale. Tutto via mouse.

**Cosa è stato fatto**:

- Creata la **`HomeView`**: una `UserControl` con checkbox per le materie, checkbox per le categorie (filtrate in base alle materie selezionate), opzioni (rotazione, cronometro, randomizza ordine, limita a N domande), riepilogo testuale di quello che sta per partire, bottone "Avvia".
- Creata la **`QuizView`**: l'utente vede la domanda, 4 bottoni A/B/C/D, e — dopo aver risposto — un pannello di feedback (Corretto/Sbagliato + spiegazione) con un bottone "Prossima domanda". Una barra di progresso in fondo.
- Creata la **`RiepilogoView`**: alla fine del quiz, percentuale di corrette, durata, lista delle sbagliate, bottone "Torna alla home".
- Creata la **`SessioneQuiz`**: la state machine event-driven che sostituisce il loop console. Espone proprietà osservabili che la `QuizView` ascolta via binding.

**Concetti nuovi introdotti**:

- **State machine event-driven** (vedi 2.2): la sessione è un oggetto con stato e metodi. Niente loop bloccante.
- **`ObservableObject` + `INotifyPropertyChanged`** (vedi 2.4): le proprietà notificano i loro cambiamenti, la View si aggiorna sola.
- **`DataContext`**: ogni `UserControl` ha un `DataContext`, di solito la sua istanza C# (`this.DataContext = this;` nel costruttore della UC, o `this.DataContext = sessione;` per la `QuizView` che riceve una `SessioneQuiz`). Il `DataContext` è "l'oggetto a cui i binding del file XAML guardano".
- **Binding**: in XAML scrivi `<TextBlock Text="{Binding DomandaTesto}"/>`. Il sistema cerca una proprietà `DomandaTesto` sull'oggetto puntato dal `DataContext` corrente, prende il valore e lo mette nel TextBlock. Quando la proprietà cambia (e notifica via INPC), il TextBlock si aggiorna.
- **`ObservableCollection<T>`**: una lista che notifica quando si aggiungono/rimuovono elementi. La `QuizView` ha un `ObservableCollection<RispostaItem>` per i 4 bottoni risposta: al cambio domanda, `Risposte.Clear()` + `.Add(...)` x4, e la View ridisegna i bottoni.

**Decisione strategica**: il `DataContext` si setta **una sola volta nel costruttore** della UserControl. Mai resettarlo a `null` o cambiare. Sembra un dettaglio, ma è importante: il riassegnare `DataContext` rompe la selezione corrente delle liste, gli scroll position, eccetera. È una trappola classica delle vecchie app WPF.

**Trappole scoperte**: vedi Parte V, punti 2-5.

### Step 3 — Cronologia e Sospesi

**Obiettivo**: rendere visibili nella GUI le due collezioni di dati utente: la cronologia delle sessioni passate e le sessioni in pausa. La console le sa già gestire, ma in GUI non c'erano.

**Cosa è stato fatto**:

- Aggiunta la tab **Cronologia** alla `MainWindow`. La `CronologiaView` mostra una tabella con: data, modalità, materia, % corrette, durata. Ogni riga ha una **striscia colorata a sinistra**: verde se ≥80%, ambra se 50-79%, rosso se <50%. Doppio click apre il dettaglio.
- Aggiunta la `CronologiaDettaglioView`: per ogni domanda della sessione, mostra il testo, la risposta data, la risposta corretta, la spiegazione. Bordo verde sulle corrette, rosso sulle sbagliate.
- Aggiunta la tab **Sospesi** alla `MainWindow`. La `SospesiView` elenca le sessioni in pausa (per ora salvate solo dal console; la pausa GUI arriverà allo Step 4). Ogni riga ha bottoni "Riprendi" (disabilitato per ora) e "Elimina".
- Pattern "**conferma inline**" per l'elimina: primo click sull'Elimina, la riga cambia e mostra "Sicuro? [Sì, elimina] [Annulla]"; secondo click su Sì conferma. Niente dialog modali. Sono le piccole UX detail che fanno la differenza.

**Concetti nuovi introdotti**:

- **Stili globali in `App.axaml`**. Quando vuoi che un certo tipo di bottone abbia lo stesso aspetto in molti posti, lo definisci una volta in `App.axaml` e lo richiami con `Classes="..."`. Esempio: `Button Classes="accent"` per i bottoni primari azzurri, `Button Classes="danger"` per i bottoni rossi.
- **Wrapper observable** (`RisultatoCronologiaItem`, `SessioneSospesaItem`, ...): non passi alla View direttamente l'oggetto del Core (`RisultatoQuiz`); lo "avvolgi" in una classe della cartella `State/` che aggiunge proprietà calcolate (es. colore della striscia) e mantiene la logica di presentazione fuori dalla XAML.

**Trappola scoperta**: vedi Parte V, punto 6 (bottone disabled "scomparso") e punto 7 (`DynamicResource` di Color nei Setter di Style). Risolte con stili globali espliciti.

### Step 4 — Tastiera e Pausa

**Obiettivo**: rendere il quiz utilizzabile senza staccare le mani dalla tastiera. Aggiungere la funzione "pausa" anche nella GUI (non solo nel console).

**Cosa è stato fatto**:

- **Tastiera nella `QuizView`**: `A/B/C/D` selezionano la risposta, `Invio` avanza al post-feedback, `ESC` apre il menu pausa.
- **Menu pausa modale**: una piccola finestra (Window 420×180, al centro della finestra padre, niente resize, niente icona nella taskbar) con tre opzioni — Annulla / Salva e esci / Riprendi (`Classes="accent"`). ESC dentro la modale = Annulla.
- **Salvataggio pausa**: il metodo `EsportaPausa()` su `SessioneQuiz` produce uno snapshot serializzabile dello stato corrente (`SessionePausa`), che viene scritto sul disco da `StorageService`. Funziona sia se l'utente preme pausa mentre sta rispondendo, sia se preme pausa dopo aver risposto ma prima di avanzare.
- **Ripresa pausa**: la `SospesiView` ha ora il bottone "Riprendi" attivo. Cliccato, ricostruisce una `SessioneQuiz` a partire dalla pausa (factory statica `RiprendiDa(SessionePausa, mappaPerId)`) e naviga al `QuizView`. La pausa originale viene cancellata a fine sessione (stesso comportamento del console).

**Concetti nuovi introdotti**:

- **Override `OnKeyDown`**: una `UserControl` può intercettare i tasti se è "Focusable" e ha il focus. Nel costruttore della `QuizView` si imposta `Focusable=true`, e in `OnAttachedToVisualTree` si chiama `this.Focus()` per prendere il focus appena la view diventa visibile.
- **`e.Handled = true`**: nel gestore degli eventi tastiera, quando hai gestito un tasto devi marcarlo come "handled", altrimenti il sistema lo passa al controllo successivo (es. un bottone con focus si auto-cliccherebbe).
- **Dialog modale in Avalonia**: si crea una `Window`, si configura, e si chiama `ShowDialog(owner)`. Il `await` blocca l'esecuzione finché l'utente non la chiude. Il risultato si comunica via una variabile catturata (`string azione = "annulla";` che viene riassegnata nei `Click` dei bottoni).

**Decisione strategica**: il salvataggio di pausa deve essere **consistente in due stati possibili** (in attesa di risposta, o in feedback post-risposta). Sembra un dettaglio, ma sbagliarlo significa che riprendendo una pausa salvata "a metà feedback" la sessione riparte male. L'implementazione ne tiene conto: in `EsportaPausa`, il discriminante è la proprietà `RispostaInviata` (true = sono in feedback, false = sto aspettando la risposta).

**Trappola scoperta**: vedi Parte V, punto 8 (`Key.Enter == Key.Return`).

### Step 5 — Eliminazione cronologia

**Obiettivo**: permettere all'utente di cancellare singole sessioni dalla cronologia, oppure svuotarla tutta.

**Cosa è stato fatto**:

- Aggiunta una proprietà `Id` (stringa `Guid`) a `RisultatoQuiz`, generata al salvataggio. I record vecchi (senza `Id`) ricevono un `Guid` alla prima lettura della cronologia, in una **migrazione lazy una-tantum**: leggi il file, se trovi record senza `Id` ne aggiungi uno e riscrivi il file; al boot successivo è già tutto a posto e la migrazione non fa nulla (è **idempotente**).
- Due nuovi metodi su `StorageService`: `EliminaRisultato(string id)` e `SvuotaCronologia()`.
- Bottone "**Elimina**" inline su ogni riga della Cronologia con conferma in-place (riusa il pattern dei Sospesi: al primo click si trasforma in "Sicuro? [Sì, elimina] [Annulla]"; ogni nuova conferma azzera quelle precedenti, una alla volta).
- Bottone "**Svuota cronologia**" nell'header della tab, con dialog modale di conferma (riusa il pattern di `ChiediConfermaAbbandono` — Window 420×180, ESC=Annulla, bottone "Sì, cancella tutto" `Classes="danger"`, disabilitato quando la lista è vuota).
- Bottone "**Elimina questa partita**" anche nel `CronologiaDettaglioView`, con conferma inline. Quando cliccato, la `CronologiaView` riceve l'evento, elimina, chiude il dettaglio e ricarica la lista.

**Concetti nuovi introdotti**:

- **Migrazione lazy idempotente**: invece di scrivere uno script di migrazione che gira una volta sola, fai sì che il codice di lettura "aggiusti" i dati man mano che li trova. Se i dati sono già a posto, l'aggiustamento non fa niente. È pratico per piccole evoluzioni di schema senza pianificazione di rilascio.
- **`Guid`** (Globally Unique Identifier): una stringa di 32 caratteri esadecimali che ha probabilità praticamente nulla di collidere con un altro `Guid` mai generato. È quello che si usa di solito per Id "facili e sicuri".
- **Eventi custom** (`event EventHandler? EliminazioneRichiesta`): un modo per far comunicare una `UserControl` figlia con quella padre senza che la figlia sappia chi è il padre. La figlia "solleva" l'evento, la padre lo "ascolta".

### Step 6 — Rifiniture UX

**Obiettivo**: due aggiustamenti emersi dal test dello Step 5.

**Cosa è stato fatto**:

1. **Home con barra "Avvia" sticky**. La spec iniziale prevedeva la barra in basso. Durante il test pratico, Luca ha trovato più ergonomica la barra **in alto**, e ha proposto di togliere il vecchio titolo + sottotitolo ("Configura un nuovo quiz" + descrizione lunga) perché diventava ridondante. Lo schema finale è uno scroll globale della pagina (Materie, Categorie, Opzioni), con solo Categorie che ha uno scroll interno (`MaxHeight=240`) perché può crescere molto. Materie no, ha 5 voci.
2. **Pausa unificata**. Prima dello Step 6, ESC apriva un menu pausa (Annulla / Salva e esci / Riprendi) e il bottone "Abbandona" apriva un dialog di conferma separato (Continua / Abbandona). Sono stati uniti in una sola modale, raggiungibile sia da ESC sia dal bottone in alto, con tre opzioni: Annulla / Abbandona / Salva e esci. Il bottone in alto è stato rinominato "**Pausa**".

**Fix laterali emersi nel test**:

- Il pannello di feedback usava `Foreground="{DynamicResource SystemBaseHighColor}"` per il testo. In dark mode questo brush diventa quasi-bianco, ma il pannello ha background hardcoded chiaro (`#E5F4EC` per il verde corretto, `#F9E7E6` per il rosso sbagliato). Risultato: testo bianco su sfondo chiaro = illeggibile. Fix: `Foreground="#1F1F1F"` esplicito.
- Il bottone "Prossima domanda" è stato uniformato agli altri bottoni primari usando `Classes="accent"` invece di `Background` e `Foreground` hardcoded.

**Lezione importante**: una spec di design è un'ipotesi. Spesso è giusta, a volte è sbagliata, e quasi sempre viene rifinita dopo il primo test pratico. Mai trattare la spec come legge: il giudice finale è il test.

**Trappola scoperta**: vedi Parte V, punto 9 (`SystemBaseHighColor` illeggibile in dark mode con background fisso chiaro) e punti 10-11 (sticky bar con `BoxShadow` e doppio scroll innestato).

### Step 7 — Navigazione tra domande

**Obiettivo**: permettere all'utente di rivedere le risposte già date durante una sessione in corso, e aggiungere una scorciatoia tastiera alternativa ad `A/B/C/D` per chi preferisce confermare con `Invio` dopo aver evidenziato una scelta con le frecce.

**Cosa è stato fatto**:

- **Modello dati**: aggiunti 3 campi a `DettaglioRisposta` (la classe che descrive una singola risposta dentro un `RisultatoQuiz`): `RisposteShufflate` (la lista delle 4 risposte nell'ordine mostrato), `IndiceCorrettoShufflato` (l'indice 0-3 della risposta corretta), `IndiceDataShufflato` (l'indice 0-3 della risposta data dall'utente). Servono a ricostruire i 4 bottoni colorati anche dopo una pausa/ripresa. Sono campi **additivi**: i record JSON vecchi (di cronologia precedente lo Step 7) caricano con default vuoto/-1, e i record nuovi popolano i campi a ogni `RispondiA`.
- **Stato in `SessioneQuiz`**: aggiunte due variabili nuove. `_viewIndex` (nullable: `null` = sto guardando la corrente, altrimenti l'indice della passata che sto rivedendo dentro `Risultato.Dettagli`). `_indiceHighlight` (nullable: l'indice 0-3 della risposta evidenziata con le frecce, o `null` se nessuna).
- **Backup/restore stato live**: quando entro in view-mode, salvo lo stato corrente in un `record _StatoLive` privato; quando esco, lo ripristino. Questo permette di "riusare" le proprietà observable correnti per mostrare i dati della passata (Opzione A della spec), senza dover duplicare il blocco XAML del quiz.
- **`RispostaItem` estesa**: aggiunte due proprietà observable, `IsEnabled` (per disabilitare i 4 bottoni in view-mode indipendentemente dal loro stato colorato) e `IsHighlighted` (per il bordo evidenziato dalle frecce). E una proprietà calcolata `PuoCliccare = IsEnabled && IsNeutra` che ora pilota il `Button.IsEnabled` al posto della vecchia `IsNeutra`.
- **Banner view-mode**: una fascia gialla crema in alto sopra l'header, visibile solo quando `InViewMode=true`, con il testo "Domanda X di Y (vista)" e un bottone "Torna alla corrente".
- **Tastiera estesa** nel `OnKeyDown` della `QuizView`: `↑/↓` chiamano `HighlightSu`/`HighlightGiu`; `Invio` controlla prima se c'è un highlight pendente (in tal caso conferma quella risposta), altrimenti se siamo in feedback avanza; `←/→` scorrono le passate (`VaiAPassataPrecedente` / `VaiAPassataSuccessiva`); in view-mode i tasti `A/B/C/D`, `↑/↓` e `Invio` sono no-op.
- **Sicurezza pausa**: la `EsportaPausa` chiama `TornaACorrente()` come prima cosa, per evitare di catturare lo stato view-mode invece del live.

**Iterazione UX rispetto alla spec**: la spec proponeva di evidenziare la risposta in highlight con un bordo dell'accent color (azzurro) spesso 2 pixel. Al test pratico era troppo poco visibile. È stato sostituito con un bordo giallo `#FFD500` spesso 3 pixel. È esattamente il pattern "la spec è un'ipotesi, il test è il giudice".

**Fix UX laterale**: il bottone "Prossima domanda" (`Classes="accent"`) aveva il testo bianco. Nel feedback box (background hex chiaro) era illeggibile, soprattutto in dark mode dove anche il bottone tende a essere più chiaro. Aggiunto uno style `Button.prossima /template/ ContentPresenter` con `TextBlock.Foreground="#1F1F1F"` e una classe locale `prossima` sul bottone. Il `Foreground` diretto come attributo non vinceva perché lo style globale `.accent` lo ridefinisce nel template (trappola 12 nella Parte V).

**Concetto nuovo introdotto**:

- **Record privato come "snapshot"**: il `record _StatoLive(...)` è un piccolo tipo C# 9 che è essenzialmente un costruttore con N proprietà readonly. Pratico per fare backup/restore di uno stato composito.

---

## Parte V — Le trappole che ci hanno insegnato qualcosa

Queste sono le 12 "trappole" che abbiamo incontrato e documentato nel file di handoff. Le ripeto qui in forma discorsiva, raccontando perché sono successe e come le abbiamo evitate. Sono il vero apprendimento del progetto.

### Trappola 1 — `System.Console` shadowing

Il primo nome che abbiamo dato al progetto console era `Wsa.Quiz.Console`. Sembrava ovvio. Risultato: tutte le chiamate `Console.WriteLine(...)` smettevano di compilare con errori strani tipo "Cannot find type WriteLine in namespace Wsa.Quiz.Console". Cosa succedeva: il compilatore C# cerca i nomi a partire dal namespace corrente verso l'esterno. Trovava `Wsa.Quiz.Console` come sotto-namespace di se stesso e si fermava lì, senza arrivare a `System.Console` di sistema. Soluzione: rinominato in `Wsa.Quiz.Cli`. Lezione: **mai chiamare un namespace come una classe di sistema importante**.

### Trappola 2 — `Avalonia.Diagnostics` non esiste più

Cercavamo un pacchetto NuGet (le "dipendenze" di .NET si scaricano da nuget.org) chiamato `Avalonia.Diagnostics`. Esisteva nelle vecchie versioni. In Avalonia 12 è stato sostituito da `AvaloniaUI.DiagnosticsSupport`, e ad ogni modo è opzionale. Lezione: **prima di aggiungere un pacchetto, verifica che esista nella versione corrente del framework**. La documentazione delle vecchie versioni di Avalonia online è ancora indicizzata e ti porta fuori strada.

### Trappola 3 — `RefreshBinding` con `DataContext = null/this`

Nel primissimo tentativo di app WPF (prima di passare ad Avalonia) avevamo questo brutto trucco: per simulare le notifiche di cambiamento, ogni tanto facevamo `this.DataContext = null; this.DataContext = this;`. Sembrava funzionare. Ma cancellava la selezione delle ListView a ogni interazione, scrollava in cima, eccetera. Soluzione: implementare `INotifyPropertyChanged` correttamente e impostare il `DataContext` **una sola volta** nel costruttore. Lezione: **i workaround che sembrano funzionare a volte mascherano bug terribili**. Investire nel meccanismo corretto, anche se sembra più lavoro all'inizio.

### Trappola 4 — `OutputType=WinExe` anche su Mac e Linux

Stavamo cercando il modo "giusto" per dichiarare un'app desktop multipiattaforma. Sembrava che `WinExe` fosse Windows-specifico. È un nome storico: in pratica è il flag standard per app desktop con UI in qualunque piattaforma .NET. Lezione: **i nomi nei file di configurazione spesso vengono da decisioni di 15 anni fa e non sono più rappresentativi**.

### Trappola 5 — Compiled bindings richiedono `x:DataType` ovunque

Avalonia ha un'ottimizzazione chiamata "compiled bindings": invece di risolvere i `{Binding}` a runtime guardando il DataContext, li risolve già in fase di compilazione. È più veloce ed è il default in Avalonia 11+. Però richiede che ogni `UserControl` e ogni `DataTemplate` dichiari il tipo del DataContext con un attributo `x:DataType="state:SessioneQuiz"`. Se ti dimentichi, il build dà errori cripti tipo "could not resolve property X on type System.Object". Soluzione: aggiungere sempre `x:DataType` sui root e sui DataTemplate. Lezione: **gli errori "magici" del compilatore di solito hanno una causa precisa documentata; cercare il messaggio esatto su Stack Overflow risolve in 5 minuti**.

### Trappola 6 — Bottone accent disabled "scomparso"

Il tema Fluent applica `Background="{DynamicResource SystemAccentColorBrush}"` per i bottoni primari. Quando il bottone va in `IsEnabled=false`, il sistema gli applica un'opacità ridotta. Su sfondo chiaro, il risultato è un bottone color "azzurro pastello chiaro" che si confonde con lo sfondo, e sembra che il bottone sia sparito. Soluzione: in `App.axaml` abbiamo definito uno **style globale** `Button.accent` con i 4 stati esplicitati sul template del bottone (`Button.accent`, `Button.accent:pointerover`, `Button.accent:pressed`, `Button.accent:disabled`), con colori in hex assoluti. I bottoni primari ora usano `Classes="accent"` invece di settare `Background` a mano. Lezione: **se uno stato visivo di un componente è importante (come `:disabled` per i bottoni di azione), definiscilo esplicitamente. Non fidarti del default del tema**.

### Trappola 7 — `DynamicResource` di tipo `Color` nei Setter di Style

In XAML diretto puoi scrivere `Background="{DynamicResource SystemBaseLowColor}"` e Avalonia converte automaticamente il `Color` in `SolidColorBrush` (perché il `Background` si aspetta un Brush). Ma se la stessa cosa è dentro un `Setter` di Style, la conversione automatica è meno affidabile e può dare errori a runtime. Soluzione: nei Setter usare la versione `Brush` (`DynamicResource SystemAccentColorBrush`) o un valore esplicito in hex. Lezione: **la stessa sintassi in due posti diversi di XAML può comportarsi diversamente. Quando un Setter di Style "non funziona" controlla i tipi**.

### Trappola 8 — `Key.Enter == Key.Return`

In `Avalonia.Input.Key` i due valori `Key.Enter` e `Key.Return` sono lo stesso enum constant (entrambi = 6). Se metti due `case Key.Enter:` e `case Key.Return:` nello stesso `switch`, il compilatore dà errore CS0152 ("the switch statement contains multiple cases with the same label"). Soluzione: tenerne uno solo. Lezione: **gli enum sembrano "etichette diverse" ma possono essere alias dello stesso numero. Quando un `switch` rifiuta di compilare per "duplicate cases", controlla i valori numerici degli enum**.

### Trappola 9 — `SystemBaseHighColor` come `Foreground` su background hex chiaro = illeggibile in dark mode

I brush "system" di Avalonia (`SystemBaseHighColor`, `SystemBaseMediumColor`, eccetera) **si invertono** in base al tema attivo: in light mode sono scuri, in dark mode sono chiari. È giusto se entrambi (foreground e background) usano brush di sistema, perché si invertono insieme e il contrasto si mantiene. Ma se il background è un **hex fisso chiaro** (per esempio il pannello di feedback `#E5F4EC` verde tenue), il foreground "auto-invertibile" in dark mode diventa quasi-bianco, e il contrasto sparisce. Soluzione: quando il background è hex fisso, usare un foreground hex fisso compatibile (es. `#1F1F1F` nero quasi). Lezione: **i temi automatici funzionano solo se le coppie foreground/background sono entrambe automatiche, o entrambe fisse. Mai mischiare**.

### Trappola 10 — `BoxShadow` su sticky bar adiacente a `ScrollViewer`

Nella prima versione della Home (Step 6), avevamo provato a mettere una `BoxShadow="0 -2 8 0 #28000000"` su una barra `DockPanel.Dock="Bottom"` per dare profondità. L'ombra è di 8 pixel verso l'alto. Quei 8 pixel coprono visivamente l'ultimo pezzo del viewport del `ScrollViewer` sopra. Effetto: anche con scroll a fondo, l'ultima riga sembra "tagliata" perché c'è un alone scuro sopra. Soluzione: niente ombra, basta una `BorderThickness="1"` sul lato che separa. Lezione: **gli effetti visivi (ombre, glow, blur) "sforano" il rettangolo del controllo. In layout densi, sforare significa coprire qualcos'altro**.

### Trappola 11 — Doppio scroll innestato (pagina + pannello con `MaxHeight`)

Sempre nella prima versione della Home, c'era un `ScrollViewer` esterno per tutta la pagina + un `ScrollViewer` con `MaxHeight=240` su due pannelli interni (Materie e Categorie). Effetto: la rotella del mouse "perde" il pannello interno appena esci con il puntatore, e i pannelli con `MaxHeight` sembrano "tagliati" intenzionalmente — il che confonde l'utente. Soluzione applicata nello Step 6: un solo scroll dove possibile; se serve un cap interno, deve essere giustificato da una vera differenza di volume dati. Categorie può avere 50+ voci → cap sì; Materie ne ha 5 → cap no. Lezione: **uno scroll è una promessa "qui c'è più contenuto che non vedi"; due scroll annidati sono una promessa rotta**.

### Trappola 12 — `Foreground` diretto su `Button Classes="accent"` non vince

Scoperta nello Step 7. Provando ad assegnare `Foreground="#1F1F1F"` come attributo al bottone "Prossima domanda" (`Classes="accent"`), il testo restava bianco. Il motivo: lo style globale `Button.accent` definito in `App.axaml` (per risolvere la Trappola 6) colpisce il `ContentPresenter#PART_ContentPresenter` del template del bottone, e quel setter ha **priorità** sul `Foreground` impostato come attributo. Soluzione: aggiungere una classe locale (`Classes="accent prossima"`) e definire uno style `Button.prossima /template/ ContentPresenter` con `TextBlock.Foreground="#1F1F1F"`. Lezione: **gli style XAML con selector `/template/ ContentPresenter` hanno priorità rispetto agli attributi del controllo. Quando un attributo non "vince", il problema è quasi sempre uno style con selector più specifico**.

---

## Parte VI — Come si rifà da zero

Se volessi rifare questo progetto da zero su un computer nuovo, ecco il percorso.

### Prerequisiti

1. **.NET 8 SDK**. Scaricalo da [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) per il tuo sistema operativo. Installa, riapri il terminale, e prova `dotnet --version`. Deve rispondere `8.0.qualcosa`.
2. **Git**. Su Windows scarica da [git-scm.com](https://git-scm.com/). Su Mac di solito è già presente (sennò `xcode-select --install`). Su Linux è `apt install git` o equivalente. Verifica con `git --version`.
3. **Un editor / IDE**. Le opzioni:
   - **Visual Studio** (Windows/Mac): pesante ma completo. Edizione Community gratis.
   - **JetBrains Rider** (Win/Mac/Linux): a pagamento ma ottimo. Versione di prova gratuita.
   - **VS Code** (Win/Mac/Linux): leggero, gratis. Installa l'estensione "C# Dev Kit" e "Avalonia for VS Code".

### Step pratico 1 — Clonare il repository

```bash
git clone <url-del-repo> wsa-quiz
cd wsa-quiz
```

Il primo comando scarica una copia locale del repository nella cartella `wsa-quiz`. Il secondo entra dentro.

### Step pratico 2 — Verificare la struttura

Dovresti vedere:

```
Wsa.Quiz.sln
materie.json
domande/
wsa.quiz.core/
wsa.quiz.cli/
wsa.quiz.app/
README.md
WSA_QUIZ_HANDOFF.md
MANUALE.md (questo file)
docs/
```

Se manca qualcosa, è un problema del clone, non tuo. Riprova con un altro `git clone`.

### Step pratico 3 — Aprire la solution

- **Visual Studio**: doppio click su `Wsa.Quiz.sln`. Te lo apre nell'Explorer di solution.
- **Rider**: File → Open → seleziona `Wsa.Quiz.sln`.
- **VS Code**: `code .` dal terminale dentro la cartella. Poi `Ctrl+Shift+P` → "Open Solution" → `Wsa.Quiz.sln`.

L'IDE scaricherà le dipendenze automaticamente (Avalonia, Newtonsoft.Json, ecc.) — la prima volta ci mette qualche minuto, le successive è istantaneo.

### Step pratico 4 — Build e Run (versione console)

Da terminale:

```bash
dotnet run --project wsa.quiz.cli
```

`dotnet run` fa due cose: compila il progetto, e se la compilazione va a buon fine lancia l'eseguibile. Il flag `--project wsa.quiz.cli` dice "esegui quello, non gli altri due".

Dovresti vedere il menu testuale del quiz console. Premi un numero per scegliere una materia e prova un quiz.

### Step pratico 5 — Build e Run (versione grafica)

```bash
dotnet run --project wsa.quiz.app
```

Si apre una finestra Avalonia. Dovresti vedere la Home con la barra "Avvia quiz" in alto, materie e categorie a seguire, opzioni in fondo.

Se non si apre: leggi il messaggio di errore. La maggior parte degli errori al primo avvio sono:

- **SDK sbagliato**: hai installato .NET 7 invece di .NET 8. Aggiorna.
- **File `materie.json` o `domande/` non trovati**: probabilmente non sono stati copiati nei `bin/`. Prova `dotnet clean && dotnet build`. Verifica che il `.csproj` dell'app abbia le righe `<None Include="..\materie.json">`.
- **Errore di permessi sulla cartella utente** (`%APPDATA%\WsaQuiz` non scrivibile): raro su Windows normale, possibile in alcune configurazioni aziendali. Esegui con permessi normali.

### Step pratico 6 — Aggiungere una domanda

Apri uno qualunque dei file in `domande/cs/` (per esempio). Vedi una struttura come quella descritta nel `README.md`. Aggiungi un nuovo blocco `{ "id": ..., ... }` all'array. Salva. Lancia di nuovo `dotnet run --project wsa.quiz.app` e cerca la nuova domanda — appare nel pool della materia C# / categoria corrispondente.

### Step pratico 7 — Modificare un pezzo di codice

Apri `wsa.quiz.app/Views/QuizView.axaml`. Trova il `<Border>` del feedback (cerca `IsVisible="{Binding RispostaInviata}"`). Cambia il colore del titolo modificando il `TextBlock` del `FeedbackTitolo`: per esempio aggiungi `FontSize="20"` per ingrandirlo.

Salva, fai `dotnet run --project wsa.quiz.app`, e vedi il cambiamento. Questo è il ciclo classico: edit → build → run → osserva. Si chiama "ciclo di feedback" e più è veloce, più sei produttivo.

Bonus: **hot reload**. Avalonia supporta in modo limitato il "live reload" dei file XAML. Significa che mentre l'app è aperta, modificando un `.axaml` e salvando, l'app si ridisegna senza riavviare. Funziona in Rider e Visual Studio quasi sempre, su VS Code ogni tanto. Non funziona sui file `.cs` (per quelli devi rifare `dotnet run`).

---

## Parte VII — Come abbiamo lavorato con l'IA

Questo progetto è stato sviluppato in coppia con un assistente AI (Claude Code). Non è un caso che il lavoro sia diviso in "step" e che ogni step abbia spec + plan + execute. È un workflow chiamato "**spec-driven development with checkpoints**", supportato da un set di skill chiamate **superpowers** che Claude carica automaticamente.

### Il workflow

1. **Brainstorming** — All'inizio di ogni step, l'assistente non scrive subito codice. Fa domande chiarificatrici sull'obiettivo, su cosa è in scope e cosa no, sui trade-off. Genera idee, le presenta, e attende il feedback dell'umano. Questa fase si conclude quando il problema è ben definito.

2. **Spec di design** — Dopo il brainstorming, viene scritta una spec in `docs/superpowers/specs/`, datata. La spec contiene: obiettivo, decisioni UX prese e perché, modello dati (eventuali nuovi campi/classi), file toccati (stima), trappole previste, test manuali da fare alla fine. **La spec NON è una verità immutabile**: è un'ipotesi che verrà rifinita al test pratico.

3. **Implementation plan** — La spec viene tradotta in un plan in `docs/superpowers/plans/`, anch'esso datato. Il plan rompe l'implementazione in **task bite-sized** (ognuno con i suoi step). Ogni task ha verifica esplicita (`dotnet build`, eventualmente un test manuale puntuale) e un commit alla fine.

4. **Execution** — L'assistente esegue il plan task per task. Dopo ogni task: build, eventuale test, commit. Se qualcosa non quadra, ferma e chiede.

5. **Test manuale finale** — Quando tutti i task sono fatti, l'app si avvia e si testano gli scenari della spec. Le iterazioni UX emerse qui (es. "il bordo accent 2px non si vede, fallo giallo 3px" dello Step 7) si applicano subito, ognuna come commit separato `fix(stepN): ...`.

6. **Handoff update** — Alla fine, il file `WSA_QUIZ_HANDOFF.md` viene aggiornato con: cosa è stato fatto in questo step, decisioni prese, trappole nuove, prossimi step. È il documento "memoria" del progetto fra una sessione di lavoro e l'altra. Quando ricomincio domani, basta leggere il handoff per riprendere il filo.

### Perché funziona

L'aspetto importante è che ogni step produce **artefatti scritti** (la spec, il plan, il handoff aggiornato). Questo significa che:

- Posso interrompere a metà step senza perdere contesto: i file ricordano cosa stavo facendo.
- Posso rivedere a freddo le decisioni: la spec ha il "perché", non solo il "cosa".
- Posso onboardare qualcun altro: handoff + manuale (questo file) bastano.

L'altro aspetto è che ogni step è **piccolo**. Un'implementazione completa richiede normalmente fra 4 e 8 task, ognuno completabile in pochi minuti. Se durante un task scopro che la spec è sbagliata, mi fermo subito, aggiorno spec/plan, e riparto. Non vado avanti con la testa bassa per ore.

### Le 7 spec esistenti

- `docs/superpowers/specs/2026-05-08-step4-tastiera-pausa-design.md`
- `docs/superpowers/specs/2026-05-09-step5-eliminazione-cronologia-design.md`
- `docs/superpowers/specs/2026-05-09-step6-rifiniture-ux-design.md`
- `docs/superpowers/specs/2026-05-10-step7-navigazione-domande-design.md`
- (gli step 1, 2, 3 hanno spec descritte solo nei `README_STEP*.md` e nel handoff, precedono l'adozione completa del workflow)

---

## Parte VIII — Cosa c'è dopo

Lo stato attuale è "**Step 7 completo**". I prossimi step pianificati (nel file di handoff) sono:

- **Step 8 — Navigazione tastiera globale**: estendere le frecce e Invio anche fuori dal Quiz (dentro la modale Esc, dentro la Home, Tab fra Cronologia e Sospesi).
- **Step 9 — Grafici**: aggiungere una quarta tab "Statistiche" con bar chart e drill-down per materia/categoria. Libreria prevista: LiveCharts2.
- **Step 10 — Dark mode**: toggle Fluent chiaro/scuro, posizione del toggle ancora da decidere.
- **Step 11 — Esportazione e filtri cronologia**: export CSV/JSON, filtri sulla tabella.
- **Step 12 — Import contenuti da GUI**: caricare nuovi `.json` di domande, o creare nuove materie, senza editare i file a mano.
- **Step 13 — Preferenze utente persistite**: `settings.json` per ricordare le ultime opzioni quiz e la tab aperta.
- **Step 14 — Rifiniture distribuzione**: icona app, "About", build self-contained.

Ognuno di questi step seguirà il workflow descritto nella Parte VII. Quando uno di essi sarà completato, questo manuale verrà esteso con una nuova sezione nella Parte IV.

---

## Chiusura

Questo manuale racconta sette step di lavoro. Sette unità piccole, ciascuna con il suo obiettivo, le sue trappole, le sue iterazioni. Messi insieme producono un'app che funziona — su Windows, e in teoria su Mac e Linux — che condivide dati con una versione console, che si controlla da mouse o tastiera, e che ha una cronologia consultabile delle partite passate.

L'idea più importante che mi porto a casa, e che è la chiave di tutto: **i progetti grandi sono fatti di passi piccoli**. Non c'è nessuno "step 1: scrivi l'app intera". C'è "step 1: estrai il Core". Poi "step 2: porta una schermata in GUI". Poi "step 3: aggiungi la cronologia". Ognuno è gestibile in poche ore. Sommati, fanno qualcosa che da fuori sembra impossibile.

Il secondo concetto, altrettanto importante: **la spec è un'ipotesi, il test pratico è il giudice**. Si pianifica perché si fanno meno errori, non perché si crede di non farne. Quando l'errore arriva (e arriva sempre), si aggiorna la spec e si va avanti.

Per il resto: leggi il `README.md` per il "come si usa", il `WSA_QUIZ_HANDOFF.md` per i dettagli operativi e la roadmap, e le spec/plan datati per il dettaglio di ogni step.

Buon ripasso.
