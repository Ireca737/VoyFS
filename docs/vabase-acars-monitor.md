# FlyLabFS — VaBase ACARS Status Monitor

**Status:** historical investigation (6 October); implementation has progressed — see current status below  
**Branch:** `feat/flylab-ui`  
**Date:** 2026-10-06

## Status reconciliation — 8 October 2026

This document preserves the original investigation and proposed design. It is not the current implementation checklist. See [FlyLab roadmap, C5–C7](flylab-roadmap.md) for the reconciled status: the PowerShell bench transition was confirmed; an embedded producer exists, but its equivalent runtime validation remains open in the recovered evidence; the ACARS indicator/JSON consumer is not yet connected. The live stage reader and the embedded JSON producer are separate paths. Do not treat the historical “next validation steps” below as wholly unimplemented or already completed.

## Objective

Add a non-invasive ACARS status indicator to the FlyLabFS Main Form without modifying VaBase, JoinFS core, TeamSpeak, or simulator integration.

The operational requirement is a three-state indicator:

- **OFF** — VaBase is not running.
- **READY** — VaBase is running and ACARS is available/pre-flight, but the flight session has not been started.
- **RUNNING** — the VaBase flight session is active.

The optional warning logic is separate from state detection:

- OFF: optical indication only.
- READY while aircraft is stationary: no warning.
- READY + aircraft moving + warning enabled: visual warning and intermittent chime.
- READY → RUNNING: optional short confirmation chime.

`AIRCRAFT_MOVING` must never determine the ACARS state; it is used only for warning logic.

## Reverse-engineering results

VaBase was treated as a black box. No modification, injection, memory scraping, or HTTPS decryption was required.

### Process detection

The active client process is:

```text
vaBaseLive.exe
```

Therefore:

```text
vaBaseLive absent  -> OFF
vaBaseLive present -> inspect VaBase UI state
```

Process presence alone is not sufficient to distinguish READY from RUNNING.

## VaBase state machine observations

Observed stages during testing:

```text
READY / client available
        |
        | FSUIPC becomes available
        v
PRE-FLIGHT
        |
        | Start Flight
        v
BOARDING
        |
        | aircraft movement detected
        v
TAXIING
```

Important distinction:

- FSUIPC provides simulator data.
- VaBase decides the internal flight stage.
- Flight stage changes are therefore VaBase state-machine decisions, even when triggered by external simulator conditions.

## UI Automation discovery

Windows UI Automation exposes the current VaBase flight stage directly.

Validated control:

```text
AutomationId : lblStage
ClassName    : Text
ControlType  : ControlType.Text
Name         : Taxiing   # example observed value
```

Direct PowerShell test:

```powershell
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$proc = Get-Process vaBaseLive -ErrorAction Stop

$root = [System.Windows.Automation.AutomationElement]::FromHandle(
    $proc.MainWindowHandle
)

$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
    "lblStage"
)

$stage = $root.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    $cond
)

$stage.Current.Name
```

Validated result:

```text
Taxiing
```

This gives FlyLabFS a direct, local, non-invasive status source with no dependency on the VaBase backend or network timing.

## Flight Log discovery

The VaBase Flight Log is also exposed through Windows UI Automation.

Validated control:

```text
AutomationId : txtMsg
ClassName    : TextBox
ControlType  : ControlType.Edit
Patterns     :
  - ValuePattern
  - ScrollPattern
  - TextPattern
```

The full live Flight Log can be read through `TextPattern`:

```powershell
$tp = $log.GetCurrentPattern(
    [System.Windows.Automation.TextPattern]::Pattern
)

$tp.DocumentRange.GetText(-1)
```

Observed entries include:

```text
FLAPS UP
FLAPS 1
FLAPS 2
Landing lights ON
Landing lights OFF
Starting engine(s)
Engine(s) shutdown
Gear UP / DOWN
Detected aircraft taxiing
Aircraft boarding in progress
Your flight to ... has now been started.
You have successfully logged in ...
```

This confirms that VaBase exposes not only raw simulator-dependent information but also semantic events already processed by its internal event/state engine.

## Network findings

