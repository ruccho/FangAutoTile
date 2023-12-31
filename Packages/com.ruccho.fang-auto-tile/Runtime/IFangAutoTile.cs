using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ruccho.Fang
{
    public interface IFangAutoTile
    {
        FangAutoTile Original { get; }
        bool IsSlope { get; }
    }
}