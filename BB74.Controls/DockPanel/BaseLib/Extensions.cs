using BaseLib.Xwt.Interop;
using System;
using System.Reflection;
using Xwt;
using Xwt.Backends;

namespace BaseLib.Xwt
{
    public static class Extensions
    {
        public static void ClampPage(this Scrollbar sb)
        {
            sb.ClampPage(sb.LowerValue, sb.UpperValue);
        }
        public static void ClipToBounds(this Canvas widget)
        {
            ClipToBounds(widget.GetBackend());
        }
        public static void ClipToBounds(this IWidgetBackend backend)
        {
            var nativectl = backend.NativeWidget;

            Type t = Win32.swc_panel;

            if (nativectl.GetType().IsDerived(t))
            {
                nativectl.GetType().SetPropertyValue(nativectl, "ClipToBounds", true);
            }
        }
        public static void Active(this Window window)
        {
            if (BaseLib.Xwt.Platform.OSPlatform == PlatformID.MacOSX)
            {
                var nswin = window.GetBackend().Window;
                nswin.GetType().Invoke(nswin, "MakeKeyWindow", new object[0]);
            }
            else if (BaseLib.Xwt.Platform.OSPlatform == PlatformID.Unix)
            {
                var gtkwin = window.GetBackend().Window;
                gtkwin.GetType().Invoke(gtkwin, "ActivateDefault");
            }
        }
        public static IWidgetBackend GetBackend(this Widget o)
        {
            return (IWidgetBackend)global::Xwt.Toolkit.CurrentEngine.GetSafeBackend(o);
        }
        public static IWindowFrameBackend GetBackend(this WindowFrame o)
        {
            return (IWindowFrameBackend)global::Xwt.Toolkit.CurrentEngine.GetSafeBackend(o);
        }
        public static IWindowBackend GetBackend(this Window o)
        {
            return (IWindowBackend)global::Xwt.Toolkit.CurrentEngine.GetSafeBackend(o);
        }
        public static ICellViewBackend GetBackend(this CellView o)
        {
            return (ICellViewBackend)global::Xwt.Toolkit.CurrentEngine.GetSafeBackend(o);
        }
        public static bool IsDerived(this Type b, Type t)
        {
            while (b != null && !object.ReferenceEquals(b, t))
            {
                b = b.BaseType;
            }
            return b != null;
        }
    }
}
namespace BaseLib
{
    internal static class Extensions
    {
        public static Size GetMaxSize(this Canvas canvas)
        {
            var r = Rectangle.Zero;
            bool f = true;
            foreach (var c in canvas.Children)
            {
                r= r.Union(c.ParentBounds);
            }
            return r.Size;
        }
        public static double CalculateFor(this SizeConstraint constraint, double value)
        {
            if (value < 0)
            {
                return constraint.AvailableSize;
            }
            if (constraint == SizeConstraint.Unconstrained)
            {
                return value;
            }
            return Math.Min(value, constraint.AvailableSize);
        }
        public static object InvokeStatic(this Type type, string method, params object[] arguments)
        {
            return type.GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, arguments);
        }
        public static object InvokeStaticPrivate(this Type type, string method, params object[] arguments)
        {
            return type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments);
        }
        public static object Invoke(this Type type, object instance, string method, params object[] arguments)
        {
            return type.GetMethod(method, BindingFlags.Public | BindingFlags.Instance).Invoke(instance, arguments);
        }
        public static object InvokePrivate(this Type type, object instance, string method, params object[] arguments)
        {
            return type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(instance, arguments);
        }
        public static object GetPropertyValue(this Type type, object instance, string propertyname)
        {
            return type.GetProperty(propertyname, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty).GetValue(instance, new object[0]);
        }
        public static void SetPropertyValue(this Type type, object instance, string propertyname, object value)
        {
            type.GetProperty(propertyname, BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty).SetValue(instance, value, new object[0]);
        }
        public static object GetPropertyValuePrivate(this Type type, object instance, string propertyname)
        {
            return type.GetProperty(propertyname, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty).GetValue(instance, new object[0]);
        }
        public static object GetPropertyValueStatic(this Type type, string propertyname)
        {
            return type.GetProperty(propertyname, BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty).GetValue(null, new object[0]);
        }
        public static object GetFieldValue(this Type type, object instance, string propertyname)
        {
            return type.GetField(propertyname, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField).GetValue(instance);
        }
        public static object GetFieldValuePrivate(this Type type, object instance, string propertyname)
        {
            return type.GetField(propertyname, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField).GetValue(instance);
        }
        public static object GetFieldValueStatic(this Type type, object instance, string propertyname)
        {
            return type.GetField(propertyname, BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField).GetValue(instance);
        }
        public static void SetFieldValuePrivate(this Type type, object instance, string propertyname, object value)
        {
            type.GetField(propertyname, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetField).SetValue(instance, value);
        }

        public static bool IsDerived(this Type b, Type t)
        {
            while (b != null && !object.ReferenceEquals(b, t))
            {
                b = b.BaseType;
            }
            return b != null;
        }
        /*     public static T InvokePrivate<T>(this Type type, string method, params object[] arguments)
             {
                 return (T)type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments);
             }
             public static T GetPropertyValue<T>(this Type type, string propertyname)
             {
                 return (T)type.GetProperty(propertyname, BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty).GetValue(null, new object[0]);
             }
             public static T GetPropertyValuePrivate<T>(this Type type, string propertyname)
             {
                 return (T)type.GetProperty(propertyname, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetProperty).GetValue(null, new object[0]);
             }
             public static T GetFieldValuePrivate<T>(this Type type, string propertyname)
             {
                 return (T)type.GetField(propertyname, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField).GetValue(null);
             }
             public static void SetFieldValuePrivate(this Type type, string propertyname, object value)
             {
                 type.GetField(propertyname, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.SetField).SetValue(null, value);
             }*/
    }
}
