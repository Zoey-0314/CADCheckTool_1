using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Correct_test1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Correct_test1.Core;


namespace Correct_test1.Readers
{


    /// <summary>
    /// 修改记录读取器
    ///
    /// 竖版：
    /// 保留原成功逻辑
    ///
    /// 横版：
    /// 新增十列表格读取
    ///
    /// </summary>
    public class RevisionTableReader
    {



        //================================================
        // 横版修改记录模板坐标
        //================================================


        /// <summary>
        /// 横版修改记录读取
        ///
        /// 返回：
        /// 左五列 + 右五列
        /// </summary>
        private List<HorizontalRevisionRow> ReadHorizontalRows(
            List<TitleText> texts)
        {
            if (texts == null)
            {
                return new List<HorizontalRevisionRow>();
            }

            return ParseHorizontalTable(texts);
        }





        public List<TitleText> ReadAllTexts(
    Database db,
    ObjectId blockId)
        {
            return ReadTexts(
                db,
                blockId
            );
        }
        /// <summary>
        /// 兼容旧测试命令
        /// 横版返回普通RevisionInfo列表
        /// </summary>
        public List<RevisionInfo> ReadHorizontal(
            List<TitleText> texts)
        {
            List<HorizontalRevisionRow> rows =
                ReadHorizontalRows(texts);

            List<RevisionInfo> result =
                new List<RevisionInfo>();

            foreach (HorizontalRevisionRow row in rows)
            {
                if (IsValid(row.Left))
                {
                    result.Add(row.Left);
                }

                if (IsValid(row.Right))
                {
                    result.Add(row.Right);
                }
            }

            return result;
        }








        //================================================
        // 竖版读取
        // 注意：
        // 这一部分保持原逻辑
        //================================================



        /// <summary>
        /// 读取竖版修改记录
        /// </summary>
        public List<RevisionInfo> ReadVertical(
            List<TitleText> texts)
        {
            if (texts == null)
            {
                return new List<RevisionInfo>();
            }

            List<TitleText> revisionTexts =
                texts
                    .Where(t =>
                        t.X >= 82.7599 &&
                        t.X <= 192.7611 &&
                        t.Y >= 65.4386 &&
                        t.Y <= 95.4386)
                    .ToList();

            List<List<TitleText>> rows =
                GroupByRow(revisionTexts);

            List<RevisionInfo> result =
                new List<RevisionInfo>();

            foreach (List<TitleText> row in rows)
            {
                RevisionInfo info =
                    ParseRow(row);

                if (!string.IsNullOrWhiteSpace(info.Mark) ||
                    !string.IsNullOrWhiteSpace(info.Description) ||
                    !string.IsNullOrWhiteSpace(info.Date))
                {
                    result.Add(info);
                }
            }

            return result;
        }

        //================================================
        // 横版十列表格读取核心
        //================================================


