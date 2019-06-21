using System;
using System.Reflection;

namespace BaseLib.Xwt.Interop
{
    static class XamMac
    {
        public static Type appkit_nsapplication = Platform.GetType("AppKit.NSApplication");
        public static Type appkit_nswindow = Platform.GetType("AppKit.NSWindow");
        public static Type appkit_nsevent = Platform.GetType("AppKit.NSEvent");
        public static Type appkit_nseventmask = Platform.GetType("AppKit.NSEventMask");
        public static Type appkit_nsrunningapp = Platform.GetType("AppKit.NSRunningApplication");
        public static Type xwtmacbackend = Platform.GetType("Xwt.Mac.MacDesktopBackend");
        public static Type found_nsrunloopmode = Platform.GetType("Foundation.NSRunLoopMode");
        public static Type found_nsdate = Platform.GetType("Foundation.NSDate");
        public static Type found_nsstring = Platform.GetType("Foundation.NSString");
        public static Type cg_cgrect = Platform.GetType("CoreGraphics.CGRect");
        public static Type cg_cgpoint = Platform.GetType("CoreGraphics.CGPoint");
        public static Type cg_cgsize = Platform.GetType("CoreGraphics.CGSize");


        public static MethodInfo mi_nsapp_nextevent = appkit_nsapplication.GetMethod("NextEvent", new Type[] { appkit_nseventmask, found_nsdate, found_nsrunloopmode, typeof(bool) });
        public static MethodInfo mi_nswindow_nextevent = appkit_nswindow.GetMethod("NextEventMatchingMask", new Type[] { appkit_nseventmask/*, found_nsdate, found_nsstring, typeof(bool) */});
    }
}
