using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Models;
using System.Collections.Generic;

namespace Correct_test1.Readers
{
    /// <summary>
    /// 标题栏文字读取器
    /// 只负责：CAD实体 -> TitleText，不负责解析字段
    /// </summary>
    public class TitleBlockReader
    {
        public List<TitleText> Read(Database db, List<LayoutInfo> layouts)
        {
            List<TitleText> result = new List<TitleText>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (LayoutInfo layout in layouts)
                {
                    BlockTableRecord btr = tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                    if (btr == null)
                        continue;

                    foreach (ObjectId id in btr)
                    {
                        Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        if (ent == null)
                            continue;

                        // 普通文字
                        if (ent is DBText text)
                        {
                            result.Add(new TitleText()
                            {
                                Text = Clean(text.TextString),
                                X = text.Position.X,
                                Y = text.Position.Y,
                                Height = text.Height,
                                LayoutName = layout.LayoutName,
                                ObjectId = text.ObjectId
                            });
                        }
                        // 多行文字
                        else if (ent is MText mtext)
                        {
                            result.Add(new TitleText()
                            {
                                Text = Clean(mtext.Text),
                                X = mtext.Location.X,
                                Y = mtext.Location.Y,
                                Height = mtext.TextHeight,
                                LayoutName = layout.LayoutName,
                                ObjectId = mtext.ObjectId
                            });
                        }
                        // 属性块
                        else if (ent is BlockReference block)
                        {
                            foreach (ObjectId aid in block.AttributeCollection)
                            {
                                AttributeReference att = tr.GetObject(aid, OpenMode.ForRead) as AttributeReference;
                                if (att == null)
                                    continue;

                                result.Add(new TitleText()
                                {
                                    Text = Clean(att.TextString),
                                    X = att.Position.X,
                                    Y = att.Position.Y,
                                    LayoutName = layout.LayoutName,
                                    ObjectId = att.ObjectId
                                });
                            }
                        }
                    }
                }

                tr.Commit();
            }

            return result;
        }

        public List<TitleText> FilterNumericTexts(
            List<TitleText> texts)
        {
            List<TitleText> result =
                new List<TitleText>();

            if (texts == null)
                return result;

            foreach (TitleText text in texts)
            {
                if (text == null ||
                    string.IsNullOrWhiteSpace(text.Text))
                {
                    continue;
                }

                string value = text.Text.Trim();
                bool isNumeric = true;

                foreach (char character in value)
                {
                    if (character < '0' || character > '9')
                    {
                        isNumeric = false;
                        break;
                    }
                }

                if (isNumeric)
                    result.Add(text);
            }

            return result;
        }

        private string Clean(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            return text
                .Replace("\\P", "\n")
                .Trim();
        }
    }
}