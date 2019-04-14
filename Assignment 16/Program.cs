using System;
using System.Numerics;
using System.Windows.Forms;
using System.Drawing;

//http://zetcode.com/gui/csharpwinforms/introduction/
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;

namespace mandelbrot
{
    class MainClass : Form
    {
        public static void Main(string[] args)
        {
            Application.Run(new MainClass());
        }

        //gui widgets
        Bitmap img;
        TrackBar maxIterTrackbar;
        Label maxIterLabel;
        Label totalTimeLabel;
        ProgressBar progressbar;

        //percent of the way we are through the
        //computation. -1=no computation going on
        int progress=-1;

        //used to periodically update the progress bar.
        //Note that this Timer runs in the GUI thread.
        System.Windows.Forms.Timer timer;

        //for box-drawing
        int[] mouseDownLocation;
        int[] mouseCurrLocation;

        //region of fractal that is being displayed
        double xmin = -2;
        double xmax = 2;
        double ymin = -2;
        double ymax = 2;

        //fractal iterations is 2**maxiter_
        int maxiter_ = 8;

        public MainClass()
        {
            Size = new Size(512, 512);

            Panel imgpanel = new Panel();
            imgpanel.Parent = this;
            imgpanel.Dock = DockStyle.Fill;

            Panel controlpanel = new Panel();
            controlpanel.Parent = this;
            controlpanel.Dock = DockStyle.Top;
            controlpanel.AutoSize = true;

            maxIterTrackbar = new TrackBar();
            maxIterTrackbar.Parent = controlpanel;
            maxIterTrackbar.Size = new Size(100, 5);
            maxIterTrackbar.Location = new Point(150, 4);
            maxIterTrackbar.TickStyle = TickStyle.None;
            maxIterTrackbar.Minimum = 0;
            maxIterTrackbar.Maximum = 20;
            maxIterTrackbar.Value = maxiter_;
            maxIterTrackbar.ValueChanged += new EventHandler((s, e) => {
                maxiter_ = maxIterTrackbar.Value;
                maxIterLabel.Text = ""+(1<<maxiter_); 
            });

            maxIterTrackbar.MouseUp += new MouseEventHandler((s, e) => {
                img=null;
                imgpanel.Refresh();
            });
                
            maxIterLabel = new Label();
            maxIterLabel.Parent = controlpanel;
            maxIterLabel.Location = new Point(250, 4);
            maxIterLabel.Text = ""+(1<<maxiter_);

            progressbar = new ProgressBar();
            progressbar.Parent = controlpanel;
            progressbar.Size = new Size(100, 15);
            progressbar.Location = new Point(4, 4);
            progressbar.Minimum = 0;
            progressbar.Maximum = 100;
            progressbar.Value = 0;
            progressbar.Style = ProgressBarStyle.Continuous;

            totalTimeLabel = new Label();
            totalTimeLabel.Parent = controlpanel;
            totalTimeLabel.Dock = DockStyle.Right;
            totalTimeLabel.Text = "";

            timer = new System.Windows.Forms.Timer();
            timer.Tick += (object sender, EventArgs e) => {
                if( progress < 0 )
                    progressbar.Value = 0;
                else
                    progressbar.Value = progress;
            };
            timer.Interval = 250;   

            MainMenu mbar = new MainMenu();
            MenuItem file = new MenuItem("File");
            MenuItem quit = new MenuItem("Quit", (s, e) => {
                Environment.Exit(0);
            });
            file.MenuItems.Add(quit);
            mbar.MenuItems.Add(file);
            this.Menu = mbar;

            imgpanel.MouseDown += new MouseEventHandler((s, e) => {
                if( e.Button == MouseButtons.Right ){
                    xmin = -2;
                    xmax=2;
                    ymin = -2;
                    ymax=2;
                    img=null;
                    imgpanel.Refresh();
                    return;
                }
                mouseDownLocation = new int[]{e.Location.X,e.Location.Y};
                mouseCurrLocation = new int[]{e.Location.X,e.Location.Y};
            });

            imgpanel.MouseMove += new MouseEventHandler((s, e) => {
                if( mouseDownLocation != null ){
                    mouseCurrLocation = new int[]{e.Location.X,e.Location.Y};
                    double aspect = imgpanel.Size.Height * 1.0 / imgpanel.Size.Width;
                    int w = Math.Abs(mouseDownLocation[0] - mouseCurrLocation[0]);
                    double desiredh = w * aspect;
                    if( mouseCurrLocation[1] < mouseDownLocation[1] ){
                        mouseCurrLocation[1] = (int)(mouseDownLocation[1] - desiredh);
                    } else {
                        mouseCurrLocation[1] = (int)(mouseDownLocation[1] + desiredh);
                    }
                    var g = imgpanel.CreateGraphics();
                    drawFractalImage(g,imgpanel);
                    g.Dispose();
                }
            });

            imgpanel.MouseUp += new MouseEventHandler((s, e) => {
                if( mouseDownLocation == null )
                    return;
                double px1 = (mouseDownLocation[0] ) * 1.0/imgpanel.Size.Width;
                double py1 = (mouseDownLocation[1] ) * 1.0/imgpanel.Size.Height;
                double px2 = (mouseCurrLocation[0] ) * 1.0/imgpanel.Size.Width;
                double py2 = (mouseCurrLocation[1] ) * 1.0/imgpanel.Size.Height;
                double tmp;
                if( px1 > px2 ){
                    tmp = px1;
                    px1=px2;
                    px2=tmp;
                }
                if( py1 > py2 ){
                    tmp=py1;
                    py1=py2;
                    py2=tmp;
                }
                double dx = xmax-xmin;
                double dy = ymax-ymin;
                double xmin_ = xmin + px1 * dx;
                double xmax_ = xmin + px2 * dx;
                double ymin_ = ymin + py1 * dy;
                double ymax_ = ymin + py2 * dy;
                xmin = xmin_;
                ymin = ymin_;
                xmax = xmax_;
                ymax = ymax_;
                mouseDownLocation = null;
                mouseCurrLocation = null;
                img=null;
                imgpanel.Refresh();
            });

            imgpanel.Paint += new PaintEventHandler(async (sender,e) => {
                if( img == null ){
                    await computeImage(imgpanel.Size.Width,imgpanel.Size.Height);
                }
                var g = imgpanel.CreateGraphics();
                drawFractalImage(g,imgpanel);
            });

            CenterToScreen();
        }

