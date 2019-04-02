using BaseLib.XwtPlatForm;
using System;
using System.Runtime.InteropServices;
using Xwt;
using Xwt.Backends;

namespace BaseLib.DockIt_Xwt
{
    partial class XwtImpl
    {
        class GTK : IXwtImpl
        {
            class DragWindow : XwtImpl.DragWindow
            {
                public DragWindow(IXwt xwt, Canvas widget, Point position)
                    : base(xwt, widget, position, false)
                {
                }
                public override void Show(out IDockPane dockpane, out DockPosition? dockat)
                {
                    try
                    {
                        this.doexit = false;
                        this.result = true;

                        (this as Window).Show();
                        this.Content.SetFocus();

                        while (!this.doexit)
                        {
                            var gtkwin = (this.GetBackend() as IWindowFrameBackend).Window;
                            var display = gtkwin.GetType().GetPropertyValue(gtkwin, "Display");
                            var screen = display.GetType().GetPropertyValue(display, "DefaultScreen");

                            Type t = PlatForm.GetType("Gdk.ModifierType");

                            var parms = new object[] { 0, 0, Enum.ToObject(t, 0) };
                            var mi = display.GetType().GetMethod("GetPointer", new Type[] { Type.GetType("System.Int32&"), Type.GetType("System.Int32&"), PlatForm.GetType("Gdk.ModifierType&") });
                            mi.Invoke(display, parms);
                            //                        display.GetType().Invoke(display, "GetPointer", parms);
                            //   display.GetPointer(out int x, out int y, out Gdk.ModifierType mask);
                            int x = (int)parms[0];
                            int y = (int)parms[1];
                            int mask = (int)parms[2];

                            this.doexit = (mask & 0x100) == 0;

                            this.Location = new Point(x, y).Offset(-5, -5);

                            this.CheckMove(new Point(x, y), true);

                            this.xwt.DoEvents();
                        }

                        this.xwt.ReleaseCapture(this.Content);
                        DockPanel.ClrHightlight();
                        this.Close();

                        base.SetResult(out dockpane, out dockat);
                    }
                    catch (Exception e)
                    {
                        throw;
                    }
                }
            }
            /*       [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "/*")]
                   private static extern int x11_gdk_device_grab(IntPtr device, IntPtr gdkwindow, int ownerevents, bool owner_events, int mask, IntPtr cursor, int time);
                   [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "gdk_device_ungrab")]
                   private static extern int x11_gdk_device_ungrab(IntPtr device, int time);*/

            [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "gdk_pointer_grab")]
            private static extern int x11_gdk_pointer_grab(IntPtr gdkwindow, bool owner_events, int mask, IntPtr confine_to_gdkwin, IntPtr cursor, long time);
            [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "gdk_pointer_grab", CallingConvention = CallingConvention.Cdecl)]
            private static extern int win_gdk_pointer_grab(IntPtr gdkwindow, bool owner_events, int mask, IntPtr confine_to_gdkwin, IntPtr cursor, long time);

