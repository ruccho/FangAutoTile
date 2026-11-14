using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ruccho.Fang
{
    internal static class FangPhysicsShapeUtility
    {
        // Clip lines are moved outward by this distance (in pixels) so that no vertex lies exactly on them.
        private const float ClipMargin = 1e-3f;

        // Pieces smaller than this area (in square pixels) are discarded.
        private const float MinArea = 1e-2f;

        /// <summary>
        /// Gets the physics shape of the sprite in pixels from the bottom-left corner of the sprite rect.
        /// </summary>
        public static List<Vector2[]> GetPhysicsShapeInPixels(Sprite sprite)
        {
            var result = new List<Vector2[]>();
            var list = new List<Vector2>();
            var ppu = sprite.pixelsPerUnit;
            var pivot = sprite.pivot;
            for (int i = 0; i < sprite.GetPhysicsShapeCount(); i++)
            {
                sprite.GetPhysicsShape(i, list);
                var points = new Vector2[list.Count];
                for (int j = 0; j < points.Length; j++) points[j] = list[j] * ppu + pivot;
                result.Add(points);
            }

            return result;
        }

        /// <summary>
        /// Builds the physics shape of a tile from the physics shape of the source.
        /// Each part of the source shape inside <paramref name="sourceRects"/>[i] is moved to
        /// <paramref name="destinationPositions"/>[i] in the same way as the pixels of the tile.
        /// </summary>
        /// <param name="sourceShape">Paths in pixels from the bottom-left corner of the source.</param>
        /// <param name="sourceRects">Rects in pixels from the bottom-left corner of the source.</param>
        /// <param name="destinationPositions">Positions in pixels from the bottom-left corner of the tile.</param>
        /// <returns>Paths in the local space of a sprite whose pivot is at the center of the tile.</returns>
        public static List<Vector2[]> Compose(IReadOnlyList<Vector2[]> sourceShape, IReadOnlyList<RectInt> sourceRects,
            IReadOnlyList<Vector2Int> destinationPositions, int tileWidth, int tileHeight, float pixelsPerUnit)
        {
            var result = new List<Vector2[]>();
            var pieces = new List<List<Vector2>>();
            var center = new Vector2(tileWidth * 0.5f, tileHeight * 0.5f);

            for (int q = 0; q < sourceRects.Count; q++)
            {
                var src = sourceRects[q];
                var offset = (Vector2)(destinationPositions[q] - src.position) - center;

                foreach (var path in sourceShape)
                {
                    pieces.Clear();
                    ClipToRect(path, new Rect(src.x, src.y, src.width, src.height), pieces);
                    foreach (var piece in pieces)
                    {
                        var points = new Vector2[piece.Count];
                        for (int i = 0; i < points.Length; i++) points[i] = (piece[i] + offset) / pixelsPerUnit;
                        result.Add(points);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Clips a simple polygon with an axis-aligned rect. The result can consist of multiple polygons.
        /// </summary>
        public static void ClipToRect(IReadOnlyList<Vector2> polygon, Rect rect, List<List<Vector2>> result)
        {
            var current = new List<List<Vector2>> { new(polygon) };
            current = ClipWithHalfPlane(current, 0, rect.xMin - ClipMargin, true);
            current = ClipWithHalfPlane(current, 0, rect.xMax + ClipMargin, false);
            current = ClipWithHalfPlane(current, 1, rect.yMin - ClipMargin, true);
            current = ClipWithHalfPlane(current, 1, rect.yMax + ClipMargin, false);

            foreach (var piece in current)
            {
                for (int i = 0; i < piece.Count; i++)
                {
                    var p = piece[i];
                    p.x = Mathf.Clamp(p.x, rect.xMin, rect.xMax);
                    p.y = Mathf.Clamp(p.y, rect.yMin, rect.yMax);
                    piece[i] = p;
                }

                RemoveRedundantPoints(piece);
                if (piece.Count >= 3 && Mathf.Abs(SignedArea(piece)) >= MinArea) result.Add(piece);
            }
        }

        private readonly struct Node
        {
            public readonly Vector2 Position;
            public readonly bool IsCrossing;
            public readonly bool IsExit;

            public Node(Vector2 position, bool isCrossing, bool isExit)
            {
                Position = position;
                IsCrossing = isCrossing;
                IsExit = isExit;
            }
        }

        /// <summary>
        /// Clips simple polygons with the half plane where the coordinate on <paramref name="axis"/> is greater (or less)
        /// than <paramref name="value"/>. A concave polygon is split into separate polygons instead of being connected
        /// with edges on the clip line.
        /// </summary>
        private static List<List<Vector2>> ClipWithHalfPlane(List<List<Vector2>> polygons, int axis, float value,
            bool keepGreater)
        {
            var output = new List<List<Vector2>>();
            var nodes = new List<Node>();
            var crossings = new List<int>();
            int other = 1 - axis;

            foreach (var polygon in polygons)
            {
                int n = polygon.Count;
                if (n < 3) continue;

                float Distance(Vector2 p) => keepGreater ? p[axis] - value : value - p[axis];

                nodes.Clear();
                crossings.Clear();
                int numInside = 0;

                for (int i = 0; i < n; i++)
                {
                    var a = polygon[i];
                    var b = polygon[(i + 1) % n];
                    var da = Distance(a);
                    var db = Distance(b);
                    bool aInside = da >= 0f;
                    bool bInside = db >= 0f;

                    if (aInside)
                    {
                        nodes.Add(new Node(a, false, false));
                        numInside++;
                    }

                    if (aInside != bInside)
                    {
                        var p = a + (b - a) * (da / (da - db));
                        p[axis] = value;
                        crossings.Add(nodes.Count);
                        nodes.Add(new Node(p, true, aInside));
                    }
                }

                if (crossings.Count == 0)
                {
                    if (numInside == n) output.Add(new List<Vector2>(polygon));
                    continue;
                }

                // Along the clip line, the inside of a simple polygon lies between the 2k-th and (2k+1)-th crossings.
                crossings.Sort((x, y) => nodes[x].Position[other].CompareTo(nodes[y].Position[other]));
                var partner = new int[nodes.Count];
                bool paired = crossings.Count % 2 == 0;
                for (int k = 0; paired && k + 1 < crossings.Count; k += 2)
                {
                    int x = crossings[k], y = crossings[k + 1];
                    if (nodes[x].IsExit == nodes[y].IsExit) paired = false;
                    partner[x] = y;
                    partner[y] = x;
                }

                if (!paired)
                {
                    // not a simple polygon; keep the inside vertices as one polygon
                    var fallback = new List<Vector2>(nodes.Count);
                    foreach (var node in nodes) fallback.Add(node.Position);
                    output.Add(fallback);
                    continue;
                }

                var visited = new bool[nodes.Count];
                for (int start = 0; start < nodes.Count; start++)
                {
                    if (visited[start]) continue;
                    if (nodes[start].IsCrossing && !nodes[start].IsExit) continue;

                    var piece = new List<Vector2>();
                    int index = start;
                    for (int guard = 0; guard <= nodes.Count; guard++)
                    {
                        visited[index] = true;
                        piece.Add(nodes[index].Position);

                        if (nodes[index].IsCrossing && nodes[index].IsExit)
                        {
                            index = partner[index];
                            visited[index] = true;
                            piece.Add(nodes[index].Position);
                        }

                        index = (index + 1) % nodes.Count;
                        if (index == start || visited[index]) break;
                    }

                    output.Add(piece);
                }
            }

            return output;
        }

        private static void RemoveRedundantPoints(List<Vector2> polygon)
        {
            bool removed = true;
            while (removed && polygon.Count >= 3)
            {
                removed = false;
                for (int i = 0; i < polygon.Count && polygon.Count >= 3; i++)
                {
                    var prev = polygon[(i + polygon.Count - 1) % polygon.Count];
                    var cur = polygon[i];
                    var next = polygon[(i + 1) % polygon.Count];
                    var d0 = cur - prev;
                    var d1 = next - cur;
                    // duplicated points, and points on a straight line or at the tip of a spike
                    if (d0.sqrMagnitude < 1e-8f || Mathf.Abs(d0.x * d1.y - d0.y * d1.x) < 1e-5f)
                    {
                        polygon.RemoveAt(i);
                        removed = true;
                        i--;
                    }
                }
            }
        }

        private static float SignedArea(List<Vector2> polygon)
        {
            float area = 0f;
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }

        /// <summary>
        /// Overwrites the physics shape of a sprite asset.
        /// Sprite.OverridePhysicsShape() is not allowed on sprites created with Sprite.Create().
        /// </summary>
        public static void SetPhysicsShape(Sprite sprite, IReadOnlyList<Vector2[]> shape)
        {
            var so = new SerializedObject(sprite);
            var pathsProp = so.FindProperty("m_PhysicsShape");
            pathsProp.arraySize = shape.Count;
            for (int i = 0; i < shape.Count; i++)
            {
                var pointsProp = pathsProp.GetArrayElementAtIndex(i);
                var points = shape[i];
                pointsProp.arraySize = points.Length;
                for (int j = 0; j < points.Length; j++) pointsProp.GetArrayElementAtIndex(j).vector2Value = points[j];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
