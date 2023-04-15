namespace AI_ShortestPath;

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Drawing;

class Program
{
     static void Main(string[] args)
    {
        string[] problemFiles = Directory.GetFiles(args[0]);
        string[] SolutionFiles = Directory.GetFiles(args[1]);

        foreach (var filenames in problemFiles.Zip(SolutionFiles, (p, s) => new { PathProblem = p, PathSolution = s }))
        {
            Console.WriteLine(filenames.PathProblem);
            Problem problem = new Problem(filenames.PathProblem, filenames.PathSolution);
            problem.Search("A*");
        }
    }
}

public class State: IComparable<State>
{
    public (int, int) Coord = (0, 0);
    public decimal Heu = 0;
    public State? Parent = null;

    public State((int, int) coord, decimal heu, State? parent)
    {
        Coord = coord;
        Heu = heu;
        Parent = parent;
    }

    public int CompareTo(State? other)
    {
        if (other == null)
        {
            return 1;
        }
        return this.Heu.CompareTo(other.Heu);
    }
}

public class Problem
{
    // Define properties
    private (int, int) _mapSize = (0, 0);
    private (int, int) _posAgent = (0, 1);
    public (int, int) PosAgent
    {
        get => _posAgent;
        set => _posAgent = value;
    }

    private readonly List<(int, int)> _goals = new List<(int, int)>();
    private readonly List<(int, int)> _walls = new List<(int, int)>();

    private readonly Dictionary<string, string> _symbols = new Dictionary<string, string>()
    {
        {"wall", " #"},
        {"agent", " o"},
        {"goal", " x"},
        {"area", " ."},
        {"visited", " -" }
    };
    
    Queue<State> queue = new Queue<State>();
    List<State> visitedStates = new List<State>();
    List<State> path = new List<State>();


