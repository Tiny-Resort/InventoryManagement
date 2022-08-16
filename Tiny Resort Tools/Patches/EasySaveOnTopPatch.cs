using HarmonyLib;
using UnityEngine;

namespace TinyResort {

    [HarmonyPatch(typeof(SaveLoad), "EasySaveOnTop")]
    public class EasySaveOnTopPatch {
        [HarmonyPostfix]
        public static void patch(SaveLoad __instance) {
            TRTools.LogToConsole("Finished Saving Game");
            TRData.postSave?.Invoke();
        }
    }
    
}
