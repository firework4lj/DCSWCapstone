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
        public string _ttnt;
        public SerialPort sp = new SerialPort("COM4", 9600, Parity.None, 8, StopBits.One);
        private readonly Joystick joystick;
        private readonly JoystickTypes joystickType;
        public bool testStarted = false;
        public bool testInProgress = false;
        public bool avgNotInit = true;
        public DateTime startTime;
        public DateTime endTime;
        public long elapsedTicks;
        public double totalResponse;
        public int tests = 0;
        public DateTime pvtLastRan = DateTime.Now;

        // TUNING VARIABLES:
        public static int historySize = 16; // 16 measurements are stored of the steering wheel degrees for logic to be ran on.
        public static int sensorHistorySize = 16; // 16 measurements are stored of the pressure sensor in order to determine when the user grips it harder. 
        public static int sensorAvgHistorySize = 2; // 2 sensor average stores to compare new averages to in order to determine when a blip is detected. (it will only compare 2 averages at the current state.)
        public static int secondsBetweenTests = 60; // 600 seconds, or 10 minutes between tests. Set to 30 seconds for testing
        public static int leftTurnMaxDegree = -400; // How far from center should the driver be able to turn left before the test is executed - MAX is -5000
        public static int rightTurnMaxDegree = 400; // How far from center should the driver be able to turn right before the test is executed - MAX is 5000
        public static double lowerBoundSensorAlg = 1.005; // Sensor average divided by previous average must result in a value greater than 1.2 indicating a blip has occured.
        // END TUNING VARIABLES

        public int[] wheelHistory = new int[historySize];
        public int[] sensorHistory = new int[sensorHistorySize];
        public double[] sensorAvgHistory = new double[sensorAvgHistorySize];

        public Home()
            {
                DataContext = this;
                Loaded += MainWindow_Loaded;
                InitializeComponent();
                //Set up time to be used for timing user responses
                _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                _timer.Tick += _timer_Tick;
                _timer.Start();
            // Initialize history array to all 0's
            for(int i = 0; i< historySize; i++)
            {
                wheelHistory[i] = 0;
            }
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
                // Insert new reading into degree history, push list back.
                int [] temp = new int[historySize];
                for (int i = 0; i < historySize-1; i++)
                {
                    if (i == 0)
                    {
                        temp[i] = xVal;
                    }
                    else
                    {
                        temp[i] = wheelHistory[i-1];
                    }
                }
                wheelHistory = temp;
                // Print history to console for debugging
              // for(int i = 0; i < historySize; i++)
              // {
              //     Console.Write(wheelHistory[i]+",");
              // }
              // Console.Write("\n");
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
           // No need for this debug message anymore.
           // Console.WriteLine(message);
           //message it the 9 character string sent from the arduino. The first 4 chars are for the left sensor, the next 4 are for the right sensor, and the last is for the button
           ArduinoSerialMsg = string.Format("Left Sensor = {0}{1}{2}{3} | Right Sensor = {4}{5}{6}{7} | Button Status = {8}", message[0], message[1], message[2], message[3], message[4], message[5], message[6], message[7], message[8]);
           int leftSensor = int.Parse(string.Format("{0}{1}{2}{3}", message[0], message[1], message[2], message[3]));
           int rightSensor = int.Parse(string.Format("{0}{1}{2}{3}", message[4], message[5], message[6], message[7]));
            int[] temp = new int[sensorHistorySize];
            for (int i = 0; i < sensorHistorySize; i++)
            {
                if (i == 0)
                {
                    temp[i] = (leftSensor+rightSensor)/2; // Average the value between the two sensors.
                }
                else
                {
                    temp[i] = sensorHistory[i - 1];
                }
            }
            sensorHistory = temp;

            double averageVal = 0;
            for (int i = 0; i < sensorHistorySize; i++)
            {
                averageVal = sensorHistory[i] + averageVal;
            }
            averageVal = averageVal / sensorHistorySize;
            double[] temp2 = new double[sensorAvgHistorySize];
            for (int i = 0; i < sensorAvgHistorySize; i++)
            {
                if (i == 0)
                {
                    temp2[i] = averageVal; // Average value of all the data in the stored sensor measurments.
                }
                else
                {
                    temp2[i] = sensorAvgHistory[i - 1];
                }
                if (avgNotInit)
                {
                    temp2[1] = averageVal; // Set the last average val equal to the current avg val to prevent divide by 0
                    avgNotInit = false;
                }
            }
            sensorAvgHistory = temp2;
            for(int i = 0; i < sensorAvgHistorySize; i++)
            {
                Console.Write(sensorAvgHistory[i]+",");
            }
            Console.Write("\n");

            // First if statement should check if pvt is required at this point. 
            if (testInProgress == true || checkPvtRequired()) // Check if the test is required by time. Bypass if the test has already been started.
            {
                if (testInProgress == true || checkPvtSafe()) // Check wheel history to determine if it is safe to perform a pvt test. Bypass if the test has already been started
                {
                    if (testInProgress == false)
                    {
                        sp.Write("<s>\n");
                        sp.Write("V1\n");
                        sp.Write("L1\n");
                        startTime = DateTime.Now;
                        testInProgress = true;
                    }
                    else if (testInProgress == true)
                    {
                        if ((message[8] == '0') || sensorBlipDetector()) // If button is pressed, or pressure sensors exceed 650, stop test. Pressure sensors ***NEED TO BE DYNAMICALLY SET***
                        {
                            endTime = DateTime.Now;
                            testInProgress = false;
                            elapsedTicks = endTime.Ticks - startTime.Ticks;
                            TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
                            stopTest();
                            tests++;
                            totalResponse = totalResponse + elapsedSpan.TotalMilliseconds;
                            PVTAverage = string.Format("Average response time of: {0}ms over {1} tests.", Math.Truncate(totalResponse / tests), tests);
                            pvtLastRan = endTime; // Set the last run time to the end time of the current test.
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
        }

        bool checkPvtRequired()
        {
            long elapsedTicksFromLastPvt = DateTime.Now.Ticks - pvtLastRan.Ticks;
            TimeSpan elapsedTimeSpanFromLastPvt = new TimeSpan(elapsedTicksFromLastPvt);
            TimeToNextTest = string.Format("Time to Next Test: {0} Seconds", Math.Truncate(secondsBetweenTests - elapsedTimeSpanFromLastPvt.TotalSeconds));
            if(elapsedTimeSpanFromLastPvt.TotalSeconds >= secondsBetweenTests)
            {
                // Test is required now.
                // It has been 10 minutes since last pvt test. 
                Console.WriteLine("Test is now required based on time elapsed.");
                return true;
            }
            else
            {
                return false;
            }
        }

        bool checkPvtSafe()
        {
            // This is where the algorithm determines if the driver has been driving straight for an extended period of time and we can safely execute the test.
            for(int i = 0; i < historySize; i++)
            {
                if((wheelHistory[i] > leftTurnMaxDegree) && (wheelHistory[i] < rightTurnMaxDegree))
                {
                    continue;
                }
                else
                {
                    // Wheel has not been straight enough for a PVT test to be performed. 
                    Console.WriteLine("Test cannot be perfomed safely at this time.");
                    return false;
                }
            }
            // Wheel has been within parameters for a PVT test to be performed.
            return true;
        }

        bool sensorBlipDetector()
        {
            // Compare the values, and figure out if there is a sizeable blip. Aka sensor values are higher for an extended period of time. This might be difficult.
            double diff = sensorAvgHistory[0] / sensorAvgHistory[1];
            Console.WriteLine(diff);
            if (diff >= lowerBoundSensorAlg){
                Console.WriteLine("BLIP DETECTED");
                return true;
            }
            return false;
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

        public string TimeToNextTest
        {
            get
            {
                return _ttnt;
            }
            set
            {
                if (value == _ttnt) return;
                _ttnt = value;
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
        private void startPVT_Click(object sender, RoutedEventArgs e)
        {
            sp.Write("<s>\n");
            sp.Write("V1\n");
            sp.Write("L1\n");
            pvtLastRan = pvtLastRan.AddDays(-5); // Cause a pvt test to be performed now
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
