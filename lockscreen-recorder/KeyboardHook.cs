using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lockscreen_recorder
{
    public class KeyboardHook
    {
        struct KeyEvent
        {
            public Keys key;
            public bool pressed;
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        private IntPtr hookId = IntPtr.Zero;
        private ConcurrentQueue<KeyEvent> queue = new ConcurrentQueue<KeyEvent>();
        private Thread? dispatcher = null;


        // Define the low-level keyboard callback delegate
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Import necessary Win32 API functions using P/Invoke
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // Constructor to set up the keyboard hook
        public KeyboardHook()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                hookId = SetWindowsHookEx(WH_KEYBOARD_LL, HookCallback, GetModuleHandle(curModule.ModuleName), 0);
            }

            dispatcher = new Thread(DispatchEvents);
            dispatcher.Start();
        }

        private void DispatchEvents()
        {
            while (Valid())
            {
                if (queue.IsEmpty)
                {
                    Thread.Sleep(100);
                    continue;
                }

                KeyEvent keyEvent;
                while (!queue.IsEmpty)
                {
                    if (queue.TryDequeue(out keyEvent))
                    {
                        DispatchEvent(keyEvent);
                    }
                }
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                OnKeyPressed((Keys)vkCode, true);
            }

            else if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                OnKeyPressed((Keys)vkCode, false);
            }

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        // Event handler for key press event
        public event Action<Keys, bool> KeyPressed;

        private void DispatchEvent(KeyEvent keyEvent)
        {
            KeyPressed?.Invoke(keyEvent.key, keyEvent.pressed);
        }

        protected virtual void OnKeyPressed(Keys key, bool isPressed)
        {
            var keyEvent = new KeyEvent();
            keyEvent.key = key;
            keyEvent.pressed = isPressed;

            queue.Enqueue(keyEvent);
        }

        public void Cleanup()
        {
            lock (this)
            {
                if (hookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(hookId);
                    hookId = IntPtr.Zero;
                }
            }

            dispatcher?.Join();
            dispatcher = null;
        }

        public bool Valid()
        {
            lock (this)
            {
                return hookId != IntPtr.Zero;
            }
        }

        // Dispose method to clean up the hook
        public void Dispose()
        {
            Cleanup();
        }
    }
}
