# Architecture

The Radio Bridge is intentionally external to the JoinFS and TeamSpeak cores.

## Architectural invariants

### 1. Zero Core Modification

FlyLab integrations must remain external to the JoinFS and TeamSpeak cores.

JoinFS and TeamSpeak must remain independently updateable or replaceable without requiring FlyLab-specific modifications to their core code. Integration logic belongs in external FlyLab components and configuration.

### 2. Separation of Engine and Deployment

FlyLabFS and its reusable components are the engine. They must not contain structural dependencies on a specific community, organization, or deployment.

VOY is a deployment/configuration of FlyLabFS, not the identity of the reusable engine. VOY-specific names, TeamSpeak ServerQuery accounts, credentials, servers, channels, permission groups, and other deployment-specific settings may retain the VOY identity as long as they remain configurable and are not embedded as assumptions in the engine logic.

The same FlyLab component must be able to support multiple independent deployments, including deployments running in parallel on the same server, by using separate configuration and identities where required.

In practical terms:

```text
FlyLabFS / reusable FlyLab component
  -> deployment configuration
      -> VOYRadioBridge / VOY TeamSpeak environment

  -> another deployment configuration
      -> AnotherRadioBridge / another TeamSpeak environment
```

A deployment-specific identifier such as `VOYRadioBridge` is therefore valid when it identifies the VOY instance. It must not become a requirement of the reusable FlyLab engine.

## Current Radio Bridge flow

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

The two invariants above govern future development of the Radio Bridge, including COM2 support, configuration, service orchestration, and future user interfaces.
