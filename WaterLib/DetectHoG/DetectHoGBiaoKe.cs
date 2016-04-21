using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;

using Emgu.CV;
using Emgu.CV.Structure;

namespace WaterLib
{
    public partial class DetectHoG
    {
        private List<DetectResult> DetectBiaoKeWithRotationModels(Image<Gray, byte> image)
        {
            //resize the image
            double scale = winSize_64.Height / 160.0; //check statistics.txt to decide this scaling factor.
            Image<Gray, byte> img_scaled = image.Resize(scale, Emgu.CV.CvEnum.Inter.Linear);

            MCvObjectDetection[] objects; int id = -1;
            List<MCvObjectDetection> total_objs = new List<MCvObjectDetection>();

            for (int deg = 0; deg < 360; deg += 30)
            {
                switch (deg)
                {
                    case 0: hog_detection.SetSVMDetector(svm_model_biaoke_chong_64_0); id = 0; break;
                    case 30: hog_detection.SetSVMDetector(svm_model_biaoke_chong_64_30); id = 30; break;
                    case 60: hog_detection.SetSVMDetector(svm_model_biaoke_chong_64_60); id = 60; break;
                    case 90: hog_detection.SetSVMDetector(svm_model_biaoke_chong_64_90); id = 90; break;
                    case 120: hog_detection.SetSVMDetector(svm_model_biaoke_chong_64_120); id = 120; break;
                    case 150: hog_detection.SetSVMDetector(svm_model_biaoke_chong_64_150); id = 150; break;
                    case 180: hog_detection.SetSVMDetector(svm_model_biaoke_chong_64_180); id = 180; break;
                    case 210: hog_detection.SetSVMDetector(svm_model_biaoke_chong_64_210); id = 210; break;
                    case 240: hog_detection.SetSVMDetector(svm_model_biaoke_chong_64_240); id = 240; break;
                    case 270: hog_detection.SetSVMDetector(svm_model_biaoke_chong_64_270); id = 270; break;
                    case 300: hog_detection.SetSVMDetector(svm_model_biaoke_chong_64_300); id = 300; break;
                    case 330: hog_detection.SetSVMDetector(svm_model_biaoke_chong_64_330); id = 330; break;
                }
                objects = hog_detection.DetectMultiScale(img_scaled, hitThreshold, winStride, testPadding, scaleStep, groupThreshold, useMeanShiftGrouping);
                for (int i = 0; i < objects.Length; i++) objects[i].ClassId = id;
                total_objs.AddRange(objects);
            }

            //Rescale back to orignal size and Remove some obvious false alarms
            List<DetectResult> results = new List<DetectResult>();
            for (int i = 0; i < total_objs.Count; i++)
            {
                Rectangle rect = total_objs[i].Rect;
                Rectangle orig_rect = new Rectangle((int)(rect.X / scale + .5), (int)(rect.Y / scale + .5), (int)(rect.Width / scale + .5), (int)(rect.Height / scale + .5));

                //Remove some obvious false alarms
                if (OutsideImage(orig_rect, image.Width, image.Height)) continue;

                //if (orig_rect.Width < 120 || orig_rect.Height < 120) continue;
                if (orig_rect.Width > 250 || orig_rect.Height > 250) continue;

                DetectResult result = new DetectResult();
                result.chong_type = CHONG_TYPE.BIAO_KE_CHONG;
                result.rect = orig_rect;
                result.score = total_objs[i].Score;
                result.deg = total_objs[i].ClassId;
                results.Add(result);
            }

            if (results.Count == 0) return results;

            //Sort patches
            DetectResult[] result_arr = results.ToArray();
            double[] score_arr = new double[results.Count];
            for (int k = 0; k < results.Count; k++) score_arr[k] = -results[k].score;
            Array.Sort(score_arr, result_arr);

            //Remove overlapping ones
            results = new List<DetectResult>(); results.Add(result_arr[0]);
            for (int i = 1; i < result_arr.Length; i++)
            {
                DetectResult obj = result_arr[i];
                Rectangle obj_rect = obj.rect;
                double obj_area = obj_rect.Width * obj_rect.Height;

                //Check with existing ones, if signficantly overlapping with existing ones, ignore
                bool overlapping = false;
                for (int k = 0; k < results.Count; k++)
                {
                    Rectangle result_rect = results[k].rect;
                    Rectangle intersection = Rectangle.Intersect(obj_rect, result_rect);
                    if (intersection.IsEmpty) continue;
                    double intersection_area = intersection.Width * intersection.Height;
                    double result_area = result_rect.Width * result_rect.Height;

                    if (intersection_area > obj_area / 2 || intersection_area > result_area / 2)
                    {
                        overlapping = true; break;
                    }
                }

                if (!overlapping) results.Add(obj);
            }

            //Cropp the patch out and save into results
            for (int i = 0; i < results.Count; i++)
            {
                DetectResult result = results[i];
                image.ROI = result.rect;
                result.patch = image.Resize(winSize_64.Width, winSize_64.Height, Emgu.CV.CvEnum.Inter.Linear);
                image.ROI = Rectangle.Empty;
                results[i] = result;
            }

            return results;
        }

    }
}
