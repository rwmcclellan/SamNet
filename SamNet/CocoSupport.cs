// Copyright (c) 2026 Robert W. McClellan, Matthew J. McClellan
// Licensed under the GNU General Public License v3.0. See LICENSE in the repository root.

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Open.IP;

namespace SamNet
{
    public class CocoSupport()
    {
        public static OpenSafeBool ExportCoco(string outputJsonPathRoot, List<SamDataSet> instances)
        {

            try
            {
                var coco = new CocoDataset();

                // 1. License
                coco.licenses.Add(new CocoLicense());

                // 2. Image
                coco.images.Add(new CocoImage
                {
                    id = 1,
                    file_name = SamModel.OriginalImageName,
                    width = SamModel.mtImage.Width,
                    height = SamModel.mtImage.Height
                });

                // 3. Categories (Total up Labels from Sam DataSet)

                List<string> names = new List<string>();
                foreach (SamDataSet info in instances) names.Add(info.Label);
                names = names.Distinct().OrderBy(b => b).ToList();

                var categoryMap = new Dictionary<(string Super, string Label), int>();
                int catId = 1;

                foreach (var s in names)
                {
                    List<string> subnames = new List<string>();
                    foreach (SamDataSet info in instances) if (info.Label == s) subnames.Add(info.SubLabel);

                    foreach (var l in subnames)
                    {
                        coco.categories.Add(new CocoCategory
                        {
                            id = catId,
                            name = l,                 // sub-label
                            supercategory = s         // super-label
                        });
                        categoryMap[(s, l)] = catId;
                        catId++;
                    }
                }

                // 4. Annotations 
                int annId = 1;
                // get size from mtImage as all sizes should be the same
                List<int> sizeList = new List<int> { SamModel.mtImage.Rows, SamModel.mtImage.Cols };
                foreach (var inst in instances)
                {
                    if (!categoryMap.TryGetValue((inst.Label, inst.SubLabel), out int categoryId))
                        return new OpenSafeBool("ArgumentException in ExportCoco", $"Unknown category: {inst.Label}/{inst.SubLabel}");

                    OpenSafeString getCompressed = OpenIP.RleCompress(inst.MaskRle);
                    if (getCompressed.IsSuccess)
                    {
                        coco.annotations.Add(new CocoAnnotation
                        {
                            id = annId++,
                            image_id = 1,
                            category_id = categoryId,
                            segmentation = new CocoSemantic(sizeList, getCompressed.Str),
                            bbox = new List<double> { inst.Rect.X, inst.Rect.Y, inst.Rect.Width, inst.Rect.Height },    // [x, y, width, height]
                            area = inst.MaskArea,
                            score = inst.Score,
                            iscrowd = 0
                        });
                    }
                    else
                    {
                        SamLog.AddEntry("Coco Compression", getCompressed);
                    }
                }

                // 5. Serialize
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = null          // keep exact COCO field names (snake_case already used)
                };

                string json = JsonSerializer.Serialize(coco, options);

                string finalPath = outputJsonPathRoot + "\\Annotations\\instance.json";
                File.WriteAllText(finalPath, json);

                SamLog.AddEntry("ExportCoco", $"COCO RLE JSON written to: {finalPath}");

                finalPath = outputJsonPathRoot + "\\images\\" + SamModel.OriginalImageName; ;
                CvInvoke.Imwrite(finalPath, SamModel.mtImage);

                SamLog.AddEntry("ExportCoco", $"COCO image written to: {finalPath}");

                OpenSafeMat osm = SamModel.CombineListIntDetectionMasks(-1);
                if (osm.IsSuccess)
                {
                    int index = SamModel.OriginalImageName.LastIndexOf(".");
                    if (index > 0)
                    {
                        string pngname = SamModel.OriginalImageName.Substring(0, index) + "_Mask.Png";
                        finalPath = outputJsonPathRoot + "\\PNG_Masks\\" + pngname;
                        CvInvoke.Imwrite(finalPath, osm.Mt);
                        if (osm.Mt != null) osm.Mt.Dispose();
                        SamLog.AddEntry("ExportCoco", $"COCO Mask written to: {finalPath}");
                    }
                }
                return new OpenSafeBool(true);
            }
            catch (System.AccessViolationException ex)
            {
                return new OpenSafeBool("System Access Exception in CocoSupport", ex.Message);
            }
            catch (Exception ex)
            {
                return new OpenSafeBool("Exception in CocoSupport", ex.Message);
            }
            finally
            {

            }
        }


