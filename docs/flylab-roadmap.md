# FlyLabFS — Roadmap e stato del progetto

Aggiornamento: 8 ottobre 2026. Prima riconciliazione tra pianificazione delle chat, codice e documentazione Git. Le voci «da verificare» non sono nuovi difetti accertati.

## Perimetro e fonti

Questa roadmap consolida le fasi già discusse; non crea una nuova serie di milestone GitHub. L'esistenza di oggetti Milestone nativi GitHub non è stata verificata: il connettore non esponeva l'endpoint richiesto.

Fonti Git lette:
- Client `feat/flylab-ui`: `6962543eb84f3f293658dd0254c393e9da45d174` (codice applicativo ultimo aggiornato in `2dc3e56`).
- Base `main`: `2d9913d205e2f48dd4458337858a349624cdc3fc`.
- `baseline/l2-ownship-avionics-stable`: `fe6dcad6b8ab9a29aa612b41e9ab91379ec122af`.
- `release/flylab-beta-pre-acars`: `3b71a60a62a248454060a9df7364863a4f90fdad`. Il branch è un riferimento sorgente; non dimostra da solo quale pacchetto sia stato distribuito.
- Radio `feat/voy-radio-bridge`: `6336d5c028a114243947c56c85e3537c4e6496fc`.

Fonti conversazionali recuperate: «Programmare ripristino server» (30 settembre), «Progettare la grafica FlyLab», «collegamento a VaBase ACARS», «Diagnosi disallineamento hub», «Test A2 RadioBridge», «Integrazione JoinFS TeamSpeak». Le conferme di test riportate dalle chat sono storiche: non sono nuovi test eseguiti durante questa ricostruzione.

Stati: **completato** = risultato documentato nel perimetro indicato; **in corso** = lavoro/diagnosi aperti; **da validare** = implementazione presente, esito operativo mancante; **pianificato** = passo della roadmap non attestato come completato; **da verificare** = informazioni insufficienti o da riallineare. Gli ID C/S qui sotto servono al raccordo documentale, senza attribuire nuovi nomi alle vecchie milestone.

## Client, interfaccia e ACARS

| ID / traguardo | Stato | Evidenza | Prossimo passo / criterio di chiusura |
| --- | --- | --- | --- |
| C1 — Styling e shell FlyLab | Completato nel perimetro grafico approvato | Approvazione utente dello styling nella chat grafica; classi FlyLab e hook nei form presenti nel codice | Conservare come base; aggiornare il registro se si cambia un aggancio. L'approvazione grafica non certifica tutte le varianti. |
| C2 — L2 ownship/avionica | Baseline Git presente; collaudo per variante da verificare | Branch `baseline/l2-ownship-avionics-stable`; COM1/COM2/XPDR e heading letti dagli oggetti JoinFS | Collegare il pacchetto e l'ambiente effettivamente validati alla baseline. |
| C3 — Traffico/TCAS Whazzup | Implementato; conferma finale del collaudo da verificare | Provider Whazzup, range 2–40 NM e simbologia nel codice; commit `e035f30` heading-up e successivo ritiro phantom | Non riaprire automaticamente il vecchio problema phantom: cercare/confermare esito finale dopo le correzioni. |
| C4 — Release beta precedente ad ACARS | Riferimento Git presente | Branch `release/flylab-beta-pre-acars`, inclusa icona EXE FlyLab | Associare nome/versione del pacchetto distribuito e varianti provate; non equiparare il branch a una release pubblicata. |
| C5 — WhaACARS V1 al banco PowerShell | Completato per il test documentato | Conferma utente: `NOT_STARTED → RUNNING → NOT_STARTED` con Start/Abort e scrittura su cambio stato | Preservare questo comportamento come riferimento per la prova embedded. |
| C6 — WhaACARS embedded | In corso / da validare | Producer integrato (`5652409`, `d9ce604`, `93fd4a7`), diagnostica `8bb337f`; nelle ultime conferme recuperate processo rilevato ma controlli UIA non trovati | Dimostrare la stessa sequenza del banco avviando FlyLabFS; verificare JSON, sequence e arresto worker. Non chiudere in base al nome “certified” del commit. |
| C7 — Consumer/indicatore ACARS | Pianificato, collegamento non completato | Indicatore ancora scollegato; testo fase usa VaBaseMonitor; JSON del producer non consumato dalla UI alla baseline | Dopo C6, collegare lo stato alla UI secondo le decisioni della chat ACARS. Opzione avvisi e suoni restano requisiti, non funzionalità già validate. |
| C8 — Base FlyLabFS consolidata | Traguardo dopo completamento ACARS | Decisione utente in questa chat | Tag, pacchetto, configurazioni e inventario coerenti; evoluzioni successive come widget separati e manutenzione upstream. |

Il documento [VaBase ACARS](vabase-acars-monitor.md) conserva l'indagine iniziale; la distinzione banco/embedded/consumer in questa tabella prevale sulle vecchie diciture generiche “implementation pending”.

## Server e connettività

Roadmap originale del 30 settembre: fotografia configurazione → riproduzione difetto → avvio pulito → isolamento differenze → confronto CONSOLE/client HUB → ripristino e consolidamento. Non risultano evidenze sufficienti per assegnare una chiusura individuale a tutte le sei fasi.

