// Copyright (c) 2026 Robert W. McClellan, Matthew J. McClellan
// Licensed under the GNU General Public License v3.0. See LICENSE in the repository root.

using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Drawing;
using System.Text;

namespace Open.IP
{
    public class OpenIP
    {
        public static MCvScalar Black = new MCvScalar(0);
        public static MCvScalar White = new MCvScalar(255);
        public static OpenSafeRectangle FindMaskBoundingBox(Mat? mt)
        {
            if ((mt is null) || (mt.Rows == 0)) return new OpenSafeRectangle("Source Mat is empty");
            try
            {
                Mat mtRowProj = new Mat();
                Mat mtColProj = new Mat();
                CvInvoke.Reduce(mt, mtRowProj, ReduceDimension.SingleRow, ReduceType.ReduceMax, DepthType.Cv8U);
                CvInvoke.Reduce(mt, mtColProj, ReduceDimension.SingleCol, ReduceType.ReduceMax, DepthType.Cv8U);

                VectorOfPoint points = new VectorOfPoint();
                CvInvoke.FindNonZero(mtRowProj, points);
                System.Drawing.Rectangle xbox = points.Size > 0 ? CvInvoke.BoundingRectangle(points) : System.Drawing.Rectangle.Empty;
                points.Dispose();
                points = new VectorOfPoint();
                CvInvoke.FindNonZero(mtColProj, points);
                System.Drawing.Rectangle ybox = points.Size > 0 ? CvInvoke.BoundingRectangle(points) : System.Drawing.Rectangle.Empty;
                points.Dispose();
                System.Drawing.Rectangle box = new System.Drawing.Rectangle(xbox.X, ybox.Y, xbox.Width, ybox.Height);
                return new OpenSafeRectangle(box);
            }
            catch (Exception ex)
            {
                return new OpenSafeRectangle("Exception in SafeRectangle ", ex.Message);
            }
        }
        public static OpenSafeMat ByteArrayToMat(byte[] data, int rows, int cols)
        {
            Mat mt = new Mat();
            try
            {
                mt = new Mat(rows, cols, DepthType.Cv8U, 1);
                mt.SetTo(data);

                return new OpenSafeMat(mt.Clone());
            }
            catch (Exception ex)
            {
                return new OpenSafeMat("Exception in ByteArrayToMat", ex.Message);
            }
            finally
            {
                if (mt != null) mt.Dispose();
            }
        }
        public static OpenSafeMat CreateRoiMat(Mat mtSource, System.Drawing.Rectangle rect)
        {
            Mat mt = new Mat();
            try
            {
                if (mtSource == null) return new OpenSafeMat("Source Mat is null");

                string errString = string.Empty;                
                if (rect.X < 0) errString += " - out of range left";
                if (rect.Y < 0) errString += " - out of range top";
                if ((rect.X + rect.Width > mtSource.Width)) errString += " - out of range right";
                if ((rect.Y + rect.Height > mtSource.Height)) errString += " - out of range bottom";
                if (errString.Length > 0) return new OpenSafeMat("Rectangle error" + errString);

                mt = new Mat(mtSource, rect);
                return new OpenSafeMat(mt.Clone());
            }
            catch (Exception ex)
            {
                return new OpenSafeMat("Exception creating RoiMat", ex.Message);
            }
            finally
            {
                if (mt != null) mt.Dispose();
            }
        }
        public static OpenSafeMat CreateOutlineMat(Mat mtImage, Mat mtSourceMask)
        {
            Mat mt = mtImage.Clone();
            Mat mtMask = new Mat();
            Mat mtThreeChannel = new Mat();
            try
            {
                CvInvoke.Erode(mtSourceMask, mtMask, null, new System.Drawing.Point(1, 1), 1, BorderType.Default, CvInvoke.MorphologyDefaultBorderValue);
                CvInvoke.BitwiseXor(mtSourceMask, mtMask, mtMask);
                CvInvoke.CvtColor(mtMask, mtThreeChannel, Emgu.CV.CvEnum.ColorConversion.Gray2Bgr);
                CvInvoke.BitwiseOr(mt, mtThreeChannel, mt);
                return new OpenSafeMat(mt.Clone());
            }
            catch (Exception ex)
            {
                return new OpenSafeMat("Exception in Create Outline Mat", ex.Message);
            }
            finally
            {
                if (mt != null) mt.Dispose();
                if (mtMask != null) mtMask.Dispose();
                if (mtThreeChannel != null) mtThreeChannel.Dispose();
            }
        }
        public static OpenSafeMat CreateRoiLocationMat(Mat mtSource, System.Drawing.Rectangle rect)
        {
            Mat mt = new Mat();
            try
            {
                mt = new Mat(mtSource.Rows, mtSource.Cols, mtSource.Depth, 1);
                mt.SetTo(Black);
                CvInvoke.Rectangle(mt, rect, White, -1);
                CvInvoke.BitwiseAnd(mt, mtSource, mt);
                return new OpenSafeMat(mt.Clone());
            }
            catch (Exception ex)
            {
                return new OpenSafeMat("Exception creating RoiMat", ex.Message);
            }
            finally
            {
                if (mt != null) mt.Dispose();
            }
        }
        public static OpenSafeListOfInt BinaryMatToListOfInt(Mat mask)
        {
            string str = string.Empty;
            try
            {
                if (mask.IsEmpty || mask.NumberOfChannels != 1 || mask.Depth != DepthType.Cv8U)
                    return new OpenSafeListOfInt("Mask must be a single-channel 8U Mat");
                List<int> list = new List<int>();

                unsafe
                {
                    byte* ptr = (byte*)mask.DataPointer.ToPointer();
                    long step = mask.Step;          // bytes per row

                    int count = 0;
                    bool isForeground = false;      // we start expecting background (0)

                    // COCO order: column-major (top-to-bottom, then left-to-right)
                    for (int x = 0; x < mask.Cols; x++)
                    {
                        for (int y = 0; y < mask.Rows; y++)
                        {
                            bool pixelOn = ptr[y * step + x] != 0;

                            if (pixelOn == isForeground)
                            {
                                count++;
                            }
                            else
                            {
                                // emit the finished run
                                list.Add(count);

                                // start new run
                                isForeground = pixelOn;
                                count = 1;
                            }
                        }
                    }

                    // emit the final run
                    list.Add(count);
                }

                return new OpenSafeListOfInt(list);
            }
            catch (AccessViolationException ex)
            {
                Console.WriteLine("Memory access error: " + ex.Message);
                return new OpenSafeListOfInt("MemoryAccessViolation in MatToByteArray - Program may be unstable, consider closing", ex.Message);
            }
            catch (Exception ex)
            {
                return new OpenSafeListOfInt("Exception in BinaryMaskToListOfInt", ex.Message);
            }
            finally
            {

            }
        }
        public static OpenSafeString BinaryMaskToRleString(Mat mask)
        {
            string str = string.Empty;
            try
            {
                if (mask.IsEmpty || mask.NumberOfChannels != 1 || mask.Depth != DepthType.Cv8U)
                    return new OpenSafeString("Mask must be a single-channel 8U Mat", "");

                // Pre-allocate a reasonable capacity (most masks have far fewer runs than pixels)
                var sb = new StringBuilder(mask.Rows * 16);

                unsafe
                {
                    byte* ptr = (byte*)mask.DataPointer.ToPointer();
                    long step = mask.Step;          // bytes per row

                    int count = 0;
                    bool isForeground = false;      // we start expecting background (0)

                    // COCO order: column-major (top-to-bottom, then left-to-right)
                    for (int x = 0; x < mask.Cols; x++)
                    {
                        for (int y = 0; y < mask.Rows; y++)
                        {
                            bool pixelOn = ptr[y * step + x] != 0;

                            if (pixelOn == isForeground)
                            {
                                count++;
                            }
                            else
                            {
                                // emit the finished run
                                if (sb.Length > 0) sb.Append(' ');
                                sb.Append(count);

                                // start new run
                                isForeground = pixelOn;
                                count = 1;
                            }
                        }
                    }

                    // emit the final run
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(count);
                }

                return new OpenSafeString(sb.ToString());
            }
            catch (Exception ex)
            {
                return new OpenSafeString("Exception in BinaryMaskToRleString", ex.Message);
            }
            finally
            {

            }
        }
        public static OpenSafeByteArray MatBgrToByteArrayRgb(Mat mtSource)
        {
            try
            {
                if (mtSource == null || mtSource.IsEmpty) return new OpenSafeByteArray("Mat is null or empty");
                if (mtSource.NumberOfChannels != 3) return new OpenSafeByteArray("Only 3-channel Mats are supported");
                if (mtSource.Depth != DepthType.Cv8U) return new OpenSafeByteArray("Only 8-bit unsigned Mats are supported");

                int width = mtSource.Width;
                int height = mtSource.Height;

                // Allocate tightly-packed RGB buffer
                byte[] pixels = new byte[width * height * 3];

                if (mtSource.IsContinuous)
                {
                    // Fast path – one big copy + BGR→RGB swap
                    var data = new byte[width * height * 3];
                    System.Runtime.InteropServices.Marshal.Copy(mtSource.DataPointer, data, 0, data.Length);

                    int src = 0;
                    int dst = 0;
                    for (int i = 0; i < width * height; i++)
                    {
                        pixels[dst++] = data[src + 2]; // R
                        pixels[dst++] = data[src + 1]; // G
                        pixels[dst++] = data[src + 0]; // B
                        src += 3;
                    }
                }
                else
                {
                    // Safe path – copy row by row (handles non-continuous Mats)
                    int step = (int)mtSource.Step;          // bytes per row including padding
                    IntPtr ptr = mtSource.DataPointer;

                    unsafe
                    {
                        byte* srcBase = (byte*)ptr;
                        int dst = 0;

                        for (int y = 0; y < height; y++)
                        {
                            byte* row = srcBase + y * step;
                            for (int x = 0; x < width; x++)
                            {
                                pixels[dst++] = row[x * 3 + 2]; // R
                                pixels[dst++] = row[x * 3 + 1]; // G
                                pixels[dst++] = row[x * 3 + 0]; // B
                            }
                        }
                    }
                }
                return new OpenSafeByteArray(pixels);
            }
            catch (AccessViolationException ex)
            {
                Console.WriteLine("Memory access error: " + ex.Message);
                return new OpenSafeByteArray("MemoryAccessViolation in MatToByteArray - Program may be unstable, consider closing", ex.Message);
            }
            catch (Exception ex)
            {
                return new OpenSafeByteArray("Exception in MatToByteArray", ex.Message);
            }
            finally
            {

            }
        }
        public static OpenSafeMat ListOfIntToBinaryMask(List<int> list, int rows, int cols, byte intensity = 255)
        {
            Mat mt = new Mat(rows, cols, DepthType.Cv8U, 1);
            try
            {

                mt.SetTo(Black);

                int total = rows * cols;
                int aTotal = list.Sum();
                if (total != aTotal) return new OpenSafeMat("In RleStringToBinaryMask - Total size of Mat does not match total pixel count in rleString");

                int pos = 0;
                unsafe
                {
                    byte* ptr = (byte*)mt.DataPointer.ToPointer();
                    long step = mt.Step;

                    for (int i = 0; i < list.Count(); i++)
                    {
                        if ((i & 1) == 1)   // inside the isForeground block:
                        {
                            for (int j = 0; j < list[i]; j++)
                            {
                                int x = (pos + j) / rows;
                                int y = (pos + j) % rows;
                                ptr[y * step + x] = intensity;
                            }
                        }
                        pos += list[i];
                    }
                }

                return new OpenSafeMat(mt.Clone());
            }
            catch (AccessViolationException ex)
            {
                Console.WriteLine("Memory access error: " + ex.Message);
                return new OpenSafeMat("MemoryAccessViolation in RleStringToBinaryMask - Program may be unstable, consider closing", ex.Message);
            }
            catch (Exception ex)
            {
                return new OpenSafeMat("Exception in BinaryMaskToRleString", ex.Message);
            }
            finally
            {
                if (mt != null) mt.Dispose();
            }
        }
        public static OpenSafeMat RleStringToBinaryMask(string rleString, int rows, int cols)
        {
            Mat mt = new Mat(rows, cols, DepthType.Cv8U, 1);
            try
            {

                mt.SetTo(Black);

                int total = rows * cols;
                int[] rlearray = OpenIP.ParseRleCounts(rleString);
                int aTotal = rlearray.Sum();
                if (total != aTotal) return new OpenSafeMat("In RleStringToBinaryMask - Total size of Mat does not match total pixel count in rleString");

                int pos = 0;
                unsafe
                {
                    byte* ptr = (byte*)mt.DataPointer.ToPointer();
                    long step = mt.Step; 
                   
                    for (int i = 0; i < rlearray.Length; i++)
                    {
                        if ((i & 1) == 1)   // inside the isForeground block:
                        {
                            for (int j = 0; j < rlearray[i]; j++)
                            {
                                int x = (pos+j) / rows;
                                int y = (pos+j) % rows;
                                ptr[y * step + x] = 255;
                            }                            
                        }
                        pos += rlearray[i];
                    }
                }

                return new OpenSafeMat(mt.Clone());
            }
            catch (AccessViolationException ex)
            {
                Console.WriteLine("Memory access error: " + ex.Message);
                return new OpenSafeMat("MemoryAccessViolation in RleStringToBinaryMask - Program may be unstable, consider closing", ex.Message);
            }
            catch (Exception ex)
            {
                return new OpenSafeMat("Exception in BinaryMaskToRleString", ex.Message);
            }
            finally
            {
                if (mt != null) mt.Dispose();
            }
        }
        public static int[] ParseRleCounts(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            // Most numbers are short; this is only an initial capacity estimate.
            var values = new List<int>(text.Length / 3);

            int value = 0;
            bool inNumber = false;

            foreach (char c in text)
            {
                if ((uint)(c - '0') <= 9)
                {
                    checked
                    {
                        value = value * 10 + (c - '0');
                    }

                    inNumber = true;
                }
                else if (c == ' ')
                {
                    if (inNumber)
                    {
                        values.Add(value);
                        value = 0;
                        inNumber = false;
                    }
                }
                else
                {
                    throw new FormatException(
                        $"Expected a digit or space; found '{c}'.");
                }
            }

            if (inNumber)
                values.Add(value);

            return values.ToArray();
        }
        public static List<int> DecodeCompressedCounts(string counts)
        {
            var runs = new List<int>();
            int index = 0;

            while (index < counts.Length)
            {
                int value = 0;
                int shift = 0;
                int character;

                do
                {
                    if (index >= counts.Length)
                        throw new FormatException("Truncated COCO compressed RLE.");

                    character = counts[index++] - 48;

                    if (character < 0 || character > 63)
                        throw new FormatException("Invalid COCO compressed RLE character.");

                    value |= (character & 0x1F) << shift;
                    shift += 5;
                }
                while ((character & 0x20) != 0);

                // Sign extension: COCO uses a signed variable-length representation.
                if ((character & 0x10) != 0)
                    value |= -1 << shift;

                // Starting with run #3, counts are delta-coded against run #1
                // (zero-based: i > 2 uses i - 2).
                if (runs.Count > 2)
                    value += runs[runs.Count - 2];

                if (value < 0)
                    throw new FormatException("Negative RLE run length.");

                runs.Add(value);
            }

            return runs;
        }
        public static OpenSafeListOfInt DecodeRleCompression(string str)
        {
            List<int> list = new List<int>();
            try
            {
                int index = 0;

                while (index < str.Length)
                {
                    int value = 0;
                    int shift = 0;
                    int character;

                    do
                    {
                        if (index >= str.Length) return new OpenSafeListOfInt("Format Error - Truncated COCO compressed RLE.");

                        character = str[index++] - 48;

                        if (character < 0 || character > 63) return new OpenSafeListOfInt("Format Error - Invalid COCO compressed RLE character.");

                        value |= (character & 0x1F) << shift;
                        shift += 5;
                    }
                    while ((character & 0x20) != 0);

                    // Sign extension: COCO uses a signed variable-length representation.
                    if ((character & 0x10) != 0)
                        value |= -1 << shift;

                    // Starting with run #3, counts are delta-coded against run #1
                    // (zero-based: i > 2 uses i - 2).
                    if (list.Count > 2)
                        value += list[list.Count - 2];

                    if (value < 0) return new OpenSafeListOfInt("Format Error - Negative RLE run length.");

                    list.Add(value);
                }

                return new OpenSafeListOfInt(list);
            }
            catch (Exception ex)
            {
                return new OpenSafeListOfInt("Exception in DecodeRleCompression", ex.Message);
            }
            finally
            {

            }
        }
        public static OpenSafeString RleCompress(List<int> counts)
        {
            string str = string.Empty;
            try
            {
                if (counts == null || counts.Count == 0)
                    return new OpenSafeString("In RleCompress, the source List of int is null or empty");

                var sb = new StringBuilder(4000);

                for (int i = 0; i < counts.Count; i++)
                {
                    // Delta encoding: from the 3rd value onward, store difference from the value two steps back
                    long x = counts[i];
                    if (i > 2)
                        x -= counts[i - 2];

                    bool more = true;
                    while (more)
                    {
                        // Take lowest 5 bits
                        int c = (int)(x & 0x1F);
                        x >>= 5;

                        // Continuation bit logic (same as original C code)
                        more = (c & 0x10) != 0 ? x != -1 : x != 0;

                        if (more)
                            c |= 0x20;          // set continuation bit

                        c += 48;                // map to printable ASCII range '0'–'o' (48–111)
                        sb.Append((char)c);
                    }
                }

                return new OpenSafeString(true, sb.ToString());
            }
            catch (Exception ex)
            {
                return new OpenSafeString("Exception in RleCompress", ex.Message);
            }
            finally
            {

            }
        }
        public static OpenSafeRectangle ListDoubleToRectangle(List<double> list)
        {
            try
            {
                if (list.Count != 4) return new OpenSafeRectangle("Bounding box does not represent a retangle");
                int x = (int) Math.Round(list[0]);
                int y = (int) Math.Round(list[1]);
                int w = (int) Math.Round(list[2]);
                int h = (int) Math.Round(list[3]);
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(x,y,w,h);
                return new OpenSafeRectangle(rect);
            }
            catch (Exception ex)
            {
                return new OpenSafeRectangle("Exception in ListDoubleToRectangle", ex.Message);
            }
            finally
            {

            }
        }

