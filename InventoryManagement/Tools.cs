﻿using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TinyResort {

    public class Tools {

        public static int currentFuel;
        public static int nextFuel;
        public static int maxFuel;
        public static float percentRemaining;
        public static bool warnedPercent;
        public static InventoryItems currentTool = new InventoryItems();
        public static List<InventoryItems> replacementTools = new List<InventoryItems>();
        public static Inventory currentInventory;

        [HarmonyPrefix]
        public static void useItemWithFuelPatch(Inventory __instance) {
      
            currentInventory = __instance;

            currentFuel = __instance.invSlots[__instance.selectedSlot].stack;
            maxFuel = __instance.allItems[__instance.invSlots[__instance.selectedSlot].itemNo].fuelMax;
            nextFuel = __instance.invSlots[__instance.selectedSlot].stack - __instance.allItems[__instance.invSlots[__instance.selectedSlot].itemNo].fuelOnUse;
            percentRemaining = (float)nextFuel / maxFuel * 100f;

            if (percentRemaining <= InventoryManagement.warnPercentage.Value && !warnedPercent && InventoryManagement.showNotifications.Value && !__instance.allItems[__instance.invSlots[__instance.selectedSlot].itemNo].itemPrefab.name.Contains("Watercan")) {
                TRTools.TopNotification($"{__instance.allItems[__instance.invSlots[__instance.selectedSlot].itemNo].itemName}", $"Durability is at {percentRemaining}%");
                ASound WarnTools = StatusManager.manage.lowHealthSound;
                WarnTools.volume = WarnTools.volume + 0.2f;
                SoundManager.Instance.play2DSound(WarnTools);

                for (int i = 0; i < SoundManager.Instance.my2DAudios.Length; i++) { InventoryManagement.Plugin.Log($"Sound: {SoundManager.Instance.my2DAudios[i].name}"); }
                warnedPercent = true;
            }
            if (percentRemaining <= InventoryManagement.swapPercentage.Value && InventoryManagement.doSwapTools.Value) {
                currentTool.fuel = nextFuel;
                currentTool.itemID = __instance.invSlots[__instance.selectedSlot].itemNo;
                currentTool.slotID = __instance.selectedSlot;
                CheckForReplacement(currentTool);
                if (replacementTools.Count > 0) {
                    Inventory.Instance.invSlots[currentTool.slotID].updateSlotContentsAndRefresh(replacementTools[0].itemID, replacementTools[0].fuel);
                    Inventory.Instance.invSlots[replacementTools[0].slotID].updateSlotContentsAndRefresh(currentTool.itemID, currentTool.fuel);
                    Inventory.Instance.equipNewSelectedSlot();
                }
            }
        }

        public static void CheckForReplacement(InventoryItems current) {

            replacementTools.Clear();
            for (var i = 0; i < currentInventory.invSlots.Length; i++) {
                InventoryManagement.Plugin.Log($"i: {i} | Size: {currentInventory.invSlots.Length}");
                if (currentInventory.invSlots[i].itemNo != -1 && Inventory.Instance.allItems[currentInventory.invSlots
                        [i].itemNo].hasFuel) {
                    float tempItemFuel = (float)currentInventory.invSlots[i].stack / currentInventory.invSlots[i].itemInSlot.fuelMax * 100;

                    var checkLastWord = currentInventory.invSlots[i].itemInSlot.itemName.Contains(currentInventory.invSlots[currentInventory.selectedSlot].itemInSlot.itemName.Split(' ')[currentInventory.invSlots[currentInventory.selectedSlot].itemInSlot.itemName.Split(' ').Length - 1]);
                    var similarTools = InventoryManagement.useSimilarTools.Value ? (currentInventory.invSlots[i].itemNo == current.itemID || checkLastWord) : currentInventory.invSlots[i].itemNo == current.itemID;
                    if (current.slotID != i && similarTools && tempItemFuel > 5f) {
                        InventoryItems tempTool = new InventoryItems();
                        tempTool.fuel = currentInventory.invSlots[i].stack;
                        tempTool.itemID = currentInventory.invSlots[i].itemNo;
                        tempTool.slotID = i;
                        replacementTools.Add(tempTool);
                    }
                }
            }
            replacementTools = replacementTools.OrderByDescending(i => i.itemID == current.itemID).ThenBy(i => i.itemID).ThenBy(i => i.fuel).ToList();
        }

        [HarmonyPrefix]
        public static void equipNewItemPrefix() {
            if (currentInventory != null) {
                if (currentInventory.invSlots[currentInventory.selectedSlot].stack > nextFuel) warnedPercent = false;
            }
        }

        public class InventoryItems {
            public int fuel;
            public bool hasFuel;
            public int itemID;
            public int slotID;
        }
    }

}
