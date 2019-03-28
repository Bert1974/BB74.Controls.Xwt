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
            class DragWindow : XwtImpl.DragWindow
            {
                private bool doexit;

                class MyCanvas : Canvas
                {
                    private readonly DragWindow owner;

                    public MyCanvas(DragWindow dragWindow)
                    {
                        this.owner = dragWindow;

                        ExpandHorizontal = true;
                        ExpandVertical = true;
                        CanGetFocus = true;
                    }
                    protected override void OnKeyPressed(KeyEventArgs args)
                    {
                        if (args.Key == Key.Escape)
                        {
                            owner.doclose(false);
                            args.Handled = true;
                            return;
                        }
                        base.OnKeyPressed(args);
                    }
                    protected override void OnButtonPressed(ButtonEventArgs args)
                    {
                        base.OnButtonPressed(args);
                    }
                    protected override void OnButtonReleased(ButtonEventArgs args)
                    {
                        base.OnButtonReleased(args);
                    }
                }
                public DragWindow(IXwt wxt, Canvas widget, Point position)
                    : base(wxt,widget, position)
                {
                    var backend = Toolkit.CurrentEngine.GetSafeBackend(this);
                    (backend as IWindowFrameBackend).ShowInTaskbar = false;

                    this.Content = new MyCanvas(this);
                }

                protected override bool OnCloseRequested()
                {
                    if (!this.doexit)
                    {
                        this.doclose(false);
                        return false;
                    }
                    return true;
                }
                private void doclose(bool apply)
                {
                    this.result = apply;
                    this.doexit = true;
                }

                private void Content_MouseMoved(object sender, MouseMovedEventArgs e)
                {
                    var w = (this.GetBackend() as IWindowFrameBackend).Window;
                    var pt = (sender as Widget).ConvertToScreenCoordinates(e.Position).Offset(-5, -5);
                    this.Location = pt;
                }

                public override void Show()
                {
                    this.doexit = false;
                    this.result = true;

                    (this as Window).Show();

                    this.Content.SetFocus();
                    this.xwt.SetCapture(this.Content);

                    while (!this.doexit)
                    {
                        Type t = XwtImpl.GetType("AppKit.NSEvent");
                        var pt=t.GetPropertyValueStatic("CurrentMouseLocation");
                        var mask = t.GetPropertyValueStatic("CurrentPressedMouseButtons");
                      /*  var gtkwin = (this.GetBackend() as IWindowFrameBackend).Window;
                        var display = gtkwin.GetType().GetPropertyValue(gtkwin, "Display");
                        var screen = display.GetType().GetPropertyValue(display, "DefaultScreen");*/

                        //  Type t = XwtImpl.GetType("Gdk.ModifierType");

                        //   var parms = new object[] { 0, 0, Enum.ToObject(t, 0) };
                        //   var mi = display.GetType().GetMethod("GetPointer", new Type[] { Type.GetType("System.Int32&"), Type.GetType("System.Int32&"), XwtImpl.GetType("Gdk.ModifierType&") });
                        //    mi.Invoke(display, parms);
                        //                        display.GetType().Invoke(display, "GetPointer", parms);
                        //   display.GetPointer(out int x, out int y, out Gdk.ModifierType mask);
                        //  int mask = (int)parms[2];

                        var x = (int)Convert.ToDouble(pt.GetType().GetPropertyValue(pt, "X"));
                        var y = (int)Convert.ToDouble(pt.GetType().GetPropertyValue(pt, "Y"));

                        this.doexit = (Convert.ToUInt32(mask) & 1/*button1mask*/) == 0;

                        this.Location = new Point(x, y).Offset(-5, -5);
                        this.Content.SetFocus();

                        this.xwt.DoEvents();

                        var dp = DockPanel.GetHits(x, y);

                        if (dp.Any())
                        {
                          //  var rootwin = screen.GetType().GetPropertyValue(screen, "RootWindow");

                        /*    var wins = (Array)rootwin.GetType().GetPropertyValue(rootwin, "Children");

                            var allwin = wins.OfType<object>().Where(_gdkwin => DockPanel.AllDockPanels.Any(_dp =>
                            {
                                var w = (_dp.ParentWindow?.GetBackend() as IWindowFrameBackend)?.Window;
                                return object.ReferenceEquals(w.GetType().GetPropertyValue(w, "GdkWindow"), _dp);
                            })).ToList();

                            var hit = dp.OrderBy(_dp =>
                            {
                                var w = (_dp.ParentWindow?.GetBackend() as IWindowFrameBackend)?.Window;
                                var w2 = w.GetType().GetPropertyValue(w, "GdkWindow");
                                return allwin.IndexOf(w2);
                            }).First();

                            var wp = hit.ConvertToScreenCoordinates(hit.Bounds.Location);

                            DockPanel.SetHighLight(hit, new Point(x - wp.X, y - wp.Y), out this.droppane, out this.drophit);*/
                        }
                        else
                        {
                            DockPanel.ClrHightlight();
                        }
                    }

                    this.xwt.ReleaseCapture(this.Content);
                    DockPanel.ClrHightlight();
                    this.Close();

                    if (this.result)
                    {
                    }
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
            public void StartDrag(Canvas widget, Point position, IDockContent[] documents)
            {
                var r = new DragWindow(this, widget, position);
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
                return new DragWindow(this, widget, position);
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

            void IXwt.SetParent(WindowFrame r, WindowFrame parentWindow)
            {
                Type et = XwtImpl.GetType("AppKit.NSWindowLevel");
                var level = Enum.ToObject(et, 3L/*floating*/);
                var w = (r.GetBackend() as IWindowFrameBackend).Window;
                w.GetType().SetPropertyValue(w, "Level", level);
            }
        }
    }
}