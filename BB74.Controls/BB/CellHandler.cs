using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xwt;
using Xwt.Drawing;

namespace BaseLib.Xwt.Controls
{
    public interface ICellHandlerContainer
    {
        IListDataSource DataSource { get; }
        bool Selected(int row);
    }
    public abstract class CellHandler
    {
        public abstract class Cell
        {
            public CellHandler Owner { get; }
            public int Row { get; set; }
            public int Column { get; }
            public Rectangle position;
            private Widget widget;

            protected Cell(CellHandler owner, int row, int cell)
            {
                this.Owner = owner;
                this.Row = row;
                this.Column = cell;
            }
            public virtual Widget Widget
            {
                get => this.widget;
                set
                {
                    if (this.widget != null)
                    {
                        this.widget.ButtonPressed -= Widget_ButtonPressed;
                    }
                    this.widget = value;
                    if (this.widget != null)
                    {
                        this.widget.ButtonPressed += Widget_ButtonPressed;
                    }
                }
            }

            private void Widget_ButtonPressed(object sender, ButtonEventArgs e)
            {
            }

            public abstract bool NeedsPaint { get; }
            public virtual void SetPosition(Canvas canvas, int row, CellHandler cellHandler, Rectangle rectangle)
            {
                this.position = rectangle;

                if (this.widget != null)
                {
                    canvas.SetChildBounds(this.widget, rectangle);
                }
            }
            public virtual Size OnGetPreferredSize(SizeConstraint widthconstraints, SizeConstraint heightconstraints)
            {
                return this.widget?.GetBackend().GetPreferredSize(widthconstraints, heightconstraints) ?? Size.Zero;
            }
        }

        protected readonly ICellHandlerContainer owner;
        protected readonly CellView target;

        public abstract void Initialize();
        public abstract void Remove();
        public abstract Cell CreateForRow(int row, int cell);
        public abstract void InitialzeForRow(Cell cell);
        public abstract void DestroyForRow(Cell cell);
        public abstract void Sync(Cell cell);


        protected CellHandler(ICellHandlerContainer owner, CellView cell)
        {
            this.owner = owner;
            this.target = cell;
        }

        public virtual void Draw(Context ctx, int row, Cell cell)
        {
            ctx.SetColor(this.owner.Selected(row) ? Colors.LightBlue : Colors.White);
            ctx.Rectangle(cell.position);
            ctx.Fill();
        }

        public static CellHandler CreateFor(ICellHandlerContainer owner, CellView cell)
        {
            CellHandler r = null;
            if (cell.GetType() == typeof(ImageCellView))
            {
                r = new ImageCellHandler(owner, cell);
            }
            if (cell.GetType() == typeof(TextCellView))
            {
                r = new TextCellHandler(owner, cell);
            }
            r?.Initialize();
            return r;
        }
    }

    class ImageCellHandler : CellHandler
    {
        public class ImageCell : Cell
        {
            public override bool NeedsPaint => this.image != null;

            private ImageCellHandler owner;
            internal Image image;

            public ImageCell(ImageCellHandler owner, int row, int cell)
                : base(owner, row, cell)
            {
                this.owner = owner;
            }

            public override Size OnGetPreferredSize(SizeConstraint widthConstraints, SizeConstraint heightConstraints)
            {
                return new Size(widthConstraints.CalculateFor(this.image?.Width ?? 0), heightConstraints.CalculateFor(this.image?.Height ?? 0));
            }
        }
        public ImageCellHandler(ICellHandlerContainer owner, CellView cell)
            : base(owner, cell)
        {
        }

        public override void Initialize()
        {
        }
        public override void Remove()
        {
        }
        public override Cell CreateForRow(int row, int cell)
        {
            return new ImageCell(this, row, cell);
        }
        public override void InitialzeForRow(Cell cell)
        {
            Sync(cell);
        }
        public override void DestroyForRow(Cell cell)
        {
        }
        public override void Sync(Cell cell)
        {
            var imagecell = cell as ImageCell;

            if ((this.target.VisibleField?.Index ?? -1) != -1 ? (bool)owner.DataSource.GetValue(cell.Row, this.target.VisibleField.Index) : this.target.Visible)
            {
                imagecell.image = (Image)this.owner.DataSource.GetValue(cell.Row, cell.Column);
            }
        }
        public override void Draw(Context ctx, int row, Cell cell)
        {
            base.Draw(ctx, row, cell);
            var imagecell = cell as ImageCell;

            if (imagecell.image != null)
            {
                ctx.DrawImage(imagecell.image, cell.position);
            }
        }
    }
    class TextCellHandler : CellHandler
    {
        public class TextCell : Cell
        {
            public override bool NeedsPaint => !this.editable;

            private TextCellHandler owner;
            internal bool editable = false;

            public TextCell(TextCellHandler owner, int row, int cell)
                : base(owner, row, cell)
            {
                this.owner = owner;
            }
        }
        public TextCellHandler(ICellHandlerContainer owner, CellView cell)
            : base(owner, cell)
        {
        }
        public override void Initialize()
        {
        }
        public override void Remove()
        {
        }
        public override Cell CreateForRow(int row, int cell)
        {
            return new TextCell(this, row, cell);
        }
        public override void InitialzeForRow(Cell cell)
        {
            if ((this.target.VisibleField?.Index ?? -1) != -1 ? (bool)owner.DataSource.GetValue(cell.Row, this.target.VisibleField.Index) : this.target.Visible)
            {
                var txtcell = cell as TextCell;
                if ((target as TextCellView).EditableField != null)
                {
                    txtcell.editable = (bool)this.owner.DataSource.GetValue(cell.Row, (target as TextCellView).EditableField.Index);
                }
                else
                {
                    txtcell.editable = (target as TextCellView).Editable;
                }
                if (txtcell.editable)
                {
                    txtcell.Widget = new TextEntry() { };
                }
                else
                {
                    txtcell.Widget = new Label() { };
                }
            }
            Sync(cell);
        }
        public override void DestroyForRow(Cell cell)
        {
        }

        public override void Sync(Cell cell)
        {
            // check visible change ??

            var txtcell = cell as TextCell;

            if ((this.target.VisibleField?.Index ?? -1) != -1 ? (bool)owner.DataSource.GetValue(cell.Row, this.target.VisibleField.Index) : this.target.Visible)
            {
                var txt = (string)this.owner.DataSource.GetValue(cell.Row, cell.Column);

                if (txtcell.Widget is TextEntry)
                {
                    (txtcell.Widget as TextEntry).Text = txt;
                }
                else
                {
                    (txtcell.Widget as Label).Text = txt;
                }
            }
        }
    }
}
