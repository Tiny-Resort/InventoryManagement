﻿using System.Collections.Generic;
using System.Linq;

namespace TinyResort {

    // TODO: Add ability to sort by various parameters
    internal class SortItems {

        internal static List<InventoryItems> inventoryToSort = new List<InventoryItems>();
        internal static List<ChestItems> chestToSort = new List<ChestItems>();
        internal static Dictionary<string, int> typeOrder = new Dictionary<string, int>();
        internal static int currentChestX;
        internal static int currentChestY;
        internal static HouseDetails currentChestHouseDetails;
        internal static Chest currentChest;

        internal static void SortInventory() {

            ParseAllItems();

            inventoryToSort = inventoryToSort.OrderBy(i => i.invTypeOrder).ThenBy(i => i.sortID).ThenBy(i => i.value).ToList();
            for (int j = 11; j < Inventory.Instance.invSlots.Length; j++) {
                if (!LockSlots.lockedSlots.Contains(j)) { Inventory.Instance.invSlots[j].updateSlotContentsAndRefresh(-1, 0); }
            }
            for (int k = 0; k < inventoryToSort.Count; k++) {
                for (int l = k + 11; l < Inventory.Instance.invSlots.Length; l++) {
                    InventoryManagement.Plugin.Log($"Locked SLot?: {l}: {!LockSlots.lockedSlots.Contains(l)}");
                    if (!LockSlots.lockedSlots.Contains(l) && Inventory.Instance.invSlots[l].itemNo == -1) {
                        if (!inventoryToSort[k].isStackable && inventoryToSort[k].hasFuel) { Inventory.Instance.invSlots[l].updateSlotContentsAndRefresh(inventoryToSort[k].itemID, inventoryToSort[k].fuel); }
                        else { Inventory.Instance.invSlots[l].updateSlotContentsAndRefresh(inventoryToSort[k].itemID, inventoryToSort[k].quantity); }
                        break;
                    }
                }
            }
            NotificationManager.manage.createChatNotification($"The player's inventory has been sorted.");

            if (InventoryManagement.modDisabled) { TRTools.TopNotification("Inventory Management", "Sorting chests is disabled in the deep mines."); }
            SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
        }

        internal static void SortChest() {

            ParseAllItems();

            if (ChestWindow.chests.chestWindowOpen && !InventoryManagement.modDisabled) {
                chestToSort = chestToSort.OrderBy(i => i.invTypeOrder).ThenBy(i => i.sortID).ThenBy(i => i.value).ToList();
                for (int i = 0; i < chestToSort.Count; i++) {
                    if (chestToSort[i].hasFuel) {
                        if (InventoryManagement.clientInServer) { NetworkMapSharer.Instance.localChar.myPickUp.CmdChangeOneInChest(currentChestX, currentChestY, i, chestToSort[i].itemID, chestToSort[i].fuel); }
                        else
                            ContainerManager.manage.changeSlotInChest(currentChestX, currentChestY, i, chestToSort[i].itemID, chestToSort[i].fuel, currentChestHouseDetails);
                    }
                    else if (!chestToSort[i].hasFuel) {
                        if (InventoryManagement.clientInServer) { NetworkMapSharer.Instance.localChar.myPickUp.CmdChangeOneInChest(currentChestX, currentChestY, i, chestToSort[i].itemID, chestToSort[i].quantity); }
                        else
                            ContainerManager.manage.changeSlotInChest(currentChestX, currentChestY, i, chestToSort[i].itemID, chestToSort[i].quantity, currentChestHouseDetails);
                    }
                    if (chestToSort.Count - 1 == i && i != 23) {
                        for (int j = i + 1; j < 24; j++) {
                            if (InventoryManagement.clientInServer) { NetworkMapSharer.Instance.localChar.myPickUp.CmdChangeOneInChest(currentChestX, currentChestY, j, -1, 0); }
                            else
                                ContainerManager.manage.changeSlotInChest(currentChestX, currentChestY, j, -1, 0, currentChestHouseDetails);
                        }
                    }
                }
                NotificationManager.manage.createChatNotification($"The chest's inventory has been sorted.");
            }
            if (InventoryManagement.modDisabled) { TRTools.TopNotification("Inventory Management", "Sorting chests is disabled in the deep mines."); }
            SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);

        }

        internal static void SortToChests() {
            InventoryManagement.OnFinishedParsing = () => InventoryManagement.UpdateAllItems();
            InventoryManagement.ParseAllItems();
            InventoryManagement.totalDeposited = 0;
        }

