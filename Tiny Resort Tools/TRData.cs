using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace TinyResort {

    public static class TRData {

        private static int DataVersion;
        public static Dictionary<string, ModData> Data = new Dictionary<string, ModData>();

        private static int currentSlot => SaveLoad.saveOrLoad.currentSaveSlotNo();
        private static string dataPath => Path.Combine(Application.persistentDataPath, "Slot" + currentSlot, "Mod Data");

        /// <summary>
        /// This event will run just BEFORE the game saves. So, if you are adding things to the game that will break a normal save,
        /// subscribe to this function and remove the offending data temporarily. You can re-add the data in a different function
        /// that is called through postSave.
        /// </summary>
        public static OnSave preSave;
        public delegate void OnSave();

        /// <summary>
        /// This event will run just AFTER the game saves. So, if you are adding things to the game data that will
        /// break a normal save, then you want to add them in a function that is called by postSave.
        /// </summary>
        public static PostSave postSave;
        public delegate void PostSave();

        /// <summary> This event will run just AFTER a save slot is loaded. </summary>
        public static OnLoad onLoad;
        public delegate void OnLoad();

        /// <summary>Subscribes to the save system so that your mod data is saved and loaded properly.</summary>
        /// <param name="pluginGuid">The GUID of your plugin.</param>
        /// <returns>A link to the data for your mod. Keep a reference to this. It is used to get and set all saved values.</returns>
        public static ModData Subscribe(string pluginGuid) {
            Data[pluginGuid] = new ModData();
            Data[pluginGuid].Package = new DataPackage();
            preSave += () => Save(pluginGuid);
            onLoad += () => Load(pluginGuid);
            return Data[pluginGuid];
        }

        /// <summary>Saves your mods data to a file in the save game slot folder.</summary>
        /// <param name="pluginGuid">The GUID of your plugin.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Save(string pluginGuid) {

            if (TRTools.InMainMenu) {
                TRTools.LogToConsole("Can not save or load mod data until a save slot has been loaded.");
                return;
            }

            // Creates the folder and finds the save file location
            Directory.CreateDirectory(dataPath);
            var savePath = Path.Combine(dataPath, pluginGuid + ".sav");

            // Back up the previous save file if one exists
            if (File.Exists(savePath)) {
                var backupPath = Path.Combine(dataPath, "backup_" + pluginGuid + ".sav");
                if (File.Exists(backupPath)) { File.Delete(backupPath);}
                File.Copy(savePath, backupPath);
            }

            // Records important information about this save
            Data[pluginGuid].Package.SaveTime = DateTime.Now;
            Data[pluginGuid].Package.DataVersion = DataVersion;

            // Creates file
            var Formatter = new BinaryFormatter();
            var NewFile = File.Create(savePath);
            Formatter.Serialize(NewFile, Data[pluginGuid].Package);
            NewFile.Close();

        }

        /// <summary> Loads your mod's save data (or creates it if none exist) </summary>
        /// <param name="pluginGuid">The GUID of your plugin.</param>
        /// <returns>Your mod's data.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Load(string pluginGuid) {

            if (TRTools.InMainMenu) {
                TRTools.LogToConsole(pluginGuid + " is trying to load mod data before the player has loaded into a save slot.", LogSeverity.Error);
                return;
            }

            if (!Data.ContainsKey(pluginGuid)) {
                TRTools.LogToConsole(pluginGuid + " is trying to load mod data before subscribing.", LogSeverity.Error);
                return;
            }

            // If there is no save file, create new mod data
            if (!File.Exists(Path.Combine(dataPath, pluginGuid + ".sav"))) { Data[pluginGuid].Package = new DataPackage(); }

            // Otherwise, load the latest save file as the mod data
            else {
                var Formatter = new BinaryFormatter();
                var LoadedFile = File.Open(Path.Combine(dataPath, pluginGuid + ".sav"), FileMode.Open);
                Data[pluginGuid].Package = (DataPackage) Formatter.Deserialize(LoadedFile);
                LoadedFile.Close();
            }

        }

    }

    [Serializable]
    public class ModData {
        
        public DataPackage Package = new DataPackage();

        /*public void SetInt(string name, int value) { Package.savedInts[name] = value; }
        public int GetInt(string name) {
            if (Package.savedInts.TryGetValue(name, out var val)) { return val; }
            SetInt(name, 0);
            return 0;
        }
        public void SetBool(string name, bool value) { Package.savedBools[name] = value; }
        public bool GetBool(string name) {
            if (Package.savedBools.TryGetValue(name, out var val)) { return val; }
            SetBool(name, false);
            return false;
        }
        
        public void SetFloat(string name, float value) { Package.savedFloats[name] = value; }
        public float GetFloat(string name) {
            if (Package.savedFloats.TryGetValue(name, out var val)) { return val; }
            SetFloat(name, 0f);
            return 0f;
        }
        
        public void SetString(string name, string value) { Package.savedStrings[name] = value; }
        public string GetString(string name) {
            if (Package.savedStrings.TryGetValue(name, out var val)) { return val; }
            SetString(name, "");
            return "";
        }*/
        
        public void SetValue(string name, object value) { Package.data[name] = value; }
        public object GetValue(string name) {
            if (Package.data.TryGetValue(name, out var val)) { return val; }
            return null;
        }
        
    }

    [Serializable]
    public class DataPackage {
        public DateTime SaveTime;
        public int DataVersion;
        //public Dictionary<string, int> savedInts = new Dictionary<string, int>();
        //public Dictionary<string, bool> savedBools = new Dictionary<string, bool>();
        //public Dictionary<string, float> savedFloats = new Dictionary<string, float>();
        //public Dictionary<string, string> savedStrings = new Dictionary<string, string>();
        public Dictionary<string, object> data = new Dictionary<string, object>();
    }

}