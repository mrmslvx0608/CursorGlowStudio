// CursorGlow Studio | Windows 10/11 | .NET Framework 4.x
// Custom cursors + a click-through, always-on blue radial glow.
// Local-only application. No network calls or administrator rights required.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Globalization;

namespace CursorGlowStudio
{
    internal static class Program
    {
        internal static readonly string Folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        internal static readonly string ConfigFile = Path.Combine(Folder, "settings.ini");
        internal static readonly string BackupFile = Path.Combine(Folder, "original-cursors.ini");
        internal static readonly uint ShowMessage = Native.RegisterWindowMessage("CursorGlowStudio_OPEN_SETTINGS_2026");

        [STAThread]
        private static void Main(string[] args)
        {
            try { Native.SetProcessDPIAware(); } catch { }
            bool restore = args.Length > 0 && args[0] == "--restore-cursors";
            bool applyDefault = args.Length > 0 && args[0] == "--apply-default-cursor";
            if (restore || applyDefault)
            {
                try
                {
                    if (restore) CursorManager.Restore();
                    else CursorManager.Apply("09-KaoNaSlici", "01-Crna", "02-Normalna");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "CursorGlow Studio - kursori", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            bool first;
            using (Mutex mutex = new Mutex(true, "Local\\CursorGlowStudio_OneInstance_2026", out first))
            {
                if (!first)
                {
                    Native.PostMessage(new IntPtr(0xFFFF), ShowMessage, IntPtr.Zero, IntPtr.Zero);
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                bool background = args.Length > 0 && args[0] == "--background";
                Application.Run(new SettingsForm(background));
            }
        }
    }

    internal static class Native
    {
        internal const int WS_EX_LAYERED = 0x00080000;
        internal const int WS_EX_TRANSPARENT = 0x00000020;
        internal const int WS_EX_TOOLWINDOW = 0x00000080;
        internal const int WS_EX_NOACTIVATE = 0x08000000;
        internal const uint ULW_ALPHA = 0x00000002;
        internal const uint SWP_NOSIZE = 0x0001;
        internal const uint SWP_NOZORDER = 0x0004;
        internal const uint SWP_NOACTIVATE = 0x0010;
        internal const uint SWP_NOOWNERZORDER = 0x0200;
        [StructLayout(LayoutKind.Sequential)] internal struct POINT { public int X; public int Y; public POINT(int x,int y){X=x;Y=y;} }
        [StructLayout(LayoutKind.Sequential)] internal struct SIZE { public int Width; public int Height; public SIZE(int w,int h){Width=w;Height=h;} }
        [StructLayout(LayoutKind.Sequential, Pack=1)] internal struct BLENDFUNCTION { public byte BlendOp; public byte BlendFlags; public byte SourceConstantAlpha; public byte AlphaFormat; }
        [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
        [DllImport("user32.dll", CharSet=CharSet.Unicode)] internal static extern uint RegisterWindowMessage(string msg);
        [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr hWnd,uint msg,IntPtr wParam,IntPtr lParam);
        [DllImport("user32.dll")] internal static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] internal static extern int ReleaseDC(IntPtr hWnd,IntPtr hDC);
        [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] internal static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] internal static extern IntPtr SelectObject(IntPtr hdc,IntPtr hgdiobj);
        [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr hObject);
        [DllImport("user32.dll",SetLastError=true)] internal static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr hdcDst,ref POINT pptDst,ref SIZE psize,IntPtr hdcSrc,ref POINT pptSrc,uint crKey,ref BLENDFUNCTION pblend,uint dwFlags);
        [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr hWnd,IntPtr hWndInsertAfter,int x,int y,int cx,int cy,uint flags);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern bool SystemParametersInfo(uint action,uint param,IntPtr pvParam,uint flags);
    }

    internal sealed class Config
    {
        internal int Radius = 72;
        internal int Intensity = 34;
        internal int Softness = 36;
        internal int OffsetX = 12;
        internal int OffsetY = 10;
        internal string ColorHex = "#529EFF";
        internal string Theme = "01-Crni"; // retained for compatibility with v1 settings
        internal string CursorShape = "09-KaoNaSlici";
        internal string CursorColor = "01-Crna";
        internal string CursorSize = "02-Normalna";
        internal bool GlowOn = true;
        internal bool TrailOn = true;
        internal int TrailStyle = 0;
        internal int TrailLength = 240;      // px
        internal int TrailDuration = 650;    // ms
        internal int TrailWidth = 12;        // px
        internal int TrailOpacity = 75;      // %
        internal int TrailDensity = 4;       // 1..6
        internal bool TrailMatchGlow = true;
        internal string TrailColorHex = "#529EFF";
        internal bool StartWindows = true;

