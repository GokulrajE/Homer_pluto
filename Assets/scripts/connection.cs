using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System;
using System.Linq;

public class connection : MonoBehaviour
{
    public GameObject panel;
    public GameObject calib_panel;
    public GameObject test_panel;
    public Image statusImage;
    public GameObject loading;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI statusModeText;
    public TextMeshProUGUI angleText;
    public TextMeshProUGUI controlText;
    public TextMeshProUGUI torqueText;
    public TextMeshProUGUI targetleText;
    public TextMeshProUGUI errText;
    public TextMeshProUGUI errsumText;
    public TextMeshProUGUI errdiffText;
    public TextMeshProUGUI mech;
    public TextMeshProUGUI actuated;
    public TextMeshProUGUI buttonstate;
    public TextMeshProUGUI Calibration;
    public TextMeshProUGUI time;
    public TextMeshProUGUI controltype;
    public TextMeshProUGUI calib_status;
    public TextMeshProUGUI distance;
    public TextMeshProUGUI button_message;
    public TextMeshProUGUI lblFeedforwardTorqueValue;
    public TextMeshProUGUI lblPositionTargetValue;
    public ToggleGroup RadioOptions;  // For the 3 options
    public Slider torque;                  // For torque controlslider
    public Slider positionSlider;         //for positionSlider
    public string[] availabe;
    public bool diagonistic = false;
    public byte Diagonistic = 0x06;
    public static String[] outdatatype = new string[] { "SENSORSTREAM", "CONTROLPARAM", "DIAGNOSTICS" };
    public static String[] mechanisum = new string[] { "WFE","WUD","WPS","HOC","NOMECH"};
    public static String[] calibration = new string[] { "NOCALLIB", "YESCALLIB"};
    public static String[] control_Type = new string[] {"NONE","POSITION","RESIST","TORQUE"};
    //"GET_VERSION":0x00,"CALIBRATE":0x01,"START_STREAM": 0x02,"STOP_STREAM": 0x03,"SET_CONTROL_TYPE":0x04,"SET_CONTROL_TARGET": 0x05,"SET_DIAGNOSTICS":0x06,
    public static int[] indatatype = new int[] { 0, 1, 2, 3, 4, 5, 6 };
    public static int[] sensordatanumber = new int[] { 4,0,7 };//0:sesnorStream;2:diagonistic;
    public static int[] calibration_Status = new int[] { 0, 1, 2, 3, 4 };
    public static int[] calib_angle = new int[] { 120, 120, 120, 140 };
    public int calib_state;
    public int[] contolType_ = new int[] { 0, 1, 2, 3 };//none,posistion,resist,torque
    public static int calib_st { get; private set; }
   

    // Start is called before the first frame update
    void Start()
    {
       
        // Set default values for sliders
        torque.minValue = -1.0f;  // Set minimum value for torque slider
        torque.maxValue = 1.0f;   // Set maximum value for torque slider
        torque.value = 0.0f; ;
        // Set default values and range for position slider
        positionSlider.minValue = -135.0f;  // Set minimum value for position slider
        positionSlider.maxValue = 0.0f;     // Set maximum value for position slider
        positionSlider.value = -135.0f;     // Set initial value to -135°
        statusImage.enabled = false;
        availabe = ConnectToRobot.availablePorts();
        TryConnectToDevice();
        panel.SetActive(false);
        // Attach controls callback
        AttachControlCallbacks();

        // Update the UI when starting
        UpdateUI();
    }

