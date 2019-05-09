using System;
using System.Threading;
using System.Threading.Tasks;
using Xwt;

namespace BaseLib.Xwt
{
    public partial class XwtImpl : IXwt
    {
        protected abstract class RealXwt
        {
            public abstract void DoEvents();

            public void QueueOnUI(Action method)
            {
                throw new NotImplementedException();
            }

            public abstract void ReleaseCapture(Widget widget);

            public abstract void SetCapture(XwtImpl xwt, Widget widget);

            public void SetCapture(Widget widget)
            {
                throw new NotImplementedException();
            }

            public abstract void SetParent(WindowFrame r, WindowFrame parentWindow);
            public abstract void GetMouseInfo(WindowFrame window, out int mx, out int my, out uint buttons);
        }

        private RealXwt Implementation;
        
        protected XwtImpl()
        {
        }
        public static XwtImpl Create()
        {
            return new XwtImpl();
        }
        private RealXwt CheckImpl()
        {
            if (Implementation == null)
            {
                if (Toolkit.CurrentEngine.Type == ToolkitType.Wpf)
                {
                    this.Implementation = new WPF();
                }
                else if (Toolkit.CurrentEngine.Type == ToolkitType.Gtk)
                {
                    this.Implementation = new GTKXwt();
                }
                else if (Toolkit.CurrentEngine.Type == ToolkitType.Gtk3)
                {
                    this.Implementation = new GTK3Xwt();
                }
               else if (Toolkit.CurrentEngine.Type == ToolkitType.XamMac)
                {
                    this.Implementation = new XamMacXwt();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return Implementation;
        }
        public void /*IXwt.*/SetCapture(Widget widget)
        {
            CheckImpl().SetCapture(this, widget);
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
            window.GetBackend().Bounds = pos;
        }
        public void SetParent(WindowFrame r, WindowFrame parentWindow)
        {
            CheckImpl().SetParent(r, parentWindow);
        }

        CancellationTokenSource worker2cancel = new CancellationTokenSource();
        public void QueueOnUI(Action function)
        {
            Task.Factory.StartNew(function, worker2cancel.Token, TaskCreationOptions.None, Application.UITaskScheduler);
        }

        public void GetMouseInfo(WindowFrame window, out int mx, out int my, out uint buttons)
        {
            CheckImpl().GetMouseInfo(window, out mx, out my, out buttons);
        }
    }
}
