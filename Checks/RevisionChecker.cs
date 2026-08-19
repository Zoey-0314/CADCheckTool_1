using System;
using System.Collections.Generic;

using Correct_test1.Models;


namespace Correct_test1.Checks
{

    public class RevisionChecker
    {

        /// <summary>
        /// 修改记录完整性检查
        /// 规则：
        /// 1. Mark 有值
        /// 2. Description 有值
        /// 才认为是一条有效修改记录
        /// 检查：
        /// Date
        /// Signer
        /// 不检查：
        /// RevisionNumber
        /// </summary>

        public List<RevisionCheckIssue> Check(
            string layoutName,
            List<RevisionInfo> revisions)
        {

            List<RevisionCheckIssue> issues =
                new List<RevisionCheckIssue>();

            if (revisions == null)
                return issues;

            foreach (RevisionInfo rev in revisions)
            {

                if (rev == null)
                    continue;

                // 没有标记，不检查
                if (
                    string.IsNullOrWhiteSpace(
                        rev.Mark))
                {
                    continue;
                }

                // 没有更改内容，不认为是一条记录
                if (
                    string.IsNullOrWhiteSpace(
                        rev.Description))
                {
                    continue;
                }

                // 检查日期

                if (
                    string.IsNullOrWhiteSpace(
                        rev.Date))
                {

                    issues.Add(
                        CreateIssue(
                            layoutName,
                            rev,
                            "更改日期",
                            "缺少更改日期"
                        )
                    );

                }

                // 检查签名

                if (
                    string.IsNullOrWhiteSpace(
                        rev.Signer))
                {

                    issues.Add(
                        CreateIssue(
                            layoutName,
                            rev,
                            "签名",
                            "缺少签名"
                        )
                    );

                }

            }

            return issues;

        }

        private RevisionCheckIssue CreateIssue(
            string layoutName,
            RevisionInfo rev,
            string field,
            string message)
        {

            return new RevisionCheckIssue()
            {

                LayoutName =
                    layoutName,

                Orientation =
                    "",

                RowNumber =
                    0,

                Mark =
                    rev.Mark,

                MissingField =
                    field,

                X =
                    0,

                Y =
                    0,

                Message =
                    message

            };

        }

    }

}