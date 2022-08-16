using System.Collections.Generic;

namespace TinyResort {

    public class TRItems {

        private static Dictionary<int, InventoryItem> itemDetails = new Dictionary<int, InventoryItem>();
        private static void InitializeItemDetails() {
            foreach (var item in Inventory.inv.allItems) {
                var id = item.getItemId();
                itemDetails[id] = item;
            }
        }

        public static bool DoesItemExist(int itemID) {
            if (itemDetails.Count <= 0) { InitializeItemDetails(); }
            return itemID >= 0 && itemDetails.ContainsKey(itemID);
        }

        public static InventoryItem GetItemDetails(int itemID) {
            if (itemDetails.Count <= 0) { InitializeItemDetails(); }
            if (itemID < 0) {
                TRTools.LogToConsole("Attempting to get item details for item with ID of " + itemID + " which does not exist.");
                return null;
            }
            return itemDetails[itemID];
        }
        
    }

}
