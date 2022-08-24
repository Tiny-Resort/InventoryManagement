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

namespace TinyResort {

    // TODO: Add ability to sort by various parameters
    // TODO: Add sort button in inventory and chest windows
    public class SortItems {
        public static List<ParseInventory.InventoryItems> inventoryToSort = new List<ParseInventory.InventoryItems>();
        public static List<ParseChests.ChestItems> chestToSort = new List<ParseChests.ChestItems>();

        public static void SortInventory() {
            
            ParseAllItems();
            
            #region InventorySort

            // Inventory will vanish if you try to sort both at the same time.
            if (!ChestWindow.chests.chestWindowOpen || InventoryManagement.alwaysInventory.Value) {
                inventoryToSort = inventoryToSort.OrderBy(i => i.invTypeOrder).ThenBy(i => i.sortID).ThenBy(i => i.value).ToList();
                for (int j = 11; j < Inventory.inv.invSlots.Length; j++) {
                    if (!LockSlots.lockedSlots.Contains(j)) { Inventory.inv.invSlots[j].updateSlotContentsAndRefresh(-1, 0); }
                }
                for (int k = 0; k < inventoryToSort.Count; k++) {
                    for (int l = k + 11; l < Inventory.inv.invSlots.Length; l++) {
                        InventoryManagement.Plugin.LogToConsole($"Locked SLot?: {l}: {!LockSlots.lockedSlots.Contains(l)}");
                        if (!LockSlots.lockedSlots.Contains(l) && Inventory.inv.invSlots[l].itemNo == -1) {
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
            if (InventoryManagement.modDisabled) { TRTools.TopNotification("Inventory Management", "Sorting chests is disabled in the deep mines."); }
            SoundManager.manage.play2DSound(SoundManager.manage.inventorySound);

            #endregion
        }

        public static void ParseAllItems() {
            inventoryToSort.Clear();
            chestToSort.Clear();
            currentChest = null;

            inventoryToSort = ParseInventory.ParsePlayerInventory();
            chestToSort = ParseChests.ParseCurrentChest();


        }
    }
    


}
