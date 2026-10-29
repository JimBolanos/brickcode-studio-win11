// ---------------------------------------------------------------------------
// Settings panel, Remote window and About window.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace BrickCode
{
    /// A card that lets the owner paint captions on top of it.
    class PaintCard : CardPanel
    {
        public Action<Graphics, PaintCard> Painter;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Painter != null)
            {
                Dr.Quality(e.Graphics);
                Painter(e.Graphics, this);
            }
        }
    }

    // -----------------------------------------------------------------------
    // Right-hand settings panel
    // -----------------------------------------------------------------------
    class SettingsPanel : DblPanel
    {
        public readonly PaintCard CardTop = new PaintCard();
        public readonly PaintCard CardUtils = new PaintCard();
        public readonly PaintCard CardCmd = new PaintCard();

        public readonly DropButton Target = new DropButton();
        public readonly DropButton Slot = new DropButton();
        public readonly DropButton ThemeDd = new DropButton();

        public readonly FlatButton BtnRun = new FlatButton();
        public readonly FlatButton BtnClear = new FlatButton();
        public readonly FlatButton BtnDatalog = new FlatButton();
        public readonly FlatButton BtnNear = new FlatButton();
        public readonly FlatButton BtnFar = new FlatButton();

        public readonly EntryBox Firmware = new EntryBox();
        public readonly FlatButton BtnBrowse = new FlatButton();
        public readonly FlatButton BtnSendFw = new FlatButton();

        public readonly EntryBox Port = new EntryBox();
        public readonly FlatButton BtnDetect = new FlatButton();

        public readonly EntryBox Shutoff = new EntryBox();
        public readonly FlatButton BtnMinus = new FlatButton();
        public readonly FlatButton BtnPlus = new FlatButton();
        public readonly FlatButton BtnApply = new FlatButton();

        public readonly FlatButton BtnTestTools = new FlatButton();
        public readonly FlatButton BtnTestBrick = new FlatButton();
        public readonly FlatButton BtnRemote = new FlatButton();
        public readonly FlatButton BtnFirmwareDlg = new FlatButton();

        public readonly EntryBox Cmd = new EntryBox();
        public readonly FlatButton BtnCmdRun = new FlatButton();

        public SettingsPanel()
        {
            Controls.Add(CardTop);
            Controls.Add(CardUtils);
            Controls.Add(CardCmd);

            CardTop.Controls.Add(Target);
            CardTop.Controls.Add(Slot);
            CardTop.Controls.Add(ThemeDd);

            CardUtils.Controls.Add(BtnRun);
            CardUtils.Controls.Add(BtnClear);
            CardUtils.Controls.Add(BtnDatalog);
            CardUtils.Controls.Add(BtnNear);
            CardUtils.Controls.Add(BtnFar);
            CardUtils.Controls.Add(Firmware);
            CardUtils.Controls.Add(BtnBrowse);
            CardUtils.Controls.Add(BtnSendFw);
            CardUtils.Controls.Add(Port);
            CardUtils.Controls.Add(BtnDetect);
            CardUtils.Controls.Add(Shutoff);
            CardUtils.Controls.Add(BtnMinus);
            CardUtils.Controls.Add(BtnPlus);
            CardUtils.Controls.Add(BtnApply);
            CardUtils.Controls.Add(BtnTestTools);
            CardUtils.Controls.Add(BtnTestBrick);
            CardUtils.Controls.Add(BtnRemote);
            CardUtils.Controls.Add(BtnFirmwareDlg);

            CardCmd.Controls.Add(Cmd);
            CardCmd.Controls.Add(BtnCmdRun);

            for (int i = 0; i < App.Targets.Length; i++) Target.Items.Add(App.Targets[i]);
            for (int i = 1; i <= 5; i++) Slot.Items.Add(i.ToString());
            Slot.SelectedIndex = 0;

            Shutoff.Box.TextAlign = HorizontalAlignment.Center;
            Shutoff.Text = "15";

            CardTop.Painter = PaintTop;
            CardUtils.Painter = PaintUtils;
            CardCmd.Painter = PaintCmd;
        }

        public string TargetNote()
        {
            int i = Target.SelectedIndex;
            if (i < 0 || i >= App.TargetNotes.Length) i = 0;
            return I18n.T(App.TargetNotes[i]);
        }

        void PaintTop(Graphics g, PaintCard c)
        {
            Dr.TextLeft(g, I18n.T("TARGET"), Ui.B(8.25f), Ui.P.Muted, new Rectangle(16, 10, c.Width - 24, 16));
            Dr.TextLeft(g, TargetNote(), Ui.F(8.25f), Ui.P.Muted, new Rectangle(16, 66, c.Width - 24, 16));
            Dr.TextLeft(g, I18n.T("PROGRAM SLOT"), Ui.B(8.25f), Ui.P.Muted, new Rectangle(16, 88, c.Width - 24, 16));
            Dr.TextLeft(g, I18n.T("EDITOR THEME"), Ui.B(8.25f), Ui.P.Muted, new Rectangle(16, 150, c.Width - 24, 16));
        }

        void PaintUtils(Graphics g, PaintCard c)
        {
            int cx = 20, cy = 18;
            Point[] tri = new Point[]
            {
                new Point(cx - 4, cy - 2), new Point(cx + 4, cy - 2), new Point(cx, cy + 3)
            };
            using (SolidBrush b = new SolidBrush(Ui.P.Fg)) g.FillPolygon(b, tri);
            Dr.TextLeft(g, I18n.T("Brick utilities (RCX / Scout)"), Ui.B(9.75f), Ui.P.Fg,
                        new Rectangle(30, 8, c.Width - 36, 20));
            Dr.TextLeft(g, I18n.T("Tower port"), Ui.F(8.75f), Ui.P.Muted,
                        new Rectangle(16, PortY, LabelW - 8, 44));
            Dr.TextLeft(g, I18n.T("Auto-shutoff"), Ui.F(8.75f), Ui.P.Muted,
                        new Rectangle(16, ShutY, LabelW - 8, 34));
            Dr.TextLeft(g, I18n.T("min"), Ui.F(8.75f), Ui.P.Muted,
                        new Rectangle(ShutMinX, ShutY, 30, 30));
        }

        void PaintCmd(Graphics g, PaintCard c)
        {
            Dr.TextLeft(g, "nqc", Ui.B(9.75f), Ui.P.Fg, new Rectangle(16, 0, 40, c.Height));
        }

        int PortY, ShutY, ShutMinX, LabelW = 74;

        public void ApplyTheme()
        {
            BackColor = Ui.P.Bg;

            FlatButton[] colored =
            {
                BtnRun, BtnClear, BtnDatalog, BtnNear, BtnFar, BtnSendFw,
                BtnTestTools, BtnTestBrick, BtnRemote, BtnFirmwareDlg
            };
            Color[] fills =
            {
                Ui.P.Blue, Ui.P.Red, Ui.P.Magenta, Ui.P.Green, Ui.P.Green, Ui.P.Yellow,
                Ui.P.Green, Ui.P.Green, Ui.P.Blue, Ui.P.Yellow
            };
            for (int i = 0; i < colored.Length; i++)
            {
                colored[i].Fill = fills[i];
                colored[i].FgColor = Color.White;
                colored[i].Radius = 10;
            }

            FlatButton[] plain = { BtnBrowse, BtnDetect, BtnApply, BtnMinus, BtnPlus, BtnCmdRun };
            for (int i = 0; i < plain.Length; i++)
            {
                plain[i].Fill = Ui.P.Field;
                plain[i].FgColor = Ui.P.Fg;
                plain[i].BorderColor = Ui.P.Border;
                plain[i].Radius = 9;
            }

            Firmware.ApplyTheme();
            Port.ApplyTheme();
            Shutoff.ApplyTheme();
            Cmd.ApplyTheme();

            Firmware.Placeholder = I18n.T("firmware file (.lgo)");
            Port.Placeholder = I18n.T("empty = let nqc decide  ·  usb for the USB tower, or a COM port");
            Cmd.Placeholder = I18n.T("any nqc arguments, e.g. -firmware firm0328.lgo  ·  -sleep 5  ·  up/down for history");

            BtnRun.Text = I18n.T("Run");
            BtnClear.Text = I18n.T("Clear brick");
            BtnDatalog.Text = I18n.T("Datalog");
            BtnNear.Text = I18n.T("IR near");
            BtnFar.Text = I18n.T("IR far");
            BtnBrowse.Text = "…";
            BtnSendFw.Text = I18n.T("Send firmware");
            BtnDetect.Text = I18n.T("Detect");
            BtnMinus.Text = "−";
            BtnPlus.Text = "+";
            BtnApply.Text = I18n.T("Apply");
            BtnTestTools.Text = I18n.T("Test tools");
            BtnTestBrick.Text = I18n.T("Test brick");
            BtnRemote.Text = I18n.T("Remote");
            BtnFirmwareDlg.Text = I18n.T("Firmware…");
            BtnCmdRun.Text = I18n.T("Run");

            ThemeDd.Items.Clear();
            ThemeDd.Items.Add(I18n.T("Light"));
            ThemeDd.Items.Add(I18n.T("Dark"));

            Invalidate(true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            Dr.Quality(g);
            using (SolidBrush b = new SolidBrush(Ui.P.Bg))
                g.FillRectangle(b, ClientRectangle);
            Dr.TextLeft(g, I18n.T("Settings"), Ui.B(10.5f), Ui.P.Fg, new Rectangle(14, 6, Width - 20, 24));
        }

        public void Relayout()
        {
            // The Spanish labels are longer than the English ones, so the
            // label column is measured rather than fixed.
            using (Graphics g = CreateGraphics())
            {
                int a = Dr.MeasureWidth(g, I18n.T("Tower port"), Ui.F(8.75f));
                int b = Dr.MeasureWidth(g, I18n.T("Auto-shutoff"), Ui.F(8.75f));
                LabelW = Math.Max(74, Math.Max(a, b) + 12);
            }

            int w = Width;
            int pad = 2;
            int cardW = w - pad * 2;
            int y = 32;

            CardTop.SetBounds(pad, y, cardW, 212);
            int iw = cardW - 32;
            Target.SetBounds(16, 30, iw, 34);
            Slot.SetBounds(16, 106, iw, 34);
            ThemeDd.SetBounds(16, 168, iw, 34);

            y += 212 + 10;

            CardUtils.SetBounds(pad, y, cardW, 360);
            int gap = 8;
            int rowH = 40;
            int col3 = (iw - gap * 2) / 3;
            int ry = 30;
            BtnRun.SetBounds(16, ry, col3, rowH);
            BtnClear.SetBounds(16 + col3 + gap, ry, col3, rowH);
            BtnDatalog.SetBounds(16 + (col3 + gap) * 2, ry, col3, rowH);
            ry += rowH + gap;
            BtnNear.SetBounds(16, ry, col3, rowH);
            BtnFar.SetBounds(16 + col3 + gap, ry, col3, rowH);
            ry += rowH + gap + 4;

            int browseW = 42, sendW = 122;
            Firmware.SetBounds(16, ry, iw - browseW - sendW - gap * 2, rowH);
            BtnBrowse.SetBounds(16 + Firmware.Width + gap, ry, browseW, rowH);
            BtnSendFw.SetBounds(16 + Firmware.Width + gap + browseW + gap, ry, sendW, rowH);
            ry += rowH + gap + 2;

            PortY = ry;
            int detectW = 66;
            Port.SetBounds(16 + LabelW, ry, iw - LabelW - detectW - gap, rowH);
            BtnDetect.SetBounds(16 + iw - detectW, ry, detectW, rowH);
            ry += rowH + gap;

            ShutY = ry;
            int sx = 16 + LabelW + 6;
            Shutoff.SetBounds(sx, ry, 44, 32);
            BtnMinus.SetBounds(sx + 50, ry, 32, 32);
            BtnPlus.SetBounds(sx + 86, ry, 32, 32);
            ShutMinX = sx + 128;
            BtnApply.SetBounds(16 + iw - 72, ry, 72, 32);
            ry += 32 + gap + 2;

            // Four across when the labels fit (English), two by two otherwise
            // (Spanish), so nothing is ever clipped.
            int col4 = (iw - gap * 3) / 4;
            bool fourFit;
            using (Graphics g = CreateGraphics())
            {
                int widest = 0;
                string[] labels =
                {
                    BtnTestTools.Text, BtnTestBrick.Text, BtnRemote.Text, BtnFirmwareDlg.Text
                };
                for (int i = 0; i < labels.Length; i++)
                {
                    int lw = Dr.MeasureWidth(g, labels[i], Ui.B(9.75f));
                    if (lw > widest) widest = lw;
                }
                fourFit = widest + 16 <= col4;
            }

            if (fourFit)
            {
                BtnTestTools.SetBounds(16, ry, col4, rowH);
                BtnTestBrick.SetBounds(16 + col4 + gap, ry, col4, rowH);
                BtnRemote.SetBounds(16 + (col4 + gap) * 2, ry, col4, rowH);
                BtnFirmwareDlg.SetBounds(16 + (col4 + gap) * 3, ry, col4, rowH);
                ry += rowH + 12;
            }
            else
            {
                int col2 = (iw - gap) / 2;
                BtnTestTools.SetBounds(16, ry, col2, 38);
                BtnTestBrick.SetBounds(16 + col2 + gap, ry, col2, 38);
                ry += 38 + gap;
                BtnRemote.SetBounds(16, ry, col2, 38);
                BtnFirmwareDlg.SetBounds(16 + col2 + gap, ry, col2, 38);
                ry += 38 + 10;
            }

            CardUtils.Height = ry;
            y += CardUtils.Height + 10;

            // Pinned to the bottom when there is room, flowing underneath the
            // utilities otherwise; AutoScroll makes it reachable either way.
            int cmdH = 64;
            int pinned = Height - 6 - cmdH;
            if (pinned > y) y = pinned;
            CardCmd.SetBounds(pad, y, cardW, cmdH);
            int runW = 64;
            Cmd.SetBounds(52, 12, cardW - 52 - runW - 12 - 16, 40);
            BtnCmdRun.SetBounds(cardW - runW - 16, 12, runW, 40);
        }
    }

    // -----------------------------------------------------------------------
    // Remote: the buttons of the LEGO IR remote, sent with nqc -raw1.
    // -----------------------------------------------------------------------
    class RemoteForm : Form
    {
        readonly MainForm owner;
        bool toggle;

        public RemoteForm(MainForm main)
        {
            owner = main;
            Text = I18n.T("Remote");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Ui.P.Bg;
            ForeColor = Ui.P.Fg;
            Font = Ui.F(9.75f);
            ClientSize = new Size(320, 430);
            Icon = BrickIcon.WindowIcon();

            int y = 14;
            PictureBox logo = new PictureBox();
            logo.Image = BrickIcon.Bitmap(64);
            logo.SizeMode = PictureBoxSizeMode.AutoSize;
            logo.Location = new Point((ClientSize.Width - 64) / 2, y);
            Controls.Add(logo);
            y += 76;

            y = Caption(I18n.T("Message"), y);
            int[] msgMask = { 0x0100, 0x0200, 0x0400 };
            for (int i = 0; i < 3; i++)
                Add(new Rectangle(90 + i * 50, y, 44, 36), (i + 1).ToString(), Ui.P.Blue, msgMask[i], false);
            y += 48;

            string[] names = { "A", "B", "C" };
            int[] fwd = { 0x0800, 0x1000, 0x2000 };
            int[] rev = { 0x4000, 0x8000, 0x0001 };
            for (int i = 0; i < 3; i++)
            {
                int x = 76 + i * 60;
                Add(new Rectangle(x, y, 48, 32), "▲", Ui.P.Green, fwd[i], true);
                Label nl = new Label();
                nl.Text = names[i];
                nl.Font = Ui.B(9.75f);
                nl.ForeColor = Ui.P.Fg;
                nl.TextAlign = ContentAlignment.MiddleCenter;
                nl.SetBounds(x, y + 34, 48, 18);
                Controls.Add(nl);
                Add(new Rectangle(x, y + 54, 48, 32), "▼", Ui.P.Green, rev[i], true);
            }
            y += 100;

            y = Caption(I18n.T("Program"), y);
            for (int i = 0; i < 5; i++)
                Add(new Rectangle(34 + i * 52, y, 46, 36), "P" + (i + 1), Ui.P.Magenta, 0x0002 << i, false);
            y += 50;

            Add(new Rectangle(70, y, 84, 36), I18n.T("Stop"), Ui.P.Red, 0x0040, false);
            Add(new Rectangle(166, y, 84, 36), I18n.T("Sound"), Ui.P.Yellow, 0x0080, false);
        }

        int Caption(string text, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = Ui.B(8.25f);
            l.ForeColor = Ui.P.Muted;
            l.SetBounds(18, y, 200, 16);
            Controls.Add(l);
            return y + 20;
        }

        void Add(Rectangle r, string text, Color fill, int mask, bool hold)
        {
            FlatButton b = new FlatButton();
            b.Text = text;
            b.Fill = fill;
            b.FgColor = Color.White;
            b.Radius = 9;
            b.Bounds = r;
            if (hold)
            {
                b.MouseDown += delegate { Send(mask); };
                b.MouseUp += delegate { Send(0); };
            }
            else
            {
                b.Click += delegate { Send(mask); };
            }
            Controls.Add(b);
        }

        void Send(int mask)
        {
            string hex = mask.ToString("X4");
            string msg = (toggle ? "DA" : "D2") + hex.Substring(2, 2) + hex.Substring(0, 2);
            toggle = !toggle;
            owner.RunRemote(msg);
        }
    }

    // -----------------------------------------------------------------------
    // About
    // -----------------------------------------------------------------------
    class AboutForm : Form
    {
        public AboutForm(Form parent)
        {
            Text = I18n.T("About");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Ui.P.Bg;
            ClientSize = new Size(407, 654);
            Icon = BrickIcon.WindowIcon();

            CardPanel card = new CardPanel();
            card.Radius = 14;
            card.FillColor = Ui.P.Card;
            card.SetBounds(14, 16, ClientSize.Width - 28, ClientSize.Height - 32);
            Controls.Add(card);

            int w = card.Width;
            int y = 18;

            PictureBox icon = new PictureBox();
            icon.Image = BrickIcon.Bitmap(88);
            icon.SizeMode = PictureBoxSizeMode.AutoSize;
            icon.BackColor = Color.Transparent;
            icon.Location = new Point((w - 88) / 2, y);
            card.Controls.Add(icon);
            y += 88 + 12;

            y = Center(card, App.Name, Ui.B(15f), Ui.P.Fg, y, 26);
            y = Center(card, I18n.T("Version {0}", App.Version), Ui.F(8.25f), Ui.P.Muted, y + 2, 16);
            y = Center(card, I18n.T("NQC editor  ·  Windows"), Ui.B(10f), Ui.P.Accent, y + 10, 20);
            y = Center(card,
                I18n.T("Write NQC for the RCX and Scout, press F5, and the program\ncompiles, downloads and runs on the brick."),
                Ui.F(8.75f), Ui.P.Muted, y + 8, 34);

            PaintCard info = new PaintCard();
            info.Radius = 10;
            info.FillColor = Ui.P.Field;
            info.DrawBorder = false;
            info.SetBounds(16, y + 12, w - 32, 6 * 19 + 20);
            card.Controls.Add(info);

            string[,] rows =
            {
                { I18n.T("Compiler"),     I18n.T("nqc (RCX / Scout)") },
                { I18n.T("NQC targets"),  "RCX2 · RCX · CM · Scout · Spy" },
                { I18n.T("NQC link"),     I18n.T("IR tower (USB / serial)") },
                { I18n.T("Highlighting"), I18n.T("Custom (regex)") },
                { I18n.T("Theme"),        I18n.T("Light · Dark") },
                { "Windows",              "Windows 10 / 11 · .NET Framework" }
            };
            info.Painter = delegate(Graphics g, PaintCard c)
            {
                for (int i = 0; i < rows.GetLength(0); i++)
                {
                    int ry = 10 + i * 19;
                    Dr.TextLeft(g, rows[i, 0], Ui.F(8.25f), Ui.P.Muted,
                                new Rectangle(14, ry, 120, 18));
                    Dr.TextRight(g, rows[i, 1], Ui.B(8.25f), Ui.P.Fg,
                                 new Rectangle(120, ry, c.Width - 134, 18));
                }
            };

            y = info.Bottom + 14;
            y = Center(card, I18n.T("MIT License"), Ui.F(8f), Ui.P.Muted, y, 16);
            y = Link(card, App.GithubShort, App.Github, y + 10);
            y = Link(card, App.RebrickableShort, App.Rebrickable, y + 12);
            y = Center(card, "© 2026 Jim Bolaños", Ui.F(8f), Ui.P.Muted, y + 12, 16);
            y = Center(card,
                I18n.T("Independent project - not affiliated with or endorsed\nby the LEGO Group. LEGO and MINDSTORMS are trademarks\nof the LEGO Group."),
                Ui.F(8f), Ui.P.Muted, y + 10, 48);

            FlatButton close = new FlatButton();
            close.Text = I18n.T("Close");
            close.Fill = Ui.P.Field;
            close.FgColor = Ui.P.Fg;
            close.BorderColor = Ui.P.Border;
            close.Radius = 9;
            close.SetBounds((w - 96) / 2, y + 12, 96, 32);
            close.Click += delegate { Close(); };
            card.Controls.Add(close);

            ClientSize = new Size(ClientSize.Width, close.Bottom + 16 + 32);
            card.Height = ClientSize.Height - 32;
        }

        int Center(Control parent, string text, Font f, Color c, int y, int h)
        {
            Label l = new Label();
            l.AutoSize = false;
            l.Text = text;
            l.Font = f;
            l.ForeColor = c;
            l.BackColor = Color.Transparent;
            l.TextAlign = ContentAlignment.MiddleCenter;
            l.SetBounds(10, y, parent.Width - 20, h);
            parent.Controls.Add(l);
            return y + h;
        }

        int Link(Control parent, string text, string url, int y)
        {
            // A plain Label rather than a LinkLabel: LinkLabel lays its text
            // out on its own and ignores the centring here.
            Label l = new Label();
            l.AutoSize = false;
            l.Text = text;
            l.Font = Ui.F(9.5f, FontStyle.Bold | FontStyle.Underline);
            l.ForeColor = Ui.P.Accent;
            l.BackColor = Color.Transparent;
            l.TextAlign = ContentAlignment.MiddleCenter;
            l.Cursor = Cursors.Hand;
            l.SetBounds(10, y, parent.Width - 20, 20);
            l.Click += delegate
            {
                try { Process.Start(url); }
                catch (Exception) { }
            };
            parent.Controls.Add(l);
            return y + 20;
        }
    }
}
