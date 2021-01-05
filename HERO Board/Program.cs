using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using CTRE.Gadgeteer.Module;
using CTRE.Phoenix;
using CTRE.Phoenix.Controller;
using CTRE.Phoenix.MotorControl;
using CTRE.Phoenix.MotorControl.CAN;

namespace HERO_Board
{
    public class Program
    {
        public GameController gamepad = new GameController(new CTRE.Phoenix.UsbHostDevice(0));
        public PneumaticControlModule pcm = new PneumaticControlModule(0);
        public DisplayModule display = new DisplayModule(CTRE.HERO.IO.Port8, DisplayModule.OrientationType.Landscape);
        public TalonSRX talon0;
        public TalonSRX talon1;
        public AnalogInput pot0 = new AnalogInput(CTRE.HERO.IO.Port1.Analog_Pin3);
        public AnalogInput pot1 = new AnalogInput(CTRE.HERO.IO.Port1.Analog_Pin4);
        public AnalogInput pot2 = new AnalogInput(CTRE.HERO.IO.Port1.Analog_Pin5);
        public ControlMode motionMode = ControlMode.PercentOutput;
        public Font ninaB = Properties.Resources.GetFont(Properties.Resources.FontResources.NinaB);

        public DisplayModule.LabelSprite title, label1, label2, label3, label4;
        public DisplayModule.RectSprite rect1, rect2, rect3, rect4;
        public DisplayModule.ResourceImageSprite image;

        public float xAxisA, xAxisB, yAxisA, yAxisB;
        public double angle, radian;
        public int analog0, analog1, analog2;
        public bool buttonX, buttonY, buttonA, buttonB;
        public bool buttonLeft, buttonRight;
        public bool toggleLeft, toggleRight;

        public double output = 0;
        public double xSave = 0;
        public double ySave = 0;
        public double aSave = 0;
        public double bSave = 0;
        public bool xSwitch = false;
        public bool ySwitch = false;
        public bool aSwitch = false;
        public bool bSwitch = false;
        public bool xState = false;
        public bool yState = false;
        public bool aState = false;
        public bool bState = false;

        public int kSlotId = 0;
        public int kTimeout = 50;
        public bool modeSwitch = false;
        public bool pistonSwitch = false;
        public bool pistonState = false;
        public int tSleep;

        public int displayMode = 0;
        public bool displayModeRefresh = false;
        public bool displaySwitchLeft = false;
        public bool displaySwitchRight = false;

        public static void Main()
        {
            Program program = new Program();
            program.Run();
        }

        public void Run()
        {
            Config(0, 1);
            while (true)
            {
                tSleep = 50;
                if (gamepad.GetConnectionStatus() == UsbDeviceConnection.Connected)
                {
                    Values();
                    Settings();
                    Motion();
                    Piston();
                }
                Display();
                Debug.Print(displayMode.ToString());
                Debug.Print(displayModeRefresh.ToString());
                CTRE.Phoenix.Watchdog.Feed();
                Thread.Sleep(tSleep);
            }
        }

        public void Config(int talonId0, int talonId1)
        {
            talon0 = new TalonSRX(talonId0);
            talon1 = new TalonSRX(talonId1);

            talon0.ConfigSelectedFeedbackSensor(FeedbackDevice.CTRE_MagEncoder_Relative, 0);
            talon0.SetSensorPhase(false);
            talon0.SetNeutralMode(NeutralMode.Brake);
            talon0.Config_kF(kSlotId, 0.1153f, kTimeout);
            talon0.Config_kP(kSlotId, 2.00f, kTimeout);
            talon0.Config_kI(kSlotId, 0f, kTimeout);
            talon0.Config_kD(kSlotId, 20f, kTimeout);
            talon0.Config_IntegralZone(kSlotId, 0, kTimeout);
            talon0.SelectProfileSlot(kSlotId, 0); 
            talon0.ConfigNominalOutputForward(0f, kTimeout);
            talon0.ConfigNominalOutputReverse(0f, kTimeout);
            talon0.ConfigPeakOutputForward(1.0f, kTimeout);
            talon0.ConfigPeakOutputReverse(-1.0f, kTimeout);
            talon0.ConfigMotionCruiseVelocity(8000, kTimeout);
            talon0.ConfigMotionAcceleration(16000, kTimeout);
        }
        public void Settings()
        {
            talon0.ConfigMotionCruiseVelocity(analog1, kTimeout);
            talon0.ConfigMotionAcceleration(analog2, kTimeout);
        }

