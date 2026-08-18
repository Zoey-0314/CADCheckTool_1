$ErrorActionPreference = 'Stop'

function Get-NormalizedText {
    param([string]$Path)

    if (!(Test-Path $Path)) {
        throw "找不到文件: $Path"
    }

    $text = [System.IO.File]::ReadAllText($Path)
    return $text.Replace("`r`n", "`n").Replace("`r", "`n")
}

function Write-Utf8Bom {
    param(
        [string]$Path,
        [string]$Content
    )

    $utf8Bom = New-Object System.Text.UTF8Encoding($true)
    $normalized = $Content.Replace("`r`n", "`n").Replace("`r", "`n")
    $windowsText = $normalized.Replace("`n", [Environment]::NewLine)
    [System.IO.File]::WriteAllText($Path, $windowsText, $utf8Bom)
}

function Replace-ExactNormalized {
    param(
        [string]$Path,
        [string]$OldText,
        [string]$NewText,
        [string]$Description
    )

    $content = Get-NormalizedText $Path
    $oldNormalized = $OldText.Replace("`r`n", "`n").Replace("`r", "`n")
    $newNormalized = $NewText.Replace("`r`n", "`n").Replace("`r", "`n")

    if (!$content.Contains($oldNormalized)) {
        throw "补丁停止：未找到预期代码 [$Description]，文件: $Path。请不要继续强行应用。"
    }

    $content = $content.Replace($oldNormalized, $newNormalized)
    Write-Utf8Bom $Path $content
    Write-Host "OK  $Description"
}

$repo = (& git rev-parse --show-toplevel 2>$null)
if ([string]::IsNullOrWhiteSpace($repo)) {
    throw '请在 CADCheckTool Git 仓库中运行本脚本。'
}

Set-Location $repo

$dirty = (& git status --porcelain)
if ($dirty) {
    throw "当前工作区不是 clean 状态。请先 git add/commit，再运行补丁。"
}

Write-Host '开始应用 A3/A4 标题栏基准点偏移补丁...'

# ============================================================
# 1. 横竖版 + A3/A4 基准点解析
# ============================================================
$orientationPath = 'Core/TitleBlockOrientationDetector.cs'
$orientationContent = @'
using Correct_test1.Models;

using System;
using System.Collections.Generic;

namespace Correct_test1.Core
{
    public sealed class TitleBlockAnchorInfo
    {
        public bool Found { get; set; }

        public bool IsHorizontal { get; set; }

        public string PaperSize { get; set; }

        public double BaseX { get; set; }

        public double BaseY { get; set; }

        public double ActualX { get; set; }

        public double ActualY { get; set; }

        public double OffsetX
        {
            get { return ActualX - BaseX; }
        }

        public double OffsetY
        {
            get { return ActualY - BaseY; }
        }
    }


    public static class TitleBlockOrientationDetector
    {
        public const double A3BaseX = 50.8579;
        public const double A3BaseY = 315.3767;

        public const double A4BaseX = 86.4;
        public const double A4BaseY = 350.1487;

        private const double A3ExpectedHeight = 5.0;
        private const double A4ExpectedHeight = 3.5;


        public static bool TryResolveAnchor(
            List<TitleText> texts,
            out TitleBlockAnchorInfo info)
        {
            info = null;

            if (texts == null ||
                texts.Count == 0)
            {
                return false;
            }

            TitleText bestText = null;
            bool bestIsHorizontal = false;
            string bestPaperSize = "";
            double bestBaseX = 0.0;
            double bestBaseY = 0.0;
            double bestScore = double.MaxValue;

            foreach (TitleText text in texts)
            {
                if (text == null ||
                    string.IsNullOrWhiteSpace(text.Text))
                {
                    continue;
                }

                string value = NormalizePaperSizeText(text.Text);

                bool isHorizontal;
                string paperSize;
                double baseX;
                double baseY;
                double expectedHeight;

                if (value == "A3")
                {
                    isHorizontal = true;
                    paperSize = "A3";
                    baseX = A3BaseX;
                    baseY = A3BaseY;
                    expectedHeight = A3ExpectedHeight;
                }
                else if (value == "A4")
                {
                    isHorizontal = false;
                    paperSize = "A4";
                    baseX = A4BaseX;
                    baseY = A4BaseY;
                    expectedHeight = A4ExpectedHeight;
                }
                else
                {
                    continue;
                }

                double dx = text.X - baseX;
                double dy = text.Y - baseY;

                double positionDistance =
                    Math.Sqrt(dx * dx + dy * dy);

                double heightPenalty =
                    Math.Abs(text.Height - expectedHeight) * 20.0;

                double score =
                    positionDistance + heightPenalty;

                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestText = text;
                bestIsHorizontal = isHorizontal;
                bestPaperSize = paperSize;
                bestBaseX = baseX;
                bestBaseY = baseY;
            }

            if (bestText == null)
            {
                return false;
            }

            info = new TitleBlockAnchorInfo
            {
                Found = true,
                IsHorizontal = bestIsHorizontal,
                PaperSize = bestPaperSize,
                BaseX = bestBaseX,
                BaseY = bestBaseY,
                ActualX = bestText.X,
                ActualY = bestText.Y
            };

            return true;
        }


