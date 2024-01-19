using BepInEx;
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

    internal static RectTransform GetInventorySlotPositions() {
        GameObject inventoryWindows = GameObject.Find("Canvas/Menu/");
        return inventoryWindows.GetComponent<RectTransform>();
    }
    
    public static void CreateInventoryButtons() {
        InventoryMenu = GameObject.Find("Canvas/Menu");
        float NumberOfSlotsPerRow = 0;
        float adjustment = 0;
        GameObject InventorySlots = GameObject.Find("Canvas/Menu/InventoryWindows");
        if (InventorySlots.transform.GetChild(32).gameObject.activeInHierarchy) {
            NumberOfSlotsPerRow = 11;
        }
        else if (InventorySlots.transform.GetChild(28).gameObject.activeInHierarchy){
            NumberOfSlotsPerRow = 10;
            adjustment = 5f;
        }
        else if (InventorySlots.transform.GetChild(25).gameObject.activeInHierarchy) {
            NumberOfSlotsPerRow = 9;
            adjustment = 10f;
        }
        else {
            NumberOfSlotsPerRow = 8;
            adjustment = 15f;
        }
        Grid = new GameObject();
        Grid.name = "Inventory Management Grid";
        try { Grid.transform.SetParent(InventoryMenu.transform); }
        catch { return; }
        Grid.transform.SetAsLastSibling();
        gridLayoutGroup = Grid.AddComponent<GridLayoutGroup>();
        Grid.AddComponent<CanvasRenderer>();
        
        float x_distance = (270f / 11f) * NumberOfSlotsPerRow - adjustment;
        Grid.transform.localPosition = new Vector3(-x_distance, -160, 0);
        gridLayoutGroup.cellSize = new Vector2(52, 30);
        gridLayoutGroup.spacing = new Vector2(8, 2);
        gridLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = 3;
        
        rect = Grid.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect = GetInventorySlotPositions();
        
        SendToChests = TRInterface.CreateButton(ButtonTypes.MainMenu, Grid.transform, "Send to Chests", SortItems.SortToChests);
        SendToChests.textMesh.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 10);
        SendToChests.textMesh.fontSize = 8;
        SendToChests.name = "Send To Chest Button (TR)";

        SortInventory = TRInterface.CreateButton(ButtonTypes.MainMenu, Grid.transform, "Sort\nBag", SortItems.SortInventory);
        SortInventory.rectTransform.sizeDelta = new Vector2(50, 10);
        SortInventory.textMesh.fontSize = 8;
        SortInventory.name = "Sort Inventory Button (TR)";
    }

    public static void CreateChestButtons() {
        ChestWindowLayout = GameObject.Find("Canvas/ChestWindow/Contents");
        GameObject InventorySlots = GameObject.Find("Canvas/Menu/InventoryWindows");
        
        Grid = new GameObject();
        Grid.name = "Chest Window Grid";
        try { Grid.transform.SetParent(ChestWindowLayout.transform); }
        catch { return; }
        Grid.transform.SetAsLastSibling();
        gridLayoutGroup = Grid.AddComponent<GridLayoutGroup>();
        Grid.AddComponent<CanvasRenderer>();

        Grid.transform.localPosition = new Vector3(155, 137, 0);

        gridLayoutGroup.cellSize = new Vector2(52, 40);
        gridLayoutGroup.spacing = new Vector2(8, 2);
        gridLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = 1;

        rect = Grid.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect = ChestWindowLayout.GetComponent<RectTransform>();
        
        try { SortChest = TRInterface.CreateButton(ButtonTypes.MainMenu, Grid.transform, "Sort\nChest", SortItems.SortChest); }
        catch { return; }
        SortChest.name = "Sort Chest Button (TR)";
        
        SortChest.textMesh.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 38);
        SortChest.background.color = new Color(0.502f, 0.3569f, 0.2353f, 1f);
        SortChest.textMesh.fontSize = 10;
    }
    
}
