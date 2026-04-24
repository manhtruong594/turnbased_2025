using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RedBjorn.ProtoTiles
{
    public class NodePathFinder
    {
        static Dictionary<INode, float> ScoreG = new Dictionary<INode, float>();
        static Dictionary<INode, float> ScoreF = new Dictionary<INode, float>();
        static Dictionary<INode, INode> CameFrom = new Dictionary<INode, INode>();

        public static HashSet<INode> AccessibleArea(IMapNode map, INode origin)
        {
            map.Reset();
            var open = new Queue<INode>();
            var closed = new HashSet<INode>();

            open.Enqueue(origin);
            var index = 0;
            while (open.Count > 0 && index < 100000)
            {
                var current = open.Dequeue();
                current.Considered = true;
                foreach (var n in map.NeighborsMovable(current).Where(neigh => neigh != null))
                {
                    if (n.Vacant && !n.Considered)
                    {
                        n.Considered = true;
                        open.Enqueue(n);
                        index++;
                    }
                }
                current.Visited = true;
                closed.Add(current);

            }
            return closed;
        }

        public static HashSet<INode> WalkableArea(IMapNode map, INode origin, float range)
        {
            map.Reset(Mathf.CeilToInt(range), origin);
            origin.Depth = 0f;
            var open = new Queue<INode>();
            var closed = new HashSet<INode>();

            open.Enqueue(origin);
            var index = 0;
            while (open.Count > 0 && index < 100000)
            {
                var current = open.Dequeue();
                current.Considered = true;
                foreach (var n in map.NeighborsMovable(current).Where(neigh => neigh != null))
                {
                    var currentDistance = current.Depth + map.Distance(current, n);
                    if (n.Vacant && !n.Considered && currentDistance <= range)
                    {
                        n.Considered = true;
                        n.Depth = currentDistance;
                        open.Enqueue(n);
                        index++;
                    }
                }
                current.Visited = true;
                closed.Add(current);

            }
            return closed;
        }

        public static HashSet<Vector3Int> WalkableAreaPositions(IMapNode map, INode origin, float range)
        {
            map.Reset(Mathf.CeilToInt(range), origin);
            origin.Depth = 0f;
            var open = new Queue<INode>();
            var closed = new HashSet<Vector3Int>();

            open.Enqueue(origin);
            var index = 0;
            while (open.Count > 0 && index < 100000)
            {
                var current = open.Dequeue();
                current.Considered = true;
                foreach (var n in map.NeighborsMovable(current).Where(neigh => neigh != null))
                {
                    var currentDistance = current.Depth + map.Distance(current, n);
                    if (n.Vacant && !n.Considered && currentDistance <= range)
                    {
                        n.Considered = true;
                        n.Depth = currentDistance;
                        open.Enqueue(n);
                        index++;
                    }
                }
                current.Visited = true;
                closed.Add(current.Position);

            }
            return closed;
        }

        public static List<INode> Path(IMapNode map, INode start, INode finish, float range)
        {
            if (start.MovableArea != finish.MovableArea)
            {
                return null;
            }
            var fullPath = FindPath(map, start, finish);
            return TrimPath(map, fullPath, range);
        }

        public static List<INode> Path(IMapNode map, INode start, INode finish)
        {
            if (start.MovableArea != finish.MovableArea)
            {
                return null;
            }
            return FindPath(map, start, finish);
        }

        static List<INode> FindPath(IMapNode map, INode start, INode finish)
        {
            ScoreG.Clear();
            ScoreF.Clear();
            CameFrom.Clear();

            var path = new List<INode>();
            if (!finish.Vacant)
            {
                return path;
            }
            var open = new List<INode>();
            var closed = new List<INode>();
            open.Add(start);
            ScoreF[start] = map.Distance(start, finish);
            ScoreG[start] = 0;

            while (open.Any())
            {
                var check = open
                    .OrderBy(o => ScoreF[o])
                    .ThenBy(o => DiagonalStepPenalty(o))
                    .ThenBy(o => DirectionChangePenalty(o))
                    .First();
                if (check == finish)
                {
                    break;
                }
                else if (closed.Contains(check))
                {
                    continue;
                }

                closed.Add(check);
                open.Remove(check);
                foreach (var node in map.NeighborsMovable(check).Where(n => n.Vacant))
                {
                    var currengScoreG = ScoreG[check] + map.Distance(check, node);
                    var gN = -1f;
                    if (ScoreG.TryGetValue(node, out gN))
                    {
                        if (currengScoreG < gN || (Mathf.Approximately(currengScoreG, gN) && BetterPathStyle(check, node)))
                        {
                            CameFrom[node] = check;
                            ScoreG[node] = currengScoreG;
                            ScoreF[node] = currengScoreG + map.Distance(node, finish);
                        }
                    }
                    else
                    {
                        open.Add(node);
                        ScoreG[node] = currengScoreG;
                        ScoreF[node] = currengScoreG + map.Distance(node, finish);
                        CameFrom[node] = check;
                    }
                }
            }
            var current = finish;
            while (CameFrom.ContainsKey(current))
            {
                path.Add(current);
                current = CameFrom[current];
            }
            path.Add(start);
            path.Reverse();

            return path;
        }

        static bool BetterPathStyle(INode candidateParent, INode node)
        {
            INode currentParent;
            if (!CameFrom.TryGetValue(node, out currentParent) || currentParent == null)
            {
                return true;
            }

            var candidateDiagonalPenalty = DiagonalStepPenalty(candidateParent, node);
            var currentDiagonalPenalty = DiagonalStepPenalty(currentParent, node);
            if (candidateDiagonalPenalty != currentDiagonalPenalty)
            {
                return candidateDiagonalPenalty < currentDiagonalPenalty;
            }

            var candidateTurnPenalty = DirectionChangePenalty(candidateParent, node);
            var currentTurnPenalty = DirectionChangePenalty(currentParent, node);
            return candidateTurnPenalty < currentTurnPenalty;
        }

        static int DirectionChangePenalty(INode node)
        {
            INode parent;
            if (!CameFrom.TryGetValue(node, out parent) || parent == null)
            {
                return 0;
            }

            return DirectionChangePenalty(parent, node);
        }

        static int DirectionChangePenalty(INode parent, INode node)
        {
            INode grandParent;
            if (!CameFrom.TryGetValue(parent, out grandParent) || grandParent == null)
            {
                return 0;
            }

            var previousDirection = parent.Position - grandParent.Position;
            var currentDirection = node.Position - parent.Position;
            return previousDirection == currentDirection ? 0 : 1;
        }

        static int DiagonalStepPenalty(INode node)
        {
            INode parent;
            if (!CameFrom.TryGetValue(node, out parent) || parent == null)
            {
                return 0;
            }

            return DiagonalStepPenalty(parent, node);
        }

        static int DiagonalStepPenalty(INode parent, INode node)
        {
            var delta = node.Position - parent.Position;
            return NonZeroAxisCount(delta) > 1 ? 1 : 0;
        }

        static int NonZeroAxisCount(Vector3Int value)
        {
            var count = 0;
            if (value.x != 0)
            {
                count++;
            }
            if (value.y != 0)
            {
                count++;
            }
            if (value.z != 0)
            {
                count++;
            }
            return count;
        }

        static List<INode> TrimPath(IMapNode map, List<INode> path, float range)
        {
            var distance = 0f;
            int trimIndex = -1;
            for (int i = 0; i < path.Count - 1; i++)
            {
                var step = distance + map.Distance(path[i], path[i + 1]);
                if (step <= range)
                {
                    distance = step;
                }
                else
                {
                    trimIndex = i + 1;
                    break;
                }
            }
            if (trimIndex >= 0)
            {
                path.RemoveRange(trimIndex, path.Count - trimIndex);
            }
            return path;
        }
    }
}