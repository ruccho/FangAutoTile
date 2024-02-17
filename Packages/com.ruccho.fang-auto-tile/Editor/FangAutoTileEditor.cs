using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Ruccho.Fang
{
    [CustomEditor(typeof(FangAutoTile)), CanEditMultipleObjects]
    public class FangAutoTileEditor : Editor
    {
        public static readonly string p_EnablePadding = "enablePadding";
        public static readonly string p_OneTilePerUnit = "oneTilePerUnit";
        public static readonly string p_PhysicsShapeGeneration = "physicsShapeGeneration";
        public static readonly string p_PixelsPerUnit = "pixelsPerUnit";
        public static readonly string p_WrapMode = "wrapMode";
        public static readonly string p_FilterMode = "filterMode";
        public static readonly string p_MainChannel = "mainChannel";
        public static readonly string p_NumSlopes = "numSlopes";
        public static readonly string p_SubChannels = "subChannels";

        public static readonly string p_FrameMode = "frameMode";
        public static readonly string p_AnimationMinSpeed = "animationMinSpeed";
        public static readonly string p_AnimationMaxSpeed = "animationMaxSpeed";
        public static readonly string p_AnimationStartTime = "animationStartTime";
        public static readonly string p_ColliderType = "colliderType";
        public static readonly string p_IsSlope = "isSlope";
        public static readonly string p_ConnectableTiles = "connectableTiles";


        public static readonly string p_Packer = "packer";
        public static readonly string p_CompiledChannels = "compiledChannels";
        public static readonly string p_Combinations = "combinations";
        public static readonly string p_CombinationTable = "combinationTable";

        public static readonly string p_TC_CombinationId = "combinationId";
        public static readonly string p_TC_Frames = "frames";

        public static readonly string p_SlopeDefinition = "slopeDefinition";
        public static readonly string p_SD_Sizes = "sizes";
        public static readonly string p_STSD_FloorUp = "floorUp";
        public static readonly string p_STSD_FloorDown = "floorDown";
        public static readonly string p_STSD_CeilUp = "ceilUp";
        public static readonly string p_STSD_CeilDown = "ceilDown";
        public static readonly string p_STAD_HorizontalTiles = "horizontalTiles";
        public static readonly string p_STAD_VerticalTiles = "verticalTiles";
        public static readonly string p_STD_Frames = "frames";

        private static readonly GUIContent tempGUIContent = new ();

        private static readonly int paddingSize = 2;

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUI.BeginChangeCheck();

            if (Foldout("tile", "Tile Settings"))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(p_FrameMode));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(p_AnimationMinSpeed));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(p_AnimationMaxSpeed));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(p_AnimationStartTime));

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(p_ColliderType));

                    tempGUIContent.text = "Is Slope (experimental)";
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(p_IsSlope), tempGUIContent);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(p_ConnectableTiles));
                }

                GUILayout.Space(20f);
            }

            if (Foldout("generation", "Tile Generation Settings"))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(p_EnablePadding));

                    var oneTilePerUnitProp = serializedObject.FindProperty(p_OneTilePerUnit);
                    EditorGUILayout.PropertyField(oneTilePerUnitProp);

                    if (!oneTilePerUnitProp.boolValue)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(p_PixelsPerUnit));
                    }

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(p_PhysicsShapeGeneration));
                    
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(p_WrapMode));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(p_FilterMode));


                    var mainChannelProp = serializedObject.FindProperty(p_MainChannel);

                    if (!serializedObject.isEditingMultipleObjects)
                    {
                        GUILayout.Space(EditorGUIUtility.singleLineHeight * 0.5f);

                        EditorGUILayout.LabelField("Sources", EditorStyles.boldLabel);

                        EditorGUILayout.PropertyField(serializedObject.FindProperty(p_NumSlopes));

                        mainChannelProp.objectReferenceValue = EditorGUILayout.ObjectField("Main Channel",
                            mainChannelProp.objectReferenceValue, typeof(Texture2D), false);
                    }
                    else
                    {
                        EditorGUILayout.ObjectField(mainChannelProp, typeof(Texture2D));
                    }

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(p_SubChannels));
                }

                GUILayout.Space(20f);
            }

            bool generate = false;

            if (!serializedObject.isEditingMultipleObjects)
            {
                bool isValid = CheckValidity(out string validityMessage);


                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(p_Packer));
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.HelpBox(validityMessage, isValid ? MessageType.Info : MessageType.Error);

                EditorGUI.BeginDisabledGroup(!isValid);
                generate = GUILayout.Button("Generate!", GUILayout.Height(50f));
                EditorGUI.EndDisabledGroup();

                Color c = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button("Clear all generated contents")) Clear();
                GUI.color = c;
                EditorGUILayout.HelpBox(GetInfo(), MessageType.Info);
            }


            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (generate) Generate();
        }

        private static bool Foldout(string id, string title)
        {
            bool display = EditorPrefs.GetBool($"{typeof(FangAutoTileEditor).FullName}/foldout/{id}", false);

            var style = new GUIStyle("ShurikenModuleTitle");
            style.font = new GUIStyle(EditorStyles.label).font;
            style.fontSize = 12;
            style.border = new RectOffset(15, 7, 4, 4);
            style.fixedHeight = 22;
            style.contentOffset = new Vector2(20f, -2f);

            var rect = GUILayoutUtility.GetRect(16f, 22f, style);
            GUI.Box(rect, title, style);

            var e = Event.current;

            var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                display = !display;
                EditorPrefs.SetBool($"{typeof(FangAutoTileEditor).FullName}/foldout/{id}", display);
                e.Use();
            }

            return display;
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            var combinationsProp = serializedObject.FindProperty(p_Combinations);
            if (combinationsProp.arraySize > 0)
            {
                var framesProp = combinationsProp.GetArrayElementAtIndex(0).FindPropertyRelative(p_TC_Frames);
                if (framesProp.arraySize > 0)
                {
                    var sprite = framesProp.GetArrayElementAtIndex(0).objectReferenceValue as Sprite;
                    if (sprite)
                    {
                        Texture2D p = AssetPreview.GetAssetPreview(sprite);
                        if (p)
                        {
                            Texture2D f = new Texture2D(width, height);
                            EditorUtility.CopySerialized(p, f);
                            return f;
                        }
                    }
                }
            }

            return base.RenderStaticPreview(assetPath, subAssets, width, height);
        }


        public bool CheckValidity(out string message)
        {
            var mainChannelProp = serializedObject.FindProperty(p_MainChannel);

            var mainChannel = mainChannelProp.objectReferenceValue as Texture2D;

            if (!mainChannel)
            {
                message = "Set main channel texture.";
                return false;
            }

            int width = mainChannel.width;
            int height = mainChannel.height;
            int numSlopes = serializedObject.FindProperty(p_NumSlopes).intValue;

            // 5 (base) + 
            // 4 (slopeSize=1) +
            // n * 4 * 2 (slopeSize=n) + 
            // ...
            int numTilesInFrame = 5 + 4 * numSlopes * (numSlopes + 1);
            if (numSlopes >= 1) numTilesInFrame -= 4;

            int tileSize = height / numTilesInFrame;

            if (height % numTilesInFrame != 0 || width % tileSize != 0)
            {
                message = "Size of the texture has to be specific format:\n" +
                          " width:  (Tile size) * (Number of frame)\n" +
                          $" height: (Tile size) * {numTilesInFrame} (when NumSlopes is {numSlopes})\n";
                return false;
            }

            var subChannelsProp = serializedObject.FindProperty(p_SubChannels);

            int numSubChannels = subChannelsProp.arraySize;

            for (int i = 0; i < numSubChannels; i++)
            {
                var e = subChannelsProp.GetArrayElementAtIndex(i);
                var subChannel = e.objectReferenceValue as Texture2D;

                if (subChannel == null)
                {
                    message = "Sub Channels contain empty element.";
                    return false;
                }

                if (subChannel.width != width || subChannel.height != height)
                {
                    message = "Size of sub channel texture doesn't match one of the main channel.";
                    return false;
                }

                /*
                if (!subChannel.isReadable)
                {
                    message =
                        "Turn on \"Read / Write Enabled\" option of the sub channel texture in the Texture Import Settings.";
                    return false;
                }
                */
            }

            message = "Ready to generate a tile!";
            return true;
        }

        private string GetInfo()
        {
            var combinationsProp = serializedObject.FindProperty(p_Combinations);

            if (combinationsProp.arraySize == 0)
            {
                return "No tiles generated";
            }

            var framesProp = combinationsProp.GetArrayElementAtIndex(0).FindPropertyRelative(p_TC_Frames);

            int frames = framesProp.arraySize;

            if (frames == 0 || !framesProp.GetArrayElementAtIndex(0).objectReferenceValue)
            {
                return "Combinations enumerated but no sprites generated";
            }

            var sprite = framesProp.GetArrayElementAtIndex(0).objectReferenceValue as Sprite;
            var texture = sprite.texture;

            return $"Generated:" +
                   $" Combinations: {combinationsProp.arraySize}\n" +
                   $" Number of frames: {frames}\n" +
                   $" Tile size: {sprite.rect.width} x {sprite.rect.height}\n" +
                   $" Texture size: {texture.width} x {texture.height}";
        }

        private void Generate()
        {
            if (!CheckValidity(out var mes))
            {
                EditorUtility.DisplayDialog("Fang Auto Tile", mes, "OK");
                return;
            }

            var packerProp = serializedObject.FindProperty(p_Packer);
            bool isPacked = packerProp.objectReferenceValue;

            if (isPacked && !EditorUtility.DisplayDialog("Fang Auto Tile",
                    "This tile is set to be used with texture packer. Are you sure?", "OK", "Cancel"))
            {
                return;
            }

            try
            {
                GenerateCombination();
                GenerateTexture();
            }
            finally
            {
                serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target));
            }
        }

        private void Clear()
        {
            if (!EditorUtility.DisplayDialog("Confirm",
                    "Clearing all generated contents may cause missing references of textures. Are you sure?", "Yes",
                    "No"))
            {
                return;
            }

            ClearCombinations();
            ClearTextures();

            /*
            foreach(var o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(target)))
            {
                if (AssetDatabase.IsSubAsset(o))
                {
                    DestroyImmediate(o, true);
                }
            }
            */

            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target));
        }

        public void GenerateCombination()
        {
            var combinationTableProp = serializedObject.FindProperty(p_CombinationTable);
            var combinationsProp = serializedObject.FindProperty(p_Combinations);

            //いちど生成すれば内容は固定
            if (combinationsProp.arraySize > 0) return;

            ClearCombinations();

            //List<uint> combinationIds = new List<uint>();
            int[] combinationIds = new int[2341];

            int numCombinations = 0;
            for (int neighborCombination = 0; neighborCombination < 256; neighborCombination++)
            {
                uint combinationId = GetCombinationId((byte)neighborCombination);

                if (combinationIds[combinationId] == 0)
                {
                    combinationIds[combinationId] = numCombinations + 1;
                    numCombinations++;
                }

                var tableElement = combinationTableProp.GetArrayElementAtIndex(neighborCombination);
                tableElement.intValue = combinationIds[combinationId] - 1;
            }

            combinationsProp.arraySize = numCombinations;

            for (int i = 0; i < combinationIds.Length; i++)
            {
                if (combinationIds[i] != 0)
                {
                    combinationsProp
                        .GetArrayElementAtIndex(combinationIds[i] - 1).FindPropertyRelative(p_TC_CombinationId)
                        .longValue = i;
                }
            }

            uint GetCombinationId(byte neighborCombination)
            {
                // tl tc tr
                // ml    mr
                // bl bc br

                bool bl = (neighborCombination & (1 << 0)) != 0;
                bool bc = (neighborCombination & (1 << 1)) != 0;
                bool br = (neighborCombination & (1 << 2)) != 0;
                bool mr = (neighborCombination & (1 << 3)) != 0;
                bool tr = (neighborCombination & (1 << 4)) != 0;
                bool tc = (neighborCombination & (1 << 5)) != 0;
                bool tl = (neighborCombination & (1 << 6)) != 0;
                bool ml = (neighborCombination & (1 << 7)) != 0;

                byte c_bl = DetermineKind(bc, ml, bl);
                byte c_br = DetermineKind(bc, mr, br);
                byte c_tl = DetermineKind(tc, ml, tl);
                byte c_tr = DetermineKind(tc, mr, tr);

                // UInt32 combinationId: ....................[.bl][.br][.tl][.tr]
                uint combinationId = 0;

                combinationId += (uint)(c_bl << 9);
                combinationId += (uint)(c_br << 6);
                combinationId += (uint)(c_tl << 3);
                combinationId += (uint)(c_tr << 0);

                return combinationId;
            }

            byte DetermineKind(bool vertical, bool horizontal, bool corner)
            {
                if (!vertical && !horizontal) return 0;
                if (vertical && !horizontal) return 1;
                if (!vertical && horizontal) return 2;
                if (!corner) return 3;
                return 4;
            }
        }

        private void GenerateTexture()
        {
            var mainChannelProp = serializedObject.FindProperty(p_MainChannel);
            var subChannelsProp = serializedObject.FindProperty(p_SubChannels);
            var compiledChannelsProp = serializedObject.FindProperty(p_CompiledChannels);
            var packerProp = serializedObject.FindProperty(p_Packer);

            packerProp.objectReferenceValue = null;

            var enablePadding = serializedObject.FindProperty(p_EnablePadding).boolValue;
            var wrapMode = (TextureWrapMode)serializedObject.FindProperty(p_WrapMode).enumValueIndex;
            var filterMode = (FilterMode)serializedObject.FindProperty(p_FilterMode).enumValueIndex;

            int wholeChannels = subChannelsProp.arraySize + 1;


            IReadOnlyList<ITileImage> segments = GetSegments(enablePadding).ToList();

            var segmentsOrdered = segments.OrderByDescending((seg) => seg.Width * seg.Height);

            int texSize = GetSuitableTextureSize(segmentsOrdered);

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

            {
                var src = mainChannelProp.objectReferenceValue as Texture2D;
                var srcBuffer = new TemporaryTexture2DBuffer(src);

                var dest = compiledChannelsProp.GetArrayElementAtIndex(0).objectReferenceValue as Texture2D;
                GenerateTilesForTexture(dest, segmentsOrdered.Select(s => new TileDrawingItem(s, srcBuffer)),
                    true);
            }

            for (int i = 1; i < wholeChannels; i++)
            {
                var src = subChannelsProp.GetArrayElementAtIndex(i - 1).objectReferenceValue as Texture2D;
                var srcBuffer = new TemporaryTexture2DBuffer(src);

                var dest = compiledChannelsProp.GetArrayElementAtIndex(i).objectReferenceValue as Texture2D;
                GenerateTilesForTexture(dest, segmentsOrdered.Select(s => new TileDrawingItem(s, srcBuffer)),
                    false);
            }
        }

        public static int GetSuitableTextureSize(IEnumerable<ITileImage> segmentsOrdered)
        {
            ulong square = 0;
            foreach (var segment in segmentsOrdered)
            {
                square += (ulong)(segment.Width * segment.Height);
            }

            int minTexSizeExp = Mathf.CeilToInt(Mathf.Log(2f, Mathf.Sqrt(square)));
            while (true)
            {
                int currentTexSize = (int)Mathf.Pow(2, minTexSizeExp);

                using (var segmentOrdered = segmentsOrdered.GetEnumerator())
                {
                    int x = 0;
                    int y = 0;
                    int hMax = 0;
                    bool isFailed = false;
                    while (segmentOrdered.MoveNext())
                    {
                        int w = segmentOrdered.Current.Width;

                        int h = segmentOrdered.Current.Width;

                        hMax = Mathf.Max(h, hMax);

                        if (currentTexSize <= y + h)
                        {
                            isFailed = true;
                            break;
                        }

                        x += w;

                        if (currentTexSize <= x)
                        {
                            x = w;

                            y += hMax;
                            hMax = 0;

                            if (currentTexSize <= y)
                            {
                                isFailed = true;
                                break;
                            }
                        }
                    }

                    if (!isFailed) break;

                    minTexSizeExp++;
                }
            }

            return (int)Mathf.Pow(2, minTexSizeExp);
        }

        public static void GenerateTilesForTexture(Texture2D dstChannel,
            IEnumerable<TileDrawingItem> orderedSegments, bool createSprite)
        {
            int texSize = dstChannel.width;

            var dstBuffer = new TemporaryTexture2DBuffer(dstChannel);
            dstBuffer.ClearPixels();

            using (var segment = orderedSegments.GetEnumerator())
            {
                int x = 0;
                int y = 0;
                int hMax = 0;
                while (segment.MoveNext())
                {
                    var srcChannel = segment.Current.SourceBuffer;
                    var s = segment.Current.Segment;

                    int w = s.Width;

                    int h = s.Width;

                    hMax = Mathf.Max(h, hMax);


                    if (texSize <= x + w)
                    {
                        x = 0;

                        y += hMax;
                        hMax = 0;

                        if (texSize <= y)
                        {
                            throw new InvalidOperationException();
                        }
                    }

                    //Debug.Log($"x: {x}, y : {y}");
                    var spriteRect = s.LocalSpriteRect;
                    spriteRect.x += x;
                    spriteRect.y += y;

                    s.Copy(srcChannel, dstBuffer, x, y);

                    if (createSprite)
                    {
                        var sprite = s.SpriteProperty.objectReferenceValue as Sprite;
                        
                        if (sprite) DestroyImmediate(sprite, true);

                        sprite = Sprite.Create(dstChannel, spriteRect, new Vector2(0.5f, 0.5f),
                            s.PixelsPerUnit, 0, SpriteMeshType.FullRect);

                        AssetDatabase.AddObjectToAsset(sprite, s.SpriteProperty.serializedObject.targetObject);

                        s.SpriteProperty.objectReferenceValue = sprite;
                        
                        sprite.hideFlags |= HideFlags.HideInHierarchy;

                        var contours = s.PhysicsShape.Points;

                        if (contours != null)
                        {
                            var spriteSO = new SerializedObject(sprite);
                            var contoursArrayProp = spriteSO.FindProperty("m_PhysicsShape");
                            contoursArrayProp.arraySize = contours.Count;
                            for (int i = 0; i < contours.Count; i++)
                            {
                                var pointsArrayProp = contoursArrayProp.GetArrayElementAtIndex(i);
                                var points = contours[i];
                                pointsArrayProp.arraySize = points.Length;

                                for (int j = 0; j < points.Length; j++)
                                {
                                    var pointProp = pointsArrayProp.GetArrayElementAtIndex(j);
                                    pointProp.vector2Value = points[j];
                                }
                            }

                            spriteSO.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }

                    x += w;
                }
            }

            dstBuffer.Apply();
        }

        public IEnumerable<ITileImage> GetSegments(bool enablePadding)
        {
            var mainChannelProp = serializedObject.FindProperty(p_MainChannel);

            var explicitPixelsPerUnit = serializedObject.FindProperty(p_PixelsPerUnit).intValue;
            var oneTilePerUnit = serializedObject.FindProperty(p_OneTilePerUnit).boolValue;
            var physicsShapeGeneration = (PhysicsShapeGenerationMode) serializedObject.FindProperty(p_PhysicsShapeGeneration).enumValueIndex;

            var numSlopes = serializedObject.FindProperty(p_NumSlopes).intValue;

            var mainChannel = mainChannelProp.objectReferenceValue as Texture2D;

            int width = mainChannel.width;
            int height = mainChannel.height;

            // 5 (base) + 
            // 4 (slopeSize=1) +
            // n * 4 * 2 (slopeSize=n) + 
            // ...
            int numTilesInFrame = 5 + 4 * numSlopes * (numSlopes + 1);
            if (numSlopes >= 1) numTilesInFrame -= 4;

            int tileSize = height / numTilesInFrame;
            int numFrames = width / tileSize;

            int pixelsPerUnit = oneTilePerUnit ? tileSize : explicitPixelsPerUnit;

            var combinationsProp = serializedObject.FindProperty(p_Combinations);
            for (int combination = 0; combination < combinationsProp.arraySize; combination++)
            {
                var element = combinationsProp.GetArrayElementAtIndex(combination);
                var combinationIdProp = element.FindPropertyRelative(p_TC_CombinationId);
                var framesProp = element.FindPropertyRelative(p_TC_Frames);

                // Delete sprites that are exactly unused
                for (int f = numFrames; f < framesProp.arraySize; f++)
                {
                    var frameProp = framesProp.GetArrayElementAtIndex(f);
                    var frame = frameProp.objectReferenceValue;
                    if (frame)
                    {
                        DestroyImmediate(frame, true);
                    }
                }

                framesProp.arraySize = numFrames;

                uint combinationId = (uint)combinationIdProp.longValue;

                byte tr = (byte)((combinationId >> 0) & 0b111);
                byte tl = (byte)((combinationId >> 3) & 0b111);
                byte br = (byte)((combinationId >> 6) & 0b111);
                byte bl = (byte)((combinationId >> 9) & 0b111);

                //Debug.Log($"BL: {bl}, BR: {br}, TL: {tl}, TR: {tr}");

                for (int frame = 0; frame < numFrames; frame++)
                {
                    var frameProp = framesProp.GetArrayElementAtIndex(frame);
                    var image = new TileCombinationImage(
                        GetSegment(tileSize, 0, frame, bl),
                        GetSegment(tileSize, 1, frame, br),
                        GetSegment(tileSize, 2, frame, tl),
                        GetSegment(tileSize, 3, frame, tr),
                        frameProp,
                        pixelsPerUnit,
                        enablePadding
                    );
                    if (physicsShapeGeneration == PhysicsShapeGenerationMode.Fine)
                    {
                        image.PhysicsShape = new(new List<Vector2[]>(new[]
                        {
                            new[]
                            {
                                new Vector2(-0.5f, -0.5f),
                                new Vector2(-0.5f, 0.5f),
                                new Vector2(0.5f, 0.5f),
                                new Vector2(0.5f, -0.5f)
                            }
                        }));
                    }

                    yield return image;
                }
            }

            // slopes

            var slopeDefinitionProp = serializedObject.FindProperty(p_SlopeDefinition);

            // - sizes

            var sizesProp = slopeDefinitionProp.FindPropertyRelative(p_SD_Sizes);
            sizesProp.arraySize = numSlopes;

            int slopeVerticalCursor = 5;
            for (int sizeIndex = 0; sizeIndex < numSlopes; sizeIndex++)
            {
                var sizeProp = sizesProp.GetArrayElementAtIndex(sizeIndex);
                var floorUpProp = sizeProp.FindPropertyRelative(p_STSD_FloorUp);
                var floorDownProp = sizeProp.FindPropertyRelative(p_STSD_FloorDown);
                var ceilUpProp = sizeProp.FindPropertyRelative(p_STSD_CeilUp);
                var ceilDownProp = sizeProp.FindPropertyRelative(p_STSD_CeilDown);


                bool isVertical = false;
                while (true)
                {
                    foreach (var image in ProcessSlopeAngle(floorUpProp, sizeIndex, isVertical, 0)) yield return image;
                    foreach (var image in ProcessSlopeAngle(floorDownProp, sizeIndex, isVertical, 2))
                        yield return image;
                    foreach (var image in ProcessSlopeAngle(ceilUpProp, sizeIndex, isVertical, 1)) yield return image;
                    foreach (var image in ProcessSlopeAngle(ceilDownProp, sizeIndex, isVertical, 3)) yield return image;

                    if (isVertical) break;
                    if (sizeIndex == 0) break;
                    isVertical = true;
                }

                IEnumerable<ITileImage> ProcessSlopeAngle(SerializedProperty angleDefinition, int sizeIndex,
                    bool isVertical, byte direction)
                {
                    var tilesProp = angleDefinition.FindPropertyRelative(
                        isVertical ? p_STAD_VerticalTiles : p_STAD_HorizontalTiles);
                    tilesProp.arraySize = sizeIndex + 1;
                    for (int index = 0; index < sizeIndex + 1; index++)
                    {
                        var tileProp = tilesProp.GetArrayElementAtIndex(index);
                        var framesProp = tileProp.FindPropertyRelative(p_STD_Frames);
                        framesProp.arraySize = numFrames;

                        for (int frame = 0; frame < numFrames; frame++)
                        {
                            var frameProp = framesProp.GetArrayElementAtIndex(frame);
                            var seg = GetSegment(tileSize, frame, slopeVerticalCursor);

                            var image = new TileSingleImage(seg, frameProp, pixelsPerUnit, enablePadding);

                            // shape
                            var shape = new List<Vector2>();

                            if (physicsShapeGeneration == PhysicsShapeGenerationMode.Fine)
                            {

                                if (index > 0)
                                {
                                    shape.Add(new(0, (float)index / (sizeIndex + 1))); // left
                                }

                                shape.Add(new(1, (float)(index + 1) / (sizeIndex + 1))); // right
                                shape.Add(new(1, 0)); // right (bottom)
                                shape.Add(new(0, 0)); // left (bottom)


                                // transform
                                for (int i = 0; i < shape.Count; i++)
                                {
                                    var p = shape[i];

                                    p.x -= 0.5f;
                                    p.y -= 0.5f;

                                    if (isVertical)
                                    {
                                        (p.x, p.y) = (-p.y, -p.x);
                                    }

                                    switch (direction)
                                    {
                                        case 0:
                                            break;
                                        case 1:
                                            p.x = -p.x;
                                            p.y = -p.y;
                                            break;
                                        case 2:
                                            p.x = -p.x;
                                            break;
                                        case 3:
                                            p.y = -p.y;
                                            break;
                                    }

                                    shape[i] = p;
                                }

                                image.PhysicsShape = new(new List<Vector2[]>(new[] { shape.ToArray() }));
                            }


                            yield return image;
                        }

                        slopeVerticalCursor++;
                    }
                }
            }
        }

        private void ClearCombinations()
        {
            var combinationTableProp = serializedObject.FindProperty(p_CombinationTable);
            var combinationsProp = serializedObject.FindProperty(p_Combinations);

            ClearSprites();

            combinationTableProp.ClearArray();
            combinationTableProp.arraySize = 1 << 8;

            combinationsProp.ClearArray();
            combinationsProp.arraySize = 0;
            
            var sizesProp = serializedObject.FindProperty(p_SlopeDefinition).FindPropertyRelative(p_SD_Sizes);
            sizesProp.ClearArray();
            sizesProp.arraySize = 0;
        }

        private void ClearSprites()
        {
            var combinationsProp = serializedObject.FindProperty(p_Combinations);
            for (int c = 0; c < combinationsProp.arraySize; c++)
            {
                var framesProp = combinationsProp.GetArrayElementAtIndex(c).FindPropertyRelative(p_TC_Frames);
                ClearFrames(framesProp);
            }

            var sizesProp = serializedObject.FindProperty(p_SlopeDefinition).FindPropertyRelative(p_SD_Sizes);
            for (int i = 0; i < sizesProp.arraySize; i++)
            {
                var sizeProp = sizesProp.GetArrayElementAtIndex(i);
                var floorUpProp = sizeProp.FindPropertyRelative(p_STSD_FloorUp);
                var floorDownProp = sizeProp.FindPropertyRelative(p_STSD_FloorDown);
                var ceilUpProp = sizeProp.FindPropertyRelative(p_STSD_CeilUp);
                var ceilDownProp = sizeProp.FindPropertyRelative(p_STSD_CeilDown);

                ClearAngle(floorUpProp);
                ClearAngle(floorDownProp);
                ClearAngle(ceilUpProp);
                ClearAngle(ceilDownProp);

                void ClearAngle(SerializedProperty angleProp)
                {
                    var horiProp = angleProp.FindPropertyRelative(p_STAD_HorizontalTiles);
                    var vertProp = angleProp.FindPropertyRelative(p_STAD_VerticalTiles);

                    ClearAngleSlope(horiProp);
                    ClearAngleSlope(vertProp);
                }

                void ClearAngleSlope(SerializedProperty angleSlopeProp)
                {
                    for (int i = 0; i < angleSlopeProp.arraySize; i++)
                    {
                        ClearFrames(angleSlopeProp.GetArrayElementAtIndex(i).FindPropertyRelative(p_STD_Frames));
                    }
                }
            }

            void ClearFrames(SerializedProperty framesProp)
            {
                int numFrames = framesProp.arraySize;
                for (int i = 0; i < numFrames; i++)
                {
                    var frameProp = framesProp.GetArrayElementAtIndex(i);
                    var frame = frameProp.objectReferenceValue as Sprite;
                    if (!frame) continue;

                    frameProp.objectReferenceValue = null;
                    DestroyImmediate(frame, true);
                }
            }
        }

        public void ClearTextures()
        {
            var compiledChannelsProp = serializedObject.FindProperty(p_CompiledChannels);
            for (int c = 0; c < compiledChannelsProp.arraySize; c++)
            {
                var channelProp = compiledChannelsProp.GetArrayElementAtIndex(c);

                var channel = channelProp.objectReferenceValue as Texture2D;
                if (!channel) continue;

                DestroyImmediate(channel, true);

                channelProp.objectReferenceValue = null;
            }
        }


        private static TileSegment GetSegment(int tileSize, int quarter, int frame, int kind)
        {
            int baseX = frame * tileSize;
            int baseY = kind * tileSize;
            int tileX = (quarter % 2 == 0) ? 0 : tileSize / 2;
            int tileY = (quarter / 2 == 0) ? tileSize / 2 : 0;
            int width = (quarter % 2 == 0) ? tileSize / 2 : tileSize - tileSize / 2;
            int height = (quarter / 2 == 0) ? tileSize / 2 : tileSize - tileSize / 2;

            return new TileSegment(baseX + tileX, baseY + tileY, width, height);
        }

        private static TileSegment GetSegment(int tileSize, int frame, int kind)
        {
            int baseX = frame * tileSize;
            int baseY = kind * tileSize;

            return new TileSegment(baseX, baseY, tileSize, tileSize);
        }
    }


    public class TileDrawingItem
    {
        public ITileImage Segment { get; }
        public TemporaryTexture2DBuffer SourceBuffer { get; }

        public TileDrawingItem(ITileImage segment, TemporaryTexture2DBuffer sourceBuffer)
        {
            Segment = segment;
            SourceBuffer = sourceBuffer;
        }
    }

    public readonly struct TilePhysicsShape
    {
        public List<Vector2[]> Points { get; }

        public TilePhysicsShape(List<Vector2[]> points)
        {
            Points = points;
        }
    }

    public interface ITileImage
    {
        int Width { get; }

        int Height { get; }

        Rect LocalSpriteRect { get; }

        int PixelsPerUnit { get; }
        SerializedProperty SpriteProperty { get; }

        TilePhysicsShape PhysicsShape { get; }

        void Copy(TemporaryTexture2DBuffer src, TemporaryTexture2DBuffer dst, int dstX, int dstY);
    }

    public class TileSingleImage : ITileImage
    {
        public bool Expand { get; }
        public TileSegment Self { get; }

        public SerializedProperty SpriteProperty { get; }
        public TilePhysicsShape PhysicsShape { get; set; }

        public int PixelsPerUnit { get; }

        public int TileWidth => Self.W;
        public int TileHeight => Self.H;

        public int Width => Expand ? TileWidth + 2 : TileWidth;

        public int Height => Expand ? TileHeight + 2 : TileHeight;

        public Rect LocalSpriteRect =>
            Expand ? new Rect(1, 1, TileWidth, TileHeight) : new Rect(0, 0, TileWidth, TileHeight);

        public TileSingleImage(TileSegment self, SerializedProperty spriteProperty, int pixelsPerUnit, bool expand)
        {
            Expand = expand;
            Self = self;
            SpriteProperty = spriteProperty;
            PixelsPerUnit = pixelsPerUnit;
        }

        public void Copy(TemporaryTexture2DBuffer src, TemporaryTexture2DBuffer dst, int dstX, int dstY)
        {
            bool expand = Expand;

            var tileDstX = dstX;
            var tileDstY = dstY;

            if (expand)
            {
                tileDstX += 1;
                tileDstY += 1;
            }

            Self.Copy(src, dst, tileDstX, tileDstY);

            if (Expand)
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                int ToDstIndex(int localX, int localY) => dst.Width * (dstY + localY) + dstX + localX;

                // edges
                //  - bottom
                Array.Copy(dst.Pixels, ToDstIndex(1, 1), dst.Pixels, ToDstIndex(1, 0),
                    TileWidth);

                //  - top
                Array.Copy(dst.Pixels, ToDstIndex(1, TileHeight), dst.Pixels,
                    ToDstIndex(1, TileHeight + 1), TileWidth);

                // - side
                for (int y = 0; y < TileHeight; y++)
                {
                    dst.Pixels[ToDstIndex(0, y + 1)] = dst.Pixels[ToDstIndex(1, y + 1)];
                    dst.Pixels[ToDstIndex(TileWidth + 1, y + 1)] = dst.Pixels[ToDstIndex(TileWidth, y + 1)];
                }

                // corners

                dst.Pixels[ToDstIndex(0, 0)] = dst.Pixels[ToDstIndex(1, 1)];
                dst.Pixels[ToDstIndex(TileWidth + 1, 0)] = dst.Pixels[ToDstIndex(TileWidth, 1)];
                dst.Pixels[ToDstIndex(0, TileHeight + 1)] = dst.Pixels[ToDstIndex(1, TileHeight)];
                dst.Pixels[ToDstIndex(TileWidth + 1, TileHeight + 1)] = dst.Pixels[ToDstIndex(TileWidth, TileHeight)];
            }
        }
    }

    public class TileCombinationImage : ITileImage
    {
        public bool Expand { get; }
        public TileSegment Bl { get; }
        public TileSegment Br { get; }
        public TileSegment Tl { get; }
        public TileSegment Tr { get; }

        public SerializedProperty SpriteProperty { get; }
        public TilePhysicsShape PhysicsShape { get; set; }

        public int PixelsPerUnit { get; }

        public int TileWidth => Bl.W + Br.W;
        public int TileHeight => Bl.H + Tl.H;

        public int Width => Expand ? TileWidth + 2 : TileWidth;

        public int Height => Expand ? TileHeight + 2 : TileHeight;

        public Rect LocalSpriteRect =>
            Expand ? new Rect(1, 1, TileWidth, TileHeight) : new Rect(0, 0, TileWidth, TileHeight);


        public TileCombinationImage(TileSegment bl, TileSegment br, TileSegment tl, TileSegment tr,
            SerializedProperty spriteProperty, int pixelsPerUnit, bool expand)
        {
            Expand = expand;
            Bl = bl;
            Br = br;
            Tl = tl;
            Tr = tr;
            SpriteProperty = spriteProperty;
            PixelsPerUnit = pixelsPerUnit;
        }

        public void Copy(TemporaryTexture2DBuffer src, TemporaryTexture2DBuffer dst, int dstX, int dstY)
        {
            bool expand = Expand;

            var tileDstX = dstX;
            var tileDstY = dstY;

            if (expand)
            {
                tileDstX += 1;
                tileDstY += 1;
            }


            Bl.Copy(src, dst, tileDstX, tileDstY);
            Br.Copy(src, dst, tileDstX + Bl.W, tileDstY);
            Tl.Copy(src, dst, tileDstX, tileDstY + Bl.H);
            Tr.Copy(src, dst, tileDstX + Bl.W, tileDstY + Bl.H);

            if (Expand)
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                int ToDstIndex(int localX, int localY) => dst.Width * (dstY + localY) + dstX + localX;

                // edges
                //  - bottom
                Array.Copy(dst.Pixels, ToDstIndex(1, 1), dst.Pixels, ToDstIndex(1, 0),
                    TileWidth);

                //  - top
                Array.Copy(dst.Pixels, ToDstIndex(1, TileHeight), dst.Pixels,
                    ToDstIndex(1, TileHeight + 1), TileWidth);

                // - side
                for (int y = 0; y < TileHeight; y++)
                {
                    dst.Pixels[ToDstIndex(0, y + 1)] = dst.Pixels[ToDstIndex(1, y + 1)];
                    dst.Pixels[ToDstIndex(TileWidth + 1, y + 1)] = dst.Pixels[ToDstIndex(TileWidth, y + 1)];
                }

                // corners

                dst.Pixels[ToDstIndex(0, 0)] = dst.Pixels[ToDstIndex(1, 1)];
                dst.Pixels[ToDstIndex(TileWidth + 1, 0)] = dst.Pixels[ToDstIndex(TileWidth, 1)];
                dst.Pixels[ToDstIndex(0, TileHeight + 1)] = dst.Pixels[ToDstIndex(1, TileHeight)];
                dst.Pixels[ToDstIndex(TileWidth + 1, TileHeight + 1)] = dst.Pixels[ToDstIndex(TileWidth, TileHeight)];
            }
        }
    }

    [CustomPreview(typeof(FangAutoTile))]
    public class FangAutoTilePreview : ObjectPreview
    {
        private GUIContent previewTitle = new GUIContent("Generated Tiles");

        public override bool HasPreviewGUI()
        {
            var tile = target as FangAutoTile;
            if (!tile) return false;
            if (!tile.GetAllSprites().Any()) return false;
            return true;
        }

        public override GUIContent GetPreviewTitle()
        {
            return previewTitle;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            float padding = 5f;

            Rect previewRect = r;
            previewRect.width -= 40f;
            previewRect.height -= 40f;
            previewRect.x += 20f;
            previewRect.y += 20f;

            var tile = target as FangAutoTile;
            if (!tile) return;
            var sprites = tile.GetAllSprites();
            int maxCount = 120;
            int spritesCount = sprites.Count();

            int count = Mathf.Min(maxCount, spritesCount);
            int numX = Mathf.CeilToInt(Mathf.Sqrt(previewRect.width / previewRect.height * count));

            float gridSize = previewRect.width / numX;

            int i = 0;
            foreach (var s in sprites)
            {
                if (count <= i) break;

                int x = i % numX;
                int y = i / numX;

                var previewTexture = AssetPreview.GetAssetPreview(s);
                if (previewTexture)
                {
                    Rect texureRect = previewRect;
                    texureRect.width = gridSize - padding * 2f;
                    texureRect.height = gridSize - padding * 2f;
                    texureRect.x += x * gridSize + padding;
                    texureRect.y += y * gridSize + padding;

                    EditorGUI.DrawTextureTransparent(texureRect, previewTexture);
                }

                i++;
            }

            float labelHeight =
                new GUIStyle("PreOverlayLabel").CalcHeight(
                    new GUIContent($"Previewing {count} of {spritesCount} Objects"),
                    r.width);
            EditorGUI.DropShadowLabel(new Rect(r.x, r.yMax - labelHeight - 5f, r.width, labelHeight),
                $"Previewing {count} of {spritesCount} Objects");
        }
    }

    public class TileSegment
    {
        public int X { get; }
        public int Y { get; }
        public int W { get; }
        public int H { get; }

        public TileSegment(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }

        public void Copy(TemporaryTexture2DBuffer src, TemporaryTexture2DBuffer dst, int dstX, int dstY)
        {
            //Debug.Log($"Copy segment src: ({X}, {Y}), dst: ({dstX}, {dstY}), size: ({W}, {H})");

            var srcYFlipped = src.Height - Y - H;

            src.CopyTo(dst, X, srcYFlipped, dstX, dstY, W, H);
        }
    }

    public class TemporaryTexture2DBuffer
    {
        public Texture2D Texture { get; }
        public Color[] Pixels { get; private set; }

        public int Width => Texture.width;
        public int Height => Texture.height;

        public TemporaryTexture2DBuffer(Texture2D texture)
        {
            Texture = texture;

            if (!texture.isReadable)
            {
                var path = AssetDatabase.GetAssetPath(texture);
                if (!string.IsNullOrEmpty(path))
                {
                    var importer = AssetImporter.GetAtPath(path);
                    if (importer is TextureImporter textureImporter)
                    {
                        textureImporter.isReadable = true;
                        textureImporter.SaveAndReimport();
                        Debug.LogWarning(
                            $"[FangAutoTile] \"Read/Write Enabled\" setting of \"{texture.name}\" has been turned on automatically.");
                    }
                }
            }

            Pixels = Texture.GetPixels();
        }

        public void ClearPixels()
        {
            Pixels = new Color[Width * Height];
        }

        public void Apply()
        {
            Texture.SetPixels(Pixels);
            Texture.Apply();
        }

        public void CopyTo(TemporaryTexture2DBuffer dst, int srcX, int srcY, int dstX, int dstY, int w, int h)
        {
            if (Width < srcX + w || Height < srcY + h) throw new InvalidOperationException();
            if (dst.Width < dstX + w || dst.Height < dstY + h) throw new InvalidOperationException();

            for (int y = 0; y < h; y++)
            {
                Array.Copy(Pixels, (srcY + y) * Width + srcX, dst.Pixels, (dstY + y) * dst.Width + dstX, w);
            }
        }
    }
}