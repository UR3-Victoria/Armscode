using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Media;
using System.IO.Ports;
using System.Threading;

using MediaColor = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;

//QueuePointMarty
//RobotDef.cs
//VPoint.cs


namespace Team_Victoria_Controller
{

    public partial class MainWindow : Window
    {
        public enum Connection { Unknown, Disconnected, Connected};

        public Connection statusEve;
        public Connection statusMarty;
        public Connection statusCam;

        private Capture _cap = null;
        private DispatcherTimer capTimer;
        private DispatcherTimer scanTimer;
        private DispatcherTimer progTimer;

        public Boolean _scanning;
        public int _waiting;
        public Boolean _moving; //not really true, only TRUE for waiting for move

        public Boolean _autoCommand;
        public Boolean _doCommand;

        public static Boolean A_stable;
        public static Boolean B_stable;
        public static Boolean C_stable;


        public static Boolean _eveID;


        ShapeDetection shapeDetector;


        List<VPoint> points = new List<VPoint>();

        Queue<string> commands1 = new Queue<string>();
        Queue<string> commands2 = new Queue<string>();

        SerialPort EvePort;
        SerialPort MartyPort;

        //some program points
        VPoint eveCapturePoint;
        VPoint eveHomePoint;
        VPoint squarePoint;
        VPoint trianglePoint;
        VPoint testPoint;
        VPoint testPoint2;


