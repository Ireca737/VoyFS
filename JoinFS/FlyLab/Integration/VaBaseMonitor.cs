#if !CONSOLE
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Automation;

namespace JoinFS.FlyLab.Integration
{
    /// <summary>
    /// Read-only observer for the VaBase desktop client.
    /// Uses Windows UI Automation only: no process injection, memory access or network inspection.
    /// </summary>
    internal sealed class VaBaseMonitor
    {
        private const string ProcessName = "vaBaseLive";
        private const string StageAutomationId = "lblStage";

        private int processId = -1;
        private IntPtr windowHandle = IntPtr.Zero;
        private AutomationElement stageElement;

        internal VaBaseSnapshot Read()
        {
            try
            {
                var process = Process.GetProcessesByName(ProcessName)
                    .FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);

                if (process == null)
                {
                    Reset();
                    return new VaBaseSnapshot(false, string.Empty);
                }

                if (process.Id != processId || process.MainWindowHandle != windowHandle || stageElement == null)
                {
                    processId = process.Id;
                    windowHandle = process.MainWindowHandle;
                    stageElement = FindStageElement(windowHandle);
                }

                string stage = stageElement?.Current?.Name?.Trim() ?? string.Empty;

                // If the cached automation element became stale, resolve it once more.
                if (string.IsNullOrWhiteSpace(stage))
                {
                    stageElement = FindStageElement(windowHandle);
                    stage = stageElement?.Current?.Name?.Trim() ?? string.Empty;
                }

                return new VaBaseSnapshot(true, stage);
            }
            catch
            {
                // VaBase can disappear between process discovery and UIA access.
                Reset();
                return new VaBaseSnapshot(false, string.Empty);
            }
        }

        private static AutomationElement FindStageElement(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return null;

            var root = AutomationElement.FromHandle(handle);
            if (root == null)
                return null;

            var condition = new PropertyCondition(
                AutomationElement.AutomationIdProperty,
                StageAutomationId);

            return root.FindFirst(TreeScope.Descendants, condition);
        }

        private void Reset()
        {
            processId = -1;
            windowHandle = IntPtr.Zero;
            stageElement = null;
        }
    }

    internal readonly struct VaBaseSnapshot
    {
        internal VaBaseSnapshot(bool isRunning, string flightStage)
        {
            IsRunning = isRunning;
            FlightStage = flightStage ?? string.Empty;
        }

        internal bool IsRunning { get; }
        internal string FlightStage { get; }
    }
}
#endif
