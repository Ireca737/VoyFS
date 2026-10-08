# FlyLabFS — Registro dei punti di aggancio a JoinFS

Data: 8 ottobre 2026. Responsabilità: manutentore della modifica FlyLab.

## Baseline e limiti della verifica

Inventario verificato mediante lettura del codice e diff Git, non mediante esecuzione Windows o volo di prova.

- Repository: `Ireca737/VoyFS`.
- Client: `feat/flylab-ui`, commit `2dc3e56c988a46c283181635d09b4778c448691e`.
- Base del confronto: `main`, commit `2d9913d205e2f48dd4458337858a349624cdc3fc`, coincidente con il merge-base del client. Non equivale a una verifica dell'ultima revisione Tuduce disponibile.
- RadioBridge: lettura separata di `feat/voy-radio-bridge`, commit `6336d5c028a114243947c56c85e3537c4e6496fc`. Non si presume che il server ATC-3 esegua esattamente questa revisione.
- Nel delta client: otto form modificati, `Program.cs`, `JoinFS.csproj`; classi aggiunte sotto `JoinFS/FlyLab`, workflow build e documentazione. Nessun Designer modificato rispetto alla base; nessuna modifica a networking, SimConnect, protocollo, interpolazione o model matching nel delta esaminato.

**Separazione dal core non significa assenza di dipendenze interne.** La UI legge anche oggetti JoinFS direttamente. Il traffico usa **Whazzup**, che non è WebSocket. Il RadioBridge esaminato usa un webhook HTTP, non un client WebSocket.

## Regola di manutenzione

Ogni modifica che aggiunge, cambia o rimuove un aggancio deve aggiornare questo registro nello stesso commit o nella stessa PR. Anche un nuovo campo letto, nome di controllo, colore usato come stato, formato file o evento costituisce un aggancio.

Per ciascuna riga conservare ID, sorgente/consumatore, contratto atteso, conseguenza di una rottura e controllo di compatibilità. Aggiornare baseline e risultati di verifica a ogni integrazione upstream. Registrare esplicitamente i controlli non eseguiti; una compilazione riuscita non valida lookup per stringa o significato dei dati.

Classificazione del delta: GREEN = componente isolato; YELLOW = hook o dipendenza upstream da verificare; RED = modifica della logica core da evitare e riesaminare. Una lettura interna YELLOW non è una modifica del core. Anche un componente GREEN può dipendere dal contratto di un altro prodotto.

## Agganci client verificati

I percorsi UI sono sotto `JoinFS/FlyLab/UI/`; i form sotto `JoinFS/Forms/`.

