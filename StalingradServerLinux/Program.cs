
using System;
using System.Net;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft;
using System.Threading.Tasks;
using System.Threading;
using System.Configuration;


namespace StalingradServerV1
{



    class Program
    {



        public static void Main(string[] args)
        {

            var wssv_url = ConfigurationManager.AppSettings.Get("wssv_url");
            var http_url = ConfigurationManager.AppSettings.Get("http_url"); ;
            var mongo_url = ConfigurationManager.AppSettings.Get("mongo_url"); ;
            WSServer wssv = new WSServer(); 
            wssv.init(wssv_url); // init the web sockets server
            Console.WriteLine("Web sockets server started at " + wssv.address);

            MongoClient dbClient = new MongoClient(mongo_url);

            HttpServer.listener = new HttpListener();
            HttpServer.url = http_url;
            HttpServer.listener.Prefixes.Add(http_url);
            HttpServer.listener.Start();

            

            Console.WriteLine("Listening for connections on {0}", HttpServer.url);

            // Handle requests
            Task listenTask = HttpServer.HandleIncomingConnections(dbClient); // start server

            listenTask.GetAwaiter().GetResult();

            HttpServer.listener.Close();

            Console.ReadKey();
            wssv.stop();
        }

    }




}