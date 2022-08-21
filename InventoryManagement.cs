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

// TODO: Add the ability to send specific items to nearest chest by some key combination
// TODO: Update method to use .sav rather than config file
// TODO: Add a highlight on new items obtianed since last opening inventory. Will need to remove the items if you sort to chests automatically without opening first
// TODO: Add button to send all items to chests
namespace TinyResort {

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class InventoryManagement : BaseUnityPlugin {

        public static TRPlugin Plugin;
        public const string pluginGuid = "tinyresort.dinkum.InventoryManagement";
        public const string pluginName = "Inventory Management";
        public const string pluginVersion = "0.8.0";
        private static InventoryManagement instance;

        public delegate void ParsingEvent();
        public static ParsingEvent OnFinishedParsing;

        public static ConfigEntry<int> radius;
        public static ConfigEntry<bool> ignoreHotbar;
        public static ConfigEntry<string> ignoreSpecifcSlots;
        public static ConfigEntry<KeyCode> exportKeybind;
        public static ConfigEntry<KeyCode> lockSlotKeybind;
        public static ConfigEntry<string> sortingOrder;
        public static ConfigEntry<KeyCode> sortKeybind;
        public static ConfigEntry<bool> alwaysInventory;
        public static ConfigEntry<float> warnPercentage;
        public static ConfigEntry<float> swapPercentage;
        public static ConfigEntry<bool> doSwapTools;
        public static ConfigEntry<bool> showNotifications;
        public static ConfigEntry<bool> useSimilarTools;
        public static TextMeshPro x;

        public static List<(Chest chest, HouseDetails house)> nearbyChests = new List<(Chest chest, HouseDetails house)>();
        private static Dictionary<(int xPos, int yPos), HouseDetails> unconfirmedChests = new Dictionary<(int xPos, int yPos), HouseDetails>();
        public static Dictionary<int, ItemInfo> nearbyItems = new Dictionary<int, ItemInfo>();
        public static HouseDetails playerHouse;
        public static Vector3 currentPosition;
        
        public static List<int> lockedSlots = new List<int>();
        public static Color LockedSlotColor = new Color(.5f, .5f, .5f, 1f);
        public static Color defaultColor = Color.white;
        public static int tempCurrentIgnore;
        public static int totalDeposited = 0;
        public static bool initalizedObjects = false;
        public static bool ButtonIsReady = false;
        public static bool clientInServer;
        public static bool checkIfLocking;
        public static bool findingNearbyChests;

        public static bool ClientInsideHouse => NetworkMapSharer.share.localChar.myInteract.insidePlayerHouse && clientInServer;
        public static bool ClientOutsideHouse => !NetworkMapSharer.share.localChar.myInteract.insidePlayerHouse && clientInServer;
        
        public static bool modDisabled => RealWorldTimeLight.time.underGround;


        public UnityEvent onButtonPress = new UnityEvent();
        
        public static Vector3 playerPosition;
        
        public static GameObject GO;

        public void Awake() {
            instance = this;

            Plugin = TRTools.Initialize(this, Logger, 50, pluginGuid, pluginName, pluginVersion);
            #region Configuration

            radius = Config.Bind<int>("General", "Range", 30, "Increases the range it looks for storage containers by an approximate tile count.");
            ignoreHotbar = Config.Bind<bool>("General", "IgnoreHotbar", true, "Set to true to disable auto importing from the hotbar.");
            ignoreSpecifcSlots = Config.Bind<string>("DO NOT EDIT", "IgnoreSpecificSlots", "", "DO NOT EDIT.");
            exportKeybind = Config.Bind<KeyCode>("Keybinds", "SortToChestKeybind", KeyCode.KeypadDivide, "Unity KeyCode used for sending inventory items into storage chests.");
            lockSlotKeybind = Config.Bind<KeyCode>("Keybinds", "LockSlot", KeyCode.LeftAlt, "Unity KeyCode used for locking inventory slots. Use this key in combination with the left mouse button to lock the slots.");

            sortingOrder = Config.Bind<string>("SortingOrder", "SortOrder", "relics,bugsorfish,clothes,vehicles,placeables,base,tools,food", "Order to sort inventory and chest items.");
            sortKeybind = Config.Bind<KeyCode>("Keybinds", "SortInventoryOrChests", KeyCode.KeypadMultiply, "Unity KeyCode used for sorting player and chest inventories.");
            alwaysInventory = Config.Bind<bool>("SortingOrder", "AlwaysSortPlayerInventory", false, "Set to true if you would like the keybind to sort player inventory and chests while a chest window is open. By default, it will only sort the chest.");

            warnPercentage = Config.Bind<float>("Tools", "WarnPercentage", 5, "Set the percentage of remaining durability for the notification to warn you.");
            swapPercentage = Config.Bind<float>("Tools", "SwapPercentage", 2, "Set the percentage of remaining durability you want it to automatically swap tools.");
            doSwapTools = Config.Bind<bool>("Tools", "SwapTools", true, "Set to false if you want to disable the automatic swapping of tools.");
            showNotifications = Config.Bind<bool>("Tools", "ShowNotifications", true, "Set to false if you want to disable notifications about tools breaking at the specificed percentage.");
            useSimilarTools = Config.Bind<bool>("Tools", "UseSimilarTools", true, "Set to false if you want to disable swapping to similar tools (i.e swapping from Basic Axe to Copper Axe).");

            #endregion

            #region Parse Config String to Int

            // Convert this to use a .sav instead of using the base config settings
            if (ignoreSpecifcSlots != null) {
                string[] tmp = ignoreSpecifcSlots.Value.Trim().Split(',');
                for (var i = 0; i < tmp.Length - 1; i++) {
                    int.TryParse(tmp[i], out int tmpInt);
                    lockedSlots.Add(tmpInt);
                }
            }
            string[] tmpSort = sortingOrder.Value.Trim().ToLower().Split(',');
            for (var i = 0; i < tmpSort.Length; i++) {
                Plugin.LogToConsole($"tempSort List: {tmpSort[i]}");
                SortItems.typeOrder[tmpSort[i]] = i;
            }
            foreach (var element in SortItems.typeOrder) { Plugin.LogToConsole($"Key: {element.Key} | Value: {element.Value}"); }

            initalizedObjects = false;

            #endregion

            #region Patching

            Plugin.QuickPatch(typeof(RealWorldTimeLight), "Update", typeof(InventoryManagement), "updateRWTLPrefix");
            Plugin.QuickPatch(typeof(Inventory), "moveCursor", typeof(InventoryManagement), "moveCursorPrefix");
            Plugin.QuickPatch(typeof(CurrencyWindows), "openInv", typeof(InventoryManagement), null, "openInvPostfix");
            Plugin.QuickPatch(typeof(ContainerManager), "UserCode_TargetOpenChest", typeof(InventoryManagement), null, "UserCode_TargetOpenChestPostfix");
            Plugin.QuickPatch(typeof(ChestWindow), "openChestInWindow", typeof(InventoryManagement), "openChestInWindowPrefix");

            Plugin.QuickPatch(typeof(Inventory), "useItemWithFuel", typeof(Tools), "useItemWithFuelPatch");
            Plugin.QuickPatch(typeof(EquipItemToChar), "equipNewItem", typeof(Tools), "equipNewItemPrefix");

            #endregion
        }
        
        // Update method to export items to chest to a buttom or a different (customizable) hotkey. 
        [HarmonyPrefix]
        public static void updateRWTLPrefix(RealWorldTimeLight __instance) {
            clientInServer = !__instance.isServer;

            if (Input.GetKeyDown(exportKeybind.Value)) {
                if (!modDisabled) {
                    RunSequence();
                    totalDeposited = 0;
                }
                else
                    TRTools.TopNotification("Inventory Management", "Send items to chests is disabled in the mines/playerhouse.");
            }
            if (Input.GetKeyDown(sortKeybind.Value)) { SortItems.SortInventory(); }
        }

        public void LateUpdate() {
            Color tmpColor = Inventory.inv.invOpen ? LockedSlotColor : Color.white;
            if (lockedSlots.Count > 0 && Inventory.inv.inventoryWindow != null) {
                for (int i = 0; i < Inventory.inv.invSlots.Length; i++) {
                    if (lockedSlots.Contains(i)) { Inventory.inv.invSlots[i].GetComponent<Image>().color = tmpColor; }
                }
            }
        }

        [HarmonyPostfix]
        public static void openInvPostfix() {

            if (!initalizedObjects && ButtonIsReady) {
                GO = Instantiate(Inventory.inv.walletSlot.transform.parent.gameObject, Inventory.inv.walletSlot.transform.parent.parent, true);
                Destroy(GO.transform.GetChild(0).gameObject);
                Destroy(GO.transform.GetChild(1).GetChild(2).gameObject);
                var Im = GO.transform.GetChild(1).GetComponent<Image>();

                // var rect = GO.GetComponent<RectTransform>();
                Im.rectTransform.anchoredPosition += new Vector2(190, 0);
                var textGO = GO.GetComponentsInChildren<TextMeshProUGUI>();
                textGO[0].enableWordWrapping = false;
                textGO[0].rectTransform.anchoredPosition += new Vector2(-18, 0);
                textGO[0].text = "Sort to Chests";

                instance.onButtonPress.AddListener(RunSequence);
                var buttonGO = Im.gameObject.AddComponent<InvButton>();
                buttonGO.gameObject.AddComponent<GraphicRaycaster>();
                buttonGO.onButtonPress.AddListener(RunSequence);
                //buttonGO.onButtonPress.Invoke();
                initalizedObjects = true;
            }
        }

        public static bool moveCursorPrefix(Inventory __instance) {
            if (InputMaster.input.UISelect()) {
                if (Input.GetKey(lockSlotKeybind.Value) && Input.GetMouseButtonDown(0)) { // Update to use different key combination so you can do it while not picking up item?
                    InventorySlot slot = __instance.cursorPress();
                    if (slot != null) {
                        Plugin.LogToConsole($"Slot: {slot.transform.parent.gameObject.name}"); 
                        if (slot.transform.parent.gameObject.name == "InventoryWindows") { tempCurrentIgnore = slot.transform.GetSiblingIndex() + 11; }
                        else if (slot.transform.parent.gameObject.name == "QuickSlotBar") { tempCurrentIgnore = slot.transform.GetSiblingIndex(); }
                        else {
                            Debug.Log($"Mouse Click On Button: {slot.transform.parent.gameObject.name}");
                            tempCurrentIgnore = -1;
                        }
                        if (tempCurrentIgnore != -1 && lockedSlots.Contains(tempCurrentIgnore)) {
                            lockedSlots.Remove(tempCurrentIgnore);
                            slot.slotBackgroundImage.color = defaultColor;
                        }
                        else if (tempCurrentIgnore != -1) {
                            lockedSlots.Add(tempCurrentIgnore);
                            Inventory.inv.invSlots[tempCurrentIgnore].slotBackgroundImage.color = LockedSlotColor;
                        }
                    }
                    checkIfLocking = true;
                }
                ignoreSpecifcSlots.Value = null;
                foreach (var element in lockedSlots) {
                    var tempString = ignoreSpecifcSlots == null ? element + "," : ignoreSpecifcSlots.Value + element + ",";
                    ignoreSpecifcSlots.Value = tempString;
                }
                ignoreSpecifcSlots.ConfigFile.Save();
                for (int i = 0; i < Inventory.inv.invSlots.Length; i++) {
                    if (lockedSlots.Contains(i)) { Inventory.inv.invSlots[i].GetComponent<Image>().color = LockedSlotColor; }
                }
                if (checkIfLocking) { return false; }
            }
            checkIfLocking = false;
            return true;
        }

        public static void RunSequence() {
            OnFinishedParsing = () => UpdateAllItems();
            ParseAllItems();
            totalDeposited = 0;
        }

        public static int GetItemCount(int itemID) => nearbyItems.TryGetValue(itemID, out var info) ? info.quantity : 0;

        public static ItemInfo GetItem(int itemID) => nearbyItems.TryGetValue(itemID, out var info) ? info : null;

        public static void removeFromPlayerInventory(int itemID, int slotID, int amountRemaining) {
            Inventory.inv.invSlots[slotID].stack = amountRemaining;
            Inventory.inv.invSlots[slotID].updateSlotContentsAndRefresh(amountRemaining == 0 ? -1 : itemID, amountRemaining);
        }

        public static void UpdateAllItems() {
            var startingPoint = ignoreHotbar.Value ? 11 : 0;

            for (int i = startingPoint; i < Inventory.inv.invSlots.Length; i++) {
                if (!lockedSlots.Contains(i)) {
                    int invItemId = Inventory.inv.invSlots[i].itemNo;
                    if (invItemId != -1) {
                        int amountToAdd = Inventory.inv.invSlots[i].stack;
                        bool hasFuel = Inventory.inv.invSlots[i].itemInSlot.hasFuel;
                        bool isStackable = Inventory.inv.invSlots[i].itemInSlot.isStackable;
                        var info = GetItem(invItemId);

                        // Stops if we don't have the item stored in a chest
                        if (info != null) {
                            for (var d = 0; d < info.sources.Count; d++) {

                                int slotToUse = 998;
                                int tmpFirstEmptySlot = checkForEmptySlot(info.sources[d].chest);
                                if ((hasFuel || !isStackable) && tmpFirstEmptySlot != 999) { slotToUse = tmpFirstEmptySlot; }
                                else if ((hasFuel || !isStackable) && tmpFirstEmptySlot == 999) { slotToUse = tmpFirstEmptySlot; }
                                else if (isStackable) { slotToUse = info.sources[d].slotID; }
                                info.sources[d].quantity = !isStackable ? amountToAdd : info.sources[d].quantity + amountToAdd;
                                if (slotToUse != 999) {
                                    if (clientInServer) {
                                        NetworkMapSharer.share.localChar.myPickUp.CmdChangeOneInChest(
                                            info.sources[d].chest.xPos,
                                            info.sources[d].chest.yPos,
                                            slotToUse,
                                            invItemId,
                                            info.sources[d].quantity
                                        );
                                    }
                                    else {
                                        ContainerManager.manage.changeSlotInChest(
                                            info.sources[d].chest.xPos,
                                            info.sources[d].chest.yPos,
                                            slotToUse,
                                            invItemId,
                                            info.sources[d].quantity,
                                            info.sources[d].inPlayerHouse
                                        );
                                    }
                                    removeFromPlayerInventory(invItemId, i, 0);
                                    totalDeposited++;
                                    break;
                                }
                                hasFuel = false;
                                isStackable = false;
                            }
                        }
                    }
                }
            }
            NotificationManager.manage.createChatNotification($"{totalDeposited} item(s) have been deposited.");
            SoundManager.manage.play2DSound(SoundManager.manage.inventorySound);
        }

        public static int checkForEmptySlot(Chest ChestInfo) {
            for (var i = 0; i < ChestInfo.itemIds.Length; i++) {
                if (ChestInfo.itemIds[i] == -1) { return i; }
            }
            return 999;
        }

        public static bool openChestInWindowPrefix() { return !findingNearbyChests; }

        [HarmonyPostfix]
        public static void UserCode_TargetOpenChestPostfix(ContainerManager __instance, int xPos, int yPos, int[] itemIds, int[] itemStack) {
            // TODO: Get proper house details
            if (unconfirmedChests.TryGetValue((xPos, yPos), out var house)) {
                unconfirmedChests.Remove((xPos, yPos));
                AddChest(xPos, yPos, house);
            }
        }
        
        public static void ParseAllItems() {
            instance.StopAllCoroutines();
            instance.StartCoroutine(ParseAllItemsRoutine());
        }

        public static IEnumerator ParseAllItemsRoutine() {
            findingNearbyChests = true;
            FindNearbyChests();
            if (clientInServer) { yield return new WaitUntil(() => unconfirmedChests.Count <= 0); }
            nearbyItems.Clear();

            // Get all items in nearby chests
            foreach (var ChestInfo in nearbyChests) {
                for (var i = 0; i < ChestInfo.chest.itemIds.Length; i++) {
                    if (ChestInfo.chest.itemIds[i] != -1 && TRItems.DoesItemExist(ChestInfo.chest.itemIds[i])) { AddItem(ChestInfo.chest.itemIds[i], ChestInfo.chest.itemStacks[i], i, TRItems.GetItemDetails(ChestInfo.chest.itemIds[i]).checkIfStackable(), ChestInfo.house, ChestInfo.chest); }
                }
            }
            findingNearbyChests = false;
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

            
            playerPosition = NetworkMapSharer.share.localChar.myInteract.transform.position;

            // Gets chests inside houses
            if (ClientInsideHouse || !clientInServer) {
                Collider[] chestsInsideHouse = Physics.OverlapBox(new Vector3(playerPosition.x, -88, playerPosition.z), new Vector3(radius.Value * 2, 5, radius.Value * 2), Quaternion.identity, LayerMask.GetMask(LayerMask.LayerToName(15)));
                for (var i = 0; i < chestsInsideHouse.Length; i++) {
                    ChestPlaceable chestComponent = chestsInsideHouse[i].GetComponentInParent<ChestPlaceable>();
                    if (chestComponent == null) continue;
                    chests.Add((chestComponent, true));
                }
            }

            // Gets chests in the overworld
            if (ClientOutsideHouse || !clientInServer) {
                Collider[] chestsOutside = Physics.OverlapBox(new Vector3(playerPosition.x, -7, playerPosition.z), new Vector3(radius.Value * 2, 20, radius.Value * 2), Quaternion.identity, LayerMask.GetMask(LayerMask.LayerToName(15)));
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

                if (clientInServer) {
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

        private static void AddChest(int xPos, int yPos, HouseDetails house) {
            nearbyChests.Add((ContainerManager.manage.activeChests.First(i => i.xPos == xPos && i.yPos == yPos), house));
            nearbyChests = nearbyChests.Distinct().ToList();
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
}