using System;
using Microsoft.SPOT;
using System.Threading;
using Microsoft.SPOT.Hardware;
using GHIElectronics.NETMF.Hardware;
using GHIElectronics.NETMF.FEZ;



namespace MFConsoleApplication1
{

    public enum SubCommand
    {
        SETALL = 0,
        SETNUMBOARDS = 1,
        SCROLLX = 2,
        SCROLLY = 3,
        SETFORECOLOR = 4,//       uint[3] color
        SETBACKCOLOR = 5,//       uint[3] color
        CLEAR = 6,       //       -
        DRAWPOINT = 7,   //       int x, int y
        DRAWCHAR = 8,    //       int x, int y, char c
        DRAWROW = 9,     //       uint row, uint data
        DRAWCOL = 10,    //       uint col, uint data
        DRAWALL = 11,    //       uint[8] data
        SHIFTLEFT = 12,
        SHIFTRIGHT = 13,
        DRAWLINEV = 14,
        DRAWLINEH = 15,
        CLEARPOINT = 16
    }

    class Color
    {
        byte _red;
        byte _green;
        byte _blue;

        public Color(byte red, byte green, byte blue)
        {
            _red = red;
            _green = green;
            _blue = blue;
        }

        public byte Red() 
        {
            return _red;
        }

        public byte Green() 
        {
            return _green;
        }

        public byte Blue() 
        {
            return _blue;
        }

        public byte[] GetColorValue()
        {
            byte[] value = { _red, _green, _blue };           
            return value;
        }

        public byte[] GetColorValuePacked()
        {

            byte g1 = (byte)(_red | ((byte)(((byte)(_green & 0x7)) << 0x5)));
            byte g2 = (byte)((_blue << 0x2) | ((byte)(((byte)(_green & 0x18)) >> 0x3)));

            byte[] value = { g1, g2 };

            return value;
        }

        public static Color ColorWhite()
        {
            return new Color(0xFF,0xFF,0xFF);
        }

        public static Color ColorBlack()
        {
            return new Color(0x00, 0x00, 0x00);
        }

        public static Color ColorRed()
        {
            return new Color(0xFF, 0x00, 0x00);
        }

        public static Color ColorGreen()
        {
            return new Color(0x00, 0xFF, 0x00);
        }

        public static Color ColorBlue()
        {
            return new Color(0x00, 0x00, 0xFF);
        }

        public void StartSpectrum(int type)
        {
            byte minimumValue = 0;
            byte maximumValue = 31;

            _red = maximumValue;
            _green = minimumValue;
            _blue = minimumValue;

        }

        public void ShiftSpectrum(byte stepValue)
        {
            byte minimumValue = 0;
            byte maximumValue = 30;
        
            if      ( (_red == maximumValue) && (_green == minimumValue) && (_blue <  maximumValue)) _blue += stepValue;
            else if ((_red > minimumValue) && (_green == minimumValue) && (_blue == maximumValue)) _red -= stepValue;
            else if ((_red == minimumValue) && (_green < maximumValue) && (_blue == maximumValue)) _green += stepValue;
            else if ((_red == minimumValue) && (_green == maximumValue) && (_blue > minimumValue)) _blue -= stepValue;
            else if ((_red < maximumValue) && (_green == maximumValue) && (_blue == minimumValue)) _red += stepValue;
            else if ((_red == maximumValue) && (_green > minimumValue) && (_blue == minimumValue)) _green -= stepValue;
            else
            {
                _red = maximumValue;
                _green = minimumValue;
                _blue = maximumValue;
            }
            


        }

    }

    public enum Selectors 
    {
        A,
        B,
        C
    }

    public enum Dials
    {
        R,
        G,
        B
    }

    public delegate void SelectorChange(Selectors id, int newPosition);

    public class Selector
    {
        AnalogIn.Pin inputPin;
        int currentPosition;
        AnalogIn selectorPort;
        Selectors selectorId;

        public SelectorChange OnChange;

        public Selector(Selectors id, AnalogIn.Pin analogPin)
            : base()
        {
            inputPin = analogPin;
            selectorId = id;

            selectorPort = new AnalogIn(inputPin);
            selectorPort.SetLinearScale(0, 3300);
            currentPosition = 0;

        }

