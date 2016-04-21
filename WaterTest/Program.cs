using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

using WaterLib;

namespace WaterTest
{
    class Program
    {
        static DetectHoG hog;
        static string dataFolder;

        static void EvaluateNegatives()
        {
            string image_folder = Path.Combine(dataFolder, @"Negative\Original"); 
            if (!Directory.Exists(image_folder)) return;

            string ret_folder = image_folder.Replace("Original", "result");

            if (Directory.Exists(ret_folder)) { Directory.Delete(ret_folder, true); System.Threading.Thread.Sleep(500); }
            Directory.CreateDirectory(ret_folder);

            int biaoke_count_total = 0, zhong_count_total = 0, chui_xi_guan_count_total = 0, dan_zhi_lun_count_total = 0, zhu_wen_lun_count_total = 0;
            int biaoke_count, zhong_count, chui_xi_guan_count, dan_zhi_lun_count, zhu_wen_lun_count;

            string evaluation_file = ret_folder.Replace("result", "evaluation.txt");

            string[] image_files = Directory.EnumerateFiles(image_folder)
                .Where(file => file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpg"))
                .ToArray();

            using (StreamWriter sw = new StreamWriter(evaluation_file))
            {
                sw.WriteLine("Filename\tZhong Count\tBiaoKe Count\tChuiXiGuan Count\tDanZhiLun Count\tZhuWenLun Count");
                for (int i = 0; i < image_files.Length; i++)
                {
                    string file = image_files[i];
                    System.Console.WriteLine("Processing " + file);

                    Image<Gray, byte> image = new Image<Gray, byte>(file);
                    WaterLib.DetectResult[] result = hog.DetectWithRotationModels(image);

                    biaoke_count = 0; zhong_count = 0; chui_xi_guan_count = 0; dan_zhi_lun_count = 0; zhu_wen_lun_count = 0;
                    if (result != null)
                    {
                        for (int k = 0; k < result.Length; k++)
                        {
                            System.Drawing.Rectangle rect = result[k].rect;
                            image.Draw(rect, new Gray(255), 3);
                            image.Draw(result[k].chong_type.ToString() + " " + result[k].score.ToString("f2"), new System.Drawing.Point(rect.Left + 3, rect.Bottom - 3), FontFace.HersheyPlain, 1, new Gray(255), 1);

                            switch (result[k].chong_type)                            
                            {
                                case CHONG_TYPE.ZHONG_CHONG: zhong_count++; break;
                                case CHONG_TYPE.BIAO_KE_CHONG: biaoke_count++; break;
                                case CHONG_TYPE.CHUI_XI_GUAN_CHONG: chui_xi_guan_count++; break;
                                case CHONG_TYPE.DAN_ZHI_LUN_CHONG: dan_zhi_lun_count++; break;
                                case CHONG_TYPE.ZHU_WEN_LUN_CHONG: zhu_wen_lun_count++; break;
                            }
                        }
                    }

                    zhong_count_total += zhong_count;
                    biaoke_count_total += biaoke_count;
                    chui_xi_guan_count_total += chui_xi_guan_count;
                    dan_zhi_lun_count_total += dan_zhi_lun_count;
                    zhu_wen_lun_count_total += zhu_wen_lun_count;

                    if (result != null && result.Length > 0) image.Save(file.Replace("Original", "result"));

                    sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", Path.GetFileNameWithoutExtension(file), zhong_count, biaoke_count, chui_xi_guan_count, dan_zhi_lun_count, zhu_wen_lun_count);
                }
                sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", "Total", zhong_count_total, biaoke_count_total, chui_xi_guan_count_total, dan_zhi_lun_count_total, zhu_wen_lun_count_total);
            }
        }

        static void Evaluate(CHONG_TYPE chongType)
        {
            string image_folder = "";
            switch (chongType)
            {
                case CHONG_TYPE.ZHONG_CHONG: image_folder = Path.Combine(dataFolder, @"Zhong\Original"); break;
                case CHONG_TYPE.BIAO_KE_CHONG: image_folder = Path.Combine(dataFolder, @"BiaoKe\Original"); break;
                case CHONG_TYPE.CHUI_XI_GUAN_CHONG: image_folder = Path.Combine(dataFolder, @"ChuiXiGuan\Original"); break;
                case CHONG_TYPE.DAN_ZHI_LUN_CHONG: image_folder = Path.Combine(dataFolder, @"DanZhiLun\Original"); break;
                case CHONG_TYPE.ZHU_WEN_LUN_CHONG: image_folder = Path.Combine(dataFolder, @"ZhuWenLun\Original"); break;
            }
            if (!Directory.Exists(image_folder)) return;

            string ret_folder = image_folder.Replace("Original", "result");

            if (Directory.Exists(ret_folder)) { Directory.Delete(ret_folder, true); System.Threading.Thread.Sleep(500); }
            Directory.CreateDirectory(ret_folder);

            int gt_count_total = 0, auto_count_total = 0, gt_count, auto_count;
            string evaluation_file = ret_folder.Replace("result", "evaluation.txt");

            List<Sample> samples = DetectHoG.LoadSamples(chongType, dataFolder.TrimEnd(new char[] { '\\' }) + @"\");

            using (StreamWriter sw = new StreamWriter(evaluation_file))
            {
                sw.WriteLine("Filename\tGT Count\tAuto Count");
                for (int i = 0; i < samples.Count; i++)
                {
                    Sample sample = samples[i]; string file = sample.imageFile;
                    System.Console.WriteLine("Processing " + file);

                    Image<Gray, byte> image = new Image<Gray, byte>(file);
                    WaterLib.DetectResult[] result = hog.DetectWithRotationModels(image);

                    gt_count = sample.xs.Count; auto_count = 0;
                    if (result != null)
                    {
                        for (int k = 0; k < result.Length; k++)
                        {
                            System.Drawing.Rectangle rect = result[k].rect;
                            image.Draw(rect, new Gray(255), 3);
                            image.Draw(result[k].chong_type.ToString() + " " + result[k].score.ToString("f2"), new System.Drawing.Point(rect.Left + 3, rect.Bottom - 3), FontFace.HersheyPlain, 1, new Gray(255), 1);

                            if (result[k].chong_type == chongType) auto_count++;
                        }
                    }

                    gt_count_total += gt_count;
                    auto_count_total += auto_count;

                    image.Save(file.Replace("original", "result"));
                    sw.WriteLine("{0}\t{1}\t{2}", Path.GetFileNameWithoutExtension(file), gt_count, auto_count);
                }
                sw.WriteLine("{0}\t{1}\t{2}", "Total", gt_count_total, auto_count_total);
            }
        }

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                System.Console.WriteLine("Usage: WaterTest <test_image_folder>");
                return;
            }
            dataFolder = args[0];
            if (!System.IO.Directory.Exists(dataFolder))
            {
                System.Console.WriteLine("Can not find " + dataFolder);
                return;
            }

            hog = new WaterLib.DetectHoG();

            //Evaluate(CHONG_TYPE.CHUI_XI_GUAN_CHONG);
            //Evaluate(CHONG_TYPE.DAN_ZHI_LUN_CHONG);
            //Evaluate(CHONG_TYPE.ZHU_WEN_LUN_CHONG);
            Evaluate(CHONG_TYPE.BIAO_KE_CHONG);
            Evaluate(CHONG_TYPE.ZHONG_CHONG);
            EvaluateNegatives();
        }
    }
}
