using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    /// <summary>
    /// 座標指定はBitmapフォーマットに合わせて下から上に増加でいいかな？
    /// </summary>
    class SmallCanvas
    {
        double posx = 0;
        double posy = 0;
        uint[,] data;  // 忘れられがちだが、C#には多次元配列が存在する
        uint penColor = 0x000000u;
        int w = 1;
        int h = 1;

        public SmallCanvas(int width, int height, uint bgColorRGB)
        {
            data = new uint[height, width];
            w = width;
            h = height;

            bgColorRGB &= 0x00FFFFFFu;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    data[y, x] = bgColorRGB;
                }
            }
        }

        public void MoveTo(double x, double y)
        {
            posx = x;
            posy = y;
        }

        public void LineTo(double x, double y)
        {
            double dx = x - posx;
            double dy = y - posy;
            double initx = posx;
            double inity = posy;


            if (Math.Abs(dx) >= Math.Abs(dy))
            {
                if (dx < 0)
                {
                    dx = -dx; dy = -dy;
                    initx = x; inity = y;
                }

                int minI = Math.Max(0, (int)Math.Ceiling(initx));
                int maxI = Math.Min(w - 1, (int)Math.Floor(initx + dx));
                double slope = (y - posy) / (x - posx);
                for (int i = minI; i <= maxI; i++)
                {
                    int ploty = (int)Math.Round(inity + (i - initx) * slope);
                    if (0 <= ploty && ploty <= h - 1)
                    {
                        data[ploty, i] = penColor;
                    }
                }
            }
            else
            {
                if (dy < 0)
                {
                    dx = -dx; dy = -dy;
                    initx = x; inity = y;
                }

                int minI = Math.Max(0, (int)Math.Ceiling(inity));
                int maxI = Math.Min(h - 1, (int)Math.Floor(inity + dy));
                double slope = (x - posx) / (y - posy);
                for (int i = minI; i <= maxI; i++)
                {
                    int plotx = (int)Math.Round(initx + (i - inity) * slope);
                    if (0 <= plotx && plotx <= w - 1)
                    {
                        data[i, plotx] = penColor;
                    }
                }
            }

            posx = x;
            posy = y;
        }

        public void Plot2d(double xmin, double xmax, double ymin, double ymax, Func<double, double> f)
        {
            double xscale = (xmax - xmin) / w;
            double yscale = h / (ymax - ymin);

            int i = 0;
            {
                double x2 = i * xscale + xmin;
                double y2 = f(x2);
                MoveTo(i, (y2 - ymin) * yscale);
            }
            for (i = 1; i < w; i++)
            {
                double x2 = i * xscale + xmin;
                double y2 = f(x2);
                LineTo(i, (y2 - ymin) * yscale);
            }
        }
        /// <summary>
        /// ボーデ線図
        /// </summary>
        public void PlotBode(double xmin, double xmax, double ymin_dB, double ymax_dB, Func<double, double> f)
        {
            double xminE = Math.Log(xmin);
            double xmaxE = Math.Log(xmax);

            double xscale = (xmaxE - xminE) / w;
            double yscale = h / (ymax_dB - ymin_dB);

            int i = 0;
            {
                double x2 = i * xscale + xminE;
                double y2 = 20 * Math.Log10(f(Math.Exp(x2)));
                MoveTo(i, (y2 - ymin_dB) * yscale);
            }
            for (i = 1; i < w; i++)
            {
                double x2 = i * xscale + xminE;
                double y2 = 20 * Math.Log10(f(Math.Exp(x2)));
                LineTo(i, (y2 - ymin_dB) * yscale);
            }
        }
        /// <summary>
        /// ボーデ線図
        /// </summary>
        public void PlotBodeFill(double xmin, double xmax, double ymin_dB, double ymax_dB, Func<double, double> f)
        {
            double xminE = Math.Log(xmin);
            double xmaxE = Math.Log(xmax);

            double xscale = (xmaxE - xminE) / w;
            double yscale = h / (ymax_dB - ymin_dB);

            for (int i = 0; i < w; i++)
            {
                double x2 = i * xscale + xminE;
                double y2 = 20 * Math.Log10(f(Math.Exp(x2)));
                MoveTo(i, -1);
                LineTo(i, (y2 - ymin_dB) * yscale);
            }
        }

        public void Export(Stream str)
        {
            BitmapWriter bw = new BitmapWriter(str, w, h);
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    bw.Write(data[i, j]);
                }
                bw.WriteNewLine();
            }
            bw.Close();
        }
    }
}