        public static bool IsHorizontal(
            List<TitleText> texts)
        {
            TitleBlockAnchorInfo anchor;

            if (TryResolveAnchor(
                    texts,
                    out anchor))
            {
                return anchor.IsHorizontal;
            }

            if (texts == null)
            {
                return false;
            }

            int markCount = 0;

            foreach (TitleText text in texts)
            {
                if (text == null ||
                    string.IsNullOrWhiteSpace(text.Text))
                {
                    continue;
                }

                if (text.Text.Contains("标记"))
                {
                    markCount++;
                }
            }

            return markCount >= 2;
        }


        public static List<TitleText> NormalizeToBaseline(
            List<TitleText> texts,
            TitleBlockAnchorInfo anchor)
        {
            if (texts == null)
            {
                return new List<TitleText>();
            }

            if (anchor == null ||
                !anchor.Found)
            {
                return texts;
            }

            List<TitleText> result =
                new List<TitleText>();

            foreach (TitleText text in texts)
            {
                if (text == null)
                {
                    continue;
                }

                result.Add(
                    new TitleText
                    {
                        Text = text.Text,
                        X = text.X - anchor.OffsetX,
                        Y = text.Y - anchor.OffsetY,
                        Height = text.Height,
                        LayoutName = text.LayoutName,
                        ViewportId = text.ViewportId,
                        ObjectId = text.ObjectId
                    });
            }

            return result;
        }


        private static string NormalizePaperSizeText(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            return value
                .Replace("\\P", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace(" ", "")
                .Replace("\t", "")
                .Trim()
                .ToUpperInvariant();
        }
    }
}
'@
Write-Utf8Bom $orientationPath $orientationContent
Write-Host 'OK  A3/A4横竖版和基准点解析'

# ============================================================
# 2. TitleBlockCheckManager：解析时归一化，标记时加回偏移
# ============================================================
$managerPath = 'Checks/TitleBlockCheckManager.cs'

$old = @'
            //--------------------------------
            // 判断横竖版
            // 保持原有逻辑
            //--------------------------------

            bool isHorizontal =
                TitleBlockOrientationDetector
                    .IsHorizontal(
                        texts);



            //--------------------------------
            // 标题栏解析
            //--------------------------------

            DrawingInfo info =
                parser.Parse(
                    texts,
                    isHorizontal
                );
'@
$new = @'
            //--------------------------------
            // A3 / A4 直接决定横竖版。
            // 同时由图幅文字计算整张标题栏的平移偏移。
            //--------------------------------

            TitleBlockAnchorInfo anchorInfo;

            bool hasAnchor =
                TitleBlockOrientationDetector
                    .TryResolveAnchor(
                        texts,
                        out anchorInfo);

            bool isHorizontal =
                hasAnchor
                    ? anchorInfo.IsHorizontal
                    : TitleBlockOrientationDetector
                        .IsHorizontal(
                            texts);

            double offsetX =
                hasAnchor
                    ? anchorInfo.OffsetX
                    : 0.0;

            double offsetY =
                hasAnchor
                    ? anchorInfo.OffsetY
                    : 0.0;

            List<TitleText> parseTexts =
                hasAnchor
                    ? TitleBlockOrientationDetector
                        .NormalizeToBaseline(
                            texts,
                            anchorInfo)
                    : texts;



            //--------------------------------
            // 标题栏解析
            //--------------------------------