        public static OpenSafeMat DummyTemplate()
        {
            Mat mt = new Mat();
            try
            {

                return new OpenSafeMat(mt.Clone());
            }
            catch (Exception ex)
            {
                return new OpenSafeMat("Exception in Dummy Template", ex.Message);
            }
            finally
            {
                if (mt != null) mt.Dispose();
            }
        }

        public static Rectangle AddPaddingToRect(System.Drawing.Rectangle displayedRect, int rows, int cols, int v)
        {
            int left = Math.Max(displayedRect.X - v, 0);
            int top = Math.Max(displayedRect.Y - v, 0);
            int width = Math.Min(displayedRect.Width + 2 * v, cols - left);
            int height = Math.Min(displayedRect.Height + 2 * v, rows - top);
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(left, top, width, height);
            return rect;
        }

        #region ByteArrayToMat (Forked from ApexIP)

        /// <summary>
        /// Convert byte[] to a Mat />.
        /// </summary>
        public static Mat ByteArrayToMat(byte[] data)
        {
            Mat mt = new Mat();
            CvInvoke.Imdecode(data, ImreadModes.ColorBgr, mt);
            return mt;
        }

        #endregion

        #region RemoveSmallContours (Forked from ApexIP)

        /// <summary>
        /// Creates a mask containing contours whose area exceeds a specified fraction
        /// of the largest detected contour.
        /// </summary>
        /// <param name="mtSource">
        /// Source binary image used to detect contours. Nonzero regions are treated as blobs.
        /// </param>
        /// <param name="minimumPercentOfLargestBlob">
        /// Minimum area ratio, relative to the largest contour, required for a blob to be retained.
        /// For example, <c>0.10</c> retains blobs larger than 10% of the largest blob.
        /// </param>
        /// <returns>
        /// An <see cref="OpenSafeMat"/> containing a single-channel mask with retained blobs drawn in white.
        /// Returns an error-state <see cref="OpenSafeMat"/> if the largest contour has zero area.
        /// </returns>
        /// <remarks>
        /// The largest contour is always retained. All other contours are retained only when their
        /// normalized area is strictly greater than <paramref name="minimumPercentOfLargestBlob"/>.
        /// </remarks>
        public static OpenSafeMat RemoveSmallContours(Mat mtSource, double minimumPercentOfLargestBlob)
        {
            PriorityQueue<int, double> pq = new PriorityQueue<int, double>();
            Mat mtMask = new Mat(mtSource.Rows, mtSource.Cols, mtSource.Depth, 1);
            mtMask.SetTo(Black);

            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(mtSource, contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);

                for (int k = 0; k < contours.Size; k++)
                {
                    double darea = CvInvoke.ContourArea(contours[k], true);
                    pq.Enqueue(k, darea);
                }
                int kk = 0;
                double da = 0;
                double biggestArea = 0;
                bool firstContour = true;
                while (pq.TryDequeue(out kk, out da))
                {
                    if (firstContour)
                    {
                        biggestArea = -da;
                        if (biggestArea <= 0)
                        {
                            mtMask.Dispose();
                            return new OpenSafeMat("Largest contour in RemoveSmallBlobsFromMat has no area");
                        }
                        CvInvoke.DrawContours(mtMask, contours, kk, White, -1);
                        firstContour = false;
                    }
                    else
                    {
                        if (da > -1) break; // only lakes remain
                        double area = -da / biggestArea;
                        if (area > minimumPercentOfLargestBlob)
                        {
                            CvInvoke.DrawContours(mtMask, contours, kk, White, -1);
                        }
                    }
                }
            }
            CvInvoke.BitwiseAnd(mtMask, mtSource, mtMask);
            return new OpenSafeMat(mtMask);
        }
        #endregion

