// ---------------------------------------------------------------------------
// The four bricks and the app logo, drawn with the Cairo-like layer.
//
// This is the drawing code of the Apple / Linux edition (bricks.inc) carried
// over line by line, so a brick looks the same on both. They were drawn from
// photographs of the real hardware: RCX yellow with three inputs and three
// outputs, Scout blue with the light-sensor barrel, NXT white with the orange
// enter button, EV3 white and grey with its button cluster lit red. The EV3
// carries no logo on purpose.
// ---------------------------------------------------------------------------

using System;

namespace BrickCode
{
    static class Bk
    {
        public static void Hex(Cr cr, string hex) { cr.Hex(hex); }

        public static void Rrect(Cr cr, double x, double y, double w, double h, double r)
        {
            cr.NewPath();
            if (r > w / 2) r = w / 2;
            if (r > h / 2) r = h / 2;
            cr.NewSubPath();
            cr.Arc(x + w - r, y + r, r, -Math.PI / 2, 0);
            cr.Arc(x + w - r, y + h - r, r, 0, Math.PI / 2);
            cr.Arc(x + r, y + h - r, r, Math.PI / 2, Math.PI);
            cr.Arc(x + r, y + r, r, Math.PI, 3 * Math.PI / 2);
            cr.ClosePath();
        }

        public static void Fill(Cr cr, string hex, double x, double y, double w, double h, double r)
        {
            cr.Hex(hex);
            if (r <= 0) { cr.NewPath(); cr.Rectangle(x, y, w, h); }
            else Rrect(cr, x, y, w, h, r);
            cr.Fill();
        }

        public static void Dot(Cr cr, string hex, double cx, double cy, double r)
        {
            cr.Hex(hex);
            cr.NewPath();
            cr.Arc(cx, cy, r, 0, 2 * Math.PI);
            cr.Fill();
        }

        public static void Text(Cr cr, string hex, double x, double y, double size,
                                bool bold, bool centre, string s)
        {
            cr.Hex(hex);
            cr.Text(x, y, size, bold, centre, s);
        }

        public static void Ellipse(Cr cr, string hex, double cx, double cy, double rx, double ry)
        {
            cr.Save();
            cr.NewPath();
            cr.Translate(cx, cy);
            cr.Scale(rx, ry);
            cr.Arc(0, 0, 1, 0, 2 * Math.PI);
            cr.Restore();
            cr.Hex(hex);
            cr.Fill();
        }

        /// How much of the card a brick takes up.
        const double BkScale = 0.50;
        /// Every drawing is scaled to the same height, whatever its shape.
        const double BkH = 266.0;

        public static void BeginN(Cr cr, int w, int h, double x0, double x1, double y0, double y1)
        {
            double s = Math.Min(w / 210.0, h / 272.0) * BkScale * (BkH / (y1 - y0));
            cr.Translate(w / 2.0, h / 2.0);
            cr.Scale(s, s);
            cr.Translate(-(x0 + x1) / 2, -(y0 + y1) / 2);
        }

        static void Rgb(string hex, out double r, out double g, out double b)
        {
            int o = hex[0] == '#' ? 1 : 0;
            r = Convert.ToInt32(hex.Substring(o, 2), 16) / 255.0;
            g = Convert.ToInt32(hex.Substring(o + 2, 2), 16) / 255.0;
            b = Convert.ToInt32(hex.Substring(o + 4, 2), 16) / 255.0;
        }

        /// A rounded rectangle filled top to bottom, for the bezel and the foot.
        public static void Vgrad(Cr cr, string top, string bottom, double x, double y,
                                 double w, double h, double r)
        {
            double ar, ag, ab, br, bg, bb;
            Rgb(top, out ar, out ag, out ab);
            Rgb(bottom, out br, out bg, out bb);
            CrPattern p = cr.LinearPattern(0, y, 0, y + h);
            p.Stop(0, ar, ag, ab);
            p.Stop(1, br, bg, bb);
            cr.SetSource(p);
            if (r <= 0) { cr.NewPath(); cr.Rectangle(x, y, w, h); }
            else Rrect(cr, x, y, w, h, r);
            cr.Fill();
        }

