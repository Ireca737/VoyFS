# FlyLabFS — Studio Avalonia

Decisione del 22 ottobre 2026: congelare lo sviluppo delle personalizzazioni WinForms, con obiettivo di chiusura del client sulla release JoinFS 26.6.0, e studiare la migrazione ad Avalonia su un ramo separato.

## Riferimenti verificati

- Snapshot WinForms: branch `baseline/flylab-winforms-freeze-2026-10-22`, commit `e6f0c2e87c09b793d1d4d395f109a9ea46fa5963`, copiato da `feat/flylab-ui` senza modifiche.
- Release upstream 26.6: commit `a39f5cd8c2ee67635ebda636c4d9fd5d21f6b6f1`.
- Il merge-base tra lo snapshot FlyLab e v26.6 è `2d9913d205e2f48dd4458337858a349624cdc3fc`; 26 commit raggiungibili da v26.6 non sono nello snapshot. Non sono necessariamente 26 modifiche da applicare singolarmente.
- Lo snapshot NON costituisce una release FlyLab certificata sulla base ufficiale 26.6.0. Prima della chiusura finale occorre riconciliare sorgenti, pacchetto effettivamente usato e modifiche upstream necessarie. Installare il client originale 26.6 non aggiorna il fork.
- Ramo di studio: `experiment/flylab-avalonia`, creato dal commit upstream `aeaff8dd4b1a0e4a11a2c17641700c3e00d2ea31`. Base fissata, non riferimento mobile a main.

## Regole architetturali

Core Tuduce: nessuna personalizzazione funzionale FlyLab nei sottosistemi di rete, simulatore o funzioni native. Personalizzazioni FlyLab nell'interfaccia Avalonia e nei moduli esterni, con adattatori piccoli e documentati. Privilegiare file e componenti propri; registrare ogni modifica inevitabile ai punti di avvio o composizione upstream.

Valutare i servizi di JoinFS.UI e gli adattatori JoinFS/Live; non considerarli API stabili garantite. Conservare identità grafica, comandi e comportamento FlyLab, verificando la copertura delle interfacce prima del porting. Non trasferire in blocco i vecchi form nel nuovo ramo.

## Sequenza dello studio

1. Compilare ed eseguire la UI upstream senza personalizzazioni; distinguere il launcher con dati fittizi dal client reale.
2. Leggere il registro degli agganci conservato nello snapshot: docs/flylab-joinfs-integration-register.md. Mappare ciascun aggancio ai servizi disponibili, rilevando le lacune.
3. Prototipare shell e tema FlyLab in componenti separati.
4. Trasferire progressivamente radar/Whazzup, COM/XPDR e indicatori/widget esterni; verificare ogni collegamento.
5. Confrontare comportamento e grafica con il client WinForms approvato prima di una distribuzione beta.

La roadmap storica resta nello snapshot: docs/flylab-roadmap.md. Gli stati operativi di ACARS e RadioBridge devono essere riconciliati con le rispettive attività dedicate; questo studio non li dichiara risolti né avvia diagnosi parallele.

## Stato iniziale

Creati i due riferimenti Git e questo documento. Nessun porting, merge del core sul ramo WinForms, build o test Windows eseguito. Nessun installer o configurazione runtime archiviato in questa attività. Il branch baseline è un riferimento convenzionalmente congelato, non un tag immutabile né un branch protetto.