        public int Read()
        {

            int tmpVal1 = selectorPort.Read();
            int tmpVal2 = selectorPort.Read();
            int tmpVal3 = selectorPort.Read();

            int tmpVal = ((tmpVal1 + tmpVal2 + tmpVal3) / 3);
            int tmpPos = GetSelectorPosition(selectorId, tmpVal);
           
            if (tmpPos != currentPosition)
            {
                currentPosition = tmpPos;
               // Debug.Print("Selector " + selectorId + " Value: " + tmpVal + " Pos: " + currentPosition);
                if (OnChange != null)
                {
                    OnChange(selectorId, currentPosition);
                }
            }

            return tmpPos;
        }

        static int GetSelectorPosition(Selectors selectorid, int voltage)
        {
            if (selectorid == Selectors.A)
            {
                    Debug.Print("Selector " + selectorid + " Value: " + voltage);

                if (voltage <  115) return 1;
                if (voltage <  230) return 2;
                if (voltage <  350) return 3;
                if (voltage <  450) return 4;
                if (voltage <  840) return 5;
                if (voltage < 1470) return 6;
                if (voltage < 1880) return 7;
                if (voltage < 2340) return 8;
                if (voltage < 2800) return 9;
                if (voltage < 3000) return 10;


                return 11;

            }
            else if (selectorid == Selectors.B)
            {

                if (voltage < 150) return 1;
                if (voltage < 250) return 2;
                if (voltage < 350) return 3;
                if (voltage < 450) return 4;
                if (voltage < 800) return 5;
                if (voltage < 1450) return 6;
                if (voltage < 1850) return 7;
                if (voltage < 2250) return 8;
                if (voltage < 2650) return 9;
                if (voltage < 2850) return 10;
                return 11;
                

            }
            else if (selectorid == Selectors.C)
            {

                if (voltage < 200) return 7;
                if (voltage < 350) return 8;
                if (voltage < 450) return 9;
                if (voltage < 650) return 10;
                if (voltage < 1150) return 11;

                if (voltage < 1850) return 6;

                if (voltage < 2250) return 1;
                if (voltage < 2650) return 2;
                if (voltage < 3050) return 3;
                if (voltage < 3180) return 4;
                
                return 5;


            }
            return voltage;
        }
    
    }


    public enum DialMode
    {
        BarTop = 0,
        BarBottom = 1,
        OneFree = 2,
        ThreeFree = 3,
        Pan = 4,
        AUT = 16,
        ANI = 32,
        CYC = 64,
        RST = 128
    }

    public delegate void DialChanged(Dials DialId, byte newPosition);

    public class Dial
    {
        Cpu.Pin pinInt;
        InterruptPort intPort;
        //int i2cAddress;

        //I2CDevice.I2CTransaction[] readActions;

        byte currentValue;

        I2CDevice.Configuration i2con;

        public DialChanged OnChange;
        Dials dialId;

       // byte[] RegisterNum;
       // byte[] RegisterValue;

        // I2CDevice MyI2C;

        public Dial(Dials id, Cpu.Pin interruptPin, ushort i2cAddress) : base()
        {

            dialId = id;
            pinInt = interruptPin;

            //MyI2C = i2cdev;

            

            i2con = new I2CDevice.Configuration(i2cAddress, 100);


            // the pin will generate interrupt on high and low edges
            intPort = new InterruptPort(pinInt,
                                     true,
                                     Port.ResistorMode.PullUp,
                                     Port.InterruptMode.InterruptEdgeHigh);

            // add an interrupt handler to the pin
            intPort.OnInterrupt += new NativeEventHandler(OnInterrupt);

        }

        void OnInterrupt(uint port, uint state, DateTime time)
        {
            Read();
           
        }

        public void Init()
        {

            I2CDevice MyI2C = new I2CDevice(i2con);

            //create transactions (we need 2 in this example)
            I2CDevice.I2CTransaction[] initActions = new I2CDevice.I2CTransaction[1];

            // create write buffer (we need one byte)
            //initActions[0] = MyI2C.CreateWriteTransaction(new byte[] { 0, (byte)(DialMode.AUT | DialMode.CYC | DialMode.OneFree) });
            I2CDevice.CreateWriteTransaction(new byte[] { 0, (byte)(DialMode.AUT | DialMode.CYC | DialMode.OneFree) });
            int result = MyI2C.Execute(initActions, 1000);
            //Debug.Print("Device Init Result " + result);


            MyI2C.Dispose();


        }

