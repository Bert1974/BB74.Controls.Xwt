using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xwt;

namespace BaseLib.DockIt_Xwt
{
    public abstract class PlatForm
    {
        public static readonly PlatForm Instance = PlatForm.Create(); // forms-enumerator

        private static PlatForm Create()
        {
            if (System.Environment.OSVersion.Platform != PlatformID.MacOSX &&
                System.Environment.OSVersion.Platform != PlatformID.Unix)
            {
                return new Win32();
            }
            /*      else if (System.Environment.OSVersion.Platform == PlatformID.MacOSX)
                  {
                      //todo implemenent
                      throw new NotImplementedException("OSX PlatForm");
                  }*/
            else
            {
                return new X11();
            }
         //   throw new NotImplementedException("Platform");
        }
        
        public IEnumerable<Tuple<IntPtr,IntPtr>> Search(IntPtr display, Point pt)
        {
            foreach (var form in AllForms(display))
            {
                Rectangle r = GetWindowRect(form.Item1,form.Item2);

           //     if (!(form is BaseLib.DockIt.Internals.DragForm) && form.Visible)
                {
                    if (pt.X >= r.X && pt.X < r.X + r.Width &&
                        pt.Y >= r.Y && pt.Y < r.Y + r.Height)
                    {
                        yield return form;//Search(form, form.PointToClient(pt), Point.Empty);
                    }
                }
            }
            //return null;
        }

     //   public abstract object GetWindow(IntPtr handle);

        protected abstract Rectangle GetWindowRect(IntPtr display, IntPtr form);

        /*public virtual void SetPos(Control ctl, Rectangle r)
          {
              ctl.SuspendLayout();
              ctl.Location = r.Location;
              ctl.Size = r.Size;
              ctl.ResumeLayout(true);
          }*/

        public abstract IEnumerable<Tuple<IntPtr,IntPtr>> AllForms(IntPtr display);

      /*  private Control Search(Control form, Point point, Point bp)
        {
            foreach (Control ctl in form.Controls)
            {
                Rectangle r = new Rectangle(bp.X + ctl.Location.X, bp.Y + ctl.Location.Y, ctl.Width, ctl.Height);

                if (r.Contains(point))
                {
                    return Search(ctl, point, r.Location) ?? ctl;
                }
            }
            return null;
        }*/
    }
    internal class Win32 : PlatForm
    {
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        static extern int GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        protected override Rectangle GetWindowRect(IntPtr display, IntPtr form)
        {
            RECT r = new RECT();
            GetWindowRect(form, out r);
            return new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
        }

        public override IEnumerable<Tuple<IntPtr,IntPtr>> AllForms(IntPtr display)
        {
            var found = new List<IntPtr>();
            EnumWindowsProc func = (hwnd, lparam) =>
            {
                if ((GetWindowLongPtr(hwnd, -16) & 0x10000000L) != 0) // WS_STYLE&WS_VISIBLE
                {
                    found.Add(hwnd);
                }
                return true;
            };
            EnumWindows(func, IntPtr.Zero);

            return found.Select(_h=>new Tuple<IntPtr,IntPtr>(IntPtr.Zero,_h));
        }

     /*   public override object GetWindow(IntPtr handle)
        {
            var backend = this.Window.

            var t = XwtImpl.GetType("System.Windows.Interop.HwndSource");
            var hwndhit = t.InvokeStatic("FromHwnd", new object[] { handle });

            if (hwndhit != null)
            {
            }
        }*/
/*  public override void SetPos(Control ctl, Rectangle r)
{
const short SWP_NOZORDER = 0X4;
const short SWP_NOCOPYBITS = 0x0100;
const short SWP_NOACTIVATE = 0x0010;
SetWindowPos(ctl.Handle, IntPtr.Zero, r.X, r.Y, r.Width, r.Height, SWP_NOZORDER| SWP_NOCOPYBITS| SWP_NOACTIVATE);
}*/
            }
    internal class X11 : PlatForm
    {
        const string libX11 = "libX11";

