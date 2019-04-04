using System;
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
            public override void ReleaseCapture(Widget widget)
            {
                var gtkwin = widget.GetBackend().NativeWidget;
                Gtk.gtk_grab.InvokeStatic("Remove", gtkwin);
            }
            public override void SetCapture(XwtImpl ixwt, Widget widget)
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
            public override void SetParent(WindowFrame r, WindowFrame parentWindow)
            {
                var gtkwin = r.GetBackend().Window;
                var gtkwinparent = parentWindow.GetBackend().Window;
                gtkwin.GetType().SetPropertyValue(gtkwin, "TransientFor", gtkwinparent);
            }
        }
    }
}