        public void SetValue(byte newValue)
        {

            I2CDevice MyI2C = new I2CDevice(i2con);

            //create transactions (we need 2 in this example)
            I2CDevice.I2CTransaction[] setActions = new I2CDevice.I2CTransaction[1];

            // create write buffer (we need one byte)
            byte[] RegisterNum = new byte[2] { 2, newValue };
            setActions[0] = I2CDevice.CreateWriteTransaction(RegisterNum);

            MyI2C.Execute(setActions, 1000);

            MyI2C.Dispose();
        
        }

        public void Read()
        {
            I2CDevice MyI2C = new I2CDevice(i2con);

            //create transactions (we need 2 in this example)
            I2CDevice.I2CTransaction[] readActions = new I2CDevice.I2CTransaction[2];

            // create write buffer (we need one byte)
            byte[] RegisterNum = new byte[1] { 2 };
            readActions[0] = I2CDevice.CreateWriteTransaction(RegisterNum);

            // create read buffer to read the register
            byte[] RegisterValue = new byte[1];
            readActions[1] = I2CDevice.CreateReadTransaction(RegisterValue);


            // Now we access the I2C bus and timeout in one second if no responce
            MyI2C.Execute(readActions, 1000);

            if (currentValue != RegisterValue[0])
            {
                currentValue = RegisterValue[0];

                if (OnChange != null)
                {
                    OnChange(dialId, currentValue);
                }
            }

            MyI2C.Dispose();

            //Debug.Print("Register value: " + RegisterValue[0].ToString());
        }


    }

    public delegate void SomeInterruptHappened(int interruptno);

    public class Program
    {
        /*
        static Cpu.Pin pinIntR = (Cpu.Pin)FEZ_Pin.Interrupt.Di5;
        static Cpu.Pin pinIntG = (Cpu.Pin)FEZ_Pin.Interrupt.Di7;
        static Cpu.Pin pinIntB = (Cpu.Pin)FEZ_Pin.Interrupt.Di6;
        */
        static AnalogIn.Pin pinSelectorA = (AnalogIn.Pin)FEZ_Pin.AnalogIn.An0;
        static AnalogIn.Pin pinSelectorB = (AnalogIn.Pin)FEZ_Pin.AnalogIn.An1;
        static AnalogIn.Pin pinSelectorC = (AnalogIn.Pin)FEZ_Pin.AnalogIn.An2;
        /*
        static InterruptPort intR;
        static InterruptPort intG;
        static InterruptPort intB;
        */
        static Selector [] selectors = new Selector[3];
        // static Dial[] dials = new Dial[3];

        // static I2CDevice.Configuration i2con;
        // static I2CDevice i2c;
        

        public static void Heartbeat()
        {

            sbyte dirr = 1;
            byte duty = 40;
            PWM pwm = new PWM((PWM.Pin)FEZ_Pin.PWM.LED);
            while (true)
            {
                pwm.Set(10000, duty);
                duty = (byte)(duty + dirr);
                if (duty > 98 || duty < 2)
                {
                    dirr *= -1;
                }
                Thread.Sleep(10);
            }

        }

        static void OnSelectorChange(Selectors id, int newPosition)
        {

        }

        static void OnDialChanged(Dials DialId, byte newPosition)
        {

        }

        static void SomeChange(uint data1, uint data2, DateTime time)
        {
            Debug.Print("Some Interrupt Event");
        }


        byte[] currentBackColor = {0x00, 0x00, 0x00};
        byte[] currentForeColor = {0xFF, 0xFF, 0xFF};

        static SPI.Configuration _spi_config;
        static SPI _spi;

        static void SendData(byte[] data)
        {
            byte[] rx_data = new byte[data.Length];

            _spi.WriteRead(data, rx_data);

            rx_data = null;

        }

        static void Execute(SubCommand command, byte[] data)
        {
            int length = data.Length + 2;

            byte[] tx_data = new byte[length];
            byte[] rx_data = new byte[length];

            tx_data[0] = 42;
            tx_data[1] = (byte)command;

            for (int i = 0; i < data.Length; i++)
            {
                tx_data[i + 2] = data[i];
            }

            _spi.WriteRead(tx_data, rx_data); 

           
        }