        /// A closed polygon from x,y pairs.
        public static void Poly(Cr cr, string hex, double[] pt, int n)
        {
            cr.Hex(hex);
            cr.NewPath();
            cr.MoveTo(pt[0], pt[1]);
            for (int i = 1; i < n; i++) cr.LineTo(pt[i * 2], pt[i * 2 + 1]);
            cr.ClosePath();
            cr.Fill();
        }

        /// The red the EV3 buttons glow with: bright in the middle, gone at the edge.
        public static void Glow(Cr cr, double cx, double cy, double r)
        {
            CrPattern p = cr.RadialPattern(cx, cy, 0, cx, cy, r);
            p.StopA(0.00, 1.00, 0.945, 0.933, 1.0);
            p.StopA(0.25, 1.00, 0.294, 0.243, 0.95);
            p.StopA(1.00, 0.898, 0.188, 0.169, 0.0);
            cr.SetSource(p);
            cr.NewPath();
            cr.Arc(cx, cy, r, 0, 2 * Math.PI);
            cr.Fill();
        }

    public static void DrawLogo(Cr cr, int w, int h) {
        cr.Save();
        double s = Math.Min(w, h) / 512.0;
        cr.Translate((w - 512 * s) / 2, (h - 512 * s) / 2);
        cr.Scale(s, s);

        CrPattern g = cr.LinearPattern(0, 0, 0, 512);
        g.Stop(0, 0x1C / 255.0, 0x2A / 255.0, 0x4A / 255.0);
        g.Stop(1, 0x12 / 255.0, 0x1A / 255.0, 0x30 / 255.0);
        Bk.Rrect(cr, 0, 0, 512, 512, 112.64);
        cr.SetSource(g);
        cr.Fill();
        Bk.Fill(cr, "#787E88", 66, 92, 380, 340, 44);
        Bk.Fill(cr, "#DEE2E8", 66, 84, 380, 336, 44);
        Bk.Fill(cr, "#F4F6FA", 80, 92, 352, 20, 10);
        Bk.Fill(cr, "#3C4048", 112, 128, 288, 138, 16);
        Bk.Fill(cr, "#96B08C", 122, 138, 268, 118, 10);

        Bk.Fill(cr, "#223422", 196, 170, 22, 22, 3);
        Bk.Fill(cr, "#223422", 296, 170, 22, 22, 3);
        Bk.Fill(cr, "#223422", 196, 214, 108, 12, 5);
        Bk.Fill(cr, "#223422", 184, 200, 16, 14, 5);
        Bk.Fill(cr, "#223422", 300, 200, 16, 14, 5);

        Bk.Fill(cr, "#464C58", 112, 292, 64, 48, 12);
        Bk.Fill(cr, "#464C58", 336, 292, 64, 48, 12);
        Bk.Fill(cr, "#464C58", 180, 362, 152, 30, 12);

        Bk.Ellipse(cr, "#FA8C14", 256, 316, 46, 34);
        Bk.Ellipse(cr, "#FFB046", 256, 310, 32, 18);

        Bk.Fill(cr, "#282C34", 112, 404, 52, 20, 6);
        Bk.Fill(cr, "#282C34", 176, 404, 52, 20, 6);
        Bk.Fill(cr, "#282C34", 240, 404, 52, 20, 6);
        Bk.Fill(cr, "#282C34", 304, 404, 52, 20, 6);

        cr.Restore();
    }

