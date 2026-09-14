// Copyright (c) 2026 Robert W. McClellan, Matthew J. McClellan
// Licensed under the GNU General Public License v3.0. See LICENSE in the repository root.

using Emgu.CV;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SharedToolbox
{
    public class ImageSupportWPF
    {
        public static int CamWidth, CamHeight, ImWidth, ImHeight, CenterX, CenterY, Zoom;

        public static int MouseDownX, MouseDownY;

        public static bool MouseActive;


        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);
        public static BitmapSource? ToBitmapSource(Mat? mt)
        {
            using (System.Drawing.Bitmap source = mt.ToBitmap())
            {
                IntPtr ptr = source.GetHbitmap();
                BitmapSource? bs = null;
                try
                {
                    bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    DeleteObject(ptr); //release the HBitmap
                }
                return bs;
            }
        }

        public static Mat? PanZoomMat(Mat? mt)
        {
            if (Zoom == 100)
            {
                CenterX = CamWidth >> 1;
                CenterY = CamHeight >> 1;
                return mt;
            }
            double z = Zoom / 100.0;
            int width = (int)(mt!.Cols / z);
            int height = (int)(mt.Rows / z);
            int x = CenterX - (width >> 1);
            int y = CenterY - (height >> 1);
            if (x < 0)   // Image cropping to edge
            {
                x = 0;
                CenterX = width >> 1;
            }
            if (y < 0)
            {
                y = 0;
                CenterY = height >> 1;
            }
            if ((x + width) >= CamWidth)
            {
                x = CamWidth - width - 1;
                CenterX = x + (width >> 1);
            }
            if ((y + height) >= CamHeight)
            {
                y = CamHeight - height - 1;
                CenterY = y + (height >> 1);
            }
            if ((width == 0) || (height == 0))   //  Protect against a crash
            {
                if (x < 0) x = 0;
                if (y < 0) y = 0;
                if (x > (CamWidth - 2)) x = CamWidth - 2;
                if (y > (CamHeight - 2)) x = CamHeight - 2;
                width = 1;
                height = 1;
            }
            Rectangle rect = new Rectangle(x, y, width, height);
            Mat mtNew = new Mat(mt, rect);
            return mtNew;
        }

        public static void UpdateZoom(int z)
        {
            Zoom = z;
        }

        public static BitmapSource FetchPngFile(string fname)
        {
            if (File.Exists(fname))
            {
                using (FileStream stream = new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    PngBitmapDecoder decoder = new PngBitmapDecoder(stream,
                        BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

                    return decoder.Frames[0];
                }
            }
            else
            {
                PixelFormat pf = PixelFormats.Gray8;
                int width = 300;
                int height = 240;

                int rawStride = (width * pf.BitsPerPixel + 7) / 8;
                byte[] rawImage = new byte[rawStride * height];
                Random value = new Random();
                value.NextBytes(rawImage);
                return BitmapSource.Create(width, height, 96, 96, pf, null, rawImage, rawStride);
            }
        }


    }
}