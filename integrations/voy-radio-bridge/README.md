# VOY Radio Bridge

External FlyLab/VoyFS integration that maps JoinFS COM1 state to TeamSpeak channels through ServerQuery.

## Verified baseline

POC 0.1 was validated end-to-end on 2 October 2026:

SIM / cockpit -> JoinFS Client -> FlyLab JoinFS Hub -> COM webhook -> VOY Radio Bridge -> TeamSpeak ServerQuery -> clientmove.

No JoinFS core modifications, TeamSpeak core modifications, or TeamSpeak client plugins are required.

## Matching rules

- JoinFS `nickname` must equal the TeamSpeak nickname.
- The aircraft `callsign` is not used for identity.
- COM1 is normalized from comma decimal notation (for example `122,800`) to dot notation (`122.800`).
- A TeamSpeak channel is selected when its Topic exactly matches the normalized frequency.

Verified mappings included 121.500, 122.800 and 123.200.

## Local configuration

Copy:

- `config/radio-bridge.example.json` to `config/radio-bridge.json`
- `config/credentials.example.json` to `config/credentials.json`

The real files are ignored by Git. Never commit ServerQuery credentials.

## TeamSpeak least-privilege account

The validated dedicated query login is `VOYRadioBridge`. The dedicated ServerQuery group needs:

- `b_virtualserver_select = 1`
- `b_virtualserver_channel_list = 1`
- `b_virtualserver_client_list = 1`
- `b_channel_join_permanent = 1`
- `i_client_move_power = 100` for the currently validated VOY permission model

The required move power must be at least the target client's `i_client_needed_move_power`.

## Current scope

POC 0.1 handles COM1 only. COM2, reconnect handling, already-in-channel handling, structured logging and service orchestration are follow-up work.
