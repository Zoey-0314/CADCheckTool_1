using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

using Correct_test1.Models;
using Correct_test1.Readers;

using System.Collections.Generic;
using System.IO;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;

using WinForms = System.Windows.Forms;

namespace Correct_test1
{
    public class TitleTestCommand
    {
        [CommandMethod("TESTREVISION")]
        public void TestRevision()
        {
            Document doc =
                Application.DocumentManager
                .MdiActiveDocument;

            if (doc == null)
                return;

            Database db =
                doc.Database;

            Editor ed =
                doc.Editor;

            try
            {
                LayoutReader layoutReader =
                    new LayoutReader();

                List<LayoutInfo> layouts =
                    layoutReader.ReadLayouts(
                        db,
                        ed
                    );

                RevisionTableReader reader =
                    new RevisionTableReader();

                string path =
                    @"D:\Revision_Test.csv";

                using (StreamWriter sw =
                    new StreamWriter(
                        path,
                        false,
                        Encoding.UTF8))
                {
                    // 恢复为统一表头：布局,类型,标记,更改内容,更改日期,签名,变更号
                    sw.WriteLine(
                        "布局,类型,标记,更改内容,更改日期,签名,变更号"
                    );

                    foreach (LayoutInfo layout in layouts)
                    {
                        if (layout.IsModelSpace)
                            continue;

                        ed.WriteMessage("\n====================");
                        ed.WriteMessage("\n读取布局:" + layout.LayoutName);

                        // 保留已有的“标记计数”判定横竖逻辑（不可修改）
                        bool isHorizontal = false;

                        List<TitleText> titleTexts = layout.TitleTexts;
                        if (titleTexts == null || titleTexts.Count == 0)
                        {
                            // 兼容：若 LayoutInfo.TitleTexts 未填，读取图块内所有文字
                            titleTexts = reader.ReadAllTexts(db, layout.BlockTableRecordId);
                        }

                        int markCount = 0;
                        foreach (var t in titleTexts)
                        {
                            if (
                                !string.IsNullOrEmpty(t.Text)
                                &&
                                t.Text.Trim() == "标记"
                            )
                            {
                                markCount++;
                            }
                        }

                        // 保持规则：标记出现1次 -> 竖版，出现2次 -> 横版
                        if (markCount >= 2)
                            isHorizontal = true;
                        else
                            isHorizontal = false;

                        // 统一收集为 List<RevisionInfo>
                        List<RevisionInfo> allRevisions = new List<RevisionInfo>();

                        if (isHorizontal)
                        {
                            // 使用 ReadHorizontal()（已存在，返回 List<RevisionInfo>）
                            List<RevisionInfo> horizontalRevs =
                                reader.ReadHorizontal(
                                    db,
                                    layout.BlockTableRecordId
                                );

                            ed.WriteMessage("\n横版读取数量:" + horizontalRevs.Count);

                            allRevisions.AddRange(horizontalRevs);
                        }
                        else
                        {
                            // 使用原有 ReadVertical()
                            List<RevisionInfo> verticalRevs =
                                reader.ReadVertical(
                                    db,
                                    layout.BlockTableRecordId
                                );

                            ed.WriteMessage("\n竖版读取数量:" + verticalRevs.Count);

                            allRevisions.AddRange(verticalRevs);
                        }

                        // 统一输出 allRevisions
                        foreach (RevisionInfo rev in allRevisions)
                        {
                            sw.WriteLine(
                                layout.LayoutName
                                + (isHorizontal ? ",横版," : ",竖版,")
                                + Escape(rev.Mark) + ","
                                + Escape(rev.Description) + ","
                                + Escape(rev.Date) + ","
                                + Escape(rev.Signer) + ","
                                + Escape(rev.RevisionNumber)
                            );
                        }
                    }
                }

                WinForms.MessageBox.Show(
                    "完成\nCSV文件:\n" + path,
                    "修改记录测试"
                );
            }
            catch (System.Exception ex)
            {
                WinForms.MessageBox.Show(
                    ex.Message,
                    "错误"
                );
            }
        }

        /// <summary>
        /// CSV转义
        /// </summary>
        private string Escape(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(","))
            {
                return "\"" + value + "\"";
            }

            return value;
        }
    }
}