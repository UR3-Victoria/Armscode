using System;

namespace Team_Victoria_Controller
{
    class Geometry
    {

        public static double Pyth(double _x, double _y)
        {
            double xyD = Math.Pow(_x, 2) + Math.Pow(_y, 2);
            return Math.Sqrt(xyD);
        }

        public static double PythRev(double _z, double _o)
        {
            double zoEdge = Math.Pow(_z, 2) - Math.Pow(_o, 2);
            return Math.Sqrt(zoEdge);
        }

        public static double LawOfCosines(double opposite, double adj1, double adj2)
        {
            double top = -1 * Math.Pow(opposite, 2) + Math.Pow(adj1, 2) + Math.Pow(adj2, 2);
            double bottom = 2 * adj1 * adj2;

            return Math.Acos(top / bottom);
        }

        public static double LawOfCosinesRev(double angle, double adj1, double adj2)
        {
            return Math.Sqrt(Math.Pow(adj1, 2) + Math.Pow(adj2,2) - (2 * adj1 * adj2 * Math.Cos(angle)));
        }

        public static double RtD(double rads)
        {
            return (rads * 360) / (2 * Math.PI);
        }

        public static double DtoR(double degs)
        {
            return (degs * 2 * Math.PI) / 360;
        }

        public static double XYtoTheta(double x, double y)
        {
            if (x > 0)
                return Math.Atan((double)y / (double)x);
            else if (x < 0)
                return Math.PI + Math.Atan((double)y / (double)x);
            else
                return 0;
        }
        public static double XYtoR(double x, double y)
        {
            return Pyth(x, y);
        }

        public static double PolarToX(double theta, double r)
        {
            return Math.Cos(theta) * r;
        }
        public static double PolarToY(double theta, double r)
        {
            return Math.Sin(theta) * r;
        }

    }
}