| ID | Sorgente → consumatore | Contratto e impatto | Controllo dopo aggiornamento |
| --- | --- | --- | --- |
| J01 | `MainForm.cs` → `FlyLabMainChrome.Attach(this, main)` | YELLOW: controlli già inizializzati e istanza `Main`; creazione shell e timer 250 ms. Cambiare ordine di avvio può rompere il binding. | Aprire/chiudere la finestra, con e senza simulatore; controllare shell e worker ACARS. |
| J02 | Controlli/menu originali → `FlyLabMainChrome.ProxyButton`, `Find`, `FindItem` | YELLOW: ricerca ricorsiva per nome/tipo e `PerformClick()` sui controlli originali. Un nome non trovato disabilita il proxy senza errore di compilazione. Menu/status originali nascosti; `Combo_Join` spostata nella shell. | Provare ogni comando della tabella nomi; verificare selezione hub, CREA e COLLEGATI. |
| J03 | Pulsanti di stato e Settings → `RefreshOperationalState`, `MirrorState` | YELLOW: copia `BackColor`, `ForeColor`, `Enabled`; stato rete dedotto da uguaglianza con `ColourActiveBackground`. ACARS inizializzato con `ColourInactiveBackground/Text`. Dipendenza anche semantica dai colori, non soltanto estetica. | Stati attivo/attesa/inattivo e colori personalizzati; verificare disponibilità radar. |
| J04 | `Main.sim.userAircraft.variableSet` → avionica FlyLab | YELLOW: `VariableMgr.CreateVuid("com active frequency:1")`, `"com active frequency:2"`, `"transponder code:1"`; lettura `GetFrequency` e `GetInteger`, formato F3/D4. Lettura diretta interna, non WebSocket/Whazzup. | Cambiare COM1/COM2 e squawk; controllare unità, decimali, zeri iniziali e disconnessione sim. |
| J05 | `userAircraft.flightPlan.callsign`, `Position.angles.y` → radar | YELLOW: callsign diretto e heading da radianti a gradi. La disponibilità della fascia avionica segue `variableSet != null`. | Cambio callsign e prua, sim assente, caricamento/cambio aereo. |
| J06 | `whazzup.txt` → `FlyLabWhazzupTrafficProvider` → radar | YELLOW: contratto file descritto sotto. Posizione/quota ownship e traffico provengono dal file, heading ownship dall'oggetto interno. Un formato valido ma diverso può dare dati errati senza eccezione. | Due piloti, ownship, quote relative, movimento, file assente e output interrotto; più installazioni JoinFS. |
| J07 | `AircraftForm.cs` → Theme + DetailListChrome | YELLOW: griglia, dettagli, menu, refresh e geometria originale; colori refresh modificati anche nei metodi di aggiornamento. | Lista, selezione, dettagli/FPL, menu, refresh, resize. |
| J08 | `AtcForm.cs` → Theme + ListChrome | YELLOW: refresh riparentato nel footer; griglia ridimensionata; `SetCount(itemList.Count, "ATC")`; colori refresh nei metodi originali. | Conteggio, aggiornamento, menu e resize. |
| J09 | `HubsForm.cs` → Theme + DetailListChrome | YELLOW: due griglie, `ColHubName`, altezza dettagli e refresh; preservare colori operativi hub. | Hub e dettagli, connessione, colori, refresh e resize. |
| J10 | `SessionForm.cs` → Theme + DetailListChrome | YELLOW: lista utenti, chat e refresh; mantenere colori chat configurabili e gestione `NO_COMMS`. | Lista, messaggi, selezione, menu, colori e resize. |
| J11 | `ObjectsForm.cs` → Theme + DetailListChrome | YELLOW: griglia, filtri, sostituzione, refresh. | Lista, filtri, sostituzione, refresh e resize. |
| J12 | `FlightPlanForm.cs` → `FlyLabFlightPlanChrome.Apply` | YELLOW: lookup per nome e riposizionamento runtime dei controlli originali. | Tutti i campi, import SimBrief, Clear, OK/Cancel; controlli mancanti possono sfuggire alla build. |
| J13 | `SettingsForm.cs` → `FlyLabSettingsChrome.Apply` | YELLOW: visita ricorsiva per tipo con eccezioni per nome; preserva campioni dei colori operativi. | Apertura, modifica, salvataggio/annullamento, Reset e campioni colore. |
| J14 | `Program.cs`, `MainForm.cs`, progetto → splash/icone/branding | YELLOW: `FlyLabSplashForm.ShowStartup()` prima di `OpenForms`; icona con fallback originale; titolo sostituisce prefisso `JoinFS-`; ApplicationIcon e risorsa embedded nel csproj. | Avvio e icona EXE/finestra nelle varianti distribuite; verificare guardie `!CONSOLE` e build CONSOLE se coinvolta. |

### Nomi dei controlli Main (J02/J03)

`Main_Menu`, `StatusStrip_Main`, `Menu_View_Session`, `Menu_View_Aircraft`, `Menu_View_Atc`, `Menu_View_Hubs`, `Menu_View_Objects`, `Tool_Map`, `Button_FlightPlan`, `Button_SimBrief`, `Menu_File_Settings`, `Menu_View_Monitor`, `Button_Simulator`, `Button_Network`, `Button_Global`, `Button_Create`, `Combo_Join`, `Button_Join`.