        #region EditDetection (Forked from ApexIP)

        /// <summary>
        /// Edits a binary detection mask by adding or subtracting freehand strokes.
        /// Strokes are scaled from the given coordinate system into the mask rectangle.
        /// Left-mouse strokes are added; other strokes are subtracted.
        /// </summary>
        /// <param name="mtMask">Source mask to edit.</param>
        /// <param name="rect">Target rectangle in mask coordinates.</param>
        /// <param name="bsWidth">Horizontal scale factor used to map stroke coordinates.</param>
        /// <param name="bsHeight">Vertical scale factor used to map stroke coordinates.</param>
        /// <param name="points">Collection of line segments that form the user strokes.</param>
        /// <returns>
        /// An <see cref="OpenSafeMat"/> containing the edited mask on success,
        /// or an error message if the operation fails.
        /// </returns>
        /// 
        public static OpenSafeMat EditDetection(Mat mtMask, System.Drawing.Rectangle rect, double bsWidth, double bsHeight,
          List<OpenPoints> points)
        {
            Mat mt = mtMask.Clone();
            Mat mtAdd = new Mat(mtMask.Rows, mtMask.Cols, mtMask.Depth, 1);
            Mat mtSubtract = new Mat(mtMask.Rows, mtMask.Cols, mtMask.Depth, 1);
            try
            {
                mtAdd.SetTo(Black);
                mtSubtract.SetTo(Black);
                int lastX = -1;
                int lastY = -1;
                int who = -1;
                bool isAddition = false;
                List<System.Drawing.Point> listPoints = new List<System.Drawing.Point>();
                for (int i = 0; i < points.Count; i++)
                {
                    int x = (int)Math.Round(points[i].X / bsWidth * rect.Width) + rect.X;
                    int y = (int)Math.Round(points[i].Y / bsHeight * rect.Height) + rect.Y;
                    if ((points[i].Index != who) || (i == (points.Count() - 1)))
                    {
                        if (i == (points.Count() - 1))
                        {
                            if ((lastX != x) || (lastY != y)) listPoints.Add(new System.Drawing.Point(x, y));
                        }
                        who = points[i].Index;
                        bool wasAddition = isAddition;
                        isAddition = points[i].IsLeftMouse;
                        if (listPoints.Count > 0)
                        {
                            System.Drawing.Point[] pointArray = listPoints.ToArray();
                            using (var contours = new VectorOfVectorOfPoint())
                            {
                                using (var vp = new VectorOfPoint(pointArray))
                                {
                                    contours.Push(vp);
                                }
                                if (wasAddition)
                                {
                                    CvInvoke.DrawContours(mtAdd, contours, 0, White, -1);
                                }
                                else
                                {
                                    CvInvoke.DrawContours(mtSubtract, contours, 0, White, -1);
                                }
                            }
                            listPoints.Clear();
                        }
                    }
                    if ((lastX != x) || (lastY != y)) listPoints.Add(new System.Drawing.Point(x, y));
                    lastX = x;
                    lastY = y;
                }
                CvInvoke.BitwiseOr(mt, mtAdd, mt);  // Add additions
                CvInvoke.BitwiseNot(mtSubtract, mtSubtract);
                CvInvoke.BitwiseAnd(mt, mtSubtract, mt);  // crop subtractions
                return new OpenSafeMat(mt.Clone());
            }
            catch (Exception ex)
            {
                return new OpenSafeMat("Exception in Edit Detection", ex.Message);
            }
            finally
            {
                if (mt != null) mt.Dispose();
                if (mtAdd != null) mtAdd.Dispose();
                if (mtSubtract != null) mtSubtract.Dispose();
            }
        }