    // Update is called once per frame
    void Update()
    {
        
        if (ConnectToRobot.isPLUTO)
        {

            //if (MySerialThread.buttonst == 0)
            //{
            //    calibrate();
            //}
            //connection._Calibration.update_calib_ui();
            update_calib_ui();
            updateAngVal();
            connection._Calibration.calibrationSetState();
            statusText.text = "connected PLUTO";
            statusImage.enabled = true;
            loading.SetActive(false);
            statusImage.color = Color.green;
           
        }
        Debug.Log(calib_st+"calib_st");
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.D))
        {
            panel.SetActive(!panel.activeSelf);
            // Toggle showing the angle
            if (panel.activeSelf)
            {
                updateAngVal();
                Debug.Log("Ctrl + A pressed, toggling angle display");
            }
        }
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
        {
            calib_panel.SetActive(!calib_panel.activeSelf);
            // Toggle showing the angle

            calib_st = -1;
            connection._Calibration.calibrate("NOMECH");
            if (calib_panel.activeSelf)
            {
                //updateAngVal();

                if (calib_st == -1)
                {
                    if (MySerialThread.Statusmode.Equals(outdatatype[2]))
                    {
                        MySerialThread.SendMessage(new byte[] { (byte)indatatype[2] });

                    }

                    //connection._Calibration.update_calib_ui();
                    connection._Calibration.calibrate("NOMECH");
                    calib_st = 0;//to set zero
                }
                
                Debug.Log("Ctrl + A pressed, toggling angle display");
            }
        }
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.T))
        {
            test_panel.SetActive(!test_panel.activeSelf);
            // Toggle showing the angle
            if (test_panel.activeSelf)
            {
                MySerialThread.SendMessage(new byte[] { (byte)indatatype[6] });
                //MySerialThread.SendMessage(new byte[] { 6 });
                //updateAngVal();
                Debug.Log("Ctrl + A pressed, toggling angle display");
            }
        }

        UpdateTorquePositionalControl();
    }
   
    private void TryConnectToDevice()
    {

        Debug.Log("Available Ports: " + string.Join(", ", availabe));
        //ConnectToRobot.Connect("COM3");
        foreach (String port in availabe)
        {
            if (port == "Select Port") continue;
            Debug.Log("Trying port: " + port);

            try
            {
                ConnectToRobot.Connect(port);
                if (ConnectToRobot.isPLUTO)
                {
                    
                    Debug.Log("Connected to PLUTO on port " + port);
                    break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error connecting to port " + port + ": " + ex.Message);
            }
        }
       
    }
    private void UpdateTorquePositionalControl()
    {
        // Handle torque and positional control slider updates
        float torqueValue = GetTorqueSliderValue();
        float positionalValue = GetPositionSliderValue();

        Debug.Log("Torque Value: " + torqueValue);
        Debug.Log("Positional Control Value: " + positionalValue);
    }
    public static class ConnectToRobot
    {
        public static string _port;
        public static bool isPLUTO = false;

        public static string[] availablePorts()
        {
            string[] portNames = SerialPort.GetPortNames();
            string[] comPorts = new string[portNames.Length + 1];
            comPorts[0] = "Select Port";
            Array.Copy(portNames, 0, comPorts, 1, portNames.Length); // Copy the old values
            if (comPorts.Length > 1)
            {
                Debug.Log("Available Port: " + comPorts[1]);
            }
            else
            {
                Debug.LogWarning("No available serial ports found.");
            }
            return comPorts;
        }

        public static void Connect(string port)
        {
            _port = port;
            if (_port == null)
            {
                _port = "COM3";
                MySerialThread.InitSerialComm(_port);
            }
            else
            {

                MySerialThread.InitSerialComm(_port);
            }
            if (MySerialThread.serPort == null)
            {
                // Setup serial communication with the robot.
            }

            else
            {

                if (MySerialThread.serPort.IsOpen)
                {

                    UnityEngine.Debug.Log("Already Opended");
                    MySerialThread.Disconnect();
                    //AppData.WriteSessionInfo("DisConnecting to robot.");
                }

                if (MySerialThread.serPort.IsOpen == false)
                {

                    UnityEngine.Debug.Log(_port);
                    MySerialThread.Connect();
                    //AppData.WriteSessionInfo("Connecting to robot.");
                }
            }

        }
        public static void disconnect()
        {
            ConnectToRobot.isPLUTO = false;
            MySerialThread.Disconnect();
        }

    }
    public static class MySerialThread
    {
        static public bool stop;
        static public bool pause;
        static public SerialPort serPort { get; private set; }
        static private Thread reader;
        static private uint _count;
        static byte[] packet;
        static int plcnt = 0;
        static public uint count
        {
            get { return _count; }
        }
        public static double HOCScale = 3.97 * Math.PI / 180;
        static private byte[] rawBytes = new byte[256];
        static private double t0, t1;
        static public float framerate { get; private set; }
        static public float AngVal { get; private set; }
        static public float torqueVal { get; private set; }
        static public float controlVal { get; private set; }
        static public float TargetVal { get; private set; }
        static public float errVal { get; private set; }
        static public float errsumVal { get; private set; }
        static public float errdiffVal { get; private set; }
        static public String mech { get; private set; }
        static public int actu { get; private set; }
        static public byte buttonst { get; private set; }
        static public String Statusmode { get; private set; }
        static public String calib_status { get; private set; }
        static public String current_time { get; private set; }
        static public String controlType { get; private set; }
        static public byte pressed { get; private set; }
        static public byte released { get; private set; }
        static public double hocdis { get; private set; }
        public static void SetPressed(byte value)
        {
            pressed = value;
        }

        static public void InitSerialComm(string port)
        {
            serPort = new SerialPort();
            // Allow the user to set the appropriate properties.
            serPort.PortName = port;
            serPort.BaudRate = 115200;
            serPort.Parity = Parity.None;
            serPort.DataBits = 8;
            serPort.StopBits = StopBits.One;
            serPort.Handshake = Handshake.None;
            serPort.DtrEnable = true;

            // Set the read/write timeouts
            serPort.ReadTimeout = 250;
            serPort.WriteTimeout = 250;
        }

        static public void Connect()
        {
            stop = false;
            if (serPort.IsOpen == false)
            {
                try
                {
                    serPort.Open();
                }
                catch (Exception ex)
                {
                    Debug.Log("exception: " + ex);
                }
                
                reader = new Thread(serialreaderthread);
               
                reader.Priority = System.Threading.ThreadPriority.AboveNormal;
                t0 = 0.0;
                t1 = 0.0;
                _count = 0;
                reader.Start();


            }
        }

        static public void Disconnect()
        {
            stop = true;
            if (serPort.IsOpen)
            {
                reader.Abort();
                serPort.Close();
            }


        }

        static public void resetCount()
        {
            _count = 0;
        }

        static private void serialreaderthread()
        {
            byte[] _floatbytes = new byte[4];

            // start stop watch.
            while (stop == false)
            {
                // Do nothing if paused
                if (pause)
                {
                    continue;
                }
                try
                {
                    // Read full packet.

                    if (readFullSerialPacket())
                    {
                        ConnectToRobot.isPLUTO = true;
                        pareseByteArray(rawBytes,plcnt);
                        if(buttonst == 0)
                        {
                            pressed = 1;
                            released = 0;
                        }
                        else
                        { 
                            released = 1;
                        }
                        
                    }
                    else
                    {
                        ConnectToRobot.isPLUTO = false;
                    }

                    //  Debug.Log("connected");
                }
                catch (TimeoutException)
                {
                   
                    continue;
                }
               
            }
            serPort.Close();
        }

       
        // Read a full serial packet.
        static private bool readFullSerialPacket()
        {
            plcnt = 0;
            int chksum = 0;
            int _chksum;
            byte[] temp;
            // Header bytes
            if ((serPort.ReadByte() == 0xFF) && (serPort.ReadByte() == 0xFF))
            {
                plcnt = 0;
                //SerialPayload.count++;
                chksum = 255 + 255;
                //// Number of bytes to read.
                rawBytes[plcnt++] = (byte)serPort.ReadByte();
                Debug.Log(rawBytes[0] + "rawbytes0no.ofbytes");
                chksum += rawBytes[0];
                
                DateTime now = DateTime.Now;
                byte[] dateTimeBytes = BitConverter.GetBytes(now.Ticks);
                if (rawBytes[0] != 255)
                { 
                    // read payload
                    for (int i = 0; i < rawBytes[0] - 1; i++)
                    {
                        rawBytes[plcnt++] = (byte)serPort.ReadByte();
                        chksum += rawBytes[plcnt - 1];
                    }
                    _chksum = serPort.ReadByte();
                    // Add timestamp to rawBytes
                    Array.Copy(dateTimeBytes, 0, rawBytes, plcnt, dateTimeBytes.Length);
                    plcnt += dateTimeBytes.Length;
                    return (_chksum == (chksum & 0xFF));
                }
                else
                {
                    Debug.Log("data error");
                    //Disconnect();
                    return false;
                }
            }
            else
            {
                //Disconnect();
                return false;
            }
        }
        public static void pareseByteArray(byte[] receivedbytes,int datalength)
        {
             byte status = rawBytes[1];//status
            Debug.Log(status + "status");
            int error = 255*rawBytes[3] + rawBytes[2];//error
            Debug.Log(error + "error");
            int statusmode = getstatusMode(status);//statusmode[Sensorstream,Diagonistics]
            Debug.Log(statusmode + "mode");
            int ismech = getmech(rawBytes[4]);//mechanisum
            Debug.Log(ismech + "mec");
            int isact = getisact(rawBytes[4]);//Actuated
            Debug.Log(isact + "act");
            int dtype = getdatatype(status);//sensordatatype
            Debug.Log(dtype + "datatype");
            //if sensordatanumber is 4 - sensorstream
            if (dtype == sensordatanumber[0]) {
                byte[][] floatBytesArray = new byte[dtype][];
                float[] sensordata = new float[dtype];
                for (int i = 0; i < dtype; i++)
                {
                    floatBytesArray[i] = new byte[] { rawBytes[5 + (i * 4)], rawBytes[6 + (i * 4)], rawBytes[7 + (i * 4)], rawBytes[8 + (i * 4)] };
                    float floatValue = BitConverter.ToSingle(floatBytesArray[i], 0);
                    sensordata[i] = floatValue;
                    //Debug.Log("Decoded floating point value: "+ i +""+ floatValue);
                }
                AngVal = sensordata[0];
                torqueVal = sensordata[1];
                controlVal = sensordata[2];
                TargetVal = sensordata[3];
                buttonst = rawBytes[(dtype+1)*4+1];
                Debug.Log(rawBytes[21] + "buttonstatus");
            }
            //if sensordata number is 7 - diagonistics
            if (dtype == sensordatanumber[2])
            {
                byte[][] floatBytesArray = new byte[dtype][];
                float[] sensordata = new float[dtype];
                for (int i = 0; i < dtype; i++)
                {
                    floatBytesArray[i] = new byte[] { rawBytes[5 + (i * 4)], rawBytes[6 + (i * 4)], rawBytes[7 + (i * 4)], rawBytes[8 + (i * 4)] };
                    float floatValue = BitConverter.ToSingle(floatBytesArray[i], 0);
                    sensordata[i] = floatValue;
                    //Debug.Log("Decoded floating point value: "+ i +""+ floatValue);
                }
                AngVal = sensordata[0];
                torqueVal = sensordata[1];
                controlVal = sensordata[2];
                TargetVal = sensordata[3];
                errVal = sensordata[4];
                errdiffVal = sensordata[5];
                errsumVal = sensordata[6];
                buttonst = rawBytes[(dtype + 1 )* 4+1];
                Debug.Log(rawBytes[33] + "buttonstatus");
            }
            int dateTimeStartIndex = datalength - 8;  // The last 8 bytes are for the DateTime
            byte[] dateTimeBytes = new byte[8];
            Array.Copy(receivedbytes, dateTimeStartIndex, dateTimeBytes, 0, 8);

            // Convert the 8-byte array back to a long (Ticks)
            long dateTimeTicks = BitConverter.ToInt64(dateTimeBytes, 0);

            // Create a DateTime from the ticks
            DateTime timestamp = new DateTime(dateTimeTicks);
            hocdis = gethocdis(AngVal);
            current_time = timestamp.ToString();
            Statusmode = outdatatype[statusmode];
            mech = mechanisum[ismech];
            actu = isact;
            controlType = getcontroltype(status);
            calib_status = getcalibstatus(status);
            Debug.Log(mech+"mech");
            Debug.Log(actu+"actuat"); 
            Debug.Log(rawBytes[1] + "status");
            static double gethocdis(float angel)
            {
                return HOCScale * Math.Abs(angel);

            }
            //extract mechanisum from actuated byte
            static int getmech(byte data)
            {
                return (data >> 4);

            }
            //extract actuated  from actuated byte
             static int getisact(byte data)
            {
                return (data & 0x01);

            }
            //extract statusmode from status byte
            static int getstatusMode(byte data)
            {
                return (data >> 4);

            }
            //extract sensordatanumber from status byte 
            static int getdatatype(byte data)
            {
                int num = data >> 4;
                int datatype = sensordatanumber[num];
                return datatype;
            }
            //extract calibrationstatus from status byte
            static String getcalibstatus(byte data)
            {
                int num = data & 0x01;
                return (calibration[num]);
            }
            //extract controltype from status byte
            static String getcontroltype(byte data)
            {
                int num = (data & 0x0E) >> 1;
                return (control_Type[num]);
            }
        }
        //send command to robot
        public static void SendMessage(byte[] outBytes)
        {
            // Prepare the payload (with the header, length, message, and checksum)
            List<byte> outPayload = new List<byte>
            {
             0xAA, // Header byte 1
             0xAA, // Header byte 2
             (byte)(outBytes.Length + 1) // Length of the message (+1 for checksum)
            };

            // Add the message bytes to the payload
            outPayload.AddRange(outBytes);

            // Calculate checksum (sum of all bytes modulo 256)
            byte checksum = (byte)(outPayload.Sum(b => b) % 256);

            // Add the checksum at the end of the payload
            outPayload.Add(checksum);

            // If debugging is enabled, print the outgoing data
            bool outDebug = true; // Set this to true or false based on your debugging needs
            if (outDebug)
            {
                Console.Write("\nOut data: ");
                foreach (var elem in outPayload)
                {
                    Debug.Log($"{elem} ");
                }
                Console.WriteLine();
            }

            // Send the message to the serial port
            try
            {
                serPort.Write(outPayload.ToArray(), 0, outPayload.Count);
                Debug.Log("Message sent to device.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }
        

    }
    public  static class _Calibration
    {   
       
        
        public static  void calibrate(string mec)
        { 
            int in_dtype = indatatype[1];
            int index = Array.IndexOf(mechanisum, mec);
            byte[] data = new byte[] { ((byte)in_dtype),((byte)index) };
            MySerialThread.SendMessage(data);
           
            
        }
        public static void calibrationSetState()
        {   
            //Zeroset
            if (MySerialThread.pressed == MySerialThread.released && calib_st == 0)
            {
                    calibrate("HOC");
                    Debug.Log("pressed");

                if (MySerialThread.calib_status.Equals(connection.calibration[1]))
                {

                    calib_st = 2;
                    MySerialThread.SetPressed(0);
                }
                else
                {
                    //MySerialThread.SetPressed(0);
                    calib_st = 0;

                }

            }
            //romset
            if (MySerialThread.pressed == MySerialThread.released && calib_st == 2)
            {
                //romcheck
                if (-MySerialThread.AngVal >= 0.9 * (Double)calib_angle[3] && -MySerialThread.AngVal <= 1.1 * (Double)calib_angle[3])
                {
                    calib_st = 3;
                    MySerialThread.SetPressed(0);
                    
                }
                else{
                    calib_st = 4;
                    MySerialThread.SetPressed(0);
                    
                }
                //Debug.Log("pressed");
                
            }
            if((MySerialThread.pressed == MySerialThread.released && calib_st == 3)|| (MySerialThread.pressed == MySerialThread.released && calib_st == 4))
            {
                MySerialThread.SetPressed(0);
            }
            if(MySerialThread.pressed == MySerialThread.released && calib_st == -1)
            {
                MySerialThread.SetPressed(0);
            }

        }
        
    }
    public void update_calib_ui()
    {
        switch (calib_st)
        {
            case 0:
                calib_status.text = "State : NoData";
                distance.text = "Dist:_NA_";
                button_message.text = "Press the PLUTO button zero set";
                break;

           
            case 2:
                calib_status.text = "State : Zero Set";
                distance.text = "Dist: " + MySerialThread.hocdis.ToString("F2") + "cm";
                button_message.text = "Press the PLUTO button rom set";
                break;

            case 3:
                calib_status.text = "State : All Done!";
                distance.text = "Dist: " + MySerialThread.hocdis.ToString("F2") + "cm";
                button_message.text = "Press ctrl+C to close";
                break;

            case 4:
                calib_status.text = "State : Error";
                distance.text = "Dist: " + MySerialThread.hocdis.ToString("F2") + "cm";
                button_message.text = "Press ctrl+C to close";
                break;

            default:
                Debug.LogError("Invalid calibration state");
                break;
        }
    }
       //testcontrol type
        public void AttachControlCallbacks()
        {
            // Attach the Toggle event listeners
            RadioOptions.transform.Find("No_ctrl").GetComponent<Toggle>().onValueChanged.AddListener(delegate { OnControlTypeSelected(); });
            RadioOptions.transform.Find("Torque").GetComponent<Toggle>().onValueChanged.AddListener(delegate { OnControlTypeSelected(); });
            RadioOptions.transform.Find("posistion").GetComponent<Toggle>().onValueChanged.AddListener(delegate { OnControlTypeSelected(); });

            // Attach the Slider event listeners
            torque.onValueChanged.AddListener(delegate { OnTorqueTargetChanged(); });
            positionSlider.onValueChanged.AddListener(delegate { OnPositionTargetChanged(); });
        }

        public void OnControlTypeSelected()
        {
            // Reset sliders when a control option is selected
            torque.value = 0;
            positionSlider.value = 0;
            Debug.Log("working");
            // Handle control types based on the selected toggle
            Toggle activeToggle = RadioOptions.ActiveToggles().FirstOrDefault();
            if (activeToggle != null)
            {
                string selectedOption = activeToggle.name;
                int in_dtype = indatatype[4];
            if (selectedOption == "No_ctrl")
                {
                int ctrl_type = contolType_[0];

                MySerialThread.SendMessage(new byte[] {(byte)in_dtype,(byte) ctrl_type });
                    //DeviceControl.SetControlType("NONE");
                    //Debug.Log("no control is not executed");
                }
                else if (selectedOption == "Torque")
                {
                int ctrl_type = contolType_[3];

                MySerialThread.SendMessage(new byte[] { (byte)in_dtype, (byte)ctrl_type });
                
                //DeviceControl.SetControlType("TORQUE");
                }
                else if (selectedOption == "posistion")
                {
                int ctrl_type = contolType_[1];

                MySerialThread.SendMessage(new byte[] { (byte)in_dtype, (byte)ctrl_type });
                //DeviceControl.SetControlType("POSITION");
            }
            }

            // Update UI accordingly
            UpdateUI();
        }

        private void OnTorqueTargetChanged()
        {
            // Handle torque target changes and send them to the device
            float torqueValue = GetTorqueSliderValue();
            
            SetControlTarget(torqueValue);
            UpdateUI();
        }

        private void OnPositionTargetChanged()
        {
            // Handle position target changes and send them to the device
            float positionValue = GetPositionSliderValue();
            SetControlTarget(positionValue);
            UpdateUI();
        }

        private void UpdateUI()
        {
            bool noControl = !RadioOptions.transform.Find("Torque").GetComponent<Toggle>().isOn &&
                             !RadioOptions.transform.Find("posistion").GetComponent<Toggle>().isOn;

            // Enable/disable sliders
            torque.interactable = RadioOptions.transform.Find("Torque").GetComponent<Toggle>().isOn;
            positionSlider.interactable = RadioOptions.transform.Find("posistion").GetComponent<Toggle>().isOn;
            // Using PlutoDefs1.GetTargetRange for torque and position ranges
            double[] torqueRange = PlutoDefs1.GetTargetRange("TORQUE");
            double[] positionRange = PlutoDefs1.GetTargetRange("POSITION");

            if (noControl)
            {
                lblFeedforwardTorqueValue.text = "Feedforward Torque Value (Nm):";
                lblPositionTargetValue.text = "Target Position Value (deg):";
            }
            else
            {
                lblFeedforwardTorqueValue.text = $"Feedforward Torque Value (Nm) [{torqueRange[0]}, {torqueRange[1]}]: {torque.value:F1}Nm";
                lblPositionTargetValue.text = $"Target Position Value (deg) [{positionRange[0]}, {positionRange[1]}]: {positionSlider.value:F1}deg";
            }
        }

        // Get torque slider value and convert it to the appropriate target range
        private float GetTorqueSliderValue()
        {
            float sliderMin = torque.minValue;
            float sliderMax = torque.maxValue;
            double[] torqueRange = PlutoDefs1.GetTargetRange("TORQUE");
            float targetMin = (float)torqueRange[0];
            float targetMax = (float)torqueRange[1];
            return targetMin + (targetMax - targetMin) * (torque.value - sliderMin) / (sliderMax - sliderMin);
        }

        // Get position slider value and convert it to the appropriate target range
        private float GetPositionSliderValue()
        {
            float sliderMin = positionSlider.minValue;
            float sliderMax = positionSlider.maxValue;
            double[] positionRange = PlutoDefs1.GetTargetRange("POSITION");
            float targetMin = (float)positionRange[0];
            float targetMax = (float)positionRange[1];
            return targetMin + (targetMax - targetMin) * (positionSlider.value - sliderMin) / (sliderMax - sliderMin);
        }
    // Function to set the controller target position
    public static void SetControlTarget(float target)
    {
        int in_dtype = indatatype[5];
        byte[] targetBytes = BitConverter.GetBytes(target);
        List<byte> payload = new List<byte> { (byte)in_dtype };
        payload.AddRange(targetBytes);
        if (payload.Count > 0)
        {
            UnityEngine.Debug.Log("Out data");
            foreach (var elem in payload)
            {
                UnityEngine.Debug.Log(elem + "set_control_target");
            }
        }
        MySerialThread.SendMessage(payload.ToArray());
    }
    //testcontrl type
    // ui consol update
    public void updateAngVal()
    {
        angleText.text = "Angel :" + MySerialThread.AngVal.ToString("F2")+"degree";
        controlText.text = "control :" + MySerialThread.controlVal.ToString("F2");
        torqueText.text = "Torque :" + MySerialThread.torqueVal.ToString("F2")+"Nm";
        targetleText.text = "Target :" + MySerialThread.TargetVal.ToString("F2");
        errText.text = "err :" + MySerialThread.errVal.ToString("F2");
        errdiffText.text = "errdiff :" + MySerialThread.errdiffVal.ToString("F2");
        errsumText.text = "errsum :" + MySerialThread.errsumVal.ToString("F2");
        buttonstate.text = "Bt_St :" + MySerialThread.buttonst;
        mech.text = "Mec :" + MySerialThread.mech;
        actuated.text = "Act :" + MySerialThread.actu.ToString();
        statusModeText.text = "Status :" + MySerialThread.Statusmode;
        Calibration.text ="calib_st :" +MySerialThread.calib_status;
        time.text = MySerialThread.current_time;
        controltype.text = "ctrl_type :"+MySerialThread.controlType;
    }
    
    private void OnApplicationQuit()
    {
        ConnectToRobot.disconnect();
    }

    public void quitApplication()
    {
        Application.Quit();
    }
}