I proxy laterali copiano `Enabled` alla creazione; i tre indicatori vengono aggiornati periodicamente. Verificare anche variazioni di disponibilità successive all'avvio.

### Controlli delle liste (J07–J11)

Tutte usano `Button_Refresh`; modifiche ai suoi colori sono presenti nei form oltre al richiamo iniziale.

| Form | Altri controlli referenziati dalla personalizzazione |
| --- | --- |
| Aircraft | `DataGrid_AircraftList`, `Context_Aircraft`, `Label_Details`, `Label_FlightPlan1/2`, `label1/2/3/4/6` |
| Atc | `DataGrid_AtcList`, `Context_ATC` |
| Hubs | `DataGrid_HubList`, `Context_Hub`, `ColHubName`, `DataGrid_Hub`, `label1/2/3/4` |
| Session | `DataGrid_UserList`, `Context_User`, `Text_Receive`, `Text_Transmit`, `Button_Send`, `Check_Chat`, `Context_Chat`, `label1/2/3` |
| Objects | `DataGrid_ObjectList`, `label1`, `Check_Group`, `Check_ListIgnoredObjects`, `Button_Substitute` |

`FlyLabDetailListChrome` conserva i controlli, aggiunge header e modifica geometria griglia a ogni resize; dipende dallo spazio originario sotto la griglia. `FlyLabListChrome` ATC modifica anche il parent del refresh. Designer invariati non eliminano queste dipendenze di layout.

### Contratto Whazzup (J06)

- Cerca prima Documenti/`JoinFS-FS2020`, `JoinFS-FS2024`, `JoinFS-FSX`, `JoinFS-P3D` o `JoinFS-XPLANE`, secondo compilazione; fallback al `whazzup.txt` più recente sotto cartelle `JoinFS*`.
- Lettura condivisa con writer; sezione `!CLIENTS`, separatore `:`, almeno 10 campi e ruolo `PILOT` in indice 3.
- Indici zero-based: callsign 0; identità pilota 1/2; latitudine 5; longitudine 6; quota 7; velocità 8. Heading: ultimo campo intero non vuoto cercato dall'indice finale fino al 9.
- Ownship riconosciuto dal campo 1 vuoto. Traffico filtrato per prefisso `VOY` nei campi 1/2: attualmente è un'assunzione hardcoded del client, da tenere distinta dall'obiettivo multi-brand del bridge.
- Il parametro `ownshipCallsign` non determina l'identificazione dell'ownship nel parser attuale.
- Verificare impostazione di produzione Whazzup, unità/ordine campi, frequenza di aggiornamento e scelta del file in caso di più installazioni. Non c'è qui una garanzia di freschezza: interrompere il writer è un controllo necessario.

### Nomi FlightPlan e Settings (J12/J13)

FlightPlan: `label1`…`label8`, `Label_Altitude`, `Label_SimBriefStatus`, `Text_Callsign`, `Text_Type`, `Text_From`, `Text_To`, `Text_Route`, `Text_Remarks`, `Text_Altitude`, `Text_SimBriefUsername`, `Combo_Rules`, `Button_ImportSimBrief`, `Button_Clear`, `Button_OK`, `Button_Cancel`.

Settings: eccezioni su `Text_Airport`, `Button_OK`, `Button_Reset`, `Label_Active`, `Label_Waiting`, `Label_Inactive`, `Label_LabelColour`. I restanti controlli sono trattati per tipo.

## Dipendenze esterne e funzionalità non ancora collegate

