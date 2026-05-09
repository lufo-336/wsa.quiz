# Step 6 — UX rifiniture: Home sticky + Pausa unificata

> Design spec. Approvata da Luca durante il test dello step 5.

## Obiettivo

Due fix UX scoperti provando lo step 5:

1. **Home: bottone "Avvia quiz" sempre visibile.** Selezionando materie/categorie il bottone scivola sotto la fold e diventa difficile cliccarlo, soprattutto a finestra ridotta.
2. **Pausa unificata.** Oggi `ESC` apre il menu pausa (Annulla / Salva e esci / Riprendi) e il bottone "Abbandona" apre una conferma separata (Continua / Abbandona). Le due modali si fondono in una sola, raggiungibile sia da `ESC` sia dal bottone in alto, con tre azioni: Annulla / Abbandona / Salva e esci.

## Decisioni prese

- **Home**: barra "AVVIO" estratta dallo `ScrollViewer` come `DockPanel.Dock="Bottom"`. Aggiunto `MaxHeight=240` al pannello Materie (oggi è l'unico senza cap; Categorie ce l'ha già).
- **Pausa**: bottone in alto a destra rinominato da "Abbandona" a "**Pausa**". Stesso bottone e `ESC` aprono la stessa modale unificata. Tre voci: Annulla (focus default, ESC chiude come Annulla) / Abbandona (`danger`, separato a sinistra) / Salva e esci (`accent`, a destra). Modale 480×210.

## Fix 1 — Home: barra "Avvia" sticky

### Layout attuale (problematico)

```
ScrollViewer
└─ StackPanel (verticale)
   ├─ Titolo
   ├─ Sottotitolo
   ├─ Grid Materie | Categorie    ← Materie non ha MaxHeight
   ├─ Border Opzioni
   └─ Grid AVVIO (riepilogo + bottone)   ← scivola fuori vista
```

### Layout target

```
DockPanel (root)
├─ Border DockPanel.Dock="Bottom"      ← barra sticky
│  └─ Grid (riepilogo + bottone "Avvia quiz")
└─ ScrollViewer (riempie il resto)
   └─ StackPanel
      ├─ Titolo
      ├─ Sottotitolo
      ├─ Grid Materie | Categorie    ← entrambi MaxHeight=240
      └─ Border Opzioni
```

Cambi specifici a `wsa.quiz.app/Views/HomeView.axaml`:

1. Sostituire il root `<ScrollViewer Padding="20">` con un `<DockPanel>` che contiene:
   - una `<Border>` `DockPanel.Dock="Bottom"` che ospita la "AVVIO" Grid (oggi alla fine dello StackPanel)
   - un `<ScrollViewer Padding="20">` con il resto del contenuto (titoli, materie/categorie, opzioni — niente più "AVVIO")
2. La Border sticky deve avere un sottile `BorderBrush` in alto (`BorderThickness="0,1,0,0"`) per separarsi visivamente dal contenuto sopra. Padding `20,12,20,12`. Background `{DynamicResource SystemRegionBrush}` per evidenziarla leggermente.
3. Aggiungere `MaxHeight="240"` al pannello "Materie" `<Border>` con `ScrollViewer` interno per scorrere la lista, identico al pattern già usato per Categorie. Il `DockPanel` interno con il TextBlock "Materie" come Top e l'`ItemsControl` dentro un `ScrollViewer`.
4. Il `MaxWidth="980"` resta sullo `StackPanel` interno per limitare la larghezza del contenuto su monitor grandi. La barra sticky NON ha MaxWidth (occupa tutta la larghezza della tab); il riepilogo+bottone restano allineati a sinistra/destra come oggi.

Niente cambia in `HomeView.axaml.cs` (gli handler `OnAvviaClick`, `OnPulisciCategorieClick` restano agganciati agli stessi nomi).

## Fix 2 — Pausa unificata

### Stato attuale (da rimuovere)

`QuizView.axaml.cs`:
- `OnAbbandonaClick(...)` → `ChiediConfermaAbbandono()` (Window 380×160, due bottoni: Continua / Abbandona).
- `OnKeyDown` su `Key.Escape` → `ApriMenuPausa()` (Window 420×180, tre bottoni: Annulla / Salva e esci / Riprendi accent).

### Stato target

- Bottone in alto a destra del Quiz rinominato da "Abbandona" a "**Pausa**".
- `OnPausaClick(...)` → `ApriMenuPausa()` (rinominata mentalmente: ora è la "modale unificata").
- `OnKeyDown` su `Key.Escape` → stessa `ApriMenuPausa()`.
- `ChiediConfermaAbbandono()` viene **eliminata**.

### Nuova `ApriMenuPausa()`

```
Window 480×210, CenterOwner, no resize, no taskbar.
Title: "Quiz in pausa"

Contenuto:
  TextBlock SemiBold 14: "Quiz in pausa. Cosa vuoi fare?"
  TextBlock 12 dim:
    "Annulla riprende il quiz.
     Abbandona lo chiude e lo registra in cronologia come abbandonato.
     Salva e esci lo mette nei Sospesi: lo riprenderai dalla relativa tab."

Pulsantiera (Grid 3 colonne: Auto, *, Auto):
  - Sinistra:  [Abbandona]  Classes="danger"
  - Centro:    spazio elastico
  - Destra:    [Annulla]  [Salva e esci] Classes="accent"

Annulla = pulsante con focus di default.
ESC dentro la modale = stesso effetto di Annulla (chiude senza azione).
```

