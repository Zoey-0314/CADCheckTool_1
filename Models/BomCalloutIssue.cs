using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using System.Collections.Generic;


namespace Correct_test1.Models
{
    /// <summary>
    /// 单个BOM序号问题。
    ///
    /// 与旧版相比，
    /// 现在每一个问题都明确携带：
    ///
    /// LayoutName
    /// Number
    /// Position
    ///
    /// 因此不会再丢失布局信息。
    /// </summary>
    public class BomCalloutIssue
    {
        public int Number
        {
            get;
            set;
        }


        public string LayoutName
        {
            get;
            set;
        }
        =
        "";


        public Point3d Position
        {
            get;
            set;
        }


        public ObjectId SpaceId
        {
            get;
            set;
        }


        public string Message
        {
            get;
            set;
        }
        =
        "";
    }


    /// <summary>
    /// BOM序号检查结果。
    ///
    /// MissingCallouts / ExtraCallouts
    /// 继续保留，
    /// 避免旧代码立即失效。
    ///
    /// 新增：
    ///
    /// MissingIssues
    /// ExtraIssues
    ///
    /// 真正绘制Marker时，
    /// 后续统一使用Issue，
    /// 因为Issue携带Layout信息。
    /// </summary>
    public class BomCalloutResult
    {
        public HashSet<int> MissingCallouts
        {
            get;
            set;
        }
        =
        new HashSet<int>();


        public HashSet<int> ExtraCallouts
        {
            get;
            set;
        }
        =
        new HashSet<int>();


        public List<BomCalloutIssue> MissingIssues
        {
            get;
            set;
        }
        =
        new List<BomCalloutIssue>();


        public List<BomCalloutIssue> ExtraIssues
        {
            get;
            set;
        }
        =
        new List<BomCalloutIssue>();
    }
}