        static void Execute(SubCommand command)
        {
            int length = 3;

            byte[] tx_data = new byte[length];
            byte[] rx_data = new byte[length];

            tx_data[0] = 42;
            tx_data[1] = (byte)command;
            tx_data[2] = 0;

            _spi.WriteRead(tx_data, rx_data);

        }

        static void SetColorNumber(byte colorid)
        {

            switch (colorid)
            {
                case 1:
                    Execute(SubCommand.SETFORECOLOR, Color.ColorBlack().GetColorValue());
                    Execute(SubCommand.SETBACKCOLOR, Color.ColorWhite().GetColorValue());
                    break;

                case 2:
                    Execute(SubCommand.SETFORECOLOR, Color.ColorBlack().GetColorValue());
                    Execute(SubCommand.SETBACKCOLOR, Color.ColorWhite().GetColorValue());
                    break;

                case 3:
                    Execute(SubCommand.SETFORECOLOR, Color.ColorBlack().GetColorValue());
                    Execute(SubCommand.SETBACKCOLOR, Color.ColorWhite().GetColorValue());
                    break;

                case 4:
                    Execute(SubCommand.SETFORECOLOR, Color.ColorBlack().GetColorValue());
                    Execute(SubCommand.SETBACKCOLOR, Color.ColorWhite().GetColorValue());
                    break;

                case 5:
                    Execute(SubCommand.SETFORECOLOR, Color.ColorBlack().GetColorValue());
                    Execute(SubCommand.SETBACKCOLOR, Color.ColorWhite().GetColorValue());
                    break;

                case 6:
                    Execute(SubCommand.SETFORECOLOR, Color.ColorBlack().GetColorValue());
                    Execute(SubCommand.SETBACKCOLOR, Color.ColorWhite().GetColorValue());
                    break;

                case 7:
                    Execute(SubCommand.SETFORECOLOR, Color.ColorBlack().GetColorValue());
                    Execute(SubCommand.SETBACKCOLOR, Color.ColorWhite().GetColorValue());
                    break;

                case 8:
                    Execute(SubCommand.SETFORECOLOR, Color.ColorBlack().GetColorValue());
                    Execute(SubCommand.SETBACKCOLOR, Color.ColorWhite().GetColorValue());
                    break;

                case 9:
                    Execute(SubCommand.SETFORECOLOR, Color.ColorBlack().GetColorValue());
                    Execute(SubCommand.SETBACKCOLOR, Color.ColorWhite().GetColorValue());
                    break;

                case 10:
                    Execute(SubCommand.SETFORECOLOR, Color.ColorBlack().GetColorValue());
                    Execute(SubCommand.SETBACKCOLOR, Color.ColorWhite().GetColorValue());
                    break;

                case 11:
                    Execute(SubCommand.SETFORECOLOR, Color.ColorBlack().GetColorValue());
                    Execute(SubCommand.SETBACKCOLOR, Color.ColorWhite().GetColorValue());
                    break;

            }

        }

        static void DisplayImage(Resources.BinaryResources resid)
        {
            byte[] data = Resources.GetBytes(resid);
            SendData(data);
        }

        /// <summary>
        /// Abstract base class for display animations
        /// </summary>
        class DisplayRoutine
        {
            protected int _setting1;
            protected int _setting2;
            protected int _phase;
            protected int _phasecount;
            protected bool _running;

            protected DisplayRoutine()
            {
                _setting1 = 0;
                _setting2 = 0;
                _phasecount = 1;
                _phase = 0;
                _running = false;
            }

            public virtual void UpdateSettings(int setting1, int setting2)
            {
                _setting1 = setting1;
                _setting2 = setting2;
                
            }

            public virtual void Start(int setting1, int setting2)
            {
                _setting1 = setting1;
                _setting2 = setting2;
                _phase = 0;
                _running = true;
            }

            public virtual void Stop()
            {
                _running = false;
            }

            public virtual void Next()
            {
                if (_running)
                {
                    if (_phase < _phasecount) _phase++;
                    else _phase = 0;

                    if (_phase == _phasecount) _phase = 0; 
                }
            }

            public virtual int Display()
            {
                return 0;
            }

        }

