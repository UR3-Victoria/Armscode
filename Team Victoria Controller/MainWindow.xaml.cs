using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Media;
using System.IO.Ports;

using MediaColor = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;

//QueuePointLuis
//RobotDef.cs
//VPoint.cs


namespace Team_Victoria_Controller
{

    public partial class MainWindow : Window
    {
        public enum Connection { Unknown, Disconnected, Connected};

        public Connection statusEve;
        public Connection statusLuis;
        public Connection statusCam;

        private Capture _cap = null;
        private DispatcherTimer capTimer;
        private DispatcherTimer scanTimer;
        private DispatcherTimer progTimer;

        public Boolean _scanning;
        public int _waiting;
        public Boolean _moving; //not really true, only TRUE for waiting for move

        public static Boolean A_stable;
        public static Boolean B_stable;
        public static Boolean C_stable;


        ShapeDetection shapeDetector;


        List<VPoint> points = new List<VPoint>();

        Queue<string> commands1 = new Queue<string>();
        Queue<string> commands2 = new Queue<string>();

        SerialPort EvePort; // = new SerialPort("COM3");
        SerialPort LuisPort;

        //some program points
        VPoint eveCapturePoint;
        VPoint eveHomePoint;
        VPoint squarePoint;
        VPoint trianglePoint;


