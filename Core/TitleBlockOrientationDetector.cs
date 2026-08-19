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