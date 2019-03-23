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
        class XamMac : IXwtImpl
        {
            class DragWindow : Window
            {
                private readonly IXwt xwt;
                private readonly Widget widget;

                public DragWindow(IXwt wxt, Canvas widget)
                {
                    this.xwt = wxt;
                    this.widget = widget;

                    this.Content = new Canvas()
                    {
                        ExpandHorizontal = true,
                        ExpandVertical = true
                    };
                    this.Content.MouseMoved += Content_MouseMoved;

                    /*       Application.eve

                           var test=widget.GetBackend().NativeWidget;
                           var h = (IntPtr)test.GetType().GetPropertyValue(test, "Handle");*/
                }

                private void Content_MouseMoved(object sender, MouseMovedEventArgs e)
                {
                        var w = (this.GetBackend() as IWindowFrameBackend).Window;
                        var pt = (sender as Widget).ConvertToScreenCoordinates(e.Position).Offset(-5, -5);
                        this.Location = pt;
                }

                protected override void OnShown()
                {
                    base.OnShown();

                    this.xwt.SetCapture(this.Content);
                }
            }

            class eventhandlerinfo
            {
                public EventInfo ei;
                public FieldInfo fi;
            }
            class eventdelegate
            {
                public string name;
                public Widget widget;
                public Delegate handler;
                internal Delegate[] oldfunc;
           //     internal MulticastDelegate value;
            }
            private Dictionary<string, eventhandlerinfo> ei = new Dictionary<string, eventhandlerinfo>();
            private Dictionary<string, eventdelegate> hh = new Dictionary<string, eventdelegate>();

            public void ReleaseCapture(Widget widget)
            {
                KillCapture();
            }
            void KillCapture()
            {
                foreach (var h in this.hh.Values.Reverse())
                {
                    this.ei[h.name].ei.RemoveEventHandler(h.widget, h.handler);
                }
                this.hh.Clear();
            }

            private void AddEvent<T>(Widget widget, string name, EventHandler<T> func)
                where T : EventArgs
            {
                if (!this.ei.TryGetValue(name, out eventhandlerinfo e))
                {
                    this.ei[name] = e = new eventhandlerinfo()
                    {
                        ei = widget.GetType().GetEvent(name[0].ToString().ToUpper()+name.Substring(1), BindingFlags.Public | BindingFlags.Instance),
                        fi = typeof(Widget).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                    };
                }
                if (!this.hh.TryGetValue(name, out eventdelegate h))
                {
                  //  var d = e.fi.GetValue(widget) as MulticastDelegate;
                 //   var mi = typeof(XamMac).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                  h = new eventdelegate()
                    {
                        name = name,
                        widget = widget,
                        handler = func,
                   //     value=d,
                        oldfunc = (e.fi.GetValue(widget) as MulticastDelegate)?.GetInvocationList().ToArray() ?? new Delegate[0]
                     };
                    this.hh[name] =h;

                    foreach (var f in h.oldfunc)
                    {
                        e.ei.RemoveEventHandler(f.Target, f);
                    }
                    e.ei.AddEventHandler(widget, func);
                    //     e.ei.AddEventHandler(widget, func);
                }
            }
            public void SetCapture(Widget widget)
            {
                KillCapture();
       /*         var backend = widget.GetBackend().NativeWidget;
                //    Type t = XwtImpl.GetType("AppKit.NSView");
                var props = backend.GetType().GetProperties().Where(_p => _p.Name == "Widget").ToArray();
                // var w = typeof(global::Xwt.Backends.IWidgetBackend).GetPropertyValue(backend, "Widget");
                //      (widget.ParentWindow as global::Xwt.WindowFrame).

                var view = backend.GetType().GetPropertyValue(backend, "View");*/

                // three method// subclass oview, override 3x? OpenTK.Platform.MacOS?

                AddEvent<ButtonEventArgs>(widget, "buttonPressed", this.buttonPressed);
                AddEvent<ButtonEventArgs>(widget, "buttonReleased", this.buttonReleased);
                AddEvent<MouseMovedEventArgs>(widget, "mouseMoved", this.mouseMoved);


          /*      eveinthandelerinfo ei = null;

                if (!this.ei.TryGetValue(, out ei))
                {
                    this.ei[ei] = new eveinthandelerinfo() { }
                    }
                this.ei = ;

                Debug.Assert(ei.IsMulticast);

                EventHandler<ButtonEventArgs> h = this.buttonpress;

                this.widget = widget;
                this.handler =
                     Delegate.CreateDelegate(ei.EventHandlerType,
                                             this,
                                             mi);
                                             */
                /*    var fi=;

                    this.oldfunc=f(i.GetValue(widget) as Delegate).GetInvocationList().ToArray();
                     {
                     }
                     */
            //    ei.AddEventHandler(widget, handler);

                //    ei.EventHandlerType;

                /*     var backend2 =(global::Xwt.Backends.IWindowFrameBackend)typeof(global::Xwt.WindowFrame).GetPropertyValuePrivate(widget.ParentWindow,"Backend");
                     var w =(backend2 as global::Xwt.Backends.IWindowFrameBackend).Window;
                     //var w = props[0].GetValue(backend,new object[0]);


                     while (true)
                     {
                         Type et = XwtImpl.GetType("AppKit.NSEventMask");
                         var ev = Enum.ToObject(et, 0x42);
                         var e = w.GetType().Invoke(w, "NextEventMatchingMask", new object[] { ev});
                     }*/
            }
            public void StartDrag(Canvas widget, Point position)
            {
                var r = new DragWindow(this, widget);
                r.Resizable = false;
                r.Decorated = false;
                r.Size = new Size(32, 32);
                r.Location = position.Offset(-5, -5);
                var backend = Toolkit.CurrentEngine.GetSafeBackend(r);
                (backend as IWindowFrameBackend).ShowInTaskbar = false;
                r.Show();
            }

            private void buttonPressed(object sender, ButtonEventArgs e)
            {
            }
            private void buttonReleased(object sender, ButtonEventArgs e)
            {
            }
            private void mouseMoved(object sender, MouseMovedEventArgs e)
            {
            }

            XwtImpl.DragWindow IXwtImpl.Create(Canvas widget, Point position)
            {
                throw new NotImplementedException();
            }
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