| ID | Componente/contratto verificato | Stato e controllo |
| --- | --- | --- |
| E01 | `JoinFS/FlyLab/Integration/VaBaseMonitor.cs`: processo `vaBaseLive`, handle finestra e UI Automation `lblStage`; consumato da `RefreshOperationalState` | Attivo nel codice per testo fase sul radar. Verificare VaBase assente/presente, lblStage mancante, transizioni. Dipende da VaBase/Windows, non dalla lettura radio JoinFS. |
| E02 | `WhaAcarsProducer.cs`: worker `powershell.exe`, UIA `lblStage`, `btnStartFlight`, `btnAbortFlight`; `whaacars.json` accanto all'app | Producer avviato dalla shell e fermato alla chiusura; output su cambio stato con temporaneo e sostituzione file, polling 500 ms. JSON: source, available, status, flightStage, startFlightFound/Enabled, abortFlightFound/Enabled, sequence, timestamp. Stati NO_ACARS/RUNNING/NOT_STARTED/UNKNOWN/ERROR. Abort abilitato ha precedenza. Verificare directory scrivibile, ciclo di vita worker e transizioni. Il JSON non è ancora consumato dalla UI. |
| E03 | Indicatore ACARS e pulsanti COMMS/ACARS/TOOLS | Indicatore ACARS ancora non collegato allo stato; pulsanti laterali placeholder disabilitati. Nessun monitor TeamSpeak attivo trovato nello strato FlyLab esaminato. Non registrarli come integrazioni operative. |
| E04 | Branch radio: `integrations/voy-radio-bridge/VoyRadioBridge.ps1` riceve HTTP JSON `comsupdate[]` | Consuma `nickname` e `com1`, normalizza virgola in punto. Confronto esatto nickname TeamSpeak; frequenza associata al Topic canale. `callsign` non è identità TS; COM2 non pilotata da questo script. Verificare payload reale della CONSOLE prima dell'upgrade e versione installata. |
| E05 | Stesso bridge → TeamSpeak ServerQuery | `clientlist`, `channellist -topic`, `clientmove clid=... cid=...`; configurazione e credenziali separate. Verificare identità, permessi, Topic e cambio canale con utente di test. Nessun plugin TS richiesto da questa implementazione. |

Architettura bridge verificata: [ARCHITECTURE.md sul branch radio](https://github.com/Ireca737/VoyFS/blob/6336d5c028a114243947c56c85e3537c4e6496fc/integrations/voy-radio-bridge/docs/ARCHITECTURE.md). Il documento ACARS del 6 ottobre in questa cartella conserva stato/proposte precedenti: per distinguere implementato e pianificato usare la baseline del presente registro e il codice.

## Procedura di verifica a ogni upgrade

1. Fissare commit/tag FlyLab funzionante, pacchetto e configurazioni di ripristino; scegliere commit upstream preciso.
2. Ricalcolare delta FlyLab e delta upstream dalla base comune; riesaminare J01–J14 ed E01–E05 quando coinvolti. Censire nuove dipendenze anche nei file FlyLab aggiunti.
3. Integrare in branch temporaneo; conservare logica upstream e adattare i soli collegamenti necessari. Non risolvere conflitti accettando indiscriminatamente un lato.
4. Controllare modifiche ai Designer anche se FlyLab non li modifica, lookup per nome, colori/stati e formati esterni.
5. Compilare le varianti da distribuire. Il workflow corrente copre FS2020/FS2024/FSX, non tutte le varianti e non i test runtime; un branch `integration/**` non attiva attualmente il trigger push `feat/**` (usare esecuzione manuale o procedura equivalente).
6. Eseguire i controlli delle righe interessate e una prova base avvio/connessione/riconnessione, grafica, avionica e traffico. Collaudare compatibilità con client del gruppo; CONSOLE e RadioBridge hanno verifica distinta dal client.
7. Registrare esito, ambiente e controlli non eseguiti; beta prima della distribuzione generale. Aggiornare baseline e registro insieme alla modifica.

## Registro verifiche

| Data | Revisione | Attività | Esito / limiti |
| --- | --- | --- | --- |
| 2026-10-08 | Client `2dc3e56`, base `2d9913d`, bridge `6336d5c` | Inventario sorgenti, diff, lookup e contratti | Verificato staticamente; nessun test Windows, volo, server o compatibilità con un nuovo upstream eseguito. |
