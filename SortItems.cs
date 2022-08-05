/*
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

namespace TR {

    public class SortItems {

        public static Dictionary<int, ItemInfo> nearbyItems = new Dictionary<int, ItemInfo>();

        
        
        public static int GetItemCount(int itemID) => nearbyItems.TryGetValue(itemID, out var info) ? info.quantity : 0;

        public static ItemInfo GetItem(int itemID) => nearbyItems.TryGetValue(itemID, out var info) ? info : null;
        
        public static void AddItem(int itemID, int quantity, int slotID, bool isStackable, HouseDetails isInHouse, Chest chest) {

            if (!nearbyItems.TryGetValue(itemID, out var info)) { info = new ItemInfo(); }
            ItemStack source = new ItemStack();

            if (!isStackable) {
                source.fuel = quantity;
                quantity = 1;
            }
            info.quantity += quantity;

            source.quantity += quantity;
            source.chest = chest;
            source.slotID = slotID;
            source.inPlayerHouse = isInHouse;

            info.sources.Add(source);
            nearbyItems[itemID] = info;
        }
        
        public static void ParseAllItems() {
            if (!InventoryManagement.allItemsInitialized) { InventoryManagement.InitializeAllItems(); }
            
            // If inventory is open add contents to a list
            for (var i = 0; i < Inventory.inv.invSlots.Length; i++) {
                if (Inventory.inv.invSlots[i].itemNo != -1 && InventoryManagement.allItems.ContainsKey(Inventory.inv.invSlots[i].itemNo))
                    AddItem(Inventory.inv.invSlots[i].itemNo, Inventory.inv.invSlots[i].stack, i, InventoryManagement.allItems[Inventory.inv.invSlots[i].itemNo].checkIfStackable(), null, null);
                else if (!InventoryManagement.allItems.ContainsKey(Inventory.inv.invSlots[i].itemNo)) { Debug.Log($"Failed Item: {Inventory.inv.invSlots[i].itemNo} |  {Inventory.inv.invSlots[i].stack}"); }
            }

            // Get all items in nearby chests
            // If chest is open, add contents of chest to a list
            for (var i = 0; i < ChestInfo.chest.itemIds.Length; i++) {
                if (ChestInfo.chest.itemIds[i] != -1 && allItems.ContainsKey(ChestInfo.chest.itemIds[i])) AddItem(ChestInfo.chest.itemIds[i], ChestInfo.chest.itemStacks[i], i, allItems[ChestInfo.chest.itemIds[i]].checkIfStackable(), ChestInfo.house, ChestInfo.chest);
            }
        }
    }

}
*/
