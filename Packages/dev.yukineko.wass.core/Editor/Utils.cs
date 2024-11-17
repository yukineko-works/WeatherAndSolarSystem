using UnityEditor;
using UnityEngine;

namespace yukineko.WeatherAndSolarSystem.Editor
{
    internal static class Utils
    {
        public static bool IsSupportedSkybox()
        {
            var skybox = RenderSettings.skybox;
            if (skybox != null)
            {
                switch (skybox.shader.name)
                {
                    case "CaminoVR/Skybox":
                    case "Typhon/SkyBox1.1":
                        return true;
                }
            }

            return false;
        }

        public static void OpenSupportedSkyboxPage()
        {
            Application.OpenURL("https://vpm.yukineko.dev/docs/wass-core/supported-skybox");
        }
    }
}