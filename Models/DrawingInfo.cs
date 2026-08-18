using System.Collections.Generic;

namespace Correct_test1.Models
{
    /// <summary>
    /// 单张图纸信息
    /// </summary>
    public class DrawingInfo
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 项目号
        /// </summary>
        public string ProjectNumber { get; set; }
        //=========================
        // 标题栏基础信息
        //=========================

        /// <summary>
        /// 图号
        /// </summary>
        public string DrawingNumber { get; set; }

        /// <summary>
        /// 图纸名称
        /// </summary>
        public string DrawingName { get; set; }
        /// <summary>
        /// 材料
        /// </summary>
        public string Material { get; set; }

        /// <summary>
        /// 规格
        /// </summary>
        public string Specification { get; set; }

        /// <summary>
        /// 表面处理
        /// </summary>
        public string SurfaceTreatment { get; set; }

        //=========================
        // 签字栏
        //=========================

        /// <summary>
        /// 制图
        /// </summary>
        public string Designer { get; set; }

        /// <summary>
        /// 校对
        /// </summary>
        public string Checker { get; set; }

        /// <summary>
        /// 标审
        /// </summary>
        public string Reviewer { get; set; }

        /// <summary>
        /// 批准
        /// </summary>
        public string Approver { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public string TitleDate { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public string PageNumber { get; set; }

        public List<TitleText> PageNumberSourceTexts
        {
            get;
            set;
        }
=
new List<TitleText>();

        //=========================
        // 图纸结构信息
        //=========================

        /// <summary>
        /// 所属布局
        /// </summary>
        public string LayoutName { get; set; }

        /// <summary>
        /// 是否横版
        /// true 横版
        /// false 竖版
        /// </summary>
        public bool IsHorizontal { get; set; }

        /// <summary>
        /// 修改记录
        /// </summary>
        public List<RevisionInfo> Revisions { get; set; }

        public DrawingInfo()
        {
            Revisions = new List<RevisionInfo>();
        }
    }
}