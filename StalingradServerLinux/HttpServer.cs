using System;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using MongoDB.Driver;
using MongoDB.Bson;
//using System.Text.Json;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace StalingradServerV1
{

    class PostParams
    {
        public int session_id { get; set; }
        public List<string> tank_ids { get; set; }
        public string map_id { get; set; }
    }


    class HttpServer
    {
        public static HttpListener listener;
        public static string url = "http://localhost:8000/";
        public static int pageViews = 0;
        public static int requestCount = 0;
        public static async Task<PostParams> getPostParams(HttpListenerRequest req)
        {

            Stream body = req.InputStream;
            Encoding encoding = req.ContentEncoding;
            StreamReader reader = new System.IO.StreamReader(body, encoding);

            string s = await reader.ReadToEndAsync();
            PostParams tmp_obj = JsonConvert.DeserializeObject<PostParams>(s);
            //Console.WriteLine("client data : " + s);
            body.Close();
            reader.Close();
            return tmp_obj;

        }

        public static async Task saveScoreAPI(HttpListenerRequest req, HttpListenerResponse resp, MongoClient dbClient)
        {

            Console.WriteLine("trying to save score");
            PostParams tmp_obj = await getPostParams(req);
            GameSession.active_sessions.FirstOrDefault(o => o.session_id == tmp_obj.session_id).saveScore(dbClient);

        }


        //each game session runs in it's own thread. 
        public static async Task startNewSessionAPI(HttpListenerRequest req, HttpListenerResponse resp, MongoClient dbClient)
        {

            PostParams tmp_obj = await getPostParams(req);

            GameSession newGameSession = new GameSession();

            newGameSession.session_id = GameSession.max_session_no++; // this is the session id returned to the client that's also used to close the session

            newGameSession.loadMap(tmp_obj.map_id, dbClient);
            newGameSession.loadTanks(tmp_obj.tank_ids, dbClient);

            GameSession.active_sessions.Add(newGameSession);
            newGameSession.game_thread = new Thread(newGameSession.Run);
            newGameSession.game_thread.Start(); // starting the emulation in it's thread. see Run method of GameSession class 

            byte[] data;

            data = Encoding.UTF8.GetBytes("{\"session_id\" : " + newGameSession.session_id + "}");

            resp.ContentType = "application/json";
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = data.LongLength;


            await resp.OutputStream.WriteAsync(data, 0, data.Length);


        }
        public static async Task getTanksAPI(HttpListenerRequest req, HttpListenerResponse resp, MongoClient dbClient)
        {
            byte[] data;
            Console.WriteLine("tanks requested");

            var database = dbClient.GetDatabase("stalingrad0");

            //var filter = Builders<BsonDocument>.Filter.Eq("type", "Panzer IV");

            var tanks_collection = database.GetCollection<BsonDocument>("tanks");

            var tanksDocuments = tanks_collection.Find(new BsonDocument()).ToList();


            string json_result = tanksDocuments.ToJson();


            for (int i = 0; i < tanksDocuments.Count; i++)
            {
                string result = Regex.Match(json_result, @"ObjectId\(([^\)]*)\)").Value;
                string id = result.Replace("ObjectId(", string.Empty).Replace(")", String.Empty);
                json_result = json_result.Replace(result, id);

            }
            data = Encoding.UTF8.GetBytes(json_result);


            resp.ContentType = "application/json";
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = data.LongLength;

            await resp.OutputStream.WriteAsync(data, 0, data.Length);

        }


        public static async Task getMapsAPI(HttpListenerRequest req, HttpListenerResponse resp, MongoClient dbClient)
        {
            byte[] data;
            Console.WriteLine("maps requested");

            var database = dbClient.GetDatabase("stalingrad0");

            var maps_collection = database.GetCollection<BsonDocument>("maps");

            var mapsDocuments = maps_collection.Find(new BsonDocument()).ToList();

            string json_result = mapsDocuments.ToJson();


            for (int i = 0; i < mapsDocuments.Count; i++)
            {
                string result = Regex.Match(json_result, @"ObjectId\(([^\)]*)\)").Value;
                string id = result.Replace("ObjectId(", string.Empty).Replace(")", String.Empty);
                json_result = json_result.Replace(result, id);

            }


            data = Encoding.UTF8.GetBytes(json_result);

            resp.ContentType = "application/json";
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = data.LongLength;
            await resp.OutputStream.WriteAsync(data, 0, data.Length);

        }

        public static async Task terminateSessionAPI(HttpListenerRequest req, HttpListenerResponse resp)// this is used when exiting the main loop at the client side
        {
            PostParams tmp_obj = await getPostParams(req);
            Console.WriteLine(tmp_obj.session_id);
            GameSession.active_sessions.ForEach(delegate (GameSession game_ses)
            {
                if (game_ses.session_id == tmp_obj.session_id)
                {
                    game_ses.terminate = true;
                    return;
                }
            });
            Console.WriteLine("End of client data:");
            Console.WriteLine("terminate session");

        }


        public static async Task getGameStateAPI(HttpListenerRequest req, HttpListenerResponse resp) // this is actually not used because i changed to use web sockets for updating the position of the tanks on the screen
        {
            PostParams tmp_obj = await getPostParams(req);
            GameSession.active_sessions.ForEach(async delegate (GameSession game_ses)
            {
                if (game_ses.session_id == tmp_obj.session_id)
                {
                    byte[] data;
                    string json_result = game_ses.getGameState();

                    data = Encoding.UTF8.GetBytes(json_result);

                    resp.ContentType = "application/json";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;

                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    return;
                }
            });

            Console.WriteLine(tmp_obj.session_id);
        }

        public static async Task HandleIncomingConnections(MongoClient dbClient)
        {
            bool runServer = true;
            while (runServer)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/shutdown"))
                {
                    Console.WriteLine("Shutdown requested");
                    runServer = false;
                }

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/api/v1/tanks"))
                {
                    await getTanksAPI(req, resp, dbClient);
                }

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/api/v1/maps"))
                {
                    await getMapsAPI(req, resp, dbClient);
                }

                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/api/v1/simulate"))//start a new session
                {
                    await startNewSessionAPI(req, resp, dbClient);
                }

                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/api/v1/score"))//start a new session
                {

                    await saveScoreAPI(req, resp, dbClient);

                }

                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/api/v1/terminate"))//start a new session
                {
                    await terminateSessionAPI(req, resp);
                }

                if ((req.HttpMethod == "GET") && (req.Url.AbsolutePath == "/api/v1/game_state"))//start a new session
                {
                    await getGameStateAPI(req, resp);
                }
                resp.Close();

            }
        }
    }
}
