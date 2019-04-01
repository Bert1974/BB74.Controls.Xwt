using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Xwt;
using Xwt.Backends;
using Xwt.Drawing;

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
            protected class MyCanvas : Xwt.Canvas
            {
                private readonly DragWindow owner;

                public MyCanvas(DragWindow dragWindow)
                {
                    this.owner = dragWindow;

                    this.Margin = 0;
                    this.MinWidth = this.MinHeight = 10;
                    this.ExpandHorizontal = true;
                    this.ExpandVertical = true;
                    this.CanGetFocus = true;
                    this.BackgroundColor = Colors.LightYellow;

                    this.ButtonPressed += (s, e) => { };
                    this.ButtonReleased += (s, e) => { };
                    this.MouseMoved += (s, e) => { };
                }
                protected override void OnDraw(Context ctx, Rectangle dirtyRect)
                {
                    base.OnDraw(ctx, dirtyRect);

                    ctx.SetColor(Colors.Black);
                    ctx.Rectangle(this.Bounds);
                    ctx.Stroke();
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
                    owner.doclose(false);
                    //    base.OnButtonPressed(args);
                }
                protected override void OnButtonReleased(ButtonEventArgs args)
                {
                    owner.doclose(true);
                    //     base.OnButtonReleased(args);
                }
                protected override void OnMouseMoved(MouseMovedEventArgs args)
                {
                    owner.CheckMove(this.ConvertToScreenCoordinates(args.Position));
                }
            }
            protected readonly IXwt xwt;
            protected readonly Widget widget;
            internal bool result, doexit;
            internal DockPosition? drophit;
            internal IDockPane droppane;

            protected DragWindow(IXwt wxt, Canvas widget, Point position)
            {
                this.xwt = wxt;
                this.widget = widget;

                this.result = this.doexit = false;

                //  this.Resizable = false;
                this.Decorated = false;
                this.Location = position.Offset(-5, -5);
                this.Size = new Size(32, 32);
                this.Width = this.Height = 32;
                this.Opacity = 0.8;
                this.Padding = 0;

                if (Toolkit.CurrentEngine.Type == ToolkitType.Wpf)
                {
                    var wpfwin = (this.GetBackend() as IWindowFrameBackend).Window;
                    wpfwin.GetType().SetPropertyValue(wpfwin, "AllowsTransparency", true);
                    wpfwin.GetType().SetPropertyValue(wpfwin, "MaxWidth", 32);
                }
                this.Content = new MyCanvas(this);
            }
            public new abstract void Show();
            protected virtual void doclose(bool apply)
            {
                this.result = apply;
                this.doexit = true;
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
            protected virtual void CheckMove(Point pt)
            {
                (this.GetBackend() as IWindowFrameBackend).Bounds = new Rectangle(pt.Offset(-5, -5),new Size(32,32));

                var hits = BaseLib.DockIt_Xwt.PlatForm.Instance.Search(this, pt); // all hit window-handle son system

                foreach (var w in hits)
                {
                    if (object.ReferenceEquals(this.BackendHost.Backend.Window, w.Item2))
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
                    if (Toolkit.CurrentEngine.Type != ToolkitType.Wpf)
                    {
                        break; // don't know enumerated strange window with wpf
                    }
                }
                DockPanel.ClrHightlight();
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
        protected XwtImpl()
        {
        }
        public static XwtImpl Create()
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
                else if (Toolkit.CurrentEngine.Type == ToolkitType.Gtk3)
                {
                    this.impl = new GTK3();
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
        public void /*IXwt.*/SetCapture(Widget widget)
        {
            CheckImpl().SetCapture(widget);
        }
        public void /*IXwt.*/ReleaseCapture(Widget widget)
        {
            CheckImpl().ReleaseCapture(widget);
        }
        public void StartDrag(IDockPane widget, Point position, IDockContent[] documents)
        {
            var dragwin = CheckImpl().Create(widget.Widget, position);
            
            Application.InvokeAsync(() =>
            {
                dragwin.Show();

                if (dragwin.result && dragwin.droppane != null && dragwin.drophit.HasValue)
                {
                    widget.DockPanel.MovePane(widget as IDockPane, documents, dragwin.droppane, dragwin.drophit.Value);
                }
                else if (dragwin.result)
                {
                    widget.DockPanel.FloatPane(widget, documents, dragwin.Location.Offset(5, 5));
                }
                dragwin.Dispose();
            });
        }
        public void /*IXwt.*/DoEvents()
        {
            CheckImpl().DoEvents();
        }
        public void SetPos(WindowFrame window, Rectangle pos)
        {
            (window.GetBackend() as IWindowFrameBackend).Bounds = pos;
        }
        public void SetParent(WindowFrame r, WindowFrame parentWindow)
        {
            CheckImpl().SetParent(r, parentWindow);
        }
    }
}