        #region EditDetectionMain

        /// <summary>
        /// Edits a binary detection mask by adding freehand strokes.
        /// Left-mouse strokes are added
        /// </summary>
        /// <param name="mtMask">Source mask to added to</param>
        /// <param name="bsWidth">Horizontal scale factor used to map stroke coordinates.</param>
        /// <param name="bsHeight">Vertical scale factor used to map stroke coordinates.</param>
        /// <param name="points">Collection of line segments that form the user strokes.</param>
        /// <returns>
        /// An <see cref="OpenSafeMat"/> containing the edited mask on success,
        /// or an error message if the operation fails.
        /// </returns>
        /// 
        public static OpenSafeMat EditDetectionMain(Mat mtMask, double bsWidth, double bsHeight, List<OpenPoints> points)
        {
            Mat mt = mtMask.Clone();
            Mat mtAdd = new Mat(mtMask.Rows, mtMask.Cols, mtMask.Depth, 1);
            try
            {
                mtAdd.SetTo(Black);
                int lastX = -1;
                int lastY = -1;
                int who = -1;
                List<System.Drawing.Point> listPoints = new List<System.Drawing.Point>();
                for (int i = 0; i < points.Count; i++)
                {
                    int x = (int)Math.Round(points[i].X * mtMask.Cols / bsWidth);
                    int y = (int)Math.Round(points[i].Y * mtMask.Rows / bsHeight);
                    if ((points[i].Index != who) || (i == (points.Count() - 1)))
                    {
                        if (i == (points.Count() - 1))
                        {
                            if ((lastX != x) || (lastY != y)) listPoints.Add(new System.Drawing.Point(x, y));
                        }
                        who = points[i].Index;
                        if (listPoints.Count > 0)
                        {
                            System.Drawing.Point[] pointArray = listPoints.ToArray();
                            using (var contours = new VectorOfVectorOfPoint())
                            {
                                using (var vp = new VectorOfPoint(pointArray))
                                {
                                    contours.Push(vp);
                                }
                                CvInvoke.DrawContours(mtAdd, contours, 0, White, -1);
                            }
                            listPoints.Clear();
                        }
                    }
                    if ((lastX != x) || (lastY != y)) listPoints.Add(new System.Drawing.Point(x, y));
                    lastX = x;
                    lastY = y;
                }
                CvInvoke.BitwiseNot(mt, mt);   // Only unused areas are allowed to be added to
                CvInvoke.BitwiseAnd(mt, mtAdd, mtAdd);  // Add the new contour minus overlap with existing mask
                return new OpenSafeMat(mtAdd.Clone());
            }
            catch (Exception ex)
            {
                return new OpenSafeMat("Exception in EditDetectionMain", ex.Message);
            }
            finally
            {
                if (mt != null) mt.Dispose();
                if (mtAdd != null) mtAdd.Dispose();
            }
        }

