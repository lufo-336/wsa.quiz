# WSA Quiz

App di quiz multi-materia pensata per ripassare il programma del corso **Web & Software Architect (WSA)**. Esiste in due eseguibili che condividono lo stesso Core, gli stessi dati e la stessa cronologia:

- **`wsa.quiz.cli`** — versione console (.NET 8). Pratica per ripassare senza distrazioni o da terminale.
- **`wsa.quiz.app`** — versione grafica (.NET 8 + Avalonia 12). Tre tab: Home (configurazione + Avvia), Cronologia (sessioni passate con drill-down), Sospesi (sessioni in pausa, riprendibili).

Una sessione di quiz può essere **classica** (ogni domanda chiesta una volta) o in **modalità rotazione** (le sbagliate tornano in coda finché non vengono indovinate). Si possono filtrare le domande per materia e categoria, limitare a N domande, randomizzare l'ordine, attivare/disattivare il cronometro. Si può mettere in pausa e riprendere dopo. La cronologia mostra punteggio, durata, % corrette e — al doppio click — il dettaglio domanda per domanda.

## Avvio rapido

Requisiti: **.NET 8 SDK**.

```bash
# Console
dotnet run --project wsa.quiz.cli

# GUI
dotnet run --project wsa.quiz.app
```

La cronologia viene salvata in una cartella utente condivisa fra console e GUI:

- Windows: `%APPDATA%\WsaQuiz`
- macOS:   `~/Library/Application Support/WsaQuiz`
- Linux:   `~/.config/WsaQuiz`

## Aggiungere materie, categorie e domande

I dati si modificano via file JSON nella root del progetto. A ogni build i file vengono copiati negli output `bin/`, quindi basta rilanciare l'eseguibile per vedere le modifiche.

### 1. Materie — `materie.json`

Elenco di oggetti, uno per materia. La `cartella` è relativa alla root e contiene tutti i `.json` con le domande di quella materia.

```json
[
  {
    "id": "cs",
    "nome": "C#",
    "cartella": "domande/cs",
    "formato": "standard"
  }
]
```

| Campo      | Tipo   | Note                                                                 |
|------------|--------|----------------------------------------------------------------------|
| `id`       | string | Identificatore breve, usato internamente. Univoco.                   |
| `nome`     | string | Etichetta mostrata in Home e nella cronologia.                       |
| `cartella` | string | Path relativo alla root. La cartella deve esistere.                  |
| `formato`  | string | Per ora sempre `"standard"`. Riservato per formati alternativi.      |

### 2. Domande — `domande/<materia>/<qualsiasi>.json`

Ogni file è un array di domande. Si possono avere più file per materia (es. `csharp_domande.json`, `csharp_domande_2.json`): il loader li unisce tutti. La **categoria** è un campo libero su ciascuna domanda — non va dichiarata altrove. La Home offre i filtri per materia e categoria automaticamente.

```json
[
  {
    "id": 302,
    "categoria": "Basi del linguaggio",
    "domanda": "Quale tipo di dato si usa in C# per rappresentare numeri interi?",
    "risposte": [
      "int",
      "char",
      "double",
      "float"
    ],
    "rispostaCorretta": 0,
    "spiegazione": "Il tipo `int` rappresenta numeri interi in C# (es. 42, -10, 0). `double` è per numeri decimali, `float` è un decimale a precisione singola, `char` rappresenta un singolo carattere."
  }
]
```

| Campo              | Tipo     | Note                                                                                  |
|--------------------|----------|---------------------------------------------------------------------------------------|
| `id`               | int      | Univoco per il file. Usato per ordinare/tracciare.                                    |
| `categoria`        | string   | Etichetta libera. Stessa stringa = stessa categoria nei filtri.                       |
| `domanda`          | string   | Testo della domanda.                                                                  |
| `risposte`         | string[] | Esattamente **4** opzioni.                                                            |
| `rispostaCorretta` | int      | Indice 0..3 della risposta giusta in `risposte`.                                      |
| `spiegazione`      | string   | Mostrata dopo aver risposto. Markdown leggero (apici, backtick, a-capo) consentito.   |

Internamente le 4 risposte vengono **rimescolate ad ogni partita** (in modo che A/B/C/D non siano sempre nello stesso ordine), e l'indice corretto viene ricalcolato di conseguenza.

## Generare nuove domande con un'IA

Le risposte multiple sono soggette a due bias tipici quando vengono generate da modelli di linguaggio:

