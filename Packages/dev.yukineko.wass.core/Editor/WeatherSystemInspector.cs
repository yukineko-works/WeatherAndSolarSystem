using UnityEditor;
using UnityEngine;

namespace yukineko.WeatherAndSolarSystem.Editor
{
    [CustomEditor(typeof(WeatherSystem))]
    public class WeatherSystemInspector : UnityEditor.Editor
    {
        private bool _showMonthlySunnyPercentage = false;
        private bool _usingSupportedSkybox = false;

        public void OnEnable()
        {
            UpdateSkyboxInfo();
        }

        private void UpdateSkyboxInfo()
        {
            _usingSupportedSkybox = Utils.IsSupportedSkybox();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("天候システム", EditorStyles.largeLabel);

            YNUI.TitleBox("天気に関する設定", "晴天率や変更間隔などを設定できます");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_weatherChangeSpanMinutes"), new GUIContent("天気が変わる間隔 (分)"));
            _showMonthlySunnyPercentage = EditorGUILayout.Foldout(_showMonthlySunnyPercentage, "月ごとの晴天確率 (%)");
            if (_showMonthlySunnyPercentage)
            {
                var monthlySunnyPercentage = serializedObject.FindProperty("_monthlySunnyPercentage");
                if (monthlySunnyPercentage.arraySize != 12)
                {
                    monthlySunnyPercentage.arraySize = 12;
                }

                EditorGUI.indentLevel++;
                for (var i = 0; i < monthlySunnyPercentage.arraySize; i++)
                {
                    var percentage = monthlySunnyPercentage.GetArrayElementAtIndex(i);
                    percentage.intValue = EditorGUILayout.IntSlider($"{i + 1}月", percentage.intValue, 0, 100);
                }
                EditorGUI.indentLevel--;
            }

            YNUI.TitleBox("GameObjectに関する設定", "天気に応じて有効にするGameObjectを設定できます");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_weatherAssetContainerSunny"), new GUIContent("晴れ"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_weatherAssetContainerCloudy"), new GUIContent("曇り"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_weatherAssetContainerRainy"), new GUIContent("雨"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_weatherAssetContainerSnowy"), new GUIContent("雪"));

            YNUI.TitleBox("Skyboxに関する設定", "天気に応じてSkyboxの雲の量を変更できます");

            if (!_usingSupportedSkybox)
            {
                YNUI.InfoBox("非対応のSkyboxが設定されているため、以下の設定は反映されません。", MessageType.Warning, false);
                UpdateSkyboxInfo();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("設定を開く"))
                {
                    EditorApplication.ExecuteMenuItem("Window/Rendering/Lighting");
                }
                if (GUILayout.Button("対応しているSkybox一覧"))
                {
                    Utils.OpenSupportedSkyboxPage();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }

            GUI.enabled = _usingSupportedSkybox;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_skyboxSunnyCloudCoverage"), new GUIContent("晴れの場合の雲の量 (%)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_skyboxCloudyCloudCoverage"), new GUIContent("曇りの場合の雲の量 (%)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_skyboxRainyCloudCoverage"), new GUIContent("雨/雪の場合の雲の量 (%)"));
            GUI.enabled = true;

            YNUI.TitleBox("その他の設定");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_randomSeed"), new GUIContent("シード値"));
            EditorGUILayout.Space();
            if (GUILayout.Button("ランダムな値に変更"))
            {
                serializedObject.FindProperty("_randomSeed").uintValue = (uint)Random.Range(0, uint.MaxValue);
            }

            YNUI.InfoBox("シード値を指定することで、このギミックを導入している他のワールドと異なる(または同じ)天気にすることができます。");

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}