// Erstellt mit:
// ------------ VISUAL STUDIO C# 2017 --------------
//
// Erforderliche Ressource:
// ------------ Frame Network 4.5.1 ----------------
//
// Datum: 30.09.2019 LE



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

using log4net;
using Costura;
using F4KeyFile;
using System.Timers;
using Timer = System.Timers.Timer;


// Adresses for PHCC-boards (hardcoded on PIC):
//
// PHCC-CMDS: 0x44
// PHCC-40DO RightEyebrow, Caution Panel: 0x10
// PHCC-40DO LeftEyebrow, Indexers, Other Indicators: 0x12

// Adresses for PHCC-boards (hardcoded on PIC)
// All charges from "Airdances PHCC lot":

// DOA_Stepper : 0x73, 0x72, 0x71, 0x70
// DOA_Aircore : 0x83, 0x82, 0x81, 0x80
// DOA_Servo   : 0x2C, 0x2B, 0x2A
// DOA_AnOut   : 0x23, 0x22, 0x21, 0x20
// DOA_40DO    : 0x13, 0x12, 0x11, 0x10
// DOA_7seg    : 0x34, 0x33, 0x32, 0x31, 0x30

// Definition für 7-Segment-Anzeige:
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



        //public string[,] keycombo = new string[,] { }; // OFFENSICHTLICH NICHT VERWENDET!


        // -------------------------------------------------------------------------------------
        // True, solange PHCC getestet wird.
        // Ausschalten, wenn Arduino ebenfalls läuft!
        // -------------------------------------------------------------------------------------
        public bool PHCC_Test_Environment = false;

        // -------------------------------------------------------------------------------------
        // Nachfolgene Zeilen: 
        //
        // OFFENSICHTLICH VARIABLEN FÜR POTENTIOMETER ----> NICHT ERFORDERLICH, da LEO BODNAR VORHANDEN!
        //
        // -------------------------------------------------------------------------------------
        //public sbyte altAB = 0;
        //public sbyte altAB2 = 0;

        // public sbyte[] schritteTab = {0,0,0,0,0,0,0,-1,0,0,0,0,0,1,0,0}; // 1/1 resolution, bad results!!!!
        //  public sbyte[] schritteTab = {0,0,1,0,0,0,0,-1,-1,0,0,0,0,1,0,0}; // TEST2
        // public sbyte[] schritteTab = { 0, 0, -1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, -1, 0, 0 }; // TEST2
        // public sbyte[] schritteTab = { 0,  1, -1, 0, -1, 0, 0,  1,  1, 0, 0, -1, 0, -1,  1, 0 }; //TEST1
        // public sbyte[] schritteTab = { 0, -1, 1, 0, 1, 0, 0, -1, -1, 0, 0, 1, 0, 1, -1, 0 };  //  1/4 resolution

        //public sbyte[] schritteTab = { 0, 0, 1, 0, 0, 0, 0, -1, -1, 0, 0, 0, 0, 1, 0, 0 }; // ALPS UHF PRESETS !-WORKS-

        //public bool CW;
        //public bool CCW;
        //public string TestPulled;
        //public string TestPulled2;
        //public bool CWAlt = false;
        //public bool CCWAlt = false;

        //public int CCWbyte;
        //public int CWbyte;
        //public int wert = 0;  //sbyte
        //public int wertAlt = 0;
        //public bool neutral = false;

        //public bool left;
        //public bool right;
        //public int Position;

        //public int position = 0;
        //public int positionAlt = 0;

        // -------------------------------------------------------------------------------------
        public bool on = false;

        // Variable zum Pruefen, ob FalconBMS-Software laeuft oder nicht.
        // Standard: Auf false.
        public bool _keepRunning = false;

        // Variable, um Warning-Lights, etc. 
        // nach erfolgtem Flug beim Betreten der UI wieder auszuschalten.  // WORKS! 26.09.2019 LE.
        public int Reset_PHCC_when_in_UI = 1;

        public Form1()
        {
            
            InitializeComponent();
            getAvaiblePorts();

            this.label1.Text = "";
            this.label2.Text = "";
            this.label3.Text = "";
            this.label4.Text = "";
            this.label6.Text = "";

            //   SpeechSynthesizer readerSpeech = new SpeechSynthesizer();
            //   readerSpeech = new SpeechSynthesizer();
            //    readerSpeech.Rate = -2;
            //    readerSpeech.Speak("Welcome back Stryker, software started. All systems are working fine. Have a good flight sir - and always clear skies.");

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

           // Port3_PHCC_Input_Output.PortName = "COM1";



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
        public bool _ON = true;
        public bool _OFF = false;
        public int ON = 1;
        public int OFF = 0;

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
        public bool _prevGo = false;
        public bool _prevNoGo = false;
        public bool _prevDispenseReady = false;
        public bool _prevChaffWasLow = false;
        public bool _prevFlareWasLow = false;

        // RIGHT EYEBROW - WARNING LIGHTS:
        public bool _prevENG_FIRE;
        public bool _prevENGINE;
        public bool _prevHYD;
        public bool _prevFLCS;
        public bool _prevDbuWarn;
        public bool _prevCAN;
        public bool _prevT_L_CFG;
        public bool _prevOXY_BROW;

        public byte Eyebrow_Right;

        public byte bOld = 0;
        public byte bNew = 0;
        public byte all = 0;

        // CAUTIION PANEL 
        // 1st ROW (starts from left):
        public bool _prevFltControlSys = false;
        public bool _prevElec_Fault = false;
        public bool _prevProbeHeat = false;
        public bool _prevProbeHeat_blinking = false;
        public bool ProbeHeat_blinking = false;
        public bool ProbeHeat_blinking_active = false;
        public bool _prevcadc = false;
        public bool _prevCONFIG = false;
        public bool _prevATF_Not_Engaged = false;
        public bool _prevFwdFuelLow = false;
        public bool _prevAftFuelLow = false;

        public bool FltControlSys_active;
        public bool Elec_Fault_active;
        public bool ProbeHeat_active;
        public bool cadc_active;
        public bool CONFIG_active;
        public bool ATF_Not_Engaged_active;
        public bool FwdFuelLow_active;
        public bool AftFuelLow_active;

        public byte CautionPanel_Row1;
        public byte CautionPanel_Row1_ProbeHeat_blinking;

        // CAUTIION PANEL 
        // 2nd ROW (starts from left):
        public bool _prevEngineFault;
        public bool _prevSEC;
        public bool _prevFUEL_OIL_HOT;
        public bool _prevInlet_Icing;
        public bool _prevOverheat;
        public bool _prevEEC;
        public bool _prevBUC;

        public byte CautionPanel_Row2;

        // CAUTIION PANEL 
        // 3rd ROW (starts from left):
        public bool _prevAvionics;
        public bool _prevEQUIP_HOT;
        public bool _prevRadarAlt;
        public bool _prevIFF;
        public bool _prevNuclear;

        public byte CautionPanel_Row3;

        // CAUTIION PANEL 
        // 4th ROW (starts from left):
        public bool _prevSEAT_ARM;
        public bool _prevNWSFail;
        public bool _prevANTI_SKID;
        public bool _prevHook;
        public bool _prevOXY_LOW;
        public bool _prevCabinPress;

        public byte CautionPanel_Row4;

        // CAUTION PANEL
        // other:
        public bool _prevAllLampBitsOn;

        // MISC PANEL & LEFT EYEBROW - WARNING LIGHTS:
        public bool _prevTFR_ENGAGED;
        public bool _prevTFR_STBY;
        public bool _prevECM;

        public bool _prevMasterCaution;
        public bool _prevTF;

        public byte Misc;

        // TWP PANEL
        public bool Launch_active;
        public bool Launch_blinking_active;
        public bool HandOff_active;
        public bool PriMode_active;
        public bool Naval_active;
        public bool Unk_active;
        public bool TgtSep_active;
        public bool SysTest_active;

        public bool _prevLaunch;
        public bool _prevLaunch_blinking;
        public bool _prevHandOff;
        public bool _prevNaval;
        public bool _prevUnk;
        public bool _prevTgtSep;
        public bool _prevSysTest;
        public bool _prevPriMode;

        public bool _prevAuxPwr;    /* Variable für TWP SysTest "POWER ON" */ 

        public byte TWP;
        public byte TWP_Shp;

        public byte blinkTWP;
        public byte blinkTWP_Shp;

        public byte blinkNew1 = 0;
        public byte blinkOld1 = 0;
        public byte blinkAll1 = 0;

        public byte blinkNew2 = 0;
        public byte blinkOld2 = 0;
        public byte blinkAll2 = 0;

        // Variablen für Lightbits, Lightbits2, Lightbits3 & HSI_Bits:
        public int _prevONGROUND = 0;
        public int _prevFlcs_ABCD = 0;
        public int _prevLEFlaps = 0;
        public int _prevFuelLow = 0;

        public int _prevAutoPilotOn = 0;

        public int _prevAOA_Above = 0;
        public int _prevAOA_OnPath = 0;
        public int _prevAOA_Below = 0;

        public int _prevRefuelRDY = 0;
        public int _prevRefuelAR = 0;
        public int _prevRefuelDSC = 0;

        public int _prevAuxSrch = 0;
        public int _prevAuxAct = 0;
        public int _prevAuxLow = 0;

        public int _prevEcmPwr = 0;
        public int _prevEcmFail = 0;

        public int _prevEPUOn = 0;
        public int _prevJFSOn = 0;

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
        public bool SimSeatOn;

        // UHF Radio:
        public int myBupUhfPreset;
        public int _prevmyBupUhfPreset = 0;

        public int myBupUhfPresetEiner;
        public int myBupUhfPresetZehner = 0;

        public int _prevmyBupUhfPresetEiner = 9;
        public int _prevmyBupUhfPresetZehner = 9;

        public int myBupUhfFreq;
        public int _prevmyBupUhfFreq = 0;

        public int myBupUhfFreqHunderttausender;
        public int myBupUhfFreqZehntausender;
        public int myBupUhfFreqTausender;
        public int myBupUhfFreqHunderter;
        public int myBupUhfFreqZehner;
        public int myBupUhfFreqEiner;

        public int _prevmyBupUhfFreqHunderttausender = 9;
        public int _prevmyBupUhfFreqZehntausender = 9;
        public int _prevmyBupUhfFreqTausender = 9;
        public int _prevmyBupUhfFreqHunderter = 9;
        public int _prevmyBupUhfFreqZehner = 9;
        public int _prevmyBupUhfFreqEiner = 9;

        // Allgemeine Zählervariable:
        public int i = 0;

        public string Baudrate;
        public string FirmwareVersion;

        // ----------------------------------------------------------------------------------//
        //       Nur erforderlich, wenn als Anwendung auf einem Single-PC konzipiert!        // 
        // ----------------------------------------------------------------------------------//
        //   [DllImport("User32.dll")]                                                       //
        //     static extern IntPtr FindWindow(string lpClassName, string lpWindowName);     //
        //   [DllImport("User32.dll")]                                                       //
        //   static extern int SetForegroundWindow(IntPtr hWnd);                             //
        // ----------------------------------------------------------------------------------//

        // Test Threads - in Endversion NICHT erforderlich:
        private Thread DataThread;
        private Thread TestingDiversesThread;

        // Arduino Threads, 
        // ENGINE INSTRUMENT Gauges:
        private Thread OilThread;
        private Thread NozzleThread;
        private Thread RPMThread;
        private Thread FTITThread;


        // Arduino Threads:
        // RIGHT AUX CONSOLE Gauges:
        private Thread CompassThread;
        private Thread FuelQtyThread;
        private Thread HydAThread;                           /* erforderlich?  ggfs. Hyd A & B zusammenfassen? */
        private Thread HydBThread;                           /* erforderlich?  ggfs. Hyd A & B zusammenfassen? */
        private Thread LiquidOxyThread;                      /* erforderlich?  ggfs. mit EPU & Cabin Press zusammenfassen? */
        private Thread EpuFuelThread;                        /* erforderlich?  ggfs. mit Liquid Oxy & Cabin Press zusammenfassen? */
        private Thread CabinPressThread;                     /* erforderlich?  ggfs. mit EPU & Liquid Oxy zusammenfassen? */
        private Thread ClockThread;
        private Thread TwaPanelThread;

        // Arduino Threads, 
        // LEFT AUX CONSOLE:
        private Thread SpeedBrakeThread;

        // Arduino Threads,
        // LEFT CONSOLE:
        private Thread Blink_JetEngineStartPanelThread;
        private Thread TrimPanelThread;

        // Arduino Threads,
        // RIGHT CONSOLE Gauge:
        private Thread OxyPanelThread;                 /* erforderlich? ggfs. mit EPU, Cabinpress & Liquid Oxy zusammenfassen? */


        // PHCC Threads, 
        // JAY-EL OUTPUTS:
        private Thread SimOutputThread;
        private Thread Blink_ProbeHeatThread;
        private Thread Blink_MissileLaunchThread;
        private Thread Blink_SysTestThread;

        // PHCC Threads,
        // SWITCHES Abfragen: 
        private Thread SwitchesThread;

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

                        Port3_PHCC_Input_Output = new Device("COM1");

                        this.label1.Text = "Self-Test running...";
                        this.label2.Text = "";
                        this.label3.Text = "";
                        this.label4.Text = "";
                        this.label6.Text = "";
                      
                        try {
                            //Baudrate = Port3_PHCC_Input_Output.SerialPort.BaudRate.ToString();
                            //FirmwareVersion = Port3_PHCC_Input_Output.FirmwareVersion;
                     
                            //Port3_PHCC_Input_Output.DoaSendRaw(0x44, 0, 32);
                         
                        }

                        catch //(Exception ex)
                        {
                            this.label1.Text = "Self-Test accomplished.";
                          
                            this.label6.ForeColor = Color.Red;
                            this.label6.Text = "Connection not established!";
                           
                            this.label4.ForeColor = Color.Red;
                            this.label4.Text = "PHCC NOT CONNECTED OR NO POWER!!!";
                           // this.label2.Text=ex.Message.ToString();
                         
                          


                            for (int z = 1; z <= 9;)
                            {
                                this.label3.Text = "";
                                Wait(500);
                                this.label3.Text = "Check powersupply and try again...";
                                Wait(500);
                                z++;
                            }
                          //  Application.Restart();
                         //   Environment.Exit(0);
                            return;
                        }

                        this.label2.ForeColor = Color.Green;
                        this.label2.Text = "PHCC communication started! " + Baudrate + FirmwareVersion;
                      
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
                    this.DataThread = new Thread(new ThreadStart(Data));
                    this.TestingDiversesThread = new Thread(new ThreadStart(TestingDiverses));

                    this.OilThread = new Thread(new ThreadStart(Oil));
                    this.NozzleThread = new Thread(new ThreadStart(Nozzle));
                    this.RPMThread = new Thread(new ThreadStart(RPM));
                    this.FTITThread = new Thread(new ThreadStart(FTIT));

                    this.CompassThread = new Thread(new ThreadStart(Compass));
                    this.FuelQtyThread = new Thread(new ThreadStart(FuelQty));
                    this.HydAThread = new Thread(new ThreadStart(HydA));
                    this.HydBThread = new Thread(new ThreadStart(HydB));
                    this.LiquidOxyThread = new Thread(new ThreadStart(LiquidOxy));
                    this.EpuFuelThread = new Thread(new ThreadStart(EpuFuel));
                    this.CabinPressThread = new Thread(new ThreadStart(CabinPress));
                    this.ClockThread = new Thread(new ThreadStart(Clock));
                    this.TwaPanelThread = new Thread(new ThreadStart(TwaPanel));

                    this.SpeedBrakeThread = new Thread(new ThreadStart(Speedbrake));

                    this.Blink_JetEngineStartPanelThread = new Thread(new ThreadStart(Blink_JetEngineStartPanel));
                    this.TrimPanelThread = new Thread(new ThreadStart(TrimPanel));

                    this.OxyPanelThread = new Thread(new ThreadStart(OxyPanel));

                    this.Blink_ProbeHeatThread = new Thread(new ThreadStart(Blink_ProbeHeat));
                    this.Blink_MissileLaunchThread = new Thread(new ThreadStart(Blink_MissileLaunch));
                    this.Blink_SysTestThread = new Thread(new ThreadStart(Blink_SysTest));
                    this.SimOutputThread = new Thread(new ThreadStart(SimOutput));

                    this.SwitchesThread = new Thread(new ThreadStart(Switches));

                    //this.WaitThread = new Thread(new ParameterizedThreadStart(Wait));

                    DataThread.Start();                 /* In Endversion NICHT ERFORDERLICH! - vorübergehend abgeschaltet! */
                    //  TestingDiversesThread.Start();      /* In Endversion NICHT ERFORDERLICH! - vorübergehend abgeschaltet! */

                    //  OilThread.Start();                  /* vorübergehend abgeschaltet! */
                    //  NozzleThread.Start();               /* vorübergehend abgeschaltet! */
                    //  RPMThread.Start();                  /* vorübergehend abgeschaltet! */
                    //  FTITThread.Start();                 /* vorübergehend abgeschaltet! */

                    //  CompassThread.Start();
                    //  FuelQtyThread.Start();
                    //  HydAThread.Start();
                    //  HydBThread.Start();
                    //  LiquidOxyThread.Start();
                    //  EpuFuelThread.Start();
                    //  CabinPressThread.Start();
                    //  ClockThread.Start();
                    //  TwaPanelThread.Start();

                    // SpeedBrakeThread.Start();

                    // Blink_JetEngineStartPanelThread.Start();
                    // TrimPanelThread.Start();

                    SimOutputThread.Start();
                    Blink_ProbeHeatThread.Start();
                    Blink_MissileLaunchThread.Start();
                    Blink_SysTestThread.Start();

                    SwitchesThread.Start();



                    try
                    {
                        // Port3_PHCC_Input_Output.DoaSendRaw(0x44, 34, 0);
                        //   Thread.Sleep(5);

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
        //public bool CheckRunning()
        //{
        //    using (Reader myReader = new F4SharedMem.Reader())

        //        while (_keepRunning == true)
        //        {
        //            if (myReader.IsFalconRunning == true)
        //            {
        //                while ((myReader.IsFalconRunning) && (_keepRunning))
        //                {
        //                    return true;
        //                }
        //                if myRader.Is
        //            }
        //            else { return false; }
        //        }
        //    if (_keepRunning == false){ return false; }
        //}

        public void Data()
        {
            // Test-Umgebung für diverse Funktionen.
            // In der Final-Version nicht erforderlich!

            using (Reader myReader = new F4SharedMem.Reader())

             // while (_keepRunning == true)
             //   {
             //      if (myReader.IsFalconRunning == true)
              //      {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {
                            //  FlightData myFlightData = new FlightData();
                            FlightData mySharedMemTest = myReader.GetCurrentData();
                          
                            int TimeInSeconds = mySharedMemTest.currentTime;
                            int Days = TimeInSeconds / 86400;
                            int   Hours = TimeInSeconds % 86400 / 3600;
                            int    Minutes = TimeInSeconds % 3600 / 60;
                            int  Seconds = TimeInSeconds % 60;
                            // Rohdaten / Integer anzeigen:
                            this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), TimeInSeconds.ToString());

                            if (Hours < 10) // Null voranstellen
                            {
                                this.label7.Invoke(new Action<string>(s => { label7.Text = s; }), "0" + Hours.ToString());
                            }
                            else
                            {
                                this.label7.Invoke(new Action<string>(s => { label7.Text = s; }), Hours.ToString());
                            }
                            if (Minutes < 10) // Null voranstellen
                            {
                                this.label8.Invoke(new Action<string>(s => { label8.Text = s; }), "0" + Minutes.ToString());
                            }
                            else
                            {
                                    this.label8.Invoke(new Action<string>(s => { label8.Text = s; }), Minutes.ToString());
                            }
                            if (Seconds < 10) // Null voranstellen
                            {
                                this.label9.Invoke(new Action<string>(s => { label9.Text = s; }), "0" + Seconds.ToString());
                            }
                            else
                            {
                                this.label9.Invoke(new Action<string>(s => { label9.Text = s; }), Seconds.ToString());
                            }
                        }

                        // IntellivibeData <--- Status des Fliegers abfragen!
                        //---------------------------------------------------

                        //byte[] pilotsStatus = mySharedMem0.pilotsStatus.ToArray();
                        //Byte flags = pilotsStatus[0];
                        //String bin = Convert.ToString(flags, 2);  //    10110 (binär)

                        // byte[] myArr = (byte[])mySharedMem0.pilotsStatus.ToArray();
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
                        //  if (myArr[0] == 3)
                        //  {
                        //       this.label10.Invoke(new Action<string>(s => { label10.Text = s; }), "Flying");

                        //    }
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

                        //    Application.DoEvents();

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


             //   }
      //  }
        

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

                                //// Testaufbau ...
                                //// Byte statt String an Arduino senden:
                                //float myOilPressureValueFloat = mySharedMem1.oilPressure;
                                //byte[] myOilPressureValueByte = BitConverter.GetBytes(myOilPressureValueFloat);
                                //Console.Write("\n" + (myOilPressureValueByte));

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

        public void Compass() { }
        public void FuelQty() { }
        public void HydA() { }
        public void HydB() { }
        public void LiquidOxy() { }
        public void EpuFuel() { }
        public void CabinPress() { }
        public void Clock() { }

        public void TwaPanel() { }

        public void Speedbrake()
        {
            using (Reader myReader = new F4SharedMem.Reader())

                while (_keepRunning == true)
                {
                    if (myReader.IsFalconRunning == true)
                    {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {

                            {
                                FlightData mySharedMem6 = myReader.GetCurrentData();

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

        public void Blink_JetEngineStartPanel() { }
        public void TrimPanel() { }

        public void OxyPanel() { }


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


        public void Blink_ProbeHeat()
        { // WORKS! 16.05.2019 LE.
            using (Reader myReader = new F4SharedMem.Reader())

               //while (_keepRunning == true)
               //{
               //     if (myReader.IsFalconRunning == true)
               //    {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {
                            FlightData mySharedMem8 = myReader.GetCurrentData();

                    //if (ProbeHeat_blinking == true)
                    //{

                   // if ((((BlinkBits)mySharedMem8.blinkBits & BlinkBits.PROBEHEAT)
                   //   == BlinkBits.PROBEHEAT) != _prevProbeHeat_blinking)
                  //  {
                        while (((BlinkBits)mySharedMem8.blinkBits & BlinkBits.PROBEHEAT)
                         == BlinkBits.PROBEHEAT)
                        {
                            // Check if it's time to leave "while loop":
                            mySharedMem8 = myReader.GetCurrentData();
                            ProbeHeat_blinking = true;

                            CautionPanel_Row1 += 4;
                            Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                            ProbeHeat_blinking_active = true;
                            Thread.Sleep(125); //125, fast blinktime
                        Application.DoEvents();

                            CautionPanel_Row1 -= 4;
                            Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                            ProbeHeat_blinking_active = false;
                            Thread.Sleep(125); //125, fast blinktime
                        Application.DoEvents();
                    }
                     //   _prevProbeHeat_blinking = ((BlinkBits)mySharedMem8.blinkBits & BlinkBits.PROBEHEAT)
                     //       == BlinkBits.PROBEHEAT;
                    }
                       // if (((BlinkBits)mySharedMem8.blinkBits & BlinkBits.PROBEHEAT)
                     //      != BlinkBits.PROBEHEAT)
                     //   {
                            // if (ProbeHeat_blinking_active=true) { CautionPanel_Row1 -= 4; }
                            //  Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);

                         //   ProbeHeat_blinking_active = false;
                        //    ProbeHeat_blinking = false;
                            //   }
                           
                       // }  
                          //  }
                       // }
               //     }
               //}
           
        }


        public void Blink_MissileLaunch()
        { // WORKS! 28.02.2020 LE.
            using (Reader myReader = new F4SharedMem.Reader())

                //while (_keepRunning == true)
                //{
                //    if (myReader.IsFalconRunning == true)
                //    {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {
                            FlightData mySharedMem9 = myReader.GetCurrentData();

                            if (SysTest_active == false)
                            {
                                if ((((BlinkBits)mySharedMem9.blinkBits & BlinkBits.Launch)
                                    == BlinkBits.Launch) != _prevLaunch_blinking)
                                {
                                    while (((BlinkBits)mySharedMem9.blinkBits & BlinkBits.Launch)
                                       == BlinkBits.Launch)
                                    {
                                        // Check if it's time to leave the "while-loop":
                                        mySharedMem9 = myReader.GetCurrentData();

                                        TWP_Shp += 12;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 6, TWP_Shp);
                                        Launch_blinking_active = true;
                                        Wait(250); //fast blinktime

                                        TWP_Shp -= 12;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 6, TWP_Shp);
                                        Launch_blinking_active = false;
                                        Wait(250); //fast blinktime
                                      //  Application.DoEvents();
                                    }
                                    if (((BlinkBits)mySharedMem9.blinkBits & BlinkBits.Launch)
                                       == 0)
                                    {
                                        if (Launch_blinking_active) { TWP_Shp -= 12; }
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 6, TWP_Shp);
                                        Launch_blinking_active = false;
                                    }
                                    _prevLaunch_blinking = ((BlinkBits)mySharedMem9.blinkBits &
                                     BlinkBits.Launch) == BlinkBits.Launch;
                                }
                            }
                        }
                //    }
                //}
        }

        public void Blink_SysTest()  // WORKS! 28.02.2020 LE. 
        {
            using (Reader myReader = new F4SharedMem.Reader())

                //while (_keepRunning == true)
                //{
                //    if (myReader.IsFalconRunning == true)
                //    {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {
                            if (SysTest_active == true)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    blinkTWP_Shp = 15;
                                    Port3_PHCC_Input_Output.DoaSend40DO(0x11, 6, blinkTWP_Shp);

                                    blinkTWP = 255;
                                    Port3_PHCC_Input_Output.DoaSend40DO(0x11, 7, blinkTWP);
                                    Thread.Sleep(250);
                                    Application.DoEvents();

                                    blinkTWP_Shp = 1;
                                    Port3_PHCC_Input_Output.DoaSend40DO(0x11, 6, blinkTWP_Shp);

                                    blinkTWP = 75;
                                    Port3_PHCC_Input_Output.DoaSend40DO(0x11, 7, blinkTWP);
                                    Thread.Sleep(250);
                                    Application.DoEvents();

                                    if (i == 3)
                                    {
                                        // leave loop after blinking three times,
                                        // restore all lights to their ex-status (before they've entered the loop).
                                        blinkTWP_Shp = 0;
                                        if (Unk_active) { blinkTWP_Shp += 3; } else { blinkTWP_Shp += 1; }
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 6, blinkTWP_Shp);
                                        blinkTWP_Shp = 0;

                                        blinkTWP = 0;
                                        if (HandOff_active) { blinkTWP += 192; } else { blinkTWP += 64; }
                                        if (PriMode_active) { blinkTWP += 32; } else { blinkTWP += 16; }
                                        if (TgtSep_active) { blinkTWP += 4; } else { blinkTWP += 8; }
                                        if (SysTest_active) { blinkTWP += 3; } else { blinkTWP += 1; }
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 7, blinkTWP);
                                        blinkTWP = 0;
                                        SysTest_active = false;
                                    }
                                }
                            }
                        }
                //    }
                //}
        }

        public void SimOutput()
        {
            using (Reader myReader = new F4SharedMem.Reader())  //Neue Instanz von Reader, um die Daten von FalconBMS auslesen zu kÃƒÂ¶nnen.
            {
             //   while (_keepRunning == true)
              //  {
              //      if (myReader.IsFalconRunning == true)
              //      {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {
                            FlightData myFlightData = new FlightData();
                            FlightData myCurrentData = myReader.GetCurrentData();
                            myChaffCount = myCurrentData.ChaffCount;
                            myFlareCount = myCurrentData.FlareCount;

                            myBupUhfPreset = myCurrentData.BupUhfPreset;
                            myBupUhfFreq = myCurrentData.BupUhfFreq;

                            //////////////////////////////////////////////////////////////////////////////////////
                            //                                UHF BACKUP-RADIO                                  //
                            //               --> Need to crosscheck with "Flightdata.h" <<--                    //
                            //////////////////////////////////////////////////////////////////////////////////////
                            // -------------------------------------------------------------------------------- //
                            //                7segment-displays for preset / frequencies                        //
                            // --------------------------------------------------------------------- works! --- //
                            int BupUhfPresetEinertemp = myBupUhfPreset % 10;
                            int BupUhfPresetZehnertemp = myBupUhfPreset % 100;

                            myBupUhfPresetEiner = BupUhfPresetEinertemp;
                            myBupUhfPresetZehner = BupUhfPresetZehnertemp / 10;

                            int myBupUhfFreqEinertemp = myBupUhfFreq % 10;
                            int myBupUhfFreqZehnertemp = myBupUhfFreq % 100;
                            int myBupUhfFreqHundertertemp = myBupUhfFreq % 1000;
                            int myBuhpUhfFreqTausendertemp = myBupUhfFreq % 10000;
                            int myBuhpUhfFreqZehntausendertemp = myBupUhfFreq % 100000;
                            int myBupUhfFreqHunderttausendertemp = myBupUhfFreq % 1000000;

                            myBupUhfFreqEiner = myBupUhfFreqEinertemp;
                            myBupUhfFreqZehner = myBupUhfFreqZehnertemp / 10;
                            myBupUhfFreqHunderter = myBupUhfFreqHundertertemp / 100;
                            myBupUhfFreqTausender = myBuhpUhfFreqTausendertemp / 1000;
                            myBupUhfFreqZehntausender = myBuhpUhfFreqZehntausendertemp / 10000;
                            myBupUhfFreqHunderttausender = myBupUhfFreqHunderttausendertemp / 100000;

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



                            
                            if (((myCurrentData.hsiBits & 0x80000000) == 0) && (Reset_PHCC_when_in_UI == 1))
                            {
                                byte zero = 0;

                                // EYEBROW & CAUTION Panel reset:
                                Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, zero);
                                Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, zero);
                                Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, zero);
                                Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, zero);
                                Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, zero);

                                // MISC Panel-Lights & TWP LIGHTS reset:
                                Port3_PHCC_Input_Output.DoaSend40DO(0x11, 3, zero);
                                Port3_PHCC_Input_Output.DoaSend40DO(0x11, 4, zero);
                                Port3_PHCC_Input_Output.DoaSend40DO(0x11, 5, zero);
                                Port3_PHCC_Input_Output.DoaSend40DO(0x11, 6, zero);
                                Port3_PHCC_Input_Output.DoaSend40DO(0x11, 7, zero);

                                // UHF, sechststellige Backup-Frequency löschen: 
                                Port3_PHCC_Input_Output.DoaSendRaw(0x30, 16, zero);
                                Port3_PHCC_Input_Output.DoaSendRaw(0x30, 17, zero);
                                Port3_PHCC_Input_Output.DoaSendRaw(0x30, 18, zero);
                                Port3_PHCC_Input_Output.DoaSendRaw(0x30, 19, zero);
                                Port3_PHCC_Input_Output.DoaSendRaw(0x30, 20, zero);
                                Port3_PHCC_Input_Output.DoaSendRaw(0x30, 21, zero);

                                // UHF, zweistellige Preset-Anzeige löschen:
                                Port3_PHCC_Input_Output.DoaSendRaw(0x30, 22, zero);
                                Port3_PHCC_Input_Output.DoaSendRaw(0x30, 23, zero);

                                // CMDS, 
                                // When loading screen appears, clear L0 chaffs / L0 flares from previous missions.
                                // Statusanzeige & LED reset:
                                Port3_PHCC_Input_Output.DoaSendRaw(0x44, 34, zero);
                                Port3_PHCC_Input_Output.DoaSendRaw(0x44, 33, zero);

                                Reset_PHCC_when_in_UI = 0;
                               
                            }

                            //label4.Invoke(new Action<string>(s => { label4.Text = s; label4.ForeColor = Color.Red; }), myCurrentData.pilotsStatus[0].ToString());
                            //if (myCurrentData.pilotsStatus[0] == 3)
                            //{

                            label4.Invoke(new Action<string>(s => { label4.Text = s; label4.ForeColor = Color.Red; }), myCurrentData.pilotsStatus[0].ToString());
                            if ((myCurrentData.hsiBits & 0x80000000) != 0)
                            {

                                Reset_PHCC_when_in_UI = 1;

                                if (myBupUhfPreset != _prevmyBupUhfPreset)
                                {
                                    if (myBupUhfPresetEiner != _prevmyBupUhfPresetEiner)
                                    {
                                        if (myBupUhfPresetEiner == 0) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 23, 238);
                                        if (myBupUhfPresetEiner == 1) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 23, 96);
                                        if (myBupUhfPresetEiner == 2) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 23, 205);
                                        if (myBupUhfPresetEiner == 3) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 23, 233);
                                        if (myBupUhfPresetEiner == 4) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 23, 99);
                                        if (myBupUhfPresetEiner == 5) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 23, 171);
                                        if (myBupUhfPresetEiner == 6) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 23, 175);
                                        if (myBupUhfPresetEiner == 7) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 23, 224);
                                        if (myBupUhfPresetEiner == 8) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 23, 239);
                                        if (myBupUhfPresetEiner == 9) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 23, 235);
                                        _prevmyBupUhfPresetEiner = myBupUhfPresetEiner;
                                    }

                                    if (myBupUhfPresetZehner != _prevmyBupUhfPresetZehner)
                                    {
                                        if (myBupUhfPresetZehner == 0) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 22, 238);
                                        if (myBupUhfPresetZehner == 1) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 22, 96);
                                        if (myBupUhfPresetZehner == 2) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 22, 205);
                                        _prevmyBupUhfPresetZehner = myBupUhfPresetZehner;
                                    }

                                    _prevmyBupUhfPreset = myBupUhfPreset;

                                }

                                if (myBupUhfFreq != _prevmyBupUhfFreq)
                                {
                                    if (myBupUhfFreqEiner != _prevmyBupUhfFreqEiner)
                                    {
                                        if (myBupUhfFreqEiner == 0) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 21, 238);
                                        if (myBupUhfFreqEiner == 1) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 21, 96);
                                        if (myBupUhfFreqEiner == 2) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 21, 205);
                                        if (myBupUhfFreqEiner == 3) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 21, 233);
                                        if (myBupUhfFreqEiner == 4) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 21, 99);
                                        if (myBupUhfFreqEiner == 5) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 21, 171);
                                        if (myBupUhfFreqEiner == 6) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 21, 175);
                                        if (myBupUhfFreqEiner == 7) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 21, 224);
                                        if (myBupUhfFreqEiner == 8) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 21, 239);
                                        if (myBupUhfFreqEiner == 9) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 21, 235);
                                        _prevmyBupUhfFreqEiner = myBupUhfFreqEiner;
                                    }

                                    if (myBupUhfFreqZehner != _prevmyBupUhfFreqZehner)
                                    {
                                        if (myBupUhfFreqZehner == 0) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 20, 238);
                                        if (myBupUhfFreqZehner == 1) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 20, 96);
                                        if (myBupUhfFreqZehner == 2) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 20, 205);
                                        if (myBupUhfFreqZehner == 3) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 20, 233);
                                        if (myBupUhfFreqZehner == 4) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 20, 99);
                                        if (myBupUhfFreqZehner == 5) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 20, 171);
                                        if (myBupUhfFreqZehner == 6) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 20, 175);
                                        if (myBupUhfFreqZehner == 7) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 20, 224);
                                        if (myBupUhfFreqZehner == 8) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 20, 239);
                                        if (myBupUhfFreqZehner == 9) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 20, 235);
                                        _prevmyBupUhfFreqZehner = myBupUhfFreqZehner;
                                    }

                                    if (myBupUhfFreqHunderter != _prevmyBupUhfFreqHunderter)
                                    {
                                        if (myBupUhfFreqHunderter == 0) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 19, 238);
                                        if (myBupUhfFreqHunderter == 1) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 19, 96);
                                        if (myBupUhfFreqHunderter == 2) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 19, 205);
                                        if (myBupUhfFreqHunderter == 3) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 19, 233);
                                        if (myBupUhfFreqHunderter == 4) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 19, 99);
                                        if (myBupUhfFreqHunderter == 5) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 19, 171);
                                        if (myBupUhfFreqHunderter == 6) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 19, 175);
                                        if (myBupUhfFreqHunderter == 7) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 19, 224);
                                        if (myBupUhfFreqHunderter == 8) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 19, 239);
                                        if (myBupUhfFreqHunderter == 9) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 19, 235);
                                        _prevmyBupUhfFreqHunderter = myBupUhfFreqHunderter;
                                    }

                                    if (myBupUhfFreqTausender != _prevmyBupUhfFreqTausender)
                                    {
                                        if (myBupUhfFreqTausender == 0) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 18, 254);
                                        if (myBupUhfFreqTausender == 1) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 18, 112);
                                        if (myBupUhfFreqTausender == 2) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 18, 221);
                                        if (myBupUhfFreqTausender == 3) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 18, 249);
                                        if (myBupUhfFreqTausender == 4) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 18, 115);
                                        if (myBupUhfFreqTausender == 5) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 18, 187);
                                        if (myBupUhfFreqTausender == 6) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 18, 191);
                                        if (myBupUhfFreqTausender == 7) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 18, 240);
                                        if (myBupUhfFreqTausender == 8) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 18, 255);
                                        if (myBupUhfFreqTausender == 9) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 18, 251);
                                        _prevmyBupUhfFreqTausender = myBupUhfFreqTausender;
                                    }

                                    if (myBupUhfFreqZehntausender != _prevmyBupUhfFreqZehntausender)
                                    {
                                        if (myBupUhfFreqZehntausender == 0) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 17, 238);
                                        if (myBupUhfFreqZehntausender == 1) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 17, 96);
                                        if (myBupUhfFreqZehntausender == 2) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 17, 205);
                                        if (myBupUhfFreqZehntausender == 3) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 17, 233);
                                        if (myBupUhfFreqZehntausender == 4) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 17, 99);
                                        if (myBupUhfFreqZehntausender == 5) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 17, 171);
                                        if (myBupUhfFreqZehntausender == 6) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 17, 175);
                                        if (myBupUhfFreqZehntausender == 7) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 17, 224);
                                        if (myBupUhfFreqZehntausender == 8) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 17, 239);
                                        if (myBupUhfFreqZehntausender == 9) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 17, 235);
                                        _prevmyBupUhfFreqZehntausender = myBupUhfFreqZehntausender;
                                    }

                                    if (myBupUhfFreqHunderttausender != _prevmyBupUhfFreqHunderttausender)
                                    {
                                        if (myBupUhfFreqHunderttausender == 0) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 16, 238);
                                        if (myBupUhfFreqHunderttausender == 1) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 16, 96);
                                        if (myBupUhfFreqHunderttausender == 2) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 16, 205);
                                        if (myBupUhfFreqHunderttausender == 3) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 16, 233);
                                        if (myBupUhfFreqHunderttausender == 4) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 16, 99);
                                        if (myBupUhfFreqHunderttausender == 5) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 16, 171);
                                        if (myBupUhfFreqHunderttausender == 6) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 16, 175);
                                        if (myBupUhfFreqHunderttausender == 7) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 16, 224);
                                        if (myBupUhfFreqHunderttausender == 8) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 16, 239);
                                        if (myBupUhfFreqHunderttausender == 9) Port3_PHCC_Input_Output.DoaSendRaw(0x30, 16, 235);
                                        _prevmyBupUhfFreqHunderttausender = myBupUhfFreqHunderttausender;
                                    }

                                    myBupUhfFreq = _prevmyBupUhfFreq;

                                }

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
                        //                                    if (myCurrentData.cmdsMode == 0)
                        if (myCurrentData.cmdsMode != 0)
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
                                if (myFlareZehner == 9) Port3_PHCC_Input_Output.DoaSendRaw(0x44, 14, 57);
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
                            if (ChaffLow == false)
                            {
                                if ((myCurrentData.lightBits2 & 0x400) != 0)
                                //  if (((myCurrentData.lightBits2 & 0x400) != 0) && (ChaffLow == false))
                                {
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 8, 76);
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 9, 111);
                                    // Thread.Sleep(5);
                                    Wait(5);
                                    _prevChaffLow = (myCurrentData.lightBits2 & 0x400);
                                    ChaffLow = true;
                                    _prevChaffWasLow = true;
                                }
                            }
                            if (_prevChaffWasLow == true)
                            {
                                //if (((myCurrentData.lightBits2 & 0x400) != 0) && (_prevChaffWasLow == true))
                                if ((myCurrentData.lightBits2 & 0x400) != 0)
                                {
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 8, 76);
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 9, 111);
                                    //Thread.Sleep(5);
                                    Wait(5);
                                    _prevChaffLow = (myCurrentData.lightBits2 & 0x400);
                                    ChaffLow = true;
                                }
                            }
                            if (ChaffLow == true)
                            {
                                //  if (((myCurrentData.lightBits2 & 0x400) == 0) && (ChaffLow == true))
                                if ((myCurrentData.lightBits2 & 0x400) == 0)
                                {
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 8, 32);
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 9, 32);
                                    //Thread.Sleep(5);
                                    Wait(5);
                                    _prevChaffLow = (myCurrentData.lightBits2 & 0x400);
                                    ChaffLow = false;
                                }
                            }
                            if (FlareLow == false)
                            {
                                // Show "L0", when low on Flares:
                                //  if (((myCurrentData.lightBits2 & 0x800) != 0) && (FlareLow == false))
                                if ((myCurrentData.lightBits2 & 0x800) != 0)
                                {
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 12, 76);
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 13, 111);
                                    _prevFlareLow = (myCurrentData.lightBits2 & 0x800);
                                    //Thread.Sleep(5);
                                    Wait(5);
                                    FlareLow = true;
                                    _prevFlareWasLow = true;
                                }
                            }
                            if (_prevFlareWasLow == true)
                            {
                                //  if (((myCurrentData.lightBits2 & 0x800) != 0) && (_prevFlareWasLow == true))
                                if ((myCurrentData.lightBits2 & 0x800) != 0)
                                {
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 12, 76);
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 13, 111);
                                    _prevFlareLow = (myCurrentData.lightBits2 & 0x800);
                                    //Thread.Sleep(5);
                                    Wait(5);
                                    FlareLow = true;
                                }
                            }
                            if (FlareLow == true) {
                                //if (((myCurrentData.lightBits2 & 0x800) == 0) && (FlareLow == true))
                                if ((myCurrentData.lightBits2 & 0x800) == 0)
                                {
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 12, 32);
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 13, 32);
                                    // Thread.Sleep(5);
                                    Wait(5);
                                    _prevFlareLow = (myCurrentData.lightBits2 & 0x800);
                                    FlareLow = false;
                                }
                            }
                            if (AutoDegree == false) {
                                // Show "AUTO DEGR":
                                //  if (((myCurrentData.lightBits2 & 0x100) != 0) && (AutoDegree == false))
                                if ((myCurrentData.lightBits2 & 0x100) != 0)
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
                                    //Thread.Sleep(5);
                                    Wait(5);
                                    AutoDegree = true;
                                } }

                            if (AutoDegree == true) {
                                // Remove "AUTO DEGR":
                                //if (((myCurrentData.lightBits2 & 0x100) == 0) && (AutoDegree == true))
                                if ((myCurrentData.lightBits2 & 0x100) == 0)
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
                                    //Thread.Sleep(5);
                                    Wait(5);
                                    AutoDegree = false;
                                } }

                            if ((myCurrentData.lightBits2 & 0x40) != 0)
                            {

                                if ((myCurrentData.lightBits2 & 0x200) == 0)
                                // Lighten up "GO":
                                // if (((myCurrentData.lightBits2 & 0x40) != 0) && ((myCurrentData.lightBits2 & 0x200) == 0))// && ( _prevGo==false))
                                {
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 33, 4);
                                    _prevGo = true;
                                    _prevNoGo = false;
                                    _prevDispenseReady = false;
                                } }

                            // Lighten up "NOGO":
                            if (((myCurrentData.lightBits2 & 0x80) != 0))//&& ( _prevNoGo == false))
                            {
                                Port3_PHCC_Input_Output.DoaSendRaw(0x44, 33, 8);
                                _prevGo = false;
                                _prevNoGo = true;
                                _prevDispenseReady = false;
                            }
                            if ((myCurrentData.lightBits2 & 0x200) != 0)
                            {
                                // Lighten up "DISPENSE READY":
                                // if (((myCurrentData.lightBits2 & 0x200) != 0) && ((myCurrentData.lightBits2 & 0x40) == 0))// && ( _prevDispenseReady==false))
                                if ((myCurrentData.lightBits2 & 0x40) == 0)
                                {
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 33, 3);
                                    _prevGo = false;
                                    _prevNoGo = false;
                                    _prevDispenseReady = true;
                                }
                                if ((myCurrentData.lightBits2 & 0x40) != 0)
                                {
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 33, 5);
                                    _prevGo = true;
                                    _prevNoGo = false;
                                    _prevDispenseReady = true;
                                }
                            } }
                          
                                    // Lighten up "GO" and "DISPENSE READY":
                                    //if (((myCurrentData.lightBits2 & 0x200) != 0) && ((myCurrentData.lightBits2 & 0x40) != 0))// && (_prevDispenseReady == false) && (_prevGo == false))
                                    //{
                                    //    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 33, 5);
                                    //    _prevGo = true;
                                    //    _prevNoGo = false;
                                    //    _prevDispenseReady = true;
                                    //}
                               
                                // }
                                // Reset CMDS-display & LED when CMDS power is off:
                                if (myCurrentData.cmdsMode == 0)
                                {
                                    if ((myCurrentData.lightBits2 & 0x800) != 0) FlareLow = false;
                                    if ((myCurrentData.lightBits2 & 0x400) != 0) ChaffLow = false;
                                    if ((myCurrentData.lightBits2 & 0x100) != 0) AutoDegree = false;
                                    if ((myCurrentData.lightBits2 & 0x800) == 0) FlareLow = true;
                                    if ((myCurrentData.lightBits2 & 0x400) == 0) ChaffLow = true;
                                    if ((myCurrentData.lightBits2 & 0x100) == 0) AutoDegree = true;
                                    _prevGo = false;
                                    _prevNoGo = false;
                                    _prevDispenseReady = false;
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 34, 0);
                                    Port3_PHCC_Input_Output.DoaSendRaw(0x44, 33, 0);


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
                                        Eyebrow_Right += 2;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }
                                    else
                                    {
                                        Eyebrow_Right -= 2;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }

                                    _prevENG_FIRE = ((LightBits)myCurrentData.lightBits & LightBits.ENG_FIRE)
                                    == LightBits.ENG_FIRE;
                                }
                                // ---------------
                                //  ENGINE FAULT
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.ENGINE)
                                    == LightBits2.ENGINE != _prevENGINE)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.ENGINE)
                                        == LightBits2.ENGINE)
                                    {
                                        Eyebrow_Right += 1;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }
                                    else
                                    {
                                        Eyebrow_Right -= 1;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }
                                    _prevENGINE = ((LightBits2)myCurrentData.lightBits2 & LightBits2.ENGINE)
                                    == LightBits2.ENGINE;
                                }
                                // ---------------
                                //  HYD FAULT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.HYD)
                                    == LightBits.HYD != _prevHYD)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.HYD)
                                        == LightBits.HYD)
                                    {
                                        Eyebrow_Right += 4;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }
                                    else
                                    {
                                        Eyebrow_Right -= 4;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }
                                    _prevHYD = ((LightBits)myCurrentData.lightBits & LightBits.HYD)
                                    == LightBits.HYD;
                                }
                                // ---------------
                                //  FLCS FAULT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.FLCS)
                                    == LightBits.FLCS != _prevFLCS)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.FLCS)
                                        == LightBits.FLCS)
                                    {
                                        Eyebrow_Right += 8;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }
                                    else
                                    {
                                        Eyebrow_Right -= 8;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }
                                    _prevFLCS = ((LightBits)myCurrentData.lightBits & LightBits.FLCS)
                                    == LightBits.FLCS;
                                }
                                // ---------------
                                //  DBU FAULT
                                // ---------------
                                if (((LightBits3)myCurrentData.lightBits3 & LightBits3.DbuWarn)
                                    == LightBits3.DbuWarn != _prevDbuWarn)
                                {
                                    if (((LightBits3)myCurrentData.lightBits3 & LightBits3.DbuWarn)
                                        == LightBits3.DbuWarn)
                                    {
                                        Eyebrow_Right += 16;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }
                                    else
                                    {
                                        Eyebrow_Right -= 16;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }
                                    _prevDbuWarn = ((LightBits3)myCurrentData.lightBits3 & LightBits3.DbuWarn)
                                    == LightBits3.DbuWarn;
                                }
                                // ---------------
                                //  T_L_CFG FAULT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.T_L_CFG)
                                    == LightBits.T_L_CFG != _prevT_L_CFG)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.T_L_CFG)
                                        == LightBits.T_L_CFG)
                                    {
                                        Eyebrow_Right += 32;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }
                                    else
                                    {
                                        Eyebrow_Right -= 32;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }
                                    _prevT_L_CFG = ((LightBits)myCurrentData.lightBits & LightBits.T_L_CFG)
                                    == LightBits.T_L_CFG;
                                }
                                // ---------------
                                //   CAN FAULT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.CAN)
                                    == LightBits.CAN != _prevCAN)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.CAN)
                                        == LightBits.CAN)
                                    {
                                        Eyebrow_Right += 128;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }
                                    else
                                    {
                                        Eyebrow_Right -= 128;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }
                                    _prevCAN = ((LightBits)myCurrentData.lightBits & LightBits.CAN)
                                    == LightBits.CAN;
                                }
                                // ---------------
                                //  OXY BROW FAULT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.OXY_BROW)
                                    == LightBits.OXY_BROW != _prevOXY_BROW)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.OXY_BROW)
                                        == LightBits.OXY_BROW)
                                    {
                                        Eyebrow_Right += 64;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }
                                    else
                                    {
                                        Eyebrow_Right -= 64;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 7, Eyebrow_Right);
                                    }
                                    _prevOXY_BROW = ((LightBits)myCurrentData.lightBits & LightBits.OXY_BROW)
                                    == LightBits.OXY_BROW;
                                }

                                // -------------------------------------------------------------------------------- //
                                //                          CAUTION PANEL                                           //      
                                // -------------------------------------------------------------------------------- //
                                //int FltControlSys = (myCurrentData.lightBits & 0x40000);

                                //int Elec_Fault = (myCurrentData.lightBits3 & 0x400);
                                //int PROBEHEAT = (myCurrentData.lightBits2 & 0x1000000);
                                //int CONFIG = (myCurrentData.lightBits & 0x40);
                                //int cadc = (myCurrentData.lightBits3 & 0x400000);
                                //int ATFnotEngaged = (myCurrentData.lightBits3 & 0x10000000);
                                //int FwdFuelLow = (myCurrentData.lightBits2 & 0x40000);
                                //int AftFuelLow = (myCurrentData.lightBits2 & 0x80000);

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
                                        CautionPanel_Row1 += 1;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                        FltControlSys_active = true;
                                    }
                                    else
                                    {
                                        CautionPanel_Row1 -= 1;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                        FltControlSys_active = false;
                                    }
                                    _prevFltControlSys = ((LightBits)myCurrentData.lightBits & LightBits.FltControlSys)
                                    == LightBits.FltControlSys;
                                }
                                // ---------------
                                //  ELEC FAULT                     /* Maybe blinking in future, but not implemented in BMS 4.34 yet. 10.05.2019 LE. */
                                // ---------------
                                if (((LightBits3)myCurrentData.lightBits3 & LightBits3.Elec_Fault)
                                    == LightBits3.Elec_Fault != _prevElec_Fault)
                                {
                                    if (((LightBits3)myCurrentData.lightBits3 & LightBits3.Elec_Fault)
                                        == LightBits3.Elec_Fault)
                                    {
                                        CautionPanel_Row1 += 2;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                        Elec_Fault_active = true;
                                    }
                                    else
                                    {
                                        CautionPanel_Row1 -= 2;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                        Elec_Fault_active = false;
                                    }
                                    _prevElec_Fault = ((LightBits3)myCurrentData.lightBits3 & LightBits3.Elec_Fault)
                                    == LightBits3.Elec_Fault;
                                }
                                // ---------------
                                //  PROBE HEAT FAULT
                                // ---------------
                                // Not neccessary, because extra Thread "ProbeHeat_Blinking"!
                                //--------------------------------------------------------------
                                //if (((LightBits2)myCurrentData.lightBits2 & LightBits2.PROBEHEAT)
                                //       == LightBits2.PROBEHEAT != _prevProbeHeat)
                                //{
                                //    // Nur ausführen, wenn Probeheat nicht "blinkt":
                                //    if ((((BlinkBits)myCurrentData.blinkBits & BlinkBits.PROBEHEAT)
                                //        == BlinkBits.PROBEHEAT) == false)
                                //    {
                                //        if (((LightBits2)myCurrentData.lightBits2 & LightBits2.PROBEHEAT)
                                //            == LightBits2.PROBEHEAT)
                                //        {
                                //            CautionPanel_Row1 += 4;
                                //            Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                //            ProbeHeat_active = true;
                                //        }
                                //        else
                                //        {
                                //            CautionPanel_Row1 -= 4;
                                //            Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                //            ProbeHeat_active = false;
                                //        }
                                //    }
                                //    _prevProbeHeat = ((LightBits2)myCurrentData.lightBits2 & LightBits2.PROBEHEAT)
                                //    == LightBits2.PROBEHEAT;
                                //}
                                //if ((((LightBits2)myCurrentData.lightBits2 & LightBits2.PROBEHEAT)
                                //       == LightBits2.PROBEHEAT != _prevProbeHeat) && (((BlinkBits)myCurrentData.blinkBits & BlinkBits.PROBEHEAT)
                                //       != BlinkBits.PROBEHEAT))
                                //{
                                //    ProbeHeat_blinking = false;

                                //    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.PROBEHEAT)
                                //       == LightBits2.PROBEHEAT)
                                //    {
                                //        CautionPanel_Row1 += 4;
                                //        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                //        ProbeHeat_active = true;
                                //    }
                                //    else
                                //    {
                                //        CautionPanel_Row1 -= 4;
                                //        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                //        ProbeHeat_active = false;
                                //    }
                                //_prevProbeHeat = ((LightBits2)myCurrentData.lightBits2 & LightBits2.PROBEHEAT)
                                //== LightBits2.PROBEHEAT;
                                //}
                                //else
                                //{
                                //    ProbeHeat_blinking = true;
                                //}
                               
                            
                                // ---------------
                                //  C ADC FAULT
                                // ---------------
                                if (((LightBits3)myCurrentData.lightBits3 & LightBits3.cadc)
                                    == LightBits3.cadc != _prevcadc)
                                {
                                    if (((LightBits3)myCurrentData.lightBits3 & LightBits3.cadc)
                                    == LightBits3.cadc)
                                    {
                                        CautionPanel_Row1 += 8;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                        cadc_active = true;
                                    }
                                    else
                                    {
                                        CautionPanel_Row1 -= 8;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                        cadc_active = false;
                                    }
                                    _prevcadc = ((LightBits3)myCurrentData.lightBits3 & LightBits3.cadc) 
                                    == LightBits3.cadc;
                                }
                                // ---------------
                                //  STORES FAULT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.CONFIG)
                                    == LightBits.CONFIG != _prevCONFIG)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.CONFIG)
                                    == LightBits.CONFIG)
                                    {
                                        CautionPanel_Row1 += 16;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                        CONFIG_active = true;
                                    }
                                    else
                                    {
                                        CautionPanel_Row1 -= 16;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                        CONFIG_active = false;
                                    }
                                    _prevCONFIG = ((LightBits)myCurrentData.lightBits & LightBits.CONFIG)
                                    == LightBits.CONFIG;
                                }
                                // ---------------
                                //  ATF NOT ENGAGED
                                // ---------------
                                if (((LightBits3)myCurrentData.lightBits3 & LightBits3.ATF_Not_Engaged)
                                    == LightBits3.ATF_Not_Engaged != _prevATF_Not_Engaged)
                                {
                                    if (((LightBits3)myCurrentData.lightBits3 & LightBits3.ATF_Not_Engaged)
                                    == LightBits3.ATF_Not_Engaged)
                                    {
                                        CautionPanel_Row1 += 32;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                        ATF_Not_Engaged_active = true;
                                    }
                                    else
                                    {
                                        CautionPanel_Row1 -= 32;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                        ATF_Not_Engaged_active = false;
                                    }
                                    _prevATF_Not_Engaged = ((LightBits3)myCurrentData.lightBits3 & LightBits3.ATF_Not_Engaged)
                                    == LightBits3.ATF_Not_Engaged;
                                }
                                // ---------------
                                //  FWD FUEL LOW
                                // ---------------
                                 if (((LightBits2)myCurrentData.lightBits2 & LightBits2.FwdFuelLow)
                                     == LightBits2.FwdFuelLow != _prevFwdFuelLow)
                                 {
                                        if (((LightBits2)myCurrentData.lightBits2 & LightBits2.FwdFuelLow)
                                         == LightBits2.FwdFuelLow)
                                        {
                                        CautionPanel_Row1 += 64;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                        FwdFuelLow_active = true;
                                        }
                                        else
                                        {
                                        CautionPanel_Row1 -= 64;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                        FwdFuelLow_active = false;
                                        }
                                    _prevFwdFuelLow = ((LightBits2)myCurrentData.lightBits2 & LightBits2.FwdFuelLow) 
                                    == LightBits2.FwdFuelLow;
                                 }
                                // ---------------
                                //  AFT FUEL LOW
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.AftFuelLow)
                                     == LightBits2.AftFuelLow != _prevAftFuelLow)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.AftFuelLow)
                                    == LightBits2.AftFuelLow)
                                    {
                                        CautionPanel_Row1 += 128;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                        AftFuelLow_active = true;
                                    }
                                    else
                                    {
                                        CautionPanel_Row1 -= 128;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 3, CautionPanel_Row1);
                                        AftFuelLow_active = false;
                                    }
                                    _prevAftFuelLow = ((LightBits2)myCurrentData.lightBits2 & LightBits2.AftFuelLow)
                                    == LightBits2.AftFuelLow;
                                }
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
                                        CautionPanel_Row2 += 1;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, CautionPanel_Row2);
                                    }
                                    else
                                    {
                                        CautionPanel_Row2 -= 1;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, CautionPanel_Row2);
                                    }
                                    _prevEngineFault = ((LightBits)myCurrentData.lightBits & LightBits.EngineFault) 
                                    == LightBits.EngineFault;
                                }
                                // ---------------
                                //  SEC FAULT
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.SEC)
                                    == LightBits2.SEC != _prevSEC)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.SEC)
                                    == LightBits2.SEC)
                                    {
                                        CautionPanel_Row2 += 2;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, CautionPanel_Row2);
                                    }
                                    else
                                    {
                                        CautionPanel_Row2 -= 2;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, CautionPanel_Row2);
                                    }
                                    _prevSEC = ((LightBits2)myCurrentData.lightBits2 & LightBits2.SEC) 
                                    == LightBits2.SEC;
                                }
                                // ---------------
                                //  FUEL OIL HOT
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.FUEL_OIL_HOT)
                                    == LightBits2.FUEL_OIL_HOT != _prevFUEL_OIL_HOT)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.FUEL_OIL_HOT)
                                    == LightBits2.FUEL_OIL_HOT)
                                    {
                                        CautionPanel_Row2 += 4;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, CautionPanel_Row2);
                                    }
                                    else
                                    {
                                        CautionPanel_Row2 -= 4;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, CautionPanel_Row2);
                                    }
                                    _prevFUEL_OIL_HOT = ((LightBits2)myCurrentData.lightBits2 & LightBits2.FUEL_OIL_HOT)
                                    == LightBits2.FUEL_OIL_HOT;
                                }
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
                                        CautionPanel_Row2 += 16;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, CautionPanel_Row2);
                                    }
                                    else
                                    {
                                        CautionPanel_Row2 -= 16;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, CautionPanel_Row2);
                                    }
                                    _prevOverheat = ((LightBits)myCurrentData.lightBits & LightBits.Overheat) 
                                    == LightBits.Overheat;
                                }
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
                                        CautionPanel_Row2 += 64;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, CautionPanel_Row2);
                                    }
                                    else
                                    {
                                        CautionPanel_Row2 -= 64;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, CautionPanel_Row2);
                                    }
                                    _prevBUC = ((LightBits2)myCurrentData.lightBits2 & LightBits2.BUC) 
                                    == LightBits2.BUC;
                                }

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
                                        CautionPanel_Row3 += 1;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, CautionPanel_Row3);
                                    }
                                    else
                                    {
                                        CautionPanel_Row3 -= 1;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, CautionPanel_Row3);
                                    }
                                    _prevAvionics = ((LightBits)myCurrentData.lightBits & LightBits.Avionics)
                                    == LightBits.Avionics;
                                }
                                // ---------------
                                //  EQUIP HOT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.EQUIP_HOT)
                                    == LightBits.EQUIP_HOT != _prevEQUIP_HOT)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.EQUIP_HOT)
                                    == LightBits.EQUIP_HOT)
                                    {
                                        CautionPanel_Row3 += 2;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, CautionPanel_Row3);
                                    }
                                    else
                                    {
                                        CautionPanel_Row3 -= 2;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, CautionPanel_Row3);
                                    }
                                    _prevEQUIP_HOT = ((LightBits)myCurrentData.lightBits & LightBits.EQUIP_HOT)
                                    == LightBits.EQUIP_HOT;
                                }
                                // ---------------
                                //  RADAR ALT
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.RadarAlt)
                                    == LightBits.RadarAlt != _prevRadarAlt)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.RadarAlt)
                                    == LightBits.RadarAlt)
                                    {
                                        CautionPanel_Row3 += 4;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, CautionPanel_Row3);
                                    }
                                    else
                                    {
                                        CautionPanel_Row3 -= 4;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, CautionPanel_Row3);
                                    }
                                    _prevRadarAlt = ((LightBits)myCurrentData.lightBits & LightBits.RadarAlt)
                                    == LightBits.RadarAlt;
                                }
                                // ---------------
                                //  IFF
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.IFF)
                                    == LightBits.IFF != _prevIFF)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.IFF)
                                    == LightBits.IFF)
                                    {
                                        CautionPanel_Row3 += 8;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, CautionPanel_Row3);
                                    }
                                    else
                                    {
                                        CautionPanel_Row3 -= 8;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, CautionPanel_Row3);
                                    }
                                    _prevIFF = ((LightBits)myCurrentData.lightBits & LightBits.IFF)
                                    == LightBits.IFF;
                                }
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
                                        CautionPanel_Row4 += 1;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, CautionPanel_Row4);
                                    }
                                    else
                                    {
                                        CautionPanel_Row4 -= 1;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, CautionPanel_Row4);
                                    }
                                    _prevSEAT_ARM = ((LightBits2)myCurrentData.lightBits2 & LightBits2.SEAT_ARM)
                                    == LightBits2.SEAT_ARM;
                                }
                                // ---------------
                                //  NWS FAIL
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.NWSFail)
                                    == LightBits.NWSFail != _prevNWSFail)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.NWSFail)
                                    == LightBits.NWSFail)
                                    {
                                        CautionPanel_Row4 += 2;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, CautionPanel_Row4);
                                    }
                                    else
                                    {
                                        CautionPanel_Row4 -= 4;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, CautionPanel_Row4);
                                    }
                                    _prevNWSFail = ((LightBits)myCurrentData.lightBits & LightBits.NWSFail)
                                    == LightBits.NWSFail;
                                }
                                // ---------------
                                //  ANTI SKID
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.ANTI_SKID)
                                    == LightBits2.ANTI_SKID != _prevANTI_SKID)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.ANTI_SKID)
                                    == LightBits2.ANTI_SKID)
                                    {
                                        CautionPanel_Row4 += 4;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, CautionPanel_Row4);
                                    }
                                    else
                                    {
                                        CautionPanel_Row4 -= 4;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, CautionPanel_Row4);
                                    }
                                    _prevANTI_SKID = ((LightBits2)myCurrentData.lightBits2 & LightBits2.ANTI_SKID) 
                                    == LightBits2.ANTI_SKID;
                                }
                                // ---------------
                                //  HOOK
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.Hook)
                                    == LightBits.Hook != _prevHook)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.Hook)
                                    == LightBits.Hook)
                                    {
                                        CautionPanel_Row4 += 8;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, CautionPanel_Row4);
                                    }
                                    else
                                    {
                                        CautionPanel_Row4 -= 8;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, CautionPanel_Row4);
                                    }
                                    _prevHook = ((LightBits)myCurrentData.lightBits & LightBits.Hook)
                                    == LightBits.Hook;
                                }
                                // ---------------
                                //  OXY LOW
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.OXY_LOW)
                                    == LightBits2.OXY_LOW != _prevOXY_LOW)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.OXY_LOW)
                                    == LightBits2.OXY_LOW)
                                    {
                                        CautionPanel_Row4 += 16;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, CautionPanel_Row4);
                                    }
                                    else
                                    {
                                        CautionPanel_Row4 -= 16;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, CautionPanel_Row4);
                                    }
                                    _prevOXY_LOW = ((LightBits2)myCurrentData.lightBits2 & LightBits2.OXY_LOW)
                                    == LightBits2.OXY_LOW;
                                }
                                // ---------------
                                //  CABIN PRESS 
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.CabinPress)
                                    == LightBits.CabinPress != _prevCabinPress)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.CabinPress)
                                    == LightBits.CabinPress)
                                    {
                                        CautionPanel_Row4 += 32;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, CautionPanel_Row4);
                                    }
                                    else
                                    {
                                        CautionPanel_Row4 -= 32;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, CautionPanel_Row4);
                                    }
                                    _prevCabinPress = ((LightBits)myCurrentData.lightBits & LightBits.CabinPress)
                                    == LightBits.CabinPress;
                                }
                                // -------------------------------------------------------------------------------- //
                                //                          WARNING LIGHTS                                          //      
                                //                          LEFT  EYEBROW                                          //
                                // -------------------------------------------------------------------------------- //

                                // ---------------
                                //  MASTER CAUTION
                                // ---------------
                               if (((LightBits)myCurrentData.lightBits & LightBits.MasterCaution)
                                  == LightBits.MasterCaution != _prevMasterCaution)
                    
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.MasterCaution)
                                    == LightBits.MasterCaution)
                                    {
                                        Misc += 1;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 3, Misc);
                                    }
                                    else
                                    {
                                        Misc -= 1;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 3, Misc);
                                    }
                                    _prevMasterCaution = ((LightBits)myCurrentData.lightBits & LightBits.MasterCaution)
                                    == LightBits.MasterCaution;
                                }
                                // ---------------
                                //   TF-FAIL
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.TF)
                                    == LightBits.TF != _prevTF)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.TF)
                                    == LightBits.TF)
                                    {
                                        Misc += 64;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 3, Misc);
                                    }
                                    else
                                    {
                                        Misc -= 64;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 3, Misc);
                                    }
                                    _prevTF = ((LightBits)myCurrentData.lightBits & LightBits.TF)
                                    == LightBits.TF;
                                }

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
                                        CautionPanel_Row2 += 168;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, CautionPanel_Row2);

                                        //3rd ROW "NUCLEAR", "------", "-----", "-----"
                                        CautionPanel_Row3 += 240;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, CautionPanel_Row3);

                                        //4th ROW "-----", "-----"
                                        CautionPanel_Row4 += 192;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, CautionPanel_Row4);

                                        // LEFT WARNING LIGHTS "-------", "-------", "-------"
                                        Misc += 176;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 3, Misc);
                                    }
                                    else
                                    {
                                        // 2nd ROW "INLET ICING", "-------"
                                        CautionPanel_Row2 -= 168;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 4, CautionPanel_Row2);

                                        //3rd ROW "NUCLEAR", "------", "-----", "-----"
                                        CautionPanel_Row3 -= 240;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 5, CautionPanel_Row3);

                                        //4th ROW "-----", "-----"
                                        CautionPanel_Row4 -= 192;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x10, 6, CautionPanel_Row4);

                                        // LEFT WARNING LIGHTS "-------", "-------", "-------"
                                        Misc -= 176;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 3, Misc);
                                    }
                                    _prevAllLampBitsOn = ((LightBits)myCurrentData.lightBits & LightBits.AllLampBitsOn) 
                                    == LightBits.AllLampBitsOn;
                                }

                                // -------------------------------------------------------------------------------- //
                                //                         LEFT AUX CONSOLE                                         //      
                                //                          MISC PANEL                                              //
                                // -------------------------------------------------------------------------------- //
                                // ---------------
                                //  TFR_ENABLED
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.TFR_ENGAGED)
                                    == LightBits2.TFR_ENGAGED != _prevTFR_ENGAGED)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.TFR_ENGAGED)
                                    == LightBits2.TFR_ENGAGED)
                                    {
                                        Misc += 4;
                                    }
                                    else
                                    {
                                        Misc -= 4;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 3, Misc); ;
                                    }
                                    _prevTFR_ENGAGED = ((LightBits2)myCurrentData.lightBits2 & LightBits2.TFR_ENGAGED)
                                    == LightBits2.TFR_ENGAGED;
                                }
                                // ---------------
                                //  TFR_STANDBY
                                // ---------------
                                if (((LightBits)myCurrentData.lightBits & LightBits.TFR_STBY)
                                    == LightBits.TFR_STBY != _prevTFR_STBY)
                                {
                                    if (((LightBits)myCurrentData.lightBits & LightBits.TFR_STBY)
                                    == LightBits.TFR_STBY)
                                    {
                                        Misc += 2;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 3, Misc);
                                    }
                                    else
                                    {
                                        Misc -= 2;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 3, Misc); ;
                                    }
                                    _prevTFR_STBY = ((LightBits)myCurrentData.lightBits & LightBits.TFR_STBY)
                                    == LightBits.TFR_STBY;
                                }
                                // ---------------
                                //   ECM ENABLED
                                // ---------------
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.EcmPwr)
                                    == LightBits2.EcmPwr != _prevECM)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.EcmPwr)
                                    == LightBits2.EcmPwr)
                                    {
                                        Misc += 8;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 3, Misc);
                                    }
                                    else
                                    {
                                        Misc -= 8;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 3, Misc);
                                    }
                                    _prevECM = ((LightBits2)myCurrentData.lightBits2 & LightBits2.EcmPwr)
                                    == LightBits2.EcmPwr;
                                }
                                // -------------------------------------------------------------------------------- //
                                //                          CAUTION LIGHTS                                          //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                           
                                //int LEFlaps = (myCurrentData.lightBits & 0x80000);
                                //int EngineFault = (myCurrentData.lightBits & 0x100000);
                                //int Overheat = (myCurrentData.lightBits & 0x200000);
                                //int FuelLow = (myCurrentData.lightBits & 0x400000);
                                //int Avionics = (myCurrentData.lightBits & 0x800000);
                                //int RadarAlt = (myCurrentData.lightBits & 0x1000000);
                                //int IFF = (myCurrentData.lightBits & 0x2000000);
                                //int ECM = (myCurrentData.lightBits & 0x4000000);
                                //int Hook = (myCurrentData.lightBits & 0x8000000);
                                //int NWSFail = (myCurrentData.lightBits & 0x1000000);
                                //int CabinPress = (myCurrentData.lightBits & 0x2000000);
                                //int AutoPilotOn = (myCurrentData.lightBits & 0x4000000);
                                //int TFR_STBY = (myCurrentData.lightBits & 0x8000000);
                               

                                //int SEC = (myCurrentData.lightBits2 & 0x400000);
                                //int OXY_LOW = (myCurrentData.lightBits2 & 0x800000);
                                //int SEAT_ARM = (myCurrentData.lightBits2 & 0x2000000);
                                //int BUC = (myCurrentData.lightBits2 & 0x4000000);
                                //int FUEL_OIL_HOT = (myCurrentData.lightBits2 & 0x8000000);
                                //long ANTI_SKID = (myCurrentData.lightBits2 & 0x10000000);
                                //long TFR_ENGAGED = (myCurrentData.lightBits2 & 0x20000000);
                                //long GEARHANDLE = (myCurrentData.lightBits2 & 0x40000000);


                                
                                //int Lef_Fault = (myCurrentData.lightBits3 & 0x800);
                                //int OnGround = (myCurrentData.lightBits3 & 0x1000);
                                //int FlcsBitRun = (myCurrentData.lightBits3 & 0x2000);
                                //int FlcsBitFail = (myCurrentData.lightBits3 & 0x4000);
                                //int NoseGearDown = (myCurrentData.lightBits3 & 0x10000);
                                //int LeftGearDown = (myCurrentData.lightBits3 & 0x20000);
                                //int RightGearDown = (myCurrentData.lightBits3 & 0x40000);
                                //int ParkBrakeOn = (myCurrentData.lightBits3 & 0x100000);
                                //int Power_Off = (myCurrentData.lightBits3 & 0x200000);
                              

                                //int EPUOn = (myCurrentData.lightBits2 & 0x100000);
                                //int JFSOn = (myCurrentData.lightBits2 & 0x200000);





                                // -------------------------------------------------------------------------------- //
                                //                          AOA INDEXER                                             //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                //int AOA_Above = (myCurrentData.lightBits & 0x1000);
                                //int AOA_OnPath = (myCurrentData.lightBits & 0x2000);
                                //int AOA_Below = (myCurrentData.lightBits & 0x4000);

                                //int RefuelRDY = (myCurrentData.lightBits & 0x8000);
                                //int RefuelAR = (myCurrentData.lightBits & 0x10000);
                                //int RefuelDSC = (myCurrentData.lightBits & 0x20000);


                                // -------------------------------------------------------------------------------- //
                                // LightBits2              THREAT WARNING PRIME                                     //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                              


                                // -------------------------------------------------------------------------------- //
                                //                         AUX THREAT WARNING                                       //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                //int AuxSrch = (myCurrentData.lightBits2 & 0x1000);
                                //int AuxAct = (myCurrentData.lightBits2 & 0x2000);
                                //int AuxLow = (myCurrentData.lightBits2 & 0x4000);
                                //int AuxPwr = (myCurrentData.lightBits2 & 0x8000);

                                // -------------------------------------------------------------------------------- //
                                //                         ECM                                                      //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                //int EcmPwr = (myCurrentData.lightBits2 & 0x10000);
                                //int EcmFail = (myCurrentData.lightBits2 & 0x20000);

                                // -------------------------------------------------------------------------------- //
                                // LightBits3              ELEC PANEL                                               //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                //int FlcsPmg = (myCurrentData.lightBits3 & 0x1);
                                //int MainGen = (myCurrentData.lightBits3 & 0x2);
                                //int StbyGen = (myCurrentData.lightBits3 & 0x4);
                                //int EpuGen = (myCurrentData.lightBits3 & 0x8);
                                //int EpuPmg = (myCurrentData.lightBits3 & 0x10);
                                //int ToFlcs = (myCurrentData.lightBits3 & 0x20);
                                //int FlcsRly = (myCurrentData.lightBits3 & 0x40);
                                //int BatFail = (myCurrentData.lightBits3 & 0x80);


                                // -------------------------------------------------------------------------------- //
                                //                         EPU PANEL                                                //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                //int Hydrazine = (myCurrentData.lightBits3 & 0x100);
                                //int Air = (myCurrentData.lightBits3 & 0x200);


                                // -------------------------------------------------------------------------------- //
                                //                         LEFT AUX CONSOLE                                         //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                int SpeedBrake = (myCurrentData.lightBits3 & 0x800000);

                                if (_prevSpeedBrake != SpeedBrake)
                                {
                                    if (SpeedBrake == OFF)
                                    {
                                        // Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 0);
                                        _prevSpeedBrake = SpeedBrake;
                                     //   Thread.Sleep(5);
                                    }

                                    if (SpeedBrake == ON)
                                    {
                                        //  Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 1);
                                        _prevSpeedBrake = SpeedBrake;
                                     //   Thread.Sleep(5);
                                    }
                                }

                                // ----------------------------------------------------------------------------------//
                                //                           TWP - Threat Warning Prime                              //
                                // ----------------------------------------------------------------------------------//
                                // Define short-definition of lightbits:
                               // int HandOff = (myCurrentData.lightBits2 & 0x1);
                               // int Launch = (myCurrentData.lightBits2 & 0x2);
                               // int PriMode = (myCurrentData.lightBits2 & 0x4);
                               // int Naval = (myCurrentData.lightBits2 & 0x8);
                               // int Unk = (myCurrentData.lightBits2 & 0x10);
                               // int TgtSep = (myCurrentData.lightBits2 & 0x20);
                               // int SysTest = (myCurrentData.lightBits3 & 0x1000000);

                                // ---------------
                                // HANDOFF, PRIORITY MODE & SYS TEST LOWER INDICATOR
                                // ---------------
                                // Lower indicator,
                                // just goes on when system is powerd:
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.AuxPwr)
                                   == LightBits2.AuxPwr != _prevAuxPwr)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.AuxPwr)
                                    == LightBits2.AuxPwr)
                                    {
                                        TWP += 64; // HANDOFF "ON"
                                        TWP += 16; // PRIORITY "OPEN"
                                        TWP += 8;  // TGT SEP, lower Half "ON"
                                        TWP += 1;  // SysTest, lower Half "SysTest" ON
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 7, TWP);

                                        TWP_Shp += 1; // UNKNOWN, lower Half "ON" (Naval)
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 6, TWP_Shp);
                                    }
                                    else
                                    {
                                        TWP -= 64; // HANDOFF "ON"
                                        TWP -= 16; // PRIORITY "OPEN"
                                        TWP -= 8;  // TGT SEP, lower Half "ON"
                                        TWP -= 1;  // SysTest, lower Half "SysTest" ON
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 7, TWP);

                                        TWP_Shp -= 1; // UNKNOWN, lower Half "ON" (Naval)
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 6, TWP_Shp);

                                    }
                                    _prevAuxPwr = ((LightBits2)myCurrentData.lightBits2 & LightBits2.AuxPwr)
                                    == LightBits2.AuxPwr;
                                }
                                // ---------------
                                // HANDOFF
                                // ---------------
                                // Upper indicator:
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.HandOff)
                                   == LightBits2.HandOff != _prevHandOff)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.HandOff)
                                    == LightBits2.HandOff)
                                    {
                                        TWP += 128;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 7, TWP);
                                        HandOff_active = true;
                                    }
                                    else
                                    {
                                        TWP -= 128;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 7, TWP);
                                        HandOff_active = false;
                                    }
                                    _prevHandOff = ((LightBits2)myCurrentData.lightBits2 & LightBits2.HandOff)
                                    == LightBits2.HandOff;
                                }
                                // ---------------
                                // PRIORITY MODE
                                // ---------------
                                // Upper indicator, PRIORITY:
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.PriMode)
                                      == LightBits2.PriMode != _prevPriMode)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.PriMode)
                                    == LightBits2.PriMode)
                                    {
                                        TWP -= 16; // Unteren Indicator "OPEN" ausschalten
                                        TWP += 32; // Oberen Indicator "PRIORITY" einschalten
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 7, TWP);
                                        PriMode_active = true;
                                    }
                                    else
                                    {
                                        TWP -= 32; // Oberen Indicator "PRIORITY" ausschalten
                                        TWP += 16; // Unteren Indicator "OPEN" einschalten
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 7, TWP);
                                        PriMode_active = false;
                                    }
                                    _prevPriMode = ((LightBits2)myCurrentData.lightBits2 & LightBits2.PriMode)
                                    == LightBits2.PriMode;
                                }
                                // ---------------
                                //  TGT SEP
                                // ---------------
                                // TGP SEP - upper indicator "TGT SEP", 
                                // goes on when switch is pushed:
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.TgtSep)
                                       == LightBits2.TgtSep != _prevTgtSep)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.TgtSep)
                                    == LightBits2.TgtSep)
                                    {
                                        TWP += 4;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 7, TWP);
                                        TgtSep_active = true;
                                    }
                                    else
                                    {
                                        TWP -= 4;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 7, TWP);
                                        TgtSep_active = false;
                                    }
                                    _prevTgtSep = ((LightBits2)myCurrentData.lightBits2 & LightBits2.TgtSep)
                                    == LightBits2.TgtSep;
                                }
                                // ---------------
                                //  SYS TEST - PERFORMING TEST
                                // ---------------
                                // SysTest - upper indicator "ON", 
                                // goes on when system performing a test:
                                if (((LightBits3)myCurrentData.lightBits3 & LightBits3.SysTest)
                                        == LightBits3.SysTest != _prevSysTest)
                                {
                                    if (((LightBits3)myCurrentData.lightBits3 & LightBits3.SysTest)
                                    == LightBits3.SysTest)
                                    {
                                        TWP += 2;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 7, TWP);
                                        SysTest_active = true;
                                    }
                                    else
                                    {
                                        TWP -= 2;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 7, TWP);
                                        SysTest_active = false;
                                    }
                                _prevSysTest = ((LightBits3)myCurrentData.lightBits3 & LightBits3.SysTest)
                                == LightBits3.SysTest;
                            }
                                // ---------------
                                //  UNKNOWN / SHIP 
                                // ---------------
                                // UNKNOWN / SHP - upper indicator "UNKNOWN", 
                                if (((LightBits2)myCurrentData.lightBits2 & LightBits2.Unk)
                                         == LightBits2.Unk != _prevUnk)
                                {
                                    if (((LightBits2)myCurrentData.lightBits2 & LightBits2.Unk)
                                    == LightBits2.Unk)
                                    {
                                        TWP_Shp += 2;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 6, TWP_Shp);
                                        Unk_active = true;
                                    }
                                    else
                                    {
                                        TWP_Shp -= 2;
                                        Port3_PHCC_Input_Output.DoaSend40DO(0x11, 6, TWP_Shp);
                                        Unk_active = false;
                                    }
                                    _prevUnk = ((LightBits2)myCurrentData.lightBits2 & LightBits2.Unk)
                                    == LightBits2.Unk;
                                }
                                // -------------------------------------------------------------------------------- //
                                //                         HSI BITS                                                 //
                                //     -> Not all are necessary because using YAME suite!!! <-                      //
                                // --------------------------------------------------------------------- works! --- //
                                // Define short-definition of lightbits:
                                int OuterMarker = (myCurrentData.hsiBits & 0x4000);
                                int MiddleMarker = (myCurrentData.hsiBits & 0x8000);
                                int Flying = (myCurrentData.hsiBits & 0x10000);

                                if (_prevOuterMarker != OuterMarker)
                                {
                                    if (OuterMarker == OFF)
                                    {
                                        // Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 0);
                                        _prevOuterMarker = OuterMarker;
                                     //   Thread.Sleep(5);
                                    }

                                    if (OuterMarker == ON)
                                    {
                                        //  Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 1);
                                        _prevOuterMarker = OuterMarker;
                                       // Thread.Sleep(5);
                                    }
                                }

                                if (_prevMiddleMarker != MiddleMarker)
                                {
                                    if (MiddleMarker == OFF)
                                    {
                                        // Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 0);
                                        _prevMiddleMarker = MiddleMarker;
                                     //   Thread.Sleep(5);
                                    }
                                    if (MiddleMarker == ON)
                                    {
                                        //  Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 1);
                                        _prevMiddleMarker = MiddleMarker;
                                      //  Thread.Sleep(5);
                                    }
                                }
                                if (_prevFlying != Flying)
                                {
                                    if (Flying == OFF)
                                    {
                                        // Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 0);
                                        _prevFlying = Flying;
                                      //  Thread.Sleep(5);
                                    }
                                    if (Flying == ON)
                                    {
                                        //  Port3_PHCC_Input_Output.DoaSendRaw(0x30, 0x0, 1);
                                        _prevFlying = Flying;
                                      //  Thread.Sleep(5);
                                    }
                            }
                                }          Thread.Sleep(50);
                    Application.DoEvents();
                                            }
                   // }
              //  }
            }
        }

        private void Switches()
        {
            using (Reader myReader = new F4SharedMem.Reader())  //Neue Instanz von Reader, um die Daten von FalconBMS auslesen zu kÃƒÂ¶nnen.
            {
                //while (_keepRunning == true)
                //{
                //    if (myReader.IsFalconRunning == true)
                //    {
                        while ((myReader.IsFalconRunning) && (_keepRunning))
                        {
                            FlightData myCurrentSwitchData = myReader.GetCurrentData();

                            if ((myCurrentSwitchData.hsiBits & 0x80000000) != 0)
                            {
                                F4Callbacks F4 = new F4Callbacks();
                                Switch = Port3_PHCC_Input_Output.DigitalInputs;
                                // Key2 @ Key64-board, Slot 1.
                                //        EjectHandlePulled = Port3_PHCC_Input_Output.DigitalInputs.GetValue(1).ToString(); // Zahlenwert ÃƒÂ¤ndern fÃƒÂ¼r Schalteranzahl! 0 --> Schalter 1. 64 ist Schalter 65.

                                //Switch[1] = SimSeatOn;
                                // Thread.Sleep(5);  // 25 möglich????

                                //if (_prevSwitchState[0] != Switch[0])
                                //{
                                //    if (Switch[0] == ON)
                                //    {
                                //        CW = true;
                                //        CCW = false;
                                //         F4.AltPressureIncrease1();

                                //        if ((Switch[0] == ON) && (Switch[1] == OFF))
                                //        {
                                //            CW = true;
                                //            CCW = false;
                                //            F4.AltPressureIncrease1();

                                //            if ((Switch[0] == ON) && (Switch[1] == ON))
                                //            {
                                //                CW = true;
                                //                CCW = false;
                                //                F4.AltPressureIncrease1();
                                //            }

                                //        }

                                //        //if (CW == true)
                                //        //    {
                                //        //        F4.AltPressureIncrease1();
                                //        //        CW = false;


                                //    }
                                //        _prevSwitchState[0] = Switch[0];
                                //        _prevSwitchState[1] = Switch[1];                            
                                //}

                                //if (_prevSwitchState[1] != Switch[1])
                                //{
                                //    if (Switch[1] == ON)
                                //    {
                                //        CCW = true;
                                //        CW = false;
                                //         F4.AltPressureDecrease1();

                                //        if ((Switch[1] == ON) && (Switch[0] == OFF))
                                //        {
                                //            CCW = true;
                                //            CW = false;
                                //            F4.AltPressureDecrease1();

                                //            if ((Switch[1] == ON) && (Switch[0] == ON))
                                //            {
                                //                CCW = true;
                                //                CW = false;
                                //                F4.AltPressureDecrease1();
                                //            }


                                //        }

                                //        //if (CCW == true)
                                //        //    {
                                //        //        F4.AltPressureDecrease1();
                                //        //        CCW = false;
                                //        //        }
                                //    }
                                //        _prevSwitchState[1] = Switch[1];
                                //        _prevSwitchState[0] = Switch[0];
                                //}
                                if (_prevSwitchState[0] != Switch[0])
                                {

                                    if (Switch[0] == _ON)
                                    {
                                        F4.SimSeatOn();
                                        _prevSwitchState[0] = Switch[0];

                                    }
                                    if (Switch[0] == _OFF)
                                    {
                                        F4.SimSeatOff();
                                        _prevSwitchState[1] = Switch[1];

                                        SendCallback("SimSeatOff");  //<---- WORKS! 26.05.2019 LE

                                    }
                                }
                        //Thread.Sleep(250); // Testweise eingefügt. 04.03.2020 LE.
                        //SwitchesThread.Join();

                        Thread.Sleep(50);
                        Application.DoEvents();
                            }
                        }

                    }
           
            //    }
            //}
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
                          //  Application.Restart();
                          //  Environment.Exit(0);
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
            while ((DateTime.Now - start).TotalMilliseconds < ms) {   Application.DoEvents(); }
              
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

      
        public void SendCallback(string callback)
        {
          //  _viewerState.KeyFile.SendCallbackByName(callback);
        }

        private void button4_Click(object sender, EventArgs e)
        {//WORKS! 26.05.2019 Le

            //   _viewerState.KeyFile.SendCallbackByName("SimTogglePaused");
           
            SendCallback("SimTogglePaused");
            //SendCallback(Callbacks.SimTogglePaused.ToString());

           // string binding = Convert.ToString(_viewerState.KeyFile.GetBindingForCallback("SimTogglePaused"));
                
           //     label5.Invoke(new Action<string>(s => { label5.Text = s; label5.ForeColor = Color.Red; }),binding);

        }


       
    



        private void button5_Click(object sender, EventArgs e)
        {
            Port3_PHCC_Input_Output.Reset();
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

