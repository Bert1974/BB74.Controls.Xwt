using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using Xwt;
using Xwt.Backends;

namespace BaseLib.DockIt_Xwt
{
    public interface IXwt
    {
        void SetCapture(Widget widget);
        void ReleaseCapture(Widget widget);
        void StartDrag(Canvas widget, Point position);
        void DoEvents();
    }
    public interface IDockToolbar : IDockContent
    {
    }
    public interface IDockDocument : IDockContent
    {
    }
    public interface IDockContent
    {
        Widget Widget { get; }
        string TabText { get; }
    }
    public interface IDockPane : IDockLayout
    {
        IEnumerable<IDockContent> Documents { get; }
        IDockContent Document { get; }

        void Add(IDockContent[] docs);
        bool Remove(IDockContent[] docs); // return true if pane now empty
        void RemoveWidget();

        void ActiveDocChanged();

        Canvas Widget { get; }
        void SetDrop(DockPosition? hit);
        void ClearDrop();
        DockPosition? HitTest(Point position);
        void Update(DockPosition? sel);
    }
    public interface IDockSplitter : IDockLayout
    {
        IEnumerable<IDockLayout> Layouts { get; }
        Orientation Orientation { get; }

        void Insert(int ind, IDockLayout dockLayout);
        void Remove(IDockLayout destination, bool removewidget);
        /*    Orientation Orientation { get; }

void Insert(int ind, IDockLayout dockTarget);
void Remove(IDockLayout dw, bool removewindow);
void GetSize(bool setsize);*/
    }
    public interface IDockLayout
    {
        void Layout(Point zero, Size size);
        void GetSize(bool setsize);
        Point Location { get; }
        Size MinimumSize { get; }
        Size MaximumSize { get; }
        Size Size { get; }
        DockPanel DockPanel {get;}

        bool HitTest(Point position, out IDockSplitter splitter, out int ind);
    }
    public interface IDockNotify
    {
        void OnLoaded(IDockPane pane);
        void OnUnloading();
    }
    public enum DockPosition
    {
        Document,
        Left,
        Right,
        Top,
        Bottom,
        Center,
        Float
    }
    public enum Orientation
    { 
        Horizontal,
        Vertical
    }
}
