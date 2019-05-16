// Erstellt mit:
// ------------ VISUAL STUDIO C# 2017 --------------
//
// Erforderliche Ressource:
// ------------ Frame Network 4.5.1 ----------------
//
// Datum: 14.07.2017 LE



using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using F4SharedMem;
using F4SharedMem.Headers;
using System.Collections.ObjectModel;
using System.Reflection;
using Phcc;
using System.Speech;
using System.Speech.Synthesis;
//using Common.Serialization;
//using log4net;
using Costura;
//using Common.Application;
//using Common;
using F4KeyFile;
using System.Timers;
using Timer = System.Timers.Timer;



// Adresses for PHCC-boards (hardcoded on PIC):
//
// PHCC-CMDS: 0x44
// PHCC-40DO RightEyebrow, Caution Panel: 0x10
// PHCC-40DO LeftEyebrow, Indexers, Other Indicators: 0x12

// Adresses for PHCC-boards (hardcoded on PIC):
// For all charges from "Airdances PHCC lot".

// DOA_Stepper : 0x73, 0x72, 0x71, 0x70
// DOA_Aircore : 0x83, 0x82, 0x81, 0x80
// DOA_Servo   : 0x2C, 0x2B, 0x2A
// DOA_AnOut   : 0x23, 0x22, 0x21, 0x20
// DOA_40DO    : 0x13, 0x12, 0x11, 0x10
// DOA_7seg    : 0x34, 0x33, 0x32, 0x31, 0x30

// Definition für 7-Segment-Anzeige:
//
//
//             --a--
//            |     |
//            f     b
//            |     |
//             --g--
//            |     |
//            e     c
//            |     |
//             --d--      . DP
//
// Board-Adresse: 0x30, 
// Sub-Addr. 0x00       (Erste-7-Segment-Anzeige, usw.)
// Sub-Addr. 0 bis 31   (14 wäre Display 15,  bei 32 Displays maximum pro Karte).
//
// Data Byte für einzelne Segmente:
//
// SEGA 0x80
// SEGB 0x40
// SEGC 0x20
// SEGD 0x08
// SEGE 0x04
// SEGF 0x02
// SEGG 0x01
// SEGDP 0x10
// 
//
// OFF: 0
// ON: 1

namespace WindowsFormsApplication1
{

    public partial class Form1 : Form
    {


        // SerialPort port2;                       // Ansonsten port statt port1 ... 
        // auch unten bei den RPM-Write-Routinen!
        SerialPort Port1_Arduino_EngineGauges;     // VERSUCHSAUFBAU für mehrere Arduino-Karten.
        SerialPort Port2_Arduino_SpeedBrake;
        private static Device Port3_PHCC_Input_Output = new Device();

        public string[,] keycombo = new string[,] { };

        public string testKF;
        public int testKFx;
        // -------------------------------------------------------------------------------------
        // True, solange PHCC getestet wird.
        // Ausschalten, wenn Arduino ebenfalls läuft!
        // -------------------------------------------------------------------------------------
        public bool PHCC_Test_Environment = false;

        public sbyte altAB = 0;
        public sbyte altAB2 = 0;

        // public sbyte[] schritteTab = {0,0,0,0,0,0,0,-1,0,0,0,0,0,1,0,0}; // 1/1 resolution, bad results!!!!
        //  public sbyte[] schritteTab = {0,0,1,0,0,0,0,-1,-1,0,0,0,0,1,0,0}; // TEST2
        // public sbyte[] schritteTab = { 0, 0, -1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, -1, 0, 0 }; // TEST2
        // public sbyte[] schritteTab = { 0,  1, -1, 0, -1, 0, 0,  1,  1, 0, 0, -1, 0, -1,  1, 0 }; //TEST1
        // public sbyte[] schritteTab = { 0, -1, 1, 0, 1, 0, 0, -1, -1, 0, 0, 1, 0, 1, -1, 0 };  //  1/4 resolution

        public sbyte[] schritteTab = { 0, 0, 1, 0, 0, 0, 0, -1, -1, 0, 0, 0, 0, 1, 0, 0 }; // ALPS UHF PRESETS !-WORKS-


        public bool CW;
        public bool CCW;
        public string TestPulled;
        public string TestPulled2;
        public bool CWAlt = false;
        public bool CCWAlt = false;

        public int CCWbyte;
        public int CWbyte;
        public int wert = 0;  //sbyte
        public int wertAlt = 0;
        public bool neutral = false;

        public bool left;
        public bool right;
        public int Position;

        public int position = 0;
        public int positionAlt = 0;

        public bool on = false;

        // Variable zum Pruefen, ob FalconBMS-Software laeuft oder nicht.
        // Standard: Auf false.
        public bool _keepRunning = false;



        public Form1()
        {
            InitializeComponent();
            getAvaiblePorts();

            this.label1.Text = "";
            this.label2.Text = "";
            this.label3.Text = "";
            this.label4.Text = "";
            this.label6.Text = "";





            //  SpeechSynthesizer readerSpeech = new SpeechSynthesizer();
            //  readerSpeech = new SpeechSynthesizer();
            // readerSpeech.Rate = -2;
            // readerSpeech.Speak("Welcome back Stryker, software started. All systems are working fine. Have a good Flight, Sir!");

            //if (port1 != null)
            //    port1.Close(); 

            /// <summary>
            // Zuweisung des COM-Ports fÃ¼r die PHCC-Hardware;
            // bezieht sich auf die o.g. neue Instanz von Device bzw. myDevice. 
            // Standardwerte fuer Read-/WriteTimeout jeweils 500.
            /// </summary>
            /// 
            //port1 = new SerialPort("COM13", 9600); //9600 
            //port2 = new SerialPort("COM4", 115200); //9600 

            Port3_PHCC_Input_Output.PortName = "COM1";

            //myDevice.PortName = "COM1";
            //myDevice.SerialPort.WriteTimeout = 500;
            //myDevice.SerialPort.ReadTimeout = 500;

            /// <summary>
            // Software-Reset fuer die PHCC-Hardware bei Programmstart
            // zur Beseitigung etwaiger Störungen.
            /// </summary>
            //myDevice.Reset();
            // Port3_PHCC_Input_Output.Reset();

            //    //un-comment this line to cause the arduino to re-boot when the serial connects
            //    //port.DtrEnabled = true;
            try
            {
                //port1.Open();
                //  Port3_PHCC_Input_Output.SerialPort.Open();
                //port1.Write("X");

                //string message = port1.ReadLine();
                //Console.WriteLine(message);
                //label6.Text = message;
                Application.DoEvents();
                // label6.Text = "All Systems working!";
                _keepRunning = true;
                //return;
            }

            catch
            {

            }
        }

        // Variablen für PHCC-Eingaben / Schalterstellungen:
        public bool[] Switch = new bool[1024]; // = null;
        public bool[] _prevSwitchState = new bool[1024];
        public bool ON = true;
        public bool OFF = false;

        // Variablen für Arduiono-Karten:
        public string sendReset;

        // Variablen für Drehzahlmesser:
        public double myRPM;     // = 1; //float
        public int myRPMinteger;
        public string myRPMValue;

        // Variablen für FTIT:
        public double myFTIT;   //float
        public int myFTITinteger;
        public string myFTITValue;

        // Variablen für Öldruck:
        public float myOilPressure;
        public int myOilPressureInteger;
        public string myOilPressureValue;

        // Variablen für Düsenverstellung:
        public float myNozzlePos;
        public int myNozzlePosInteger;
        public string myNozzlePosValue;

        // Variablen für Fuel-Amount-Anzeige:
        public float myFuelFwd;
        public float myFuelAft;
        public float myFuelTotal;

        // Variablen für Pitch:
        public float myPitch;

        // Variablen für Kopfposition (falls erforderlich):
        public float myCurrentHeading;
        public int myCurrentHeadingint;
        public float myCurrentHeadx;

        // Variablen für SpeedBrake-Status (0: geschlossen, 1: 60-Grad offen):
        public float mySpeedBrake;
        public int mySpeedBrakeInteger;
        public bool SpeedBrakePowerOff = true;
        public bool SpeedBrakeClosed = true;
        public bool SpeedBrakeOpen = false;
        public string SpeedBrakeValue;

        // Variablen für Chaff- / Flare-Counter:
        public float myChaffCount;
        public float myFlareCount;
        public int _prevChaffEinerState = 99;
        public int _prevChaffZehnerState = 99;
        public int _prevFlareEinerState = 99;
        public int _prevFlareZehnerState = 99;
        public bool AutoDegree = false;
        public bool FlareLow = false;
        public bool ChaffLow = false;

        // RIGHT EYEBROW - WARNING LIGHTS:
        public bool _prevENG_FIRE;
        public bool _prevENGINE;
        public bool _prevHYD;
        public bool _prevFLCS;
        public bool _prevDbuWarn;
        public bool _prevCAN;
        public bool _prevT_L_CFG;
        public bool _prevOXY_BROW;

        public byte bOld = 0;
        public byte bNew = 0;
        public byte all = 0;

        // CAUTIION PANEL 
        // 1st ROW (starts from left):
        public bool _prevFltControlSys;
        public bool _prevElec_Fault;
        public bool _prevPROBEHEAT;
        public bool _prevcadc;
        public bool _prevCONFIG;
        public bool _prevATF_Not_Engaged;
        public bool _prevFwdFuelLow;
        public bool _prevAftFuelLow;

        public byte bOld1 = 0;
        public byte bNew1 = 0;
        public byte all1 = 0;

        // CAUTIION PANEL 
        // 2nd ROW (starts from left):
        public bool _prevEngineFault;
        public bool _prevSEC;
        public bool _prevFUEL_OIL_HOT;
        public bool _prevInlet_Icing;
        public bool _prevOverheat;
        public bool _prevEEC;
        public bool _prevBUC;

        public byte bOld2 = 0;
        public byte bNew2 = 0;
        public byte all2 = 0;

        // CAUTIION PANEL 
        // 3rd ROW (starts from left):
        public bool _prevAvionics;
        public bool _prevEQUIP_HOT;
        public bool _prevRadarAlt;
        public bool _prevIFF;
        public bool _prevNuclear;

        public byte bOld3 = 0;
        public byte bNew3 = 0;
        public byte all3 = 0;

        // CAUTIION PANEL 
        // 4th ROW (starts from left):
        public bool _prevSEAT_ARM;
        public bool _prevNWSFail;
        public bool _prevANTI_SKID;
        public bool _prevHook;
        public bool _prevOXY_LOW;
        public bool _prevCabinPress;

        public byte bOld4 = 0;
        public byte bNew4 = 0;
        public byte all4 = 0;

        // CAUTION PANEL
        public bool _prevAllLampBitsOn;


        // Variablen für Lightbits, Lightbits2, Lightbits3 & HSI_Bits:
        public int _prevMasterCaution = 0;
        public int _prevTF = 0;
        public int _prevONGROUND = 0;
        public int _prevFlcs_ABCD = 0;
        public int _prevLEFlaps = 0;
        public int _prevFuelLow = 0;
        public int _prevECM = 0;

        public int _prevAutoPilotOn = 0;
        public int _prevTFR_STBY = 0;

        public int _prevAOA_Above = 0;
        public int _prevAOA_OnPath = 0;
        public int _prevAOA_Below = 0;

        public int _prevRefuelRDY = 0;
        public int _prevRefuelAR = 0;
        public int _prevRefuelDSC = 0;

        public int _prevHandOff = 0;
        public int _prevLaunch = 0;
        public int _prevPriMode = 0;
        public int _prevNaval = 0;
        public int _prevUnk = 0;
        public int _prevTgtSep = 0;

        public int _prevAuxSrch = 0;
        public int _prevAuxAct = 0;
        public int _prevAuxLow = 0;
        public int _prevAuxPwr = 0;

        public int _prevEcmPwr = 0;
        public int _prevEcmFail = 0;

        public int _prevEPUOn = 0;
        public int _prevJFSOn = 0;

        public long _prevTFR_ENGAGED = 0;
        public long _prevGEARHANDLE = 0;

        public int _prevFlcsPmg = 0;
        public int _prevMainGen = 0;
        public int _prevStbyGen = 0;
        public int _prevEpuGen = 0;
        public int _prevEpuPmg = 0;
        public int _prevToFlcs = 0;
        public int _prevFlcsRly = 0;
        public int _prevBatFail = 0;

