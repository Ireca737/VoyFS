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
$LastKnownLog = ''

function Find-ById($Root, [string]$Id) {
    if ($null -eq $Root) { return $null }
    $c = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $Id)
    return $Root.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants, $c)
}

while ($true) {
    if ($null -eq (Get-Process -Id $ParentPid -ErrorAction SilentlyContinue)) { break }

    $available = $false
    $status = 'NO_ACARS'
    $flightStage = 'NO ACARS'
    $startEnabled = $false
    $abortEnabled = $false
    $processFound = $false
    $mainWindowHandle = 0
    $rootFound = $false
    $stageFound = $false
    $startFound = $false
    $abortFound = $false
    $uiaError = ''

    try {
        $p = Get-Process vaBaseLive -ErrorAction SilentlyContinue |
             Where-Object { $_.MainWindowHandle -ne 0 } |
             Select-Object -First 1

        if ($p) {
            $available = $true
            $processFound = $true
            $mainWindowHandle = [int64]$p.MainWindowHandle
            $root = [System.Windows.Automation.AutomationElement]::FromHandle(
                $p.MainWindowHandle)
            $rootFound = ($null -ne $root)

            $stage = Find-ById $root 'lblStage'
            $stageFound = ($null -ne $stage)
            if ($stage -and -not [string]::IsNullOrWhiteSpace($stage.Current.Name)) {
                $flightStage = $stage.Current.Name.Trim()
            }

            $start = Find-ById $root 'btnStartFlight'
            $startFound = ($null -ne $start)
            if ($start) { $startEnabled = $start.Current.IsEnabled }

            $abort = Find-ById $root 'btnAbortFlight'
            $abortFound = ($null -ne $abort)
            if ($abort) { $abortEnabled = $abort.Current.IsEnabled }

            if ($abortEnabled -and -not $startEnabled) {
                $status = 'RUNNING'
            }
            elseif ($startEnabled -and -not $abortEnabled) {
                $status = 'NOT_STARTED'
            }
            else {
                $status = 'UNKNOWN'
            }

            # txtMsg is exposed by VaBase UIA only while Flight Log is materialized.
            # Keep the last actually observed entry when the tab is not available.
            $log = Find-ById $root 'txtMsg'
            if ($log) {
                try {
                    $tp = $log.GetCurrentPattern(
                        [System.Windows.Automation.TextPattern]::Pattern)
                    $fullLog = $tp.DocumentRange.GetText(-1)
                    if (-not [string]::IsNullOrWhiteSpace($fullLog)) {
                        $lines = $fullLog -split ""`r?`n"" |
                            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
                        if ($lines.Count -gt 0) {
                            $LastKnownLog = $lines[0].Trim()
                        }
                    }
                } catch {}
            }
        }
        else {
            $LastKnownLog = ''
        }
    } catch {
        $uiaError = $_.Exception.GetType().FullName + ': ' + $_.Exception.Message
    }

    $signature = @(
        $available, $status, $flightStage, $LastKnownLog,
        $startEnabled, $abortEnabled, $processFound, $mainWindowHandle,
        $rootFound, $stageFound, $startFound, $abortFound, $uiaError) -join '|'

    if ($signature -ne $LastSignature) {
        $Sequence++

        $data = [ordered]@{
            source = 'VaBase'
            available = $available
            status = $status
            flightStage = $flightStage
            lastLog = $LastKnownLog
            startFlightEnabled = $startEnabled
            abortFlightEnabled = $abortEnabled
            diagnostic = [ordered]@{
                processFound = $processFound
                mainWindowHandle = $mainWindowHandle
                rootFound = $rootFound
                lblStageFound = $stageFound
                btnStartFlightFound = $startFound
                btnAbortFlightFound = $abortFound
                uiaError = $uiaError
            }
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
