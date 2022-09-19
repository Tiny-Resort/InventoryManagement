using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
        public const string pluginVersion = "0.8.3";
        private static InventoryManagement instance;

        public delegate void ParsingEvent();
        public static ParsingEvent OnFinishedParsing;

        public static ConfigEntry<int> radius;
        public static ConfigEntry<bool> ignoreHotbar;
        public static ConfigEntry<string> ignoreSpecifcSlots;
        public static ConfigEntry<KeyCode> exportKeybind;
        public static ConfigEntry<KeyCode> exportControllerKeybind;
        public static ConfigEntry<KeyCode> lockSlotKeybind;
        public static ConfigEntry<KeyCode> lockSlotControllerKeybind;

        public static ConfigEntry<string> sortingOrder;
        public static ConfigEntry<KeyCode> sortKeybind;
        public static ConfigEntry<KeyCode> sortControllerKeybind;

        public static ConfigEntry<bool> alwaysInventory;
        public static ConfigEntry<float> warnPercentage;
        public static ConfigEntry<float> swapPercentage;
        public static ConfigEntry<bool> doSwapTools;
        public static ConfigEntry<bool> showNotifications;
        public static ConfigEntry<bool> useSimilarTools;

        public static List<(Chest chest, HouseDetails house)> nearbyChests = new List<(Chest chest, HouseDetails house)>();
        private static Dictionary<(int xPos, int yPos), HouseDetails> unconfirmedChests = new Dictionary<(int xPos, int yPos), HouseDetails>();
        public static Dictionary<int, ItemInfo> nearbyItems = new Dictionary<int, ItemInfo>();
        public static HouseDetails playerHouse;
        public static Vector3 currentPosition;

        public static int tempCurrentIgnore;
        public static int totalDeposited = 0;
        public static bool initalizedObjects = false;
        public static bool ButtonIsReady = true;
        public static bool clientInServer;
        public static bool findingNearbyChests;

        public static bool ClientInsideHouse => NetworkMapSharer.share.localChar.myInteract.insidePlayerHouse && clientInServer;
        public static bool ClientOutsideHouse => !NetworkMapSharer.share.localChar.myInteract.insidePlayerHouse && clientInServer;

        public static bool modDisabled => RealWorldTimeLight.time.underGround;

        public static GameObject button;
        public static GameObject InventoryMenu;
        public static GameObject Grid;

        public static GameObject SendToChests;
        public static GameObject SortInventory;
        public static GameObject SortChest;

        public static Vector3 playerPosition;

        public static bool LookingForButtons = true;

        public static GameObject GO;

        public static GameObject Test1;

        public void Awake() {
            instance = this;

            //TRDrawing.Instantiate("MapCanvas/MenuScreen/CornerStuff/CreditsButton");

            Plugin = TRTools.Initialize(this, 50);

            #region Configuration

            radius = Config.Bind<int>("General", "Range", 30, "Increases the range it looks for storage containers by an approximate tile count.");
            ignoreHotbar = Config.Bind<bool>("General", "IgnoreHotbar", true, "Set to true to disable auto importing from the hotbar.");
            ignoreSpecifcSlots = Config.Bind<string>("DO NOT EDIT", "IgnoreSpecificSlots", "", "DO NOT EDIT.");
            exportKeybind = Config.Bind<KeyCode>("Keybinds", "SortToChestKeybind", KeyCode.KeypadDivide, "Unity KeyCode used for sending inventory items into storage chests.");
            exportControllerKeybind = Config.Bind<KeyCode>("Keybinds", "SortToChestKeybind", KeyCode.None, "Unity KeyCode used for sending inventory items into storage chests.");

            lockSlotKeybind = Config.Bind<KeyCode>("Keybinds", "LockSlot", KeyCode.LeftAlt, "Unity KeyCode used for locking inventory slots. Use this key in combination with the left mouse button to lock the slots.");
            lockSlotControllerKeybind = Config.Bind<KeyCode>("Keybinds", "LockSlot", KeyCode.None, "Unity KeyCode used for locking inventory slots. Use this key in combination with the left mouse button to lock the slots.");

            sortingOrder = Config.Bind<string>("SortingOrder", "SortOrder", "relics,bugsorfish,clothes,vehicles,placeables,base,tools,food", "Order to sort inventory and chest items.");
            sortKeybind = Config.Bind<KeyCode>("Keybinds", "SortInventoryOrChests", KeyCode.KeypadMultiply, "Unity KeyCode used for sorting player and chest inventories.");
            sortControllerKeybind = Config.Bind<KeyCode>("Keybinds", "SortInventoryOrChests", KeyCode.None, "Unity KeyCode used for sorting player and chest inventories.");

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
                    LockSlots.lockedSlots.Add(tmpInt);
                }
            }
            string[] tmpSort = sortingOrder.Value.Trim().ToLower().Split(',');
            for (var i = 0; i < tmpSort.Length; i++) {
                Plugin.Log($"tempSort List: {tmpSort[i]}");
                SortItems.typeOrder[tmpSort[i]] = i;
            }
            foreach (var element in SortItems.typeOrder) { Plugin.Log($"Key: {element.Key} | Value: {element.Value}"); }

            initalizedObjects = false;

            #endregion

            #region Patching

            Plugin.QuickPatch(typeof(RealWorldTimeLight), "Update", typeof(InventoryManagement), "updateRWTLPrefix");
            Plugin.QuickPatch(typeof(CurrencyWindows), "openInv", typeof(InventoryManagement), null, "openInvPostfix");
            Plugin.QuickPatch(typeof(ContainerManager), "UserCode_TargetOpenChest", typeof(InventoryManagement), null, "UserCode_TargetOpenChestPostfix");
            Plugin.QuickPatch(typeof(ChestWindow), "openChestInWindow", typeof(InventoryManagement), "openChestInWindowPrefix");

            // Patches for locking slots
            Plugin.QuickPatch(typeof(Inventory), "moveCursor", typeof(LockSlots), "moveCursorPrefix");

            // Patches for tools
            Plugin.QuickPatch(typeof(Inventory), "useItemWithFuel", typeof(Tools), "useItemWithFuelPatch");
            Plugin.QuickPatch(typeof(EquipItemToChar), "equipNewItem", typeof(Tools), "equipNewItemPrefix");

            #endregion

        }

        private void Update() {
            if (WorldManager.manageWorld && LookingForButtons) {
                // LookingForButtons = !LookForButtons();
                //if (!LookingForButtons) 
                CreateButtons();
                LookingForButtons = false;
            }
            if (Test1) { Plugin.Log($"Test Info: {Test1.name}"); } 
        }

        /*public static bool LookForButtons() {
            if (!button)  button = GameObject.Find("MapCanvas/MenuScreen/CornerStuff/CreditsButton");
            if (!InventoryMenu) InventoryMenu = GameObject.Find("Canvas/Menu");
            if (button && InventoryMenu) return true;
            else return false;
        }*/

        public static void CreateButtons() {

            InventoryMenu = TRDrawing.FindObject("Canvas/Menu");

            Plugin.Log($"Menu? : {InventoryMenu.name}");
            Grid = new GameObject();
            Grid.name = "Inventory Management Grid";
            Grid.transform.SetParent(InventoryMenu.transform);
            Grid.transform.SetAsLastSibling();
            var gridLayoutGroup = Grid.AddComponent<GridLayoutGroup>();

            gridLayoutGroup.cellSize = new Vector2(52, 30);
            gridLayoutGroup.spacing = new Vector2(8, 2);
            gridLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = 3;

            var rect = Grid.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.localScale = Vector3.one;
            rect.anchoredPosition = new Vector2(-179, -475);

            Plugin.Log($"Is Button Ready? {button}");

            string ButtonLocation = "MapCanvas/MenuScreen/CornerStuff/CreditsButton";

            SendToChests = TRDrawing.AddButton(ButtonLocation, Grid, "Send To Chests", "Send to Chests", 8, RunSequence);
            /*SendToChests = TRDrawing.InstantiateObject("MapCanvas/MenuScreen/CornerStuff/CreditsButton", Grid);

            SendToChests.name = "Send To Chests";
            var STCText = SendToChests.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            STCText.text = "Send to Chests";
            STCText.fontSize = 8;*/

            //var STCClick = SendToChests.GetComponent<InvButton>();
            //var STCClick = TRDrawing.AddButton(SendToChests, RunSequence);
            
            /*STCClick.onButtonPress = new UnityEvent();
            STCClick.onButtonPress.AddListener(RunSequence);
            STCClick.isACloseButton = false;
            STCClick.isSnappable = true;*/

            SortChest = TRDrawing.InstantiateObject("MapCanvas/MenuScreen/CornerStuff/CreditsButton", Grid); 
            SortChest.name = "Sort Chest";
            var SCText = SortChest.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            SCText.text = "Sort\nChest";
            SCText.fontSize = 8;

            var SCClick = SortChest.GetComponent<InvButton>();
            SCClick.onButtonPress = new UnityEvent();
            SCClick.onButtonPress.AddListener(RunSequence);
            SCClick.isACloseButton = false;
            SCClick.isSnappable = true;

            
            SortInventory = TRDrawing.InstantiateObject("MapCanvas/MenuScreen/CornerStuff/CreditsButton", Grid);
            SortInventory.name = "Sort Inventory";
            var SIText = SortInventory.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            SIText.text = "Sort\nBag";
            SIText.fontSize = 8;

            var SIClick = SortInventory.GetComponent<InvButton>();
            SIClick.onButtonPress = new UnityEvent();
            SIClick.onButtonPress.AddListener(RunSequence);
            SIClick.isACloseButton = false;
            SIClick.isSnappable = true;
            Plugin.Log($"Made all");

            Inventory.inv.buttonsToSnapTo.Add(SortInventory.GetComponent<RectTransform>());
            Inventory.inv.buttonsToSnapTo.Add(SortChest.GetComponent<RectTransform>());
            Inventory.inv.buttonsToSnapTo.Add(SendToChests.GetComponent<RectTransform>());
            initalizedObjects = true;
            Plugin.Log($"Done");

        }

        [HarmonyPrefix]
        public static void updateRWTLPrefix(RealWorldTimeLight __instance) {
            clientInServer = !__instance.isServer;

            if (Input.GetKeyDown(exportKeybind.Value) || Input.GetKeyDown(exportControllerKeybind.Value)) {
                if (!modDisabled) {
                    RunSequence();
                    totalDeposited = 0;
                }
                else
                    TRTools.TopNotification("Inventory Management", "Send items to chests is disabled in the mines/playerhouse.");
            }
            if (Input.GetKeyDown(sortKeybind.Value) || Input.GetKeyDown(sortControllerKeybind.Value)) { SortItems.SortInventory(); }
        }

        public void LateUpdate() {
            Color tmpColor = Inventory.inv.invOpen ? LockSlots.LockedSlotColor : Color.white;
            if (LockSlots.lockedSlots.Count > 0 && Inventory.inv.inventoryWindow != null) {
                for (int i = 0; i < Inventory.inv.invSlots.Length; i++) {
                    if (LockSlots.lockedSlots.Contains(i)) { Inventory.inv.invSlots[i].GetComponent<Image>().color = tmpColor; }
                }
            }
        }

        [HarmonyPostfix]
        public static void openInvPostfix() {
            //  Inventory.inv.buttonsToSnapTo.Add(SortInventory.GetComponent<RectTransform>());
            //  Inventory.inv.buttonsToSnapTo.Add(SortChest.GetComponent<RectTransform>());
            //  Inventory.inv.buttonsToSnapTo.Add(SendToChests.GetComponent<RectTransform>());
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
                if (!LockSlots.lockedSlots.Contains(i)) {
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
            if (clientInServer) {
                yield return new WaitForSeconds(.5f);
                Plugin.Log($"Probably Locked - CountRemaining: {unconfirmedChests.Count}");
            } //WaitUntil(() => unconfirmedChests.Count <= 0); }
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
                    if (chestComponent.gameObject.name == "RecyclingBox(Clone)") continue;
                    chests.Add((chestComponent, true));
                }
            }

            // Gets chests in the overworld
            if (ClientOutsideHouse || !clientInServer) {
                Collider[] chestsOutside = Physics.OverlapBox(new Vector3(playerPosition.x, -7, playerPosition.z), new Vector3(radius.Value * 2, 20, radius.Value * 2), Quaternion.identity, LayerMask.GetMask(LayerMask.LayerToName(15)));
                for (var j = 0; j < chestsOutside.Length; j++) {
                    ChestPlaceable chestComponent = chestsOutside[j].GetComponentInParent<ChestPlaceable>();
                    if (chestComponent == null) continue;
                    if (chestComponent.gameObject.name == "RecyclingBox(Clone)") continue;
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
