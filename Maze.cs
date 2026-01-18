using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raycaster
{

    public static class Extensions
    {
        public static int[,] BuildRaycasterMap(Maze maze, int mazeWidth, int mazeHeight)
        {
            int mapWidth = mazeWidth * 2 + 1;
            int mapHeight = mazeHeight * 2 + 1;

            int[,] map = new int[mapHeight, mapWidth];

            // Fill everything with walls
            for (int y = 0; y < mapHeight; y++)
                for (int x = 0; x < mapWidth; x++)
                    map[y, x] = 1;

            // Carve passages
            for (int y = 0; y < mazeHeight; y++)
            {
                for (int x = 0; x < mazeWidth; x++)
                {
                    int mx = x * 2 + 1;
                    int my = y * 2 + 1;

                    // Cell interior
                    map[my, mx] = 0;

                    var cell = maze[x, y];

                    if (!cell.HasFlag(CellState.Right))
                        map[my, mx + 1] = 0;

                    if (!cell.HasFlag(CellState.Bottom))
                        map[my + 1, mx] = 0;
                }
            }

            return map;
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
        {
            var e = source.ToArray();
            for (var i = e.Length - 1; i >= 0; i--)
            {
                var swapIndex = rng.Next(i + 1);
                yield return e[swapIndex];
                e[swapIndex] = e[i];
            }
        }

        public static CellState OppositeWall(this CellState orig)
        {
            return (CellState)(((int)orig >> 2) | ((int)orig << 2)) & CellState.Initial;
        }

        public static bool HasFlag(this CellState cs, CellState flag)
        {
            return ((int)cs & (int)flag) != 0;
        }
    }

    [Flags]
    public enum CellState
    {
        Top = 1,
        Right = 2,
        Bottom = 4,
        Left = 8,
        Visited = 128,
        Initial = Top | Right | Bottom | Left,
    }

    public struct RemoveWallAction
    {
        public System.Drawing.Point Neighbour;
        public CellState Wall;
    }

    public class Maze
    {
        private readonly CellState[,] _cells;
        private readonly int _width;
        private readonly int _height;
        private readonly Random _rng;

        public Maze(int width, int height)
        {
            _width = width;
            _height = height;
            _cells = new CellState[width, height];
            for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                    _cells[x, y] = CellState.Initial;
            _rng = new Random();
            VisitCell(_rng.Next(width), _rng.Next(height));
        }

        public CellState this[int x, int y]
        {
            get { return _cells[x, y]; }
            set { _cells[x, y] = value; }
        }

        public IEnumerable<RemoveWallAction> GetNeighbours(System.Drawing.Point p)
        {
            if (p.X > 0) yield return new RemoveWallAction { Neighbour = new System.Drawing.Point(p.X - 1, p.Y), Wall = CellState.Left };
            if (p.Y > 0) yield return new RemoveWallAction { Neighbour = new System.Drawing.Point(p.X, p.Y - 1), Wall = CellState.Top };
            if (p.X < _width - 1) yield return new RemoveWallAction { Neighbour = new System.Drawing.Point(p.X + 1, p.Y), Wall = CellState.Right };
            if (p.Y < _height - 1) yield return new RemoveWallAction { Neighbour = new System.Drawing.Point(p.X, p.Y + 1), Wall = CellState.Bottom };
        }

        public void VisitCell(int x, int y)
        {
            this[x, y] |= CellState.Visited;
            foreach (var p in GetNeighbours(new System.Drawing.Point(x, y)).Shuffle(_rng).Where(z => !(this[z.Neighbour.X, z.Neighbour.Y].HasFlag(CellState.Visited))))
            {
                this[x, y] -= p.Wall;
                this[p.Neighbour.X, p.Neighbour.Y] -= p.Wall.OppositeWall();
                VisitCell(p.Neighbour.X, p.Neighbour.Y);
            }
        }

        public void Build()
        {
            var firstLine = string.Empty;
            for (var y = 0; y < _height; y++)
            {
                var sbTop = new StringBuilder();
                var sbMid = new StringBuilder();
                for (var x = 0; x < _width; x++)
                {
                    sbTop.Append(this[x, y].HasFlag(CellState.Top) ? "╬══" : "╬  ");
                    sbMid.Append(this[x, y].HasFlag(CellState.Left) ? "║  " : "   ");
                }
                if (firstLine == string.Empty)
                    firstLine = sbTop.ToString();
            }
        }
    }
}