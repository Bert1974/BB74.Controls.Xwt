﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xwt;

namespace BaseLib.DockIt_Xwt
{
    internal class DockSplitter : IDockSplitter
    {
        private List<IDockLayout> _dock = new List<IDockLayout>();

        public DockSplitter(DockPanel dockPanel, IDockLayout p1, IDockLayout p2, Orientation orientation)
        {
            this.DockPanel = dockPanel;

            this._dock.Add(p1);
            this._dock.Add(p2);

            this.Orientation = orientation;
        }

        public IEnumerable<IDockLayout> Layouts => this._dock;
        public Orientation Orientation { get; private set; }

        public Point Location { get; private set; }
        public Size MinimumSize { get; private set; }
        public Size MaximumSize { get; private set; }

        public Size WidgetSize { get; private set; }

        public DockPanel DockPanel { get; private set; }

        public void Insert(int ind, IDockLayout dockLayout)
        {
            this._dock.Insert(ind, dockLayout);
        }
        public void Remove(IDockLayout destination, bool removewidget)
        {
            if (removewidget)
            {
                (destination as IDockPane)?.RemoveWidget();
            }
            this._dock.Remove(destination);
        }

        public void Layout(Point pos, Size size)
        {
            this.Location = pos;
            this.WidgetSize = size;

            if (_dock.Count > 0)
            {
                var tot = this.Orientation == Orientation.Vertical ? this.WidgetSize.Height : this.WidgetSize.Width;
                var wh = this.Orientation != Orientation.Vertical ? this.WidgetSize.Height : this.WidgetSize.Width;
                var e = 4;// Math.Min(4, tot - 4);

                var pt = Point.Zero;// parent.Location;

                double[] facs, mi, ma;
                double mm;

                switch (this.Orientation)
                {
                    case Orientation.Horizontal:
                        facs = _dock.Select(_p => Math.Max(_p.WidgetSize.Width, _p.MinimumSize.Width)).ToArray();
                        mi = _dock.Select(_p => _p.MinimumSize.Width).ToArray();
                        ma = _dock.Select(_p => _p.MaximumSize.Width).ToArray();
                        mm = _dock.Select(_p => _p.MinimumSize.Height).Max();
                        break;
                    case Orientation.Vertical:
                        facs = _dock.Select(_p => Math.Max(_p.WidgetSize.Height, _p.MinimumSize.Height)).ToArray();
                        mi = _dock.Select(_p => _p.MinimumSize.Height).ToArray();
                        ma = _dock.Select(_p => _p.MaximumSize.Height).ToArray();
                        mm = _dock.Select(_p => _p.MinimumSize.Width).Max();
                        break;
                    default:
                        throw new NotImplementedException();
                }
                tot = Math.Max(tot - e * (_dock.Count - 1), 0);
                var min = mi.Sum();

                wh = Math.Max(wh, mm);
                tot = Math.Max(tot, min);

                double factot = 0;
                var newfact = (tot - min);

                double fam = 0;

                for (int nit = 0; nit < _dock.Count; nit++)
                {
                    fam += mi[nit];
                    facs[nit] -= mi[nit];
                    factot += facs[nit];

                    facs[nit] = Math.Max(facs[nit], 0);
                }

                double ev = 0;
                double[] nv = new double[facs.Length];

                if (factot <= 0)
                {
                    for (int nit = 0; nit < _dock.Count; nit++)
                    {
                        nv[nit] = mi[nit] + newfact / (double)_dock.Count;
                    }
                    factot = newfact;
                }
                else
                {
                    //       int tot = 0;

                    for (int nit = 0; nit < _dock.Count; nit++)
                    {
                        nv[nit] = mi[nit] + facs[nit] * newfact / (double)factot;

                        /*      if (ma[nit] != 0 && nv[nit] > ma[nit])
                              {
                                  ev += nv[nit] - ma[nit];
                                  nv[nit] = ma[nit];
                              }
                              else
                              {
                                  tot++;
                              }*/
                    }
                    //ev te verdelen over 
                    /*    var fev = new double[facs.Length];
                        for (int nit = 0; nit < _dock.Length; nit++)
                        {
                            int v=
                            if (ma[nit] == 0)
                            {
                                fev[nit]=
                            }
                                    || nv[nit] < ma[nit])
                            {
                                fev[nit] = ;
                            }
                        }*/
                }
                double xy = 0;
                for (int nit = 0; nit < _dock.Count; nit++)
                {
                    var oxy = xy;
                    xy += nv[nit];
                    if (ev != 0 && ma[nit] == 0)
                    {
                    }

                    int v1 = (int)Math.Round(oxy);
                    int v2 = (int)Math.Round(xy);

                    _dock[nit].Layout(
                        this.Orientation == Orientation.Vertical ? new Point(this.Location.X + 0, this.Location.Y + v1) : new Point(this.Location.X + v1, this.Location.Y + 0),
                        this.Orientation == Orientation.Vertical ? new Size(wh, v2 - v1) : new Size(v2 - v1, wh));

                    xy += e;

                }
                /*     int splitpos = 0;
                      int xy = 0;

                      for (int nit = 0; nit < _dock.Length; nit++)
                      {
                          int nextpos = splitpos + facs[nit];
                          int xy2 = (nextpos) * (tot - (facs.Length - 1) * e) / factot + nit * e;

                          (_dock[nit] as Control).Location = this.Orientation == Orientation.Vertical ? new Point(0, xy) : new Point(xy, 0);
                          (_dock[nit] as Control).Size = this.Orientation == Orientation.Vertical ? new Size(wh, xy2 - xy) : new Size(xy2 - xy, wh);

                          splitpos = nextpos;
                          xy = xy2 + e;
                      }*/
                /*   for (int nit = 0; nit < _dock.Length; nit++)
                   {
                       (panes[nit] as IDockLayout).DoLayout();
                   }*/
            }
        }
        public void GetSize(bool setsize)
        {
            foreach (var o in this._dock)
            {
                o.GetSize(setsize);
            }

            double miw = 0, mih = 0;

            switch (this.Orientation)
            {
                case Orientation.Horizontal:
                    miw = this._dock.Select(_p => _p.MinimumSize.Width).Sum();
                    mih = this._dock.Select(_p => _p.MinimumSize.Height).Max();
                    break;
                case Orientation.Vertical:
                    miw = this._dock.Select(_p => _p.MinimumSize.Width).Max();
                    mih = this._dock.Select(_p => _p.MinimumSize.Height).Sum();
                    break;
                default:
                    throw new NotImplementedException();
            }
            this.MinimumSize = new Size(miw, mih);

            if (setsize)
            {
                this.WidgetSize = this.MinimumSize;
         /*       switch (this.Orientation)
                {
                    case Orientation.Horizontal:
                        this.WidgetSize = new Size(this._dock.Select(_ctl => _ctl.WidgetSize.Width).Sum(), this._dock.Select(_ctl => _ctl.WidgetSize.Height).Max());
                        break;
                    case Orientation.Vertical:
                        this.WidgetSize = new Size(this._dock.Select(_ctl => _ctl.WidgetSize.Width).Max(), this._dock.Select(_ctl => _ctl.WidgetSize.Height).Sum());
                        break;
                }*/
            }
        }
        public bool HitTest(Point position, out IDockSplitter splitter, out int ind)
        {
            if (position.X >= this.Location.X && position.X < this.Location.X + this.WidgetSize.Width &&
                position.Y >= this.Location.Y && position.Y < this.Location.Y + this.WidgetSize.Height)
            {
                int cnt = 0, e = 4;
                double v = 0;
                switch (this.Orientation)
                {
                    case Orientation.Horizontal: v = this.Location.X; break;
                    case Orientation.Vertical: v = this.Location.Y; break;
                }
                foreach (var o in this._dock)
                {
                    if (o.HitTest(position, out splitter, out ind))
                    {
                        if (splitter == null)
                        {
                            return false;
                        }
                        return true;
                    }
                    switch (this.Orientation)
                    {
                        case Orientation.Horizontal:
                            v += o.WidgetSize.Width;
                            if (position.X >= v && position.X < v + e)
                            {
                                splitter = this;
                                ind = cnt;
                                return true;
                            }
                            break;
                        case Orientation.Vertical:
                            v += o.WidgetSize.Height;
                            if (position.Y >= v && position.Y < v + e)
                            {
                                splitter = this;
                                ind = cnt;
                                return true;
                            }
                            break;
                    }
                    v += e;
                    cnt++;
                }
                splitter = null;
                ind = -1;
                return true;
            }
            splitter = null;
            ind = -1;
            return false;
        }
    }
}