﻿// COPYRIGHT 2010 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.
//  
// INPUTSETTINGS
// 
// This module links the user keystrokes to their functions.
// For example, the F1 key links to the DisplayHelpMenu command.
// 
// All program commands are described by their UserCommand enum.
// InputSettings.Commands[] lists the key combinations that trigger each command.
// Each key combination is described by a UserCommandInput class or its descendants:
//      UserCommandKeyInput - represents a key press with optional CTRL ALT or SHIFT
//                ie ALT-F4 does the exit command
//      UserCommandModifierInput - represents a combo of CTRL ALT SHIFT used to modify another command, ie fast/slow etc
//                ie SHIFT used with a movement key speeds it up
//      UserCommandModifiableKeyInput - represents a key that works with one of the modifiers specified above
//                ie UP ARROW - can be modified with SHIFT for fast or CTRL for slow.
//
// Keys are specified in one of two ways:
//      XNA virtual keys, described by an enum such as Keys.Up, represent a key by its symbol
//  or  numeric scan code which represents the key by its physical location of the keyboard
// The programmer will specify the default key using one of these methods.
// 
// Maintenance - a programmer wishing to add a new keyboard operated function does the following:
//       1.  add to enum UserCommands, observing that the first word in the enum name represents the category
//       2.  add an entry in SetDefaults()
//       3.  if this is a UserCommandModifiableInput type, then you may need
//             to add an entry in FixModifiableKeys() to ensure the 'ignore keys' 
//             are set properly after the user changes the modifier.
//
// Clients use the UserInput class ( in UserInput.cs) to determine if a specific UserCommand has been triggered ie
//      if( UserInput.IsPressed( UserCommands.ControlDoorRight) ) ....  
//         which passes the current keyboard state to the UserCommandInput class 
//         who evaluates if the appropriate key combination has been pressed.
//
// Each UserCommandInput class provides additional methods to serialize its state as text out to the registry
// or vice versa, parsing settings in as text from the registry or a command line override.
//
// InputSetting initializes itself in three ways:
//   - first from the defaults defined by the programmer in SetDefaults()
//   - then by entries placed in the registry by the Menu's option configuration panel
//   - then by command line overrides such as -DisplayHelpMenu=45  where the number represents a key scan code
//           Note- command line overrides are not checked for conflict errors


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Win32;


namespace ORTS
{
    public enum UserCommands
    {
        GamePauseMenu,
        GameSave,
        GameQuit,
        GamePause,
        GameScreenshot,
        GameFullscreen,
        GameSwitchAhead,
        GameSwitchBehind,
        GameSwitchPicked,
        GameSwitchWithMouse,
        GameUncoupleWithMouse,
        GameLocomotiveSwap,
        GameRequestControl,
        GameMultiPlayerDispatcher,

        DisplayNextWindowTab,
        DisplayHelpWindow,
        DisplayTrackMonitorWindow,
        DisplayHUD,
        DisplayCarLabels,
        DisplayStationLabels,
        DisplaySwitchWindow,
        DisplayTrainOperationsWindow,
        DisplayNextStationWindow,
        DisplayCompassWindow,

        DebugLocomotiveFlip,
        DebugResetSignal,
        DebugForcePlayerAuthorization,
        DebugSpeedUp,
        DebugSpeedDown,
        DebugSpeedReset,
        DebugOvercastIncrease,
        DebugOvercastDecrease,
        DebugClockForwards,
        DebugClockBackwards,
        DebugLogger,
        DebugWeatherChange,
        DebugLockShadows,
        DebugDumpKeymap,
        DebugLogRenderFrame,
        DebugTracks,
        DebugSignalling,
        DebugResetWheelSlip,
        DebugToggleAdvancedAdhesion,

        CameraCab,
        CameraOutsideFront,
        CameraOutsideRear,
        CameraTrackside,
        CameraPassenger,
        CameraBrakeman,
        CameraFree,
        CameraPreviousFree, 
        CameraHeadOutForward,
        CameraHeadOutBackward,
        CameraToggleShowCab,
        CameraMoveFast,
        CameraMoveSlow,
        CameraPanLeft,
        CameraPanRight,
        CameraPanUp,
        CameraPanDown,
        CameraPanIn,
        CameraPanOut,
        CameraRotateLeft,
        CameraRotateRight,
        CameraRotateUp,
        CameraRotateDown,
        CameraCarNext,
        CameraCarPrevious,
        CameraCarFirst,
        CameraCarLast,
        CameraJumpingTrains,
        CameraJumpBackPlayer,

