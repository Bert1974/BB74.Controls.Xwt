using BaseLib.XwtPlatForm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BaseLib.DockIt_Xwt.Interop
{
    static class Gtk
    {
        public static Type gtk_window = PlatForm.GetType("Gtk.Window");
        public static Type gtk_application = PlatForm.GetType("Gtk.Application");
        public static Type gtk_grab =  PlatForm.GetType("Gtk.Grab");
    }
    static class Gdk
    {
        public static Type gdk_threads = PlatForm.GetType("Gdk.Threads");
    }
}
