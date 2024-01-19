using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Schema;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System;

// TODO: Add the ability to send specific items to nearest chest by some key combination
// TODO: Update method to use .sav rather than config file
// TODO: Add a highlight on new items obtianed since last opening inventory. Will need to remove the items if you sort to chests automatically without opening first

// TODO: Add check to make sure host has activeChest list from the host. 
namespace TinyResort; 

[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
[BepInDependency("dev.TinyResort.TRTools")]
public class InventoryManagement : BaseUnityPlugin {

    public delegate void ParsingEvent();
    public const string pluginGuid = "dev.TinyResort.InventoryManagement";
    public const string pluginName = "Inventory Management";
    public const string pluginVersion = "0.9.0";

    public static TRPlugin Plugin;
    private static InventoryManagement instance;
    public static ParsingEvent OnFinishedParsing;

    public static ConfigEntry<int> radius;
    public static ConfigEntry<bool> ignoreHotbar;
    public static ConfigEntry<string> ignoreSpecifcSlots;

    // Keybinds. Leaving in so people have the option to use them instead of the buttons.
    public static ConfigEntry<KeyCode> exportKeybind;
    public static ConfigEntry<KeyCode> exportControllerKeybind;
    public static ConfigEntry<KeyCode> lockSlotKeybind;
    public static ConfigEntry<KeyCode> lockSlotControllerKeybind;
    public static ConfigEntry<KeyCode> sortKeybind;
    public static ConfigEntry<KeyCode> sortControllerKeybind;

    // Sorting order. This can be modified by the user in the configuration file.
    public static ConfigEntry<string> sortingOrder;

    public static ConfigEntry<float> warnPercentage;
    public static ConfigEntry<float> swapPercentage;
    public static ConfigEntry<bool> doSwapTools;
    public static ConfigEntry<bool> showNotifications;
    public static ConfigEntry<bool> useSimilarTools;

    public static List<(Chest chest, HouseDetails house)> nearbyChests = new();
    private static readonly Dictionary<(int xPos, int yPos), HouseDetails> unconfirmedChests = new();
    public static Dictionary<int, ItemInfo> nearbyItems = new();
    public static HouseDetails playerHouse;
    public static Vector3 currentPosition;

    public static int tempCurrentIgnore;
    public static int totalDeposited;
    public static bool initalizedObjects = false;
    public static bool ButtonIsReady = true;
    public static bool clientInServer;

    public static Vector3 playerPosition;
    public static TRModData modData;
    public static TRCustomItem couch;

    public static bool ClientInsideHouse =>
        NetworkMapSharer.Instance.localChar.myInteract.IsInsidePlayerHouse && clientInServer;
    public static bool ClientOutsideHouse =>
        !NetworkMapSharer.Instance.localChar.myInteract.IsInsidePlayerHouse && clientInServer;


    public static bool modDisabled => RealWorldTimeLight.time.GetCurrentMapArea() != WorldArea.MAIN_ISLAND;
    public void Awake() {
        instance = this;

        Plugin = this.Initialize(50);
        modData = TRData.Subscribe(pluginGuid);

        TRData.postLoadEvent += LockSlots.LoadLockedSlots;
        Plugin.RequireAPIVersion("0.8.8");

        #region Configuration
        radius = Config.Bind(
            "General", "Range", 30, "Increases the range it looks for storage containers by an approximate tile count."
        );
        ignoreHotbar = Config.Bind(
            "General", "IgnoreHotbar", true, "Set to true to disable auto importing from the hotbar."
        );
        ignoreSpecifcSlots = Config.Bind("DO NOT EDIT", "IgnoreSpecificSlots", "", "DO NOT EDIT.");
        exportKeybind = Config.Bind(
            "Keybinds", "SortToChestKeybind", KeyCode.None,
            "Unity KeyCode used for sending inventory items into storage chests."
        );
        exportControllerKeybind = Config.Bind(
            "Keybinds", "SortToChestKeybind", KeyCode.None,
            "Unity KeyCode used for sending inventory items into storage chests."
        );

        lockSlotKeybind = Config.Bind(
            "Keybinds", "LockSlot", KeyCode.LeftAlt,
            "Unity KeyCode used for locking inventory slots. Use this key in combination with the left mouse button to lock the slots."
        );
        lockSlotControllerKeybind = Config.Bind(
            "Keybinds", "LockSlot", KeyCode.None,
            "Unity KeyCode used for locking inventory slots. Use this key in combination with the left mouse button to lock the slots."
        );

        sortingOrder = Config.Bind(
            "SortingOrder", "SortOrder", "relics,bugsorfish,clothes,vehicles,placeables,base,tools,food",
            "Order to sort inventory and chest items."
        );
        sortKeybind = Config.Bind(
            "Keybinds", "SortInventoryOrChests", KeyCode.None,
            "Unity KeyCode used for sorting player and chest inventories."
        );
        sortControllerKeybind = Config.Bind(
            "Keybinds", "SortInventoryOrChests", KeyCode.None,
            "Unity KeyCode used for sorting player and chest inventories."
        );

        warnPercentage = Config.Bind<float>(
            "Tools", "WarnPercentage", 5, "Set the percentage of remaining durability for the notification to warn you."
        );
        swapPercentage = Config.Bind<float>(
            "Tools", "SwapPercentage", 2,
            "Set the percentage of remaining durability you want it to automatically swap tools."
        );
        doSwapTools = Config.Bind(
            "Tools", "SwapTools", true, "Set to false if you want to disable the automatic swapping of tools."
        );
        showNotifications = Config.Bind(
            "Tools", "ShowNotifications", true,
            "Set to false if you want to disable notifications about tools breaking at the specificed percentage."
        );
        useSimilarTools = Config.Bind(
            "Tools", "UseSimilarTools", true,
            "Set to false if you want to disable swapping to similar tools (i.e swapping from Basic Axe to Copper Axe)."
        );
        #endregion

        #region Parse Config String to Int
        // Convert this to use a .sav instead of using the base config settings
        if (ignoreSpecifcSlots != null) {
            var tmp = ignoreSpecifcSlots.Value.Trim().Split(',');
            for (var i = 0; i < tmp.Length - 1; i++) {
                int.TryParse(tmp[i], out var tmpInt);
                LockSlots.lockedSlots.Add(tmpInt);
            }
        }

        // TODO: Remove this and make sort button a rotation between options. 
        var tmpSort = sortingOrder.Value.Trim().ToLower().Split(',');
        for (var i = 0; i < tmpSort.Length; i++) SortItems.typeOrder[tmpSort[i]] = i;
        #endregion

        #region Patching
        Plugin.QuickPatch(typeof(RealWorldTimeLight), "Update", typeof(InventoryManagement), "updateRWTLPrefix");
        
        // Patches for locking slots
        Plugin.QuickPatch(typeof(Inventory), "moveCursor", typeof(LockSlots), "moveCursorPrefix");

        // Patches for tools
        Plugin.QuickPatch(typeof(Inventory), "useItemWithFuel", typeof(Tools), "useItemWithFuelPatch");
        Plugin.QuickPatch(typeof(EquipItemToChar), "equipNewItem", typeof(Tools), "equipNewItemPrefix");
        #endregion
        
    }

    public void Update() {
        if (NetworkMapSharer.Instance.localChar && !CreateButtons.InventoryMenu && Inventory.Instance.invOpen) { CreateButtons.CreateInventoryButtons(); }
        if (NetworkMapSharer.Instance.localChar && !CreateButtons.ChestWindowLayout && ChestWindow.chests.chestWindowOpen) { CreateButtons.CreateChestButtons(); }
    }

    public void LateUpdate() {
        var tmpColor = Inventory.Instance.invOpen ? LockSlots.LockedSlotColor : Color.white;
        if (LockSlots.lockedSlots.Count > 0 && Inventory.Instance.inventoryWindow != null)
            for (var i = 0; i < Inventory.Instance.invSlots.Length; i++)
                if (LockSlots.lockedSlots.Contains(i))
                    Inventory.Instance.invSlots[i].GetComponent<Image>().color = tmpColor;
    }


    // If people still want to use the hotkeys
    [HarmonyPrefix] public static void updateRWTLPrefix(RealWorldTimeLight __instance) {
        clientInServer = !__instance.isServer;
        
        if (Input.GetKeyDown(exportKeybind.Value) || Input.GetKeyDown(exportControllerKeybind.Value)) {
            if (!modDisabled) {
                SortItems.SortToChests();
                totalDeposited = 0;
            }
            else {
                TRTools.TopNotification(
                    "Inventory Management", "Send items to chests is disabled in the playerhouse, the mines, and the off the main island."
                );
            }
        }
        if (Input.GetKeyDown(sortKeybind.Value) || Input.GetKeyDown(sortControllerKeybind.Value))
            SortItems.SortInventory();
    }

    public static int GetItemCount(int itemID) => nearbyItems.TryGetValue(itemID, out var info) ? info.quantity : 0;

    public static ItemInfo GetItem(int itemID) => nearbyItems.TryGetValue(itemID, out var info) ? info : null;

    public static void RemoveItemFromPlayerInventory(int itemID, int slotID, int amountRemaining) {
        Inventory.Instance.invSlots[slotID].stack = amountRemaining;
        Inventory.Instance.invSlots[slotID]
                 .updateSlotContentsAndRefresh(amountRemaining == 0 ? -1 : itemID, amountRemaining);
    }
    
    public static void DepositItems() {
        var startingPoint = ignoreHotbar.Value ? 11 : 0;

        for (var i = startingPoint; i < Inventory.Instance.invSlots.Length; i++)
            if (!LockSlots.lockedSlots.Contains(i)) {
                var invItemId = Inventory.Instance.invSlots[i].itemNo;
                if (invItemId != -1) {
                    var amountToAdd = Inventory.Instance.invSlots[i].stack;
                    var hasFuel = Inventory.Instance.invSlots[i].itemInSlot.hasFuel;
                    var isStackable = Inventory.Instance.invSlots[i].itemInSlot.isStackable;
                    var info = GetItem(invItemId);

                    // Stops if we don't have the item stored in a chest
                    if (info != null)
                        for (var d = 0; d < info.sources.Count; d++) {

                            var slotToUse = 998;
                            var tmpFirstEmptySlot = checkForEmptySlot(info.sources[d].chest);
                            if ((hasFuel || !isStackable) && tmpFirstEmptySlot != 999)
                                slotToUse = tmpFirstEmptySlot;
                            else if ((hasFuel || !isStackable) && tmpFirstEmptySlot == 999)
                                slotToUse = tmpFirstEmptySlot;
                            else if (isStackable) slotToUse = info.sources[d].slotID;
                            info.sources[d].quantity =
                                !isStackable ? amountToAdd : info.sources[d].quantity + amountToAdd;
                            if (slotToUse != 999) {
                                if (clientInServer)
                                    NetworkMapSharer.Instance.localChar.myPickUp.CmdChangeOneInChest(
                                        info.sources[d].chest.xPos,
                                        info.sources[d].chest.yPos,
                                        slotToUse,
                                        invItemId,
                                        info.sources[d].quantity
                                    );
                                else
                                    ContainerManager.manage.changeSlotInChest(
                                        info.sources[d].chest.xPos,
                                        info.sources[d].chest.yPos,
                                        slotToUse,
                                        invItemId,
                                        info.sources[d].quantity,
                                        info.sources[d].inPlayerHouse
                                    );
                                RemoveItemFromPlayerInventory(invItemId, i, 0);
                                totalDeposited++;
                                break;
                            }
                            hasFuel = false;
                            isStackable = false;
                        }
                }
            }
        NotificationManager.manage.createChatNotification($"{totalDeposited} item(s) have been deposited.");
        SoundManager.Instance.play2DSound(SoundManager.Instance.inventorySound);
    }

    public static int checkForEmptySlot(Chest ChestInfo) {
        for (var i = 0; i < ChestInfo.itemIds.Length; i++)
            if (ChestInfo.itemIds[i] == -1)
                return i;
        return 999;
    }

    internal static HouseDetails GetPlayerHouse() {
        for (var i = 0; i < HouseManager.manage.allHouses.Count; i++)
            if (HouseManager.manage.allHouses[i].isThePlayersHouse) {
                playerHouse = HouseManager.manage.allHouses[i];
            }
        return playerHouse;
    }




    internal static void GenerateNearbyItems() {    
        nearbyItems.Clear();
        GetPlayerHouse();
        
        // This should never happen?
        if (playerHouse == null) return;
        
        foreach (Chest chest in ContainerManager.manage.activeChests) {
            // Main Island is 0
            HouseDetails chestInsidePlayerHouse = null;
            
            if (chest.placedInWorldLevel != 0) continue;
            if (chest.inside && chest.xPos == playerHouse.xPos && chest.yPos == playerHouse.yPos) {
                chestInsidePlayerHouse = playerHouse;
            }
            
            for (var i = 0; i < chest.itemIds.Length; i++) {
                if (chest.itemIds[i] != -1 && TRItems.GetItemDetails(chest.itemIds[i])) {
                    AddItem(
                        chest.itemIds[i],  chest.itemStacks[i], i,
                        TRItems.GetItemDetails(chest.itemIds[i]).checkIfStackable(), chestInsidePlayerHouse,
                        chest
                    );
                }

            }
        }
    }
    
    public static void AddItem(
        int itemID, int quantity, int slotID, bool isStackable, HouseDetails isInHouse, Chest chest
    ) {
        if (!nearbyItems.TryGetValue(itemID, out var info)) info = new ItemInfo();
        var source = new ItemStack();

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


    public class ItemInfo {
        public int quantity;
        public List<ItemStack> sources = new();
    }

    public class ItemStack {
        public Chest chest;
        public int fuel;
        public HouseDetails inPlayerHouse;
        public bool playerInventory;
        public int quantity;
        public int slotID;
    }
}
