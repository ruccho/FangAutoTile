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
    /// The sprites reference the texture of the source sprite before packing (or the textures of a
    /// <see cref="FangSourceSpritePacker"/>), and have the secondary textures of the source sprite.
    /// </summary>
    internal readonly struct FangMeshSpriteBuilder
    {
        private readonly Texture2D texture;
        private readonly SecondarySpriteTexture[] secondaryTextures;
        private readonly RectInt sourceRect;

        // set when the sprites reference the textures of a packer
        private readonly bool isPacked;
        private readonly FangSourceSpritePacker.Entry entry;

        public bool IsValid => texture != null;

        public FangMeshSpriteBuilder(Sprite source)
        {
            texture = null;
            secondaryTextures = Array.Empty<SecondarySpriteTexture>();
            sourceRect = default;
            isPacked = false;
            entry = default;

            if (!source) return;

            texture = GetSourceTexture(source);
            if (!texture) return;

            // Sprite.rect is the location on the texture before packing
            var rect = source.rect;
            sourceRect = new RectInt(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), Mathf.RoundToInt(rect.width),
                Mathf.RoundToInt(rect.height));

            secondaryTextures = GetSecondaryTextures(source);
        }

        /// <summary>
        /// Gets the secondary textures of the sprite before packing.
        /// </summary>
        public static SecondarySpriteTexture[] GetSecondaryTextures(Sprite source)
        {
            if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(source)) is TextureImporter importer)
            {
                return importer.secondarySpriteTextures?.Where(s => s.texture).ToArray() ??
                       Array.Empty<SecondarySpriteTexture>();
            }

            var count = source.GetSecondaryTextureCount();
            var result = new SecondarySpriteTexture[count];
            if (count > 0) source.GetSecondaryTextures(result);
            return result;
        }

        /// <summary>
        /// Gets the texture of the sprite before packing.
        /// </summary>
        public static Texture2D GetSourceTexture(Sprite source)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(source));
            return texture ? texture : source.texture;
        }

        /// <summary>
        /// Builds sprites that reference the location of the source sprite in the textures of a packer.
        /// </summary>
        public FangMeshSpriteBuilder(FangSourceSpritePacker packer, FangSourceSpritePacker.Entry entry)
        {
            var pages = packer.Pages;
            texture = pages.Length > 0 ? pages[0].Texture : null;
            sourceRect = new RectInt(Vector2Int.zero, entry.Size);
            isPacked = true;
            this.entry = entry;
            secondaryTextures = pages.Skip(1)
                .Select(p => new SecondarySpriteTexture { name = p.Name, texture = p.Texture }).ToArray();
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

                    // position of the rect in the texture
                    var origin = sourceRect.position + src.position;
                    if (isPacked && !entry.TryGetPosition(src, out origin))
                        throw new InvalidOperationException(
                            $"The rect {src} is not inside one tile of the packed source sprite.");

                    for (int c = 0; c < 4; c++)
                    {
                        var corner = new Vector2Int((c & 1) * src.width, (c >> 1) * src.height);
                        positions[q * 4 + c] = new Vector3(
                            (dst.x + corner.x - halfWidth) / pixelsPerUnit,
                            (dst.y + corner.y - halfHeight) / pixelsPerUnit,
                            0f);
                        uvs[q * 4 + c] = new Vector2(
                            (float)(origin.x + corner.x) / texture.width,
                            (float)(origin.y + corner.y) / texture.height);
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
