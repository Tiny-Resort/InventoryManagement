using HarmonyLib;
using UnityEngine;

namespace TinyResort {

    [HarmonyPatch(typeof(SaveLoad), "SaveGame")]
    public class SaveGamePatch {

        [HarmonyPrefix]
        public static void patch(SaveLoad __instance) {
            TRTools.LogToConsole("Started Saving Game");
            TRData.preSave?.Invoke();
        }

    }

}
