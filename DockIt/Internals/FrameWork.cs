using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xwt;
using Xwt.Backends;

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
                if (Directory.Exists("/Applications")
                       & Directory.Exists("/System")
                       & Directory.Exists("/Users")
                       & Directory.Exists("/Volumes"))
                {
                    return new Cocoa(); 
                }
                else
                {
                    return new X11();
                }
            }
            //   throw new NotImplementedException("Platform");
        }
        
        public IEnumerable<Tuple<IntPtr, object>> Search(Window window, Point pt)
        {
            foreach (var form in AllForms(window.GetBackend() as IWindowFrameBackend))
            {
                Rectangle r = GetWindowRect(form.Item2);

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

        protected abstract Rectangle GetWindowRect(object form);
        
        public abstract IEnumerable<Tuple<IntPtr, object>> AllForms(Xwt.Backends.IWindowFrameBackend window);

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

    internal class Cocoa : PlatForm
    {
     //   const string qlib = "/System/Library/Frameworks/QuartzCore.framework/QuartzCore";
        const string qlib = @"/System/Library/Frameworks/ApplicationServices.framework/Frameworks/CoreGraphics.framework/CoreGraphics";

        [DllImport(qlib)]
        static extern IntPtr CGWindowListCopyWindowInfo(int option, uint relativeToWindow);

        [DllImport(qlib)]
        static extern IntPtr CFArrayGetValueAtIndex(IntPtr array, int index);
        [DllImport(qlib)]
        static extern int CFArrayGetCount(IntPtr array);

        [DllImport(qlib)]
        static extern IntPtr CFDictionaryGetValue(IntPtr dict, IntPtr key);
        [DllImport(qlib)]
        static extern int CFDictionaryGetCount(IntPtr dict);
        [DllImport(qlib)]
        static extern int CFDictionaryGetKeysAndValues(IntPtr dict, IntPtr[] keys, IntPtr[] values);

        [DllImport(qlib)]
        static extern bool CGRectMakeWithDictionaryRepresentation(IntPtr dict, out CGRect rect);


        [StructLayout(LayoutKind.Sequential)]
        struct CGRect
        {
           public double x, y, w, h;
        }
        public override IEnumerable<Tuple<IntPtr, object>> AllForms(IWindowFrameBackend window)
        {
            var wi = CGWindowListCopyWindowInfo(0x11, 0);

            var count = CFArrayGetCount(wi);

            var xwtmacbackend = XwtImpl.GetType("Xwt.Mac.MacDesktopBackend");
            var cgrecttype = XwtImpl.GetType("CoreGraphics.CGRect");

            var typensapp = XwtImpl.GetType("AppKit.NSApplication");
            var app = typensapp.GetPropertyValueStatic("SharedApplication");
            var winarray = (Array)app.GetType().GetPropertyValue(app, "Windows");

            var typensrunapp = XwtImpl.GetType("AppKit.NSRunningApplication");
            var curapp = typensrunapp.GetPropertyValueStatic("CurrentApplication");
            var appid = (int)typensrunapp.GetPropertyValue(curapp, "ProcessIdentifier");

            var kCGWindowIsOnscreen = OpenTK.Platform.MacOS.Cocoa.ToNSString("kCGWindowIsOnscreen");
            var id_window = OpenTK.Platform.MacOS.Cocoa.ToNSString("kCGWindowNumber");
            var id_owner = OpenTK.Platform.MacOS.Cocoa.ToNSString("kCGWindowOwnerPID");
            var kCGWindowBounds = OpenTK.Platform.MacOS.Cocoa.ToNSString("kCGWindowBounds");

            IntPtr NSNumber = OpenTK.Platform.MacOS.Class.Get("NSNumber");

            for (int nit = 0; nit < count; nit++)
            {
                var cfdict = CFArrayGetValueAtIndex(wi, nit);

                var visible = OpenTK.Platform.MacOS.Cocoa.SendBool(CFDictionaryGetValue(cfdict, kCGWindowIsOnscreen), OpenTK.Platform.MacOS.Selector.Get("boolValue"));

                if (visible)
                {
                    var windowid = OpenTK.Platform.MacOS.Cocoa.SendInt(CFDictionaryGetValue(cfdict, id_window), OpenTK.Platform.MacOS.Selector.Get("intValue"));
                    var i2 = OpenTK.Platform.MacOS.Cocoa.SendInt(CFDictionaryGetValue(cfdict, id_owner), OpenTK.Platform.MacOS.Selector.Get("intValue"));

                    if (i2 == appid)
                    {
                        foreach (var nswin in winarray)
                        {
                            object nint = nswin.GetType().GetPropertyValue(nswin, "WindowNumber");
                            if ((nint as System.IConvertible).ToInt32(null) == windowid)
                            {
                                yield return new Tuple<IntPtr, object>(new IntPtr(windowid), nswin);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (CGRectMakeWithDictionaryRepresentation(CFDictionaryGetValue(cfdict, kCGWindowBounds), out CGRect rr))
                        {
                            object r2 = Activator.CreateInstance(cgrecttype, new object[] { rr.x, rr.y, rr.w, rr.h });
                            var r3=(Xwt.Rectangle)xwtmacbackend.InvokeStatic("ToDesktopRect",r2);
                            yield return new Tuple<IntPtr, object>(new IntPtr(windowid), r3);
                            continue;
                        }
                    }
                }
            }
            OpenTK.Platform.MacOS.Cocoa.SendVoid(wi, OpenTK.Platform.MacOS.Selector.Release);
        }
        protected override Rectangle GetWindowRect(object form)
        {
            if (form is IWindowFrameBackend)
            {
                return (form as IWindowFrameBackend).Bounds;
            }
            if (form is Rectangle)
            {
                return (Rectangle)form;
            }
            throw new ApplicationException();
        }
    }

    internal class Win32 : PlatForm
    {
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        
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

        protected override Rectangle GetWindowRect(object form)
        {
            var t = XwtImpl.GetType("System.Windows.Interop.WindowInteropHelper");
            var wih = Activator.CreateInstance(t, new object[] { form });
            var hwnd = (IntPtr)wih.GetType().GetPropertyValue(wih, "Handle");
            RECT r = new RECT();
            GetWindowRect(hwnd, out r);
            return new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
        }
        public override IEnumerable<Tuple<IntPtr, object>> AllForms(Xwt.Backends.IWindowFrameBackend window)
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

            if (Xwt.Toolkit.CurrentEngine.Type == ToolkitType.Wpf)
            {
                var t = XwtImpl.GetType("System.Windows.Interop.HwndSource");

                return found.Select(_h =>
               {
                   var obj = t.InvokeStatic("FromHwnd", _h);
                   obj = obj?.GetType().GetPropertyValue(obj, "RootVisual");
                   return new Tuple<IntPtr, object>(_h, obj);
               }).Where(_t => _t.Item2 != null);
            }
            return found.Select(_h=>new Tuple<IntPtr, object>(_h,_h));
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
            public IntPtr screen;            /* back pointer to correct screen */
        }

        public override IEnumerable<Tuple<IntPtr, object>> AllForms(Xwt.Backends.IWindowFrameBackend window)
        {
            //    var display = XOpenDisplay(IntPtr.Zero);
            try
            {
          //    var gtkkwin = window.Window;
           //     var gdkdisp = gtkkwin.GetType().GetPropertyValue(gtkkwin, "Display");
            //    var gdkscr = gtkkwin.GetType().GetPropertyValue(gtkkwin, "Screen");
            //    var rw = gdkscr.GetType().GetPropertyValue(gdkscr, "RootWindow");
            //   var h = (IntPtr)disp.GetType().GetPropertyValue(disp, "Handle");
             //   var screen = XDefaultScreen(display);
             //   var rootWindow = XRootWindow(display, screen);

                //    window.Window;

                foreach (var form in _AllWindows(null, null))
                {
                    yield return new Tuple<IntPtr, object>((IntPtr)form.GetType().GetPropertyValue(form,"Handle"), form);
                }
            }
            finally
            {
            //    XCloseDisplay(display);
            }
        }


        [DllImport("libgdk-win32-2.0-0.dll")]
        public static extern IntPtr gdk_x11_display_get_xdisplay(IntPtr gdskdisplay);

        private IEnumerable<object> _AllWindows(object display, object win)
        {
            Type t = XwtImpl.GetType("Gtk.Window");
            var a= t.InvokeStatic("ListToplevels");
            return ((Array)a).Cast<object>().Reverse();
         //   var wins = (Array)win.GetType().GetPropertyValue(win, "Children");

         //          return wins.Cast<Object>();

          /*  var xdisp = gdk_x11_display_get_xdisplay((IntPtr)display.GetType().GetPropertyValue(display, "Handle"));
            var visual=win.GetType().GetPropertyValue(win,"Visual);")
            var status = XQueryTree(
                    xdisp, 
                    (IntPtr)win.GetType().GetPropertyValue(win,"Handle"),
                     out IntPtr root_return, out IntPtr parent_return, out IntPtr children_return, out int nchildren_return);

            if (nchildren_return > 0)
            {
                try
                {
                    IntPtr[] children = new IntPtr[nchildren_return];
                    Marshal.Copy(children_return, children, 0, nchildren_return);

                    for (int nit = children.Length - 1; nit >= 0; nit--)
                    {
                        yield return children[nit];
                    }
                }
                finally
                {
                    XFree(children_return);
                }
            }*/
        }
        protected override Rectangle GetWindowRect(object gtkwin)
        {
            var v1 = new object[] { 0, 0 };
            var v2 = new object[] { 0, 0 };

            gtkwin.GetType().Invoke(gtkwin, "GetPosition", v1);
            gtkwin.GetType().Invoke(gtkwin, "GetSize", v2);

            //var r = gdkwin.GetType().GetPropertyValue(gdkwin, "FrameExtents");
            //   var display = gdkwin.GetType().GetPropertyValue(gdkwin, "Display");
            //   var attr = new XWindowAttributes();
            //   XGetWindowAttributes((IntPtr)display.GetType().GetPropertyValue(display,"Handle"), (IntPtr)gdkwin.GetType().GetPropertyValue(gdkwin, "Handle"), ref attr);
            return new Rectangle((int)v1[0], (int)v1[1], (int)v2[0], (int)v2[1]);
        }
    }
}