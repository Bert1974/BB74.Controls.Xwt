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
        private readonly Func<double> func;
        private readonly Orientation direction;
        private double size;

        private double Width => (this.ctl?.Size.Width ?? this.func?.Invoke() ?? this.size);
        private double Height => (this.ctl?.Size.Height ?? this.func?.Invoke() ?? this.size);

        public Spacer(Widget match, Orientation direction)
        {
            this.ctl = match;
            this.direction = direction;
            base.BackgroundColor = Colors.Transparent;
            base.HorizontalPlacement = base.VerticalPlacement = WidgetPlacement.Fill;
        }
        public Spacer(double size, Orientation direction)
        {
            this.size = size;
            this.direction = direction;
            base.BackgroundColor = Colors.Transparent;
            base.HorizontalPlacement = base.VerticalPlacement = WidgetPlacement.Fill;
        }
        public Spacer(Func<double> func, Orientation direction)
        {
            this.func = func;
            this.direction = direction;
            base.BackgroundColor = Colors.Transparent;
            base.HorizontalPlacement = base.VerticalPlacement = WidgetPlacement.Fill;
        }
        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            switch (this.direction)
            {
                case Orientation.Horizontal: return new Size(this.Width, 0);
                case Orientation.Vertical: return new Size(0, this.Height);
                default:throw new NotImplementedException();
            }
        }
    }
}
