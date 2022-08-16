using System;
using System.Collections.Generic;
using BepInEx;
using UnityEngine;
using UnityEngine.UI;
    
namespace TinyResort {

    public class TRMap : MonoBehaviour {

        private static List<MapMarker> AvailableMarkers = new List<MapMarker>();
        private static Dictionary<string, List<MapMarker>> MarkersInUse = new Dictionary<string, List<MapMarker>>();

        /// <summary> Gets a list of usable markers, removing excess markers or creating new ones as necessary </summary>
        /// <param name="category">Helps organize markers by mod and use.</param>
        /// <param name="desiredMarkerCount">How many markers are needed at the moment.</param>
        /// <param name="markerSprite">Sprite to use for the map marker.</param>
        /// <param name="markerSize">Width and height of the map marker in pixels.</param>
        /// <returns>A list of all active map markers.</returns>
        public static List<MapMarker> RefreshPool(string category, int desiredMarkerCount, Sprite markerSprite, float markerSize) {
            var markers = GetMarkers(category);
            while (desiredMarkerCount > markers.Count) { CreateMarker(category, markerSprite, markerSize); }
            while (markers.Count > desiredMarkerCount) { ReleaseMarker(category, markers[0]); }
            return markers;
        }

        /// <summary> Positions a map marker based on an object's world position. </summary>
        /// <param name="category">Helps organize markers by mod and use.</param>
        /// <param name="index">The index of the marker you want to reposition.</param>
        /// <param name="worldPosition">The world position of the object (or location) the marker represents.</param>
        public static void SetMarkerPosition(string category, int index, Vector3 worldPosition) {
            MarkersInUse[category][index].mainRect.localPosition = new Vector2(worldPosition.x / 2f / RenderMap.map.mapScale, worldPosition.z / 2f / RenderMap.map.mapScale);
        }

        /// <summary> Changes the marker's tint color. </summary>
        /// <param name="category">Helps organize markers by mod and use.</param>
        /// <param name="index">The index of the marker you want to reposition.</param>
        /// <param name="newColor">The tint you want the marker to have. Pure white uses the sprite as is.</param>
        public static void SetMarkerColor(string category, int index, Color newColor) { MarkersInUse[category][index].markerImage.color = newColor; }

        /// <summary> Creates a new map marker. </summary>
        /// <param name="category">Helps organize markers by mod and use.</param>
        /// <param name="markerSprite">Sprite to use for the map marker.</param>
        /// <param name="markerSize">Width and height of the map marker in pixels.</param>
        /// <returns>The map marker.</returns>
        public static MapMarker CreateMarker(string category, Sprite markerSprite, float markerSize) {

            // If there's a dot that's not in use, return it
            MapMarker marker;
            if (AvailableMarkers.Count > 0) {
                marker = AvailableMarkers[0];
                AvailableMarkers.RemoveAt(0);
            }

            // Otherwise, create a new dot
            else {

                // Copy the player marker and put it above the player marker to make sure the player marker is drawn on top
                var GO = Instantiate(RenderMap.map.charPointer.gameObject, RenderMap.map.mapParent);
                GO.transform.SetSiblingIndex(RenderMap.map.charPointer.GetSiblingIndex());

                // Set up the position, size, rotation and art for the marker
                marker = new MapMarker();
                marker.mainRect = GO.GetComponent<RectTransform>();
                marker.mainRect.rotation = Quaternion.identity;
                marker.markerImage = marker.mainRect.transform.GetChild(0).GetChild(0).GetComponent<Image>();
                marker.markerImage.sprite = markerSprite;
                marker.markerImage.rectTransform.sizeDelta = Vector2.one * markerSize;

            }

            // Activates the marker and adds to it to the category's marker list
            marker.mainRect.gameObject.SetActive(true);
            if (!MarkersInUse.ContainsKey(category)) { MarkersInUse[category] = new List<MapMarker>(); }
            MarkersInUse[category].Add(marker);
            
            return marker;

        }

        /// <summary> Disables the marker and adds it back to the object pool for future reuse. </summary>
        /// <param name="category">Helps organize markers by mod and use.</param>
        /// <param name="marker">The marker that should be released.</param>
        public static void ReleaseMarker(string category, MapMarker marker) {
            MarkersInUse[category].Remove(marker);
            AvailableMarkers.Add(marker);
            marker.mainRect.gameObject.SetActive(false);
        }

        /// <summary> Gets all map markers in the given category </summary>
        /// <param name="category">Helps organize markers by mod and use.</param>
        /// <returns>A list of all active map markers.</returns>
        public static List<MapMarker> GetMarkers(string category) {
            if (!MarkersInUse.ContainsKey(category)) { MarkersInUse[category] = new List<MapMarker>(); }
            return MarkersInUse[category];
        }

        // Keeps icons an appropriate scale to avoid issues when zooming in and out
        public static void FixMarkerScale() {
            foreach (var keyValuePair in MarkersInUse) {
                foreach (var marker in MarkersInUse[keyValuePair.Key]) {
                    marker.mainRect.localScale = Vector2.one * 0.2f;
                    marker.markerImage.rectTransform.localScale = Vector2.one;
                }
            }
        }

    }

    public class MapMarker {
        public RectTransform mainRect;
        public Image markerImage;
    }

}
