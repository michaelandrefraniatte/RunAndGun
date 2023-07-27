using System;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Threading;
using System.Globalization;
using System.Linq;
using System.Numerics;
namespace Gun
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint TimeBeginPeriod(uint ms);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint TimeEndPeriod(uint ms);
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);
        public static uint CurrentResolution = 0;
        [DllImport("WiimotePairing.dll", EntryPoint = "connect")]
        private static extern bool connect();
        [DllImport("WiimotePairing.dll", EntryPoint = "disconnect")]
        private static extern bool disconnect();
        [DllImport("LeftJoyconPairing.dll", EntryPoint = "disconnectLeft")]
        private static extern bool disconnectLeft();
        [DllImport("LeftJoyconPairing.dll", EntryPoint = "lconnect")]
        private static extern bool lconnect();
        [DllImport("hid.dll")]
        private static extern void HidD_GetHidGuid(out Guid gHid);
        [DllImport("hid.dll")]
        private extern static bool HidD_SetOutputReport(IntPtr HidDeviceObject, byte[] lpReportBuffer, uint ReportBufferLength);
        [DllImport("setupapi.dll")]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, string Enumerator, IntPtr hwndParent, UInt32 Flags);
        [DllImport("setupapi.dll")]
        private static extern Boolean SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInvo, ref Guid interfaceClassGuid, Int32 memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);
        [DllImport("setupapi.dll")]
        private static extern Boolean SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData, UInt32 deviceInterfaceDetailDataSize, out UInt32 requiredSize, IntPtr deviceInfoData);
        [DllImport("setupapi.dll")]
        private static extern Boolean SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData, UInt32 deviceInterfaceDetailDataSize, out UInt32 requiredSize, IntPtr deviceInfoData);
        [DllImport("Kernel32.dll")]
        private static extern SafeFileHandle CreateFile(string fileName, [MarshalAs(UnmanagedType.U4)] FileAccess fileAccess, [MarshalAs(UnmanagedType.U4)] FileShare fileShare, IntPtr securityAttributes, [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition, [MarshalAs(UnmanagedType.U4)] uint flags, IntPtr template);
        [DllImport("Kernel32.dll")]
        private static extern IntPtr CreateFile(string fileName, System.IO.FileAccess fileAccess, System.IO.FileShare fileShare, IntPtr securityAttributes, System.IO.FileMode creationDisposition, EFileAttributes flags, IntPtr template);
        [DllImport("lhidread.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Lhid_read_timeout")]
        private static unsafe extern int Lhid_read_timeout(SafeFileHandle dev, byte[] data, UIntPtr length);
        [DllImport("lhidread.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Lhid_write")]
        private static unsafe extern int Lhid_write(SafeFileHandle device, byte[] data, UIntPtr length);
        [DllImport("lhidread.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Lhid_open_path")]
        private static unsafe extern SafeFileHandle Lhid_open_path(IntPtr handle);
        [DllImport("lhidread.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Lhid_close")]
        private static unsafe extern void Lhid_close(SafeFileHandle device);
        private static bool back, start, A, B, X, Y, up, left, down, right, leftstick, rightstick, leftbumper, rightbumper, lefttrigger, righttrigger;
        private static double leftstickx, leftsticky, rightstickx, rightsticky;
        private const double REGISTER_IR = 0x04b00030, REGISTER_EXTENSION_INIT_1 = 0x04a400f0, REGISTER_EXTENSION_INIT_2 = 0x04a400fb, REGISTER_EXTENSION_TYPE = 0x04a400fa, REGISTER_EXTENSION_CALIBRATION = 0x04a40020, REGISTER_MOTIONPLUS_INIT = 0x04a600fe;
        private static double irx0, iry0, irx1, iry1, irx, iry, irxc, iryc, mWSIRSensors0X, mWSIRSensors0Y, mWSIRSensors1X, mWSIRSensors1Y, mWSButtonStateIRX, mWSButtonStateIRY, mWSRawValuesX, mWSRawValuesY, mWSRawValuesZ, calibrationinit, mousex, mousey, mWSIRSensors0Xcam, mWSIRSensors0Ycam, mWSIRSensors1Xcam, mWSIRSensors1Ycam, mWSIRSensorsXcam, mWSIRSensorsYcam, viewpower1x = 0.33f, viewpower2x = 0f, viewpower3x = 0.67f, viewpower1y = 0.25f, viewpower2y = 0.75f, viewpower3y = 0f, dzx = 2.0f, dzy = 2.0f, lowsensx = 1f, lowsensy = 1f, centery = 140f;
        private static bool mWSIR1found, mWSIR0found, mWSButtonStateA, mWSButtonStateB, mWSButtonStateMinus, mWSButtonStateHome, mWSButtonStatePlus, mWSButtonStateOne, mWSButtonStateTwo, mWSButtonStateUp, mWSButtonStateDown, mWSButtonStateLeft, mWSButtonStateRight, running, Getstate;
        private static byte[] buff = new byte[] { 0x55 }, mBuff = new byte[22], aBuffer = new byte[22];
        private const byte Type = 0x12, IR = 0x13, WriteMemory = 0x16, ReadMemory = 0x16, IRExtensionAccel = 0x37;
        private static FileStream mStream;
        private static SafeFileHandle handle = null;
        public static bool closed = false;
        private static int[] wd = { 2 };
        private static int[] wu = { 2 };
        public static void valchanged(int n, bool val)
        {
            if (val)
            {
                if (wd[n] <= 1)
                {
                    wd[n] = wd[n] + 1;
                }
                wu[n] = 0;
            }
            else
            {
                if (wu[n] <= 1)
                {
                    wu[n] = wu[n] + 1;
                }
                wd[n] = 0;
            }
        }
        private void Form1_Shown(object sender, EventArgs e)
        {
            TimeBeginPeriod(1);
            NtSetTimerResolution(1, true, ref CurrentResolution);
            AppDomain.CurrentDomain.UnhandledException += new System.UnhandledExceptionEventHandler(AppDomain_UnhandledException);
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            Task.Run(() => Start());
            Task.Run(() => LSPControl.Connect());
        }
        public static void AppDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            CloseControl();
        }
        public static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            CloseControl();
        }
        private void Start()
        {
            running = true;
            connectingWiimote();
            Task.Run(() => taskD());
            connectingJoyconLeft();
            Task.Run(() => taskDLeft());
            Thread.Sleep(3000);
            calibrationinit = -aBuffer[4] + 135f;
            stick_rawLeft[0] = report_bufLeft[6 + (ISLEFT ? 0 : 3)];
            stick_rawLeft[1] = report_bufLeft[7 + (ISLEFT ? 0 : 3)];
            stick_rawLeft[2] = report_bufLeft[8 + (ISLEFT ? 0 : 3)];
            stick_calibrationLeft[0] = (UInt16)(stick_rawLeft[0] | ((stick_rawLeft[1] & 0xf) << 8));
            stick_calibrationLeft[1] = (UInt16)((stick_rawLeft[1] >> 4) | (stick_rawLeft[2] << 4));
            acc_gcalibrationLeftX = (Int16)(report_bufLeft[13] | ((report_bufLeft[14] << 8) & 0xff00));
            acc_gcalibrationLeftY = (Int16)(report_bufLeft[15] | ((report_bufLeft[16] << 8) & 0xff00));
            acc_gcalibrationLeftZ = (Int16)(report_bufLeft[17] | ((report_bufLeft[18] << 8) & 0xff00));
            acc_gLeft.X = ((Int16)(report_bufLeft[13] | ((report_bufLeft[14] << 8) & 0xff00)) - acc_gcalibrationLeftX) * (1.0f / 4000f);
            acc_gLeft.Y = ((Int16)(report_bufLeft[15] | ((report_bufLeft[16] << 8) & 0xff00)) - acc_gcalibrationLeftY) * (1.0f / 4000f);
            acc_gLeft.Z = ((Int16)(report_bufLeft[17] | ((report_bufLeft[18] << 8) & 0xff00)) - acc_gcalibrationLeftZ) * (1.0f / 4000f);
            InitDirectAnglesLeft = acc_gLeft;
            Task.Run(() => taskX());
            this.WindowState = FormWindowState.Minimized;
        }
        private static void connectingWiimote()
        {
            do
                Thread.Sleep(1);
            while (!connect());
            do
                Thread.Sleep(1);
            while (!ScanWiimote());
        }
        private static void connectingJoyconLeft()
        {
            do
                Thread.Sleep(1);
            while (!lconnect());
            do
                Thread.Sleep(1);
            while (!ScanLeft());
        }
        private static double Scale(double value, double min, double max, double minScale, double maxScale)
        {
            double scaled = minScale + (double)(value - min) / (max - min) * (maxScale - minScale);
            return scaled;
        }
        private static void taskX()
        {
            while (running)
            {
                mWSIRSensors0X = aBuffer[6] | ((aBuffer[8] >> 4) & 0x03) << 8;
                mWSIRSensors0Y = aBuffer[7] | ((aBuffer[8] >> 6) & 0x03) << 8;
                mWSIRSensors1X = aBuffer[9] | ((aBuffer[8] >> 0) & 0x03) << 8;
                mWSIRSensors1Y = aBuffer[10] | ((aBuffer[8] >> 2) & 0x03) << 8;
                mWSIR0found = mWSIRSensors0X > 0f & mWSIRSensors0X <= 1024f & mWSIRSensors0Y > 0f & mWSIRSensors0Y <= 768f;
                mWSIR1found = mWSIRSensors1X > 0f & mWSIRSensors1X <= 1024f & mWSIRSensors1Y > 0f & mWSIRSensors1Y <= 768f;
                if (mWSIR0found)
                {
                    mWSIRSensors0Xcam = mWSIRSensors0X - 512f;
                    mWSIRSensors0Ycam = mWSIRSensors0Y - 384f;
                }
                if (mWSIR1found)
                {
                    mWSIRSensors1Xcam = mWSIRSensors1X - 512f;
                    mWSIRSensors1Ycam = mWSIRSensors1Y - 384f;
                }
                if (mWSIR0found & mWSIR1found)
                {
                    mWSIRSensorsXcam = (mWSIRSensors0Xcam + mWSIRSensors1Xcam) / 2f;
                    mWSIRSensorsYcam = (mWSIRSensors0Ycam + mWSIRSensors1Ycam) / 2f;
                }
                if (mWSIR0found)
                {
                    irx0 = 2 * mWSIRSensors0Xcam - mWSIRSensorsXcam;
                    iry0 = 2 * mWSIRSensors0Ycam - mWSIRSensorsYcam;
                }
                if (mWSIR1found)
                {
                    irx1 = 2 * mWSIRSensors1Xcam - mWSIRSensorsXcam;
                    iry1 = 2 * mWSIRSensors1Ycam - mWSIRSensorsYcam;
                }
                irxc = irx0 + irx1;
                iryc = iry0 + iry1;
                mWSButtonStateIRX = irxc;
                mWSButtonStateIRY = iryc * 2f;
                irx = mWSButtonStateIRX * (1024f / 1360f);
                iry = mWSButtonStateIRY + centery >= 0 ? Scale(mWSButtonStateIRY + centery, 0f, 1360f + centery, 0f, 1024f) : Scale(mWSButtonStateIRY + centery, -1360f + centery, 0f, -1024f, 0f);
                if (irx >= 1024f)
                    irx = 1024f;
                if (irx <= -1024f)
                    irx = -1024f;
                if (iry >= 1024f)
                    iry = 1024f;
                if (iry <= -1024f)
                    iry = -1024f;
                mWSButtonStateA = (aBuffer[2] & 0x08) != 0;
                mWSButtonStateB = (aBuffer[2] & 0x04) != 0;
                mWSButtonStateMinus = (aBuffer[2] & 0x10) != 0;
                mWSButtonStateHome = (aBuffer[2] & 0x80) != 0;
                mWSButtonStatePlus = (aBuffer[1] & 0x10) != 0;
                mWSButtonStateOne = (aBuffer[2] & 0x02) != 0;
                mWSButtonStateTwo = (aBuffer[2] & 0x01) != 0;
                mWSButtonStateUp = (aBuffer[1] & 0x08) != 0;
                mWSButtonStateDown = (aBuffer[1] & 0x04) != 0;
                mWSButtonStateLeft = (aBuffer[1] & 0x01) != 0;
                mWSButtonStateRight = (aBuffer[1] & 0x02) != 0;
                mWSRawValuesX = aBuffer[3] - 135f + calibrationinit;
                mWSRawValuesY = aBuffer[4] - 135f + calibrationinit;
                mWSRawValuesZ = aBuffer[5] - 135f + calibrationinit;
                ProcessButtonsAndStickLeft();
                ExtractIMUValuesLeft();
                valchanged(0, LeftButtonCAPTURE);
                if (wd[0] == 1 & !Getstate)
                {
                    viewpower1x = 1f;
                    viewpower2x = 0f;
                    viewpower3x = 0f;
                    viewpower1y = 1f;
                    viewpower2y = 0f;
                    viewpower3y = 0f;
                    dzx = 25.0f;
                    dzy = 25.0f;
                    leftstickx = 0;
                    leftsticky = 0;
                    rightsticky = 0;
                    rightstickx = 0;
                    lefttrigger = false;
                    righttrigger = false;
                    leftbumper = false;
                    rightbumper = false;
                    leftstick = false;
                    rightstick = false;
                    A = false;
                    B = false;
                    X = false;
                    Y = false;
                    start = false;
                    back = false;
                    Getstate = true;
                }
                else
                {
                    if (wd[0] == 1 & Getstate)
                    {
                        viewpower1x = 0.33f;
                        viewpower2x = 0f;
                        viewpower3x = 0.67f;
                        viewpower1y = 0.25f;
                        viewpower2y = 0.75f;
                        viewpower3y = 0f;
                        dzx = 4.0f;
                        dzy = 4.0f;
                        leftstickx = 0;
                        leftsticky = 0;
                        rightsticky = 0;
                        rightstickx = 0;
                        lefttrigger = false;
                        righttrigger = false;
                        leftbumper = false;
                        rightbumper = false;
                        leftstick = false;
                        rightstick = false;
                        A = false;
                        B = false;
                        X = false;
                        Y = false;
                        start = false;
                        back = false;
                        Getstate = false;
                    }
                }
                if (!Getstate)
                {
                    down = LeftButtonDPAD_DOWN;
                    left = LeftButtonDPAD_LEFT;
                    right = LeftButtonDPAD_RIGHT;
                    up = LeftButtonDPAD_UP;
                    rightstick = acc_gLeft.Y <= -1.13;
                    leftstick = LeftButtonSHOULDER_2;
                    A = LeftButtonSHOULDER_1;
                    back = mWSButtonStateOne;
                    start = mWSButtonStateTwo;
                    X = mWSButtonStateHome | ((mWSRawValuesZ > 0 ? mWSRawValuesZ : -mWSRawValuesZ) >= 30f & (mWSRawValuesY > 0 ? mWSRawValuesY : -mWSRawValuesY) >= 30f & (mWSRawValuesX > 0 ? mWSRawValuesX : -mWSRawValuesX) >= 30f);
                    rightbumper = mWSButtonStatePlus | mWSButtonStateUp;
                    leftbumper = mWSButtonStateMinus | mWSButtonStateUp;
                    B = mWSButtonStateDown;
                    Y = mWSButtonStateLeft | mWSButtonStateRight;
                    lefttrigger = mWSButtonStateA;
                    righttrigger = mWSButtonStateB;
                    changeScale(dzx, dzy);
                    changeView(lowsensx, lowsensy);
                    if (GetStickLeft()[1] > 0.25f)
                        leftsticky = 32767;
                    if (GetStickLeft()[1] < -0.25f)
                        leftsticky = -32767;
                    if (GetStickLeft()[1] <= 0.25f & GetStickLeft()[1] >= -0.25f)
                        leftsticky = 0;
                    if (GetStickLeft()[0] > 0.25f)
                        leftstickx = 32767;
                    if (GetStickLeft()[0] < -0.25f)
                        leftstickx = -32767;
                    if (GetStickLeft()[0] <= 0.25f & GetStickLeft()[0] >= -0.25f)
                        leftstickx = 0;
                }
                else
                {
                    irx = DirectAnglesLeft.X * 1350f;
                    iry = -(DirectAnglesLeft.Y * 1350f / 0.5f);
                    if (irx >= 1024f)
                        irx = 1024f;
                    if (irx <= -1024f)
                        irx = -1024f;
                    if (iry >= 1024f)
                        iry = 1024f;
                    if (iry <= -1024f)
                        iry = -1024f;
                    if (irx >= 0f)
                        mousex = Scale(irx * irx * irx / 1024f / 1024f * viewpower3x + irx * irx / 1024f * viewpower2x + irx * viewpower1x, 0f, 1024f, (dzx / 100f) * 1024f, 1024f);
                    if (irx <= 0f)
                        mousex = Scale(-(-irx * -irx * -irx) / 1024f / 1024f * viewpower3x - (-irx * -irx) / 1024f * viewpower2x - (-irx) * viewpower1x, -1024f, 0f, -1024f, -(dzx / 100f) * 1024f);
                    if (iry >= 0f)
                        mousey = Scale(iry * iry * iry / 1024f / 1024f * viewpower3y + iry * iry / 1024f * viewpower2y + iry * viewpower1y, 0f, 1024f, (dzy / 100f) * 1024f, 1024f);
                    if (iry <= 0f)
                        mousey = Scale(-(-iry * -iry * -iry) / 1024f / 1024f * viewpower3y - (-iry * -iry) / 1024f * viewpower2y - (-iry) * viewpower1y, -1024f, 0f, -1024f, -(dzy / 100f) * 1024f);
                    leftstickx = (short)(mousex * 32767f / 1024f);
                    mousey = 0;
                    leftsticky = (short)(mousey * 32767f / 1024f);
                    lefttrigger = LeftButtonDPAD_LEFT | LeftButtonSL;
                    righttrigger = LeftButtonDPAD_DOWN | LeftButtonSR;
                    A = LeftButtonDPAD_UP;
                    X = LeftButtonDPAD_RIGHT;
                    B = LeftButtonMINUS;
                    Y = LeftButtonSL & LeftButtonSR;
                    rightstick = LeftButtonSTICK;
                    leftbumper = LeftButtonSHOULDER_1;
                    rightbumper = LeftButtonSHOULDER_2;
                    if (GetStickLeft()[0] > 0.25f)
                        rightsticky = 32767;
                    if (GetStickLeft()[0] < -0.25f)
                        rightsticky = -32767;
                    if (GetStickLeft()[0] >= -0.25f & GetStickLeft()[0] <= 0.25f)
                        rightsticky = 0;
                    if (GetStickLeft()[1] < -0.25f)
                        rightstickx = 32767;
                    if (GetStickLeft()[1] > 0.25f)
                        rightstickx = -32767;
                    if (GetStickLeft()[1] <= 0.25f & GetStickLeft()[1] >= -0.25f)
                        rightstickx = 0;
                }
                LSPControl.control = back.ToString() + "," + start.ToString() + "," + A.ToString() + "," + B.ToString() + "," + X.ToString() + "," + Y.ToString() + "," + up.ToString() + "," + left.ToString() + "," + down.ToString() + "," + right.ToString() + "," + leftstick.ToString() + "," + rightstick.ToString() + "," + leftbumper.ToString() + "," + rightbumper.ToString() + "," + lefttrigger.ToString() + "," + righttrigger.ToString() + "," + leftstickx.ToString() + "," + leftsticky.ToString() + "," + rightstickx.ToString() + "," + rightsticky + ",end";
                System.Threading.Thread.Sleep(1);
            }
        }
        private static void changeScale(double dzchangedx, double dzchangedy)
        {
            if (irx >= 0f)
                mousex = Scale(irx * irx * irx / 1024f / 1024f * viewpower3x + irx * irx / 1024f * viewpower2x + irx * viewpower1x, 0f, 1024f, (dzchangedx / 100f) * 1024f, 1024f);
            if (irx <= 0f)
                mousex = Scale(-(-irx * -irx * -irx) / 1024f / 1024f * viewpower3x - (-irx * -irx) / 1024f * viewpower2x - (-irx) * viewpower1x, -1024f, 0f, -1024f, -(dzchangedx / 100f) * 1024f);
            if (iry >= 0f)
                mousey = Scale(iry * iry * iry / 1024f / 1024f * viewpower3y + iry * iry / 1024f * viewpower2y + iry * viewpower1y, 0f, 1024f, (dzchangedy / 100f) * 1024f, 1024f);
            if (iry <= 0f)
                mousey = Scale(-(-iry * -iry * -iry) / 1024f / 1024f * viewpower3y - (-iry * -iry) / 1024f * viewpower2y - (-iry) * viewpower1y, -1024f, 0f, -1024f, -(dzchangedy / 100f) * 1024f);
        }
        private static void changeView(double lowsenschangedx, double lowsenschangedy)
        {
            rightstickx = (short)(-mousex / 1024f / lowsenschangedx * 32767f);
            rightsticky = (short)(-mousey / 1024f / lowsenschangedy * 32767f);
        }
        private static void taskD()
        {
            while (running)
            {
                try
                {
                    mStream.Read(aBuffer, 0, 22);
                }
                catch { }
            }
        }
        private static void taskDLeft()
        {
            while (running)
            {
                try
                {
                    Lhid_read_timeout(handleLeft, report_bufLeft, (UIntPtr)report_lenLeft);
                }
                catch { }
            }
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            CloseControl();
        }
        private static void CloseControl()
        {
            running = false;
            closed = true;
            Thread.Sleep(100);
            try
            {
                LSPControl.Disconnect();
            }
            catch { }
            try
            {
                Lhid_close(handleLeft);
                handleLeft.Close();
                disconnectLeft();
            }
            catch { }
            try
            {
                mStream.Close();
                handle.Close();
                disconnect();
            }
            catch { }
        }
        private const string vendor_id = "57e", vendor_id_ = "057e", product_r1 = "0330", product_r2 = "0306", product_l = "2006";
        private enum EFileAttributes : uint
        {
            Overlapped = 0x40000000,
            Normal = 0x80
        };
        struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr RESERVED;
        }
        struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public UInt32 cbSize;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }
        private static bool ScanWiimote()
        {
            int index = 0;
            Guid guid;
            HidD_GetHidGuid(out guid);
            IntPtr hDevInfo = SetupDiGetClassDevs(ref guid, null, new IntPtr(), 0x00000010);
            SP_DEVICE_INTERFACE_DATA diData = new SP_DEVICE_INTERFACE_DATA();
            diData.cbSize = Marshal.SizeOf(diData);
            while (SetupDiEnumDeviceInterfaces(hDevInfo, new IntPtr(), ref guid, index, ref diData))
            {
                UInt32 size;
                SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, new IntPtr(), 0, out size, new IntPtr());
                SP_DEVICE_INTERFACE_DETAIL_DATA diDetail = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                diDetail.cbSize = 5;
                if (SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, ref diDetail, size, out size, new IntPtr()))
                {
                    if ((diDetail.DevicePath.Contains(vendor_id) | diDetail.DevicePath.Contains(vendor_id_)) & (diDetail.DevicePath.Contains(product_r1) | diDetail.DevicePath.Contains(product_r2)))
                    {
                        WiimoteFound(diDetail.DevicePath);
                        WiimoteFound(diDetail.DevicePath);
                        WiimoteFound(diDetail.DevicePath);
                        return true;
                    }
                }
                index++;
            }
            return false;
        }
        private static void WiimoteFound(string path)
        {
            do
            {
                handle = CreateFile(path, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, (uint)EFileAttributes.Overlapped, IntPtr.Zero);
                WriteData(handle, IR, (int)REGISTER_IR, new byte[] { 0x08 }, 1);
                WriteData(handle, Type, (int)REGISTER_EXTENSION_INIT_1, new byte[] { 0x55 }, 1);
                WriteData(handle, Type, (int)REGISTER_EXTENSION_INIT_2, new byte[] { 0x00 }, 1);
                WriteData(handle, Type, (int)REGISTER_MOTIONPLUS_INIT, new byte[] { 0x04 }, 1);
                ReadData(handle, 0x0016, 7);
                ReadData(handle, (int)REGISTER_EXTENSION_TYPE, 6);
                ReadData(handle, (int)REGISTER_EXTENSION_CALIBRATION, 16);
                ReadData(handle, (int)REGISTER_EXTENSION_CALIBRATION, 32);
            }
            while (handle.IsInvalid);
            mStream = new FileStream(handle, FileAccess.ReadWrite, 22, true);
        }
        private static void ReadData(SafeFileHandle _hFile, int address, short size)
        {
            mBuff[0] = (byte)ReadMemory;
            mBuff[1] = (byte)((address & 0xff000000) >> 24);
            mBuff[2] = (byte)((address & 0x00ff0000) >> 16);
            mBuff[3] = (byte)((address & 0x0000ff00) >> 8);
            mBuff[4] = (byte)(address & 0x000000ff);
            mBuff[5] = (byte)((size & 0xff00) >> 8);
            mBuff[6] = (byte)(size & 0xff);
            HidD_SetOutputReport(_hFile.DangerousGetHandle(), mBuff, 22);
        }
        private static void WriteData(SafeFileHandle _hFile, byte mbuff, int address, byte[] buff, short size)
        {
            mBuff[0] = (byte)mbuff;
            mBuff[1] = (byte)(0x04);
            mBuff[2] = (byte)IRExtensionAccel;
            Array.Copy(buff, 0, mBuff, 3, 1);
            HidD_SetOutputReport(_hFile.DangerousGetHandle(), mBuff, 22);
            mBuff[0] = (byte)WriteMemory;
            mBuff[1] = (byte)(((address & 0xff000000) >> 24));
            mBuff[2] = (byte)((address & 0x00ff0000) >> 16);
            mBuff[3] = (byte)((address & 0x0000ff00) >> 8);
            mBuff[4] = (byte)((address & 0x000000ff) >> 0);
            mBuff[5] = (byte)size;
            Array.Copy(buff, 0, mBuff, 6, 1);
            HidD_SetOutputReport(_hFile.DangerousGetHandle(), mBuff, 22);
        }
        private static void ProcessButtonsAndStickLeft()
        {
            LeftButtonSHOULDER_1 = (report_bufLeft[3 + (ISLEFT ? 2 : 0)] & 0x40) != 0;
            LeftButtonSHOULDER_2 = (report_bufLeft[3 + (ISLEFT ? 2 : 0)] & 0x80) != 0;
            LeftButtonSR = (report_bufLeft[3 + (ISLEFT ? 2 : 0)] & 0x10) != 0;
            LeftButtonSL = (report_bufLeft[3 + (ISLEFT ? 2 : 0)] & 0x20) != 0;
            LeftButtonDPAD_DOWN = (report_bufLeft[3 + (ISLEFT ? 2 : 0)] & (ISLEFT ? 0x01 : 0x04)) != 0;
            LeftButtonDPAD_RIGHT = (report_bufLeft[3 + (ISLEFT ? 2 : 0)] & (ISLEFT ? 0x04 : 0x08)) != 0;
            LeftButtonDPAD_UP = (report_bufLeft[3 + (ISLEFT ? 2 : 0)] & (ISLEFT ? 0x02 : 0x02)) != 0;
            LeftButtonDPAD_LEFT = (report_bufLeft[3 + (ISLEFT ? 2 : 0)] & (ISLEFT ? 0x08 : 0x01)) != 0;
            LeftButtonMINUS = ((report_bufLeft[4] & 0x01) != 0);
            LeftButtonCAPTURE = ((report_bufLeft[4] & 0x20) != 0);
            LeftButtonSTICK = ((report_bufLeft[4] & (ISLEFT ? 0x08 : 0x04)) != 0);
            stick_rawLeft[0] = report_bufLeft[6 + (ISLEFT ? 0 : 3)];
            stick_rawLeft[1] = report_bufLeft[7 + (ISLEFT ? 0 : 3)];
            stick_rawLeft[2] = report_bufLeft[8 + (ISLEFT ? 0 : 3)];
            stick_precalLeft[0] = (UInt16)(stick_rawLeft[0] | ((stick_rawLeft[1] & 0xf) << 8));
            stick_precalLeft[1] = (UInt16)((stick_rawLeft[1] >> 4) | (stick_rawLeft[2] << 4));
            stickLeft = CenterSticksLeft(stick_precalLeft);
        }
        private static void ExtractIMUValuesLeft()
        {
            acc_gLeft.X = ((Int16)(report_bufLeft[13] | ((report_bufLeft[14] << 8) & 0xff00)) - acc_gcalibrationLeftX) * (1.0f / 4000f);
            acc_gLeft.Y = ((Int16)(report_bufLeft[15] | ((report_bufLeft[16] << 8) & 0xff00)) - acc_gcalibrationLeftY) * (1.0f / 4000f);
            acc_gLeft.Z = ((Int16)(report_bufLeft[17] | ((report_bufLeft[18] << 8) & 0xff00)) - acc_gcalibrationLeftZ) * (1.0f / 4000f);
            DirectAnglesLeft = acc_gLeft - InitDirectAnglesLeft;
        }
        private static double[] CenterSticksLeft(UInt16[] vals)
        {
            double[] s = { 0, 0 };
            s[0] = ((int)((vals[0] - stick_calibrationLeft[0]) / 100f)) / 13f;
            s[1] = ((int)((vals[1] - stick_calibrationLeft[1]) / 100f)) / 13f;
            return s;
        }
        private static double[] stickLeft = { 0, 0 };
        private static SafeFileHandle handleLeft;
        private static byte[] stick_rawLeft = { 0, 0, 0 };
        private static UInt16[] stick_calibrationLeft = { 0, 0 };
        private static UInt16[] stick_precalLeft = { 0, 0 };
        private static Vector3 acc_gLeft = new Vector3();
        private const uint report_lenLeft = 25;
        private static Vector3 InitDirectAnglesLeft, DirectAnglesLeft;
        private static bool LeftButtonSHOULDER_1, LeftButtonSHOULDER_2, LeftButtonSR, LeftButtonSL, LeftButtonDPAD_DOWN, LeftButtonDPAD_RIGHT, LeftButtonDPAD_UP, LeftButtonDPAD_LEFT, LeftButtonMINUS, LeftButtonSTICK, LeftButtonCAPTURE, ISLEFT;
        private static byte[] report_bufLeft = new byte[report_lenLeft];
        private static byte[] buf_Left = new byte[report_lenLeft];
        private static float acc_gcalibrationLeftX, acc_gcalibrationLeftY, acc_gcalibrationLeftZ;
        private static double[] GetStickLeft()
        {
            return stickLeft;
        }
        private static bool ScanLeft()
        {
            int index = 0;
            System.Guid guid;
            HidD_GetHidGuid(out guid);
            System.IntPtr hDevInfo = SetupDiGetClassDevs(ref guid, null, new System.IntPtr(), 0x00000010);
            SP_DEVICE_INTERFACE_DATA diData = new SP_DEVICE_INTERFACE_DATA();
            diData.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(diData);
            while (SetupDiEnumDeviceInterfaces(hDevInfo, new System.IntPtr(), ref guid, index, ref diData))
            {
                System.UInt32 size;
                SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, new System.IntPtr(), 0, out size, new System.IntPtr());
                SP_DEVICE_INTERFACE_DETAIL_DATA diDetail = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                diDetail.cbSize = 5;
                if (SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, ref diDetail, size, out size, new System.IntPtr()))
                {
                    if ((diDetail.DevicePath.Contains(vendor_id) | diDetail.DevicePath.Contains(vendor_id_)) & diDetail.DevicePath.Contains(product_l))
                    {
                        ISLEFT = true;
                        AttachJoyLeft(diDetail.DevicePath);
                        AttachJoyLeft(diDetail.DevicePath);
                        AttachJoyLeft(diDetail.DevicePath);
                        return true;
                    }
                }
                index++;
            }
            return false;
        }
        private static void AttachJoyLeft(string path)
        {
            do
            {
                IntPtr handle = CreateFile(path, System.IO.FileAccess.ReadWrite, System.IO.FileShare.ReadWrite, new System.IntPtr(), System.IO.FileMode.Open, EFileAttributes.Normal, new System.IntPtr());
                handleLeft = Lhid_open_path(handle);
                SubcommandLeft(0x40, new byte[] { 0x1 }, 1);
                SubcommandLeft(0x3, new byte[] { 0x30 }, 1);
            }
            while (handleLeft.IsInvalid);
        }
        private static void SubcommandLeft(byte sc, byte[] buf, uint len)
        {
            Array.Copy(buf, 0, buf_Left, 11, len);
            buf_Left[0] = 0x1;
            buf_Left[1] = 0;
            buf_Left[10] = sc;
            Lhid_write(handleLeft, buf_Left, (UIntPtr)(len + 11));
            Lhid_read_timeout(handleLeft, buf_Left, (UIntPtr)report_lenLeft);
        }
    }
    public class LSPControl
    {
        private static IPEndPoint ipEnd;
        private static Socket client;
        public static string ip;
        public static int port;
        public static string control;
        public static void Connect()
        {
            ip = "10.0.0.13";
            port = Convert.ToInt32(62000);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            while (!client.Connected)
            {
                try
                {
                    ipEnd = new IPEndPoint(IPAddress.Parse(ip), port);
                    client.Blocking = true;
                    client.Connect(ipEnd);
                }
                catch { }
                System.Threading.Thread.Sleep(1);
            }
            Task.Run(() => taskSend());
        }
        public static void taskSend()
        {
            while (!Form1.closed)
            {
                try
                {
                    byte[] clientData;
                    clientData = controlToByteArray(control);
                    client.Send(clientData);
                }
                catch { }
                System.Threading.Thread.Sleep(10);
            }
        }
        public static void Disconnect()
        {
            client.Close();
        }
        public static byte[] controlToByteArray(string control)
        {
            byte[] data = Encoding.ASCII.GetBytes(control);
            return data;
        }
    }
}