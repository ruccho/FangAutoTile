using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ruccho.Fang
{
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Fang/Packer", fileName = "New Fang Auto Tile Packer", order = 1)]
#endif
    public class FangAutoTilePacker : ScriptableObject
    {
#pragma warning disable CS0414
        [SerializeField] private FangAutoTile[] targets = default;
        [SerializeField] private bool enablePadding = false;
        [SerializeField] private TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        [SerializeField] private FilterMode filterMode = FilterMode.Point;

        [SerializeField, HideInInspector] private Texture2D[] compiledChannels = default;
#pragma warning restore CS0414
    }
}