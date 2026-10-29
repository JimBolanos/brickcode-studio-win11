// ---------------------------------------------------------------------------
// Window chrome: toolbar, file chip, left sidebar, status bar.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BrickCode
{
    // -----------------------------------------------------------------------
    // The "NQC | untitled | : | v" chip at the top left
    // -----------------------------------------------------------------------
    class FileChip : Control
    {
        public string FileName = "untitled";
        public bool Dirty;
        public event EventHandler MenuRequested;

        bool hover;

        public FileChip()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (MenuRequested != null) MenuRequested(this, EventArgs.Empty);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(Parent != null ? Parent.BackColor : Ui.P.Card))
                e.Graphics.FillRectangle(b, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Dr.Quality(g);
            Rectangle r = new Rectangle(0, 0, Width, Height);
            Color fill = Ui.P.Field;
            if (hover) fill = Palette.Mix(fill, Ui.P.Dark ? Color.White : Color.Black, 0.07);
            Dr.Fill(g, r, 11, fill);
            Dr.Border(g, r, 11, Ui.P.Border);

            // NQC badge
            Rectangle badge = new Rectangle(9, (Height - 22) / 2, 36, 22);
            Dr.Fill(g, badge, 6, Ui.P.Blue);
            Dr.TextCenter(g, "NQC", Ui.B(8f), Color.White, badge);

            // file name
            string name = FileName + (Dirty ? " •" : "");
            int nameRight = Width - 52;
            Dr.TextLeft(g, name, Ui.B(10.5f), Ui.P.Fg,
                        new Rectangle(badge.Right + 10, 0, nameRight - badge.Right - 10, Height));

            // kebab
            int kx = Width - 42;
            for (int i = 0; i < 3; i++)
                Dr.Dot(g, kx, Height / 2 - 6 + i * 6, 3, Ui.P.Muted);

            // caret
            DropButton.DrawCaret(g, new Rectangle(Width - 28, 0, 18, Height), Ui.P.Muted);
        }
    }

    // -----------------------------------------------------------------------
    // Toolbar. Buttons are real controls; the icon, the mode pill and the
    // brick status pill are painted.
    // -----------------------------------------------------------------------
    class ToolBar : DblPanel
    {
        public string ModeText = "NQC  ·  RCX / Scout";
        public string StatusText = "No brick connected";
        public int StatusLevel = 0;       // 0 none, 1 tower, 2 brick

        public Rectangle ModeRect, StatusRect;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            Dr.Quality(g);

            using (SolidBrush b = new SolidBrush(Ui.P.Darker))
                g.FillRectangle(b, ClientRectangle);
            using (Pen p = new Pen(Ui.P.Border))
                g.DrawLine(p, 0, Height - 1, Width, Height - 1);

            // app icon
            GraphicsState state = g.Save();
            g.TranslateTransform(16, (Height - 30) / 2);
            BrickIcon.Draw(g, 30);
            g.Restore(state);

            // mode pill
            if (ModeRect.Width > 0)
            {
                Dr.Fill(g, ModeRect, 10, Ui.P.Field);
                Rectangle inner = new Rectangle(ModeRect.X + 3, ModeRect.Y + 3,
                                                ModeRect.Width - 6, ModeRect.Height - 6);
                Dr.Fill(g, inner, 8, Ui.P.Dark ? Palette.Mix(Ui.P.Card, Color.White, 0.08) : Color.White);
                Dr.Border(g, inner, 8, Ui.P.Border);
                Dr.TextCenter(g, ModeText, Ui.B(9.75f), Ui.P.Fg, inner);
            }

            // brick status pill
            if (StatusRect.Width > 0)
            {
                Dr.Fill(g, StatusRect, 16, Ui.P.Field);
                Color dot = Ui.P.Muted;
                if (StatusLevel == 1) dot = Ui.P.DotBusy;
                else if (StatusLevel == 2) dot = Ui.P.DotOk;
                Dr.Dot(g, StatusRect.X + 17, StatusRect.Y + StatusRect.Height / 2, 9, dot);
                Dr.TextLeft(g, StatusText, Ui.B(9.25f), Ui.P.Fg,
                    new Rectangle(StatusRect.X + 28, StatusRect.Y,
                                  StatusRect.Width - 36, StatusRect.Height));
            }
        }
    }

    // -----------------------------------------------------------------------
    // Left sidebar
    // -----------------------------------------------------------------------
    class SideItem
    {
        public string Id;
        public string Label;
        public Color Swatch;      // Empty means a status dot instead
        public bool IsDot;
        public int DotLevel;      // for IsDot items
        public Rectangle Bounds;
    }

    class SideBar : DblPanel
    {
        public List<SideItem> Bricks = new List<SideItem>();
        public List<SideItem> Tools = new List<SideItem>();
        public SideItem Settings;
        public bool SettingsActive;
        public event EventHandler<SideEventArgs> ItemClicked;

        string hoverId;

        public SideBar()
        {
            Cursor = Cursors.Hand;
        }

        public string BricksTitle = "Bricks";
        public string ToolsTitle = "Tools";

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            string id = HitTest(e.Location);
            if (id != hoverId) { hoverId = id; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hoverId != null) { hoverId = null; Invalidate(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            string id = HitTest(e.Location);
            if (id != null && ItemClicked != null)
                ItemClicked(this, new SideEventArgs(id));
        }

        string HitTest(Point p)
        {
            for (int i = 0; i < Tools.Count; i++)
                if (Tools[i].Bounds.Contains(p)) return Tools[i].Id;
            for (int i = 0; i < Bricks.Count; i++)
                if (Bricks[i].Bounds.Contains(p)) return Bricks[i].Id;
            if (Settings != null && Settings.Bounds.Contains(p)) return Settings.Id;
            return null;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            Dr.Quality(g);
            using (SolidBrush b = new SolidBrush(Ui.P.Field))
                g.FillRectangle(b, ClientRectangle);

            int y = 30;
            Dr.TextLeft(g, BricksTitle, Ui.B(9.25f), Ui.P.Fg, new Rectangle(18, y, Width - 24, 18));
            y += 20;
            for (int i = 0; i < Bricks.Count; i++)
            {
                Bricks[i].Bounds = new Rectangle(8, y, Width - 16, 27);
                DrawItem(g, Bricks[i]);
                y += 27;
            }

            y += 26;
            Dr.TextLeft(g, ToolsTitle, Ui.B(9.25f), Ui.P.Fg, new Rectangle(18, y, Width - 24, 18));
            y += 20;
            for (int i = 0; i < Tools.Count; i++)
            {
                Tools[i].Bounds = new Rectangle(8, y, Width - 16, 29);
                DrawItem(g, Tools[i]);
                y += 29;
            }

            if (Settings != null)
            {
                Settings.Bounds = new Rectangle(8, Height - 42, Width - 16, 30);
                if (SettingsActive)
                {
                    Dr.Fill(g, Settings.Bounds, 9, Ui.P.Card);
                    Dr.Border(g, Settings.Bounds, 9, Ui.P.Border);
                }
                DrawItem(g, Settings);
            }
        }

        void DrawItem(Graphics g, SideItem it)
        {
            Rectangle r = it.Bounds;
            if (hoverId == it.Id && !(it == Settings && SettingsActive))
                Dr.Fill(g, r, 9, Palette.Mix(Ui.P.Field, Ui.P.Fg, 0.07));

            if (it.IsDot)
            {
                Color c = Ui.P.Muted;
                if (it.DotLevel == 1) c = Ui.P.DotBusy;
                else if (it.DotLevel == 2) c = Ui.P.DotOk;
                Dr.Dot(g, r.X + 16, r.Y + r.Height / 2, 10, c);
            }
            else
            {
                Rectangle sw = new Rectangle(r.X + 11, r.Y + (r.Height - 15) / 2, 15, 15);
                Dr.Fill(g, sw, 4, it.Swatch);
            }
            Dr.TextLeft(g, it.Label, Ui.F(10f), Ui.P.Fg,
                        new Rectangle(r.X + 32, r.Y, r.Width - 38, r.Height));
        }
    }

    class SideEventArgs : EventArgs
    {
        public readonly string Id;
        public SideEventArgs(string id) { Id = id; }
    }

    // -----------------------------------------------------------------------
    // Status bar
    // -----------------------------------------------------------------------
    class StatusBarPanel : DblPanel
    {
        public string LeftText = "Ready.";
        public string RightText = "";
        public int Level = 0;      // 0 ok, 1 busy, 2 error

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            Dr.Quality(g);
            using (SolidBrush b = new SolidBrush(Ui.P.Bg))
                g.FillRectangle(b, ClientRectangle);

            Color dot = Ui.P.DotOk;
            if (Level == 1) dot = Ui.P.DotBusy;
            else if (Level == 2) dot = Ui.P.DotError;
            Dr.Dot(g, 24, Height / 2, 10, dot);
            Dr.TextLeft(g, LeftText, Ui.F(9.25f), Ui.P.Muted,
                        new Rectangle(38, 0, Width / 2, Height));
            Dr.TextRight(g, RightText, Ui.F(9.25f), Ui.P.Muted,
                         new Rectangle(Width / 2, 0, Width / 2 - 18, Height));
        }
    }

    // -----------------------------------------------------------------------
    // A labelled console panel with its Clear button
    // -----------------------------------------------------------------------
    class ConsolePanel : CardPanel
    {
        public readonly LogView Log = new LogView();
        public readonly FlatButton ClearBtn = new FlatButton();
        public string Title = "Console";

        public ConsolePanel()
        {
            Controls.Add(Log);
            Controls.Add(ClearBtn);
            ClearBtn.Radius = 8;
            ClearBtn.FontSize = 9f;
        }

        public void ApplyTheme()
        {
            Log.ApplyTheme();
            ClearBtn.Fill = Ui.P.Field;
            ClearBtn.FgColor = Ui.P.Fg;
            ClearBtn.BorderColor = Ui.P.Border;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ClearBtn.SetBounds(Width - 92, 9, 76, 28);
            Log.SetBounds(12, 48, Math.Max(10, Width - 24), Math.Max(10, Height - 60));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            Dr.Quality(g);

            // header strip: same colour as the page, rounded at the top only
            Rectangle head = new Rectangle(1, 1, Width - 2, 44);
            using (GraphicsPath p = Dr.Round(head, 12))
            using (SolidBrush b = new SolidBrush(Ui.P.Bg))
                g.FillPath(b, p);
            using (SolidBrush b = new SolidBrush(Ui.P.Bg))
                g.FillRectangle(b, 1, 20, Width - 2, 25);
            using (Pen p = new Pen(Ui.P.Border))
                g.DrawLine(p, 1, 45, Width - 2, 45);

            Dr.TextLeft(g, Title, Ui.B(10.5f), Ui.P.Fg, new Rectangle(14, 6, Width - 120, 34));
        }
    }
}
