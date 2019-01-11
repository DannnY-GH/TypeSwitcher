using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using static _CS__TypeSwitcher.WinAPI;

namespace _CS__TypeSwitcher {
    public partial class MainWindow : Window {
        private static LowLevelKeyboardProc _kbdProc = KeyboardHookCallback;
        private static LowLevelMouseProc _mouseProc = MouseHookCallback;
        private static IntPtr keyboardHookID = IntPtr.Zero;
        private static IntPtr mouseHookID = IntPtr.Zero;
        private static IntPtr NON_ZERO = (IntPtr)1;
        public static bool captureKBD = false;
        public static bool captureMouse = false;
        TaskbarIcon tbi;
        static Emuledit edt;
        public MainWindow() {
            SetupHooks();
            InitializeComponent();
            edt = new Emuledit();
            tbi = (TaskbarIcon)FindResource("MainNotifyIcon");
        }
        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (!captureKBD)
                return CallNextHookEx(keyboardHookID, nCode, wParam, lParam);
            KBDLLHOOKSTRUCT kbd = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
            if (kbd.scanCode == 0)
                return CallNextHookEx(keyboardHookID, nCode, wParam, lParam);
            bool isKeyEaten = false;
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) {
                int vkCode = Marshal.ReadInt32(lParam);
                isKeyEaten = edt.ProcessKeyPress(vkCode, KeyState.Down);
                edt.PrintContent();
            }
            else {
                int vkCode = Marshal.ReadInt32(lParam);
                isKeyEaten = edt.ProcessKeyPress(vkCode, KeyState.Up);
                edt.PrintContent();
            }
            if (isKeyEaten)
                return NON_ZERO;
            else
                return CallNextHookEx(keyboardHookID, nCode, wParam, lParam);
        }
        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (!captureMouse)
                return CallNextHookEx(mouseHookID, nCode, wParam, lParam);
            if (nCode >= 0 && MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam) {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                Console.WriteLine(hookStruct.pt.x + ", " + hookStruct.pt.y);
                edt.Reset();
            }
            return CallNextHookEx(mouseHookID, nCode, wParam, lParam);
        }
        private static void UnhookAll() {
            UnhookWindowsHookEx(keyboardHookID);
            captureKBD = false;
            UnhookWindowsHookEx(mouseHookID);
            captureMouse = false;
        }
        private static void SetupHooks() {
            keyboardHookID = SetHook(_kbdProc);
            captureMouse = true;
            mouseHookID = SetHook(_mouseProc);
            captureKBD = true;
        }
        private void MainWindowClosed(object sender, EventArgs e) {
            tbi.Dispose();
            UnhookAll();
        }
    }
}
