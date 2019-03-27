using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xwt;

namespace BaseLib.DockIt_Xwt
{
    class DragDrop
    {
        internal static void StartDrag(DockPane pane, IDockContent[] documents, Point position)
        {
            pane.DockPanel.xwt.StartDrag(pane,position, documents);
        }
    }
}
