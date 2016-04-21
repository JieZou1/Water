using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WaterLib;

namespace WaterTrain
{
    class Program
    {
        static void Main(string[] args)
        {
            CHONG_TYPE chong_type = CHONG_TYPE.BIAO_KE_CHONG;

            //DetectHoG.CropChong(chong_type);

            //DetectHoG.NormalizeChong(chong_type);
            //DetectHoG.NormalizeChongWithRotation(chong_type);

            //DetectHoG.SampleStatistics(chong_type);

            //DetectHoG.NegativePatches(chong_type);

            DetectHoG.TrainDetection(chong_type);
            //DetectHoG.ToSingleVector(chong_type);
            //DetectHoG.BootStrapDetection(chong_type);
            //DetectHoG.TrainVerification(chong_type);
            //DetectHoG.TrainFinalDecision();

            //DetectHoG.TrainVerify();
            //DetectHoG.TrainVerifySingleChong();

            //DetectHoG.BootStrap(CHONG_TYPE.CHUI_XI_GUAN_CHONG);
            //DetectHoG.BootStrap(CHONG_TYPE.ZHONG_CHONG);
            //DetectHoG.BootStrap(CHONG_TYPE.BIAO_KE_CHONG);
            //DetectHoG.BootStrapNegatives();

            //CompareResults();
        }

        static void CompareResults()
        {
            string[] files = System.IO.Directory.GetFiles(@"\users\jie\projects\Water1\programs\Debug\Zhong");
            List<string> list1 = new List<string>(); List<int> counts1 = new List<int>();
            for (int i = 0; i < files.Length; i++)
            {
                string file = System.IO.Path.GetFileNameWithoutExtension(files[i]).Split(new char[] { '{' })[0];
                int index = list1.IndexOf(file);
                if (index == -1) { list1.Add(file); counts1.Add(1); }
                else counts1[index]++;
            }
            List<string> list2 = new List<string>(); List<int> counts2 = new List<int>();
            using (System.IO.StreamReader sr = new System.IO.StreamReader(@"\BaiduYunDownload\2016.01.08\Negative\evaluation20160108.txt"))
            {
                string line = sr.ReadLine(); string[] words;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("Total")) break;
                    words = line.Split();
                    int count = int.Parse(words[1]);
                    if (count == 0) continue;

                    list2.Add(words[0]); counts2.Add(count);
                }
            }
        }
    }
}
