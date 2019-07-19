using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xwt;
using Xwt.Backends;
using Xwt.Drawing;

namespace BaseLib.Xwt.Controls
{
    public class Spacer : Label
    {
        private readonly Widget ctl;
        private readonly Orientation direction;

        public Spacer(Widget match, Orientation direction)
        {
            this.ctl = match;
            this.direction = direction;
            this.ctl.BoundsChanged += Ctl_BoundsChanged;
            base.BackgroundColor = Colors.Transparent;
            base.HorizontalPlacement = base.VerticalPlacement = WidgetPlacement.Fill;
        }

        private void Ctl_BoundsChanged(object sender, EventArgs e)
        {
            switch (this.direction)
            {
                case Orientation.Horizontal: this.WidthRequest = this.ctl.Size.Width; break;
                case Orientation.Vertical: this.HeightRequest = this.ctl.Size.Height; break;
            }
        }
    }
}
