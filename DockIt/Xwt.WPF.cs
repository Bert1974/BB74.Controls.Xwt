using System;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using Xwt;
using Xwt.Backends;

namespace BaseLib.DockIt_Xwt
{
    partial class XwtImpl
    {
        class WPF : IXwtImpl
        {
            class DragWindow : XwtImpl.DragWindow
            {
                private bool doexit;    

                public DragWindow(IXwt xwt, Canvas widget, Point position)
                    : base(xwt, widget, position)
                {
                    this.Content = new Canvas()
                    {
                        ExpandHorizontal = true,
                        ExpandVertical = true,
                        CanGetFocus=true
                    };
                    this.Content.MouseMoved += Content_MouseMoved;
                    this.Content.ButtonPressed += Content_ButtonPressed;
                    this.Content.ButtonReleased += Content_ButtonReleased;
                }
                protected override bool OnCloseRequested()
                {
                    if (!this.doexit)
                    {
                        close(false);
                        return false;
                    }
                    return base.OnCloseRequested();
                }
                private void Content_ButtonReleased(object sender, ButtonEventArgs e)
                {
                    close(e.Button == PointerButton.Left);
                }
                private void Content_ButtonPressed(object sender, ButtonEventArgs e)
                {
                    close(false);
                }
                private void close(bool apply)
                {
                    this.result = apply;
                    this.doexit = true;
                }
                private void Content_MouseMoved(object sender, MouseMovedEventArgs e)
                {
                    var pt = (sender as Widget).ConvertToScreenCoordinates(e.Position);
                    this.Location = pt.Offset(-5, -5);

                    var hits = BaseLib.DockIt_Xwt.PlatForm.Instance.Search(IntPtr.Zero, pt); // all hit window-handle son system

                    foreach (var w in hits)
                    {
                        if (BackendHost.Backend.NativeHandle == w.Item2)
                        {
                            continue;// hit through dragwindow
                        }
                        var hit = DockPanel.CheckHit(w.Item2, pt.X, pt.Y);

                        if (hit != null)
                        {
                            var b = hit.ConvertToScreenCoordinates(hit.Bounds.Location);

                            DockPanel.SetHighLight(hit, new Point(pt.X - b.X, pt.Y - b.Y), out this.droppane, out this.drophit);
                            return;
                        }
                        //       break; // don't know enumerated strange window with wpf
                    }
                    DockPanel.ClrHightlight();
                }
                public override void Show()
                {
                    this.doexit = false;

                    (this as Window).Show();
                    this.Content.SetFocus();

                    this.xwt.SetCapture(this.Content);

                    while (!this.doexit)
                    {
                        this.xwt.DoEvents();
                    }
                    this.xwt.ReleaseCapture(this.Content);

                    DockPanel.ClrHightlight();

                    base.Close();
                }
            }

            [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            public void DoEvents()
            {
                var t1 = XwtImpl.GetType("System.Windows.Threading.DispatcherFrame");
                var frame = Activator.CreateInstance(t1);

                var t = XwtImpl.GetType("System.Windows.Threading.Dispatcher");
                var current = t.GetPropertyValueStatic("CurrentDispatcher");
                
                var t3 = XwtImpl.GetType("System.Windows.Threading.DispatcherPriority");
                var t4 = XwtImpl.GetType("System.Windows.Threading.DispatcherOperationCallback");

                var callback = Delegate.CreateDelegate(t4, typeof(WPF), "ExitFrame");
                
                var mi = current.GetType().GetMethod("BeginInvoke", new Type[] { typeof(Delegate),t3, typeof(object[])});
                mi.Invoke(current, new object[] {(Delegate)callback, Enum.Parse(t3, "Background"), new object[] { frame } });
                
                mi = t.GetMethod("PushFrame",BindingFlags.Static|BindingFlags.Public,null, new Type[] { frame.GetType()}, new ParameterModifier[] { new ParameterModifier(1) });
                mi.Invoke(null, new object[] { frame });

           /*     DispatcherFrame frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                    new DispatcherOperationCallback(ExitFrame), frame);
                Dispatcher.PushFrame(frame);*/
            }

            public static object ExitFrame(object f)
            {
                f.GetType().SetPropertyValue(f, "Continue", false);
              //  ((DispatcherFrame)f).Continue = false;
                return null;
            }
            void IXwt.ReleaseCapture(Widget widget)
            {
                var backend = Toolkit.CurrentEngine.GetSafeBackend(widget);
                var w = backend.GetType().GetPropertyValue(backend, "Widget");
                w.GetType().Invoke(w, "ReleaseMouseCapture");
            }

            void IXwt.SetCapture(Widget widget)
            {
                var backend = Toolkit.CurrentEngine.GetSafeBackend(widget);
                var w = backend.GetType().GetPropertyValue(backend, "Widget");
                w.GetType().Invoke(w, "CaptureMouse");
            }

            void IXwt.StartDrag(Canvas widget, Point position)
            {
                throw new NotImplementedException();
            }

            public XwtImpl.DragWindow Create(Canvas widget, Point position)
            {
                return new DragWindow(this, widget, position);
            }
        }
    }
}