        internal static Config Load()
        {
            Config c = new Config();
            if (!File.Exists(Program.ConfigFile)) return c;
            try
            {
                Dictionary<string,string> dic = Ini.Load(Program.ConfigFile);
                c.Radius = Bounded(dic, "Radius", c.Radius, 30, 140);
                c.Intensity = Bounded(dic, "Intensity", c.Intensity, 5, 85);
                c.Softness = Bounded(dic, "Softness", c.Softness, 10, 75);
                c.OffsetX = Bounded(dic, "OffsetX", c.OffsetX, -30, 30);
                c.OffsetY = Bounded(dic, "OffsetY", c.OffsetY, -30, 30);
                if (dic.ContainsKey("ColorHex")) c.ColorHex = dic["ColorHex"];
                if (dic.ContainsKey("Theme")) c.Theme = dic["Theme"];
                if (dic.ContainsKey("CursorShape")) c.CursorShape = dic["CursorShape"];
                if (dic.ContainsKey("CursorColor")) c.CursorColor = dic["CursorColor"];
                if (dic.ContainsKey("CursorSize")) c.CursorSize = dic["CursorSize"];
                if (dic.ContainsKey("GlowOn")) c.GlowOn = dic["GlowOn"] == "1";
                if (dic.ContainsKey("StartWindows")) c.StartWindows = dic["StartWindows"] == "1";
                if (dic.ContainsKey("TrailOn")) c.TrailOn = dic["TrailOn"] == "1";
                c.TrailStyle = Bounded(dic,"TrailStyle",c.TrailStyle,0,TrailPainter.Styles.Length-1);
                c.TrailLength = Bounded(dic,"TrailLength",c.TrailLength,60,700);
                c.TrailDuration = Bounded(dic,"TrailDuration",c.TrailDuration,150,1600);
                c.TrailWidth = Bounded(dic,"TrailWidth",c.TrailWidth,2,28);
                c.TrailOpacity = Bounded(dic,"TrailOpacity",c.TrailOpacity,10,100);
                c.TrailDensity = Bounded(dic,"TrailDensity",c.TrailDensity,1,6);
                if(dic.ContainsKey("TrailMatchGlow"))c.TrailMatchGlow=dic["TrailMatchGlow"]=="1";
                if(dic.ContainsKey("TrailColorHex"))c.TrailColorHex=dic["TrailColorHex"];
                try { ColorTranslator.FromHtml(c.TrailColorHex); } catch { c.TrailColorHex="#529EFF"; }
                try { ColorTranslator.FromHtml(c.ColorHex); } catch { c.ColorHex = "#529EFF"; }
            }
            catch { }
            return c;
        }
        private static int Bounded(Dictionary<string,string> d,string name,int def,int low,int high)
        {
            int v;
            if (!d.ContainsKey(name) || !int.TryParse(d[name], out v)) return def;
            return Math.Max(low,Math.Min(high,v));
        }
        internal void Save()
        {
            Dictionary<string,string> d = new Dictionary<string,string>();
            d["Radius"]=Radius.ToString(); d["Intensity"]=Intensity.ToString();
            d["Softness"]=Softness.ToString(); d["OffsetX"]=OffsetX.ToString();
            d["OffsetY"]=OffsetY.ToString(); d["ColorHex"]=ColorHex;
            d["Theme"]=Theme;d["CursorShape"]=CursorShape;d["CursorColor"]=CursorColor;d["CursorSize"]=CursorSize; d["GlowOn"]=GlowOn ? "1":"0";
            d["StartWindows"]=StartWindows ? "1":"0";
            d["TrailOn"]=TrailOn ? "1":"0"; d["TrailStyle"]=TrailStyle.ToString();
            d["TrailLength"]=TrailLength.ToString();d["TrailDuration"]=TrailDuration.ToString();
            d["TrailWidth"]=TrailWidth.ToString();d["TrailOpacity"]=TrailOpacity.ToString();
            d["TrailDensity"]=TrailDensity.ToString();d["TrailMatchGlow"]=TrailMatchGlow ? "1":"0";
            d["TrailColorHex"]=TrailColorHex;
            Ini.Save(Program.ConfigFile,d);
        }
    }

    internal static class Ini
    {
        internal static Dictionary<string,string> Load(string file)
        {
            Dictionary<string,string> d = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in File.ReadAllLines(file))
            {
                int idx = line.IndexOf('=');
                if (idx > 0) d[line.Substring(0,idx).Trim()] = line.Substring(idx+1);
            }
            return d;
        }
        internal static void Save(string file,Dictionary<string,string> pairs)
        {
            List<string> lines=new List<string>();
            foreach (KeyValuePair<string,string> item in pairs) lines.Add(item.Key+"="+item.Value);
            File.WriteAllLines(file,lines.ToArray(),System.Text.Encoding.UTF8);
        }
    }

    internal static class CursorManager
    {
        internal static readonly string[] Roles = { "Arrow","Help","AppStarting","Wait","Crosshair","IBeam","NWPen","No","SizeNS","SizeWE","SizeNWSE","SizeNESW","SizeAll","UpArrow","Hand","Pin","Person" };
        private const string CursorReg = @"Control Panel\Cursors";
        internal static void BackUpIfNeeded()
        {
            if (File.Exists(Program.BackupFile)) return;
            Dictionary<string,string> old = new Dictionary<string,string>();
            using (RegistryKey key=Registry.CurrentUser.OpenSubKey(CursorReg,false))
            {
                if (key==null) throw new Exception("Windows registar za kursore nije dostupan.");
                foreach (string role in Roles) old[role] = Convert.ToString(key.GetValue(role,""));
            }
            Ini.Save(Program.BackupFile,old);
        }
        internal static string GetDirectory(string shape,string color,string size)
        {
            // Only prepackaged cursor names are allowed; never use a user-supplied path here.
            if (Array.IndexOf(Shapes,shape)<0 || Array.IndexOf(Colors,color)<0 || Array.IndexOf(Sizes,size)<0)
                throw new Exception("Odabrani oblik, boja ili velicina kursora nisu valjani.");
            return Path.Combine(Program.Folder,"CursorsV2",shape,color,size);
        }
        internal static readonly string[] Shapes={"01-Klasican","02-Tanki","03-Zaobljeni","04-Outline","05-Futuristicki","06-Pixel","07-Dijamant","08-Krug","09-KaoNaSlici"};
        internal static readonly string[] Colors={"01-Crna","02-Bijela","03-Plava","04-Ljubicasta","05-Cyan","06-Zelena"};
        internal static readonly string[] Sizes={"01-Mala","02-Normalna","03-Velika"};
        internal static void Apply(string shape,string color,string size)
        {
            string dir=GetDirectory(shape,color,size);
            if(!Directory.Exists(dir))throw new Exception("Nedostaje set kursora. Ponovno instaliraj program: "+dir);
            foreach(string role in Roles)
                if(!File.Exists(Path.Combine(dir,role+".cur")))throw new Exception("Nedostaje "+role+".cur. Ponovno instaliraj program.");
            BackUpIfNeeded();
            using(RegistryKey key=Registry.CurrentUser.OpenSubKey(CursorReg,true))
            {
                if(key==null) throw new Exception("Nije moguce urediti kursore za trenutnog korisnika.");
                foreach(string role in Roles) key.SetValue(role,Path.Combine(dir,role+".cur"),RegistryValueKind.String);
            }
            Reload();
        }
        internal static void ApplyCustomCursor(string role,string original)
        {
            if(Array.IndexOf(Roles,role)<0)throw new Exception("Nepoznata vrsta pokazivaca.");
            string ext=Path.GetExtension(original).ToLowerInvariant();
            if(ext!=".cur" && ext!=".ani")throw new Exception("Odaberi .cur ili .ani datoteku.");
            string targetDir=Path.Combine(Program.Folder,"Cursors","Custom");
            Directory.CreateDirectory(targetDir);
            string target=Path.Combine(targetDir,"Vlastiti_"+role+ext);
            if(!string.Equals(Path.GetFullPath(original),Path.GetFullPath(target),StringComparison.OrdinalIgnoreCase))File.Copy(original,target,true);
            BackUpIfNeeded();
            using(RegistryKey key=Registry.CurrentUser.OpenSubKey(CursorReg,true))
            {
                if(key==null)throw new Exception("Nije moguce urediti kursore.");
                key.SetValue(role,target,RegistryValueKind.String);
            }
            Reload();
        }
        internal static void Restore()
        {
            if (!File.Exists(Program.BackupFile)) return;
            Dictionary<string,string> old=Ini.Load(Program.BackupFile);
            using (RegistryKey key=Registry.CurrentUser.OpenSubKey(CursorReg,true))
            {
                if (key==null) throw new Exception("Nije moguće vratiti Windows kursore.");
                foreach (string role in Roles)
                    if (old.ContainsKey(role)) key.SetValue(role,old[role],RegistryValueKind.String);
            }
            Reload();
            File.Delete(Program.BackupFile);
        }
        internal static void Reload()
        {
            if (!Native.SystemParametersInfo(0x0057,0,IntPtr.Zero,0x0002))
                throw new Exception("Windows nije potvrdio ponovno učitavanje kursora. Pokušaj odjaviti i ponovno prijaviti Windows korisnika.");
        }
    }

