using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.HID;
using UnityEngine.UI;

namespace TinyResort {

    public class ParseInventory {
        
        public static Dictionary<string, int> typeOrder = new Dictionary<string, int>();
        public static List<InventoryItems> inventoryToSort = new List<InventoryItems>();


        public static List<InventoryItems> ParsePlayerInventory() {
            inventoryToSort.Clear();
            getInventory();
            return inventoryToSort;
        }
        
        public static void getInventory() {
            for (var i = 11; i < Inventory.inv.invSlots.Length; i++) {
                
                if (!LockSlots.lockedSlots.Contains(i)
                 && Inventory.inv.invSlots[i].itemNo != -1
                 && TRItems.DoesItemExist(Inventory.inv.invSlots[i].itemNo)) {
                    var item = TRItems.GetItemDetails(Inventory.inv.invSlots[i].itemNo);
                    AddInventoryItem(Inventory.inv.invSlots[i].itemNo, Inventory.inv.invSlots[i].stack, item.checkIfStackable(), item.hasFuel, item.value);
                }
            }
        }

        public static void AddInventoryItem(int itemID, int quantity, bool isStackable, bool hasFuel, int value) {
            if (inventoryToSort.Any(i => i.itemID == itemID) && Inventory.inv.allItems[itemID].checkIfStackable()) {
                var tmpInventoryItem = inventoryToSort.Find(i => i.itemID == itemID);
                tmpInventoryItem.quantity += quantity;
            }
            else {
                var tempItem = new InventoryItems();
                if (!isStackable && hasFuel) {
                    tempItem.hasFuel = hasFuel;
                    tempItem.fuel = quantity;
                    quantity = 1;
                }
                tempItem.itemID = itemID;
                tempItem.quantity = quantity;
                tempItem.isStackable = isStackable;
                tempItem.itemType = getItemType(itemID);
                tempItem.invTypeOrder = typeOrder[tempItem.itemType];
                tempItem.value = value;
                tempItem.sortID = checkSortOrder(Inventory.inv.allItems[itemID].itemPrefab.name);
                inventoryToSort.Add(tempItem);
            }
        }

        #region Sort Order
        public static int checkSortOrder(string prefabName) {
            int tmpInt = 10000;
            
            switch (prefabName) {
                    case "PickAxe":
                        tmpInt = 0;
                        break;
                    case "CopperPickAxe":
                        tmpInt = 1;
                        break;
                    case "IronPickAxe":
                        tmpInt = 2;
                        break;
                    case "Axe":
                        tmpInt = 3;
                        break;
                    case "CopperAxe":
                        tmpInt = 4;
                        break;
                    case "IronAxe":
                        tmpInt = 5;
                        break;
                    case "Hoe":
                        tmpInt = 6;
                        break;
                    case "CopperHoe":
                        tmpInt = 7;
                        break;
                    case "IronHoe":
                        tmpInt = 8;
                        break;
                    case "Watercan":
                        tmpInt = 9;
                        break;
                    case "CopperWatercan":
                        tmpInt = 10;
                        break;
                    case "IronWatercan":
                        tmpInt = 11;
                        break;
                    case "Scythe":
                        tmpInt = 12;
                        break;
                    case "CopperScythe":
                        tmpInt = 13;
                        break;
                    case "IronScythe":
                        tmpInt = 14;
                        break;
                    case "Shovel":
                        tmpInt = 15;
                        break;
                    case "BasicFishingRod":
                        tmpInt = 16;
                        break;
                    case "CopperFishingRod":
                        tmpInt = 17;
                        break;
                    case "IronFishingRod":
                        tmpInt = 18;
                        break;
                    case "Spear":
                        tmpInt = 19;
                        break;
                    case "CopperSpear":
                        tmpInt = 20;
                        break;
                    case "IronSpear":
                        tmpInt = 21;
                        break;
                    case "Hammer":
                        tmpInt = 22;
                        break;
                    case "CopperHammer":
                        tmpInt = 23;
                        break;
                    case "IronHammer":
                        tmpInt = 24;
                        break;
                    case "WoodenBat":
                        tmpInt = 25;
                        break;
                    case "Crocbat":
                        tmpInt = 26;
                        break;
                    case "Flamebat":
                        tmpInt = 27;
                        break;
                    case "Stick Trap_Hand":
                        tmpInt = 28;
                        break;
                    case "Trap_Hand":
                        tmpInt = 29;
                        break;
                    case "Crude Furnace Hand ":
                        tmpInt = 30;
                        break;
                    case "Furnace_Handheld":
                        tmpInt = 31;
                        break;
                    case "CrateHand":
                        tmpInt = 32;
                        break;
                    case "ChestHand":
                        tmpInt = 33;
                        break;
                }
            InventoryManagement.Plugin.LogToConsole($"PrefabName: {prefabName} | Temp Int: {tmpInt}");
            return tmpInt;
        }
        #endregion
        
        public static string getItemType(int itemID) {
            InventoryItem currentItem = Inventory.inv.allItems[itemID];
            if (currentItem.spawnPlaceable) {
                if (currentItem.spawnPlaceable.GetComponent<Vehicle>()) { return "vehicles"; }
                return "placeables";
            }
            else {
                {
                    if (currentItem.bug || currentItem.fish || currentItem.underwaterCreature) { return "bugsorfish"; }
                    if (currentItem.isATool) { return "tools"; }
                    if (currentItem.relic) { return "relics"; }
                    if (currentItem.equipable && currentItem.equipable.cloths) { return "clothes"; }
                    if (currentItem.consumeable) { return "food"; }
                    if ((currentItem.placeable && !currentItem.burriedPlaceable) || (currentItem.placeableTileType > -1 && WorldManager.manageWorld.tileTypes[currentItem.placeableTileType].isPath)) { return "placeables"; }
                    if (currentItem.itemChange && currentItem.itemChange.changesAndTheirChanger[0].changesWhenComplete && currentItem.itemChange.changesAndTheirChanger[0].changesWhenComplete.consumeable) { return "food"; }
                }
            }
            return "base";
        }

        public class InventoryItems {
            public int itemID;
            public int slotID;
            public int sortID;
            public int quantity;
            public int fuel;
            public bool hasFuel;
            public bool isStackable;
            public int invTypeOrder;
            public string itemType;
            public int value;
        }
    }

}