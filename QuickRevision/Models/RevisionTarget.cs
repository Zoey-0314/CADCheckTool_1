using Autodesk.AutoCAD.DatabaseServices;

namespace Correct_test1.QuickRevision.Models
{
    /// <summary>
    /// QuickRevision统一目标模型。
    /// 无论原始对象是：
    /// DBText
    /// MText
    /// Dimension
    /// TableCell
    /// 最终统一转换为RevisionTarget。
    /// </summary>
    public class RevisionTarget
    {
        /// <summary>
        /// 原始CAD对象ObjectId。
        /// </summary>
        public ObjectId SourceId
        {
            get;
            set;
        }


        /// <summary>
        /// 原始对象类型。
        /// 例如：
        /// DBText
        /// MText
        /// RotatedDimension
        /// TableCell[3,2]
        /// </summary>
        public string SourceType
        {
            get;
            set;
        }


        /// <summary>
        /// 用户修改前看到的内容。
        /// AB项目号判断也使用这个原始内容，
        /// 而不是用户输入的新内容。
        /// </summary>
        public string Text
        {
            get;
            set;
        }


        /// <summary>
        /// 快速划改新实体应该写入的空间。
        /// PaperSpace目标：
        /// 对应Layout BlockTableRecord。
        /// Viewport目标：
        /// ModelSpace。
        /// </summary>
        public ObjectId TargetSpaceId
        {
            get;
            set;
        }


        /// <summary>
        /// 原文字实际范围最左侧X。
        /// </summary>
        public double LeftX
        {
            get;
            set;
        }


        /// <summary>
        /// 原文字实际范围最右侧X。
        /// </summary>
        public double RightX
        {
            get;
            set;
        }


        /// <summary>
        /// 原文字实际范围最下侧Y。
        /// </summary>
        public double BottomY
        {
            get;
            set;
        }


        /// <summary>
        /// 原文字实际范围最上侧Y。
        /// </summary>
        public double TopY
        {
            get;
            set;
        }


        /// <summary>
        /// 原文字中心Y。
        /// StrikeLineWriter目前使用这个值
        /// 创建水平删除线。
        /// </summary>
        public double CenterY
        {
            get;
            set;
        }


        /// <summary>
        /// 原文字高度。
        /// ReplacementTextWriter和以后
        /// ProjectNumberWriter都会尽量继承这个高度。
        /// </summary>
        public double TextHeight
        {
            get;
            set;
        }


        /// <summary>
        /// 原文字宽度。
        /// </summary>
        public double TextWidth
        {
            get
            {
                return
                    RightX -
                    LeftX;
            }
        }


        /// <summary>
        /// 是否来自Viewport内部ModelSpace。
        /// </summary>
        public bool IsInViewport
        {
            get;
            set;
        }


        /// <summary>
        /// 来自Viewport时保存对应Viewport。
        /// PaperSpace对象保持ObjectId.Null。
        /// </summary>
        public ObjectId ViewportId
        {
            get;
            set;
        }


        /// <summary>
        /// 原文字样式。
        /// 能读取到时保存，
        /// 新文字尽量继承。
        /// </summary>
        public ObjectId TextStyleId
        {
            get;
            set;
        }


        /// <summary>
        /// Table专属上下文。
        /// 普通DBText/MText/Dimension：
        /// null
        /// TableCell：
        /// 保存Table、行列、表格右边界等信息。
        /// </summary>
        public TableRevisionContext TableContext
        {
            get;
            set;
        }


        /// <summary>
        /// 是否为Table单元格目标。
        /// </summary>
        public bool IsTableCell
        {
            get
            {
                return
                    TableContext != null &&
                    TableContext.IsValid();
            }
        }


        /// <summary>
        /// 当前目标是否满足：
        /// TableCell
        /// +
        /// 原内容AB开头
        /// 后续Service直接使用这个属性判断
        /// 是否需要自动生成项目号。
        /// </summary>
        public bool ShouldWriteProjectNumber
        {
            get
            {
                if (!IsTableCell)
                    return false;


                if (string.IsNullOrWhiteSpace(
                        Text))
                {
                    return false;
                }


                return
                    Text.Trim()
                        .StartsWith(
                            "AB",
                            System.StringComparison
                                .OrdinalIgnoreCase);
            }
        }


        /// <summary>
        /// 判断RevisionTarget的基本数据是否有效。
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(
                    Text))
            {
                return false;
            }


            if (!IsValidNumber(
                    LeftX) ||
                !IsValidNumber(
                    RightX) ||
                !IsValidNumber(
                    BottomY) ||
                !IsValidNumber(
                    TopY) ||
                !IsValidNumber(
                    CenterY) ||
                !IsValidNumber(
                    TextHeight))
            {
                return false;
            }


            if (RightX <= LeftX)
                return false;


            if (TopY < BottomY)
                return false;


            if (TargetSpaceId.IsNull ||
                !TargetSpaceId.IsValid)
            {
                return false;
            }


            return true;
        }


        private static bool IsValidNumber(
            double value)
        {
            return
                !double.IsNaN(value) &&
                !double.IsInfinity(value) &&
                System.Math.Abs(value) < 1E15;
        }
    }
}
