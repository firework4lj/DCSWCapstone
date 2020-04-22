using System;
using System.IO.Ports;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using SlimDX.DirectInput;


namespace DCSWMonitorApp
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
        public partial class Home : INotifyPropertyChanged
        {
        private enum JoystickTypes
        {
            Driving,
            Joystick,
        }
        DispatcherTimer _timer = new DispatcherTimer();
        public string _leftAxis;
        public string _arduinoSerialCom;
        public string _pvtresults;
        public string _pvtaverage;
        public SerialPort sp = new SerialPort("COM4", 9600, Parity.None, 8, StopBits.One);
        private readonly Joystick joystick;
        private readonly JoystickTypes joystickType;
        public bool testStarted = false;
        public bool testInProgress = false;
        public DateTime startTime;
        public DateTime endTime;
        public long elapsedTicks;
        public double totalResponse;
        public int tests = 0;

        public Home()
            {
                DataContext = this;
                Loaded += MainWindow_Loaded;
                //Closing += MainWindow_Closing;
                InitializeComponent();
                _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                _timer.Tick += _timer_Tick;
                _timer.Start();
            try
            {
                try
                {
                    sp.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                    sp.Open();
                    ArduinoSerialMsg = string.Format("PVT Results = Null");
                    PVTResults = string.Format("PVT Results = NULL ms |");
                    PVTAverage = string.Format("Average response time of: NULL ms over 0 tests.");
                    ArduinoSerialMsg = string.Format("Left Sensor = NULL | Right Sensor = NULL | Button Status = NULL");
                }
                catch (System.IO.IOException e)
                {
                    ArduinoSerialMsg = string.Format("PVT Results = Null");
                    PVTResults = string.Format("PVT Results = NULL ms |");
                    PVTAverage = string.Format("Average response time of: NULL ms over 0 tests.");
                    ArduinoSerialMsg = string.Format("Left Sensor = NULL | Right Sensor = NULL | Button Status = NULL");
                    MessageBox.Show("Arduino not connected!");
                }
            }
            catch (System.NullReferenceException en)
            {
                MessageBox.Show("I/O not connected");
            }
            //Console.Read();
            var directInput = new DirectInput();

            // Prefer a Driving device but make do with fallback to a Joystick if we have to
            var deviceInstance = FindAttachedDevice(directInput, DeviceType.Driving);
            if (null == deviceInstance)
            {
                deviceInstance = FindAttachedDevice(directInput, DeviceType.Joystick);
            }
            if (null == deviceInstance)
            {
                //throw new Exception("No Driving or Joystick devices attached.");
                MessageBox.Show("Wheel is not connected");
            }
            else
            {
                joystickType = (DeviceType.Driving == deviceInstance.Type ? JoystickTypes.Driving : JoystickTypes.Joystick);

                // A little debug spew is often good for you
                Console.WriteLine("First Suitable Device Selected \"" + deviceInstance.InstanceName + "\":");
                Console.WriteLine("\tProductName: " + deviceInstance.ProductName);
                Console.WriteLine("\tType: " + deviceInstance.Type);
                Console.WriteLine("\tSubType: " + deviceInstance.Subtype);

                // Data for both Driving and Joystick device types is received via Joystick
                joystick = new Joystick(directInput, deviceInstance.InstanceGuid);
                var result = joystick.Acquire();
                foreach (DeviceObjectInstance deviceObject in joystick.GetObjects())
                {
                    if ((deviceObject.ObjectType & ObjectDeviceType.Axis) != 0)
                        joystick.GetObjectPropertiesById((int)deviceObject.ObjectType).SetRange(-5000, 5000);
                }
                if (!result.IsSuccess)
                    throw new Exception("Failed to acquire DirectInput device.");

                Console.WriteLine("Joystick acquired.");
            }
        }

        private DeviceInstance FindAttachedDevice(DirectInput directInput, DeviceType deviceType)
        {
            var devices = directInput.GetDevices(deviceType, DeviceEnumerationFlags.AttachedOnly);
            return devices.Count > 0 ? devices[0] : null;
        }

        void _timer_Tick(object sender, EventArgs e)
            {
                DisplayControllerInformation();
            }

        void DisplayControllerInformation()
        {
            if (joystick != null)
            {
                var state = joystick.GetCurrentState();
                int xVal = state.X;
                LeftAxis = string.Format("Wheel Position: {0} Degrees", xVal);
            }
            else
            {
                LeftAxis = string.Format("Wheel Position: NOT CONNECTED");
            }
        }

        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //Write the serial port data to the console.
            string message = sp.ReadLine();
           Console.WriteLine(message);
           ArduinoSerialMsg = string.Format("Left Sensor = {0}{1}{2}{3} | Right Sensor = {4}{5}{6}{7} | Button Status = {8}", message[0], message[1], message[2], message[3], message[4], message[5], message[6], message[7], message[8]);
           int leftSensor = int.Parse((string) string.Format("{0}{1}{2}{3}", message[0], message[1], message[2], message[3]));
           int rightSensor = int.Parse(string.Format("{0}{1}{2}{3}", message[4], message[5], message[6], message[7]));
           if (/*axisdata shows the user is in a safe position to start*/true)
           {
               if (/*pvt test needed*/testStarted == true && testInProgress == false)
               {
                   // Execute test
                   startTime = DateTime.Now;
                   testStarted = false;
                   testInProgress = true;
               }
               else if(testStarted == false && testInProgress == true)
               {
                   if ((message[8] == '0') || (leftSensor > 650 /*|| rightSensor > 650*/)) {
                       endTime = DateTime.Now;
                       testInProgress = false;
                       elapsedTicks = endTime.Ticks - startTime.Ticks;
                       TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
                       stopTest();
                       tests++;
                       // Print pvt results to program
                       totalResponse = totalResponse + elapsedSpan.TotalMilliseconds;
                       PVTAverage = string.Format("Average response time of: {0}ms over {1} tests.", Math.Truncate(totalResponse / tests), tests);
                       
                       if (elapsedSpan.TotalMilliseconds > 500)
                       {
                           PVTResults = string.Format("PVT Results = {0}ms | Test Failure", Math.Truncate(elapsedSpan.TotalMilliseconds));
                       }
                       else
                       {
                           PVTResults = string.Format("PVT Results = {0}ms | Test Pass", Math.Truncate(elapsedSpan.TotalMilliseconds));
                       }
                    }
                }
            }
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
            {
               joystick.Unacquire();
            }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            return;
        }

        #region Properties

        // **************************
        // Message section
        // **************************


        // Output the steering wheel axis data
        public string LeftAxis
        {
            get
            {
                return _leftAxis;
            }
            set
            {
                if (value == _leftAxis) return;
                _leftAxis = value;
                OnPropertyChanged();
            }
        }

        // Output the pvt results
        public string PVTResults
        {
            get
            {
                return _pvtresults;
            }
            set
            {
                if (value == _pvtresults) return;
                _pvtresults = value;
                OnPropertyChanged();
            }
        }

        // Output the arduino's serial message (diagnostic)
        public string ArduinoSerialMsg
        {
            get
            {
                return _arduinoSerialCom;
            }
            set
            {
                if (value == _arduinoSerialCom) return;
                _arduinoSerialCom = value;
                OnPropertyChanged();
            }
        }
        // Output average response time.
        public string PVTAverage
        {
            get
            {
                return _pvtaverage;
            }
            set
            {
                if (value == _pvtaverage) return;
                _pvtaverage = value;
                OnPropertyChanged();
            }
        }

        // **************************
        // End message section
        // **************************


        // **************************
        // Button section
        // **************************

        // Start pvt test by sending the vibration motor a signal to turn on. 
        // Also set the testStarted variable to true to signal the test has begun.
        private void startPVT_Click_V(object sender, RoutedEventArgs e)
        {
            sp.Write("<s>\n");
            
            sp.Write("V1\n");
            testStarted = true;
        }

        // Start pvt test by sending the led a signal to turn on
        // Also set the testStarted variable to true to signal the test has begun.
        private void startPVT_Click_L(object sender, RoutedEventArgs e)
        {
            sp.Write("<s>\n");
            sp.Write("L1\n");
            testStarted = true;
        }


        // Reset the light and vibration motor to off.
        public void stopTest()
        {   
            sp.Write("L0\n");
            sp.Write("V0\n");
        }

        // Turn on the led
        private void ledOn_Click(object sender, RoutedEventArgs e)
        {
            sp.Write("L1\n");
            
        }

        // Turn off the led
        private void ledOff_Click(object sender, RoutedEventArgs e)
        {
            sp.Write("L0\n");
        }

        // Turn on the vibration motor
        private void vibOn_Click(object sender, RoutedEventArgs e)
        {
            sp.Write("V1\n");

        }

        // Turn off the vibration motor
        private void vibOff_Click(object sender, RoutedEventArgs e)
        {
            sp.Write("V0\n");
        }

        // **************************
        // End button section
        // **************************

        public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }

            #endregion
        }
}
