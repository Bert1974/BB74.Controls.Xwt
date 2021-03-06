﻿using BaseLib.Xwt.Controls.DockPanel.Internals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xwt;

namespace BaseLib.Xwt.Controls.DockPanel.Serialization
{
    [Serializable()]
    public abstract class DockState
    {
        public static Type[] SerializeTypes => new Type[] {  typeof(DockState), typeof(DocumentsSave), typeof(PaneSave), typeof(PaneEmpty), typeof(FloatWindowSave) };

        public static DockSave SaveState(DockPanel dockPanel)
        {
            var forms = BaseLib.Xwt.Platform.Instance.AllForms(dockPanel.ParentWindow).Where(_t=>_t.Item2!=null).Select(_t=>_t.Item2).ToArray(); // all open windows where framework element is found

            return new DockSave()
            {
                state = dockPanel.Current != null ? Save(dockPanel.Current) : new PaneEmpty(),
                floating = dockPanel.Floating.OrderByDescending(_f => Array.IndexOf(forms, _f)).Select(_f => DockState.Save(_f)).ToArray()
            };
        }
        private static DockState Save(object p)
        {
            if (p is IDockPane)
            {
                return Save(p as IDockPane);
            }
            if (p is IDockSplitter)
            {
                return Save(p as IDockSplitter);
            }
            if (p is IDockFloatWindow)
            {
                return Save(p as IDockFloatWindow);
            }
            throw new NotImplementedException();
        }
        private static DockState Save(IDockPane p)
        {
            return new DocumentsSave()
            {
                persiststring = p.Documents.Select(_i => Save(_i)).ToArray(),
                clientsize = p.WidgetSize
            };
        }
        private static string Save(IDockContent p)
        {
            var b = new StringBuilder();

            b.Append(p.GetType().FullName);
            b.Append(":");
            if (p is IDockSerializable)
            {
                b.Append("D:");
                b.Append((p as IDockSerializable).Serialize());
            }
            b.Append(":");
            return b.ToString();
        }
        private static DockState Save(IDockFloatWindow p)
        {
            return new FloatWindowSave()
            {
                Position = p.Window.Location,
                Size = p.Window.Size,
                Content = DockState.Save(p.DockPanel.Current)
            };
        }
        private static DockState Save(IDockSplitter split)
        {
            return new PaneSave() { panes = split.Layouts.Select(_p => Save((object)_p)).ToArray(), orientation = split.Orientation, clientsize = split.WidgetSize};
        }

        internal abstract object Restore(DockPanel dockPanel, DeserializeDockContent deserializeDockContent);
    }
    [Serializable()]
    public class DockSave : DockState
    {
        public DockState state;
        public DockState[] floating;

        internal override object Restore(DockPanel dockPanel, DeserializeDockContent deserializeDockContent)
        {
            var result = state.Restore(dockPanel, deserializeDockContent);
            
            return result;
        }

        private IDockSplitter FindSplitter(IDockLayout content, IDockPane searchfor, out int ind)
        {
            if (content is IDockSplitter)
            {
                int cnt = 0;
                foreach (var l in (content as IDockSplitter).Layouts)
                {
                    if (object.ReferenceEquals(searchfor, l))
                    {
                        ind = cnt;
                        return content as IDockSplitter;
                    }
                    var r = FindSplitter(l, searchfor, out ind);
                    if (r != null)
                    {
                        return r;
                    }
                    cnt++;
                }
            }
            ind = -1;
            return null;
        }

        private IEnumerable<IDockLayout> _AllLayouts(IDockLayout item)
        {
            yield return item as IDockLayout;
            if (item is IDockSplitter)
            {
                foreach (var l in (item as IDockSplitter).Layouts.SelectMany(_l => _AllLayouts(_l)))
                {
                    yield return l;
                }
            }
        }
    }
    [Serializable()]
    public class DocumentsSave : DockState
    {
        public string[] persiststring;
        public Size clientsize;

        internal override object Restore(DockPanel dockPanel, DeserializeDockContent deserializeDockContent)
        {
            var ctls = this.persiststring.Select((_t) =>
            {
                int ind = _t.IndexOf(":");
                if (ind > 0)
                {
                    Type t = BaseLib.Xwt.Platform.GetType(_t.Substring(0, ind));

                    if (ind + 2 >= _t.Length)
                    {
                        try
                        {
                            return deserializeDockContent(dockPanel, t, null) ?? (IDockContent)Activator.CreateInstance(t);
                        }
                        catch
                        {
                        }
                    }
                    else if (_t.Substring(ind + 1, 2) == "D:")
                    {
                        var data = _t.Substring(ind + 3, _t.Length - (ind + 4));
                        return deserializeDockContent(dockPanel, t, data);
                    }
                }
                return null;
            }).Where(_c => _c != null).ToArray();

            var result = new DockPane(dockPanel, ctls) { WidgetSize = this.clientsize };
            return result;
        }
    }
    [Serializable()]
    public class PaneEmpty : DockState
    {
        internal override object Restore(DockPanel dockPanel, DeserializeDockContent deserializeDockContent)
        {
            return null;
        }
    }
    [Serializable()]
    public class PaneSave : DockState
    {
        public DockState[] panes;
        public Orientation orientation;
        public Size clientsize;

        internal override object Restore(DockPanel dockPanel, DeserializeDockContent deserializeDockContent)
        {
            var r = new DockSplitter(dockPanel, this.orientation, this.panes.Select(_sd => _sd.Restore(dockPanel, deserializeDockContent) as IDockLayout).Where(_c => _c != null).ToArray());
            r.WidgetSize = clientsize;
            r.GetSize(false);
            return r;
        }
    }
    [Serializable()]
    public class FloatWindowSave : DockState
    {
        public Point Position;
        public Size Size;
        public DockState Content;

        internal override object Restore(DockPanel dockPanel, DeserializeDockContent deserializeDockContent)
        {
            var form = FloatWindow.Create(dockPanel, new IDockContent[0], new Rectangle(this.Position, this.Size), out IDockPane pane);

            form.DockPanel.BeginLayout();
            try
            {
             //   (pane as IDockLayout)?.Remove();
                form.DockPanel.Dock((IDockLayout)this.Content.Restore(form.DockPanel, deserializeDockContent));
            }
            catch { }
            form.DockPanel.EndLayout();
            return form;
        }
    }
}