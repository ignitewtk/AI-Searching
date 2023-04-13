namespace AI_ShortestPath;

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

class Program
{
     static void Main(string[] args)
    {
        Console.WriteLine(args[2]);
        Problem problem = new Problem(args[0], args[1]);
        problem.Search();
       
        // problem.Render(problem.PosAgent);
       
        return;
    }
}

public class State
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

    private List<(int, int)> _goals = new List<(int, int)>();
    private List<(int, int)> _walls = new List<(int, int)>();

    private Dictionary<string, string> _symbols = new Dictionary<string, string>()
    {
        {"wall", " #"},
        {"agent", " o"},
        {"goal", " x"},
        {"area", " ."},
        {"visited", " -" }
    };
    
    Queue<State> queue = new Queue<State>();
    List<State> visitedStates = new List<State>();


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
            // Console.WriteLine(String.Format("Map Size:\t ({0}, {1})", _mapSize.Item1, _mapSize.Item2));

            // Get coordinates of the agent
            line = sr.ReadLine();
            subs = line.Split(',');
            matches = rx.Matches(line);
            _posAgent = (Int32.Parse(matches[0].Value), Int32.Parse(matches[1].Value));
            // Add the initial state into queue
            queue.Enqueue(new State(PosAgent, 0, null));
            // Console.WriteLine(String.Format("Position of Agent:\t ({0}, {1})", PosAgent.Item1, PosAgent.Item2));

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
        } catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        } finally
        {
            Console.WriteLine("Final");
        }
    }

    public List<State> GetCandidates(State state)
    {
        List<State> tmp = new List<State>
        {
            new State((state.Coord.Item1, state.Coord.Item2 - 1), 0, state),
            new State((state.Coord.Item1 + 1, state.Coord.Item2), 0, state),
            new State((state.Coord.Item1, state.Coord.Item2 + 1), 0, state),
            new State((state.Coord.Item1 - 1, state.Coord.Item2), 0, state)
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


    public void Search()
    {
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
            } else
            {
                // Get candidates of the current state, push them into queue.
                List<State> candidates = GetCandidates(currentState);
                foreach (State t in candidates)
                {
                    
                    // Apply cost method and pick the best one
                    // Check if the new state has been explored
                    if (queue.Any(s => s.Coord.Item1 == t.Coord.Item1 && s.Coord.Item2 == t.Coord.Item2) || 
                        visitedStates.Any(s => s.Coord.Item1 == t.Coord.Item1 && s.Coord.Item2 == t.Coord.Item2))
                    {

                    } else
                    {
                        queue.Enqueue(t);
                    }
                }
                // Dequeue a new state as current state, here can apply a heuristic strategy, call the cost() method.
                Console.WriteLine(String.Format("{0}, {1}", queue.Count, visitedStates.Count));
                foreach (State t in queue)
                {
                    Console.Write(String.Format("{0}, ", t.Coord));
                }
                Console.WriteLine();
                // Render(currentState.Coord);
                // Thread.Sleep(1);

            }
        }
        

        Console.WriteLine(queue.Count);
    }
    public void GetPath(State finalState)
    {
        State tmp = finalState;
        Stack<(int, int)> path = new Stack<(int, int)> ();
        while (tmp.Parent != null)
        {
            path.Push(tmp.Coord);
            tmp = tmp.Parent;
        }
        Console.WriteLine(String.Format("The legnth of the path is: {0}.", path.Count));
        while (path.Count > 0)
        {
            (int, int) coord = path.Pop();
            Render(coord);
            Console.WriteLine("===============");
        }
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
                } else if (_goals.Any(coord => coord.Item1 == x && coord.Item2 == y))
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

    }
}


