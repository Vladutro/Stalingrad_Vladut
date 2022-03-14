using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using WebSocketSharp.NetCore;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Configuration;
using System.Collections.Specialized;

//using System.Text.Json;
namespace StalingradClient
{
    internal class Program
    {

        public static int refresh_rate = 2;
        public static int map_height = 50;
        public static int map_width = 50;
        public static string get_session_state_json = null;
        public static bool update_received = false;

        public static bool linux_client = false;
        public class Map
        {
            public string _id { get; set; }
            public string name { get; set; }
            public List<Point> coords { get; set; }


            public static int selected_map = 0;
            public static string obstacle_tile = "▓▓";
            public static string obstacle_char = "▓";
            public static string clear_map_tile = "░░";
            public static char clear_map_char = '░';

        }


        public static List<Tank> tanks;
        public static List<Tank> tanks_full_list;
        public static List<Map> maps;


        public static List<Tank> selected_tanks;
        public class Point
        {
            public int x { get; set; }
            public int y { get; set; }
        }
        public class Tank // this class is for keeping each players position as well as for deserializing list of tanks when they are requested
        {

            public string _id { get; set; }
            public string type { get; set; }
            public float fire_rate_ps { get; set; }
            public int moving_speed { get; set; }
            public int HP { get; set; }
            public List<string> components { get; set; }
            public string callsign_id { get; set; }

            public int score = 0;

            public Point pos { get; set; }
            public string tank_id { get; set; }
            public char tank_char = 'R';
            public Tank()
            {
                pos = new Point();
                pos.x = 0;
                pos.y = 0;
                tank_id = "";
            }
        }

        class HTTPReturnParams // this is used for deserializing http returned json
        {
            public int session_id { get; set; }
            public string[] tanks { get; set; }
            public string map_id { get; set; }
            public string score_id { get; set; }
            public Point[] coords { get; set; }
        }

        static HttpClient client = new HttpClient();
        static async Task<string> getMapList(string path) // Get map list GET request
        {
            string map_list_str = "";
            HttpResponseMessage response = await client.GetAsync(path);
            map_list_str = await response.Content.ReadAsStringAsync();
            maps = JsonConvert.DeserializeObject<List<Map>>(map_list_str);
            //Console.WriteLine(map_list_str);
            return map_list_str;
        }

        static async Task<string> saveScore(string path, string session_id) // save score POST request
        {
            string res = "";

            string map_id = maps[Map.selected_map]._id;
            var myContent = "{\"session_id\" : \"" + session_id + "\"}";
            var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
            var byteContent = new ByteArrayContent(buffer);

            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = await client.PostAsync(path, byteContent);
            res = await response.Content.ReadAsStringAsync();
            string s = await response.Content.ReadAsStringAsync();
            return s;
        }
        static async Task<string> getTankList(string path) // get tanks GET request 
        {
            string tank_list_str = "";
            HttpResponseMessage response = await client.GetAsync(path);
            tank_list_str = await response.Content.ReadAsStringAsync();

            tanks_full_list = JsonConvert.DeserializeObject<List<Tank>>(tank_list_str);
            return tank_list_str;
        }

        static async Task<string> startSession(string path) // start session POST request 
        {
            string res = "";
            string map_id = maps[Map.selected_map]._id;
            var myContent = "{\"tank_ids\" : [\"" + selected_tanks[0]._id + "\", \"" + selected_tanks[1]._id + "\"], \"map_id\" : \"" + map_id + "\"}";
            var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
            var byteContent = new ByteArrayContent(buffer);

            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = await client.PostAsync(path, byteContent);
            res = await response.Content.ReadAsStringAsync();
            string s = await response.Content.ReadAsStringAsync();


            HTTPReturnParams tmp_obj = JsonConvert.DeserializeObject<HTTPReturnParams>(s);
            Console.WriteLine("received response and session " + tmp_obj.session_id + " started");
            return tmp_obj.session_id.ToString();
        }


