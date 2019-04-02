using Xwt;

namespace BaseLib.DockIt_Xwt
{
    class DragDrop
    {
        internal static void StartDrag(DockPane pane, IDockContent[] documents, Point position)
        {
            (pane.DockPanel.xwt as XwtImpl).StartDrag(pane,position, documents);
        }
    }
}
