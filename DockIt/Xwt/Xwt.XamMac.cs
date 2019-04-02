using BaseLib.XwtPlatForm;
using System;
using System.Linq;
using Xwt;
using Xwt.Backends;

namespace BaseLib.DockIt_Xwt
{
    partial class XwtImpl
    {
        class XamMac : IXwtImpl
        {
            class DragWindow : XwtImpl.DragWindow
            {
                public DragWindow(IXwt wxt, Canvas widget, Point position)
                    : base(wxt,widget, position, false)
                {
                    var backend = Toolkit.CurrentEngine.GetSafeBackend(this);
                    (backend as IWindowFrameBackend).ShowInTaskbar = false;
                }
                public override void Show(out IDockPane dockpane, out DockPosition? dockat)
                {
                    this.doexit = false;
                    this.result = true;

                    (this as Window).Show();

                    this.Content.SetFocus();
                 //  this.xwt.SetCapture(this.Content);

                    while (!this.doexit)
                    {
                        Type t = PlatForm.GetType("AppKit.NSEvent");
                        var pt=t.GetPropertyValueStatic("CurrentMouseLocation");
                        var mask = t.GetPropertyValueStatic("CurrentPressedMouseButtons");

                        var x = (int)Convert.ToDouble(pt.GetType().GetPropertyValue(pt, "X"));
                       var y = (int)Convert.ToDouble(pt.GetType().GetPropertyValue(pt, "Y"));

                        this.doexit = (Convert.ToUInt32(mask) & 1/*button1mask*/) == 0;
                        
                        var xwtmacbackend = PlatForm.GetType("Xwt.Mac.MacDesktopBackend");
                        var cgsizetype = PlatForm.GetType("CoreGraphics.CGPoint");

                        var cgpt = Activator.CreateInstance(cgsizetype, new object[] { (double)x, (double)y });
                        var pt2 = (Xwt.Point)xwtmacbackend.InvokeStatic("ToDesktopPoint", cgpt);

                        this.CheckMove(pt2, true);

                        this.xwt.DoEvents();
                    }

              //      this.xwt.ReleaseCapture(this.Content);
                    DockPanel.ClrHightlight();
                    this.Close();

                    base.SetResult(out dockpane, out dockat);
                }
            }

 /*           class eventhandlerinfo
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
            private Dictionary<string, eventdelegate> hh = new Dictionary<string, eventdelegate>();*/

            public void ReleaseCapture(Widget widget)
            {
                KillCapture();
            }
            void KillCapture()
            {
             /*  foreach (var h in this.hh.Values.Reverse())
                {
                    this.ei[h.name].ei.RemoveEventHandler(h.widget, h.handler);
                }
                this.hh.Clear();*/
            }

      /*      private void AddEvent<T>(Widget widget, string name, EventHandler<T> func)
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
            }*/
            public void SetCapture(Widget widget)
            {
                KillCapture();
                /*         var backend = widget.GetBackend().NativeWidget;
                         //    Type t = PlatForm.GetType("AppKit.NSView");
                         var props = backend.GetType().GetProperties().Where(_p => _p.Name == "Widget").ToArray();
                         // var w = typeof(global::Xwt.Backends.IWidgetBackend).GetPropertyValue(backend, "Widget");
                         //      (widget.ParentWindow as global::Xwt.WindowFrame).

                         var view = backend.GetType().GetPropertyValue(backend, "View");*/

                // three method// subclass oview, override 3x? OpenTK.Platform.MacOS?

           /*     AddEvent<ButtonEventArgs>(widget, "buttonPressed", this.buttonPressed);
                AddEvent<ButtonEventArgs>(widget, "buttonReleased", this.buttonReleased);
                AddEvent<MouseMovedEventArgs>(widget, "mouseMoved", this.mouseMoved);
                */

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
                         Type et = PlatForm.GetType("AppKit.NSEventMask");
                         var ev = Enum.ToObject(et, 0x42);
                         var e = w.GetType().Invoke(w, "NextEventMatchingMask", new object[] { ev});
                     }*/
            }
      /*      public void StartDrag(Canvas widget, Point position, IDockContent[] documents)
            {
                var r = new DragWindow(this, widget, position);
                r.Resizable = false;
                r.Decorated = false;
                r.Size = new Size(32, 32);
                r.Location = position.Offset(-5, -5);
                var backend = Toolkit.CurrentEngine.GetSafeBackend(r);
                (backend as IWindowFrameBackend).ShowInTaskbar = false;
                r.Show();
            }*/
       /*    private void buttonPressed(object sender, ButtonEventArgs e)
            {
            }
            private void buttonReleased(object sender, ButtonEventArgs e)
            {
            }
            private void mouseMoved(object sender, MouseMovedEventArgs e)
            {
            }
            */
            XwtImpl.DragWindow IXwtImpl.Create(Canvas widget, Point position)
            {
                return new DragWindow(this, widget, position);
            }

            public void DoEvents()
            {
                var t = PlatForm.GetType("AppKit.NSApplication");
                object o = t.GetPropertyValueStatic("SharedApplication");
                object e;
                object mask = Enum.Parse(PlatForm.GetType("AppKit.NSEventMask"), "AnyEvent");
                object now = PlatForm.GetType("Foundation.NSDate").GetPropertyValueStatic("Now");
                object mode = Enum.Parse(PlatForm.GetType("Foundation.NSRunLoopMode"), "EventTracking");
                object[] args = { mask, now, mode, true };
                var mi = o.GetType().GetMethod("NextEvent", args.Select(_a => _a.GetType()).ToArray());
                do
                {
                    e = mi.Invoke(o, args);
                }
                while (e != null);
            }

            void IXwt.SetParent(WindowFrame r, WindowFrame parentWindow)
            {
                Type et = PlatForm.GetType("AppKit.NSWindowLevel");
                var level = Enum.ToObject(et, 3L/*floating*/);
                var w = (r.GetBackend() as IWindowFrameBackend).Window;
                w.GetType().SetPropertyValue(w, "Level", level);
            }
        }
    }
}