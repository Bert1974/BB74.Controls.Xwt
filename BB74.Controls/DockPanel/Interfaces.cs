using System;
using System.Collections.Generic;
using Xwt;

namespace BaseLib.Xwt
{
    using Xwt = global::Xwt;

    public interface IXwt
    {
        void SetCapture(Widget widget);
        void ReleaseCapture(Widget widget);
        void DoEvents();
        void DoEvents(Func<bool> cancelfunction);
        void SetParent(WindowFrame window, WindowFrame parentWindow);
        void QueueOnUI(Action method);
        void GetMouseInfo(WindowFrame window, out int mx, out int my, out uint buttons);
        void StartDrag(Widget widget, DragOperation operation);
    }
}
namespace BaseLib.Xwt.Controls.DockPanel
{
    using Xwt = global::Xwt;
    
    public interface IDockToolbar : IDockContent // use this for your docking toolbar widget
    {
    }
    public interface IDockDocument : IDockContent// use this for your docking document widget
    {
    }
    public interface IDockNotify // optional for IDockContent
    {
        void OnLoaded(IDockPane pane);
        void OnUnloading();
    }
    public interface IDockCustomize // optional for IDockContent
    {
        bool CanClose { get; }
        bool CanFloat { get; }
        bool HideWhenDocking { get; }
    }
    public delegate IDockContent DeserializeDockContent(DockPanel dock, Type type, string persistString);
    public interface IDockSerializable // optional for IDockContent
    {
        string Serialize();
    }
    public interface IDockContent
    {
        IDockPane DockPane { get; set; }
        Widget Widget { get; }
        string TabText { get; }
    }
    public interface IDockFloatWindow //: IDockPane//, IDockNotify
    {
        Xwt.Window Window { get; }
        DockPanel DockPanel { get; }
        DockPanel MainDockPanel { get; }
        //  DockPanel MainDockPanel { get; }

        IDockPane DockToolbar(IDockContent[] controls, DockPosition pos, IDockPane destination);

        void Close();
        void Invalidate();
    }
    public interface IDockPane : IDockLayout
    {
        IEnumerable<IDockContent> Documents { get; }
        IDockContent Document { get; }

        void Add(IDockContent[] docs);
        bool Remove(IDockContent[] docs); // return true if pane now empty

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
        IDockLayout Get(int ind);
    }
    public interface IDockLayout : IDisposable
    {
        void Layout(Point zero, Size size);
        void GetSize(bool setsize);
        Point Location { get; }
        Size MinimumSize { get; }
        Size MaximumSize { get; }
        Size WidgetSize { get; }
        DockPanel DockPanel { get; }

        bool HitTest(Point position, out IDockSplitter splitter, out int ind);
        void AddWidget();
        void RemoveWidget();
        void NewDockPanel(DockPanel dockpanel);
        void OnHidden();
    }
    public enum DockPosition
    {
        //       Document,->Center
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
    public enum DocumentStyle
    {
        JustDock,
        SDI
    }
}