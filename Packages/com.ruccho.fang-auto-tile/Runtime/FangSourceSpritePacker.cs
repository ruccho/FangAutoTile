using System;
using UnityEngine;

namespace Ruccho.Fang
{
    /// <summary>
    /// Packs the source sprites of tiles with <see cref="FangAutoTile.SpriteSourceType.SourceSpriteMesh"/> into shared textures,
    /// so that tilemaps that contain multiple tiles are drawn with one texture.
    /// The sprites of the tiles reference the packed textures instead of the source sprites.
    /// Each tile of the source sprites is placed separately, and tiles with the same pixels are placed once.
    /// </summary>
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Fang/Source Sprite Packer", fileName = "New Fang Source Sprite Packer", order = 4)]
#endif
    public class FangSourceSpritePacker : ScriptableObject
    {
        [Serializable]
        public struct Page
        {
            [SerializeField] private string name;
            [SerializeField] private Texture2D texture;

            /// <summary>
            /// Empty for the main texture, and the name of the secondary texture otherwise.
            /// </summary>
            public string Name => name;

            public Texture2D Texture => texture;

            public Page(string name, Texture2D texture)
            {
                this.name = name;
                this.texture = texture;
            }
        }

        /// <summary>
        /// Locations of the tiles of a source sprite in the packed textures.
        /// Each tile of the source sprite is placed separately, and tiles with the same pixels share a location.
        /// </summary>
        [Serializable]
        public struct Entry
        {
            [SerializeField] private Sprite source;
            [SerializeField] private Vector2Int size;
            [SerializeField] private int tileSize;
            [SerializeField] private Vector2Int[] tilePositions;

            public Sprite Source => source;

            /// <summary>
            /// Size of the source sprite in pixels.
            /// </summary>
            public Vector2Int Size => size;

            public int TileSize => tileSize;

            public Entry(Sprite source, Vector2Int size, int tileSize, Vector2Int[] tilePositions)
            {
                this.source = source;
                this.size = size;
                this.tileSize = tileSize;
                this.tilePositions = tilePositions;
            }

            /// <summary>
            /// Gets the position in the packed textures of a rect in the source sprite.
            /// The rect has to be inside one tile.
            /// </summary>
            /// <param name="sourceRect">Rect in pixels from the bottom-left corner of the source sprite.</param>
            /// <param name="position">Position in pixels from the bottom-left corner of the packed textures.</param>
            public bool TryGetPosition(RectInt sourceRect, out Vector2Int position)
            {
                position = default;
                if (tileSize <= 0 || tilePositions == null) return false;

                int columns = size.x / tileSize;
                var tile = new Vector2Int(sourceRect.x / tileSize, sourceRect.y / tileSize);
                var local = sourceRect.position - tile * tileSize;
                if (local.x + sourceRect.width > tileSize || local.y + sourceRect.height > tileSize) return false;

                int index = tile.y * columns + tile.x;
                if (index < 0 || index >= tilePositions.Length) return false;

                position = tilePositions[index] + local;
                return true;
            }
        }

#pragma warning disable CS0414
        [SerializeField] private FangAutoTile[] targets = default;

        [SerializeField, Min(0), Tooltip("Pixels extruded around each source sprite.")]
        private int padding = 0;

        [SerializeField] private TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        [SerializeField] private FilterMode filterMode = FilterMode.Point;
#pragma warning restore CS0414

        [SerializeField, HideInInspector] private Page[] pages = default;
        [SerializeField, HideInInspector] private Entry[] entries = default;
        [SerializeField, HideInInspector] private FangAutoTile[] packedTiles = default;
        [SerializeField, HideInInspector] private int numPackedTiles = 0;

        /// <summary>
        /// Number of tile images placed in the packed textures. Tiles with the same pixels are counted once.
        /// </summary>
        public int NumPackedTiles => numPackedTiles;

        public FangAutoTile[] Targets => targets;

        /// <summary>
        /// The main texture first, followed by the secondary textures.
        /// </summary>
        public Page[] Pages => pages ?? Array.Empty<Page>();

        public Entry[] Entries => entries ?? Array.Empty<Entry>();

        public bool TryGetEntry(Sprite source, out Entry entry)
        {
            foreach (var e in Entries)
            {
                if (e.Source != source) continue;
                entry = e;
                return true;
            }

            entry = default;
            return false;
        }
    }
}
