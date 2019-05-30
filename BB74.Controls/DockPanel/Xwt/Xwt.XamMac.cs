using BaseLib.Xwt.Interop;
using System;
using System.Diagnostics;
using Xwt;

namespace BaseLib.Xwt
{
    using Xwt = global::Xwt;
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
                          DoEvents();
                      }
                  });
                }
            }

            private Xwt.Point MousePositionForWidget(Widget widget)
            {
                var cgpoint = XamMac.appkit_nsevent.GetPropertyValueStatic("CurrentMouseLocation");
                var pt = (Xwt.Point)XamMac.xwtmacbackend.InvokeStatic("ToDesktopPoint", cgpoint);
                return pt.Offset(-widget.ScreenBounds.X, -widget.ScreenBounds.Y);
            }

            public override void DoEvents()
            {
                object e;
                int cnt = 500;
                if (this.captureitem != null)
                {
                    object o = XamMac.appkit_nsapplication.GetPropertyValueStatic("SharedApplication");
                    object mask = Enum.Parse(XamMac.appkit_nseventmask, "AnyEvent");
                    //object mask = Enum.ToObject(XamMac.appkit_nseventmask, (ulong)0x44);
                    object now = XamMac.found_nsdate.GetPropertyValueStatic("DistantFuture");
                    object mode = Enum.Parse(XamMac.found_nsrunloopmode, "EventTracking");

                    var nswin = this.captureitem.ParentWindow.GetBackend().Window;

                    this.captureitemeventdoeventcnt++;
               //     do
                    {
                        e = XamMac.mi_nswindow_nextevent.Invoke(nswin, new object[] { mask });

                        if (this.captured != 0 && e != null)
                        {
                            var et = e.GetType().GetPropertyValue(e, "Type");

                            if ((ulong)et == 6) // left mouse drag
                            {
                                var pt = MousePositionForWidget(this.captureitem);
                                var args = new MouseMovedEventArgs(0, pt.X, pt.Y);

                                this.captureitem.GetType().InvokePrivate(this.captureitem, "OnMouseMoved", new object[] { args });
                            }
                            else if ((ulong)et == 2) // left mouse ip
                            {
                                var pt = MousePositionForWidget(this.captureitem);
                                var args = new ButtonEventArgs() { Button = PointerButton.Left, X = pt.X, Y = pt.Y, Handled = false };

                                this.captureitem.GetType().InvokePrivate(this.captureitem, "OnButtonReleased", new object[] { args });
                            }
                        }
                    }
               //     while (e != null && this.captured > 0 && --cnt >= 0);
                    this.captureitemeventdoeventcnt--;
                }
                else
                {
                    object o = XamMac.appkit_nsapplication.GetPropertyValueStatic("SharedApplication");
                    object mask = Enum.Parse(XamMac.appkit_nseventmask, "AnyEvent");
                    object now = XamMac.found_nsdate.GetPropertyValueStatic("DistantFuture");
                    object mode = Enum.Parse(XamMac.found_nsrunloopmode, "EventTracking");
                 //   do
                    {
                        e = XamMac.mi_nsapp_nextevent.Invoke(o, new object[] { mask, now, mode, true });
                    }
                 //    while (e != null && --cnt >= 0);
                }
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
        }
    }
}