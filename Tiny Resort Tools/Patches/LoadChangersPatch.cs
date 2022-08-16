using HarmonyLib;
using UnityEngine;

namespace TinyResort {

    [HarmonyPatch(typeof(SaveLoad), "loadChangers")]
    public class LoadChangersPatch {

        // Runs mod data loading after the last bit of game data is loaded
        [HarmonyPostfix]
        public static void patch(SaveLoad __instance) {
            TRTools.LogToConsole("Finished Loading In");
            TRTools.InMainMenu = false;
            TRData.onLoad?.Invoke();
        }

    }

}
