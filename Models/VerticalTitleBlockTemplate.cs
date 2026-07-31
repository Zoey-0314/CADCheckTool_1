namespace Correct_test1.Models
{


    /// <summary>
    /// 竖版标题栏模板
    /// </summary>
    public class VerticalTitleBlockTemplate
    {


        public double LeftX
        {
            get;
            set;
        }


        public double RightX
        {
            get;
            set;
        }


        public double TopY
        {
            get;
            set;
        }


        public double BottomY
        {
            get;
            set;
        }



        public VerticalTitleBlockTemplate()
        {


            //标题栏外框

            LeftX = 82.7599;

            RightX = 282.7611;


            TopY = 105.4386;

            BottomY = 65.4386;



        }





        /*
         
         左侧：

         名称
         材料
         规格
         表面处理

         X:
         82.7599~192.7611


        */


        public double LeftInfoRightX
        {
            get
            {
                return 192.7611;
            }
        }





        /*
         
         图号
         公司
         制图
         校对
         标审
         日期
         页码

        */



        public double MiddleX
        {
            get
            {
                return 232.7611;
            }
        }



        public double DateSplitX
        {
            get
            {
                return 262.7611;
            }
        }





        //横线


        public double DrawingNumberBottomY
        {
            get
            {
                return 97.4386;
            }
        }




        public double CompanyBottomY
        {
            get
            {
                return 77.4386;
            }
        }




        public double DesignerBottomY
        {
            get
            {
                return 73.4386;
            }
        }




        public double ReviewerBottomY
        {
            get
            {
                return 69.4386;
            }
        }




    }


}