        public void Values()
        {
            xAxisA = gamepad.GetAxis(0);
            xAxisB = gamepad.GetAxis(2);
            yAxisA = gamepad.GetAxis(1) * -1f;
            yAxisB = gamepad.GetAxis(5) * -1f;
            buttonX = gamepad.GetButton(1);
            buttonB = gamepad.GetButton(3);
            buttonY = gamepad.GetButton(4);
            buttonA = gamepad.GetButton(2);
            buttonLeft = gamepad.GetButton(5);
            buttonRight = gamepad.GetButton(6);
            toggleLeft = gamepad.GetButton(7);
            toggleRight = gamepad.GetButton(8);

            Deadband(ref xAxisA);
            Deadband(ref xAxisB);
            Deadband(ref yAxisA);
            Deadband(ref yAxisB);

            radian = System.Math.Atan2(yAxisA, xAxisA);
            angle = -radian * (180 / System.Math.PI);
            angle += 90;
            if (angle < 0)
            {
                angle = (angle + 360) % 360;
            }

            analog0 = (int) System.Math.Round(pot0.Read() * 10);
            analog1 = (int) System.Math.Round(pot1.Read() * 10) * 1000 + 5000;
            analog2 = (int) System.Math.Round(pot2.Read() * 10) * 1000 + 10000;
            if (analog0 < 1)
            {
                analog0 = 1;
            }
        }
        public static void Deadband(ref float val)
        {
            if (val < 0.10f && val > -0.10f)
            { 
                val = 0; 
            } 
        }

        public void Motion()
        {
            if (buttonLeft == true && modeSwitch == false)
            {
                modeSwitch = true;
                if (motionMode == ControlMode.PercentOutput)
                {
                    motionMode = ControlMode.MotionMagic;
                }
                else
                {
                    motionMode = ControlMode.PercentOutput;
                }
            }
            if (buttonLeft == false && modeSwitch == true)
            {
                modeSwitch = false;
            }

            if (motionMode == ControlMode.PercentOutput)
            {
                output = 0;
                talon0.Set(motionMode, yAxisA);
            }
            else if (motionMode == ControlMode.MotionMagic)
            {
                output = yAxisA * 4096 * analog0;
                MotionToggle();
                MotionSave();
                MotionOutput();
                talon0.Set(motionMode, output);
            }
            Thread.Sleep(10);
        }
        public void MotionToggle()
        {
            if (buttonX == true && xSwitch == false)
            {
                xSwitch = true;
                if (xState == false)
                {
                    xState = true;
                    yState = false;
                    aState = false;
                    bState = false;
                }
                else
                {
                    xState = false;
                }
            }
            if (buttonX == false && xSwitch == true)
            {
                xSwitch = false;
            }

            if (buttonY == true && ySwitch == false)
            {
                ySwitch = true;
                if (yState == false)
                {
                    xState = false;
                    yState = true;
                    aState = false;
                    bState = false;
                }
                else
                {
                    yState = false;
                }
            }
            if (buttonY == false && ySwitch == true)
            {
                ySwitch = false;
            }

            if (buttonA == true && aSwitch == false)
            {
                aSwitch = true;
                if (aState == false)
                {
                    xState = false;
                    yState = false;
                    aState = true;
                    bState = false;
                }
                else
                {
                    aState = false;
                }
            }
            if (buttonA == false && aSwitch == true)
            {
                aSwitch = false;
            }

            if (buttonB == true && bSwitch == false)
            {
                bSwitch = true;
                if (bState == false)
                {
                    xState = false;
                    yState = false;
                    aState = false;
                    bState = true;
                }
                else
                {
                    bState = false;
                }
            }
            if (buttonB == false && bSwitch == true)
            {
                bSwitch = false;
            }
        }
        public void MotionSave()
        {
            if (output != 0)
            {
                if (buttonX == true)
                {
                    xSave = output;
                }
                else if (buttonY == true)
                {
                    ySave = output;
                }
                else if (buttonA == true)
                {
                    aSave = output;
                }
                else if (buttonB == true)
                {
                    bSave = output;
                }
            }
        }
        public void MotionOutput()
        {
            if (xState == true)
            {
                output = xSave;
            }
            else if (yState == true)
            {
                output = ySave;
            }
            else if (aState == true)
            {
                output = aSave;
            }
            else if (bState == true)
            {
                output = bSave;
            }
        }