        //draw fractal image to the screen as well as the rectangle
        //if the user is dragging it out. This does not refresh
        //the fractal image itself.
        void drawFractalImage(Graphics g, Panel imgpanel){
            g.DrawImage(img,
                0, 0,
                imgpanel.Size.Width,imgpanel.Size.Height);
            if( mouseCurrLocation != null ){
                int x = Math.Min(mouseDownLocation[0], mouseCurrLocation[0]);
                int y = Math.Min(mouseDownLocation[1], mouseCurrLocation[1]);
                int w = Math.Abs(mouseDownLocation[0] - mouseCurrLocation[0]);
                int h = Math.Abs(mouseDownLocation[1] - mouseCurrLocation[1]);

                Pen p = new Pen(Color.Black);
                g.DrawRectangle(p, new Rectangle(x, y, w, h));
                p = new Pen(Color.White);
                g.DrawRectangle(p, new Rectangle(x + 1, y + 1, w - 2, h - 2));
                g.DrawRectangle(p, new Rectangle(x - 1, y - 1, w + 2, h + 2));
            }
        }

        //call compute() to update the fractal image
        //and then copy the data into the bitmap
        async Task computeImage(int w, int h){
            progress = 0;
            var startTime = System.DateTime.Now;
            timer.Start();
            int maxiter = 1 << maxiter_;
            img = new Bitmap(w,h,PixelFormat.Format24bppRgb);
            var bdata = img.LockBits( new Rectangle(0,0,img.Width,img.Height),
                ImageLockMode.ReadWrite,System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            img.UnlockBits(bdata);

            var T = Task.Run(() => {
                return compute(bdata.Width, bdata.Height, bdata.Stride, maxiter);
            });

            var data = await T;
            bdata = img.LockBits(new Rectangle(0, 0, img.Width, img.Height),
                ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Marshal.Copy(data,0,bdata.Scan0,data.Length);
            img.UnlockBits(bdata);
            timer.Stop();
            var endTime = System.DateTime.Now;
            var totalTime = endTime - startTime;
            totalTimeLabel.Text = ""+totalTime.TotalSeconds+" sec";
            progress = -1;
            progressbar.Value = 0;
        }

        //this is where the actual Mandelbrot computation takes place.
        byte[] compute(int w, int h, int stride, int maxiter)
        {
            byte[] data = new byte[h * stride];
            var deltaY = (ymax - ymin) / (double)(h);
            var deltaX = (xmax - xmin) / (double)(w);
            int x, y;
            double px, py;
            byte[] rgb = new byte[3];
            for(y = 0,py = ymin; y < h; y++,py += deltaY) {
                int tmp = 100 * y / h;
                Interlocked.Exchange(ref progress, tmp);
                int idx = y * stride;
                for(x = 0,px = xmin; x < w; x++,px += deltaX) {
                    int iter = iterationsToInfinity(px, py, maxiter);
                    mapColor(iter, maxiter, rgb);
                    data[idx++] = rgb[0];
                    data[idx++] = rgb[1];
                    data[idx++] = rgb[2];
                }
            }
            return data;
        }

        //for point px,py, return the number of iterations it takes
        //to get to infinity.
        static int iterationsToInfinity(double px, double py, int maxiter)
        {
            Complex c = new Complex(px, py);
            Complex z = new Complex(0, 0);
            for(int k = 0; k < maxiter; k++) {
                z = z * z;
                z = z + c;
                if(z.Real * z.Real + z.Imaginary * z.Imaginary > 4) {
                    return k;
                }
            }
            return maxiter;
        }

        //map an iteration count to a color
        static void mapColor(int k, int MAX_ITERS, byte[] rgb)
        {
            // Map a color to an RGB value
            // When k=0, returns red
            // As k approaches MAX_ITERS, the returned color
            // will proceed through orange, yellow, green, blue, and purple
            // If k >= MAX_ITERS, the returned color is black.
            // Returned values: red, green, blue, in the range 0...255
            //N. Schaller's algorithm to map
            //HSV to RGB values.
            //http://www.cs.rit.edu/~ncs/color/t_convert.html

            double s = 0.8;   //saturation
            double v = 0.95;  //value
            double h = k / (double)MAX_ITERS * 360.0 / 60.0;       //hue

            if(h >= 6)
                v = 0;

            int ipart = (int)h;
            double fpart = h - ipart;
            double A = v * (1 - s);
            double B = v * (1 - s * fpart);
            double C = v * (1 - s * (1 - fpart));
            double r, g, b;

            if(ipart == 0) {
                r = v;
                g = C;
                b = A;
            } else if(ipart == 1) {
                r = B;
                g = v;
                b = A;
            } else if(ipart == 2) {
                r = A;
                g = v;
                b = C;
            } else if(ipart == 3) {
                r = A;
                g = B;
                b = v;
            } else if(ipart == 4) {
                r = C;
                g = A;
                b = v;
            } else {
                r = v;
                g = A;
                b = B;
            }
            rgb[0] = (byte)(r * 255);
            rgb[1] = (byte)(g * 255);
            rgb[2] = (byte)(b * 255);
        }
    }
}
