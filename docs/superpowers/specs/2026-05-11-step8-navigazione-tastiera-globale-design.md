# Step 8 — Navigazione tastiera globale (design)

> Spec di design per lo step 8 del progetto WSA Quiz (Avalonia). Brainstormato con Luca il 2026-05-11.

## Obiettivo

Estendere la navigazione da tastiera fuori dal `QuizView` (dove già funziona dallo step 4 e 7) in modo che l'app sia interamente usabile senza mouse. Quattro aree coperte: modale pausa, `HomeView`, switch fra tab principali, liste `CronologiaView` e `SospesiView`.

## Decisioni UX consolidate

| Decisione | Valore scelto | Motivo |
|---|---|---|
| Meccanica di base | Ibrida: focus nativo Avalonia + indice custom solo dove serve | Riduce codice; per liste/dialog il focus nativo basta, la Home con griglia 2D eterogenea richiede logica dedicata |
| Indicatore visivo focus | Bordo giallo `#FFD500` 3px ovunque | Riusa l'idioma dello step 7 (highlight risposta), coerente per Luca |
| Trigger dell'indicatore | Pseudo-classe `:focus-visible` (focus da tastiera, NON da click) | Evita di colorare l'ultimo bottone cliccato col mouse |
| Switch tab principali | `Ctrl+Tab` / `Ctrl+Shift+Tab` ciclico | Non collide con `←/→` del quiz (step 7); standard browser/IDE |
| Navigazione Home | `Tab`/`Shift+Tab` fra zone + `↑/↓` dentro la zona | Avvia → Materie → Categorie → Opzioni; pattern Office/Explorer |
| Modale pausa | `Tab` + `←/→`, focus iniziale = Annulla, `Invio` conferma, `Esc` = Annulla | Default conservativo: Esc+Invio non causa azioni distruttive |
| Liste Cronologia/Sospesi | `↑/↓` riga, `Invio` apre/Riprende, `Canc` avvia conferma inline | Coerente con `ListBox` nativo + pattern conferma esistente |

## Architettura — indicatore visivo unificato

In `App.axaml` aggiungiamo uno style globale che, sotto la pseudo-classe `:focus-visible`, disegna un bordo `#FFD500` 3px attorno a `Button`, `CheckBox`, `ListBoxItem`, `TabItem`. Avalonia 12 supporta `:focus-visible` come pseudo-classe distinta da `:focus` per i casi di focus mosso da tastiera.

Implementazione probabile:
```xml
<Style Selector="Button:focus-visible">
  <Setter Property="Template">
    <!-- bordo giallo aggiunto al template -->
  </Setter>
</Style>
```

Approccio alternativo (se `:focus-visible` non è ancora pieno in Avalonia 12.0.2): wrappare il `ContentPresenter` di ciascun template controllando `IsKeyboardFocusWithin` via trigger. Da verificare in fase di implementazione.

Il focus mouse (`PointerPressed`) **non** mostra il giallo. Questo si ottiene o nativamente con `:focus-visible`, o impostando `Focusable=False` su elementi che vogliamo cliccabili ma non keyboard-focusables — da preferire la prima via.

## Sezione 1 — Switch tab principali

**Dove:** `MainWindow.axaml.cs` override `OnKeyDown` (oppure `AddHandler(KeyDownEvent, ..., RoutingStrategies.Tunnel)` per intercettare prima dei figli).

**Comportamento:**
- `Ctrl+Tab` → `MainTabControl.SelectedIndex = (SelectedIndex + 1) % 3`
- `Ctrl+Shift+Tab` → `MainTabControl.SelectedIndex = (SelectedIndex - 1 + 3) % 3`
- Dopo lo switch, mettiamo il focus sul primo controllo focusable della nuova view tramite `FocusManager` o `view.Focus()`.
- `e.Handled = true` per impedire propagazione.

**Non interferenze:**
- Dentro al quiz: il `QuizView` intercetta lettere/`Invio`/`Esc`/`←→/↑↓`, ma **non** modifier-Tab. Quindi `Ctrl+Tab` passa di sopra alla `MainWindow`.
- Dentro alla modale pausa: la modale è una `Window` separata che possiede `Owner`. Una `Window` figlia non riceve gli eventi tastiera della `MainWindow`. Quando la modale è aperta, `Ctrl+Tab` premuto dentro la modale NON cambia tab (è gestito dal `KeyDown` della modale stessa, che lo ignora o lo blocca con `e.Handled=true`).

## Sezione 2 — Modale pausa

**Dove:** la `Window` modale creata dal `QuizView` quando si preme Esc/Pausa (struttura definita nello step 6, "Pausa unificata").

**Comportamento all'apertura:**
- `cancelButton.Focus()` in `OnAttachedToVisualTree`.
- Il bordo giallo evidenzia Annulla.