        /// <summary>
        /// Display stored images for a fixed time
        /// </summary>
        class DisplayRoutineImages : DisplayRoutine
        {
            public DisplayRoutineImages() : base() {
                _phasecount = 8;
            }

            public override int Display()
            {

                if (_phase == 0) DisplayImage(Resources.BinaryResources.baum_1);
                else if (_phase == 1) DisplayImage(Resources.BinaryResources.welle_1);
                else if (_phase == 2) DisplayImage(Resources.BinaryResources.sonne_1);
                else if (_phase == 3) DisplayImage(Resources.BinaryResources.mann_1);
                else if (_phase == 4) DisplayImage(Resources.BinaryResources.fisch_1);
                else if (_phase == 5) DisplayImage(Resources.BinaryResources.schlange_1);
                else if (_phase == 6) DisplayImage(Resources.BinaryResources.space_invader_1);
                else if (_phase == 7) DisplayImage(Resources.BinaryResources.pfeil_rechts_1);

                return 600;
            }
        }


        /// <summary>
        /// Display alphabet character in fixed foreground / background color
        /// </summary>
        class DisplayRoutineAlphabet : DisplayRoutine
        {

            Color FontColor;

            public DisplayRoutineAlphabet()
                : base()
            {
                _phasecount = (90-65);
                FontColor = new Color(0,0,0);
            }

            public override void Start(int setting1, int setting2)
            {
                base.Start(setting1, setting2);
                FontColor.StartSpectrum(1);
                Execute(SubCommand.SETBACKCOLOR, Color.ColorBlack().GetColorValue());
            }

            public override void Next()
            {
                base.Next();
                FontColor.ShiftSpectrum(2);
            }

            public override int Display()
            {                    
                Execute(SubCommand.SETFORECOLOR, FontColor.GetColorValue());
                Execute(SubCommand.CLEAR);
                Execute(SubCommand.DRAWCHAR, new byte[] { 1, 0, (byte)(65 + _phase) });

                return 500;
            }

        }

        /// <summary>
        /// Display number character in fixed foreground / background color
        /// </summary>
        class DisplayRoutineNumbers : DisplayRoutine
        {

            Color FontColor;

            public DisplayRoutineNumbers()
                : base()
            {
                _phasecount = (57 - 47);
                FontColor = new Color(0, 0, 0);
            }

            public override void Start(int setting1, int setting2)
            {
                base.Start(setting1, setting2);
                FontColor.StartSpectrum(1);
                Execute(SubCommand.SETBACKCOLOR, Color.ColorBlack().GetColorValue());
            }

            public override void Next()
            {
                base.Next();
                FontColor.ShiftSpectrum(2);
            }

            public override int Display()
            {
                Execute(SubCommand.SETFORECOLOR, FontColor.GetColorValue());
                Execute(SubCommand.CLEAR);
                Execute(SubCommand.DRAWCHAR, new byte[] { 1, 0, (byte)(48 + _phase) });

                return 500;
            }

        }

        /// <summary>
        /// Displays a shifting range of colors 
        /// </summary>
        class DisplayRoutineColorshifter : DisplayRoutine
        {

            Color currentColor;

            public DisplayRoutineColorshifter()
                : base()
            {
                _phasecount = 16000;
                currentColor = new Color(0, 0, 0);
            }

            public override void Next()
            {
                base.Next();
                currentColor.ShiftSpectrum(2);
            }

            public override int Display()
            {

                Execute(SubCommand.SHIFTLEFT, currentColor.GetColorValue());

                return 18;
            }

        }

        /// <summary>
        /// Displays a game of pong
        /// </summary>
        class DisplayRoutinePong : DisplayRoutine
        {

        }

        /// <summary>
        /// Displays a scrolling text
        /// </summary>
        class DisplayRoutineTextScrollYuki : DisplayRoutine
        {

            Color FontColor;

            public DisplayRoutineTextScrollYuki()
                : base()
            {
                _phasecount = 40;
                FontColor = new Color(0,0,0);
            }

            public override void Start(int setting1, int setting2)
            {
                base.Start(setting1, setting2);
                FontColor.StartSpectrum(1);
                Execute(SubCommand.SETBACKCOLOR, Color.ColorBlack().GetColorValue());
            }

