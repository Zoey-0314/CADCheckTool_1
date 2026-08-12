using Correct_test1.Models;
using System.Collections.Generic;

namespace Correct_test1.Checks
{
    /// <summary>
    /// BOM序号与图纸零件序号标注双向一致性检查
    /// </summary>
    public class BomCalloutChecker
    {
        /// <summary>
        /// 执行双向一致性检查。
        /// </summary>
        /// <param name="boms">已解析的BOM列表</param>
        /// <param name="callouts">图纸中识别出的零件序号标注</param>
        public List<BomCalloutIssue> Check(
            List<BomData> boms,
            List<PartCallout> callouts)
        {
            List<BomCalloutIssue> issues = new List<BomCalloutIssue>();

            // ── 收集BOM有效序号（No列中为纯正整数的项） ──────────────
            Dictionary<int, Autodesk.AutoCAD.Geometry.Point3d> bomNumbers
                = new Dictionary<int, Autodesk.AutoCAD.Geometry.Point3d>();

            if (boms != null)
            {
                foreach (BomData bom in boms)
                {
                    if (bom == null || bom.Items == null)
                        continue;

                    foreach (BomItem item in bom.Items)
                    {
                        if (string.IsNullOrWhiteSpace(item.No))
                            continue;

                        int n;
                        if (!int.TryParse(item.No.Trim(), out n) || n <= 0)
                            continue;

                        if (!bomNumbers.ContainsKey(n))
                            bomNumbers[n] = item.NoCellPosition;
                    }
                }
            }

            // ── 收集图纸有效序号（Distinct集合，保留第一个位置） ──────
            Dictionary<int, Autodesk.AutoCAD.Geometry.Point3d> drawingNumbers
                = new Dictionary<int, Autodesk.AutoCAD.Geometry.Point3d>();

            if (callouts != null)
            {
                foreach (PartCallout callout in callouts)
                {
                    if (!drawingNumbers.ContainsKey(callout.Number))
                        drawingNumbers[callout.Number] = callout.TextPosition;
                }
            }

            // ── 情况A：BOM有、图上无 → MissingDrawingCallout ─────────
            foreach (KeyValuePair<int, Autodesk.AutoCAD.Geometry.Point3d> kv in bomNumbers)
            {
                if (!drawingNumbers.ContainsKey(kv.Key))
                {
                    issues.Add(new BomCalloutIssue
                    {
                        IssueType = BomCalloutIssueType.MissingDrawingCallout,
                        Number = kv.Key,
                        Message = string.Format("BOM序号{0}未在图中标注", kv.Key),
                        MarkerPosition = kv.Value
                    });
                }
            }

            // ── 情况B：图上有、BOM无 → ExtraDrawingCallout ────────────
            foreach (KeyValuePair<int, Autodesk.AutoCAD.Geometry.Point3d> kv in drawingNumbers)
            {
                if (!bomNumbers.ContainsKey(kv.Key))
                {
                    issues.Add(new BomCalloutIssue
                    {
                        IssueType = BomCalloutIssueType.ExtraDrawingCallout,
                        Number = kv.Key,
                        Message = string.Format("图中序号{0}在BOM中不存在", kv.Key),
                        MarkerPosition = kv.Value
                    });
                }
            }

            return issues;
        }
    }
}