        #endregion

        #endregion

        #region ExactThreshold (Forked from ApexIP)

        /// <summary>
        /// Thesholds an image using an exact threshold value. Pixels with values equal to the threshold are retained.
        /// </summary>
        /// <param name="mtSource">Source mask to threshold.</param>
        /// <param name="threshold">Integer value that must be matched exacly to be retained</param>
        /// <returns>
        /// An <see cref="OpenSafeMat"/> containing the edited mask on success,
        /// or an error message if the operation fails.
        /// </returns>
        /// 
        public static OpenSafeMat ExactThreshold(Mat mtSource, int threshold)
        {
            Mat mtHigh = new Mat(mtSource.Rows, mtSource.Cols, mtSource.Depth, 1);
            Mat mtLow = new Mat(mtSource.Rows, mtSource.Cols, mtSource.Depth, 1);
            try
            {
                CvInvoke.Threshold(mtSource, mtHigh, threshold - 1, 255, ThresholdType.Binary);
                CvInvoke.Threshold(mtSource, mtLow, threshold, 255, ThresholdType.BinaryInv);
                CvInvoke.BitwiseAnd(mtHigh, mtLow, mtHigh);
                return new OpenSafeMat(mtHigh.Clone());
            }
            catch (Exception ex)
            {
                return new OpenSafeMat("Exception in ExactThreshold", ex.Message);
            }
            finally
            {
                if (mtLow != null) mtLow.Dispose();
            }
        }

