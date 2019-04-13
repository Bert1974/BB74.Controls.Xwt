using BaseLib.DockIt_Xwt;
using BaseLib.Xwt;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Xwt;
using Xwt.Drawing;

namespace DockExample
{
    partial class mainwindow
    {
        class testtoolitem : Canvas, IDockToolbar, IDockSerializable
        {
            Widget IDockContent.Widget => this;
            string IDockContent.TabText => $"tool{this.Id}";
            IDockPane IDockContent.DockPane { get; set; }

            public int Id { get; }

            public testtoolitem(mainwindow main)
                : this(main.dock)
            {
            }
            private testtoolitem(DockPanel dock)
            {
                var w = dock.AllContent.OfType<testtoolitem>().Select(_tw => _tw.Id);

                if (w.Any())
                {
                    Id = w.Max() + 1;
                }
                else
                {
                    Id = 1;
                }
                Initialize();
            }
            public testtoolitem(string data)
            {
                this.Id = int.Parse(data);
                Initialize();
            }
            void Initialize()
            {
                this.MinWidth = this.MinHeight = 10;
                this.BackgroundColor = Colors.Aquamarine;
            }
            string IDockSerializable.Serialize()
            {
                return this.Id.ToString();
            }
        }



        class testpropertiesitem : Canvas, IDockToolbar
        {
            private PropertyGrid widget;
            private IDockPane dockpane;

            Widget IDockContent.Widget => this;
            string IDockContent.TabText => $"Properties";
            IDockPane IDockContent.DockPane
            {
                get => this.dockpane;
                set
                {
                    if (this.dockpane != null)
                    {
                        this.dockpane.DockPanel.ActiveDocumentChanged -= DockPanel_ActiveDocumentChanged;
                    }
                    if ((this.dockpane = value) != null)
                    {
                        this.dockpane.DockPanel.ActiveDocumentChanged += DockPanel_ActiveDocumentChanged;
                    }
                }
            }

            [TypeConverter(typeof(ExpandableObjectConverter))]
            public struct point
            {
                public int X { get; set; }
                public int Y { get; set; }
            }
                [TypeConverter(typeof(ExpandableObjectConverter))]
                class testobject
            {
                [Category("abc")]
                public point pt { get; set; } = new point();

                [Category("abc")]
                [DefaultValue("hello")]
                public string Text { get; set; }

                public Int32 getal { get; set; }


                public enum testenum
                {
                    een,
                    twee,
                    drie
                }

                public testenum test { get; set; }

           //     [Category("test")]
            //    public System.Drawing.Size size { get; set; } = new System.Drawing.Size();
            //    public System.Drawing.Rectangle rect { get; set; } = new System.Drawing.Rectangle();
            }
            private void DockPanel_ActiveDocumentChanged(object sender, EventArgs e)
            {
                this.widget.SelectedObject = new testobject();
            }
            
            public testpropertiesitem()
            {
                Initialize();
            }
            void Initialize()
            {
                this.BackgroundColor = Colors.Aquamarine;

                this.widget = new BaseLib.Xwt.PropertyGrid();
                this.AddChild(this.widget);

                this.MinWidth = this.widget.MinWidth;
                this.MinWidth = this.widget.MinHeight;
                this.OnBoundsChanged();

            }
            protected override void OnBoundsChanged()
            {
                this.SetChildBounds(this.widget, this.Bounds);
            }
        }
    }
}