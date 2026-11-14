using System;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace Ruccho.Fang
{
    /// <summary>
    /// Builds tile sprites whose meshes reference regions of a source sprite directly.
    /// The sprites reference the texture of the source sprite before packing, and have the secondary textures of the source sprite.
    /// </summary>
    internal readonly struct FangMeshSpriteBuilder
    {
        private readonly Texture2D texture;
        private readonly SecondarySpriteTexture[] secondaryTextures;
        private readonly RectInt sourceRect;

        public bool IsValid => texture != null;

        public FangMeshSpriteBuilder(Sprite source)
        {
            texture = null;
            secondaryTextures = Array.Empty<SecondarySpriteTexture>();
            sourceRect = default;

            if (!source) return;

            var path = AssetDatabase.GetAssetPath(source);
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!texture) texture = source.texture;
            if (!texture) return;

            // Sprite.rect is the location on the texture before packing
            var rect = source.rect;
            sourceRect = new RectInt(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), Mathf.RoundToInt(rect.width),
                Mathf.RoundToInt(rect.height));

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                secondaryTextures = importer.secondarySpriteTextures?.Where(s => s.texture).ToArray() ??
                                    Array.Empty<SecondarySpriteTexture>();
            }
            else
            {
                var count = source.GetSecondaryTextureCount();
                if (count > 0)
                {
                    secondaryTextures = new SecondarySpriteTexture[count];
                    source.GetSecondaryTextures(secondaryTextures);
                }
            }
        }

        public int SourceWidth => sourceRect.width;
        public int SourceHeight => sourceRect.height;

        /// <summary>
        /// Builds a sprite of <paramref name="tileWidth"/> x <paramref name="tileHeight"/> pixels with its pivot at the center.
        /// Each quad copies <paramref name="sourceRects"/>[i] (in pixels, from the bottom-left corner of the source sprite rect)
        /// to <paramref name="destinationPositions"/>[i] (in pixels, from the bottom-left corner of the tile).
        /// </summary>
        public Sprite Build(int tileWidth, int tileHeight, float pixelsPerUnit,
            ReadOnlySpan<RectInt> sourceRects, ReadOnlySpan<Vector2Int> destinationPositions)
        {
            if (!IsValid) throw new InvalidOperationException("The source sprite is not available.");

            // The rect only determines the bounds of the sprite. The mesh below determines what is drawn.
            var rect = new Rect(0, 0, Mathf.Min(tileWidth, texture.width), Mathf.Min(tileHeight, texture.height));
            var sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect,
                Vector4.zero, false, secondaryTextures);

            var numQuads = sourceRects.Length;
            var positions = new NativeArray<Vector3>(numQuads * 4, Allocator.Temp);
            var uvs = new NativeArray<Vector2>(numQuads * 4, Allocator.Temp);
            var indices = new NativeArray<ushort>(numQuads * 6, Allocator.Temp);

            try
            {
                var halfWidth = tileWidth * 0.5f;
                var halfHeight = tileHeight * 0.5f;

                for (int q = 0; q < numQuads; q++)
                {
                    var src = sourceRects[q];
                    var dst = destinationPositions[q];

                    for (int c = 0; c < 4; c++)
                    {
                        var corner = new Vector2Int((c & 1) * src.width, (c >> 1) * src.height);
                        positions[q * 4 + c] = new Vector3(
                            (dst.x + corner.x - halfWidth) / pixelsPerUnit,
                            (dst.y + corner.y - halfHeight) / pixelsPerUnit,
                            0f);
                        uvs[q * 4 + c] = new Vector2(
                            (float)(sourceRect.x + src.x + corner.x) / texture.width,
                            (float)(sourceRect.y + src.y + corner.y) / texture.height);
                    }

                    var v = (ushort)(q * 4);
                    var o = q * 6;
                    indices[o + 0] = v;
                    indices[o + 1] = (ushort)(v + 2);
                    indices[o + 2] = (ushort)(v + 1);
                    indices[o + 3] = (ushort)(v + 1);
                    indices[o + 4] = (ushort)(v + 2);
                    indices[o + 5] = (ushort)(v + 3);
                }

                sprite.SetVertexCount(numQuads * 4);
                sprite.SetVertexAttribute(VertexAttribute.Position, positions);
                sprite.SetVertexAttribute(VertexAttribute.TexCoord0, uvs);
                sprite.SetIndices(indices);
            }
            finally
            {
                positions.Dispose();
                uvs.Dispose();
                indices.Dispose();
            }

            return sprite;
        }
    }
}
