using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.NetCore.Server;
using Newtonsoft.Json;
using WebSocketSharp.NetCore;

namespace StalingradServerV1
{

    public class WsObject
    {
        public string command { get; set; }
        public int session_id { get; set; }

    }
    public class GameSessionState : WebSocketBehavior
    {



        protected override void OnOpen()
        {


            Console.WriteLine("New connection established for game session . session id " + this.ID);

            base.OnOpen();
        }
        protected override void OnError(WebSocketSharp.NetCore.ErrorEventArgs e)
        {

            /*
            Console.Write("Lost WS connection with id \"" + this.ID + "\". The corespoding game session with id# "  +
                GameSession.active_sessions.FirstOrDefault(o => o.wsserver_id == this.ID).session_id + " will also be closed");

            GameSession.active_sessions.FirstOrDefault(o => o.wsserver_id == this.ID).terminate = true;*/
            //Console.WriteLine(e.Message);

            //base.OnError(e);
        }
        protected override void OnClose(CloseEventArgs e)
        {
            // Console.WriteLine("Connection " + this.ID + " has been closed .");


            if(GameSession.active_sessions.FirstOrDefault(o => o.wsserver_id == this.ID) == null)
            {
                Console.WriteLine("WS connection with id \"" + this.ID + "\" was closed.");
                base.OnClose(e);
                return;
            }


            Console.WriteLine("WS connection with id \"" + this.ID + "\" was closed. The corespoding game session with id# " +
               GameSession.active_sessions.FirstOrDefault(o => o.wsserver_id == this.ID).session_id + " will also be closed");

            GameSession.active_sessions.FirstOrDefault(o => o.wsserver_id == this.ID).terminate = true;


            base.OnClose(e);
        }
        protected override void OnMessage(MessageEventArgs e)
        {
            //Console.WriteLine("Received session state request:" + e.Data);
            //ws_object = new WsObject();
            WsObject ws_object = JsonConvert.DeserializeObject<WsObject>(e.Data);

            if (GameSession.active_sessions.FirstOrDefault(o => o.session_id == ws_object.session_id) == null)
                return;

            switch (ws_object.command)
            {
                case "END_SESSION":
              
                    GameSession.active_sessions.FirstOrDefault(o => o.session_id == ws_object.session_id).terminate = true;
                   
                    break;
                case "GET_GAME_SESSION":
                    GameSession.active_sessions.FirstOrDefault(o => o.session_id == ws_object.session_id).wsserver_id = this.ID; // keep hold of the id in case of losing connection to be able to close the session
                    Send(GameSession.active_sessions.FirstOrDefault(o => o.session_id == ws_object.session_id).getGameState());
                          
                    break;
            }


            //base.OnMessage(e);
        }
    }


    internal class WSServer
    {
        public WebSocketServer wssv = null;
        public string address = string.Empty;
        public WSServer()
        {

        }

        public void init(string url = "ws://localhost:7890/")
        {
            address = url.Trim();
            wssv = new WebSocketServer(address);
            wssv.AddWebSocketService<GameSessionState>("/game_session");
            wssv.Start();

        }
        public void stop()
        {

            wssv.Stop();

            Console.WriteLine("WS server stopped");
        }

    }
}
