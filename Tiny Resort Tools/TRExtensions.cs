using UnityEngine;

namespace TinyResort {

    public static class TRExtensions {
        
        /// <summary>
        /// Allows you to copy any class by value rather than reference in order to change things about a copy without changing the original.
        /// </summary>
        public static T DeepCopy<T>(this T classToCopy) where T : class => JsonUtility.FromJson<T>(JsonUtility.ToJson(classToCopy));

        /// <summary> Creates an exact copy of a component and adds it to the gameobject. </summary>
        public static T CopyComponent<T>(this T original, GameObject obj) where T : Component {
            var type = original.GetType();
            var copy = obj.AddComponent(type);
            var fields = type.GetFields();
            foreach (var field in fields) { field.SetValue(copy, field.GetValue(original)); }
            return copy as T;
        }
        
    }

}