            public override void Next()
            {
                base.Next();
                FontColor.ShiftSpectrum(2);
            }

            public override int Display()
            {                    
                Execute(SubCommand.SETFORECOLOR, FontColor.GetColorValue());
                Execute(SubCommand.CLEAR);
                Execute(SubCommand.DRAWCHAR, new byte[] { (byte)(8  - _phase), 0, (byte)('Y') });
                Execute(SubCommand.DRAWCHAR, new byte[] { (byte)(16 - _phase), 0, (byte)('U') });
                Execute(SubCommand.DRAWCHAR, new byte[] { (byte)(24 - _phase), 0, (byte)('K') });
                Execute(SubCommand.DRAWCHAR, new byte[] { (byte)(32 - _phase), 0, (byte)('I') });

                return 600;
            }
    
        }

        /// <summary>
        /// Displays a scrolling text
        /// </summary>
        class DisplayRoutineTextScrollAnton : DisplayRoutine
        {

            Color FontColor;

            public DisplayRoutineTextScrollAnton()
                : base()
            {
                _phasecount = 48;
                FontColor = new Color(0, 0, 0);
            }

            public override void Start(int setting1, int setting2)
            {
                base.Start(setting1, setting2);
                FontColor.StartSpectrum(1);
                Execute(SubCommand.SETBACKCOLOR, Color.ColorBlack().GetColorValue());
            }

            public override void Next()
            {
                base.Next();
                FontColor.ShiftSpectrum(2);
            }

            public override int Display()
            {
                Execute(SubCommand.SETFORECOLOR, FontColor.GetColorValue());
                Execute(SubCommand.CLEAR);
                Execute(SubCommand.DRAWCHAR, new byte[] { (byte)(8 - _phase), 0, (byte)('A') });
                Execute(SubCommand.DRAWCHAR, new byte[] { (byte)(16 - _phase), 0, (byte)('N') });
                Execute(SubCommand.DRAWCHAR, new byte[] { (byte)(24 - _phase), 0, (byte)('T') });
                Execute(SubCommand.DRAWCHAR, new byte[] { (byte)(32 - _phase), 0, (byte)('O') });
                Execute(SubCommand.DRAWCHAR, new byte[] { (byte)(40 - _phase), 0, (byte)('N') });

                return 600;
            }

        }

        /// <summary>
        /// Displays moving lines
        /// </summary>
        class DisplayRoutineMovingLines : DisplayRoutine
        {
            //int _mode;

            Color currentColor;

            public DisplayRoutineMovingLines()
                : base()
            {
                _phasecount = 8;

                currentColor = new Color(0, 0, 0);
            }

            public override void Start(int setting1, int setting2)
            {
                base.Start(setting1, setting2);
                currentColor.StartSpectrum(1);
                Execute(SubCommand.SETBACKCOLOR, Color.ColorBlack().GetColorValue());
            }

            public override void Next()
            {
                base.Next();
                currentColor.ShiftSpectrum(2);
            }

            public override int Display()
            {
                if (_phase == 0) Execute(SubCommand.DRAWLINEV, new byte[] { (byte)(_phasecount - 1), 0, 0, 0 });
                else Execute(SubCommand.DRAWLINEV, new byte[] { (byte)(_phase - 1), 0, 0, 0 });
                Execute(SubCommand.DRAWLINEV, new byte[] { (byte)(_phase),currentColor.Red(),currentColor.Green(),currentColor.Blue() });
 
                return 120;
            }



        }

        /// <summary>
        /// Displays moving dot
        /// </summary>
        class DisplayRoutineMovingDot : DisplayRoutine
        {
              
            Color currentColor;
            byte x, y;
            byte x_old, y_old;


            public DisplayRoutineMovingDot()
                : base()
            {
                _phasecount = 8;

                currentColor = new Color(0, 0, 0);
            }

            public override void Start(int setting1, int setting2)
            {
                base.Start(setting1, setting2);
                currentColor.StartSpectrum(1);
                x = 1;
                y = 1;
                x_old = 1;
                y_old = 1;
                Execute(SubCommand.CLEAR);
                Execute(SubCommand.SETBACKCOLOR, Color.ColorBlack().GetColorValue());
                Execute(SubCommand.SETFORECOLOR, Color.ColorGreen().GetColorValue());
            }