    internal static class AutoStart
    {
        private const string KeyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
        internal static void Set(bool value)
        {
            using (RegistryKey key=Registry.CurrentUser.CreateSubKey(KeyName))
            {
                if (key==null) throw new Exception("Windows Run registar nije dostupan.");
                if (value) key.SetValue("CursorGlowStudio","\""+Application.ExecutablePath+"\" --background",RegistryValueKind.String);
                else key.DeleteValue("CursorGlowStudio",false);
            }
        }
        internal static bool IsEnabled()
        {
            using (RegistryKey key=Registry.CurrentUser.OpenSubKey(KeyName,false))
                return key != null && key.GetValue("CursorGlowStudio",null) != null;
        }
    }

    internal static class GlowFactory
    {
        internal static Bitmap Make(Config c)
        {
            int radius=c.Radius;
            int size=radius*2+8;
            Bitmap image=new Bitmap(size,size,PixelFormat.Format32bppPArgb);
            Color tint;
            try { tint=ColorTranslator.FromHtml(c.ColorHex); } catch { tint=Color.CornflowerBlue; }
            byte[] pixels = new byte[size*size*4];
            int center=size/2;
            double exponent=1.20 + c.Softness/22.0;
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                double dx=(x+0.5-center)/radius;
                double dy=(y+0.5-center)/radius;
                double t=1-dx*dx-dy*dy;
                if (t<=0) continue;
                int a=(int)Math.Round(255.0*(c.Intensity/100.0)*Math.Pow(t,exponent));
                if (a<=0) continue;
                if (a>255) a=255;
                int off=(y*size+x)*4;
                pixels[off+0]=(byte)(tint.B*a/255);
                pixels[off+1]=(byte)(tint.G*a/255);
                pixels[off+2]=(byte)(tint.R*a/255);
                pixels[off+3]=(byte)a;
            }
            BitmapData data=image.LockBits(new Rectangle(0,0,size,size),ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
            try { Marshal.Copy(pixels,0,data.Scan0,pixels.Length); }
            finally { image.UnlockBits(data); }
            return image;
        }
    }

