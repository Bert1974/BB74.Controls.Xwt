﻿using BaseLib.Xwt.Interop;
using System;
using System.Diagnostics;
using System.Reflection;
using Xwt;

namespace BaseLib.Xwt
{
    using Xwt = global::Xwt;

    class oo
    {
        public oo(Type type)
        {
            this.type = type;
        }
        public oo(Type type, object obj)
        {
            this.type = type;
            this.obj = obj;
        }
        public oo(object obj)
        {
            this.obj = obj;
            this.type = obj.GetType();
        }

        public Type type;
        public object obj;
    }

    static class Extensions2
    {
        public static object alloc(this Type type, params object[] values)
        {
            return Activator.CreateInstance(type, values);
        }
        public static oo o(this Type type)
        {
            return new oo(type);
        }
        public static oo o(this object obj)
        {
            return new oo(obj);
        }
        public static oo prop(this oo oo, string prop)
        {
            var value = oo.type.GetProperty(prop).GetValue(oo.obj, new object[0]);
            return new oo(value);
        }
        public static T value<T>(this oo oo, string prop)
        {
            return (T)oo.prop(prop).obj;
        }
    }
    partial class XwtImpl
    {
        class XamMacXwt : RealXwt
        {
            internal int captured = 0, captureitemeventdoeventcnt = 0;
            private Widget captureitem = null;

            public override void ReleaseCapture(Widget widget)
            {
                Debug.Assert(object.ReferenceEquals(this.captureitem, widget));
                Debug.Assert(this.captured == 1);

                if (--this.captured == 0)
                {
                    this.captureitem = null;
                }
            }
            public override void SetCapture(XwtImpl xwt, Widget widget)
            {
                Debug.Assert(this.captured == 0 ? this.captureitem == null : object.ReferenceEquals(this.captureitem, widget));
                Debug.Assert(this.captured == 0);

                if (this.captured++ == 0)
                {
                    this.captureitem = widget;
                }
                if (this.captureitemeventdoeventcnt == 0)
                {
                    xwt.QueueOnUI(() =>
                  {
                      if (this.captureitemeventdoeventcnt != 0)
                      {
                          return;
                      }
                      while (captured > 0)
                      {
                          DoEvents(()=>true);
                      }
                  });
                }
            }

            private Xwt.Point MousePositionForWidget(Widget widget)
            {
                var cgpoint = XamMac.appkit_nsevent.GetPropertyValueStatic("CurrentMouseLocation");
                var pt = (Xwt.Point)XamMac.xwtmacbackend.InvokeStatic("ToDesktopPoint", cgpoint);
                /*  for (var p = this.captureitem.Parent; p != null; p = p.Parent)
                   {
                       pt= pt.Offset(-p.ParentBounds.X, -p.ParentBounds.Y);
                   }*/

                var scrpt = widget.ConvertToScreenCoordinates(Point.Zero);

                return pt.Offset(-scrpt.X, -scrpt.Y);
            }
            
