using System.Linq;
using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

namespace TinyResort {

    public class TRDrawing : MonoBehaviour {

        public static void Initialize() { }

        /*public static GameObject MapCanvas;
        private static GameObject BlankOptionsMenu;
        public Image background;

        public static void Initialize() {
            
            /*Debug.Log("INITIALIZING MOD WINDOW: " + SceneManager.GetActiveScene().name);

            // Creates a canvas object identical to the Dinkum MapCanvas object
            MapCanvas = new GameObject();
            MapCanvas.AddComponent<Canvas>();
            MapCanvas.AddComponent<GraphicRaycaster>();
            var scal = MapCanvas.AddComponent<CanvasScaler>();
            scal.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scal.referenceResolution = new Vector2(1200, 800);
            scal.matchWidthOrHeight = 0.4f;

            // Finds the Dinkum OptionsWindow and uses it to get all the parts of an options menu
            var WindowAnimators = Resources.FindObjectsOfTypeAll<WindowAnimator>().ToList();
            Debug.Log("ANIMATORS COUNT: " + WindowAnimators.Count);
            foreach (var Anim in WindowAnimators) {
                if (Anim.gameObject.name == "OptionWindow" && !Anim.gameObject.GetComponent<VerticalLayoutGroup>()) {
                    BlankOptionsMenu = Instantiate(Anim.gameObject, MapCanvas.transform);
                    Debug.Log("FOUND OPTION WINDOW");
                    break;
                }
            }
            
        }

        public static TRDrawing CreateOptionsMenu(Vector2 topLeftPosition, Vector2 size) {
            var GO = new GameObject("Mod Window");
            GO.AddComponent<TRDrawing>();
            var window = new TRDrawing();
            
            return window;
        }

        public static ModButton CreateButton() {
            return new ModButton();
        }

        public class ModButton { }*/

        /// <summary>
        /// Creates a sprite in the shape of a circle with a border.
        /// </summary>
        /// <param name="pixelRadius">The radius of the circle (in pixels).</param>
        /// <param name="mainColor">The color of the circle.</param>
        /// <param name="borderThickness">Thickness of the border to the circle (in pixels). Set to 0 to have no border.</param>
        /// <param name="borderColor">Color of the border.</param>
        /// <returns>The sprite.</returns>
        public static Sprite CircleSprite(int pixelRadius, Color mainColor, float borderThickness, Color borderColor) {

            Texture2D tex = new Texture2D(pixelRadius * 2, pixelRadius * 2, TextureFormat.RGBA32, false);

            for (int u = 0; u < 2 * pixelRadius + 1; u++) {
                for (int v = 0; v < 2 * pixelRadius + 1; v++) {
                    var pos = Mathf.Pow(pixelRadius - u, 2) + Mathf.Pow(pixelRadius - v, 2);
                    if (pos < Mathf.Pow(pixelRadius, 2)) { tex.SetPixel(u, v, pos < Mathf.Pow(pixelRadius - borderThickness, 2) ? mainColor : borderColor); }
                    else { tex.SetPixel(u, v, Color.clear); }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(Vector2.zero, Vector2.one * 2 * pixelRadius), Vector2.one * 0.5f);

        }

    }

}
