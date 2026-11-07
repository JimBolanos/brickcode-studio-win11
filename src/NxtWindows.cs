// ---------------------------------------------------------------------------
// The NXT Explorer, the NXT Screen and the VLL window.
//
// None of them touches USB itself: each one starts nxt_tool.py (or rcx_tool.py)
// and reads what it prints, the same way the main window does. The Screen keeps
// one nxt_tool.py running and talks to it through stdin and stdout.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace BrickCode
{
    /// Runs one nxt_tool.py command off the UI thread and hands back its output.
    static class ToolRun
    {
        public static bool Start(MainForm owner, string[] args, Action<List<string>, int> done)
        {
            ProcessStartInfo psi = owner.ToolPsi(true, false, args);
            if (psi == null) return false;
            List<string> lines = new List<string>();
            Process p = new Process();
            p.StartInfo = psi;
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
            {
                if (e.Data != null) lock (lines) lines.Add(e.Data);
            };
            p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e)
            {
                if (e.Data != null) lock (lines) lines.Add(e.Data);
            };
            p.Exited += delegate
            {
                p.WaitForExit();          // lets the last lines arrive
                int code = p.ExitCode;
                p.Dispose();
                List<string> copy;
                lock (lines) copy = new List<string>(lines);
                owner.BeginInvoke(new MethodInvoker(delegate { done(copy, code); }));
            };
            try
            {
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                owner.LogError(ex.Message);
                return false;
            }
            return true;
        }
    }

    // -----------------------------------------------------------------------
    // NXT Explorer: name, firmware, battery and the files on the brick.
    // -----------------------------------------------------------------------
    class NxtExplorerForm : Form
    {
        readonly MainForm owner;
        readonly ListView list = new ListView();
        readonly Label head = new Label();
        readonly Label note = new Label();
        readonly FlatButton bRefresh = new FlatButton();
        readonly FlatButton bRun = new FlatButton();
        readonly FlatButton bSend = new FlatButton();
        readonly FlatButton bDelete = new FlatButton();
        bool busy;

        public NxtExplorerForm(MainForm main)
        {
            owner = main;
            Text = I18n.T("NXT Explorer");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Ui.P.Bg;
            ForeColor = Ui.P.Fg;
            Font = Ui.F(9.75f);
            ClientSize = new Size(460, 470);
            Icon = BrickIcon.WindowIcon();

            head.Font = Ui.B(10f);
            head.ForeColor = Ui.P.Fg;
            head.SetBounds(18, 14, 424, 40);
            head.Text = I18n.T("Not connected");
            Controls.Add(head);

            list.View = View.Details;
            list.HeaderStyle = ColumnHeaderStyle.None;
            list.FullRowSelect = true;
            list.MultiSelect = false;
            list.HideSelection = false;
            list.BorderStyle = BorderStyle.None;
            list.BackColor = Ui.P.Field;
            list.ForeColor = Ui.P.Fg;
            list.Font = Ui.F(10f);
            list.Columns.Add("name", 270);
            list.Columns.Add("size", 130, HorizontalAlignment.Right);
            list.SetBounds(18, 60, 424, 290);
            list.SelectedIndexChanged += delegate { UpdateButtons(); };
            Controls.Add(list);

            note.ForeColor = Ui.P.Muted;
            note.Font = Ui.F(8.75f);
            note.SetBounds(18, 356, 424, 36);
            Controls.Add(note);

            Style(bRefresh, I18n.T("Refresh"), Ui.P.Blue, new Rectangle(18, 400, 100, 40));
            Style(bRun, I18n.T("Run"), Ui.P.Green, new Rectangle(126, 400, 80, 40));
            Style(bSend, I18n.T("Send file…"), Ui.P.Magenta, new Rectangle(214, 400, 120, 40));
            Style(bDelete, I18n.T("Delete"), Ui.P.Red, new Rectangle(342, 400, 100, 40));
            bRefresh.Click += delegate { Reload(); };
            bRun.Click += delegate { RunSelected(); };
            bSend.Click += delegate { SendFile(); };
            bDelete.Click += delegate { DeleteSelected(); };
            UpdateButtons();

            Shown += delegate { Reload(); };
        }

        void Style(FlatButton b, string text, Color fill, Rectangle r)
        {
            b.Text = text;
            b.Fill = fill;
            b.FgColor = Color.White;
            b.Radius = 10;
            b.Bounds = r;
            Controls.Add(b);
        }

        string Selected()
        {
            return list.SelectedItems.Count == 0 ? null : list.SelectedItems[0].Text;
        }

        void UpdateButtons()
        {
            string n = Selected();
            bool rxe = n != null && n.EndsWith(".rxe", StringComparison.OrdinalIgnoreCase);
            bRefresh.Enabled = !busy;
            bSend.Enabled = !busy;
            bRun.Enabled = !busy && rxe;
            bDelete.Enabled = !busy && n != null;
        }

        void SetBusy(bool b, string text)
        {
            busy = b;
            if (text != null) note.Text = text;
            UpdateButtons();
        }

        static string After(string line)
        {
            int i = line.IndexOf(':');
            return i < 0 ? line.Trim() : line.Substring(i + 1).Trim();
        }

        static string Errors(List<string> lines)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                string t = lines[i].Trim();
                if (t.Length == 0) continue;
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(t);
            }
            return sb.ToString();
        }

        void Reload()
        {
            if (busy) return;
            SetBusy(true, I18n.T("Reading the NXT…"));
            bool ok = ToolRun.Start(owner, new string[] { "info" }, delegate(List<string> lines, int code)
            {
                if (IsDisposed) return;
                if (code != 0)
                {
                    head.Text = I18n.T("Not connected");
                    list.Items.Clear();
                    SetBusy(false, Errors(lines));
                    return;
                }
                string name = "", fw = "", bat = "", free = "";
                for (int i = 0; i < lines.Count; i++)
                {
                    string l = lines[i].Trim();
                    if (l.StartsWith("Name")) name = After(l);
                    else if (l.StartsWith("Firmware")) fw = After(l);
                    else if (l.StartsWith("Battery")) bat = After(l);
                    else if (l.StartsWith("Free")) free = After(l);
                }
                head.Text = name + "\n" + fw + "  ·  " + bat + "  ·  " + free;
                LoadFiles();
            });
            if (!ok) SetBusy(false, I18n.T("Could not start python. Press Test tools."));
        }

        void LoadFiles()
        {
            bool ok = ToolRun.Start(owner, new string[] { "files" }, delegate(List<string> lines, int code)
            {
                if (IsDisposed) return;
                list.Items.Clear();
                int count = 0;
                for (int i = 0; i < lines.Count; i++)
                {
                    int tab = lines[i].IndexOf('\t');
                    if (tab <= 0) continue;
                    long size;
                    if (!long.TryParse(lines[i].Substring(tab + 1).Trim(), out size)) continue;
                    ListViewItem it = new ListViewItem(lines[i].Substring(0, tab));
                    it.SubItems.Add(size.ToString("N0") + " " + I18n.T("bytes"));
                    list.Items.Add(it);
                    count++;
                }
                SetBusy(false, code != 0 ? Errors(lines) : count + " " + I18n.T("file(s)."));
            });
            if (!ok) SetBusy(false, null);
        }

        void DeleteSelected()
        {
            string n = Selected();
            if (n == null || busy) return;
            if (MessageBox.Show(this, string.Format(I18n.T("Delete {0} from the NXT?"), n),
                    I18n.T("Delete"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            SetBusy(true, null);
            bool ok = ToolRun.Start(owner, new string[] { "delete", n }, delegate(List<string> lines, int code)
            {
                busy = false;
                if (IsDisposed) return;
                if (code != 0) note.Text = Errors(lines);
                Reload();
            });
            if (!ok) SetBusy(false, null);
        }

        void RunSelected()
        {
            string n = Selected();
            if (n == null || busy) return;
            SetBusy(true, null);
            bool ok = ToolRun.Start(owner, new string[] { "run", n }, delegate(List<string> lines, int code)
            {
                if (IsDisposed) return;
                SetBusy(false, code == 0 ? I18n.T("Running on the brick.") : Errors(lines));
            });
            if (!ok) SetBusy(false, null);
        }

        void SendFile()
        {
            if (busy) return;
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = I18n.T("NXT programs") + " (*.rxe)|*.rxe";
                if (d.ShowDialog(this) != DialogResult.OK) return;
                string path = d.FileName;
                SetBusy(true, I18n.T("Sending…"));
                bool ok = ToolRun.Start(owner, new string[] { "upload", path }, delegate(List<string> lines, int code)
                {
                    busy = false;
                    if (IsDisposed) return;
                    if (code != 0) note.Text = Errors(lines);
                    Reload();
                });
                if (!ok) SetBusy(false, null);
            }
        }
    }

    // -----------------------------------------------------------------------
    // NXT Screen: the brick's display, live, with its four buttons.
    // -----------------------------------------------------------------------
    class NxtScreenForm : Form
    {
        const int BrickW = 380, BrickH = 440, LcdX = 40, LcdY = 46, Zoom = 3;
        static readonly Rectangle[] KeyBoxes =
        {
            new Rectangle(44, 284, 60, 56), new Rectangle(154, 278, 72, 66),
            new Rectangle(276, 284, 60, 56), new Rectangle(160, 358, 60, 30)
        };

        readonly MainForm owner;
        readonly FlatButton bConnect = new FlatButton();
        readonly Label status = new Label();
        readonly byte[] pixels = new byte[800];
        Process proc;
        int held;
        bool closing;

        public NxtScreenForm(MainForm main)
        {
            owner = main;
            Text = I18n.T("NXT Screen");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Ui.P.Bg;
            Font = Ui.F(9.75f);
            KeyPreview = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            ClientSize = new Size(BrickW + 40, BrickH + 90);
            Icon = BrickIcon.WindowIcon();

            bConnect.Text = I18n.T("Connect");
            bConnect.Fill = Ui.P.Blue;
            bConnect.FgColor = Color.White;
            bConnect.Radius = 10;
            bConnect.Bounds = new Rectangle(20, BrickH + 30, 110, 36);
            bConnect.Click += delegate { Connect(); };
            Controls.Add(bConnect);

            status.ForeColor = Ui.P.Muted;
            status.Font = Ui.F(8.75f);
            status.SetBounds(140, BrickH + 24, BrickW - 120, 48);
            status.TextAlign = ContentAlignment.MiddleLeft;
            status.Text = I18n.T("Not connected");
            Controls.Add(status);

            Shown += delegate { Connect(); };
            FormClosing += delegate { Disconnect(); };
        }

        void Say(string text)
        {
            if (!IsDisposed) status.Text = text;
        }

        void Connect()
        {
            Disconnect();
            closing = false;
            ProcessStartInfo psi = owner.ToolPsi(true, true, new string[] { "screen" });
            if (psi == null) { Say(I18n.T("Could not start python. Press Test tools.")); return; }
            Process p = new Process();
            p.StartInfo = psi;
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
            {
                if (e.Data != null) OnLine(e.Data);
            };
            p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e)
            {
                if (e.Data != null && e.Data.Trim().Length > 0) Post(e.Data.Trim());
            };
            p.Exited += delegate
            {
                if (!closing && !IsDisposed)
                    try { BeginInvoke(new MethodInvoker(delegate { if (proc == p) Say(I18n.T("Disconnected. Press Connect.")); })); }
                    catch (Exception) { }
            };
            try
            {
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                proc = p;
                Say(I18n.T("Connecting…"));
            }
            catch (Exception ex)
            {
                Say(ex.Message);
            }
        }

        void Post(string text)
        {
            if (IsDisposed || closing) return;
            try { BeginInvoke(new MethodInvoker(delegate { Say(text); })); }
            catch (Exception) { }
        }

        void OnLine(string line)
        {
            if (line.StartsWith("FRAME ") && line.Length >= 6 + 1600)
            {
                byte[] b = new byte[800];
                for (int i = 0; i < 800; i++)
                    b[i] = Convert.ToByte(line.Substring(6 + i * 2, 2), 16);
                if (IsDisposed || closing) return;
                try
                {
                    BeginInvoke(new MethodInvoker(delegate
                    {
                        Array.Copy(b, pixels, 800);
                        Say(I18n.T("Connected"));
                        Invalidate();
                    }));
                }
                catch (Exception) { }
            }
            else if (line.StartsWith("ERROR "))
                Post(line.Substring(6));
            else if (line.StartsWith("[ERROR]") || line.StartsWith("  [USB]"))
                Post(line.Trim());
        }

        void Disconnect()
        {
            closing = true;
            Process p = proc;
            proc = null;
            if (p == null) return;
            try
            {
                p.StandardInput.WriteLine("quit");
                p.StandardInput.Flush();
                if (!p.WaitForExit(1500)) p.Kill();
            }
            catch (Exception)
            {
                try { if (!p.HasExited) p.Kill(); } catch (Exception) { }
            }
            held = 0;
        }

        void SendCmd(string text)
        {
            Process p = proc;
            if (p == null) return;
            try { p.StandardInput.WriteLine(text); p.StandardInput.Flush(); }
            catch (Exception) { }
        }

        void Press(int btn)
        {
            if (held != 0) return;
            held = btn;
            Invalidate();
            SendCmd("press " + btn);
        }

        void Release()
        {
            if (held == 0) return;
            int b = held;
            held = 0;
            Invalidate();
            SendCmd("release " + b);
        }

        Point Logical(Point p)
        {
            return new Point(p.X - 20, p.Y - 10);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Point p = Logical(e.Location);
            for (int i = 0; i < 4; i++)
                if (KeyBoxes[i].Contains(p)) { Press(i + 1); return; }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            Release();
        }

        static int KeyToButton(Keys k)
        {
            if (k == Keys.Left) return 1;
            if (k == Keys.Return) return 2;
            if (k == Keys.Right) return 3;
            if (k == Keys.Escape) return 4;
            return 0;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            int b = KeyToButton(e.KeyCode);
            if (b != 0) { Press(b); e.Handled = true; e.SuppressKeyPress = true; return; }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (KeyToButton(e.KeyCode) != 0) { Release(); e.Handled = true; return; }
            base.OnKeyUp(e);
        }

        static GraphicsPath Rounded(float x, float y, float w, float h, float r)
        {
            GraphicsPath gp = new GraphicsPath();
            gp.AddArc(x, y, 2 * r, 2 * r, 180, 90);
            gp.AddArc(x + w - 2 * r, y, 2 * r, 2 * r, 270, 90);
            gp.AddArc(x + w - 2 * r, y + h - 2 * r, 2 * r, 2 * r, 0, 90);
            gp.AddArc(x, y + h - 2 * r, 2 * r, 2 * r, 90, 90);
            gp.CloseFigure();
            return gp;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TranslateTransform(20, 10);

            // body
            using (GraphicsPath body = Rounded(1, 1, BrickW - 2, BrickH - 2, 24))
            {
                using (LinearGradientBrush lb = new LinearGradientBrush(new Rectangle(0, 0, BrickW, BrickH),
                    Color.FromArgb(245, 245, 245), Color.FromArgb(220, 220, 220), 90f))
                    g.FillPath(lb, body);
                using (Pen pen = new Pen(Color.FromArgb(178, 178, 178), 1.5f))
                    g.DrawPath(pen, body);
            }
            using (GraphicsPath strip = Rounded(2, 2, BrickW - 4, 30, 22))
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(204, 204, 204)))
            {
                g.FillPath(sb, strip);
                g.FillRectangle(sb, 2, 17, BrickW - 4, 15);
            }
            using (Font f = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(107, 107, 107)))
            {
                string[] ports = { "A", "B", "C" };
                for (int i = 0; i < 3; i++) g.DrawString(ports[i], f, tb, 70 + i * 60, 8);
                g.DrawString("USB", f, tb, 300, 8);
            }

            // LCD
            using (GraphicsPath bezel = Rounded(LcdX - 8, LcdY - 8, 100 * Zoom + 16, 64 * Zoom + 16, 8))
            using (SolidBrush bb = new SolidBrush(Color.FromArgb(38, 38, 38)))
                g.FillPath(bb, bezel);
            using (SolidBrush lcd = new SolidBrush(Color.FromArgb(199, 214, 184)))
                g.FillRectangle(lcd, LcdX, LcdY, 100 * Zoom, 64 * Zoom);
            g.SmoothingMode = SmoothingMode.None;
            using (SolidBrush ink = new SolidBrush(Color.FromArgb(13, 20, 13)))
            {
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 100; x++)
                        if (((pixels[(y / 8) * 100 + x] >> (y % 8)) & 1) != 0)
                            g.FillRectangle(ink, LcdX + x * Zoom, LcdY + y * Zoom, Zoom, Zoom);
            }
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // keys
            for (int i = 0; i < 4; i++)
            {
                Rectangle k = KeyBoxes[i];
                double dim = held == i + 1 ? 0.8 : 1.0;
                if (i == 1)
                {
                    using (GraphicsPath gp = Rounded(k.X, k.Y, k.Width, k.Height, 12))
                    using (SolidBrush ob = new SolidBrush(Color.FromArgb((int)(245 * dim), (int)(138 * dim), (int)(31 * dim))))
                        g.FillPath(ob, gp);
                }
                else if (i == 3)
                {
                    using (GraphicsPath gp = Rounded(k.X, k.Y, k.Width, k.Height, 8))
                    using (SolidBrush xb = new SolidBrush(Color.FromArgb((int)(173 * dim), (int)(173 * dim), (int)(173 * dim))))
                        g.FillPath(xb, gp);
                }
                else
                {
                    PointF[] tri = i == 0
                        ? new PointF[] { new PointF(k.X + 3, k.Y + k.Height / 2f), new PointF(k.Right - 3, k.Y + 3), new PointF(k.Right - 3, k.Bottom - 3) }
                        : new PointF[] { new PointF(k.Right - 3, k.Y + k.Height / 2f), new PointF(k.X + 3, k.Y + 3), new PointF(k.X + 3, k.Bottom - 3) };
                    Color c = Color.FromArgb((int)(196 * dim), (int)(196 * dim), (int)(196 * dim));
                    using (SolidBrush ab = new SolidBrush(c))
                    using (Pen ap = new Pen(c, 6f))
                    {
                        ap.LineJoin = LineJoin.Round;
                        g.FillPolygon(ab, tri);
                        g.DrawPolygon(ap, tri);
                    }
                }
            }
        }
    }

    // -----------------------------------------------------------------------
    // VLL: the Scout sends a code to a Micro Scout.
    // -----------------------------------------------------------------------
    class VllForm : Form
    {
        readonly MainForm owner;

        public VllForm(MainForm main)
        {
            owner = main;
            Text = I18n.T("VLL (Micro Scout)");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Ui.P.Bg;
            ForeColor = Ui.P.Fg;
            Font = Ui.F(9.75f);
            ClientSize = new Size(330, 230);
            Icon = BrickIcon.WindowIcon();

            Label hint = new Label();
            hint.Text = I18n.T("Point the Scout's light at the Micro Scout.");
            hint.ForeColor = Ui.P.Muted;
            hint.SetBounds(16, 14, 298, 20);
            Controls.Add(hint);

            Add(new Rectangle(94, 48, 64, 38), "▲", Ui.P.Green, 0);
            Add(new Rectangle(172, 48, 64, 38), "▼", Ui.P.Green, 1);

            Label beep = new Label();
            beep.Text = I18n.T("Beep");
            beep.Font = Ui.B(8.25f);
            beep.ForeColor = Ui.P.Muted;
            beep.SetBounds(16, 104, 200, 16);
            Controls.Add(beep);
            for (int i = 0; i < 5; i++)
                Add(new Rectangle(28 + i * 56, 128, 48, 38), (i + 1).ToString(), Ui.P.Yellow, 4 + i);
        }

        void Add(Rectangle r, string text, Color fill, int code)
        {
            FlatButton b = new FlatButton();
            b.Text = text;
            b.Fill = fill;
            b.FgColor = Color.White;
            b.Radius = 9;
            b.Bounds = r;
            b.Click += delegate { owner.RunVll(code); };
            Controls.Add(b);
        }
    }
}
