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

    internal class LocateItem : MonoBehaviour {

        private static LocateItem instance;
        private static Dictionary<(int xPos, int yPos), HouseDetails> unconfirmedChests = new Dictionary<(int xPos, int yPos), HouseDetails>();
        public static Dictionary<int, ItemInfo> nearbyItems = new Dictionary<int, ItemInfo>();
        public static List<(Chest chest, HouseDetails house)> nearbyChests = new List<(Chest chest, HouseDetails house)>();

        public static Vector3 playerPosition;
        public static HouseDetails playerHouse;
        
        public delegate void ParsingEvent();
        public static ParsingEvent OnFinishedParsing;

        public class ItemInfo {
            public int quantity;
            public List<ItemStack> sources = new List<ItemStack>();
        }

        public class ItemStack {
            public bool playerInventory;
            public int slotID;
            public int quantity;
            public int fuel;
            public HouseDetails inPlayerHouse;
            public Chest chest;
        }
        
        public static void RunSequence() {
            OnFinishedParsing = () => ShowChestWithItem();
            ParseAllItems();
            
            // detect specific check, color chest for timelimit, etc
        }

        public static void ShowChestWithItem() {
            
        }
        
         public static void ParseAllItems() {
            instance.StopAllCoroutines();
            instance.StartCoroutine(ParseAllItemsRoutine());
        }

        public static IEnumerator ParseAllItemsRoutine() {
            InventoryManagement.findingNearbyChests = true;
            FindNearbyChests();
            if (InventoryManagement.clientInServer) { yield return new WaitUntil(() => unconfirmedChests.Count <= 0); }
            nearbyItems.Clear();

            // Get all items in nearby chests
            foreach (var ChestInfo in nearbyChests) {
                for (var i = 0; i < ChestInfo.chest.itemIds.Length; i++) {
                    if (ChestInfo.chest.itemIds[i] != -1 && TRItems.GetItemDetails(ChestInfo.chest.itemIds[i])) { AddItem(ChestInfo.chest.itemIds[i], ChestInfo.chest.itemStacks[i], i, TRItems.GetItemDetails(ChestInfo.chest.itemIds[i]).checkIfStackable(), ChestInfo.house, ChestInfo.chest); }
                }
            }
            InventoryManagement.findingNearbyChests = false;
            OnFinishedParsing?.Invoke();
            OnFinishedParsing = null;
        }

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

        public static void FindNearbyChests() {
            var chests = new List<(ChestPlaceable chest, bool insideHouse)>();
            nearbyChests.Clear();

            for (int i = 0; i < HouseManager.manage.allHouses.Count; i++) {
                if (HouseManager.manage.allHouses[i].isThePlayersHouse) { playerHouse = HouseManager.manage.allHouses[i]; }
            }

            
            playerPosition = NetworkMapSharer.Instance.localChar.myInteract.transform.position;

            // Gets chests inside houses
            if (InventoryManagement.ClientInsideHouse || !InventoryManagement.clientInServer) {
                Collider[] chestsInsideHouse = Physics.OverlapBox(new Vector3(playerPosition.x, -88, playerPosition.z), new Vector3(InventoryManagement.radius.Value * 2, 5, InventoryManagement.radius.Value * 2), Quaternion.identity, LayerMask.GetMask(LayerMask.LayerToName(15)));
                for (var i = 0; i < chestsInsideHouse.Length; i++) {
                    ChestPlaceable chestComponent = chestsInsideHouse[i].GetComponentInParent<ChestPlaceable>();
                    if (chestComponent == null) continue;
                    chests.Add((chestComponent, true));
                }
            }

            // Gets chests in the overworld
            if (InventoryManagement.ClientOutsideHouse || !InventoryManagement.clientInServer) {
                Collider[] chestsOutside = Physics.OverlapBox(new Vector3(playerPosition.x, -7, playerPosition.z), new Vector3(InventoryManagement.radius.Value * 2, 20, InventoryManagement.radius.Value * 2), Quaternion.identity, LayerMask.GetMask(LayerMask.LayerToName(15)));
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
                    NetworkMapSharer.Instance.localChar.myPickUp.CmdOpenChest(tempX, tempY);
                    NetworkMapSharer.Instance.localChar.CmdCloseChest(tempX, tempY);
                }
                else {
                    ContainerManager.manage.checkIfEmpty(tempX, tempY, house);
                    AddChest(tempX, tempY, house);
                }
            }
        }

        private static void AddChest(int xPos, int yPos, HouseDetails house) {
            nearbyChests.Add((ContainerManager.manage.activeChests.First(i => i.xPos == xPos && i.yPos == yPos), house));
            nearbyChests = nearbyChests.Distinct().ToList();
        }
    }
}