        ControlForwards,
        ControlBackwards,
        ControlReverserForward,
        ControlReverserBackwards,
        ControlThrottleIncrease,
        ControlThrottleDecrease,
        ControlTrainBrakeIncrease,
        ControlTrainBrakeDecrease,
        ControlEngineBrakeIncrease,
        ControlEngineBrakeDecrease,
        ControlDynamicBrakeIncrease,
        ControlDynamicBrakeDecrease,
        ControlBailOff,
        ControlInitializeBrakes,
        ControlHandbrakeFull,
        ControlHandbrakeNone,
        ControlRetainersOn,
        ControlRetainersOff,
        ControlBrakeHoseConnect,
        ControlBrakeHoseDisconnect,
        ControlAlerter,
        ControlEmergency,
        ControlSander,
        ControlWiper,
        ControlHorn,
        ControlBell,
        ControlDoorLeft,
        ControlDoorRight,
        ControlMirror,
        ControlLight,
        ControlPantographFirst,
        ControlPantographSecond,
        ControlDieselPlayer,
        ControlDieselHelper,
        ControlHeadlightIncrease,
        ControlHeadlightDecrease,
        ControlInjector1Increase,
        ControlInjector1Decrease,
        ControlInjector1,
        ControlInjector2Increase,
        ControlInjector2Decrease,
        ControlInjector2,
        ControlBlowerIncrease,
        ControlBlowerDecrease,
        ControlDamperIncrease,
        ControlDamperDecrease,
        ControlFiringRateIncrease,
        ControlFiringRateDecrease,
        ControlFireShovelFull,
        ControlCylinderCocks,
        ControlFiring,
    }

    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Shift = 1,
        Control = 2,
        Alt = 4
    }

    public static class InputSettings
    {
        public static string RegistryKey { get { return Program.RegistryKey + @"\Keys"; } }

        public static UserCommandInput[] Commands = new UserCommandInput[Enum.GetNames(typeof(UserCommands)).Length];
        public static UserSettings.Source[] Sources = new UserSettings.Source[Enum.GetNames(typeof(UserCommands)).Length];

        public static readonly string[] KeyboardLayout = new[] {
            "[01 ]   [3B ][3C ][3D ][3E ]   [3F ][40 ][41 ][42 ]   [43 ][44 ][57 ][58 ]   [37 ][46 ][11D]",
            "                                                                                            ",
            "[29 ][02 ][03 ][04 ][05 ][06 ][07 ][08 ][09 ][0A ][0B ][0C ][0D ][0E     ]   [52 ][47 ][49 ]",
            "[0F   ][10 ][11 ][12 ][13 ][14 ][15 ][16 ][17 ][18 ][19 ][1A ][1B ][2B   ]   [53 ][4F ][51 ]",
            "[3A     ][1E ][1F ][20 ][21 ][22 ][23 ][24 ][25 ][26 ][27 ][28 ][1C      ]                  ",
            "[2A       ][2C ][2D ][2E ][2F ][30 ][31 ][32 ][33 ][34 ][35 ][36         ]        [48 ]     ",
            "[1D   ][    ][38  ][39                          ][    ][    ][    ][1D   ]   [4B ][50 ][4D ]",
        };

        enum MapType
        {
            VirtualToCharacter = 2,
            VirtualToScan = 0,
            VirtualToScanEx = 4,
            ScanToVirtual = 1,
            ScanToVirtualEx = 3,
        }

        [DllImport("user32.dll")]
        static extern int MapVirtualKey(int code, MapType mapType);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetKeyNameText(int scanCode, [Out] string name, int nameLength);

        // Keyboard scancodes are basically constant; some keyboards have extra buttons (e.g. UK ones tend to have an
        // extra button next to Left Shift) or move one or two around (e.g. UK ones tend to move 0x2B down one row)
        // but generally this layout is right. Numeric keypad omitted as most keys are just duplicates of the main
        // keys (in two sets, based on Num Lock) and we don't use them. Scancodes are in hex.
        //
        // Break/Pause (0x11D) is handled specially and doesn't use the expect 0x45 scancode.
        //
        // [01 ]   [3B ][3C ][3D ][3E ]   [3F ][40 ][41 ][42 ]   [43 ][44 ][57 ][58 ]   [37 ][46 ][11D]
        // 
        // [29 ][02 ][03 ][04 ][05 ][06 ][07 ][08 ][09 ][0A ][0B ][0C ][0D ][0E     ]   [52 ][47 ][49 ]
        // [0F   ][10 ][11 ][12 ][13 ][14 ][15 ][16 ][17 ][18 ][19 ][1A ][1B ][2B   ]   [53 ][4F ][51 ]
        // [3A     ][1E ][1F ][20 ][21 ][22 ][23 ][24 ][25 ][26 ][27 ][28 ][1C      ]
        // [2A       ][2C ][2D ][2E ][2F ][30 ][31 ][32 ][33 ][34 ][35 ][36         ]        [48 ]
        // [1D   ][    ][38  ][39                          ][    ][    ][    ][1D   ]   [4B ][50 ][4D ]

        public static IEnumerable<UserCommands> GetScanCodeCommands(int scanCode)
        {
            return Enum.GetValues(typeof(UserCommands)).OfType<UserCommands>().Where(uc => (Commands[(int)uc] is UserCommandKeyInput) && ((Commands[(int)uc] as UserCommandKeyInput).ScanCode == scanCode));
        }

        public static Color GetScanCodeColor(int scanCode)
        {
            // These should be placed in order of priority - the first found match is used.
            var prefixesToColors = new List<KeyValuePair<string, Color>>()
            {
                new KeyValuePair<string, Color>("ControlReverser", Color.DarkGreen),
                new KeyValuePair<string, Color>("ControlThrottle", Color.DarkGreen),
                new KeyValuePair<string, Color>("ControlTrainBrake", Color.DarkRed),
                new KeyValuePair<string, Color>("ControlEngineBrake", Color.DarkRed),
                new KeyValuePair<string, Color>("ControlDynamicBrake", Color.DarkRed),
                new KeyValuePair<string, Color>("ControlBrakeHose", Color.DarkRed),
                new KeyValuePair<string, Color>("ControlEmergency", Color.DarkRed),
                new KeyValuePair<string, Color>("ControlBailOff", Color.DarkRed),
                new KeyValuePair<string, Color>("ControlInitializeBrakes", Color.DarkRed),
                new KeyValuePair<string, Color>("Control", Color.DarkBlue),
                new KeyValuePair<string, Color>("Camera", Color.Orange),
                new KeyValuePair<string, Color>("Display", Color.DarkGoldenrod),
                //new KeyValuePair<string, Color>("Game", Color.Blue),
                new KeyValuePair<string, Color>("", Color.Gray),
            };

            foreach (var prefixToColor in prefixesToColors)
                foreach (var command in GetScanCodeCommands(scanCode))
                    if (command.ToString().StartsWith(prefixToColor.Key))
                        return prefixToColor.Value;

            return Color.TransparentBlack;
        }

        public static void DrawKeyboardMap(Action<Rectangle> drawRow, Action<Rectangle, int, string> drawKey)
        {
            for (var y = 0; y < KeyboardLayout.Length; y++)
            {
                var keyboardLine = KeyboardLayout[y];
                if (drawRow != null)
                    drawRow(new Rectangle(0, y, keyboardLine.Length, 1));

                var x = keyboardLine.IndexOf('[');
                var lastIndex = -1;
                while (x != -1)
                {
                    var x2 = keyboardLine.IndexOf(']', x);

                    var scanCodeString = keyboardLine.Substring(x + 1, 3).Trim();
                    var keyScanCode = scanCodeString.Length > 0 ? int.Parse(scanCodeString, NumberStyles.HexNumber) : 0;

                    var keyName = InputSettings.GetScanCodeKeyName(keyScanCode);
                    // Only allow F-keys to show >1 character names. The rest we'll remove for now.
                    if ((keyName.Length > 1) && !new[] { 0x3B, 0x3C, 0x3D, 0x3E, 0x3F, 0x40, 0x41, 0x42, 0x43, 0x44, 0x57, 0x58 }.Contains(keyScanCode))
                        keyName = "";

                    if (drawKey != null)
                        drawKey(new Rectangle(x, y, x2 - x + 1, 1), keyScanCode, keyName);

                    lastIndex = x2;
                    x = keyboardLine.IndexOf('[', x2);
                }
            }
        }

        public static void DumpToText( string filePath )
        {
            using (var writer = new StreamWriter(File.OpenWrite(filePath)))
            {
                writer.WriteLine("{0,-40}{1,-40}{2}", "Command", "Key", "Unique Inputs");
                writer.WriteLine(new String('=', 40 * 3));
                foreach (UserCommands command in Enum.GetValues(typeof(UserCommands)))
                    writer.WriteLine("{0,-40}{1,-40}{2}", FormatCommandName(command), Commands[(int)command], String.Join(", ", Commands[(int)command].UniqueInputs().OrderBy(s => s).ToArray()));
            }
        }

        public static void DumpToGraphic( string filePath)
        {
            var keyWidth = 50;
            var keyHeight = 4 * keyWidth;
            var keySpacing = 5;
            var keyFontLabel = new System.Drawing.Font(System.Drawing.SystemFonts.MessageBoxFont.FontFamily, keyHeight * 0.33f, System.Drawing.GraphicsUnit.Pixel);
            var keyFontCommand = new System.Drawing.Font(System.Drawing.SystemFonts.MessageBoxFont.FontFamily, keyHeight * 0.22f, System.Drawing.GraphicsUnit.Pixel);
            var keyboardLayoutBitmap = new System.Drawing.Bitmap(KeyboardLayout[0].Length * keyWidth, KeyboardLayout.Length * keyHeight);
            using (var g = System.Drawing.Graphics.FromImage(keyboardLayoutBitmap))
            {
                DrawKeyboardMap(null, (keyBox, keyScanCode, keyName) =>
                {
                    var keyCommands = GetScanCodeCommands(keyScanCode);
                    var keyCommandNames = String.Join("\n", keyCommands.Select(c => String.Join(" ", FormatCommandName(c).Split(' ').Skip(1).ToArray())).ToArray());

                    var keyColor = GetScanCodeColor(keyScanCode);
                    var keyTextColor = System.Drawing.Brushes.Black;
                    if (keyColor == Color.TransparentBlack)
                    {
                        keyColor = Color.White;
                    }
                    else
                    {
                        keyColor.R += (byte)((255 - keyColor.R) * 2 / 3);
                        keyColor.G += (byte)((255 - keyColor.G) * 2 / 3);
                        keyColor.B += (byte)((255 - keyColor.B) * 2 / 3);
                    }
                    var w = g.MeasureString(keyName, keyFontLabel).Width;

                    Scale(ref keyBox, keyWidth, keyHeight);
                    keyBox.Inflate(-keySpacing, -keySpacing);

                    g.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb((int)keyColor.PackedValue)), keyBox.Left, keyBox.Top, keyBox.Width, keyBox.Height);
                    g.DrawRectangle(System.Drawing.Pens.Black, keyBox.Left, keyBox.Top, keyBox.Width, keyBox.Height);
                    g.DrawString(keyName, keyFontLabel, keyTextColor, keyBox.Right - g.MeasureString(keyName, keyFontLabel).Width + keySpacing, keyBox.Top - 3 * keySpacing);
                    g.DrawString(keyCommandNames, keyFontCommand, keyTextColor, keyBox.Left, keyBox.Bottom - keyCommands.Count() * keyFontCommand.Height);
                });
            }
            keyboardLayoutBitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        public static void Scale(ref Rectangle rectangle, int scaleX, int scaleY)
        {
            rectangle.X *= scaleX;
            rectangle.Y *= scaleY;
            rectangle.Width *= scaleX;
            rectangle.Height *= scaleY;
        }

        public static void Initialize(IEnumerable<string> options)
        {
            SetDefaults();
            LoadUserSettings(options);
        }

        public static void SetDefaults()
        {
            // TODO HERE... Program.Settings.KeySettings[UserCommands.GamePauseMenu]);  
            Commands[(int)UserCommands.GamePauseMenu] = new UserCommandKeyInput(0x01);
            Commands[(int)UserCommands.GameSave] = new UserCommandKeyInput(0x3C);
            Commands[(int)UserCommands.GameQuit] = new UserCommandKeyInput(0x3E, KeyModifiers.Alt);
            Commands[(int)UserCommands.GamePause] = new UserCommandKeyInput(Keys.Pause);
            Commands[(int)UserCommands.GameScreenshot] = new UserCommandKeyInput(Keys.PrintScreen);
            Commands[(int)UserCommands.GameFullscreen] = new UserCommandKeyInput(0x1C, KeyModifiers.Alt);
            Commands[(int)UserCommands.GameSwitchAhead] = new UserCommandKeyInput(0x22);
			Commands[(int)UserCommands.GameSwitchBehind] = new UserCommandKeyInput(0x22, KeyModifiers.Shift);
			Commands[(int)UserCommands.GameSwitchPicked] = new UserCommandKeyInput(0x22, KeyModifiers.Alt);
			Commands[(int)UserCommands.GameSwitchWithMouse] = new UserCommandModifierInput(KeyModifiers.Alt);
            Commands[(int)UserCommands.GameUncoupleWithMouse] = new UserCommandKeyInput(0x16);
            Commands[(int)UserCommands.GameLocomotiveSwap] = new UserCommandKeyInput(0x12, KeyModifiers.Control);
			Commands[(int)UserCommands.GameRequestControl] = new UserCommandKeyInput(0x12, KeyModifiers.Shift);

            Commands[(int)UserCommands.DisplayNextWindowTab] = new UserCommandModifierInput(KeyModifiers.Shift);
            Commands[(int)UserCommands.DisplayHelpWindow] = new UserCommandModifiableKeyInput(0x3B, Commands[(int)UserCommands.DisplayNextWindowTab]);
            Commands[(int)UserCommands.DisplayTrackMonitorWindow] = new UserCommandKeyInput(0x3E);
            Commands[(int)UserCommands.DisplayHUD] = new UserCommandModifiableKeyInput(0x3F, Commands[(int)UserCommands.DisplayNextWindowTab]);
            Commands[(int)UserCommands.DisplayStationLabels] = new UserCommandKeyInput(0x40);
            Commands[(int)UserCommands.DisplayCarLabels] = new UserCommandKeyInput(0x41);
            Commands[(int)UserCommands.DisplaySwitchWindow] = new UserCommandKeyInput(0x42);
            Commands[(int)UserCommands.DisplayTrainOperationsWindow] = new UserCommandKeyInput(0x43);
            Commands[(int)UserCommands.DisplayNextStationWindow] = new UserCommandKeyInput(0x44);
            Commands[(int)UserCommands.DisplayCompassWindow] = new UserCommandKeyInput(0x0B);

            Commands[(int)UserCommands.DebugLocomotiveFlip] = new UserCommandKeyInput(0x21, KeyModifiers.Shift | KeyModifiers.Control);
            Commands[(int)UserCommands.DebugResetSignal] = new UserCommandKeyInput(0x0F);
            Commands[(int)UserCommands.DebugForcePlayerAuthorization] = new UserCommandKeyInput(0x0F, KeyModifiers.Control);
            Commands[(int)UserCommands.DebugSpeedUp] = new UserCommandKeyInput(0x49, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugSpeedDown] = new UserCommandKeyInput(0x51, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugSpeedReset] = new UserCommandKeyInput(0x47, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugOvercastIncrease] = new UserCommandKeyInput(0x0D, KeyModifiers.Control);
            Commands[(int)UserCommands.DebugOvercastDecrease] = new UserCommandKeyInput(0x0C, KeyModifiers.Control);
            Commands[(int)UserCommands.DebugClockForwards] = new UserCommandKeyInput(0x0D);
            Commands[(int)UserCommands.DebugClockBackwards] = new UserCommandKeyInput(0x0C);
            Commands[(int)UserCommands.DebugLogger] = new UserCommandKeyInput(0x58);
            Commands[(int)UserCommands.DebugWeatherChange] = new UserCommandKeyInput(0x19, KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugLockShadows] = new UserCommandKeyInput(0x1F, KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugDumpKeymap] = new UserCommandKeyInput(0x3B, KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugLogRenderFrame] = new UserCommandKeyInput(0x58, KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugTracks] = new UserCommandKeyInput(0x40, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugSignalling] = new UserCommandKeyInput(0x57, KeyModifiers.Control | KeyModifiers.Alt);
            Commands[(int)UserCommands.DebugResetWheelSlip] = new UserCommandKeyInput(0x2D, KeyModifiers.Control);
            Commands[(int)UserCommands.DebugToggleAdvancedAdhesion] = new UserCommandKeyInput(0x2D, KeyModifiers.Control | KeyModifiers.Alt);

            Commands[(int)UserCommands.CameraCab] = new UserCommandKeyInput(0x02);
            Commands[(int)UserCommands.CameraOutsideFront] = new UserCommandKeyInput(0x03);
            Commands[(int)UserCommands.CameraOutsideRear] = new UserCommandKeyInput(0x04);
            Commands[(int)UserCommands.CameraTrackside] = new UserCommandKeyInput(0x05);
            Commands[(int)UserCommands.CameraPassenger] = new UserCommandKeyInput(0x06);
            Commands[(int)UserCommands.CameraBrakeman] = new UserCommandKeyInput(0x07);
            Commands[(int)UserCommands.CameraFree] = new UserCommandKeyInput(0x09);
            Commands[(int)UserCommands.CameraPreviousFree] = new UserCommandKeyInput( 0x09, KeyModifiers.Shift );
            Commands[(int)UserCommands.CameraHeadOutForward] = new UserCommandKeyInput( 0x47 );
            Commands[(int)UserCommands.CameraHeadOutBackward] = new UserCommandKeyInput(0x4F);
            Commands[(int)UserCommands.CameraToggleShowCab] = new UserCommandKeyInput(0x02, KeyModifiers.Shift );
            Commands[(int)UserCommands.CameraMoveFast] = new UserCommandModifierInput(KeyModifiers.Shift);
            Commands[(int)UserCommands.CameraMoveSlow] = new UserCommandModifierInput(KeyModifiers.Control);
            Commands[(int)UserCommands.CameraPanLeft] = new UserCommandModifiableKeyInput(0x4B, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraPanRight] = new UserCommandModifiableKeyInput(0x4D, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraPanUp] = new UserCommandModifiableKeyInput(0x48, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraPanDown] = new UserCommandModifiableKeyInput(0x50, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraPanIn] = new UserCommandModifiableKeyInput(0x49, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraPanOut] = new UserCommandModifiableKeyInput(0x51, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraRotateLeft] = new UserCommandModifiableKeyInput(0x4B, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraRotateRight] = new UserCommandModifiableKeyInput(0x4D, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraRotateUp] = new UserCommandModifiableKeyInput(0x48, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraRotateDown] = new UserCommandModifiableKeyInput(0x50, KeyModifiers.Alt, Commands[(int)UserCommands.CameraMoveFast], Commands[(int)UserCommands.CameraMoveSlow]);
            Commands[(int)UserCommands.CameraCarNext] = new UserCommandKeyInput(0x49, KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraCarPrevious] = new UserCommandKeyInput(0x51, KeyModifiers.Alt);
            Commands[(int)UserCommands.CameraCarFirst] = new UserCommandKeyInput(0x47, KeyModifiers.Alt);
			Commands[(int)UserCommands.CameraCarLast] = new UserCommandKeyInput(0x4F, KeyModifiers.Alt);
			Commands[(int)UserCommands.CameraJumpingTrains] = new UserCommandKeyInput(0x0A, KeyModifiers.Alt);
			Commands[(int)UserCommands.CameraJumpBackPlayer] = new UserCommandKeyInput(0x0A);

            Commands[(int)UserCommands.ControlForwards] = new UserCommandKeyInput(0x11);
            Commands[(int)UserCommands.ControlBackwards] = new UserCommandKeyInput(0x1F);
            Commands[(int)UserCommands.ControlReverserForward] = new UserCommandKeyInput(0x11);
            Commands[(int)UserCommands.ControlReverserBackwards] = new UserCommandKeyInput(0x1F);
            Commands[(int)UserCommands.ControlThrottleIncrease] = new UserCommandKeyInput(0x20);
            Commands[(int)UserCommands.ControlThrottleDecrease] = new UserCommandKeyInput(0x1E);
            Commands[(int)UserCommands.ControlTrainBrakeIncrease] = new UserCommandKeyInput(0x28);
            Commands[(int)UserCommands.ControlTrainBrakeDecrease] = new UserCommandKeyInput(0x27);
            Commands[(int)UserCommands.ControlEngineBrakeIncrease] = new UserCommandKeyInput(0x1B);
            Commands[(int)UserCommands.ControlEngineBrakeDecrease] = new UserCommandKeyInput(0x1A);
            Commands[(int)UserCommands.ControlDynamicBrakeIncrease] = new UserCommandKeyInput(0x34);
            Commands[(int)UserCommands.ControlDynamicBrakeDecrease] = new UserCommandKeyInput(0x33);
            Commands[(int)UserCommands.ControlBailOff] = new UserCommandKeyInput(0x35);
            Commands[(int)UserCommands.ControlInitializeBrakes] = new UserCommandKeyInput(0x35, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlHandbrakeFull] = new UserCommandKeyInput(0x28, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlHandbrakeNone] = new UserCommandKeyInput(0x27, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlRetainersOn] = new UserCommandKeyInput(0x1B, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlRetainersOff] = new UserCommandKeyInput(0x1A, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlBrakeHoseConnect] = new UserCommandKeyInput(0x2B);
            Commands[(int)UserCommands.ControlBrakeHoseDisconnect] = new UserCommandKeyInput(0x2B, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlAlerter] = new UserCommandKeyInput(0x2C);
            Commands[(int)UserCommands.ControlEmergency] = new UserCommandKeyInput(0x0E);
            Commands[(int)UserCommands.ControlSander] = new UserCommandKeyInput(0x2D);
            Commands[(int)UserCommands.ControlWiper] = new UserCommandKeyInput(0x2F);
            Commands[(int)UserCommands.ControlHorn] = new UserCommandKeyInput(0x39);

            Commands[(int)UserCommands.ControlBell] = new UserCommandKeyInput(0x30);
            Commands[(int)UserCommands.ControlDoorLeft] = new UserCommandKeyInput(0x10);
            Commands[(int)UserCommands.ControlDoorRight] = new UserCommandKeyInput(0x10, KeyModifiers.Shift);

            Commands[(int)UserCommands.ControlMirror] = new UserCommandKeyInput(0x2F, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlLight] = new UserCommandKeyInput(0x26);
			Commands[(int)UserCommands.ControlPantographFirst] = new UserCommandKeyInput(0x19);
			Commands[(int)UserCommands.ControlPantographSecond] = new UserCommandKeyInput(0x19, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlDieselPlayer] = new UserCommandKeyInput(0x15);
            Commands[(int)UserCommands.ControlDieselHelper] = new UserCommandKeyInput(0x15, KeyModifiers.Shift);
			Commands[(int)UserCommands.ControlHeadlightIncrease] = new UserCommandKeyInput(0x23);
            Commands[(int)UserCommands.ControlHeadlightDecrease] = new UserCommandKeyInput(0x23, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlInjector1Increase] = new UserCommandKeyInput(0x25);
            Commands[(int)UserCommands.ControlInjector1Decrease] = new UserCommandKeyInput(0x25, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlInjector1] = new UserCommandKeyInput(0x17);
            Commands[(int)UserCommands.ControlInjector2Increase] = new UserCommandKeyInput(0x26);
            Commands[(int)UserCommands.ControlInjector2Decrease] = new UserCommandKeyInput(0x26, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlInjector2] = new UserCommandKeyInput(0x18);
            Commands[(int)UserCommands.ControlBlowerIncrease] = new UserCommandKeyInput(0x31);
            Commands[(int)UserCommands.ControlBlowerDecrease] = new UserCommandKeyInput(0x31, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlDamperIncrease] = new UserCommandKeyInput(0x32);
            Commands[(int)UserCommands.ControlDamperDecrease] = new UserCommandKeyInput(0x32, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlFiringRateIncrease] = new UserCommandKeyInput(0x13);
            Commands[(int)UserCommands.ControlFiringRateDecrease] = new UserCommandKeyInput(0x13, KeyModifiers.Shift);
            Commands[(int)UserCommands.ControlFireShovelFull] = new UserCommandKeyInput(0x13, KeyModifiers.Control);
            Commands[(int)UserCommands.ControlCylinderCocks] = new UserCommandKeyInput(0x2E);
			Commands[(int)UserCommands.ControlFiring] = new UserCommandKeyInput(0x21, KeyModifiers.Control);
			Commands[(int)UserCommands.GameMultiPlayerDispatcher] = new UserCommandKeyInput(0x0A, KeyModifiers.Control);

            // for every user command
            foreach (var commandEnum in Enum.GetValues(typeof(UserCommands)))
            {
                if (Commands[(int)commandEnum] != null)
                    Sources[(int)commandEnum] = UserSettings.Source.Default;
            }
        }

        public static void LoadUserSettings(IEnumerable<string> options)      // options enumerates a list of option strings
        {                                                       // ie { "-GameScreenshot=47", "-GameSwitchWithMouse=0,1,0,0"

            // This special command-line option prevents the registry values from being used.
            var allowRegistryValues = !options.Contains("skip-user-settings", StringComparer.OrdinalIgnoreCase);

            var optionsDictionary = new Dictionary<string, string>();
            foreach (var option in options)
            {
                // Pull apart the command-line options so we can find them by setting name.
                var k = option.Split(new[] { '=', ':' }, 2)[0].ToLowerInvariant();
                var v = option.Contains('=') || option.Contains(':') ? option.Split(new[] { '=', ':' }, 2)[1].ToLowerInvariant() : "yes";
                optionsDictionary[k] = v;
            }
            // optionsDictionary contains eg { "GameScreenshot":"47",  "GameSwitchWithMouse":"0,1,0,0" }

            using (var RK = Registry.CurrentUser.OpenSubKey(InputSettings.RegistryKey))
            {
                // for every user command
                foreach (var eCommand in Enum.GetValues(typeof(UserCommands)))
                {
                    // Read in the command-line option, if it exists into optValue.
                    var propertyNameLower = eCommand.ToString().ToLowerInvariant();
                    var optValue = optionsDictionary.ContainsKey(propertyNameLower) ? (object)optionsDictionary[propertyNameLower] : null;
                    if (optValue != null)
                    {
                        try
                        {
                            Commands[(int)eCommand].SetFromRegString((string)optValue);
                            Sources[(int)eCommand] = UserSettings.Source.CommandLine;

                            continue;  // if that worked, were good, on to the next property
                        }
                        catch (ArgumentException)
                        {
                            Trace.TraceWarning("Unable to load {0} value from command line {1}", eCommand.ToString(), optValue);
                        }

                    }
                    // Read in the registry option, if it exists and we haven't disabled registry use
                    var regValue = allowRegistryValues && RK != null ? RK.GetValue(eCommand.ToString(), null) : null;
                    if (regValue != null)
                    {
                        try
                        {
                            Commands[(int)eCommand].SetFromRegString((string)regValue);
                            Sources[(int)eCommand] = UserSettings.Source.Registry;
                        }
                        catch (ArgumentException)
                        {
                            Trace.TraceWarning("Unable to load {0} value from registry {1}", eCommand.ToString(), regValue);
                        }
                    }
                }
            }
        }

        private static bool IsModifier(UserCommands command)
        {
            return InputSettings.Commands[(int)command].GetType() == typeof(UserCommandModifierInput);
        }

        /// <summary>
        /// Check Commands for duplicate key assignments and other errors
        /// </summary>
        /// <param name="debug">In release mode, don't report problems with the default assignments or with Debug key assignments.</param>
        /// <returns></returns>
        public static string CheckForErrors( bool debug)
        {


            StringBuilder errors = new StringBuilder();

            // Check for conflicting modifiers
            foreach (var eCommand in Enum.GetValues(typeof(UserCommands)))
            {
                UserCommandInput command = InputSettings.Commands[(int)eCommand];
                if( command.GetType() == typeof( UserCommandModifiableKeyInput ) )
                {
                    if( !debug )
                        if (eCommand.ToString().ToUpper() == "DEBUG" ) continue;

                    UserCommandModifiableKeyInput mc = (UserCommandModifiableKeyInput)command;

                    bool conflict = (mc.Control && mc.IgnoreControl) || (mc.Alt && mc.IgnoreAlt) || (mc.Shift && mc.IgnoreShift);

                    if( conflict )
                        errors.AppendFormat("Command {0} conflicts with its CTRL,ALT,SHIFT modifiers.\n", eCommand.ToString() );

                }
            }

            // Check for two commands assigned to the same key
            var firstUserCommand = Enum.GetValues(typeof(UserCommands)).Cast<UserCommands>().Min();
            var lastUserCommand = Enum.GetValues(typeof(UserCommands)).Cast<UserCommands>().Max();
            for (var outerCommand = firstUserCommand; outerCommand <= lastUserCommand; outerCommand++)
            {
                if (!IsModifier((UserCommands)outerCommand))  // modifiers are allowed to be duplicated
                {
                    for (var innerCommand = outerCommand + 1; innerCommand <= lastUserCommand; innerCommand++)
                    {
                        if (!debug)
                        {
                            // In release mode, ignore problems with the debug commands
                            if (outerCommand.ToString().ToUpper() == "DEBUG" || innerCommand.ToString().ToUpper() == "DEBUG") continue;
                            // And ignore problems with the default values
                            if (InputSettings.Sources[(int)innerCommand] == UserSettings.Source.Default
                                && InputSettings.Sources[(int)outerCommand] == UserSettings.Source.Default) continue;
                        }

                        var outerCommandUniqueInputs = Commands[(int)outerCommand].UniqueInputs();
                        var innerCommandUniqueInputs = Commands[(int)innerCommand].UniqueInputs();
                        var sharedUniqueInputs = outerCommandUniqueInputs.Where(id => innerCommandUniqueInputs.Contains(id));
                        foreach (var uniqueInput in sharedUniqueInputs)
                            errors.AppendFormat("Commands {0} and {1} conflict on input {2}\n", outerCommand, innerCommand, uniqueInput);
                    }
                }
            }

            return errors.ToString();
        }

        public static string FormatCommandName(UserCommands command)
        {
            var name = command.ToString();
            var nameU = name.ToUpperInvariant();
            var nameL = name.ToLowerInvariant();
            for (var i = name.Length - 1; i > 0; i--)
            {
                if (((name[i - 1] != nameU[i - 1]) && (name[i] == nameU[i])) ||
                    (name[i - 1] == nameL[i - 1]) && (name[i] != nameL[i]))
                {
                    name = name.Insert(i, " ");
                    nameL = nameL.Insert(i, " ");
                }
            }
            return name;
        }

        public static Keys GetScanCodeKeys(int scanCode)
        {
            var sc = scanCode;
            if (scanCode >= 0x0100)
                sc = 0xE100 | (scanCode & 0x7F);
            else if (scanCode >= 0x0080)
                sc = 0xE000 | (scanCode & 0x7F);
            return (Keys)MapVirtualKey(sc, MapType.ScanToVirtualEx);
        }

        public static string GetScanCodeKeyName(int scanCode)
        {
            var xnaName = Enum.GetName(typeof(Keys), GetScanCodeKeys(scanCode));
            var keyName = new String('\0', 32);
            var keyNameLength = GetKeyNameText(scanCode << 16, keyName, keyName.Length);

            keyName = keyName.Substring(0, keyNameLength);
            if (keyName.Length > 0)
            {
                // Pause is mapped to "Right Control" and GetKeyNameText prefers "NUM 9" to "PAGE UP" too so pick the
                // XNA key name in these cases.
                if ((scanCode == 0x11D) || keyName.StartsWith("NUM ", StringComparison.OrdinalIgnoreCase) || keyName.StartsWith(xnaName, StringComparison.OrdinalIgnoreCase) || xnaName.StartsWith(keyName, StringComparison.OrdinalIgnoreCase))
                    return xnaName;

                return keyName;
            }

            // If we failed to convert the scan code to a name, show the scan code for debugging.
            return String.Format(" [sc=0x{0:X2}]", scanCode);
        }

        public static string KeyAssignmentAsString(bool ctrl, bool alt, bool shift)
        {
            return KeyAssignmentAsString(ctrl, alt, shift, 0, Keys.None, false, false, false);
        }

        public static string KeyAssignmentAsString(bool ctrl, bool alt, bool shift, int scanCode, Keys vkey)
        {
            return KeyAssignmentAsString(ctrl, alt, shift, scanCode, vkey, false, false, false);
        }

        public static string KeyAssignmentAsString(bool ctrl, bool alt, bool shift, int scanCode, Keys vkey, bool ictrl, bool ialt, bool ishift)
        {
            StringBuilder key = new StringBuilder();

            if (shift) key = key.Append("Shift + ");
            if (ctrl) key = key.Append("Control + ");
            if (alt) key = key.Append("Alt + ");
            if (scanCode == 0 && vkey == Keys.None && key.Length > 0) key.Length -= 3; //command modifiers don't end in +

            if (scanCode != 0)
                key.Append(InputSettings.GetScanCodeKeyName(scanCode));
            else if (vkey != Keys.None)
                key.Append(vkey);

            if (ishift) key.Append(" (+ Shift)");
            if (ictrl) key.Append(" (+ Control)");
            if (ialt) key.Append(" (+ Alt)");

            return key.ToString();
        }
    }


    public abstract class UserCommandInput
    {
        public abstract bool IsKeyDown(KeyboardState keyboardState);

        public abstract IEnumerable<string> UniqueInputs();

        public abstract void SetFromRegString(string specifier); // ie scancode,vkey,ctrl,alt,shift  "45", or "45,0,0,1,0"  

        public abstract void SetFromValues(int scancode, Keys vkey, bool ctrl, bool alt, bool shift);

        public abstract void ToValue(out int scancode, out Keys vkey, out bool ctrl, out bool alt, out bool shift);

        public abstract void ToValue(out int scancode, out Keys vkey, out bool ctrl, out bool alt, out bool shift,out bool ictrl, out bool ialt, out bool ishift);

        public abstract string ToRegString(); // reverses of SetFrom ,ie produces string like "45,0,0,0,1"

        public abstract string  ToEditString();  // this is how the command appears in the user configuration editor

        public override string ToString()
        {
            return "";
        }
    }

    // Used as an input specifier for other commands
    public class UserCommandModifierInput : UserCommandInput
    {
        public bool Shift;
        public bool Control;
        public bool Alt;

        protected UserCommandModifierInput(bool shift, bool control, bool alt)
        {
            Shift = shift;
            Control = control;
            Alt = alt;
        }

        public UserCommandModifierInput(KeyModifiers modifiers)
            : this((modifiers & KeyModifiers.Shift) != 0, (modifiers & KeyModifiers.Control) != 0, (modifiers & KeyModifiers.Alt) != 0)
        {
        }

        protected bool IsModifiersMatching(KeyboardState keyboardState, bool shift, bool control, bool alt)
        {
            return (!shift || keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift)) &&
                (!control || keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl)) &&
                (!alt || keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt));
        }

        public override bool IsKeyDown(KeyboardState keyboardState)
        {
            return IsModifiersMatching(keyboardState, Shift, Control, Alt);
        }

        public override IEnumerable<string> UniqueInputs()
        {
            var key = new StringBuilder();
            if (Shift) key = key.Append("Shift+");
            if (Control) key = key.Append("Control+");
            if (Alt) key = key.Append("Alt+");
            if (key.Length > 0) key.Length -= 1;
            return new[] { key.ToString() };
        }

        public override void SetFromRegString(string specifier) // ie 0,ctrl,alt,shift  "0,0,0,1,0"  
        {               
            int[] v = ((string)specifier).Split(',').Select(s => int.Parse(s)).ToArray();
            if ( true || false )
            if ( v[0] != 0  || v[1] != 0  )  throw new System.Exception("First two params of a CommandModifier must be 0");
            Control = ( v[2] != 0 );
            Alt = ( v[3] != 0 );
            Shift = ( v[4] != 0 );
        }

        public override void ToValue(out int scancode, out Keys vkey, out bool ctrl, out bool alt, out bool shift, out bool ictrl, out bool ialt, out bool ishift)
        {
            ictrl = false;
            ialt = false;
            ishift = false;
            ToValue(out scancode, out vkey, out ctrl, out alt, out shift);
        }

        public override void ToValue(out int scancode, out Keys vkey, out bool ctrl, out bool alt, out bool shift)
        {
            scancode = 0;
            vkey = 0;
            ctrl = Control;
            alt = Alt;
            shift = Shift;
        }

        public override void SetFromValues(int scancode, Keys vkey, bool ctrl, bool alt, bool shift)
        {
            Control = ctrl;
            Alt = alt;
            Shift = shift;
        }

        public override string ToRegString()
        {
            StringBuilder s = new StringBuilder();

            s.Append( "0,0,");
            s.Append( Control ? "1,": "0," );
            s.Append( Alt ? "1,":"0," );
            s.Append( Shift ? "1": "0" );

            return s.ToString();
        }

        public override string ToEditString()
        {
            return ToString();
        }

        public override string ToString()
        {
            var key = new StringBuilder();
            if (Shift) key = key.Append("Shift + ");
            if (Control) key = key.Append("Control + ");
            if (Alt) key = key.Append("Alt + ");
            if (key.Length > 0) key.Length -= 3;
            return key.ToString();
        }

        
    }

    // Activates when the key is pressed with the correct combo of CTRL ALT SHIFT or NONE
    public class UserCommandKeyInput : UserCommandInput
    {
        public int ScanCode;
        public Keys VirtualKey;
        public bool Shift;
        public bool Control;
        public bool Alt;

        protected UserCommandKeyInput(int scancode, Keys virtualKey, bool shift, bool control, bool alt)
        {
            Debug.Assert((scancode >= 1 && scancode <= 127) || (virtualKey != Keys.None), "Scan code for keyboard input is outside the allowed range of 1-127.");
            ScanCode = scancode;
            VirtualKey = virtualKey;
            Shift = shift;
            Control = control;
            Alt = alt;
        }

        public UserCommandKeyInput(int scancode)
            : this(scancode, KeyModifiers.None)
        {
        }

        public UserCommandKeyInput(Keys virtualKey)
            : this(virtualKey, KeyModifiers.None)
        {
        }

        public UserCommandKeyInput(int scancode, KeyModifiers modifiers)
            : this(scancode, Keys.None, (modifiers & KeyModifiers.Shift) != 0, (modifiers & KeyModifiers.Control) != 0, (modifiers & KeyModifiers.Alt) != 0)
        {
        }

        public UserCommandKeyInput(Keys virtualKey, KeyModifiers modifiers)
            : this(0, virtualKey, (modifiers & KeyModifiers.Shift) != 0, (modifiers & KeyModifiers.Control) != 0, (modifiers & KeyModifiers.Alt) != 0)
        {
        }

        protected Keys Key
        {
            get
            {
                return VirtualKey == Keys.None ? InputSettings.GetScanCodeKeys(ScanCode) : VirtualKey;
            }
        }

        protected bool IsKeyMatching(KeyboardState keyboardState, Keys key)
        {
            return keyboardState.IsKeyDown(key);
        }

        protected bool IsModifiersMatching(KeyboardState keyboardState, bool shift, bool control, bool alt)
        {
            return ((keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift)) == shift) &&
                ((keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl)) == control) &&
                ((keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt)) == alt);
        }

        public override bool IsKeyDown(KeyboardState keyboardState)
        {
            return IsKeyMatching(keyboardState, Key) && IsModifiersMatching(keyboardState, Shift, Control, Alt);
        }

        public override IEnumerable<string> UniqueInputs()
        {
            var key = new StringBuilder();
            if (Shift) key = key.Append("Shift+");
            if (Control) key = key.Append("Control+");
            if (Alt) key = key.Append("Alt+");
            if (VirtualKey == Keys.None)
                key.AppendFormat("0x{0:X2}", ScanCode);
            else
                key.Append(VirtualKey);
            return new[] { key.ToString() };
        }


        public override void SetFromRegString(string specifier) // ie scanCode,(ctrl,alt,shift)  "45,0,0,1,0" or "67"
        {
            int[] v = ((string)specifier).Split(',').Select(s => int.Parse(s)).ToArray();
            ScanCode = v[0];
            VirtualKey = (Keys)( v[1] );
            if( v.Length > 1 ) Control = (v[2] != 0);
            if (v.Length > 2) Alt = (v[3] != 0);
            if (v.Length > 3) Shift = (v[4] != 0);
        }

        public override void SetFromValues(int scancode, Keys vkey, bool ctrl, bool alt, bool shift)
        {
            ScanCode = scancode;
            VirtualKey = vkey;
            Control = ctrl;
            Alt = alt;
            Shift = shift;
        }

        public override void ToValue(out int scancode, out Keys vkey, out bool ctrl, out bool alt, out bool shift, out bool ictrl, out bool ialt, out bool ishift)
        {
            ictrl = false;
            ialt = false;
            ishift = false;
            ToValue(out scancode, out vkey, out ctrl, out alt, out shift);
        }

        public override void ToValue(out int scancode, out Keys vkey, out bool ctrl, out bool alt, out bool shift)
        {
            scancode = ScanCode;
            vkey = VirtualKey;
            ctrl = Control;
            alt = Alt;
            shift = Shift;
        }

        public override string ToRegString()  // ie scanCode,ctrl,alt,shift  ie "45,0,1,0"
        {
            StringBuilder s = new StringBuilder();

            s.Append(ScanCode.ToString());
            s.Append(',');
            s.Append(((int)VirtualKey).ToString());
            s.Append(Control ? ",1,":",0," );
            s.Append(Alt ? "1,": "0," );
            s.Append(Shift ? "1":"0" );

            return s.ToString();
        }

        public override string ToEditString()
        {
            return ToString();
        }

        public override string ToString()
        {
            var key = new StringBuilder();
            if (Shift) key.Append("Shift + ");
            if (Control) key.Append("Control + ");
            if (Alt) key.Append("Alt + ");
            if (VirtualKey == Keys.None)
                key.Append(InputSettings.GetScanCodeKeyName(ScanCode));
            else
                key.Append(VirtualKey);
            return key.ToString();
        }
    }

    // Activates when the key is pressed disregarding how the specified CTRL ALT SHIFT are pressed
    public class UserCommandModifiableKeyInput : UserCommandKeyInput
    {
        public bool IgnoreShift;
        public bool IgnoreControl;
        public bool IgnoreAlt;

        UserCommandModifiableKeyInput(int scanCode, bool shift, bool control, bool alt, bool ignoreShift, bool ignoreControl, bool ignoreAlt)
            : base(scanCode, Keys.None, shift, control, alt)
        {
            IgnoreShift = ignoreShift;
            IgnoreControl = ignoreControl;
            IgnoreAlt = ignoreAlt;
        }

        UserCommandModifiableKeyInput(int scanCode, KeyModifiers modifiers, IEnumerable<UserCommandModifierInput> combine)
            : this(scanCode, (modifiers & KeyModifiers.Shift) != 0, (modifiers & KeyModifiers.Control) != 0, (modifiers & KeyModifiers.Alt) != 0, combine.Any(c => c.Shift), combine.Any(c => c.Control), combine.Any(c => c.Alt))
        {
        }

        public UserCommandModifiableKeyInput(int scanCode, KeyModifiers modifiers, params UserCommandInput[] combine)
            : this(scanCode, modifiers, combine.Cast<UserCommandModifierInput>())
        {
        }

        public UserCommandModifiableKeyInput(int scanCode, params UserCommandInput[] combine)
            : this(scanCode, KeyModifiers.None, combine)
        {
        }

        public override bool IsKeyDown(KeyboardState keyboardState)
        {
            var shiftState = IgnoreShift ? keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift) : Shift;
            var controlState = IgnoreControl ? keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl) : Control;
            var altState = IgnoreAlt ? keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt) : Alt;
            return IsKeyMatching(keyboardState, Key) && IsModifiersMatching(keyboardState, shiftState, controlState, altState);
        }

        public override IEnumerable<string> UniqueInputs()
        {
            IEnumerable<string> inputs = new[] { Key.ToString() };

            // This must result in the output being Shift+Control+Alt+key.

            if (IgnoreAlt)
                inputs = inputs.SelectMany(i => new[] { i, "Alt+" + i });
            else if (Alt)
                inputs = inputs.Select(i => "Alt+" + i);

            if (IgnoreControl)
                inputs = inputs.SelectMany(i => new[] { i, "Control+" + i });
            else if (Control)
                inputs = inputs.Select(i => "Control+" + i);

            if (IgnoreShift)
                inputs = inputs.SelectMany(i => new[] { i, "Shift+" + i });
            else if (Shift)
                inputs = inputs.Select(i => "Shift+" + i);

            return inputs;
        }

        public override void SetFromRegString(string specifier) // ie scanCode,vkey,(ctrl,alt,shift),(ignore ctrl, ignore alt ignore shift )
                                                       // "45,0,0,1,0" or "67" or "33,0,0,0,0,0,1,1"
        {
            int[] v = ((string)specifier).Split(',').Select(s => int.Parse(s)).ToArray();
            ScanCode = v[0];
            VirtualKey = (Keys)v[1];
            if (v.Length > 1) Control = (v[2] != 0);
            if (v.Length > 2) Alt = (v[3] != 0);
            if (v.Length > 3) Shift = (v[4] != 0);
            if (v.Length > 4) IgnoreControl = (v[5] != 0);
            if (v.Length > 5) IgnoreAlt = (v[6] != 0);
            if (v.Length > 6) IgnoreShift = (v[7] != 0);
        }


        public override string ToRegString()  // ie scanCode,vkey,ctrl,alt,shift, ignore ctrl, ignore alt, ignore shift  ie "45,0,0,1,0,1,1,0"
        {
            StringBuilder s = new StringBuilder();

            Debug.Assert(VirtualKey == Keys.None);  // all user overrides are entered as scan codes
            s.Append(ScanCode.ToString());
            s.Append(',');
            s.Append(((int)VirtualKey).ToString());
            s.Append(Control ? ",1,": ",0," );
            s.Append(Alt ? "1,": "0," );
            s.Append(Shift ? "1,": "0," );
            s.Append(IgnoreControl ? "1,": "0,"  );
            s.Append(IgnoreAlt ? "1,": "0,"  );
            s.Append(IgnoreShift ? "1":"0" );

            return s.ToString();
        }

        public override void ToValue(out int scancode, out Keys vkey, out bool ctrl, out bool alt, out bool shift, out bool ictrl, out bool ialt, out bool ishift)
        {
            ictrl = IgnoreControl;
            ialt = IgnoreAlt;
            ishift = IgnoreShift;
            ToValue(out scancode, out vkey, out ctrl, out alt, out shift);
        }

        public override string ToEditString()
        {
            return base.ToString();
        }

        public override string ToString()
        {
            var key = new StringBuilder(base.ToString());
            if (IgnoreShift) key.Append(" (+ Shift)");
            if (IgnoreControl) key.Append(" (+ Control)");
            if (IgnoreAlt) key.Append(" (+ Alt)");
            return key.ToString();
        }
    }
}

