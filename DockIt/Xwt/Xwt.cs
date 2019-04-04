using BaseLib.XwtPlatForm;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public void QueueOnUI(Action function)
        {
           var worker2cancel = new CancellationTokenSource();
            Task.Factory.StartNew(function, worker2cancel.Token, TaskCreationOptions.None, Application.UITaskScheduler);
        }
    }
}
