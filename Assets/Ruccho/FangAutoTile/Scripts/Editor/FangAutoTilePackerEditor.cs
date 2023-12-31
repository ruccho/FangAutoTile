using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Ruccho.Fang
{
    [CustomEditor(typeof(FangAutoTilePacker))]
    public class FangAutoTilePackerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Generate!"))
            {
                Generate();
            }
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            var compiledChannelsProp = serializedObject.FindProperty("compiledChannels");
            var channelProp = compiledChannelsProp.GetArrayElementAtIndex(0);
            
            if (channelProp != null)
            {
                var tex = channelProp.objectReferenceValue as Texture2D;

                if (tex)
                {
                    Texture2D p = AssetPreview.GetAssetPreview(tex);
                    if (p)
                    {
                        Texture2D f = new Texture2D(width, height);
                        EditorUtility.CopySerialized(p, f);
                        return f;
                    }
                }
            }

            return base.RenderStaticPreview(assetPath, subAssets, width, height);
        }

        private void Generate()
        {
            var targetsProp = serializedObject.FindProperty("targets");
            var enablePadding = serializedObject.FindProperty("enablePadding").boolValue;
            var compiledChannelsProp = serializedObject.FindProperty("compiledChannels");
            
            var wrapMode = (TextureWrapMode) serializedObject.FindProperty("wrapMode").enumValueIndex;
            var filterMode = (FilterMode) serializedObject.FindProperty("filterMode").enumValueIndex;

            int? numSubChannels = null;

            List<FangAutoTileEditor> editors = new List<FangAutoTileEditor>();
            for (int i = 0; i < targetsProp.arraySize; i++)
            {
                var targetProp = targetsProp.GetArrayElementAtIndex(i);
                var target = targetProp.objectReferenceValue;
                if (target)
                {
                    var editor = CreateEditor(target, typeof(FangAutoTileEditor)) as FangAutoTileEditor;
                    editors.Add(editor);

                    if (!editor.CheckValidity(out var mes))
                    {
                        EditorUtility.DisplayDialog("Fang Auto Tile", mes, "OK");
                        return;
                    }

                    var subChannels = editor.serializedObject.FindProperty(FangAutoTileEditor.p_SubChannels).arraySize;
                    if (numSubChannels == null) numSubChannels = subChannels;
                    else if (numSubChannels.Value != subChannels)
                    {
                        EditorUtility.DisplayDialog("Fang Auto Tile", "The number of sub channels does not match.", "Yes");
                        return;
                    }
                    editor.GenerateCombination();
                }
            }

            if (!numSubChannels.HasValue)
            {
                Debug.Log("There are no tiles.");
                return;
            }

            int wholeChannels = numSubChannels.Value + 1;

            foreach (var e in editors)
            {
                e.serializedObject.FindProperty(FangAutoTileEditor.p_Packer).objectReferenceValue = target;
                e.ClearTextures();
            }

            //main channel

            List<TileDrawingItem> segments = new List<TileDrawingItem>();
            {
                foreach (var e in editors)
                {
                    var mainChannel =
                        e.serializedObject.FindProperty(FangAutoTileEditor.p_MainChannel)
                            .objectReferenceValue as Texture2D;
                    segments.AddRange(e.GetSegments()
                        .Select(s => new TileDrawingItem(s, new TemporaryTexture2DBuffer(mainChannel))));
                }

                var segmentsOrdered = segments.OrderByDescending(s => s.Segment.Width * s.Segment.Height);

                int texSize =
                    FangAutoTileEditor.GetSuitableTextureSize(segmentsOrdered.Select(s => s.Segment), enablePadding);

                //Validate textures
                for (int i = 0; i < Mathf.Max(wholeChannels, compiledChannelsProp.arraySize); i++)
                {
                    if (wholeChannels <= i)
                    {
                        //Need to be deleted
                        var tex = compiledChannelsProp.GetArrayElementAtIndex(i).objectReferenceValue as Texture2D;
                        if (!tex) continue;

                        DestroyImmediate(tex, true);
                    }
                    else
                    {
                        if (compiledChannelsProp.arraySize <= i)
                        {
                            compiledChannelsProp.InsertArrayElementAtIndex(i);
                            compiledChannelsProp.GetArrayElementAtIndex(i).objectReferenceValue = null;
                        }

                        var element = compiledChannelsProp.GetArrayElementAtIndex(i);
                        var tex = element.objectReferenceValue as Texture2D;
                        var format = GraphicsFormat.R8G8B8A8_SRGB;
                    
                        if (tex)
                        {
                            if (tex.width != texSize || tex.height != texSize || tex.graphicsFormat != format)
                            {
                                tex.Reinitialize(texSize, texSize, format, false);
                            }
                        }
                        else
                        {
                            tex = new Texture2D(texSize, texSize, DefaultFormat.LDR, TextureCreationFlags.None);
                            tex.Reinitialize(texSize, texSize, format, false);
                            tex.name = "Texture";
                            AssetDatabase.AddObjectToAsset(tex, target);
                            element.objectReferenceValue = tex;
                        }

                        tex.wrapMode = wrapMode;
                        tex.filterMode = filterMode;
                    }
                }


                var dest = compiledChannelsProp.GetArrayElementAtIndex(0).objectReferenceValue as Texture2D;
                FangAutoTileEditor.GenerateTilesForTexture(dest, segmentsOrdered, enablePadding, true);
            }

            //sub channels
            for (int i = 1; i < wholeChannels; i++)
            {
                segments.Clear();

                foreach (var e in editors)
                {
                    var subChannel =
                        e.serializedObject.FindProperty(FangAutoTileEditor.p_SubChannels).GetArrayElementAtIndex(i - 1)
                            .objectReferenceValue as Texture2D;
                    segments.AddRange(e.GetSegments()
                        .Select(s => new TileDrawingItem(s, new TemporaryTexture2DBuffer(subChannel))));
                }

                var segmentsOrdered = segments.OrderByDescending(s => s.Segment.Width * s.Segment.Height);

                var dest = compiledChannelsProp.GetArrayElementAtIndex(i).objectReferenceValue as Texture2D;
                FangAutoTileEditor.GenerateTilesForTexture(dest, segmentsOrdered, enablePadding, false);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (var e in editors)
            {
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(e.target));
                e.serializedObject.ApplyModifiedProperties();
            }

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target));
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}