            DrawingInfo info =
                parser.Parse(
                    parseTexts,
                    isHorizontal
                );
'@
Replace-ExactNormalized $managerPath $old $new '标题栏A3/A4判断和坐标归一化'

$old = @'
            List<TextHeightIssue> textHeightIssues =
                CheckTextHeights(texts, isHorizontal);
'@
$new = @'
            List<TextHeightIssue> textHeightIssues =
                CheckTextHeights(
                    texts,
                    isHorizontal,
                    offsetX,
                    offsetY);
'@
Replace-ExactNormalized $managerPath $old $new '标题栏字高检查使用基准偏移'

$old = @'
                    fieldMarker.DrawMarker(
                        db,
                        layout.LayoutName,
                        info.IsHorizontal,
                        result.ObjectName,
                        "标题栏" + result.ObjectName + "未填写");
'@
$new = @'
                    fieldMarker.DrawMarker(
                        db,
                        layout.LayoutName,
                        info.IsHorizontal,
                        result.ObjectName,
                        "标题栏" + result.ObjectName + "未填写",
                        offsetX,
                        offsetY);
'@
Replace-ExactNormalized $managerPath $old $new '字段错误框使用基准偏移'

$old = @'
                    fieldMarker.DrawMarker(
                        db,
                        layout.LayoutName,
                        info.IsHorizontal,
                        "PageNumber",
                        pageMessage);
'@
$new = @'
                    fieldMarker.DrawMarker(
                        db,
                        layout.LayoutName,
                        info.IsHorizontal,
                        "PageNumber",
                        pageMessage,
                        offsetX,
                        offsetY);
'@
Replace-ExactNormalized $managerPath $old $new '页码错误框使用基准偏移'

$old = @'
                        drawingNumberMarker.DrawMarker(
                            db,
                            layout.LayoutName,
                            info.IsHorizontal,
                            fileDrawingNumber
                        );
'@
$new = @'
                        drawingNumberMarker.DrawMarker(
                            db,
                            layout.LayoutName,
                            info.IsHorizontal,
                            fileDrawingNumber,
                            default(Autodesk.AutoCAD.Geometry.Point3d),
                            offsetX,
                            offsetY
                        );
'@
Replace-ExactNormalized $managerPath $old $new '标题栏图号标记使用基准偏移'

$old = @'
        private List<TextHeightIssue> CheckTextHeights(
            List<TitleText> texts,
            bool isHorizontal)
        {
'@
$new = @'
        private List<TextHeightIssue> CheckTextHeights(
            List<TitleText> texts,
            bool isHorizontal,
            double offsetX,
            double offsetY)
        {
'@
Replace-ExactNormalized $managerPath $old $new '字高检查增加偏移参数'

$old = @'
            AddRegionHeightIssues(
                texts,
                regions.Find(x => x.FieldName == "DrawingName"),
                5.0,
                "名称文字高度错误",
                issues);

            AddRegionHeightIssues(
                texts,
                regions.Find(x => x.FieldName == "DrawingNumber"),
                3.5,
                "图号文字高度错误",
                issues);
'@
$new = @'
            AddRegionHeightIssues(
                texts,
                regions.Find(x => x.FieldName == "DrawingName"),
                5.0,
                "名称文字高度错误",
                issues,
                offsetX,
                offsetY);

            AddRegionHeightIssues(
                texts,
                regions.Find(x => x.FieldName == "DrawingNumber"),
                3.5,
                "图号文字高度错误",
                issues,
                offsetX,
                offsetY);
'@
Replace-ExactNormalized $managerPath $old $new '字段字高区域跟随偏移'

$old = @'
        private void AddRegionHeightIssues(
            List<TitleText> texts,
            TitleFieldRegion region,
            double expectedHeight,
            string message,
            List<TextHeightIssue> issues)
        {
            if (region == null)
                return;

            foreach (TitleText text in texts)
            {
                if (region.Contains(text.X, text.Y))
                {
                    AddHeightIssue(text, expectedHeight, message, issues);
                }
            }
        }
'@
$new = @'
        private void AddRegionHeightIssues(
            List<TitleText> texts,
            TitleFieldRegion region,
            double expectedHeight,
            string message,
            List<TextHeightIssue> issues,
            double offsetX,
            double offsetY)
        {
            if (region == null)
                return;

            foreach (TitleText text in texts)
            {
                if (region.Contains(
                        text.X - offsetX,
                        text.Y - offsetY))
                {
                    AddHeightIssue(text, expectedHeight, message, issues);
                }
            }
        }
'@
Replace-ExactNormalized $managerPath $old $new '字高区域判断按A3/A4归一化'

# ============================================================
# 3. TitleBlockFieldMarker：配置区域整体平移
# ============================================================
$fieldMarkerPath = 'Markers/TitleBlockFieldMarker.cs'
$fieldMarkerContent = @'
using System;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Configs;
using Correct_test1.Core;
using Correct_test1.Models;

namespace Correct_test1.Markers
{
    public class TitleBlockFieldMarker : MarkerBase
    {
        public void DrawMarker(
            Database db,
            string layoutName,
            bool isHorizontal,
            string fieldName,
            string message,
            double offsetX = 0.0,
            double offsetY = 0.0)
        {
            using (Transaction transaction = db.TransactionManager.StartTransaction())
            {
                DBDictionary layouts = transaction.GetObject(
                    db.LayoutDictionaryId,
                    OpenMode.ForRead) as DBDictionary;

                if (!layouts.Contains(layoutName))
                    return;

                Layout layout = transaction.GetObject(
                    layouts.GetAt(layoutName),
                    OpenMode.ForRead) as Layout;

                BlockTableRecord space = transaction.GetObject(
                    layout.BlockTableRecordId,
                    OpenMode.ForWrite) as BlockTableRecord;

                string regionName = ToRegionName(fieldName);

                TitleFieldRegion region = (isHorizontal
                    ? TitleBlockHorizontalConfig.Regions
                    : TitleBlockVerticalConfig.Regions)
                    .Find(x => x.FieldName == regionName);

                if (space == null || region == null)
                    return;

                double minX = region.MinX + offsetX;
                double maxX = region.MaxX + offsetX;
                double minY = region.MinY + offsetY;
                double maxY = region.MaxY + offsetY;

                ObjectId layerId = EnsureLayer(
                    db,
                    transaction,
                    MarkerConfig.TitleBlockLayerName,
                    Color.FromRgb(0, 255, 0));

                Polyline rectangle = new Polyline();
                rectangle.AddVertexAt(0, new Point2d(minX, minY), 0, 0, 0);
                rectangle.AddVertexAt(1, new Point2d(maxX, minY), 0, 0, 0);
                rectangle.AddVertexAt(2, new Point2d(maxX, maxY), 0, 0, 0);
                rectangle.AddVertexAt(3, new Point2d(minX, maxY), 0, 0, 0);
                rectangle.Closed = true;
                rectangle.LayerId = layerId;
                rectangle.Color = Color.FromRgb(0, 255, 0);

                space.AppendEntity(rectangle);
                transaction.AddNewlyCreatedDBObject(rectangle, true);

                double textHeight = MarkerConfig.TextHeight;
                double textY = maxY;

                if (regionName == "DrawingNumber")
                {
                    bool hasExistingText = false;

                    foreach (ObjectId entityId in space)
                    {
                        DBText existingText = transaction.GetObject(
                            entityId,
                            OpenMode.ForRead) as DBText;

                        if (existingText != null &&
                            existingText.Layer == MarkerConfig.TitleBlockLayerName &&
                            Math.Abs(existingText.Position.X - (maxX + 5)) < 0.001 &&
                            existingText.Position.Y >= maxY - 0.001)
                        {
                            hasExistingText = true;
                            break;
                        }
                    }

                    if (hasExistingText)
                        textY += 10;
                }

                DBText text = new DBText
                {
                    TextString = message,
                    Position = new Point3d(maxX + 5, maxY, 0),
                    Height = textHeight,
                    LayerId = layerId,
                    Color = Color.FromRgb(0, 255, 0)
                };

                text.Position = new Point3d(maxX + 5, textY, 0);

                space.AppendEntity(text);
                transaction.AddNewlyCreatedDBObject(text, true);

                transaction.Commit();
            }
        }

        private static string ToRegionName(string fieldName)
        {
            switch (fieldName)
            {
                case "图号": return "DrawingNumber";
                case "图纸名称": return "DrawingName";
                case "材料": return "Material";
                case "规格": return "Specification";
                case "表面处理": return "SurfaceTreatment";
                case "制图": return "Designer";
                case "校对": return "Checker";
                case "标审": return "Reviewer";
                case "批准": return "Approver";
                case "日期": return "TitleDate";
                case "页码": return "PageNumber";
                default: return fieldName;
            }
        }
    }
}
'@
Write-Utf8Bom $fieldMarkerPath $fieldMarkerContent
Write-Host 'OK  标题栏字段标记跟随偏移'

# ============================================================
# 4. TitleBlockDrawingNumberMarker：标题栏图号框整体平移
# ============================================================
$drawingMarkerPath = 'Markers/TitleBlockDrawingNumberMarker.cs'

$old = @'
            Point3d textPosition = default(Point3d)

        )
'@
$new = @'
            Point3d textPosition = default(Point3d),

            double offsetX = 0.0,

            double offsetY = 0.0

        )
'@
Replace-ExactNormalized $drawingMarkerPath $old $new '图号标记增加偏移参数'

$old = @'
                    double x1 = region.MinX;
                    double x2 = region.MaxX;

