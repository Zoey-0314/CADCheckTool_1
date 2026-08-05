using System.Collections.Generic;

using Correct_test1.Models;


namespace Correct_test1.Configs
{

    /// <summary>
    /// 横版标题栏区域配置
    /// </summary>
    public static class TitleBlockHorizontalConfig
    {


        public static List<TitleFieldRegion> Regions =
            new List<TitleFieldRegion>
            {

                new TitleFieldRegion
                {
                    FieldName="DrawingName",

                    IsHorizontal=true,

                    MinX=329.8816,
                    MaxX=389.8816,

                    MinY=53.3206,
                    MaxY=77.3206
                },


                new TitleFieldRegion
                {
                    FieldName="DrawingNumber",

                    IsHorizontal=true,

                    MinX=389.8816,
                    MaxX=449.8816,

                    MinY=69.3206,
                    MaxY=77.3206
                },


                new TitleFieldRegion
                {
                    FieldName="Material",

                    IsHorizontal=true,

                    MinX=329.8816,
                    MaxX=389.8816,

                    MinY=45.3206,
                    MaxY=53.3206
                },


                new TitleFieldRegion
                {
                    FieldName="Specification",

                    IsHorizontal=true,

                    MinX=329.8816,
                    MaxX=389.8816,

                    MinY=41.3206,
                    MaxY=45.3206
                },


                new TitleFieldRegion
                {
                    FieldName="SurfaceTreatment",

                    IsHorizontal=true,

                    MinX=329.8816,
                    MaxX=389.8816,

                    MinY=37.3206,
                    MaxY=41.3206
                },


                new TitleFieldRegion
                {
                    FieldName="Designer",

                    IsHorizontal=true,

                    MinX=389.8816,
                    MaxX=429.8816,

                    MinY=49.3206,
                    MaxY=53.3206
                },


                new TitleFieldRegion
                {
                    FieldName="Checker",

                    IsHorizontal=true,

                    MinX=389.8816,
                    MaxX=429.8816,

                    MinY=45.3206,
                    MaxY=49.3206
                },


                new TitleFieldRegion
                {
                    FieldName="Reviewer",

                    IsHorizontal=true,

                    MinX=389.8816,
                    MaxX=429.8816,

                    MinY=41.3206,
                    MaxY=45.3206
                },


                new TitleFieldRegion
                {
                    FieldName="Approver",

                    IsHorizontal=true,

                    MinX=389.8816,
                    MaxX=429.8816,

                    MinY=37.3206,
                    MaxY=41.3206
                },


                new TitleFieldRegion
                {
                    FieldName="TitleDate",

                    IsHorizontal=true,

                    MinX=429.8816,
                    MaxX=449.8816,

                    MinY=45.3206,
                    MaxY=53.3206
                },


                new TitleFieldRegion
                {
                    FieldName="PageNumber",

                    IsHorizontal=true,

                    MinX=429.8816,
                    MaxX=449.8816,

                    MinY=37.3206,
                    MaxY=45.3206
                }

            };


    }

}