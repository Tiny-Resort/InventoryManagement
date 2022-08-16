using HarmonyLib;
using UnityEngine;

namespace TinyResort {

    [HarmonyPatch(typeof(SaveLoad), "returnToMenu")]
    public class ReturnToMenuPatch {
        [HarmonyPostfix]
        public static void patch(SaveLoad __instance) {
            TRTools.LogToConsole("Returning to Main Menu");
            TRTools.InMainMenu = true;
        }
    }

}
