using UnityEditor;
using UnityEngine;

namespace yukineko.WeatherAndSolarSystem.Editor
{
    public static class EditorItem
    {
        public const string menuParent = "GameObject/Weather And Solar System/";

        public static void GenerateObject(string guid)
        {
            var item = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null) return;

            var prefab = PrefabUtility.InstantiatePrefab(item, Selection.activeTransform);
            if (prefab == null) return;

            Selection.activeGameObject = prefab as GameObject;
        }

        [MenuItem(menuParent + "Day Cycle System")]
        public static void CreateDayCycleSystem() => GenerateObject("8d0f9521acfe93248b9d4790c617a41f");

        [MenuItem(menuParent + "Weather System")]
        public static void CreateWeatherSystem() => GenerateObject("e5c8645bd95be1940a84469d8145baaf");
    }
}