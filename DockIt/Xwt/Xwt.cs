using BaseLib.XwtPlatForm;
using System;
using System.Linq;
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
        #region DragWindow
        protected abstract class DragWindow : Window
        {
            protected class MyCanvas : Xwt.Canvas
            {
                private readonly DragWindow owner;
                private readonly bool checkmouse;

                public MyCanvas(DragWindow dragWindow, bool checkmouse)
                {
                    this.owner = dragWindow;
                    this.checkmouse = checkmouse;

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
                protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
                {
                    return new Size(32, 32);// base.OnGetPreferredSize(widthConstraint, heightConstraint);
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
                    if (this.checkmouse)
                    {
                        var pt = this.ConvertToScreenCoordinates(args.Position);
                        owner.CheckMove(pt, true);
                    }
                }
            }

            internal void SetResult(out IDockPane dockpane, out DockPosition? dockat)
            {
                if (this.result)
                {
                    dockpane = this.droppane;
                    dockat = this.drophit;
                }
                else
                {
                    dockpane = null;
                    dockat = null;
                }
            }

            protected readonly IXwt xwt;
            protected readonly Widget widget;
            internal bool result, doexit;
            internal DockPosition? drophit;
            internal IDockPane droppane;

            protected DragWindow(IXwt wxt, Canvas widget, Point position, bool checkmouse)
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
                this.Title = "dragform";

                this.Content = new MyCanvas(this, checkmouse);
            }
            public abstract void Show(out IDockPane dockpane,out DockPosition? dockat);
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
            protected void CheckMove(Point point, bool setpos)
            {
                XwtImpl.CheckMove(this, point, setpos, ref this.droppane, ref this.drophit);
            }
        }
        #endregion

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
        public static void CheckMove(Window window, Point pt, bool setpos, ref IDockPane droppane, ref DockPosition? drophit)
        {
            if (setpos)
            {
                (window.GetBackend() as IWindowFrameBackend).Bounds = new Rectangle(pt.Offset(-5, -5), new Size(32, 32));
            }
            var hits = BaseLib.XwtPlatForm.PlatForm.Instance.Search(window, pt); // all hit window-handle son system

            foreach (var w in hits)
            {
                if (object.ReferenceEquals((window.GetBackend() as IWindowBackend).Window, w.Item2))
                {
                    continue;// hit through dragwindow
                }
                var hit = DockPanel.CheckHit(w.Item2, pt.X, pt.Y);

                if (hit != null)
                {
                    var b = hit.ConvertToScreenCoordinates(hit.Bounds.Location);

                    DockPanel.SetHighLight(hit, new Point(pt.X - b.X, pt.Y - b.Y), out droppane, out drophit);
                    return;
                }
                if (Toolkit.CurrentEngine.Type == ToolkitType.Wpf)
                {
                    if (w.Item2.GetType().FullName != "Microsoft.VisualStudio.DesignTools.WpfTap.WpfVisualTreeService.Adorners.AdornerLayerWindow")
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            droppane = null; drophit = null;
            DockPanel.ClrHightlight();
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
                if (Toolkit.CurrentEngine.Type == ToolkitType.Wpf)
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
                dragwin.Show(out IDockPane droppane, out DockPosition? drophit);

                if (dragwin.result && droppane != null && drophit.HasValue)
                {
                    widget.DockPanel.MovePane(widget as IDockPane, documents, droppane, drophit.Value);
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
