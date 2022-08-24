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

    public class ParseChests {
        public static List<(Chest chest, HouseDetails house)> nearbyChests = new List<(Chest chest, HouseDetails house)>();
        public static Dictionary<(int xPos, int yPos), HouseDetails> unconfirmedChests = new Dictionary<(int xPos, int yPos), HouseDetails>();
        
        public static HouseDetails playerHouse;
        public static Vector3 currentPosition;
        public static Vector3 playerPosition;

        public static bool ClientInsideHouse => NetworkMapSharer.share.localChar.myInteract.insidePlayerHouse && InventoryManagement.clientInServer;
        public static bool ClientOutsideHouse => !NetworkMapSharer.share.localChar.myInteract.insidePlayerHouse && InventoryManagement.clientInServer;

        public static int currentChestX;
        public static int currentChestY;
        public static HouseDetails currentChestHouseDetails;
        public static Chest currentChest;
        
        public static List<ChestItems> chestToSort = new List<ChestItems>();


        private static void AddChest(int xPos, int yPos, HouseDetails house) {
            nearbyChests.Add((ContainerManager.manage.activeChests.First(i => i.xPos == xPos && i.yPos == yPos), house));
            nearbyChests = nearbyChests.Distinct().ToList();
        }

        [HarmonyPostfix]
        public static void UserCode_TargetOpenChestPostfix(ContainerManager __instance, int xPos, int yPos, int[] itemIds, int[] itemStack) {
            // TODO: Get proper house details
            if (unconfirmedChests.TryGetValue((xPos, yPos), out var house)) {
                unconfirmedChests.Remove((xPos, yPos));
                AddChest(xPos, yPos, house);
            }
        }

        public static void FindNearbyChests() {
            var chests = new List<(ChestPlaceable chest, bool insideHouse)>();
            nearbyChests.Clear();

            for (int i = 0; i < HouseManager.manage.allHouses.Count; i++) {
                if (HouseManager.manage.allHouses[i].isThePlayersHouse) { playerHouse = HouseManager.manage.allHouses[i]; }
            }


            playerPosition = NetworkMapSharer.share.localChar.myInteract.transform.position;
            var currentRadius = InventoryManagement.radius.Value;
            // Gets chests inside houses
            if (ClientInsideHouse || !InventoryManagement.clientInServer) {
                Collider[] chestsInsideHouse = Physics.OverlapBox(new Vector3(playerPosition.x, -88, playerPosition.z), new Vector3(currentRadius * 2, 5, currentRadius * 2), Quaternion.identity, LayerMask.GetMask(LayerMask.LayerToName(15)));
                for (var i = 0; i < chestsInsideHouse.Length; i++) {
                    ChestPlaceable chestComponent = chestsInsideHouse[i].GetComponentInParent<ChestPlaceable>();
                    if (chestComponent == null) continue;
                    chests.Add((chestComponent, true));
                }
            }

            // Gets chests in the overworld
            if (ClientOutsideHouse || !InventoryManagement.clientInServer) {
                Collider[] chestsOutside = Physics.OverlapBox(new Vector3(playerPosition.x, -7, playerPosition.z), new Vector3(currentRadius * 2, 20, currentRadius * 2), Quaternion.identity, LayerMask.GetMask(LayerMask.LayerToName(15)));
                for (var j = 0; j < chestsOutside.Length; j++) {
                    ChestPlaceable chestComponent = chestsOutside[j].GetComponentInParent<ChestPlaceable>();
                    if (chestComponent == null) continue;
                    chests.Add((chestComponent, false));
                }
            }

            for (var k = 0; k < chests.Count; k++) {

                var tempX = chests[k].chest.myXPos();
                var tempY = chests[k].chest.myYPos();

                HouseDetails house = chests[k].insideHouse ? playerHouse : null;

                if (InventoryManagement.clientInServer) {
                    unconfirmedChests[(tempX, tempY)] = house;
                    NetworkMapSharer.share.localChar.myPickUp.CmdOpenChest(tempX, tempY);
                    NetworkMapSharer.share.localChar.CmdCloseChest(tempX, tempY);
                }
                else {
                    ContainerManager.manage.checkIfEmpty(tempX, tempY, house);
                    AddChest(tempX, tempY, house);
                }
            }
        }


        public static void ParseInventory() {
            for (var i = 11; i < Inventory.inv.invSlots.Length; i++) {
                if (!LockSlots.lockedSlots.Contains(i)
                 && Inventory.inv.invSlots[i].itemNo != -1
                 && TRItems.DoesItemExist(Inventory.inv.invSlots[i].itemNo)) {
                    AddInventoryItem(
                        Inventory.inv.invSlots[i].itemNo, 
                        Inventory.inv.invSlots[i].stack, 
                        TRItems.GetItemDetails(Inventory.inv.invSlots[i].itemNo).checkIfStackable(), 
                        TRItems.GetItemDetails(Inventory.inv.invSlots[i].itemNo).hasFuel, 
                        TRItems.GetItemDetails(Inventory.inv.invSlots[i].itemNo).value);
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

        public static void ParseOpenChest() {
            chestToSort.Clear();
            
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

        public static void ParseCurrentChest() {
            
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

}