Comportamento:
- **Annulla** → chiude la modale, niente altro.
- **Abbandona** → chiude la modale, poi `_sessione.Abbandona()`. Il flusso esistente porta al `RiepilogoView` con `Abbandonato=true`.
- **Salva e esci** → chiude la modale, poi `_sessione.EsportaPausa()` + `_storage.SalvaPausa(...)` + raise `QuizMessoInPausa` (identico al flusso attuale).

### Refactor del code-behind

In `QuizView.axaml.cs`:
- Rinominare `OnAbbandonaClick` → `OnPausaClick`. Internamente invece di `ChiediConfermaAbbandono()` chiama `ApriMenuPausa()`.
- Eliminare il metodo `ChiediConfermaAbbandono()` per intero.
- Riscrivere `ApriMenuPausa()` con il nuovo layout (Window 480×210, terzo bottone "Abbandona" a sinistra, due a destra) e con due "esiti" possibili: `azione = "salva" | "abbandona" | "annulla"`. Una semplice variabile locale `string azione = "annulla"` settata dai click handler dei bottoni, valutata dopo la chiusura della modale.
- Le chiamate `_sessione.Abbandona()` e `Focus()` restano nei comportamenti finali; la `EsportaPausa`+`SalvaPausa` resta uguale.

In `QuizView.axaml`:
- `Content="Abbandona"` → `Content="Pausa"`.
- `Click="OnAbbandonaClick"` → `Click="OnPausaClick"`.

## Cosa NON fa questo step

- Non aggiunge keyboard shortcut per "Pausa" oltre a `ESC` (nessuna nuova lettera tipo `P`).
- Non tocca la modale di conferma "Svuota cronologia" (step 5).
- Non cambia il flusso di ripresa (Sospesi → Riprendi) né la conclusione di un quiz.
- Non aggiunge persistenza di "ultime materie selezionate" o simili (è step 11).

## Test manuali (acceptance)

### Home

1. Apri la GUI, vai a Home, ridimensiona la finestra in altezza (es. 600px). Senza selezionare nulla, la barra "Avvia" deve essere già visibile in basso.
2. Seleziona tutte le materie disponibili. Le categorie appaiono. La barra "Avvia quiz" rimane visibile in fondo, senza scroll.
3. Selezionando molte categorie, il pannello scorre internamente (come oggi). Il pannello Materie con tante voci scorre internamente (nuovo, max 240px).
4. Il bottone "Avvia quiz" è abilitato/disabilitato come oggi (`PuoAvviare`); il riepilogo selezione appare a sinistra come oggi.
5. Su finestra molto larga (es. 1600px), il contenuto resta limitato a `MaxWidth=980` ma la barra sticky occupa tutta la larghezza. Allineamento corretto.

### Pausa

6. Avvia un quiz. Il bottone in alto a destra dice "**Pausa**".
7. Click sul bottone "Pausa" → modale 480×210 con tre voci. Layout: Abbandona a sinistra (rosso), Annulla + Salva e esci a destra (Salva e esci accent).
8. ESC apre la stessa modale (non più una conferma diretta dello step 4).
9. Dentro la modale, ESC chiude (= Annulla); riprendo il quiz con focus tastiera attivo.
10. Click "Annulla" → idem.
11. Click "Abbandona" → vado in Riepilogo, sessione marcata Abbandonato (cronologia mostra "Abbandonato").
12. Click "Salva e esci" → torno a Home (come oggi); tab Sospesi mostra la pausa nuova.
13. Tasti `A/B/C/D/Invio` continuano a funzionare dopo aver chiuso la modale (focus restituito al `QuizView`).

## Trappole previste

- **`StackPanel` interno con `DockPanel` esterno**: passando da `ScrollViewer` root a `DockPanel` root, il `DockPanel` riempie tutta l'area della tab. La `Border` sticky (Bottom) si posiziona sempre in fondo. Lo `ScrollViewer` interno deve avere `LastChildFill="True"` (default del DockPanel) per espandersi.
- **`MaxHeight=240` su Materie**: la sezione Materie usa `ItemsControl` dentro un `DockPanel`. Per avere uno scroll funzionante quando si supera 240px, va wrappato in `ScrollViewer VerticalScrollBarVisibility="Auto"` come Categorie. Se invece si mette `MaxHeight` solo sul `Border` esterno la lista viene tagliata senza scroll — sbagliato.
- **Modale 480 vs 420**: vecchio menu pausa era 420×180. La nuova ha tre bottoni e una descrizione più lunga: 480×210 sembra adeguato. Verificare che il testo non venga troncato. In caso, alzare a 220.
- **Default focus su Annulla**: in Avalonia `Focus()` esplicito in `Window.Opened` — verificare che funzioni; in alternativa `IsDefault=true` su `Annulla` (ma `IsDefault` su Avalonia spesso si lega a Invio, non al focus visivo).
