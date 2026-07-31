using System.Collections.Generic;


namespace Correct_test1.Models
{

    /// <summary>
    /// 标题栏标准数据模型
    /// 保存从CAD标题栏解析出的信息
    /// </summary>
    public class TitleBlockData
    {


        /// <summary>
        /// 标题栏类型
        /// A: 普通标题栏
        /// B: 带修改记录标题栏
        /// </summary>
        public string TemplateType
        {
            get;
            set;
        }



        //--------------------------------
        // 基础信息
        //--------------------------------


        /// <summary>
        /// 图号
        /// 例如：
        /// NS135H
        /// NS265R
        /// </summary>
        public string DrawingNumber
        {
            get;
            set;
        }



        /// <summary>
        /// 图纸名称
        /// 来自文件名和标题栏验证
        /// </summary>
        public string DrawingName
        {
            get;
            set;
        }



        /// <summary>
        /// 公司名称
        /// </summary>
        public string CompanyName
        {
            get;
            set;
        }



        /// <summary>
        /// 英文公司名称
        /// </summary>
        public string CompanyEnglishName
        {
            get;
            set;
        }




        //--------------------------------
        // 零件信息
        //--------------------------------


        /// <summary>
        /// 材料
        /// </summary>
        public string Material
        {
            get;
            set;
        }



        /// <summary>
        /// 规格
        /// </summary>
        public string Specification
        {
            get;
            set;
        }



        /// <summary>
        /// 表面处理
        /// </summary>
        public string SurfaceTreatment
        {
            get;
            set;
        }







        //--------------------------------
        // 人员信息
        //--------------------------------


        /// <summary>
        /// 制图人员
        /// </summary>
        public string Designer
        {
            get;
            set;
        }



        /// <summary>
        /// 校对人员
        /// </summary>
        public string Checker
        {
            get;
            set;
        }



        /// <summary>
        /// 标审人员
        /// </summary>
        public string Reviewer
        {
            get;
            set;
        }



        /// <summary>
        /// 批准人员
        /// </summary>
        public string Approver
        {
            get;
            set;
        }





        //--------------------------------
        // 日期
        //--------------------------------


        /// <summary>
        /// 标题栏发布日期
        /// 注意：
        /// 不是修改记录日期
        /// </summary>
        public string DrawingDate
        {
            get;
            set;
        }







        //--------------------------------
        // 修改记录
        //--------------------------------


        /// <summary>
        /// 修改记录列表
        /// 一个图可能有多条修改记录
        /// </summary>
        public List<RevisionInfo> Revisions
        {
            get;
            set;
        }



        public TitleBlockData()
        {

            Revisions =
                new List<RevisionInfo>();

        }



    }


}