                    double y1 = region.MinY;
                    double y2 = region.MaxY;
'@
$new = @'
                    double x1 = region.MinX + offsetX;
                    double x2 = region.MaxX + offsetX;

                    double y1 = region.MinY + offsetY;
                    double y2 = region.MaxY + offsetY;
'@
Replace-ExactNormalized $drawingMarkerPath $old $new '图号标记区域跟随偏移'

# ============================================================
# 5. ProjectVersionConfig：项目号/版本号模板支持偏移
# ============================================================
$projectConfigPath = 'ProjectVersion/Configs/ProjectVersionConfig.cs'
$projectConfigContent = @'
namespace Correct_test1.ProjectVersion.Configs
{
    public class ProjectVersionTemplate
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double TextHeight { get; set; }

        public double Width { get; set; }

        public double SearchTolerance { get; set; }

        public string TextStyleName { get; set; }
    }


    public static class ProjectVersionConfig
    {
        public static readonly
            ProjectVersionTemplate Horizontal =
                new ProjectVersionTemplate
                {
                    X = 114.7533,
                    Y = 315.8613,
                    TextHeight = 5.0,
                    Width = 34.3439,
                    SearchTolerance = 15.0,
                    TextStyleName = "CONN"
                };


        public static readonly
            ProjectVersionTemplate Vertical =
                new ProjectVersionTemplate
                {
                    X = 130.816,
                    Y = 351.0263,
                    TextHeight = 4.0,
                    Width = 27.4752,
                    SearchTolerance = 15.0,
                    TextStyleName = "CONN"
                };