    internal sealed class GlowOverlay : Form
    {
        private Bitmap bitmap;
        private int previousX=int.MinValue;
        private int previousY=int.MinValue;
        private int shiftX=0;
        private int shiftY=0;
        internal Bitmap Preview { get { return bitmap; } }
        internal GlowOverlay()
        {
            FormBorderStyle=FormBorderStyle.None;
            ShowInTaskbar=false;
            TopMost=true;
            StartPosition=FormStartPosition.Manual;
            Bounds=new Rectangle(-300,-300,8,8);
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp=base.CreateParams;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TRANSPARENT | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
                return cp;
            }
        }
        internal void Render(Config cfg)
        {
            Bitmap next=GlowFactory.Make(cfg);
            Bitmap former=bitmap;
            bitmap=next;
            shiftX=cfg.OffsetX; shiftY=cfg.OffsetY;
            previousX=int.MinValue;
            if (IsHandleCreated && Visible) UpdatePixels();
            if (former!=null) former.Dispose();
        }
        internal void UpdatePixels()
        {
            if(bitmap==null || !IsHandleCreated) return;
            IntPtr screenDC=IntPtr.Zero, memDC=IntPtr.Zero, hBitmap=IntPtr.Zero, old=IntPtr.Zero;
            try
            {
                screenDC=Native.GetDC(IntPtr.Zero);
                memDC=Native.CreateCompatibleDC(screenDC);
                hBitmap=bitmap.GetHbitmap(Color.FromArgb(0));
                old=Native.SelectObject(memDC,hBitmap);
                System.Drawing.Point mouse=System.Windows.Forms.Cursor.Position;
                int x=mouse.X+shiftX-bitmap.Width/2;
                int y=mouse.Y+shiftY-bitmap.Height/2;
                Native.POINT pos=new Native.POINT(x,y);
                Native.POINT src=new Native.POINT(0,0);
                Native.SIZE size=new Native.SIZE(bitmap.Width,bitmap.Height);
                Native.BLENDFUNCTION blend=new Native.BLENDFUNCTION();
                blend.BlendOp=0; blend.BlendFlags=0; blend.SourceConstantAlpha=255; blend.AlphaFormat=1;
                if (!Native.UpdateLayeredWindow(Handle,screenDC,ref pos,ref size,memDC,ref src,0,ref blend,Native.ULW_ALPHA))
                    throw new Exception("Windows nije uspio iscrtati transparentni glow.");
                previousX=x; previousY=y;
            }
            finally
            {
                if (old!=IntPtr.Zero && memDC!=IntPtr.Zero) Native.SelectObject(memDC,old);
                if (hBitmap!=IntPtr.Zero) Native.DeleteObject(hBitmap);
                if (memDC!=IntPtr.Zero) Native.DeleteDC(memDC);
                if (screenDC!=IntPtr.Zero) Native.ReleaseDC(IntPtr.Zero,screenDC);
            }
        }
        internal void Follow()
        {
            if (!Visible || bitmap==null) return;
            System.Drawing.Point p=System.Windows.Forms.Cursor.Position;
            int x=p.X+shiftX-bitmap.Width/2;
            int y=p.Y+shiftY-bitmap.Height/2;
            if (x==previousX && y==previousY) return;
            Native.SetWindowPos(Handle,IntPtr.Zero,x,y,0,0,Native.SWP_NOSIZE|Native.SWP_NOZORDER|Native.SWP_NOACTIVATE|Native.SWP_NOOWNERZORDER);
            previousX=x; previousY=y;
        }
        protected override void Dispose(bool disposing)
        {
            if(disposing && bitmap!=null) { bitmap.Dispose(); bitmap=null; }
            base.Dispose(disposing);
        }
    }

    internal sealed class PreviewPanel : Panel
    {
        internal GlowOverlay Glow;
        internal string DirectoryPath="";
        internal PreviewPanel()
        {
            DoubleBuffered=true;
            BackColor=Color.FromArgb(240,245,252);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g=e.Graphics;
            g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using(Brush dot=new SolidBrush(Color.FromArgb(208,217,233)))
                for(int y=11;y<Height;y+=19)for(int x=11;x<Width;x+=19)g.FillEllipse(dot,x,y,2,2);
            Bitmap b=Glow!=null?Glow.Preview:null;
            if(b!=null)g.DrawImage(b,new Rectangle(57-b.Width/2,72-b.Height/2,b.Width,b.Height));
            DrawExample(g,"Arrow",new Rectangle(12,15,82,86));
            DrawExample(g,"Hand",new Rectangle(130,18,68,75));
            DrawExample(g,"IBeam",new Rectangle(257,24,60,66));
            using(Brush f=new SolidBrush(Color.FromArgb(43,60,86)))
            using(Font font=new Font("Segoe UI",8.5f))
            {
                g.DrawString("Strelica",font,f,21,112);
                g.DrawString("Link",font,f,144,112);
                g.DrawString("Tekst",font,f,267,112);
            }
        }
        private void DrawExample(Graphics g,string role,Rectangle rect)
        {
            string path=Path.Combine(DirectoryPath,role+".cur");
            if(File.Exists(path))
            {
                try { using(Cursor c=new Cursor(path))c.Draw(g,rect);return; }
                catch { }
            }
            Cursors.Arrow.Draw(g,rect);
        }
    }

    internal sealed class SettingsForm : Form
    {
        private readonly Config cfg;
        private readonly GlowOverlay overlay;
        private readonly TrailOverlay trails;
        private readonly System.Windows.Forms.Timer followTimer;
        private readonly NotifyIcon tray;
        private readonly bool background;
        private bool reallyExit=false;
        private bool ready=false;
        private CheckBox chkGlow,chkStartup,chkTrail,chkMatchGlow;
        private ComboBox cmbColors,cmbShapes,cmbCursorColors,cmbCursorSizes,cmbRole,cmbTrailStyle;
        private TrackBar sizeBar,strengthBar,softBar,offsetXBar,offsetYBar,trailLengthBar,trailDurationBar,trailWidthBar,trailOpacityBar,trailDensityBar;
        private Label sizeValue,strengthValue,softValue,offsetXValue,offsetYValue,statusLabel,trailLengthValue,trailDurationValue,trailWidthValue,trailOpacityValue,trailDensityValue;
        private PreviewPanel preview,glowPreview;
        private TrailPreviewPanel trailPreview;
        private Panel pageGlow,pageTrail,pageCursor;
        private Button navGlow,navTrail,navCursor;
        private ToolStripMenuItem glowMenu;
        private Color surface=Color.FromArgb(24,33,50), text=Color.FromArgb(236,242,252), muted=Color.FromArgb(166,179,197), accent=Color.FromArgb(78,157,255);

        internal SettingsForm(bool startHidden)
        {
            background=startHidden;
            cfg=Config.Load();
            overlay=new GlowOverlay();
            trails=new TrailOverlay();
            Text="CursorGlow Studio";
            this.Icon=System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            ClientSize=new Size(1040,760);
            MinimumSize=new Size(1040,760);
            MaximumSize=new Size(1040,760);
            StartPosition=FormStartPosition.CenterScreen;
            BackColor=Color.FromArgb(14,21,35);
            ForeColor=text;
            Font=new Font("Segoe UI",9.5f);
            MaximizeBox=false;
            BuildInterface();
            tray=new NotifyIcon();
            tray.Icon=Icon;
            tray.Text="CursorGlow Studio 3";
            tray.Visible=true;
            ContextMenuStrip menu=new ContextMenuStrip();
            menu.Items.Add("Otvori postavke",null,delegate { OpenSettings(); });
            glowMenu=new ToolStripMenuItem("Plavi glow uključen");
            glowMenu.Checked=cfg.GlowOn; glowMenu.CheckOnClick=false;
            glowMenu.Click+=delegate { chkGlow.Checked=!chkGlow.Checked; };
            menu.Items.Add(glowMenu);
            ToolStripMenuItem trailMenu=new ToolStripMenuItem("Tragovi miša ukljuceni");
            trailMenu.Checked=cfg.TrailOn;
            trailMenu.Click+=delegate {chkTrail.Checked=!chkTrail.Checked;trailMenu.Checked=chkTrail.Checked;};
            menu.Items.Add(trailMenu);
            chkTrail.CheckedChanged+=delegate{trailMenu.Checked=chkTrail.Checked;};
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Zatvori program",null,delegate { reallyExit=true; Close(); });
            tray.ContextMenuStrip=menu;
            tray.DoubleClick+=delegate { OpenSettings(); };
            followTimer=new System.Windows.Forms.Timer();
            followTimer.Interval=16;
            followTimer.Tick+=delegate
            {
                if(cfg.GlowOn)overlay.Follow();
                if(cfg.TrailOn)
                {
                    try { trails.Follow(cfg); }
                    catch(Exception) { cfg.TrailOn=false;chkTrail.Checked=false;trails.Reset();Persist();statusLabel.Text="Tragovi iskljuceni zbog problema s grafickim prikazom."; }
                }
                else trails.Reset();
            };
            this.Shown+=delegate
            {
                try
                {
                    if(cfg.GlowOn) overlay.Show();
                    overlay.Render(cfg);
                    followTimer.Start();
                }
                catch(Exception ex) { MessageBox.Show(this,ex.Message,"Glow problem",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
                if(background) BeginInvoke(new Action(delegate { Hide(); }));
            };
            this.FormClosing+=delegate(object sender,FormClosingEventArgs e)
            {
                if(!reallyExit && e.CloseReason==CloseReason.UserClosing) { e.Cancel=true; Hide(); }
            };
            this.FormClosed+=delegate { followTimer.Stop(); followTimer.Dispose(); overlay.Close(); overlay.Dispose();trails.Close();trails.Dispose(); tray.Visible=false; tray.Dispose(); };
            ready=true;
            UpdateLabels();
        }
        protected override void WndProc(ref Message msg)
        {
            if(Program.ShowMessage!=0 && msg.Msg==Program.ShowMessage) { OpenSettings(); return; }
            base.WndProc(ref msg);
        }
        private void OpenSettings() { if(!Visible) Show(); WindowState=FormWindowState.Normal; BringToFront(); Activate(); }

        private Label AddLabel(Control parent,string label,int x,int y,int w,int h,int fontSize,Color color,bool bold)
        {
            Label result=new Label();
            result.Text=label; result.SetBounds(x,y,w,h); result.ForeColor=color;
            result.BackColor=Color.Transparent;
            result.Font=new Font("Segoe UI",fontSize,bold ? FontStyle.Bold : FontStyle.Regular);
            result.AutoSize=false;
            parent.Controls.Add(result);
            return result;
        }
        private Panel Card(int x,int y,int width,int height)
        {
            Panel p=new Panel();p.BackColor=surface;p.SetBounds(x,y,width,height);
            Controls.Add(p);return p;
        }
        private Button MakeButton(Control parent,string title,int x,int y,int w,int h,bool primary)
        {
            Button button=new Button();button.SetBounds(x,y,w,h);button.Text=title;
            button.FlatStyle=FlatStyle.Flat; button.FlatAppearance.BorderSize=primary?0:1;
            button.FlatAppearance.BorderColor=Color.FromArgb(76,92,118);
            button.BackColor=primary?accent:Color.FromArgb(42,54,76);
            button.ForeColor=primary?Color.FromArgb(12,22,40):text;
            button.Font=new Font("Segoe UI",9f,FontStyle.Bold);
            button.Cursor=Cursors.Hand;
            parent.Controls.Add(button);return button;
        }
        private TrackBar MakeBar(Panel parent,int x,int y,int w,int min,int max,int current,EventHandler handler)
        {
            TrackBar b=new TrackBar();b.SetBounds(x,y,w,43);b.Minimum=min;b.Maximum=max;b.TickStyle=TickStyle.None;b.Value=current;
            b.BackColor=surface;
            parent.Controls.Add(b);b.ValueChanged+=handler;return b;
        }
        private ComboBox CreateChoice(Control parent,int x,int y,int w,string[] entries,int selected)
        {
            ComboBox c=new ComboBox();c.DropDownStyle=ComboBoxStyle.DropDownList;
            c.SetBounds(x,y,w,30);c.Font=new Font("Segoe UI",9.5f);
            c.Items.AddRange(entries);c.SelectedIndex=selected<0?0:selected;
            parent.Controls.Add(c);return c;
        }
        private void UpdateCursorPreview()
        {
            try
            {
                string dir=CursorManager.GetDirectory(cfg.CursorShape,cfg.CursorColor,cfg.CursorSize);
                if(preview!=null){preview.DirectoryPath=dir;preview.Invalidate();}
                if(glowPreview!=null){glowPreview.DirectoryPath=dir;glowPreview.Invalidate();}
            }
            catch(Exception ex){ShowError(ex);}
        }
        private Panel CardIn(Control parent,int x,int y,int w,int h)
        {
            Panel p=new Panel();p.SetBounds(x,y,w,h);p.BackColor=surface;parent.Controls.Add(p);return p;
        }
        private void SwitchPage(int selected)
        {
            pageGlow.Visible=selected==0;pageTrail.Visible=selected==1;pageCursor.Visible=selected==2;
            Button[] buttons={navGlow,navTrail,navCursor};
            for(int i=0;i<buttons.Length;i++)
            {
                buttons[i].BackColor=i==selected?accent:Color.FromArgb(38,50,71);
                buttons[i].ForeColor=i==selected?Color.FromArgb(12,22,40):text;
            }
            if(selected==1 && trailPreview!=null)trailPreview.Invalidate();
        }
        private void RefreshTrail()
        {
            trails.Reset();UpdateTrailLabels();
            if(trailPreview!=null)trailPreview.Invalidate();
            if(ready)Persist();
        }
        private void UpdateTrailLabels()
        {
            if(trailLengthValue==null)return;
            trailLengthValue.Text=cfg.TrailLength+" px";
            trailDurationValue.Text=cfg.TrailDuration+" ms";
            trailWidthValue.Text=cfg.TrailWidth+" px";
            trailOpacityValue.Text=cfg.TrailOpacity+" %";
            trailDensityValue.Text=cfg.TrailDensity+" / 6";
        }
        private void TrailPreset(int preset)
        {
            bool previous=ready;ready=false;
            if(preset==0){cfg.TrailStyle=0;cfg.TrailLength=180;cfg.TrailDuration=450;cfg.TrailWidth=10;cfg.TrailOpacity=60;cfg.TrailDensity=3;}
            else if(preset==1){cfg.TrailStyle=1;cfg.TrailLength=330;cfg.TrailDuration=700;cfg.TrailWidth=12;cfg.TrailOpacity=85;cfg.TrailDensity=4;}
            else if(preset==2){cfg.TrailStyle=5;cfg.TrailLength=480;cfg.TrailDuration=980;cfg.TrailWidth=16;cfg.TrailOpacity=85;cfg.TrailDensity=5;}
            else {cfg.TrailStyle=3;cfg.TrailLength=380;cfg.TrailDuration=1100;cfg.TrailWidth=14;cfg.TrailOpacity=90;cfg.TrailDensity=6;}
            cfg.TrailOn=true;chkTrail.Checked=true;
            cmbTrailStyle.SelectedIndex=cfg.TrailStyle;
            trailLengthBar.Value=cfg.TrailLength;trailDurationBar.Value=cfg.TrailDuration;
            trailWidthBar.Value=cfg.TrailWidth;trailOpacityBar.Value=cfg.TrailOpacity;trailDensityBar.Value=cfg.TrailDensity;
            ready=previous;RefreshTrail();
        }
        private void BuildInterface()
        {
            AddLabel(this,"CursorGlow Studio 3",24,14,700,44,24,text,true);
            AddLabel(this,"Kursor kao na slici  •  plavi glow  •  tragovi i efekti pomicanja",25,57,980,22,10,muted,false);
            navGlow=MakeButton(this,"SVJETLOSNI EFEKT",22,101,313,41,false);
            navTrail=MakeButton(this,"TRAGOVI I EFEKTI",344,101,313,41,false);
            navCursor=MakeButton(this,"IZGLED KURSORA",666,101,351,41,false);
            pageGlow=CardIn(this,21,155,998,534);pageTrail=CardIn(this,21,155,998,534);pageCursor=CardIn(this,21,155,998,534);
            navGlow.Click+=delegate{SwitchPage(0);};
            navTrail.Click+=delegate{SwitchPage(1);};
            navCursor.Click+=delegate{SwitchPage(2);};
            BuildGlowPage();BuildTrailPage();BuildCursorPage();SwitchPage(1);
            chkStartup=new CheckBox();chkStartup.SetBounds(24,711,245,28);
            chkStartup.Text="Pokreni s Windowsom";chkStartup.ForeColor=text;chkStartup.BackColor=BackColor;
            chkStartup.Checked=AutoStart.IsEnabled();Controls.Add(chkStartup);
            chkStartup.CheckedChanged+=delegate
            {
                if(!ready)return;
                try{AutoStart.Set(chkStartup.Checked);cfg.StartWindows=chkStartup.Checked;Persist();}
                catch(Exception ex){ShowError(ex);}
            };
            statusLabel=AddLabel(this,"V3: postavi tragove i odaberi kursor kao na slici u kartici IZGLED KURSORA.",280,708,725,39,9,muted,false);
        }
        private void BuildGlowPage()
        {
            Panel left=CardIn(pageGlow,7,9,478,513);
            Panel right=CardIn(pageGlow,497,9,491,513);
            AddLabel(left,"SVJETLOSNI EFEKT OKO MISA",17,12,430,27,11,text,true);
            chkGlow=new CheckBox();chkGlow.SetBounds(18,49,405,28);
            chkGlow.Text="Stalno prikazuj glow";chkGlow.Checked=cfg.GlowOn;chkGlow.ForeColor=text;chkGlow.BackColor=surface;left.Controls.Add(chkGlow);
            chkGlow.CheckedChanged+=delegate
            {
                if(!ready)return;
                cfg.GlowOn=chkGlow.Checked;glowMenu.Checked=cfg.GlowOn;
                if(cfg.GlowOn){overlay.Show();overlay.Render(cfg);}else overlay.Hide();
                RefreshTrail();Persist();
            };
            AddLabel(left,"Boja svjetla",20,90,240,22,10,muted,false);
            cmbColors=CreateChoice(left,19,114,290,new string[]{"Plava (kao na slici)","Ledena / cyan","Ljubicasta","Ruzicasta","Zelena","Vlastita boja"},0);
            string[] colorCodes={"#529EFF","#66F4FC","#A882FF","#FF66B8","#65F5AC"};
            int selected=Array.IndexOf(colorCodes,cfg.ColorHex.ToUpperInvariant());cmbColors.SelectedIndex=selected>=0?selected:5;
            cmbColors.SelectedIndexChanged+=delegate{if(!ready)return;if(cmbColors.SelectedIndex>=0&&cmbColors.SelectedIndex<5){cfg.ColorHex=colorCodes[cmbColors.SelectedIndex];RefreshGlow();}};
            Button customColor=MakeButton(left,"Odaberi",321,113,136,32,false);
            customColor.Click+=delegate
            {
                using(ColorDialog d=new ColorDialog())
                {
                    d.FullOpen=true;d.Color=ColorTranslator.FromHtml(cfg.ColorHex);
                    if(d.ShowDialog(this)==DialogResult.OK){cfg.ColorHex=ColorTranslator.ToHtml(d.Color);cmbColors.SelectedIndex=5;RefreshGlow();}
                }
            };
            AddLabel(left,"Velicina sjaja",20,161,250,22,10,muted,false);
            sizeValue=AddLabel(left,"",363,161,92,22,10,accent,true);
            sizeBar=MakeBar(left,17,183,439,30,140,cfg.Radius,delegate{if(!ready)return;cfg.Radius=sizeBar.Value;RefreshGlow();});
            AddLabel(left,"Jacina sjaja",20,229,250,22,10,muted,false);
            strengthValue=AddLabel(left,"",363,229,92,22,10,accent,true);
            strengthBar=MakeBar(left,17,251,439,5,85,cfg.Intensity,delegate{if(!ready)return;cfg.Intensity=strengthBar.Value;RefreshGlow();});
            AddLabel(left,"Mekoca prijelaza",20,298,250,22,10,muted,false);
            softValue=AddLabel(left,"",363,298,92,22,10,accent,true);
            softBar=MakeBar(left,17,320,439,10,75,cfg.Softness,delegate{if(!ready)return;cfg.Softness=softBar.Value;RefreshGlow();});
            AddLabel(left,"Pomak sjaja X",20,367,250,22,10,muted,false);
            offsetXValue=AddLabel(left,"",363,367,92,22,10,accent,true);
            offsetXBar=MakeBar(left,17,389,439,-30,30,cfg.OffsetX,delegate{if(!ready)return;cfg.OffsetX=offsetXBar.Value;RefreshGlow();});
            AddLabel(left,"Pomak sjaja Y",20,434,250,22,10,muted,false);
            offsetYValue=AddLabel(left,"",363,434,92,22,10,accent,true);
            offsetYBar=MakeBar(left,17,456,439,-30,30,cfg.OffsetY,delegate{if(!ready)return;cfg.OffsetY=offsetYBar.Value;RefreshGlow();});
            AddLabel(right,"PREGLED KURSORA I SJAJA",21,17,439,26,11,text,true);
            glowPreview=new PreviewPanel();glowPreview.SetBounds(20,54,451,176);glowPreview.Glow=overlay;right.Controls.Add(glowPreview);
            AddLabel(right,"Isti mekani plavi halo kao na izvornom screenshotu.",19,247,450,49,10,muted,false);
            Button originalGlow=MakeButton(right,"VRATI POSTAVKE SJAJA SA SLIKE",20,315,451,42,true);
            originalGlow.Click+=delegate
            {
                bool prior=ready;ready=false;
                cfg.ColorHex="#529EFF";cfg.Radius=72;cfg.Intensity=34;cfg.Softness=36;cfg.OffsetX=12;cfg.OffsetY=10;
                cmbColors.SelectedIndex=0;sizeBar.Value=cfg.Radius;strengthBar.Value=cfg.Intensity;
                softBar.Value=cfg.Softness;offsetXBar.Value=cfg.OffsetX;offsetYBar.Value=cfg.OffsetY;
                ready=prior;RefreshGlow();
            };
            AddLabel(right,"Tragovi se podesavaju zasebno u kartici TRAGOVI I EFEKTI.\nMozes ukljuciti oboje istovremeno.",21,387,445,80,10,muted,false);
        }
        private void BuildTrailPage()
        {
            Panel left=CardIn(pageTrail,7,9,495,513);
            Panel right=CardIn(pageTrail,512,9,476,513);
            AddLabel(left,"TRAGOVI PRI POMICANJU MISA",18,12,450,26,11,text,true);
            chkTrail=new CheckBox();chkTrail.SetBounds(18,42,449,27);chkTrail.Text="Prikazi trag iza kursora";
            chkTrail.Checked=cfg.TrailOn;chkTrail.ForeColor=text;chkTrail.BackColor=surface;left.Controls.Add(chkTrail);
            chkTrail.CheckedChanged+=delegate{if(!ready)return;cfg.TrailOn=chkTrail.Checked;RefreshTrail();};
            AddLabel(left,"STIL TRAGA",18,76,460,20,10,muted,true);
            cmbTrailStyle=CreateChoice(left,18,99,457,TrailPainter.Styles,cfg.TrailStyle);
            cmbTrailStyle.SelectedIndexChanged+=delegate{if(!ready)return;cfg.TrailStyle=cmbTrailStyle.SelectedIndex;RefreshTrail();};
            AddLabel(left,"Duljina traga",19,144,320,20,10,muted,false);
            trailLengthValue=AddLabel(left,"",380,144,92,20,10,accent,true);
            trailLengthBar=MakeBar(left,16,165,460,60,700,cfg.TrailLength,delegate{if(!ready)return;cfg.TrailLength=trailLengthBar.Value;RefreshTrail();});
            AddLabel(left,"Trajanje nestajanja",19,212,320,20,10,muted,false);
            trailDurationValue=AddLabel(left,"",364,212,110,20,10,accent,true);
            trailDurationBar=MakeBar(left,16,234,460,150,1600,cfg.TrailDuration,delegate{if(!ready)return;cfg.TrailDuration=trailDurationBar.Value;RefreshTrail();});
            AddLabel(left,"Debljina traga",19,282,320,20,10,muted,false);
            trailWidthValue=AddLabel(left,"",380,282,92,20,10,accent,true);
            trailWidthBar=MakeBar(left,16,304,460,2,28,cfg.TrailWidth,delegate{if(!ready)return;cfg.TrailWidth=trailWidthBar.Value;RefreshTrail();});
            AddLabel(left,"Intenzitet / vidljivost",19,350,330,20,10,muted,false);
            trailOpacityValue=AddLabel(left,"",380,350,92,20,10,accent,true);
            trailOpacityBar=MakeBar(left,16,372,460,10,100,cfg.TrailOpacity,delegate{if(!ready)return;cfg.TrailOpacity=trailOpacityBar.Value;RefreshTrail();});
            AddLabel(left,"Gustoca tocaka i cestica",19,418,330,20,10,muted,false);
            trailDensityValue=AddLabel(left,"",380,418,92,20,10,accent,true);
            trailDensityBar=MakeBar(left,16,440,460,1,6,cfg.TrailDensity,delegate{if(!ready)return;cfg.TrailDensity=trailDensityBar.Value;RefreshTrail();});

            AddLabel(right,"PREGLED ODABRANOG TRAGA",18,13,439,26,11,text,true);
            trailPreview=new TrailPreviewPanel();trailPreview.Settings=cfg;trailPreview.SetBounds(19,48,438,190);right.Controls.Add(trailPreview);
            chkMatchGlow=new CheckBox();chkMatchGlow.SetBounds(18,251,437,29);
            chkMatchGlow.Text="Boja traga neka prati boju sjaja";chkMatchGlow.Checked=cfg.TrailMatchGlow;
            chkMatchGlow.BackColor=surface;chkMatchGlow.ForeColor=text;right.Controls.Add(chkMatchGlow);
            chkMatchGlow.CheckedChanged+=delegate{if(!ready)return;cfg.TrailMatchGlow=chkMatchGlow.Checked;RefreshTrail();};
            Button chooseColor=MakeButton(right,"ODABERI POSEBNU BOJU TRAGA",19,285,437,32,false);
            chooseColor.Click+=delegate
            {
                using(ColorDialog d=new ColorDialog())
                {
                    d.FullOpen=true;d.Color=ColorTranslator.FromHtml(cfg.TrailColorHex);
                    if(d.ShowDialog(this)==DialogResult.OK){cfg.TrailColorHex=ColorTranslator.ToHtml(d.Color);cfg.TrailMatchGlow=false;chkMatchGlow.Checked=false;RefreshTrail();}
                }
            };
            AddLabel(right,"BRZI PROFILI",19,329,436,20,10,muted,true);
            MakeButton(right,"Diskretni",19,354,209,42,false).Click+=delegate{TrailPreset(0);};
            MakeButton(right,"Gaming neon",240,354,216,42,false).Click+=delegate{TrailPreset(1);};
            MakeButton(right,"Duga kometa",19,407,209,42,false).Click+=delegate{TrailPreset(2);};
            MakeButton(right,"Iskrice",240,407,216,42,false).Click+=delegate{TrailPreset(3);};
            AddLabel(right,"Trag nestaje sam kad mis stane. Efekt ne blokira klikove.",20,464,434,34,9,muted,false);
        }
        private void BuildCursorPage()
        {
            Panel left=CardIn(pageCursor,7,9,494,513);
            Panel right=CardIn(pageCursor,513,9,475,513);
            AddLabel(left,"IZGLED I OBLIK POKAZIVACA",19,12,450,26,11,text,true);
            // PreviewPanel from Glow tab also gets updated for cursors; each page has one preview.
            AddLabel(left,"Odabrani oblik, boja i velicina kursora",18,54,457,25,10,muted,false);
            AddLabel(left,"OBLIK",19,105,451,19,9,muted,true);
            string[] shapes={"Klasican - siroka strelica","Tanki - precizna strelica","Moderni - zaobljeni",
                "Outline - samo obrub","Futuristicki - ostri","Pixel - retro","Dijamant - geometrijski",
                "Krug - precizni nisan","KAO NA SLICI - crni trokut s bijelim rubom"};
            cmbShapes=CreateChoice(left,18,129,457,shapes,Array.IndexOf(CursorManager.Shapes,cfg.CursorShape));
            AddLabel(left,"BOJA KURSORA",19,187,217,22,9,muted,true);
            AddLabel(left,"VELICINA KURSORA",260,187,215,22,9,muted,true);
            cmbCursorColors=CreateChoice(left,18,215,219,new string[]{"Crna","Bijela","Plava","Ljubicasta","Cyan","Zelena"},Array.IndexOf(CursorManager.Colors,cfg.CursorColor));
            cmbCursorSizes=CreateChoice(left,258,215,217,new string[]{"Mala","Normalna","Velika"},Array.IndexOf(CursorManager.Sizes,cfg.CursorSize));
            cmbShapes.SelectedIndexChanged+=delegate{cfg.CursorShape=CursorManager.Shapes[cmbShapes.SelectedIndex];UpdateCursorPreview();if(ready)Persist();};
            cmbCursorColors.SelectedIndexChanged+=delegate{cfg.CursorColor=CursorManager.Colors[cmbCursorColors.SelectedIndex];UpdateCursorPreview();if(ready)Persist();};
            cmbCursorSizes.SelectedIndexChanged+=delegate{cfg.CursorSize=CursorManager.Sizes[cmbCursorSizes.SelectedIndex];UpdateCursorPreview();if(ready)Persist();};
            Button apply=MakeButton(left,"PRIMIJENI ODABRANI KURSOR U WINDOWSU",18,281,457,45,true);
            apply.Click+=delegate
            {
                try {CursorManager.Apply(cfg.CursorShape,cfg.CursorColor,cfg.CursorSize);
                    statusLabel.Text="Primijenjen: "+cmbShapes.Text+" / "+cmbCursorColors.Text+" / "+cmbCursorSizes.Text;Persist();}
                catch(Exception ex){ShowError(ex);}
            };
            AddLabel(left,"SAVJET ZA IZGLED SA SLIKE",19,355,455,24,11,text,true);
            AddLabel(left,"Izaberi KAO NA SLICI, Crna, Normalna. Sjaj stavi na Plava.\nZa trag biraj Meki trag ili Kometa.",19,389,455,78,10,muted,false);
            AddLabel(right,"PREGLED 3 VRSTE POKAZIVACA",18,12,449,26,11,text,true);
            // A second preview exists on the cursor page and becomes the active cursor preview.
            PreviewPanel cursorPreview=new PreviewPanel();cursorPreview.SetBounds(18,46,437,172);cursorPreview.Glow=overlay;right.Controls.Add(cursorPreview);
            preview=cursorPreview;
            AddLabel(right,"PREGLED: strelica  •  ruka  •  tekst",20,229,437,23,10,muted,false);
            AddLabel(right,"ZAMIJENI POJEDINACNI POKAZIVAC",18,281,444,24,11,text,true);
            string[] roleNames={"Obicna strelica","Pomoc (?)","Pokretanje u pozadini","Cekanje / ucitavanje","Krizic","Unos teksta","Olovka","Nije dozvoljeno","Promjena visine","Promjena sirine","Promjena dijagonalno 1","Promjena dijagonalno 2","Pomicanje u svim smjerovima","Strelica gore","Ruka za linkove","Pin lokacija","Pokazivac osoba"};
            cmbRole=CreateChoice(right,18,315,268,roleNames,0);
            Button customCursor=MakeButton(right,"UVEZI CUR/ANI",298,315,156,32,false);
            customCursor.Click+=delegate
            {
                using(OpenFileDialog d=new OpenFileDialog())
                {
                    d.Title="Odaberi vlastiti kursor za: "+cmbRole.Text;
                    d.Filter="Windows kursori (*.cur;*.ani)|*.cur;*.ani";
                    if(d.ShowDialog(this)==DialogResult.OK)
                    {
                        try{CursorManager.ApplyCustomCursor(CursorManager.Roles[cmbRole.SelectedIndex],d.FileName);statusLabel.Text="Zamijenjen pokazivac: "+cmbRole.Text;}
                        catch(Exception ex){ShowError(ex);}
                    }
                }
            };
            Button restore=MakeButton(right,"VRATI PRETHODNE WINDOWS KURSORE",18,376,436,40,false);
            restore.Click+=delegate
            {
                try{CursorManager.Restore();statusLabel.Text="Vraceni Windows kursori iz sigurnosne kopije.";}
                catch(Exception ex){ShowError(ex);}
            };
            AddLabel(right,"Promjene oblika su Windows postavke; ne trebaju biti aktivni\ntragovi ni glow da bi kursor ostao promijenjen.",20,438,437,55,9,muted,false);
            UpdateCursorPreview();
        }
        private void RefreshGlow()
        {
            try
            {
                if(cfg.GlowOn){ if(!overlay.Visible)overlay.Show();overlay.Render(cfg); }
                else overlay.Render(cfg);
                UpdateLabels();if(preview!=null)preview.Invalidate();if(glowPreview!=null)glowPreview.Invalidate();if(trailPreview!=null)trailPreview.Invalidate();Persist();
            }
            catch(Exception ex){ShowError(ex);}
        }
        private void UpdateLabels()
        {
            UpdateTrailLabels();
            sizeValue.Text=cfg.Radius+" px";
            strengthValue.Text=cfg.Intensity+" %";
            softValue.Text=cfg.Softness+" %";
            offsetXValue.Text=cfg.OffsetX+" px";
            offsetYValue.Text=cfg.OffsetY+" px";
        }
        private void Persist()
        {
            try { cfg.Save(); }
            catch(Exception ex) { statusLabel.Text="Postavke nisu spremljene: "+ex.Message; }
        }
        private void ShowError(Exception ex){MessageBox.Show(this,ex.Message,"CursorGlow Studio",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }
}
