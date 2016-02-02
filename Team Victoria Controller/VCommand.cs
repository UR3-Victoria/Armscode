using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Team_Victoria_Controller
{
    class VCommand
    {
        public const String DoAScan = "Scan for shapes";
        public const String SortAShape = "Sort a shape";

        public const String Wait = "Wait for some number of seconds:";
        public const String WaitForStop = "Wait until move complete...TIMOUT:";

        //public const String WaitForA = "Wait until base movement is 50% complete"; 
        //public const String WaitForB = "Wait until shoulder movement is 50% complete";
        //public const String WaitForC = "Wait until elbow movement is  50% complete";
    }

    /*
    class VCommand
    {
        public const String DoAScan = "Scan for shapes";
        public const String SortAShape = "Sort a shape";

        public const String Wait = "Wait for some number of seconds:";
        public const String WaitForStop = "Wait until move complete...TIMOUT:";

        public const String SetLights = "Set lights to:"; //L (1 or 0)
        public const String SetVacuum = "Set vacuum motor to:"; //V (1 or 0)

        public const String ONLINE = "Online";
        public const String OFFLINE = "Offline";

        public const String MotorA_GOTO = "Rotate base to: ";  //A (angle as float)
        public const String MotorB_GOTO = "Rotate shoulder to: ";  //B
        public const String MotorC_GOTO = "Rotate elbow to: ";  //C
        public const String MotorD_GOTO = "Rotate wrist to: ";  //D

        public const String WaitForA = "Wait until base movement is 50% complete";
        public const String WaitForB = "Wait until shoulder movement is 50% complete";
        public const String WaitForC = "Wait until elbow movement is  50% complete";
    }
     */
}
