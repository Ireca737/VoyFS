#if !CONSOLE
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace JoinFS.FlyLab.Integration
{
    /// <summary>
    /// Embedded FlyLab WhaACARS producer.
    /// The PowerShell UIA probe is hosted/started by FlyLabFS because that is the
    /// UI Automation path proven to expose VaBase controls reliably on the test system.
    /// No external .ps1 file or user action is required.
    /// </summary>
    internal sealed class WhaAcarsProducer : IDisposable
    {
        private Process worker;

        internal string OutputPath { get; } =
            Path.Combine(AppContext.BaseDirectory, "whaacars.json");

        internal void Start()
        {
            if (worker != null && !worker.HasExited)
                return;

            string encoded = Convert.ToBase64String(
                Encoding.Unicode.GetBytes(BuildScript()));

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoLogo -NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand " + encoded,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.Environment["WHAACARS_OUTPUT"] = OutputPath;
            startInfo.Environment["WHAACARS_PARENT_PID"] =
                Process.GetCurrentProcess().Id.ToString();

            worker = Process.Start(startInfo);
        }

        public void Dispose()
        {
            try
            {
                if (worker != null && !worker.HasExited)
                    worker.Kill();
            }
            catch
            {
                // FlyLab shutdown must never be blocked by the observer.
            }
            finally
            {
                worker?.Dispose();
                worker = null;
            }
        }

        private static string BuildScript()
        {
            return @"
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$OutputFile = $env:WHAACARS_OUTPUT
$ParentPid = [int]$env:WHAACARS_PARENT_PID
$Sequence = 0
$LastSignature = $null

function Find-ById($Root, $Id) {
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        $Id
    )

    $Root.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants,
        $cond
    )
}

while ($true) {
    if ($null -eq (Get-Process -Id $ParentPid -ErrorAction SilentlyContinue)) { break }

    $available = $false
    $status = 'NO_ACARS'
    $flightStage = 'NO ACARS'

    $startFound = $false
    $startEnabled = $false
    $abortFound = $false
    $abortEnabled = $false

    try {
        $p = Get-Process vaBaseLive -ErrorAction SilentlyContinue |
             Where-Object { $_.MainWindowHandle -ne 0 } |
             Select-Object -First 1

        if ($p) {
            $available = $true

            $root = [System.Windows.Automation.AutomationElement]::FromHandle(
                $p.MainWindowHandle
            )

            $stage = Find-ById $root 'lblStage'
            if ($stage) {
                $flightStage = $stage.Current.Name
            }
            else {
                $flightStage = 'UNKNOWN'
            }

            $start = Find-ById $root 'btnStartFlight'
            if ($start) {
                $startFound = $true
                $startEnabled = $start.Current.IsEnabled
            }

            $abort = Find-ById $root 'btnAbortFlight'
            if ($abort) {
                $abortFound = $true
                $abortEnabled = $abort.Current.IsEnabled
            }

            # WhaACARS state machine V1 - certified on the PowerShell test bench.
            # Abort has priority during VaBase's short Start Flight UI transition.
            if ($abortFound -and $abortEnabled) {
                $status = 'RUNNING'
            }
            elseif ($startFound -and $startEnabled) {
                $status = 'NOT_STARTED'
            }
            else {
                $status = 'UNKNOWN'
            }
        }
    }
    catch {
        $status = 'ERROR'
    }

    $signature = @(
        $available
        $status
        $flightStage
        $startFound
        $startEnabled
        $abortFound
        $abortEnabled
    ) -join '|'

    if ($signature -ne $LastSignature) {
        $Sequence++

        $data = [ordered]@{
            source = 'VaBase'
            available = $available
            status = $status
            flightStage = $flightStage
            startFlightFound = $startFound
            startFlightEnabled = $startEnabled
            abortFlightFound = $abortFound
            abortFlightEnabled = $abortEnabled
            sequence = $Sequence
            timestamp = (Get-Date).ToString('o')
        }

        $json = $data | ConvertTo-Json
        $temp = $OutputFile + '.tmp'
        $json | Set-Content -LiteralPath $temp -Encoding UTF8
        Move-Item -LiteralPath $temp -Destination $OutputFile -Force

        $LastSignature = $signature
    }

    Start-Sleep -Milliseconds 500
}
";
        }
    }
}
#endif
