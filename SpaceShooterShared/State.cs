using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceShooterShared
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
        public int Health { get; set; }
    }


    public class State
    {
        public int Role { get; set; }

        public Player Player { get; set; }
        public List<Enemy> Enemies { get; set; } = new List<Enemy>();

        // ready 눌렀을 때 바로 시작하게끔 
        public bool Ready { get; set; }


    }



}