        [DllImport(libX11)]
        public extern static IntPtr XOpenDisplay(IntPtr display);

        [DllImport(libX11)]
        public extern static int XCloseDisplay(IntPtr display);

        [DllImport(libX11)]
        public extern static int XQueryTree(IntPtr display, IntPtr window, out IntPtr root_return, out IntPtr parent_return, out IntPtr children_return, out int nchildren_return);

        [DllImport(libX11)]
        public extern static IntPtr XRootWindow(IntPtr display, int screen_number);

        [DllImport(libX11)]
        public extern static int XDefaultScreen(IntPtr display);

        [DllImport(libX11)]
        public extern static int XFree(IntPtr data);

        [DllImport(libX11)]
        public extern static void XGetWindowAttributes(IntPtr display, IntPtr hwnd, ref XWindowAttributes wndAttr);

        [StructLayout(LayoutKind.Sequential)]
        internal struct XWindowAttributes
        {
            public int x, y;           /* location of window */
            public int width, height;      /* width and height of window */
            public int border_width;       /* border width of window */
            public int depth;          /* depth of window */
            public IntPtr visual;         /* the associated visual structure */
            public IntPtr root;            /* root of screen containing window */
            public int _class;          /* InputOutput, InputOnly*/
            public int bit_gravity;        /* one of the bit gravity values */
            public int win_gravity;        /* one of the window gravity values */
            public int backing_store;      /* NotUseful, WhenMapped, Always */
            public ulong backing_planes;   /* planes to be preserved if possible */
            public ulong backing_pixel;    /* value to be used when restoring planes */
            public int/*Bool*/ save_under;        /* boolean, should bits under be saved? */
            public IntPtr colormap;      /* color map to be associated with window */
            public int/*Bool*/ map_installed;     /* boolean, is color map currently installed*/
            public int map_state;          /* IsUnmapped, IsUnviewable, IsViewable */
            public long all_event_masks;       /* set of events all people have interest in*/
            public long your_event_mask;       /* my event mask */
            public long do_not_propagate_mask; /* set of events that should not propagate */
            public int/*Bool*/ override_redirect;     /* boolean value for override-redirect */
            public IntPtr screen;			/* back pointer to correct screen */
        }

        public override IEnumerable<Tuple<IntPtr,IntPtr>> AllForms(IntPtr display)
        {
            //    var display = XOpenDisplay(IntPtr.Zero);
            try
            {
                var screen = XDefaultScreen(display);
                var rootWindow = XRootWindow(display, screen);

                foreach (var form in _AllWindows(display, rootWindow))
                {
                    yield return new Tuple<IntPtr,IntPtr>(display,form);
                }
            }
            finally
            {
                XCloseDisplay(display);
            }
        }
        private IEnumerable<IntPtr> _AllWindows(IntPtr display, IntPtr rootWindow)
        {
            var status = XQueryTree(display, rootWindow, out IntPtr root_return, out IntPtr parent_return, out IntPtr children_return, out int nchildren_return);

            if (nchildren_return > 0)
            {
                try
                {
                    IntPtr[] children = new IntPtr[nchildren_return];
                    Marshal.Copy(children_return, children, 0, nchildren_return);

                    for (int nit = children.Length - 1; nit >= 0; nit--)
                    {
                        /*   Control ctl = Control.FromHandle(children[nit]);

                           if (ctl != null)
                           {
                               if (ctl is Form)
                               {
                                   yield return ctl as Form;
                               }
                           }
                           else
                           {
                               foreach (var form in _AllWindows(display, children[nit]))
                               {
                                   yield return form;
                               }
                           }*/
                        yield return children[nit];
                    }
                }
                finally
                {
                    XFree(children_return);
                }
            }
        }

        protected override Rectangle GetWindowRect(IntPtr display, IntPtr form)
        {
            var attr = new XWindowAttributes();
            XGetWindowAttributes(display, form, ref attr);
            return new Rectangle(attr.x, attr.y, attr.width, attr.height);
        }
    }
}