**OnKeyDown della modale:**
- `Tab` / `Shift+Tab` → focus nativo Avalonia, lascia gestire al sistema (nessun override necessario).
- `←` → focus al bottone precedente (ciclico: Annulla → Salva → Abbandona → Annulla)
- `→` → focus al bottone successivo (ciclico: Annulla → Abbandona → Salva → Annulla)
- `Invio` → focus nativo: il bottone focused riceve `Click`. Avalonia di default attiva il bottone focused su `Enter` quando ha `IsDefault=false` ma `Click()` esplicito è più affidabile. Da verificare al primo test.
- `Esc` → comportamento esistente, equivale ad Annulla (chiude modale, torna al quiz).
- `Ctrl+Tab` → ignorato (`e.Handled=true`) per non confondere con switch tab globale mentre c'è una modale.

**Indice dei tre bottoni in `Children`:** Annulla (0), Abbandona (1), Salva e esci (2). L'ordine visivo in XAML è lo stesso. Le frecce navigano sull'ordine visivo.

## Sezione 3 — Home (4 zone)

**Quattro zone in ordine `Tab`/`Shift+Tab`:**

1. **Avvia** (barra sticky in alto, 1 elemento: il `Button` "Avvia quiz")
2. **Materie** (lista di `CheckBox`)
3. **Categorie** (lista di `CheckBox`, dentro `ScrollViewer MaxHeight=240`)
4. **Opzioni** (mix: `CheckBox` rotazione + `CheckBox` cronometro + `CheckBox` randomizza + `NumericUpDown` limita N)

**Implementazione:**

Su ciascun container di zona, impostiamo `KeyboardNavigation.TabNavigation="Once"`. Questo fa sì che `Tab` atterri sul primo elemento focusable della zona, e il `Tab` successivo salta alla zona dopo.

In `HomeView.axaml.cs` override `OnKeyDown`:

```csharp
if (e.Key == Key.Down || e.Key == Key.Up)
{
    var focused = FocusManager.Instance?.Current;
    if (focused is NumericUpDown) return; // lascia che incrementi/decrementi
    var zona = TrovaZonaAntenata(focused);
    if (zona != null)
    {
        MuoviInZona(zona, e.Key == Key.Down ? +1 : -1);
        e.Handled = true;
    }
}
```

`TrovaZonaAntenata` risale i visual ancestor finché trova un controllo con un `Tag="Zona"` o un nome convenuto (`PART_ZonaMaterie`, `PART_ZonaCategorie`, ecc.).

`MuoviInZona` raccoglie tutti i controlli focusable della zona in ordine visivo e sposta il focus al successivo/precedente (ciclico solo dentro la zona, NON wrappa alla zona dopo — `Tab` serve a quello).

**Casi speciali:**
- Barra Avvia: una sola entità focusable, le frecce non fanno nulla.
- `NumericUpDown` in Opzioni: il filtro sopra preserva il comportamento nativo `↑↓` = incrementa/decrementa.
- `Spazio` su `CheckBox` focused → comportamento nativo (toggle), niente codice.
- Categorie con `ScrollViewer`: navigando `↑/↓` oltre il viewport, Avalonia normalmente scrolla automaticamente per portare il focused in vista (`BringIntoView` su `GotFocus`). Da verificare.

## Sezione 4 — Liste Cronologia e Sospesi

**Conversione `ItemsControl` → `ListBox`:**

Verificare se attualmente sono `ItemsControl` o `ListBox`. In ogni caso vogliamo un `ListBox` con `SelectionMode="Single"`. Il `ListBox` di Avalonia ha già navigazione tastiera `↑/↓` su `ListBoxItem` con focus visibile (rinforzato dal nostro bordo giallo globale).

**`CronologiaView`:**
- `↑/↓` → muove `SelectedIndex` (nativo)
- `Invio` → apre dettaglio della riga selezionata (= replica del doppio-click esistente). Override `OnKeyDown`.
- `Canc` → invoca lo stesso handler di click su "Elimina" della riga selezionata. Avvia conferma inline (riga mostra "Sicuro? Sì, elimina / Annulla").
- Quando una riga è in stato conferma e ha il focus, `Canc` di nuovo (o `Invio` mentre il focus è sul bottone "Sì, elimina") conferma. `Esc` annulla la conferma.

**`SospesiView`:** identico, ma `Invio` = Riprendi e `Canc` = Elimina con conferma inline.

**`CronologiaDettaglioView`:**
- `Esc` → torna alla lista (= bottone "← Cronologia").
- `↑/↓` → scroll nativo del `ScrollViewer` (default).
- Il bottone "Elimina questa partita" (step 5) resta navigabile via `Tab`; `Canc` non lo invoca da qui (per evitare ambiguità con la lista sottostante).

## Conflitti già analizzati

