using BaseLib.Xwt.Interop;
using System;
using Xwt;

namespace BaseLib.Xwt
{
    partial class XwtImpl
    {
        class GTK3Xwt : RealXwt
        {
            public override void ReleaseCapture(Widget widget)
            {
                var gtkwin = widget.GetBackend().NativeWidget;
                Gtk.gtk_grab.InvokeStatic("Remove", gtkwin);
            }

            public override void SetCapture(XwtImpl xwt, Widget widget)
            {
                var gtkwin = widget.GetBackend().NativeWidget;
                Gtk.gtk_grab.InvokeStatic("Add", gtkwin);
            }
            public override void DoEvents()
            {
                var mi_iteration = Gtk.gtk_application.GetMethod("RunIteration", new Type[] { typeof(bool) });

                Gdk.gdk_threads.InvokeStatic("Enter");
                int n = 500;

                while ((bool)Gtk.gtk_application.InvokeStatic("EventsPending") && --n > 0)
                {
                    mi_iteration.Invoke(null, new object[] { false });
                }
                Gdk.gdk_threads.InvokeStatic("Leave");
            }
            public override void  SetParent(WindowFrame r, WindowFrame parentWindow)
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