using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Team_Victoria_Controller
{
    class EveDef
    {
        public const string name = "Victoria";
        public const string serial_num = "BA-EVE-03";

        public const int FloorToRoot_Z = 190;
        public const int RootToElbow_L = 300;
        public const int ElbowToWrist_L = 300;
        public const int WristToEnd_Z = 75;
        public const int WristToEnd_L = 50;

        public const int WristToCam_Z = 20;
        public const double CameraWidthOverZ = 0.578; 
    }

    class MartyDef
    {
        public const string name = "Marty McFly";

        public const int FloorToRoot_Z = 88;
        public const int RootToElbow_L = 144;//linka
        public const int ElbowToWrist_L = 163; //linkb
        public const int WristToEnd_Z = 65;
        public const int WristToEnd_L = 10;

        public const int DistanceFromEve = 600;
    }


}
