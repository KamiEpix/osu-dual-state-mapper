using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;

namespace DualStateMapper
{
    public class Program
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;
        private const int KEYEVENTF_KEYUP = 0x0002;

        private static LowLevelKeyboardProc _keyboardProc = KeyboardHookCallback;
        private static LowLevelMouseProc _mouseProc = MouseHookCallback;
        private static IntPtr _keyboardHookID = IntPtr.Zero;
        private static IntPtr _mouseHookID = IntPtr.Zero;
        private static ManualResetEvent _exitEvent = new ManualResetEvent(false);
        private static bool isInputPressed = false;
        private static int inputKey = 0;
        private static bool isInputMouse = false;
        private static int outputKey = 0;
        private static byte outputScanCode = 0;
        private static bool waitingForInput = false;
        private static bool waitingForOutput = false;
        private static DateTime lastRemapRequest = DateTime.MinValue;

        [STAThread]
        public static void Main()
        {
            Console.WriteLine("Osu! Dual State Mapper");
            Console.WriteLine("---------------------");
            Console.WriteLine("A key mapper that treats press and release as separate held inputs.");

            _keyboardHookID = SetKeyboardHook(_keyboardProc);
            _mouseHookID = SetMouseHook(_mouseProc);

            SetupInitialMapping();
            ShowCurrentMapping();
            ShowMenu();

            Console.CancelKeyPress += (sender, e) => {
                e.Cancel = true;
                _exitEvent.Set();
            };

            while (!_exitEvent.WaitOne(100))
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.R && (DateTime.Now - lastRemapRequest).TotalMilliseconds > 500)
                    {
                        lastRemapRequest = DateTime.Now;
                        Remap();
                    }
                    else if (key.Key == ConsoleKey.Q)
                    {
                        _exitEvent.Set();
                    }
                }
            }

            UnhookWindowsHookEx(_keyboardHookID);
            UnhookWindowsHookEx(_mouseHookID);
        }

        private static void SetupInitialMapping()
        {
            Console.WriteLine("\nPress the physical key or mouse button you want to use as input...");
            Thread.Sleep(500);
            waitingForInput = true;

            while (waitingForInput && !_exitEvent.WaitOne(100)) { }

            Thread.Sleep(1000);

            if (!_exitEvent.WaitOne(0))
            {
                Console.WriteLine("\nPress the key you want it to send (keyboard key only)...");
                Thread.Sleep(500);
                waitingForOutput = true;

                while (waitingForOutput && !_exitEvent.WaitOne(100)) { }

                if (!isInputMouse && inputKey == outputKey)
                {
                    Console.WriteLine("\nWarning: Mapping a key to itself may cause issues.");
                    Console.WriteLine("Press 'R' to try again with different keys, or any other key to continue anyway.");
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.R)
                    {
                        SetupInitialMapping();
                    }
                }
            }
        }

        private static void ShowCurrentMapping()
        {
            Console.WriteLine("\nCurrent mapping:");
            string inputDesc = isInputMouse ? GetMouseButtonName(inputKey) : $"Key 0x{inputKey:X2}";
            Console.WriteLine($"{inputDesc} -> Key 0x{outputKey:X2}");
        }

        private static string GetMouseButtonName(int button)
        {
            switch (button)
            {
                case WM_LBUTTONDOWN:
                    return "Left Mouse";
                case WM_RBUTTONDOWN:
                    return "Right Mouse";
                case WM_MBUTTONDOWN:
                    return "Middle Mouse";
                case WM_XBUTTONDOWN:
                    return "Extra Mouse";
                default:
                    return $"Mouse Button {button}";
            }
        }

        private static void ShowMenu()
        {
            Console.WriteLine("\nCommands:");
            Console.WriteLine("R - Remap keys/buttons");
            Console.WriteLine("Q - Quit");
            Console.WriteLine("\nMapper is only active when Osu! window is focused!");
        }

        private static void Remap()
        {
            Console.WriteLine("\nPress the physical key or mouse button you want to use as input...");
            Thread.Sleep(500);
            waitingForInput = true;

            while (waitingForInput && !_exitEvent.WaitOne(100)) { }

            Thread.Sleep(1000);

            if (!_exitEvent.WaitOne(0))
            {
                Console.WriteLine("\nPress the key you want it to send (keyboard key only)...");
                Thread.Sleep(500);
                waitingForOutput = true;

                while (waitingForOutput && !_exitEvent.WaitOne(100)) { }

                if (!isInputMouse && inputKey == outputKey)
                {
                    Console.WriteLine("\nWarning: Mapping a key to itself may cause issues.");
                    Console.WriteLine("Press 'R' to try again with different keys, or any other key to continue anyway.");
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.R)
                    {
                        Remap();
                        return;
                    }
                }

                ShowCurrentMapping();
                ShowMenu();
            }
        }

        private static bool IsOsuActive()
        {
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, buff, nChars) > 0)
            {
                return buff.ToString().ToLower().Contains("osu!");
            }
            return false;
        }

        private static IntPtr SetKeyboardHook(LowLevelKeyboardProc proc)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, IntPtr.Zero, 0);
        }

        private static IntPtr SetMouseHook(LowLevelMouseProc proc)
        {
            return SetWindowsHookEx(WH_MOUSE_LL, proc, IntPtr.Zero, 0);
        }

        private static void SendKey(bool isKeyDown)
        {
            try
            {
                keybd_event((byte)outputKey, outputScanCode, isKeyDown ? 0 : KEYEVENTF_KEYUP, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending key: {ex.Message}");
            }
        }

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool isKeyDown = wParam == (IntPtr)WM_KEYDOWN;

                if (waitingForInput)
                {
                    inputKey = vkCode;
                    isInputMouse = false;
                    waitingForInput = false;
                    return (IntPtr)1;
                }
                else if (waitingForOutput)
                {
                    outputKey = vkCode;
                    if (vkCode == 0x5A) // Z
                        outputScanCode = 0x2C;
                    else if (vkCode == 0x58) // X
                        outputScanCode = 0x2D;
                    else
                        outputScanCode = 0;
                    waitingForOutput = false;
                    return (IntPtr)1;
                }

                if (!isInputMouse && vkCode == inputKey)
                {
                    if (!IsOsuActive())
                    {
                        return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
                    }

                    if (isKeyDown && !isInputPressed)
                    {
                        isInputPressed = true;
                        SendKey(false);
                        SendKey(true);
                    }
                    else if (!isKeyDown && isInputPressed)
                    {
                        isInputPressed = false;
                        SendKey(false);
                        SendKey(true);
                    }
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int mouseMessage = wParam.ToInt32();

                if (waitingForInput)
                {
                    if (IsMouseDown(mouseMessage))
                    {
                        inputKey = mouseMessage;
                        isInputMouse = true;
                        waitingForInput = false;
                        return (IntPtr)1;
                    }
                    return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
                }

                if (isInputMouse)
                {
                    bool isMouseButton = IsMouseButtonMessage(mouseMessage, inputKey, out bool isDown);
                    if (isMouseButton)
                    {
                        if (!IsOsuActive())
                        {
                            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
                        }

                        if (isDown && !isInputPressed)
                        {
                            isInputPressed = true;
                            SendKey(false);
                            SendKey(true);
                        }
                        else if (!isDown && isInputPressed)
                        {
                            isInputPressed = false;
                            SendKey(false);
                            SendKey(true);
                        }
                        return (IntPtr)1;
                    }
                }
            }
            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        private static bool IsMouseDown(int message)
        {
            return message == WM_LBUTTONDOWN || message == WM_RBUTTONDOWN ||
                   message == WM_MBUTTONDOWN || message == WM_XBUTTONDOWN;
        }

        private static bool IsMouseButtonMessage(int message, int targetButton, out bool isDown)
        {
            isDown = false;

            switch (targetButton)
            {
                case WM_LBUTTONDOWN:
                    if (message == WM_LBUTTONDOWN) isDown = true;
                    return message == WM_LBUTTONDOWN || message == WM_LBUTTONUP;

                case WM_RBUTTONDOWN:
                    if (message == WM_RBUTTONDOWN) isDown = true;
                    return message == WM_RBUTTONDOWN || message == WM_RBUTTONUP;

                case WM_MBUTTONDOWN:
                    if (message == WM_MBUTTONDOWN) isDown = true;
                    return message == WM_MBUTTONDOWN || message == WM_MBUTTONUP;

                case WM_XBUTTONDOWN:
                    if (message == WM_XBUTTONDOWN) isDown = true;
                    return message == WM_XBUTTONDOWN || message == WM_XBUTTONUP;
            }

            return false;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    }
}