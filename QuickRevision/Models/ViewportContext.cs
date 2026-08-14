using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Correct_test1.QuickRevision.Models
{
    /// <summary>
    /// 描述一次Viewport点击所需要的上下文。
    ///
    /// 用户始终在Paper Space点击，
    /// 不要求用户双击进入Viewport。
    ///
    /// ViewportResolver找到Viewport之后，
    /// 使用该模型把Viewport相关信息传给
    /// ViewportCoordinateConverter。
    /// </summary>
    public class ViewportContext
    {
        /// <summary>
        /// Viewport对象ObjectId。
        /// </summary>
        public ObjectId ViewportId
        {
            get;
            set;
        }


        /// <summary>
        /// Viewport编号。
        ///
        /// 通常布局空间本身为1，
        /// 浮动Viewport通常为2及以上。
        /// </summary>
        public int ViewportNumber
        {
            get;
            set;
        }


        /// <summary>
        /// 用户在Paper Space中点击的位置。
        /// </summary>
        public Point3d PaperPoint
        {
            get;
            set;
        }


        /// <summary>
        /// Paper Space点击点转换到
        /// Model Space后的坐标。
        ///
        /// 由ViewportCoordinateConverter负责计算。
        /// </summary>
        public Point3d ModelPoint
        {
            get;
            set;
        }


        /// <summary>
        /// Viewport在Paper Space中的中心点。
        /// </summary>
        public Point3d CenterPoint
        {
            get;
            set;
        }


        /// <summary>
        /// Viewport在Paper Space中的宽度。
        /// </summary>
        public double Width
        {
            get;
            set;
        }


        /// <summary>
        /// Viewport在Paper Space中的高度。
        /// </summary>
        public double Height
        {
            get;
            set;
        }


        /// <summary>
        /// Viewport缩放比例。
        /// </summary>
        public double CustomScale
        {
            get;
            set;
        }


        /// <summary>
        /// Viewport所看到Model Space区域的中心。
        ///
        /// ViewCenter使用二维坐标。
        /// </summary>
        public Point2d ViewCenter
        {
            get;
            set;
        }


        /// <summary>
        /// Viewport观察目标点。
        /// </summary>
        public Point3d ViewTarget
        {
            get;
            set;
        }


        /// <summary>
        /// Viewport观察方向。
        ///
        /// 第一版主要针对普通二维机械图，
        /// 但这个字段保留下来，
        /// 避免以后重新改模型。
        /// </summary>
        public Vector3d ViewDirection
        {
            get;
            set;
        }


        /// <summary>
        /// Viewport视图扭转角。
        ///
        /// 当前第一版不考虑旋转Viewport，
        /// 但提前保存这个属性。
        /// </summary>
        public double TwistAngle
        {
            get;
            set;
        }


        /// <summary>
        /// 当前点击点是否位于该Viewport矩形范围内。
        /// </summary>
        public bool ContainsPaperPoint()
        {
            double halfWidth =
                Width / 2.0;

            double halfHeight =
                Height / 2.0;

            double minX =
                CenterPoint.X - halfWidth;

            double maxX =
                CenterPoint.X + halfWidth;

            double minY =
                CenterPoint.Y - halfHeight;

            double maxY =
                CenterPoint.Y + halfHeight;

            return
                PaperPoint.X >= minX &&
                PaperPoint.X <= maxX &&
                PaperPoint.Y >= minY &&
                PaperPoint.Y <= maxY;
        }


        /// <summary>
        /// 判断ViewportContext是否具备基本有效数据。
        /// </summary>
        public bool IsValid()
        {
            if (ViewportId.IsNull ||
                !ViewportId.IsValid)
            {
                return false;
            }

            if (ViewportNumber <= 1)
                return false;

            if (Width <= 0)
                return false;

            if (Height <= 0)
                return false;

            if (CustomScale <= 0)
                return false;

            return true;
        }
    }
}