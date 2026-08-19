using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using System.Collections.Generic;


namespace Correct_test1.Models
{
    /// <summary>
    /// 单个BOM序号问题。
    /// 当前对象
    /// 现在每一个问题都明确携带：
    /// LayoutName
    /// Number
    /// Position
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
    /// MissingCallouts / ExtraCallouts 用于兼容调用方；
    /// 绘制标记使用携带布局信息的 MissingIssues / ExtraIssues。
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
