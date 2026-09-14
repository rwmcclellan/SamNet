// Copyright (c) 2026 Robert W. McClellan, Matthew J. McClellan
// Licensed under the GNU General Public License v3.0. See LICENSE in the repository root.

using Open.IP;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using SamNet.Native;
using static SamNet.Native.SamNative;

namespace SamNet
{
    public static class SamModel
    {
        public static int CurrentPage = 1;
        public static string OriginalImageName = string.Empty;
        public static Mat mtImage = new Mat();
        public static Mat mtMask = new Mat();
        public static Mat mtOutlineMask = new Mat();
        public static List<WorkingDetection> WorkingDetections = new List<WorkingDetection>();
        public static string TextPrompt = string.Empty;
        public static SamEnum SEnum = SamEnum.None;

        public static List<SamDataSet> DataSet = new List<SamDataSet>();

        public static void ClearSamDetections()
        {
            foreach (var detection in WorkingDetections)
            {
                if (detection.mtMask != null) detection.mtMask.Dispose();
            }
            WorkingDetections.Clear();
            if (mtMask != null)
            {
                mtMask.Dispose();
                mtMask = new Mat();
            }
            if (mtOutlineMask != null)
            {
                mtOutlineMask.Dispose();
                mtOutlineMask = new Mat();
            }
        }

        public static void ClearDataSet()
        {
            DataSet.Clear();
        }

        public static OpenSafeMat CombineListIntDetectionMasks(int unique)
        {
            Mat mt = new Mat();
            Mat mtWork = new Mat();
            try
            {
                mt = new Mat(SamModel.mtMask.Rows, SamModel.mtMask.Cols, SamModel.mtMask.Depth, 1);
                mt.SetTo(new MCvScalar(0));
                foreach (var set in SamModel.DataSet)
                {
                    byte intensity = 255;
                    if ((unique >= 0) && (set.UniqueID != unique)) intensity = 128;

                    OpenSafeMat osm = OpenIP.ListOfIntToBinaryMask(set.MaskRle, mt.Rows, mt.Cols, intensity);
                    if (osm.IsSuccess)
                    {
                        CvInvoke.BitwiseOr(mt, osm.Mt, mt);
                        osm.Mt.Dispose();
                    }
                    else
                    {
                        SamLog.AddEntry("CombineListIntDetectionMasks", osm);
                        return new OpenSafeMat("Failure in CombineListIntDetectionMasks");
                    }
                }
                return new OpenSafeMat(mt.Clone());
            }
            catch (Exception ex)
            {
                return new OpenSafeMat("Exception in CombineListIntDetectionMats", ex.Message);
            }
            finally
            {
                if (mt != null) mt.Dispose();
                if (mtWork != null) mtWork.Dispose();
            }
        }

        public static OpenSafeBool CombineDetectionMasks()
        {
            Mat mt = new Mat();
            try
            {
                SamModel.mtMask.SetTo(new MCvScalar(0));
                for (int i = 0; i < SamModel.WorkingDetections.Count(); i++)
                {
                    CvInvoke.BitwiseOr(SamModel.mtMask, SamModel.WorkingDetections[i].mtMask, SamModel.mtMask);
                }
                CvInvoke.Erode(SamModel.mtMask, mt, null, new System.Drawing.Point(1, 1), 1, BorderType.Default, CvInvoke.MorphologyDefaultBorderValue);
                CvInvoke.BitwiseXor(SamModel.mtMask, mt, mt);
                SamModel.mtOutlineMask = mt.Clone();
                return new OpenSafeBool(true);
            }
            catch (Exception ex)
            {
                return new OpenSafeBool("Exception in CombineDetectionMats", ex.Message);
            }
            finally
            {
                if (mt != null) mt.Dispose();
            }
        }
    }

    public class WorkingDetection
    {
        public SamEnum Senum;
        public int Index;
        public int DataSetUniqueID = -1;   // Determined later by calling program
        public Mat mtMask;
        public float Score;
        public float Iou;
        public System.Drawing.Rectangle Rect;
        public string Label = String.Empty;
        public string SubLabel = String.Empty;

        public WorkingDetection(SamEnum se, int ind, Mat m, float s, float iou, Sam3Box? box)
        {
            Senum = se;
            Index = ind;
            mtMask = m;
            Score = s;
            Iou = iou;;
            //         Box = box;
            if (box != null)
            {
                if (box.Value.X1 == 0)
                {
                    Rect = new System.Drawing.Rectangle(0, 0, 0, 0);
                }
                else
                {
                    Rect = new System.Drawing.Rectangle((int)box.Value.X0, (int)box.Value.Y0,
                        (int)(box.Value.X1 - box.Value.X0), (int)(box.Value.Y1 - box.Value.Y0));
                }
            }
            else
            {
                Rect = new System.Drawing.Rectangle(0, 0, 0, 0);
            }
        }

        public WorkingDetection(SamEnum se, int ind, Mat m, float s, float iou, System.Drawing.Rectangle rect)
        {
            Senum = se;
            Index = ind;
            mtMask = m;
            Score = s;
            Iou = iou;
            Rect = rect;
        }

        public WorkingDetection(SamEnum se, int ind, Mat m, System.Drawing.Rectangle rect, string label, string sublabel)
        {
            Senum = se;
            Index = ind;
            mtMask = m;
            Score = 0;
            Iou = 0;
            Rect = rect;
            Label = label;
            SubLabel = sublabel;
        }

        public WorkingDetection(SamEnum se, int ind, Mat m, float s, float iou, System.Drawing.Rectangle rect, string label, string sublabel)
        {
            Senum = se;
            Index = ind;
            mtMask = m;
            Score = s;
            Iou = iou;
            Rect = rect;
            Label = label;
            SubLabel = sublabel;
        }
    }

    public class SamDataSet
    {
        public int UniqueID { get; set; }
        public string Label { get; set; }
        public string SubLabel { get; set; }
        public List<int> MaskSize { get; set; }
        public List<int> MaskRle { get; set; }
        public System.Drawing.Rectangle Rect { get; set; }
        public int MaskArea { get; set; }
        public float Score { get; set; }

        public SamDataSet()
        {
            UniqueID = -1;
            Label = String.Empty;
            SubLabel = String.Empty;
            Rect = new System.Drawing.Rectangle();
            MaskRle = new List<int>();
            MaskSize = new List<int>();
            Score = 0.0f;
        }

        public SamDataSet(string label, string subLabel, List<int> maskrle, System.Drawing.Rectangle rect,
            int maskarea, List<int> maskSize, float score)
        {
            Label = label;
            SubLabel = subLabel;
            MaskRle = maskrle;
            MaskSize = new List<int>();
            if (SamModel.DataSet.Count == 0)
            {
                UniqueID = 0;
            }
            else
            {
                UniqueID = SamModel.DataSet.Max(a => a.UniqueID) + 1;
            }
            Rect = rect;
            MaskArea = maskarea;
            MaskSize = maskSize;
            Score = score;
        }
    }
}
