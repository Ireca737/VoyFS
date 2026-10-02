# Architecture

The Radio Bridge is intentionally external to the JoinFS and TeamSpeak cores.

```text
Simulator
  -> JoinFS Client
  -> FlyLab JoinFS Hub
  -> HTTP COM webhook (127.0.0.1:8787)
  -> VoyRadioBridge.ps1
  -> TeamSpeak ServerQuery (127.0.0.1:10011)
  -> clientmove
  -> TeamSpeak channel whose Topic equals COM1
```

The hub receives simulator state from JoinFS clients, including remote WAN clients. The webhook itself remains local to the FlyLab server.

Identity is based on JoinFS `nickname` matching the TeamSpeak nickname. Aircraft callsign/tail number is deliberately independent.

This design preserves the zero-core-modification constraint for both JoinFS and TeamSpeak.
