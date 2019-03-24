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
    partial class XwtImpl
    {
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
                        ei = widget.GetType().GetEvent(name[0].ToString().ToUpper() + name.Substring(1), BindingFlags.Public | BindingFlags.Instance),
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
                    this.hh[name] = h;

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

            public void DoEvents()
            {
                var t = XwtImpl.GetType("AppKit.NSApplication");
                object o = t.GetPropertyValueStatic("SharedApplication");
                object e;
                object mask = Enum.Parse(XwtImpl.GetType("AppKit.NSEventMask"), "AnyEvent");
                object now = XwtImpl.GetType("Foundation.NSDate").GetPropertyValueStatic("Now");
                object mode = Enum.Parse(XwtImpl.GetType("Foundation.NSRunLoopMode"), "EventTracking");
                object[] args = { mask, now, mode, true };
                var mi = o.GetType().GetMethod("NextEvent", args.Select(_a => _a.GetType()).ToArray());
                do
                {
                    e = mi.Invoke(o, args);
                }
                while (e != null);
            }
        }
    }
}