While an active VaBase flight session was running, ProcMon showed short-lived HTTPS transactions toward:

```text
euk-88811.eukservers.com:443
```

A regular cycle of approximately 10 seconds was observed.

Testing with flap changes showed:

- Flight Log updates immediately.
- No dedicated TCP transaction is created at the instant of the flap event.
- The regular HTTPS loop continues independently.

Working model:

```text
Simulator / FSUIPC
        |
        v
VaBase event/state engine
        |
        +------> Flight Log GUI       (immediate)
        |
        +------> Flight Stage         (immediate)
        |
        +------> backend publisher    (~10 s periodic cycle)
```

Therefore the Flight Log / Flight Stage path and the backend telemetry loop are parallel mechanisms.

The network heartbeat is useful for diagnostics, but it is not the preferred primary signal for the FlyLabFS ACARS indicator.

## Proposed FlyLabFS implementation

Keep the detector logic outside the Main Form.

Suggested component:

```text
VaBaseMonitor
   |- IsProcessRunning()
   |- ReadStage()
   |- ReadFlightLog()
   '- GetAcarsState()
```

Suggested state enum:

```csharp
enum AcarsState
{
    Off,
    Ready,
    Running,
    Unknown
}
```

`Unknown` is important: if `vaBaseLive.exe` exists but UI Automation temporarily cannot resolve `lblStage`, FlyLabFS must not incorrectly report OFF or READY.

### Mapping strategy

Initial mapping:

```text
vaBaseLive absent
    -> OFF

vaBaseLive present
+ lblStage == "Pre-flight"
    -> READY

vaBaseLive present
+ lblStage is a known active flight stage
    -> RUNNING

vaBaseLive present
+ lblStage unavailable / unexpected
    -> UNKNOWN
```

Active stages must be mapped explicitly after completing a short stage-validation flight. Do not implement RUNNING as simply “anything except Pre-flight” until all normal VaBase stages have been observed.

## Main Form integration

The Main Form should only consume `AcarsState` and render the appropriate visual state.

Conceptually:

```text
VaBaseMonitor
      |
      v
 AcarsState
      |
      v
 Main Form indicator
```

The UI must not contain the VaBase discovery logic itself.

Settings requirement:

```text
Avvisa stato ACARS: ON / OFF
```

This setting controls warnings only; it must not disable status detection.

Warning logic:

```text
READY + AIRCRAFT_MOVING + WarningEnabled
    -> visual warning + intermittent chime

READY -> RUNNING
    -> optional short confirmation chime
```

## Architectural lesson for future FlyLab ACARS

The VaBase investigation reveals a useful separation of responsibilities:

```text
raw simulator data
        |
        v
event/state engine
        |
        +--> semantic events
        +--> flight state machine
        '--> telemetry publisher
```

For a future native FlyLab ACARS, the preferred equivalent is:

```text
JoinFS / Whazzup
        |
        v
FlyLab State Reader
        |
        v
FlyLab Event / State Engine
        |
        +--> semantic events
        +--> ACARS state machine
        '--> telemetry publisher
```

Architectural principle:

> Observe first, integrate second, modify never unless unavoidable.

For future FlyLab development, prefer existing JoinFS/Whazzup data before adding direct simulator integrations.

## Next validation steps

1. Abort the current VaBase flight and confirm `lblStage == Pre-flight`.
2. Start Flight and confirm immediate `Pre-flight -> Boarding`.
3. Perform a short complete flight and record all actual VaBase stage names.
4. Freeze the explicit READY/RUNNING mapping.
5. Implement `VaBaseMonitor` in FlyLabFS.
6. Integrate the three-state ACARS indicator into the Main Form.
7. Keep `txtMsg` support available for diagnostics and future event-recorder research.

## Current conclusion

For the operational FlyLabFS ACARS indicator, the problem is effectively solved at the sensing level:

- process detection provides **OFF**;
- UI Automation `lblStage` provides **READY/RUNNING** discrimination;
- `txtMsg` exposes the full VaBase semantic Flight Log for research and diagnostics.

No modification of VaBase or JoinFS core is required.