    /* ------------------------------------------------------------------ RCX */
    public static void DrawRcx(Cr cr, int w, int h) {
        cr.Save();
        Bk.BeginN(cr, w, h, 6, 204, 1.9, 266);

        Bk.Fill(cr, "#C9A510", 6, 6, 198, 260, 13);      /* the edge below */
        Bk.Fill(cr, "#F4C81B", 6, 2, 198, 260, 13);      /* the yellow face */
        Bk.Fill(cr, "#1D1D1F", 74, 2, 62, 17, 4);        /* dark block on top */

        /* Three ports, not a grid. The brick has three inputs and three
         * outputs; the old drawing showed eight pads above and sixteen below,
         * which matched nothing on it and left the numbers labelling air. */
        Bk.Fill(cr, "#9A9A9A", 30, 26, 150, 46, 7);      /* inputs 1 2 3 */
        for (int port = 0; port < 3; port++)
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 2; col++)
                    Bk.Fill(cr, "#C2C2C2", 34 + port * 54 + col * 18,
                            32 + row * 19, 16, 15, 2.5);

        Bk.Text(cr, "#1D1D1F", 27,  86, 11, true, false, "View");
        Bk.Text(cr, "#1D1D1F", 155, 86, 11, true, false, "Prgm");
        Bk.Text(cr, "#1D1D1F", 72,  87, 14, true, true,  "1");
        Bk.Text(cr, "#1D1D1F", 105, 87, 14, true, true,  "2");
        Bk.Text(cr, "#1D1D1F", 138, 87, 14, true, true,  "3");

        /* The screen. The old drawing left it out entirely, which is why the
         * middle read as a gap with the labels floating in it. */
        Bk.Fill(cr, "#7E8780", 60, 90, 90, 46, 10);
        Bk.Fill(cr, "#C5CCC0", 62, 92, 86, 42, 9);
        Bk.Dot(cr, "#3F4637", 105, 106, 3.1);            /* the running figure */
        Bk.Hex(cr, "#3F4637");
        cr.SetLineWidth(2.6);
        cr.SetCapRound();
        cr.NewPath();
        cr.MoveTo(105, 111); cr.LineTo(105, 120);
        cr.MoveTo(105, 113); cr.LineTo( 99, 117);
        cr.MoveTo(105, 113); cr.LineTo(112, 115);
        cr.MoveTo(105, 120); cr.LineTo( 99, 126);
        cr.MoveTo(105, 120); cr.LineTo(111, 125);
        cr.Stroke();
        cr.NewPath();

        /* a size smaller than the rest: at 11 it ran under the screen */
        Bk.Text(cr, "#1D1D1F", 22,  116, 10, true, false, "On-Off");
        Bk.Text(cr, "#1D1D1F", 160, 116, 11, true, false, "Run");
        Bk.Text(cr, "#1D1D1F", 72,  150, 14, true, true,  "A");
        Bk.Text(cr, "#1D1D1F", 105, 150, 14, true, true,  "B");
        Bk.Text(cr, "#1D1D1F", 138, 150, 14, true, true,  "C");

        Bk.Fill(cr, "#2B2B2D", 22,  90,  30, 15, 3);     /* View  */
        Bk.Fill(cr, "#2B2B2D", 158, 90,  30, 15, 3);     /* Prgm  */
        Bk.Fill(cr, "#D4302B", 22,  120, 30, 16, 3);     /* On-Off */
        Bk.Fill(cr, "#189A48", 158, 120, 30, 16, 3);     /* Run   */

        /* Outputs: three ports again, and the same height as the inputs. The
         * old one was nearly twice as tall; on the brick they match, and the
         * plain yellow that leaves below is on the real one too. */
        Bk.Fill(cr, "#4A4A4C", 30, 156, 150, 46, 7);     /* outputs A B C */
        for (int port = 0; port < 3; port++)
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 2; col++)
                    Bk.Fill(cr, "#161618", 34 + port * 54 + col * 18,
                            162 + row * 19, 16, 15, 2.5);

        Bk.Text(cr, "#1D1D1F", 20, 252, 12, true, false, "RCX 2.0");
        cr.Save();
        cr.Translate(190, 214);
        cr.Rotate(Math.PI / 2);
        Bk.Text(cr, "#1D1D1F", 0, 0, 11, true, false, "123456");
        cr.Restore();

