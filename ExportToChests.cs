using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;

namespace TR {

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class ExportToChests : BaseUnityPlugin {

        public const string pluginGuid = "tinyresort.dinkum.AutoStackToChests";
        public const string pluginName = "Auto Stack to Chests";
        public const string pluginVersion = "0.5.0";
        public static ManualLogSource StaticLogger;

        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<int> radius;

        public static Dictionary<int, ItemInfo> nearbyItems = new Dictionary<int, ItemInfo>();
        public static List<(Chest chest, HouseDetails house)> nearbyChests = new List<(Chest chest, HouseDetails house)>();
        public static Dictionary<int, InventoryItem> allItems = new Dictionary<int, InventoryItem>();
        public static List<ChestPlaceable> knownChests = new List<ChestPlaceable>();
        public static HouseDetails playerHouse;
        public static HouseDetails currentHouseDetails;
        public static Transform playerHouseTransform;
        public static bool isInside;
        public static Vector3 currentPosition;

        public static bool allItemsInitialized;

        private void Awake() {

            StaticLogger = Logger;

            #region Configuration

            nexusID = Config.Bind<int>("General", "NexusID", 28, "Nexus mod ID for updates");
            radius = Config.Bind<int>("General", "Range", 30, "Increases the range it looks for storage containers by an approximate tile count.");
            isDebug = Config.Bind<bool>("General", "DebugMode", false, "Turns on debug mode. This makes it laggy when attempting to craft.");

            #endregion

            #region Logging

            ManualLogSource logger = Logger;

            bool flag;
            BepInExInfoLogInterpolatedStringHandler handler = new BepInExInfoLogInterpolatedStringHandler(18, 1, out flag);
            if (flag) { handler.AppendLiteral("Plugin " + pluginGuid + " (v" + pluginVersion + ") loaded!"); }
            logger.LogInfo(handler);

            #endregion

            #region Patching

            Harmony harmony = new Harmony(pluginGuid);

            MethodInfo updateRWTL = AccessTools.Method(typeof(RealWorldTimeLight), "Update");
            MethodInfo updateRWTLPrefix = AccessTools.Method(typeof(ExportToChests), "updateRWTLPrefix");

            MethodInfo update = AccessTools.Method(typeof(CharInteract), "Update");
            MethodInfo updatePrefix = AccessTools.Method(typeof(ExportToChests), "updatePrefix");

            harmony.Patch(updateRWTL, new HarmonyMethod(updateRWTLPrefix));
            harmony.Patch(update, new HarmonyMethod(updatePrefix));

            #endregion

        }

        [HarmonyPrefix]
        private static void updateRWTLPrefix(RealWorldTimeLight __instance) {
            if (Input.GetKeyDown(KeyCode.F6)) { RunSequence(); }
        }

        [HarmonyPrefix]
        public static void updatePrefix(CharInteract __instance) {
            currentPosition = __instance.transform.position;

            if (Input.GetKeyDown(KeyCode.F12)) Dbgl($"Current Position: ({currentPosition.x}, {currentPosition.y}, {currentPosition.z}) | Underground: {RealWorldTimeLight.time.underGround}");
            currentHouseDetails = __instance.insideHouseDetails;
            playerHouseTransform = __instance.playerHouseTransform;
            isInside = __instance.insidePlayerHouse;
        }

        public static void Dbgl(string str = "") {
            if (isDebug.Value) { StaticLogger.LogInfo(str); }
        }

        private static void InitializeAllItems() {
            allItemsInitialized = true;
            foreach (var item in Inventory.inv.allItems) {
                var id = item.getItemId();
                allItems[id] = item;
            }

        }

        public static void RunSequence() {
            ParseAllItems();
            UpdateAllItems();
        }

        public static int GetItemCount(int itemID) => nearbyItems.TryGetValue(itemID, out var info) ? info.quantity : 0;

        public static ItemInfo GetItem(int itemID) => nearbyItems.TryGetValue(itemID, out var info) ? info : null;

        public static void removeFromPlayerInventory(int itemID, int slotID, int amountRemaining) {
            Inventory.inv.invSlots[slotID].stack = amountRemaining;
            Inventory.inv.invSlots[slotID].updateSlotContentsAndRefresh(amountRemaining == 0 ? -1 : itemID, amountRemaining);
        }

        public static void UpdateAllItems() {
            for (int i = 0; i < Inventory.inv.invSlots.Length; i++) {
                int invItemId = Inventory.inv.invSlots[i].itemNo;
                if (invItemId != -1) {
                    int amountToAdd = Inventory.inv.invSlots[i].stack;
                    bool hasFuel = Inventory.inv.invSlots[i].itemInSlot.hasFuel;
                    var info = GetItem(invItemId);

                    // Stops if we don't have the item stored in a chest
                    if (info != null) { 
                        for (var d = 0; d < info.sources.Count; d++) {
                            int tmpFirstEmptySlot = checkForEmptySlot(info.sources[d].chest);
                            int tmpSlotID = hasFuel && tmpFirstEmptySlot != 999 ? tmpFirstEmptySlot : info.sources[d].slotID;
                                ContainerManager.manage.changeSlotInChest(
                                    info.sources[d].chest.xPos,
                                    info.sources[d].chest.yPos,
                                    tmpSlotID,
                                    invItemId,
                                info.sources[d].quantity + amountToAdd,
                                    info.sources[d].inPlayerHouse
                                );
                                removeFromPlayerInventory(invItemId, i, 0);
                        }
                    }
                }
            }
        }

        public static int checkForEmptySlot(Chest ChestInfo) {
            for (var i = 0; i < ChestInfo.itemIds.Length; i++) {
                if (ChestInfo.itemIds[i] == -1) { return i; }
            }
            return 999;
        }

        public static void ParseAllItems() {
            if (!allItemsInitialized) { InitializeAllItems(); }

            // Recreate known chests and inventory and clear items
            FindNearbyChests();
            nearbyItems.Clear();

            // Get all items in nearby chests
            foreach (var ChestInfo in nearbyChests) {
                for (var i = 0; i < ChestInfo.chest.itemIds.Length; i++) {
                    if (ChestInfo.chest.itemIds[i] != -1 && allItems.ContainsKey(ChestInfo.chest.itemIds[i])) { AddItem(ChestInfo.chest.itemIds[i], ChestInfo.chest.itemStacks[i], i, allItems[ChestInfo.chest.itemIds[i]].checkIfStackable(), ChestInfo.house, ChestInfo.chest); }
                }
            }
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

            // Debug.Log($"Chest Inventory{chest} -- Slot ID: {slotID} | ItemID: {itemID} | Quantity: {source.quantity} | Chest X: {chest.xPos} | Chest Y: {chest.yPos}");

            nearbyItems[itemID] = info;
        }

        public static void FindNearbyChests() {
            nearbyChests.Clear();

            var chests = new List<(Collider hit, bool insideHouse)>();
            Collider[] chestsInsideHouse;
            Collider[] chestsOutside;
            int tempX, tempY;

            for (int i = 0; i < HouseManager.manage.allHouses.Count; i++) {
                if (HouseManager.manage.allHouses[i].isThePlayersHouse) { playerHouse = HouseManager.manage.allHouses[i]; }
            }

            Dbgl($"{currentPosition.x} {0} {currentPosition.z}");
            chestsOutside = Physics.OverlapBox(new Vector3(currentPosition.x, -7, currentPosition.z), new Vector3(radius.Value * 2, 40, radius.Value * 2));
            Dbgl($"{currentPosition.x} {-88} {currentPosition.z}");
            chestsInsideHouse = Physics.OverlapBox(new Vector3(currentPosition.x, -88, currentPosition.z), new Vector3(radius.Value * 2, 5, radius.Value * 2));

            for (var i = 0; i < chestsInsideHouse.Length; i++) { chests.Add((chestsInsideHouse[i], true)); }
            for (var j = 0; j < chestsOutside.Length; j++) { chests.Add((chestsOutside[j], false)); }
            for (var k = 0; k < chests.Count; k++) {

                ChestPlaceable chestComponent = chests[k].hit.GetComponentInParent<ChestPlaceable>();
                if (chestComponent == null) continue;
                if (!knownChests.Contains(chestComponent)) { knownChests.Add(chestComponent); }

                var id = chestComponent.gameObject.GetInstanceID();
                var layer = chestComponent.gameObject.layer;
                var name = chestComponent.gameObject.name;
                var tag = chestComponent.gameObject.tag;

                // Dbgl($"ID: {id} | Layer: {layer} | Name: {name} | Tag: {tag}");

                tempX = chestComponent.myXPos();
                tempY = chestComponent.myYPos();

                if (chests[k].insideHouse) { ContainerManager.manage.checkIfEmpty(tempX, tempY, playerHouse); }
                else
                    ContainerManager.manage.checkIfEmpty(tempX, tempY, null);

                nearbyChests.Add((ContainerManager.manage.activeChests.First(i => i.xPos == tempX && i.yPos == tempY && i.inside == chests[k].insideHouse), playerHouse));
                nearbyChests = nearbyChests.Distinct().ToList();
            }
        }
    }

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

}
