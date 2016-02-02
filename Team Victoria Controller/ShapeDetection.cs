using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Documents;
using System.Windows;

using DrawingPoint = System.Drawing.Point;

namespace Team_Victoria_Controller
{
    class ShapeDetection
    {
        private Image<Bgr, Byte> source;
        private Image<Gray, Byte> working;
        private Image<Bgr, Byte> post;

        public List<VShape> shapes = new List<VShape>();

        public ShapeDetection()
        {
            
        }

        public void SetSource(Image<Bgr, Byte> input)
        {
            source = input;
            post = new Image<Bgr, byte>(480, 640, new Bgr(32, 32, 32));
        }

        public int FindShapes()
        {
            source = source.Resize(480, 640, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

            Random rnd = new Random();

            working = source.Convert<Gray, Byte>();

            working._SmoothGaussian(3);
            working = working.Canny(100, 60);

            Bgr red = new Bgr(0, 0, 255);
            Bgr green = new Bgr(0, 255, 0);
            Bgr blue = new Bgr(255, 0, 0);


            using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
                for (Contour<DrawingPoint> contours = working.FindContours(); contours != null; contours = contours.HNext)
                {
                    if (contours.Area > 1000)
                    {
                        
                        VShape newShape = new VShape(contours.ApproxPoly(contours.Perimeter * 0.04, storage).ToArray(), new Bgr(rnd.Next(32, 255), rnd.Next(32, 255), rnd.Next(32, 255)));

                        if (newShape._isTriangle || newShape._isSquare)
                        {
                            bool IsAdded = false;

                            //MessageBox.Show("Found something");

                            for (int i = 0; i < shapes.Count; i++)
                            {
                                if (shapes[i].IsAnalogous(newShape))
                                {
                                    newShape.ID = shapes[i].ID;
                                    shapes[i] = newShape;
                                    //shapes.Add(newShape);
                                    IsAdded = true;
                                    //break;
                                }
                            }

                            if (!IsAdded)
                            {
                                shapes.Add(newShape);
                            }
                        }
                    }
                }

            List<VShape> damnedShapes = new List<VShape>();

            foreach (VShape shape in shapes)
            {
                if (shape.health == 0)
                {
                    damnedShapes.Add(shape);
                    continue;
                }
                else
                    shape.health -= 1;

                //post.Draw(new CircleF(shape.FindCenter(), shape.health * (float)shape.scale / 50), shape.ID, 0);
                //////frame.Draw("          Inches away: " + Math.Round((1750/(shape.scale+7))-1.0,2).ToString() + ">" + shape.scale.ToString(), ref newFont, shape.FindCenter(), new Bgr(255, 64, 128));
                //post.Draw("       Center X: " + shape.FindCenter().X.ToString(), ref newFont, new Point(shape.FindCenter().X, shape.FindCenter().Y - 8), new Bgr(255, 64, 128));
                //post.Draw("       Center Y: " + shape.FindCenter().Y.ToString(), ref newFont, new Point(shape.FindCenter().X, shape.FindCenter().Y + 8), new Bgr(255, 64, 128));

                post.FillConvexPoly(shape._shape, shape.ID);
                //post.Draw(new CircleF(new PointF(20, 20), 10), new Bgr(128,128,128), 0);

            }

            foreach (VShape damnedShape in damnedShapes)
            {
                shapes.Remove(damnedShape);
            }


            return shapes.Count;

        }

        public Image<Bgr, Byte> releasePost()
        {
            return post;
        }
        
    }
}
