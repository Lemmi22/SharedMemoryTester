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
using System.Collections.ObjectModel;
using System.Reflection;
using Phcc;
using System.Speech;
using System.Speech.Synthesis;

// Keycodes from:     BMS - Pitbuilder.key

namespace WindowsFormsApplication1
{
    /* public class Value7seg {

         public byte SEGA = 0x80;
         public byte SEGB = 0x40;
         public byte SEGC = 0x20;
         public byte SEGD = 0x08;
         public byte SEGE = 0x04;
         public byte SEGF = 0x02;
         public byte SEGG = 0x01;
         public byte SEGDP = 0x10;
         public char Charto7seg(char test)
         {// Variablen for 7-Segments:
          // SEGA 0x80
          // SEGB 0x40
          // SEGC 0x20
          // SEGD 0x08
          // SEGE 0x04
          // SEGF 0x02
          // SEGG 0x01
          // SEGDP 0x10
             switch (test)
             {
                 case '0': return (SEGA += SEGB += SEGC += SEGD += SEGE += SEGF);
                 case "1": return (SEGB += SEGC);
                 case "2": return (SEGA += SEGB += SEGG += SEGE += SEGD);
                 case "3": return (SEGA += SEGB += SEGC += SEGD += SEGG);
                 case "4": return (SEGF += SEGG += SEGB += SEGC);
                 case "5": return (SEGA += SEGF += SEGG += SEGC += SEGD);
                 case "6": return (SEGF += SEGE += SEGD += SEGC += SEGG);
                 case "7": return (SEGA += SEGB += SEGC);
                 case "8": return (SEGA += SEGB += SEGC += SEGD += SEGE += SEGF += SEGG);
                 case "9": return (SEGA += SEGB += SEGC += SEGF += SEGG);
                     /* 

                     case '.': return (SEGDP);
                     case '-': return (SEGG);
                     case '+': return (SEGG|SEGF|SEGE);
                     case 'A':
                     case 'a': return (SEGA|SEGB|SEGC|SEGE|SEGF|SEGG);
                     case 'C':
                     case 'c': return (SEGA|SEGD|SEGE|SEGF);
                     case 'E':
                     case 'e': return (SEGA|SEGD|SEGE|SEGF|SEGG);
                     case 'F':
                     case 'f': return (SEGA|SEGE|SEGF|SEGG);
                     case 'G':
                     case 'g': return (SEGA|SEGC|SEGD|SEGE|SEGF);
                     case 'H':
                     case 'h': return (SEGB|SEGC|SEGE|SEGF|SEGG);
                     case 'J':
                     case 'j': return (SEGB|SEGC|SEGD|SEGE);
                     case 'L':
                     case 'l': return (SEGD|SEGE|SEGF);
                     case 'N':
                     case 'n': return (SEGC|SEGE|SEGG);
                     case 'O':
                     case 'o': return (SEGC|SEGD|SEGE|SEGG);
                     case 'P':
                     case 'p': return (SEGA|SEGB|SEGE|SEGF|SEGG);
                     case 'R':
                     case 'r': return (SEGE|SEGG);
                     case 'T':
                     case 't': return (SEGD|SEGE|SEGF|SEGG);
                     case 'U':
                     case 'u': return (SEGB|SEGC|SEGD|SEGE|SEGF);
                     case 'Y':
                     case 'y': return (SEGB|SEGE|SEGF|SEGG);
                     case ' ':
                     */


    //             --a--
    //            |     |
    //            f     b
    //            |     |
    //             --g--
    //            |     |
    //            e     c
    //            |     |
    //             --d--


