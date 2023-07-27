using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using System.Security.Principal;
namespace Run
{
    internal class Program
    {
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint ms);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint ms);
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        private static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
        private static uint CurrentResolution = 0;
        private static ConsoleEventDelegate handler;
        private delegate bool ConsoleEventDelegate(int eventType);
        private static ThreadStart threadstart;
        private static Thread thread;
        public static bool closed = false;
        static void Main(string[] args)
        {
            bool runelevated = false;
            bool oneinstanceonly = true;
            try
            {
                TimeBeginPeriod(1);
                NtSetTimerResolution(1, true, ref CurrentResolution);
                SetProcessPriority();
                if (oneinstanceonly)
                {
                    if (AlreadyRunning())
                    {
                        return;
                    }
                }
                if (runelevated)
                {
                    if (!hasAdminRights())
                    {
                        RunElevated();
                        return;
                    }
                }
            }
            catch
            {
                return;
            }
            TimeBeginPeriod(1);
            NtSetTimerResolution(1, true, ref CurrentResolution);
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);
            Task.Run(() => LSPControl.Connect());
            Task.Run(() => LSPSimulate.Connect());
            Console.ReadLine();
        }
        public static bool hasAdminRights()
        {
            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        public static void RunElevated()
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.Verb = "runas";
                processInfo.FileName = Application.ExecutablePath;
                Process.Start(processInfo);
            }
            catch { }
        }
        private static void SetProcessPriority()
        {
            using (Process p = Process.GetCurrentProcess())
            {
                p.PriorityClass = ProcessPriorityClass.RealTime;
            }
        }
        private static bool AlreadyRunning()
        {
            String thisprocessname = Process.GetCurrentProcess().ProcessName;
            Process[] processes = Process.GetProcessesByName(thisprocessname);
            if (processes.Length > 1)
                return true;
            else
                return false;
        }
        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                closed = true;
                Thread.Sleep(100);
                threadstart = new ThreadStart(FormClose);
                thread = new Thread(threadstart);
                thread.Start();
                Thread.Sleep(10000);
            }
            return false;
        }
        private static void FormClose()
        {
            LSPControl.Disconnect();
            LSPSimulate.Disconnect();
        }
    }
    public class LSPControl
    {
        private static IPEndPoint ipEnd;
        private static Socket sock;
        private static Socket client;
        public static int port;
        public static string[] control;
        public static void Connect()
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            while (!client.Connected)
            {
                try
                {
                    port = Convert.ToInt32(62000);
                    ipEnd = new IPEndPoint(IPAddress.Any, port);
                    sock.Blocking = true;
                    sock.Bind(ipEnd);
                    sock.Listen(100);
                    client.Blocking = true;
                    client = sock.Accept();
                }
                catch { }
                System.Threading.Thread.Sleep(1);
            }
            Task.Run(() => taskReceive());
        }
        public static void taskReceive()
        {
            while (!Program.closed)
            {
                try
                {
                    byte[] clientData = new byte[1024 * 2];
                    int received = client.Receive(clientData);
                    if (received > 0)
                    {
                        control = byteArrayToControl(clientData);
                    }
                }
                catch { }
                System.Threading.Thread.Sleep(10);
            }
        }
        public static void Disconnect()
        {
            client.Close();
        }
        public static string[] byteArrayToControl(byte[] byteArrayIn)
        {
            string str = Encoding.ASCII.GetString(byteArrayIn);
            string[] splitstring = str.Split(',');
            List<string> newsplitstring = new List<string>();
            foreach (string valuestring in splitstring)
            {
                int pFrom = valuestring.IndexOf(".");
                string newvaluestring;
                if (pFrom > 0)
                {
                    int pTo = valuestring.Length;
                    string result = valuestring.Substring(pFrom, pTo - pFrom);
                    newvaluestring = valuestring.Replace(result, "");
                }
                else
                {
                    newvaluestring = valuestring;
                }
                newsplitstring.Add(newvaluestring);
            }
            return newsplitstring.ToArray();
        }
    }
    public class LSPSimulate
    {
        public static void Connect()
        {
            ScpBus.LoadController();
            Task.Run(() => SetControl());
        }
        public static void SetControl()
        {
            while (!Program.closed)
            {
                try
                {
                    bool back = bool.Parse(LSPControl.control[0]); 
                    bool start = bool.Parse(LSPControl.control[1]); 
                    bool A = bool.Parse(LSPControl.control[2]); 
                    bool B = bool.Parse(LSPControl.control[3]); 
                    bool X = bool.Parse(LSPControl.control[4]); 
                    bool Y = bool.Parse(LSPControl.control[5]); 
                    bool up = bool.Parse(LSPControl.control[6]); 
                    bool left = bool.Parse(LSPControl.control[7]); 
                    bool down = bool.Parse(LSPControl.control[8]); 
                    bool right = bool.Parse(LSPControl.control[9]); 
                    bool leftstick = bool.Parse(LSPControl.control[10]); 
                    bool rightstick = bool.Parse(LSPControl.control[11]); 
                    bool leftbumper = bool.Parse(LSPControl.control[12]); 
                    bool rightbumper = bool.Parse(LSPControl.control[13]); 
                    bool lefttrigger = bool.Parse(LSPControl.control[14]); 
                    bool righttrigger = bool.Parse(LSPControl.control[15]);
                    double leftstickx = Convert.ToSingle(LSPControl.control[16]);
                    double leftsticky = Convert.ToSingle(LSPControl.control[17]);
                    double rightstickx = Convert.ToSingle(LSPControl.control[18]);
                    double rightsticky = Convert.ToSingle(LSPControl.control[19]);
                    ScpBus.SetController(back, start, A, B, X, Y, up, left, down, right, leftstick, rightstick, leftbumper, rightbumper, lefttrigger, righttrigger, leftstickx, leftsticky, rightstickx, rightsticky);
                }
                catch { }
                System.Threading.Thread.Sleep(1);
            }
        }
        public static void Disconnect()
        {
            ScpBus.UnLoadController();
        }
    }
    public class ScpBus : IDisposable
    {
        public static int[] wd = { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
        public static int[] wu = { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
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
        public static ScpBus scpBus;
        public static X360Controller controller;
        public static void LoadController()
        {
            scpBus = new ScpBus();
            scpBus.PlugIn(1);
            controller = new X360Controller();
        }
        public static void UnLoadController()
        {
            SetController(false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, 0, 0, 0, 0);
            Thread.Sleep(100);
            scpBus.Unplug(1);
        }
        public static void SetController(bool back, bool start, bool A, bool B, bool X, bool Y, bool up, bool left, bool down, bool right, bool leftstick, bool rightstick, bool leftbumper, bool rightbumper, bool lefttrigger, bool righttrigger, double leftstickx, double leftsticky, double rightstickx, double rightsticky)
        {
            valchanged(1, back);
            if (wd[1] == 1)
                controller.Buttons ^= X360Buttons.Back;
            if (wu[1] == 1)
                controller.Buttons &= ~X360Buttons.Back;
            valchanged(2, start);
            if (wd[2] == 1)
                controller.Buttons ^= X360Buttons.Start;
            if (wu[2] == 1)
                controller.Buttons &= ~X360Buttons.Start;
            valchanged(3, A);
            if (wd[3] == 1)
                controller.Buttons ^= X360Buttons.A;
            if (wu[3] == 1)
                controller.Buttons &= ~X360Buttons.A;
            valchanged(4, B);
            if (wd[4] == 1)
                controller.Buttons ^= X360Buttons.B;
            if (wu[4] == 1)
                controller.Buttons &= ~X360Buttons.B;
            valchanged(5, X);
            if (wd[5] == 1)
                controller.Buttons ^= X360Buttons.X;
            if (wu[5] == 1)
                controller.Buttons &= ~X360Buttons.X;
            valchanged(6, Y);
            if (wd[6] == 1)
                controller.Buttons ^= X360Buttons.Y;
            if (wu[6] == 1)
                controller.Buttons &= ~X360Buttons.Y;
            valchanged(7, up);
            if (wd[7] == 1)
                controller.Buttons ^= X360Buttons.Up;
            if (wu[7] == 1)
                controller.Buttons &= ~X360Buttons.Up;
            valchanged(8, left);
            if (wd[8] == 1)
                controller.Buttons ^= X360Buttons.Left;
            if (wu[8] == 1)
                controller.Buttons &= ~X360Buttons.Left;
            valchanged(9, down);
            if (wd[9] == 1)
                controller.Buttons ^= X360Buttons.Down;
            if (wu[9] == 1)
                controller.Buttons &= ~X360Buttons.Down;
            valchanged(10, right);
            if (wd[10] == 1)
                controller.Buttons ^= X360Buttons.Right;
            if (wu[10] == 1)
                controller.Buttons &= ~X360Buttons.Right;
            valchanged(11, leftstick);
            if (wd[11] == 1)
                controller.Buttons ^= X360Buttons.LeftStick;
            if (wu[11] == 1)
                controller.Buttons &= ~X360Buttons.LeftStick;
            valchanged(12, rightstick);
            if (wd[12] == 1)
                controller.Buttons ^= X360Buttons.RightStick;
            if (wu[12] == 1)
                controller.Buttons &= ~X360Buttons.RightStick;
            valchanged(13, leftbumper);
            if (wd[13] == 1)
                controller.Buttons ^= X360Buttons.LeftBumper;
            if (wu[13] == 1)
                controller.Buttons &= ~X360Buttons.LeftBumper;
            valchanged(14, rightbumper);
            if (wd[14] == 1)
                controller.Buttons ^= X360Buttons.RightBumper;
            if (wu[14] == 1)
                controller.Buttons &= ~X360Buttons.RightBumper;
            if (lefttrigger)
                controller.LeftTrigger = 255;
            else
                controller.LeftTrigger = 0;
            if (righttrigger)
                controller.RightTrigger = 255;
            else
                controller.RightTrigger = 0;
            controller.LeftStickX = (short)leftstickx;
            controller.LeftStickY = (short)leftsticky;
            controller.RightStickX = (short)rightstickx;
            controller.RightStickY = (short)rightsticky;
            scpBus.Report(controller.GetReport());
        }
        public const string SCP_BUS_CLASS_GUID = "{F679F562-3164-42CE-A4DB-E7DDBE723909}";
        public const int ReportSize = 28;

        public readonly SafeFileHandle _deviceHandle;

        /// <summary>
        /// Creates a new ScpBus object, which will then try to get a handle to the SCP Virtual Bus device. If it is unable to get the handle, an IOException will be thrown.
        /// </summary>
        public ScpBus() : this(0) { }

        /// <summary>
        /// Creates a new ScpBus object, which will then try to get a handle to the SCP Virtual Bus device. If it is unable to get the handle, an IOException will be thrown.
        /// </summary>
        /// <param name="instance">Specifies which SCP Virtual Bus device to use. This is 0-based.</param>
        public ScpBus(int instance)
        {
            string devicePath = "";

            if (Find(new Guid(SCP_BUS_CLASS_GUID), ref devicePath, instance))
            {
                _deviceHandle = GetHandle(devicePath);
            }
            else
            {
                throw new IOException("SCP Virtual Bus Device not found");
            }
        }

        /// <summary>
        /// Creates a new ScpBus object, which will then try to get a handle to the specified SCP Virtual Bus device. If it is unable to get the handle, an IOException will be thrown.
        /// </summary>
        /// <param name="devicePath">The path to the SCP Virtual Bus device that you want to use.</param>
        public ScpBus(string devicePath)
        {
            _deviceHandle = GetHandle(devicePath);
        }

        /// <summary>
        /// Closes the handle to the SCP Virtual Bus device. Call this when you are done with your instance of ScpBus.
        /// 
        /// (This method does the same thing as the Dispose() method. Use one or the other.)
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Closes the handle to the SCP Virtual Bus device. Call this when you are done with your instance of ScpBus.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal disposer, called by either the finalizer or the Dispose() method.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_deviceHandle != null && !_deviceHandle.IsInvalid)
            {
                _deviceHandle.Dispose();
            }
        }

        /// <summary>
        /// Plugs in an emulated Xbox 360 controller.
        /// </summary>
        /// <param name="controllerNumber">Used to identify the controller. Give each controller you plug in a different number. Number must be non-zero.</param>
        /// <returns>True if the operation was successful, false otherwise.</returns>
        public bool PlugIn(int controllerNumber)
        {
            if (_deviceHandle.IsInvalid)
                throw new ObjectDisposedException("SCP Virtual Bus device handle is closed");

            int transfered = 0;
            byte[] buffer = new byte[16];

            buffer[0] = 0x10;
            buffer[1] = 0x00;
            buffer[2] = 0x00;
            buffer[3] = 0x00;

            buffer[4] = (byte)((controllerNumber) & 0xFF);
            buffer[5] = (byte)((controllerNumber >> 8) & 0xFF);
            buffer[6] = (byte)((controllerNumber >> 16) & 0xFF);
            buffer[7] = (byte)((controllerNumber >> 24) & 0xFF);

            return NativeMethods.DeviceIoControl(_deviceHandle, 0x2A4000, buffer, buffer.Length, null, 0, ref transfered, IntPtr.Zero);
        }

        /// <summary>
        /// Unplugs an emulated Xbox 360 controller.
        /// </summary>
        /// <param name="controllerNumber">The controller you want to unplug.</param>
        /// <returns>True if the operation was successful, false otherwise.</returns>
        public bool Unplug(int controllerNumber)
        {
            if (_deviceHandle.IsInvalid)
                throw new ObjectDisposedException("SCP Virtual Bus device handle is closed");

            int transfered = 0;
            byte[] buffer = new Byte[16];

            buffer[0] = 0x10;
            buffer[1] = 0x00;
            buffer[2] = 0x00;
            buffer[3] = 0x00;

            buffer[4] = (byte)((controllerNumber) & 0xFF);
            buffer[5] = (byte)((controllerNumber >> 8) & 0xFF);
            buffer[6] = (byte)((controllerNumber >> 16) & 0xFF);
            buffer[7] = (byte)((controllerNumber >> 24) & 0xFF);

            return NativeMethods.DeviceIoControl(_deviceHandle, 0x2A4004, buffer, buffer.Length, null, 0, ref transfered, IntPtr.Zero);
        }

        /// <summary>
        /// Unplugs all emulated Xbox 360 controllers.
        /// </summary>
        /// <returns>True if the operation was successful, false otherwise.</returns>
        public bool UnplugAll()
        {
            if (_deviceHandle.IsInvalid)
                throw new ObjectDisposedException("SCP Virtual Bus device handle is closed");

            int transfered = 0;
            byte[] buffer = new byte[16];

            buffer[0] = 0x10;
            buffer[1] = 0x00;
            buffer[2] = 0x00;
            buffer[3] = 0x00;

            return NativeMethods.DeviceIoControl(_deviceHandle, 0x2A4004, buffer, buffer.Length, null, 0, ref transfered, IntPtr.Zero);
        }
        int transferred;
        byte[] outputBuffer = null;
        /// <summary>
        /// Sends an input report for the current state of the specified emulated Xbox 360 controller. Note: Only use this if you don't care about rumble data, otherwise use the 3-parameter version of Report().
        /// </summary>
        /// <param name="controllerNumber">The controller to report.</param>
        /// <param name="controllerReport">The controller report. If using the included X360Controller class, this can be generated with the GetReport() method. Otherwise see http://free60.org/wiki/GamePad#Input_report for details.</param>
        /// <returns>True if the operation was successful, false otherwise.</returns>
        public bool Report(byte[] controllerReport)
        {
            return NativeMethods.DeviceIoControl(_deviceHandle, 0x2A400C, controllerReport, controllerReport.Length, outputBuffer, outputBuffer?.Length ?? 0, ref transferred, IntPtr.Zero);
        }

        public static bool Find(Guid target, ref string path, int instance = 0)
        {
            IntPtr detailDataBuffer = IntPtr.Zero;
            IntPtr deviceInfoSet = IntPtr.Zero;

            try
            {
                NativeMethods.SP_DEVICE_INTERFACE_DATA DeviceInterfaceData = new NativeMethods.SP_DEVICE_INTERFACE_DATA(), da = new NativeMethods.SP_DEVICE_INTERFACE_DATA();
                int bufferSize = 0, memberIndex = 0;

                deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref target, IntPtr.Zero, IntPtr.Zero, NativeMethods.DIGCF_PRESENT | NativeMethods.DIGCF_DEVICEINTERFACE);

                DeviceInterfaceData.cbSize = da.cbSize = Marshal.SizeOf(DeviceInterfaceData);

                while (NativeMethods.SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref target, memberIndex, ref DeviceInterfaceData))
                {
                    NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref DeviceInterfaceData, IntPtr.Zero, 0, ref bufferSize, ref da);
                    detailDataBuffer = Marshal.AllocHGlobal(bufferSize);

                    Marshal.WriteInt32(detailDataBuffer, (IntPtr.Size == 4) ? (4 + Marshal.SystemDefaultCharSize) : 8);

                    if (NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref DeviceInterfaceData, detailDataBuffer, bufferSize, ref bufferSize, ref da))
                    {
                        IntPtr pDevicePathName = detailDataBuffer + 4;

                        path = Marshal.PtrToStringAuto(pDevicePathName).ToUpper(CultureInfo.InvariantCulture);
                        Marshal.FreeHGlobal(detailDataBuffer);

                        if (memberIndex == instance) return true;
                    }
                    else Marshal.FreeHGlobal(detailDataBuffer);


                    memberIndex++;
                }
            }
            finally
            {
                if (deviceInfoSet != IntPtr.Zero)
                {
                    NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }

            return false;
        }

        public static SafeFileHandle GetHandle(string devicePath)
        {
            devicePath = devicePath.ToUpper(CultureInfo.InvariantCulture);

            SafeFileHandle handle = NativeMethods.CreateFile(devicePath, (NativeMethods.GENERIC_WRITE | NativeMethods.GENERIC_READ), NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE, IntPtr.Zero, NativeMethods.OPEN_EXISTING, NativeMethods.FILE_ATTRIBUTE_NORMAL | NativeMethods.FILE_FLAG_OVERLAPPED, UIntPtr.Zero);

            if (handle == null || handle.IsInvalid)
            {
                throw new IOException("Unable to get SCP Virtual Bus Device handle");
            }

            return handle;
        }
    }

    internal static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DEVICE_INTERFACE_DATA
        {
            internal int cbSize;
            internal Guid InterfaceClassGuid;
            internal int Flags;
            internal IntPtr Reserved;
        }

        internal const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        internal const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        internal const uint FILE_SHARE_READ = 1;
        internal const uint FILE_SHARE_WRITE = 2;
        internal const uint GENERIC_READ = 0x80000000;
        internal const uint GENERIC_WRITE = 0x40000000;
        internal const uint OPEN_EXISTING = 3;
        internal const int DIGCF_PRESENT = 0x0002;
        internal const int DIGCF_DEVICEINTERFACE = 0x0010;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, UIntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeviceIoControl(SafeFileHandle hDevice, int dwIoControlCode, byte[] lpInBuffer, int nInBufferSize, byte[] lpOutBuffer, int nOutBufferSize, ref int lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern int SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, int memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, int flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData, int deviceInterfaceDetailDataSize, ref int requiredSize, ref SP_DEVICE_INTERFACE_DATA deviceInfoData);
    }
    /// <summary>
    /// A virtual Xbox 360 Controller. After setting the desired values, use the GetReport() method to generate a controller report that can be used with ScpBus's Report() method.
    /// </summary>
    public class X360Controller
    {
        /// <summary>
        /// Generates a new X360Controller object with the default initial state (no buttons pressed, all analog inputs 0).
        /// </summary>
        public X360Controller()
        {
            Buttons = X360Buttons.None;
            LeftTrigger = 0;
            RightTrigger = 0;
            LeftStickX = 0;
            LeftStickY = 0;
            RightStickX = 0;
            RightStickY = 0;
        }

        /// <summary>
        /// Generates a new X360Controller object. Optionally, you can specify the initial state of the controller.
        /// </summary>
        /// <param name="buttons">The pressed buttons. Use like flags (i.e. (X360Buttons.A | X360Buttons.X) would be mean both A and X are pressed).</param>
        /// <param name="leftTrigger">Left trigger analog input. 0 to 255.</param>
        /// <param name="rightTrigger">Right trigger analog input. 0 to 255.</param>
        /// <param name="leftStickX">Left stick X-axis. -32,768 to 32,767.</param>
        /// <param name="leftStickY">Left stick Y-axis. -32,768 to 32,767.</param>
        /// <param name="rightStickX">Right stick X-axis. -32,768 to 32,767.</param>
        /// <param name="rightStickY">Right stick Y-axis. -32,768 to 32,767.</param>
        public X360Controller(X360Buttons buttons, byte leftTrigger, byte rightTrigger, short leftStickX, short leftStickY, short rightStickX, short rightStickY)
        {
            Buttons = buttons;
            LeftTrigger = leftTrigger;
            RightTrigger = rightTrigger;
            LeftStickX = leftStickX;
            LeftStickY = leftStickY;
            RightStickX = rightStickX;
            RightStickY = rightStickY;
        }

        /// <summary>
        /// Generates a new X360Controller object with the same values as the specified X360Controller object.
        /// </summary>
        /// <param name="controller">An X360Controller object to copy values from.</param>
        public X360Controller(X360Controller controller)
        {
            Buttons = controller.Buttons;
            LeftTrigger = controller.LeftTrigger;
            RightTrigger = controller.RightTrigger;
            LeftStickX = controller.LeftStickX;
            LeftStickY = controller.LeftStickY;
            RightStickX = controller.RightStickX;
            RightStickY = controller.RightStickY;
        }

        /// <summary>
        /// The controller's currently pressed buttons. Use the X360Button values like flags (i.e. (X360Buttons.A | X360Buttons.X) would be mean both A and X are pressed).
        /// </summary>
        public X360Buttons Buttons { get; set; }

        /// <summary>
        /// The controller's left trigger analog input. Value can range from 0 to 255.
        /// </summary>
        public byte LeftTrigger { get; set; }

        /// <summary>
        /// The controller's right trigger analog input. Value can range from 0 to 255.
        /// </summary>
        public byte RightTrigger { get; set; }

        /// <summary>
        /// The controller's left stick X-axis. Value can range from -32,768 to 32,767.
        /// </summary>
        public short LeftStickX { get; set; }

        /// <summary>
        /// The controller's left stick Y-axis. Value can range from -32,768 to 32,767.
        /// </summary>
        public short LeftStickY { get; set; }

        /// <summary>
        /// The controller's right stick X-axis. Value can range from -32,768 to 32,767.
        /// </summary>
        public short RightStickX { get; set; }

        /// <summary>
        /// The controller's right stick Y-axis. Value can range from -32,768 to 32,767.
        /// </summary>
        public short RightStickY { get; set; }

        byte[] bytes = new byte[20];
        byte[] fullReport = { 0x1C, 0, 0, 0, (byte)((1) & 0xFF), (byte)((1 >> 8) & 0xFF), (byte)((1 >> 16) & 0xFF), (byte)((1 >> 24) & 0xFF), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        /// <summary>
        /// Generates an Xbox 360 controller report as specified here: http://free60.org/wiki/GamePad#Input_report. This can be used with ScpBus's Report() method.
        /// </summary>
        /// <returns>A 20-byte Xbox 360 controller report.</returns>
        public byte[] GetReport()
        {
            bytes[0] = 0x00;                                 // Message type (input report)
            bytes[1] = 0x14;                                 // Message size (20 bytes)

            bytes[2] = (byte)((ushort)Buttons & 0xFF);       // Buttons low
            bytes[3] = (byte)((ushort)Buttons >> 8 & 0xFF);  // Buttons high

            bytes[4] = LeftTrigger;                          // Left trigger
            bytes[5] = RightTrigger;                         // Right trigger

            bytes[6] = (byte)(LeftStickX & 0xFF);            // Left stick X-axis low
            bytes[7] = (byte)(LeftStickX >> 8 & 0xFF);       // Left stick X-axis high
            bytes[8] = (byte)(LeftStickY & 0xFF);            // Left stick Y-axis low
            bytes[9] = (byte)(LeftStickY >> 8 & 0xFF);       // Left stick Y-axis high

            bytes[10] = (byte)(RightStickX & 0xFF);          // Right stick X-axis low
            bytes[11] = (byte)(RightStickX >> 8 & 0xFF);     // Right stick X-axis high
            bytes[12] = (byte)(RightStickY & 0xFF);          // Right stick Y-axis low
            bytes[13] = (byte)(RightStickY >> 8 & 0xFF);     // Right stick Y-axis high

            // Remaining bytes are unused

            System.Array.Copy(bytes, 0, fullReport, 8, 20);

            return fullReport;
        }
    }

    /// <summary>
    /// The buttons to be used with an X360Controller object.
    /// </summary>
    [Flags]
    public enum X360Buttons
    {
        None = 0,

        Up = 1 << 0,
        Down = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,

        Start = 1 << 4,
        Back = 1 << 5,

        LeftStick = 1 << 6,
        RightStick = 1 << 7,

        LeftBumper = 1 << 8,
        RightBumper = 1 << 9,

        Logo = 1 << 10,

        A = 1 << 12,
        B = 1 << 13,
        X = 1 << 14,
        Y = 1 << 15,

    }
}