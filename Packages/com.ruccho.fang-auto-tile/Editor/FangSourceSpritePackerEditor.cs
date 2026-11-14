using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace Ruccho.Fang
{
    [CustomEditor(typeof(FangSourceSpritePacker))]
    public class FangSourceSpritePackerEditor : Editor
    {
        private const int MaxTextureSize = 8192;

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("targets"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("padding"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wrapMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("filterMode"));

            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(EditorGUIUtility.singleLineHeight * 0.5f);

            if (GUILayout.Button("Generate!", GUILayout.Height(50f)))
            {
                if (!Generate(out var message)) EditorUtility.DisplayDialog("Fang Auto Tile", message, "OK");
                GUIUtility.ExitGUI();
            }

            var packer = (FangSourceSpritePacker)target;
            var pages = packer.Pages;
            if (pages.Length == 0)
            {
                EditorGUILayout.HelpBox("Not generated", MessageType.Info);
                return;
            }

            var main = pages[0].Texture;
            EditorGUILayout.HelpBox(
                $"Sources: {packer.Entries.Length}\n" +
                $"Tiles: {packer.Entries.Sum(e => e.TileSize > 0 ? e.Size.x / e.TileSize * (e.Size.y / e.TileSize) : 0)} ({packer.NumPackedTiles} unique)\n" +
                (main ? $"Texture size: {main.width} x {main.height}\n" : "") +
                "Secondary textures: " +
                (pages.Length == 1 ? "none" : string.Join(", ", pages.Skip(1).Select(p => p.Name))),
                MessageType.Info);
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            var pages = ((FangSourceSpritePacker)target).Pages;
            if (pages.Length > 0 && pages[0].Texture)
            {
                var preview = AssetPreview.GetAssetPreview(pages[0].Texture);
                if (preview)
                {
                    var result = new Texture2D(width, height);
                    EditorUtility.CopySerialized(preview, result);
                    return result;
                }
            }

            return base.RenderStaticPreview(assetPath, subAssets, width, height);
        }

        /// <summary>
        /// Packs the source sprites of the targets and generates the targets again with the packed textures.
        /// Tiles removed from the targets are generated again with their source sprites.
        /// </summary>
        public bool Generate(out string message)
        {
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            var packer = (FangSourceSpritePacker)target;
            var tiles = (packer.Targets ?? Array.Empty<FangAutoTile>()).Where(t => t).Distinct().ToList();
            var editors = new List<FangAutoTileEditor>();

            try
            {
                // validate the targets
                foreach (var tile in tiles)
                {
                    var editor = (FangAutoTileEditor)CreateEditor(tile, typeof(FangAutoTileEditor));
                    editors.Add(editor);
                    editor.serializedObject.Update();

                    if (!editor.UsesSourceSprite)
                    {
                        message = $"\"{tile.name}\" does not use {nameof(FangAutoTile.SpriteSourceType.SourceSpriteMesh)}.";
                        return false;
                    }

                    // the tile may not be packed yet
                    editor.serializedObject.FindProperty(FangAutoTileEditor.p_SourcePacker).objectReferenceValue = null;
                    if (!editor.CheckValidity(out var tileMessage))
                    {
                        message = $"\"{tile.name}\": {tileMessage}";
                        return false;
                    }
                }

                var sources = tiles.Select(t => t.SourceSprite).Distinct().ToList();

                // all sources need the same secondary textures
                var secondaryNames = sources.Count > 0
                    ? FangMeshSpriteBuilder.GetSecondaryTextures(sources[0]).Select(s => s.name).ToArray()
                    : Array.Empty<string>();
                foreach (var source in sources)
                {
                    var names = FangMeshSpriteBuilder.GetSecondaryTextures(source).Select(s => s.name);
                    if (!names.OrderBy(n => n, StringComparer.Ordinal)
                            .SequenceEqual(secondaryNames.OrderBy(n => n, StringComparer.Ordinal)))
                    {
                        message =
                            $"The secondary textures of \"{source.name}\" ({string.Join(", ", names)}) differ from those of \"{sources[0].name}\" ({string.Join(", ", secondaryNames)}).";
                        return false;
                    }
                }

                // channel 0 is the main texture
                var channelTextures = sources.Select(source =>
                {
                    var secondaries = FangMeshSpriteBuilder.GetSecondaryTextures(source);
                    return new[] { FangMeshSpriteBuilder.GetSourceTexture(source) }
                        .Concat(secondaryNames.Select(n => secondaries.First(s => s.name == n).texture as Texture2D))
                        .ToArray();
                }).ToList();

                for (int i = 0; i < sources.Count; i++)
                {
                    var main = channelTextures[i][0];
                    if (channelTextures[i].Any(t => !t || t.width != main.width || t.height != main.height))
                    {
                        message =
                            $"The secondary textures of \"{sources[i].name}\" have to be Texture2D of the same size as the main texture.";
                        return false;
                    }
                }

                // tile size of each source sprite
                var tileSizes = new int[sources.Count];
                foreach (var editor in editors)
                {
                    var tile = (FangAutoTile)editor.target;
                    int index = sources.IndexOf(tile.SourceSprite);
                    var rect = ToRectInt(tile.SourceSprite.rect);
                    int numSlopes = editor.serializedObject.FindProperty(FangAutoTileEditor.p_NumSlopes).intValue;
                    FangTileLayout.TryGet(rect.width, rect.height, numSlopes, out var layout);
                    if (tileSizes[index] != 0 && tileSizes[index] != layout.TileSize)
                    {
                        message = $"The tiles that use \"{sources[index].name}\" have different tile sizes.";
                        return false;
                    }

                    tileSizes[index] = layout.TileSize;
                }

                int numChannels = secondaryNames.Length + 1;
                var sourceRects = sources.Select(s => ToRectInt(s.rect)).ToList();

                // pixels of each channel of each source sprite
                var sourcePixels = new Color32[sources.Count][][];
                for (int i = 0; i < sources.Count; i++)
                {
                    sourcePixels[i] = new Color32[numChannels][];
                    for (int c = 0; c < numChannels; c++)
                    {
                        var copy = FangAutoTileEditor.CopyTextureRect(channelTextures[i][c], sourceRects[i]);
                        sourcePixels[i][c] = copy.GetPixels32();
                        DestroyImmediate(copy);
                    }
                }

                // tiles with the same pixels in all channels share a location
                var uniqueTiles = new List<(int source, RectInt rect)>();
                var buckets = new Dictionary<int, List<int>>();
                var tileMaps = new int[sources.Count][];
                for (int i = 0; i < sources.Count; i++)
                {
                    int tileSize = tileSizes[i];
                    int columns = sourceRects[i].width / tileSize;
                    int rows = sourceRects[i].height / tileSize;
                    tileMaps[i] = new int[columns * rows];

                    for (int y = 0; y < rows; y++)
                    for (int x = 0; x < columns; x++)
                    {
                        var rect = new RectInt(x * tileSize, y * tileSize, tileSize, tileSize);
                        int hash = HashTile(sourcePixels[i], sourceRects[i].width, rect);
                        if (!buckets.TryGetValue(hash, out var bucket)) buckets[hash] = bucket = new List<int>();

                        int found = bucket.FindIndex(u =>
                            uniqueTiles[u].rect.size == rect.size &&
                            TilesEqual(sourcePixels[i], sourceRects[i].width, rect,
                                sourcePixels[uniqueTiles[u].source], sourceRects[uniqueTiles[u].source].width,
                                uniqueTiles[u].rect));

                        if (found < 0)
                        {
                            bucket.Add(uniqueTiles.Count);
                            tileMaps[i][y * columns + x] = uniqueTiles.Count;
                            uniqueTiles.Add((i, rect));
                        }
                        else
                        {
                            tileMaps[i][y * columns + x] = bucket[found];
                        }
                    }
                }

                // pack
                int padding = serializedObject.FindProperty("padding").intValue;
                if (!TryPack(uniqueTiles.Select(u => u.rect.size).ToList(), padding, out var packedRects, out var size))
                {
                    message = $"The source sprites do not fit in a texture of {MaxTextureSize} x {MaxTextureSize}.";
                    return false;
                }

                var wrapMode = (TextureWrapMode)serializedObject.FindProperty("wrapMode").intValue;
                var filterMode = (FilterMode)serializedObject.FindProperty("filterMode").intValue;

                // pages
                var pagesProp = serializedObject.FindProperty("pages");
                for (int c = numChannels; c < pagesProp.arraySize; c++)
                {
                    var unused = pagesProp.GetArrayElementAtIndex(c).FindPropertyRelative("texture").objectReferenceValue;
                    if (unused) DestroyImmediate(unused, true);
                }

                pagesProp.arraySize = numChannels;

                for (int c = 0; c < numChannels; c++)
                {
                    var pageProp = pagesProp.GetArrayElementAtIndex(c);
                    var textureProp = pageProp.FindPropertyRelative("texture");
                    var name = c == 0 ? "" : secondaryNames[c - 1];
                    pageProp.FindPropertyRelative("name").stringValue = name;

                    bool linear = sources.Count > 0 &&
                                  !GraphicsFormatUtility.IsSRGBFormat(channelTextures[0][c].graphicsFormat);
                    var format = linear ? GraphicsFormat.R8G8B8A8_UNorm : GraphicsFormat.R8G8B8A8_SRGB;

                    var page = textureProp.objectReferenceValue as Texture2D;
                    if (!page)
                    {
                        page = new Texture2D(size.x, size.y, format, TextureCreationFlags.None);
                        AssetDatabase.AddObjectToAsset(page, packer);
                        textureProp.objectReferenceValue = page;
                    }
                    else if (page.width != size.x || page.height != size.y || page.graphicsFormat != format)
                    {
                        page.Reinitialize(size.x, size.y, format, false);
                    }

                    page.name = c == 0 ? "Main" : name;
                    page.wrapMode = wrapMode;
                    page.filterMode = filterMode;

                    var pixels = new Color32[size.x * size.y];
                    for (int u = 0; u < uniqueTiles.Count; u++)
                    {
                        var (source, rect) = uniqueTiles[u];
                        Blit(sourcePixels[source][c], sourceRects[source].width, rect, pixels, size.x,
                            packedRects[u].position, padding);
                    }

                    page.SetPixels32(pixels);
                    page.Apply(false, false);
                    EditorUtility.SetDirty(page);
                }

                // entries
                var entriesProp = serializedObject.FindProperty("entries");
                entriesProp.arraySize = sources.Count;
                for (int i = 0; i < sources.Count; i++)
                {
                    var entryProp = entriesProp.GetArrayElementAtIndex(i);
                    entryProp.FindPropertyRelative("source").objectReferenceValue = sources[i];
                    entryProp.FindPropertyRelative("size").vector2IntValue = sourceRects[i].size;
                    entryProp.FindPropertyRelative("tileSize").intValue = tileSizes[i];
                    var positionsProp = entryProp.FindPropertyRelative("tilePositions");
                    positionsProp.arraySize = tileMaps[i].Length;
                    for (int t = 0; t < tileMaps[i].Length; t++)
                        positionsProp.GetArrayElementAtIndex(t).vector2IntValue = packedRects[tileMaps[i][t]].position;
                }

                serializedObject.FindProperty("numPackedTiles").intValue = uniqueTiles.Count;

                // tiles removed from the targets reference their source sprites again
                var packedTilesProp = serializedObject.FindProperty("packedTiles");
                var removedTiles = Enumerable.Range(0, packedTilesProp.arraySize)
                    .Select(i => packedTilesProp.GetArrayElementAtIndex(i).objectReferenceValue as FangAutoTile)
                    .Where(t => t && !tiles.Contains(t)).ToList();

                packedTilesProp.arraySize = tiles.Count;
                for (int i = 0; i < tiles.Count; i++)
                    packedTilesProp.GetArrayElementAtIndex(i).objectReferenceValue = tiles[i];

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();

                foreach (var tile in removedTiles)
                {
                    var editor = (FangAutoTileEditor)CreateEditor(tile, typeof(FangAutoTileEditor));
                    editors.Add(editor);
                    editor.serializedObject.Update();
                    var sourcePackerProp = editor.serializedObject.FindProperty(FangAutoTileEditor.p_SourcePacker);
                    if (sourcePackerProp.objectReferenceValue != packer) continue;
                    sourcePackerProp.objectReferenceValue = null;
                    GenerateTile(editor);
                }

                foreach (var editor in editors.Take(tiles.Count))
                {
                    editor.serializedObject.FindProperty(FangAutoTileEditor.p_SourcePacker).objectReferenceValue = packer;
                    GenerateTile(editor);
                }

                AssetDatabase.SaveAssets();
                message = null;
                return true;
            }
            finally
            {
                foreach (var editor in editors) DestroyImmediate(editor);
            }
        }

        private static void GenerateTile(FangAutoTileEditor editor)
        {
            editor.GenerateCombination();
            editor.GenerateSourceSpriteData();
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(editor.target);
            AssetDatabase.SaveAssets();
            FangAutoTileEditor.RefreshTilemaps(editor.target as FangAutoTile);
        }

        private static RectInt ToRectInt(Rect rect)
        {
            return new RectInt(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), Mathf.RoundToInt(rect.width),
                Mathf.RoundToInt(rect.height));
        }

        private static int HashTile(Color32[][] channels, int width, RectInt rect)
        {
            unchecked
            {
                int hash = 17;
                foreach (var pixels in channels)
                {
                    for (int y = rect.yMin; y < rect.yMax; y++)
                    for (int x = rect.xMin; x < rect.xMax; x++)
                    {
                        var p = pixels[y * width + x];
                        hash = hash * 31 + (p.r | (p.g << 8) | (p.b << 16) | (p.a << 24));
                    }
                }

                return hash;
            }
        }

        private static bool TilesEqual(Color32[][] a, int widthA, RectInt rectA, Color32[][] b, int widthB,
            RectInt rectB)
        {
            for (int c = 0; c < a.Length; c++)
            {
                for (int y = 0; y < rectA.height; y++)
                for (int x = 0; x < rectA.width; x++)
                {
                    var pa = a[c][(rectA.y + y) * widthA + rectA.x + x];
                    var pb = b[c][(rectB.y + y) * widthB + rectB.x + x];
                    if (pa.r != pb.r || pa.g != pb.g || pa.b != pb.b || pa.a != pb.a) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Copies a rect of the source pixels to the destination, and extrudes the pixels on the edges by the padding.
        /// </summary>
        private static void Blit(Color32[] src, int srcWidth, RectInt srcRect, Color32[] dst, int dstWidth,
            Vector2Int dstPosition, int padding)
        {
            for (int y = -padding; y < srcRect.height + padding; y++)
            {
                int sy = srcRect.y + Mathf.Clamp(y, 0, srcRect.height - 1);
                for (int x = -padding; x < srcRect.width + padding; x++)
                {
                    int sx = srcRect.x + Mathf.Clamp(x, 0, srcRect.width - 1);
                    dst[(dstPosition.y + y) * dstWidth + dstPosition.x + x] = src[sy * srcWidth + sx];
                }
            }
        }

        /// <summary>
        /// Packs rects into the smallest texture whose width and height are powers of two, with shelves of rects
        /// sorted by height. Among textures of the same area, the one closer to a square is preferred.
        /// </summary>
        /// <returns>Rects without padding.</returns>
        private static bool TryPack(IReadOnlyList<Vector2Int> sizes, int padding, out RectInt[] rects,
            out Vector2Int textureSize)
        {
            var order = Enumerable.Range(0, sizes.Count)
                .OrderByDescending(i => sizes[i].y).ThenByDescending(i => sizes[i].x).ToArray();
            long area = sizes.Sum(s => (long)(s.x + padding * 2) * (s.y + padding * 2));

            var candidates = new List<Vector2Int>();
            for (int w = 1; w <= MaxTextureSize; w *= 2)
            for (int h = 1; h <= MaxTextureSize; h *= 2)
                if ((long)w * h >= area)
                    candidates.Add(new Vector2Int(w, h));

            foreach (var candidate in candidates
                         .OrderBy(c => (long)c.x * c.y)
                         .ThenBy(c => Mathf.Abs(c.x - c.y))
                         .ThenByDescending(c => c.x))
            {
                if (TryPackShelves(sizes, order, padding, candidate, out rects))
                {
                    textureSize = candidate;
                    return true;
                }
            }

            rects = null;
            textureSize = default;
            return false;
        }

        private static bool TryPackShelves(IReadOnlyList<Vector2Int> sizes, int[] order, int padding,
            Vector2Int textureSize, out RectInt[] rects)
        {
            rects = new RectInt[sizes.Count];
            int x = 0, y = 0, shelfHeight = 0;
            foreach (var i in order)
            {
                int w = sizes[i].x + padding * 2;
                int h = sizes[i].y + padding * 2;
                if (w > textureSize.x) return false;

                if (x + w > textureSize.x)
                {
                    x = 0;
                    y += shelfHeight;
                    shelfHeight = 0;
                }

                if (y + h > textureSize.y) return false;

                rects[i] = new RectInt(x + padding, y + padding, sizes[i].x, sizes[i].y);
                x += w;
                shelfHeight = Mathf.Max(shelfHeight, h);
            }

            return true;
        }
    }
}
