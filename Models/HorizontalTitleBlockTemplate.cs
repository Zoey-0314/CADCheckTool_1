namespace Correct_test1.Models
{


    /// <summary>
    /// 横版修改记录模板
    /// </summary>
    public class HorizontalRevisionTemplate
    {


        /*
         
         横版修改记录区域

         根据你提供的横板图纸数据

         */


        /// <summary>
        /// 左边界
        /// </summary>
        public double MinX
        {
            get;
            set;
        }



        /// <summary>
        /// 右边界
        /// </summary>
        public double MaxX
        {
            get;
            set;
        }



        /// <summary>
        /// 下边界
        /// </summary>
        public double MinY
        {
            get;
            set;
        }



        /// <summary>
        /// 上边界
        /// </summary>
        public double MaxY
        {
            get;
            set;
        }





        public HorizontalRevisionTemplate()
        {


            /*
             
             横版修改记录大区域

             根据横板：

             更改记录标题
             标记
             更改内容
             日期
             签名
             变更号

             */


            MinX = 14.0;


            MaxX = 330.0;



            MinY = 37.0;


            MaxY = 77.5;



        }






        //列分割


        /// <summary>
        /// 标记右边
        /// </summary>
        public double MarkEndX
        {
            get
            {
                return 53.0;
            }
        }





        /// <summary>
        /// 内容右边
        /// </summary>
        public double DescriptionEndX
        {
            get
            {
                return 220.0;
            }
        }






        /// <summary>
        /// 日期右边
        /// </summary>
        public double DateEndX
        {
            get
            {
                return 260.0;
            }
        }





        /// <summary>
        /// 签名右边
        /// </summary>
        public double SignerEndX
        {
            get
            {
                return 300.0;
            }
        }



    }


}