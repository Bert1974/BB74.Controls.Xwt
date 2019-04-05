using System;
using System.Runtime.InteropServices;
using BaseLib.DockIt_Xwt.Interop;
using BaseLib.XwtPlatForm;
using Xwt;
using Xwt.Backends;

namespace BaseLib.DockIt_Xwt
{
    partial class XwtImpl
    {
        class GTKXwt : RealXwt
        {
         /*   [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "gdk_pointer_grab")]
            private static extern int x11_gdk_pointer_grab(IntPtr gdkwindow, bool owner_events, int mask, IntPtr confine_to_gdkwin, IntPtr cursor, int time);

            [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "gdk_pointer_ungrab")]
            private static extern void x11_gdk_pointer_ungrab(int time);*/

            public override void ReleaseCapture(Widget widget)
            {
              //  x11_gdk_pointer_ungrab(0);

                var gtkwin = widget.GetBackend().NativeWidget;
                Gtk.gtk_grab.InvokeStatic("Remove", gtkwin);
            }
            public override void SetCapture(XwtImpl xwt, Widget widget)
            {
                var gtkwin = widget.GetBackend().NativeWidget;
                Gtk.gtk_grab.InvokeStatic("Add", gtkwin);

                //  var gtk = widget.GetBackend().NativeWidget;
            /*    var gdk = gtkwin.GetType().GetPropertyValue(gtkwin, "GdkWindow");
                var h = (IntPtr)gdk.GetType().GetPropertyValue(gdk, "Handle");

                int r = x11_gdk_pointer_grab(h, true, (1 << 5)/*|(1<<9)|(1<10)*//*0x3ffffe*/, IntPtr.Zero, IntPtr.Zero, 0);
                
                Console.WriteLine($"gdk_grab={r}");*/
            }
            public override void DoEvents()
            {
                var mi_iteration = Gtk.gtk_application.GetMethod("RunIteration", new Type[] { typeof(bool) });

                Gdk.gdk_threads.InvokeStatic("Enter");
                int n = 500;

                while ((bool)Gtk.gtk_application.InvokeStatic("EventsPending") && n-- > 0)
                {
                    mi_iteration.Invoke(null, new object[] { false });
                }
                Gdk.gdk_threads.InvokeStatic("Leave");
            }
            public override void SetParent(WindowFrame r, WindowFrame parentWindow)
            {
                var gtkwin = r.GetBackend().Window;
                var gtkwinparent = parentWindow.GetBackend().Window;
                gtkwin.GetType().SetPropertyValue(gtkwin, "TransientFor", gtkwinparent);
            }

            public override void GetMouseInfo(WindowFrame window, out int mx, out int my, out uint buttons)
            {
                Type t = XwtImpl.GetType("Gdk.ModifierType");

                var display = Interop.Gtk.gtk_window.GetPropertyValue(window.GetBackend().Window, "Display");

                var parms = new object[] { 0, 0, Enum.ToObject(t, 0) };
                var mi = display.GetType().GetMethod("GetPointer", new Type[] { Type.GetType("System.Int32&"), Type.GetType("System.Int32&"), XwtImpl.GetType("Gdk.ModifierType&") });

                mi.Invoke(display, parms);

                mx = (int)parms[0];
                my = (int)parms[1];

                int mask = (int)parms[2];

                buttons = (uint)
                            ((mask & 0x100) != 0 ? 1 : 0);
            }
        }
    }
}