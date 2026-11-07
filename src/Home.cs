// ---------------------------------------------------------------------------
// The HOME screen (one card per brick), the bottom navigation and the ABOUT
// screen -- the same three things the Apple / Linux edition opens with.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BrickCode
{
    // -----------------------------------------------------------------------
    // Bottom navigation: HOME  CODE  BLOCKS  ABOUT
    // -----------------------------------------------------------------------
    class NavBar : Control
    {
        public static readonly string[] Ids = { "home", "code", "blocks", "about" };
        public string[] Labels = { "HOME", "CODE", "BLOCKS", "ABOUT" };
        public string Active = "home";
        public event EventHandler<SideEventArgs> Picked;

        readonly Rectangle[] boxes = new Rectangle[4];
        int hover = -1;

        public NavBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        int HitTest(Point p)
        {
            for (int i = 0; i < 4; i++) if (boxes[i].Contains(p)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int h = HitTest(e.Location);
            if (h != hover) { hover = h; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hover != -1) { hover = -1; Invalidate(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int h = HitTest(e.Location);
            if (h >= 0 && Picked != null) Picked(this, new SideEventArgs(Ids[h]));
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Dr.Quality(g);
            using (SolidBrush b = new SolidBrush(Ui.P.Bg))
                g.FillRectangle(b, ClientRectangle);

            Font f = Ui.B(9.75f);
            int[] w = new int[4];
            int total = 0, gap = 14;
            for (int i = 0; i < 4; i++)
            {
                w[i] = Dr.MeasureWidth(g, Labels[i], f) + 44;
                total += w[i];
            }
            total += gap * 3;
            int x = (Width - total) / 2;
            int h = 44, y = (Height - h) / 2;
            for (int i = 0; i < 4; i++)
            {
                boxes[i] = new Rectangle(x, y, w[i], h);
                bool on = Ids[i] == Active;
                if (on)
                {
                    using (GraphicsPath p = Dr.Round(new Rectangle(x + 1, y + 1, w[i] - 2, h - 2), h / 2))
                    using (Pen pen = new Pen(Color.FromArgb(193, 0, 133), 2f))
                        g.DrawPath(pen, p);
                }
                Color c = on ? Ui.P.Fg : (hover == i ? Ui.P.Fg : Ui.P.Muted);
                Dr.TextCenter(g, Labels[i], f, c, boxes[i]);
                x += w[i] + gap;
            }
        }
    }

    // -----------------------------------------------------------------------
    // HOME: pick the brick you are programming
    // -----------------------------------------------------------------------
    class HomePage : Control
    {
        class Card
        {
            public string Id, Lang, Name, Foot;
            public Action<Cr, int, int> Art;
            public Color[] Cols;
            public double[] Pos;
            public float Angle;        // GDI+ angle: CSS degrees minus 90
            public bool Glow;          // the EV3's dark corner
            public Rectangle Box;
        }

        public event EventHandler<SideEventArgs> Picked;
        public string Title = "BrickCode Studio";
        public string Sub = "Pick the brick you are programming";

        readonly List<Card> cards = new List<Card>();
        int hover = -1;

        static Color C(string hex)
        {
            return Color.FromArgb(Convert.ToInt32(hex.Substring(1, 2), 16),
                                  Convert.ToInt32(hex.Substring(3, 2), 16),
                                  Convert.ToInt32(hex.Substring(5, 2), 16));
        }

        /// picker = the three cards of the BLOCKS window: which block editor to open.
        public HomePage() : this(false) { }

        public HomePage(bool picker)
        {
            if (picker)
            {
                Title = "Block editor";
                Sub = "Pick the brick you are building for";
            }
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;

            cards.Add(new Card
            {
                Id = "rcx", Lang = "NQC", Name = "RCX", Foot = "USB tower  ·  3 sensors, 3 motors",
                Art = delegate(Cr cr, int w, int h) { Bk.DrawRcx(cr, w, h); },
                Cols = new Color[] { C("#8147D3"), C("#7D3AA6"), C("#82257A"), C("#5A1A47"), C("#3F172E") },
                Pos = new double[] { 0, 0.20, 0.50, 0.80, 1 }, Angle = 165 - 90
            });
            cards.Add(new Card
            {
                Id = "scout", Lang = "NQC", Name = "SCOUT", Foot = "Built-in light sensor  ·  stand-alone",
                Art = delegate(Cr cr, int w, int h) { Bk.DrawScout(cr, w, h); },
                Cols = new Color[] { C("#2F6874"), C("#5A2E5A"), C("#85206B"), C("#7A1753"), C("#511539") },
                Pos = new double[] { 0, 0.14, 0.40, 0.70, 1 }, Angle = 200 - 90
            });
            cards.Add(new Card
            {
                Id = "nxt", Lang = "NXC", Name = "NXT", Foot = "USB or Bluetooth  ·  encoders",
                Art = delegate(Cr cr, int w, int h) { Bk.DrawNxt(cr, w, h); },
                Cols = new Color[] { C("#0F9D93"), C("#257685"), C("#4A3A5C"), C("#5A1A40"), C("#321528") },
                Pos = new double[] { 0, 0.22, 0.45, 0.68, 1 }, Angle = 175 - 90
            });
            cards.Add(new Card
            {
                Id = "ev3", Lang = "PYBRICKS", Name = "EV3", Foot = "Blocks only  ·  paste the Python into Pybricks",
                Art = delegate(Cr cr, int w, int h) { Bk.DrawEv3(cr, w, h); },
                Cols = new Color[] { C("#1A8C90"), C("#2C4F78"), C("#46397A"), C("#5A34A0"), C("#7243BE") },
                Pos = new double[] { 0, 0.20, 0.42, 0.68, 1 }, Angle = 135 - 90, Glow = true
            });

            if (picker)
            {
                // RCX and Scout share one editor (NQC), so they share one card.
                cards.RemoveAt(1);
                cards[0].Name = "RCX · SCOUT";
                cards[0].Foot = "Writes NQC  ·  works offline";
                cards[1].Foot = "Writes NXC  ·  works offline";
                cards[2].Foot = "Writes Python  ·  paste it into Pybricks";
            }
        }

        string T(string s) { return I18n.T(s); }

        int HitTest(Point p)
        {
            for (int i = 0; i < cards.Count; i++) if (cards[i].Box.Contains(p)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int h = HitTest(e.Location);
            if (h != hover) { hover = h; Cursor = h >= 0 ? Cursors.Hand : Cursors.Default; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hover != -1) { hover = -1; Invalidate(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int h = HitTest(e.Location);
            if (h >= 0 && Picked != null) Picked(this, new SideEventArgs(cards[h].Id));
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Dr.Quality(g);
            using (SolidBrush b = new SolidBrush(Ui.P.Bg))
                g.FillRectangle(b, ClientRectangle);

            // masthead: the logo, then the wordmark
            Font word = new Font("Segoe UI", 17f, FontStyle.Bold);
            string title = Title;
            int tw = Dr.MeasureWidth(g, title, word) + title.Length * 3;
            int total = 38 + 14 + tw;
            int x0 = (Width - total) / 2;
            using (Bitmap logo = BrickIcon.Bitmap(38))
                g.DrawImage(logo, x0, 18, 38, 38);
            using (Font spaced = word)
                DrawSpaced(g, title, spaced, Ui.P.Fg, x0 + 52, 20, 3f);
            Dr.TextCenter(g, T(Sub), Ui.F(9.5f), Ui.P.Muted, new Rectangle(0, 58, Width, 22));

            // the cards: four bricks, or three block editors
            int margin = 20, gap = 18, top = 96, bottom = Height - 18;
            int cw = (Width - margin * 2 - gap * (cards.Count - 1)) / cards.Count;
            int ch = Math.Max(200, bottom - top);
            for (int i = 0; i < cards.Count; i++)
            {
                Card c = cards[i];
                c.Box = new Rectangle(margin + i * (cw + gap), top, cw, ch);
                DrawCard(g, c, i == hover);
            }
        }

        static void DrawSpaced(Graphics g, string s, Font f, Color col, int x, int y, float spacing)
        {
            using (SolidBrush b = new SolidBrush(col))
            {
                float cx = x;
                for (int i = 0; i < s.Length; i++)
                {
                    string ch = s.Substring(i, 1);
                    g.DrawString(ch, f, b, cx, y, StringFormat.GenericTypographic);
                    cx += g.MeasureString(ch, f, 1000, StringFormat.GenericTypographic).Width + spacing;
                    if (ch == " ") cx += 2;
                }
            }
        }

        void DrawCard(Graphics g, Card c, bool hot)
        {
            Rectangle r = c.Box;
            using (GraphicsPath path = Dr.Round(r, 22))
            {
                using (LinearGradientBrush lb = new LinearGradientBrush(r, Color.Black, Color.White, c.Angle, false))
                {
                    ColorBlend cb = new ColorBlend(c.Cols.Length);
                    for (int i = 0; i < c.Cols.Length; i++)
                    {
                        cb.Positions[i] = (float)c.Pos[i];
                        cb.Colors[i] = c.Cols[i];
                    }
                    lb.InterpolationColors = cb;
                    g.FillPath(lb, path);
                }

                if (c.Glow)
                {
                    // a dark corner at the bottom left, fading out at 60 %
                    float rad = (float)(Math.Sqrt(r.Width * r.Width + r.Height * r.Height) * 0.6);
                    using (GraphicsPath ring = new GraphicsPath())
                    {
                        ring.AddEllipse(r.Left - rad, r.Bottom - rad, rad * 2, rad * 2);
                        using (PathGradientBrush pb = new PathGradientBrush(ring))
                        {
                            pb.CenterPoint = new PointF(r.Left, r.Bottom);
                            ColorBlend cb = new ColorBlend(2);
                            cb.Positions[0] = 0f; cb.Colors[0] = Color.FromArgb(0, 40, 18, 48);
                            cb.Positions[1] = 1f; cb.Colors[1] = Color.FromArgb(230, 40, 18, 48);
                            pb.InterpolationColors = cb;
                            GraphicsState st = g.Save();
                            g.SetClip(path, CombineMode.Intersect);
                            g.FillPath(pb, path);
                            g.Restore(st);
                        }
                    }
                }

                // text
                int pad = 22;
                Dr.TextLeft(g, T(c.Lang), Ui.B(8.5f), Color.FromArgb(204, 255, 255, 255),
                            new Rectangle(r.X + pad, r.Y + 18, r.Width - pad * 2, 18));
                using (Font nf = new Font("Segoe UI", 21f, FontStyle.Bold))
                    DrawSpaced(g, T(c.Name), nf, Color.White, r.X + pad, r.Y + 44, 2f);
                Rectangle foot = new Rectangle(r.X + pad, r.Bottom - 58, r.Width - pad * 2, 44);
                using (StringFormat sf = new StringFormat())
                using (SolidBrush fb = new SolidBrush(Color.FromArgb(224, 255, 255, 255)))
                {
                    sf.LineAlignment = StringAlignment.Far;
                    g.DrawString(T(c.Foot), Ui.B(8.75f), fb, foot, sf);
                }

                // the brick
                Rectangle art = new Rectangle(r.X + 8, r.Y + 96, r.Width - 16, r.Height - 96 - 62);
                if (art.Width > 20 && art.Height > 20)
                {
                    GraphicsState st = g.Save();
                    g.SetClip(art);
                    Cr cr = new Cr(g, art.X, art.Y);
                    cr.Save();
                    c.Art(cr, art.Width, art.Height);
                    cr.Restore();
                    g.Restore(st);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                }

                if (hot)
                    using (Pen pen = new Pen(Color.FromArgb(56, 255, 255, 255), 2.5f))
                        g.DrawPath(pen, path);
            }
        }
    }

    // -----------------------------------------------------------------------
    // ABOUT
    // -----------------------------------------------------------------------
    class AboutPage : Control
    {
        class Row { public string Title, Desc, Url, Action; public Color Dot; public Rectangle Box; }

        public event EventHandler<SideEventArgs> Action;
        readonly List<Row> docs = new List<Row>();
        readonly List<Row> links = new List<Row>();
        readonly List<Row> chips = new List<Row>();
        int hover = -1;
        List<Row> all = new List<Row>();

        const string Guide = "https://github.com/JimBolanos/brickcode-studio-apple-linux/blob/main/docs/USER_GUIDE.en.md";
        const string GuideEs = "https://github.com/JimBolanos/brickcode-studio-apple-linux/blob/main/docs/USER_GUIDE.es.md";
        const string WinRepo = "https://github.com/JimBolanos/brickcode-studio-win11";

        static Color C(string hex)
        {
            return Color.FromArgb(Convert.ToInt32(hex.Substring(1, 2), 16),
                                  Convert.ToInt32(hex.Substring(3, 2), 16),
                                  Convert.ToInt32(hex.Substring(5, 2), 16));
        }

        public AboutPage()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Default;
            BuildRows();
        }

        string GuideUrl(string anchor)
        {
            return (I18n.Spanish ? GuideEs : Guide) + "#" + anchor;
        }

        void BuildRows()
        {
            docs.Clear(); links.Clear(); chips.Clear(); all.Clear();
            docs.Add(new Row { Dot = C("#1E88FF"), Title = "Getting started",
                Desc = "Write a program, press F5, watch it run on the brick.",
                Url = GuideUrl("1-what-is-brickcode-studio") });
            docs.Add(new Row { Dot = C("#FF5CC8"), Title = "Install",
                Desc = "Unzip, install the tower driver, run BrickCodeStudio.exe.", Url = WinRepo });
            docs.Add(new Row { Dot = C("#F5C000"), Title = "Firmware",
                Desc = "Sending firm0328.lgo to an RCX, and when you need to.", Url = GuideUrl("firmware") });
            docs.Add(new Row { Dot = C("#1CC8F0"), Title = "Tools, one by one",
                Desc = "Every utility in the side bar, and what it does to the brick.",
                Url = GuideUrl("7-tools-one-by-one") });
            docs.Add(new Row { Dot = C("#1DB954"), Title = "Troubleshooting",
                Desc = "The tower, the firmware, and what to try when nothing answers.",
                Url = GuideUrl("9-troubleshooting") });
            docs.Add(new Row { Dot = C("#FF4D4D"), Title = "Block editor",
                Desc = "Drag blocks instead of typing. Programs keep their blocks.", Action = "blocks" });

            links.Add(new Row { Title = "Repository", Desc = "brickcode-studio-win11", Url = WinRepo });
            links.Add(new Row { Title = "Issues & ideas", Desc = "brickcode-studio-win11/issues", Url = WinRepo + "/issues" });
            links.Add(new Row { Title = "NQC reference", Desc = "bricxcc.sourceforge.net/nqc", Url = "https://bricxcc.sourceforge.net/nqc" });
            links.Add(new Row { Title = "NXC guide", Desc = "bricxcc.sourceforge.net/nbc", Url = "https://bricxcc.sourceforge.net/nbc" });

            chips.Add(new Row { Title = App.GithubShort, Url = App.Github });
            chips.Add(new Row { Title = App.RebrickableShort, Url = App.Rebrickable });

            all.AddRange(docs); all.AddRange(links); all.AddRange(chips);
        }

        public void Refresh2() { BuildRows(); Invalidate(); }

        string T(string s) { return I18n.T(s); }

        int HitTest(Point p)
        {
            for (int i = 0; i < all.Count; i++) if (all[i].Box.Contains(p)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int h = HitTest(e.Location);
            if (h != hover) { hover = h; Cursor = h >= 0 ? Cursors.Hand : Cursors.Default; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hover != -1) { hover = -1; Invalidate(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int h = HitTest(e.Location);
            if (h < 0) return;
            Row r = all[h];
            if (r.Action != null)
            {
                if (Action != null) Action(this, new SideEventArgs(r.Action));
                return;
            }
            try { Process.Start(r.Url); }
            catch (Exception) { }
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        void Card(Graphics g, Rectangle r)
        {
            Dr.Fill(g, r, 14, Ui.P.Card);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Dr.Quality(g);
            using (SolidBrush b = new SolidBrush(Ui.P.Bg))
                g.FillRectangle(b, ClientRectangle);

            using (Font word = new Font("Segoe UI", 17f, FontStyle.Bold))
            {
                int tw = Dr.MeasureWidth(g, "BrickCode Studio", word) + 16 * 3;
                DrawSpaced(g, "BrickCode Studio", word, Ui.P.Fg, (Width - tw) / 2, 14, 3f);
            }

            int margin = 22, gap = 16, top = 64;
            int total = Width - margin * 2 - gap * 2;
            int wl = (int)(total * 0.29), wr = (int)(total * 0.26), wm = total - wl - wr;
            int xl = margin, xm = xl + wl + gap, xr = xm + wm + gap;
            Color accent = Color.FromArgb(193, 0, 133);

            // --- left: version, what it uses ---
            Rectangle c1 = new Rectangle(xl, top, wl, 130);
            Card(g, c1);
            Dr.TextLeft(g, T("VERSION") + " " + App.Version, Ui.B(8.5f), accent, new Rectangle(c1.X + 20, c1.Y + 18, c1.Width - 40, 18));
            Dr.TextLeft(g, T("NQC + NXC editor"), Ui.B(13f), Ui.P.Fg, new Rectangle(c1.X + 20, c1.Y + 40, c1.Width - 40, 28));
            DrawWrapped(g, T("Write NQC for the RCX and Scout, or NXC for the NXT, press F5, and the program compiles, downloads and runs on the brick."),
                        Ui.F(8.75f), Ui.P.Muted, new Rectangle(c1.X + 20, c1.Y + 72, c1.Width - 40, 52));

            string[,] uses =
            {
                { T("Compilers"), "nqc (RCX / Scout)  ·  nbc (NXT)" },
                { T("NQC targets"), "RCX2 · RCX · CM · Scout · Spy" },
                { T("NQC link"), T("IR tower (USB / serial)") },
                { T("NXC link"), T("USB (libusb)") },
                { T("Highlighting"), T("Custom (regex)") },
                { T("Platforms"), "Windows 10 / 11 · WinForms" }
            };
            Rectangle c2 = new Rectangle(xl, c1.Bottom + gap, wl, 44 + uses.GetLength(0) * 26);
            Card(g, c2);
            Dr.TextLeft(g, T("What it uses"), Ui.B(8.25f), Ui.P.Muted, new Rectangle(c2.X + 20, c2.Y + 14, c2.Width - 40, 18));
            for (int i = 0; i < uses.GetLength(0); i++)
            {
                int ry = c2.Y + 40 + i * 26;
                Dr.TextLeft(g, uses[i, 0], Ui.F(8.75f), Ui.P.Muted, new Rectangle(c2.X + 20, ry, 110, 22));
                Dr.TextRight(g, uses[i, 1], Ui.B(8.5f), Ui.P.Fg, new Rectangle(c2.X + 110, ry, c2.Width - 130, 22));
            }

            // --- middle: documentation, author ---
            int rowH = 56;
            Rectangle c3 = new Rectangle(xm, top, wm, 44 + docs.Count * (rowH + 8));
            Card(g, c3);
            Dr.TextLeft(g, T("Documentation"), Ui.B(8.25f), Ui.P.Muted, new Rectangle(c3.X + 20, c3.Y + 14, c3.Width - 40, 18));
            for (int i = 0; i < docs.Count; i++)
            {
                Row r = docs[i];
                r.Box = new Rectangle(c3.X + 14, c3.Y + 40 + i * (rowH + 8), c3.Width - 28, rowH);
                bool hot = hover == i;
                Dr.Fill(g, r.Box, 10, hot ? Ui.P.Hover(Ui.P.Field, 0.08) : Ui.P.Field);
                Dr.Dot(g, r.Box.X + 22, r.Box.Y + rowH / 2, 12, r.Dot);
                Dr.TextLeft(g, T(r.Title), Ui.B(9.5f), Ui.P.Fg, new Rectangle(r.Box.X + 42, r.Box.Y + 8, r.Box.Width - 80, 20));
                Dr.TextLeft(g, T(r.Desc), Ui.F(8.25f), Ui.P.Muted, new Rectangle(r.Box.X + 42, r.Box.Y + 28, r.Box.Width - 80, 20));
                Dr.TextRight(g, "›", Ui.F(13f), Ui.P.Muted, new Rectangle(r.Box.Right - 36, r.Box.Y, 26, rowH));
            }

            Rectangle c4 = new Rectangle(xm, c3.Bottom + gap, wm, 132);
            Card(g, c4);
            using (SolidBrush ab = new SolidBrush(accent))
                g.FillEllipse(ab, c4.X + 22, c4.Y + 24, 62, 62);
            Dr.TextCenter(g, "JB", Ui.B(15f), Color.White, new Rectangle(c4.X + 22, c4.Y + 24, 62, 62));
            Dr.TextLeft(g, "Jim Bolaños", Ui.B(12.5f), Ui.P.Fg, new Rectangle(c4.X + 100, c4.Y + 14, c4.Width - 120, 24));
            DrawWrapped(g, T("Builds and programs LEGO robots as a hobby — RCX, Scout, NXT and Powered Up hubs. Made BrickCode Studio so these classic bricks keep running on modern computers."),
                        Ui.F(8.25f), Ui.P.Muted, new Rectangle(c4.X + 100, c4.Y + 40, c4.Width - 120, 52));
            int cx = c4.X + 100;
            for (int i = 0; i < chips.Count; i++)
            {
                Row r = chips[i];
                int w = Dr.MeasureWidth(g, r.Title, Ui.B(8f)) + 22;
                r.Box = new Rectangle(cx, c4.Bottom - 30, w, 22);
                Dr.Fill(g, r.Box, 8, hover == docs.Count + links.Count + i ? Ui.P.Hover(Ui.P.Field, 0.1) : Ui.P.Field);
                Dr.TextCenter(g, r.Title, Ui.B(8f), Ui.P.Fg, r.Box);
                cx += w + 8;
            }

            // --- right: links, legal ---
            int lrow = 48;
            Rectangle c5 = new Rectangle(xr, top, wr, 44 + links.Count * lrow);
            Card(g, c5);
            Dr.TextLeft(g, T("Links"), Ui.B(8.25f), Ui.P.Muted, new Rectangle(c5.X + 20, c5.Y + 14, c5.Width - 40, 18));
            for (int i = 0; i < links.Count; i++)
            {
                Row r = links[i];
                r.Box = new Rectangle(c5.X + 10, c5.Y + 38 + i * lrow, c5.Width - 20, lrow - 4);
                bool hot = hover == docs.Count + i;
                if (hot) Dr.Fill(g, r.Box, 8, Ui.P.Hover(Ui.P.Card, 0.08));
                Dr.TextLeft(g, T(r.Title), Ui.B(9f), Ui.P.Fg, new Rectangle(r.Box.X + 10, r.Box.Y + 4, r.Box.Width - 40, 18));
                Dr.TextLeft(g, r.Desc, Ui.F(8f), Ui.P.Accent, new Rectangle(r.Box.X + 10, r.Box.Y + 22, r.Box.Width - 40, 18));
                Dr.TextRight(g, "↗", Ui.F(10f), Ui.P.Muted, new Rectangle(r.Box.Right - 30, r.Box.Y, 22, r.Box.Height));
            }

            Rectangle c6 = new Rectangle(xr, c5.Bottom + gap, wr, 168);
            Card(g, c6);
            Dr.TextLeft(g, T("Legal"), Ui.B(8.25f), Ui.P.Muted, new Rectangle(c6.X + 20, c6.Y + 14, c6.Width - 40, 18));
            Dr.TextLeft(g, "MIT License", Ui.B(8.75f), Ui.P.Fg, new Rectangle(c6.X + 20, c6.Y + 38, c6.Width - 40, 16));
            Dr.TextLeft(g, "© 2026 Jim Bolaños", Ui.B(8.75f), Ui.P.Fg, new Rectangle(c6.X + 20, c6.Y + 55, c6.Width - 40, 16));
            DrawWrapped(g, "Blockly 13.4.0, Apache 2.0.\nnqc by Dave Baum · nbc by John Hansen.", Ui.F(8.25f), Ui.P.Muted,
                        new Rectangle(c6.X + 20, c6.Y + 80, c6.Width - 40, 34));
            DrawWrapped(g, T("Not affiliated with or endorsed by the LEGO Group. LEGO, MINDSTORMS, RCX, Scout and NXT are trademarks of the LEGO Group."),
                        Ui.F(7.5f), Ui.P.Muted, new Rectangle(c6.X + 20, c6.Y + 118, c6.Width - 40, 44));
        }

        static void DrawWrapped(Graphics g, string s, Font f, Color c, Rectangle r)
        {
            using (SolidBrush b = new SolidBrush(c))
                g.DrawString(s, f, b, r);
        }

        static void DrawSpaced(Graphics g, string s, Font f, Color col, int x, int y, float spacing)
        {
            using (SolidBrush b = new SolidBrush(col))
            {
                float cx = x;
                for (int i = 0; i < s.Length; i++)
                {
                    string ch = s.Substring(i, 1);
                    g.DrawString(ch, f, b, cx, y, StringFormat.GenericTypographic);
                    cx += g.MeasureString(ch, f, 1000, StringFormat.GenericTypographic).Width + spacing;
                    if (ch == " ") cx += 2;
                }
            }
        }
    }

    // -----------------------------------------------------------------------
    // BLOCKS: choose which block editor to open (RCX / Scout, NXT or EV3).
    // -----------------------------------------------------------------------
    class BlocksPicker : Form
    {
        public string Choice;

        public BlocksPicker()
        {
            Text = I18n.T("Block editor");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Ui.P.Bg;
            ClientSize = new Size(900, 540);
            Icon = BrickIcon.WindowIcon();

            HomePage page = new HomePage(true);
            page.Dock = DockStyle.Fill;
            page.Picked += delegate(object o, SideEventArgs e)
            {
                Choice = e.Id;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(page);
        }
    }
}