        public int _prevHydrazine = 0;
        public int _prevAir = 0;

        public int _prevLef_Fault = 0;
        public int _prevOnGround = 0;
        public int _prevFlcsBitRun = 0;
        public int _prevFlcsBitFail = 0;
        public int _prevNoseGearDown = 0;
        public int _prevLeftGearDown = 0;
        public int _prevRightGearDown = 0;
        public int _prevParkBrakeOn = 0;
        public int _prevPower_Off = 0;

        public int _prevSpeedBrake = 0;

        public int _prevSysTest = 0;

        public int _prevOuterMarker = 0;
        public int _prevMiddleMarker = 0;
        public int _prevFlying = 0;
        public int _prevChaffLow = 0;
        public int _prevFlareLow = 0;

        public byte UHFPresetEiner;
        public byte UHFPresetZehner;
        public byte _prevUHFPresetEiner;
        public byte _prevUHFPresetZehner;

        public bool _prevMCaution;

        public bool ENGINE_TEST = false;

        // Allgemeine Zählervariable:
        public int i = 0;

        // ----------------------------------------------------------------------------------//
        //       Nur erforderlich, wenn als Anwendung auf einem Single-PC konzipiert!        // 
        // ----------------------------------------------------------------------------------//
        //    [DllImport("User32.dll")]                                                      //
        //     static extern IntPtr FindWindow(string lpClassName, string lpWindowName);     //
        //    [DllImport("User32.dll")]                                                     //
        //    static extern int SetForegroundWindow(IntPtr hWnd);                            //
        // ----------------------------------------------------------------------------------//

        private Thread DataThread;
        private Thread RPMThread;
        private Thread NozzleThread;
        private Thread FTITThread;
        private Thread OilThread;
        private Thread SimOutputThread;
        private Thread SpeedBrakeThread;
        private Thread TestingDiversesThread;
        private Thread SwitchesThread;
        private Thread Blink_ElecSysThread;

        private static Device _phccDevice = new Device();
        // Verfügbare COM-Ports auslesen und in Combo-Box zur Verfügung stellen:
        public void getAvaiblePorts()
        {
            String[] ports = SerialPort.GetPortNames();
            comboBox1.Items.AddRange(ports);
            button2.Enabled = false;

        }



        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBox1.Text == "")
                {
                    // Fals kein Com-Port gewählt:
                    this.label1.Text = "Please select COM-Port!";
                }

