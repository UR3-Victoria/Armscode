using Emgu.CV.Structure;
using System;
using System.Drawing;

namespace Team_Victoria_Controller
{
    class VShape
    {
        public Point[] _shape;

        public double[] sides;

        //public bool _isEquilateral;
        //public bool _isRegular;
        public bool _isSquare;
        public bool _isTriangle;

        public double scale;
        public int health;
        public Bgr ID;

        public VShape(Point[] shape, Bgr newID)
        {
            _shape = shape;
            _isSquare = false;
            _isTriangle = false;

            health = 10;
            ID = newID;


            int len = _shape.Length;
            sides = new double[len];

            for (int i = 0; i < len - 1; i++)
            {
                sides[i] = Distance(_shape[i], _shape[i + 1]);
            }
            sides[len - 1] = Distance(_shape[len - 1], _shape[0]);

            scale = Math.Round(FindScale(), 2);

            if (IsRegular() && IsEquilateral())
            {
                if (_shape.Length == 3)
                    _isTriangle = true;
                else if (_shape.Length == 4)
                    _isSquare = true;
            }
        }


        //========================================================================================

        public Point FindCenter()
        {
            int x, y;
            x = 0;
            y = 0;

            foreach (Point p in _shape)
            {
                x += p.X;
                y += p.Y;
            }

            x = x / _shape.Length;
            y = y / _shape.Length;

            return new Point(x, y);
        }

        public bool IsAnalogous(VShape other)
        {
            return (Distance(FindCenter(), other.FindCenter()) < 32 && _shape.Length == other._shape.Length);
        }

        //========================================================================================


        private double FindScale()
        {
            double perimeter = 0;

            foreach (double side in sides)
            {
                perimeter += side;
            }

            return perimeter / _shape.Length;
        }

        private bool IsEquilateral()
        {
            int len = _shape.Length;

            bool _isEquilateral = true;

            for (int i = 0; i < len - 1; i++)
            {
                if (Math.Abs(sides[i] - sides[i + 1]) > scale / 8)
                {
                    _isEquilateral = false;
                }
            }

            if (Math.Abs(sides[len - 1] - sides[0]) > scale / 8)
            {
                _isEquilateral = false;
            }

            return _isEquilateral;
        }

        private bool IsRegular()
        {
            int len = _shape.Length;
            double[] ds = new double[len];
            bool _isRegular = true;

            for (int i = 0; i < len; i++)
            {
                ds[i] = Distance(_shape[i], FindCenter());
            }

            for (int i = 0; i < len - 1; i++)
            {
                if (Math.Abs(ds[i] - ds[i + 1]) > scale / 8)
                {
                    _isRegular = false;
                }
            }

            if (Math.Abs(ds[len - 1] - ds[0]) > scale / 8)
            {
                _isRegular = false;
            }


            return _isRegular;
        }

        private double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

    }
}
