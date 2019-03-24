using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Xwt;
using Xwt.Backends;

namespace BaseLib.DockIt_Xwt
{
    public partial class XwtImpl : IXwt
    {
        protected interface IXwtImpl : IXwt
        {
            DragWindow Create(Canvas widget, Point position);
        }
        protected abstract class DragWindow : Window
        {
            protected readonly IXwt xwt;
            protected readonly Widget widget;
            public bool result { get; protected set; }
            internal DockPosition? drophit;
            internal IDockPane droppane;

            protected DragWindow(IXwt wxt, Canvas widget, Point position)
            {
                this.xwt = wxt;
                this.widget = widget;

                this.Resizable = false;
                this.Decorated = false;
                this.Location = position.Offset(-5, -5);
                this.Size = new Size(32, 32);
                this.Opacity = 0.8;
            }
            public new abstract void Show();
        }

        private IXwtImpl impl;

        internal static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }
        public XwtImpl()
        {
        }
        public static IXwt Create()
        {
            return new XwtImpl();
        }
        private IXwtImpl CheckImpl()
        {
            if (impl == null)
            {
                if (Toolkit.CurrentEngine.Type==ToolkitType.Wpf)
                {
                    this.impl = new WPF();
                }
                else if (Toolkit.CurrentEngine.Type == ToolkitType.Gtk)
                {
                    this.impl = new GTK();
                }
                else if (Toolkit.CurrentEngine.Type == ToolkitType.XamMac)
                {
                    this.impl = new XamMac();
                }
                else 
                {
                    throw new NotImplementedException();
                }
            }
            return impl;
        }
        void IXwt.SetCapture(Widget widget)
        {
            CheckImpl().SetCapture(widget);
        }
        void IXwt.ReleaseCapture(Widget widget)
        {
            CheckImpl().ReleaseCapture(widget);
        }
        void IXwt.StartDrag(Canvas widget, Point position)
        {
            var dragwin = CheckImpl().Create(widget, position);
            dragwin.Show();

            if (dragwin.result && dragwin.droppane != null && dragwin.drophit.HasValue)
            {
                if (dragwin.droppane == null|| !dragwin.drophit.HasValue)
                {
                //    this.owner.DockPanel.FloatPane(this.owner, docnum, floatpos);
                }
                else
                {
                    dragwin.droppane.DockPanel.MovePane(widget as IDockPane, (widget as IDockPane).Documents.ToArray(), dragwin.droppane, dragwin.drophit.Value);
                }
            }
        }

        public void DoEvents()
        {
            CheckImpl().DoEvents();
        }
    }
    internal static class Extension
    {
        public static IWidgetBackend GetBackend(this Widget o)
        {
           return (IWidgetBackend)global::Xwt.Toolkit.CurrentEngine.GetSafeBackend(o);
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
            return type.GetField(propertyname, BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField).GetValue(instance);
        }

        public static bool IsDerived(this Type b,Type t)
        {
            while(b!=null&&!object.ReferenceEquals(b,t))
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
