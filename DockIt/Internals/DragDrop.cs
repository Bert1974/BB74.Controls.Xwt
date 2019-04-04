using System;
using Xwt;
using Xwt.Backends;
using Xwt.Drawing;

namespace BaseLib.DockIt_Xwt
{
    static class DockItDragDrop
    {
        #region abstract class DragWindow
        internal abstract class DragWindow : Window
        {
            #region MyCancas
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
                    this.BackgroundColor = DockPanel.DropTargetColor;
                    this.GetBackend().CanGetFocus = true;

                    /*    this.ButtonPressed += (s, e) => { };
                        this.ButtonReleased += (s, e) => { };
                        this.MouseMoved += (s, e) => { };*/
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
                    args.Handled = true;
                    owner.doclose(false);
                    //    base.OnButtonPressed(args);
                }
                protected override void OnButtonReleased(ButtonEventArgs args)
                {
                    args.Handled = true;
                    owner.doclose(true);
                    //     base.OnButtonReleased(args);
                }
                protected override void OnMouseMoved(MouseMovedEventArgs args)
                {
                    args.Handled = true;
                    if (this.checkmouse)
                    {
                        var pt = this.ConvertToScreenCoordinates(args.Position);
                        owner.CheckMove(pt, true);
                    }
                }
            }
            #endregion

            public delegate void DragResultFunction(bool result, IDockPane pane, DockPosition? pos, Point floatpos);
            public void Show(DragResultFunction resultfunction)
            {
                this.doexit = false;
                this.result = true;

                (this as Window).Show();

                this.Content.SetFocus();
                this.xwt.SetCapture(this.Content);

                while (!this.doexit)
                {
                    this.xwt.DoEvents();
                }
                this.xwt.ReleaseCapture(this.Content);
                DockPanel.ClrHightlight();
                this.Close();

                resultfunction?.Invoke(this.result, this.droppane, this.drophit, this.Location);
            }

            protected readonly IXwt xwt;
            internal bool result, doexit;
            internal DockPosition? drophit;
            internal IDockPane droppane;
            private readonly Size floatsize;

            protected DragWindow(IXwt wxt, Point position, Size size, bool checkmouse)
            {
                this.xwt = wxt;

                this.result = this.doexit = false;

                //  this.Resizable = false;
                this.Decorated = false;
                this.Location = position.Offset(-5, -5);
                this.Size = new Size(32, 32);
                this.Width = this.Height = 32;
                this.Opacity = 0.8;
                this.Padding = 0;
                this.Title = "dragform";

                this.floatsize = size;

                this.Content = new MyCanvas(this, checkmouse);

                this.GetBackend().ShowInTaskbar = false;
            }
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
                DockItDragDrop.CheckMove(this, point, setpos, this.floatsize, ref this.droppane, ref this.drophit);
            }

            internal virtual void SetPosition(Rectangle rectangle)
            {
                (this.GetBackend() as IWindowFrameBackend).Bounds = rectangle;
            }
        }

        #endregion

        #region class DragWindowWpf
        class DragWindowWpf : DragWindow
        {
            public DragWindowWpf(IXwt xwt, Point position, Size size)
                : base(xwt, position, size, true)
            {
                var wpfwin = (this.GetBackend() as IWindowFrameBackend).Window;
                wpfwin.GetType().SetPropertyValue(wpfwin, "AllowsTransparency", true);

                SetMaxWidth(32);
            }
            internal override void SetPosition(Rectangle rectangle)
            {
                SetMaxWidth(Convert.ToInt32(rectangle.Width));
                base.SetPosition(rectangle);
            }
            private void SetMaxWidth(int width)
            {
                var wpfwin = (this.GetBackend() as IWindowFrameBackend).Window;
                  wpfwin.GetType().SetPropertyValue(wpfwin, "MaxWidth", width);
            }
        }
        #endregion

        #region class DragWindowXamMac
        class DragWindowXamMac : DragWindow
        {
            public DragWindowXamMac(IXwt wxt, Point position, Size size)
                : base(wxt, position, size, true)
            {
            }
        }
        #endregion

        #region class DragWindowGTK2
        class DragWindowGTK2 : DragWindow
        {
            public DragWindowGTK2(IXwt xwt, Point position, Size size)
                : base(xwt, position, size, true)
            {
            }
        }
        #endregion

        #region class DragWindowGTK3
        class DragWindowGTK3 : DragWindow
        {
            public DragWindowGTK3(IXwt xwt, Point position, Size size)
                : base(xwt, position, size, true)
            {
            }
        }

        #endregion

        internal static void StartDrag(FloatWindow floatform, Point position)
        {
            floatform.Visible = false;
            DragWindow dragwin = CreateDragWin(floatform.DockPanel.xwt, position, floatform.DockPanel.Current.WidgetSize);

            //  floatform.DockPanel.xwt.QueueOnUI(() => {
            dragwin.Show((result, droppane, drophit, pt) =>
            {
                if (result && droppane != null && drophit.HasValue)
                {
                    floatform.DockPanel.DockFloatform(floatform, droppane, drophit.Value);
                }
                else if (result)
                {
                    (floatform.GetBackend() as IWindowFrameBackend).Bounds = new Rectangle(pt, (floatform.GetBackend() as IWindowFrameBackend).Bounds.Size);
                    floatform.Visible = true;
                }
                dragwin.Dispose();
            });
        //    });
        }
        public static void StartDrag(IDockPane pane, IDockContent[] documents, Point position)
        {
            DragWindow dragwin = CreateDragWin(pane.DockPanel.xwt, position, pane.WidgetSize);

            //  pane.DockPanel.xwt.QueueOnUI(() => {

            dragwin.Show((result, droppane, drophit, pt) =>
            {
                if (result && droppane != null && drophit.HasValue)
                {
                    pane.DockPanel.MovePane(pane, documents, droppane, drophit.Value);
                }
                else if (result)
                {
                    pane.DockPanel.FloatPane(pane, documents, pt, pane.WidgetSize);
                }
                dragwin.Dispose();
            });
       //     });
        }


        private static DragWindow CreateDragWin(IXwt xwt, Point position, Size size)
        {
            switch (Toolkit.CurrentEngine.Type)
            {
                case ToolkitType.Wpf: return new DragWindowWpf(xwt, position, size);
                case ToolkitType.XamMac: return new DragWindowXamMac(xwt, position, size);
                case ToolkitType.Gtk: return new DragWindowGTK2(xwt, position, size);
                case ToolkitType.Gtk3: return new DragWindowGTK3(xwt, position, size);
                default: throw new NotImplementedException();
            }
        }
        internal static void CheckMove(DragWindow window, Point pt, bool setpos, Size floatsize, ref IDockPane droppane, ref DockPosition? drophit)
        {
            try
            {
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
                            break; // window in front
                        }
                    }
                    else
                    {
                        break;// window in front
                    }
                }
                droppane = null; drophit = null;
                DockPanel.ClrHightlight();
            }
            catch { throw; }
            finally
            {
                if (setpos)
                {
                    if (droppane == null || !drophit.HasValue)
                    {
                        window.SetPosition(new Rectangle(pt.Offset(-5, -5), floatsize));
                    }
                    else
                    {
                        window.SetPosition(new Rectangle(pt.Offset(-5, -5), new Size(32, 32)));
                    }
                }
            }
        }
    }
}