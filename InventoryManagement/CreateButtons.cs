using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace TinyResort;

internal class CreateButtons {

    public static GameObject InventoryMenu;
    public static GameObject ChestWindowLayout;
    public static GameObject Grid;
    public static TRButton SendToChests;
    public static TRButton SortInventory;
    public static TRButton SortChest;
    public static GridLayoutGroup gridLayoutGroup;
    public static RectTransform rect;

    
    // TODO: Get the size of the inventory slots grid and use that as a reference point so it is always in the same spot. 
    internal static RectTransform GetInventorySlotPositions() {
        GameObject inventoryWindows = GameObject.Find("Canvas/Menu/");
        return inventoryWindows.GetComponent<RectTransform>();
    }
    
    public static void CreateInventoryButtons() {
        InventoryMenu = GameObject.Find("Canvas/Menu");
        float NumberOfSlotsPerRow = 0;
        GameObject InventorySlots = GameObject.Find("Canvas/Menu/InventoryWindows");
        if (InventorySlots.transform.GetChild(32).gameObject.activeInHierarchy) {
            NumberOfSlotsPerRow = 11;
        }
        else if (InventorySlots.transform.GetChild(26).gameObject.activeInHierarchy){
            NumberOfSlotsPerRow = 9;
        }
        else {
            NumberOfSlotsPerRow = 7;
        }
        Grid = new GameObject();
        Grid.name = "Inventory Management Grid";
        try { Grid.transform.SetParent(InventoryMenu.transform); }
        catch { return; }
        Grid.transform.SetAsLastSibling();
        gridLayoutGroup = Grid.AddComponent<GridLayoutGroup>();
        Grid.AddComponent<CanvasRenderer>();
        
        // Distance isn't perfect but better?
        float x_distance = (270f / 11f) * NumberOfSlotsPerRow;
        Grid.transform.localPosition = new Vector3(-x_distance, -160, 0);
        //gridLayoutGroup.cellSize = new Vector2(Screen.currentResolution.width * (52 / 1920), Screen.currentResolution.height * (30 / 1080));
        gridLayoutGroup.cellSize = new Vector2(52, 30);
        gridLayoutGroup.spacing = new Vector2(8, 2);
        gridLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = 3;
        
        rect = Grid.GetComponent<RectTransform>(); 
        /*rect.pivot = new Vector2(0.5f, 1);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.anchorMin = new Vector2(0.5f, 1);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = new Vector2(-210, -475);*/
        rect.localScale = Vector3.one;
        rect = GetInventorySlotPositions();
        
        SendToChests = TRInterface.CreateButton(ButtonTypes.MainMenu, Grid.transform, "Send to Chests", SortItems.SortToChests);
        SendToChests.textMesh.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 10);
        SendToChests.textMesh.fontSize = 8;

        SortInventory = TRInterface.CreateButton(ButtonTypes.MainMenu, Grid.transform, "Sort\nBag", SortItems.SortInventory);
        SortInventory.rectTransform.sizeDelta = new Vector2(50, 10);
        SortInventory.textMesh.fontSize = 8;
    }

    public static void CreateChestButtons() {
        ChestWindowLayout = GameObject.Find("Canvas/ChestWindow/Contents");

        try { SortChest = TRInterface.CreateButton(ButtonTypes.MainMenu, ChestWindowLayout.transform, "Sort\nChest", SortItems.SortChest); }
        catch { return; }
        SortChest.name = "SortChest Button (TR)";
        SortChest.rectTransform = ChestWindowLayout.GetComponent<RectTransform>();

        //SortChest.rectTransform.anchoredPosition = new Vector2(796, -193);
        //SortChest.rectTransform.sizeDelta = new Vector2(80, 38);
        SortChest.textMesh.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 38);
        SortChest.background.color = new Color(0.502f, 0.3569f, 0.2353f, 1f);
        SortChest.textMesh.fontSize = 10;
    }
    
}