| Contesto | Tasto | Chi vince | Note |
|---|---|---|---|
| QuizView attivo | `A/B/C/D` `↑↓` `←→` `Invio` `Esc` | QuizView | Nessuna sovrapposizione con le nuove scorciatoie |
| QuizView attivo | `Ctrl+Tab` | MainWindow | Cambia tab. Quiz resta vivo, si ritrova al ritorno |
| Modale pausa aperta | `Ctrl+Tab` | Modale (ignora) | Niente switch tab con modale aperta |
| HomeView, focus su NumericUpDown | `↑↓` | NumericUpDown nativo | Incrementa/decrementa N |
| HomeView, focus su CheckBox | `↑↓` | HomeView | Sposta focus alla checkbox successiva nella zona |
| HomeView, focus su CheckBox | `Spazio` | CheckBox nativo | Toggla |
| CronologiaView, focus su lista | `Canc` | CronologiaView | Avvia conferma inline |
| CronologiaView, focus su lista in conferma | `Esc` | CronologiaView | Annulla conferma (toggla flag) |
| CronologiaDettaglioView | `Esc` | Dettaglio | Torna alla lista |
| Ovunque | `Ctrl+Tab` | MainWindow | Cambia tab in modo ciclico |

## Test manuale (criteri di completamento)

Una sessione end-to-end che copre:

1. Boot → al primo `Tab` il focus diventa visibile (bordo giallo) sul primo controllo della Home.
2. `Tab` dalla Home gira fra le 4 zone (Avvia → Materie → Categorie → Opzioni → Avvia) ciclicamente; il bordo giallo segue.
3. `↑/↓` dentro Materie e Categorie sposta fra checkbox; `Spazio` toggla; lo scroll di Categorie segue il focused.
4. `↑/↓` su `NumericUpDown` (Opzioni → Limita a N) incrementa/decrementa il valore, NON cambia il focus.
5. `Ctrl+Tab` cambia tab Home → Cronologia → Sospesi → Home; il focus atterra dentro la nuova view e il bordo giallo è visibile.
6. In Cronologia: `↑/↓` sceglie riga, `Invio` apre dettaglio, `Esc` torna; `Canc` mostra "Sicuro? Sì / Annulla" inline; `Canc` di nuovo (o focus su "Sì, elimina" + `Invio`) elimina; `Esc` annulla la conferma.
7. In Sospesi: come Cronologia, ma `Invio` = Riprendi (parte la sessione, naviga al QuizView).
8. Dentro al quiz: tutte le scorciatoie pre-esistenti (step 4 e 7) continuano a funzionare invariate. `Ctrl+Tab` cambia tab anche da qui.
9. Dentro la modale pausa: focus su Annulla all'apertura (bordo giallo visibile); `←/→` e `Tab` muovono ciclicamente fra i 3 bottoni; `Invio` conferma il focused; `Esc` annulla; `Ctrl+Tab` ignorato.
10. Click col mouse su un qualsiasi controllo: NON deve apparire il bordo giallo. Il bordo giallo appare solo quando il focus arriva da tastiera.

## Out of scope

- **Statistiche / tab quarto** (step 9): non aggiunto, non navigato.
- **Dark mode** (step 10): il bordo giallo `#FFD500` resta lo stesso anche in dark mode quando ci arriveremo.
- **Scorciatoie globali tipo F1, F5**: non aggiunte.
- **Accelerator/AccessKey su voci di menu**: non c'è menu nell'app oggi, non rilevante.

## Trappole anticipate

- **`:focus-visible` in Avalonia 12.0.2**: la pseudo-classe potrebbe non essere implementata in modo identico a CSS. Fallback: gestione via code-behind che imposta una pseudo-classe custom (`:keyboard-focus`) su `GotKeyboardFocus` / `LostKeyboardFocus` e la rimuove su `PointerPressed`.
- **`Tab` come carattere dentro `NumericUpDown`**: il `NumericUpDown` di Avalonia accetta input numerico e gestisce `Tab` come uscita; non dovrebbe consumarlo, ma da verificare.
- **`Ctrl+Tab` su Windows con altre app**: è un tasto di sistema in alcuni casi (es. `Alt+Tab` switcher), ma `Ctrl+Tab` puro è libero. OK.
- **Focus che si "perde" dopo certe operazioni**: dopo lo switch tab è facile dimenticare di mettere il focus sulla nuova view → la prima `Tab` da utente sembrerebbe inerte. Mitigazione: chiamare `view.Focus()` esplicito subito dopo `SelectedIndex = ...`.
- **Indicatore giallo su `ListBoxItem`**: il template di `ListBoxItem` ha già un "selected" visivo (background azzurro Fluent). Il bordo giallo si somma. Verificare che la combinazione non sia confondente; eventualmente per `ListBoxItem` il bordo va sopra al selected-highlight, non al posto.

## Riferimenti

- Step 4 (tastiera quiz): `docs/superpowers/specs/2026-05-08-step4-tastiera-pausa-design.md`
- Step 6 (Home sticky + Pausa unificata): `docs/superpowers/specs/2026-05-09-step6-rifiniture-ux-design.md`
- Step 7 (navigazione domande passate, origine del giallo `#FFD500` 3px): `docs/superpowers/specs/2026-05-10-step7-navigazione-domande-design.md`
- Handoff progetto: `WSA_QUIZ_HANDOFF.md`
