using Correct_test1.Core;
using Correct_test1.Models;

using System;
using System.Collections.Generic;

namespace Correct_test1.Checks
{
    /// <summary>
    /// BOM非标件归档检查。
    /// </summary>
    public class NonStandardArchiveChecker
    {
        /// <summary>
        /// 检查一个BOM。
        /// 只返回：
        /// 在归档目录中不存在的AB非标件。
        /// 已存在的不返回结果。
        /// </summary>
        public List<NonStandardArchiveCheckResult>
            Check(
                BomData bom,
                NonStandardArchiveIndex archiveIndex)
        {
            List<NonStandardArchiveCheckResult>
                results =
                    new List<NonStandardArchiveCheckResult>();


            if (bom == null ||
                bom.Items == null)
            {
                return results;
            }


            // 归档目录不可用：
            //
            // 不把任何AB件误判为不存在。

            if (archiveIndex == null ||
                !archiveIndex.IsAvailable)
            {
                return results;
            }


            foreach (
                BomItem item
                in bom.Items)
            {
                if (item == null)
                    continue;


                // 直接复用现有AB件分类器。
                //
                // 不重新写StartsWith("AB")规则。

                if (PartNumberTypeClassifier
                        .Classify(
                            item.PartNumber)
                    != PartNumberType
                        .NonStandardPart)
                {
                    continue;
                }


                // 得到归档搜索关键字

                string searchKey =
                    BuildSearchKey(
                        item.PartNumber);


                // 无效AB图号不参与搜索，
                // 防止出现只剩"AB"然后大范围误匹配。

                if (string.IsNullOrWhiteSpace(
                        searchKey))
                {
                    continue;
                }


                // 在内存索引中查找

                string matchedFilePath;


                bool exists =
                    archiveIndex.Contains(
                        searchKey,
                        out matchedFilePath);


                // 找到了：
                //
                // 正常，不生成任何结果。

                if (exists)
                    continue;


                // 没找到：
                //
                // 后续这个结果会：
                //
                // 1. 单张检查标记
                // 2. 批量检查加入报表

                NonStandardArchiveCheckResult result =
                    new NonStandardArchiveCheckResult();


                result.BomItem =
                    item;


                result.DrawingNumber =
                    bom.DrawingNumber;


                result.SourceLayoutName =
                    bom.SourceLayoutName;


                result.OriginalPartNumber =
                    item.PartNumber == null
                        ? ""
                        : item.PartNumber.Trim();


                result.SearchKey =
                    searchKey;


                result.Message =
                    "非标归档图纸未找到："
                    + searchKey;


                results.Add(
                    result);
            }


            return results;
        }


        /// <summary>
        /// 将BOM中的AB件号转换为归档搜索关键字。
        /// 规则：
        /// AB452J101
        /// -> AB452J
        /// AB452CA123
        /// -> AB452CA
        /// AB452CA
        /// -> AB452CA
        /// 只删除末尾连续数字。
        /// 中间数字保持不变。
        /// </summary>
        public static string BuildSearchKey(
            string partNumber)
        {
            if (string.IsNullOrWhiteSpace(
                    partNumber))
            {
                return "";
            }


            // 使用现有CAD文字清洗能力。

            string value =
                CadTextCleaner.Clean(
                    partNumber);


            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return "";
            }


            value =
                value.Trim();


            // 必须是真正的AB非标件

            if (PartNumberTypeClassifier
                    .Classify(
                        value)
                != PartNumberType
                    .NonStandardPart)
            {
                return "";
            }


            // 从字符串最后开始，
            // 删除连续数字。

            int endIndex =
                value.Length;


            // 从末尾开始过滤：
            //
            // 1. 数字
            // 2. 下划线
            //
            // 可以连续、混合出现。

            while (endIndex > 0)
            {
                char lastChar =
                    value[endIndex - 1];


                if (char.IsDigit(lastChar) ||
                    lastChar == '_')
                {
                    endIndex--;
                    continue;
                }


                break;
            }


            string result =
                value.Substring(
                    0,
                    endIndex)
                .Trim();


            // 防止：
            //
            // AB123
            // -> AB
            //
            // 这种错误数据导致搜索整个归档中
            // 所有包含AB的文件。

            if (result.Length <= 2 ||
                string.Equals(
                    result,
                    "AB",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }


            return result;
        }
    }
}