    public class F4Callbacks
    {
        //public enum Callbacks
        //{
        //    SimTogglePaused
        //}
        public enum ScanCodes : int
        {
            NotAssigned = -1,
            Escape = 0x01,
            One = 0x02,
            Two = 0x03,
            Three = 0x04,
            Four = 0x05,
            Five = 0x06,
            Six = 0x07,
            Seven = 0x08,
            Eight = 0x09,
            Nine = 0x0A,
            Zero = 0x0B,
            Minus = 0x0C,
            Equals = 0x0D,
            Backspace = 0x0E,
            Tab = 0x0F,
            Q = 0x10,
            W = 0x11,
            E = 0x12,
            R = 0x13,
            T = 0x14,
            Y = 0x15,
            U = 0x16,
            I = 0x17,
            O = 0x18,
            P = 0x19,
            LBracket = 0x1A,
            RBracket = 0x1B,
            Return = 0x1C,
            LControl = 0x1D,
            A = 0x1E,
            S = 0x1F,
            D = 0x20,
            F = 0x21,
            G = 0x22,
            H = 0x23,
            J = 0x24,
            K = 0x25,
            L = 0x26,
            Semicolon = 0x27,
            Apostrophe = 0x28,
            Grave = 0x29,
            LShift = 0x2A,
            Backslash = 0x2B,
            Z = 0x2C,
            X = 0x2D,
            C = 0x2E,
            V = 0x2F,
            B = 0x30,
            N = 0x31,
            M = 0x32,
            Comma = 0x33,
            Period = 0x34,
            Slash = 0x35,
            RShift = 0x36,
            Multiply = 0x37,
            LMenu = 0x38,
            Space = 0x39,
            CapsLock = 0x3A,
            F1 = 0x3B,
            F2 = 0x3C,
            F3 = 0x3D,
            F4 = 0x3E,
            F5 = 0x3F,
            F6 = 0x40,
            F7 = 0x41,
            F8 = 0x42,
            F9 = 0x43,
            F10 = 0x44,
            NumLock = 0x45,
            ScrollLock = 0x46,
            NumPad7 = 0x47,
            NumPad8 = 0x48,
            NumPad9 = 0x49,
            Subtract = 0x4A,
            NumPad4 = 0x4B,
            NumPad5 = 0x4C,
            NumPad6 = 0x4D,
            Add = 0x4E,
            NumPad1 = 0x4F,
            NumPad2 = 0x50,
            NumPad3 = 0x51,
            NumPad0 = 0x52,
            Decimal = 0x53,
            F11 = 0x57,
            F12 = 0x58,
            F13 = 0x64,
            F14 = 0x65,
            F15 = 0x66,
            Kana = 0x70,
            Convert = 0x79,
            NoConvert = 0x7B,
            Yen = 0x7D,
            NumPadEquals = 0x8D,
            Circumflex = 0x90,
            At = 0x91,
            Colon = 0x92,
            Underline = 0x93,
            Kanji = 0x94,
            Stop = 0x95,
            Ax = 0x96,
            Unlabeled = 0x97,
            NumPadEnter = 0x9C,
            RControl = 0x9D,
            NumPadComma = 0xB3,
            Divide = 0xB5,
            SysRq = 0xB7,
            RMenu = 0xB8,
            Home = 0xC7,
            Up = 0xC8,
            Prior = 0xC9,
            Left = 0xCB,
            Right = 0xCD,
            End = 0xCF,
            Down = 0xD0,
            Next = 0xD1,
            Insert = 0xD2,
            Delete = 0xD3,
            LWin = 0xDB,
            RWin = 0xDC,
            Apps = 0xDD
        }

        public class KeyAndMouseUtils
        {
            internal enum INPUT_TYPE : uint
            {
                INPUT_MOUSE = 0,
                INPUT_KEYBOARD = 1,
                INPUT_HARDWARE = 2,
            }