        public MainWindow()
        {
            InitializeComponent();

            statusEve = Connection.Unknown;
            statusMarty = Connection.Unknown;
            statusCam = Connection.Unknown;

            _autoCommand = true;


            EvePort = new SerialPort();
            MartyPort = new SerialPort();

            EvePort.DataReceived += new SerialDataReceivedEventHandler(DataReceived);
            MartyPort.DataReceived += new SerialDataReceivedEventHandler(DataReceived);

            MartyPort.BaudRate = 115200;

            /*
            if(MessageBox.Show("Attempt communication with " + EveDef.name + "?", "Establishing Connection...", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                statusEve = Connection.Disconnected;
            }

            if (MessageBox.Show("Attempt communication with " + MartyDef.name + "?", "Establishing Connection...", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                statusMarty = Connection.Disconnected;
            }
             */

            string evePortName = string.Empty;
            string martyPortName = string.Empty;

            
            while(statusEve == Connection.Unknown || statusMarty == Connection.Unknown)
            {
                string[] ports = SerialPort.GetPortNames();

                cmbSelect1.ItemsSource = ports;

                switch (ports.Length)
                {
                    case 1:
                        StartEveComm(ports[0]);
                        statusMarty = Connection.Disconnected;
                        if (!ProbeEve())
                        {
                            StartMartyComm(ports[0]);
							martyPortName = ports[0];
                            statusEve = Connection.Disconnected;
                        }
						else
                        {
                            evePortName = ports[0];
                        }
                        break;
                    case 2:
                        StartEveComm(ports[0]);
                        if (ProbeEve())
                        {
                            StartMartyComm(ports[1]);
                            evePortName = ports[0];
                            martyPortName = ports[1];
                        }
                        else
                        {
                            StartEveComm(ports[1]);
                            StartMartyComm(ports[0]);
                            evePortName = ports[1];
                            martyPortName = ports[0];
                        }
                        break;
                    default:
                        if (MessageBox.Show("No robots found. Refresh?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                        {
                            statusEve = Connection.Disconnected;
                            statusMarty = Connection.Disconnected;
                        }
                        else
                        {
                            statusEve = Connection.Unknown;
                            statusMarty = Connection.Unknown;
                        }
                        break;
                }

            }
            
            cmbSelect1.ItemsSource = SerialPort.GetPortNames();
            cmbSelect2.ItemsSource = SerialPort.GetPortNames();

            cmbSelect1.Text = evePortName;
            cmbSelect2.Text = martyPortName;
            lblStatus1.Content = statusEve.ToString();
            lblStatus2.Content = statusMarty.ToString();

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
            eveCapturePoint.immune = true;

            eveHomePoint = new VPoint(Geometry.DtoR(0), Geometry.DtoR(90), Geometry.DtoR(150), Geometry.DtoR(90));
            eveHomePoint.ID = new Bgr(198, 64, 64);
            eveHomePoint.tag = "Home Position";
            eveHomePoint.immune = true;

            //squarePoint = new VPoint((int)Geometry.PolarToX(Geometry.DtoR(150), 350), (int)Geometry.PolarToY(Geometry.DtoR(150), 350), 30);
            squarePoint = new VPoint(-250, 350, 60);
            squarePoint.ID = new Bgr(64, 64, 198);
            squarePoint.tag = "Destination for sorted squares";
            squarePoint.immune = true;

            //trianglePoint = new VPoint((int)Geometry.PolarToX(Geometry.DtoR(30), 350), (int)Geometry.PolarToY(Geometry.DtoR(30), 350), 30);
            trianglePoint = new VPoint(-250, 500, 60);
            trianglePoint.ID = new Bgr(64, 198, 64);
            trianglePoint.tag = "Destination for sorted triangles";
            trianglePoint.immune = true;

            testPoint = new VPoint(0, 400, 0);
            testPoint.ID = new Bgr(200, 100, 200);
            testPoint.tag = "Triangle";

            testPoint2 = new VPoint(0, 400, 0);
            testPoint2.ID = new Bgr(100, 200, 200);
            testPoint2.tag = "Square";

            points.Add(eveCapturePoint);
            points.Add(eveHomePoint);
            points.Add(squarePoint);
            points.Add(trianglePoint);
            //points.Add(testPoint);
            //points.Add(testPoint2);

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
                    " ,   D: " + Math.Round(Geometry.RtD(point.Eve.D), 2) + Environment.NewLine + "MARTY: " +

                    "     A: " + Math.Round(point.Marty.A) +
                    " ,   B: " + Math.Round(point.Marty.B) +
                    " ,   C: " + Math.Round(point.Marty.C);

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

            if(Keyboard.IsKeyDown(Key.N))
            {
                _doCommand = true;
            }

            if (_autoCommand == false && _doCommand == false)
            {
                SetStatus("Waiting for 'Do Next Command'");
                return;
            }

            if (_autoCommand == false && _doCommand == true)
            {
                _doCommand = false;
            }

            //quit if empty
            if (commands1.Count == 0 && commands2.Count == 0)
                SortAShape();


            //==============EVE'S COMMANDS====================

            if (commands1.Count != 0 && statusEve == Connection.Connected)
            {
                SetStatus(commands1.Peek());

                switch (commands1.Peek())
                {
                    case VCommand.WaitForStop:

                        commands1.Dequeue();
                        _waiting = (int)((1000 / progTimer.Interval.Milliseconds) * Single.Parse(commands1.Dequeue()));
                        _moving = true;
                        break;

                    case VCommand.Wait:

                        commands1.Dequeue();
                        _waiting = (int)((1000 / progTimer.Interval.Milliseconds) * Single.Parse(commands1.Dequeue()));
                        break;

                    case VCommand.DoAScan:
                        commands1.Dequeue();
                        capTimer.Start();
                        ScanStart();
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

            }




            //==============MARTY's COMMANDS====================


            if (commands2.Count != 0 && statusMarty == Connection.Connected)
            {
                SetStatus(commands2.Peek());

                switch (commands2.Peek())
                {

                    case VCommand.Wait:

                        commands2.Dequeue();
                        _waiting = (1000 / progTimer.Interval.Milliseconds) * Int32.Parse(commands2.Dequeue());
                        break;

                    default:
                        String command = commands2.Dequeue();
                        MartyPort.Write(command);

                        break;
                }
            }
            


        }

        private void SortAShape()
        {

            //FIX ME

            VPoint shape = points.Find(x => x.tag == "Square");

            if (shape == null)
            {
                shape = points.Find(x => x.tag == "Triangle");

                if (shape == null)
                {
                    ProgramStart();
                    DisplayCommands();
                    return;
                }
            }



            if (shape.tag == "Square")
            {
                VPoint on_shape = new VPoint(shape.x, shape.y, shape.z);
                on_shape.tag = shape.tag;

                VPoint above_shape = new VPoint(shape.x, shape.y, 30);

                VPoint near_shape = new VPoint(shape.x, shape.y, 15);



                VPoint far_above_shape = new VPoint(shape.x, shape.y, 200);

                VPoint square_cap_a = new VPoint(squarePoint.Eve.A, far_above_shape.Eve.B, far_above_shape.Eve.C, far_above_shape.Eve.D);

                


                QueuePointEve(above_shape, false);
                commands1.Enqueue(VCommand.WaitForStop);
                commands1.Enqueue("2");



                QueuePointEve(near_shape, false);
                commands1.Enqueue(VCommand.WaitForStop);
                commands1.Enqueue("1");

                commands1.Enqueue("P1");




                QueuePointEve(on_shape, false);
                commands1.Enqueue(VCommand.WaitForStop);
                commands1.Enqueue("1");

                commands1.Enqueue(VCommand.Wait);
                commands1.Enqueue("0.5");




                QueuePointEve(far_above_shape, false);
                commands1.Enqueue(VCommand.WaitForStop);
                commands1.Enqueue("2");

                commands1.Enqueue(VCommand.Wait);
                commands1.Enqueue("0.5");



                QueuePointEve(square_cap_a, false);
                commands1.Enqueue(VCommand.WaitForStop);
                commands1.Enqueue("2");



                if (shape.tag == "Square")
                    QueuePointEve(squarePoint, true);
                if (shape.tag == "Triangle")
                    QueuePointEve(trianglePoint, true);
                commands1.Enqueue(VCommand.WaitForStop);
                commands1.Enqueue("3");

                commands1.Enqueue(VCommand.Wait);
                commands1.Enqueue("0.5");

                commands1.Enqueue("P0");



                commands1.Enqueue(VCommand.Wait);
                commands1.Enqueue("1");




                QueuePointEve(eveCapturePoint, false);

                commands1.Enqueue(VCommand.WaitForStop);
                commands1.Enqueue("2");

                commands1.Enqueue(VCommand.Wait);
                commands1.Enqueue("0.5");

                commands1.Enqueue(VCommand.DoAScan);

                commands1.Enqueue(VCommand.Wait);
                commands1.Enqueue("0.5");

                DisplayPoints();

            }

            if (shape.tag == "Triangle")
            {
                //These four lines create a shape to process.
                //If you need to shift the position (xy) of the percieved shape, do it here
                VPoint on_shape = new VPoint(shape.x, shape.y); //ex. new VPoint(shape2.x - 30, shape2.y, 10)
                on_shape.tag = shape.tag;

                DisplayPoints();

            

                QueuePointMarty(on_shape); //Go to ahape

                commands2.Enqueue("M:1"); //Electromagnet on

                commands2.Enqueue(VCommand.Wait);
                commands2.Enqueue("2");

                commands2.Enqueue("A:100"); //Lift

                commands2.Enqueue(VCommand.Wait);
                commands2.Enqueue("2");

                commands2.Enqueue("B:60");

                commands2.Enqueue(VCommand.Wait);
                commands2.Enqueue("2");

                QueuePointMarty(trianglePoint); //Go to square destination

                commands2.Enqueue("M:0"); //Electromagnet off

                commands2.Enqueue(VCommand.Wait);
                commands2.Enqueue("2");

                commands2.Enqueue("A:142"); //Pounce position

                commands2.Enqueue(VCommand.Wait);
                commands2.Enqueue("2");

                commands2.Enqueue("B:4");

                commands2.Enqueue(VCommand.Wait);
                commands2.Enqueue("2");


                commands2.Enqueue("C:90");

                commands2.Enqueue(VCommand.Wait);
                commands2.Enqueue("2");
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

            foreach(VPoint point in points)
            {
                if (point.immune == false)
                    point.healthy = false;
            }

            foreach (VShape shape in shapeDetector.shapes)
            {
                int i = points.FindIndex(x => x.ID.ToString() == shape.ID.ToString());


                if (i == -1)
                {
                    points.Add(new VPoint(shape, eveCapturePoint));
                }
                else
                {
                    points[i] = new VPoint(shape, eveCapturePoint);
                }

            }

            List<VPoint> damnedPoints = new List<VPoint>();

            foreach (VPoint point in points)
            {
                if (point.healthy == false)
                {
                    damnedPoints.Add(point);
                    continue;
                }
            }

            foreach (VPoint damnedPoint in damnedPoints)
            {
                try
                {
                    if (damnedPoint.tag == "Triangle")
                        EvePort.WriteLine("T");
                    else
                    {
                        EvePort.WriteLine("S");
                    }
                        
                }
                catch
                {

                }
                finally
                {
                    points.Remove(damnedPoint);
                }
                
            }

            if(shapeDetector.shapes.Count == 0)
            {
                commands1.Clear();
                commands2.Clear();
                DisplayCommands();
                try
                {
                    EvePort.WriteLine("M0");
                }
                catch { } 

				progTimer.Stop();

                SetStatus("Done!");
                MessageBox.Show("All shapes sorted");

               
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
            commands1.Enqueue("2");

            commands1.Enqueue(VCommand.DoAScan);


            try
            {
                EvePort.WriteLine("M1");
            }
            catch
            {

            }

            progTimer.Start();
        }

        private void ProgramStartDefault()
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

            try
            {
                EvePort.WriteLine("M1");
            }
            catch
            {

            }

            progTimer.Start();

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
        private void QueuePointMarty(VPoint point)
        {
            //NEEDS CONTENT

            //Make sure to know if you are using radians or degrees
            //suggestion:

            commands2.Enqueue("C:" + Math.Round(point.Marty.C, 0).ToString());
            commands2.Enqueue(VCommand.Wait);
            commands2.Enqueue("2");
            commands2.Enqueue("B:" + Math.Round(point.Marty.B, 0).ToString());
            commands2.Enqueue(VCommand.Wait);
            commands2.Enqueue("2");
            commands2.Enqueue("A:" + Math.Round(point.Marty.A, 0).ToString());
            commands2.Enqueue(VCommand.Wait);
            commands2.Enqueue("2");
            
        }


        //========================================= COMMUNICATION ===========================================

        private bool ProbeEve()
        {
            _eveID = false;

            if(!EvePort.IsOpen)
            {
                EvePort.Open();
            }
            
            
            EvePort.WriteLine("I");

            Thread.Sleep(1000);

            return _eveID;
        }

        private void StartEveComm(String port)
        {
            statusEve = Connection.Connected;
            if (EvePort.IsOpen)
                EvePort.Close();
            EvePort.PortName = port;
            EvePort.Open();
        }
        private void StartMartyComm(String port)
        {
            statusMarty = Connection.Connected;
            if (MartyPort.IsOpen)
                MartyPort.Close();
            MartyPort.PortName = port;
            MartyPort.Open();
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
            if (indata.Equals("VICTORIA"))
                _eveID = true;

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
			EvePort.WriteLine("M1A0B90C180");
        }
        

        private void RunNextCommand(object sender, RoutedEventArgs e)
        {
            _doCommand = true;
        }

        private void ManualCommands(object sender, RoutedEventArgs e)
        {
            _autoCommand = false;
            SetStatus("Manual Mode On. Press 'n' for next command");
        }

        private void AutoCommands(object sender, RoutedEventArgs e)
        {
            _autoCommand = true;
            SetStatus("Manual Mode Off");
        }

        private void StartProgramDefault(object sender, RoutedEventArgs e)
        {
            ProgramStartDefault();
        }

        private void btnRefresh1_Click(object sender, RoutedEventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();

            
            if(cmbSelect1.SelectedItem != null)
            {
                StartEveComm(cmbSelect1.SelectedItem.ToString());
            }


            cmbSelect1.ItemsSource = ports;
            cmbSelect2.ItemsSource = ports;
            lblStatus1.Content = statusEve.ToString();
            lblStatus2.Content = statusMarty.ToString();
        }

        private void btnRefresh2_Click(object sender, RoutedEventArgs e)
        {

            if (cmbSelect2.SelectedItem != null)
            {
                StartMartyComm(cmbSelect2.SelectedItem.ToString());
            }

            cmbSelect1.ItemsSource = SerialPort.GetPortNames();
            cmbSelect2.ItemsSource = SerialPort.GetPortNames();
            lblStatus1.Content = statusEve.ToString();
            lblStatus2.Content = statusMarty.ToString();

        }

        private void AddEveCommand(object sender, RoutedEventArgs e)
        {

        }

        private void AddMartyCommand(object sender, RoutedEventArgs e)
        {

        }



    }
}

