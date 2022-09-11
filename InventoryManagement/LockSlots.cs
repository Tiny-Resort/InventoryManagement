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

    public class LockSlots {


        public static List<int> lockedSlots = new List<int>();
        public static Color LockedSlotColor = new Color(.5f, .5f, .5f, 1f);

        public static int tempCurrentIgnore;
        public static bool checkIfLocking;
        public static Color defaultColor = Color.white;


        
        public static bool moveCursorPrefix(Inventory __instance) {
            if (InputMaster.input.UISelect()) {
                if (Input.GetKey(InventoryManagement.lockSlotKeybind.Value) && Input.GetMouseButtonDown(0)) { // Update to use different key combination so you can do it while not picking up item?
                    InventorySlot slot = __instance.cursorPress();
                    if (slot != null) {
                        InventoryManagement.Plugin.Log($"Slot: {slot.transform.parent.gameObject.name}");
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
                InventoryManagement.ignoreSpecifcSlots.Value = null;
                foreach (var element in lockedSlots) {
                    var tempString = InventoryManagement.ignoreSpecifcSlots == null ? element + "," : InventoryManagement.ignoreSpecifcSlots.Value + element + ",";
                    InventoryManagement.ignoreSpecifcSlots.Value = tempString;
                }
                InventoryManagement.ignoreSpecifcSlots.ConfigFile.Save();
                for (int i = 0; i < Inventory.inv.invSlots.Length; i++) {
                    if (lockedSlots.Contains(i)) { Inventory.inv.invSlots[i].GetComponent<Image>().color = LockedSlotColor; }
                }
                if (checkIfLocking) { return false; }
            }
            checkIfLocking = false;
            return true;
        }
    }

}