using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Ruccho.Fang
{
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Fang/Override Tile", fileName = "New Fang Override Tile", order = 1)]
#endif
    public class FangOverrideTile : TileBase, IFangAutoTile
    {
        [SerializeField] private FangAutoTile original;
        [SerializeField] private bool isSlope;
        [SerializeField] private Color editorTint = Color.white;
        
        public FangAutoTile Original => original;
        public bool IsSlope => isSlope;

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            original.RefreshTile(position, tilemap);
        }

        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {
            return original.StartUp(position, tilemap, go);
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            original.GetTileData(position, tilemap, ref tileData, isSlope, editorTint);
        }

        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
        {
            return original.GetTileAnimationData(position, tilemap, ref tileAnimationData);
        }
    }
}