        public static void clearTanks() // Clearing the map after moving a tank to a new position
        {

            if (tanks.Count > 0)
                for (int i = 0; i < tanks.Count; i++)
                {
                    Console.SetCursorPosition(tanks[i].pos.x * 2, tanks[i].pos.y);
                    Console.Write(Map.clear_map_tile);
                }
            Console.SetCursorPosition(0, map_height + 1);
        }
        public static void drawTanks() // Drawing the tanks 
        {
            if (tanks.Count > 0)
                for (int i = 0; i < tanks.Count; i++)
                {

                    Console.SetCursorPosition(tanks[i].pos.x * 2, tanks[i].pos.y);
                    if (tanks[i].type == "T-34")
                    {
                        Console.Write("R");

                    }
                    else
                    {
                        Console.Write("G");
                    }

                }
            Console.SetCursorPosition(0, map_height + 1);
        }

        public static void drawMap(int y_offset = 0) // draw map 
        {
            Console.SetCursorPosition(0, y_offset);

            string canvas_str = new string(Map.clear_map_char, map_width * 2);

            for (int i = 0; i < map_height; i++)
            {
                Console.WriteLine(canvas_str);
            }

            for (int j = 0; j < maps[Map.selected_map].coords.Count; j++)
            {
                Console.SetCursorPosition(maps[Map.selected_map].coords[j].x * 2, maps[Map.selected_map].coords[j].y + y_offset);
                Console.Write(Map.obstacle_tile);
            }

            Console.SetCursorPosition(0, 0);

        }



        public static void drawStartLogo()
        {
            
            Console.WriteLine("           ░░███████ ]▄▄▄▄▄▄▄▄");
            Console.WriteLine("         ▄▄▄█████████▄▄▄ ");
            Console.WriteLine("      [███████████████████]");
            Console.WriteLine("     \\°▲°▲°▲°▲°▲°▲°▲°▲°▲°▲°/");
            Console.WriteLine("<<<Welcome to Stallingrad Battle V1>>");
            
        }


        public static void drawMapList()
        {
            Console.Clear();
            for (int i = 0; i < maps.Count; i++)
            {
                string canvas_str = new string(Map.clear_map_char, map_width * 2);

                for (int j = 0; j < map_height; j++)
                {
                    Console.WriteLine(canvas_str);
                }
                Console.WriteLine(maps[i].name);
               // Console.WriteLine("\n");
            }


            for (int i = 0; i < maps.Count; i++)
            {
                int y_offset = i * (map_height + 3);

                for (int j = 0; j < maps[i].coords.Count; j++)
                {
                    Console.SetCursorPosition(maps[i].coords[j].x * 2, maps[i].coords[j].y + y_offset);
                    Console.Write(Map.obstacle_tile);
                }
            }
            Console.SetCursorPosition(0, maps.Count * (map_height + 3) + 2);
        }

