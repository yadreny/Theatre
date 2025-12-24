using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AlSo
{
    public class WindowManipulator
    {
        protected Process Process { get; }

        public WindowManipulator(Process process)
        {
            Process = process;
        }

        public void MoveWindow(Rect rect) => MoveWindow((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        public void MoveWindow(int x, int y, int width, int height)
            => SetWindowPos(MainWindowHandle, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_SHOWWINDOW);
        public void Minimize() => SendMessage(MainWindowHandle, WM_SYSCOMMAND, SC_MINIMIZE, 0);
        public void Resotre() => SendMessage(MainWindowHandle, WM_SYSCOMMAND, SC_RESTORE, 0);
        public void BringToFront() => SetForegroundWindow(MainWindowHandle);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);


        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_RESTORE = 0xF120;

        private IntPtr MainWindowHandle=> Process.MainWindowHandle;
    }
}