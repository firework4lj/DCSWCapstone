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
                InitializeComponent();
                //Set up time to be used for timing user responses
                _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                _timer.Tick += _timer_Tick;
                _timer.Start();
            try
            {
                //Check if a serial input device is connected, if so open serial input either way set initial values to null
                try
                {
                    //null values here are simply for intitialization prior to input
                    sp.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
                    sp.Open();
                    ArduinoSerialMsg = string.Format("PVT Results = Null");
                    PVTResults = string.Format("PVT Results = NULL ms |");
                    PVTAverage = string.Format("Average response time of: NULL ms over 0 tests.");
                    ArduinoSerialMsg = string.Format("Left Sensor = NULL | Right Sensor = NULL | Button Status = NULL");
                }
                catch (System.IO.IOException e)
                {
                    //null values here will persist due to a lack of input
                    ArduinoSerialMsg = string.Format("PVT Results = Null");
                    PVTResults = string.Format("PVT Results = NULL ms |");
                    PVTAverage = string.Format("Average response time of: NULL ms over 0 tests.");
                    ArduinoSerialMsg = string.Format("Left Sensor = NULL | Right Sensor = NULL | Button Status = NULL");
                    MessageBox.Show("Arduino not connected!");
                }
            }
            catch (System.NullReferenceException en)
            {
                //pop up message box to let user know there is no input
                MessageBox.Show("I/O not connected");
            }
            var directInput = new DirectInput();

            //Get steering wheel input
            //Check if there is input from a Driving device, which is preferred, if not, fallback to a Joystick. If neither throw an exception.
            var deviceInstance = FindAttachedDevice(directInput, DeviceType.Driving);
            if (null == deviceInstance)
            {
                deviceInstance = FindAttachedDevice(directInput, DeviceType.Joystick);
            }
            if (null == deviceInstance)
            {
                //throw new Exception("No Driving or Joystick devices attached.")
                MessageBox.Show("Wheel is not connected");
            }
            else
            {
                //store whether the input is coming from a driving wheel or a joystick
                joystickType = (DeviceType.Driving == deviceInstance.Type ? JoystickTypes.Driving : JoystickTypes.Joystick);

                // Information about input driving device for debugging
                Console.WriteLine("First Suitable Device Selected \"" + deviceInstance.InstanceName + "\":");
                Console.WriteLine("\tProductName: " + deviceInstance.ProductName);
                Console.WriteLine("\tType: " + deviceInstance.Type);
                Console.WriteLine("\tSubType: " + deviceInstance.Subtype);

                // Data for both Driving and Joystick devices is received via Joystick type
                joystick = new Joystick(directInput, deviceInstance.InstanceGuid);
                var result = joystick.Acquire();
                //start retrieving data from the driving input
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
        //used to get steering wheel device for lines 79-89
        private DeviceInstance FindAttachedDevice(DirectInput directInput, DeviceType deviceType)
        {
            var devices = directInput.GetDevices(deviceType, DeviceEnumerationFlags.AttachedOnly);
            return devices.Count > 0 ? devices[0] : null;
        }
        //used to make sure wheel position data constantly updated
        void _timer_Tick(object sender, EventArgs e)
            {
                DisplayControllerInformation();
            }

        void DisplayControllerInformation()
        {
            if (joystick != null)
            {
                //if we have input from the driving device, get the x axis data which corresponds to its rotation value
                var state = joystick.GetCurrentState();
                int xVal = state.X;
                LeftAxis = string.Format("Wheel Position: {0} Degrees", xVal);
            }
            else
            {
                LeftAxis = string.Format("Wheel Position: NOT CONNECTED");
            }
        }
        //getting data input for arduino for all connections with hardware other than steering wheel 
        private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //Write the serial port data to the console.
           string message = sp.ReadLine();
           Console.WriteLine(message);
           //message it the 9 character string sent from the arduino. The first 4 chars are for the left sensor, the next 4 are for the right sensor, and the last is for the button
           ArduinoSerialMsg = string.Format("Left Sensor = {0}{1}{2}{3} | Right Sensor = {4}{5}{6}{7} | Button Status = {8}", message[0], message[1], message[2], message[3], message[4], message[5], message[6], message[7], message[8]);
           int leftSensor = int.Parse((string) string.Format("{0}{1}{2}{3}", message[0], message[1], message[2], message[3]));
           int rightSensor = int.Parse(string.Format("{0}{1}{2}{3}", message[4], message[5], message[6], message[7]));
           if (/*axisdata shows the user is in a safe position to start*/true)
                //this if has not yet been implimented. It will check that the user is not turning before starting a test. For now it is always true
           {
               if (/*pvt test needed*/testStarted == true && testInProgress == false)
                    //checks if test needs to be started and that one is not already happening so we don't start a test with one already going
               {
                   // Start PVT test
                   //get current time to compare time at response to later
                   startTime = DateTime.Now;
                   //set test started to false, once we start running a test we won't need to run one anymore
                   testStarted = false;
                   testInProgress = true;
               }
               //what to execute for PVT test
               else if(testStarted == false && testInProgress == true)
               {
                   if ((message[8] == '0') || (leftSensor > 650 || rightSensor > 650)) {
                       //If the button is pressed or the left or right pressure sensor on the steering wheel is squeezed
                       //These are all methods of the user responding to the test so the time and test should stop
                       endTime = DateTime.Now;
                       testInProgress = false;
                       //Calculate the reaction time
                       elapsedTicks = endTime.Ticks - startTime.Ticks;
                       TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
                       stopTest();
                       //increase variable that keeps track of the number of tests that have been run so far, used for averaging results
                       tests++;
                       //Add response time for current test to response times from previous tests, used for averaging results
                       totalResponse = totalResponse + elapsedSpan.TotalMilliseconds;
                       // Print pvt results to program
                       PVTAverage = string.Format("Average response time of: {0}ms over {1} tests.", Math.Truncate(totalResponse / tests), tests);
                       //A PVT response over 500ms is considered a faliure
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
