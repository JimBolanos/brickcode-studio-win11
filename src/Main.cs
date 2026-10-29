// ---------------------------------------------------------------------------
// Main window: layout, actions and the nqc runner.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BrickCode
{
    delegate void DragHandler(object sender, int delta);

    class SplitHandle : Control
    {
        public event DragHandler Dragged;
        bool dragging;
        int startY;

        public SplitHandle()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.HSplit;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            dragging = true;
            startY = Cursor.Position.Y;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!dragging) return;
            int now = Cursor.Position.Y;
            int delta = now - startY;
            if (delta == 0) return;
            startY = now;
            if (Dragged != null) Dragged(this, delta);
        }

        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); dragging = false; }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Dr.Quality(g);
            using (SolidBrush b = new SolidBrush(Ui.P.Bg))
                g.FillRectangle(b, ClientRectangle);
            Rectangle bar = new Rectangle(Width / 2 - 70, Height / 2 - 2, 140, 4);
            Dr.Fill(g, bar, 2, Palette.Mix(Ui.P.Bg, Ui.P.Fg, 0.16));
        }
    }

    class MainForm : Form
    {
        // chrome
        readonly ToolBar tb = new ToolBar();
        readonly FileChip chip = new FileChip();
        readonly FlatButton plusBtn = new FlatButton();
        readonly DropButton langDd = new DropButton();
        readonly FlatButton btnCancel = new FlatButton();
        readonly FlatButton btnDownload = new FlatButton();
        readonly FlatButton btnRun = new FlatButton();
        readonly SideBar side = new SideBar();
        readonly StatusBarPanel status = new StatusBarPanel();

        // content
        readonly CardPanel editorCard = new CardPanel();
        readonly CodeEditor editor = new CodeEditor();
        readonly SplitHandle splitter = new SplitHandle();
        readonly ConsolePanel console = new ConsolePanel();
        readonly FlatButton railGear = new FlatButton();
        readonly FlatButton railTerm = new FlatButton();
        readonly SettingsPanel settings = new SettingsPanel();

        // state
        string filePath = null;
        bool dirty;
        bool dark;
        bool consoleVisible = true;
        bool settingsVisible;
        int consoleH = 182;
        readonly List<string> cmdHistory = new List<string>();
        int cmdPos;

        // process
        Process proc;
        readonly Timer watchdog = new Timer();
        bool cancelled;
        List<Step> queue;
        int queueAt;

        static readonly Regex ErrorRe = new Regex(
            @"^(?<file>[^:]+):(?<line>\d+):\s*(?<sev>Error|Warning)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public MainForm()
        {
            SuspendLayout();
            Text = App.Name;
            ClientSize = new Size(1280, 800);
            MinimumSize = new Size(1000, 660);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            Icon = BrickIcon.WindowIcon();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            BuildUi();
            LoadSettings();
            ApplyTheme();

            editor.Code = App.Sample;
            dirty = false;
            UpdateChip();

            ResumeLayout(false);

            Shown += delegate
            {
                Relayout();
                editor.Highlight();
                CheckTools();
            };
        }

        // -------------------------------------------------------------------
        // Build
        // -------------------------------------------------------------------
        void BuildUi()
        {
            Controls.Add(tb);
            tb.Controls.Add(chip);
            tb.Controls.Add(plusBtn);
            tb.Controls.Add(langDd);
            tb.Controls.Add(btnCancel);
            tb.Controls.Add(btnDownload);
            tb.Controls.Add(btnRun);

            plusBtn.Text = "+";
            plusBtn.FontSize = 13f;
            plusBtn.Radius = 9;
            plusBtn.Click += delegate { NewFile(); };

            langDd.Items.Add("English");
            langDd.Items.Add("Español");
            langDd.Bold = true;
            langDd.Radius = 10;
            langDd.SelectionChanged += delegate
            {
                I18n.Spanish = langDd.SelectedIndex == 1;
                ApplyTheme();
                Relayout();
            };

            chip.MenuRequested += delegate { ShowFileMenu(); };

            btnCancel.Click += delegate { CancelRun(); };
            btnDownload.Click += delegate { Compile(false); };
            btnRun.Click += delegate { Compile(true); };

            Controls.Add(side);
            side.Bricks.Add(new SideItem { Id = "tower", Label = "RCX tower", IsDot = true, DotLevel = 0 });
            side.Tools.Add(new SideItem { Id = "remote", Label = "Remote" });
            side.Tools.Add(new SideItem { Id = "datalog", Label = "Datalog" });
            side.Tools.Add(new SideItem { Id = "firmware", Label = "Firmware…" });
            side.Tools.Add(new SideItem { Id = "testbrick", Label = "Test brick" });
            side.Tools.Add(new SideItem { Id = "testtools", Label = "Test tools" });
            side.Settings = new SideItem { Id = "settings", Label = "Settings" };
            side.ItemClicked += delegate(object s, SideEventArgs e) { SideAction(e.Id); };

            Controls.Add(editorCard);
            editorCard.Radius = 12;
            editorCard.Controls.Add(editor);

            Controls.Add(splitter);
            splitter.Dragged += delegate(object s, int d)
            {
                consoleH = Math.Max(60, Math.Min(ClientSize.Height - 300, consoleH - d));
                Relayout();
            };

            Controls.Add(console);
            console.ClearBtn.Click += delegate { console.Log.Clear(); };

            Controls.Add(railGear);
            Controls.Add(railTerm);
            railGear.Glyph = "⚙";
            railGear.GlyphFont = Ui.Sym(13f);
            railGear.Radius = 12;
            railGear.Click += delegate { settingsVisible = !settingsVisible; side.SettingsActive = settingsVisible; side.Invalidate(); Relayout(); };
            railTerm.Glyph = ">_";
            railTerm.GlyphFont = Ui.Mono(11f, FontStyle.Bold);
            railTerm.Radius = 12;
            railTerm.Click += delegate { consoleVisible = !consoleVisible; Relayout(); };

            Controls.Add(settings);
            settings.Visible = false;
            settings.Target.SelectionChanged += delegate { settings.CardTop.Invalidate(); UpdateMode(); };
            settings.ThemeDd.SelectionChanged += delegate
            {
                dark = settings.ThemeDd.SelectedIndex == 1;
                ApplyTheme();
                Relayout();
            };
            settings.BtnRun.Click += delegate { Brick("run", null, 30000, "Running command…"); };
            settings.BtnClear.Click += delegate { Brick("clear", null, 60000, "Running command…"); };
            settings.BtnDatalog.Click += delegate { Brick("datalog", null, 60000, "Running command…"); };
            settings.BtnNear.Click += delegate { Brick("near", null, 20000, "Running command…"); };
            settings.BtnFar.Click += delegate { Brick("far", null, 20000, "Running command…"); };
            settings.BtnBrowse.Click += delegate { PickFirmware(); };
            settings.BtnSendFw.Click += delegate { SendFirmware(); };
            settings.BtnDetect.Click += delegate { DetectPort(); };
            settings.BtnMinus.Click += delegate { BumpShutoff(-1); };
            settings.BtnPlus.Click += delegate { BumpShutoff(1); };
            settings.BtnApply.Click += delegate { ApplyShutoff(); };
            settings.BtnTestTools.Click += delegate { CheckTools(); };
            settings.BtnTestBrick.Click += delegate { TestBrick(); };
            settings.BtnRemote.Click += delegate { OpenRemote(); };
            settings.BtnFirmwareDlg.Click += delegate { PickFirmware(); };
            settings.BtnCmdRun.Click += delegate { RunFreeCommand(); };
            settings.Cmd.Box.KeyDown += CmdKeyDown;

            Controls.Add(status);

            editor.DirtyChanged += delegate
            {
                if (!dirty) { dirty = true; UpdateChip(); }
            };

            watchdog.Tick += delegate
            {
                watchdog.Stop();
                if (proc != null && !proc.HasExited)
                {
                    Log(I18n.T("No answer in time. Stopping the command."), Ui.P.LogErr, true);
                    CancelRun();
                }
            };
        }

        // -------------------------------------------------------------------
        // Theme and text
        // -------------------------------------------------------------------
        void ApplyTheme()
        {
            Ui.P = Palette.Make(dark);
            BackColor = Ui.P.Bg;

            tb.BackColor = Ui.P.Card;
            side.BackColor = Ui.P.Bg;
            status.BackColor = Ui.P.Bg;
            splitter.BackColor = Ui.P.Bg;
            editorCard.FillColor = Ui.P.EditorBg;
            console.FillColor = Ui.P.Card;

            plusBtn.Fill = Ui.P.Field;
            plusBtn.FgColor = Ui.P.Fg;
            plusBtn.BorderColor = Ui.P.Border;

            btnCancel.Text = I18n.T("Cancel");
            btnCancel.Fill = Ui.P.Field;
            btnCancel.FgColor = Ui.P.Fg;
            btnCancel.BorderColor = Ui.P.Border;

            btnDownload.Text = I18n.T("Download");
            btnDownload.Glyph = "⬇";
            btnDownload.GlyphFont = Ui.Sym(9.5f);
            btnDownload.Fill = Ui.P.Accent;
            btnDownload.FgColor = Color.White;

            btnRun.Text = I18n.T("Run  (F5)");
            btnRun.Glyph = "▶";
            btnRun.GlyphFont = Ui.Sym(9f);
            btnRun.Fill = Ui.P.Green;
            btnRun.FgColor = Color.White;

            railGear.Fill = Ui.P.Field;
            railGear.FgColor = Ui.P.Muted;
            railGear.BorderColor = Ui.P.Border;
            railTerm.Fill = Ui.P.Field;
            railTerm.FgColor = Ui.P.Muted;
            railTerm.BorderColor = Ui.P.Border;

            side.BricksTitle = I18n.T("Bricks");
            side.ToolsTitle = I18n.T("Tools");
            side.Bricks[0].Label = I18n.T("RCX tower");
            string[] toolIds = { "remote", "datalog", "firmware", "testbrick", "testtools" };
            string[] toolNames = { "Remote", "Datalog", "Firmware…", "Test brick", "Test tools" };
            Color[] toolColors = { Ui.P.Yellow, Ui.P.Red, Ui.P.Muted, Ui.P.Green, Ui.P.Blue };
            for (int i = 0; i < side.Tools.Count && i < toolIds.Length; i++)
            {
                side.Tools[i].Label = I18n.T(toolNames[i]);
                side.Tools[i].Swatch = toolColors[i];
            }
            side.Settings.Label = I18n.T("Settings");
            side.Settings.Swatch = Ui.P.Muted;

            tb.StatusText = I18n.T(brickKey);
            console.Title = I18n.T("Console");
            console.ClearBtn.Text = I18n.T("Clear");
            console.ApplyTheme();

            status.RightText = I18n.T("NQC for the RCX and Scout  ·  compiled and sent by nqc");
            if (string.IsNullOrEmpty(status.LeftText) || status.Level == 0)
                status.LeftText = I18n.T("Ready.");

            settings.ApplyTheme();
            settings.ThemeDd.Select(dark ? 1 : 0, false);
            editor.ApplyTheme();
            UpdateMode();

            tb.Invalidate(true);
            side.Invalidate();
            status.Invalidate();
            Invalidate(true);
        }

        void UpdateMode()
        {
            tb.ModeText = "NQC  ·  RCX / Scout";
            tb.Invalidate();
            settings.CardTop.Invalidate();
        }

        void UpdateChip()
        {
            chip.FileName = filePath == null
                ? I18n.T("untitled")
                : Path.GetFileName(filePath);
            chip.Dirty = dirty;
            chip.Invalidate();
            Text = chip.FileName + (dirty ? " •" : "") + "  -  " + App.Name;
        }

        void SetStatus(string text, int level)
        {
            status.LeftText = text;
            status.Level = level;
            status.Invalidate();
        }

        string brickKey = "No brick connected";

        void SetBrick(int level, string text)
        {
            brickKey = text;
            tb.StatusLevel = level;
            tb.StatusText = I18n.T(text);
            side.Bricks[0].DotLevel = level;
            side.Invalidate();
            Relayout();
        }

        // -------------------------------------------------------------------
        // Layout
        // -------------------------------------------------------------------
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Relayout();
        }

        void Relayout()
        {
            if (ClientSize.Width < 50 || ClientSize.Height < 50) return;
            int W = ClientSize.Width, H = ClientSize.Height;
            const int tbH = 60, sbW = 210, stH = 30, pad = 10, railW = 64, setW = 412;

            tb.SetBounds(0, 0, W, tbH);
            side.SetBounds(0, tbH, sbW, H - tbH - stH);
            status.SetBounds(0, H - stH, W, stH);

            LayoutToolbar(W, tbH);

            int cy0 = tbH, cy1 = H - stH;
            int sx = W;
            if (settingsVisible)
            {
                sx = W - pad - setW;
                settings.SetBounds(sx, cy0 + 4, setW, cy1 - cy0 - 10);
                settings.Visible = true;
                settings.Relayout();
            }
            else settings.Visible = false;

            int railX = sx - pad - railW;
            railGear.SetBounds(railX, cy0 + 13, railW, 48);
            railTerm.SetBounds(railX, cy0 + 69, railW, 48);

            int ex0 = sbW + pad;
            int ew = Math.Max(120, railX - pad - ex0);
            int top = cy0 + 4;

            int ch = consoleVisible ? Math.Min(consoleH, Math.Max(60, cy1 - top - 160)) : 0;
            int sh = consoleVisible ? 12 : 0;
            int eh = Math.Max(80, cy1 - pad - top - ch - sh);

            editorCard.SetBounds(ex0, top, ew, eh);
            editor.SetBounds(8, 8, Math.Max(20, ew - 16), Math.Max(20, eh - 16));

            splitter.Visible = consoleVisible;
            console.Visible = consoleVisible;
            if (consoleVisible)
            {
                splitter.SetBounds(ex0, editorCard.Bottom, ew, sh);
                console.SetBounds(ex0, splitter.Bottom, ew, ch);
            }
        }

        void LayoutToolbar(int W, int tbH)
        {
            chip.SetBounds(62, 12, 176, 36);
            plusBtn.SetBounds(248, 13, 34, 34);

            int runW = 128, dlW = 132, cancelW = 80, gap = 8;
            int x = W - 12 - runW;
            btnRun.SetBounds(x, 12, runW, 36);
            x -= gap + dlW;
            btnDownload.SetBounds(x, 12, dlW, 36);
            x -= gap + cancelW;
            btnCancel.SetBounds(x, 12, cancelW, 36);

            int modeW = 180, langW = 100;
            int statusW;
            using (Graphics g = CreateGraphics())
                statusW = Dr.MeasureWidth(g, tb.StatusText, Ui.B(9.25f)) + 46;
            int groupW = modeW + 12 + langW + 12 + statusW;
            int leftEdge = plusBtn.Right + 16;
            int rightEdge = btnCancel.Left - 16;
            int gx = leftEdge + ((rightEdge - leftEdge) - groupW) / 2;
            if (gx < leftEdge) gx = leftEdge;

            tb.ModeRect = new Rectangle(gx, 14, modeW, 32);
            langDd.SetBounds(gx + modeW + 12, 13, langW, 34);
            tb.StatusRect = new Rectangle(gx + modeW + 12 + langW + 12, 13, statusW, 34);
            tb.Invalidate();
        }

        // -------------------------------------------------------------------
        // File handling
        // -------------------------------------------------------------------
        void ShowFileMenu()
        {
            ContextMenuStrip m = new ContextMenuStrip();
            m.Renderer = new MenuRenderer();
            m.BackColor = Ui.P.Card;
            m.ForeColor = Ui.P.Fg;
            m.Font = Ui.F(9.75f);
            m.ShowImageMargin = false;
            AddItem(m, I18n.T("New"), delegate { NewFile(); });
            AddItem(m, I18n.T("Open…"), delegate { OpenFile(); });
            AddItem(m, I18n.T("Save"), delegate { Save(false); });
            AddItem(m, I18n.T("Save as…"), delegate { Save(true); });
            m.Items.Add(new ToolStripSeparator());
            AddItem(m, I18n.T("About"), delegate { new AboutForm(this).ShowDialog(this); });
            m.Show(chip, new Point(0, chip.Height + 2));
        }

        static void AddItem(ContextMenuStrip m, string text, EventHandler h)
        {
            ToolStripMenuItem mi = new ToolStripMenuItem(text);
            mi.ForeColor = Ui.P.Fg;
            mi.Click += h;
            m.Items.Add(mi);
        }

        bool ConfirmDiscard()
        {
            if (!dirty) return true;
            string name = filePath == null ? I18n.T("untitled") : Path.GetFileName(filePath);
            DialogResult r = MessageBox.Show(this,
                I18n.T("Save changes to {0}?", name) + "\n\n" +
                I18n.T("If you don't save, your changes are lost."),
                App.Name, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel) return false;
            if (r == DialogResult.Yes) return Save(false);
            return true;
        }

        void NewFile()
        {
            if (!ConfirmDiscard()) return;
            filePath = null;
            editor.Code = App.Sample;
            dirty = false;
            editor.SetErrorLine(-1);
            UpdateChip();
        }

        void OpenFile()
        {
            if (!ConfirmDiscard()) return;
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Title = I18n.T("Open NQC file");
                d.Filter = I18n.T("NQC programs") + " (*.nqc)|*.nqc|" +
                           I18n.T("All files") + " (*.*)|*.*";
                if (d.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    editor.Code = File.ReadAllText(d.FileName);
                    filePath = d.FileName;
                    dirty = false;
                    editor.SetErrorLine(-1);
                    UpdateChip();
                }
                catch (Exception ex)
                {
                    Log(ex.Message, Ui.P.LogErr, true);
                }
            }
        }

        bool Save(bool saveAs)
        {
            if (saveAs || filePath == null)
            {
                using (SaveFileDialog d = new SaveFileDialog())
                {
                    d.Title = I18n.T("Save program");
                    d.Filter = I18n.T("NQC programs") + " (*.nqc)|*.nqc|" +
                               I18n.T("All files") + " (*.*)|*.*";
                    d.DefaultExt = "nqc";
                    d.FileName = filePath == null ? "program.nqc" : Path.GetFileName(filePath);
                    if (d.ShowDialog(this) != DialogResult.OK) return false;
                    filePath = d.FileName;
                }
            }
            try
            {
                File.WriteAllText(filePath, editor.Code, Encoding.UTF8);
                dirty = false;
                UpdateChip();
                return true;
            }
            catch (Exception ex)
            {
                Log(ex.Message, Ui.P.LogErr, true);
                return false;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.F5) { Compile(true); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.S) { Save(e.Shift); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.O) { OpenFile(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.N) { NewFile(); e.Handled = true; }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!ConfirmDiscard()) { e.Cancel = true; return; }
            SaveSettings();
            base.OnFormClosing(e);
        }

        // -------------------------------------------------------------------
        // Side bar actions
        // -------------------------------------------------------------------
        void SideAction(string id)
        {
            switch (id)
            {
                case "settings":
                    settingsVisible = !settingsVisible;
                    side.SettingsActive = settingsVisible;
                    side.Invalidate();
                    Relayout();
                    break;
                case "remote": OpenRemote(); break;
                case "datalog": Brick("datalog", null, 60000, "Running command…"); break;
                case "firmware": PickFirmware(); break;
                case "testbrick": TestBrick(); break;
                case "testtools": CheckTools(); break;
                case "tower": DetectPort(); break;
            }
        }

        void OpenRemote()
        {
            RemoteForm f = new RemoteForm(this);
            f.Show(this);
        }

        // -------------------------------------------------------------------
        // Settings panel helpers
        // -------------------------------------------------------------------
        void BumpShutoff(int delta)
        {
            int v;
            if (!int.TryParse(settings.Shutoff.Text.Trim(), NumberStyles.Integer,
                              CultureInfo.InvariantCulture, out v)) v = 15;
            v += delta;
            if (v < 0) v = 0;
            if (v > 255) v = 255;
            settings.Shutoff.Text = v.ToString(CultureInfo.InvariantCulture);
        }

        void ApplyShutoff()
        {
            int v;
            if (!int.TryParse(settings.Shutoff.Text.Trim(), NumberStyles.Integer,
                              CultureInfo.InvariantCulture, out v)) v = 15;
            Brick("sleep", v.ToString(CultureInfo.InvariantCulture), 20000, "Auto-shutoff set.");
        }

        void PickFirmware()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Title = I18n.T("Select firmware file");
                d.Filter = I18n.T("RCX firmware (*.lgo, *.srec)") + "|*.lgo;*.srec|" +
                           I18n.T("All files") + " (*.*)|*.*";
                if (d.ShowDialog(this) == DialogResult.OK)
                {
                    settings.Firmware.Text = d.FileName;
                    if (!settingsVisible)
                    {
                        settingsVisible = true;
                        side.SettingsActive = true;
                        side.Invalidate();
                        Relayout();
                    }
                }
            }
        }

        void SendFirmware()
        {
            string fw = settings.Firmware.Text.Trim();
            if (fw.Length == 0 || !File.Exists(fw))
            {
                Log(I18n.T("Select a firmware file (.lgo) first."), Ui.P.LogErr, true);
                return;
            }
            Log(I18n.T("Flashing firmware takes a while. Keep the RCX pointed at the tower and do not move it."),
                Ui.P.LogInfo, false);
            Brick("firmware", fw, App.FirmwareTimeoutMs, "Sending firmware…");
        }

        void DetectPort()
        {
            string env = Environment.GetEnvironmentVariable("RCX_PORT");
            if (!string.IsNullOrEmpty(env) &&
                env.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            {
                settings.Port.Text = env;
                Log(I18n.T("Tower port: {0} ({1})", env, I18n.T("from RCX_PORT in this environment")),
                    Ui.P.LogOk, true);
                SetBrick(1, "RCX tower connected");
                return;
            }
            // Anything else means the USB tower: ask rcx_tool whether it is there.
            Log(I18n.T("Looking for the USB tower…"), Ui.P.LogDim, false);
            Brick("tower", null, 20000, "Looking for the USB tower…");
        }

        void TestBrick()
        {
            Brick("ping", null, 25000, "Asking the brick…");
        }

        void CheckTools()
        {
            Log(I18n.T("Checking the tools…"), Ui.P.LogDim, false);
            Found("nqc.exe", FindNqc());
            if (SerialTower())
            {
                Log(I18n.T("Serial tower on {0}: nqc talks to the brick by itself.",
                           settings.Port.Text.Trim()), Ui.P.LogInfo, false);
                return;
            }
            Found("python", FindPython());
            Found("rcx_tool.py", FindRcxTool());
            string dll = Path.Combine(AppDir, "libusb-1.0.dll");
            Found("libusb-1.0.dll", File.Exists(dll) ? dll : null);
            string pyusb = Path.Combine(Path.Combine(AppDir, "usb"), "core.py");
            Found("pyusb", File.Exists(pyusb) ? Path.GetDirectoryName(pyusb) : null);
        }

        void Found(string name, string path)
        {
            if (path == null)
                Log(I18n.T("{0}: NOT found in PATH.", name), Ui.P.LogErr, true);
            else
                Log(I18n.T("{0}: found at {1}", name, path), Ui.P.LogOk, false);
        }

        void RunFreeCommand()
        {
            string text = settings.Cmd.Text.Trim();
            if (text.Length == 0) return;
            cmdHistory.Add(text);
            cmdPos = cmdHistory.Count;
            string[] parts = SplitArgs(text);
            List<Step> steps = new List<Step>();
            steps.Add(NqcStep(parts));
            RunSteps(steps, "Running command…");
        }

        void CmdKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { RunFreeCommand(); e.Handled = true; e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Up && cmdHistory.Count > 0)
            {
                if (cmdPos > 0) cmdPos--;
                settings.Cmd.Text = cmdHistory[cmdPos];
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down && cmdHistory.Count > 0)
            {
                if (cmdPos < cmdHistory.Count - 1) { cmdPos++; settings.Cmd.Text = cmdHistory[cmdPos]; }
                else { cmdPos = cmdHistory.Count; settings.Cmd.Text = ""; }
                e.Handled = true;
            }
        }

        static string[] SplitArgs(string line)
        {
            List<string> outp = new List<string>();
            StringBuilder cur = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"') { quoted = !quoted; continue; }
                if (c == ' ' && !quoted)
                {
                    if (cur.Length > 0) { outp.Add(cur.ToString()); cur.Length = 0; }
                    continue;
                }
                cur.Append(c);
            }
            if (cur.Length > 0) outp.Add(cur.ToString());
            return outp.ToArray();
        }

        // -------------------------------------------------------------------
        // Compile
        //
        // nqc always does the compiling. Who carries the program to the brick
        // depends on the tower:
        //
        //   COM port in the Tower port box -> a serial tower, nqc does it all.
        //   anything else                  -> the USB tower, which on Windows
        //     10/11 has no LEGO driver, so it goes through rcx_tool.py over
        //     libusbK. nqc cannot open \\.\LEGOTOWER1 there.
        // -------------------------------------------------------------------
        bool SerialTower()
        {
            string p = settings.Port.Text.Trim();
            return p.Length > 0 &&
                   p.StartsWith("COM", StringComparison.OrdinalIgnoreCase);
        }

        string SlotText()
        {
            string s = settings.Slot.SelectedItem;
            return s.Length == 0 ? "1" : s;
        }

        void Compile(bool run)
        {
            if (filePath == null)
            {
                Log(I18n.T("Save the program first - the compiler works on a file, not on the editor buffer."),
                    Ui.P.LogInfo, false);
                if (!Save(false)) return;
            }
            else if (dirty && !Save(false)) return;

            editor.SetErrorLine(-1);
            List<Step> steps = new List<Step>();

            if (SerialTower())
            {
                Step s = NqcStep("-pgm", SlotText(), "-d", filePath);
                if (run) s.Args.Add("-run");
                steps.Add(s);
            }
            else
            {
                string rcx = Path.ChangeExtension(filePath, ".rcx");
                steps.Add(NqcStep("-O", rcx, filePath));
                steps.Add(ToolStep(App.DefaultTimeoutMs, "download", rcx, SlotText()));
                if (run) steps.Add(ToolStep(30000, "run", SlotText()));
            }

            RunSteps(steps, run ? "Compiling and running…" : "Compiling and downloading…");
        }

        /// One brick action, routed to whichever tool can reach the tower.
        void Brick(string action, string arg, int timeout, string busyKey)
        {
            List<Step> steps = new List<Step>();
            if (SerialTower())
            {
                switch (action)
                {
                    case "run": steps.Add(NqcStep("-pgm", SlotText(), "-run")); break;
                    case "clear": steps.Add(NqcStep("-pgm", SlotText(), "-clear")); break;
                    case "datalog": steps.Add(NqcStep("-datalog")); break;
                    case "near": steps.Add(NqcStep("-near")); break;
                    case "far": steps.Add(NqcStep("-far")); break;
                    case "ping": steps.Add(NqcStep("-near")); break;
                    case "sleep": steps.Add(NqcStep("-sleep", arg)); break;
                    case "firmware": steps.Add(NqcStep("-firmware", arg)); break;
                    default: return;
                }
            }
            else
            {
                switch (action)
                {
                    case "run": steps.Add(ToolStep(timeout, "run", SlotText())); break;
                    case "clear": steps.Add(ToolStep(timeout, "clear", SlotText())); break;
                    case "datalog": steps.Add(ToolStep(timeout, "datalog")); break;
                    case "near": steps.Add(ToolStep(timeout, "near")); break;
                    case "far": steps.Add(ToolStep(timeout, "far")); break;
                    case "ping": steps.Add(ToolStep(timeout, "ping")); break;
                    case "tower": steps.Add(ToolStep(timeout, "tower")); break;
                    case "sleep": steps.Add(ToolStep(timeout, "sleep", arg)); break;
                    case "firmware": steps.Add(ToolStep(timeout, "firmware", arg)); break;
                    default: return;
                }
            }
            RunSteps(steps, busyKey);
        }

        // -------------------------------------------------------------------
        // Tools
        // -------------------------------------------------------------------
        string AppDir
        {
            get
            {
                try { return Path.GetDirectoryName(Application.ExecutablePath); }
                catch (Exception) { return Environment.CurrentDirectory; }
            }
        }

        static string FindOnPath(string exeName, string[] alsoLookIn)
        {
            for (int i = 0; i < alsoLookIn.Length; i++)
            {
                try
                {
                    string p = Path.Combine(alsoLookIn[i], exeName);
                    if (File.Exists(p)) return p;
                }
                catch (Exception) { }
            }
            string env = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(env)) return null;
            string[] dirs = env.Split(new char[] { ';' });
            for (int i = 0; i < dirs.Length; i++)
            {
                string d = dirs[i].Trim();
                if (d.Length == 0) continue;
                try
                {
                    string p = Path.Combine(d, exeName);
                    if (File.Exists(p)) return p;
                }
                catch (Exception) { }
            }
            return null;
        }

        string FindNqc()
        {
            return FindOnPath("nqc.exe", new string[]
            {
                AppDir,
                Path.Combine(AppDir, "nqc"),
                Path.Combine(AppDir, "bin")
            });
        }

        /// py.exe is preferred: it is the launcher the Store build installs.
        string FindPython()
        {
            string[] here = new string[] { AppDir };
            string[] names = { "py.exe", "python.exe", "python3.exe" };
            for (int i = 0; i < names.Length; i++)
            {
                string p = FindOnPath(names[i], here);
                if (p != null) return p;
            }
            return null;
        }

        string FindRcxTool()
        {
            string p = Path.Combine(AppDir, "rcx_tool.py");
            return File.Exists(p) ? p : null;
        }

        static string Quote(string a)
        {
            if (a == null) return "\"\"";
            if (a.IndexOf(' ') < 0 && a.IndexOf('"') < 0) return a;
            return "\"" + a.Replace("\"", "\\\"") + "\"";
        }

        // -------------------------------------------------------------------
        // Steps
        // -------------------------------------------------------------------
        class Step
        {
            public string Exe;
            public string Shown;            // what to print before the $ line
            public List<string> Args = new List<string>();
            public int Timeout = App.DefaultTimeoutMs;
            public bool Silent;
            public bool SetPort;            // export RCX_PORT for this one
        }

        Step NqcStep(params string[] args)
        {
            Step s = new Step();
            s.Exe = FindNqc();
            s.Shown = "nqc";
            s.SetPort = true;
            s.Args.Add("-T" + settings.Target.SelectedItem);
            string port = settings.Port.Text.Trim();
            if (port.Length > 0) s.Args.Add("-S" + port);
            for (int i = 0; i < args.Length; i++)
                if (args[i] != null) s.Args.Add(args[i]);
            return s;
        }

        Step ToolStep(int timeout, params string[] args)
        {
            Step s = new Step();
            s.Exe = FindPython();
            s.Shown = "python rcx_tool.py";
            s.Timeout = timeout;
            string py = s.Exe == null ? "" : Path.GetFileName(s.Exe);
            if (string.Equals(py, "py.exe", StringComparison.OrdinalIgnoreCase))
                s.Args.Add("-3");
            s.Args.Add(FindRcxTool());
            for (int i = 0; i < args.Length; i++)
                if (args[i] != null) s.Args.Add(args[i]);
            return s;
        }

        public void RunRemote(string raw)
        {
            List<Step> steps = new List<Step>();
            if (SerialTower())
            {
                Step s = NqcStep("-raw1", raw);
                s.Silent = true;
                s.Timeout = 15000;
                steps.Add(s);
            }
            else
            {
                // rcx_tool wants the 16-bit mask; nqc wants D2 + the two bytes.
                string mask = raw.Length >= 6 ? raw.Substring(4, 2) + raw.Substring(2, 2) : raw;
                Step s = ToolStep(15000, "remote", mask);
                s.Silent = true;
                steps.Add(s);
            }
            RunSteps(steps, "Sending remote command…");
        }

        void RunSteps(List<Step> steps, string busyKey)
        {
            if (proc != null && !proc.HasExited)
            {
                Log(I18n.T("A command is already running. Wait for it or press Cancel."),
                    Ui.P.LogInfo, false);
                return;
            }
            if (steps == null || steps.Count == 0) return;

            for (int i = 0; i < steps.Count; i++)
                if (!CheckStep(steps[i])) return;

            queue = steps;
            queueAt = 0;
            SetStatus(I18n.T(busyKey), 1);
            StartStep();
        }

        bool CheckStep(Step s)
        {
            if (s.Exe == null)
            {
                if (s.Shown == "nqc")
                    Log(I18n.T("'{0}' was not found. Put it next to this program or in your PATH - every action in this window runs it.", "nqc.exe"),
                        Ui.P.LogErr, true);
                else
                    Log(I18n.T("Python was not found. Install Python 3 from the Microsoft Store, then start this program again - the USB tower needs it."),
                        Ui.P.LogErr, true);
                SetStatus(I18n.T("Tool not found."), 2);
                return false;
            }
            if (s.Shown != "nqc" && FindRcxTool() == null)
            {
                Log(I18n.T("rcx_tool.py is missing from this folder. It is what talks to the USB tower."),
                    Ui.P.LogErr, true);
                SetStatus(I18n.T("Tool not found."), 2);
                return false;
            }
            return true;
        }

        void StartStep()
        {
            Step s = queue[queueAt];

            StringBuilder line = new StringBuilder("$ ");
            line.Append(s.Shown);
            int skip = s.Shown == "nqc" ? 0 : s.Args.Count - CountToolArgs(s);
            for (int i = skip; i < s.Args.Count; i++)
            {
                line.Append(' ');
                line.Append(Quote(s.Args[i]));
            }
            if (!s.Silent) Log(line.ToString(), Ui.P.LogCmd, false);

            StringBuilder cmd = new StringBuilder();
            for (int i = 0; i < s.Args.Count; i++)
            {
                if (cmd.Length > 0) cmd.Append(' ');
                cmd.Append(Quote(s.Args[i]));
            }

            ProcessStartInfo psi = new ProcessStartInfo(s.Exe, cmd.ToString());
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            // rcx_tool.py loads libusb-1.0.dll and the bundled pyusb from the
            // program folder, so that is where it has to run.
            psi.WorkingDirectory = s.Shown == "nqc" && filePath != null
                ? Path.GetDirectoryName(filePath)
                : AppDir;

            if (s.SetPort)
            {
                string port = settings.Port.Text.Trim();
                if (port.Length > 0) psi.EnvironmentVariables["RCX_PORT"] = port;
            }

            cancelled = false;
            try
            {
                proc = new Process();
                proc.StartInfo = psi;
                proc.EnableRaisingEvents = true;
                proc.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null) Post(e.Data, false);
                };
                proc.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null) Post(e.Data, true);
                };
                proc.Exited += delegate { PostExit(s.Silent); };
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                SetBusy(true);
                watchdog.Interval = s.Timeout;
                watchdog.Start();
            }
            catch (Exception ex)
            {
                Log(I18n.T("Could not start {0}: {1}", Path.GetFileName(s.Exe), ex.Message),
                    Ui.P.LogErr, true);
                SetStatus(I18n.T("Failed to start."), 2);
                SetBusy(false);
                proc = null;
                queue = null;
            }
        }

        /// Number of trailing arguments that belong to rcx_tool rather than
        /// to the python launcher, so the $ line stays readable.
        static int CountToolArgs(Step s)
        {
            int n = s.Args.Count;
            for (int i = 0; i < s.Args.Count; i++)
                if (s.Args[i] != null &&
                    s.Args[i].EndsWith("rcx_tool.py", StringComparison.OrdinalIgnoreCase))
                    return n - i - 1;
            return n;
        }

        void Post(string text, bool isErr)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    Color c = isErr ? Ui.P.LogErr : Ui.P.LogOut;
                    Match m = ErrorRe.Match(text);
                    if (m.Success)
                    {
                        bool warn = string.Equals(m.Groups["sev"].Value, "Warning",
                                                  StringComparison.OrdinalIgnoreCase);
                        c = warn ? Ui.P.LogInfo : Ui.P.LogErr;
                        int ln;
                        if (!warn && int.TryParse(m.Groups["line"].Value, out ln))
                            editor.SetErrorLine(ln);
                    }
                    Log(text, c, false);
                });
            }
            catch (Exception) { }
        }

        void PostExit(bool silent)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    watchdog.Stop();
                    int code = -1;
                    try { if (proc != null) code = proc.ExitCode; }
                    catch (Exception) { }
                    proc = null;

                    // Chain: keep going only while each step succeeds.
                    if (!cancelled && code == 0 && queue != null &&
                        queueAt + 1 < queue.Count)
                    {
                        queueAt++;
                        StartStep();
                        return;
                    }

                    queue = null;
                    SetBusy(false);
                    if (!silent)
                    {
                        if (cancelled)
                        {
                            Log(I18n.T("Stopped."), Ui.P.LogInfo, true);
                            SetStatus(I18n.T("Stopped."), 2);
                        }
                        else if (code == 0)
                        {
                            Log("✔ " + I18n.T("Finished."), Ui.P.LogOk, true);
                            SetStatus(I18n.T("Command finished."), 0);
                            SetBrick(2, "RCX tower connected");
                        }
                        else
                        {
                            Log("✘ " + I18n.T("Finished with errors."), Ui.P.LogErr, true);
                            SetStatus(I18n.T("Finished with errors."), 2);
                        }
                    }
                });
            }
            catch (Exception) { }
        }

        void SetBusy(bool busy)
        {
            btnRun.Enabled = !busy;
            btnDownload.Enabled = !busy;
            btnCancel.Enabled = busy;
        }

        void CancelRun()
        {
            cancelled = true;
            watchdog.Stop();
            try
            {
                if (proc != null && !proc.HasExited) proc.Kill();
            }
            catch (Exception) { }
            SetBusy(false);
        }

        void Log(string text, Color c, bool bold)
        {
            console.Log.Append(text + "\n", c, bold);
        }

        // -------------------------------------------------------------------
        // Settings file
        // -------------------------------------------------------------------
        string SettingsPath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BrickCodeStudio");
                return Path.Combine(dir, "settings.txt");
            }
        }

        void LoadSettings()
        {
            try
            {
                string p = SettingsPath;
                if (!File.Exists(p)) return;
                string[] lines = File.ReadAllLines(p);
                for (int i = 0; i < lines.Length; i++)
                {
                    int eq = lines[i].IndexOf('=');
                    if (eq <= 0) continue;
                    string k = lines[i].Substring(0, eq).Trim();
                    string v = lines[i].Substring(eq + 1).Trim();
                    int n;
                    switch (k)
                    {
                        case "dark": dark = v == "1"; break;
                        case "lang":
                            I18n.Spanish = v == "es";
                            langDd.Select(I18n.Spanish ? 1 : 0, false);
                            break;
                        case "target":
                            for (int t = 0; t < App.Targets.Length; t++)
                                if (App.Targets[t] == v) settings.Target.Select(t, false);
                            break;
                        case "slot":
                            if (int.TryParse(v, out n) && n >= 1 && n <= 5)
                                settings.Slot.Select(n - 1, false);
                            break;
                        case "port": settings.Port.Text = v; break;
                        case "firmware": settings.Firmware.Text = v; break;
                        case "shutoff": settings.Shutoff.Text = v; break;
                        case "consoleH": if (int.TryParse(v, out n) && n > 60) consoleH = n; break;
                        case "console": consoleVisible = v == "1"; break;
                        case "settings":
                            settingsVisible = v == "1";
                            side.SettingsActive = settingsVisible;
                            break;
                        case "w": if (int.TryParse(v, out n) && n >= 1000) Width = n; break;
                        case "h": if (int.TryParse(v, out n) && n >= 660) Height = n; break;
                    }
                }
            }
            catch (Exception) { }
        }

        void SaveSettings()
        {
            try
            {
                string p = SettingsPath;
                string dir = Path.GetDirectoryName(p);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("dark=" + (dark ? "1" : "0"));
                sb.AppendLine("lang=" + (I18n.Spanish ? "es" : "en"));
                sb.AppendLine("target=" + settings.Target.SelectedItem);
                sb.AppendLine("slot=" + settings.Slot.SelectedItem);
                sb.AppendLine("port=" + settings.Port.Text);
                sb.AppendLine("firmware=" + settings.Firmware.Text);
                sb.AppendLine("shutoff=" + settings.Shutoff.Text);
                sb.AppendLine("consoleH=" + consoleH.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("console=" + (consoleVisible ? "1" : "0"));
                sb.AppendLine("settings=" + (settingsVisible ? "1" : "0"));
                sb.AppendLine("w=" + Width.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("h=" + Height.ToString(CultureInfo.InvariantCulture));
                File.WriteAllText(p, sb.ToString());
            }
            catch (Exception) { }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), App.Name + " - error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