        static void drawMapSelectMenu(int offset_y = 0) // here we select the map we want to use for playing 
        {
            Console.WriteLine("Select a map from the list to start the game.");
            offset_y++;
            Console.CursorVisible = false;

            int map_offset = maps.Count + 2;
            Map.selected_map = 0;
            if (maps.Count == 0)
                return;

            
            Console.SetCursorPosition(0, offset_y + Map.selected_map); ;

            Console.WriteLine("<<" + maps[Map.selected_map].name + ">>");
         

            for (int i = 1; i < maps.Count; i++)
            {
                Console.WriteLine(maps[i].name);
            }
            Map.selected_map = 0;
            bool select_map = true;
            if(!linux_client)
                drawMap(offset_y + map_offset);

            while (select_map)
            {
                ConsoleKeyInfo key_pressed = Console.ReadKey();
                //Console.ReadKey().Key != ConsoleKey.Enter

                switch (key_pressed.Key)
                {

                    case ConsoleKey.Enter:
                        select_map = false;

                        break;

                    case ConsoleKey.DownArrow:
                        Console.SetCursorPosition(0, offset_y + Map.selected_map);
                       
                        Console.WriteLine(maps[Map.selected_map].name + "                       ");

                        Map.selected_map++;
                        if (Map.selected_map >= maps.Count)
                        {
                            Map.selected_map = 0;

                        }
                        Console.SetCursorPosition(0, offset_y + Map.selected_map);
                        
                        Console.WriteLine("<<" + maps[Map.selected_map].name + ">>");

                        if (!linux_client)
                            drawMap(offset_y + map_offset);
                        break;

                    case ConsoleKey.UpArrow:
                        Console.SetCursorPosition(0, offset_y + Map.selected_map);
                        Console.WriteLine(maps[Map.selected_map].name + "                       ");

                        //Console.ResetColor();
                        Map.selected_map--;
                        if (Map.selected_map < 0)
                        {
                            Map.selected_map = maps.Count - 1;
                        }
                        Console.SetCursorPosition(0, offset_y + Map.selected_map);

                        Console.WriteLine("<<" + maps[Map.selected_map].name + ">>");

                        if (!linux_client)
                            drawMap(offset_y + map_offset);
                        break;

                }

            }

            Console.CursorVisible = false;
            Console.Clear();


        }
        static void drawTankSelectMenu(string type, int offset_y) // depending on the type the method lists only the german or russian tanks
        {
            Console.WriteLine(" Please select a " + type + " tank. Use the arrows to select the tank and press enter to submit");
            Console.CursorVisible = false;
            offset_y = offset_y + 1;
            List<int> tank_indexes = new List<int>();
            for (int i = 0; i < tanks_full_list.Count; i++)
            {
                if (tanks_full_list[i].type != type)
                {
                    continue;
                }
                tank_indexes.Add(i);
            }

            int selected_tank = 0;

            bool select_tank = true;

            for (int i = 0; i < tank_indexes.Count; i++)
            {
                Console.WriteLine(tanks_full_list[tank_indexes[i]].type + " '" + tanks_full_list[tank_indexes[i]].callsign_id + "'");
            }

           
            Console.SetCursorPosition(0, offset_y + selected_tank);
       
            //Console.WriteLine(tanks_full_list[tank_indexes[selected_tank]].type + " '" + tanks_full_list[tank_indexes[selected_tank]].callsign_id + "'");
            Console.WriteLine("<<" + tanks_full_list[tank_indexes[selected_tank]].type + " '" + tanks_full_list[tank_indexes[selected_tank]].callsign_id + "'>>");



            while (select_tank)
            {
                ConsoleKeyInfo key_pressed = Console.ReadKey();
                //Console.ReadKey().Key != ConsoleKey.Enter

                switch (key_pressed.Key)
                {

                    case ConsoleKey.Enter:

                        selected_tanks.Add(tanks_full_list[tank_indexes[selected_tank]]);
                        select_tank = false;

                        break;

                    case ConsoleKey.DownArrow:
                        Console.SetCursorPosition(0, offset_y + selected_tank);

              
                        Console.WriteLine(tanks_full_list[tank_indexes[selected_tank]].type + " '" + tanks_full_list[tank_indexes[selected_tank]].callsign_id + "'" + "                    ");



                        selected_tank++;
                        if (selected_tank >= tank_indexes.Count)
                        {
                            selected_tank = 0;

                        }
                        Console.SetCursorPosition(0, offset_y + selected_tank);
                       
                       
                        Console.WriteLine("<<" + tanks_full_list[tank_indexes[selected_tank]].type + " '" + tanks_full_list[tank_indexes[selected_tank]].callsign_id + "'>>");

             

                        break;

                    case ConsoleKey.UpArrow:
                        Console.SetCursorPosition(0, offset_y + selected_tank);
                       
                        Console.WriteLine(tanks_full_list[tank_indexes[selected_tank]].type + " '" + tanks_full_list[tank_indexes[selected_tank]].callsign_id + "'" + "                    ");

                      
                        selected_tank--;
                        if (selected_tank < 0)
                        {
                            selected_tank = tank_indexes.Count - 1;
                        }
                        Console.SetCursorPosition(0, offset_y + selected_tank);
                        Console.WriteLine("<<" + tanks_full_list[tank_indexes[selected_tank]].type + " '" + tanks_full_list[tank_indexes[selected_tank]].callsign_id + "'>>");

                        


                        break;

                }

            }
            Console.CursorVisible = true;
            Console.Clear();

        }

