using Emgu.CV.Structure;
using System.Drawing;
using System;

namespace Team_Victoria_Controller
{
    public struct robotCords
    {
        public double A;
        public double B;
        public double C;
        public double D;
    }

    class VPoint
    {
        
        public string tag;
        public Bgr ID;

        public robotCords Eve;
        public robotCords Marty;

        public int x;
        public int y;
        public int z;

        public VPoint(VShape shape, VPoint camera)
        {
            ID = shape.ID;

            if (shape._isSquare)
                tag = "Square";
            if (shape._isTriangle)
                tag = "Triangle";

            Point camXY = shape.FindCenter();

            //Right so this is where things get ugly. Camera X and Y are divided by the total to get a percentage.
            //50% is subtracted to make the coordinant a percentage of total width relative to middle.
            //This is multiplied by the Camera constant (viewing angle) times the height of the 

            double xToCam = ((double)camXY.X / 480 - 0.5) * (EveDef.CameraWidthOverZ * 1.00000 * (camera.z + EveDef.WristToEnd_Z - EveDef.WristToCam_Z));
            double yToCam = ((double)camXY.Y / 640 - 0.5) * (EveDef.CameraWidthOverZ * 1.33333 * (camera.z + EveDef.WristToEnd_Z - EveDef.WristToCam_Z));
            
            double rToCam = Geometry.XYtoR(xToCam, yToCam);
            double thetaToCam = Geometry.XYtoTheta(xToCam, yToCam);

            //these are the distance from the center of camera to the object in correctly oriented mm
            int dX = (int)Math.Round(Geometry.PolarToX(thetaToCam + camera.Eve.A, rToCam));
            int dY = (int)Math.Round(Geometry.PolarToY(thetaToCam + camera.Eve.A, rToCam));

            //the real camera position x and y, plus the dX and dY to the object, minus the extra x and y to get to the end-effector from the camera
            x = camera.x + dX - (int)Math.Round(Geometry.PolarToX(camera.Eve.A + Math.PI / 2, EveDef.WristToEnd_L));
            y = camera.y + dY - (int)Math.Round(Geometry.PolarToY(camera.Eve.A + Math.PI / 2, EveDef.WristToEnd_L));
  
            z = 0;

            TransformXYZtoEVE();
            TransformXYZtoMARTY();

            Eve.D = Math.PI / 2; //NEEDS MORE
            Marty.D = 0;
        }
        public VPoint(int _x, int _y, int _z)
        {
            x = _x;
            y = _y;
            z = _z;

            TransformXYZtoEVE();
            TransformXYZtoMARTY();

            Eve.D = Math.PI / 2;
            Marty.D = 0; 
        }
        public VPoint(int _x, int _y)
        {
            x = _x;
            y = _y;
            z = 0;

            TransformXYZtoEVE();
            TransformXYZtoMARTY();

            Eve.D = Math.PI / 2;
            Marty.D = 0;
        }
        public VPoint(double _root, double _shoulder, double _elbow, double _wrist)
        {
            Eve.A = _root;
            Eve.B = _shoulder;
            Eve.C = _elbow;
            Eve.D = _wrist;

            TransformEVEtoXYZ();
        }

        private void TransformXYZtoEVE()
        {

            Eve.A = Geometry.XYtoTheta(x, y) - (Math.PI / 2);
            double r = Geometry.XYtoR(x, y);

            double d = Geometry.Pyth(z + EveDef.WristToEnd_Z - EveDef.FloorToRoot_Z, r - EveDef.WristToEnd_L);        

            double dTheta = Math.Acos((r - EveDef.WristToEnd_L)/ d);

            if ((z + EveDef.WristToEnd_Z - EveDef.FloorToRoot_Z) < 0)
                dTheta = dTheta * -1;

            Eve.B = Geometry.LawOfCosines(EveDef.ElbowToWrist_L, EveDef.RootToElbow_L, d) + dTheta;

            Eve.C = Geometry.LawOfCosines(d, EveDef.ElbowToWrist_L, EveDef.RootToElbow_L) + Eve.B;
        }
        private void TransformXYZtoMARTY()
        {

            int Lx = -x;
            int Ly = MartyDef.DistanceFromEve - y;

            double ang1 = (Geometry.XYtoTheta(Lx, Ly) * 180.0) / Math.PI;
            //ang1 += 90;
            //ang1 = (180 - (ang1));
            Marty.C = ang1;

            double linka = MartyDef.RootToElbow_L;
            double linkb = MartyDef.ElbowToWrist_L;

            double r = (Math.Sqrt(Math.Pow(Lx, 2) + Math.Pow(Ly, 2)) - MartyDef.WristToEnd_L);
            double dh = MartyDef.FloorToRoot_Z - MartyDef.WristToEnd_Z;
            double d = Math.Sqrt((r * r) + (dh * dh));
            double theta2 = Math.Acos((linka * linka + linkb * linkb - d * d) / (2 * linka * linkb)) * (180 / Math.PI);
            double theta1c = Math.Acos((d * d + linka * linka - linkb * linkb) / (2 * d * linka)) * (180 / Math.PI);
            double thetac = Math.Atan(r / dh) * (180 / Math.PI);
            double theta1 = theta1c + thetac - 90;

            Marty.B = theta2;
            Marty.A = theta1 - 15;

        }

        private void TransformEVEtoXYZ()
        {
            double d = Geometry.LawOfCosinesRev(Eve.C - Eve.B, EveDef.RootToElbow_L, EveDef.ElbowToWrist_L);

            double r = Math.Cos(Eve.B - Geometry.LawOfCosines(EveDef.ElbowToWrist_L, EveDef.RootToElbow_L, d)) * d + EveDef.WristToEnd_L;

            z = (int)Math.Round(Math.Sin(Eve.B - Geometry.LawOfCosines(EveDef.ElbowToWrist_L, EveDef.RootToElbow_L, d)) * d + EveDef.FloorToRoot_Z - EveDef.WristToEnd_Z);

            x = (int)Math.Round(Math.Sin(Eve.A) * r);
            y = (int)Math.Round(Math.Cos(Eve.A) * r);
        }


    }
}
