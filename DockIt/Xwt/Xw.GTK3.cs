using BaseLib.XwtPlatForm;
using System;
using System.Runtime.InteropServices;
using Xwt;
using Xwt.Backends;

namespace BaseLib.DockIt_Xwt
{
    partial class XwtImpl
    {
        class GTK3 : RealXwt
        {
            public override void ReleaseCapture(Widget widget)
            {
                var gtkwin = widget.GetBackend().NativeWidget;
                Type type_gtk_grab = PlatForm.GetType("Gtk.Grab");
                type_gtk_grab.InvokeStatic("Remove", gtkwin);
            }

            public override void SetCapture(XwtImpl xwt, Widget widget)
            {
                var gtkwin = widget.GetBackend().NativeWidget;
                Type type_gtk_grab = PlatForm.GetType("Gtk.Grab");
                type_gtk_grab.InvokeStatic("Add", gtkwin);
            }
            public override void DoEvents()
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
            public override void  SetParent(WindowFrame r, WindowFrame parentWindow)
            {
                var gtkwin = r.GetBackend().Window;
                var gtkwinparent = parentWindow.GetBackend().Window;
                gtkwin.GetType().SetPropertyValue(gtkwin, "TransientFor", gtkwinparent);
            }
        }
    }
}