            [Flags()]
            internal enum KEYEVENTF : uint
            {
                EXTENDEDKEY = 0x0001,
                KEYUP = 0x0002,
                UNICODE = 0x0004,
                SCANCODE = 0x0008,
            }
            internal enum MAPVK_MAPTYPES : uint
            {
                MAPVK_VK_TO_VSC = 0x0,
                MAPVK_VSC_TO_VK = 0x1,
                MAPVK_VK_TO_CHAR = 0x2,
                MAPVK_VSC_TO_VK_EX = 0x3,
                MAPVK_VK_TO_VSC_EX = 0x4,
            }
            [Flags()]
            internal enum MOUSEEVENTF : uint
            {
                MOVE = 0x0001,  // mouse move
                LEFTDOWN = 0x0002,  // left button down
                LEFTUP = 0x0004,  // left button up
                RIGHTDOWN = 0x0008,  // right button down
                RIGHTUP = 0x0010,  // right button up
                MIDDLEDOWN = 0x0020,  // middle button down
                MIDDLEUP = 0x0040,  // middle button up
                XDOWN = 0x0080,  // x button down
                XUP = 0x0100,  // x button down
                WHEEL = 0x0800,  // wheel button rolled
                VIRTUALDESK = 0x4000,  // map to entire virtual desktop
                ABSOLUTE = 0x8000,  // absolute move
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct MOUSEINPUT
            {
                public int dx;
                public int dy;
                public uint mouseData;
                public MOUSEEVENTF dwFlags;
                public uint time;
                public UIntPtr dwExtraInfo;
            }
            [StructLayout(LayoutKind.Sequential)]
            internal struct HARDWAREINPUT
            {
                public uint uMsg;
                public ushort wParamL;
                public ushort wParamH;
            }
            [StructLayout(LayoutKind.Sequential)]
            internal struct KEYBDINPUT
            {
                public ushort wVk;
                public ushort wScan;
                public KEYEVENTF dwFlags;
                public uint time;
                public IntPtr dwExtraInfo;
            }
            [StructLayout(LayoutKind.Explicit)]
            internal struct INPUT
            {
                [FieldOffset(0)]
                public INPUT_TYPE type;
                [FieldOffset(4)]
                public MOUSEINPUT mi;
                [FieldOffset(4)]
                public KEYBDINPUT ki;
                [FieldOffset(4)]
                public HARDWAREINPUT hi;
            }

            [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "MapVirtualKey", SetLastError = false)]
            internal static extern uint MapVirtualKey(uint uCode, MAPVK_MAPTYPES uMapType);

            [DllImport("user32.dll")]
            internal static extern IntPtr GetMessageExtraInfo();

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

            // -----------------------------------------------------------------------------------//
            //       Nur erforderlich, wenn als Anwendung auf einem Single-PC konzipiert!         // 
            // -----------------------------------------------------------------------------------//
            // [DllImportAttribute("User32.dll", SetLastError = true)]                            //
            // internal static extern IntPtr FindWindow(String ClassName, String WindowName);     //
            //                                                                                    //
            // [DllImportAttribute("User32.dll", SetLastError = true)]                            //
            //  internal static extern bool SetForegroundWindow(IntPtr hWnd);                     //
            // -----------------------------------------------------------------------------------//

            internal static void SendMouseInput(MOUSEEVENTF dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo)
            {
                INPUT input = new INPUT();
                input.mi = new MOUSEINPUT();
                input.type = INPUT_TYPE.INPUT_MOUSE;
                input.mi.dwFlags = dwFlags;
                input.mi.dx = (int)dx;
                input.mi.dy = (int)dy;
                input.mi.mouseData = dwData;
                input.mi.time = 0;
                input.mi.dwExtraInfo = dwExtraInfo;
                SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
            }

            internal static void SendKeyInput(ushort scanCode, bool press, bool release)
            {
                if (!press && !release)
                {
                    return;
                }

                int numInputs = 0;
                if (press && release)
                {
                    numInputs = 2;
                }
                else
                {
                    numInputs = 1;
                }
                INPUT[] inputs = new INPUT[numInputs];
                int curInput = 0;
                if (press)
                {
                    INPUT input = new INPUT();
                    input.ki = new KEYBDINPUT();
                    input.ki.wScan = scanCode;
                    input.ki.time = 0;
                    input.ki.dwFlags = KEYEVENTF.SCANCODE;
                    if ((scanCode & 0x80) > 0)
                    {
                        input.ki.dwFlags |= KEYEVENTF.EXTENDEDKEY;
                    }
                    System.Diagnostics.Debug.WriteLine(input.ki.wScan);
                    input.ki.dwExtraInfo = GetMessageExtraInfo();
                    input.type = INPUT_TYPE.INPUT_KEYBOARD;
                    inputs[curInput] = input;
                    curInput++;
                }
                if (release)
                {
                    INPUT input = new INPUT();
                    input.ki = new KEYBDINPUT();
                    input.ki.wScan = scanCode;
                    input.ki.time = 0;
                    input.ki.dwFlags = (KEYEVENTF.KEYUP | KEYEVENTF.SCANCODE);
                    if ((scanCode & 0x80) > 0)
                    {
                        input.ki.dwFlags |= KEYEVENTF.EXTENDEDKEY;
                    }
                    input.ki.dwExtraInfo = GetMessageExtraInfo();
                    input.type = INPUT_TYPE.INPUT_KEYBOARD;
                    inputs[curInput] = input;
                }
                SendInput((uint)numInputs, inputs, Marshal.SizeOf(typeof(INPUT)));

            }