                else
                {
                    if (PHCC_Test_Environment != true)
                    {
                        Port1_Arduino_EngineGauges = new SerialPort(comboBox1.Text, 115200); //9600

                        this.label1.Text = "Self-Test running...";
                        this.label2.Text = "";
                        this.label3.Text = "";
                        this.label4.Text = "";
                        this.label6.Text = "";
                        Wait(1500);
                        try
                        {
                            //Port3_PHCC_Input_Output.Reset();
                            //  Port3_PHCC_Input_Output.SetIdle();
                            //  Wait(1500);
                        }

                        catch
                        {
                            this.label1.Text = "Self-Test accomplished.";
                            Wait(1000);
                            this.label6.ForeColor = Color.Red;
                            this.label6.Text = "Connection not established!";
                            //  Wait(1000);
                            this.label2.ForeColor = Color.Red;
                            this.label2.Text = "PHCC has NO POWER!!!";
                            Wait(500);

                            for (int z = 1; z <= 3;)
                            {
                                this.label3.Text = "";
                                Wait(500);
                                this.label3.Text = "Check powersupply and try again...";
                                Wait(500);
                                z++;
                            }
                            return;
                        }
                    }

                    if (PHCC_Test_Environment == true)
                    {
                        //_phccDevice = new Device("COM1");
                        //   var firmwareVersion = _phccDevice.FirmwareVersion;

                        this.label1.Text = "Self-Test running...";
                        this.label2.Text = "";
                        this.label3.Text = "";
                        this.label4.Text = "";
                        this.label6.Text = "";
                        Wait(1500);
                        try
                        {
                            //Port3_PHCC_Input_Output.Reset();
                            // Port3_PHCC_Input_Output.SetIdle();
                            Wait(1500);
                        }

                        catch
                        {
                            this.label1.Text = "Self-Test accomplished.";
                            Wait(1000);
                            this.label6.ForeColor = Color.Red;
                            this.label6.Text = "Connection not established!";
                            //  Wait(1000);
                            this.label2.ForeColor = Color.Red;
                            this.label2.Text = "PHCC has NO POWER!!!";
                            Wait(500);

                            for (int z = 1; z <= 3;)
                            {
                                this.label3.Text = "";
                                Wait(500);
                                this.label3.Text = "Check powersupply and try again...";
                                Wait(500);
                                z++;
                            }
                            return;
                        }

                        this.label2.ForeColor = Color.Green;
                        this.label2.Text = "PHCC communication started!";
                        //  Wait(1000);
                        //   this.label2.Text = "PHCC Firmware Version: " + firmwareVersion;
                        Wait(1000);
                        this.label3.ForeColor = Color.Orange;
                        this.label3.Text = "Arduino 1 - Enginegauge communication OFF";
                        Wait(1000);
                        this.label4.ForeColor = Color.Orange;
                        this.label4.Text = "Arduino 2 - Speedbrake communication OFF";
                        Wait(500);
                    }

                    button1.Enabled = false;
                    button2.Enabled = true;
                    progressBar1.Value = 100;

                    using (Reader myReader = new F4SharedMem.Reader())

                        if (myReader.IsFalconRunning == true)
                        {

                            this.label6.Text = "Datalink established - All Systems working!";
                            label1.Invoke(new Action<string>(s => { label1.Text = s; label1.ForeColor = Color.Green; }), "Falcon active");
                        }

                        else
                        {
                            // SpeechSynthesizer readerSpeech = new SpeechSynthesizer();
                            //readerSpeech = new SpeechSynthesizer();
                            // readerSpeech.Speak("Datalink disabled!- Falcon B M S not active!");

                            label6.Invoke(new Action<string>(s => { label6.Text = s; label6.ForeColor = Color.Red; }), "Datalink disabled...");
                            label1.Invoke(new Action<string>(s => { label1.Text = s; label1.ForeColor = Color.Red; }), "Falcon NOT active");
                        }

                    this.RPMThread = new Thread(new ThreadStart(RPM));
                    this.NozzleThread = new Thread(new ThreadStart(Nozzle));
                    this.FTITThread = new Thread(new ThreadStart(FTIT));
                    this.DataThread = new Thread(new ThreadStart(Data));
                    this.OilThread = new Thread(new ThreadStart(Oil));
                    this.SimOutputThread = new Thread(new ThreadStart(SimOutput));
                    this.SpeedBrakeThread = new Thread(new ThreadStart(Speedbrake));
                    this.TestingDiversesThread = new Thread(new ThreadStart(TestingDiverses));
                    this.SwitchesThread = new Thread(new ThreadStart(Switches));
                    this.Blink_ElecSysThread = new Thread(new ThreadStart(Blink_ElecSys));
                    //  DataThread.Start();   // vorübergehend abgeschaltet!
                    // RPMThread.Start();     // vorübergehend abgeschaltet!
                    //  NozzleThread.Start(); // vorübergehend abgeschaltet!
                    //   FTITThread.Start();  // vorübergehend abgeschaltet!
                    //   OilThread.Start();   // vorübergehend abgeschaltet!
                    SimOutputThread.Start();
                    Blink_ElecSysThread.Start();
                    // TestingDiversesThread.Start();
                    //SwitchesThread.Start();
                    try
                    {
                        Port3_PHCC_Input_Output.DoaSendRaw(0x44, 34, 0);
                        Thread.Sleep(5);

                        // Port3_PHCC_Input_Output.DoaSendRaw(0x30, 3, 0);
                        //  Thread.Sleep(5);
                        //  Port3_PHCC_Input_Output.DoaSendRaw(0x30, 4, 0);
                        // Port1_Arduino_EngineGauges.Open();
                    }

                    catch
                    {
                    }
                }
            }

            catch { }
            Application.DoEvents();

            _prevSwitchState[1] = false;

        }

        //  catch (UnauthorizedAccessException)
        //      {
        //          label1.Text = "Unauthorized Access!";
        //       }

        // currently working
    //    OuterMarker  = 0x01,	// defined in HsiBits    - slow flashing for outer marker
	//	MiddleMarker = 0x02,	// defined in HsiBits    - fast flashing for middle marker
	//	PROBEHEAT    = 0x04,	// defined in LightBits2 - probeheat system is tested
	//	AuxSrch      = 0x08,	// defined in LightBits2 - search function in NOT activated and a search radar is painting ownship
	//	Launch       = 0x10,	// defined in LightBits2 - missile is fired at ownship
	//	PriMode      = 0x20,	// defined in LightBits2 - priority mode is enabled but more than 5 threat emitters are detected
	//	Unk          = 0x40,	// defined in LightBits2 - unknown is not active but EWS detects unknown radar

		// not working yet, defined for future use
	//	Elec_Fault   = 0x80,	// defined in LightBits3 - non-resetting fault
	//	OXY_BROW     = 0x100,	// defined in LightBits  - monitor fault during Obogs
	//	EPUOn        = 0x200,	// defined in LightBits3 - abnormal EPU operation
	//	JFSOn_Slow   = 0x400,	// defined in LightBits3 - slow blinking: non-critical failure - 1x pro Sek.
	//	JFSOn_Fast   = 0x800,	// defined in LightBits3 - fast blinking: critical failure - 2x pro Sek.



        public void Blink_ElecSys()
        { // WORKS! 16.05.2019 LE.
            using (Reader myReader = new F4SharedMem.Reader())

                while (_keepRunning == true)
                {
                    if (myReader.IsFalconRunning == true)
                    {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {
                            {
                                FlightData mySharedMem8 = myReader.GetCurrentData();

                                if ((mySharedMem8.blinkBits & 0x04) == 0)
                                { this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "NOT Blinking"); }
                                if ((mySharedMem8.blinkBits & 0x04) != 0)
                                { this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "Blinking"); }
                                
                                //falls neuer Zustand erreicht:
                                if (((LightBits2)mySharedMem8.lightBits2 & LightBits2.PROBEHEAT)
                                   == LightBits2.PROBEHEAT != _prevPROBEHEAT)
                                {
                                    mySharedMem8 = myReader.GetCurrentData();
                                  
                                    //falls Lightbit ON ist:
                                   if ((mySharedMem8.blinkBits & 0x04) != 0)
                                        {

                                    while (((LightBits2)mySharedMem8.lightBits2 & LightBits2.PROBEHEAT) == LightBits2.PROBEHEAT)
                                    {
                                       
                                            mySharedMem8 = myReader.GetCurrentData();
                                            bNew1 = 4;
                                            all1 = (byte)(bOld1 + bNew1);
                                            Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                            bOld1 = all1;
                                            on = true;
                                            Thread.Sleep(125); //schnelle Blinkfolge

                                            mySharedMem8 = myReader.GetCurrentData();
                                            bNew1 = 4;
                                            all1 = (byte)(bOld1 - bNew1);
                                            Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                            bOld1 = all1;
                                            on = false;
                                            Thread.Sleep(125); //schnelle Blinkfolge
                                        }

                                        if (((LightBits2)mySharedMem8.lightBits2 & LightBits2.PROBEHEAT) != LightBits2.PROBEHEAT)
                                        {
                                            if (on == true) { all1 = (byte)(bOld1 - bNew1); }
                                            if (on != true) { all1 = (byte)(bOld1); }
                                            Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                            on = false;
                                            //   break;
                                        }
                                   
                                    _prevPROBEHEAT = ((LightBits2)mySharedMem8.lightBits2 &
                                        LightBits2.PROBEHEAT) == LightBits2.PROBEHEAT;
                                    on = false;
                                   }
                                }
                               
                        
                                // NOT IMPLEMENTED IN BMS,
                                // FOR FUTURE USAGE! 10.05.2019 LE.
                                //
                                //        if (((LightBits3)mySharedMem8.lightBits3 & LightBits3.Elec_Fault)
                                //       == LightBits3.Elec_Fault != _prevElec_Fault)
                                //        {

                                //            while (((LightBits3)mySharedMem8.lightBits3 & LightBits3.Elec_Fault) == LightBits3.Elec_Fault)
                                //            {

                                //                mySharedMem8 = myReader.GetCurrentData();

                                //                if ((mySharedMem8.blinkBits & 0x80) == 0)
                                //               { this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "NOT Blinking"); }
                                //                if ((mySharedMem8.blinkBits & 0x80) != 0)
                                //                { this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "Blinking"); }

                                //                bNew1 = 2;
                                //                    all1 = (byte)(bOld1 + bNew1);
                                //                    Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                //                    bOld1 = all1;
                                //                    on = true;
                                //                    Thread.Sleep(500);


                                //                    bNew1 = 2;
                                //                    all1 = (byte)(bOld1 - bNew1);
                                //                    Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                //                    bOld1 = all1;
                                //                   on = false;
                                //                    Thread.Sleep(500);
                                //                }


                                //            if (((LightBits3)mySharedMem8.lightBits3 & LightBits3.Elec_Fault) != LightBits3.Elec_Fault)
                                //            {

                                //                Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                //                break;


                                //            }
                                //            _prevElec_Fault = ((LightBits3)mySharedMem8.lightBits3 &
                                //            LightBits3.Elec_Fault) == LightBits3.Elec_Fault;
                                //       }
                            }
                        }


                    }
                }
        }

        
                          

        public void Oil()
        {
            using (Reader myReader = new F4SharedMem.Reader())

                while (_keepRunning == true)
                {
                    if (myReader.IsFalconRunning == true)
                    {
                       
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {

                            {
                                FlightData mySharedMem1 = myReader.GetCurrentData();
                                
                                myOilPressure = mySharedMem1.oilPressure;

                                this.textBox7.Invoke(new Action<string>(s => { textBox7.Text = s; }), myOilPressure.ToString());

                                // Beginn der Öldruck-Skala bei 0, Ende bei 100%.
                                // Gesamtwinkel der Skala von 0 bis 100% = 310 Grad!

                                int Untergrenze = 0;
                                int Obergrenze = 100;

                                // myoilPressureInteger = (Umwandlung zu Integer)(3 Schritte pro Grad(Gesamtwinkelzahl der Skalenwerte)((Real gemessener Wert aus BMS) - Untergrenze Skala) / (Obergrenze Skala - Untergrenze Skala)

                                myOilPressureInteger = (int)(3 * (310 * (myOilPressure - Untergrenze) / (Obergrenze - Untergrenze)));
                                this.textBox12.Invoke(new Action<string>(s => { textBox12.Text = s; }), myOilPressureInteger.ToString());

                                myOilPressureValue = "D";
                                myOilPressureValue += myOilPressureInteger.ToString();
                                myOilPressureValue += "\n";
                                Port1_Arduino_EngineGauges.Write(myOilPressureValue);

                                // Beginn der "mittleren" Skala bei FTIT: Skalen-Werte 7 (Min.) bis 10 (Max.).
                                // Gesamtwinkelzahl der Skalenwerte 7 bis 10: 165 Grad (235 Schritte bei Motor X27).

                                Application.DoEvents();

                            }
                        }
                    }
                }
        }

        public void Nozzle()
        {
            using (Reader myReader = new F4SharedMem.Reader())

                while (_keepRunning == true)
                {
                    if (myReader.IsFalconRunning == true)
                    {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {

                            {
                                FlightData mySharedMem2 = myReader.GetCurrentData();

                                myNozzlePos = mySharedMem2.nozzlePos;
                                myNozzlePos = myNozzlePos * 100;
                                this.textBox5.Invoke(new Action<string>(s => { textBox6.Text = s; }), myNozzlePos.ToString());

                                // Beginn der Nozzle-Skala bei 0, Ende bei 100%.
                                // Gesamtwinkel der Skala von 0 bis 100% = 238 Grad!

                                int Untergrenze = 0;
                                int Obergrenze = 100;

                                // mynozzlePosInteger = (Umwandlung zu Integer)(3 Schritte pro Grad(Gesamtwinkelzahl der Skalenwerte)((Real gemessener Wert aus BMS) - Untergrenze Skala) / (Obergrenze Skala - Untergrenze Skala)

                                myNozzlePosInteger = (int)(3 * (238 * (myNozzlePos - Untergrenze) / (Obergrenze - Untergrenze)));
                                this.textBox11.Invoke(new Action<string>(s => { textBox11.Text = s; }), myNozzlePosInteger.ToString());

                                myNozzlePosValue = "C";
                                myNozzlePosValue += myNozzlePosInteger.ToString();
                                myNozzlePosValue += "\n";
                                Port1_Arduino_EngineGauges.Write(myNozzlePosValue);

                                Application.DoEvents();
                            }
                        }
                    }
                }
        }

       

        public void RPM()
        {
            // FUNKTIONIERT!!!! 
            // Skala klein und mittel implementiert.
            // Skala "10 bis 12" fehlt noch.
            // -----------------------------
            // Stand: 01.11.2016 LE

            using (Reader myReader = new F4SharedMem.Reader())

                while (_keepRunning == true)
                {
                    if (myReader.IsFalconRunning == true)
                    {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {

                            {
                                FlightData mySharedMem4 = myReader.GetCurrentData();

                                myRPM = mySharedMem4.rpm;

                                this.textBox5.Invoke(new Action<string>(s => { textBox5.Text = s; }), myRPM.ToString());

                                // Beginn der "kleinen" RPM-Skala bei 0, Ende bei 60%.
                                // Gesamtwinkel der Skala von 0 bis 60% = 130 Grad!

                                if (myRPM <= 60)
                                {
                                    int Untergrenze = 0;
                                    int Obergrenze = 60;

                                    // myRPMinteger = (Umwandlung zu Integer)(3 Schritte pro Grad(Gesamtwinkelzahl der Skalenwerte)((Real gemessener Wert aus BMS) - Untergrenze Skala) / (Obergrenze Skala - Untergrenze Skala)

                                    myRPMinteger = (int)(3 * (130 * (myRPM - Untergrenze) / (Obergrenze - Untergrenze)));

                                    myRPMValue = "B";
                                    myRPMValue += myRPMinteger.ToString();
                                    myRPMValue += "\n";
                                    Port1_Arduino_EngineGauges.Write(myRPMValue);
                                }

                                // Beginn der "mittleren" RPM-Skala bei 60, Ende bei 100%.
                                // Gesamtwinkelzahl der Skala von 60 bis 100% = 200 Grad!.

                                if (myRPM > 60)
                                {
                                    int Untergrenze2 = 60;
                                    int Obergrenze2 = 100;

                                    // myRPMinteger = (Umwandlung zu Integer)(3 Schritte pro Grad(Gesamtwinkelzahl der Skalenwerte)((Real gemessener Wert aus BMS) - Untergrenze Skala) / (Obergrenze Skala - Untergrenze Skala)
                                    // myRPMinteger = myRPMinteger plus Schrittzahl des Endwertes der nächstkleineren Skala.

                                    myRPMinteger = (int)(3 * (200 * (myRPM - Untergrenze2) / (Obergrenze2 - Untergrenze2)));
                                    myRPMinteger = myRPMinteger + 390;

                                    myRPMValue = "B";
                                    myRPMValue += myRPMinteger.ToString();
                                    myRPMValue += "\n";
                                    Port1_Arduino_EngineGauges.Write(myRPMValue);
                                }

                                Application.DoEvents();
                            }
                        }
                    }
                }
        }

        public void FTIT()
        {
            // FUNKTIONIERT!!!! 
            // Skala klein und mittel implementiert.
            // Skala "10 bis 12" fehlt noch.
            // -----------------------------
            // Stand: 01.11.2016 LE

            using (Reader myReader = new F4SharedMem.Reader())

                while (_keepRunning == true)
                {
                    if (myReader.IsFalconRunning == true)
                    {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {

                            {
                                FlightData mySharedMem3 = myReader.GetCurrentData();

                                myFTIT = mySharedMem3.ftit;

                                this.textBox1.Invoke(new Action<string>(s => { textBox1.Text = s; }), myFTIT.ToString());

                                // Beginn der "kleinen" Skala bei FTIT: Skalen-Werte 2 (Min.) bis 7 (Max.).
                                // Gesamtwinkelzahl der Skalenwerte 2 bis 7: 100 Grad (300 Schritte bei Motor X27).

                                if (myFTIT <= 7)
                                {
                                    int Untergrenze = 2;
                                    int Obergrenze = 7;

                                    // myFTITinteger = (Umwandlung zu Integer)(3 Schritte pro Grad(Gesamtwinkelzahl der Skalenwerte)((Real gemessener Wert aus BMS) - Untergrenze Skala) / (Obergrenze Skala - Untergrenze Skala)

                                    myFTITinteger = (int)(3 * (100 * ((myFTIT) - Untergrenze) / (Obergrenze - Untergrenze)));

                                    myFTITValue = "A";
                                    if (myFTITinteger <= 0) { myFTITinteger = 0; } // erforderlich??? 14.07.17 LE
                                    myFTITValue += myFTITinteger.ToString();
                                    this.textBox10.Invoke(new Action<string>(s => { textBox10.Text = s; }), myFTITValue.ToString());
                                    myFTITValue += "\n";
                                    Port1_Arduino_EngineGauges.Write(myFTITValue);
                                }

                                // Beginn der "mittleren" Skala bei FTIT: Skalen-Werte 7 (Min.) bis 10 (Max.).
                                // Gesamtwinkelzahl der Skalenwerte 7 bis 10: 165 Grad (235 Schritte bei Motor X27).

                                if (myFTIT > 7)
                                {
                                    int Untergrenze2 = 7;
                                    int Obergrenze2 = 10;

                                    // myFTITinteger = (Umwandlung zu Integer)(3 Schritte pro Grad(Gesamtwinkelzahl der Skalenwerte)((Real gemessener Wert aus BMS) - Untergrenze Skala) / (Obergrenze Skala - Untergrenze Skala)
                                    // myFTITinteger = myFTITinteger plus Schrittzahl des Endwertes der nächstkleineren Skala.
                                    //
                                    myFTITinteger = (int)(3 * (165 * (myFTIT - Untergrenze2) / (Obergrenze2 - Untergrenze2)));
                                    myFTITinteger = myFTITinteger + 300;

                                    myFTITValue = "A";
                                    myFTITValue += myFTITinteger.ToString();
                                    this.textBox10.Invoke(new Action<string>(s => { textBox10.Text = s; }), myFTITValue.ToString());
                                    myFTITValue += "\n";
                                    Port1_Arduino_EngineGauges.Write(myFTITValue);
                                }

                                Application.DoEvents();
                            }
                        }
                    }
                }
        }

        public void Speedbrake()
        {
            using (Reader myReader = new F4SharedMem.Reader())

                while (_keepRunning == true)
                {
                    if (myReader.IsFalconRunning == true)
                    {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {

                            { FlightData mySharedMem6 = myReader.GetCurrentData();

                                mySpeedBrake = mySharedMem6.speedBrake;
                                mySpeedBrakeInteger = (int)mySpeedBrake;

                                if (mySpeedBrakeInteger == 0)
                                {
                                    if (SpeedBrakePowerOff == true)
                                    {
                                        if (SpeedBrakeOpen == false)
                                        {
                                            SpeedBrakeValue = "A";
                                            SpeedBrakeValue += "\n";
                                            Port2_Arduino_SpeedBrake.Write(SpeedBrakeValue);
                                            SpeedBrakePowerOff = false;
                                            SpeedBrakeClosed = true;
                                            SpeedBrakeOpen = false;
                                            SpeedBrakeValue = "";
                                        }
                                    }
                                }

                                if ((mySpeedBrake > 0) && (SpeedBrakeOpen == false))
                                {
                                    SpeedBrakeValue = "B";
                                    SpeedBrakeValue += "\n";
                                    Port2_Arduino_SpeedBrake.Write(SpeedBrakeValue);
                                    SpeedBrakePowerOff = false;
                                    SpeedBrakeClosed = false;
                                    SpeedBrakeOpen = true;
                                    SpeedBrakeValue = "";
                                }

                                if ((mySpeedBrake == 0) && (SpeedBrakeOpen == true))
                                {
                                    SpeedBrakeValue = "C";
                                    SpeedBrakeValue += "\n";
                                    Port2_Arduino_SpeedBrake.Write(SpeedBrakeValue);
                                    SpeedBrakePowerOff = false;
                                    SpeedBrakeClosed = true;
                                    SpeedBrakeOpen = false;
                                    SpeedBrakeValue = "";
                                }
                                Application.DoEvents();
                            }
                        }
                    }
                }
        }

        public void Data()
        {
            // Test-Umgebung für diverse Funktionen.
            // In der Final-Version nicht erforderlich!

            using (Reader myReader = new F4SharedMem.Reader())

                while (_keepRunning == true)
                {
                    if (myReader.IsFalconRunning == true)
                    {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {
                            FlightData myFlightData = new FlightData();
                            FlightData mySharedMem0 = myReader.GetCurrentData();
                            // IntellivibeData <--- Status des Fliegers abfragen!
                            //---------------------------------------------------

                            //byte[] pilotsStatus = mySharedMem0.pilotsStatus.ToArray();
                            //Byte flags = pilotsStatus[0];
                            //String bin = Convert.ToString(flags, 2);  //    10110 (binär)

                            byte[] myArr = (byte[])mySharedMem0.pilotsStatus.ToArray();
                            //this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), myArr[0].ToString());
                            //if (myArr[0] == 0)
                            //{
                            //    this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "in UI");

                            //}   

                            //    if (myArr[0] == 1)
                            //    {
                            //        this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "Loading");

                            //    }
                            //    if (myArr[0] == 2)
                            //    {
                            //        this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "Ready and Waiting...");

                            //    }
                            if (myArr[0] == 3)
                            {
                                this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "Flying");

                            }
                            //    if (myArr[0] == 4)
                            //    {
                            //        this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "DEAD");

                            //    }
                            //    if (myArr[0] == 4)
                            //    {
                            //        this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "Unknown");

                            //    }

                            //if ((mySharedMem0.hsiBits & 0x80000000) == 0)
                            //{
                            //    this.label10.Invoke(new Action<string>(s => { label10.Text = s; }),"in UI");
                            //    break;
                            // }

                            Application.DoEvents();

                            //if ((pilotsStatus[0] != 0)) { this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "in UI"); }
                            //if ((pilotsStatus[0] != 1)) { this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "Loading"); }
                            //if ((pilotsStatus[0] != 2)) { this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "Waiting"); }
                            //if ((pilotsStatus[0] != 4)) { this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "Flying"); }
                            //if ((pilotsStatus[4] & 1) != 0) { this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "Dead"); }
                            //if ((pilotsStatus[5] & 1) != 0) { this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "Unknown"); }
                            //switch (pilotsStatus[0])
                            //{
                            //    case 0:
                            //        {
                            //            
                            //            break;
                            //        }
                            //    case 1:
                            //        {
                            //            this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "LOADING");
                            //            break;
                            //        }
                            //    case 2:
                            //        {
                            //            this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "WAITING");
                            //            break;
                            //        }
                            //    case 3:
                            //        {
                            //            this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "Flying");
                            //            break;
                            //        }
                            //    case 4:
                            //        {
                            //            this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "DEAD");
                            //            break;
                            //        }
                            //    case 5:
                            //        {
                            //            this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "UNKNOWN");
                            //            break;
                            //        }
                            //}
                        }
                    }
                }
        }
        public void TestingDiverses()
        {
            // Neue Instanz von Reader erzeugen, 
            // um die Daten von FalconBMS auslesen zu können:
            // using (Reader myReader = new Reader(FalconDataFormats.BMS4))  

            F4Callbacks F42 = new F4Callbacks();
            using (Reader myReader = new F4SharedMem.Reader())
                while (_keepRunning == true)
                {
                    if (myReader.IsFalconRunning == true)
                    {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {




                            /*
                            //Daten aus Shared-Memory auslesen:
                            FlightData myFlightData = new FlightData();
                            FlightData myCurrentData = myReader.GetCurrentData();

                            // Variablenzuweisung.

                            // Test - Kopfbewegung:
                            // Kopfbewegung wird registriert!
                            // links bis MINUS 320, rechts bis PLUS 320!
                            // Falcon.exe mit "-head" starten!
                            // bei +70 / -70 Grad Kopfdrehung ist Hud aus dem Sichtbereich, 
                            // automatische Abschaltung könnte erfolgen!

                            // float myCurrentHeadx = myCurrentData.headYaw;
                            // int myCurrentHeadxint = (int)(myCurrentHeadx*100);

                            myCurrentHeadx = myCurrentData.headYaw;
                            double Pi1 = Math.PI;
                            float a1 = (float)Pi1; // Umwandlung von Double in Float
                            myCurrentHeadx = myCurrentHeadx * (180 / a1); // Umrechnung von Radians in Degrees: Wert * (180/Pi)

                            label9.Invoke(new Action<string>(s => { label9.Text = s; }), myCurrentHeadx.ToString()); //myCurrentHeadxint;
                            label8.Invoke(new Action<string>(s => { label8.Text = s; }), myCurrentHeadx.ToString());

                            // Heading / Compass:

                            myCurrentHeading = myCurrentData.currentHeading;
                            textBox8.Invoke(new Action<string>(s => { textBox8.Text = s; }), myCurrentHeading.ToString());

                            myCurrentHeadingint = (int)(myCurrentHeading + 0.5);
                            myCurrentHeadingint = (myCurrentHeadingint * 3);
                            try
                            {
                                //    test4 = myCurrentHeadingint.ToString();
                                //    port1.Write("A");
                                //    test4 += "\n";
                                //    port1.Write(test4);
                            }

                            //myCurrentHeadingint = 0;}
                            catch { }


                            // if (myCurrentHeadingint == 360) myCurrentHeadingint = 0;
                            //test4 = myCurrentHeadingint.ToString();
                            {
                                label7.Invoke(new Action<string>(s => { label7.Text = s; }), myCurrentHeadingint.ToString());
                            }

                            if (myCurrentHeadingint == 83)
                            {
                                //  KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.G, true, false);
                                Thread.Sleep(100); //wait 5 milliseconds --> Wert 1200 fÃƒÂ¼r Eject-Handle!
                                // KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LShift, false, true); //RELEASE the "F1" key  - geÃƒÂ¤ndert auf G.  
                            }

                            if (myCurrentHeadingint == 82)
                            {
                                // KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.G, false, true);

                                //KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LShift, true, false);
                                //KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, true, false);
                                //KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.F2, true, false);
                                //Thread.Sleep(100); //wait 5 milliseconds --> Wert 1200 fÃƒÂ¼r Eject-Handle!
                                //// KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LShift, false, true); //RELEASE the "F1" key  - geÃƒÂ¤ndert auf G.  
                                //KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LShift, false, true);
                                //KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, false, true);
                                //KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.F2, false, true);
                            }

                            // Öldruck:
                            myoilPressure = myCurrentData.oilPressure;
                            textBox7.Invoke(new Action<string>(s => { textBox7.Text = s; }), myoilPressure.ToString());

                            // RPM:
                            myRPM = myCurrentData.rpm;
                            textBox5.Invoke(new Action<string>(s => { textBox5.Text = s; }), myRPM.ToString());

                            //Device myDevice = new Device();
                            //myDevice.PortName = "COM1";
                            //myDevice.DoaSendRaw(0x70, 0x0, 50);

                            Application.DoEvents();

                            // !! Wichtig, ansonsten "hängt" die gesamte Anwendung:
                            Application.DoEvents();

                            // Kraftstoffanzeige.
                            // Hinzu kommt noch: INTERNAL und EXTERNAL FUEL!
                            // (Hier noch kein Code geschrieben, folgt noch).
                            myFuelFwd = myCurrentData.fwd;
                            //textBox2.Text = myFuelFwd.ToString();
                            //label2.Text = "FWD";
                            textBox2.Invoke(new Action<string>(s => { textBox2.Text = s; }), myFuelFwd.ToString());
                            label2.Invoke(new Action<string>(s => { label2.Text = s; }), "FWD");

                            myFuelAft = myCurrentData.aft;
                            //textBox3.Text = myFuelAft.ToString();
                            //label3.Text = "AFT";
                            textBox3.Invoke(new Action<string>(s => { textBox3.Text = s; }), myFuelAft.ToString());
                            label3.Invoke(new Action<string>(s => { label3.Text = s; }), "AFT");

                            myFuelTotal = myCurrentData.total;
                            //textBox4.Text = myFuelTotal.ToString();
                            //label4.Text = "Total";
                            textBox4.Invoke(new Action<string>(s => { textBox4.Text = s; }), myFuelTotal.ToString());
                            label4.Invoke(new Action<string>(s => { label4.Text = s; }), "Total");

                            myPitch = myCurrentData.pitch;
                            double Pi = Math.PI;
                            float a = (float)Pi; // Umwandlung von Double in Float
                            myPitch = myPitch * (180 / a); // Umrechnung von Radians in Degrees: Wert * (180/Pi)
                            textBox9.Invoke(new Action<string>(s => { textBox9.Text = s; }), myPitch.ToString());

                            */

                        }
                    }
                }

        }

        public void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBox1.Text == "")
                {
                    label1.Text = "Please select COM-Port!";
                }
                else
                {
                    // port1 = new SerialPort(comboBox1.Text, 9600);

                    //if (port1 != null)
                    //    port1.Close();
                    label6.Invoke(new Action<string>(s => { label6.Text = s; label6.ForeColor = Color.Red; }), "Datalink disabled...");
                    //this.label6.Text = "Datalink disabled....";
                    //textBox1.Text = "N/A";
                    //this.label6.Text = " ";


                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 34, 0);
                    Thread.Sleep(5);


                    if ((DataThread != null) && (DataThread.IsAlive))
                    {
                        DataThread.Abort();
                        DataThread.Join();
                    }
                    if ((RPMThread != null) && (RPMThread.IsAlive))
                    {
                        RPMThread.Abort();
                        RPMThread.Join();
                    }

                    if ((NozzleThread != null) && (NozzleThread.IsAlive))
                    {
                        NozzleThread.Abort();
                        NozzleThread.Join();
                    }

                    if ((FTITThread != null) && (FTITThread.IsAlive))
                    {
                        FTITThread.Abort();
                        FTITThread.Join();
                    }
                    if ((OilThread != null) && (OilThread.IsAlive))
                    {
                        OilThread.Abort();
                        OilThread.Join();
                    }

                    button1.Enabled = true;

                    if (PHCC_Test_Environment != true)
                    {
                        if (myNozzlePosValue != "0" || myFTITValue != "0" || myRPMValue != "0" || myOilPressureValue != "0")
                        {
                            sendReset = "R";
                            sendReset += "\n";
                            Port1_Arduino_EngineGauges.Write(sendReset);
                            myOilPressureValue = "0";
                            myRPMValue = "0";
                            myFTITValue = "0";
                            myNozzlePosValue = "0";

                            if (Port1_Arduino_EngineGauges != null)
                                Port1_Arduino_EngineGauges.Close();

                            progressBar1.Value = 0;
                            button2.Enabled = false;
                        }
                    }

                    else
                    {
                        if (PHCC_Test_Environment == true)
                        {
                            label1.Text = "Running PHCC-ENVIRONMENT only!";
                            progressBar1.Value = 0;
                            button2.Enabled = false;
                        }
                        else
                        {
                            label1.Text = "Motors are still zeroized!";
                        }
                    }
                }
            }

            catch (UnauthorizedAccessException)
            {
                label1.Text = "Unauthorized Access!";
            }
        }

        // Testbutton im Interface
        public void button3_Click(object sender, EventArgs e)
        {

            F4Callbacks F4 = new F4Callbacks();
            F4.SimTogglePaused();

            //  F4.SimSeatOff();
            //  SpeechSynthesizer readerSpeech = new SpeechSynthesizer();
            //  readerSpeech = new SpeechSynthesizer();
            //  readerSpeech.SpeakAsync("Simulation stopped - Sir!");
        }

        // Testbutton im Interface

        public void Wait(int ms)
        {
            // Warteschleife - offensichtlich besser als "Thread.Sleep()"??? 14.07.2017

            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < ms)
                Application.DoEvents();
        }

        private void Switches()
        {
            using (Reader myReader = new F4SharedMem.Reader())  //Neue Instanz von Reader, um die Daten von FalconBMS auslesen zu kÃƒÂ¶nnen.
            {
                while (_keepRunning == true)
                {
                    if (myReader.IsFalconRunning == true)
                    {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {

                            F4Callbacks F4 = new F4Callbacks();
                            Switch = Port3_PHCC_Input_Output.DigitalInputs;
                            // Key2 @ Key64-board, Slot 1.
                            //        EjectHandlePulled = Port3_PHCC_Input_Output.DigitalInputs.GetValue(1).ToString(); // Zahlenwert ÃƒÂ¤ndern fÃƒÂ¼r Schalteranzahl! 0 --> Schalter 1. 64 ist Schalter 65.

                            Thread.Sleep(5);  // 25 möglich????

                            if (_prevSwitchState[1] != Switch[1])
                            {
                                if (Switch[1] == ON)
                                {
                                    F4.SimSeatOn();
                                    // Stores Config Switch to CATIII
                                    _prevSwitchState[1] = Switch[1];
                                }

                                if (Switch[1] == OFF)
                                {
                                    F4.SimSeatOff();
                                    _prevSwitchState[1] = Switch[1];

                                }
                            }

                        }
                    }

                }
                Application.DoEvents();
            }
        }


        public void SimOutput()
        {

            using (Reader myReader = new F4SharedMem.Reader())  //Neue Instanz von Reader, um die Daten von FalconBMS auslesen zu kÃƒÂ¶nnen.
            {
                while (_keepRunning == true)
                {
                    if (myReader.IsFalconRunning == true)
                    {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {
                            FlightData myFlightData = new FlightData();
                            FlightData myCurrentData = myReader.GetCurrentData();
                            myChaffCount = myCurrentData.ChaffCount;
                            myFlareCount = myCurrentData.FlareCount;



                            // Define for better reading of status:
                            int ON = 1;
                            int OFF = 0;
                            //////////////////////////////////////////////////////////////////////////////////////
                            //                                CMDS LIGHTS                                       //
                            //               --> Need to crosscheck with "Flightdata.h" <<--                    //
                            //////////////////////////////////////////////////////////////////////////////////////
                            // -------------------------------------------------------------------------------- //
                            //                Osram Displays (Chaff / Flare / L0 /  AUTO DEGR)                  //
                            // --------------------------------------------------------------------- works! --- //

                            int myFlareCount2 = (int)myFlareCount;
                            int myChaffCount2 = (int)myChaffCount;

                            int myChaffEinertemp = myChaffCount2 % 10;
                            int myChaffZehnertemp = myChaffCount2 % 100;

                            int myFlareEinertemp = myFlareCount2 % 10;
                            int myFlareZehnertemp = myFlareCount2 % 100;

                            int myChaffEiner = myChaffEinertemp;
                            int myChaffZehner = myChaffZehnertemp / 10;

                            int myFlareEiner = myFlareEinertemp;
                            int myFlareZehner = myFlareZehnertemp / 10;



                            // When loading screen appears, clear L0 chaffs / L0 flares from
                            // previous missions.


                            label4.Invoke(new Action<string>(s => { label4.Text = s; label4.ForeColor = Color.Red; }), myCurrentData.pilotsStatus[0].ToString());
                            if (myCurrentData.pilotsStatus[0] == 3)
                            {


                                label4.Invoke(new Action<string>(s => { label4.Text = s; label4.ForeColor = Color.Red; }), myCurrentData.pilotsStatus[0].ToString());
                                if ((myCurrentData.hsiBits & 0x80000000) != 0)
                                {


                                    //----------------------------------------------//
                                    // --> FEHLT NOCH, muss implementiert werden!   //
                                    // --> LED-Status für folgende CMDS LEDs:       //
                                    //                                              //
                                    //     GO           0x80                        //
                                    //     NOGO         0x100                       //
                                    //     RDY          0x200     (Dispense Rdy)    //
                                    //----------------------------------------------//

                                    //----------------------------------------------//
                                    //      Einzelne Anzeige löschen:               //
                                    //                                              //
                                    //  Subadresse eingeben                         //
                                    //  Databyte: 32                                //
                                    //                                              //
                                    //----------------------------------------------//

                                    // Normale Zahlenwerte anzeigen für
                                    // verbleibenden Caff- / Flare-Vorrat:
                                    //                                    if (myCurrentData.cmdsMode != 0)
                                    if (myCurrentData.cmdsMode == 0)
                                    {
                                        if (_prevFlareEinerState != myFlareEiner)
                                        {
                                            if (myFlareEiner == 0) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 15, 48);
                                            if (myFlareEiner == 1) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 15, 49);
                                            if (myFlareEiner == 2) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 15, 50);
                                            if (myFlareEiner == 3) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 15, 51);
                                            if (myFlareEiner == 4) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 15, 52);
                                            if (myFlareEiner == 5) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 15, 53);
                                            if (myFlareEiner == 6) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 15, 54);
                                            if (myFlareEiner == 7) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 15, 55);
                                            if (myFlareEiner == 8) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 15, 56);
                                            if (myFlareEiner == 9) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 15, 57);
                                            if (myFlareCount < 1) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 15, 48);
                                            // Thread.Sleep(5);
                                            _prevFlareEinerState = myFlareEiner;
                                        }


                                        if (_prevFlareZehnerState != myFlareZehner)
                                        {
                                            if (myFlareZehner == 0) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 14, 48);
                                            if (myFlareZehner == 1) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 14, 49);
                                            if (myFlareZehner == 2) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 14, 50);
                                            if (myFlareZehner == 3) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 14, 51);
                                            if (myFlareZehner == 4) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 14, 52);
                                            if (myFlareZehner == 5) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 14, 53);
                                            if (myFlareZehner == 6) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 14, 54);
                                            if (myFlareZehner == 7) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 14, 55);
                                            if (myFlareZehner == 8) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 14, 56);
                                            if (myFlareZehner == 9) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 10, 57);
                                            // Thread.Sleep(5);
                                            _prevFlareZehnerState = myFlareZehner;
                                        }

                                        if (_prevChaffEinerState != myChaffEiner)
                                        {
                                            if (myChaffEiner == 0) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 11, 48);
                                            if (myChaffEiner == 1) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 11, 49);
                                            if (myChaffEiner == 2) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 11, 50);
                                            if (myChaffEiner == 3) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 11, 51);
                                            if (myChaffEiner == 4) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 11, 52);
                                            if (myChaffEiner == 5) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 11, 53);
                                            if (myChaffEiner == 6) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 11, 54);
                                            if (myChaffEiner == 7) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 11, 55);
                                            if (myChaffEiner == 8) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 11, 56);
                                            if (myChaffEiner == 9) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 11, 57);
                                            if (myChaffCount < 1) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 11, 48);
                                            // Thread.Sleep(5);
                                            _prevChaffEinerState = myChaffEiner;
                                        }

                                        if (_prevChaffZehnerState != myChaffZehner)
                                        {
                                            if (myChaffZehner == 0) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 10, 48);
                                            if (myChaffZehner == 1) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 10, 49);
                                            if (myChaffZehner == 2) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 10, 50);
                                            if (myChaffZehner == 3) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 10, 51);
                                            if (myChaffZehner == 4) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 10, 52);
                                            if (myChaffZehner == 5) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 10, 53);
                                            if (myChaffZehner == 6) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 10, 54);
                                            if (myChaffZehner == 7) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 10, 55);
                                            if (myChaffZehner == 8) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 10, 56);
                                            if (myChaffZehner == 9) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 10, 57);
                                            //Thread.Sleep(5);
                                            _prevChaffZehnerState = myChaffZehner;
                                        }


                                        // Show "L0", when low on Chaffs:
                                        if (((myCurrentData.lightBits2 & 0x400) != 0) && (ChaffLow == false))
                                        {
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 8, 76);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 9, 111);
                                            Thread.Sleep(5);
                                            _prevChaffLow = (myCurrentData.lightBits2 & 0x400);
                                            ChaffLow = true;
                                        }

                                        if (((myCurrentData.lightBits2 & 0x400) == 0) && (ChaffLow == true))
                                        {
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 8, 32);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 9, 32);
                                            Thread.Sleep(5);
                                            _prevChaffLow = (myCurrentData.lightBits2 & 0x400);
                                            ChaffLow = false;
                                        }




                                        // Show "L0", when low on Flares:
                                        if (((myCurrentData.lightBits2 & 0x800) != 0) && (FlareLow == false))
                                        {
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 12, 76);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 13, 111);
                                            _prevFlareLow = (myCurrentData.lightBits2 & 0x800);
                                            Thread.Sleep(5);
                                            FlareLow = true;
                                        }

                                        if (((myCurrentData.lightBits2 & 0x800) == 0) && (FlareLow == true))
                                        {
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 12, 32);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 13, 32);
                                            Thread.Sleep(5);
                                            _prevFlareLow = (myCurrentData.lightBits2 & 0x800);
                                            FlareLow = false;
                                        }


                                        // Show "AUTO DEGR":
                                        if (((myCurrentData.lightBits2 & 0x100) != 0) && (AutoDegree == false))
                                        {
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 0, 65);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 1, 85);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 2, 84);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 3, 79);
                                            // Thread.Sleep(5);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 4, 68);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 5, 69);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 6, 71);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 7, 82);
                                            Thread.Sleep(5);
                                            AutoDegree = true;
                                        }

                                        // Remove "AUTO DEGR":
                                        if (((myCurrentData.lightBits2 & 0x100) == 0) && (AutoDegree == true))
                                        {
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 0, 32);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 1, 32);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 2, 32);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 3, 32);
                                            // Thread.Sleep(5);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 4, 32);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 5, 32);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 6, 32);
                                            Port3_PHCC_Input_Output.DoaSendRaw(0x44, 7, 32);
                                            Thread.Sleep(5);
                                            AutoDegree = false;
                                        }
                                    }
                                    // }
                                    // Reset CMDS-display when CMDS power is off:
                                    if (myCurrentData.cmdsMode == 0)
                                    {
                                        if ((myCurrentData.lightBits2 & 0x800) != 0) FlareLow = false;
                                        if ((myCurrentData.lightBits2 & 0x400) != 0) ChaffLow = false;
                                        if ((myCurrentData.lightBits2 & 0x100) != 0) AutoDegree = false;
                                        Port3_PHCC_Input_Output.DoaSendRaw(0x44, 34, 0);


                                        //Thread.Sleep(5);

                                        // Reset counter to make upper loop work again 
                                        // once CMDS power is on again:
                                        _prevChaffEinerState = _prevChaffEinerState - 1;
                                        _prevChaffZehnerState = _prevChaffZehnerState - 1;
                                        _prevFlareEinerState = _prevFlareEinerState - 1;
                                        _prevFlareZehnerState = _prevFlareZehnerState - 1;
                                    }

                                    // When status "IS FLYING", perform a display-reset!
                                    // --> Needs to be tested, what happens and if necessary! <--
                                    //  if ((myCurrentData.hsiBits & 0x80000000) == 0)
                                    //   {
                                    //      Thread.Sleep(5);
                                    //   }

                                    //////////////////////////////////////////////////////////////////////////////////////
                                    //                              STATUS LIGHTS                                       //
                                    //               --> Need to crosscheck with "Flightdata.h" <<--                    //
                                    //////////////////////////////////////////////////////////////////////////////////////
                                    // -------------------------------------------------------------------------------- //
                                    // LightBits               MASTER CAUTION                                           //
                                    // --------------------------------------------------------------------- works! --- //
                                    // Define short-definition of lightbits:



                                    int MasterCaution = (myCurrentData.lightBits & 0x1);
                                    if (MasterCaution != 0)
                                    {
                                        label6.Invoke(new Action<string>(s => { label6.Text = s; label6.ForeColor = Color.Red; }), "MASTER CAUTION  >> O N <<");
                                    }

                                    if (MasterCaution == 0)
                                    {
                                        label6.Invoke(new Action<string>(s => { label6.Text = s; label6.ForeColor = Color.Red; }), "MASTER CAUTION  >> O F F <<");
                                    }

                                    //    if ((_prevMasterCaution != MasterCaution) && ((MasterCaution) == OFF))
                                    //    {
                                    //        //  Port3_PHCC_Input_Output.DoaSendRaw(0x30, 14, 0);
                                    //        label6.Invoke(new Action<string>(s => { label6.Text = s; label6.ForeColor = Color.Red; }), "MASTER CAUTION  >> O F F <<");
                                    //        //  Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, 0);
                                    //        _prevMasterCaution = MasterCaution;
                                    //        Thread.Sleep(5);

                                    //    }

                                    //    if ((_prevMasterCaution != MasterCaution) && ((MasterCaution) == ON))
                                    //    {
                                    //        label6.Invoke(new Action<string>(s => { label6.Text = s; label6.ForeColor = Color.Red; }), "MASTER CAUTION  >> O N <<");
                                    // Port3_PHCC_Input_Output.DoaSendRaw(0x30, 14, 0x80);
                                    // Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, 255);
                                    //        _prevMasterCaution = MasterCaution;
                                    //        Thread.Sleep(5);


                                    //    }

                                    //       if ((myCurrentData.lightBits & 0x1) != 0)
                                    //       {
                                    //           textBox1.Text = "master caution or seat tripped";
                                    //       }
                                    //       else
                                    //      {
                                    //           textBox1.Text = "master caution or seat not tripped";
                                    //       }

                                    // -------------------------------------------------------------------------------- //
                                    //                          WARNING LIGHTS                                          //      
                                    //                                                                                  //
                                    // DOA_40DO:                                                                        //
                                    // Connector-numbers: 3, 4, 5, 6, 7                                                 //
                                    // Lamp-Bits: 0, 1, 2, 4, 8, 16, 32, 64, 128, 255                                   //      
                                    //                                                                                  //
                                    // --------------------------------------------------------------------- works! --- //



                                    // -------------------------------------------------------------------------------- //
                                    //                          WARNING LIGHTS                                          //      
                                    //                          RIGHT  EYEBROW                                          //
                                    // -------------------------------------------------------------------------------- //

                                    // ---------------
                                    //   ENGINE FIRE
                                    // ---------------
                                    if (((LightBits)myCurrentData.lightBits & LightBits.ENG_FIRE)
                                        == LightBits.ENG_FIRE != _prevENG_FIRE)
                                    {
                                        if (((LightBits)myCurrentData.lightBits & LightBits.ENG_FIRE)
                                        == LightBits.ENG_FIRE)
                                        {
                                            bNew = 2;
                                            byte all = (byte)(bOld + bNew);
                                            Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all);
                                            bOld = all;
                                        }
                                        else
                                        {
                                            bNew = 2;
                                            byte all = (byte)(bOld - bNew);
                                            Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all); ;
                                            bOld = all;
                                        }
                                    }

                                    _prevENG_FIRE = ((LightBits)myCurrentData.lightBits & LightBits.ENG_FIRE)
                                      == LightBits.ENG_FIRE;

                                    // ---------------
                                    //  ENGINE FAULT
                                    // ---------------
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.ENGINE)
                                        == LightBits2.ENGINE != _prevENGINE)
                                    {
                                        if (((LightBits2)myCurrentData.lightBits2 & LightBits2.ENGINE)
                                        == LightBits2.ENGINE)
                                        {
                                            bNew = 1;
                                            byte all = (byte)(bOld + bNew);
                                            Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all);
                                            bOld = all;
                                        }
                                        else
                                        {
                                            bNew = 1;
                                            byte all = (byte)(bOld - bNew);
                                            Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all);
                                            bOld = all;
                                        }
                                    }
                                    _prevENGINE = ((LightBits2)myCurrentData.lightBits2 &
                                       LightBits2.ENGINE) == LightBits2.ENGINE;

                                    // ---------------
                                    //  HYD FAULT
                                    // ---------------
                                    if (((LightBits)myCurrentData.lightBits & LightBits.HYD)
                                   == LightBits.HYD != _prevHYD)
                                    {
                                        if (((LightBits)myCurrentData.lightBits & LightBits.HYD)
                                           == LightBits.HYD)
                                        {
                                            bNew = 4;
                                            byte all = (byte)(bOld + bNew);
                                            Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all);
                                            bOld = all;
                                        }
                                        else
                                        {
                                            bNew = 4;
                                            byte all = (byte)(bOld - bNew);
                                            Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all);
                                            bOld = all;
                                        }
                                    }
                                }
                                _prevHYD = ((LightBits)myCurrentData.lightBits &
                                   LightBits.HYD) == LightBits.HYD;

                                // ---------------
                                //  FLCS FAULT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.FLCS)
                                    == LightBits.FLCS != _prevFLCS)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.FLCS)
                                    == LightBits.FLCS)
                                    {
                                        bNew = 8;
                                        byte all = (byte)(bOld + bNew);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all);
                                        bOld = all;
                                    }
                                    else
                                    {
                                        bNew = 8;
                                        byte all = (byte)(bOld - bNew);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all);
                                        bOld = all;
                                    }
                                }
                                _prevFLCS = ((LightBits)myCurrentData.lightBits &
                                   LightBits.FLCS) == LightBits.FLCS;

                                // ---------------
                                //  DBU FAULT
                                // ---------------
                                if (((LightBits3)myCurrentData.lightBits3 & LightBits3.DbuWarn)
                                    == LightBits3.DbuWarn != _prevDbuWarn)
                                {
                                    if (((LightBits3)myCurrentData.lightBits3 & LightBits3.DbuWarn)
                                    == LightBits3.DbuWarn)
                                    {
                                        bNew = 16;
                                        byte all = (byte)(bOld + bNew);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all);
                                        bOld = all;
                                    }
                                    else
                                    {
                                        bNew = 16;
                                        byte all = (byte)(bOld - bNew);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all);
                                        bOld = all;
                                    }
                                }
                                _prevDbuWarn = ((LightBits3)myCurrentData.lightBits3 &
                                   LightBits3.DbuWarn) == LightBits3.DbuWarn;

                                // ---------------
                                //  T_L_CFG FAULT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.T_L_CFG)
                                    == LightBits.T_L_CFG != _prevT_L_CFG)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.T_L_CFG)
                                    == LightBits.T_L_CFG)
                                    {
                                        bNew = 32;
                                        byte all = (byte)(bOld + bNew);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all);
                                        bOld = all;
                                    }
                                    else
                                    {
                                        bNew = 32;
                                        byte all = (byte)(bOld - bNew);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all);
                                        bOld = all;
                                    }
                                }
                                _prevT_L_CFG = ((LightBits)myCurrentData.lightBits &
                                   LightBits.T_L_CFG) == LightBits.T_L_CFG;

                                // ---------------
                                //   CAN FAULT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.CAN)
                                    == LightBits.CAN != _prevCAN)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.CAN)
                                    == LightBits.CAN)
                                    {
                                        bNew = 128;
                                        byte all = (byte)(bOld + bNew);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all);
                                        bOld = all;
                                    }
                                    else
                                    {
                                        bNew = 128;
                                        byte all = (byte)(bOld - bNew);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all);
                                        bOld = all;
                                    }
                                }
                                _prevCAN = ((LightBits)myCurrentData.lightBits &
                                   LightBits.CAN) == LightBits.CAN;

                                // ---------------
                                //  OXY BROW FAULT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.OXY_BROW)
                                    == LightBits.OXY_BROW != _prevOXY_BROW)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.OXY_BROW)
                                    == LightBits.OXY_BROW)
                                    {
                                        bNew = 64;
                                        byte all = (byte)(bOld + bNew);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all);
                                        bOld = all;
                                    }
                                    else
                                    {
                                        bNew = 64;
                                        byte all = (byte)(bOld - bNew);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, all);
                                        bOld = all;
                                    }
                                }
                                _prevOXY_BROW = ((LightBits)myCurrentData.lightBits &
                                   LightBits.OXY_BROW) == LightBits.OXY_BROW;

                                // -------------------------------------------------------------------------------- //
                                //                          CAUTION PANEL                                           //      
                                // -------------------------------------------------------------------------------- //

                                //-------------------------------
                                // 1st ROW (beginning from left)
                                // ---------------
                                //  FLCS FAULT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.FltControlSys)
                                    == LightBits.FltControlSys != _prevFltControlSys)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.FltControlSys)
                                    == LightBits.FltControlSys)
                                    {
                                        bNew1 = 1;
                                        byte all1 = (byte)(bOld1 + bNew1);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                        bOld1 = all1;
                                    }
                                    else
                                    {
                                        bNew1 = 1;
                                        byte all1 = (byte)(bOld1 - bNew1);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                        bOld1 = all1;
                                    }
                                }
                                _prevFltControlSys = ((LightBits)myCurrentData.lightBits &
                                   LightBits.FltControlSys) == LightBits.FltControlSys;

                                // ---------------
                                //  ELEC FAULT
                                // ---------------
                                // Maybe blinking in future, but not implemented in BMS 4.34 yet. 10.05.2019 LE.
                                if (((LightBits3)myCurrentData.lightBits3 & LightBits3.Elec_Fault)
                                                                  == LightBits3.Elec_Fault != _prevElec_Fault)
                                {
                                    if (((LightBits3)myCurrentData.lightBits3 & LightBits3.Elec_Fault)
                                    == LightBits3.Elec_Fault)
                                    {
                                        bNew1 = 2;
                                        byte all1 = (byte)(bOld1 + bNew1);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                        bOld1 = all1;
                                    }
                                    else
                                    {
                                        bNew1 = 2;
                                        byte all1 = (byte)(bOld1 - bNew1);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                        bOld1 = all1;
                                    }
                                }
                                _prevElec_Fault = ((LightBits3)myCurrentData.lightBits3 &
                                   LightBits3.Elec_Fault) == LightBits3.Elec_Fault;

                                // ---------------
                                //  PROBE HEAT FAULT
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.PROBEHEAT)
                                                                == LightBits2.PROBEHEAT != _prevPROBEHEAT)
                                {
                                    if ((myCurrentData.blinkBits & 0x04) == 0)
                                    {
                                        //falls Lightbit ON ist:

                                        if (((LightBits2)myCurrentData.lightBits2 & LightBits2.PROBEHEAT) == LightBits2.PROBEHEAT)
                                        {
                                            bNew1 = 4;
                                            all1 = (byte)(bOld1 + bNew1);
                                            Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                            bOld1 = all1;
                                           // on = true;
                                        }
                                        else
                                        {
                                            bNew1 = 4;
                                            all1 = (byte)(bOld1 - bNew1);
                                            Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                            bOld1 = all1;
                                          //  on = false;

                                        }
                               

                                    _prevPROBEHEAT = ((LightBits2)myCurrentData.lightBits2 &
                                        LightBits2.PROBEHEAT) == LightBits2.PROBEHEAT;
                                   // on = false;
                                     }
                                    
                                }                           

                            // ---------------
                            //  C ADC FAULT
                            // ---------------
                            if (((LightBits3)myCurrentData.lightBits3 & LightBits3.cadc)
                                    == LightBits3.cadc != _prevcadc)
                                {
                                    if (((LightBits3)myCurrentData.lightBits3 & LightBits3.cadc)
                                    == LightBits3.cadc)
                                    {
                                        bNew1 = 8;
                                        byte all1 = (byte)(bOld1 + bNew1);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                        bOld1 = all1;
                                    }
                                    else
                                    {
                                        bNew1 = 8;
                                        byte all1 = (byte)(bOld1 - bNew1);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                        bOld1 = all1;
                                    }
                                }
                                _prevcadc = ((LightBits3)myCurrentData.lightBits3 &
                                   LightBits3.cadc) == LightBits3.cadc;

                                // ---------------
                                //  STORES FAULT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.CONFIG)
                                    == LightBits.CONFIG != _prevCONFIG)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.CONFIG)
                                    == LightBits.CONFIG)
                                    {
                                        bNew1 = 16;
                                        byte all1 = (byte)(bOld1 + bNew1);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                        bOld1 = all1;
                                    }
                                    else
                                    {
                                        bNew1 = 16;
                                        byte all1 = (byte)(bOld1 - bNew1);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                        bOld1 = all1;
                                    }
                                }
                                _prevCONFIG = ((LightBits)myCurrentData.lightBits &
                                   LightBits.CONFIG) == LightBits.CONFIG;

                                // ---------------
                                //  ATF NOT ENGAGED
                                // ---------------
                                if (((LightBits3)myCurrentData.lightBits3 & LightBits3.ATF_Not_Engaged)
                                    == LightBits3.ATF_Not_Engaged != _prevATF_Not_Engaged)
                                {
                                    if (((LightBits3)myCurrentData.lightBits3 & LightBits3.ATF_Not_Engaged)
                                    == LightBits3.ATF_Not_Engaged)
                                    {
                                        bNew1 = 32;
                                        byte all1 = (byte)(bOld1 + bNew1);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                        bOld1 = all1;
                                    }
                                    else
                                    {
                                        bNew1 = 32;
                                        byte all1 = (byte)(bOld1 - bNew1);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                        bOld1 = all1;
                                    }
                                }
                                _prevATF_Not_Engaged = ((LightBits3)myCurrentData.lightBits3 &
                                   LightBits3.ATF_Not_Engaged) == LightBits3.ATF_Not_Engaged;

                                // ---------------
                                //  FWD FUEL LOW
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.FwdFuelLow)
                                    == LightBits2.FwdFuelLow != _prevFwdFuelLow)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.FwdFuelLow)
                                    == LightBits2.FwdFuelLow)
                                    {
                                        bNew1 = 64;
                                        byte all1 = (byte)(bOld1 + bNew1);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                        bOld1 = all1;
                                    }
                                    else
                                    {
                                        bNew1 = 64;
                                        byte all1 = (byte)(bOld1 - bNew1);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                        bOld1 = all1;
                                    }
                                }
                                _prevFwdFuelLow = ((LightBits2)myCurrentData.lightBits2 &
                                   LightBits2.FwdFuelLow) == LightBits2.FwdFuelLow;

                                // ---------------
                                //  AFT FUEL LOW
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.AftFuelLow)
                                    == LightBits2.AftFuelLow != _prevAftFuelLow)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.AftFuelLow)
                                    == LightBits2.AftFuelLow)
                                    {
                                        bNew1 = 128;
                                        byte all1 = (byte)(bOld1 + bNew1);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                        bOld1 = all1;
                                    }
                                    else
                                    {
                                        bNew1 = 128;
                                        byte all1 = (byte)(bOld1 - bNew1);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, all1);
                                        bOld1 = all1;
                                    }
                                }
                                _prevAftFuelLow = ((LightBits2)myCurrentData.lightBits2 &
                                   LightBits2.AftFuelLow) == LightBits2.AftFuelLow;

                                //-------------------------------
                                // 2nd ROW (beginning from left)
                                // ---------------
                                //  ENGINE FAULT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.EngineFault)
                                    == LightBits.EngineFault != _prevEngineFault)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.EngineFault)
                                    == LightBits.EngineFault)
                                    {
                                        bNew2 = 1;
                                        byte all2 = (byte)(bOld2 + bNew2);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, all2);
                                        bOld2 = all2;
                                    }
                                    else
                                    {
                                        bNew2 = 1;
                                        byte all2 = (byte)(bOld2 - bNew2);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, all2);
                                        bOld2 = all2;
                                    }
                                }
                                _prevEngineFault = ((LightBits)myCurrentData.lightBits &
                                   LightBits.EngineFault) == LightBits.EngineFault;

                                // ---------------
                                //  SEC FAULT
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.SEC)
                                    == LightBits2.SEC != _prevSEC)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.SEC)
                                    == LightBits2.SEC)
                                    {
                                        bNew2 = 2;
                                        byte all2 = (byte)(bOld2 + bNew2);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, all2);
                                        bOld2 = all2;
                                    }
                                    else
                                    {
                                        bNew2 = 2;
                                        byte all2 = (byte)(bOld2 - bNew2);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, all2);
                                        bOld2 = all2;
                                    }
                                }
                                _prevSEC = ((LightBits2)myCurrentData.lightBits2 &
                                   LightBits2.SEC) == LightBits2.SEC;

                                // ---------------
                                //  FUEL OIL HOT
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.FUEL_OIL_HOT)
                                    == LightBits2.FUEL_OIL_HOT != _prevFUEL_OIL_HOT)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.FUEL_OIL_HOT)
                                    == LightBits2.FUEL_OIL_HOT)
                                    {
                                        bNew2 = 4;
                                        byte all2 = (byte)(bOld2 + bNew2);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, all2);
                                        bOld2 = all2;
                                    }
                                    else
                                    {
                                        bNew2 = 4;
                                        byte all2 = (byte)(bOld2 - bNew2);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, all2);
                                        bOld2 = all2;
                                    }
                                }
                                _prevFUEL_OIL_HOT = ((LightBits2)myCurrentData.lightBits2 &
                                   LightBits2.FUEL_OIL_HOT) == LightBits2.FUEL_OIL_HOT;

                                // ---------------
                                //  INLET ICING
                                // ---------------
                                // Currently no lightbit implemented in 4.33.
                                // 04.05.2019 LE.

                                // ---------------
                                //  OVERHEAT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.Overheat)
                                    == LightBits.Overheat != _prevOverheat)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.Overheat)
                                    == LightBits.Overheat)
                                    {
                                        bNew2 = 16;
                                        byte all2 = (byte)(bOld2 + bNew2);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, all2);
                                        bOld2 = all2;
                                    }
                                    else
                                    {
                                        bNew2 = 16;
                                        byte all2 = (byte)(bOld2 - bNew2);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, all2);
                                        bOld2 = all2;
                                    }
                                }
                                _prevOverheat = ((LightBits)myCurrentData.lightBits &
                                   LightBits.Overheat) == LightBits.Overheat;

                                // ---------------
                                //  EEC
                                // ---------------
                                // Currently no lightbit implemented in 4.33.
                                // 04.05.2019 LE.

                                // ---------------
                                //  BUC
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.BUC)
                                    == LightBits2.BUC != _prevBUC)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.BUC)
                                    == LightBits2.BUC)
                                    {
                                        bNew2 = 64;
                                        byte all2 = (byte)(bOld2 + bNew2);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, all2);
                                        bOld2 = all2;
                                    }
                                    else
                                    {
                                        bNew2 = 64;
                                        byte all2 = (byte)(bOld2 - bNew2);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, all2);
                                        bOld2 = all2;
                                    }
                                }
                                _prevBUC = ((LightBits2)myCurrentData.lightBits2 &
                                   LightBits2.BUC) == LightBits2.BUC;

                                //-------------------------------
                                // 3rd ROW (beginning from left)
                                // ---------------
                                //  AVIONICS FAULT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.Avionics)
                                    == LightBits.Avionics != _prevAvionics)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.Avionics)
                                    == LightBits.Avionics)
                                    {
                                        bNew3 = 1;
                                        byte all3 = (byte)(bOld3 + bNew3);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, all3);
                                        bOld3 = all3;
                                    }
                                    else
                                    {
                                        bNew3 = 1;
                                        byte all3 = (byte)(bOld3 - bNew3);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, all3);
                                        bOld3 = all3;
                                    }
                                }
                                _prevAvionics = ((LightBits)myCurrentData.lightBits &
                                   LightBits.Avionics) == LightBits.Avionics;

                                // ---------------
                                //  EQUIP HOT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.EQUIP_HOT)
                                    == LightBits.EQUIP_HOT != _prevEQUIP_HOT)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.EQUIP_HOT)
                                    == LightBits.EQUIP_HOT)
                                    {
                                        bNew3 = 2;
                                        byte all3 = (byte)(bOld3 + bNew3);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, all3);
                                        bOld3 = all3;
                                    }
                                    else
                                    {
                                        bNew3 = 2;
                                        byte all3 = (byte)(bOld3 - bNew3);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, all3);
                                        bOld3 = all3;
                                    }
                                }
                                _prevEQUIP_HOT = ((LightBits)myCurrentData.lightBits &
                                   LightBits.EQUIP_HOT) == LightBits.EQUIP_HOT;

                                // ---------------
                                //  RADAR ALT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.RadarAlt)
                                    == LightBits.RadarAlt != _prevRadarAlt)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.RadarAlt)
                                    == LightBits.RadarAlt)
                                    {
                                        bNew3 = 4;
                                        byte all3 = (byte)(bOld3 + bNew3);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, all3);
                                        bOld3 = all3;
                                    }
                                    else
                                    {
                                        bNew3 = 4;
                                        byte all3 = (byte)(bOld3 - bNew3);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, all3);
                                        bOld3 = all3;
                                    }
                                }
                                _prevRadarAlt = ((LightBits)myCurrentData.lightBits &
                                   LightBits.RadarAlt) == LightBits.RadarAlt;

                                // ---------------
                                //  IFF
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.IFF)
                                    == LightBits.IFF != _prevIFF)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.IFF)
                                    == LightBits.IFF)
                                    {
                                        bNew3 = 8;
                                        byte all3 = (byte)(bOld3 + bNew3);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, all3);
                                        bOld3 = all3;
                                    }
                                    else
                                    {
                                        bNew3 = 8;
                                        byte all3 = (byte)(bOld3 - bNew3);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, all3);
                                        bOld3 = all3;
                                    }
                                }
                                _prevIFF = ((LightBits)myCurrentData.lightBits &
                                   LightBits.IFF) == LightBits.IFF;

                                // ---------------
                                //  NUCLEAR
                                // ---------------
                                // Currently no lightbit implemented in 4.33.
                                // 04.05.2019 LE.

                                //-------------------------------
                                // 4th ROW (beginning from left)
                                // ---------------
                                //  SEAT NOT ARMED
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.SEAT_ARM)
                                    == LightBits2.SEAT_ARM != _prevSEAT_ARM)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.SEAT_ARM)
                                    == LightBits2.SEAT_ARM)
                                    {
                                        bNew4 = 1;
                                        byte all4 = (byte)(bOld4 + bNew4);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, all4);
                                        bOld4 = all4;
                                    }
                                    else
                                    {
                                        bNew4 = 1;
                                        byte all4 = (byte)(bOld4 - bNew4);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, all4);
                                        bOld4 = all4;
                                    }
                                }
                                _prevSEAT_ARM = ((LightBits2)myCurrentData.lightBits2 &
                                   LightBits2.SEAT_ARM) == LightBits2.SEAT_ARM;

                                // ---------------
                                //  NWS FAIL
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.NWSFail)
                                    == LightBits.NWSFail != _prevNWSFail)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.NWSFail)
                                    == LightBits.NWSFail)
                                    {
                                        bNew4 = 2;
                                        byte all4 = (byte)(bOld4 + bNew4);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, all4);
                                        bOld4 = all4;
                                    }
                                    else
                                    {
                                        bNew4 = 2;
                                        byte all4 = (byte)(bOld4 - bNew4);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, all4);
                                        bOld4 = all4;
                                    }
                                }
                                _prevNWSFail = ((LightBits)myCurrentData.lightBits &
                                   LightBits.NWSFail) == LightBits.NWSFail;

                                // ---------------
                                //  ANTI SKID
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.ANTI_SKID)
                                    == LightBits2.ANTI_SKID != _prevANTI_SKID)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.ANTI_SKID)
                                    == LightBits2.ANTI_SKID)
                                    {
                                        bNew4 = 4;
                                        byte all4 = (byte)(bOld4 + bNew4);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, all4);
                                        bOld4 = all4;
                                    }
                                    else
                                    {
                                        bNew4 = 4;
                                        byte all4 = (byte)(bOld4 - bNew4);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, all4);
                                        bOld4 = all4;
                                    }
                                }
                                _prevANTI_SKID = ((LightBits2)myCurrentData.lightBits2 &
                                   LightBits2.ANTI_SKID) == LightBits2.ANTI_SKID;

                                // ---------------
                                //  HOOK
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.Hook)
                                    == LightBits.Hook != _prevHook)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.Hook)
                                    == LightBits.Hook)
                                    {
                                        bNew4 = 8;
                                        byte all4 = (byte)(bOld4 + bNew4);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, all4);
                                        bOld4 = all4;
                                    }
                                    else
                                    {
                                        bNew4 = 8;
                                        byte all4 = (byte)(bOld4 - bNew4);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, all4);
                                        bOld4 = all4;
                                    }
                                }
                                _prevHook = ((LightBits)myCurrentData.lightBits &
                                   LightBits.Hook) == LightBits.Hook;

                                // ---------------
                                //  OXY LOW
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.OXY_LOW)
                                    == LightBits2.OXY_LOW != _prevOXY_LOW)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.OXY_LOW)
                                    == LightBits2.OXY_LOW)
                                    {
                                        bNew4 = 16;
                                        byte all4 = (byte)(bOld4 + bNew4);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, all4);
                                        bOld4 = all4;
                                    }
                                    else
                                    {
                                        bNew4 = 16;
                                        byte all4 = (byte)(bOld4 - bNew4);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, all4);
                                        bOld4 = all4;
                                    }
                                }
                                _prevOXY_LOW = ((LightBits2)myCurrentData.lightBits2 &
                                   LightBits2.OXY_LOW) == LightBits2.OXY_LOW;

                                // ---------------
                                //  CABIN PRESS 
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.CabinPress)
                                    == LightBits.CabinPress != _prevCabinPress)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.CabinPress)
                                    == LightBits.CabinPress)
                                    {
                                        bNew4 = 32;
                                        byte all4 = (byte)(bOld4 + bNew4);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, all4);
                                        bOld4 = all4;
                                    }
                                    else
                                    {
                                        bNew4 = 32;
                                        byte all4 = (byte)(bOld4 - bNew4);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, all4);
                                        bOld4 = all4;
                                    }
                                }
                                _prevCabinPress = ((LightBits)myCurrentData.lightBits &
                                   LightBits.CabinPress) == LightBits.CabinPress;


                                // ---------------
                                //  Restliche Caution lights ohne lightbits leuchten lassen,
                                //  z.b. bei Drücken des TEST-LAMP-Knopfes: 
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.AllLampBitsOn)
                                    == LightBits.AllLampBitsOn != _prevAllLampBitsOn)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.AllLampBitsOn)
                                    == LightBits.AllLampBitsOn)
                                    {
                                        // 2nd ROW "INLET ICING", "-------"
                                        bNew2 = 168;
                                        byte all2 = (byte)(bOld2 + bNew2);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, all2);
                                        bOld2 = all2;

                                        //3rd ROW "NUCLEAR", "------", "-----", "-----"
                                        bNew3 = 240;
                                        byte all3 = (byte)(bOld3 + bNew3);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, all3);
                                        bOld3 = all3;

                                        //4th ROW "-----", "-----"
                                        bNew4 = 192;
                                        byte all4 = (byte)(bOld4 + bNew4);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, all4);
                                        bOld4 = all4;
                                    }
                                    else
                                    {
                                        // 2nd ROW "INLET ICING", "-------"
                                        bNew2 = 168;
                                        byte all2 = (byte)(bOld2 - bNew2);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, all2);
                                        bOld2 = all2;

                                        //3rd ROW "NUCLEAR", "------", "-----", "-----"
                                        bNew3 = 240;
                                        byte all3 = (byte)(bOld3 - bNew3);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, all3);
                                        bOld3 = all3;

                                        //4th ROW "-----", "-----"
                                        bNew4 = 192;
                                        byte all4 = (byte)(bOld4 - bNew4);
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, all4);
                                        bOld4 = all4;
                                    }
                                }
                                _prevAllLampBitsOn = ((LightBits)myCurrentData.lightBits &
                                   LightBits.AllLampBitsOn) == LightBits.AllLampBitsOn;

                                // -------------------------------------------------------------------------------- //
                                //                          CAUTION LIGHTS                                          //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                int FltControlSys = (myCurrentData.lightBits & 0x40000);
                                int LEFlaps = (myCurrentData.lightBits & 0x80000);
                                int EngineFault = (myCurrentData.lightBits & 0x100000);
                                int Overheat = (myCurrentData.lightBits & 0x200000);
                                int FuelLow = (myCurrentData.lightBits & 0x400000);
                                int Avionics = (myCurrentData.lightBits & 0x800000);
                                int RadarAlt = (myCurrentData.lightBits & 0x1000000);
                                int IFF = (myCurrentData.lightBits & 0x2000000);
                                int ECM = (myCurrentData.lightBits & 0x4000000);
                                int Hook = (myCurrentData.lightBits & 0x8000000);
                                int NWSFail = (myCurrentData.lightBits & 0x1000000);
                                int CabinPress = (myCurrentData.lightBits & 0x2000000);
                                int AutoPilotOn = (myCurrentData.lightBits & 0x4000000);
                                int TFR_STBY = (myCurrentData.lightBits & 0x8000000);

                                int SEC = myCurrentData.lightBits2 & 0x400000;
                                int OXY_LOW = myCurrentData.lightBits2 & 0x800000;
                                int PROBEHEAT = myCurrentData.lightBits2 & 0x1000000;
                                int SEAT_ARM = myCurrentData.lightBits2 & 0x2000000;
                                int BUC = myCurrentData.lightBits2 & 0x4000000;
                                int FUEL_OIL_HOT = myCurrentData.lightBits2 & 0x8000000;
                                long ANTI_SKID = myCurrentData.lightBits2 & 0x10000000;
                                long TFR_ENGAGED = myCurrentData.lightBits2 & 0x20000000;
                                long GEARHANDLE = myCurrentData.lightBits2 & 0x40000000;

                                int Elec_Fault = myCurrentData.lightBits3 & 0x400;
                                int Lef_Fault = myCurrentData.lightBits3 & 0x800;
                                int OnGround = myCurrentData.lightBits3 & 0x1000;
                                int FlcsBitRun = myCurrentData.lightBits3 & 0x2000;
                                int FlcsBitFail = myCurrentData.lightBits3 & 0x4000;
                                int NoseGearDown = myCurrentData.lightBits3 & 0x10000;
                                int LeftGearDown = myCurrentData.lightBits3 & 0x20000;
                                int RightGearDown = myCurrentData.lightBits3 & 0x40000;
                                int ParkBrakeOn = myCurrentData.lightBits3 & 0x100000;
                                int Power_Off = myCurrentData.lightBits3 & 0x200000;
                                int cadc = myCurrentData.lightBits3 & 0x400000;

                                int FwdFuelLow = myCurrentData.lightBits2 & 0x40000;
                                int AftFuelLow = myCurrentData.lightBits2 & 0x80000;
                                int EPUOn = myCurrentData.lightBits2 & 0x100000;
                                int JFSOn = myCurrentData.lightBits2 & 0x200000;





                                // -------------------------------------------------------------------------------- //
                                //                          AOA INDEXER                                             //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                int AOA_Above = (myCurrentData.lightBits & 0x1000);
                                int AOA_OnPath = (myCurrentData.lightBits & 0x2000);
                                int AOA_Below = (myCurrentData.lightBits & 0x4000);

                                int RefuelRDY = (myCurrentData.lightBits & 0x8000);
                                int RefuelAR = (myCurrentData.lightBits & 0x10000);
                                int RefuelDSC = (myCurrentData.lightBits & 0x20000);


                                // -------------------------------------------------------------------------------- //
                                // LightBits2              THREAT WARNING PRIME                                     //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                int HandOff = myCurrentData.lightBits2 & 0x1;
                                int Launch = myCurrentData.lightBits2 & 0x2;
                                int PriMode = myCurrentData.lightBits2 & 0x4;
                                int Naval = myCurrentData.lightBits2 & 0x8;
                                int Unk = myCurrentData.lightBits2 & 0x10;
                                int TgtSep = myCurrentData.lightBits2 & 0x20;


                                // -------------------------------------------------------------------------------- //
                                //                         AUX THREAT WARNING                                       //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                int AuxSrch = myCurrentData.lightBits2 & 0x1000;
                                int AuxAct = myCurrentData.lightBits2 & 0x2000;
                                int AuxLow = myCurrentData.lightBits2 & 0x4000;
                                int AuxPwr = myCurrentData.lightBits2 & 0x8000;

                                // -------------------------------------------------------------------------------- //
                                //                         ECM                                                      //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                int EcmPwr = myCurrentData.lightBits2 & 0x10000;
                                int EcmFail = myCurrentData.lightBits2 & 0x20000;

                                // -------------------------------------------------------------------------------- //
                                // LightBits3              ELEC PANEL                                               //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                int FlcsPmg = myCurrentData.lightBits3 & 0x1;
                                int MainGen = myCurrentData.lightBits3 & 0x2;
                                int StbyGen = myCurrentData.lightBits3 & 0x4;
                                int EpuGen = myCurrentData.lightBits3 & 0x8;
                                int EpuPmg = myCurrentData.lightBits3 & 0x10;
                                int ToFlcs = myCurrentData.lightBits3 & 0x20;
                                int FlcsRly = myCurrentData.lightBits3 & 0x40;
                                int BatFail = myCurrentData.lightBits3 & 0x80;


                                // -------------------------------------------------------------------------------- //
                                //                         EPU PANEL                                                //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                int Hydrazine = myCurrentData.lightBits3 & 0x100;
                                int Air = myCurrentData.lightBits3 & 0x200;





                                // -------------------------------------------------------------------------------- //
                                //                         LEFT AUX CONSOLE                                         //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                int SpeedBrake = myCurrentData.lightBits3 & 0x800000;

                                if (_prevSpeedBrake != SpeedBrake)
                                {
                                    if (SpeedBrake == OFF)
                                    {
                                        // Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 0);
                                        _prevSpeedBrake = SpeedBrake;
                                        Thread.Sleep(5);
                                    }

                                    if (SpeedBrake == ON)
                                    {
                                        //  Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 1);
                                        _prevSpeedBrake = SpeedBrake;
                                        Thread.Sleep(5);
                                    }
                                }
                                // -------------------------------------------------------------------------------- //
                                //                         THREAD WARNING PRIME - Additional Bits                   //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                int SysTest = myCurrentData.lightBits3 & 0x1000000;

                                if (_prevSysTest != SysTest)
                                {
                                    if (SysTest == OFF)
                                    {
                                        // Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 0);
                                        _prevSysTest = SysTest;
                                        Thread.Sleep(5);
                                    }

                                    if (SysTest == ON)
                                    {
                                        //  Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 1);
                                        _prevSysTest = SysTest;
                                        Thread.Sleep(5);
                                    }
                                }
                                // -------------------------------------------------------------------------------- //
                                //                         HSI BITS                                                 //
                                //     -> Not all are necessary because using YAME suite!!! <-                      //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                int OuterMarker = myCurrentData.hsiBits & 0x4000;
                                int MiddleMarker = myCurrentData.hsiBits & 0x8000;
                                int Flying = myCurrentData.hsiBits & 0x10000;

                                if (_prevOuterMarker != OuterMarker)
                                {
                                    if (OuterMarker == OFF)
                                    {
                                        // Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 0);
                                        _prevOuterMarker = OuterMarker;
                                        Thread.Sleep(5);
                                    }

                                    if (OuterMarker == ON)
                                    {
                                        //  Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 1);
                                        _prevOuterMarker = OuterMarker;
                                        Thread.Sleep(5);
                                    }
                                }

                                if (_prevMiddleMarker != MiddleMarker)
                                {
                                    if (MiddleMarker == OFF)
                                    {
                                        // Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 0);
                                        _prevMiddleMarker = MiddleMarker;
                                        Thread.Sleep(5);
                                    }

                                    if (MiddleMarker == ON)
                                    {
                                        //  Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 1);
                                        _prevMiddleMarker = MiddleMarker;
                                        Thread.Sleep(5);
                                    }
                                }

                                if (_prevFlying != Flying)
                                {
                                    if (Flying == OFF)
                                    {
                                        // Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 0);
                                        _prevFlying = Flying;
                                        Thread.Sleep(5);
                                    }

                                    if (Flying == ON)
                                    {
                                        //  Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 1);
                                        _prevFlying = Flying;
                                        Thread.Sleep(5);
                                    }
                                }
                            }

                        }
                    }
                }

            }
        }
       

            private void checkBox1_CheckedChanged(object sender, EventArgs e)
            {
                if (checkBox1.Checked)
                {
                    PHCC_Test_Environment = true;
                }
                else
                {
                    PHCC_Test_Environment = false;
                }
            }

            private void button6_Click(object sender, EventArgs e)
            {
                Application.Restart();
            }

        }


    } 


