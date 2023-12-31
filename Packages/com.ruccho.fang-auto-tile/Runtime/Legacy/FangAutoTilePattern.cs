using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ruccho.Utilities
{
    public class FangAutoTilePattern : ScriptableObject
    {
        public int[] Pattern = new int[4];
        public int Mask = 0x00000000;
        public int Combination;
        public Sprite[] Frames;
    }
}