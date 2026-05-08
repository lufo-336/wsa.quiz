# WSA Quiz — Step 1 (Fondazione)

Questo zip contiene la nuova struttura del progetto, da sovrapporre alla tua
cartella `wsa.quiz/` esistente. Dopo l'applicazione avrai tre progetti:

- `wsa.quiz.core/` → libreria condivisa con Models e Services
- `wsa.quiz.console/` → app console (uguale a prima nel comportamento)
- `wsa.quiz.app/` → app Avalonia (placeholder che verifica il caricamento dati)

## Come applicare

Dalla terminal, dentro la tua repo `wsa.quiz/`:

```bash
# 1. Estrai lo zip qui dentro (sovrascrive nulla di critico, aggiunge le 3 nuove cartelle e Wsa.Quiz.sln)
unzip /percorso/al/wsa_quiz_step1.zip

# 2. Lancia lo script di pulizia che rimuove i file vecchi
chmod +x step1_cleanup.sh
./step1_cleanup.sh

# 3. Build e run (servono il .NET 8 SDK e una macchina Windows per l'app Avalonia)
dotnet build Wsa.Quiz.sln
dotnet run --project wsa.quiz.console/Wsa.Quiz.Console.csproj
```

## Struttura finale di `wsa.quiz/`

```
wsa.quiz/
├── Wsa.Quiz.sln
├── README.md                 (esistente)
├── nuget.config              (esistente)
├── Come Aggiungere Domande.txt  (esistente)
├── materie.json              ← aggiungere materie qui (invariato)
├── domande/                  ← aggiungere domande qui (invariato)
│   ├── cpp/
│   ├── cs/
│   ├── frontend/
│   ├── infra/
│   └── sql/
├── wsa.quiz.core/
│   ├── Wsa.Quiz.Core.csproj
│   ├── Models/  (7 .cs)
│   └── Services/  (QuizService.cs, StorageService.cs)
├── wsa.quiz.console/
│   ├── Wsa.Quiz.Console.csproj
│   ├── Program.cs
│   └── Services/ConsoleUI.cs
└── wsa.quiz.app/
    ├── Wsa.Quiz.App.csproj
    ├── Program.cs            (Avalonia bootstrap)
    ├── App.axaml + App.axaml.cs
    ├── MainWindow.axaml + MainWindow.axaml.cs
    └── app.manifest
```

`materie.json` e `domande/` restano in radice come prima, e vengono copiati nel
bin di entrambe le app a ogni build (`<None Include="..\materie.json">` nel csproj).
**Aggiungere una nuova materia o una nuova domanda funziona esattamente come prima**:
modifichi i JSON in radice, e al prossimo build sia il console che l'app GUI li vedono.

## Cambiamento importante: cartella dati utente condivisa

Cronologia (`cronologia.json`) e sospesi (`quiz_in_pausa.json`) **non vivono piu'
nel bin di ciascun progetto**. Sono spostati in una cartella utente condivisa:

- macOS: `~/Library/Application Support/WsaQuiz/`
- Windows: `%APPDATA%\WsaQuiz\`
- Linux: `~/.config/WsaQuiz/`

Cosi' se fai un quiz dal console e poi apri l'app GUI, ti ritrovi cronologia e
sospesi. Lo stesso al contrario.

**Nota sulla migrazione**: se avevi gia' fatto qualche quiz prima dello step 1,
trovi i vecchi `cronologia.json` e `quiz_in_pausa.json` dentro la vecchia
`bin/Debug/net8.0/` (rimossa dallo script di cleanup) o nelle copie di backup.
Per portarli avanti basta copiarli nella nuova cartella utente.

## Verifica che lo step 1 sia andato a buon fine

1. **Console gira come prima**:
   `dotnet run --project wsa.quiz.console/Wsa.Quiz.Console.csproj`
   → menu identico a prima, quiz funzionanti.

2. **App Avalonia parte e legge i dati**:
   `dotnet run --project wsa.quiz.app/Wsa.Quiz.App.csproj`
   → si apre una finestra che dice "Materie configurate: 5" e
     "Domande totali caricate: N" (N variabile in base ai tuoi JSON).

Se entrambi i punti sono OK, lo step 1 e' completo e si puo' passare allo step 2.

## Note

- Tutto il rename `QuizFdP` -> `Wsa.Quiz.*` e' applicato. I namespace sono:
  `Wsa.Quiz.Core.Models`, `Wsa.Quiz.Core.Services`, `Wsa.Quiz.Console`,
  `Wsa.Quiz.Console.Services`, `Wsa.Quiz.App`.
- `StorageService` ha ora due costruttori: quello vecchio a un parametro
  (compatibile col passato) e quello nuovo a due parametri (read-only + utente).
  Entrambi gli eseguibili usano il nuovo.
- La cartella `wsa.quiz.Wpf/` viene cancellata: l'abbiamo sostituita con Avalonia.
  Se vuoi recuperarla in futuro, e' sempre nello storico git.
