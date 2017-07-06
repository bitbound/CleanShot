using CleanShot.Models;
using CleanShot.Windows;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Interop;

namespace CleanShot.Classes
{
    // Code for Hotkey class derived from here: https://blogs.msdn.microsoft.com/toub/2006/05/03/low-level-keyboard-hook-in-c/
    public static class Hotkey
    {
        public static bool IsHotkeySet { get; set; }
        public const int PrintScreen = 0x2C;
        public const int ScrollLock = 0x91;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

       public static void Set()
        {
            if (!Hotkey.IsHotkeySet)
            {
                _hookID = SetHook(_proc);
                App.Current.Exit += (send, arg) =>
                {
                    UnhookWindowsHookEx(_hookID);
                };
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == PrintScreen)
                {
                    if (Screenshot.Current?.IsVisible != true)
                    {
                        Settings.Current.CaptureMode = Settings.CaptureModes.Image;
#pragma warning disable
                        MainWindow.Current.InitiateCapture();
#pragma warning restore
                        return new IntPtr(1);
                    }
                }
                else if (vkCode == ScrollLock)
                {
                    if (Screenshot.Current?.IsVisible != true)
                    {
                        Settings.Current.CaptureMode = Settings.CaptureModes.Video;
#pragma warning disable
                        MainWindow.Current.InitiateCapture();
#pragma warning restore
                        return new IntPtr(1);
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
