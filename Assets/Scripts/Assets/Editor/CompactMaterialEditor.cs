using System;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    /// <summary>
    /// Editor for the compact material class. Displays the resulting textures at the bottom of the view.
    /// </summary>
    [CustomEditor(typeof(CompactMaterial))]
    public class CompactMaterialEditor : UnityEditor.Editor
    {
        private const float TitleOffset = 10;

        enum DisplayableTextures
        {
            Surface,
            Albedo,
            Detail,
            Color,
            Emission,
            Height,
            Specular,
            Metallic
        }
        private DisplayableTextures _selectedIndex;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var splatMaterial = (CompactMaterial) target;
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            DrawRenderTexture(128f,splatMaterial.GetAlbedoMap(),"Albedo");
            GUILayout.FlexibleSpace();
            DrawRenderTexture(128f,splatMaterial.GetDetailMap(),"Detail");
            GUILayout.FlexibleSpace();
            DrawRenderTexture(128f,splatMaterial.GetSurfaceMap(),"Surface");
            GUILayout.EndHorizontal();
            _selectedIndex = (DisplayableTextures)EditorGUILayout.EnumPopup("Texture Displayed:", _selectedIndex);
            switch (_selectedIndex)
            {
                case DisplayableTextures.Albedo:
                    EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetAspectRect(1), splatMaterial.GetAlbedoMap());
                    break;
                case DisplayableTextures.Surface:
                    EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetAspectRect(1), splatMaterial.GetSurfaceMap());
                    break;
                case DisplayableTextures.Detail:
                    EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetAspectRect(1), splatMaterial.GetDetailMap());
                    break;
                case DisplayableTextures.Color:
                    EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetAspectRect(1), splatMaterial.albedo);
                    break;
                case DisplayableTextures.Emission:
                    EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetAspectRect(1), splatMaterial.emission);
                    break;
                case DisplayableTextures.Height:
                    EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetAspectRect(1), splatMaterial.height);
                    break;
                case DisplayableTextures.Specular:
                    EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetAspectRect(1), splatMaterial.specular);
                    break;
                case DisplayableTextures.Metallic:
                    EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetAspectRect(1), splatMaterial.metallic);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void DrawRenderTexture(float size, Texture texture, string text)
        {
            var offsetVector = new Vector2(0,TitleOffset);
            var rectAlbedo = GUILayoutUtility.GetRect(size,size + TitleOffset * 4);
            GUI.Box(rectAlbedo, text);
            GUI.DrawTexture(new Rect(rectAlbedo.position + offsetVector, rectAlbedo.size + offsetVector), texture,
                ScaleMode.ScaleToFit, false);
        }
    }
}