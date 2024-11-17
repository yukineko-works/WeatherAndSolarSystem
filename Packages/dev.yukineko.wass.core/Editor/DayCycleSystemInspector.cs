using UnityEditor;
using UnityEngine;

namespace yukineko.WeatherAndSolarSystem.Editor
{
    [CustomEditor(typeof(DayCycleSystem))]
    public class DayCycleSystemInspector : UnityEditor.Editor
    {
        private bool _usingSupportedSkybox = false;

        public void OnEnable()
        {
            _usingSupportedSkybox = Utils.IsSupportedSkybox();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("リアルタイム太陽システム", EditorStyles.largeLabel);

            if (!_usingSupportedSkybox)
            {
                YNUI.InfoBox("リアルタイム対応システムは以下のSkyboxをはじめとする、プロシージャルSkybox向けに制作したギミックです。\n現在のSkyboxでは、正常に動作をしない可能性があります。", MessageType.Warning);
                if (GUILayout.Button("対応しているSkybox一覧"))
                {
                    Utils.OpenSupportedSkyboxPage();
                }
            }

            YNUI.TitleBox("基本設定");

            var sunLight = serializedObject.FindProperty("_sunLight");
            EditorGUILayout.PropertyField(sunLight, new GUIContent("DirectionalLight (太陽)"));

            if (sunLight.objectReferenceValue == null)
            {
                YNUI.InfoBox("DirectionalLight (太陽) が設定されていません", MessageType.Error);
            }

            var moonLight = serializedObject.FindProperty("_moonLight");
            EditorGUILayout.PropertyField(moonLight, new GUIContent("DirectionalLight (月)"));

            if (moonLight.objectReferenceValue == null)
            {
                YNUI.InfoBox("DirectionalLight (月) の設定は任意ですが、設定することで月光の表現を行うことができます");
            }

            YNUI.TitleBox("太陽の位置に関する設定", "太陽の位置の計算に使用するパラメータを設定できます");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_latitude"), new GUIContent("緯度"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_longitude"), new GUIContent("経度"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_timezone"), new GUIContent("タイムゾーン"));

            YNUI.InfoBox("UTCとの時差を設定できます。\n日本の場合は+9です。");

            serializedObject.FindProperty("_fixedAngle").floatValue = EditorGUILayout.Slider("角度補正 (度)", serializedObject.FindProperty("_fixedAngle").floatValue, -180f, 180f);

            YNUI.InfoBox("太陽が昇ってくる(または沈む)方向を補正することができます。");

            YNUI.TitleBox("ライティング設定", "太陽・月の光の強さや、色温度に関する設定を行うことができます");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_standardMinHeightAngle"), new GUIContent("項目の値が最小になる角度"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_standardMaxHeightAngle"), new GUIContent("項目の値が最大になる角度"));
            EditorGUILayout.Space();

            if (sunLight.objectReferenceValue != null)
            {
                var sunIntensityControlEnabled = serializedObject.FindProperty("_sunIntensityControlEnabled");
                EditorGUILayout.PropertyField(sunIntensityControlEnabled, new GUIContent("太陽の光の強さを制御する"));
                if (sunIntensityControlEnabled.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_sunIntensityMin"), new GUIContent("Intensity (最小)"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_sunIntensityMax"), new GUIContent("Intensity (最大)"));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space();

                var sunTemperatureControlEnabled = serializedObject.FindProperty("_sunTemperatureControlEnabled");
                EditorGUILayout.PropertyField(sunTemperatureControlEnabled, new GUIContent("太陽の色温度を制御する"));
                if (sunTemperatureControlEnabled.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_sunTemperatureMin"), new GUIContent("Color Temperature (最小)"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_sunTemperatureMax"), new GUIContent("Color Temperature (最大)"));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space();
            }

            if (moonLight.objectReferenceValue != null)
            {
                var moonIntensityControlEnabled = serializedObject.FindProperty("_moonIntensityControlEnabled");
                EditorGUILayout.PropertyField(moonIntensityControlEnabled, new GUIContent("月の光の強さを制御する"));
                if (moonIntensityControlEnabled.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_moonIntensityMin"), new GUIContent("Intensity (最小)"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_moonIntensityMax"), new GUIContent("Intensity (最大)"));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space();

                var moonTemperatureControlEnabled = serializedObject.FindProperty("_moonTemperatureControlEnabled");
                EditorGUILayout.PropertyField(moonTemperatureControlEnabled, new GUIContent("月の色温度を制御する"));
                if (moonTemperatureControlEnabled.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_moonTemperatureMin"), new GUIContent("Color Temperature (最小)"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_moonTemperatureMax"), new GUIContent("Color Temperature (最大)"));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space();
            }

            YNUI.TitleBox("その他の設定");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_updateFrequency"), new GUIContent("更新頻度 (秒)"));

            var debugMode = serializedObject.FindProperty("_debugMode");
            EditorGUILayout.PropertyField(debugMode, new GUIContent("デバッグモード"));
            if (debugMode.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_debugTimeValue"), new GUIContent("時間調整"));
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}