| ID / attività | Stato | Evidenza | Prossimo passo |
| --- | --- | --- | --- |
| S1 — Hub arancione/disallineamento | In corso; causa da determinare | Segnalazioni e prove nella chat diagnostica; anche serata riuscita con otto utenti e client misti, riferita dall'utente | Raccogliere un episodio riproducibile e confrontare stato del processo, listener e raggiungibilità. Il successo di una serata non chiude il difetto intermittente. |
| S2 — Affidabilità dello script PS1 di controllo hub | Da validare, priorità diagnostica indicata dall'utente | Richiesta esplicita: verificare se “Tuduce ferma” corrisponde alla CONSOLE realmente ferma | Confrontare esito script con processo/porta e prove indipendenti; acquisire versione esatta dello script. Non modificare la CONSOLE per compensare un possibile falso allarme. |
| S3 — Client misti / interoperabilità | Successi osservati; copertura limitata | Utente riferisce client legacy 3.17, Tuduce e FlyLab collegati; solo due tester FlyLab nel quadro più recente | Registrare versioni effettive dai binari/log (nelle chat compaiono numerazioni discordanti), ambiente e sintomi. Non attribuire il problema ai client sulla sola versione. |
| S4 — Consolidamento server | Da verificare | Avvii e test storici disponibili nelle chat; manca qui una fotografia attestata dell'installazione attuale | Collegare configurazione e pacchetto realmente in uso ai risultati di S1/S2, mantenendo separati aggiornamento server e client. |

## Radio Integration — fasi originali F1–F6

Il [README del POC radio](https://github.com/Ireca737/VoyFS/blob/6336d5c028a114243947c56c85e3537c4e6496fc/integrations/voy-radio-bridge/README.md) documenta un test end-to-end POC 0.1 il 2 ottobre, COM1 su 121.500/122.800/123.200. Questo risultato non chiude automaticamente le successive verifiche di stabilità WAN.

| Fase | Stato ricostruito | Evidenza / prossimo passo |
| --- | --- | --- |
| F1 — Diagnostica A1 → A2, webhook e WAN | A1 documentata nella chat; A2 da verificare | Baseline A1: CONSOLE, UDP 6112, WebSocket 8765, TS Query 10011 e RadioBridge 8787 attivi; webhook COM OFF; WAN OK. Recuperare esito A2 prima di dichiarare validato l'insieme con webhook attivo. |
| F2 — Confronto webhook / Whazzup | Pianificato; esito non recuperato | Il POC usa webhook. La lettura Whazzup del radar client non dimostra che il confronto per la radio sia concluso. |
| F3 — Radio State Model | Pianificato; chiusura non attestata | Definire/confermare il modello sulla base del percorso scelto in F2; non introdurre modifiche al core. |
| F4 — TeamSpeak Adapter | POC COM1 documentato; consolidamento da verificare | Script con nickname, Topic e clientmove. README indica reconnect, già-nel-canale, logging e orchestrazione come follow-up; non sono nuovi lavori autorizzati da questo documento. |
| F5 — Integrazione COMMS in FlyLabFS | Pianificato | Comando COMMS ancora placeholder nella baseline client. |
| F6 — COM2 | Pianificato dopo i passi precedenti | Script verificato gestisce COM1; nessuna implementazione di ascolto simultaneo COM1/COM2 attestata. |

## Come decidere un aggiornamento Tuduce

1. **Necessità:** quale voce concreta di questa roadmap risolve o abilita la modifica? Valutare beneficio, evidenze e compatibilità; esiti ammessi: adottare, rinviare, restare sulla base corrente.
2. **Contenuto e percorso:** fissare revisione/commit e dipendenze; scegliere aggiornamento ordinario, correzione mirata oppure migrazione a nuova base.
3. **Esecuzione:** salvare riferimento funzionante e pacchetto/configurazioni; fetch e analisi, poi integrazione in branch di prova; usare il [registro degli agganci](flylab-joinfs-integration-register.md).
4. **Verifica e rilascio:** prove mirate FlyLab, prova generale e beta; annotare ambienti e risultati. Client e CONSOLE hanno decisioni di rilascio separate.

Non è stata selezionata né incorporata una nuova revisione upstream in questa attività. Eventuali PR nominate nelle vecchie chat vanno rilette nel loro stato corrente prima di considerarle candidate; non sono qui presentate come correzioni confermate.

### Confine di responsabilità

FlyLab mantiene il proprio codice, i collegamenti e la configurazione che usa. Le implementazioni native JoinFS, SimConnect e TeamSpeak restano responsabilità dei rispettivi progetti. RadioBridge resta una dipendenza da osservare secondo le interfacce/istruzioni upstream: una prova di funzionamento non attribuisce a FlyLab l'audit o la manutenzione dei core. Il riallineamento documentale non autorizza modifiche funzionali al bridge.

## Manutenzione e decisioni ancora da confermare

Aggiornare questa roadmap quando una prova cambia lo stato di una voce, specificando commit/pacchetto, ambiente, data e risultato. Aggiornare il registro tecnico quando cambia un aggancio. Non creare una seconda lista concorrente dei problemi.

Restano da confermare solo questi punti operativi:
- C6: esiste un test embedded successivo con esito positivo?
- F1: il test A2 con webhook attivo è stato eseguito e con quale esito?
- S2: quale script/versione di controllo hub è in uso e risulta validato?
- C2/C3/C4: quali pacchetti/varianti hanno ricevuto l'ultima approvazione operativa?

Queste domande delimitano lacune documentali; non affermano che le funzioni siano guaste.

## Storico

- 2026-10-08: prima riconciliazione; lettura codice/documenti e recupero decisioni delle chat. Nessun nuovo test Windows, volo o server. Nessuna modifica applicativa.
