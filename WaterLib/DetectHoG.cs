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
    internal enum CHONG_VERF_TYPE
    {
        NEGATIVE = 0,
        ZHONG_0, ZHONG_45, ZHONG_90, ZHONG_135, ZHONG_180, ZHONG_225, ZHONG_270, ZHONG_315,
        BIAOKE_0, //BIAOKE_45, BIAOKE_90, BIAOKE_135, BIAOKE_180, BIAOKE_225, BIAOKE_270, BIAOKE_315,
        CHUIXIGUAN_0, //CHUIXIGUAN_45, CHUIXIGUAN_90, CHUIXIGUAN_135, CHUIXIGUAN_180, CHUIXIGUAN_225, CHUIXIGUAN_270, CHUIXIGUAN_315,
        DANZHILUN_0, //DANZHILUN_45, DANZHILUN_90, DANZHILUN_135, DANZHILUN_180, DANZHILUN_225, DANZHILUN_270, DANZHILUN_315,
        ZHUWENLUN_0, //ZHUWENLUN_45, ZHUWENLUN_90, ZHUWENLUN_135, ZHUWENLUN_180, ZHUWENLUN_225, ZHUWENLUN_270, ZHUWENLUN_315,

        //For later
        QIAOJU_0, //QIAOJU_45, QIAOJU_90, QIAOJU_135, QIAOJU_180, QIAOJU_225, QIAOJU_270, QIAOJU_315,
    }

    public partial class DetectHoG
    {
        private HOGDescriptor hog_detection;
        private HOGDescriptor hog_verfication;

        private static LibSVM svm = null;
        private static LibSVM svmZhong = null;
        private static LibSVM svmBiaoKe = null;

        public DetectHoG()
        {
            hog_detection = new HOGDescriptor(winSize_64, blockSize, blockStride, cellSize, nbins, derivAperture, winSigma, L2HysThreshold, gammaCorrection);
            hog_verfication = new HOGDescriptor(winSize_64, blockSize, blockStride, cellSize, nbins, derivAperture, winSigma, L2HysThreshold, gammaCorrection);
            if (svmZhong == null)
            {
                svmZhong = new LibSVM(); svmZhong.LoadModel("svm_model_zhong");
            }
            if (svmBiaoKe == null)
            {
                svmBiaoKe = new LibSVM(); svmBiaoKe.LoadModel("svm_model_biaoke");
            }
            if (svm == null)
            {
                svm = new LibSVM(); svm.LoadModel("svm_model_verification");
            }
        }

        private float[] FeatureExtractionDetection(IInputArray patch)
        {
            return hog_detection.Compute(patch, winStride, trainPadding, null);
        }

        private float[] FeatureExtractionVerfication(IInputArray patch)
        {
            return hog_verfication.Compute(patch, winStride, trainPadding, null);
        }

        private bool OutsideImage(Rectangle orig_rect, int image_width, int image_height)
        {
            int center_x = orig_rect.X + orig_rect.Width / 2, center_y = orig_rect.Y + orig_rect.Height / 2;

            if (center_x < 0) return true;
            if (center_y < 0) return true;
            if (center_x > image_width) return true;
            if (center_y > image_height) return true;
            if (orig_rect.X + orig_rect.Width / 3 <= 0) return true;
            if (orig_rect.Right - orig_rect.Width / 3 >= image_width) return true;
            if (orig_rect.Y + orig_rect.Height / 3 <= 0) return true;
            if (orig_rect.Bottom - orig_rect.Height / 3 >= image_height) return true;

            return false;
        }

        public DetectResult[] DetectWithRotationModels(Image<Gray, byte> image)
        {
            const double zhong_prob_threshold = 0.75;
            const double biaoke_prob_threshold = 0.90;

            List<List<DetectResult>> chongs = new List<List<DetectResult>>();
            {   //Detect Zhong Chong
                List<DetectResult> zhongs = DetectZhongWithRotationModels(image);
                for (int i = zhongs.Count - 1; i >= 0; i--)
                {
                    DetectResult result = zhongs[i];
                    //image.ROI = result.rect;
                    //Image<Gray, byte> patch = image.Resize(winSize_64.Width, winSize_64.Height, Emgu.CV.CvEnum.Inter.Linear);
                    //image.ROI = Rectangle.Empty;

                    float[] features = FeatureExtractionVerfication(result.patch);
                    double[] probs; svmZhong.PredictProb(features, out probs);

                    if (probs[0] < zhong_prob_threshold) zhongs.RemoveAt(i);
                    else
                    {
                        result.score = probs[0];
                        zhongs[i] = result;
                    }
                }
                chongs.Add(zhongs);
            }

            {   //Detect BiaoKe Chong
                List<DetectResult>  biaokes = DetectBiaoKeWithRotationModels(image);
                for (int i = biaokes.Count - 1; i >= 0; i--)
                {
                    DetectResult result = biaokes[i];
                    //image.ROI = result.rect;
                    //Image<Gray, byte> patch = image.Resize(winSize_64.Width, winSize_64.Height, Emgu.CV.CvEnum.Inter.Linear);
                    //image.ROI = Rectangle.Empty;

                    float[] features = FeatureExtractionVerfication(result.patch);
                    double[] probs; svmBiaoKe.PredictProb(features, out probs);

                    if (probs[0] < biaoke_prob_threshold) biaokes.RemoveAt(i);
                    else
                    {
                        result.score = probs[0];
                        biaokes[i] = result;
                    }
                }
                chongs.Add(biaokes);
            }

            //Check Overlapping cases
            for (int i = 0; i < chongs.Count; i++)
            {
                for (int j = chongs[i].Count - 1; j >= 0; j--)
                {
                    DetectResult chong1 = chongs[i][j];
                    double chong1_area = chong1.rect.Width * chong1.rect.Height;

                    for (int m = i + 1; m < chongs.Count; m++)
                    {
                        for (int n = chongs[m].Count - 1; n >= 0; n--)
                        {
                            DetectResult chong2 = chongs[m][n];
                            //Check whether overlapping
                            Rectangle intersection = Rectangle.Intersect(chong1.rect, chong2.rect);
                            if (intersection.IsEmpty) continue;
                            double intersection_area = intersection.Width * intersection.Height;
                            double chong2_area = chong2.rect.Width * chong2.rect.Height;

                            if (intersection_area > chong1_area / 2 || intersection_area > chong2_area / 2)
                            { //Overlap detected
                                double[] probs;
                                float[] feature1 = FeatureExtractionVerfication(chong1.patch);
                                svm.PredictProb(feature1, out probs);
                                double prob1 = probs[(int)chong1.chong_type];
                                float[] feature2 = FeatureExtractionVerfication(chong2.patch);
                                svm.PredictProb(feature2, out probs);
                                double prob2 = probs[(int)chong2.chong_type];

                                if (prob1 < prob2) chongs[i].RemoveAt(j);
                                else chongs[m].RemoveAt(n);
                            }
                        }
                    }
                }
            }

            List<DetectResult> results = new List<DetectResult>();
            for (int i = 0; i < chongs.Count; i++) results.AddRange(chongs[i]);

            return results.ToArray();

            //List<DetectResult> total_objs = new List<DetectResult>();
            ////total_objs.AddRange(DetectZhongWithRotationModels(image)); //Detecting Zhong Chong
            ////total_objs.AddRange(DetectBiaoKeWithRotationModels(image)); //Detecting Biaoke Chong
            ////total_objs.AddRange(DetectChuiXiGuanWithRotationModels(image)); //Detecting ChuiXiGuan Chong
            ////total_objs.AddRange(DetectDanZhiLunWithRotationModels(image)); //Detecting DanZhiLun Chong
            ////total_objs.AddRange(DetectZhuWenLunWithRotationModels(image)); //Detecting ZhuWenLun Chong
            ////total_objs.AddRange(DetectQiaoJuWithRotationModels(image)); //Detecting QiaoJu Chong

            ////Computes the prob for each obj and remove those are classified as NEGATIVE
            //List<DetectResult> result_list = new List<DetectResult>();
            //List<double> score_list = new List<double>();
            //for (int i = 0; i < total_objs.Count; i++)
            //{
            //    DetectResult result = total_objs[i];

            //    image.ROI = result.rect;
            //    Image<Gray, byte> patch = image.Resize(winSize_64.Width, winSize_64.Height, Emgu.CV.CvEnum.Inter.Linear);
            //    image.ROI = Rectangle.Empty;

            //    result.patch = patch;

            //    float[] features = FeatureExtractionVerfication(patch);
            //    double[] probs; svm.PredictProb(features, out probs);

            //    //Negative prob
            //    double prob_negative = probs[0];
            //    double max_prob = prob_negative; CHONG_VERF_TYPE max_type = CHONG_VERF_TYPE.NEGATIVE;

            //    {//ZHONG CHONG prob
            //        //double prob_zhong = 0; for (int k = (int)CHONG_VERF_TYPE.ZHONG_0; k <= (int)CHONG_VERF_TYPE.ZHONG_315; k++) prob_zhong += probs[k];
            //        double prob_zhong = probs[1];
            //        if (prob_zhong > max_prob) { max_prob = prob_zhong; max_type = CHONG_VERF_TYPE.ZHONG_0; }
            //    }

            //    {//BIAOKE CHONG prob
            //        //double prob_biaoke = 0; for (int k = (int)CHONG_VERF_TYPE.BIAOKE_0; k <= (int)CHONG_VERF_TYPE.BIAOKE_315; k++) prob_biaoke += probs[k];
            //        double prob_biaoke = probs[2];
            //        if (prob_biaoke > max_prob) { max_prob = prob_biaoke; max_type = CHONG_VERF_TYPE.BIAOKE_0; }
            //    }

            //    {//CHUIXIGUAN CHONG prob
            //        //double prob_chuixiguan = 0; for (int k = (int)CHONG_VERF_TYPE.CHUIXIGUAN_0; k <= (int)CHONG_VERF_TYPE.CHUIXIGUAN_315; k++) prob_chuixiguan += probs[k];
            //        double prob_chuixiguan = probs[3];
            //        if (prob_chuixiguan > max_prob) { max_prob = prob_chuixiguan; max_type = CHONG_VERF_TYPE.CHUIXIGUAN_0; }
            //    }

            //    {//DANZHILUN CHONG prob
            //        //double prob_danzhilun = 0; for (int k = (int)CHONG_VERF_TYPE.DANZHILUN_0; k <= (int)CHONG_VERF_TYPE.DANZHILUN_315; k++) prob_danzhilun += probs[k];
            //        double prob_danzhilun = probs[4];
            //        if (prob_danzhilun > max_prob) { max_prob = prob_danzhilun; max_type = CHONG_VERF_TYPE.DANZHILUN_0; }
            //    }

            //    {//ZHUWENLUN CHONG prob
            //        //double prob_zhuwenlun = 0; for (int k = (int)CHONG_VERF_TYPE.DANZHILUN_0; k <= (int)CHONG_VERF_TYPE.DANZHILUN_315; k++) prob_zhuwenlun += probs[k];
            //        double prob_zhuwenlun = probs[5];
            //        if (prob_zhuwenlun > max_prob) { max_prob = prob_zhuwenlun; max_type = CHONG_VERF_TYPE.ZHUWENLUN_0; }
            //    }

            //    if (max_type == CHONG_VERF_TYPE.NEGATIVE) continue;

            //    result.score = max_prob;
            //    if (max_type == CHONG_VERF_TYPE.ZHONG_0)
            //    {
            //        if (result.chong_type != CHONG_TYPE.ZHONG_CHONG) continue; //If the detected type is different from verified type, we ignore. So, verification is mostly used for rejecting.
            //        result.chong_type = CHONG_TYPE.ZHONG_CHONG;
            //    }
            //    else if (max_type == CHONG_VERF_TYPE.BIAOKE_0)
            //    {
            //        if (result.chong_type != CHONG_TYPE.BIAO_KE_CHONG) continue; //If the detected type is different from verified type, we ignore. So, verification is mostly used for rejecting.
            //        result.chong_type = CHONG_TYPE.BIAO_KE_CHONG;
            //    }
            //    else if (max_type == CHONG_VERF_TYPE.CHUIXIGUAN_0)
            //    {
            //        if (result.chong_type != CHONG_TYPE.CHUI_XI_GUAN_CHONG) continue; //If the detected type is different from verified type, we ignore. So, verification is mostly used for rejecting.
            //        result.chong_type = CHONG_TYPE.CHUI_XI_GUAN_CHONG;
            //    }
            //    else if (max_type == CHONG_VERF_TYPE.DANZHILUN_0)
            //    {
            //        if (result.chong_type != CHONG_TYPE.DAN_ZHI_LUN_CHONG) continue; //If the detected type is different from verified type, we ignore. So, verification is mostly used for rejecting.
            //        result.chong_type = CHONG_TYPE.DAN_ZHI_LUN_CHONG;
            //    }
            //    else if (max_type == CHONG_VERF_TYPE.ZHUWENLUN_0)
            //    {
            //        if (result.chong_type != CHONG_TYPE.ZHU_WEN_LUN_CHONG) continue; //If the detected type is different from verified type, we ignore. So, verification is mostly used for rejecting.
            //        result.chong_type = CHONG_TYPE.ZHU_WEN_LUN_CHONG;
            //    }

            //    result_list.Add(result);
            //    score_list.Add(-max_prob);
            //}

            //if (result_list.Count == 0) return null;

            ////Sort them in descenting order of prob
            //DetectResult[] result_arr = result_list.ToArray();
            //double[] score_arr = score_list.ToArray();
            //Array.Sort(score_arr, result_arr);

            ////Remove overlapping ones
            //List<DetectResult> results = new List<DetectResult>(); results.Add(result_arr[0]);
            //for (int i = 1; i < result_arr.Length; i++)
            //{
            //    DetectResult obj = result_arr[i];
            //    Rectangle obj_rect = obj.rect;
            //    double obj_area = obj_rect.Width * obj_rect.Height;

            //    //Check with existing ones, if signficantly overlapping with existing ones, ignore
            //    bool overlapping = false;
            //    for (int k = 0; k < results.Count; k++)
            //    {
            //        Rectangle result_rect = results[k].rect;
            //        Rectangle intersection = Rectangle.Intersect(obj_rect, result_rect);
            //        if (intersection.IsEmpty) continue;
            //        double intersection_area = intersection.Width * intersection.Height;
            //        double result_area = result_rect.Width * result_rect.Height;

            //        if (intersection_area > obj_area / 2 || intersection_area > result_area / 2)
            //        {
            //            overlapping = true; break;
            //        }
            //    }

            //    if (!overlapping) results.Add(obj);
            //}

            ////Remove low prob ones
            //for (int i = results.Count - 1; i >= 0; i--)
            //{
            //    DetectResult obj = results[i];
            //    if (obj.chong_type == CHONG_TYPE.ZHONG_CHONG && obj.score < 0.90) { results.RemoveAt(i); continue; }
            //    if (obj.chong_type == CHONG_TYPE.BIAO_KE_CHONG && obj.score < 0.80) { results.RemoveAt(i); continue; }
            //    if (obj.chong_type == CHONG_TYPE.CHUI_XI_GUAN_CHONG && obj.score < 0.80) { results.RemoveAt(i); continue; }
            //    if (obj.chong_type == CHONG_TYPE.DAN_ZHI_LUN_CHONG && obj.score < 0.95) { results.RemoveAt(i); continue; }
            //    if (obj.chong_type == CHONG_TYPE.ZHU_WEN_LUN_CHONG && obj.score < 0.95) { results.RemoveAt(i); continue; }
            //}

            ////2nd level SVM (which is for individual chong only) to reduce false alarms.
            //List<DetectResult> final_results = new List<DetectResult>();
            //for (int i = 0; i < results.Count; i++)
            //{
            //    DetectResult obj = results[i];
            //    if (obj.chong_type == CHONG_TYPE.ZHONG_CHONG)
            //    {
            //        float[] features = FeatureExtractionVerfication(obj.patch);
            //        double[] probs; svmZhong.PredictProb(features, out probs);
            //        if (probs[0] > 0.5) continue;
            //        obj.score = 1 - probs[0];
            //        final_results.Add(obj);
            //    }
            //    else
            //        final_results.Add(obj);
            //}

            //return final_results.ToArray();
        }

        static private Size winSize_64 = new Size(64, 64);

        static protected Size blockSize = new Size(16, 16);
        static protected Size blockStride = new Size(8, 8);
        static protected Size cellSize = new Size(8, 8);
        static protected Size winStride = new Size(8, 8);
        static protected Size trainPadding = new Size(0, 0);
        static protected int nbins = 9;
        static protected int derivAperture = 1;
        static protected double winSigma = -1;
        static protected double L2HysThreshold = 0.2;
        static protected bool gammaCorrection = true;
        static protected int nLevels = 64;

        static protected Size testPadding = new Size(32, 32);
        static protected double hitThreshold = 0;
        static protected int groupThreshold = 2;
        static protected double scaleStep = 1.05;
        static protected bool useMeanShiftGrouping = false;
    }
}
