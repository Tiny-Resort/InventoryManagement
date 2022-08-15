using System;
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

    // TODO: Add ability to sort by various parameters
    // TODO: Add sort button in inventory and chest windows
    public class SortItems {

        public static List<InventoryItems> inventoryToSort = new List<InventoryItems>();
        public static List<ChestItems> chestToSort = new List<ChestItems>();
        public static Dictionary<string, int> typeOrder = new Dictionary<string, int>();
        public static int currentChestX;
        public static int currentChestY;
        public static HouseDetails currentChestHouseDetails;
        public static Chest currentChest;

        public static void SortInventory() {
            
            ParseAllItems();
            
            #region InventorySort

            // Inventory will vanish if you try to sort both at the same time.
            if (!ChestWindow.chests.chestWindowOpen || InventoryManagement.alwaysInventory.Value) {
                inventoryToSort = inventoryToSort.OrderBy(i => i.invTypeOrder).ThenBy(i => i.sortID).ThenBy(i => i.value).ToList();
                for (int j = 11; j < Inventory.inv.invSlots.Length; j++) {
                    if (!InventoryManagement.lockedSlots.Contains(j)) { Inventory.inv.invSlots[j].updateSlotContentsAndRefresh(-1, 0); }
                }
                for (int k = 0; k < inventoryToSort.Count; k++) {
                    for (int l = k + 11; l < Inventory.inv.invSlots.Length; l++) {
                        InventoryManagement.Plugin.LogToConsole($"Locked SLot?: {l}: {!InventoryManagement.lockedSlots.Contains(l)}");
                        if (!InventoryManagement.lockedSlots.Contains(l) && Inventory.inv.invSlots[l].itemNo == -1) {
                            if (!inventoryToSort[k].isStackable && inventoryToSort[k].hasFuel) { Inventory.inv.invSlots[l].updateSlotContentsAndRefresh(inventoryToSort[k].itemID, inventoryToSort[k].fuel); }
                            else { Inventory.inv.invSlots[l].updateSlotContentsAndRefresh(inventoryToSort[k].itemID, inventoryToSort[k].quantity); }
                            break;
                        }
                    }
                }
                NotificationManager.manage.createChatNotification($"The player's inventory has been sorted.");
            }

            #endregion

            #region ChestSort

           // if (InventoryManagement.disableMod()) return;
            if (ChestWindow.chests.chestWindowOpen && !InventoryManagement.modDisabled) {
                chestToSort = chestToSort.OrderBy(i => i.invTypeOrder).ThenBy(i => i.sortID).ThenBy(i => i.value).ToList();
                for (int i = 0; i < chestToSort.Count; i++) {
                    if (chestToSort[i].hasFuel) {
                        if (InventoryManagement.clientInServer) {
                            NetworkMapSharer.share.localChar.myPickUp.CmdChangeOneInChest(currentChestX, currentChestY, i, chestToSort[i].itemID, chestToSort[i].fuel);
                        } else ContainerManager.manage.changeSlotInChest(currentChestX, currentChestY, i, chestToSort[i].itemID, chestToSort[i].fuel, currentChestHouseDetails);
                    }
                    else if (!chestToSort[i].hasFuel) {
                        if (InventoryManagement.clientInServer) { NetworkMapSharer.share.localChar.myPickUp.CmdChangeOneInChest(currentChestX, currentChestY, i, chestToSort[i].itemID, chestToSort[i].quantity); }
                        else ContainerManager.manage.changeSlotInChest(currentChestX, currentChestY, i, chestToSort[i].itemID, chestToSort[i].quantity, currentChestHouseDetails);
                    }
                    if (chestToSort.Count - 1 == i && i != 23) {
                        for (int j = i + 1; j < 24; j++) {
                            if (InventoryManagement.clientInServer) { NetworkMapSharer.share.localChar.myPickUp.CmdChangeOneInChest(currentChestX, currentChestY, j, -1, 0); }
                            else ContainerManager.manage.changeSlotInChest(currentChestX, currentChestY, j, -1, 0, currentChestHouseDetails);
                        }
                    }
                }
                NotificationManager.manage.createChatNotification($"The chest's inventory has been sorted.");
            }
            if (InventoryManagement.modDisabled) { TRTools.TopNotification("Inventory Management", "This mod is disabled in the deep mines."); }
            SoundManager.manage.play2DSound(SoundManager.manage.inventorySound);

            #endregion
        }

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

        public static void AddChestItem(int itemID, int quantity, bool isStackable, bool hasFuel, HouseDetails isInHouse, int value) {
            if (chestToSort.Any(i => i.itemID == itemID) && Inventory.inv.allItems[itemID].checkIfStackable()) {
                var tempChestItem = chestToSort.Find(i => i.itemID == itemID);
                tempChestItem.quantity += quantity;
            }
            else {
                var tempItem = new ChestItems();
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
                tempItem.inPlayerHouse = isInHouse;
                tempItem.value = value;
                tempItem.sortID = checkSortOrder(Inventory.inv.allItems[itemID].itemPrefab.name);
                chestToSort.Add(tempItem);
            }
        }
        
        public static void ParseAllItems() {
            inventoryToSort.Clear();
            chestToSort.Clear();
            currentChest = null;
            for (var i = 11; i < Inventory.inv.invSlots.Length; i++) {
                if (!InventoryManagement.lockedSlots.Contains(i) && Inventory.inv.invSlots[i].itemNo != -1 && TRItems.DoesItemExist(Inventory.inv.invSlots[i].itemNo)) {
                    AddInventoryItem(Inventory.inv.invSlots[i].itemNo, Inventory.inv.invSlots[i].stack, TRItems.GetItemDetails(Inventory.inv.invSlots[i].itemNo).checkIfStackable(), TRItems.GetItemDetails(Inventory.inv.invSlots[i].itemNo).hasFuel, TRItems.GetItemDetails(Inventory.inv.invSlots[i].itemNo).value);
                }
            }
            
            if (ChestWindow.chests.chestWindowOpen && !InventoryManagement.modDisabled) {
                currentChestHouseDetails = NetworkMapSharer.share.localChar.myInteract.insideHouseDetails == null ? null : NetworkMapSharer.share.localChar.myInteract.insideHouseDetails;
                for (int i = 0; i < ContainerManager.manage.activeChests.Count; i++) {
                    if (NetworkMapSharer.share.localChar.myInteract.selectedTile.x == ContainerManager.manage.activeChests[i].xPos
                     && NetworkMapSharer.share.localChar.myInteract.selectedTile.y == ContainerManager.manage.activeChests[i].yPos) {
                        currentChest = ContainerManager.manage.activeChests[i];
                        currentChestX = ContainerManager.manage.activeChests[i].xPos;
                        currentChestY = ContainerManager.manage.activeChests[i].yPos;
                        break;
                    }
                }
                if (currentChest != null) {
                    for (var i = 0; i < currentChest.itemIds.Length; i++) {
                        if (currentChest.itemIds[i] != -1 && TRItems.DoesItemExist(currentChest.itemIds[i])) {
                            InventoryManagement.Plugin.LogToConsole($"{currentChest.itemIds[i]}");
                            AddChestItem(currentChest.itemIds[i], currentChest.itemStacks[i], TRItems.GetItemDetails(currentChest.itemIds[i]).checkIfStackable(), TRItems.GetItemDetails(currentChest.itemIds[i]).hasFuel, currentChestHouseDetails, TRItems.GetItemDetails(currentChest.itemIds[i]).value);
                        }
                    }
                }
            }
        }
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

    public class ChestItems {
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
        public HouseDetails inPlayerHouse;
    }

}