        private List<HorizontalRevisionRow> ParseHorizontalTable(
            List<TitleText> texts)
        {

            List<HorizontalRevisionRow> result =
                new List<HorizontalRevisionRow>();

            // 数据区域过滤：只处理模板范围内的文本
            List<TitleText> dataTexts =
                texts
                .Where(t =>
                    t.X >= 45.2828
                    &&
                    t.X <= 329.8438
                    &&
                    t.Y < 67.145
                    &&
                    t.Y > 37.145
                )
                .ToList();

            // Y 容差
            double yTol = 1.5;

            // 标记列范围（左/右）
            double leftMarkMin = 45.2828;
            double leftMarkMax = 55.2828;

            double rightMarkMin = 187.5633;
            double rightMarkMax = 197.5633;

            // 左侧字段 X 范围
            double leftDescMin = 55.2828, leftDescMax = 130.2828;
            double leftDateMin = 130.2828, leftDateMax = 150.2828;
            double leftSignerMin = 150.2828, leftSignerMax = 170.2828;
            double leftRevMin = 170.2828, leftRevMax = 187.5633;

            // 右侧字段 X 范围（对应整体右移五列）
            double rightDescMin = 197.5633, rightDescMax = 272.5633;
            double rightDateMin = 272.5633, rightDateMax = 292.5633;
            double rightSignerMin = 292.5633, rightSignerMax = 312.5633;
            double rightRevMin = 312.5633, rightRevMax = 329.8438;

            // 找所有可能的标记文本（左或右标记列），按从上到下顺序处理
            var potentialMarks = dataTexts
                .Where(t =>
                    (
                        t.X >= leftMarkMin && t.X < leftMarkMax
                    )
                    ||
                    (
                        t.X >= rightMarkMin && t.X < rightMarkMax
                    )
                )
                .OrderByDescending(t => t.Y)
                .ToList();

            // 防止重复：以坐标与文本唯一标识
            HashSet<string> processed = new HashSet<string>();

            foreach (var mark in potentialMarks)
            {
                if (string.IsNullOrWhiteSpace(mark.Text))
                    continue;

                // 标记文本必须为数字（以连续数字开头）
                string s = mark.Text.Trim();
                int digits = 0;
                while (digits < s.Length && char.IsDigit(s[digits])) digits++;
                if (digits == 0)
                    continue;

                string key = $"{mark.Text}|{mark.X:F4}|{mark.Y:F4}";
                if (processed.Contains(key))
                    continue;
                processed.Add(key);

                bool isRight = (mark.X >= rightMarkMin && mark.X < rightMarkMax);
                double markY = mark.Y;

                RevisionInfo info = new RevisionInfo();
                info.Mark = mark.Text;

                // 收集同一列、且靠近 markY 的文本并合并（使用 Append）
                Func<double, double, string> collectInRange = (xmin, xmax) =>
                {
                    var segs = dataTexts
                        .Where(t => t.X >= xmin && t.X < xmax && Math.Abs(t.Y - markY) <= yTol)
                        .OrderBy(t => t.X)
                        .Select(t => t.Text)
                        .ToList();

                    if (segs.Count == 0)
                        return "";

                    string combined = "";
                    foreach (var part in segs)
                    {
                        combined = Append(combined, part);
                    }
                    return combined;
                };

                if (!isRight)
                {
                    info.Description = collectInRange(leftDescMin, leftDescMax);
                    info.Date = collectInRange(leftDateMin, leftDateMax);
                    info.Signer = collectInRange(leftSignerMin, leftSignerMax);
                    info.RevisionNumber = collectInRange(leftRevMin, leftRevMax);
                }
                else
                {
                    info.Description = collectInRange(rightDescMin, rightDescMax);
                    info.Date = collectInRange(rightDateMin, rightDateMax);
                    info.Signer = collectInRange(rightSignerMin, rightSignerMax);
                    info.RevisionNumber = collectInRange(rightRevMin, rightRevMax);
                }

                // 将生成的 info 放入 HorizontalRevisionRow（单侧填充）
                HorizontalRevisionRow row = new HorizontalRevisionRow()
                {
                    RowNumber = result.Count + 1
                };

                if (!isRight)
                    row.Left = info;
                else
                    row.Right = info;

                result.Add(row);
            }

            return result;


        }






        /// <summary>
        /// 解析横版一侧五列
        ///
        /// right=false:
        /// 左侧五列
        ///
        /// right=true:
        /// 右侧五列
        /// </summary>









        /// <summary>
        /// 根据X坐标判断所在列
        /// </summary>








        /// <summary>
        /// 判断横版模板
        /// </summary>







        private bool IsValid(
            RevisionInfo info)
        {



            if (info == null)
                return false;

    // 精确表头过滤：仅在字段完全等于表头文字时视为表头行
    string mark = info.Mark?.Trim() ?? "";
    string desc = info.Description?.Trim() ?? "";
    string date = info.Date?.Trim() ?? "";
    string signer = info.Signer?.Trim() ?? "";
    string revNumber = info.RevisionNumber?.Trim() ?? "";

    if (
        mark == "标记"
        ||
        desc == "更改内容"
        ||
        date == "更改日期"
        ||
        signer == "签名"
        ||
        revNumber == "变更号"
    )
    {
        return false;
    }

    // 只要 Mark 或 Description 任意一个非空即认为有效
    return
        !string.IsNullOrWhiteSpace(mark)
        ||
        !string.IsNullOrWhiteSpace(desc);

        }






