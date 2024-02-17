using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace Ruccho.Fang
{
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Fang/Tile", fileName = "New Fang Auto Tile", order = 0)]
#endif
    public class FangAutoTile : TileBase, IFangAutoTile
    {
        public enum FrameModeType
        {
            Animation,
            Random
        }

        #region Parameters

#pragma warning disable CS0414
        [SerializeField, Header("Layout"), Tooltip("Extrude pixels on contours to avoid weird borders on tiles.")]
        private bool enablePadding = true;

        [SerializeField, Header("Sprite / Texture"), Tooltip("Tile will be justified into one unit.")]
        private bool oneTilePerUnit = true;

        [SerializeField] private int pixelsPerUnit = 16;
        [SerializeField] private PhysicsShapeGenerationMode physicsShapeGeneration = PhysicsShapeGenerationMode.Sprite;
        [SerializeField] private TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        [SerializeField] private FilterMode filterMode = FilterMode.Point;

        [SerializeField, Header("Sources")] private Texture2D mainChannel = default;
        [SerializeField, Min(0)] private int numSlopes = 0;
        [SerializeField] private Texture2D[] subChannels = default;
#pragma warning restore CS0414

        [SerializeField] private FrameModeType frameMode = FrameModeType.Animation;

        [SerializeField, Header("Animation")] public float animationMinSpeed = 1f;
        [SerializeField] public float animationMaxSpeed = 1f;
        [SerializeField] public float animationStartTime;

        [SerializeField, Header("Collision")] private Tile.ColliderType colliderType = Tile.ColliderType.None;

        [SerializeField, Header("Auto Tiling"), Tooltip("These tiles recognized to be connected with this tile.")]
        private bool isSlope = default;

        [SerializeField] private Color editorTint = Color.white;

        [SerializeField] private TileBase[] connectableTiles = default;

        #endregion

        #region Generated

#pragma warning disable CS0414
        [SerializeField] private Texture2D[] compiledChannels = default;
        [SerializeField] private FangAutoTilePacker packer = default;
#pragma warning restore CS0414

        [SerializeField] private TileCombination[] combinations = default;

        [SerializeField] private int[] combinationTable = new int[256];

        [SerializeField] private SlopeDefinition slopeDefinition = default;

        #endregion

        public FangAutoTile Original => this;
        public bool IsSlope => isSlope;

        private Sprite[] currentAnimationFrames = default;
        
        #region Internal Members

        public IEnumerable<Sprite> GetAllSprites()
        {
            var spritesFromCombinations = combinations?.SelectMany(c => c.Frames) ?? Enumerable.Empty<Sprite>();
            var spritesFromSlopes = slopeDefinition?.Sizes?.SelectMany(s => s.GetAngles())
                .SelectMany(a => a.GetTileDefinitions()).SelectMany(tileDefs => tileDefs)
                .SelectMany(tileDef => tileDef.Frames) ?? Enumerable.Empty<Sprite>();

            return spritesFromCombinations.Concat(spritesFromSlopes);
        }

        #endregion

        public override void RefreshTile(Vector3Int location, ITilemap tileMap)
        {
            var range = Mathf.Max(1, slopeDefinition.Sizes.Length * 2);
            for (int yd = -range; yd <= range; yd++)
            for (int xd = -range; xd <= range; xd++)
            {
                if ((xd < -1 || xd > 1) && (yd < -1 || yd > 1)) continue;
                Vector3Int position = new Vector3Int(location.x + xd, location.y + yd, location.z);
                if (TileValue(tileMap, position))
                    tileMap.RefreshTile(position);
            }
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            GetTileData(position, tilemap, ref tileData, isSlope, editorTint);
        }

        public void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData, bool isSlope,
            Color editorTint)
        {
            tileData.transform = Matrix4x4.identity;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                tileData.color = editorTint;
            }
            else
#endif
            {
                tileData.color = Color.white;
            }

            byte neighborValue = GetNeighborValue(position, tilemap);

            bool value = TileValue(tilemap, position);

            int combinationIndex = combinationTable[neighborValue];

            if (combinations == null || combinations.Length <= combinationIndex) return;

            var combination = combinations[combinationIndex];
            if (combination == null) return;

            currentAnimationFrames = combination.Frames;

            if (value)
            {
                tileData.sprite = combination.Frames[(int)(combination.Frames.Length * GetRandom(position))];
                tileData.flags |= (TileFlags.LockTransform | TileFlags.LockColor);
                tileData.colliderType = colliderType;

                if (isSlope)
                {
                    byte maxSlopeSize = (byte)slopeDefinition.Sizes.Length;

                    byte tileIndexInSlope = 0;
                    byte slopeSize = 0;
                    byte edgeDir = 0xFF;

                    bool isSingleEdge = IsSingleEdge(neighborValue, out var singleEdgeDir);
                    if (IsSingleConvexCorner(neighborValue, out var sccOppositeDir))
                    {
                        tileIndexInSlope = 0;
                        GetSlopeSizeForScc(position, sccOppositeDir, out slopeSize, out edgeDir);
                    }
                    else if (isSingleEdge)
                    {
                        var dirA = (byte)((singleEdgeDir + 6) & 7);
                        var dirB = (byte)((singleEdgeDir + 2) & 7);

                        ProcessDirection(position, dirA, singleEdgeDir, maxSlopeSize,
                            out var edgeSizeA, out var endWithSccA, out var endSccOppDirA);
                        ProcessDirection(position, dirB, singleEdgeDir, maxSlopeSize,
                            out var edgeSizeB, out var endWithSccB, out var endSccOppDirB);

                        bool matched = false;
                        if (endWithSccA)
                        {
                            var distance = (byte)(edgeSizeA - 1);
                            GetSlopeSizeForScc(position + (Vector3Int)ToVector(dirA) * distance, endSccOppDirA,
                                out slopeSize, out edgeDir);
                            if (edgeDir == singleEdgeDir && distance < slopeSize)
                            {
                                matched = true;
                                tileIndexInSlope = distance;
                                sccOppositeDir = endSccOppDirA;
                            }
                        }

                        if (!matched && endWithSccB)
                        {
                            var distance = (byte)(edgeSizeB - 1);
                            GetSlopeSizeForScc(position + (Vector3Int)ToVector(dirB) * distance, endSccOppDirB,
                                out slopeSize, out edgeDir);
                            if (edgeDir == singleEdgeDir && distance < slopeSize)
                            {
                                matched = true;
                                tileIndexInSlope = distance;
                                sccOppositeDir = endSccOppDirB;
                            }
                        }

                        if (!matched)
                        {
                            tileIndexInSlope = 0;
                            slopeSize = 0;
                        }
                    }

                    if (slopeSize > 0)
                    {
                        var slopeDef = slopeDefinition.Sizes[slopeSize - 1];
                        var angleDef = slopeDef.GetAngle(sccOppositeDir);
                        var forDirection = edgeDir switch
                        {
                            1 or 5 => angleDef.HorizontalTiles,
                            3 or 7 => angleDef.VerticalTiles,
                            _ => throw new InvalidOperationException()
                        };

                        if (slopeSize == 1) forDirection = angleDef.HorizontalTiles;

                        currentAnimationFrames = forDirection[tileIndexInSlope].Frames;
                        tileData.sprite =
                            currentAnimationFrames[(int)(currentAnimationFrames.Length * GetRandom(position))];
                    }

                    void GetSlopeSizeForScc(Vector3Int position, byte sccOppositeDir, out byte slopeSize,
                        out byte edgeDir)
                    {
                        byte maxDepth = (byte)(maxSlopeSize * 2 + 1);

                        var dirA = (byte)((sccOppositeDir + 7) & 7);
                        var dirB = (byte)((sccOppositeDir + 1) & 7);

                        var edgeDirA = (byte)((sccOppositeDir + 5) & 7);
                        var edgeDirB = (byte)((sccOppositeDir + 3) & 7);

                        ProcessDirection(position, dirA, edgeDirA, maxDepth,
                            out var edgeSizeA, out var endWithSccA, out _);
                        ProcessDirection(position, dirB, edgeDirB, maxDepth,
                            out var edgeSizeB, out var endWithSccB, out _);

                        var anchorA = (byte)(endWithSccA ? edgeSizeA - 2 : edgeSizeA);
                        var anchorB = (byte)(endWithSccB ? edgeSizeB - 2 : edgeSizeB);

                        byte edgeSize;
                        var endWithScc = false;
                        if (anchorA == 0 || anchorB == 0)
                        {
                            edgeSize = 0;
                            edgeDir = 0xFF;
                        }
                        else if (anchorA > 1 && anchorB > 1 || anchorA < anchorB)
                        {
                            edgeSize = edgeSizeB;
                            endWithScc = endWithSccB;
                            edgeDir = edgeDirB;
                        }
                        else if (anchorA > anchorB)
                        {
                            edgeSize = edgeSizeA;
                            endWithScc = endWithSccA;
                            edgeDir = edgeDirA;
                        }
                        else
                        {
                            // both 1
                            edgeSize = 1;
                            edgeDir = edgeDirA;
                        }

                        tileIndexInSlope = 0;

                        if (endWithScc)
                        {
                            slopeSize = (byte)Mathf.FloorToInt((edgeSize - 1) * 0.5f);
                        }
                        else
                        {
                            slopeSize = edgeSize;
                        }

                        slopeSize = (byte)Mathf.Min(maxSlopeSize, slopeSize);
                    }

                    void ProcessDirection(Vector3Int position, byte direction, byte edgeDir, byte maxDepth,
                        out byte edgeSize,
                        out bool endWithScc, out byte endSccOppositeDirection)
                    {
                        endSccOppositeDirection = 0xFF;
                        var vec = ToVector(direction);
                        edgeSize = 1;
                        endWithScc = false;
                        for (; edgeSize < maxDepth; edgeSize++)
                        {
                            var pos = position + (Vector3Int)vec * edgeSize;
                            if (!TileValue(tilemap, pos, out var isSlope) || !isSlope) break;
                            var v = GetNeighborValue(pos, tilemap);
                            if (IsSingleConvexCorner(v, out endSccOppositeDirection))
                            {
                                endWithScc = true;
                                edgeSize++;
                                break;
                            }

                            if (!IsSingleEdge(v, out var edge) || edge != edgeDir) break;
                        }
                    }
                }
            }
        }

        private float GetRandom(Vector3Int location)
        {
            if (frameMode != FrameModeType.Random) return 0;
            long hash = location.x;
            hash = (hash + 0xabcd1234) + (hash << 15);
            hash = (hash + 0x0987efab) ^ (hash >> 11);
            hash ^= location.y;
            hash = (hash + 0x46ac12fd) + (hash << 7);
            hash = (hash + 0xbe9730af) ^ (hash << 11);
            Random.InitState((int)hash);
            return Random.value;
        }

        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap,
            ref TileAnimationData tileAnimationData)
        {
            if (frameMode != FrameModeType.Animation) return false;
            if (currentAnimationFrames == null) return false;
            if (currentAnimationFrames.Length == 1)
            {
                return false;
            }
            else
            {
                tileAnimationData.animatedSprites = currentAnimationFrames;
                tileAnimationData.animationSpeed = UnityEngine.Random.Range(animationMinSpeed, animationMaxSpeed);
                tileAnimationData.animationStartTime = animationStartTime;
                return true;
            }
        }

        private bool TileValue(ITilemap tileMap, Vector3Int position)
        {
            return TileValue(tileMap, position, out _);
        }

        private bool TileValue(ITilemap tileMap, Vector3Int position, out bool isSlope)
        {
            TileBase tile = tileMap.GetTile(position);
            isSlope = false;
            if (tile == null) return false;

            if (tile is IFangAutoTile typed && typed.Original == this)
            {
                isSlope = typed.IsSlope;
                return true;
            }

            if (tile is FangConnectorTile) return true;

            return connectableTiles != null && connectableTiles.Contains(tile);
        }

        private static Vector2Int ToVector(byte direction)
        {
            // 6 5 4
            // 7 . 3
            // 0 1 2 
            int x = Mathf.Clamp(Mathf.Abs(((direction + 5) % 8) - 4) - 2, -1, 1);
            int y = Mathf.Clamp(Mathf.Abs(((direction + 3) % 8) - 4) - 2, -1, 1);
            Vector2Int d = new Vector2Int(x, y);

            return d;
        }

        private byte GetNeighborValue(Vector3Int position, ITilemap tilemap)
        {
            byte neighborValue = 0;
            for (byte i = 0; i < 8; i++)
            {
                var d2 = ToVector(i);
                var d = new Vector3Int(d2.x, d2.y);
                neighborValue |= (byte)(TileValue(tilemap, position + d) ? 1 << i : 0);
            }

            return neighborValue;
        }

        private static bool IsSingleConvexCorner(byte neighborValue, out byte cornerOppositeDirection)
        {
            // 0 1 0
            // 0 . 1
            // 0 0 0
            byte excludePattern = 0b_00101000;

            // 0 0 0
            // 1 . 0
            // 1 1 0
            byte includePattern = 0b_10000011;

            return MatchPatternRotated(neighborValue, excludePattern, includePattern, out cornerOppositeDirection);
        }

        private static bool IsSingleEdge(byte neighborValue, out byte edgeDirection)
        {
            // 0 0 0
            // 0 . 0
            // 0 1 0
            byte excludePattern = 0b_00000010;

            // 1 1 1
            // 1 . 1
            // 0 0 0
            byte includePattern = 0b_11111000;

            if (MatchPatternRotated(neighborValue, excludePattern, includePattern, out var matched))
            {
                edgeDirection = (byte)((matched + 1) & 7);
                return true;
            }

            edgeDirection = matched;
            return false;
        }

        private static bool MatchPatternRotated(byte neighborValue, byte excludePattern, byte includePattern,
            out byte matchedDirection)
        {
            for (byte dir = 0; dir < 8; dir += 2)
            {
                byte rotated = neighborValue;
                // bitwise rotation (<dir> bits left), geometrically anti-clockwise
                rotated = (byte)(((rotated << (8 - dir)) & 0xFF) | (rotated >> dir));

                if ((byte)(rotated & excludePattern) != 0) continue;
                if ((byte)(~(rotated | ~includePattern)) != 0) continue;

                matchedDirection = dir;
                return true;
            }

            matchedDirection = 0xFF;
            return false;
        }
    }

    [Serializable]
    public class TileCombination
    {
        [SerializeField] private uint combinationId = 0;
        [SerializeField] private Sprite[] frames = default;

        public uint CombinationId => combinationId;
        public Sprite[] Frames => frames;
    }


    [Serializable]
    public class SlopeDefinition
    {
        [SerializeField] private SlopeTileSizeDefinition[] sizes;

        public SlopeTileSizeDefinition[] Sizes => sizes;
    }


    [Serializable]
    public class SlopeTileSizeDefinition
    {
        [SerializeField] private SlopeTileAngleDefinition floorUp = default;
        [SerializeField] private SlopeTileAngleDefinition floorDown = default;
        [SerializeField] private SlopeTileAngleDefinition ceilUp = default;
        [SerializeField] private SlopeTileAngleDefinition ceilDown = default;

        public SlopeTileAngleDefinition GetAngle(byte cornerOppositeDir)
        {
            return cornerOppositeDir switch
            {
                0 => floorDown,
                2 => floorUp,
                4 => ceilDown,
                6 => ceilUp,
                _ => null
            };
        }

        public IEnumerable<SlopeTileAngleDefinition> GetAngles()
        {
            yield return floorUp;
            yield return floorDown;
            yield return ceilUp;
            yield return ceilDown;
        }
    }

    [Serializable]
    public class SlopeTileAngleDefinition
    {
        [SerializeField] private SlopeTileDefinition[] horizontalTiles = default;
        [SerializeField] private SlopeTileDefinition[] verticalTiles = default;

        public SlopeTileDefinition[] HorizontalTiles => horizontalTiles;
        public SlopeTileDefinition[] VerticalTiles => verticalTiles;

        public IEnumerable<SlopeTileDefinition[]> GetTileDefinitions()
        {
            yield return horizontalTiles;
            yield return verticalTiles;
        }
    }


    [Serializable]
    public class SlopeTileDefinition
    {
        [SerializeField] private Sprite[] frames = default;
        public Sprite[] Frames => frames;
    }

    public enum PhysicsShapeGenerationMode
    {
        Sprite,
        Fine
    }
}