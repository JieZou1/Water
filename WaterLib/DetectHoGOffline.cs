using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Drawing;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;


namespace WaterLib
{
    public class Sample
    {
        public CHONG_TYPE chongType;
        public string imageFile;
        public List<double> rs, xs, ys, ws, hs;	//The original data file of the rotated bounding box from xml.

        public Sample(CHONG_TYPE chong_type, string image_file)
        {
            chongType = chong_type;
            imageFile = image_file;
            rs = new List<double>(); xs = new List<double>(); ys = new List<double>(); ws = new List<double>(); hs = new List<double>();
        }

        public void AddOneChong(string s_r, string s_x, string s_y, string s_w, string s_h)
        {
            rs.Add(double.Parse(s_r));
            xs.Add(double.Parse(s_x));
            ys.Add(double.Parse(s_y));
            ws.Add(double.Parse(s_w));
            hs.Add(double.Parse(s_h));

        }
    };

    public partial class DetectHoG
    {
        public static List<Sample> LoadSamples(CHONG_TYPE chong_type, string folder_prefix)
        {
            string folder = "";
            switch (chong_type)
            {
                case CHONG_TYPE.BIAO_KE_CHONG: folder = folder_prefix + @"BiaoKe\Original"; break;
                case CHONG_TYPE.ZHONG_CHONG: folder = folder_prefix + @"Zhong\Original"; break;
                case CHONG_TYPE.CHUI_XI_GUAN_CHONG: folder = folder_prefix + @"ChuiXiGuan\Original"; break;
                case CHONG_TYPE.DAN_ZHI_LUN_CHONG: folder = folder_prefix + @"DanZhiLun\Original"; break;
                case CHONG_TYPE.ZHU_WEN_LUN_CHONG: folder = folder_prefix + @"ZhuWenLun\Original"; break;

                case CHONG_TYPE.QIAO_JU_CHONG: folder = @"\users\jie\projects\Water1\data\QiaoJu\Original"; break;
            }

            //Load all samples from all the folders
            List<Sample> samples = new List<Sample>(); List<string> missing_xml_samples = new List<string>();

            string[] files = Directory.EnumerateFiles(folder)
                .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg"))
                .ToArray();

            for (int k = 0; k < files.Length; k++)
            {
                string img_file = files[k].ToLower();
                string xml_file = img_file.EndsWith("png") ? img_file.Replace(".png", "_data.xml") : img_file.Replace(".jpg", "_data.xml");

                if (!File.Exists(xml_file))
                {
                    missing_xml_samples.Add(img_file); continue;
                }

                Sample sample = new Sample(chong_type, img_file);

                //  .// Means descendants, which includes children of children (and so forth).
                //  ./ Means direct children.
                //If a XPath starts with a / it becomes relative to the root of the document; 
                //to make it relative to your own node start it with ./.
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument(); doc.Load(xml_file);
                HtmlAgilityPack.HtmlNodeCollection shape_nodes = doc.DocumentNode.SelectNodes("//shape");
                foreach (HtmlAgilityPack.HtmlNode shape_node in shape_nodes)
                {
                    HtmlAgilityPack.HtmlNode data_node = shape_node.SelectSingleNode(".//data");
                    HtmlAgilityPack.HtmlNode extent_node = data_node.SelectSingleNode("./extent");

                    string s_r = data_node.GetAttributeValue("Rotation", "");
                    string s_x = extent_node.GetAttributeValue("X", "");
                    string s_y = extent_node.GetAttributeValue("Y", "");
                    string s_w = extent_node.GetAttributeValue("Width", "");
                    string s_h = extent_node.GetAttributeValue("Height", "");
                    sample.AddOneChong(s_r, s_x, s_y, s_w, s_h);
                }

                samples.Add(sample);
            }

            using (StreamWriter sw = new StreamWriter("missing_xml.txt"))
            {
                for (int i = 0; i < missing_xml_samples.Count; i++) sw.WriteLine(missing_xml_samples[i]);
            }

            return samples;
        }

