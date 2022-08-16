using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace TinyResort {

    public class TRPlugin {

        public BaseUnityPlugin plugin;
        public Harmony harmony;
        
        public string pluginGuid;
        public string pluginName;
        public string pluginVersion;
        
        public ManualLogSource Logger;
        public ConfigEntry<int> nexusID;
        public ConfigEntry<bool> debugMode;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="severity"></param>
        /// <param name="debugModeOnly">If true, this message will only show in the console if the config file has DebugMode set to true.</param>
        public void LogToConsole(string text, LogSeverity severity = LogSeverity.Standard, bool debugModeOnly = true) {
            if (debugModeOnly && !debugMode.Value) return;
            TRTools.LogToConsole(text, severity, Logger);
        }

        /// <summary>
        /// Allows you to patch methods using only one line of code instead of three per method patched.
        /// </summary>
        /// <param name="sourceClassType">Typically typeOf(className) where className is the class you are patching in Dinkum.</param>
        /// <param name="sourceMethod">The name of the method in Dinkum being patched.</param>
        /// <param name="patchClassType">Typically typeOf(className) where className is the name of your class (the one that contains the patch method).</param>
        /// <param name="prefixMethod">The name of the prefix method doing the patching.</param>
        /// <param name="postfixMethod">The name of the postfix method doing the patching.</param>
        public void QuickPatch(Type sourceClassType, string sourceMethod, Type patchClassType, string prefixMethod, string postfixMethod = "") {
            MethodInfo sourceMethodInfo = AccessTools.Method(sourceClassType, sourceMethod);
            MethodInfo prefixMethodInfo = String.IsNullOrWhiteSpace(prefixMethod) ? null : AccessTools.Method(patchClassType, prefixMethod);
            MethodInfo postfixMethodInfo = String.IsNullOrWhiteSpace(postfixMethod) ? null : AccessTools.Method(patchClassType, postfixMethod);
            var prefixHarmonyMethod = String.IsNullOrWhiteSpace(prefixMethod) ? null : new HarmonyMethod(prefixMethodInfo);
            var postfixHarmonyMethod = String.IsNullOrWhiteSpace(postfixMethod) ? null : new HarmonyMethod(postfixMethodInfo);
            
            harmony.Patch(sourceMethodInfo, prefixHarmonyMethod, postfixHarmonyMethod);
        }
        
    }

    public enum LogSeverity { Standard, Warning, Error }
    public enum PatchType { Prefix, Postfix }

}