        private string Append(
            string oldText,
            string newText)
        {



            if (string.IsNullOrWhiteSpace(oldText))
            {
                return newText;
            }



            if (oldText == newText)
            {
                return oldText;
            }



            return oldText
                +
                newText;


        }
        //================================================
        // 原竖版：按Y坐标分行
        //================================================


        private List<List<TitleText>> GroupByRow(
            List<TitleText> texts)
        {



            List<List<TitleText>> rows =
                new List<List<TitleText>>();





            List<TitleText> sorted =
                texts
                .OrderByDescending(t => t.Y)
                .ToList();





            foreach (TitleText text in sorted)
            {



                bool added =
                    false;





                foreach (List<TitleText> row in rows)
                {



                    double rowY =
                        row[0].Y;




                    if (Math.Abs(
                        rowY - text.Y)
                        < 1.5)
                    {


                        row.Add(text);

                        added =
                            true;


                        break;


                    }



                }





                if (!added)
                {

                    rows.Add(
                        new List<TitleText>()
                        {
                            text
                        }
                    );

                }



            }





            return rows;


        }


        //================================================
        // 原竖版：解析一行
        //================================================



        private RevisionInfo ParseRow(
            List<TitleText> row)
        {



            RevisionInfo info =
                new RevisionInfo();


            foreach (TitleText text in row)
            {



                // 标记

                if (
                    text.X >= 82.7599
                    &&
                    text.X < 90.7599
                )
                {

                    info.Mark =
                        text.Text;

                }


                // 更改内容

                else if (
                    text.X >= 90.7599
                    &&
                    text.X < 147.7611
                )
                {

                    info.Description =
                        text.Text;

                }


                // 日期

                else if (
                    text.X >= 147.7611
                    &&
                    text.X < 162.7611
                )
                {

                    info.Date =
                        text.Text;

                }


                // 签名

                else if (
                    text.X >= 162.7611
                    &&
                    text.X < 177.7611
                )
                {

                    info.Signer =
                        text.Text;

                }



                // 变更号

                else if (
                    text.X >= 177.7611
                    &&
                    text.X <= 192.7611
                )
                {

                    info.RevisionNumber =
                        text.Text;

                }



            }


            return info;


        }


        //================================================
        // CAD文字读取
        //================================================



        private List<TitleText> ReadTexts(
    Database db,
    ObjectId blockId)
        {
            List<TitleText> result = new List<TitleText>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr =
                    tr.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord;

                if (btr == null)
                {
                    return result;
                }

                foreach (ObjectId id in btr)
                {
                    Entity ent =
                        tr.GetObject(id, OpenMode.ForRead) as Entity;

                    if (ent == null)
                        continue;

                    // 普通文字
                    if (ent is DBText text)
                    {
                        result.Add(
                            new TitleText()
                            {
                                Text = Clean(text.TextString),
                                X = text.Position.X,
                                Y = text.Position.Y
                            }
                        );
                    }
                    // 多行文字
                    else if (ent is MText mt)
                    {
                        result.Add(
                            new TitleText()
                            {
                                Text = Clean(mt.Text),
                                X = mt.Location.X,
                                Y = mt.Location.Y
                            }
                        );
                    }
                    // 块参照：先读取属性属性引用（AttributeReference），再递归读取块定义内实体（含嵌套块）
                    else if (ent is BlockReference br)
                    {
// 1) 尝试读取 AttributeReference（实例属性）
                        try
                        {
                            foreach (ObjectId aid in br.AttributeCollection)
                            {
                                AttributeReference att =
                                    tr.GetObject(aid, OpenMode.ForRead) as AttributeReference;

                                if (att == null)
                                    continue;

                                result.Add(
                                    new TitleText()
                                    {
                                        Text = Clean(att.TextString),
                                        X = att.Position.X,
                                        Y = att.Position.Y
                                    }
                                );
                            }
                        }
                        catch
                        {
                            // 忽略属性读取异常，继续尝试读取定义内实体
                        }

                        // 2) 递归读取块定义内部的文字实体（并根据 br.BlockTransform 转换坐标）
                        ReadBlockTexts(br, tr, result);
                    }
                }

                tr.Commit();
            }

