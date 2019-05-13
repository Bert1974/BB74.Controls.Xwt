using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using BaseLib.Xwt.Interop;
using Xwt;
using Xwt.Backends;

namespace BaseLib.Xwt
{
    using Xwt = global::Xwt;
    public abstract class Platform
    {
        static Dictionary<string, Type> _typeslooked = new Dictionary<string, Type>();

        public static Type GetType(string typeName)
        {
            if (_typeslooked.TryGetValue(typeName, out Type type))
            {
                return type;
            }
            type = Type.GetType(typeName);
            if (type != null) return (_typeslooked[typeName] = type);
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return (_typeslooked[typeName]=type);
            }
            return null;
        }
        public static void Initialize(ToolkitType type)
        {
            try
            {
                if (Platform.OSPlatform == PlatformID.Unix/* || Platform.OSPlatform == PlatformID.MacOSX*/)
                {
                    switch (type)
                    {
                        case ToolkitType.Gtk3:
                            LoadDlls("3.0");
                            break;
                        case ToolkitType.Gtk:
                            LoadDlls("2.0");
                            break;
                    }
                }
                Application.Initialize(type);

                //_instance = Create();
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public static PlatformID OSPlatform
        {
            get
            {
                if (System.Environment.OSVersion.Platform != PlatformID.MacOSX &&
                    System.Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    return PlatformID.Win32Windows;
                }
                else
                {
                    if (Directory.Exists("/Applications")
                           & Directory.Exists("/System")
                           & Directory.Exists("/Users")
                           & Directory.Exists("/Volumes"))
                    {
                        return PlatformID.MacOSX;
                    }
                    else
                    {
                        return PlatformID.Unix;
                    }
                }
            }
        }
        private static Platform _instance = null;
        public static Platform Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Create();
                }
                return _instance;
            }
        }
        public IEnumerable<Tuple<IntPtr, object>> Search(WindowFrame window, Point pt)
        {
            var disp = GetDisplay(window);
            return AllForms(disp, window.GetBackend()).Where(_t => GetWindowRect(disp, _t.Item2).Contains(pt));
        }
        public IEnumerable<Tuple<IntPtr, object>> AllForms(WindowFrame windowfordisplay)
        {
            var disp = GetDisplay(windowfordisplay);
            return AllForms(disp, windowfordisplay.GetBackend());
        }
        public abstract IEnumerable<Tuple<IntPtr, object>> AllForms(IntPtr display, Xwt.Backends.IWindowFrameBackend window);

        public virtual IntPtr GetDisplay(WindowFrame window) => IntPtr.Zero;
        public abstract Rectangle GetWindowRect(IntPtr display, object form);

        private static void LoadDlls(string dllversion)
        {
            LoadDll("gdk-sharp", dllversion);
            LoadDll("glib-sharp", dllversion);
            LoadDll("pango-sharp", dllversion);
            LoadDll("gdk-sharp", dllversion);
            LoadDll("gtk-sharp", dllversion);
            LoadDll("atk-sharp", dllversion);
        }
        private static void LoadDll(string name, string dllversion)
        {
            if (Platform.OSPlatform == PlatformID.Unix)
            {
                Assembly.LoadFile($"/usr/lib/cli/{name}-{dllversion}/{name}.dll");
            }
            else
            {
                Assembly.Load(new AssemblyName($"{name}, Version={dllversion}"));
            }
        }
        private static Platform Create()
        {
            switch (OSPlatform)
            {
                case PlatformID.Win32Windows: return new Implementation.PlatformWin32();
                case PlatformID.MacOSX:
                    if (Toolkit.CurrentEngine.Type == ToolkitType.XamMac)
                    {
                        return new Implementation.PlatformXamMac();
                    }
                    else if (Toolkit.CurrentEngine.Type == ToolkitType.Gtk)
                    {
                        return new Implementation.PlatformGtkMac();
                    }
                    break;
                case PlatformID.Unix:
                    {
                        if (Toolkit.CurrentEngine.Type == ToolkitType.Gtk3)
                        {
                            return new Implementation.X11_GTK3();
                        }
                        else
                        {
                            return new Implementation.X11_GTK2();
                        }
                    }
            }
            throw new NotImplementedException();
        }
    }
    namespace Implementation
    {
        internal class PlatformXamMac : Platform
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
            static extern bool CFDictionaryContainsKey(IntPtr dict, IntPtr key);
            [DllImport(qlib)]
            static extern int CFDictionaryGetKeysAndValues(IntPtr dict, IntPtr[] keys, IntPtr[] values);

            [DllImport(qlib)]
            static extern bool CGRectMakeWithDictionaryRepresentation(IntPtr dict, out CGRect rect);


            [StructLayout(LayoutKind.Sequential)]
            struct CGRect
            {
                public double x, y, w, h;
            }
            public override IEnumerable<Tuple<IntPtr, object>> AllForms(IntPtr display, IWindowFrameBackend window)
            {
                var wi = CGWindowListCopyWindowInfo(0x1, 0);

                var count = CFArrayGetCount(wi);

                var app = XamMac.appkit_nsapplication.GetPropertyValueStatic("SharedApplication");
                var winarray = (Array)app.GetType().GetPropertyValue(app, "Windows");

                var curapp = XamMac.appkit_nsrunningapp.GetPropertyValueStatic("CurrentApplication");
                var appid = (int)XamMac.appkit_nsrunningapp.GetPropertyValue(curapp, "ProcessIdentifier");

                var kCGWindowIsOnscreen = OpenTK.Platform.MacOS.Cocoa.ToNSString("kCGWindowIsOnscreen");
                var name_window = OpenTK.Platform.MacOS.Cocoa.ToNSString("kCGWindowName");
                var id_window = OpenTK.Platform.MacOS.Cocoa.ToNSString("kCGWindowNumber");
                var id_owner = OpenTK.Platform.MacOS.Cocoa.ToNSString("kCGWindowOwnerPID");
                var kCGWindowBounds = OpenTK.Platform.MacOS.Cocoa.ToNSString("kCGWindowBounds");

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
                            foreach (var nswin in winarray) // AppKit.NSApplication.SharedApplication.Windows
                            {
                                object nint = nswin.GetType().GetPropertyValue(nswin, "WindowNumber");
                                if ((nint as System.IConvertible).ToInt32(null) == windowid)
                                {
                                    yield return new Tuple<IntPtr, object>(new IntPtr(windowid), nswin);
                                }
                            }
                        }
                        else
                        {
                            if (CGRectMakeWithDictionaryRepresentation(CFDictionaryGetValue(cfdict, kCGWindowBounds), out CGRect rr))
                            {
                                var name = OpenTK.Platform.MacOS.Cocoa.FromNSString(CFDictionaryGetValue(cfdict, name_window));
                                object r2 = Activator.CreateInstance(XamMac.cg_cgrect, new object[] { rr.x, rr.y, rr.w, rr.h });
                                var r3 = (Xwt.Rectangle)XamMac.xwtmacbackend.InvokeStatic("ToDesktopRect", r2);

                                if (name != "Dock") // todo check with screenbounds
                                {
                                    yield return new Tuple<IntPtr, object>(new IntPtr(windowid), r3);
                                }
                            }
                        }
                    }
                }
                OpenTK.Platform.MacOS.Cocoa.SendVoid(wi, OpenTK.Platform.MacOS.Selector.Release);
            }
            public override Rectangle GetWindowRect(IntPtr display, object form)
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
        internal class PlatformGtkMac : Platform
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
            static extern bool CFDictionaryContainsKey(IntPtr dict, IntPtr key);
            [DllImport(qlib)]
            static extern int CFDictionaryGetKeysAndValues(IntPtr dict, IntPtr[] keys, IntPtr[] values);

            [DllImport(qlib)]
            static extern bool CGRectMakeWithDictionaryRepresentation(IntPtr dict, out CGRect rect);

            public PlatformGtkMac()
            {
                Platform.GetType("Xwt.Gtk.Mac.MacPlatformBackend").Assembly.GetType("Xwt.Mac.NSApplicationInitializer").InvokeStatic("Initialize");
            }

            [StructLayout(LayoutKind.Sequential)]
            struct CGRect
            {
                public double x, y, w, h;
            }
            
            public Dictionary<int, object> CreateGdkLookup()
            {
                var d = new Dictionary<int, object>();

                foreach (var _gtkwin in ((Array)Gtk.gtk_window.InvokeStatic("ListToplevels")).Cast<object>())
                {
                    var gdkwin = _gtkwin.GetType().GetPropertyValue(_gtkwin, "GdkWindow");
                    if (gdkwin != null)
                    {
                        var nswin = GtkMac.gdk_quartz_window_get_nswindow((IntPtr)gdkwin.GetType().GetPropertyValue(gdkwin, "Handle"));
                        var _windowid = OpenTK.Platform.MacOS.Cocoa.SendIntPtr(nswin, OpenTK.Platform.MacOS.Selector.Get("windowNumber"));
                     //   var windowid = OpenTK.Platform.MacOS.Cocoa.SendInt(_windowid, OpenTK.Platform.MacOS.Selector.Get("intValue"));
                         //   var nsint= nswin.GetType().GetPropertyValue(nswin, "WindowNumber");
                        d[_windowid.ToInt32()] = _gtkwin;
                    }
                }
                return d;
            }
            public override IEnumerable<Tuple<IntPtr, object>> AllForms(IntPtr _display, IWindowFrameBackend window)
            {
                var wi = CGWindowListCopyWindowInfo(0x1, 0);

                var count = CFArrayGetCount(wi);

                var winarray = CreateGdkLookup();

                var curapp = XamMac.appkit_nsrunningapp.GetPropertyValueStatic("CurrentApplication");
                var appid = (int)XamMac.appkit_nsrunningapp.GetPropertyValue(curapp, "ProcessIdentifier");

                var kCGWindowIsOnscreen = OpenTK.Platform.MacOS.Cocoa.ToNSString("kCGWindowIsOnscreen");
                var name_window = OpenTK.Platform.MacOS.Cocoa.ToNSString("kCGWindowName");
                var id_window = OpenTK.Platform.MacOS.Cocoa.ToNSString("kCGWindowNumber");
                var id_owner = OpenTK.Platform.MacOS.Cocoa.ToNSString("kCGWindowOwnerPID");
                var kCGWindowBounds = OpenTK.Platform.MacOS.Cocoa.ToNSString("kCGWindowBounds");

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
                            if (winarray.TryGetValue(windowid, out object gtkwin))
                            {
                                yield return new Tuple<IntPtr, object>(new IntPtr(windowid), gtkwin);
                            }
                        }
                        else
                        {
                            if (CGRectMakeWithDictionaryRepresentation(CFDictionaryGetValue(cfdict, kCGWindowBounds), out CGRect rr))
                            {
                                var name = OpenTK.Platform.MacOS.Cocoa.FromNSString(CFDictionaryGetValue(cfdict, name_window));
                             
                                if (name != "Dock") // todo check with screenbounds
                                {
                                    yield return new Tuple<IntPtr, object>(new IntPtr(windowid), new  Rectangle(rr.x,rr.y,rr.w,rr.h));
                                }
                            }
                        }
                    }
                }
                OpenTK.Platform.MacOS.Cocoa.SendVoid(wi, OpenTK.Platform.MacOS.Selector.Release);
            }
            public override Rectangle GetWindowRect(IntPtr display, object form)
            {
                if (form is IWindowFrameBackend)
                {
                    return (form as IWindowFrameBackend).Bounds;
                }
                if (form is Rectangle)
                {
                    return (Rectangle)form;
                }
                if (form.GetType().Name == "Window")
                {
                    var gdkwin = form.GetType().GetPropertyValue(form, "GdkWindow");
                    var xy = new object[] { 0, 0 };
                    var wh = new object[] { 0, 0 };
                    gdkwin.GetType().Invoke(gdkwin, "GetOrigin", xy);
                    gdkwin.GetType().Invoke(gdkwin, "GetSize", wh);
                    return new Rectangle((int)xy[0], (int)xy[1], (int)wh[0], (int)wh[1]);
                }
                throw new ApplicationException();
            }
        }

        internal class PlatformWin32 : Platform
        {
            public override Rectangle GetWindowRect(IntPtr display, object form)
            {
                IntPtr hwnd;
                if (form is IntPtr)
                {
                    hwnd = (IntPtr)form;
                }
                else if (Xwt.Toolkit.CurrentEngine.Type == ToolkitType.Wpf)
                {
                    var wih = Activator.CreateInstance(Win32.swi_wininterophelper, new object[] { form });
                    hwnd = (IntPtr)wih.GetType().GetPropertyValue(wih, "Handle");
                }
                else if (Xwt.Toolkit.CurrentEngine.Type == ToolkitType.Gtk)
                {
                    var gtkwin = form;
                    var gdkwin = gtkwin.GetType().GetPropertyValue(gtkwin, "GdkWindow");
                    var xy = new object[] { 0, 0 };
                    var wh = new object[] { 0, 0 };
                    gdkwin.GetType().Invoke(gdkwin, "GetOrigin", xy);
                    gdkwin.GetType().Invoke(gdkwin, "GetSize", wh);
                    return new Rectangle((int)xy[0], (int)xy[1], (int)wh[0], (int)wh[1]);
                }
                else
                {
                    throw new NotImplementedException();
                }
                Win32.GetWindowRect(hwnd, out Win32.RECT r);
                return new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
            }
            public override IEnumerable<Tuple<IntPtr, object>> AllForms(IntPtr display, Xwt.Backends.IWindowFrameBackend window)
            {
                var found = new List<IntPtr>();
                Win32.EnumWindowsProc func = (hwnd, lparam) =>
                {
                    if ((Win32.GetWindowLongPtr(hwnd, -16) & 0x10000000L) != 0) // WS_STYLE&WS_VISIBLE
                    {
                        found.Add(hwnd);
                    }
                    return true;
                };
                Win32.EnumWindows(func, IntPtr.Zero);

                if (Xwt.Toolkit.CurrentEngine.Type == ToolkitType.Wpf)
                {
                    return found.Select(_h =>
                   {
                       var obj = Win32.swi_hwndsource.InvokeStatic("FromHwnd", _h);
                       obj = obj?.GetType().GetPropertyValue(obj, "RootVisual");
                       return new Tuple<IntPtr, object>(_h, obj ?? _h);
                   });
                }
                else if (Xwt.Toolkit.CurrentEngine.Type == ToolkitType.Gtk)
                {
                    var d = CreateGtkLookup();

                    return found.Select(_h =>
                    {
                        d.TryGetValue(_h, out object obj);
                        //   var obj = Win32.swi_hwndsource.InvokeStatic("FromHwnd", _h);
                        //    obj = obj?.GetType().GetPropertyValue(obj, "RootVisual");
                        return new Tuple<IntPtr, object>(_h, obj ?? _h);
                    });
                }
                else
                {
                    return found.Select(_h => new Tuple<IntPtr, object>(_h, _h));
                }
            }
            public Dictionary<IntPtr, object> CreateGtkLookup()
            {
                var d = new Dictionary<IntPtr, object>();

                foreach (var _gtkwin in ((Array)Gtk.gtk_window.InvokeStatic("ListToplevels")).Cast<object>())
                {
                    var gdkwin = _gtkwin.GetType().GetPropertyValue(_gtkwin, "GdkWindow");
                    if (gdkwin != null)
                    {
                        d[gdk_win32_drawable_get_handle((IntPtr)gdkwin.GetType().GetPropertyValue(gdkwin, "Handle"))] = _gtkwin;
                    }
                }
                return d;
            }
            const string linux_libgdk_win_name = "libgdk-win32-2.0-0.dll";
            [DllImport(linux_libgdk_win_name, CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr gdk_win32_drawable_get_handle(IntPtr raw);
        }
        internal class X11_GTK2 : X11
        {
            const string libGtk = "libgdk-win32-2.0-0.dll";

            [DllImport(libGtk)]
            internal extern static IntPtr gdk_x11_drawable_get_xid(IntPtr window);

            [DllImport(libGtk)]
            public static extern IntPtr gdk_x11_display_get_xdisplay(IntPtr gdskdisplay);

            protected override IntPtr getxdisplay(IntPtr display)
            {
                return gdk_x11_display_get_xdisplay(display);
            }

            protected override IntPtr getxid(IntPtr gdkwin)
            {
                return gdk_x11_drawable_get_xid(gdkwin);
            }
        }
        internal class X11_GTK3 : X11
        {
            const string libGtk = "libgdk-3-0.dll";

            [DllImport(libGtk)]
            internal extern static IntPtr gdk_x11_window_get_xid(IntPtr window);

            [DllImport(libGtk)]
            public static extern IntPtr gdk_x11_display_get_xdisplay(IntPtr gdskdisplay);

            protected override IntPtr getxdisplay(IntPtr display)
            {
                return gdk_x11_display_get_xdisplay(display);
            }

            protected override IntPtr getxid(IntPtr gdkwin)
            {
                return gdk_x11_window_get_xid(gdkwin);
            }
            public override Rectangle GetWindowRect(IntPtr xdisp, object gtkwin)
            {
                if (!(gtkwin is IntPtr))
                {
                    var gdkwin = gtkwin.GetType().GetPropertyValue(gtkwin, "GdkWindow");
                    var t = gdkwin.GetType();
                    var xy = new object[] { 0, 0 };
                    t.Invoke(gdkwin, "GetOrigin", xy);
                    return new Rectangle((int)xy[0], (int)xy[1], (int)t.GetPropertyValue(gdkwin, "Width"), (int)t.GetPropertyValue(gdkwin, "Height"));
                }
                return base.GetWindowRect(xdisp, gtkwin);
            }
        }
        internal abstract class X11 : Platform
        {
            const string libX11 = "libX11";

            [DllImport(libX11)]
            public extern static int XQueryTree(IntPtr display, IntPtr window, out IntPtr root_return, out IntPtr parent_return, out IntPtr children_return, out int nchildren_return);

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

            protected abstract IntPtr getxdisplay(IntPtr display);
            protected abstract IntPtr getxid(IntPtr gdkwin);

            public override IntPtr GetDisplay(WindowFrame window)
            {
                var gtkkwin = window.GetBackend().Window;
                var gdkdisp = gtkkwin.GetType().GetPropertyValue(gtkkwin, "Display");
                var display = (IntPtr)gdkdisp.GetType().GetPropertyValue(gdkdisp, "Handle");
                var xdisp = getxdisplay(display);
                return xdisp;
            }
            public override IEnumerable<Tuple<IntPtr, object>> AllForms(IntPtr xdisp, Xwt.Backends.IWindowFrameBackend window)
            {
                var gtkkwin = window.Window;
                var gdkscr = gtkkwin.GetType().GetPropertyValue(gtkkwin, "Screen");
                var rw = gdkscr.GetType().GetPropertyValue(gdkscr, "RootWindow");
                var rwh = (IntPtr)rw.GetType().GetPropertyValue(rw, "Handle");
                var rootWindow = getxid(rwh);
                try
                {
                    var d = CreateGdkLookup();
                    return _AllWindows(xdisp, rootWindow, d);
                }
                finally
                {
                }
            }

            public Dictionary<IntPtr, object> CreateGdkLookup()
            {
                var d = new Dictionary<IntPtr, object>();

                foreach (var _gtkwin in ((Array)Gtk.gtk_window.InvokeStatic("ListToplevels")).Cast<object>())
                {
                    var gdkwin = _gtkwin.GetType().GetPropertyValue(_gtkwin, "GdkWindow");
                    if (gdkwin != null)
                    {
                        d[getxid((IntPtr)gdkwin.GetType().GetPropertyValue(gdkwin, "Handle"))] = _gtkwin;
                    }
                }
                return d;
            }

            private IEnumerable<Tuple<IntPtr, object>> _AllWindows(IntPtr display, IntPtr rootWindow, Dictionary<IntPtr, object> gtkwins, int level = 0)
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
                            if (gtkwins.TryGetValue(children[nit], out object gtkwin))
                            {
                                yield return new Tuple<IntPtr, object>(children[nit], gtkwin);
                            }
                            else
                            {
                                var attr = new XWindowAttributes();
                                XGetWindowAttributes(display, children[nit], ref attr);

                                if (attr.map_state == 2)
                                {
                                    if (level < 2)
                                    {
                                        bool fnd = false;
                                        foreach (var form in _AllWindows(display, children[nit], gtkwins, level + 1))
                                        {
                                            if (form.Item2 != null)
                                            {
                                                fnd = true;
                                                yield return form;
                                                break;
                                            }
                                        }
                                        if (!fnd && level == 0)
                                        {
                                            yield return new Tuple<IntPtr, object>(children[nit], children[nit]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        XFree(children_return);
                    }
                }
            }
            public override Rectangle GetWindowRect(IntPtr xdisp, object gtkwin)
            {
                if (gtkwin is IntPtr)
                {
                    var attr = new XWindowAttributes();
                    XGetWindowAttributes(xdisp, (IntPtr)gtkwin, ref attr);
                    return new Rectangle(attr.x, attr.y, attr.width, attr.height);
                }
                else
                {
                    var gdkwin = gtkwin.GetType().GetPropertyValue(gtkwin, "GdkWindow");
                    var xy = new object[] { 0, 0 };
                    var wh = new object[] { 0, 0 };
                    gdkwin.GetType().Invoke(gdkwin, "GetOrigin", xy);
                    gdkwin.GetType().Invoke(gdkwin, "GetSize", wh);
                    return new Rectangle((int)xy[0], (int)xy[1], (int)wh[0], (int)wh[1]);
                }
            }
        }
    }
}