1. **Bias della risposta più lunga**: l'IA tende a produrre la risposta corretta più dettagliata e i distrattori più brevi. Chi conosce il trucco indovina senza sapere la materia.
2. **Bias di posizione**: la risposta corretta finisce statisticamente più spesso in alcuni indici (tipicamente A o l'ultimo).

Il modello deve essere istruito esplicitamente per evitarli. Il prompt qui sotto è quello che usiamo:

> Genera **N** domande a risposta multipla in formato JSON conformi a questo schema:
>
> ```json
> {
>   "id": <int univoco>,
>   "categoria": "<stringa libera>",
>   "domanda": "<testo della domanda>",
>   "risposte": ["<A>", "<B>", "<C>", "<D>"],
>   "rispostaCorretta": <0|1|2|3>,
>   "spiegazione": "<perché la corretta è giusta e perché le altre sono sbagliate>"
> }
> ```
>
> Argomento: **<materia / categoria specifica>**. Livello: **<base / intermedio / avanzato>**.
>
> Regole obbligatorie:
>
> 1. **Lunghezza delle risposte uniforme**: le 4 opzioni devono avere lunghezza simile (stesso ordine di grandezza in caratteri, max ±30% di differenza tra la più corta e la più lunga). La risposta corretta NON deve essere sistematicamente la più lunga né la più corta: deve essere talvolta più lunga, talvolta più corta, talvolta di lunghezza simile ai distrattori.
> 2. **Distribuzione della corretta**: tra tutte le domande del batch, l'indice `rispostaCorretta` deve essere distribuito in modo approssimativamente uniforme tra 0, 1, 2 e 3. Punta a circa il 25% per ogni posizione, con un'oscillazione tollerata fino a circa il 33%, ma evita che una stessa posizione raccolga più del 35% delle corrette o meno del 15%.
> 3. **Distrattori plausibili**: i 3 distrattori devono essere errori realistici (concetti vicini, sintassi simile, sinonimi che però non si applicano), non opzioni assurde o palesemente fuori tema.
> 4. **Spiegazione completa**: la `spiegazione` deve dire perché la corretta è giusta E perché ognuna delle altre 3 è sbagliata, in 2-4 frasi.
> 5. **`id` univoci**: parti dall'`id` <numero di partenza> e incrementa di 1 per ogni domanda generata.
>
> Restituisci esclusivamente un array JSON valido, senza testo prima o dopo, senza commenti, senza markdown wrapper.

Dopo la generazione: salvare l'output come `.json` nella cartella della materia corrispondente (es. `domande/cs/csharp_domande_4.json`). Al successivo avvio dell'app le nuove domande sono già disponibili. Suggerimento: prima di confermare il batch, verificare manualmente la distribuzione delle corrette e correggere le tre/quattro inevitabili sviste — è normale che il modello sbandi sulla regola 1 o 2.

## Architettura

Tre progetti .NET 8 nella stessa solution:

```
Wsa.Quiz.sln
├── wsa.quiz.core/      Libreria. Modelli (Domanda, Materia, RisultatoQuiz, ...) e servizi
│                        (QuizService per shuffle/punteggio, StorageService per cronologia/sospesi).
├── wsa.quiz.cli/       Console. Tutta l'interazione utente in Program.cs e Services/ConsoleUI.cs.
└── wsa.quiz.app/       GUI Avalonia. State machine event-driven in State/SessioneQuiz.cs,
                         viste in Views/.
```

Storage condiviso: `StorageService(cartellaDati, cartellaUtente)`. La cartella utente contiene `cronologia.json` e `pause.json`. La cartella dati è l'output del bin (dove i `materie.json` e i `domande/` vengono copiati a ogni build).

L'app GUI usa **code-behind + INotifyPropertyChanged**, no MVVM. Stile Fluent + font Inter.

Maggiori dettagli architetturali, decisioni prese, trappole incontrate e roadmap futura sono in [`WSA_QUIZ_HANDOFF.md`](./WSA_QUIZ_HANDOFF.md).

## Stato

Lavoro in corso, sviluppato per step incrementali documentati in `docs/superpowers/`. Per ora funziona end-to-end su Windows; macOS/Linux non sono stati provati di recente ma il Core e Avalonia 12 sono cross-platform.

## Licenza

Uso personale didattico. Le domande sono pensate per il corso WSA — alcune categorie potrebbero non essere generalizzabili.