            public static void SendKeyInputToFalcon(ushort scanCode, bool press, bool release)
            {
                // -----------------------------------------------------------------------------------//
                //       Nur erforderlich, wenn als Anwendung auf einem Single-PC konzipiert!         // 
                // -----------------------------------------------------------------------------------//
                //   IntPtr falconWindow = FindWindow("FalconDisplay", null);                         //
                //  SetForegroundWindow(falconWindow);                                               //
                // -----------------------------------------------------------------------------------//

                SendKeyInput(scanCode, press, release);
            }

            /// <summary>
            /// Moves the mouse to the given relative (x,y) coordinates.
            /// </summary>
            public static void MouseMoveRelative(int x, int y)
            {
                int cur_x = Cursor.Position.X;
                int cur_y = Cursor.Position.Y;

                int new_x = cur_x + x;
                int new_y = cur_y + y;
                MouseMoveAbsolute(new_x, new_y);
            }

            /// <summary>
            /// Moves the mouse to the given absolute (x,y) coordinates.
            /// </summary>
            public static void MouseMoveAbsolute(int x, int y)
            {
                x = x * 65535 / Screen.PrimaryScreen.Bounds.Width;
                y = y * 65535 / Screen.PrimaryScreen.Bounds.Height;
                SendMouseInput((MOUSEEVENTF.ABSOLUTE | MOUSEEVENTF.MOVE), (uint)x, (uint)y, (uint)0, UIntPtr.Zero);
            }

            /// <summary>
            /// Moves the mouse wheel by given amount.
            /// </summary>
            public static void MouseWheelMove(int amount)
            {
                SendMouseInput(MOUSEEVENTF.WHEEL, (uint)0, (uint)0, (uint)amount, UIntPtr.Zero);
            }


            /// <summary>
            ///´Sends a left mouse button up event at the current cursor position.
            /// </summary>
            public static void LeftUp()
            {
                SendMouseInput(MOUSEEVENTF.LEFTUP, 0, 0, 0, UIntPtr.Zero);
            }

            /// <summary>
            /// Sends a right mouse button up event at the current cursor position.
            /// </summary>
            public static void RightUp()
            {
                SendMouseInput(MOUSEEVENTF.RIGHTUP, 0, 0, 0, UIntPtr.Zero);
            }


