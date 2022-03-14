using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StalingradServerV1
{
    public class Graph
    {


        private int _V; // n of vertices


        LinkedList<int>[] _adj; // adjacency lists

        public Graph(int V)
        {

            _adj = new LinkedList<int>[V];
            for (int i = 0; i < _adj.Length; i++)
            {
                _adj[i] = new LinkedList<int>();
            }
            _V = V;
        }


        public void AddEdge(int v, int w)
        {
            _adj[v].AddLast(w);

        }


        public List<int> searchPath(int start, int finish) // BFS alghorithm 
        {
            int s = start;
            List<int> prev = new List<int>(new int[_V]);
            List<int> path = new List<int>();


            bool[] visited = new bool[_V];
            for (int i = 0; i < _V; i++)
                visited[i] = false;

            LinkedList<int> queue = new LinkedList<int>();
            //Mark the current node as visited and enqueue it
            visited[s] = true;
            queue.AddLast(s);

            while (queue.Any())
            {

                s = queue.First();
                queue.RemoveFirst();
                LinkedList<int> list = _adj[s];

                foreach (var val in list)
                {

                    if (val == finish) // we found the destination and we need to reconstruct path; 
                    {
                        int current_pos = s;
                        while (prev[current_pos] != start)
                        {

                            path.Add(prev[current_pos]);
                            current_pos = prev[current_pos];
                        }
                        return path;
                    }

                    if (!visited[val])
                    {
                        prev[val] = s;
                        visited[val] = true;
                        queue.AddLast(val);
                    }
                }
            }

            return path;
        }
    }
}
