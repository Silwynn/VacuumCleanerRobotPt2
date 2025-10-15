using System;
using System.Threading;

namespace RobotCleaner
{
    // ===== Map Class =====
    public class Map
    {
        private enum CellType { Empty, Dirt, Obstacle, Cleaned };
        private CellType[,] _grid;
        public int Width { get; private set; }
        public int Height { get; private set; }

        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            _grid = new CellType[width, height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _grid[x, y] = CellType.Empty;
        }

        public bool IsInBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
        public bool IsDirt(int x, int y) => IsInBounds(x, y) && _grid[x, y] == CellType.Dirt;
        public bool IsObstacle(int x, int y) => IsInBounds(x, y) && _grid[x, y] == CellType.Obstacle;
        public void AddObstacle(int x, int y) => _grid[x, y] = CellType.Obstacle;
        public void AddDirt(int x, int y) => _grid[x, y] = CellType.Dirt;
        public void Clean(int x, int y) { if (IsInBounds(x, y)) _grid[x, y] = CellType.Cleaned; }

        public void Display(int robotX, int robotY)
        {
            Console.Clear();
            Console.WriteLine("Vacuum cleaner robot simulation");
            Console.WriteLine("--------------------------------");
            Console.WriteLine("Legends: #=Obstacles, D=Dirt, .=Empty, R=Robot, C=Cleaned");

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (x == robotX && y == robotY) Console.Write("R ");
                    else
                        switch (_grid[x, y])
                        {
                            case CellType.Empty: Console.Write(". "); break;
                            case CellType.Dirt: Console.Write("D "); break;
                            case CellType.Obstacle: Console.Write("# "); break;
                            case CellType.Cleaned: Console.Write("C "); break;
                        }
                }
                Console.WriteLine();
            }
            Thread.Sleep(100);
        }
    }

    // ===== Strategy Interface =====
    public interface IStrategy { void Clean(Robot robot); }

    // ===== Robot Class =====
    public class Robot
    {
        private readonly Map _map;
        private readonly IStrategy _strategy;
        public int X { get; set; }
        public int Y { get; set; }
        public Map Map => _map;

        public Robot(Map map, IStrategy strategy)
        {
            _map = map;
            _strategy = strategy;
            X = 0;
            Y = 0;
        }

        public bool Move(int newX, int newY)
        {
            if (_map.IsInBounds(newX, newY) && !_map.IsObstacle(newX, newY))
            {
                X = newX;
                Y = newY;
                _map.Display(X, Y);
                return true;
            }
            return false;
        }

        public void CleanCurrentSpot() { if (_map.IsDirt(X, Y) || !_map.IsObstacle(X, Y)) { _map.Clean(X, Y); _map.Display(X, Y); } }
        public void StartCleaning() => _strategy.Clean(this);
    }

    // ===== Strategy 1: Zig-Zag / Row by Row =====
    public class SomeStrategy : IStrategy
    {
        public void Clean(Robot robot)
        {
            int direction = 1;
            for (int y = 0; y < robot.Map.Height; y++)
            {
                int startX = (direction == 1) ? 0 : robot.Map.Width - 1;
                int endX = (direction == 1) ? robot.Map.Width : -1;

                for (int x = startX; x != endX; x += direction)
                {
                    robot.Move(x, y);
                    robot.CleanCurrentSpot();
                }
                direction *= -1;
            }
        }
    }

    // ===== Strategy 2: Perimeter Hugger =====
    public class PerimeterHuggerStrategy : IStrategy
    {
        public void Clean(Robot robot)
        {
            robot.Move(0, 0);
            robot.CleanCurrentSpot();

            while (robot.Move(robot.X + 1, robot.Y)) robot.CleanCurrentSpot(); // Right
            while (robot.Move(robot.X, robot.Y + 1)) robot.CleanCurrentSpot(); // Down
            while (robot.Move(robot.X - 1, robot.Y)) robot.CleanCurrentSpot(); // Left
            while (robot.Move(robot.X, robot.Y - 1)) robot.CleanCurrentSpot(); // Up
        }
    }

    // ===== Strategy 3: Spiral Cleaner (center out, complete) =====
    public class SpiralStrategy : IStrategy
    {
        public void Clean(Robot robot)
        {
            int centerX = robot.Map.Width / 2;
            int centerY = robot.Map.Height / 2;
            robot.Move(centerX, centerY);
            robot.CleanCurrentSpot();

            int[] dx = { 1, 0, -1, 0 }; // Right, Down, Left, Up
            int[] dy = { 0, 1, 0, -1 };
            int direction = 0, segmentLength = 1, stepsTaken = 0, turnCount = 0;

            int totalCells = robot.Map.Width * robot.Map.Height;
            bool[,] visited = new bool[robot.Map.Width, robot.Map.Height];

            // Mark obstacles as visited
            for (int x = 0; x < robot.Map.Width; x++)
                for (int y = 0; y < robot.Map.Height; y++)
                    if (robot.Map.IsObstacle(x, y)) visited[x, y] = true;

            visited[robot.X, robot.Y] = true;

            while (true)
            {
                bool moved = robot.Move(robot.X + dx[direction], robot.Y + dy[direction]);

                if (!moved)
                {
                    int tried = 0;
                    while (!moved && tried < 4)
                    {
                        direction = (direction + 1) % 4;
                        moved = robot.Move(robot.X + dx[direction], robot.Y + dy[direction]);
                        tried++;
                    }

                    if (!moved) break; // No moves possible
                }

                robot.CleanCurrentSpot();
                visited[robot.X, robot.Y] = true;

                stepsTaken++;
                if (stepsTaken == segmentLength)
                {
                    direction = (direction + 1) % 4;
                    stepsTaken = 0;
                    turnCount++;
                    if (turnCount % 2 == 0) segmentLength++;
                }

                // Check if all reachable cells are cleaned
                bool allCleaned = true;
                for (int x = 0; x < robot.Map.Width; x++)
                    for (int y = 0; y < robot.Map.Height; y++)
                        if (!visited[x, y] && !robot.Map.IsObstacle(x, y))
                            allCleaned = false;

                if (allCleaned) break;
            }
        }
    }

    // ===== Main Program with User Choice =====
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Initialize robot");
            Console.WriteLine("Choose cleaning strategy:");
            Console.WriteLine("1 = Zig-Zag (Row by Row)");
            Console.WriteLine("2 = Perimeter Hugger");
            Console.WriteLine("3 = Spiral Cleaner (Center Out, Complete)");
            Console.Write("Enter choice (1/2/3): ");

            string input = Console.ReadLine();
            IStrategy strategy;

            switch (input)
            {
                case "1":
                    strategy = new SomeStrategy();
                    break;
                case "2":
                    strategy = new PerimeterHuggerStrategy();
                    break;
                case "3":
                    strategy = new SpiralStrategy();
                    break;
                default:
                    Console.WriteLine("Invalid choice, defaulting to Zig-Zag.");
                    strategy = new SomeStrategy();
                    break;
            }

            // Create map
            Map map = new Map(20, 15);

            // Add dirt
            map.AddDirt(5, 3); map.AddDirt(10, 8); map.AddDirt(1, 1);
            map.AddDirt(15, 10); map.AddDirt(12, 5); map.AddDirt(18, 13);

            // Add obstacles
            map.AddObstacle(2, 5); map.AddObstacle(9, 1); map.AddObstacle(6, 7);
            map.AddObstacle(14, 4); map.AddObstacle(10, 12);

            // Create robot
            Robot robot = new Robot(map, strategy);
            robot.StartCleaning();

            Console.WriteLine("Done.");
        }
    }
}
