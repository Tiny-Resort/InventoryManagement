using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace TinyResort {
    
    public static class TRTools {

        private static ManualLogSource ToolsLogger;
        private static bool Initialized;
        private static Dictionary<BaseUnityPlugin, TRPlugin> HookedPlugins = new Dictionary<BaseUnityPlugin, TRPlugin>();

        public static bool InMainMenu = true;

        /// <summary> Initializes the Tiny Resort toolset </summary>
        /// <param name="plugin">Your plugin. When calling this from your plugin, simply use 'this'.</param>
        /// <param name="logger">The BepInEx logger for your plugin. Usually simply 'Logger'.</param>
        /// <param name="nexusID">The ID of your mod on nexus. This is the number at the end of the URL for your mod's nexus page. (A mod page does not need to be published in order to have an ID)</param>
        /// <param name="pluginGuid">The Guid of your plugin, generally declared at the top of your mod.</param>
        /// <param name="pluginName">The name of your plugin, generally declared at the top of your mod.</param>
        /// <param name="pluginVersion">The version of your plugin, generally declared at the top of your mod.</param>
        public static TRPlugin Initialize(this BaseUnityPlugin plugin, ManualLogSource logger, 
                                          int nexusID, string pluginGuid, string pluginName, string pluginVersion) {

            // Initializes this mod in particular
            if (!HookedPlugins.ContainsKey(plugin)) {

                HookedPlugins[plugin] = new TRPlugin();
                HookedPlugins[plugin].plugin = plugin;
                HookedPlugins[plugin].harmony = new Harmony(pluginGuid);

                if (nexusID > 0) { HookedPlugins[plugin].nexusID = plugin.Config.Bind("Developer", "NexusID", nexusID, "Nexus Mod ID. You can find it on the mod's page on Nexus."); }
                HookedPlugins[plugin].debugMode = plugin.Config.Bind("Developer", "DebugMode", false, "If true, the BepinEx console will print out debug messages related to this mod.");

                HookedPlugins[plugin].Logger = logger;
                BepInExInfoLogInterpolatedStringHandler handler = new BepInExInfoLogInterpolatedStringHandler(18, 1, out var flag);
                if (flag) { handler.AppendLiteral("Plugin " + pluginGuid + " (v" + pluginVersion + ") loaded!"); }
                HookedPlugins[plugin].Logger.LogInfo(handler);

            }

            // Initializes the TR Toolset for all mods
            if (!Initialized) {
                ToolsLogger = new ManualLogSource("TR Tools");
                HookedPlugins[plugin].harmony.PatchAll();
                TRDrawing.Initialize();
                Initialized = true;
            }

            return HookedPlugins[plugin];

        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void LogToConsole(string text, LogSeverity severity = LogSeverity.Standard, ManualLogSource Logger = null) {
            if (Logger == null) Logger = ToolsLogger;
            switch (severity) {
                case LogSeverity.Standard: Logger.LogInfo(text); break;
                case LogSeverity.Warning: Logger.LogWarning(text); break;
                case LogSeverity.Error: Logger.LogError(text); break;
            }
        }

        #region Easy Notifications

        /// <summary> Displays a notification at the top of the screen right away rather than waiting on any previous notifications. </summary>
        /// <param name="title">Large text at the top of the notification.</param>
        /// <param name="subtitle">Smaller descriptive text below the title.</param>
        /// <param name="playSFX">If true, a sound effect will be played.</param>
        public static void TopNotification(string title, string subtitle, bool playSFX = false) {
            // TODO: Play sound effect
            NotificationPatch.forceClearNotification = true;
            NotificationManager.manage.makeTopNotification(title, subtitle);
            NotificationPatch.forceClearNotification = false;
        }
        
        #endregion
        
        #region Version Checking

        private static string currentGameVersion;

        public static string CheckGameVersion(string myModGameVersion) {
                
            if (!WorldManager.manageWorld) { return "Game not fully loaded. Can not compare version numbers."; }
            currentGameVersion = "v0." + WorldManager.manageWorld.masterVersionNumber.ToString() + "." + WorldManager.manageWorld.versionNumber.ToString();

            return myModGameVersion == currentGameVersion ? 
                       "Mod was created using this version of the game." : 
                       "Mod was created for a different version of the game than is running. Issues may occur.";
            
        }
        
        #endregion

    }

}
