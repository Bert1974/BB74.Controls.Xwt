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
        //    DragWindow Create(Canvas widget, Point position);
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