    // Constructor
    public Problem(string pathProblem, string pathSolution)
    {
        // Problem configuration
        try
        {

            // StreamReader sr = new StreamReader(Path.Combine(dirProject, "problems\\RobotNav-test.txt"));
            StreamReader sr = new StreamReader(pathProblem);
            // Regex matches digits
            Regex rx = new Regex(@"\d+", RegexOptions.Compiled);

            // Get Map shape
            String? line;
            line = sr.ReadLine();
            string[] subs = line.Split(',');
            MatchCollection matches = rx.Matches(line);
            _mapSize = (Int32.Parse(matches[0].Value), Int32.Parse(matches[1].Value));

            // Get coordinates of the agent
            line = sr.ReadLine();
            subs = line.Split(',');
            matches = rx.Matches(line);
            _posAgent = (Int32.Parse(matches[0].Value), Int32.Parse(matches[1].Value));
            
            // Get coordinates of goals
            line = sr.ReadLine();
            string[] strGoals = line.Split('|');
            foreach ( string strGoal in strGoals )
            {
                matches = rx.Matches(strGoal);
                _goals.Add((Int32.Parse(matches[0].Value), Int32.Parse(matches[1].Value)));
            }

            // Get walls
            line = sr.ReadLine();
            while (line != null)
            {
                subs = line.Split(',');
                matches = rx.Matches(line);

                int wallX = Int32.Parse(matches[0].Value);
                int wallY = Int32.Parse(matches[1].Value);
                int lenX = Int32.Parse(matches[2].Value);
                int lenY = Int32.Parse(matches[3].Value);
                for (int x = wallX; x < wallX + lenX; x++)
                {
                    for (int y = wallY; y < wallY + lenY; y++)
                    {
                        _walls.Add((x, y));
                    }
                }
                line = sr.ReadLine();
            }
            Render(PosAgent);
        } catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        } finally
        {
            Console.WriteLine("Final");
        }
    }
    public decimal GetHeu((int, int) current)
    {
        decimal minHeu = Decimal.MaxValue;
        foreach ((int, int) goal in _goals)
        {
            // Manhatten
            decimal cost = Math.Abs(current.Item1 - goal.Item1) + Math.Abs(current.Item2 - goal.Item2);
            if (cost < minHeu)
            {
                minHeu = cost;
            }
        }
        return minHeu;
    }


    public bool IsValid(State state)
    {
        if (_walls.Any(coord => coord.Item1 == state.Coord.Item1 && coord.Item2 == state.Coord.Item2))
        {
            return false;
        }
        if (state.Coord.Item1 < 0 || state.Coord.Item2 < 0 || state.Coord.Item1 > _mapSize.Item2 || state.Coord.Item2 > _mapSize.Item1)
        {
            return false;
        }
        return true;
    }

    public List<State> GetCandidates(State state)
    {
        List<State> tmp = new List<State>
        {
            new State((state.Coord.Item1, state.Coord.Item2 - 1), GetHeu((state.Coord.Item1, state.Coord.Item2 - 1)), state),
            new State((state.Coord.Item1 + 1, state.Coord.Item2), GetHeu((state.Coord.Item1 + 1, state.Coord.Item2)), state),
            new State((state.Coord.Item1, state.Coord.Item2 + 1), GetHeu((state.Coord.Item1, state.Coord.Item2 + 1)), state),
            new State((state.Coord.Item1 - 1, state.Coord.Item2), GetHeu((state.Coord.Item1 - 1, state.Coord.Item2)), state)
        };
        List<State> candidates = new List<State>();
        foreach (State t in tmp)
        {
            if (IsValid(t))
            {
                candidates.Add(t);
            }
        }

        return candidates;
    }

    public void BFS()
    {
        Console.WriteLine("BFS is running...");
        // Add the initial state into queue
        queue.Enqueue(new State(PosAgent, 0, null));
        while (queue.Count > 0)
        {
            State currentState = queue.Dequeue();
            visitedStates.Add(currentState);
            if (_goals.Contains(currentState.Coord))
            {
                // Reach to a goal
                Console.WriteLine("Winner Winner, Chicken Dinner!");
                GetPath(currentState);
                break;
                // To be done: Use the latest states and trace back for the path.

                // Call the render method to visualize the solution.

                // Parse the solution, and output to a text file.
            }
            else
            {
                // Get candidates of the current state, push them into queue.
                List<State> candidates = GetCandidates(currentState);
                foreach (State t in candidates)
                {

                    // Apply cost method and pick the best one
                    // Check if the new state has been explored
                    if (!(queue.Any(s => s.Coord.Item1 == t.Coord.Item1 && s.Coord.Item2 == t.Coord.Item2) ||
                        visitedStates.Any(s => s.Coord.Item1 == t.Coord.Item1 && s.Coord.Item2 == t.Coord.Item2)))
                    {
                        queue.Enqueue(t);
                    }
                }
                // Dequeue a new state as current state, here can apply a heuristic strategy, call the cost() method.
                Console.WriteLine(String.Format("Queue nodes: {0}\tVisited nodes: {1}", queue.Count, visitedStates.Count));
                // Render(currentState.Coord, null);
                // Thread.Sleep(1);
            }
        }
    }

    public void AStar()
    {
        Console.WriteLine("A* is running...");
        // Add the initial state into queue
        queue.Enqueue(new State(PosAgent, GetHeu(PosAgent), null));
        while (queue.Count > 0)
        {
            State currentState = queue.Dequeue();
            visitedStates.Add(currentState);
            // Console.WriteLine(currentState.Coord);
            if (_goals.Contains(currentState.Coord))
            {
                // Reach to a goal
                Console.WriteLine("Winner Winner, Chicken Dinner!");
                GetPath(currentState);
                break;
                // To be done: Use the latest states and trace back for the path.

                // Call the render method to visualize the solution.

                // Parse the solution, and output to a text file.
            }
            else
            {
                // Get candidates of the current state, push them into queue.
                List<State> candidates = GetCandidates(currentState);
                decimal minCost = Decimal.MaxValue;
                List<State> bestCandiadates = new List<State>();
                foreach (State t in candidates)
                {

                    // Apply cost method and pick the best one
                    // Check if the new state has been explored
                    if (!(queue.Any(s => s.Coord.Item1 == t.Coord.Item1 && s.Coord.Item2 == t.Coord.Item2) ||
                        visitedStates.Any(s => s.Coord.Item1 == t.Coord.Item1 && s.Coord.Item2 == t.Coord.Item2)))
                    {
                        // Not explore yet
                        //if (minCost >= t.Heu) {
                        bestCandiadates.Add(t);
                        //    minCost = t.Heu;
                        //}
                    }
                }
                foreach (State bestCandiadate in bestCandiadates)
                {
                    queue.Enqueue(bestCandiadate);
                }
                // Dequeue a new state as current state, here can apply a heuristic strategy, call the cost() method.
                // Console.WriteLine(String.Format("Queue nodes: {0}\tVisited nodes: {1}", queue.Count, visitedStates.Count));
                Console.WriteLine();
                // Render(currentState.Coord, null);
                // Thread.Sleep(1);
            }
            Render(PosAgent);
        }
    }

    public void Search(String method)
    {
        switch (method)
        {
            case "bfs":
                BFS();
                break;
            case "A*":
                AStar();
                break;
            default:
                Console.WriteLine("{0} did not match any methods.", method);
                break;
        }
    }
    public void GetPath(State finalState)
    {
        State? tmp = finalState;
        // Stack<(int, int)> path = new Stack<(int, int)> ();
        while (tmp != null)
        {
            path.Add(tmp);
            tmp = tmp.Parent;
        }
        Console.WriteLine(String.Format("The legnth of the path is: {0}.", path.Count - 1));
        path.Reverse();
        Render(PosAgent);
    }
    public void Render((int, int) agentCoord)
    {
        for (int y = 0; y < _mapSize.Item1; y++)
        {
            for (int x = 0; x < _mapSize.Item2; x++)
            {
                if (_walls.Any(coord => coord.Item1 == x && coord.Item2 == y))
                {
                    Console.Write(_symbols["wall"]);
                } else if (agentCoord.Item1 == x && agentCoord.Item2 == y)
                {
                    Console.Write(_symbols["agent"]);

                } else if (path.Any(state => state.Coord.Item1 == x && state.Coord.Item2 == y))
                {
                    Console.Write(_symbols["agent"]);
                }
                else if (_goals.Any(coord => coord.Item1 == x && coord.Item2 == y))
                {
                    Console.Write(_symbols["goal"]);
                } else if (visitedStates.Any(state => state.Coord.Item1 == x && state.Coord.Item2 == y))
                {
                    Console.Write(_symbols["visited"]);
                } else
                {
                    Console.Write(_symbols["area"]);
                }
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }
}


