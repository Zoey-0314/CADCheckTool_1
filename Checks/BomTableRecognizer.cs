using Correct_test1.Configs;
using Correct_test1.Core;
using Correct_test1.Models;
using System;
using System.Text.RegularExpressions;


namespace Correct_test1.Checks
{

    /// <summary>
    /// BOM表识别器
    ///
    /// 一个CadTableData对应一个BOM判断
    ///
    /// 负责：
    /// 1. 判断是否BOM
    /// 2. 查找表头
    /// 3. 建立列映射
    /// 4. 提取BOM数据
    /// </summary>
    public class BomTableRecognizer
    {


        /// <summary>
        /// 判断当前Table是否为BOM
        /// </summary>
        public bool IsBom(
            CadTableData table)
        {

            if (table == null)
                return false;


            BomColumnMapping mapping =
                FindColumnMapping(table);


            if (mapping == null)
                return false;


            return mapping.IsValid();

        }



        /// <summary>
        /// 解析BOM表
        /// </summary>
        public BomData Parse(
            CadTableData table)
        {

            BomData bom =
                new BomData();


            if (table == null)
                return bom;



            int headerRow =
                FindHeaderRow(table);



            if (headerRow < 0)
                return bom;



            BomColumnMapping mapping =
                FindColumnMapping(table);



            if (mapping == null ||
               !mapping.IsValid())
            {
                return bom;
            }



            // 提取图号

            bom.DrawingNumber =
                FindDrawingNumber(
                    table,
                    headerRow);



            // 提取BOM明细

            for (int r = headerRow + 1;
                r < table.Rows;
                r++)
            {


                BomItem item =
                    new BomItem();



                item.No =
                    table.GetCell(
                        r,
                        mapping.NoColumn);



                item.PartNumber =
                    CadTextCleaner.Clean(
                        table.GetCell(
                            r,
                            mapping.PartNumberColumn));



                item.Name =
                    CadTextCleaner.Clean(
                        table.GetCell(
                            r,
                            mapping.NameColumn));



                item.Quantity =
                    table.GetCell(
                        r,
                        mapping.QuantityColumn);



                // 空行跳过

                if (string.IsNullOrWhiteSpace(
                    item.PartNumber))
                {
                    continue;
                }



                bom.Items.Add(item);

            }



            return bom;

        }



        #region 表头查找


        /// <summary>
        /// 查找BOM表头所在行
        ///
        /// 必须同时包含:
        /// No.
        /// Part No.
        /// Name
        /// Qut.
        /// </summary>
        private int FindHeaderRow(
            CadTableData table)
        {


            for (int r = 0;
                r < table.Rows;
                r++)
            {


                bool hasNo = false;

                bool hasPart = false;

                bool hasName = false;

                bool hasQuantity = false;



                for (int c = 0;
                    c < table.Columns;
                    c++)
                {


                    string value =
                        table.GetCell(
                            r,
                            c);



                    if (IsHeader(
                        value,
                        BomConfig.NoHeaders))
                    {
                        hasNo = true;
                    }



                    if (IsHeader(
                        value,
                        BomConfig.PartNumberHeaders))
                    {
                        hasPart = true;
                    }



                    if (IsHeader(
                        value,
                        BomConfig.NameHeaders))
                    {
                        hasName = true;
                    }



                    if (IsHeader(
                        value,
                        BomConfig.QuantityHeaders))
                    {
                        hasQuantity = true;
                    }

                }



                if (hasNo &&
                   hasPart &&
                   hasName &&
                   hasQuantity)
                {
                    return r;
                }

            }



            return -1;

        }



        #endregion



        #region 列映射


        /// <summary>
        /// 查找字段对应列
        /// </summary>
        public BomColumnMapping FindColumnMapping(
            CadTableData table)
        {


            int headerRow =
                FindHeaderRow(table);



            if (headerRow < 0)
                return null;



            BomColumnMapping mapping =
                new BomColumnMapping();



            for (int c = 0;
                c < table.Columns;
                c++)
            {


                string header =
                    table.GetCell(
                        headerRow,
                        c);



                if (IsHeader(
                    header,
                    BomConfig.NoHeaders))
                {
                    mapping.NoColumn = c;
                }



                if (IsHeader(
                    header,
                    BomConfig.PartNumberHeaders))
                {
                    mapping.PartNumberColumn = c;
                }



                if (IsHeader(
                    header,
                    BomConfig.NameHeaders))
                {
                    mapping.NameColumn = c;
                }



                if (IsHeader(
                    header,
                    BomConfig.QuantityHeaders))
                {
                    mapping.QuantityColumn = c;
                }

            }



            return mapping;

        }



        #endregion



        #region 图号提取


        /// <summary>
        /// 提取图号
        ///
        /// 在表头之前寻找
        /// 例如:
        /// NS265R1
        /// NS135H_
        /// </summary>
        private string FindDrawingNumber(
    CadTableData table,
    int headerRow)
        {

            // 只查表头之前

            for (int r = 0;
                r < headerRow;
                r++)
            {

                for (int c = 0;
                    c < table.Columns;
                    c++)
                {

                    string value =
                        table.GetCell(r, c);



                    value =
                        CadTextCleaner.Clean(value);



                    if (IsDrawingNumber(value))
                    {
                        return value;
                    }

                }

            }



            return "";

        }
        private bool IsDrawingNumber(
    string value)
        {

            if (string.IsNullOrWhiteSpace(value))
                return false;



            value = value.Trim();



            return Regex.IsMatch(
                value,
                @"^[A-Z]{2,}[A-Z0-9_-]*[0-9]+[A-Z0-9_-]*$"
            );

        }

        #endregion

        #region 工具方法


        /// <summary>
        /// 判断单元格是否为指定表头
        /// </summary>
        private bool IsHeader(
            string value,
            System.Collections.Generic.List<string> headers)
        {


            if (string.IsNullOrWhiteSpace(value))
                return false;



            string text =
                value
                .Trim()
                .Replace("\r", "")
                .Replace("\n", "");



            foreach (string header in headers)
            {

                if (text.Equals(
                    header.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

            }



            return false;

        }


        #endregion


    }

}