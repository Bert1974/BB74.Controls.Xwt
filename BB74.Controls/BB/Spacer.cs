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
        private double size;

        private double Width => (this.ctl?.Size.Width ?? this.size);
        private double Height => (this.ctl?.Size.Height ?? this.size);

        public Spacer(Widget match, Orientation direction)
        {
            this.ctl = match;
            this.direction = direction;
            this.ctl.BoundsChanged += Ctl_BoundsChanged;
            base.BackgroundColor = Colors.Transparent;
            base.HorizontalPlacement = base.VerticalPlacement = WidgetPlacement.Fill;
        }
        public Spacer(double size, Orientation direction)
        {
            this.size = size;
            this.direction = direction;
            this.ctl.BoundsChanged += Ctl_BoundsChanged;
            base.BackgroundColor = Colors.Transparent;
            base.HorizontalPlacement = base.VerticalPlacement = WidgetPlacement.Fill;
        }

        private void Ctl_BoundsChanged(object sender, EventArgs e)
        {
            switch (this.direction)
            {
                case Orientation.Horizontal: this.WidthRequest = this.Width; break;
                case Orientation.Vertical: this.HeightRequest = this.Height; break;
            }
        }
    }
}
