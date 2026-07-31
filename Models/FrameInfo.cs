using System;


namespace Correct_test1.Models
{

    /// <summary>
    /// 图框信息
    /// 
    /// 保存图纸外框尺寸
    /// 用于判断横竖版
    /// </summary>
    public class FrameInfo
    {


        /// <summary>
        /// 最小X
        /// </summary>
        public double MinX
        {
            get;
            set;
        }



        /// <summary>
        /// 最小Y
        /// </summary>
        public double MinY
        {
            get;
            set;
        }



        /// <summary>
        /// 最大X
        /// </summary>
        public double MaxX
        {
            get;
            set;
        }



        /// <summary>
        /// 最大Y
        /// </summary>
        public double MaxY
        {
            get;
            set;
        }





        public double Width
        {
            get
            {
                return MaxX - MinX;
            }
        }





        public double Height
        {
            get
            {
                return MaxY - MinY;
            }
        }






        /// <summary>
        /// 图纸方向
        /// Horizontal
        /// Vertical
        /// </summary>
        public string Direction
        {
            get;
            set;
        }





        public FrameInfo()
        {

            Direction =
                "Unknown";

        }



    }

}