            return result;
        }

        // 递归辅助：读取 BlockReference 实例所引用的块定义内的文字（含嵌套块）
        // 参数：br - 当前块参照实例；tr - 当前事务；result - 结果列表（追加）
        private void ReadBlockTexts(
            BlockReference br,
            Transaction tr,
            List<TitleText> result)
        {
            if (br == null || tr == null || result == null)
                return;

            try
            {
                // 获取块定义记录
                BlockTableRecord blockDef =

                    tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                if (blockDef != null)
                {
}

                if (blockDef == null)
                    return;

                // 初始变换：把块定义坐标系转换到世界坐标系（由当前实例的 BlockTransform 提供）
                Matrix3d parentTransform = br.BlockTransform;

                // 遍历定义内实体，使用递归处理嵌套块
                ReadBlockRecordEntities(blockDef, parentTransform, tr, result);
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(
    ex,
    "RevisionTableReader.ReadBlockTexts"); ;
            }
        }

        // 内部递归：遍历 BlockTableRecord 的实体并根据传入的变换写入 result
        private void ReadBlockRecordEntities(
            BlockTableRecord blockDef,
            Matrix3d transformToWorld,
            Transaction tr,
            List<TitleText> result)
        {
            if (blockDef == null || tr == null || result == null)
                return;
foreach (ObjectId innerId in blockDef)
            {
                Entity innerEnt =
                    tr.GetObject(innerId, OpenMode.ForRead) as Entity;

                if (innerEnt == null)
                    continue;
// DBText：把定义坐标通过 transformToWorld 转换为世界坐标
                if (innerEnt is DBText innerText)
                {
                    try
                    {
                        var worldPt = innerText.Position.TransformBy(transformToWorld);
                        result.Add(
                            new TitleText()
                            {
                                Text = Clean(innerText.TextString),
                                X = worldPt.X,
                                Y = worldPt.Y
                            }
                        );
                    }
                    catch { /* 忽略单实体转换错误 */ }
                }
                // MText：同上
                else if (innerEnt is MText innerMText)
                {
                    try
                    {
                        var worldPt = innerMText.Location.TransformBy(transformToWorld);
                        result.Add(
                            new TitleText()
                            {
                                Text = Clean(innerMText.Text),
                                X = worldPt.X,
                                Y = worldPt.Y
                            }
                        );
                    }
                    catch { }
                }
                // 嵌套的 BlockReference：递归处理，注意变换合成（nested.BlockTransform * 当前 transform）
                else if (innerEnt is BlockReference innerBr)
                {
                    try
                    {
                        // 组合变换：先应用内部块参照的局部变换，再应用当前的 transformToWorld
                        Matrix3d combined = innerBr.BlockTransform * transformToWorld;

                        // 获取被引用的块定义并继续处理其内部实体
                        BlockTableRecord nestedDef =
                            tr.GetObject(innerBr.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

                        if (nestedDef != null)
                        {
                            ReadBlockRecordEntities(nestedDef, combined, tr, result);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        AppLogger.Error(
    ex,
    "RevisionTableReader.ReadBlockTexts"); ;
                    }
                }
                // 其余类型暂不处理（保留扩展点）
            }
        }

        //================================================
        // 字符清理
        //================================================



        private string Clean(
            string text)
        {


            if (string.IsNullOrEmpty(text))
                return "";


            return text
                .Replace("\\P", "")
                .Trim();


        }

    }



    //================================================
    // 横版一行数据结构
    //================================================



    public class HorizontalRevisionRow
    {


        public int RowNumber
        {
            get;
            set;
        }

        // 左五列

        public RevisionInfo Left
        {
            get;
            set;
        }



        // 右五列

        public RevisionInfo Right
        {
            get;
            set;
        }


        public HorizontalRevisionRow()
        {

            Left =
                new RevisionInfo();

            Right =
                new RevisionInfo();
        }

    }

}