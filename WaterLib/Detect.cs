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
    public enum CHONG_TYPE
    {
        BIAO_KE_CHONG,
        ZHONG_CHONG,
        CHUI_XI_GUAN_CHONG,
        DAN_ZHI_LUN_CHONG,
        ZHU_WEN_LUN_CHONG,

        //For later
        QIAO_JU_CHONG,
        LIN_KE_CHONG,
        XI_GUAN_CHONG,
    }

    public struct DetectResult
    {
        public Image<Gray, byte> patch;
        public Rectangle rect;
        public double score;
        public CHONG_TYPE chong_type;
        public int deg;
    }
}
