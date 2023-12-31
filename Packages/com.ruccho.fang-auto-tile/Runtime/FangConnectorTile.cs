using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Ruccho.Fang
{
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Fang/Connector Tile", fileName = "New Fang Connector Tile", order = 2)]
#endif
    public class FangConnectorTile : TileBase
    {
        [SerializeField] private Color editorTint = new Color(1f, 0f, 0f, 0.5f);
        [SerializeField] private Vector2 size = Vector2.one;
        private Sprite sprite;

        public override void RefreshTile(Vector3Int location, ITilemap tilemap)
        {
            var range = 1;
            for (int yd = -range; yd <= range; yd++)
            for (int xd = -range; xd <= range; xd++)
            {
                Vector3Int position = new Vector3Int(location.x + xd, location.y + yd, location.z);
                if (TileValue(tilemap, position))
                    tilemap.RefreshTile(position);
            }
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            if (!sprite)
            {
                var tex = Texture2D.whiteTexture;
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width,
                    0,
                    SpriteMeshType.FullRect);
            }

#if UNITY_EDITOR

            if (!Application.isPlaying)
            {
                tileData.color = editorTint;
            }
            else
#endif
            {
                tileData.color = Color.clear;
            }


            tileData.sprite = sprite;
            tileData.flags = TileFlags.LockAll;
            tileData.transform = Matrix4x4.Scale(new Vector3(size.x, size.y, 1f));
            tileData.colliderType = Tile.ColliderType.None;
        }

        private bool TileValue(ITilemap tileMap, Vector3Int position)
        {
            TileBase tile = tileMap.GetTile(position);
            return tile is IFangAutoTile or FangConnectorTile;
        }

        private void OnDisable()
        {
            DestroyImmediate(sprite);
        }
    }
}