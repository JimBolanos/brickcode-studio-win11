// ---------------------------------------------------------------------------
// A small Cairo-like drawing layer over GDI+.
//
// The brick drawings of the Apple / Linux edition are written against Cairo
// (path, fill, stroke, translate, scale...). Rather than redraw each brick by
// hand, this class gives GDI+ the handful of calls they use, so the very same
// drawings -- measured from photographs of the real bricks -- run here too.
//
// Cairo transforms points when a path is built; GDI+ does it when drawing.
// Everything here follows Cairo: points go through the current matrix as they
// are added, and the Graphics object itself stays untransformed.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace BrickCode
{
    class CrPattern
    {
        public bool Radial;
        public double X0, Y0, X1, Y1, R1;
        public List<double> Pos = new List<double>();
        public List<Color> Col = new List<Color>();

        public void Stop(double at, double r, double g, double b)
        {
            StopA(at, r, g, b, 1.0);
        }

        public void StopA(double at, double r, double g, double b, double a)
        {
            Pos.Add(at);
            Col.Add(Color.FromArgb(C(a), C(r), C(g), C(b)));
        }

        static int C(double v)
        {
            int i = (int)Math.Round(v * 255.0);
            return i < 0 ? 0 : (i > 255 ? 255 : i);
        }
    }

    class Cr
    {
        class State
        {
            public Matrix M;
            public Color Col;
            public CrPattern Pat;
            public float W;
            public LineCap Cap;
            public LineJoin Join;
            public Region Clip;
        }

        readonly Graphics g;
        Matrix ctm = new Matrix();
        GraphicsPath path = new GraphicsPath();
        PointF cur, figStart;
        bool hasCur;
        Color col = Color.Black;
        CrPattern pat;
        float lineW = 2f;
        LineCap cap = LineCap.Flat;
        LineJoin join = LineJoin.Miter;
        readonly Stack<State> stack = new Stack<State>();

        public Cr(Graphics graphics, double originX, double originY)
        {
            g = graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            ctm.Translate((float)originX, (float)originY);
        }

        // ---- state -------------------------------------------------------
        public void Save()
        {
            State s = new State();
            s.M = ctm.Clone();
            s.Col = col; s.Pat = pat; s.W = lineW; s.Cap = cap; s.Join = join;
            s.Clip = g.Clip;
            stack.Push(s);
        }

        public void Restore()
        {
            if (stack.Count == 0) return;
            State s = stack.Pop();
            ctm.Dispose();
            ctm = s.M;
            col = s.Col; pat = s.Pat; lineW = s.W; cap = s.Cap; join = s.Join;
            g.Clip = s.Clip;
        }

        public void Translate(double x, double y) { ctm.Translate((float)x, (float)y); }
        public void Scale(double x, double y) { ctm.Scale((float)x, (float)y); }
        public void Rotate(double rad) { ctm.Rotate((float)(rad * 180.0 / Math.PI)); }

        PointF P(double x, double y)
        {
            PointF[] p = { new PointF((float)x, (float)y) };
            ctm.TransformPoints(p);
            return p[0];
        }

        float Scale1()
        {
            float[] e = ctm.Elements;
            return (float)Math.Sqrt(Math.Abs(e[0] * e[3] - e[1] * e[2]));
        }

        // ---- colour and line style ----------------------------------------
        public void Hex(string hex)
        {
            int o = hex[0] == '#' ? 1 : 0;
            col = Color.FromArgb(Convert.ToInt32(hex.Substring(o, 2), 16),
                                 Convert.ToInt32(hex.Substring(o + 2, 2), 16),
                                 Convert.ToInt32(hex.Substring(o + 4, 2), 16));
            pat = null;
        }

        public void SetSourceRgb(double r, double gr, double b)
        {
            col = Color.FromArgb((int)Math.Round(r * 255), (int)Math.Round(gr * 255), (int)Math.Round(b * 255));
            pat = null;
        }

        public CrPattern LinearPattern(double x0, double y0, double x1, double y1)
        {
            CrPattern p = new CrPattern();
            PointF a = P(x0, y0), b = P(x1, y1);
            p.X0 = a.X; p.Y0 = a.Y; p.X1 = b.X; p.Y1 = b.Y;
            return p;
        }

        public CrPattern RadialPattern(double cx, double cy, double r0, double cx1, double cy1, double r1)
        {
            CrPattern p = new CrPattern();
            PointF c = P(cx1, cy1);
            p.Radial = true;
            p.X1 = c.X; p.Y1 = c.Y; p.R1 = r1 * Scale1();
            return p;
        }

        public void SetSource(CrPattern p) { pat = p; }
        public void SetLineWidth(double w) { lineW = (float)w; }
        public void SetCapRound() { cap = LineCap.Round; }
        public void SetJoinRound() { join = LineJoin.Round; }

        // ---- path ---------------------------------------------------------
        public void NewPath()
        {
            path.Dispose();
            path = new GraphicsPath();
            hasCur = false;
        }

        public void NewSubPath()
        {
            path.StartFigure();
            hasCur = false;
        }

        public void MoveTo(double x, double y)
        {
            path.StartFigure();
            cur = figStart = P(x, y);
            hasCur = true;
        }

        public void LineTo(double x, double y)
        {
            PointF p = P(x, y);
            if (!hasCur) { path.StartFigure(); figStart = p; }
            else path.AddLine(cur, p);
            cur = p;
            hasCur = true;
        }

        public void Arc(double cx, double cy, double r, double a1, double a2)
        {
            while (a2 < a1) a2 += 2 * Math.PI;
            int n = Math.Max(6, (int)Math.Ceiling((a2 - a1) / (Math.PI / 24)));
            List<PointF> pts = new List<PointF>();
            if (hasCur) pts.Add(cur);
            for (int i = 0; i <= n; i++)
            {
                double a = a1 + (a2 - a1) * i / n;
                pts.Add(P(cx + r * Math.Cos(a), cy + r * Math.Sin(a)));
            }
            if (!hasCur) { path.StartFigure(); figStart = pts[0]; }
            path.AddLines(pts.ToArray());
            cur = pts[pts.Count - 1];
            hasCur = true;
        }

        public void ClosePath()
        {
            path.CloseFigure();
            cur = figStart;
            hasCur = false;
        }

        public void Rectangle(double x, double y, double w, double h)
        {
            PointF[] r = { P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h) };
            path.StartFigure();
            path.AddPolygon(r);
            hasCur = false;
        }

        public void Clip()
        {
            g.SetClip(path, CombineMode.Intersect);
            NewPath();
        }

        // ---- painting -----------------------------------------------------
        public void Fill()
        {
            if (path.PointCount > 0)
            {
                RectangleF bounds = path.GetBounds();
                Brush b = MakeBrush(bounds);
                g.FillPath(b, path);
                b.Dispose();
            }
            NewPath();
        }

        public void Stroke()
        {
            if (path.PointCount > 0)
            {
                using (Pen pen = new Pen(col, Math.Max(0.1f, lineW * Scale1())))
                {
                    pen.StartCap = cap;
                    pen.EndCap = cap;
                    pen.LineJoin = join;
                    g.DrawPath(pen, path);
                }
            }
            NewPath();
        }

        Brush MakeBrush(RectangleF bounds)
        {
            if (pat == null || pat.Pos.Count < 2) return new SolidBrush(col);
            ColorBlend blend = new ColorBlend(pat.Pos.Count);
            if (!pat.Radial)
            {
                float top = (float)Math.Min(pat.Y0, pat.Y1), bottom = (float)Math.Max(pat.Y0, pat.Y1);
                RectangleF r = new RectangleF(bounds.X, top, Math.Max(bounds.Width, 1f), Math.Max(bottom - top, 1f));
                LinearGradientBrush lb = new LinearGradientBrush(r, Color.Black, Color.White, LinearGradientMode.Vertical);
                for (int i = 0; i < pat.Pos.Count; i++)
                {
                    blend.Positions[i] = (float)pat.Pos[i];
                    blend.Colors[i] = pat.Col[i];
                }
                lb.InterpolationColors = blend;
                lb.WrapMode = WrapMode.TileFlipXY;
                return lb;
            }
            GraphicsPath ring = new GraphicsPath();
            ring.AddEllipse((float)(pat.X1 - pat.R1), (float)(pat.Y1 - pat.R1), (float)(2 * pat.R1), (float)(2 * pat.R1));
            PathGradientBrush pb = new PathGradientBrush(ring);
            pb.CenterPoint = new PointF((float)pat.X1, (float)pat.Y1);
            // In a path gradient 0 is the edge and 1 the centre: the reverse of Cairo.
            int n = pat.Pos.Count;
            for (int i = 0; i < n; i++)
            {
                blend.Positions[i] = (float)(1.0 - pat.Pos[n - 1 - i]);
                blend.Colors[i] = pat.Col[n - 1 - i];
            }
            pb.InterpolationColors = blend;
            ring.Dispose();
            return pb;
        }

        /// Text at a baseline point, left-anchored or centred on x, as a filled
        /// shape so it follows the matrix (the RCX serial number is rotated).
        public void Text(double x, double y, double size, bool bold, bool centre, string s)
        {
            FontStyle st = bold ? FontStyle.Bold : FontStyle.Regular;
            using (FontFamily ff = new FontFamily("Segoe UI"))
            using (GraphicsPath tp = new GraphicsPath())
            {
                float ascent = (float)(size * ff.GetCellAscent(st) / ff.GetEmHeight(st));
                tp.AddString(s, ff, (int)st, (float)size, new PointF(0, -ascent), StringFormat.GenericTypographic);
                float dx = (float)x;
                if (centre)
                {
                    RectangleF b = tp.GetBounds();
                    dx = (float)x - (b.X + b.Width / 2f);
                }
                using (Matrix m = new Matrix())
                {
                    m.Translate(dx, (float)y);
                    tp.Transform(m);
                }
                tp.Transform(ctm);
                using (SolidBrush br = new SolidBrush(col))
                    g.FillPath(br, tp);
            }
        }
    }
}
