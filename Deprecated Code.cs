namespace TinyResort; 

public class Deprecated_Code {
    /*
public static IEnumerator ParseAllItemsRoutine() {
    findingNearbyChests = true;
    FindNearbyChests();
    if (clientInServer) {
        yield return new WaitForSeconds(.5f);
        Plugin.Log($"Probably Locked - CountRemaining: {unconfirmedChests.Count}");
    } //WaitUntil(() => unconfirmedChests.Count <= 0); }
    

    // Get all items in nearby chests
    foreach (var ChestInfo in nearbyChests)
        for (var i = 0; i < ChestInfo.chest.itemIds.Length; i++)
            if (ChestInfo.chest.itemIds[i] != -1 && TRItems.GetItemDetails(ChestInfo.chest.itemIds[i]))
                AddItem(
                    ChestInfo.chest.itemIds[i], ChestInfo.chest.itemStacks[i], i,
                    TRItems.GetItemDetails(ChestInfo.chest.itemIds[i]).checkIfStackable(), ChestInfo.house,
                    ChestInfo.chest
                );
    findingNearbyChests = false;
    OnFinishedParsing?.Invoke();
    OnFinishedParsing = null;
}*/

    /*[HarmonyPostfix]
public static void UserCode_TargetOpenChestPostfix(
    ContainerManager __instance, int xPos, int yPos, int[] itemIds, int[] itemStack
) {
    // TODO: Get proper house details
    if (unconfirmedChests.TryGetValue((xPos, yPos), out var house)) {
        unconfirmedChests.Remove((xPos, yPos));
        AddChest(xPos, yPos, house);
    }
}*/

    /*public static void ParseAllItems() {
        instance.StopAllCoroutines();
        instance.StartCoroutine(ParseAllItemsRoutine());
    }*/

    /*Plugin.QuickPatch(
         typeof(ContainerManager), "UserCode_TargetOpenChest", typeof(InventoryManagement), null,
         "UserCode_TargetOpenChestPostfix"
     );*/
    
    //    public static bool openChestInWindowPrefix() => !findingNearbyChests;
    /*Plugin.QuickPatch(
    typeof(ChestWindow), "openChestInWindow", typeof(InventoryManagement), "openChestInWindowPrefix"
    );*/
    
    
    /*
    public static void FindNearbyChests() {
        var chests = new List<(ChestPlaceable chest, bool insideHouse)>();
        nearbyChests.Clear();

        for (var i = 0; i < HouseManager.manage.allHouses.Count; i++)
            if (HouseManager.manage.allHouses[i].isThePlayersHouse)
                playerHouse = HouseManager.manage.allHouses[i];

        playerPosition = NetworkMapSharer.Instance.localChar.myInteract.transform.position;

        // Gets chests inside houses
        if (ClientInsideHouse || !clientInServer) {
            var chestsInsideHouse = Physics.OverlapBox(
                new Vector3(playerPosition.x, -88, playerPosition.z),
                new Vector3(radius.Value * 2, 5, radius.Value * 2), Quaternion.identity,
                LayerMask.GetMask(LayerMask.LayerToName(15))
            );
            for (var i = 0; i < chestsInsideHouse.Length; i++) {
                var chestComponent = chestsInsideHouse[i].GetComponentInParent<ChestPlaceable>();
                if (chestComponent == null) continue;
                if (chestComponent.gameObject.name == "RecyclingBox(Clone)") continue;
                chests.Add((chestComponent, true));
            }
        }

        // Gets chests in the overworld
        if (ClientOutsideHouse || !clientInServer) {
            var chestsOutside = Physics.OverlapBox(
                new Vector3(playerPosition.x, -7, playerPosition.z),
                new Vector3(radius.Value * 2, 20, radius.Value * 2), Quaternion.identity,
                LayerMask.GetMask(LayerMask.LayerToName(15))
            );
            for (var j = 0; j < chestsOutside.Length; j++) {
                var chestComponent = chestsOutside[j].GetComponentInParent<ChestPlaceable>();
                if (chestComponent == null) continue;
                if (chestComponent.gameObject.name == "RecyclingBox(Clone)") continue;
                chests.Add((chestComponent, false));
            }
        }

        for (var k = 0; k < chests.Count; k++) {

            var tempX = chests[k].chest.myXPos();
            var tempY = chests[k].chest.myYPos();

            var house = chests[k].insideHouse ? playerHouse : null;

            if (clientInServer) {
                unconfirmedChests[(tempX, tempY)] = house;
                NetworkMapSharer.Instance.localChar.myPickUp.CmdOpenChest(tempX, tempY);
                NetworkMapSharer.Instance.localChar.CmdCloseChest(tempX, tempY);
            }
            else {
                ContainerManager.manage.checkIfEmpty(tempX, tempY, house);
                AddChest(tempX, tempY, house);
            }
        }
    }*/

    /*
    private static void AddChest(int xPos, int yPos, HouseDetails house) {
        nearbyChests.Add((ContainerManager.manage.activeChests.First(i => i.xPos == xPos && i.yPos == yPos), house));
        nearbyChests = nearbyChests.Distinct().ToList();
    } */
}