            public override void Next()
            {
                base.Next();
                currentColor.ShiftSpectrum(2);
                
                System.Random rndGen = new Random();
                

                byte new_x, new_y;


                new_x = x_old;
                new_y = y_old;


                // Debug.Print("X / Y choice random: " + val.ToString());

                while ((y_old == new_y) && (x_old == new_x))
                {

                    new_x = x;
                    new_y = y;

                    int val = rndGen.Next(2);

                    if (val == 1) // MOVE IN X DIRECTION
                    {
                        if (new_x == 0) new_x++;
                        else if (new_x == 7) new_x--;
                        else
                        {
                            val = rndGen.Next(2);
                            if (val == 1) new_x++;
                            else new_x--;
                        }

                    }
                    else // MOVE IN Y DIRECTION
                    {

                        if (new_y == 0) new_y++;
                        else if (new_y == 7) new_y--;
                        else
                        {
                            val = rndGen.Next(2);
                            if (val == 1) new_y++;
                            else new_y--;
                        }

                    }

                }

                x_old = x;
                y_old = y;

                x = new_x;
                y = new_y;

                
            }

            public override int Display()
            {
                //if (_phase == 0) Execute(SubCommand.DRAWLINEV, new byte[] { (byte)(_phase), currentColor.Red(), currentColor.Green(), currentColor.Blue() });
                Execute(SubCommand.CLEARPOINT, new byte[] { (byte)x_old, (byte)y_old });
                Execute(SubCommand.DRAWPOINT, new byte[] { (byte)x, (byte)y });
 
                return 270;
            }

        }

        /// <summary>
        /// Displays snake
        /// </summary>
        class DisplayRoutineSnake : DisplayRoutine
        {

        }

        /// <summary>
        /// Displays curtains
        /// </summary>
        class DisplayRoutineCurtains : DisplayRoutine
        {



        }

        /// <summary>
        /// Displays a sequence of fixed colors by fading in and out
        /// </summary>
        class DisplayRoutineColorfader : DisplayRoutine
        {

            byte currentR = 0;
            byte currentG = 0;
            byte currentB = 0;

            public DisplayRoutineColorfader()
                : base()
            {
                _phasecount = 7;
                currentR = 0;
                currentG = 0;
                currentB = 0;
            }

            public override void Next()
            {
                if (_phase == 0)
                {
                    if (currentR == 31)
                    {
                        _phase = 1;
                    }
                    else currentR++;
                }

                if (_phase == 1)
                {
                    if (currentR == 0)
                    {
                        _phase = 2;
                    }
                    else currentR--;
                }

                if (_phase == 2)
                {
                    if (currentG == 31)
                    {
                        _phase = 3;
                    }
                    else currentG++;
                }

                if (_phase == 3)
                {
                    if (currentG == 0)
                    {
                        _phase = 4;
                    }
                    else currentG--;
                }

                if (_phase == 4)
                {
                    if (currentB == 31)
                    {
                        _phase = 5;
                    }
                    else currentB++;
                }

                if (_phase == 5)
                {
                    if (currentB == 0)
                    {
                        _phase = 6;
                    }
                    else currentB--;
                }

                if (_phase == 6)
                {
                    if (currentB == 31)
                    {
                        _phase = 7;
                    }
                    else
                    {
                        currentR++;
                        currentG++;
                        currentB++;
                    }
                }

                if (_phase == 7)
                {
                    if (currentB == 0)
                    {
                        _phase = 0;
                    }
                    else
                    {
                        currentR--;
                        currentG--;
                        currentB--;
                    }
                }

            }

            public override int Display()
            {

                Execute(SubCommand.SETALL, new byte [] {currentR, currentG, currentB});

                return 40;
            }

        }
        
