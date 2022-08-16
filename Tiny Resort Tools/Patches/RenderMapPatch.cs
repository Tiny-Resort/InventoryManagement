using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using UnityEngine.InputSystem;

namespace TinyResort {

    [HarmonyPatch(typeof(RenderMap), "runMapFollow")]
    public class RenderMapPatch {
        
        // Forcibly clears the top notification so that it can be replaced immediately
        [HarmonyPostfix]
        public static void runMapFollowPatch(RenderMap __instance) {
            TRMap.FixMarkerScale();
        }
        
    }
    
}