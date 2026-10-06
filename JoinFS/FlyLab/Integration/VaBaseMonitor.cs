#if !CONSOLE
using System;
using System.Diagnostics;
using System.Linq;

namespace JoinFS.FlyLab.Integration
{
    /// <summary>
    /// Read-only observer for the VaBase desktop client.
    /// Uses the native Windows UI Automation COM client dynamically:
    /// no process injection, memory access or network inspection.
    /// </summary>
    internal sealed class VaBaseMonitor
    {
        private const string ProcessName = "vaBaseLive";
        private const string StageAutomationId = "lblStage";

        // Native UI Automation constants.
        private const int UIA_AutomationIdPropertyId = 30011;
        private const int TreeScopeDescendants = 4;

        private int processId = -1;
        private IntPtr windowHandle = IntPtr.Zero;
        private dynamic automation;
        private dynamic stageElement;

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
                    EnsureAutomation();
                    stageElement = FindStageElement(windowHandle);
                }

                string stage = ReadStageName(stageElement);

                // If the cached element became stale, resolve it once more.
                if (string.IsNullOrWhiteSpace(stage))
                {
                    stageElement = FindStageElement(windowHandle);
                    stage = ReadStageName(stageElement);
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

        private void EnsureAutomation()
        {
            if (automation != null)
                return;

            Type automationType =
                Type.GetTypeFromProgID("UIAutomationClient.CUIAutomation8")
                ?? Type.GetTypeFromProgID("UIAutomationClient.CUIAutomation");

            if (automationType == null)
                throw new InvalidOperationException("Windows UI Automation is not available.");

            automation = Activator.CreateInstance(automationType);
        }

        private dynamic FindStageElement(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return null;

            EnsureAutomation();

            dynamic root = automation.ElementFromHandle(handle);
            if (root == null)
                return null;

            dynamic condition = automation.CreatePropertyCondition(
                UIA_AutomationIdPropertyId,
                StageAutomationId);

            return root.FindFirst(TreeScopeDescendants, condition);
        }

        private static string ReadStageName(dynamic element)
        {
            if (element == null)
                return string.Empty;

            try
            {
                return ((string)element.CurrentName)?.Trim() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
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