        public static ProjectVersionTemplate Get(
            bool isHorizontal)
        {
            return Get(
                isHorizontal,
                0.0,
                0.0);
        }


        public static ProjectVersionTemplate Get(
            bool isHorizontal,
            double offsetX,
            double offsetY)
        {
            ProjectVersionTemplate source =
                isHorizontal
                    ? Horizontal
                    : Vertical;

            return new ProjectVersionTemplate
            {
                X = source.X + offsetX,
                Y = source.Y + offsetY,
                TextHeight = source.TextHeight,
                Width = source.Width,
                SearchTolerance = source.SearchTolerance,
                TextStyleName = source.TextStyleName
            };
        }
    }
}
'@
Write-Utf8Bom $projectConfigPath $projectConfigContent
Write-Host 'OK  项目号/版本号模板支持偏移'

# ============================================================
# 6. ProjectVersionWriteService：每个Layout独立解析A3/A4和偏移
# ============================================================
$projectServicePath = 'ProjectVersion/Services/ProjectVersionWriteService.cs'
$projectServiceContent = @'
using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Core;
using Correct_test1.Models;
using Correct_test1.ProjectVersion.Models;
using Correct_test1.ProjectVersion.Writers;
using Correct_test1.Readers;

using System.Collections.Generic;

namespace Correct_test1.ProjectVersion.Services
{
    public class ProjectVersionWriteService
    {
        public List<ProjectVersionLayoutResult>
            WriteAllLayouts(
                Database database,
                string value)
        {
            List<ProjectVersionLayoutResult>
                results =
                    new List<ProjectVersionLayoutResult>();

            if (database == null)
                return results;

            if (string.IsNullOrWhiteSpace(value))
                return results;

            LayoutReader layoutReader =
                new LayoutReader();

            TitleBlockReader titleReader =
                new TitleBlockReader();

            ProjectVersionWriter writer =
                new ProjectVersionWriter();

            List<LayoutInfo> layouts =
                layoutReader.ReadLayouts(database);

            layouts.Sort(
                (a, b) =>
                    a.TabOrder.CompareTo(b.TabOrder));

            foreach (LayoutInfo layout in layouts)
            {
                if (layout == null ||
                    layout.IsModelSpace)
                {
                    continue;
                }

                List<TitleText> texts =
                    titleReader.Read(
                        database,
                        new List<LayoutInfo>
                        {
                            layout
                        });

                if (texts == null ||
                    texts.Count == 0)
                {
                    results.Add(
                        new ProjectVersionLayoutResult
                        {
                            LayoutName = layout.LayoutName,
                            Success = false,
                            Skipped = true,
                            Message = "未读取到标题栏文字，已跳过。"
                        });

                    continue;
                }

                TitleBlockAnchorInfo anchorInfo;

                bool hasAnchor =
                    TitleBlockOrientationDetector
                        .TryResolveAnchor(
                            texts,
                            out anchorInfo);

                bool isHorizontal =
                    hasAnchor
                        ? anchorInfo.IsHorizontal
                        : TitleBlockOrientationDetector
                            .IsHorizontal(texts);

                double offsetX =
                    hasAnchor
                        ? anchorInfo.OffsetX
                        : 0.0;

                double offsetY =
                    hasAnchor
                        ? anchorInfo.OffsetY
                        : 0.0;

                ProjectVersionLayoutResult result =
                    writer.Write(
                        database,
                        layout,
                        value,
                        isHorizontal,
                        offsetX,
                        offsetY);

                results.Add(result);
            }

            return results;
        }
    }
}
'@
Write-Utf8Bom $projectServicePath $projectServiceContent
Write-Host 'OK  项目号写入按Layout使用A3/A4偏移'

