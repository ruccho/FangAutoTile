using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Text;
using UnityEngine.Experimental.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ruccho.Utilities
{
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Fang/Tile (Legacy)", fileName = "New Fang Auto Tile", order = 1)]
#endif
    public class FangAutoTile : TileBase
    {
        [SerializeField]
        public float m_MinSpeed = 1f;
        [SerializeField]
        public float m_MaxSpeed = 1f;
        [SerializeField]
        public float m_AnimationStartTime;

        [HideInInspector]
        [SerializeField]
        private Sprite[][] AllPatterns;

        [SerializeField]
        public Sprite RawTileImage;

        public Texture2D Texture;
        //Input
        public Texture2D[] CustomMaps;
        //Generated
        public Texture2D[] CustomTextures;

        public bool forceRelayout;
        [SerializeField]
        public string TileInfoMessage;
        public bool EnablePadding = false;
        public bool oneTilePerUnit = true;
        public int pixelPerUnit;
        public bool hideChildAssets = true;
        public bool enableCustomMapTexture = false;
        public Tile.ColliderType ColliderType;

        //[HideInInspector]
        [SerializeField]
        public List<FangAutoTilePattern> Patterns;

        private int CurrentIndex = 0;

        private bool CheckForBeingPrepared()
        {
            if (Patterns == null || Patterns.Count == 0) { Debug.Log("not prepared"); return false; }
            return true;
        }

        public override void RefreshTile(Vector3Int location, ITilemap tileMap)
        {
            for (int yd = -1; yd <= 1; yd++)
                for (int xd = -1; xd <= 1; xd++)
                {
                    Vector3Int position = new Vector3Int(location.x + xd, location.y + yd, location.z);
                    if (TileValue(tileMap, position))
                        tileMap.RefreshTile(position);
                }
        }
        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
        {
            if (CheckForBeingPrepared() == false) return false;
            if (Patterns[CurrentIndex].Frames.Length == 1)
            {
                return false;
            }
            else
            {
                tileAnimationData.animatedSprites = Patterns[CurrentIndex].Frames;
                tileAnimationData.animationSpeed = UnityEngine.Random.Range(m_MinSpeed, m_MaxSpeed);
                tileAnimationData.animationStartTime = m_AnimationStartTime;
                return true;
            }
        }

        public override void GetTileData(Vector3Int location, ITilemap tileMap, ref TileData tileData)
        {
            UpdateTile(location, tileMap, ref tileData);
            return;
        }

        private void UpdateTile(Vector3Int location, ITilemap tileMap, ref TileData tileData)
        {
            if (CheckForBeingPrepared() == false) return;
            tileData.transform = Matrix4x4.identity;
            tileData.color = Color.white;

            int mask = TileValue(tileMap, location + new Vector3Int(0, 1, 0)) ? 1 : 0;
            mask += TileValue(tileMap, location + new Vector3Int(1, 1, 0)) ? 2 : 0;
            mask += TileValue(tileMap, location + new Vector3Int(1, 0, 0)) ? 4 : 0;
            mask += TileValue(tileMap, location + new Vector3Int(1, -1, 0)) ? 8 : 0;
            mask += TileValue(tileMap, location + new Vector3Int(0, -1, 0)) ? 16 : 0;
            mask += TileValue(tileMap, location + new Vector3Int(-1, -1, 0)) ? 32 : 0;
            mask += TileValue(tileMap, location + new Vector3Int(-1, 0, 0)) ? 64 : 0;
            mask += TileValue(tileMap, location + new Vector3Int(-1, 1, 0)) ? 128 : 0;

            int index = 0;
            for (int i = 0; i < Patterns.Count; i++)
            {
                int masked = mask & (Patterns[i].Mask);
                if (masked == Patterns[i].Combination)
                {
                    //this is!
                    index = i;
                }
            }
            if (TileValue(tileMap, location))
            {
                tileData.sprite = Patterns[index].Frames[0];
                tileData.color = Color.white;
                tileData.flags = (TileFlags.LockTransform | TileFlags.LockColor);
                tileData.colliderType = ColliderType;
            }
            CurrentIndex = index;
        }

        private bool TileValue(ITilemap tileMap, Vector3Int position)
        {
            TileBase tile = tileMap.GetTile(position);
            return (tile != null && tile == this);
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(FangAutoTile))]
    public class FangAutoTileEditor : Editor
    {


        private FangAutoTile tile { get { return (target as FangAutoTile); } }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("");
            tile.RawTileImage = (Sprite)EditorGUILayout.ObjectField("Main Texture", tile.RawTileImage, typeof(Sprite), false, null);

            tile.EnablePadding = EditorGUILayout.ToggleLeft("Enable Padding", tile.EnablePadding, GUIStyle.none, null);
            tile.forceRelayout = EditorGUILayout.ToggleLeft("Force Re-layout", tile.forceRelayout, GUIStyle.none, null);
            tile.oneTilePerUnit = EditorGUILayout.ToggleLeft("1 Tile Per Unit", tile.oneTilePerUnit, GUIStyle.none, null);
            if (!tile.oneTilePerUnit)
            {
                tile.pixelPerUnit = EditorGUILayout.IntField("Pixel Per Unit", tile.pixelPerUnit);
            }
            tile.hideChildAssets = EditorGUILayout.ToggleLeft("Hide Sprite Assets", tile.hideChildAssets, GUIStyle.none, null);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CustomMaps"), true, null);
            if (GUILayout.Button("GENERATE!"))
            {
                SetupTiles(tile.forceRelayout);
            }
            if (!string.IsNullOrEmpty(tile.TileInfoMessage))
                EditorGUILayout.HelpBox(tile.TileInfoMessage, MessageType.Info);
            EditorGUILayout.LabelField("-");
            tile.ColliderType = (Tile.ColliderType)EditorGUILayout.EnumPopup("Collider Type", tile.ColliderType);
            float minSpeed = EditorGUILayout.FloatField("Minimum Speed", tile.m_MinSpeed);
            float maxSpeed = EditorGUILayout.FloatField("Maximum Speed", tile.m_MaxSpeed);
            if (minSpeed < 0.0f)
                minSpeed = 0.0f;

            if (maxSpeed < 0.0f)
                maxSpeed = 0.0f;

            if (maxSpeed < minSpeed)
                maxSpeed = minSpeed;

            tile.m_MinSpeed = minSpeed;
            tile.m_MaxSpeed = maxSpeed;
            tile.m_AnimationStartTime = EditorGUILayout.FloatField("Animation Start Time", tile.m_AnimationStartTime);

            if(GUILayout.Button("Export Extracted Texture"))
            {
                string path = EditorUtility.SaveFilePanel("Export Extracted Texture", "", name + ".png", "png");
                if(path.Length != 0)
                {
                    var pngData = tile.Texture.EncodeToPNG();
                    if (pngData != null) System.IO.File.WriteAllBytes(path, pngData);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(tile);
            }
            serializedObject.ApplyModifiedProperties();
        }

        public override Texture2D RenderStaticPreview
    (
        string assetPath,
        UnityEngine.Object[] subAssets,
        int width,
        int height
    )
        {
            if (tile.Patterns != null && tile.Patterns.Count != 0 && tile.Patterns[0].Frames != null && tile.Patterns[0].Frames.Length != 0)
            {
                Texture2D p = AssetPreview.GetAssetPreview(tile.Patterns[0].Frames[0]);
                Texture2D f = new Texture2D(width, height);
                EditorUtility.CopySerialized(p, f);
                return f;
            }
            return base.RenderStaticPreview(assetPath, subAssets, width, height);
        }
        private void GenerateTileInfoMessage(int tileSize, int frameCount, int texSize)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Generated data:\n");
            sb.AppendFormat("  Tile Size: {0}\n", tileSize.ToString());
            sb.AppendFormat("  Frame Count: {0}\n", frameCount.ToString());
            sb.AppendFormat("  Sprite Count: {0}\n", (tile.Patterns.Count * frameCount).ToString());
            sb.AppendFormat("  Texture Size: {0}", texSize.ToString());
            tile.TileInfoMessage = sb.ToString();
        }
        private void SetupTiles(bool forceRelayout)
        {
            int TileSize;
            int FrameCount;
            try
            {
                CheckCorrection(out TileSize, out FrameCount);
                Color[][][][] Parts = GenerateParts(tile.RawTileImage.texture, TileSize, FrameCount);
                Color[][][][][] CustomMapParts = new Color[tile.CustomMaps.Length][][][][];
                for (int i = 0; i < tile.CustomMaps.Length; i++)
                {
                    CustomMapParts[i] = GenerateParts(tile.CustomMaps[i], TileSize, FrameCount);
                }
                if (tile.Patterns == null || tile.Patterns.Count == 0)
                    EnumeratePatterns();

                int wholeTileSpriteCount = tile.Patterns.Count * FrameCount;
                int texSize = CalcTexSize(TileSize, wholeTileSpriteCount);
                CreateTextureAndSprites(Parts, CustomMapParts, texSize, TileSize, FrameCount, forceRelayout);
                GenerateTileInfoMessage(TileSize, FrameCount, texSize);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", e.Message, "OK");
                throw e;
            }
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(tile));
        }
        private void CheckCorrection(out int tileSize, out int frameCount)
        {
            tile.RawTileImage.texture.GetPixels(0, 0, 1, 1);

            int textureWidth = tile.RawTileImage.texture.width;
            int textureHeight = tile.RawTileImage.texture.height;
            //height
            if (textureHeight % 5 != 0) throw new FangAutoTileIncorrectException("Height of texture must be multiple of 5");
            tileSize = textureHeight / 5;

            if (textureWidth % tileSize != 0) throw new FangAutoTileIncorrectException("Width of texture must be (tile size * frame count)");
            frameCount = textureWidth / tileSize;
            if (tile.CustomMaps == null)
            {
                tile.CustomMaps = new Texture2D[0];
            }
            else
                foreach (Texture2D tex in tile.CustomMaps)
                {
                    if (tex.width != textureWidth || tex.height != textureHeight)
                    {
                        throw new FangAutoTileIncorrectException("All CustomMaps must be the same size as the main texture");
                    }
                }
        }
        private Color[][][][] GenerateParts(Texture2D texture, int tileSize, int frameCount)
        {
            int partSize = tileSize / 2;
            int partColors = partSize * partSize;
            Color[][][][] parts = new Color[5][][][];
            //Type Iteration
            for (int tileType = 0; tileType < 5; tileType++)
            {
                parts[tileType] = new Color[frameCount][][];
                int TLY = (4 - tileType) * tileSize;
                //Frame Iteration
                for (int frame = 0; frame < frameCount; frame++)
                {
                    parts[tileType][frame] = new Color[4][];
                    //calc top-left of tile
                    int TLX = frame * tileSize;
                    //Part Iteration
                    for (int part = 0; part < 4; part++)
                    {
                        parts[tileType][frame][part] = new Color[partColors];
                        int tlxOffsetted = TLX + ((part == 1 || part == 3) ? partSize : 0);
                        int tlyOffsetted = TLY + ((part == 0 || part == 1) ? partSize : 0);
                        parts[tileType][frame][part] = texture.GetPixels(tlxOffsetted, tlyOffsetted, partSize, partSize);
                    }
                }
            }
            return parts;
        }
        private bool IsHorizontalEdgeClosed(int type)
        {
            switch (type)
            {
                case 0:
                case 2:
                    return true;
                case 1:
                case 3:
                case 4:
                    return false;
                default:
                    throw new IndexOutOfRangeException("Tile type id must be in range from 0 to 4");
            }
        }
        private bool IsVerticalEdgeClosed(int type)
        {
            switch (type)
            {
                case 0:
                case 1:
                    return true;
                case 2:
                case 3:
                case 4:
                    return false;
                default:
                    throw new IndexOutOfRangeException("Tile type id must be in range from 0 to 4");
            }
        }
        private int GetMaskPattern(int[] parts)
        {
            if (parts.Length != 4) throw new ArgumentException("An Error occured when trying to get MASK pattern");
            int[] primitiveMaskPatterns =
            {
            85,//0b01010101,
            85,//0b01010101,
            85,//0b01010101,
            255,//0b11111111,
            255,//0b11111111
        };
            int[] directionMaskWindow = {
            193,//0b11000001,
            7,//0b00000111,
            112,//0b01110000,
            28,//0b00011100
        };
            int mask = 0;
            for (int i = 0; i < 4; i++)
            {
                mask = mask | (primitiveMaskPatterns[parts[i]] & directionMaskWindow[i]);
            }
            return mask;
        }
        private int GetCombinationPattern(int[] parts)
        {
            if (parts.Length != 4) throw new ArgumentException("An Error occured when trying to get COMBINATION pattern");
            int[] primitiveCombinationPatterns =
            {
            0,//0b00000000,
            17,//0b00010001,
            68,//0b01000100,
            85,//0b01010101,
            255,//0b11111111
        };
            int[] directionMaskWindow = {
            193,//0b11000001,
            7,//0b00000111,
            112,//0b01110000,
            28,//0b00011100
        };
            int mask = 0;
            for (int i = 0; i < 4; i++)
            {
                mask = mask | (primitiveCombinationPatterns[parts[i]] & directionMaskWindow[i]);
            }
            return mask;
        }
        private void RemoveAllSpriteAssets()
        {
            //Delete All Sprites
            if (tile.Patterns != null)
                foreach (FangAutoTilePattern pattern in tile.Patterns)
                {
                    if (pattern.Frames != null)
                        foreach (Sprite s in pattern.Frames)
                        {
                            if (s) DestroyImmediate(s, true);
                        }
                }
            //if (tile.Texture) DestroyImmediate(tile.Texture, true);
            //if (tile.CustomTextures != null)
            //    for (int i = 0; i < tile.CustomTextures.Length; i++)
            //    {
            //        DestroyImmediate(tile.CustomTextures[i], true);
            //    }
        }
        private void RemoveAllPatterns()
        {
            RemoveAllSpriteAssets();
            if (tile.Patterns != null)
            {
                foreach (FangAutoTilePattern pattern in tile.Patterns)
                {
                    UnityEngine.Object.DestroyImmediate(pattern, true);
                }
            }
            tile.Patterns = new List<FangAutoTilePattern>();

            //AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(tile));
        }
        private void EnumeratePatterns()
        {
            RemoveAllPatterns();
            for (int TL = 0; TL < 5; TL++)
            {
                for (int TR = 0; TR < 5; TR++)
                {
                    if (IsHorizontalEdgeClosed(TL) == IsHorizontalEdgeClosed(TR))
                    {
                        //Correct
                        for (int BL = 0; BL < 5; BL++)
                        {
                            if (IsVerticalEdgeClosed(TL) == IsVerticalEdgeClosed(BL))
                            {
                                //Correct
                                for (int BR = 0; BR < 5; BR++)
                                {
                                    if (IsHorizontalEdgeClosed(BL) == IsHorizontalEdgeClosed(BR) && IsVerticalEdgeClosed(TR) == IsVerticalEdgeClosed(BR))
                                    {
                                        //Correct
                                        int[] parts = new int[4] { TL, TR, BL, BR };
                                        FangAutoTilePattern pattern = CreateInstance<FangAutoTilePattern>();
                                        pattern.Mask = GetMaskPattern(parts);
                                        pattern.Combination = GetCombinationPattern(parts);
                                        pattern.Pattern = parts;
                                        pattern.name = "Pattern " + TL.ToString() + TR.ToString() + BL.ToString() + BR.ToString();
                                        pattern.hideFlags = HideFlags.HideInHierarchy;
                                        AssetDatabase.AddObjectToAsset(pattern, tile);
                                        tile.Patterns.Add(pattern);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(tile));
        }
        public int CalcTexSize(int tileSize, int tileCount)
        {
            //Approach from square
            int paddedTileSize = tileSize;
            if (tile.EnablePadding) paddedTileSize += 2;
            int s = paddedTileSize * paddedTileSize * tileCount;
            int e = (int)Mathf.Sqrt(s);
            int minW = 0;
            for (int i = 2; i <= 4096; i *= 2)
            {
                if (e < i)
                {
                    minW = i;
                    break;
                }
            }
            if (minW == 0) throw new FangAutoTileIncorrectException("Your tiles are too big or have too many frames!");
            while (true)
            {
                int c = minW / paddedTileSize;
                if (c * c >= tileCount)
                {
                    //Enough
                    break;
                }
                else
                {
                    //Falls short
                    if (minW >= 2048)
                    {
                        throw new FangAutoTileIncorrectException("Your tiles are too big or have too many frames!");

                    }
                    minW *= 2;

                    continue;
                }
            }
            return minW;
        }
        private void CreateTextureAndSprites(Color[][][][] parts, Color[][][][][] customTextureParts, int texSize, int tileSize, int frameCount, bool forceRegenerate = false)
        {
            string tilePath = AssetDatabase.GetAssetPath(tile);
            int paddedTileSize = tileSize;
            if (tile.EnablePadding) paddedTileSize += 2;
            int c = texSize / paddedTileSize;
            int column = 0;
            int row = 0;
            Color[] mainTexArray = new Color[texSize * texSize];
            Color[][] customTexArrays = new Color[customTextureParts.Length][];
            for (int i = 0; i < customTexArrays.Length; i++)
            {
                customTexArrays[i] = new Color[texSize * texSize];
            }
            Texture2D mainTex;
            Texture2D[] customTexs = new Texture2D[customTextureParts.Length];
            if (tile.oneTilePerUnit) tile.pixelPerUnit = tileSize;
            
            var format = GraphicsFormat.R8G8B8A8_SRGB;
            
            bool replaceMode = false;
            bool texParamsUnchanged = tile.Texture && tile.Texture.width == texSize && tile.Texture.height == texSize && tile.Texture.graphicsFormat == format;
            bool frameCountUnchanged = tile.Patterns[0].Frames != null && tile.Patterns[0].Frames.Length != 0 && frameCount == tile.Patterns[0].Frames.Length;
            bool tileSizeUnchanged = tile.Patterns[0].Frames != null && tile.Patterns[0].Frames.Length != 0 && tile.Patterns[0].Frames[0].rect.width == tileSize;
            bool pixPerUnitUnchanged = tile.Patterns[0].Frames != null && tile.Patterns[0].Frames.Length != 0 && tile.Patterns[0].Frames[0].pixelsPerUnit == tile.pixelPerUnit;
            bool paddingUnchanged = tile.Patterns[0].Frames != null && tile.Patterns[0].Frames.Length != 0 && tile.Patterns[0].Frames[0].rect.x == (tile.EnablePadding ? 1 : 0);
            replaceMode = texParamsUnchanged && frameCountUnchanged && tileSizeUnchanged && !forceRegenerate && pixPerUnitUnchanged && paddingUnchanged;
            
            
            //Setup main texture instance
            if (tile.Texture)
            {
                //exist
                mainTex = tile.Texture;
                if (!texParamsUnchanged)
                {
                    //Resize texture
                    mainTex.Reinitialize(texSize, texSize, format, false);
                }
            }
            else
            {
                //Create an instance of main texture
                mainTex = new Texture2D(texSize, texSize, DefaultFormat.LDR, TextureCreationFlags.None);
                mainTex.Reinitialize(texSize, texSize, format, false);
            }
            //Setup textures of custom maps
            if (tile.CustomTextures == null) tile.CustomTextures = new Texture2D[0];
            for (int i = 0; i < customTexs.Length; i++)
            {
                if (i < tile.CustomTextures.Length)
                {
                    customTexs[i] = tile.CustomTextures[i];
                    if (!texParamsUnchanged)
                    {
                        //Resize texture
                        customTexs[i].Reinitialize(texSize, texSize, TextureFormat.RGBA32, false);
                    }
                }
                else
                {
                    customTexs[i] = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
                }
            }
            //delete unused texture asset
            for (int i = customTexs.Length; i < tile.CustomTextures.Length; i++)
            {
                DestroyImmediate(tile.CustomTextures[i], true);
            }
            if (!replaceMode) RemoveAllSpriteAssets();

            int partSize = tileSize / 2;

            for (int i = 0; i < tile.Patterns.Count; i++)
            {
                if (!replaceMode)
                {
                    tile.Patterns[i].Frames = new Sprite[frameCount];
                }
                for (int frame = 0; frame < frameCount; frame++)
                {
                    int tlx = column * paddedTileSize;
                    int tly = row * paddedTileSize;
                    for (int part = 0; part < 4; part++)
                    {
                        int tlxOffsetted = tlx + (part % 2 == 1 ? partSize : 0);
                        int tlyOffsetted = tly + (part < 2 ? partSize : 0);
                        for (int line = 0; line < partSize; line++)
                        {
                            if (!tile.EnablePadding)
                            {
                                //No Padding
                                Array.Copy(parts[tile.Patterns[i].Pattern[part]][frame][part], line * partSize, mainTexArray, tlyOffsetted * texSize + tlxOffsetted, partSize);
                                for (int ctex = 0; ctex < customTexs.Length; ctex++)
                                {
                                    Array.Copy(customTextureParts[ctex][tile.Patterns[i].Pattern[part]][frame][part], line * partSize, customTexArrays[ctex], tlyOffsetted * texSize + tlxOffsetted, partSize);
                                }
                            }
                            else
                            {
                                //padding enabled
                                Array.Copy(parts[tile.Patterns[i].Pattern[part]][frame][part], line * partSize, mainTexArray, (tlyOffsetted + 1) * texSize + (tlxOffsetted + 1), partSize);
                                //Clamp x
                                if (part == 0 || part == 2)
                                    mainTexArray[(tlyOffsetted + 1) * texSize + tlxOffsetted] = parts[tile.Patterns[i].Pattern[part]][frame][part][line * partSize];
                                if (part == 1 || part == 3)
                                    mainTexArray[(tlyOffsetted + 1) * texSize + tlxOffsetted + partSize + 1] = parts[tile.Patterns[i].Pattern[part]][frame][part][(line + 1) * partSize - 1];
                                for (int ctex = 0; ctex < customTexs.Length; ctex++)
                                {
                                    Array.Copy(customTextureParts[ctex][tile.Patterns[i].Pattern[part]][frame][part], line * partSize, customTexArrays[ctex], (tlyOffsetted + 1) * texSize + (tlxOffsetted + 1), partSize);
                                    //Clamp x
                                    if (part == 0 || part == 2)
                                        customTexArrays[ctex][(tlyOffsetted + 1) * texSize + tlxOffsetted] = customTextureParts[ctex][tile.Patterns[i].Pattern[part]][frame][part][line * partSize];
                                    if (part == 1 || part == 3)
                                        customTexArrays[ctex][(tlyOffsetted + 1) * texSize + tlxOffsetted + partSize + 1] = customTextureParts[ctex][tile.Patterns[i].Pattern[part]][frame][part][(line + 1) * partSize - 1];
                                }
                            }
                            tlyOffsetted++;
                        }
                    }
                    if (tile.EnablePadding)
                    {
                        //Clamp y
                        Array.Copy(mainTexArray, (tly + 1) * texSize + tlx, mainTexArray, tly * texSize + tlx, paddedTileSize);
                        Array.Copy(mainTexArray, (tly + tileSize + 0) * texSize + tlx, mainTexArray, (tly + tileSize + 1) * texSize + tlx, paddedTileSize);
                        for (int ctex = 0; ctex < customTexs.Length; ctex++)
                        {
                            Array.Copy(customTexArrays[ctex], (tly + 1) * texSize + tlx, customTexArrays[ctex], tly * texSize + tlx, paddedTileSize);
                            Array.Copy(customTexArrays[ctex], (tly + tileSize + 0) * texSize + tlx, customTexArrays[ctex], (tly + tileSize + 1) * texSize + tlx, paddedTileSize);
                        }
                    }

                    if (!replaceMode)
                    {
                        if (tile.EnablePadding)
                        {
                            tlx += 1;
                            tly += 1;
                        }
                        Sprite s = Sprite.Create(mainTex, new Rect(tlx, tly, tileSize, tileSize), new Vector2(0.5f, 0.5f), tile.pixelPerUnit);
                        tile.Patterns[i].Frames[frame] = s;
                        if (tile.hideChildAssets)
                            s.hideFlags = HideFlags.HideInHierarchy;
                        AssetDatabase.AddObjectToAsset(s, tilePath);
                    }
                    else
                    {
                        if (!tile.hideChildAssets)
                            tile.Patterns[i].Frames[frame].hideFlags = HideFlags.None;
                        else
                            tile.Patterns[i].Frames[frame].hideFlags = HideFlags.HideInHierarchy;
                    }
                    column++;
                    if (column >= c)
                    {
                        row++;
                        column = 0;
                    }
                }
            }
            mainTex.filterMode = FilterMode.Point;
            mainTex.wrapMode = TextureWrapMode.Clamp;
            mainTex.SetPixels(mainTexArray);
            mainTex.Apply(false, false);
            if (!tile.Texture)
            {
                mainTex.name = "MainTexture";
                AssetDatabase.AddObjectToAsset(mainTex, tilePath);
                //Debug.LogWarning("FangAutoTile: The structure of \"" + tile.name + "\" has been changed. Reload current scene to apply this change to tilemap.");
            }
            tile.Texture = mainTex;

            for (int i = 0; i < customTexs.Length; i++)
            {
                customTexs[i].filterMode = FilterMode.Point;
                customTexs[i].wrapMode = TextureWrapMode.Clamp;
                customTexs[i].SetPixels(customTexArrays[i]);
                customTexs[i].Apply(false, false);
                if (i >= tile.CustomTextures.Length)
                {
                    customTexs[i].name = "CustomMap " + i;
                    AssetDatabase.AddObjectToAsset(customTexs[i], tilePath);
                }
                else
                {
                    //already exist

                }
            }
            tile.CustomTextures = customTexs;
            //AssetDatabase.ImportAsset(tilePath);
        }
    }

    [CustomPreview(typeof(FangAutoTile))]
    public class FangAutoTilePreview : ObjectPreview
    {
        private GUIContent previewTitle = new GUIContent("Tiles");
        private FangAutoTile tile { get { return (target as FangAutoTile); } }
        public override bool HasPreviewGUI()
        {
            if (!target) return false;
            return m_Targets.Length > 1;
        }

        public override GUIContent GetPreviewTitle()
        {
            return previewTitle;
        }
        public override void Initialize(UnityEngine.Object[] targets)
        {
            base.Initialize(targets);

            var sprites = new UnityEngine.Object[0];
            if (tile.Patterns != null)
                foreach (FangAutoTilePattern pattern in tile.Patterns)
                {
                    if (pattern.Frames != null)
                        ArrayUtility.AddRange(ref sprites, pattern.Frames);
                }
            if (sprites.Length != 0)
                m_Targets = sprites;
        }
        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            var previewTexture = AssetPreview.GetAssetPreview(target);
            if (previewTexture)
                EditorGUI.DrawTextureTransparent(r, previewTexture);
        }
    }
    [CustomPreview(typeof(FangAutoTile))]
    public class FangAutoTileTexturePreview : ObjectPreview
    {
        private GUIContent previewTitle = new GUIContent("Texture");
        private FangAutoTile tile { get { return (target as FangAutoTile); } }
        public override bool HasPreviewGUI()
        {
            if (!target) return false;
            return tile.Texture;
        }

        public override GUIContent GetPreviewTitle()
        {
            return previewTitle;
        }
        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (tile.Texture)
            {
                //    var previewTexture = AssetPreview.GetAssetPreview(tile.Texture);
                //    EditorGUI.DrawTextureTransparent(r, previewTexture);
                Editor e = Editor.CreateEditor(tile.Texture);
                e.OnPreviewGUI(r, background);
                //GUI.SelectionGrid(r, -1, new Texture2D[] { tile.Texture }, 1, EditorStyles.whiteBoldLabel);
                //EditorGUI.DrawTextureTransparent(r, tile.Texture);
            }
        }
    }
    public class FangAutoTileIncorrectException : Exception
    {
        public FangAutoTileIncorrectException(string message) : base(message)
        {
        }
    }
#endif
}