            [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "gdk_pointer_ungrab")]
            private static extern void x11_gdk_pointer_ungrab(int time);
            [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "gdk_pointer_ungrab", CallingConvention = CallingConvention.Cdecl)]
            private static extern void win_gdk_pointer_ungrab(int time);
         
            [DllImport("libgdk-win32-2.0-0.dll")]
            internal extern static IntPtr gdk_x11_drawable_get_xid(IntPtr window);

            [DllImport("libgdk-win32-2.0-0.dll")]
            public static extern IntPtr gdk_x11_display_get_xdisplay(IntPtr gdskdisplay);
            
            public void ReleaseCapture(Widget widget)
            {
            /*    var gtkwin = (widget.GetBackend() as IWidgetBackend).NativeWidget;
                var gdkwin = gtkwin.GetType().GetPropertyValue(gtkwin, "GdkWindow");
                var gdkdisp = gtkwin.GetType().GetPropertyValue(gtkwin, "Display");
                var gdkdisplay = (IntPtr)gdkdisp.GetType().GetPropertyValue(gdkdisp, "Handle");
                var xdisplay = gdk_x11_display_get_xdisplay(gdkdisplay);
                //  XUngrabPointer(xdisplay, 0); ;*/
                x11_gdk_pointer_ungrab(0);
            }

            public void SetCapture(Widget widget)
            {
                var gtkwin = (widget.GetBackend() as IWidgetBackend).NativeWidget;
                var gdkwin = gtkwin.GetType().GetPropertyValue(gtkwin, "GdkWindow");
           /*     var gdkdisp = gtkwin.GetType().GetPropertyValue(gtkwin, "Display");
                //      var gdkscr = gtkwin.GetType().GetPropertyValue(gtkwin, "Screen");
                //        var rw = gdkscr.GetType().GetPropertyValue(gdkscr, "RootWindow");
                var gdkdisplay = (IntPtr)gdkdisp.GetType().GetPropertyValue(gdkdisp, "Handle");
                var xdisplay = gdk_x11_display_get_xdisplay(gdkdisplay);

                //      var root = gdk_x11_drawable_get_xid((IntPtr)rw.GetType().GetPropertyValue(rw, "Handle"));// DefaultRootWindow(display);
                //       var test = XRootWindow(xdisplay, 0);
                //      var test2 = XRootWindow(gdkdisplay, 0);
                //         var test = DefaultRootWindow(gdkdisplay);
                */
                var r = x11_gdk_pointer_grab((IntPtr)gdkwin.GetType().GetPropertyValue(gdkwin, "Handle"), true, (1 << 2) | (1 << 4) | (1 << 5) | (1 << 8) | (1 << 9), IntPtr.Zero, IntPtr.Zero, 0);
                
            //    var window = gdk_x11_drawable_get_xid((IntPtr)gdkwin.GetType().GetPropertyValue(gdkwin, "Handle"));// DefaultRootWindow(display);

                //   var r = XGrabPointer(xdisplay, window, false, (XEventMask)((1 << 2) | (1 << 4) | (1<<5) | (1 << 8) | (1 << 9)), XGrabMode.GrabModeSync, XGrabMode.GrabModeSync, IntPtr.Zero, IntPtr.Zero, 0);
             
#if (false)
                 var backend = Toolkit.CurrentEngine.GetSafeBackend(widget);
                var w = backend.GetType().GetPropertyValue(backend, "Widget");
                var gdk = w.GetType().GetPropertyValue(w, "GdkWindow");
                var h = (IntPtr)gdk.GetType().GetPropertyValue(gdk, "Handle");
                /*  var gdk = w.GetType().GetPropertyValue(w, "GdkWindow");
                  var disp = gdk.GetType().GetPropertyValue(gdk, "Display");
                  var scr= gdk.GetType().GetPropertyValue(gdk, "Screen");
                  var devs = disp.GetType().GetPropertyValue(disp, "DeviceManager");
                  var dev = devs.GetType().GetPropertyValue(devs, "ClientPointer");
                  var h = (IntPtr)gdk.GetType().GetPropertyValue(gdk, "Handle");

                int r= x11_gdk_device_grab((IntPtr)dev.GetType().GetPropertyValue(dev, "Handle"), h, 2, true, (1 << 5), IntPtr.Zero, 0);*/

                var r = x11_gdk_pointer_grab(h, false, (1 << 2) | (1 << 4) | (1 << 8) | (1 << 9), IntPtr.Zero, IntPtr.Zero, 0);
                Console.WriteLine($"grab={r}");
#endif
            }

            public XwtImpl.DragWindow Create(Canvas widget, Point position)
            {
                return new DragWindow(this, widget, position);
            }
            public void DoEvents()
            {
                Type tctx = PlatForm.GetType("Gtk.Application");
                var t2 = PlatForm.GetType("Gdk.Threads");

                var mi_iteration = tctx.GetMethod("RunIteration", new Type[] { typeof(bool) });

                t2.InvokeStatic("Enter");
                int n = 500;
                
                while ((bool)tctx.InvokeStatic("EventsPending") && --n > 0)
                {
                    mi_iteration.Invoke(null, new object[] { false });
                }
                t2.InvokeStatic("Leave");
            }
            void IXwt.SetParent(WindowFrame r, WindowFrame parentWindow)
            {
            }
        }
    }
}