        public MainWindow()
        {
            InitializeComponent();

            statusEve = Connection.Unknown;
            statusLuis = Connection.Unknown;
            statusCam = Connection.Unknown;

            if(MessageBox.Show("Attempt communication with " + EveDef.name + "?", "Establishing Connection...", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                statusEve = Connection.Disconnected;
            }

            if (MessageBox.Show("Attempt communication with " + LuisDef.name + "?", "Establishing Connection...", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                statusLuis = Connection.Disconnected;
            }

            while(statusEve == Connection.Unknown || statusLuis == Connection.Unknown)
            {
                string[] ports = SerialPort.GetPortNames();

                switch (ports.Length)
                {
                    case 1:
                        if (statusEve == Connection.Unknown)
                            StartEveComm(ports[0]);
                        else
                            StartLuisComm(ports[0]);
                        break;
                    case 2:
                        ProbePort(ports[0]);
                        if (MessageBox.Show("Did " + EveDef.name + "'s lights turn on?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            StartEveComm(ports[0]);
                            StartLuisComm(ports[1]);
                            EvePort.WriteLine("L0");
                        }
                        else
                        {
                            StartEveComm(ports[1]);
                            StartLuisComm(ports[2]);
                        }
                        break;
                    default:
                        if (MessageBox.Show("No connections found. Refresh?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                        {
                            statusEve = Connection.Disconnected;
                            statusLuis = Connection.Disconnected;
                        }
                        break;
                }

            }

            while(statusCam == Connection.Unknown)
            {
                try
                {
                    _cap = new Capture(1);
                    statusCam = Connection.Connected;
                }
                catch
                {
                    if (MessageBox.Show("No camera found. Refresh?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        statusCam = Connection.Disconnected;
                        _cap = new Capture(0);
                    }
                }
            }
            


   
            PrepareTimers();

            shapeDetector = new ShapeDetection();

            eveCapturePoint = new VPoint(Geometry.DtoR(0), Geometry.DtoR(90), Geometry.DtoR(180), Geometry.DtoR(90));
            eveCapturePoint.ID = new Bgr(64, 64, 64);
            eveCapturePoint.tag = "Camera capture position";

            eveHomePoint = new VPoint(Geometry.DtoR(0), Geometry.DtoR(90), Geometry.DtoR(150), Geometry.DtoR(90));
            eveHomePoint.ID = new Bgr(198, 64, 64);
            eveHomePoint.tag = "Home Position";

            squarePoint = new VPoint((int)Geometry.PolarToX(Geometry.DtoR(150), 350), (int)Geometry.PolarToY(Geometry.DtoR(150), 350), 30);
            squarePoint.ID = new Bgr(64, 64, 198);
            squarePoint.tag = "Destination for sorted squares";

            trianglePoint = new VPoint((int)Geometry.PolarToX(Geometry.DtoR(30), 350), (int)Geometry.PolarToY(Geometry.DtoR(30), 350), 30);
            trianglePoint.ID = new Bgr(64, 198, 64);
            trianglePoint.tag = "Destination for sorted triangles";

            points.Add(eveCapturePoint);
            points.Add(eveHomePoint);
            points.Add(squarePoint);
            points.Add(trianglePoint);

            DisplayPoints();

        }

        //============================================ UTILITY ================================================


        private void PrepareTimers()
        {
            //prepare capture timer
            capTimer = new DispatcherTimer();
            capTimer.Interval = TimeSpan.FromMilliseconds(50); //20fps
            capTimer.Tick += CaptureFrame;

            //prepare scan timer
            scanTimer = new DispatcherTimer();
            scanTimer.Interval = TimeSpan.FromMilliseconds(40); //25 ticks per second
            scanTimer.Tick += ScanTick;

            //prepare program timer
            progTimer = new DispatcherTimer();
            progTimer.Interval = TimeSpan.FromMilliseconds(50); //20 ticks per second
            progTimer.Tick += ProgramTick;
        }

        private void DisplayPoints()
        {

            PointStack.Children.Clear();

            foreach (VPoint point in points)
            {
                TextBlock vpointBlock = new TextBlock();

                vpointBlock.Text = point.tag + Environment.NewLine +
                    "     X: " + point.x + " ,   Y: " + point.y + " ,   Z: " + point.z + Environment.NewLine + "EVE: " +
                    "     A: " + Math.Round(Geometry.RtD(point.Eve.A), 2) +
                    " ,   B: " + Math.Round(Geometry.RtD(point.Eve.B), 2) + 
                    " ,   C: " + Math.Round(Geometry.RtD(point.Eve.C), 2) +
                    " ,   D: " + Math.Round(Geometry.RtD(point.Eve.D), 2) + Environment.NewLine + "LUIS: " +

                    "     A: " + Math.Round(Geometry.RtD(point.Luis.A), 2) +
                    " ,   B: " + Math.Round(Geometry.RtD(point.Luis.B), 2) +
                    " ,   C: " + Math.Round(Geometry.RtD(point.Luis.C), 2);

                vpointBlock.Foreground = MediaBrushes.White;
                vpointBlock.Background = new SolidColorBrush(MediaColor.FromRgb((byte)point.ID.Red, (byte)point.ID.Green, (byte)point.ID.Blue));
                vpointBlock.Margin = new Thickness(16, 4, 16, 4);
                vpointBlock.Padding = new Thickness(24, 4, 8, 4);


                PointStack.Children.Add(vpointBlock);
            }


        }
        private void DisplayCommands()
        {
            CommandQueue1.Text = string.Join(Environment.NewLine, commands1.ToArray());
            CommandQueue2.Text = string.Join(Environment.NewLine, commands2.ToArray());
        }

        private void SetProgress(double progress)
        {
            bar_Progress.Value = progress;
        }
        private void SetStatus(string status)
        {
            txt_Status.Text = status;
        }

        private double GetProgress()
        {
            return bar_Progress.Value;
        }

        //============================================= MAIN ==============================================

        private void CaptureFrame(object s, EventArgs a)
        {
            using (Image<Bgr, Byte> frame = _cap.QueryFrame())
            {
                if (frame != null)
                {
                    ImageRaw.Source = BitmapSourceConvert.ToBitmapSource(frame.Rotate(90, new Bgr(0, 0, 0), false));
                    shapeDetector.SetSource(frame.Rotate(90, new Bgr(0, 0, 0), false));
                    if (_scanning)
                    {
                        shapeDetector.FindShapes();
                        ImagePost.Source = BitmapSourceConvert.ToBitmapSource(shapeDetector.releasePost());
                    }

                }
            }
        }
        private void ScanTick(object s, EventArgs a)
        {
            if (GetProgress() < 100)
            {
                SetProgress(GetProgress() + 4);
            }
            else
            {
                ScanStop();
            }
        }
        private void ProgramTick(object s, EventArgs a)
        {
            DisplayCommands();

            if (_scanning)
            {
                SetStatus("Scanning");
                return;
            }

            if (_waiting > 0)
            {
                _waiting--;

                SetStatus("Waiting ... " + _waiting.ToString());


                if (A_stable && B_stable && C_stable && _moving)
                {
                    _waiting = 0;
                    _moving = false;
                }
                    

                if(_waiting == 1)
                {
                    //MessageBox.Show("Movement timed out");
                    //progTimer.Stop();
                }

                return;
            }

            //quit if empty
            if (commands1.Count == 0 && commands2.Count == 0)
                return;


            //==============EVE'S COMMANDS====================


            SetStatus(commands1.Peek());

            switch (commands1.Peek())
            {
                case VCommand.WaitForStop:

                    commands1.Dequeue();
                    _waiting = (1000 / progTimer.Interval.Milliseconds) * Int32.Parse(commands1.Dequeue());
                    _moving = true;
                    break;

                case VCommand.Wait:

                    commands1.Dequeue();
                    _waiting = (1000 / progTimer.Interval.Milliseconds) * Int32.Parse(commands1.Dequeue());
                    break;

                case VCommand.DoAScan:
                    commands1.Dequeue();
                    capTimer.Start();
                    ScanStart();
                    break;

                case VCommand.SortAShape:
                    commands1.Dequeue();


                    VPoint shape2 = points.Find(x => x.tag == "Square");

                    if (shape2 == null)
                    {
                        shape2 = points.Find(x => x.tag == "Triangle");

                        if (shape2 == null)
                        {
                            ProgramStart();
                            DisplayCommands();
                            return;
                        }
                    }



                    if (shape2.tag == "Triangle")
                    {
                        VPoint shape = new VPoint(shape2.x, shape2.y, -5);
                        shape.tag = shape2.tag;

                        VPoint above_shape = new VPoint(shape2.x, shape2.y, 30);

                        points.Remove(shape2);


                        QueuePointEve(above_shape, false);
                        commands1.Enqueue(VCommand.WaitForStop);
                        commands1.Enqueue("10");


                        commands1.Enqueue("P1");


                        QueuePointEve(shape, true);
                        commands1.Enqueue(VCommand.WaitForStop);
                        commands1.Enqueue("10");

                        QueuePointEve(eveHomePoint, false);
                        commands1.Enqueue(VCommand.WaitForStop);
                        commands1.Enqueue("10");

                        if (shape.tag == "Square")
                            QueuePointEve(squarePoint, true);
                        if (shape.tag == "Triangle")
                            QueuePointEve(trianglePoint, true);
                        commands1.Enqueue(VCommand.WaitForStop);
                        commands1.Enqueue("10");

                        commands1.Enqueue("P0");

                        commands1.Enqueue(VCommand.Wait);
                        commands1.Enqueue("1");

                        QueuePointEve(eveHomePoint, false);
                        commands1.Enqueue(VCommand.WaitForStop);
                        commands1.Enqueue("10");

                        points.Remove(shape);
                        DisplayPoints();

                        commands1.Enqueue(VCommand.SortAShape);
                    }

                    if(shape2.tag == "Square")
                    {
                        //These four lines create a shape to process.
                        //If you need to shift the position (xy) of the percieved shape, do it here
                        VPoint shape = new VPoint(shape2.x, shape2.y); //ex. new VPoint(shape2.x - 30, shape2.y, 10)
                        shape.tag = shape2.tag;

                        points.Remove(shape2); //remove the shape from the list & update display
                        DisplayPoints();

                        //Here is where you would put the rest of the path code.
                        //Queue up all the commands needed to get to the shape,
                        //pick it up, and drop it at the destination.

                        //Queue the destination point like: QueuePointLuis(squarePoint);

                        //Also, use either QueuePointLuis for defined points, or commands2 for induvidual commands


                        QueuePointLuis(shape);

                        commands2.Enqueue("M1"); //Electromagnet on

                        commands2.Enqueue(""); //Lift

                        QueuePointLuis(squarePoint); //Go to square destination

                        commands2.Enqueue("M0"); //Electromagnet off

                        commands2.Enqueue(""); //Pounce position




                        
                        //this forces the program to prepare another shape for either robot
                        //(commands1 manditory)
                        //not perfect solution, patch later
                        commands1.Enqueue(VCommand.SortAShape);  
                    }

                    break;

                default:
                    String command = commands1.Dequeue();
                    
                    if(command[0] == 'A' || command[0] == 'B' || command[0] == 'C')
                    {
                        A_stable = false;
                        B_stable = false;
                        C_stable = false;
                    }
                        
                    EvePort.WriteLine(command);

                    break;
            }




            //==============LUIS' COMMANDS====================


            SetStatus(commands2.Peek());

            switch (commands2.Peek())
            {

                case VCommand.Wait:

                    commands2.Dequeue();
                    _waiting = (1000 / progTimer.Interval.Milliseconds) * Int32.Parse(commands2.Dequeue());
                    break;

                default:
                    String command = commands2.Dequeue();
                    LuisPort.WriteLine(command);

                    break;
            }


        }

        private void ScanStart()
        {
            scanTimer.Start();
            SetProgress(0);
            _scanning = true;
        }
        private void ScanStop()
        {
            scanTimer.Stop();
            SetProgress(0);
            _scanning = false;

            foreach (VShape shape in shapeDetector.shapes)
            {
                points.Add(new VPoint(shape, eveCapturePoint));
            }

            if(shapeDetector.shapes.Count == 0)
            {
                commands1.Clear();
                commands2.Clear();
                DisplayCommands();
                EvePort.WriteLine("M0");
                SetStatus("Done!");
                MessageBox.Show("All shapes sorted");

                progTimer.Stop();
            }

            DisplayPoints();
        }

        private void ProgramStart()
        {

            if (!points.Contains(eveCapturePoint))
                points.Add(eveCapturePoint);
            if (!points.Contains(eveHomePoint))
                points.Add(eveHomePoint);
            if (!points.Contains(squarePoint))
                points.Add(squarePoint);
            if (!points.Contains(trianglePoint))
                points.Add(trianglePoint);

            DisplayPoints();


            QueuePointEve(eveCapturePoint, false);

            commands1.Enqueue(VCommand.WaitForStop);
            commands1.Enqueue("5");

            commands1.Enqueue(VCommand.DoAScan);

            commands1.Enqueue(VCommand.Wait);
            commands1.Enqueue("2");


            commands1.Enqueue(VCommand.SortAShape);

            EvePort.WriteLine("M1");


        }

        private void QueuePointEve(VPoint point, Boolean down)
        {
            commands1.Enqueue("D" + Math.Round(Geometry.RtD(point.Eve.D), 2).ToString());

            if(down)
            {
                commands1.Enqueue("B" + Math.Round(Geometry.RtD(point.Eve.B), 2).ToString());
                commands1.Enqueue("C" + Math.Round(Geometry.RtD(point.Eve.C), 2).ToString());
            }
            else
            {
                commands1.Enqueue("C" + Math.Round(Geometry.RtD(point.Eve.C), 2).ToString());
                commands1.Enqueue("B" + Math.Round(Geometry.RtD(point.Eve.B), 2).ToString());
            }

            commands1.Enqueue("A" + Math.Round(Geometry.RtD(point.Eve.A), 2).ToString()); 
        }
        private void QueuePointLuis(VPoint point)
        {
            //NEEDS CONTENT

            //Make sure to know if you are using radians or degrees
            //suggestion:

            commands2.Enqueue("A" + Math.Round(point.Luis.A, 2).ToString());
            commands2.Enqueue("B" + Math.Round(point.Luis.B, 2).ToString());
            commands2.Enqueue("C" + Math.Round(point.Luis.C, 2).ToString());
        }


        //========================================= COMMUNICATION ===========================================

        private void ProbePort(String port)
        {
            EvePort = new SerialPort(port);
            EvePort.WriteLine("L1");
        }

        private void StartEveComm(String port)
        {
            statusEve = Connection.Connected;
            EvePort = new SerialPort(port);
            EvePort.DataReceived += new SerialDataReceivedEventHandler(DataReceived);
            EvePort.Open();
        }
        private void StartLuisComm(String port)
        {
            statusLuis = Connection.Connected;
            LuisPort = new SerialPort(port);
            LuisPort.DataReceived += new SerialDataReceivedEventHandler(DataReceived);
            LuisPort.Open();
        }

        private static void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadLine();

            indata = indata.ToString().TrimEnd(Environment.NewLine.ToCharArray());

            if (indata.Equals("Motor A Stable"))
                A_stable = true;
            if (indata.Equals("Motor B Stable"))
                B_stable = true;
            if (indata.Equals("Motor C Stable"))
                C_stable = true;

        }


        //========================================= MENU EVENTS ================================================

        private void StartCapture(object sender, RoutedEventArgs e)
        {
            capTimer.Start();
        }
        private void StopCapture(object sender, RoutedEventArgs e)
        {
            capTimer.Stop();
        }

        private void ManualScan(object sender, RoutedEventArgs e)
        {
            if (capTimer.IsEnabled)
                ScanStart();
            else
                MessageBox.Show("The capture is not enabled; a scan cannot be performed. ", "Capture Disabled");
        }

        private void StartProgram(object sender, RoutedEventArgs e)
        {
            ProgramStart();
            progTimer.Start();
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuMotorsOn(object sender, RoutedEventArgs e)
        {
            EvePort.WriteLine("M1");
        }
        private void MenuMotorsOff(object sender, RoutedEventArgs e)
        {
            EvePort.WriteLine("M0");
        }
        private void MenuJoystickOn(object sender, RoutedEventArgs e)
        {
            EvePort.WriteLine("J1");
        }
        private void MenuJoystickOff(object sender, RoutedEventArgs e)
        {
            EvePort.WriteLine("J0");
        }
        private void MenuPumpOn(object sender, RoutedEventArgs e)
        {
            EvePort.WriteLine("P1");
        }
        private void MenuPumpOff(object sender, RoutedEventArgs e)
        {
            EvePort.WriteLine("P0");
        }
        private void MenuLightsOn(object sender, RoutedEventArgs e)
        {
            EvePort.WriteLine("L1");
        }
        private void MenuLightsOff(object sender, RoutedEventArgs e)
        {
            EvePort.WriteLine("L0");
        }
        private void GoToHome(object sender, RoutedEventArgs e)
        { 

        }
        private void GoToInput(object sender, RoutedEventArgs e)
        {

        }



    }
}