        cr.Restore();
    }

    /* ---------------------------------------------------------------- Scout */
    public static void DrawScout(Cr cr, int w, int h) {
        cr.Save();
        Bk.BeginN(cr, w, h, 5.8, 208.2, 0, 286.2);

        Bk.Fill(cr, "#123A7A", 6, 10, 202, 276, 14);
        Bk.Fill(cr, "#1B50AE", 6, 6,  202, 276, 14);

        Bk.Fill(cr, "#17181A", 88, 0, 38, 34, 9);        /* light sensor barrel */
        Bk.Dot(cr, "#0E0F10", 107, 30, 13);
        Bk.Dot(cr, "#F2C81B", 34, 26, 6);                /* the little sun */
        Bk.Dot(cr, "#17181A", 52, 36, 4.5);
        Bk.Dot(cr, "#17181A", 68, 36, 4.5);
        Bk.Dot(cr, "#17181A", 146, 36, 4.5);
        Bk.Dot(cr, "#17181A", 162, 36, 4.5);

        /* the yellow stud pads, ports 1 and 2 */
        Bk.Fill(cr, "#E8C219", 26, 48, 56, 38, 5);
        Bk.Dot(cr, "#C9A510", 40, 60, 6);  Bk.Dot(cr, "#C9A510", 68, 60, 6);
        Bk.Dot(cr, "#C9A510", 40, 76, 6);  Bk.Dot(cr, "#C9A510", 68, 76, 6);
        Bk.Fill(cr, "#E8C219", 132, 48, 56, 38, 5);
        Bk.Dot(cr, "#C9A510", 146, 60, 6); Bk.Dot(cr, "#C9A510", 174, 60, 6);
        Bk.Dot(cr, "#C9A510", 146, 76, 6); Bk.Dot(cr, "#C9A510", 174, 76, 6);

        Bk.Text(cr, "#FFFFFF", 14,  70, 13, true, false, "1");
        Bk.Text(cr, "#FFFFFF", 194, 70, 13, true, false, "2");
        Bk.Text(cr, "#FFFFFF", 56,  99, 10, true, false, "Motion");
        Bk.Text(cr, "#FFFFFF", 98,  99, 10, true, false, "Touch");
        Bk.Text(cr, "#FFFFFF", 140, 99, 10, true, false, "Light");
        Bk.Text(cr, "#FFFFFF", 16,  116, 10, true, false, "Select");
        Bk.Text(cr, "#FFFFFF", 158, 116, 10, true, false, "Change");
        Bk.Text(cr, "#FFFFFF", 16,  152, 10, true, false, "On-Off");
        Bk.Text(cr, "#FFFFFF", 168, 152, 10, true, false, "Run");
        Bk.Text(cr, "#FFFFFF", 22,  196, 10, true, false, "Time");
        Bk.Text(cr, "#FFFFFF", 166, 196, 10, true, false, "FX");
        Bk.Text(cr, "#FFFFFF", 107, 198, 13, true, true,  "SCOUT");

        Bk.Fill(cr, "#232528", 16,  120, 34, 14, 3);
        Bk.Fill(cr, "#232528", 164, 120, 34, 14, 3);
        Bk.Fill(cr, "#D4302B", 16,  156, 34, 15, 3);
        Bk.Fill(cr, "#189A48", 164, 156, 34, 15, 3);

        Bk.Fill(cr, "#1E2023", 58, 104, 98, 78, 4);      /* the menu LCD */
        Bk.Fill(cr, "#A9C19B", 62, 108, 90, 70, 2);
        Bk.Text(cr, "#3E4C39", 65,  119, 6.5, false, false, "Forward");
        Bk.Text(cr, "#3E4C39", 92,  119, 6.5, false, false, "Reverse");
        Bk.Text(cr, "#3E4C39", 120, 119, 6.5, false, false, "Seek");
        Bk.Text(cr, "#3E4C39", 120, 130, 6.5, false, false, "Avoid");
        Bk.Text(cr, "#3E4C39", 120, 141, 6.5, false, false, "WaitFor");
        Bk.Text(cr, "#3E4C39", 120, 152, 6.5, false, false, "Brake");
        Bk.Hex(cr, "#3E4C39");
        cr.SetLineWidth(1.4);
        cr.NewPath();
        cr.Arc(72, 166, 4, 0, 2 * Math.PI); cr.Stroke();
        cr.NewPath();
        cr.Arc(84, 166, 4, 0, 2 * Math.PI); cr.Stroke();

        /* output pads A and B */
        Bk.Fill(cr, "#17181A", 24, 206, 62, 54, 6);
        Bk.Dot(cr, "#3A3C40", 40, 222, 9);  Bk.Dot(cr, "#3A3C40", 70, 222, 9);
        Bk.Dot(cr, "#3A3C40", 40, 245, 9);  Bk.Dot(cr, "#3A3C40", 70, 245, 9);
        Bk.Fill(cr, "#17181A", 128, 206, 62, 54, 6);
        Bk.Dot(cr, "#3A3C40", 144, 222, 9); Bk.Dot(cr, "#3A3C40", 174, 222, 9);
        Bk.Dot(cr, "#3A3C40", 144, 245, 9); Bk.Dot(cr, "#3A3C40", 174, 245, 9);
        Bk.Text(cr, "#FFFFFF", 14,  214, 11, true, false, "A");
        Bk.Text(cr, "#FFFFFF", 196, 214, 11, true, false, "B");

        Bk.Hex(cr, "#9BA1A8");                           /* the arrow keys */
        double[] up = { 98, 118 };
        for (int i = 0; i < 2; i++) {
            cr.MoveTo(up[i], 214);
            cr.LineTo(up[i] + 9, 225);
            cr.LineTo(up[i] - 9, 225);
            cr.ClosePath(); cr.Fill();
            cr.MoveTo(up[i], 252);
            cr.LineTo(up[i] - 9, 241);
            cr.LineTo(up[i] + 9, 241);
            cr.ClosePath(); cr.Fill();
        }
        Bk.Dot(cr, "#123A7A", 107, 272, 9);

        cr.Restore();
    }

    /* ------------------------------------------------------------------ NXT */
    public static void DrawNxt(Cr cr, int w, int h) {
        cr.Save();
        Bk.BeginN(cr, w, h, 3.8, 202.2, 0, 266.1);

        Bk.Fill(cr, "#86888C", 20, 0, 166, 26, 5);       /* A B C and USB */
        Bk.Text(cr, "#2E3034", 42,  16, 9, true, false, "A");
        Bk.Text(cr, "#2E3034", 78,  16, 9, true, false, "B");
        Bk.Text(cr, "#2E3034", 114, 16, 9, true, false, "C");
        Bk.Fill(cr, "#B9BBBF", 140, 4, 38, 18, 3);
        Bk.Text(cr, "#2E3034", 147, 17, 8, true, false, "USB");

        Bk.Fill(cr, "#BEC1C6", 12, 22, 182, 222, 16);
        Bk.Fill(cr, "#EFF1F3", 12, 18, 182, 222, 16);
        Bk.Fill(cr, "#DCDEE2", 4,   62, 14, 52, 6);      /* the side ridges */
        Bk.Fill(cr, "#DCDEE2", 188, 62, 14, 52, 6);

        Bk.Fill(cr, "#9DA0A5", 42, 36, 122, 92, 5);      /* the screen */
        Bk.Fill(cr, "#A8BE9C", 47, 41, 112, 82, 3);

        for (int i = 0; i < 4; i++)                      /* speaker grille */
            Bk.Fill(cr, "#6E7278", 168, 112 + i * 8, 18, 3.4, 1.7);

        Bk.Fill(cr, "#C9CBCF", 101, 130, 4, 18, 0);
        Bk.Fill(cr, "#EF8019", 86, 146, 34, 34, 5);      /* enter */
        Bk.Hex(cr, "#C3C6CB");                           /* the arrows, outward */
        cr.MoveTo(46, 163); cr.LineTo(74, 150);
        cr.LineTo(74, 176); cr.ClosePath(); cr.Fill();
        cr.MoveTo(160, 163); cr.LineTo(132, 150);
        cr.LineTo(132, 176); cr.ClosePath(); cr.Fill();
        Bk.Fill(cr, "#8E9196", 88, 188, 30, 17, 5);      /* back */

        Bk.Text(cr, "#5E6166", 103, 232, 23, true, true, "nxt");

        Bk.Fill(cr, "#86888C", 20, 240, 166, 26, 5);     /* 1 2 3 4 */
        Bk.Text(cr, "#2E3034", 40,  257, 9, true, false, "1");
        Bk.Text(cr, "#2E3034", 76,  257, 9, true, false, "2");
        Bk.Text(cr, "#2E3034", 112, 257, 9, true, false, "3");
        Bk.Text(cr, "#2E3034", 148, 257, 9, true, false, "4");

        cr.Restore();
    }

    /* ------------------------------------------------------------------ EV3 */

    /* A rounded rectangle filled top to bottom, for the bezel and the foot. */

    /* A closed polygon from x,y pairs. */

    /* The red the buttons glow with: bright in the middle, gone at the edge. */

    public static void DrawEv3(Cr cr, int w, int h) {
        cr.Save();
        Bk.BeginN(cr, w, h, 6, 170, 0, 254);

        /* the ridges either side, behind the body */
        Bk.Fill(cr, "#E4E5E7", 6,   49, 14, 159, 3);
        Bk.Fill(cr, "#E4E5E7", 156, 49, 14, 159, 3);

        Bk.Fill(cr, "#B9BABD", 15, 3, 142, 251, 9);      /* the edge below */

        /* the body: white, a grey bezel over the screen, a darker foot */
        cr.Save();
        Bk.Rrect(cr, 15, 0, 142, 252, 9);
        cr.Clip();
        Bk.Vgrad(cr, "#F7F7F8", "#ECECEE", 15, 0,   142, 252, 0);
        Bk.Vgrad(cr, "#E2E3E5", "#C9CACD", 15, 0,   142, 118, 0);
        Bk.Fill(cr, "#C3C4C7", 15, 112, 142, 6, 0);
        Bk.Vgrad(cr, "#B2B3B6", "#9FA0A4", 15, 218, 142, 34, 0);
        Bk.Fill(cr, "#A5A6A9", 15, 218, 142, 1.6, 0);
        cr.Restore();

        /* the screen: a black window with a grey LCD in it */
        Bk.Fill(cr, "#1A1A1B", 28, 22, 116, 70, 6);
        Bk.Fill(cr, "#3E4041", 37, 26, 98, 62, 1.5);
        Bk.Fill(cr, "#575A5B", 38, 27, 96, 60, 1);
        Bk.Text(cr, "#1C1E1E", 86, 31.6, 5.6, true, true, "EV3");
        cr.NewPath();
        Bk.Hex(cr, "#1C1E1E");                           /* battery and rules */
        cr.SetLineWidth(0.7);
        Bk.Rrect(cr, 120, 28.2, 12, 4.4, 1);
        cr.Stroke();
        cr.NewPath();
        cr.Rectangle(121.2, 29.3, 8.4, 2.2);
        cr.Fill();
        cr.MoveTo(38, 34);   cr.LineTo(134, 34);
        cr.MoveTo(38, 46);   cr.LineTo(121, 46);
        cr.Stroke();
        cr.NewPath();
        Bk.Rrect(cr, 123, 47.5, 6, 38, 1);               /* scroll bar */
        cr.Stroke();
        cr.NewPath();
        cr.Rectangle(124, 48.6, 4, 3.4);
        cr.Fill();

        /* the four tabs: run, files, apps, settings */
        cr.SetLineWidth(0.9);
        cr.SetJoinRound();
        cr.SetCapRound();
        cr.NewPath();                              /* the open one, run */
        cr.MoveTo(44, 45.5);  cr.LineTo(44, 39);
        cr.Arc(46, 39, 2, Math.PI, 3 * Math.PI / 2);
        cr.LineTo(59, 37);
        cr.Arc(59, 39, 2, -Math.PI / 2, 0);
        cr.LineTo(61, 45.5);
        cr.Stroke();
        for (int i = 0; i < 3; i++) {
            cr.NewPath();
            Bk.Rrect(cr, 66 + i * 20, 37, 15, 8.5, 2);
            cr.Stroke();
        }
        cr.NewPath();
        cr.Arc(52.5, 41.4, 3, 0, 2 * Math.PI);       /* run: circle + triangle */
        cr.Stroke();
        cr.NewPath();
        cr.MoveTo(51.6, 39.9); cr.LineTo(54.4, 41.4);
        cr.LineTo(51.6, 42.9); cr.ClosePath();
        cr.Fill();
        cr.NewPath();                              /* files: a page */
        cr.MoveTo(71, 39);     cr.LineTo(75.2, 39);
        cr.LineTo(77, 40.8);   cr.LineTo(77, 44);
        cr.LineTo(71, 44);     cr.ClosePath();
        cr.Stroke();
        cr.NewPath();                              /* apps: a grid */
        for (int r = 0; r < 2; r++)
            for (int c = 0; c < 3; c++) {
                cr.MoveTo(91 + c * 3,     39.4 + r * 3.5);
                cr.LineTo(92.6 + c * 3,   39.4 + r * 3.5);
            }
        cr.Stroke();
        cr.NewPath();                              /* settings: a wrench */
        cr.MoveTo(109.6, 43.6); cr.LineTo(116, 38.4);
        cr.Stroke();
        cr.NewPath();
        cr.Arc(117.2, 38.2, 1.5, 0, 2 * Math.PI);
        cr.Stroke();
        cr.NewPath();

        /* the little grey tab, with a corner cut off it */
        {
            double[] tab = { 24, 122,  58, 122,  58, 133,  54, 137,  24, 137 };
            Bk.Poly(cr, "#C7C8CB", tab, 5);
        }

        /* the buttons: an octagon of up and down, a pill either side, a square
         * in the middle */
        {
            double[] outer = { 71,130, 77,128, 95,128, 101,130, 109,138,
                                   109,190, 101,198, 95,200, 77,200, 71,198,
                                   63,190, 63,138 };
            double[] inner = { 72,132, 78,130, 94,130, 100,132, 107,139,
                                   107,189, 100,196, 94,198, 78,198, 72,196,
                                   65,189, 65,139 };
            Bk.Poly(cr, "#3A3A3D", outer, 12);
            Bk.Poly(cr, "#C5C6C9", inner, 12);
        }
        Bk.Fill(cr, "#3A3A3D", 39,  152, 37, 24, 12);
        Bk.Fill(cr, "#C5C6C9", 41,  154, 33, 20, 10);
        Bk.Fill(cr, "#3A3A3D", 104, 152, 31, 24, 12);
        Bk.Fill(cr, "#C5C6C9", 106, 154, 27, 20, 10);
        Bk.Fill(cr, "#3A3A3D", 76,  152.5, 24, 24, 5);
        Bk.Fill(cr, "#E9EAEC", 78,  154.5, 20, 20, 3.4);
        Bk.Fill(cr, "#B7B8BB", 79.4, 155.9, 17.2, 17.2, 2);

        /* lit red from underneath, in the gaps either side of the middle */
        Bk.Glow(cr, 66.5, 163.5, 9);
        Bk.Glow(cr, 105.5, 163.5, 9);
        Bk.Hex(cr, "#FF5A4D");
        cr.SetLineWidth(1.1);
        cr.SetCapRound();
        cr.NewPath();
        cr.MoveTo(63, 163.5);  cr.LineTo(77, 163.5);
        cr.MoveTo(99, 163.5);  cr.LineTo(113, 163.5);
        cr.Stroke();
        cr.NewPath();

        /* the name, pressed into the foot: a shade under, a highlight over */
        Bk.Text(cr, "#8F9094", 23, 236.5, 14.5, true, false, "EV3");
        cr.NewPath();
        Bk.Text(cr, "#E1E2E4", 23, 235.4, 14.5, true, false, "EV3");
        cr.NewPath();

        cr.Restore();
    }
    }
}
