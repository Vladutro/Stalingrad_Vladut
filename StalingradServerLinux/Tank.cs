
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace StalingradServerV1
{
    public class TankScore
    {
        public string tank_id { get; set; }
        public string callsign_id { get; set; }
        public int score { get; set; }
        public string type { get; set; }
    }
    public class Tank
    {

        public string callsign_id { get; set; }
        public string type { get; set; }
        public int HP { get; set; }
        public float fire_rate { get; set; }
        public int moving_speed { get; set; }
        public string _id { get; set; }
        List<String> components { get; set; }

        public bool display_position = false;

        public int status = 1; //1 is alive 0 is dead;
        public Point pos { get; set; }

        public bool shooting_started = false;
        public long shooting_started_t = 0;
        public long shooting_charging_t = 0; // this will be randomized to mimic the reflex of a real player and give some randomization to the winning side
        public int score;
        public Tank()
        {
            components = new List<String>();
            pos = new Point();
            callsign_id = "";
            HP = 100;
            fire_rate = 1;
            moving_speed = 1;
            _id = "";
            score = 0;
            type = "";


        }
        public Tank(int x, int y, string tank_id)
        {
            this._id = tank_id;
            pos = new Point();
            pos.x = x; pos.y = y;
        }


        public TankScore GetTankScore()
        {
            TankScore new_score = new TankScore();
            new_score.score = score;
            new_score.tank_id = _id;
            new_score.type = type;
            new_score.callsign_id = callsign_id;
            return new_score;
        }


        public bool hasClearLineOfFire(Point p1, Point p2, int[,] battle_map)
        {
            if (p1.x == p2.x)
            {
                int y1 = 0;
                int y2 = 0;

                if (p1.y > p2.y)
                {
                    y1 = p2.y;
                    y2 = p1.y;
                }
                else
                {
                    y1 = p1.y;
                    y2 = p2.y;
                }

                for (int i = y1; i < y2; i++)
                {
                    if (battle_map[i, p1.x] == 1)
                        return false;
                }


                return true;

            }
            else
                if (p1.y == p2.y)
            {

                int x1 = 0;
                int x2 = 0;

                if (p1.x > p2.x)
                {
                    x1 = p2.x;
                    x2 = p1.x;
                }
                else
                {
                    x1 = p1.x;
                    x2 = p2.x;
                }
                for (int i = x1 + 1; i < x2; i++)
                {
                    if (battle_map[p1.y, i] == 1)
                        return false;
                }


                return true;

            }

            return false;
        }




        public void move(Tank enemy, Graph g, int[,] battle_map)
        {
            // check if he can shoot and randomize the time till he can actually shoot so that there some possibility that both of them win a round. 
            if (hasClearLineOfFire(pos, enemy.pos, battle_map))
            {
                Random randomizer = new Random();
                shooting_started = true;
                shooting_started_t = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                shooting_charging_t = randomizer.Next(200, 500);
                return;
            }

            List<int> path = g.searchPath(pos.y * GameSession.map_width + pos.x, enemy.pos.y * GameSession.map_width + enemy.pos.x);
            if (path.Count > 1)
            {
                pos.x = path[path.Count - 1] % GameSession.map_width;
                pos.y = path[path.Count - 1] / GameSession.map_width;
            }
        }

        //fire and kill enemy tank 
        public void Fire(Tank enemy)
        {
            if (status == 1)
            {
                enemy.status = 0;
                shooting_started = false;
                enemy.shooting_started = false;
                score++;
            }

        }
    }
}





