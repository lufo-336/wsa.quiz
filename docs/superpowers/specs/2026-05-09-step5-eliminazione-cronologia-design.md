# Step 5 — Eliminazione cronologia

> Design spec. Stato: approvata da Luca, pronta per l'implementazione.

## Obiettivo

Permettere all'utente di:
1. Cancellare una singola partita dalla Cronologia (sia dalla lista, sia dal dettaglio).
2. Svuotare l'intera cronologia in un colpo solo.

Senza toccare i Sospesi e senza rompere lo storage condiviso console ↔ GUI.

## Decisioni prese in brainstorming

- **Id stabile**: `Guid` generato in `SalvaRisultato`, con migrazione lazy una-tantum dei record esistenti.
- **Trigger eliminazione riga**: bottone "Elimina" sulla riga della lista, conferma inline (stesso pattern dei Sospesi).
- **Cancella tutto**: bottone "Svuota cronologia" nell'header della tab, conferma modale.
- **Eliminazione dal dettaglio**: presente, conferma inline. Dopo conferma torna alla lista.
- **Stato vuoto**: testo centrato "Nessuna partita in cronologia."

## Architettura — file impattati

### `Wsa.Quiz.Core` (libreria condivisa)

**`Models/RisultatoQuiz.cs`**
- Aggiunge `string Id { get; set; } = "";`
- Default stringa vuota per consentire deserializzazione di record vecchi senza il campo.

**`Services/StorageService.cs`**
- `SalvaRisultato(RisultatoQuiz r)`: prima di appendere, `if (string.IsNullOrEmpty(r.Id)) r.Id = Guid.NewGuid().ToString();`
- `CaricaCronologia()`: dopo la deserializzazione, se `lista.Any(r => string.IsNullOrEmpty(r.Id))` → assegna Guid ai mancanti e riscrive il file una volta. Idempotente: dalla seconda chiamata in poi è no-op.
- Nuovi metodi pubblici:
  - `void EliminaRisultato(string id)` — leggi-filtra-riscrivi. No-op silenzioso se l'id non esiste.
  - `void SvuotaCronologia()` — scrive lista vuota nel file (NON cancella il file, per evitare race con prossimo `SalvaRisultato`/`CaricaCronologia`).

### `Wsa.Quiz.App` (GUI)

**`State/RisultatoCronologiaItem.cs`**
- Aggiungere `string Id` (passato dal wrapping nel costruttore).
- Aggiungere `bool InAttesaConfermaEliminazione` observable (pattern identico a `SessioneSospesaItem`).

**`Views/CronologiaView.axaml(.cs)`**
- Header: bottone "Svuota cronologia" (`Classes="danger"`) accanto al bottone "Aggiorna" già esistente. `IsEnabled` legato a `Items.Count > 0`.
- Riga: bottone "Elimina" (`Classes="danger"`) sulla destra; in conferma swappa a "Sì, elimina" (`danger`) + "Annulla".
- Vincolo: al massimo una conferma aperta alla volta. Aprirne una azzera le altre (riusa la logica di `SospesiView.OnEliminaClick`).
- Stato vuoto: `TextBlock` centrato "Nessuna partita in cronologia.", visibile quando la lista è vuota.

**`Views/CronologiaDettaglioView.axaml(.cs)`**
- Aggiungere bottone "Elimina questa partita" (`Classes="danger"`) accanto al "← Cronologia" già esistente.
- Click → swap inline a "Sì, elimina" + "Annulla".
- Conferma: `_storage.EliminaRisultato(_risultatoCorrente.Id)` → torno alla lista (swap interno della `CronologiaView`) → `Ricarica()`.

### `Wsa.Quiz.Cli` (console)

Nessuna modifica funzionale obbligatoria in questo step. Eventuale voce "Cancella cronologia" nel menu console è fuori scope.

## Modello dati e migrazione

```csharp
public class RisultatoQuiz
{
    public string Id { get; set; } = "";   // nuovo
    // ... resto invariato
}
```

Migrazione in `CaricaCronologia()`:

