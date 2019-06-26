using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xwt;
using Xwt.Backends;
using Xwt.Drawing;

namespace BaseLib.Xwt.Controls.PropertyGrid.Internals
{
    internal class EditCanvas : Canvas
    {
        class comboitem
        {
            public readonly object value;
            public readonly string text;

            public comboitem(object value, string text)
            {
                this.value = value;
                this.text = text;
            }
            public override string ToString() => this.text;
        }

        private readonly PropertyTab owner;
        public readonly GridItem item;
        public bool isreadonly, editmode, highlight;
        private TextEntry txtinput;
        private ComboBox cbinput;

        public EditCanvas(PropertyTab owner, GridItem item)
        {
            this.owner = owner;
            this.item = item;
            this.editmode = false;
            this.isreadonly = !this.item.TypeConverter.CanConvertFrom(this.item as ITypeDescriptorContext, typeof(string)) ||
                              !this.item.TypeConverter.CanConvertTo(this.item as ITypeDescriptorContext, typeof(string));
        }
        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);

            ctx.SetColor(Colors.White);
            ctx.Rectangle(this.Bounds);
            ctx.Fill();

            if (!this.editmode)
            {
                var value = this.owner.GetValue(item);
                string txt;

                if (this.item.TypeConverter.CanConvertTo(this.item as ITypeDescriptorContext, typeof(string)))
                {
                    txt = item.TypeConverter.ConvertToString(this.item as ITypeDescriptorContext, value);
                }
                else
                {
                    txt = value?.ToString();
                }
                ctx.SetColor(Colors.Black);

                var tl = new TextLayout() { Text = txt, Font = this.owner.Font, Trimming = TextTrimming.WordElipsis, Width = this.Bounds.Width, Height = this.Bounds.Height, TextAlignment = Alignment.Start };

                var ts = tl.GetSize();
                var xy = new Point(0, (this.Bounds.Height - ts.Height) * .5);

                ctx.DrawTextLayout(tl, xy);
            }
        }

        protected override void OnMouseEntered(EventArgs args)
        {
            base.OnMouseEntered(args);

            this.highlight = true;

            if (!this.owner.EditMode)
            {
                QueueDraw();
            }
        }
        protected override void OnMouseExited(EventArgs args)
        {
            base.OnMouseExited(args);

            this.highlight = false;

            if (!this.owner.EditMode)
            {
                QueueDraw();
            }
        }

        protected override void OnButtonPressed(ButtonEventArgs args)
        {
            if (args.Button == PointerButton.Left)
            {
                if (!this.isreadonly && !this.editmode)// && !(this.item is GridItemRoot))
                {
                    if (!this.owner.EditMode || this.owner.CancelEdit(true))
                    {
                        this.editmode = true;

                        var value = owner.GetValue(this.item);

                        if (this.item.TypeConverter.GetStandardValuesSupported(this.item as ITypeDescriptorContext))
                        {
                            this.cbinput = this.isreadonly ? new ComboBox() : new ComboBoxEntry();

                            var stdvalues = this.item.TypeConverter.GetStandardValues(this.item as ITypeDescriptorContext).Cast<object>().ToArray();
                            foreach (var stdvalue in stdvalues)
                            {
                                var stdtxt = item.PropertyDescriptor.Attributes.OfType<DisplayNameAttribute>().FirstOrDefault()?.DisplayName ??
                                             item.TypeConverter.ConvertToString(item as ITypeDescriptorContext, stdvalue);
                                this.cbinput.Items.Add(new comboitem(stdvalue, stdtxt));
                            }
                            var sel = this.cbinput.Items.Cast<comboitem>().FirstOrDefault(_i => object.Equals(value, _i.value));
                            this.cbinput.SelectedItem = sel;
                            this.AddChild(this.cbinput);
                            this.SetChildBounds(this.cbinput, this.Bounds);

                            if (!this.isreadonly)
                            {
                                (this.cbinput as ComboBoxEntry).TextEntry.TextAlignment = Alignment.Start;
                                (this.cbinput as ComboBoxEntry).TextEntry.Text = sel?.text ?? "";
                                (this.cbinput as ComboBoxEntry).TextEntry.Changed += cbinput_textchanged;
                            }
                            cbinput.SelectionChanged += Cbinput_SelectionChanged;
                        }
                        else
                        {
                            var text = item.TypeConverter?.ConvertToString(item as ITypeDescriptorContext, value) ?? value?.ToString();

                            this.txtinput = new TextEntry()
                            {
                                Text = text,
                                ExpandHorizontal = true
                            };
                            this.AddChild(this.txtinput);
                            this.SetChildBounds(this.txtinput, this.Bounds);
                        }
                        QueueDraw();

                        args.Handled = true;
                        return;
                    }
                }
            }
            base.OnButtonPressed(args);
        }
        private void cbinput_textchanged(object sender, EventArgs e)
        {
            var cb = (this.cbinput as ComboBoxEntry);
            string newvalue = cb.TextEntry.Text;

            var matches = this.cbinput.Items.Cast<comboitem>().Where(_i => _i.text.StartsWith(newvalue, StringComparison.CurrentCulture)).ToArray();

            if (matches.Count() == 1 && this.item.TypeConverter.GetStandardValuesExclusive(this.item as ITypeDescriptorContext))
            {
                /*  var fullvalue = matches.First().text;
                  if (newvalue.Length == fullvalue.Length)*/
                {
                    owner.SetValue(this.item, matches.First().value);
                    //   this.cbinput.SelectedItem =matches.First();
                }
                /* else
                 {
                     cb.TextEntry.Text = fullvalue;
                     cb.TextEntry.SelectionStart = newvalue.Length;
                     cb.TextEntry.SelectionLength = fullvalue.Length-newvalue.Length;
                 }*/
            }
            /*  else
              {
                  cb.TextEntry.SelectionLength = 0;
              }*/
        }
        internal void Showdropdown(Widget control)
        {
        }
        internal void CloseDropDown()
        {
        }

        private void Cbinput_SelectionChanged(object sender, EventArgs e)
        {
            object newvalue = (this.cbinput.SelectedItem as comboitem)?.value;

            if (newvalue != null)
            {
                owner.SetValue(this.item, newvalue);
            }
        }
        protected override void OnBoundsChanged()
        {
            base.OnBoundsChanged();

            if (this.txtinput != null)
            {
                var h = this.txtinput.Size.Height;
                var r = new Rectangle(0, (this.Bounds.Height - h) / 2, this.Bounds.Width, h);
                this.SetChildBounds(this.txtinput, r);
            }
            if (this.cbinput != null)
            {
                var h = this.cbinput.Size.Height;
                var r = new Rectangle(0, (this.Bounds.Height - h) / 2, this.Bounds.Width, h);
                this.SetChildBounds(this.cbinput, r);
            }
        }
        protected override void OnKeyPressed(KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.Escape:
                    owner.CancelEdit(false);
                    args.Handled = true;
                    return;

                case Key.Return:
                    owner.CancelEdit(true);
                    args.Handled = true;
                    return;
            }
            base.OnKeyPressed(args);
        }
        internal bool CancelEdit(bool apply)
        {
            Debug.Assert(this.editmode);

            if (apply)
            {
                object value;

                if (this.txtinput != null)
                {
                    var text = this.txtinput.Text;
                    value = item.TypeConverter.ConvertFromString(item as ITypeDescriptorContext, text);
                }
                else if (cbinput != null)
                {
                    value = (this.cbinput.SelectedItem as comboitem)?.value;

                    if (value == null)
                    {
                        throw new Exception("nothing selected");
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
                owner.SetValue(this.item, value);
            }
            this.editmode = false;
            if (this.txtinput != null)
            {
                this.RemoveChild(this.txtinput);
                this.txtinput.Dispose();
                this.txtinput = null;
            }
            if (this.cbinput != null)
            {
                this.RemoveChild(this.cbinput);
                this.cbinput.Dispose();
                this.cbinput = null;
            }
            this.QueueDraw();
            return true;
        }

        internal void Refresh()
        {
            Debug.Assert(!this.editmode);

            this.QueueDraw();
        }
    }
}