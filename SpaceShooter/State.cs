using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceShooter
{
    public class Enemy
    {
        // 우선 지피티 코드 갖다 썼는데 float을 int로 바꿔야 할듯.
        // Rotation, Health 는 필요 없을 듯 
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class Player
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int health { get; set; }
    }


    public class State
    {
        public int Role { get; set; }

        public Player Player { get; set; }
        public List<Enemy> Enemies { get; set; } = new List<Enemy>();

    }



}
