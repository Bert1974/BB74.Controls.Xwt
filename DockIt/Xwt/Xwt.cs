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
    public partial class XwtImpl : IXwt
    {
        protected interface IXwtImpl : IXwt
        {
            DragWindow Create(Canvas widget, Point position);
            bool SetPos(WindowFrame window, Rectangle pos);
            void SetParent(WindowFrame r, WindowFrame parentWindow);
        }
        protected abstract class DragWindow : Window
        {
            protected readonly IXwt xwt;
            protected readonly Widget widget;
            public bool result { get; protected set; }
            internal DockPosition? drophit;
            internal IDockPane droppane;

            protected DragWindow(IXwt wxt, Canvas widget, Point position)
            {
                this.xwt = wxt;
                this.widget = widget;

                this.Resizable = false;
                this.Decorated = false; 
                this.Location = position.Offset(-5, -5);
                this.Size = new Size(32, 32);
                this.Opacity = 0.8;
            }
            public new abstract void Show();
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
        private XwtImpl()
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
            dragwin.Show();

            if (dragwin.result && dragwin.droppane != null && dragwin.drophit.HasValue)
            {
                widget.DockPanel.MovePane(widget as IDockPane, documents, dragwin.droppane, dragwin.drophit.Value);
            }
            else if (dragwin.result)
            {
                widget.DockPanel.FloatPane(widget, documents, dragwin.Location.Offset(5,5));
            }
        }
        public void /*IXwt.*/DoEvents()
        {
            CheckImpl().DoEvents();
        }
        public void SetPos(WindowFrame window, Rectangle pos)
        {
            if (!CheckImpl().SetPos(window, pos))
            {
                window.Location = pos.Location;
                window.Size = pos.Size;
            }
        }
        public void SetParent(WindowFrame r, WindowFrame parentWindow)
        {
            CheckImpl().SetParent(r, parentWindow);
        }
    }
}