        #endregion

        #region SplitDetection (Forked from ApexIP)

        /// <summary>
        /// Splits a binary detection mask by masking a segmentation split line and finding new contours.
        /// Strokes are scaled from the given coordinate system into the mask rectangle.
        /// Right-mouse strokes determine the split line.
        /// Output is 1 mask with one ROI with a 255 value and the other ROI with a 127 value.
        /// Smaller contours are ignored. These should be added to a contiguous segmentation before split if they are desired
        /// </summary>
        /// <param name="mtMask">Source mask to edit.</param>
        /// <param name="rect">Target rectangle in mask coordinates.</param>
        /// <param name="bsWidth">Horizontal scale factor used to map stroke coordinates.</param>
        /// <param name="bsHeight">Vertical scale factor used to map stroke coordinates.</param>
        /// <param name="points">Collection of line segments that form the user strokes.</param>
        /// <returns>
        /// An <see cref="OpenSafeMat"/> containing the edited mask on success,
        /// or an error message if the operation fails.
        /// </returns>
        /// 

        public static OpenSafeMat SplitDetection(Mat mtMask, System.Drawing.Rectangle rect, double bsWidth, double bsHeight,
          List<OpenPoints> points)
        {
            Mat mt = mtMask.Clone();
            Mat mtSplit = new Mat(mtMask.Rows, mtMask.Cols, mtMask.Depth, 1);
            Mat mtWork = new Mat(mtMask.Rows, mtMask.Cols, mtMask.Depth, 1);
            try
            {
                mtSplit.SetTo(Black);
                int lastX = -1;
                int lastY = -1;
                List<System.Drawing.Point> listPoints = new List<System.Drawing.Point>();
                for (int i = 0; i < points.Count; i++)
                {
                    int x = (int)Math.Round(points[i].X / bsWidth * rect.Width) + rect.X;
                    int y = (int)Math.Round(points[i].Y / bsHeight * rect.Height) + rect.Y;
                    if (i == (points.Count() - 1))
                    {
                        listPoints.Add(new System.Drawing.Point(x, y));
                        if (listPoints.Count > 0)
                        {
                            System.Drawing.Point[] pointArray = listPoints.ToArray();
                            using (var contours = new VectorOfVectorOfPoint())
                            {
                                using (var vp = new VectorOfPoint(pointArray))
                                {
                                    contours.Push(vp);
                                }
                                CvInvoke.Polylines(mtSplit, contours, false, White, 3);
                            }
                            listPoints.Clear();
                        }
                    }
                    if ((lastX != x) || (lastY != y)) listPoints.Add(new System.Drawing.Point(x, y));
                    lastX = x;
                    lastY = y;
                }
                CvInvoke.BitwiseNot(mtSplit, mtSplit);
                CvInvoke.BitwiseAnd(mt, mtSplit, mtSplit);   //  We should have split contours
                using (var contours = new VectorOfVectorOfPoint())
                {
                    mt.SetTo(Black);
                    PriorityQueue<int, double> pq = new PriorityQueue<int, double>();
                    CvInvoke.FindContours(mtSplit, contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);

                    for (int k = 0; k < contours.Size; k++)
                    {
                        double darea = CvInvoke.ContourArea(contours[k], true);
                        pq.Enqueue(k, darea);
                    }
                    int kk = 0;
                    double da = 0;
                    bool firstContour = true;
                    while (pq.TryDequeue(out kk, out da))
                    {
                        if (firstContour)
                        {
                            mtWork.SetTo(Black);
                            CvInvoke.DrawContours(mtWork, contours, kk, White, -1);
                            CvInvoke.Dilate(mtWork, mtWork, null, new System.Drawing.Point(1, 1), 1, BorderType.Default, new MCvScalar(0));
                            CvInvoke.BitwiseAnd(mtWork, mtMask, mtWork);   //  Keep holes in contour if they exist
                            CvInvoke.BitwiseOr(mt, mtWork, mt);
                            firstContour = false;
                            continue;
                        }
                        else  // Second contour is faded a bit to show it is a separate contour
                        {
                            mtWork.SetTo(Black);
                            CvInvoke.DrawContours(mtWork, contours, kk, White, -1);
                            CvInvoke.Dilate(mtWork, mtWork, null, new System.Drawing.Point(1, 1), 1, BorderType.Default, new MCvScalar(0));
                            CvInvoke.BitwiseAnd(mtWork, mtMask, mtWork);   //  Keep holes in contour if they exist
                            CvInvoke.Threshold(mtWork, mtWork, 0, 127, ThresholdType.Binary);
                            CvInvoke.BitwiseOr(mt, mtWork, mt);
                            break;
                        }
                    }
                }
                return new OpenSafeMat(mt.Clone());
            }
            catch (Exception ex)
            {
                return new OpenSafeMat("Exception in Split Detection", ex.Message);
            }
            finally
            {
                if (mt != null) mt.Dispose();
                if (mtSplit != null) mtSplit.Dispose();
                if (mtWork != null) mtWork.Dispose();
            }
        }

