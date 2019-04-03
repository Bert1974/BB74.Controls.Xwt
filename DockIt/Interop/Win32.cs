using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xwt;

namespace BaseLib.DockIt_Xwt.Interop
{
    static class Win32
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(ref POINT lpPoint);

        public struct POINT
        {
            public int x, y;
            public static implicit operator Point(POINT pt)
            {
                return new Point(pt.x, pt.y);
            }
        }
    }
}