        public static void loadMap()
        {
            try
            {
                getMapList("api/v1/maps").GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }
        public static void loadTanks()
        {
            try // loading the tanks list from the database 
            {
                getTankList("api/v1/tanks").GetAwaiter().GetResult();// getTankListAsync("api/tanks");
                                                                     // Console.WriteLine(tank_list);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public static void drawMenus()
        {

            Console.Clear();
            drawTankSelectMenu("Panzer IV", 0); // select the german tank
            Console.Clear();
            drawTankSelectMenu("T-34", 0); // select the russian tank
            Console.Clear();
            drawMapSelectMenu(); // select the desired map for the game


        }

        public static void UpdateMap()
        {
            update_received = false;

            drawTanks();
            if(!linux_client)
            {
                Console.Write("Battle score is : " + tanks[0].type + " " + tanks[0].callsign_id + " " + tanks[0].score);
                Console.Write(" | " + tanks[1].type + " " + tanks[1].callsign_id + " " + tanks[1].score);
                Console.Write("                                     ");
                Console.WriteLine();
            }
            

        }

        static void Main(string[] args)
        {


            var wssv_url = ConfigurationManager.AppSettings.Get("wssv_url");
            var http_url = ConfigurationManager.AppSettings.Get("http_url"); ;
            string linux_client_str = ConfigurationManager.AppSettings.Get("linux_client");

            linux_client = linux_client_str == "true";

            tanks = new List<Tank>();
            maps = new List<Map>();
            tanks_full_list = new List<Tank>();
            selected_tanks = new List<Tank>();
            string session_id = null;

            
            WebSocket ws = new WebSocket(wssv_url);
            ws.OnMessage += Ws_OnMessage;
            ws.Connect();

            client.BaseAddress = new Uri(http_url);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            drawStartLogo();

            Console.WriteLine("Loaded the following configurations from the App.config file:");
            Console.WriteLine("wssv path:" + wssv_url);
            Console.WriteLine("api server path:" + http_url);

            Console.WriteLine("Press any key to start.");
            Console.ReadKey();
            Console.Clear();

            loadTanks();
            loadMap();
            drawMenus();
            try
            {
                session_id = startSession("api/v1/simulate").GetAwaiter().GetResult();
                get_session_state_json = "{\"command\" : \"GET_GAME_SESSION\", \"session_id\" : \"" + session_id + "\"}";
                //Console.WriteLine(session_id);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            bool game_run = true;
            long timer_start_ms = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            drawMap();
            Console.CursorVisible = false;

            while (game_run) // main game loop for updating the tanks position via websockets 
            {
                if (Console.KeyAvailable) // exit the loop
                {
                    break;
                }
                long current_time_ms = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                if (timer_start_ms + (1000 / refresh_rate) < current_time_ms)
                {
                    timer_start_ms += (1000 / refresh_rate);
                    ws.Send(get_session_state_json); // update 
                }

                if (update_received)
                {
                    update_received = false;
                    UpdateMap();
                }
            }

            string close_session_json = "{\"command\" : \"END_SESSION\", \"session_id\" : \"" + session_id + "\"}";
            ws.Send(close_session_json);
            ws.Close();
            Console.WriteLine("Game ended with the score:");
            
            Console.Write("Battle score is : " + tanks[0].type + " " + tanks[0].callsign_id + " - " + tanks[0].score);
            Console.Write(" | " + tanks[1].type + " " + tanks[1].callsign_id + " - " + tanks[1].score);
            Console.Write("                                     ");
            Console.WriteLine();
            

            Console.WriteLine(" Do you want to save the score? please enter y/n");
            Console.ReadKey();

            while (true)
            {
                string input = Console.ReadLine();
                input = input.ToUpper(); // save new battle score. 
                if (input == "Y")
                {
                    try
                    {
                        saveScore("api/v1/score", session_id).GetAwaiter().GetResult();// getTankListAsync("api/tanks");
                        Console.Clear();
                        drawMapSelectMenu();
                        //Console.WriteLine(map_list);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                    Console.WriteLine("saving the score");
                    break;
                }
                else if (input == "N") // discard
                {
                    Console.WriteLine("Discarding the score");
                    break;
                }
                else
                {
                    Console.WriteLine(" invalid input");
                }
            }
            Console.ReadKey();
        }

        private static void Ws_OnMessage(object sender, MessageEventArgs e) // update tanks on screen
        {
            clearTanks();
            tanks = JsonConvert.DeserializeObject<List<Tank>>(e.Data);
            update_received = true;
        }
    }
}