        public void Piston()
        {
            if (yAxisB != 0)
            {
                PistonAction();
            }
            else
            {
                PistonControl();
            }
            pcm.SetSolenoidOutput(0, pistonState);
            pcm.SetSolenoidOutput(1, !pistonState);
        }
        public void PistonAction()
        {
            tSleep = 1000 - (int) System.Math.Round(yAxisB * 1000);
            if (tSleep < 80)
            {
                tSleep = 80;
            }
            if (pistonState == true)
            {
                pistonState = false;
            }
            else
            {
                pistonState = true;
            }
        }
        public void PistonControl()
        {
            if (buttonRight == true && pistonSwitch == false)
            {
                pistonSwitch = true;
                if (pistonState == true)
                {
                    pistonState = false;
                }
                else
                {
                    pistonState = true;
                }
            }
            if (buttonRight == false && pistonSwitch == true)
            {
                pistonSwitch = false;
            }
        }

        public void Display()
        {
            if (gamepad.GetConnectionStatus() == UsbDeviceConnection.NotConnected)
            {
                if (displayMode != -1)
                {
                    display.Clear();
                    displayModeRefresh = false;
                }
                displayMode = -1;
            }
            else {
                if (displayMode == -1)
                {
                    display.Clear();
                    displayMode = 0;
                    displayModeRefresh = false;
                }
                if (toggleRight == true && displaySwitchRight == false)
                {
                    displaySwitchRight = true;
                    display.Clear();
                    displayMode += 1;
                    displayModeRefresh = false;
                }
                if (toggleRight == false && displaySwitchRight == true)
                {
                    displaySwitchRight = false;
                }
                if (toggleLeft == true && displaySwitchLeft == false)
                {
                    displaySwitchLeft = true;
                    display.Clear();
                    displayMode += 4;
                    displayModeRefresh = false;
                }
                if (toggleLeft == false && displaySwitchLeft == true)
                {
                    displaySwitchLeft = false;
                }
                displayMode %= 5;
            }
            
            switch (displayMode)
            {
                case -1:
                    DisplayInit();
                    break;
                case 0:
                    DisplayOutput();
                    break;
                case 1:
                    DisplayMotion();
                    break;
                case 2:
                    DisplayValues();
                    break;
                case 3:
                    DisplaySettings();
                    break;
                case 4:
                    DisplayPiston();
                    break;
            }
        }
        public void DisplayInit()
        {
            if (displayModeRefresh == false) { 
                image = display.AddResourceImageSprite(Properties.Resources.ResourceManager, Properties.Resources.BinaryResources.img, Bitmap.BitmapImageType.Jpeg, 44, 16);
                title = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 36, 99, 100, 30);
                title.SetText("TAS Robotics");
                displayModeRefresh = true;
            }
        }
        public void DisplayOutput()
        {
            if (displayModeRefresh == false)
            {
                title = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 34, 17, 120, 15);
                label1 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 80, 56, 80, 15);
                label2 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 80, 76, 80, 15);
                label3 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 80, 96, 80, 15);
                rect1 = display.AddRectSprite(DisplayModule.Color.White, 20, 55, 18, 55);
                rect2 = display.AddRectSprite(DisplayModule.Color.White, 47, 55, 18, 55);
                displayModeRefresh = true;
            }
            if (title == null || label1 == null || label2 == null || label3 == null || rect1 == null || rect2 == null)
            {
                display.Clear();
                title = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 34, 17, 120, 15);
                label1 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 80, 55, 80, 15);
                label2 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 80, 75, 80, 15);
                label3 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 80, 95, 80, 15);
                rect1 = display.AddRectSprite(DisplayModule.Color.White, 20, 55, 18, 55);
                rect2 = display.AddRectSprite(DisplayModule.Color.White, 47, 55, 18, 55);
            }

            string colorStringX = "0x00";
            string colorStringY = "0x00";
            string redX, blueX, greenX;
            string redY, blueY, greenY;
            int colorIntX = (int)System.Math.Round(xAxisA * 100);
            int colorIntY = (int)System.Math.Round(yAxisA * 100);
            if (colorIntX > 0)
            {
                blueX = (100 - colorIntX).ToString("X2");
                greenX = "ff";
                redX = (100 - colorIntX).ToString("X2");
            }
            else if (colorIntX < 0)
            {
                blueX = (100 + colorIntX).ToString("X2");
                greenX = (100 + colorIntX).ToString("X2");
                redX = "ff";
            }
            else
            {
                blueX = "ff";
                greenX = "ff";
                redX = "ff";
            }
            if (colorIntY > 0)
            {
                blueY = (100 - colorIntY).ToString("X2");
                greenY = "ff";
                redY = (100 - colorIntY).ToString("X2");
            }
            else if (colorIntY < 0)
            {
                blueY = (100 + colorIntY).ToString("X2");
                greenY = (100 + colorIntY).ToString("X2");
                redY = "ff";
            }
            else
            {
                blueY = "ff";
                greenY = "ff";
                redY = "ff";
            }
            colorStringX = colorStringX + blueX + greenX + redX;
            colorStringY = colorStringY + blueY + greenY + redY;
            rect1.SetColor((DisplayModule.Color)Convert.ToInt32(colorStringX, 16));
            rect2.SetColor((DisplayModule.Color)Convert.ToInt32(colorStringY, 16));

            label1.SetText("X: " + xAxisA.ToString());
            label2.SetText("Y: " + yAxisA.ToString());
            label3.SetText("Angle: " + ((int)System.Math.Round(angle)).ToString());
            title.SetText("Motor Control");
        }
        public void DisplayMotion()
        {
            if (displayModeRefresh == false)
            {
                display.Clear();
                title = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 38, 17, 120, 15);
                label1 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 20, 72, 130, 15);
                label2 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 20, 92, 130, 15);
                rect1 = display.AddRectSprite(DisplayModule.Color.White, 20, 52, 120, 10);
                displayModeRefresh = true;
            }
            if (title == null || label1 == null || label2 == null || rect1 == null)
            {
                display.Clear();
                title = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 38, 17, 120, 15);
                label1 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 20, 72, 130, 15);
                label2 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 20, 92, 130, 15);
                rect1 = display.AddRectSprite(DisplayModule.Color.White, 20, 52, 120, 10);
            }
            string colorString = "0x00";
            string red, blue, green;
            int colorInt = (int) System.Math.Round(output/4096/analog0 * 100);
            if (colorInt > 0)
            {
                blue = (100 - colorInt).ToString("X2");
                green = "ff";
                red = (100 - colorInt).ToString("X2");
            }
            else if (colorInt < 0)
            {
                blue = (100 + colorInt).ToString("X2");
                green = (100 + colorInt).ToString("X2");
                red = "ff";
            }
            else
            {
                blue = "ff";
                green = "ff";
                red = "ff";
            }
            colorString = colorString + blue + green + red;

            rect1.SetColor((DisplayModule.Color) Convert.ToInt32(colorString, 16));
            label1.SetText("Ticks: " + ((int) System.Math.Round(output)).ToString());
            label2.SetText("Rotations: " + ((double) (output/4096)).ToString());
            title.SetText("Motion Magic");
        }
        public void DisplayValues()
        {
            if (displayModeRefresh == false)
            {
                title = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 37, 17, 120, 15);
                label1 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 15, 44, 120, 15);
                label2 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 15, 64, 120, 15);
                label3 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 15, 84, 120, 15); 
                label4 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 15, 104, 120, 15);
                rect1 = display.AddRectSprite(DisplayModule.Color.White, 128, 44, 15, 15);
                rect2 = display.AddRectSprite(DisplayModule.Color.White, 128, 64, 15, 15);
                rect3 = display.AddRectSprite(DisplayModule.Color.White, 128, 84, 15, 15);
                rect4 = display.AddRectSprite(DisplayModule.Color.White, 128, 104, 15, 15);
                displayModeRefresh = true;
            }
            if (title == null || label1 == null || label2 == null || label3 == null || label4 == null || rect1 == null || rect2 == null || rect3 == null || rect4 == null)
            {
                display.Clear();
                title = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 37, 17, 120, 15);
                label1 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 15, 44, 120, 15);
                label2 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 15, 64, 120, 15);
                label3 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 15, 84, 120, 15); 
                label4 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 15, 104, 120, 15);
                rect1 = display.AddRectSprite(DisplayModule.Color.White, 128, 44, 15, 15);
                rect2 = display.AddRectSprite(DisplayModule.Color.White, 128, 64, 15, 15);
                rect3 = display.AddRectSprite(DisplayModule.Color.White, 128, 84, 15, 15);
                rect4 = display.AddRectSprite(DisplayModule.Color.White, 128, 104, 15, 15);
            }

            if (xState == true)
            {
                rect1.SetColor(DisplayModule.Color.Green);
            }
            else
            {
                rect1.SetColor(DisplayModule.Color.White);
            }
            if (yState == true)
            {
                rect2.SetColor(DisplayModule.Color.Green);
            }
            else
            {
                rect2.SetColor(DisplayModule.Color.White);
            }
            if (aState == true)
            {
                rect3.SetColor(DisplayModule.Color.Green);
            }
            else
            {
                rect3.SetColor(DisplayModule.Color.White);
            }
            if (bState == true)
            {
                rect4.SetColor(DisplayModule.Color.Green);
            }
            else
            {
                rect4.SetColor(DisplayModule.Color.White);
            }

            label1.SetText("Button X: " + ((int) System.Math.Round(xSave)).ToString());
            label2.SetText("Button Y: " + ((int) System.Math.Round(ySave)).ToString());
            label3.SetText("Button B: " + ((int) System.Math.Round(bSave)).ToString());
            label4.SetText("Button A: " + ((int) System.Math.Round(aSave)).ToString());
            title.SetText("Motion Values");
        }
        public void DisplaySettings()
        {
            if (displayModeRefresh == false)
            {
                title = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 28, 17, 120, 15);
                label1 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 14, 52, 140, 15);
                label2 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 14, 72, 140, 15);
                label3 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 14, 92, 140, 15);
                displayModeRefresh = true;
            }
            if (title == null || label1 == null || label2 == null || label3 == null)
            {
                display.Clear();
                title = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 28, 17, 120, 15);
                label1 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 14, 52, 140, 15);
                label2 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 14, 72, 140, 15);
                label3 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 14, 92, 140, 15);
            }

            label1.SetText("Max Rotations: " + analog0.ToString());
            label2.SetText("Velocity: " + analog1.ToString());
            label3.SetText("Acceleration: " + analog2.ToString());
            title.SetText("Motion Settings");
        }
        public void DisplayPiston()
        {
            if (displayModeRefresh == false)
            {
                title = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 36, 17, 120, 15);
                label1 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 70, 56, 80, 15);
                label2 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 70, 76, 80, 15);
                label3 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 70, 96, 80, 15);
                rect1 = display.AddRectSprite(DisplayModule.Color.White, 10, 55, 18, 55);
                rect2 = display.AddRectSprite(DisplayModule.Color.White, 37, 55, 18, 55);
                displayModeRefresh = true;
            }
            if (title == null || label1 == null || label2 == null || label3 == null || rect1 == null || rect2 == null)
            {
                display.Clear();
                title = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 36, 17, 120, 15);
                label1 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 70, 56, 80, 15);
                label2 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 70, 76, 80, 15);
                label3 = display.AddLabelSprite(ninaB, DisplayModule.Color.White, 70, 96, 80, 15);
                rect1 = display.AddRectSprite(DisplayModule.Color.White, 10, 55, 18, 55);
                rect2 = display.AddRectSprite(DisplayModule.Color.White, 37, 55, 18, 55);
            }

            if (pistonState == true)
            {
                rect1.SetColor(DisplayModule.Color.Blue);
                label1.SetText("State: Out");
            }
            else
            {
                rect1.SetColor(DisplayModule.Color.White);
                label1.SetText("State: In");
            }

            string colorString = "0x00";
            string red, blue, green;
            int colorInt = (int) System.Math.Round(yAxisB * 100);
            if (colorInt > 0)
            {
                blue = (100 - colorInt).ToString("X2");
                green = "ff";
                red = (100 - colorInt).ToString("X2");
            }
            else if (colorInt < 0)
            {
                blue = (100 + colorInt).ToString("X2");
                green = (100 + colorInt).ToString("X2");
                red = "ff";
            }
            else
            {
                blue = "ff";
                green = "ff";
                red = "ff";
            }
            colorString = colorString + blue + green + red;
            rect2.SetColor((DisplayModule.Color) Convert.ToInt32(colorString, 16));

            label2.SetText("Speed: " + (int) System.Math.Round(yAxisB * 1000));
            label3.SetText("Sleep: " + tSleep.ToString() + "ms");
            title.SetText("Pneumatic Control");
        }
    }
}
