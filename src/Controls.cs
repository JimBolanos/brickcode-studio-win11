// ---------------------------------------------------------------------------
// Custom controls. WinForms has nothing that looks like the BrickCode Studio
// GTK interface, so every piece is painted by hand.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BrickCode
{
    static class Ui
    {
        public static Palette P = Palette.Make(false);

        static readonly Dictionary<string, Font> cache =
            new Dictionary<string, Font>(StringComparer.Ordinal);

        public static Font F(float size, FontStyle style)
        {
            string key = size.ToString("0.0") + "/" + ((int)style).ToString();
            Font f;
            if (cache.TryGetValue(key, out f)) return f;
            f = new Font("Segoe UI", size, style, GraphicsUnit.Point);
            cache[key] = f;
            return f;
        }

        public static Font F(float size) { return F(size, FontStyle.Regular); }
        public static Font B(float size) { return F(size, FontStyle.Bold); }

        public static Font Sym(float size)
        {
            string key = "sym" + size.ToString("0.0");
            Font f;
            if (cache.TryGetValue(key, out f)) return f;
            f = new Font("Segoe UI Symbol", size, FontStyle.Regular, GraphicsUnit.Point);
            cache[key] = f;
            return f;
        }

        public static Font Mono(float size, FontStyle style)
        {
            string key = "m" + size.ToString("0.0") + "/" + ((int)style).ToString();
            Font f;
            if (cache.TryGetValue(key, out f)) return f;
            f = new Font("Consolas", size, style, GraphicsUnit.Point);
            if (f.Name.IndexOf("Consolas", StringComparison.OrdinalIgnoreCase) < 0)
                f = new Font(FontFamily.GenericMonospace, size, style, GraphicsUnit.Point);
            cache[key] = f;
            return f;
        }

        public static Font Mono(float size) { return Mono(size, FontStyle.Regular); }
    }

    // -----------------------------------------------------------------------
    // Flat rounded button
    // -----------------------------------------------------------------------
    class FlatButton : Control
    {
        public Color Fill = Color.Gray;
        public Color FgColor = Color.White;
        public Color BorderColor = Color.Empty;
        public int Radius = 10;
        public bool Bold = true;
        public float FontSize = 9.75f;
        public string Glyph = null;          // drawn before the text
        public Font GlyphFont = null;
        public int GlyphGap = 7;
        public ContentAlignment Align = ContentAlignment.MiddleCenter;

        bool hover, down;

        public FlatButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Dr.Quality(g);
            Rectangle r = new Rectangle(0, 0, Width, Height);

            Color fill = Fill;
            if (!Enabled)
                fill = Palette.Mix(fill, Ui.P.Bg, 0.55);
            else if (down)
                fill = Palette.Darken(fill, 0.88);
            else if (hover)
                fill = Palette.Mix(fill, Ui.P.Dark ? Color.White : Color.Black, 0.10);

            Dr.Fill(g, r, Radius, fill);
            if (BorderColor != Color.Empty)
                Dr.Border(g, r, Radius, BorderColor);

            Color fg = Enabled ? FgColor : Palette.Mix(FgColor, fill, 0.55);
            Font f = Bold ? Ui.B(FontSize) : Ui.F(FontSize);

            string text = Text == null ? "" : Text;
            if (!string.IsNullOrEmpty(Glyph))
            {
                Font gf = GlyphFont != null ? GlyphFont : Ui.Sym(FontSize);
                int gw = Dr.MeasureWidth(g, Glyph, gf);
                int tw = Dr.MeasureWidth(g, text, f);
                int total = gw + (text.Length > 0 ? GlyphGap + tw : 0);
                int x = (Width - total) / 2;
                if (Align == ContentAlignment.MiddleLeft) x = 12;
                Dr.TextLeft(g, Glyph, gf, fg, new Rectangle(x, 0, gw + 4, Height));
                if (text.Length > 0)
                    Dr.TextLeft(g, text, f, fg,
                        new Rectangle(x + gw + GlyphGap, 0, tw + 6, Height));
            }
            else if (Align == ContentAlignment.MiddleLeft)
                Dr.TextLeft(g, text, f, fg, new Rectangle(12, 0, Width - 18, Height));
            else
                Dr.TextCenter(g, text, f, fg, r);
        }
    }

    // -----------------------------------------------------------------------
    // Text field with a rounded border and a placeholder
    // -----------------------------------------------------------------------
    class EntryBox : Control
    {
        public readonly TextBox Box = new TextBox();
        public int Radius = 9;

        string placeholder = "";
        bool showingHint;

        public EntryBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Box.BorderStyle = BorderStyle.None;
            Box.AutoSize = false;
            Controls.Add(Box);
            Box.GotFocus += delegate { HideHint(); Invalidate(); };
            Box.LostFocus += delegate { ShowHint(); Invalidate(); };
            Box.TextChanged += delegate { Invalidate(); };
        }

        /// The placeholder is real grey text inside the box, so it is never
        /// hidden behind the child control the way painted text would be.
        public string Placeholder
        {
            get { return placeholder; }
            set
            {
                if (showingHint) { showingHint = false; Box.Text = ""; }
                placeholder = value == null ? "" : value;
                ShowHint();
            }
        }

        void ShowHint()
        {
            if (showingHint || Box.Focused) return;
            if (Box.Text.Length != 0 || placeholder.Length == 0) return;
            showingHint = true;
            Box.Text = placeholder;
            Box.ForeColor = Ui.P.Muted;
            Box.SelectionStart = 0;
        }

        void HideHint()
        {
            if (!showingHint) return;
            showingHint = false;
            Box.Text = "";
            Box.ForeColor = Ui.P.Fg;
        }

        public override string Text
        {
            get { return showingHint ? "" : Box.Text; }
            set
            {
                showingHint = false;
                Box.ForeColor = Ui.P.Fg;
                Box.Text = value == null ? "" : value;
                if (Box.Text.Length == 0) ShowHint();
            }
        }

        public void ApplyTheme()
        {
            Box.BackColor = Ui.P.Field;
            Box.ForeColor = showingHint ? Ui.P.Muted : Ui.P.Fg;
            Box.Font = Ui.F(9.75f);
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int h = Box.PreferredHeight;
            Box.SetBounds(11, Math.Max(2, (Height - h) / 2), Width - 22, h);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Dr.Quality(g);
            Rectangle r = new Rectangle(0, 0, Width, Height);
            Dr.Fill(g, r, Radius, Ui.P.Field);
            Dr.Border(g, r, Radius, Box.Focused ? Ui.P.Accent : Ui.P.Border);
        }
    }

    // -----------------------------------------------------------------------
    // Drop-down button with a themed popup
    // -----------------------------------------------------------------------
    class DropButton : Control
    {
        public List<string> Items = new List<string>();
        public int SelectedIndex = 0;
        public int Radius = 9;
        public bool Bold = false;
        public float FontSize = 9.75f;
        public bool Flat = false;        // no border, used in the toolbar
        public event EventHandler SelectionChanged;

        bool hover;

        public DropButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        public string SelectedItem
        {
            get
            {
                if (SelectedIndex >= 0 && SelectedIndex < Items.Count)
                    return Items[SelectedIndex];
                return "";
            }
        }

        public void Select(int i, bool raise)
        {
            if (i < 0 || i >= Items.Count || i == SelectedIndex) { Invalidate(); return; }
            SelectedIndex = i;
            Invalidate();
            if (raise && SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            ShowMenu();
        }

        void ShowMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Renderer = new MenuRenderer();
            menu.BackColor = Ui.P.Card;
            menu.ForeColor = Ui.P.Fg;
            menu.Font = Ui.F(9.75f);
            menu.ShowImageMargin = false;
            for (int i = 0; i < Items.Count; i++)
            {
                ToolStripMenuItem mi = new ToolStripMenuItem(Items[i]);
                mi.Tag = i;
                mi.Checked = (i == SelectedIndex);
                mi.ForeColor = Ui.P.Fg;
                mi.Click += delegate(object s, EventArgs ev)
                {
                    Select((int)((ToolStripMenuItem)s).Tag, true);
                };
                menu.Items.Add(mi);
            }
            menu.Show(this, new Point(0, Height + 2));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Dr.Quality(g);
            Rectangle r = new Rectangle(0, 0, Width, Height);
            Color fill = Ui.P.Field;
            if (hover) fill = Palette.Mix(fill, Ui.P.Dark ? Color.White : Color.Black, 0.07);
            Dr.Fill(g, r, Radius, fill);
            if (!Flat) Dr.Border(g, r, Radius, Ui.P.Border);

            Dr.TextLeft(g, SelectedItem, Bold ? Ui.B(FontSize) : Ui.F(FontSize), Ui.P.Fg,
                        new Rectangle(11, 0, Width - 34, Height));
            DrawCaret(g, new Rectangle(Width - 24, 0, 14, Height), Ui.P.Muted);
        }

        public static void DrawCaret(Graphics g, Rectangle box, Color c)
        {
            int cx = box.X + box.Width / 2;
            int cy = box.Y + box.Height / 2;
            Point[] pts = new Point[]
            {
                new Point(cx - 4, cy - 2),
                new Point(cx + 4, cy - 2),
                new Point(cx, cy + 3)
            };
            using (SolidBrush b = new SolidBrush(c))
                g.FillPolygon(b, pts);
        }
    }

    class MenuRenderer : ToolStripProfessionalRenderer
    {
        public MenuRenderer() : base(new MenuColors()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected ? Color.White : Ui.P.Fg;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (Pen p = new Pen(Ui.P.Border))
                e.Graphics.DrawRectangle(p, 0, 0,
                    e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
        }
    }

    class MenuColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Ui.P.Card; } }
        public override Color MenuItemSelected { get { return Ui.P.Accent; } }
        public override Color MenuItemSelectedGradientBegin { get { return Ui.P.Accent; } }
        public override Color MenuItemSelectedGradientEnd { get { return Ui.P.Accent; } }
        public override Color MenuItemBorder { get { return Ui.P.Accent; } }
        public override Color MenuBorder { get { return Ui.P.Border; } }
        public override Color ImageMarginGradientBegin { get { return Ui.P.Card; } }
        public override Color ImageMarginGradientMiddle { get { return Ui.P.Card; } }
        public override Color ImageMarginGradientEnd { get { return Ui.P.Card; } }
        public override Color CheckBackground { get { return Ui.P.Field; } }
        public override Color CheckSelectedBackground { get { return Ui.P.Accent; } }
    }

    // -----------------------------------------------------------------------
    // Rounded card
    // -----------------------------------------------------------------------
    class CardPanel : Panel
    {
        public int Radius = 12;
        public bool DrawBorder = true;
        public Color FillColor = Color.Empty;

        public CardPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Dr.Quality(g);
            using (SolidBrush b = new SolidBrush(Parent != null ? Parent.BackColor : Ui.P.Bg))
                g.FillRectangle(b, ClientRectangle);
            Color fill = FillColor == Color.Empty ? Ui.P.Card : FillColor;
            Rectangle r = new Rectangle(0, 0, Width, Height);
            Dr.Fill(g, r, Radius, fill);
            if (DrawBorder) Dr.Border(g, r, Radius, Ui.P.Border);
            base.OnPaint(e);   // lets callers hook the Paint event
        }
    }

    // -----------------------------------------------------------------------
    // Console output
    // -----------------------------------------------------------------------
    class LogView : RichTextBox
    {
        public LogView()
        {
            BorderStyle = BorderStyle.None;
            ReadOnly = true;
            WordWrap = false;
            ScrollBars = RichTextBoxScrollBars.Both;
            DetectUrls = false;
            TabStop = false;
        }

        public void ApplyTheme()
        {
            BackColor = Ui.P.Card;
            // Setting ForeColor or Font recolours every run already in the
            // box, so only touch them while it is still empty.
            if (TextLength == 0)
            {
                ForeColor = Ui.P.EditorFg;
                Font = Ui.Mono(9.5f);
            }
        }

        public void Append(string text, Color colour, bool bold)
        {
            if (text == null) return;
            SelectionStart = TextLength;
            SelectionLength = 0;
            SelectionColor = colour;
            SelectionFont = Ui.Mono(9.5f, bold ? FontStyle.Bold : FontStyle.Regular);
            AppendText(text);
            SelectionStart = TextLength;
            ScrollToCaret();
        }

        public void Line(string text, Color colour) { Append(text + "\n", colour, false); }
    }

    // -----------------------------------------------------------------------
    // The code editor: a RichTextBox with NQC highlighting and a gutter.
    // -----------------------------------------------------------------------
    class CodeEditor : Control
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref Point lParam);

        const int WM_SETREDRAW = 0x000B;
        const int EM_GETSCROLLPOS = 0x04DD;
        const int EM_SETSCROLLPOS = 0x04DE;
        const int EM_GETEVENTMASK = 0x043B;
        const int EM_SETEVENTMASK = 0x0445;

        public readonly RichTextBox Box = new RichTextBox();
        readonly DblPanel gutter = new DblPanel();
        readonly Timer debounce = new Timer();

        bool highlighting;
        int errorLine = -1;
        int gutterWidth = 46;

        static readonly Regex ReNumber = new Regex(@"\b\d+\.?\d*\b", RegexOptions.Compiled);
        static readonly Regex ReString = new Regex("\"([^\"\\\\]|\\\\.)*\"", RegexOptions.Compiled);
        static readonly Regex ReLineComment = new Regex(@"//[^\n]*", RegexOptions.Compiled);
        static readonly Regex ReBlockComment =
            new Regex(@"/\*.*?\*/", RegexOptions.Compiled | RegexOptions.Singleline);
        static readonly Regex RePreproc =
            new Regex(@"^\s*#\w+", RegexOptions.Compiled | RegexOptions.Multiline);
        static Regex ReKeyword, ReBuiltin;

        static readonly string[] Keywords =
        {
            "task","sub","inline","if","else","while","do","for","repeat",
            "until","break","continue","return","switch","case","default",
            "goto","start","stop","monitor","acquire","catch","asm","abs",
            "sign","int","void","const","bool","true","false",
            "long","byte","char","short","unsigned","float","string","struct",
            "typedef","safecall","enum"
        };

        static readonly string[] Builtins =
        {
            "OnFwd","OnRev","OnFor","On","Off","Float","Toggle","Fwd","Rev",
            "SetOutput","SetDirection","SetPower","Wait","SetSensor",
            "SetSensorType","SetSensorMode","ClearSensor","SensorValue",
            "SensorType","SensorMode","SensorValueBool","SensorValueRaw",
            "PlaySound","PlayTone","MuteSound","UnmuteSound","ClearSound",
            "SelectDisplay","SetWatch","Watch","ClearTimer","Timer",
            "FastTimer","SetTimer","Random","SetRandomSeed","SendMessage",
            "ClearMessage","Message","SetTxPower","SetSleepTime","SleepNow",
            "StopAllTasks","CreateDatalog","AddToDatalog","UploadDatalog",
            "SetGlobalOutput","SetGlobalDirection","SetMaxPower","Program",
            "BatteryLevel","Counter","ClearCounter","IncCounter","DecCounter",
            "OUT_A","OUT_B","OUT_C","OUT_FULL","OUT_HALF","OUT_LOW","OUT_ON",
            "OUT_OFF","OUT_FLOAT","OUT_FWD","OUT_REV","OUT_TOGGLE","SENSOR_1",
            "SENSOR_2","SENSOR_3","SENSOR_TYPE_NONE","SENSOR_TYPE_TOUCH",
            "SENSOR_TYPE_TEMPERATURE","SENSOR_TYPE_LIGHT","SENSOR_TYPE_ROTATION",
            "SENSOR_MODE_RAW","SENSOR_MODE_BOOL","SENSOR_MODE_EDGE",
            "SENSOR_MODE_PULSE","SENSOR_MODE_PERCENT","SENSOR_MODE_CELSIUS",
            "SENSOR_MODE_FAHRENHEIT","SENSOR_MODE_ROTATION","SENSOR_TOUCH",
            "SENSOR_LIGHT","SENSOR_ROTATION","SENSOR_CELSIUS",
            "SENSOR_FAHRENHEIT","SENSOR_PULSE","SENSOR_EDGE","SOUND_CLICK",
            "SOUND_DOUBLE_BEEP","SOUND_DOWN","SOUND_UP","SOUND_LOW_BEEP",
            "SOUND_FAST_UP","DISPLAY_WATCH","DISPLAY_SENSOR_1",
            "DISPLAY_SENSOR_2","DISPLAY_SENSOR_3","DISPLAY_OUT_A",
            "DISPLAY_OUT_B","DISPLAY_OUT_C","TX_POWER_LO","TX_POWER_HI",
            "TextOut","NumOut","ClearScreen","LineOut","CircleOut","RectOut",
            "PointOut","RotateMotor","ResetTachoCount","MotorTachoCount",
            "ButtonPressed","CurrentTick","SetSensorTouch","SetSensorLight",
            "SetSensorSound","SetSensorLowspeed","SetSensorColorFull",
            "SetSensorColorRed","Sensor","SensorUS","StopSound","PlayTone",
            "SendRemoteNumber","SendResponseNumber","ReceiveRemoteNumber",
            "BluetoothStatus","OUT_AB","OUT_AC","OUT_BC","OUT_ABC","IN_1",
            "IN_2","IN_3","IN_4","LCD_LINE1","LCD_LINE2","LCD_LINE3",
            "LCD_LINE4","LCD_LINE5","LCD_LINE6","LCD_LINE7","LCD_LINE8",
            "MAILBOX1","MAILBOX2","MAILBOX3","MAILBOX4","MAILBOX5"
        };

        static CodeEditor()
        {
            ReKeyword = new Regex(@"\b(" + string.Join("|", Keywords) + @")\b",
                                  RegexOptions.Compiled);
            ReBuiltin = new Regex(@"\b(" + string.Join("|", Builtins) + @")\b",
                                  RegexOptions.Compiled);
        }

        public event EventHandler DirtyChanged;

        public CodeEditor()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            gutter.Dock = DockStyle.Left;
            gutter.Width = gutterWidth;
            gutter.Paint += PaintGutter;

            Box.BorderStyle = BorderStyle.None;
            Box.Multiline = true;
            Box.WordWrap = false;
            Box.AcceptsTab = true;
            Box.ScrollBars = RichTextBoxScrollBars.Both;
            Box.DetectUrls = false;
            Box.Dock = DockStyle.Fill;
            Box.HideSelection = false;

            Controls.Add(Box);
            Controls.Add(gutter);

            debounce.Interval = 180;
            debounce.Tick += delegate { debounce.Stop(); Highlight(); };

            Box.TextChanged += delegate
            {
                if (highlighting) return;
                debounce.Stop();
                debounce.Start();
                gutter.Invalidate();
                if (DirtyChanged != null) DirtyChanged(this, EventArgs.Empty);
            };
            Box.VScroll += delegate { gutter.Invalidate(); };
            Box.HScroll += delegate { gutter.Invalidate(); };
            Box.Resize += delegate { gutter.Invalidate(); };
            Box.SelectionChanged += delegate { gutter.Invalidate(); };
            Box.FontChanged += delegate { gutter.Invalidate(); };
        }

        public void ApplyTheme()
        {
            BackColor = Ui.P.EditorBg;
            gutter.BackColor = Ui.P.GutterBg;
            Box.BackColor = Ui.P.EditorBg;
            Box.ForeColor = Ui.P.EditorFg;
            Box.Font = Ui.Mono(10.5f);
            Highlight();
            gutter.Invalidate();
        }

        public string Code
        {
            get { return Box.Text; }
            set { Box.Text = value; Highlight(); }
        }

        public void SetErrorLine(int line)
        {
            errorLine = line;
            if (line >= 1)
            {
                try
                {
                    int idx = Box.GetFirstCharIndexFromLine(line - 1);
                    if (idx >= 0)
                    {
                        Box.SelectionStart = idx;
                        Box.SelectionLength = 0;
                        Box.ScrollToCaret();
                    }
                }
                catch (Exception) { }
            }
            Highlight();
            gutter.Invalidate();
        }

        void PaintGutter(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush b = new SolidBrush(Ui.P.GutterBg))
                g.FillRectangle(b, gutter.ClientRectangle);
            using (Pen p = new Pen(Palette.Mix(Ui.P.GutterBg, Ui.P.Border, 0.8)))
                g.DrawLine(p, gutter.Width - 1, 0, gutter.Width - 1, gutter.Height);

            if (Box.Font == null) return;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int first = Box.GetLineFromCharIndex(Box.GetCharIndexFromPosition(new Point(1, 1)));
            int last = Box.GetLineFromCharIndex(
                Box.GetCharIndexFromPosition(new Point(1, Box.ClientSize.Height - 1)));
            int total = Box.Lines.Length;
            if (total == 0) total = 1;
            if (last < first) last = first;
            if (last > total) last = total;

            int caretLine = Box.GetLineFromCharIndex(Box.SelectionStart);

            using (StringFormat sf = new StringFormat(StringFormatFlags.NoWrap))
            {
                sf.Alignment = StringAlignment.Far;
                for (int i = first; i <= last + 1 && i < total; i++)
                {
                    int idx = Box.GetFirstCharIndexFromLine(i);
                    if (idx < 0) continue;
                    Point pt = Box.GetPositionFromCharIndex(idx);
                    if (pt.Y > Box.ClientSize.Height) break;
                    Color c = Ui.P.GutterFg;
                    if (i == errorLine - 1) c = Ui.P.Red;
                    else if (i == caretLine) c = Ui.P.EditorFg;
                    using (SolidBrush b = new SolidBrush(c))
                        g.DrawString((i + 1).ToString(), Box.Font, b,
                            new RectangleF(0, pt.Y + Box.Top, gutter.Width - 10,
                                           Box.Font.Height + 2), sf);
                }
            }
        }

        public void Highlight()
        {
            if (highlighting || !IsHandleCreated) return;
            highlighting = true;

            IntPtr mask = SendMessage(Box.Handle, EM_GETEVENTMASK, IntPtr.Zero, IntPtr.Zero);
            SendMessage(Box.Handle, EM_SETEVENTMASK, IntPtr.Zero, IntPtr.Zero);
            SendMessage(Box.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);

            Point scroll = new Point();
            SendMessage(Box.Handle, EM_GETSCROLLPOS, IntPtr.Zero, ref scroll);
            int selStart = Box.SelectionStart;
            int selLen = Box.SelectionLength;

            try
            {
                string text = Box.Text;
                Box.SelectAll();
                Box.SelectionColor = Ui.P.EditorFg;
                Box.SelectionBackColor = Ui.P.EditorBg;
                Box.SelectionFont = Ui.Mono(10.5f);

                if (errorLine >= 1 && errorLine <= Box.Lines.Length)
                {
                    int s = Box.GetFirstCharIndexFromLine(errorLine - 1);
                    if (s >= 0)
                    {
                        int len = Box.Lines[errorLine - 1].Length;
                        Box.Select(s, len);
                        Box.SelectionBackColor = Ui.P.Dark
                            ? Color.FromArgb(0x7f, 0x1d, 0x1d)
                            : Color.FromArgb(0xFF, 0xE1, 0xE8);
                    }
                }

                TagRegex(text, ReKeyword, Ui.P.SynKeyword, false);
                TagRegex(text, ReBuiltin, Ui.P.SynType, false);
                TagRegex(text, RePreproc, Ui.P.SynPreproc, false);
                TagRegex(text, ReNumber, Ui.P.SynNumber, false);
                TagRegex(text, ReString, Ui.P.SynString, false);
                TagRegex(text, ReLineComment, Ui.P.SynComment, true);
                TagRegex(text, ReBlockComment, Ui.P.SynComment, true);
            }
            catch (Exception) { }

            Box.SelectionStart = selStart;
            Box.SelectionLength = selLen;
            Box.SelectionColor = Ui.P.EditorFg;

            SendMessage(Box.Handle, EM_SETSCROLLPOS, IntPtr.Zero, ref scroll);
            SendMessage(Box.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            SendMessage(Box.Handle, EM_SETEVENTMASK, IntPtr.Zero, mask);
            Box.Invalidate();
            highlighting = false;
        }

        void TagRegex(string text, Regex re, Color colour, bool italic)
        {
            MatchCollection ms = re.Matches(text);
            for (int i = 0; i < ms.Count; i++)
            {
                Match m = ms[i];
                Box.Select(m.Index, m.Length);
                Box.SelectionColor = colour;
                if (italic) Box.SelectionFont = Ui.Mono(10.5f, FontStyle.Italic);
            }
        }
    }

    /// A Panel that does not flicker.
    class DblPanel : Panel
    {
        public DblPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);
        }
    }
}