        public static OpenSafeBool LoadCocoJson(string folder)
        {
            try
            {
                string annotationFolder = folder + "\\Annotations";
                string imagesFolder = folder + "\\Images";

                string jsonPath = annotationFolder + "\\instance.json";
                if (File.Exists(jsonPath))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };

                    string json = File.ReadAllText(jsonPath);
                    CocoDataset? coco_dataset = JsonSerializer.Deserialize<CocoDataset>(json, options);

                    foreach (CocoImage cocoImage in coco_dataset!.images)
                    {
                        string imagePath = imagesFolder + "\\" + cocoImage.file_name;
                        if (File.Exists(imagePath))
                        {
                            SamModel.OriginalImageName = cocoImage.file_name;
                            SamModel.mtImage = CvInvoke.Imread(imagePath, ImreadModes.ColorBgr);
                            SamModel.mtMask = new Mat(SamModel.mtImage.Rows, SamModel.mtImage.Cols, SamModel.mtImage.Depth, 1);
                            SamModel.mtMask.SetTo(new MCvScalar(0));
                            if ((coco_dataset.categories.Count() > 0) && (coco_dataset.categories.Count() == coco_dataset.annotations.Count()))
                            {
                                SamModel.DataSet.Clear();
                                for (int i = 0; i < coco_dataset.categories.Count(); i++)
                                {
                                    string label = coco_dataset.categories[i].supercategory;
                                    string sublabel = coco_dataset.categories[i].name;
                                    OpenSafeRectangle osr = OpenIP.ListDoubleToRectangle(coco_dataset.annotations[i].bbox);
                                    if (osr.IsSuccess == false)
                                    {
                                        SamLog.AddEntry("ListToDoubleRectangle", osr);
                                        return new OpenSafeBool("Cannot create bounding box at annotation " + i.ToString());
                                    }
                                    OpenSafeListOfInt osloi = InspectRle(coco_dataset.annotations[i]);
                                    if (osloi.IsSuccess == false)
                                    {
                                        SamLog.AddEntry("Coco InspectRle", osloi);
                                        return new OpenSafeBool("Cannot import segmentation mask " + i.ToString());
                                    }
                                    int area = SamModel.mtImage.Width * SamModel.mtImage.Height;
                                    SamDataSet sds = new SamDataSet(label, sublabel, osloi.ListOfInt, osr.Rect, area,
                                        coco_dataset.annotations[i].segmentation.size, coco_dataset.annotations[i].score);
                                    SamModel.DataSet.Add(sds);
                                }

                            }
                            else
                            {
                                return new OpenSafeBool("No annotations exist or categories/annotations size mismatch");
                            }
                        }
                        else
                        {
                            return new OpenSafeBool("Cannot located image file at" + imagePath);
                        }
                        break;  // We can only handle 1 image right now.
                    }
                }
                else
                {
                    return new OpenSafeBool("Cannot located JSON file at" + jsonPath);
                }
                return new OpenSafeBool(true);
            }
            catch (Exception ex)
            {
                return new OpenSafeBool("Exception in LoadCocoJson", ex.Message);
            }
            finally
            {

            }

        }


        private static OpenSafeListOfInt InspectRle(CocoAnnotation ann)
        {
            try
            {
                if (ann.segmentation!.counts is JsonElement countsElement)
                {
                    if (countsElement.ValueKind == JsonValueKind.Array)
                    {
                        // Uncompressed: List<int>
                        List<int> counts = countsElement.Deserialize<List<int>>()!;
                        return new OpenSafeListOfInt(counts);
                    }
                    else if (countsElement.ValueKind == JsonValueKind.String)
                    {
                        // Compressed string
                        string compressed = countsElement.GetString()!;
                        OpenSafeListOfInt osloi = OpenIP.DecodeRleCompression(compressed);
                        if (osloi.IsSuccess)
                        {
                            return new OpenSafeListOfInt(osloi.ListOfInt);
                        }
                        else
                        {
                            SamLog.AddEntry("Coco InspectRle", osloi);
                            return new OpenSafeListOfInt("Cannot convert rle to list of int ");
                        }
                    }
                }
                return new OpenSafeListOfInt("RLE entry is not a JsonElement");
            }
            catch (Exception ex)
            {
                return new OpenSafeListOfInt("Exception in InspectRle", ex.Message);
            }
        }
    }



    public class CocoDataset
    {
        public CocoInfo info { get; set; } = new();
        public List<CocoLicense> licenses { get; set; } = new();
        public List<CocoImage> images { get; set; } = new();
        public List<CocoAnnotation> annotations { get; set; } = new();
        public List<CocoCategory> categories { get; set; } = new();
    }

    public class CocoInfo
    {
        public string description { get; set; } = "RLE dataset";
        public string version { get; set; } = "1.0";
        public int year { get; set; } = DateTime.Now.Year;
        public string contributor { get; set; } = "";
        public string date_created { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    }

    public class CocoLicense
    {
        public int id { get; set; } = 1;
        public string name { get; set; } = "Unknown";
        public string url { get; set; } = "";
    }

    public class CocoImage
    {
        public int id { get; set; }
        public string file_name { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int license { get; set; } = 1;
        public string date_captured { get; set; } = "";
        public CocoImage()
        {
            file_name = string.Empty;
        }
    }

    public class CocoCategory
    {
        public int id { get; set; }
        public string name { get; set; }          // sub-label (label1 / label2)
        public string supercategory { get; set; } // super1 / super2 / super3

        public CocoCategory()
        {
            name = string.Empty;
            supercategory = string.Empty;
        }
    }

    public class CocoSemantic
    {
        public List<int> size { get; set; }       // [height, width]
        public object? counts { get; set; }        // List<int> (uncompressed) or string (compressed)

        public CocoSemantic()
        {
            size = new List<int>();
        }

        public CocoSemantic(List<int> s, object c)
        {
            size = s;
            counts = c;
        }
    }

    public class CocoAnnotation
    {
        public int id { get; set; }
        public int image_id { get; set; }
        public int category_id { get; set; }
        public CocoSemantic segmentation { get; set; }
        public List<double> bbox { get; set; }    // [x, y, width, height]
        public double area { get; set; }
        public int iscrowd { get; set; } = 0;
        public float score { get; set; } = 0.0f; // Optional score field for detection confidence   

        public CocoAnnotation()
        {
            bbox = new List<double>();
            segmentation = new CocoSemantic();
        }
    }

}