```csharp
var lista = JsonSerializer.Deserialize<List<RisultatoQuiz>>(json) ?? new();
bool serveRiscrittura = false;
foreach (var r in lista)
{
    if (string.IsNullOrEmpty(r.Id))
    {
        r.Id = Guid.NewGuid().ToString();
        serveRiscrittura = true;
    }
}
if (serveRiscrittura)
{
    File.WriteAllText(percorsoCronologia, JsonSerializer.Serialize(lista, opzioniJson));
}
return lista;
```

Edge case console + GUI in concorrenza sulla migrazione: ultimo writer vince, accettato (Guid diversi ma comunque entrambi validi, nessuna corruzione).

## UX — flussi

### Eliminazione da riga lista

1. Click "Elimina" → `item.InAttesaConfermaEliminazione = true`.
2. Eventuale altra riga in attesa di conferma viene azzerata.
3. UI riga swappa a "Sì, elimina" + "Annulla".
4. "Annulla" → flag a `false`, fine.
5. "Sì, elimina" → `_storage.EliminaRisultato(item.Id)` → `Ricarica()`.

### Svuota cronologia (header)

1. Click "Svuota cronologia" → dialog modale (pattern `ChiediConfermaAbbandono`):
   - `Window` 420×180, `WindowStartupLocation=CenterOwner`, `CanResize=false`, `ShowInTaskbar=false`.
   - Testo: *"Verranno eliminate **N partite** dalla cronologia. L'azione non è reversibile."*
   - Pulsanti: "Annulla" (default focus) | "Sì, cancella tutto" (`danger`).
   - ESC = Annulla.
2. Conferma → `_storage.SvuotaCronologia()` → `Ricarica()`.

### Eliminazione da dettaglio

1. Click "Elimina questa partita" → swap inline a "Sì, elimina" + "Annulla".
2. "Annulla" → flag a `false`, fine.
3. "Sì, elimina" → `_storage.EliminaRisultato(_risultatoCorrente.Id)` → torno alla lista (swap della `CronologiaView`) → `Ricarica()`.

## Cosa NON fa questo step

- Non aggiunge undo / cestino / ripristino. L'eliminazione è definitiva.
- Non tocca i Sospesi.
- Non aggiunge export/backup pre-svuotamento (rimandato a Step 9).
- Non aggiunge la voce nel menu console.

## Test manuali (acceptance)

1. **Migrazione**: aprire la GUI con un `cronologia.json` esistente (record vecchi senza `Id`). Tab Cronologia → niente errori; riaprire il file e verificare che ogni record abbia ora un campo `"Id"`.
2. **Elimina riga**: click "Elimina" → swap a conferma; "Annulla" ripristina; nuovo click + "Sì, elimina" rimuove la riga.
3. **Conferma esclusiva**: aprire conferma su riga A, poi su riga B → la conferma di A si chiude.
4. **Svuota cronologia**: dialog con N corretto. "Annulla" non cambia nulla. "Sì, cancella tutto" svuota la lista; stato vuoto visibile.
5. **Disabled quando vuota**: dopo svuotamento, il bottone header è disabilitato.
6. **Elimina dal dettaglio**: doppio click → dettaglio → "Elimina questa partita" → conferma → torno alla lista, partita assente.
7. **Persistenza**: chiudo e riapro l'app, le eliminazioni sopravvivono.
8. **Sospesi intatti**: dopo svuotamento cronologia, tab Sospesi non perde nulla.
9. **Console+GUI**: console termina un quiz mentre la GUI ha la tab aperta. "Aggiorna" o auto-refresh → la nuova partita appare con `Id` valorizzato.

## Trappole previste

- **Bottoni dentro `ListBoxItem` che propagano click alla riga**: in Avalonia il click su un Button dentro un template di lista può anche selezionare la riga e (in `CronologiaView`) far scattare il doppio-click sul dettaglio. Già gestito nei Sospesi: il Button non propaga di default. Verificare comunque sul `CronologiaView`.
- **`Items.Count > 0` per `IsEnabled` del bottone header**: serve un binding che reagisce al cambio di `Count`. Se la collection sorgente non è una `ObservableCollection<T>`, il binding di `IsEnabled` non si aggiorna. La `CronologiaView` usa già una collection observable: confermare in implementazione.
- **Riscrittura file durante migrazione**: `JsonSerializer` con `WriteIndented=true` (se usato) genera file più grandi ma più diffabili — mantenere la stessa configurazione di `SalvaRisultato` per coerenza.