# ============================================================
# 7. ProjectVersionWriter：接收偏移模板
# ============================================================
$projectWriterPath = 'ProjectVersion/Writers/ProjectVersionWriter.cs'

$old = @'
        public ProjectVersionLayoutResult Write(
            Database database,
            LayoutInfo layout,
            string value,
            bool isHorizontal)
'@
$new = @'
        public ProjectVersionLayoutResult Write(
            Database database,
            LayoutInfo layout,
            string value,
            bool isHorizontal,
            double offsetX = 0.0,
            double offsetY = 0.0)
'@
Replace-ExactNormalized $projectWriterPath $old $new '项目号写入器增加偏移参数'

$old = @'
            ProjectVersionTemplate template =
                ProjectVersionConfig.Get(
                    isHorizontal);
'@
$new = @'
            ProjectVersionTemplate template =
                ProjectVersionConfig.Get(
                    isHorizontal,
                    offsetX,
                    offsetY);
'@
Replace-ExactNormalized $projectWriterPath $old $new '项目号写入位置使用偏移模板'

# ============================================================
# 8. DrawingVersionReader：版本读取位置使用同一A3/A4偏移
# ============================================================
$versionReaderPath = 'VersionCheck/Readers/DrawingVersionReader.cs'

$old = @'
                //--------------------------------
                // 复用原横竖版判断
                //--------------------------------

                bool isHorizontal =
                    TitleBlockOrientationDetector
                        .IsHorizontal(
                            titleTexts);


                ProjectVersionTemplate template =
                    ProjectVersionConfig.Get(
                        isHorizontal);
'@
$new = @'
                //--------------------------------
                // A3 / A4 直接决定横竖版，
                // 并用同一基准点偏移版本号查找区域。
                //--------------------------------

                TitleBlockAnchorInfo anchorInfo;

                bool hasAnchor =
                    TitleBlockOrientationDetector
                        .TryResolveAnchor(
                            titleTexts,
                            out anchorInfo);

                bool isHorizontal =
                    hasAnchor
                        ? anchorInfo.IsHorizontal
                        : TitleBlockOrientationDetector
                            .IsHorizontal(
                                titleTexts);

                double offsetX =
                    hasAnchor
                        ? anchorInfo.OffsetX
                        : 0.0;

                double offsetY =
                    hasAnchor
                        ? anchorInfo.OffsetY
                        : 0.0;


                ProjectVersionTemplate template =
                    ProjectVersionConfig.Get(
                        isHorizontal,
                        offsetX,
                        offsetY);
'@
Replace-ExactNormalized $versionReaderPath $old $new '版本检查使用A3/A4偏移'

Write-Host ''
Write-Host '补丁应用完成。'
Write-Host '请先执行: git diff'
Write-Host '然后在 Visual Studio 中重新生成解决方案。'