//                  14.08.2017
//
//                  Need to implement:
//                  ------------------
//
// - TacanSources:  UFC = 0
//                  AUX = 1
//                  NUMBER_OF_SOURCES = 2
//
// - TacanBits:     band = 0x01               --> true, if Band is X.
//                  mode = 0x02               --> true, if AA selected.
//
// - HsiBits:       MainPower                 --> integer! --> 0= down, 1= middle, 2= up.
// 
// - PowerBits:     JetFuelStarter  0x40      ---> usable for Magnetic-Switch!
// 
// - BlinkBits:     OuterMarker     0x01
//                  MiddleMarker    0x02
//                  PROBEHEAT       0x04
//                  AuxSrch         0x08
//                  Launch          0x10
//                  PriMode         0x20
//                  Unk             0x40
//
// - CmdsModes:     CmdsOFF = 0
//                  CmdsSTBY = 1
//                  CmdsMAN = 2
//                  CmdsSEMI = 3
//                  CmdsAUTO = 4
//                  CmdSBYP = 5
//
// - NavModes:      ILS_TACAN = 0
//                  TACAN = 1
//                  NAV = 2
//                  ÍLS_NAV = 3
//
//                  BupUhfPreset                --> integer!
//
//                  currentTime                 --> integer, time in seconds (max 60 * 60 *24).

