namespace Correct_test1.Configs
{
    /// <summary>
    /// Global marker configuration.
    /// Contains shared parameters used by all markers.
    /// </summary>
    public static class MarkerConfig
    {

        #region Layer Names

        /// <summary>
        /// Revision check marker layer.
        /// </summary>
        public const string RevisionLayerName =
            "CADCHECK_MARKER";


        /// <summary>
        /// Title block drawing number marker layer.
        /// </summary>
        public const string TitleBlockLayerName =
            "CADCHECK_MARKER";


        #endregion



        #region Common Marker Style


        /// <summary>
        /// Marker text height.
        /// </summary>
        public const double TextHeight =
            3.0;

        /// <summary>
        /// BOM相关标记文字高度。
        /// </summary>
        public const double BomMarkerTextHeight =
            3.0;


        /// <summary>
        /// Marker line width.
        /// </summary>
        public const double LineWidth =
            0;


        /// <summary>
        /// Default polyline closed state.
        /// </summary>
        public const bool ClosedPolyline =
            true;


        #endregion



        #region Revision Marker


        /// <summary>
        /// Revision marker rectangle width.
        /// </summary>
        public const double RevisionBoxWidth =
            18.0;


        /// <summary>
        /// Revision marker rectangle height.
        /// </summary>
        public const double RevisionBoxHeight =
            5.0;


        #endregion



        #region Default Values


        /// <summary>
        /// Default marker layer color index.
        /// AutoCAD ACI color.
        /// </summary>
        public const short DefaultLayerColorIndex =
            3;


        #endregion

    }
}