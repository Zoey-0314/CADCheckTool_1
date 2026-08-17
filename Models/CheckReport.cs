using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;

using Correct_test1.VersionCheck.Models;


namespace Correct_test1.Models
{
    public class CheckReport
    {
        public string DrawingName
        {
            get;
            set;
        }


        public string DrawingNumber
        {
            get;
            set;
        }


        public Point3d DrawingNumberPosition
        {
            get;
            set;
        }


        public DateTime CheckTime
        {
            get;
            set;
        }


        public int TotalCount
        {
            get;
            set;
        }


        public int CorrectCount
        {
            get;
            set;
        }


        public int ErrorCount
        {
            get;
            set;
        }


        //==================================================
        // 标准件检查
        //==================================================

        public List<StandardPartCheckResult>
            Results
        {
            get;
            set;
        }
        =
        new List<StandardPartCheckResult>();


        //==================================================
        // 非标归档检查
        //==================================================

        public List<NonStandardArchiveCheckResult>
            NonStandardArchiveResults
        {
            get;
            set;
        }
        =
        new List<NonStandardArchiveCheckResult>();


        public bool NonStandardArchiveAvailable
        {
            get;
            set;
        }


        public string NonStandardArchiveError
        {
            get;
            set;
        }
        =
        "";


        //==================================================
        // 新增：版本号检查
        //==================================================

        public List<VersionCheckResult>
            VersionCheckResults
        {
            get;
            set;
        }
        =
        new List<VersionCheckResult>();


        /// <summary>
        /// 版本归档目录本次是否可用。
        /// </summary>
        public bool VersionArchiveAvailable
        {
            get;
            set;
        }


        /// <summary>
        /// 版本归档不可用时的原因。
        /// </summary>
        public string VersionArchiveError
        {
            get;
            set;
        }
        =
        "";


        //==================================================
        // 标准件数据库状态
        //==================================================

        public bool StandardPartDatabaseAvailable
        {
            get;
            set;
        }


        public string StandardPartDatabaseError
        {
            get;
            set;
        }
        =
        "";


        //==================================================
        // BOM序号
        //==================================================

        public BomCalloutResult BomCalloutResult
        {
            get;
            set;
        }
        =
        new BomCalloutResult();


        public List<BomData>
            Boms
        {
            get;
            set;
        }
        =
        new List<BomData>();


        public List<TitleText>
            DrawingTexts
        {
            get;
            set;
        }
        =
        new List<TitleText>();


        public HashSet<int>
            BomNumbers
        {
            get;
            set;
        }
        =
        new HashSet<int>();


        public HashSet<int>
            DrawingNumbers
        {
            get;
            set;
        }
        =
        new HashSet<int>();
    }
}