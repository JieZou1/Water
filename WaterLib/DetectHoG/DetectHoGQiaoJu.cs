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
        private List<DetectResult> DetectQiaoJuWithRotationModels(Image<Gray, byte> image)
        {
            //resize the image
            double scale = winSize_64.Height / 192.0; //check statistics.txt to decide this scaling factor.
            Image<Gray, byte> img_scaled = image.Resize(scale, Emgu.CV.CvEnum.Inter.Linear);

            MCvObjectDetection[] objects;
            List<MCvObjectDetection> total_objs = new List<MCvObjectDetection>();

            //0 degree model
            hog_detection.SetSVMDetector(svm_model_qiaoju_chong_64_0);
            objects = hog_detection.DetectMultiScale(img_scaled, hitThreshold, winStride, testPadding, scaleStep, groupThreshold, useMeanShiftGrouping);
            total_objs.AddRange(objects);

            //45 degree model
            hog_detection.SetSVMDetector(svm_model_qiaoju_chong_64_45);
            objects = hog_detection.DetectMultiScale(img_scaled, hitThreshold, winStride, testPadding, scaleStep, groupThreshold, useMeanShiftGrouping);
            total_objs.AddRange(objects);

            //90 degree model
            hog_detection.SetSVMDetector(svm_model_qiaoju_chong_64_90);
            objects = hog_detection.DetectMultiScale(img_scaled, hitThreshold, winStride, testPadding, scaleStep, groupThreshold, useMeanShiftGrouping);
            total_objs.AddRange(objects);

            //135 degree model
            hog_detection.SetSVMDetector(svm_model_qiaoju_chong_64_135);
            objects = hog_detection.DetectMultiScale(img_scaled, hitThreshold, winStride, testPadding, scaleStep, groupThreshold, useMeanShiftGrouping);
            total_objs.AddRange(objects);

            //180 degree model
            hog_detection.SetSVMDetector(svm_model_qiaoju_chong_64_180);
            objects = hog_detection.DetectMultiScale(img_scaled, hitThreshold, winStride, testPadding, scaleStep, groupThreshold, useMeanShiftGrouping);
            total_objs.AddRange(objects);

            //225 degree model
            hog_detection.SetSVMDetector(svm_model_qiaoju_chong_64_225);
            objects = hog_detection.DetectMultiScale(img_scaled, hitThreshold, winStride, testPadding, scaleStep, groupThreshold, useMeanShiftGrouping);
            total_objs.AddRange(objects);

            //270 degree model
            hog_detection.SetSVMDetector(svm_model_qiaoju_chong_64_270);
            objects = hog_detection.DetectMultiScale(img_scaled, hitThreshold, winStride, testPadding, scaleStep, groupThreshold, useMeanShiftGrouping);
            total_objs.AddRange(objects);

            // 315 degree model
            hog_detection.SetSVMDetector(svm_model_qiaoju_chong_64_315);
            objects = hog_detection.DetectMultiScale(img_scaled, hitThreshold, winStride, testPadding, scaleStep, groupThreshold, useMeanShiftGrouping);
            total_objs.AddRange(objects);

            //Rescale back to orignal size and Remove some obvious false alarms
            List<DetectResult> results = new List<DetectResult>();
            for (int i = 0; i < total_objs.Count; i++)
            {
                Rectangle rect = total_objs[i].Rect;
                Rectangle orig_rect = new Rectangle((int)(rect.X / scale + .5), (int)(rect.Y / scale + .5), (int)(rect.Width / scale + .5), (int)(rect.Height / scale + .5));

                //Remove some obvious false alarms
                if (OutsideImage(orig_rect, image.Width, image.Height)) continue;

                //if (orig_rect.Width < 120 || orig_rect.Height < 120) continue;
                if (orig_rect.Width > 500 || orig_rect.Height > 500) continue;

                DetectResult result = new DetectResult();
                result.chong_type = CHONG_TYPE.QIAO_JU_CHONG;
                result.rect = orig_rect;
                result.score = total_objs[i].Score;
                results.Add(result);
            }

            return results;
        }

    }
}