        internal static string getItemType(int itemID) {
            InventoryItem currentItem = Inventory.Instance.allItems[itemID];
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
                    if ((currentItem.placeable && !currentItem.burriedPlaceable) || (currentItem.placeableTileType > -1 && WorldManager.Instance
                                   .tileTypes[currentItem.placeableTileType].isPath)) { return "placeables"; }
                    if (currentItem.itemChange && currentItem.itemChange.changesAndTheirChanger[0].changesWhenComplete && currentItem.itemChange.changesAndTheirChanger[0].changesWhenComplete.consumeable) { return "food"; }
                }
            }
            return "base";
        }

        internal static void AddInventoryItem(int itemID, int quantity, bool isStackable, bool hasFuel, int value) {
            if (inventoryToSort.Any(i => i.itemID == itemID) && Inventory.Instance.allItems[itemID].checkIfStackable()) {
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
                tempItem.sortID = checkSortOrder(Inventory.Instance.allItems[itemID].itemPrefab.name);
                inventoryToSort.Add(tempItem);
            }
        }

        internal static int checkSortOrder(string prefabName) {
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
            return tmpInt;
        }

        internal static void AddChestItem(int itemID, int quantity, bool isStackable, bool hasFuel, HouseDetails isInHouse, int value) {
            if (chestToSort.Any(i => i.itemID == itemID) && Inventory.Instance.allItems[itemID].checkIfStackable()) {
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
                tempItem.sortID = checkSortOrder(Inventory.Instance.allItems[itemID].itemPrefab.name);
                chestToSort.Add(tempItem);
            }
        }

        internal static void ParseAllItems() {
            inventoryToSort.Clear();
            chestToSort.Clear();
            currentChest = null;
            for (var i = 11; i < Inventory.Instance.invSlots.Length; i++) {
                if (!LockSlots.lockedSlots.Contains(i) && Inventory.Instance.invSlots[i].itemNo != -1 && TRItems.GetItemDetails(Inventory.Instance.invSlots[i].itemNo)) {
                    AddInventoryItem(
                        Inventory.Instance.invSlots[i].itemNo, Inventory.Instance.invSlots[i].stack, 
                        TRItems.GetItemDetails(Inventory.Instance.invSlots[i].itemNo).checkIfStackable(), 
                        TRItems.GetItemDetails(Inventory.Instance.invSlots[i].itemNo).hasFuel, 
                        TRItems.GetItemDetails(Inventory.Instance.invSlots[i].itemNo).value);
                }
            }

            if (ChestWindow.chests.chestWindowOpen && !InventoryManagement.modDisabled) {
                currentChestHouseDetails = NetworkMapSharer.Instance.localChar.myInteract.InsideHouseDetails == null ? null : NetworkMapSharer.Instance
                                              .localChar.myInteract.InsideHouseDetails;
                for (int i = 0; i < ContainerManager.manage.activeChests.Count; i++) {
                    if (NetworkMapSharer.Instance.localChar.myInteract.selectedTile.x == ContainerManager.manage.activeChests[i].xPos
                     && NetworkMapSharer.Instance.localChar.myInteract.selectedTile.y == ContainerManager.manage.activeChests[i].yPos) {
                        currentChest = ContainerManager.manage.activeChests[i];
                        currentChestX = ContainerManager.manage.activeChests[i].xPos;
                        currentChestY = ContainerManager.manage.activeChests[i].yPos;
                        break;
                    }
                }
                if (currentChest != null) {
                    for (var i = 0; i < currentChest.itemIds.Length; i++) {
                        if (currentChest.itemIds[i] != -1 && TRItems.GetItemDetails(currentChest.itemIds[i])) {
                            InventoryManagement.Plugin.Log($"{currentChest.itemIds[i]}");
                            AddChestItem(currentChest.itemIds[i], currentChest.itemStacks[i], TRItems.GetItemDetails(currentChest.itemIds[i]).checkIfStackable(), TRItems.GetItemDetails(currentChest.itemIds[i]).hasFuel, currentChestHouseDetails, TRItems.GetItemDetails(currentChest.itemIds[i]).value);
                        }
                    }
                }
            }
        }
    }

    internal class InventoryItems {
        internal int itemID;
        internal int slotID;
        internal int sortID;
        internal int quantity;
        internal int fuel;
        internal bool hasFuel;
        internal bool isStackable;
        internal int invTypeOrder;
        internal string itemType;
        internal int value;
    }

    internal class ChestItems {
        internal int itemID;
        internal int slotID;
        internal int sortID;
        internal int quantity;
        internal int fuel;
        internal bool hasFuel;
        internal bool isStackable;
        internal int invTypeOrder;
        internal string itemType;
        internal int value;
        internal HouseDetails inPlayerHouse;
    }

}
