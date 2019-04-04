using BaseLib.XwtPlatForm;
using System;
using System.Runtime.InteropServices;
using Xwt;
using Xwt.Backends;

namespace BaseLib.DockIt_Xwt
{
    partial class XwtImpl
    {
        class GTK3 : IXwtImpl
        {
            public void ReleaseCapture(Widget widget)
            {
                var gtkwin = (widget.GetBackend() as IWidgetBackend).NativeWidget;
                Type type_gtk_grab = PlatForm.GetType("Gtk.Grab");
                type_gtk_grab.InvokeStatic("Remove", gtkwin);
            }

            public void SetCapture(Widget widget)
            {
                var gtkwin = (widget.GetBackend() as IWidgetBackend).NativeWidget;
                Type type_gtk_grab = PlatForm.GetType("Gtk.Grab");
                type_gtk_grab.InvokeStatic("Add", gtkwin);
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
                var gtkwin = (r.GetBackend() as IWindowFrameBackend).Window;
                var gtkwinparent = (parentWindow.GetBackend() as IWindowFrameBackend).Window;
                gtkwin.GetType().SetPropertyValue(gtkwin, "TransientFor", gtkwinparent);
            }

            public void QueueOnUI(Action method)
            {
                throw new NotImplementedException();
            }
        }
    }
}