            public override void DoEvents(Func<bool> cancelfunc)
            {
                object mask = Enum.Parse(XamMac.appkit_nseventmask, "AnyEvent");
                //  object mask = Enum.ToObject(XamMac.appkit_nseventmask, 0xfe/*"AnyEvent"*/);
                object now = XamMac.found_nsdate.GetPropertyValueStatic("Now");

                object e;
                int cnt = 500;
                if (this.captureitem != null)
                {
                    object nsappinstance = XamMac.appkit_nsapplication.GetPropertyValueStatic("SharedApplication");
                    //object mask = Enum.ToObject(XamMac.appkit_nseventmask, (ulong)0x44);
                    object mode = Enum.Parse(XamMac.found_nsrunloopmode, "EventTracking");


                    this.captureitemeventdoeventcnt++;

                    object e2;
                    while (cancelfunc() && this.captured > 0 && --cnt >= 0)
                    {
                        var nswin = this.captureitem.ParentWindow.GetBackend().Window;
                        e2 = XamMac.mi_nsapp_nextevent.Invoke(nsappinstance, new object[] { mask, now, mode, true }); // no dequeue

                        bool handled = false;

                        if (this.captured != 0 && e2 != null)
                        {
                      //      e2 = XamMac.mi_nsapp_nextevent.Invoke(nsappinstance, new object[] { mask, now, mode, true }); // dequeue
                            //e = XamMac.mi_nswindow_nextevent.Invoke(nswin, new object[] { mask }); // dequeues

                            var nswin2 = e2.GetType().GetPropertyValue(e2, "Window");

                            if (!object.ReferenceEquals(nswin, nswin2)) // click through window??
                            {
                                nswin2 = nswin;
                            }
                            if (nswin2 != null && object.ReferenceEquals(nswin,nswin2))
                            {
                             //   if (e != null)
                                Debug.Assert(this.captured != 0);
                                var et = e2.GetType().GetPropertyValue(e2, "Type");

                                switch ((ulong)et)
                                {
#if (false)
                                    case 10://key down
                                        {
                                            nswin.GetType().GetMethod("KeyDown").Invoke(nswin, new object[] { e });
                                          /*  var t = Platform.GetType("Xwt.Mac.IViewObjectExtensions");
                                            t.InvokeStatic("HandleKeyDown", new object[] { this.captureitem, e });
                                            Console.WriteLine("key");*/
                                            handled = true;
                                        }
                                        break;
                                    case 11://key up
                                        {
                                            nswin.GetType().GetMethod("KeyUp").Invoke(nswin, new object[] { e });
                                          /* var t = Platform.GetType("Xwt.Mac.IViewObjectExtensions");
                                            t.InvokeStatic("HandleKeyUp", new object[] { this.captureitem, e });
                                            Console.WriteLine("key");*/
                                            handled = true;
                                        }
                                        break;
#endif
                                    case 1: // left mouse down
                                    case 3: // right mouse down
                                        {
                                            if (!HandleButtonEvent("OnButtonPressed", e2))
                                            {
                                                Platform.GetType("AppKit.NSResponder").GetMethod("MouseDown").Invoke(nswin, new object[] { e2 });
                                            }
                                            handled = true;
                                        }
                                        break;
                                    case 2: // left mouse up
                                    case 4: // right mouse up
                                        {
                                            if (!HandleButtonEvent("OnButtonReleased", e2))
                                            {
                                                Platform.GetType("AppKit.NSResponder").GetMethod("MouseUp").Invoke(nswin, new object[] { e2 });
                                            }
                                            handled = true;
                                        }
                                        break;
                                    case 5:// mouse move
                                    case 6:// left mouse drag
                                    case 7:// right mouse drag
                                        {
                                            var pt = MousePositionForWidget(this.captureitem);
                                            var timestamp = (long)TimeSpan.FromSeconds((e2.GetType().GetProperty("Timestamp").GetValue(e2, new object[0]) as IConvertible).ToDouble(null)).TotalMilliseconds;
                                            var args = new MouseMovedEventArgs(timestamp, pt.X, pt.Y) { Handled = false };
                                            this.captureitem.GetType().InvokePrivate(this.captureitem, "OnMouseMoved", new object[] { args });

                                            if (!args.Handled)
                                            {
                                                Platform.GetType("AppKit.NSResponder").GetMethod("MouseMoved", BindingFlags.Instance | BindingFlags.Public).Invoke(nswin, new object[] { e2 });
                                            }
                                            handled = true;
                                        }
                                        break;
                                }
                              /*  if (!handled)
                                {
                                    XamMac.appkit_nswindow.GetMethod("SendEvent").Invoke(nswin, new object[] { e });
                                }*/
                            }
                            if (e2 != null && !handled)
                            //  else
                            {
                           /*     if (nswin2 != null)
                                {
                                    XamMac.appkit_nswindow.GetMethod("SendEvent").Invoke(nswin2, new object[] { e2 });
                                }
                                else*/
                                {
                                    XamMac.appkit_nsapplication.GetMethod("SendEvent").Invoke(nsappinstance, new object[] { e2 });
                                }
                                //  e2 = XamMac.mi_nsapp_nextevent.Invoke(nsappinstance, new object[] { mask, now, mode, true }); // deqeue
                             //   XamMac.appkit_nsapplication.GetMethod("SendEvent").Invoke(nsappinstance, new object[] { e2 });
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    this.captureitemeventdoeventcnt--;
                }
                else // not captured
                {
                    object o = XamMac.appkit_nsapplication.GetPropertyValueStatic("SharedApplication");
                    //   object mask = Enum.Parse(XamMac.appkit_nseventmask, "AnyEvent");
                    //  object now = XamMac.found_nsdate.GetPropertyValueStatic("DistantFuture");
                    object mode = Enum.Parse(XamMac.found_nsrunloopmode, "Default");
                    now = XamMac.found_nsdate.GetPropertyValueStatic("Now");
                    do
                    {
                        e = XamMac.mi_nsapp_nextevent.Invoke(o, new object[] { mask, now, mode, true });

                        if (e != null)
                        {
                         /*   var nswin = e.GetType().GetPropertyValue(e, "Window");

                            if (nswin != null)
                            {
                                XamMac.appkit_nswindow.GetMethod("SendEvent").Invoke(nswin, new object[] { e });
                            }
                            else*/
                            {
                                XamMac.appkit_nsapplication.GetMethod("SendEvent").Invoke(o, new object[] { e });
                            }
                        }
                    }
                    while (cancelfunc() && e != null && --cnt >= 0);
                }
            }   
            private bool HandleButtonEvent(string eventname, object e)
            {
                var pt = MousePositionForWidget(this.captureitem);
                var clickcnt = e.GetType().GetProperty("ClickCount").GetValue(e, new object[0]);
                var args = new ButtonEventArgs() { Button = GetPointerButton(e), X = pt.X, Y = pt.Y, Handled = false, MultiplePress = Convert.ToInt32(clickcnt), IsContextMenuTrigger= TriggersContextMenu(e) };
                this.captureitem.GetType().InvokePrivate(this.captureitem, eventname, new object[] { args });
                return args.Handled;
            }
            public static bool TriggersContextMenu(object theEvent)
            {
                var buttonnumber = (theEvent.GetType().GetProperty("ButtonNumber").GetValue(theEvent, new object[0]) as IConvertible).ToInt32(null);
                var modifierflags = (theEvent.GetType().GetProperty("ModifierFlags").GetValue(theEvent, new object[0]) as IConvertible).ToInt32(null);
                var currentbutton = (Platform.GetType("AppKit.NSEvent").GetProperty("CurrentPressedMouseButtons").GetValue(null,new object[0]) as IConvertible).ToInt32(null);

                if (buttonnumber == 1 &&
                        (currentbutton & 1 | currentbutton & 4) == 0)
                {
                    return true;
                }

                if (buttonnumber == 0 && (modifierflags & 0x40000L/*NSEventModifierMask.ControlKeyMask*/) != 0 &&
                        (currentbutton & 2 | currentbutton & 4) == 0)
                {
                    return true;
                }

                return false;
            }
            public static PointerButton GetPointerButton(object theEvent)
            {
                switch ((theEvent.GetType().GetProperty("ButtonNumber").GetValue(theEvent,new object[0]) as IConvertible).ToInt32(null))
                {
                    case 0: return PointerButton.Left;
                    case 1: return PointerButton.Right;
                    case 2: return PointerButton.Middle;
                    case 3: return PointerButton.ExtendedButton1;
                    case 4: return PointerButton.ExtendedButton2;
                }
                return (PointerButton)0;
            }

            public override void SetParent(WindowFrame r, WindowFrame parentWindow)
            {
                Type et = Platform.GetType("AppKit.NSWindowLevel");
                var level = Enum.ToObject(et, 3L/*floating*/);
                var w = r.GetBackend().Window;
                w.GetType().SetPropertyValue(w, "Level", level);
            }

            public override void GetMouseInfo(WindowFrame window, out int mx, out int my, out uint buttons)
            {
                Type et = Platform.GetType("AppKit.NSEvent");
                var flags = et.GetPropertyValueStatic("CurrentPressedMouseButtons");
                var pos = et.GetPropertyValueStatic("CurrentMouseLocation");

                var screens = (Array)Platform.GetType("AppKit.NSScreen").GetPropertyValueStatic("Screens");

                Rectangle desktopBounds = Rectangle.Zero;

                foreach (var s in screens)
                {
                    var r = s.GetType().GetPropertyValue(s, "Frame");
                    desktopBounds = desktopBounds.Union(new Rectangle(
                        (r.GetType().GetPropertyValue(r, "X") as IConvertible).ToInt32(null),
                        (r.GetType().GetPropertyValue(r, "Y") as IConvertible).ToInt32(null),
                        (r.GetType().GetPropertyValue(r, "Width") as IConvertible).ToInt32(null),
                        (r.GetType().GetPropertyValue(r, "Height") as IConvertible).ToInt32(null)));
                }

                mx = (pos.GetType().GetPropertyValue(pos, "X") as IConvertible).ToInt32(null);
                my = Convert.ToInt32(desktopBounds.Bottom - (pos.GetType().GetPropertyValue(pos, "Y") as IConvertible).ToInt32(null));
                buttons = (uint)(flags as IConvertible).ToInt32(null);

            }

            /*   void InitPasteboard(NSPasteboard pb, TransferDataSource data)
               {
                   pb.ClearContents();
                   foreach (var t in data.DataTypes)
                   {
                       if (t == TransferDataType.Text)
                       {
                           pb.AddTypes(new string[] { NSPasteboard.NSStringType }, null);
                           pb.SetStringForType((string)data.GetValue(t), NSPasteboard.NSStringType);
                       }
                   }
               }*/
            public override bool StartDrag(Widget source, DragOperation operation)
            {
                var startdata = (Xwt.Backends.DragStartData)operation.GetType().InvokePrivate(operation, "GetStartData");

                typeof(Widget).InvokePrivate(source, "DragStart", new object[] { startdata });

                var eventDelegate = (Delegate)typeof(DragOperation).GetFieldValuePrivate(operation, "Finished");
                if (eventDelegate != null)
                {
                    foreach (var handler in eventDelegate.GetInvocationList())
                    {
                        handler.Method.Invoke(handler.Target, new object[] { operation, new DragFinishedEventArgs(false) });
                    }
                }
                return true;
            }
        }
    }
}
