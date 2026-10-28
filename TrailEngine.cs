// CursorGlow Studio 3: mouse motion trails. Windows Forms/.NET Framework 4.0.
// Click-through and no focus capture; transient animations only, no global hooks.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CursorGlowStudio
{
    internal sealed class TrailPoint
    {
        internal float X, Y;
        internal long Time;
        internal TrailPoint(float x,float y,long time) { X=x;Y=y;Time=time; }
    }

    internal static class TrailPainter
    {
        internal static readonly string[] Styles = {
            "Meki svjetlosni trag", "Neonska linija", "Svjetlece tockice",
            "Iskre i zvjezdice", "Svilena vrpca", "Kometa", "Mini kursori"
        };
        private static Color TrailColor(Config c)
        {
            try { return ColorTranslator.FromHtml(c.TrailMatchGlow ? c.ColorHex : c.TrailColorHex); }
            catch { return Color.FromArgb(82,158,255); }
        }
        private static float Limit(float v,float low,float high) { return Math.Max(low,Math.Min(high,v)); }
        private static void Stroke(Graphics g,PointF a,PointF b,Color color,float width,float alpha)
        {
            int opacity=(int)Limit(alpha,0,255);
            if (opacity<2) return;
            using(Pen p=new Pen(Color.FromArgb(opacity,color),Math.Max(1,width)))
            { p.StartCap=LineCap.Round;p.EndCap=LineCap.Round;p.LineJoin=LineJoin.Round;g.DrawLine(p,a,b); }
        }
        private static void Dot(Graphics g,PointF p,Color color,float radius,float alpha)
        {
            int opacity=(int)Limit(alpha,0,255);
            if(opacity<2 || radius<=0) return;
            using(Brush b=new SolidBrush(Color.FromArgb(opacity,color)))
                g.FillEllipse(b,p.X-radius,p.Y-radius,radius*2,radius*2);
        }
        private static void Star(Graphics g,PointF p,Color color,float size,float alpha,int salt)
        {
            Stroke(g,new PointF(p.X-size,p.Y),new PointF(p.X+size,p.Y),color,1.2f,alpha);
            Stroke(g,new PointF(p.X,p.Y-size),new PointF(p.X,p.Y+size),color,1.2f,alpha);
            if(salt%3==0) { float d=size*.62f;
                Stroke(g,new PointF(p.X-d,p.Y-d),new PointF(p.X+d,p.Y+d),color,1,alpha*.50f);
                Stroke(g,new PointF(p.X-d,p.Y+d),new PointF(p.X+d,p.Y-d),color,1,alpha*.50f);
            }
            Dot(g,p,Color.White,Math.Max(.6f,size*.2f),alpha*.60f);
        }
        internal static void Draw(Graphics g,List<TrailPoint> points,Config config,long now,float ox,float oy)
        {
            if(points.Count<2) return;
            Color color=TrailColor(config);
            g.SmoothingMode=SmoothingMode.AntiAlias;
            g.CompositingMode=CompositingMode.SourceOver;
            float width=config.TrailWidth;
            float strength=config.TrailOpacity/100f;
            int step=Math.Max(1,7-config.TrailDensity);
            int maxAge=config.TrailDuration;
            float total=0;
            for(int i=1;i<points.Count;i++)
            {
                float dx=points[i].X-points[i-1].X, dy=points[i].Y-points[i-1].Y;
                total+=(float)Math.Sqrt(dx*dx+dy*dy);
            }
            float walked=0;
            for(int i=1;i<points.Count;i++)
            {
                TrailPoint pa=points[i-1],pb=points[i];
                float dx=pb.X-pa.X,dy=pb.Y-pa.Y;
                float length=(float)Math.Sqrt(dx*dx+dy*dy);
                walked+=length;
                if(length<.1f)continue;
                float age=(float)((now-pb.Time)/(double)TimeSpan.TicksPerMillisecond);
                float fade=Limit(1-age/Math.Max(100,maxAge),0,1);
                float position=total>0 ? walked/total : 0;
                float opacity=fade*(.18f+.82f*position)*strength;
                if(opacity<=.005f)continue;
                PointF a=new PointF(pa.X-ox,pa.Y-oy);
                PointF b=new PointF(pb.X-ox,pb.Y-oy);
                float thickness=width*(.25f+.75f*position);
                switch(config.TrailStyle)
                {
                    case 0: // soft diffuse halo following path
                        Stroke(g,a,b,color,thickness*3.7f,22*opacity);
                        Stroke(g,a,b,color,thickness*2.1f,35*opacity);
                        Stroke(g,a,b,color,thickness*1.1f,55*opacity);
                        Stroke(g,a,b,color,thickness*.45f,115*opacity);
                        break;
                    case 1: // electric/neon
                        Stroke(g,a,b,color,thickness*2.8f,35*opacity);
                        Stroke(g,a,b,color,thickness*1.4f,90*opacity);
                        Stroke(g,a,b,Color.White,Math.Max(1,thickness*.23f),205*opacity);
                        break;
                    case 2: // dotted lights
                        if(i%step==0 || i==points.Count-1)
                        {
                            Dot(g,b,color,thickness*1.05f,34*opacity);
                            Dot(g,b,color,thickness*.65f,160*opacity);
                            Dot(g,b,Color.White,Math.Max(.9f,thickness*.22f),180*opacity);
                        }
                        break;
                    case 3: // glimmering scattered stars
                        if(i%step==0 || i==points.Count-1)
                        {
                            float variation=(float)Math.Sin(i*17.93)*thickness*1.3f;
                            PointF moved=new PointF(b.X+(-dy/length)*variation,b.Y+(dx/length)*variation);
                            Dot(g,moved,color,thickness*2.1f,28*opacity);
                            Star(g,moved,color,Math.Max(2,thickness*1.05f),225*opacity,i);
                        }
                        break;
                    case 4: // ribbon with tapered translucent sides
                        float nx=-dy/length,ny=dx/length;
                        float wa=width*(.20f+.72f*Math.Max(0,position-1f/points.Count));
                        float wb=width*(.25f+.72f*position);
                        PointF[] quad={new PointF(a.X+nx*wa,a.Y+ny*wa),
                          new PointF(b.X+nx*wb,b.Y+ny*wb),new PointF(b.X-nx*wb,b.Y-ny*wb),
                          new PointF(a.X-nx*wa,a.Y-ny*wa)};
                        using(Brush fill=new SolidBrush(Color.FromArgb((int)Limit(125*opacity,0,255),color)))g.FillPolygon(fill,quad);
                        Stroke(g,a,b,color,Math.Max(1,thickness*.2f),85*opacity);
                        break;
                    case 5: // comet - bright tail that diffuses
                        Stroke(g,a,b,color,thickness*4f,20*opacity);
                        Stroke(g,a,b,color,thickness*2.4f,55*opacity);
                        Stroke(g,a,b,color,thickness*.6f,155*opacity);
                        if(i%step==0)Dot(g,b,Color.White,Math.Max(.6f,thickness*.19f),90*opacity);
                        break;
                    case 6: // small arrowhead silhouettes, echoing cursor
                        if(i%step==0 || i==points.Count-1)
                        {
                            float s=Math.Max(2.5f,thickness*.95f);
                            PointF v1=new PointF(b.X,b.Y);
                            PointF v2=new PointF(b.X-dx/length*s+(-dy/length)*s*.55f,b.Y-dy/length*s+(dx/length)*s*.55f);
                            PointF v3=new PointF(b.X-dx/length*s-(-dy/length)*s*.55f,b.Y-dy/length*s-(dx/length)*s*.55f);
                            using(Brush fill=new SolidBrush(Color.FromArgb((int)Limit(135*opacity,0,255),color)))g.FillPolygon(fill,new PointF[]{v1,v2,v3});
                            using(Pen edge=new Pen(Color.FromArgb((int)Limit(190*opacity,0,255),Color.White),1))g.DrawPolygon(edge,new PointF[]{v1,v2,v3});
                        }
                        break;
                }
            }
        }
    }

    internal sealed class TrailOverlay : Form
    {
        private List<TrailPoint> trace=new List<TrailPoint>();
        private Point lastMouse;
        private bool haveLast=false;
        internal TrailOverlay()
        {
            FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
            StartPosition=FormStartPosition.Manual;Bounds=new Rectangle(-400,-400,8,8);
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams p=base.CreateParams;
                p.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TRANSPARENT | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
                return p;
            }
        }
        internal void Reset()
        {
            trace.Clear();haveLast=false;
            if(Visible)Hide();
        }
        internal void Follow(Config c)
        {
            if(!c.TrailOn) { if(haveLast || trace.Count>0)Reset();return; }
            Point mouse=Cursor.Position;
            long now=DateTime.UtcNow.Ticks;
            if(!haveLast)
            {
                lastMouse=mouse;haveLast=true;
                trace.Add(new TrailPoint(mouse.X+7,mouse.Y+8,now));
            }
            else
            {
                double dx=mouse.X-lastMouse.X,dy=mouse.Y-lastMouse.Y;
                double distance=Math.Sqrt(dx*dx+dy*dy);
                if(distance>1200) { trace.Clear();trace.Add(new TrailPoint(mouse.X+7,mouse.Y+8,now)); }
                else if(distance>=2)
                {
                    // Interpolate longer jumps for continuous luminous lines/dots.
                    int sections=Math.Min(30,Math.Max(1,(int)Math.Ceiling(distance/12.0)));
                    for(int j=1;j<=sections;j++)
                    {
                        float f=j/(float)sections;
                        trace.Add(new TrailPoint(lastMouse.X+7+(float)dx*f,lastMouse.Y+8+(float)dy*f,now-(long)((sections-j)*TimeSpan.TicksPerMillisecond*2)));
                    }
                }
                lastMouse=mouse;
            }
            long maxTicks=(long)c.TrailDuration*TimeSpan.TicksPerMillisecond;
            while(trace.Count>0 && now-trace[0].Time>maxTicks)trace.RemoveAt(0);
            double total=0;int start=0;
            for(int i=trace.Count-1;i>0;i--)
            {
                double dx=trace[i].X-trace[i-1].X,dy=trace[i].Y-trace[i-1].Y;
                total+=Math.Sqrt(dx*dx+dy*dy);
                if(total>c.TrailLength){start=i;break;}
            }
            if(start>0)trace.RemoveRange(0,start);
            if(trace.Count>160)trace.RemoveRange(0,trace.Count-160);
            if(trace.Count<2){if(Visible)Hide();return;}
            Render(c,now);
        }
        private void Render(Config c,long now)
        {
            float minX=float.MaxValue,minY=float.MaxValue,maxX=float.MinValue,maxY=float.MinValue;
            foreach(TrailPoint p in trace){minX=Math.Min(minX,p.X);minY=Math.Min(minY,p.Y);maxX=Math.Max(maxX,p.X);maxY=Math.Max(maxY,p.Y);}
            int margin=c.TrailWidth*5+26;
            int left=(int)Math.Floor(minX)-margin,top=(int)Math.Floor(minY)-margin;
            int w=(int)Math.Ceiling(maxX-minX)+margin*2;
            int h=(int)Math.Ceiling(maxY-minY)+margin*2;
            if(w<=0 || h<=0 || (long)w*h>5000000L) {Reset();return;}
            using(Bitmap bitmap=new Bitmap(w,h,PixelFormat.Format32bppPArgb))
            {
                using(Graphics g=Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Transparent);
                    TrailPainter.Draw(g,trace,c,now,left,top);
                }
                if(!Visible) Show();
                IntPtr dc=IntPtr.Zero,mem=IntPtr.Zero,dib=IntPtr.Zero,old=IntPtr.Zero;
                try
                {
                    dc=Native.GetDC(IntPtr.Zero);
                    mem=Native.CreateCompatibleDC(dc);
                    dib=bitmap.GetHbitmap(Color.FromArgb(0));
                    old=Native.SelectObject(mem,dib);
                    Native.POINT at=new Native.POINT(left,top);
                    Native.POINT src=new Native.POINT(0,0);
                    Native.SIZE dimensions=new Native.SIZE(w,h);
                    Native.BLENDFUNCTION blend=new Native.BLENDFUNCTION();
                    blend.BlendOp=0;blend.BlendFlags=0;blend.SourceConstantAlpha=255;blend.AlphaFormat=1;
                    Native.UpdateLayeredWindow(Handle,dc,ref at,ref dimensions,mem,ref src,0,ref blend,Native.ULW_ALPHA);
                }
                finally
                {
                    if(old!=IntPtr.Zero && mem!=IntPtr.Zero)Native.SelectObject(mem,old);
                    if(dib!=IntPtr.Zero)Native.DeleteObject(dib);
                    if(mem!=IntPtr.Zero)Native.DeleteDC(mem);
                    if(dc!=IntPtr.Zero)Native.ReleaseDC(IntPtr.Zero,dc);
                }
            }
        }
    }

    internal sealed class TrailPreviewPanel : Panel
    {
        internal Config Settings;
        internal TrailPreviewPanel(){DoubleBuffered=true;BackColor=Color.FromArgb(13,20,35);}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if(Settings==null) return;
            Graphics g=e.Graphics;
            g.SmoothingMode=SmoothingMode.AntiAlias;
            using(Pen grid=new Pen(Color.FromArgb(27,67,92,130),1))
            {
                for(int x=20;x<Width;x+=25)g.DrawLine(grid,x,0,x,Height);
                for(int y=15;y<Height;y+=25)g.DrawLine(grid,0,y,Width,y);
            }
            List<TrailPoint> pts=new List<TrailPoint>();
            long now=DateTime.UtcNow.Ticks;
            for(int i=0;i<=48;i++)
            {
                float f=i/48f;
                float x=18+f*(Width-54);
                float y=(float)(Height*.65-Height*.32*Math.Sin(f*5.4)+10*Math.Sin(f*10));
                pts.Add(new TrailPoint(x,y,now-(48-i)*TimeSpan.TicksPerMillisecond*10));
            }
            TrailPainter.Draw(g,pts,Settings,now,0,0);
            float hx=pts[pts.Count-1].X,hy=pts[pts.Count-1].Y;
            PointF[] triangle={new PointF(hx-6,hy-7),new PointF(hx+13,hy+2),new PointF(hx+1,hy+13)};
            using(Brush b=new SolidBrush(Color.Black))g.FillPolygon(b,triangle);
            using(Pen p=new Pen(Color.White,2.3f)){p.LineJoin=LineJoin.Round;g.DrawPolygon(p,triangle);}
        }
    }
}
