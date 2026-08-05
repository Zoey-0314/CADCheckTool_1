using System.Collections.Generic;

using Correct_test1.Models;


namespace Correct_test1.Configs
{

    /// <summary>
    /// 竖版标题栏区域配置
    /// </summary>
    public static class TitleBlockVerticalConfig
    {

        public static List<TitleFieldRegion> Regions =
            new List<TitleFieldRegion>
            {

                new TitleFieldRegion
                {
                    FieldName="DrawingName",

                    IsHorizontal=false,

                    MinX=192.7611,
                    MaxX=232.7611,

                    MinY=81.4386,
                    MaxY=105.4386
                },


                new TitleFieldRegion
                {
                    FieldName="DrawingNumber",

                    IsHorizontal=false,

                    MinX=232.7611,
                    MaxX=282.7611,

                    MinY=97.4386,
                    MaxY=105.4386
                },


                new TitleFieldRegion
                {
                    FieldName="Material",

                    IsHorizontal=false,

                    MinX=192.7611,
                    MaxX=232.7611,

                    MinY=73.4386,
                    MaxY=81.4386
                },


                new TitleFieldRegion
                {
                    FieldName="Specification",

                    IsHorizontal=false,

                    MinX=192.7611,
                    MaxX=232.7611,

                    MinY=69.4386,
                    MaxY=73.4386
                },


                new TitleFieldRegion
                {
                    FieldName="SurfaceTreatment",

                    IsHorizontal=false,

                    MinX=192.7611,
                    MaxX=232.7611,

                    MinY=65.4386,
                    MaxY=69.4386
                },


                new TitleFieldRegion
                {
                    FieldName="Designer",

                    IsHorizontal=false,

                    MinX=232.7611,
                    MaxX=262.7611,

                    MinY=77.4386,
                    MaxY=81.4386
                },


                new TitleFieldRegion
                {
                    FieldName="Checker",

                    IsHorizontal=false,

                    MinX=232.7611,
                    MaxX=262.7611,

                    MinY=73.4386,
                    MaxY=77.4386
                },


                new TitleFieldRegion
                {
                    FieldName="Reviewer",

                    IsHorizontal=false,

                    MinX=232.7611,
                    MaxX=262.7611,

                    MinY=69.4386,
                    MaxY=73.4386
                },


                new TitleFieldRegion
                {
                    FieldName="Approver",

                    IsHorizontal=false,

                    MinX=232.7611,
                    MaxX=262.7611,

                    MinY=65.4386,
                    MaxY=69.4386
                },


                new TitleFieldRegion
                {
                    FieldName="TitleDate",

                    IsHorizontal=false,

                    MinX=262.7611,
                    MaxX=282.7611,

                    MinY=73.4386,
                    MaxY=81.4386
                },


                new TitleFieldRegion
                {
                    FieldName="PageNumber",

                    IsHorizontal=false,

                    MinX=262.7611,
                    MaxX=282.7611,

                    MinY=65.4386,
                    MaxY=73.4386
                }

            };


    }

}