using System;
using System.Timers;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Newtonsoft.Json;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.RegularExpressions;

namespace StalingradServerV1
{

    public class GameScore
    {
        public List<TankScore> players;
        string created_at { get; set; }
        public GameScore()
        {
            players = new List<TankScore>();
        }
    }
    public class Coord
    {
        public int x { get; set; }
        public int y { get; set; }
    }
    public class Map
    {
        public string _id { get; set; }
        public string name { get; set; }
        public List<Coord> coords { get; set; }
        //public string _id { get; set; }


    }


    public class Point
    {
        public int x;
        public int y;

        public Point(Point pos2 = null)
        {
            if (pos2 != null)
            {
                this.x = pos2.x;
                this.y = pos2.y;
            }
            else
            {
                x = 0;
                y = 0;
            }


        }
    }





    public class GameSession
    {

        public int frame_rate = 2;

        public static int map_width = 50;
        public static int map_height = 50;
        public static List<GameSession> active_sessions = new List<GameSession>();
        public LinkedList<Point> adj_list;
        public static int max_session_no = 0;
        public string wsserver_id = "";
        public int session_id { get; set; } = 0;
        public Thread game_thread;
        List<Tank> tanks;

        int[,] battle_map;
        Map map_obstacles = null;
        public Graph path_g;
        public bool status = false;
        public bool terminate = false;


        public GameSession()
        {

            tanks = new List<Tank>();
            //Map map_obstacles = new Map(); 
            battle_map = new int[map_width, map_height];

            //battle_map = new int[50, 50]; // initializing the battle map;
        }
        public bool loadTanks(List<string> tank_ids, MongoClient dbClient)
        {
            var database = dbClient.GetDatabase("stalingrad0");
            var maps_collection = database.GetCollection<BsonDocument>("tanks");


            for (int i = 0; i < tank_ids.Count; i++)
            {
                var obj_id = new ObjectId(tank_ids[i]);
                var mapsDocuments = maps_collection.Find(Builders<BsonDocument>.Filter.Eq("_id", obj_id)).FirstOrDefault();
                string json_result = mapsDocuments.ToJson();
                string result = Regex.Match(json_result, @"ObjectId\(([^\)]*)\)").Value;
                string id = result.Replace("ObjectId(", string.Empty).Replace(")", String.Empty);
                json_result = json_result.Replace(result, id);


                Tank new_tank = JsonConvert.DeserializeObject<Tank>(json_result);

                Random randomizer = new Random();

                int y = randomizer.Next(1, 48);
                int x = 2;
                if (new_tank.type == "T-34") // put all russian tanks on left and german tanks on the right side of them map
                {
                    x = 2;

                }
                else
                {
                    x = 47;
                }


                new_tank.pos.y = y;
                new_tank.pos.x = x;
                tanks.Add(new_tank);

            }



            return true;

        }

        public string saveScore(MongoClient dbClient)
        {
            GameScore game_score_object = new GameScore();

            for (int i = 0; i < tanks.Count; i++)
            {
                TankScore tscore = tanks[i].GetTankScore();
                game_score_object.players.Add(tscore);
            }

            var database = dbClient.GetDatabase("stalingrad0");
            var maps_collection = database.GetCollection<GameScore>("scores");

            maps_collection.InsertOne(game_score_object);
            return null;
        }
        public bool loadMap(string map_id, MongoClient dbClient)
        {
            var database = dbClient.GetDatabase("stalingrad0");
            var maps_collection = database.GetCollection<BsonDocument>("maps");
            var obj_id = new ObjectId(map_id);
            var mapsDocuments = maps_collection.Find(Builders<BsonDocument>.Filter.Eq("_id", obj_id)).FirstOrDefault();

            string json_result = mapsDocuments.ToJson();
            string result = Regex.Match(json_result, @"ObjectId\(([^\)]*)\)").Value;
            string id = result.Replace("ObjectId(", string.Empty).Replace(")", String.Empty);
            json_result = json_result.Replace(result, id);

            map_obstacles = JsonConvert.DeserializeObject<Map>(json_result);

            path_g = new Graph(map_width * map_height);


            if (map_obstacles != null)
                map_obstacles.coords.ForEach(delegate (Coord coord)
                {
                    battle_map[coord.y, coord.x] = 1;
                });

            for (int i = 0; i < map_height; i++)
            {
                for (int j = 0; j < map_width; j++)
                {

                    if (i > 1) // check above
                    {
                        if (battle_map[i - 1, j] == 0) // no existing obstacle
                            path_g.AddEdge(i * map_width + j, (i - 1) * map_width + j);
                    }

                    if (i < map_height - 1) // check bellow
                    {
                        if (battle_map[i + 1, j] == 0)
                            path_g.AddEdge(i * map_width + j, (i + 1) * map_width + j);

                    }

                    if (j > 1) // check left
                    {
                        if (battle_map[i, j - 1] == 0)
                        {
                            path_g.AddEdge(i * map_width + j, i * map_width + j - 1);
                        }
                    }

                    if (j < map_width - 1) // check right
                    {
                        if (battle_map[i, j + 1] == 0)
                        {
                            path_g.AddEdge(i * map_width + j, i * map_width + j + 1);
                        }
                    }
                }
            }

            Console.WriteLine("successfully loaded map with id " + map_obstacles._id);


            return true;
        }
        public string getGameState()
        {
            string output = JsonConvert.SerializeObject(tanks);
            return output;

        }
        public void ResetGame()
        {
            Random randomizer = new Random();
            int y = randomizer.Next(1, 48);
            tanks[0].pos.y = y;
            tanks[0].pos.x = 1;
            tanks[0].status = 1;
            tanks[0].shooting_started = false;
            y = randomizer.Next(1, 48);
            tanks[1].pos.y = y;
            tanks[1].pos.x = 48;
            tanks[1].status = 1;
            tanks[1].shooting_started = false;
        }

        public void Run()
        {
            status = true;
            terminate = false;

            Console.WriteLine("session started");
            long timer_start_ms = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            while (!this.terminate)
            {
                long current_time_ms = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                if (tanks[0].shooting_started || tanks[1].shooting_started) // in this part it checks if either of the tanks started shooting
                {
                    if (tanks[0].shooting_started)
                    {
                        if (tanks[0].shooting_started_t + tanks[0].shooting_charging_t < current_time_ms)
                        {
                            tanks[0].Fire(tanks[1]);
                        }
                    }
                    if (tanks[1].shooting_started)
                    {
                        if (tanks[1].shooting_started_t + tanks[1].shooting_charging_t < current_time_ms)
                        {
                            tanks[1].Fire(tanks[0]);
                        }
                    }

                    if (tanks[0].status == 0 || tanks[1].status == 0)
                    {
                        if (tanks[0].score >= 20 || tanks[1].score >= 20)
                        {
                            this.terminate = true;
                        }
                        ResetGame();
                    }
                }

                // long current_time_ms = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                if (timer_start_ms + (1000 / frame_rate) < current_time_ms) // movement and search path part
                {
                    timer_start_ms = timer_start_ms + (1000 / frame_rate);

                    tanks[0].move(tanks[1], path_g, battle_map);
                    tanks[1].move(tanks[0], path_g, battle_map);
                }
            }
            status = false;
            Console.WriteLine("session ended");
        }
    }
}