        /// <summary>
        /// Main loop
        /// </summary>
        public static void Main()
        {
            // create a thread handler
            Thread MyThreadHandler;

            // create a new thread object
            // and assing to my handler
            MyThreadHandler = new Thread(Heartbeat);

            // start my new thread
            MyThreadHandler.Start();

            selectors[0] = new Selector(Selectors.A,pinSelectorA);
            selectors[0].OnChange += new SelectorChange(OnSelectorChange);

            selectors[1] = new Selector(Selectors.B, pinSelectorB);
            selectors[1].OnChange += new SelectorChange(OnSelectorChange);

            selectors[2] = new Selector(Selectors.C, pinSelectorC);
            selectors[2].OnChange += new SelectorChange(OnSelectorChange);

            _spi_config = new
                SPI.Configuration(
                        (Cpu.Pin)FEZ_Pin.Digital.Di4, 
                        false, 
                        0, 
                        0, 
                        false, 
                        true, 
                        900, 
                        SPI.SPI_module.SPI1);

            _spi = new SPI(_spi_config);
         
            DisplayRoutineAlphabet drAlphabet = new DisplayRoutineAlphabet();
            DisplayRoutineImages drImages = new DisplayRoutineImages();
            DisplayRoutineColorshifter drColorShifter = new DisplayRoutineColorshifter();
            DisplayRoutineColorfader drColorfader = new DisplayRoutineColorfader();
            DisplayRoutinePong drPong = new DisplayRoutinePong();            
            DisplayRoutineMovingLines drMovingLines = new DisplayRoutineMovingLines();

            DisplayRoutineMovingDot drMovingDot = new DisplayRoutineMovingDot();
            DisplayRoutineSnake drSnake = new DisplayRoutineSnake();
            DisplayRoutineTextScrollYuki drTextScrollYuki = new DisplayRoutineTextScrollYuki();
            DisplayRoutineTextScrollAnton drTextScrollAnton = new DisplayRoutineTextScrollAnton();
            DisplayRoutineNumbers drNumbers = new DisplayRoutineNumbers();
            DisplayRoutineCurtains drCurtains = new DisplayRoutineCurtains();

            DisplayRoutine drCurrent = drAlphabet;

            int sleepInterval = 0;

            int selectorPosition0 = 1;
            int selectorPosition1 = 1;
            int selectorPosition2 = 1;

            int selectorPositionTmp0 = 0;
            int selectorPositionTmp1 = 0;
            int selectorPositionTmp2 = 0;

            //selectorPosition0 = selectors[0].Read();
            selectorPosition1 = selectors[1].Read();
            selectorPosition2 = selectors[2].Read();

            drCurrent.Start(selectorPosition1, selectorPosition2);

            while (true)
            {
                selectorPositionTmp0 = selectors[0].Read();
                if (selectorPosition0 != selectorPositionTmp0) {

                    selectorPosition0 = selectorPositionTmp0;

                    drCurrent.Stop();

                    if (selectorPosition0 == 1) 
                    {
                        drCurrent = drAlphabet;
                    } 
                    else if (selectorPosition0 == 2) 
                    {
                        drCurrent = drColorShifter;
                    }
                    else if (selectorPosition0 == 3)
                    {
                        drCurrent = drColorfader;
                    }
                    else if (selectorPosition0 == 4)
                    {
                        drCurrent = drImages;
                    }
                    else if (selectorPosition0 == 5)
                    {
                        drCurrent = drMovingDot;
                    }
                    else if (selectorPosition0 == 6)
                    {
                        drCurrent = drMovingLines;
                    }
                    else if (selectorPosition0 == 7)
                    {
                        drCurrent = drNumbers;
                    }
                    else if (selectorPosition0 == 8)
                    {
                        drCurrent = drTextScrollYuki; 
                    }
                    else if (selectorPosition0 == 9)
                    {
                        drCurrent = drTextScrollAnton;
                    }
                    else if (selectorPosition0 == 10)
                    {
                        drCurrent = drCurtains;
                    }
                    else if (selectorPosition0 == 11)
                    {
                        drCurrent = drPong;
                    }

                    drCurrent.Start(selectorPosition1,selectorPosition2);
                }

                //selectorPositionTmp1 = selectors[1].Read();
                //selectorPositionTmp2 = selectors[2].Read();
                if ((selectorPosition1 != selectorPositionTmp1) || (selectorPosition2 != selectorPositionTmp2)) {
                    selectorPosition1 = selectorPositionTmp1;
                    selectorPosition2 = selectorPositionTmp2;
                    drCurrent.UpdateSettings(selectorPosition1,selectorPosition2);
                }

                sleepInterval = drCurrent.Display();

                Thread.Sleep(sleepInterval);

                drCurrent.Next();
                
            }


        }

    }
}
