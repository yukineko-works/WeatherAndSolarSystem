using UnityEditor;
using UnityEngine;

namespace yukineko.WeatherAndSolarSystem.Editor
{
    public static class YNUI
    {
        private static GUIStyle _titleBoxStyle;
        private static GUIStyle _bgStyle;

        public static void TitleBox(string title, string description = "", bool margin = true)
        {
            if (margin)
            {
                EditorGUILayout.Space();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Box("", GetTitleBoxStyle(), GUILayout.ExpandHeight(true), GUILayout.Width(4));
            GUILayout.Space(4);
            GUILayout.BeginVertical();
            GUILayout.Space(8);
            GUILayout.Label(title, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(description))
            {
                GUILayout.Label(description, EditorStyles.miniLabel);
            }
            GUILayout.Space(8);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (margin)
            {
                EditorGUILayout.Space();
            }
        }

        private static GUIStyle GetTitleBoxStyle()
        {
            if (_titleBoxStyle == null)
            {
                _titleBoxStyle = new GUIStyle(GUI.skin.box);
                var texture = new Texture2D(1, 1);
                if (EditorGUIUtility.isProSkin)
                {
                    texture.SetPixel(0, 0, new Color(0.7f, 0.7f, 0.7f));
                }
                else
                {
                    texture.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f));
                }

                texture.Apply();
                _titleBoxStyle.normal.background = texture;
            }

            return _titleBoxStyle;
        }

        private static GUIStyle GetBgStyle()
        {
            if (_bgStyle == null)
            {
                _bgStyle = new GUIStyle(GUI.skin.box);
                var texture = new Texture2D(1, 1);
                if (EditorGUIUtility.isProSkin)
                {
                    texture.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f));
                }
                else
                {
                    texture.SetPixel(0, 0, new Color(0.7f, 0.7f, 0.7f));
                }

                texture.Apply();
                _bgStyle.normal.background = texture;
            }

            return _bgStyle;
        }

        public static void InfoBox(string message, MessageType type = MessageType.Info, bool margin = true, bool indent = true)
        {
            if (margin)
            {
                EditorGUILayout.Space();
            }

            if (indent) EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox(message, type);
            if (indent) EditorGUI.indentLevel--;

            if (margin)
            {
                EditorGUILayout.Space();
            }
        }
    }
}