            /// <summary>
            /// Sends a middle mouse button up event at the current cursor position.
            /// </summary>
            public static void MiddleUp()
            {
                SendMouseInput(MOUSEEVENTF.MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
            }


            /// <summary>
            /// Sends a middle mouse button down event at the current cursor position.
            /// </summary>
            public static void MiddleDown()
            {
                SendMouseInput(MOUSEEVENTF.MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
            }

            /// <summary>
            /// Sends a right mouse button down event at the current cursor position.
            /// </summary>
            public static void RightDown()
            {
                SendMouseInput(MOUSEEVENTF.RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
            }

            /// <summary>
            /// Sends a left mouse button down event at the current cursor position.
            /// </summary>
            public static void LeftDown()
            {
                SendMouseInput(MOUSEEVENTF.LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            }


            /// <summary>
            /// Sends a middle mouse button double click at the current cursor position.
            /// </summary>
            public static void MiddleDoubleClick()
            {
                SendMouseInput(MOUSEEVENTF.MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
                SendMouseInput(MOUSEEVENTF.MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
                SendMouseInput(MOUSEEVENTF.MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
                SendMouseInput(MOUSEEVENTF.MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
            }


            /// <summary>
            /// Sends a right mouse button double click at the current cursor position.
            /// </summary>
            public static void RightDoubleClick()
            {
                SendMouseInput(MOUSEEVENTF.RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
                SendMouseInput(MOUSEEVENTF.RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
                SendMouseInput(MOUSEEVENTF.RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
                SendMouseInput(MOUSEEVENTF.RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
            }


            /// <summary>
            /// Sends a left mouse button double click at the current cursor position.
            /// </summary>
            public static void LeftDoubleClick()
            {
                SendMouseInput(MOUSEEVENTF.LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
                SendMouseInput(MOUSEEVENTF.LEFTUP, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
                SendMouseInput(MOUSEEVENTF.LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
                SendMouseInput(MOUSEEVENTF.LEFTUP, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
            }


            /// <summary>
            /// Sends a right mouse button click at the current cursor position.
            /// </summary>
            public static void RightClick()
            {
                SendMouseInput(MOUSEEVENTF.RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
                SendMouseInput(MOUSEEVENTF.RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
            }


            /// <summary>
            /// Sends a left mouse button click at the current cursor position.
            /// </summary>
            public static void LeftClick()
            {
                SendMouseInput(MOUSEEVENTF.LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
                SendMouseInput(MOUSEEVENTF.LEFTUP, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
            }

            /// <summary>
            /// Sends a middle mouse button click at the current cursor position.
            /// </summary>
            public static void MiddleClick()
            {
                SendMouseInput(MOUSEEVENTF.MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
                SendMouseInput(MOUSEEVENTF.MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(100);
            }

        }
        /////////////////////////////////////////////////////////////////////////////////////////////
        //                              SEND CALLBACKS                                             //
        //               --> Need to crosscheck with "BMS - Pitbuilder.key"! <--                   //
        /////////////////////////////////////////////////////////////////////////////////////////////

        // ======================================================================================= //
        //                           1. LEFT CONSOLE                                               //
        // ======================================================================================= //
        //                           1.01 UI FUNKTIONS                                             //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           1.02 3RD PARTY SOFTWARE                                       //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           2.01 TEST PANEL                                               //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           2.02 FLT CONTROL PANEL                                        //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           2.04 MANUAL TRIM PANEL                                        //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           2.05 FUEL PANEL                                               //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           2.06 AUX COMM PANEL                                           //
        // --------------------------------------------------------------------------------------- //
        
        /* ------------------------------------------------------------------------------------------
         * Backup tacan selector knobs (DIGITRAN) using binary code!
         * Possible PIN connections:            COMMON
         *                                      PIN 1
         *                                      PIN 2
         *                                      PIN 4
         *                                      PIN 8
         *                                      
         *  Switch Position:                    Binary Code:
         *  ----------------                    ------------
         *  Position 0:                         0000
         *  Position 1:                         0001
         *  Position 2:                         0010
         *  Position 3:                         0011
         *  Position 4:                         0100
         *  Position 5:                         0101
         *  Position 6:                         0110
         *  Position 7:                         0111
         *  Position 8:                         1000
         *  Position 9:                         1001
         *  
         *  Needs four connections on PHCC-KeyPH 64 per switch (plus common)!
         *
         * ------------------------------------------------------------------------------------------  
         */

        // --------------------------------------------------------------------------------------- //
        //                           2.07 EXT LIGHTING PANEL                                       //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           2.08 EPU PANEL                                                //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           2.09 ELEC PANEL                                               //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           2.10 AVTR PANEL                                               //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           2.11 ECM PANEL                                                //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           2.12 ENG & JET START PANEL                                    //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           2.13 AUDIO 2 PANEL                                            //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           2.14 AUDIO 1 PANEL                                            //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           2.15 MPO PANEL                                                //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           2.16 UHF PANEL                                                //
        // --------------------------------- Nearly complete - just SEND-KEYSTROKES-CODE missing - //

        /*-------------------------------------------------------------------------------------------
         * Following is obsolete, using LEO BODNAR board for ROTARY ENCODER!
         *-------------------------------------------------------------------------------------------
         
        public void SimCycleRadioChannel()
        { // SHIFT + ALT + S 
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LShift, true, false);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, true, false);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.S, true, false);
            Wait(50); //50
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.S, false, true);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, false, true);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LShift, false, true);
        }
        
        public void SimDecRadioChannel()
        { // CTRL + ALT + S
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, true, false);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, true, false);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.S, true, false);
            Wait(50); //50
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.S, false, true);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, false, true);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, false, true);
        }
        *-------------------------------------------------------------------------------------------
        */

        public void SimBUPUhfFrequency1_2()
        { // SHIFT + ALT + D
        }

        public void SimBUPUhfFrequency1_3()
        { // CTRL + ALT + D
        }

        public void SimBUPUhfFrequency2_0()
        { // SHIFT + CTRL + ALT + D
        }

        public void SimBUPUhfFrequency2_1()
        { // SHIFT + CTRL + F
        }

        public void SimBUPUhfFrequency2_2()
        { // SHIFT + ALT + F
        }

        public void SimBUPUhfFrequency2_3()
        { // CTRL + ALT + F
        }

        public void SimBUPUhfFrequency2_4()
        { // SHIFT + CTRL + ALT + F
        }

        public void SimBUPUhfFrequency2_5()
        { // SHIFT + CTRL + G
        }

        public void SimBUPUhfFrequency2_6()
        { // SHIFT + ALT + G
        }
        public void SimBUPUhfFrequency2_7()
        { // CTRL + ALT + G
        }
        public void SimBUPUhfFrequency2_8()
        { // SHIFT + CTRL + ALT + G
        }

        public void SimBUPUhfFrequency2_9()
        { // SHIFT + CTRL + J
        }

        public void SimBUPUhfFrequency3_0()
        { // SHIFT + ALT + J
        }

        public void SimBUPUhfFrequency3_1()
        { // CNTRL + ALT + H
        }
        public void SimBUPUhfFrequency3_2()
        { // SHIFT + CTRL + ALT + H
        }

        public void SimBUPUhfFrequency3_3()
        { // SHIFT + CTRL + J
        }

        public void SimBUPUhfFrequency3_4()
        { //SHIFT + ALT + J
        }

        public void SimBUPUhfFrequency3_5()
        { //CNTRL + ALT + J
        }

        public void SimBUPUhfFrequency3_6()
        { // SHIFT + CTRL + ALT + J
        }

        public void SimBUPUhfFrequency3_7()
        { // SHIFT + CTRL + K
        }

        public void SimBUPUhfFrequency3_8()
        { //SHIFT + ALT + K
        }

        public void SimBUPUhfFrequency3_9()
        { // CNTRL + ALT + K
        }

        public void SimBUPUhfFrequency4_0()
        { // SHIFT + CTRL + ALT + K
        }

        public void SimBUPUhfFrequency4_1()
        { // SHIFT + CTRL + L
        }

        public void SimBUPUhfFrequency4_2()
        { // SHIFT + ALT + L
        }

        public void SimBUPUhfFrequency4_3()
        {//CTRL + ALT + L 
        }

        public void SimBUPUhfFrequency4_4()
        { //SHIFT + CTRL + ALT + L 
        }

        public void SimBUPUhfFrequency4_5()
        { // SHIFT + CTRL + ;
        }

        public void SimBUPUhfFrequency4_6()
        { // SHIFT + ALT + ; 

        }
        public void SimBUPUhfFrequency4_7()
        { // CTRL + ALT + ; 
        }

        public void SimBUPUhfFrequency4_8()
        { // SHIFT + CTRL + ALT + ;
        }

        public void SimBUPUhfFrequency4_9()
        { // SHIFT + CTRL + '
        }

        public void SimBUPUhfFrequency5_00()
        { // SHIFT + ALT + '
        }

        public void SimBUPUhfFrequency5_25()
        { // CTRL + ALT + '
        }

        public void SimBUPUhfFrequency5_50()
        { // SHIFT + CNTRL + ALT + '
        }

        public void SimBUPUhfFrequency5_75()
        { // SHIFT + CNTRL + ENTER
        }

        public void SimBUPUhfOff()
        { // SHIFT + ALT + ENTER
        }

        public void SimBUPUhfMain()
        { // SHIFT + CTRL + ALT + ENTER
        }

        public void OTWBalanceIVCvsAIUp()
        { // SHIFT + CTRL + ALT + Z
        }

        public void OTWBalanceIVCvsAIDown()
        { // SHIFT + CTRL + X
        }

        public void SimBUPUhfManual()
        { // SHIFT + CTRL + ALT + X 
        }

        public void SimBUPUhfPreset()
        { // SHIFT  + CTRL + C
        }

        public void SimBUPUhfGuard()
        { // SHIFT + ALT + C
        }

        // --------------------------------------------------------------------------------------- //
        //                           2.17 LEFT SIDE WALL                                           //
        // ---------------------------------------------------------------------------- Complete - //
        public void SimSlapSwitch()
        { // SHIFT + CTRL + V

            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LShift, true, false);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, true, false);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.V, true, false);
            Wait(50);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.V, false, true);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, false, true);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LShift, false, true);
        }

        public void AFCanopyClose()
        { // SHIT + ALT + V 

            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LShift, true, false);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, true, false);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.V, true, false);
            Wait(50);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.V, false, true);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, false, true);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LShift, false, true);
        }

        public void AFCanopyOpen()
        { // CTRL + ALT + V

         
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, true, false);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, true, false);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.V, true, false);
            Wait(50);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.V, false, true);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, false, true);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, false, true);
        }

        // --------------------------------------------------------------------------------------- //
        //                           2.18 SEAT                                                     //
        // ---------------------------------------------------------------------------- Complete - //
        public void SimEject()
        { // BACKSPACE

          //    KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Backspace, true, false);
          //   Wait(1500);
          //     KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Backspace, false, true);

            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.N, true, false);
            Wait(1500);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.N, false, true);
        }

        public void SimSeatOn()
        { // SHIFT + CRTL + ALT + V

            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LShift, true, false);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, true, false);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, true, false);
                KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.V, true, false);
            Wait(50);
               KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.V, false, true);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, false, true);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, false, true);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LShift, false, true);
        }

        public void SimSeatOff()
        { // SHIFT + CTRL + B

            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LShift, true, false);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, true, false);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.B, true, false);
            Wait(50);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.B, false, true);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, false, true);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LShift, false, true);
        }

        // --------------------------------------------------------------------------------------- //
        //                           2.19 THROTTLE QUADRANT SYSTEM                                 //
        // ---------------------------------------------------------------------------- Complete - //
        //                                No inputs neccessary,                                    //
        //                                DirectX-input via HOTAS Cougar ingame                    //
        // --------------------------------------------------------------------------------------- //

        // ======================================================================================= //
        //                          3. LEFT AUX CONSOLE                                            //
        // ======================================================================================= //
        //                           3.01 ALT GEAR CONTROL                                         //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           3.02 TWA PANEL                                                //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           3.03 HMCS PANEL                                               //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           3.04 CMDS PANEL                                               //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           3.015 GEAR PANEL                                              //
        // --------------------------------------------------------------------------------------- //


        // ======================================================================================= //
        //                           4. CENTER CONSOLE                                             //
        // ======================================================================================= //
        //                           4.01 MISC PANEL                                               //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           4.02 LEFT EYEBROW                                             //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           4.03 TWP                                                      //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           4.04 RWR                                                      //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           4.05 LEFT MFD                                                 //
        // --------------------------------------------------------------------------------------- //
        //                                No inputs neccessary,                                    //
        //                                DirectX-input via MFD Cougar ingame                      //
        // ---------------------------------------------------------------------------- Complete - //

        // --------------------------------------------------------------------------------------- //
        //                           4.06 ICP                                                      //
        // --------------------------------------------------------------------------------------- //
        //                                No inputs neccessary,                                    //
        //                                DirectX-input via FCenter-Software                       //
        // ------------------------------------------------------------------ Need to validate!  - //

        // --------------------------------------------------------------------------------------- //
        //                           4.07 MAIN INSTRUMENT                                          //
        // --------------------------------------------------------------------------------------- //

        /*-------------------------------------------------------------------------------------------
         * Following is obsolete, using LEO BODNAR board for ROTARY ENCODER!
         *
         * In detail:               HSI CRS
         *                          HSI HDG
         *                          Machmeter
         *                          Altimeter pressure
         *                          ADI centering
         *
         *                          UHF presets
         *-------------------------------------------------------------------------------------------

        public void HSICourseIncrease1()
        { // NUM9 (PageUp), NumLock has to be OFF!
         
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.NumPad9, true, false);
            Wait(50);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.NumPad9, false, true);
         
        }
        public void HSIHCourseDecrease1()
        { // NUM3 (PageUp), NumLock has to be OFF!
          
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.NumPad3, true, false);
            Wait(50);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.NumPad3, false, true);
         
        }
        public void HSIHeadingIncrease5()
        { // ALT + INSERT

             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, true, false);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Insert, true, false);
            Wait(50);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Insert, false, true);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, false, true);

        }

        public void HSIHeadingDecrease5()
        { // CTRL + INSERT

             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, true, false);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Insert, true, false);
            Wait(50);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Insert, false, true);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, false, true);

        }
        public void HSIHeadingIncrease1()
        { // CTRL + HOME

             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, true, false);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Home, true, false);
            Wait(50);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Home, false, true);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, false, true);
            
        }

        public void HSIHeadingDecrease1()
        {  // ALT + HOME

             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, true, false);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Home, true, false);
            Wait(50);
              KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Home, false, true);
             KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, false, true);
        }
        
        *-------------------------------------------------------------------------------------------
        */

        public void HSIHeadingIncrease1()
        { // ALT + Home

            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, true, false);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Home, true, false);
            Wait(50);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Home, false, true);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, false, true);
        }

        public void HSIHeadingDecrease1()
        { // CTRL + Home

            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, true, false);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Home, true, false);
            Wait(50);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Home, false, true);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, false, true);
        }

        public void HSICourseIncrease1()
        { // CTRL + DEL

            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, true, false);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Delete, true, false);
            Wait(50);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Delete, false, true);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, false, true);
        }

        public void HSICourseDecrease1()
        { // ALT + DEL

            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, true, false);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Delete, true, false);
            Wait(50);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Delete, false, true);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, false, true);
        }
        public void AltPressureIncrease1()
        { // CTRL + UpArrow

            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, true, false);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Up, true, false);
            Wait(50);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Up, false, true);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LControl, false, true);
        }

        public void AltPressureDecrease1()
        { // ALT + UpArrow

            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, true, false);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Up, true, false);
            Wait(50);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.Up, false, true);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.LMenu, false, true);
        }

        // --------------------------------------------------------------------------------------- //
        //                           4.08 INSTR MODE PANEL                                         //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           4.09 FUEL QTY PANEL                                           //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           4.10 RIGHT MFD                                                //
        // --------------------------------------------------------------------------------------- //
        //                                No inputs neccessary,                                    //
        //                                DirectX-input via MFD Cougar ingame                      //
        // ---------------------------------------------------------------------------- Complete - //

        // ======================================================================================= //
        //                           5. RIGHT CONSOLE                                              //
        // ======================================================================================= //
        //                           5.01 SNSR PWR PANEL                                           //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           5.02 HUD PANEL                                                //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           5.04 LIGHTING PANEL                                           //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           5.05 AIR COND PANEL                                           //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           5.06 ZEROIZE PANEL                                            //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           5.09 AVIONICS POWER PANEL                                     //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           5.10 OXYGEN PANEL                                             //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           5.11 FLIGHT STICK                                             //
        // --------------------------------------------------------------------------------------- //


        // ======================================================================================= //
        //                           6. MISCELANGEOUS                                              //
        // ======================================================================================= //
        //                           6.01 OTHER COCKPIT CALLBACKS                                  //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           6.03 KEYBOARD FLIGHT CONTROLS                                 //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           6.06 SIMULATION & HARDWARE                                    //
        // --------------------------------------------------------------------------------------- //
        public void SimTogglePaused()
        {
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.P, true, false);
            Wait(50);
            KeyAndMouseUtils.SendKeyInputToFalcon((ushort)ScanCodes.P, false, true);
        }


        // ======================================================================================= //
        //                           7. VIEWS                                                      //
        // ======================================================================================= //
        //                           7.01 VIEW GENERAL CONTROL                                     //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           7.02 VIEW INTERNAL                                            //
        // --------------------------------------------------------------------------------------- //

        // --------------------------------------------------------------------------------------- //
        //                           7.03 VIEW EXTERNAL                                            //
        // --------------------------------------------------------------------------------------- //


        // ======================================================================================= //
        //                           8. RADIO COMMS                                                //
        // ======================================================================================= //
        //                           8.01 GENERAL RADIO OPTIONS                                    //
        // --------------------------------------------------------------------------------------- //


        public void Wait(int ms)
            {
                // Warteschleife - offensichtlich besser als "Thread.Sleep()"??? 14.07.2017

                DateTime start = DateTime.Now;
                while ((DateTime.Now - start).TotalMilliseconds < ms)
                    Application.DoEvents();
            }
        }
    }