        #endregion
    }

    #region OpenPoints

    /// <summary>
    /// Represents a single point in a freehand stroke, including mouse-button state and stroke index.
    /// </summary>
    public class OpenPoints
    {
        /// <summary>True if the point was captured with the left mouse button (addition stroke).</summary>
        public bool IsLeftMouse { get; set; }

        /// <summary>Stroke index this point belongs to.</summary>
        public int Index { get; set; }

        /// <summary>X-coordinate of the point.</summary>
        public double X { get; set; }

        /// <summary>Y-coordinate of the point.</summary>
        public double Y { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenPoints"/> class.
        /// </summary>
        /// <param name="isLeftMouse">True if the point was captured with the left mouse button.</param>
        /// <param name="index">Stroke index.</param>
        /// <param name="x">X-coordinate.</param>
        /// <param name="y">Y-coordinate.</param>
        public OpenPoints(bool isLeftMouse, int index, double x, double y)
        {
            IsLeftMouse = isLeftMouse;
            Index = index;
            X = x;
            Y = y;
        }
    }


    #endregion

    #region OpenSafeResultBase (non-generic)
    /// <summary>
    /// Non-generic base that holds the status / message members.
    /// Use this type when you only care about success/failure and logging.
    /// </summary>
    public abstract class OpenSafeResultBase
    {
        public bool IsSuccess;
        public bool IsException = false;
        public string Message = string.Empty;
        public string ExMessage = string.Empty;

        protected OpenSafeResultBase() { }

        protected OpenSafeResultBase(string mess = "")
        {
            Message = mess;
        }

        protected OpenSafeResultBase(string message, string exMess)
        {
            IsSuccess = false;
            IsException = true;
            Message = message;
            ExMessage = exMess;
        }

    }
    #endregion

    #region OpenSafeResult<T>
    public class OpenSafeResult<T> : OpenSafeResultBase
    {
        public T Value;

        public OpenSafeResult() : base()
        {
            IsSuccess = false;
            Value = default!;
        }

        public OpenSafeResult(T value) : base()
        {
            IsSuccess = true;
            Value = value;
        }

        public OpenSafeResult(bool isSuccess, T value) : base()
        {
            IsSuccess = isSuccess;
            Value = value;
        }

        public OpenSafeResult(string mess = "") : base(mess)
        {
            Value = default!;
        }

        public OpenSafeResult(string message, string exMess) : base(message, exMess)
        {
            Value = default!;
        }
    }
    #endregion

    #region Concrete OpenSafe classes 

    public class OpenSafeBool : OpenSafeResult<bool>
    {
        public bool isGood
        {
            get => Value;
            set => Value = value;
        }

        public OpenSafeBool() : base() { }
        public OpenSafeBool(bool isGood) : base(isGood) { }
        public OpenSafeBool(string mess = "") : base(mess) { }
        public OpenSafeBool(string message, string exMess) : base(message, exMess) { }
    }
    public class OpenSafeByteArray : OpenSafeResult<byte[]>
    {
        public byte[] ByteArray
        {
            get => Value;
            set => Value = value;
        }

        public OpenSafeByteArray() : base() { }
        public OpenSafeByteArray(byte[] byteArray) : base(byteArray) { }
        public OpenSafeByteArray(string mess = "") : base(mess) { }
        public OpenSafeByteArray(string message, string exMess) : base(message, exMess) { }
    }

    public class OpenSafeListOfInt : OpenSafeResult<List<int>>
    {
        public List<int> ListOfInt
        {
            get => Value;
            set => Value = value;
        }

        public OpenSafeListOfInt() : base() { }
        public OpenSafeListOfInt(List<int> list) : base(list) { }
        public OpenSafeListOfInt(string mess = "") : base(mess) { }
        public OpenSafeListOfInt(string message, string exMess) : base(message, exMess) { }
    }

    public class OpenSafeMat : OpenSafeResult<Mat>
    {
        public Mat Mt
        {
            get => Value;
            set => Value = value;
        }

        public OpenSafeMat() : base() { }
        public OpenSafeMat(Mat mt) : base(mt) { }
        public OpenSafeMat(string mess = "") : base(mess) { }
        public OpenSafeMat(string message, string exMess) : base(message, exMess) { }
    }

    public class OpenSafeRectangle : OpenSafeResult<System.Drawing.Rectangle>
    {
        public System.Drawing.Rectangle Rect
        {
            get => Value;
            set => Value = value;
        }

        public OpenSafeRectangle() : base() { }
        public OpenSafeRectangle(System.Drawing.Rectangle rect) : base(rect) { }
        public OpenSafeRectangle(string mess = "") : base(mess) { }
        public OpenSafeRectangle(string message, string exMess) : base(message, exMess) { }
    }

    public class OpenSafeString : OpenSafeResult<String>
    {
        public string Str
        {
            get => Value;
            set => Value = value;
        }

        public OpenSafeString() : base() { }
        public OpenSafeString(string str) : base(str) { }
        public OpenSafeString(bool val, string mess = "") : base(val, mess) { }   //  Special for string to avoid type conflicts
        public OpenSafeString(string message, string exMess) : base(message, exMess) { }
    }
    #endregion

}