        public static void SampleStatistics(CHONG_TYPE chong_type)
        {
            List<Sample> samples = LoadSamples(chong_type, @"\users\jie\projects\Water2\data\");

            string statistics_file = Path.GetDirectoryName(samples[0].imageFile).Replace("original", "statistics.txt");

            //#if 0 //Calcuate some statistics
            double w_max = 0, h_max = 0, w_min = 100000, h_min = 100000, count = 0;
            double w_mean = 0, h_mean = 0, ratio_max = 0, ratio_min = 10000, ratio_mean = 0;
            List<double> sides = new List<double>(); double side_mean = 0;
            List<double> ws = new List<double>(); List<double> hs = new List<double>();
            for (int i = 0; i < samples.Count; ++i)
            {
                Sample sample = samples[i];
                for (int j = 0; j < sample.rs.Count; ++j)
                {
                    if (sample.ws[j] > w_max) { w_max = sample.ws[j]; }
                    if (sample.hs[j] > h_max) { h_max = sample.hs[j]; }
                    if (sample.ws[j] < w_min) { w_min = sample.ws[j]; }
                    if (sample.hs[j] < h_min) { h_min = sample.hs[j]; }
                    w_mean += sample.ws[j]; h_mean += sample.hs[j];
                    double ratio = sample.ws[j] / sample.hs[j];
                    if (ratio > ratio_max) ratio_max = ratio;
                    if (ratio < ratio_min) ratio_min = ratio;
                    ratio_mean += ratio;

                    ws.Add(sample.ws[j]); hs.Add(sample.hs[j]);
                    sides.Add(sample.ws[j]); sides.Add(sample.hs[j]);
                    side_mean += sample.ws[j]; side_mean += sample.hs[j];

                    count++;
                }
            }
            w_mean /= count; h_mean /= count; ratio_mean /= count; side_mean /= sides.Count;

            //Calcualte standard derivation
            double w_var = 0;
            for (int i = 0; i < ws.Count; ++i) w_var += (ws[i] - w_mean) * (ws[i] - w_mean); w_var /= ws.Count;
            double w_stddev = Math.Sqrt(w_var);
            double h_var = 0;
            for (int i = 0; i < hs.Count; ++i) h_var += (hs[i] - h_mean) * (hs[i] - h_mean); h_var /= hs.Count;
            double h_stddev = Math.Sqrt(h_var);
            double side_var = 0;
            for (int i = 0; i < sides.Count; ++i) side_var += (sides[i] - side_mean) * (sides[i] - side_mean); side_var /= sides.Count;
            double side_stddev = Math.Sqrt(side_var);

            using (StreamWriter sw = new StreamWriter(statistics_file))
            {
                sw.WriteLine("Number of Chong: {0}", count);
                sw.WriteLine();

                sw.WriteLine("Width Range: [{0}:{1}]", w_min * 1.2, w_max * 1.2);
                sw.WriteLine("Width Mean: {0}", w_mean * 1.2);
                sw.WriteLine("Width Std Dev: {0}", w_stddev * 1.2);
                sw.WriteLine();

                sw.WriteLine("Height Range: [{0}:{1}]", h_min * 1.2, h_max * 1.2);
                sw.WriteLine("Height Mean: {0}", h_mean * 1.2);
                sw.WriteLine("Height Std Dev: {0}", h_stddev * 1.2);
                sw.WriteLine();

                sw.WriteLine("Ratio Range: [{0}:{1}]", ratio_min, ratio_max);
                sw.WriteLine("Ratio Mean: {0}", ratio_mean);
                sw.WriteLine();

                sw.WriteLine("Side Mean: {0}", side_mean * 1.2);
                sw.WriteLine("Side Std Dev: {0}", side_stddev * 1.2);
                sw.WriteLine();
            }
        }

        public static void CropChong(CHONG_TYPE chong_type)
        {
            List<Sample> samples = LoadSamples(chong_type, @"\users\jie\projects\Water2\data\");
            for (int i = 0; i < samples.Count; i++)
            {
                Sample sample = samples[i];

                Emgu.CV.Image<Gray, byte> image = new Emgu.CV.Image<Gray, byte>(sample.imageFile);
                for (int k = 0; k < sample.rs.Count; k++)
                {
                    double r = sample.rs[k], x = sample.xs[k], y = sample.ys[k], w = sample.ws[k], h = sample.hs[k];

                    PointF center = new PointF((float)(x + w / 2.0), (float)(y + h / 2.0));
                    Emgu.CV.Image<Gray, byte> rotated = image.Rotate(-r, center, Inter.Linear, new Gray(255), true);

                    rotated.ROI = new Rectangle((int)(x + 0.5), (int)(y + 0.5), (int)(w + 0.5), (int)(h + 0.5));

                    string cropped_file = sample.imageFile.Replace("original", "cropped");
                    cropped_file = cropped_file.Insert(cropped_file.LastIndexOf('.'), "." + k.ToString());
                    rotated.Save(cropped_file);
                }
            }
        }

        public static void NormalizeChong(CHONG_TYPE chong_type)
        {
            List<Sample> samples = LoadSamples(chong_type, @"\users\jie\projects\Water1\data\");
            for (int i = 0; i < samples.Count; i++)
            {
                Sample sample = samples[i];

                Emgu.CV.Image<Gray, byte> image = new Emgu.CV.Image<Gray, byte>(sample.imageFile);
                //Mat image = new Emgu.CV.Mat(sample.imageFile, Emgu.CV.CvEnum.LoadImageType.Grayscale);

                for (int k = 0; k < sample.rs.Count; k++)
                {
                    double r = sample.rs[k], x = sample.xs[k], y = sample.ys[k], w = sample.ws[k], h = sample.hs[k];
                    PointF center = new PointF((float)(x + w / 2.0), (float)(y + h / 2.0));

                    RotatedRect rotated_rect = new RotatedRect(center, new SizeF((float)w, (float)h), (float)r);
                    Rectangle rect = rotated_rect.MinAreaRect();

                    Emgu.CV.Image<Gray, byte> normalized = null; //double w_h_ratio;

                    switch (chong_type)
                    {
                        case CHONG_TYPE.BIAO_KE_CHONG:
                        case CHONG_TYPE.ZHONG_CHONG:    //Force the roi to be a square
                        case CHONG_TYPE.CHUI_XI_GUAN_CHONG:
                        case CHONG_TYPE.DAN_ZHI_LUN_CHONG:
                        case CHONG_TYPE.ZHU_WEN_LUN_CHONG:
                        case CHONG_TYPE.QIAO_JU_CHONG:  //We have to use square even for slim chongs like Qiao Ju, because we have to build models for rotated chongs.
                            if (rect.Width > rect.Height)
                            {
                                int height_new = rect.Width;
                                int c = rect.Y + rect.Height / 2; rect.Y = c - height_new / 2; rect.Height = height_new;
                            }
                            else
                            {
                                int width_new = rect.Height;
                                int c = rect.X + rect.Width / 2; rect.X = c - width_new / 2; rect.Width = width_new;
                            }

                            //Expand 10% in each direction
                            rect.X -= (int)((double)rect.Width / 10 + 0.5); rect.Width = (int)(rect.Width * 1.2 + 0.5);
                            rect.Y -= (int)((double)rect.Height / 10 + 0.5); rect.Height = (int)(rect.Height * 1.2 + 0.5);
                            if (rect.Y < 0 || rect.X < 0 || rect.Y + rect.Height > image.Height || rect.X + rect.Width > image.Width) continue;

                            //Normalize to 64x64 model size
                            image.ROI = rect; normalized = image.Resize(64, 64, Inter.Linear);
                            break;
                    }

                    string chong_image_file = sample.imageFile.Replace("original", "Normalized");
                    chong_image_file = chong_image_file.Insert(chong_image_file.LastIndexOf('.'), "." + k.ToString());
                    string folder = Path.GetDirectoryName(chong_image_file);
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                    normalized.Save(chong_image_file);
                }
            }
        }

        public static void NormalizeChongWithRotation(CHONG_TYPE chong_type)
        {
            List<Sample> samples = LoadSamples(chong_type, @"\users\jie\projects\Water2\data\");
            for (int i = 0; i < samples.Count; i++)
            {
                Sample sample = samples[i];

                Emgu.CV.Image<Gray, byte> image = new Emgu.CV.Image<Gray, byte>(sample.imageFile);
                for (int k = 0; k < sample.rs.Count; k++)
                {
                    double r = sample.rs[k], x = sample.xs[k], y = sample.ys[k], w = sample.ws[k], h = sample.hs[k];
                    PointF center = new PointF((float)(x + w / 2.0), (float)(y + h / 2.0));

                    Emgu.CV.Image<Gray, byte> normalized = null; //double w_h_ratio;
                    for (int deg = 0; deg < 360; deg += 30)
                    {
                        Emgu.CV.Image<Gray, byte> rotated = image.Rotate(-(r + deg), center, Inter.Linear, new Gray(128), true);
                        Rectangle rect = new Rectangle((int)(x + 0.5), (int)(y + 0.5), (int)(w + 0.5), (int)(h + 0.5));

                        switch (chong_type)
                        {
                            case CHONG_TYPE.BIAO_KE_CHONG:
                            case CHONG_TYPE.ZHONG_CHONG:    //Force the roi to be a square
                            case CHONG_TYPE.CHUI_XI_GUAN_CHONG:
                            case CHONG_TYPE.DAN_ZHI_LUN_CHONG:
                            case CHONG_TYPE.ZHU_WEN_LUN_CHONG:
                            case CHONG_TYPE.QIAO_JU_CHONG:  //We have to use square even for slim chongs like Qiao Ju, because we have to build models for rotated chongs.
                                if (rect.Width > rect.Height)
                                {
                                    int height_new = rect.Width;
                                    int c = rect.Y + rect.Height / 2; rect.Y = c - height_new / 2; rect.Height = height_new;
                                }
                                else
                                {
                                    int width_new = rect.Height;
                                    int c = rect.X + rect.Width / 2; rect.X = c - width_new / 2; rect.Width = width_new;
                                }

                                //Expand 10% in each direction
                                rect.X -= (int)((double)rect.Width / 10 + 0.5); rect.Width = (int)(rect.Width * 1.2 + 0.5);
                                rect.Y -= (int)((double)rect.Height / 10 + 0.5); rect.Height = (int)(rect.Height * 1.2 + 0.5);
                                if (rect.Y < 0 || rect.X < 0 || rect.Y + rect.Height > image.Height || rect.X + rect.Width > image.Width) continue;
                                //if (rect.Y < 0) rect.Y = 0;
                                //if (rect.X < 0) rect.X = 0;
                                //if (rect.Y + rect.Height > image.Height) rect.Height = image.Height - rect.Y;
                                //if (rect.X + rect.Width > image.Width) rect.Width = image.Width - rect.X;

                                //Normalize to 64x64 model size
                                rotated.ROI = rect; normalized = rotated.Resize(64, 64, Inter.Linear);
                                break;
                            //case CHONG_TYPE.QIAO_JU_CHONG:  //Force the roi width-height ratio to be 48:128
                            //    w_h_ratio = (double)rect.Width / (double)rect.Height;
                            //    if (w_h_ratio > 48.0 / 128.0)
                            //    {
                            //        int height_new = (int)(128.0 * rect.Width / 48.0 + 0.5);
                            //        int c = rect.Y + rect.Height / 2; rect.Y = c - height_new / 2; rect.Height = height_new;
                            //    }
                            //    else
                            //    {
                            //        int width_new = (int)(48.0 * rect.Height / 128.0);
                            //        int c = rect.X + rect.Width / 2; rect.X = c - width_new / 2; rect.Width = width_new;
                            //    }
                            //    //Expand 10% in each direction
                            //    rect.X -= (int)((double)rect.Width / 10 + 0.5); rect.Width = (int)(rect.Width * 1.2 + 0.5);
                            //    rect.Y -= (int)((double)rect.Height / 10 + 0.5); rect.Height = (int)(rect.Height * 1.2 + 0.5);
                            //    if (rect.Y < 0 || rect.X < 0 || rect.Y + rect.Height > image.Height || rect.X + rect.Width > image.Width) continue;

                            //    //Normalize to 64x64 model size
                            //    rotated.ROI = rect; normalized = rotated.Resize(48, 128, Inter.Linear);
                            //    break;
                        }


                        string chong_image_file = sample.imageFile.Replace("original", @"64\" + deg.ToString());
                        chong_image_file = chong_image_file.Insert(chong_image_file.LastIndexOf('.'), "." + k.ToString());
                        string folder = Path.GetDirectoryName(chong_image_file);
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                        normalized.Save(chong_image_file);
                    }
                }
            }
        }

        public static void NegativePatches(CHONG_TYPE chong_type)
        {
            int side_max = 0, side_min = 0;

            //Check statistics.txt to find out Chong size in images, and set these values
            switch (chong_type)
            {
                case CHONG_TYPE.ZHONG_CHONG: side_min = 40; side_max = 180; break;
                case CHONG_TYPE.BIAO_KE_CHONG: side_min = 125; side_max = 250; break;
            }

            List<Sample> samples = LoadSamples(chong_type, @"\users\jie\projects\Water2\data\");
            for (int i = 0; i < samples.Count; i++)
            {
                Sample sample = samples[i];

                Emgu.CV.Image<Gray, byte> image = new Emgu.CV.Image<Gray, byte>(sample.imageFile);
                int image_width = image.Width, image_height = image.Height;

                Random rand = new Random();
                for (int k = 0; k < 1; k++)
                {
                    int x, y, w = 0, h = 0;
                    x = rand.Next(0, image_width); y = rand.Next(0, image_height);
                    w = h = rand.Next(side_min, side_max);

                    if (x + w >= image_width || y + h >= image_height) { k--; continue; }

                    Rectangle rect = new Rectangle(x, y, w, h);
                    image.ROI = rect;
                    Emgu.CV.Image<Gray, byte> neg_image = image.Resize(64, 64, Inter.Linear);
                    image.ROI = Rectangle.Empty;

                    string negative_image_file = sample.imageFile.Replace("original", @"64\negative");
                    negative_image_file = negative_image_file.Insert(negative_image_file.LastIndexOf('.'), "." + k.ToString());
                    string folder = Path.GetDirectoryName(negative_image_file);
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                    neg_image.Save(negative_image_file);
                }
            }
        }

        private static void LoadTrainingSamples(CHONG_TYPE chong_type, out List<string[]> pos_samples, out string[] neg_samples)
        {
            string folder = "";
            switch (chong_type)
            {
                case CHONG_TYPE.BIAO_KE_CHONG: folder = @"\users\jie\projects\Water2\data\BiaoKe\"; break;
                case CHONG_TYPE.ZHONG_CHONG: folder = @"\users\jie\projects\Water2\data\Zhong\"; break;
                case CHONG_TYPE.CHUI_XI_GUAN_CHONG: folder = @"\users\jie\projects\Water2\data\ChuiXiGuan\"; break;
                case CHONG_TYPE.DAN_ZHI_LUN_CHONG: folder = @"\users\jie\projects\Water2\data\DanZhiLun\"; break;
                case CHONG_TYPE.ZHU_WEN_LUN_CHONG: folder = @"\users\jie\projects\Water2\data\ZhuWenLun\"; break;

                case CHONG_TYPE.QIAO_JU_CHONG: folder = @"\users\jie\projects\Water2\data\QiaoJu\"; break;
            }

            pos_samples = new List<string[]>();
            for (int deg = 0; deg < 360; deg += 30)
            {
                List<string> samples = new List<string>();
                string pos_folder = folder + @"64\" + deg.ToString();
                string[] files = Directory.EnumerateFiles(pos_folder)
                    .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg") || file.ToLower().EndsWith(".bmp"))
                    .ToArray();
                pos_samples.Add(files);
            }

            string neg_folder = folder + @"64\Negative";
            neg_samples = Directory.EnumerateFiles(neg_folder)
                .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg"))
                .ToArray();
        }

        public static void TrainDetection(CHONG_TYPE chong_type)
        { 
            List<string[]> pos_samples_all; string[] neg_samples;
            LoadTrainingSamples(chong_type, out pos_samples_all, out neg_samples);

            DetectHoG hog = new DetectHoG();

            List<float> neg_targets = new List<float>(); List<float[]> neg_features = new List<float[]>();
            for (int i = 0; i < neg_samples.Length; i++)
            {
                Emgu.CV.Image<Gray, byte> patch = new Emgu.CV.Image<Gray, byte>(neg_samples[i]);
                float[] feature = hog.FeatureExtractionDetection(patch);
                neg_features.Add(feature);
                neg_targets.Add(-1.0f);
            }

            for (int k = 0; k < pos_samples_all.Count; k++)
            {
                List<float> pos_targets = new List<float>(); List<float[]> pos_features = new List<float[]>();
                for (int i = 0; i < pos_samples_all[k].Length; i++)
                {
                    Emgu.CV.Image<Gray, byte> patch = new Emgu.CV.Image<Gray, byte>(pos_samples_all[k][i]);
                    float[] feature = hog.FeatureExtractionDetection(patch);
                    pos_features.Add(feature);
                    pos_targets.Add(1.0f);
                }

                List<float> targets = new List<float>(); List<float[]> features = new List<float[]>();
                targets.AddRange(pos_targets); targets.AddRange(neg_targets);
                features.AddRange(pos_features); features.AddRange(neg_features);
                LibSVM.SaveInLibSVMFormat("train" + (k * 30).ToString() + ".txt", targets.ToArray(), features.ToArray());
            }
        }

        public static void ToSingleVector(CHONG_TYPE chong_type)
        {
            string folder = "", file ="", class_name = "";
            switch (chong_type)
            {
                case CHONG_TYPE.BIAO_KE_CHONG:
                    folder = @"\users\jie\projects\Water2\data\BiaoKe\64\DetectionModels";
                    file = folder + @"\HoGBiaoKe";
                    class_name = "svm_model_biaoke_chong_64_";
                    break;
                case CHONG_TYPE.ZHONG_CHONG:
                    folder = @"\users\jie\projects\Water2\data\Zhong\64\DetectionModels"; 
                    file = folder + @"\HoGZhong";
                    class_name = "svm_model_zhong_chong_64_";
                    break;
                case CHONG_TYPE.CHUI_XI_GUAN_CHONG: folder = @"\users\jie\projects\Water2\data\ChuiXiGuan\64"; break;
                case CHONG_TYPE.DAN_ZHI_LUN_CHONG: folder = @"\users\jie\projects\Water2\data\DanZhiLun\64"; break;
                case CHONG_TYPE.ZHU_WEN_LUN_CHONG: folder = @"\users\jie\projects\Water2\data\ZhuWenLun\64"; break;

                case CHONG_TYPE.QIAO_JU_CHONG: folder = @"\users\jie\projects\Water2\data\QiaoJu\64"; break;
            }

            string[] files = Directory.GetFiles(folder, "svm_model_*");
            for (int k = 0; k < files.Length; k++)
            {
                string svm_file = files[k]; string single_vector_svm_model = file + (k*30).ToString("d3") + ".cs";

                LibSVM svm = new LibSVM(); svm.LoadModel(svm_file);
                float[] single_vector = svm.ToSingleVector();

                using (StreamWriter sw = new StreamWriter(single_vector_svm_model))
                {
                    sw.WriteLine("namespace WaterLib");
                    sw.WriteLine("{");
                    sw.WriteLine("    public partial class DetectHoG");
                    sw.WriteLine("    {");
                    sw.WriteLine("        protected static float[] " + class_name + k * 30 + " = ");
                    sw.WriteLine("    {");
                    for (int i = 0; i < single_vector.Length; i++)
                    {
                        sw.WriteLine("{0}f,", single_vector[i]);
                    }
                    sw.WriteLine("    };");
                    sw.WriteLine("    }");
                    sw.WriteLine("}");
                }
            }
        }

        public static void TrainVerification(CHONG_TYPE chong_type)
        {
            string[] pos_samples = null, neg_samples = null;

            switch (chong_type)
            { 
                case CHONG_TYPE.ZHONG_CHONG:
                    pos_samples = Directory.GetFiles(@"\users\jie\projects\Water2\Data\Zhong\64\VerficationModels\Positives");
                    neg_samples = Directory.GetFiles(@"\users\jie\projects\Water2\Data\Zhong\64\VerficationModels\Negatives");
                    break;
                case CHONG_TYPE.BIAO_KE_CHONG:
                    pos_samples = Directory.GetFiles(@"\users\jie\projects\Water2\Data\BiaoKe\64\VerficationModels\Positives");
                    neg_samples = Directory.GetFiles(@"\users\jie\projects\Water2\Data\BiaoKe\64\VerficationModels\Negatives");
                    break;
            }

            DetectHoG hog = new DetectHoG();

            List<float> neg_targets = new List<float>(); List<float[]> neg_features = new List<float[]>();
            for (int i = 0; i < neg_samples.Length; i++)
            {
                Emgu.CV.Image<Gray, byte> patch = new Emgu.CV.Image<Gray, byte>(neg_samples[i]);
                float[] feature = hog.FeatureExtractionDetection(patch);
                neg_features.Add(feature);
                neg_targets.Add(-1.0f);
            }

            List<float> pos_targets = new List<float>(); List<float[]> pos_features = new List<float[]>();
            for (int i = 0; i < pos_samples.Length; i++)
            {
                Emgu.CV.Image<Gray, byte> patch = new Emgu.CV.Image<Gray, byte>(pos_samples[i]);
                float[] feature = hog.FeatureExtractionDetection(patch);
                pos_features.Add(feature);
                pos_targets.Add(1.0f);
            }

            List<float> targets = new List<float>(); List<float[]> features = new List<float[]>();
            targets.AddRange(pos_targets); targets.AddRange(neg_targets);
            features.AddRange(pos_features); features.AddRange(neg_features);
            LibSVM.SaveInLibSVMFormat("train.txt", targets.ToArray(), features.ToArray());
        }

        public static void TrainFinalDecision()
        {
            DetectHoG hog = new DetectHoG();

            string[] zhong_samples = Directory.GetFiles(@"\users\jie\projects\Water2\Data\Zhong\64\VerficationModels\Positives");
            List<float> zhong_targets = new List<float>(); List<float[]> zhong_features = new List<float[]>();
            for (int i = 0; i < zhong_samples.Length; i++)
            {
                Emgu.CV.Image<Gray, byte> patch = new Emgu.CV.Image<Gray, byte>(zhong_samples[i]);
                float[] feature = hog.FeatureExtractionDetection(patch);
                zhong_features.Add(feature);
                zhong_targets.Add((float)CHONG_TYPE.ZHONG_CHONG);
            }

            string[] biaoke_samples = Directory.GetFiles(@"\users\jie\projects\Water2\Data\biaoke\64\VerficationModels\Positives");
            List<float> biaoke_targets = new List<float>(); List<float[]> biaoke_features = new List<float[]>();
            for (int i = 0; i < biaoke_samples.Length; i++)
            {
                Emgu.CV.Image<Gray, byte> patch = new Emgu.CV.Image<Gray, byte>(biaoke_samples[i]);
                float[] feature = hog.FeatureExtractionDetection(patch);
                biaoke_features.Add(feature);
                biaoke_targets.Add((float)CHONG_TYPE.BIAO_KE_CHONG);
            }

            List<float> targets = new List<float>(); List<float[]> features = new List<float[]>();
            targets.AddRange(biaoke_targets); targets.AddRange(zhong_targets);
            features.AddRange(biaoke_features); features.AddRange(zhong_features);
            LibSVM.SaveInLibSVMFormat("train.txt", targets.ToArray(), features.ToArray());
        }

        public static void TrainVerify()
        {
            List<float[]> all_features = new List<float[]>(); List<float> all_labels = new List<float>();

            DetectHoG hog = new DetectHoG();
            string folder; string[] files;
            Emgu.CV.Image<Gray, byte> patch;

            #region Load Negative samples
            folder = @"\users\jie\projects\Water1\data\NegativePatch";
            files = Directory.EnumerateFiles(folder)
                .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg"))
                .ToArray();
            for (int i = 0; i < files.Length; i++)
            {
                patch = new Image<Gray, byte>(files[i]);
                float[] feature = hog.FeatureExtractionVerfication(patch);
                all_features.Add(feature);
                all_labels.Add((int)CHONG_VERF_TYPE.NEGATIVE);
            }
            #endregion

            #region Load Zhong Chong
            for (int deg = 0; deg < 360; deg += 45)
            {
                folder = @"\users\jie\projects\Water1\data\Zhong\" + deg.ToString();
                files = Directory.EnumerateFiles(folder)
                    .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg"))
                    .ToArray();
                for (int i = 0; i < files.Length; i++)
                {
                    patch = new Image<Gray, byte>(files[i]);
                    float[] feature = hog.FeatureExtractionVerfication(patch);
                    all_features.Add(feature);
                    all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_0);
                    //switch (deg)
                    //{
                    //    case 0: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_0); break;
                    //    case 45: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_45); break;
                    //    case 90: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_90); break;
                    //    case 135: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_135); break;
                    //    case 180: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_180); break;
                    //    case 225: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_225); break;
                    //    case 270: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_270); break;
                    //    case 315: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_315); break;
                    //}
                }
            }
            #endregion

            #region Load BiaoKe Chong
            for (int deg = 0; deg < 360; deg += 45)
            {
                folder = @"\users\jie\projects\Water1\data\BiaoKe\" + deg.ToString();
                files = Directory.EnumerateFiles(folder)
                    .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg"))
                    .ToArray();
                for (int i = 0; i < files.Length; i++)
                {
                    patch = new Image<Gray, byte>(files[i]);
                    float[] feature = hog.FeatureExtractionVerfication(patch);
                    all_features.Add(feature);
                    all_labels.Add((int)CHONG_VERF_TYPE.BIAOKE_0);
                    //switch (deg)
                    //{
                    //    case 0: all_labels.Add((int)CHONG_VERF_TYPE.BIAOKE_0); break;
                    //    case 45: all_labels.Add((int)CHONG_VERF_TYPE.BIAOKE_45); break;
                    //    case 90: all_labels.Add((int)CHONG_VERF_TYPE.BIAOKE_90); break;
                    //    case 135: all_labels.Add((int)CHONG_VERF_TYPE.BIAOKE_135); break;
                    //    case 180: all_labels.Add((int)CHONG_VERF_TYPE.BIAOKE_180); break;
                    //    case 225: all_labels.Add((int)CHONG_VERF_TYPE.BIAOKE_225); break;
                    //    case 270: all_labels.Add((int)CHONG_VERF_TYPE.BIAOKE_270); break;
                    //    case 315: all_labels.Add((int)CHONG_VERF_TYPE.BIAOKE_315); break;
                    //}
                }
            }
            #endregion

            #region Load ChuiXiGuan Chong
            for (int deg = 0; deg < 360; deg += 45)
            {
                folder = @"\users\jie\projects\Water1\data\ChuiXiGuan\" + deg.ToString();
                files = Directory.EnumerateFiles(folder)
                    .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg"))
                    .ToArray();
                for (int i = 0; i < files.Length; i++)
                {
                    patch = new Image<Gray, byte>(files[i]);
                    float[] feature = hog.FeatureExtractionVerfication(patch);
                    all_features.Add(feature);
                    all_labels.Add((int)CHONG_VERF_TYPE.CHUIXIGUAN_0);
                    //switch (deg)
                    //{
                    //    case 0: all_labels.Add((int)CHONG_VERF_TYPE.CHUIXIGUAN_0); break;
                    //    case 45: all_labels.Add((int)CHONG_VERF_TYPE.CHUIXIGUAN_45); break;
                    //    case 90: all_labels.Add((int)CHONG_VERF_TYPE.CHUIXIGUAN_90); break;
                    //    case 135: all_labels.Add((int)CHONG_VERF_TYPE.CHUIXIGUAN_135); break;
                    //    case 180: all_labels.Add((int)CHONG_VERF_TYPE.CHUIXIGUAN_180); break;
                    //    case 225: all_labels.Add((int)CHONG_VERF_TYPE.CHUIXIGUAN_225); break;
                    //    case 270: all_labels.Add((int)CHONG_VERF_TYPE.CHUIXIGUAN_270); break;
                    //    case 315: all_labels.Add((int)CHONG_VERF_TYPE.CHUIXIGUAN_315); break;
                    //}
                }
            }
            #endregion

            #region Load DanZhiLun Chong
            for (int deg = 0; deg < 360; deg += 45)
            {
                folder = @"\users\jie\projects\Water1\data\DanZhiLun\" + deg.ToString();
                files = Directory.EnumerateFiles(folder)
                    .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg") || file.ToLower().EndsWith(".bmp"))
                    .ToArray();
                for (int i = 0; i < files.Length; i++)
                {
                    patch = new Image<Gray, byte>(files[i]);
                    float[] feature = hog.FeatureExtractionVerfication(patch);
                    all_features.Add(feature);
                    all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_0);
                    //switch (deg)
                    //{
                    //    case 0: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_0); break;
                    //    case 45: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_45); break;
                    //    case 90: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_90); break;
                    //    case 135: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_135); break;
                    //    case 180: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_180); break;
                    //    case 225: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_225); break;
                    //    case 270: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_270); break;
                    //    case 315: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_315); break;
                    //}
                }
            }
            #endregion

            #region Load ZhuWenLun Chong
            for (int deg = 0; deg < 360; deg += 45)
            {
                folder = @"\users\jie\projects\Water1\data\ZhuWenLun\" + deg.ToString();
                files = Directory.EnumerateFiles(folder)
                    .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg") || file.ToLower().EndsWith(".bmp"))
                    .ToArray();
                for (int i = 0; i < files.Length; i++)
                {
                    patch = new Image<Gray, byte>(files[i]);
                    float[] feature = hog.FeatureExtractionVerfication(patch);
                    all_features.Add(feature);
                    all_labels.Add((int)CHONG_VERF_TYPE.ZHUWENLUN_0);
                    //switch (deg)
                    //{
                    //    case 0: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_0); break;
                    //    case 45: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_45); break;
                    //    case 90: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_90); break;
                    //    case 135: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_135); break;
                    //    case 180: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_180); break;
                    //    case 225: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_225); break;
                    //    case 270: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_270); break;
                    //    case 315: all_labels.Add((int)CHONG_VERF_TYPE.DANZHILUN_315); break;
                    //}
                }
            }
            #endregion

            LibSVM.SaveInLibSVMFormat("train.txt", all_labels.ToArray(), all_features.ToArray());
        }

        public static void TrainVerifySingleChong()
        {
            List<float[]> all_features = new List<float[]>(); List<float> all_labels = new List<float>();

            DetectHoG hog = new DetectHoG();
            string folder; string[] files;
            Emgu.CV.Image<Gray, byte> patch;

            #region Load Negative samples
            folder = @"\users\jie\projects\Water1\data\NegativeZhong";
            files = Directory.EnumerateFiles(folder)
                .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg"))
                .ToArray();
            for (int i = 0; i < files.Length; i++)
            {
                patch = new Image<Gray, byte>(files[i]);
                float[] feature = hog.FeatureExtractionVerfication(patch);
                all_features.Add(feature);
                all_labels.Add((int)CHONG_VERF_TYPE.NEGATIVE);
            }
            #endregion

            #region Load Zhong Chong
            //folder = @"\users\jie\projects\Water1\data\Zhong\Normalized";
            //files = Directory.EnumerateFiles(folder)
            //    .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg"))
            //    .ToArray();
            //for (int i = 0; i < files.Length; i++)
            //{
            //    patch = new Image<Gray, byte>(files[i]);
            //    float[] feature = hog.FeatureExtractionVerfication(patch);
            //    all_features.Add(feature);
            //    all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_0);
            //}
            #endregion

            #region Load Zhong Chong With Rotation
            for (int deg = 0; deg < 360; deg += 45)
            {
                folder = @"\users\jie\projects\Water1\data\Zhong\" + deg.ToString();
                files = Directory.EnumerateFiles(folder)
                    .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg"))
                    .ToArray();
                for (int i = 0; i < files.Length; i++)
                {
                    patch = new Image<Gray, byte>(files[i]);
                    float[] feature = hog.FeatureExtractionVerfication(patch);
                    all_features.Add(feature);
                    //all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_0);
                    switch (deg)
                    {
                        case 0: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_0); break;
                        case 45: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_45); break;
                        case 90: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_90); break;
                        case 135: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_135); break;
                        case 180: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_180); break;
                        case 225: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_225); break;
                        case 270: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_270); break;
                        case 315: all_labels.Add((int)CHONG_VERF_TYPE.ZHONG_315); break;
                    }
                }
            }
            #endregion

            LibSVM.SaveInLibSVMFormat("train.txt", all_labels.ToArray(), all_features.ToArray());
        }

        public static void BootStrapDetection(CHONG_TYPE chong_type)
        {
            DetectHoG hog = new DetectHoG(); List<DetectResult> results = null; string[] files = null;

            List<string> image_files = new List<string>();
            switch (chong_type)
            {
                case CHONG_TYPE.ZHONG_CHONG:
                    files = Directory.EnumerateFiles(@"\users\jie\projects\Water2\Data\Zhong\Original")
                        .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg"))
                        .ToArray();
                    break;
                case CHONG_TYPE.BIAO_KE_CHONG:
                    files = Directory.EnumerateFiles(@"\users\jie\projects\Water2\Data\BiaoKe\Original")
                        .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg"))
                        .ToArray();
                    break;
            }
            image_files.AddRange(files);

            //files = Directory.EnumerateFiles(@"\users\jie\projects\Water2\Data\Negative")
            //    .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg"))
            //    .ToArray();
            //image_files.AddRange(files);

            for (int i = 0; i < image_files.Count; i++)
            {
                string image_file = image_files[i];
                //if (String.Compare(Path.GetFileNameWithoutExtension(image_file) , "20151223150000_2683") <= 0) continue;
                Emgu.CV.Image<Gray, byte> image = new Image<Gray, byte>(image_file);
                switch (chong_type)
                {
                    case CHONG_TYPE.ZHONG_CHONG: results = hog.DetectZhongWithRotationModels(image); break;
                    case CHONG_TYPE.BIAO_KE_CHONG: results = hog.DetectBiaoKeWithRotationModels(image); break;
                }

                if (results == null) continue;

                for (int k = 0; k < results.Count; k++)
                //for (int k = 0; k < Math.Min(results.Count, 2); k++)
                {
                    DetectResult result = results[k];

                    if (result.rect.Width != result.rect.Height) continue; //These are patches on the edges, we don't use them for training now.
                    //if (result.score < 0.7) continue; //These are low confidence patches, we don't use them for training now.

                    string patch_file = Path.GetFileNameWithoutExtension(image_file) + result.rect.ToString() + ".jpg";
                    switch (result.chong_type)
                    {
                        case CHONG_TYPE.ZHONG_CHONG: results[k].patch.Save(@"Zhong\" + patch_file); break;
                        case CHONG_TYPE.BIAO_KE_CHONG: results[k].patch.Save(@"BiaoKe\" + patch_file); break;
                        case CHONG_TYPE.CHUI_XI_GUAN_CHONG: results[k].patch.Save(@"ChuiXiGuan\" + patch_file); break;
                    }
                }
            }
        }

        public static void BootStrapNegatives()
        {
            //Read in "evaluation.txt", if exist, use only images which chongs are detected
            List<string> names = new List<string>();
            using (StreamReader sr = new StreamReader(@"\BaiduYunDownload\2016.01.26\test20160126\Negative\evaluation.txt"))
            {
                string line; string[] words;
                line = sr.ReadLine();
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("Total")) break;
                    words = line.Split();
                    int count = int.Parse(words[1]);
                    if (count == 0) continue;
                    names.Add(words[0]);
                }
            }

            DetectHoG hog = new DetectHoG();

            //Detect Negative Images
            String[] negatives = Directory.GetFiles(@"\users\jie\projects\Water1\data1\Negative\Original");
            //String[] negatives = Directory.GetFiles(@"\BaiduYunDownload\2016.01.26\test20160126\Negative\Original");
            for (int i = 0; i < negatives.Length; i++)
            {
                string image_file = negatives[i];
                string name = Path.GetFileNameWithoutExtension(image_file);
                if (names != null && names.Count > 0 && names.IndexOf(name) == -1) continue;

                Emgu.CV.Image<Gray, byte> image = new Image<Gray, byte>(image_file);
                DetectResult[] results = hog.DetectWithRotationModels(image);

                if (results == null || results.Length == 0) continue;

                for (int k = 0; k < Math.Min(results.Length, 10); k++)
                {
                    DetectResult result = results[k];

                    if (result.rect.Width != result.rect.Height) continue; //These are patches on the edges, we don't use them for training now.

                    string patch_file = Path.GetFileNameWithoutExtension(image_file) + result.rect.ToString() + ".jpg";
                    switch (result.chong_type)
                    {
                        case CHONG_TYPE.BIAO_KE_CHONG: results[k].patch.Save(@"BiaoKe\" + patch_file); break;
                        case CHONG_TYPE.DAN_ZHI_LUN_CHONG: results[k].patch.Save(@"DanZhiLun\" + patch_file); break;
                        case CHONG_TYPE.ZHONG_CHONG: results[k].patch.Save(@"Zhong\" + patch_file); break;
                        case CHONG_TYPE.CHUI_XI_GUAN_CHONG: results[k].patch.Save(@"ChuiXiGuan\" + patch_file); break;
                        case CHONG_TYPE.ZHU_WEN_LUN_CHONG: results[k].patch.Save(@"ZhuWenLun\" + patch_file); break;
                    }
                }
            }
        }

        public static void BootStrap()
        {
            DetectHoG hog = new DetectHoG();

            string folder = @"\users\jie\projects\Water1\deliver\test";
            string[] files = System.IO.Directory.EnumerateFiles(folder)
                .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg"))
                .ToArray();

            for (int i = 0; i < files.Length; i++)
            {
                string image_file = files[i];
                Emgu.CV.Image<Gray, byte> image = new Image<Gray, byte>(image_file);
                DetectResult[] results = hog.DetectWithRotationModels(image);

                if (results == null) continue;

                for (int k = 0; k < results.Length; k++)
                //for (int k = 0; k < Math.Min(results.Length, 10); k++)
                {
                    DetectResult result = results[k];

                    if (result.rect.Width != result.rect.Height) continue; //These are patches on the edges, we don't use them for training now.
                    if (result.score < 0.7) continue; //These are low confidence patches, we don't use them for training now.

                    string patch_file = Path.GetFileNameWithoutExtension(image_file) + result.rect.ToString() + ".jpg";
                    switch (result.chong_type)
                    {
                        case CHONG_TYPE.ZHONG_CHONG: results[k].patch.Save(@"Zhong\" + patch_file); break;
                        case CHONG_TYPE.BIAO_KE_CHONG: results[k].patch.Save(@"BiaoKe\" + patch_file); break;
                        case CHONG_TYPE.CHUI_XI_GUAN_CHONG: results[k].patch.Save(@"ChuiXiGuan\" + patch_file); break;
                    }
                }
            }
        }

        public static void BootStrap(CHONG_TYPE chong_type)
        {
            DetectHoG hog = new DetectHoG();

            List<Sample> samples = LoadSamples(chong_type, @"\users\jie\projects\Water2\data\"); 
            //samples = ListEx<Sample>.RandomSelect(samples, 200);
            for (int i = 0; i < samples.Count; i++)
            {
                Sample sample = samples[i]; string image_file = sample.imageFile;
                Emgu.CV.Image<Gray, byte> image = new Image<Gray, byte>(image_file);
                DetectResult[] results = hog.DetectWithRotationModels(image);

                if (results == null) continue;

                for (int k = 0; k < results.Length; k++)
                //for (int k = 0; k < Math.Min(results.Length, 10); k++)
                {
                    DetectResult result = results[k];

                    if (result.rect.Width != result.rect.Height) continue; //These are patches on the edges, we don't use them for training now.
                    if (result.score < 0.7) continue; //These are low confidence patches, we don't use them for training now.

                    string patch_file = Path.GetFileNameWithoutExtension(image_file) + result.rect.ToString() + ".jpg";
                    switch (result.chong_type)
                    {
                        case CHONG_TYPE.ZHONG_CHONG: results[k].patch.Save(@"Zhong\" + patch_file); break;
                        case CHONG_TYPE.BIAO_KE_CHONG: results[k].patch.Save(@"BiaoKe\" + patch_file); break;
                        case CHONG_TYPE.CHUI_XI_GUAN_CHONG: results[k].patch.Save(@"ChuiXiGuan\" + patch_file); break;
                    }
                }
            }
        }
    }
}