#if !CONSOLE
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace JoinFS.FlyLab.Integration
{
    /// <summary>
    /// Read-only observer for the VaBase desktop client.
    /// Uses the managed Windows UI Automation API through reflection so the
    /// FlyLab layer does not require compile-time UIAutomation references.
    /// </summary>
    internal sealed class VaBaseMonitor
    {
        private const string ProcessName = "vaBaseLive";
        private const string StageAutomationId = "lblStage";

        private int processId = -1;
        private IntPtr windowHandle = IntPtr.Zero;
        private object stageElement;

        private Assembly uiAutomationClientAssembly;
        private Assembly uiAutomationTypesAssembly;
        private Type automationElementType;
        private Type propertyConditionType;
        private Type treeScopeType;

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

                string stage = ReadStageName(stageElement);

                // If the cached automation element became stale, resolve it once more.
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

        private void EnsureUiAutomation()
        {
            if (uiAutomationClientAssembly != null && uiAutomationTypesAssembly != null)
                return;

            // PowerShell requires both assemblies before the same UIA query works.
            // AutomationElement lives in UIAutomationClient, while condition/scope
            // types are provided by UIAutomationTypes.
            uiAutomationTypesAssembly = Assembly.Load("UIAutomationTypes");
            uiAutomationClientAssembly = Assembly.Load("UIAutomationClient");

            automationElementType = uiAutomationClientAssembly.GetType(
                "System.Windows.Automation.AutomationElement",
                throwOnError: true);

            propertyConditionType = uiAutomationTypesAssembly.GetType(
                "System.Windows.Automation.PropertyCondition",
                throwOnError: true);

            treeScopeType = uiAutomationTypesAssembly.GetType(
                "System.Windows.Automation.TreeScope",
                throwOnError: true);
        }

        private object FindStageElement(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return null;

            EnsureUiAutomation();

            var fromHandle = automationElementType.GetMethod(
                "FromHandle",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(IntPtr) },
                modifiers: null);

            object root = fromHandle?.Invoke(null, new object[] { handle });
            if (root == null)
                return null;

            object automationIdProperty = automationElementType
                .GetProperty("AutomationIdProperty", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null);

            if (automationIdProperty == null)
                return null;

            object condition = Activator.CreateInstance(
                propertyConditionType,
                new[] { automationIdProperty, (object)StageAutomationId });

            object descendants = Enum.Parse(treeScopeType, "Descendants");

            MethodInfo findFirst = automationElementType.GetMethod(
                "FindFirst",
                BindingFlags.Public | BindingFlags.Instance);

            return findFirst?.Invoke(root, new[] { descendants, condition });
        }

        private string ReadStageName(object element)
        {
            if (element == null)
                return string.Empty;

            try
            {
                // AutomationElement.Current returns an AutomationElementInformation
                // value type. Its Name member is exposed as a property on that type;
                // use the declared type rather than the boxed runtime object so the
                // reflection path mirrors PowerShell's $stage.Current.Name access.
                PropertyInfo currentProperty = automationElementType.GetProperty(
                    "Current",
                    BindingFlags.Public | BindingFlags.Instance);

                if (currentProperty == null)
                    return string.Empty;

                object current = currentProperty.GetValue(element);
                if (current == null)
                    return string.Empty;

                PropertyInfo nameProperty = currentProperty.PropertyType.GetProperty(
                    "Name",
                    BindingFlags.Public | BindingFlags.Instance);

                object name = nameProperty?.GetValue(current);
                return name?.ToString()?.Trim() ?? string.Empty;
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
