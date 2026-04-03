using System;
using System.Collections.Generic;
using System.Text;
using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Fixes.Interfaces;
using Pholus.Editor.Fixes.Models;

namespace Pholus.Editor.Fixes
{
    /// <summary>
    /// Generates diffs between original and modified code.
    /// Uses a simple line-by-line comparison algorithm.
    /// </summary>
    public class DiffGenerator : IDiffGenerator
    {
        public DiffResult GenerateDiff(string original, string modified)
        {
            var result = new DiffResult
            {
                Original = original,
                Modified = modified,
                Lines = new List<DiffLine>()
            };

            if (original == modified)
            {
                // No changes
                var lines = original.Split('\n');
                for (var i = 0; i < lines.Length; i++)
                {
                    result.Lines.Add(DiffLine.Unchanged(i + 1, i + 1, lines[i].TrimEnd('\r')));
                }
                return result;
            }

            var originalLines = SplitLines(original);
            var modifiedLines = SplitLines(modified);

            // Use Myers diff algorithm (simplified LCS-based approach)
            var diff = ComputeDiff(originalLines, modifiedLines);

            var origLineNum = 1;
            var modLineNum = 1;

            foreach (var entry in diff)
            {
                switch (entry.Type)
                {
                    case DiffEntryType.Equal:
                        result.Lines.Add(DiffLine.Unchanged(origLineNum++, modLineNum++, entry.Line));
                        break;
                    case DiffEntryType.Insert:
                        result.Lines.Add(DiffLine.Added(modLineNum++, entry.Line));
                        result.LinesAdded++;
                        break;
                    case DiffEntryType.Delete:
                        result.Lines.Add(DiffLine.Removed(origLineNum++, entry.Line));
                        result.LinesRemoved++;
                        break;
                }
            }

            return result;
        }

        public string FormatDiffForDisplay(DiffResult diff)
        {
            var sb = new StringBuilder();

            foreach (var line in diff.Lines)
            {
                var prefix = line.Type switch
                {
                    DiffLineType.Added => "+ ",
                    DiffLineType.Removed => "- ",
                    _ => "  "
                };

                sb.AppendLine($"{prefix}{line.Content}");
            }

            sb.AppendLine();
            sb.AppendLine(diff.Summary);

            return sb.ToString();
        }

        private string[] SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Array.Empty<string>();
            }

            var lines = text.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].TrimEnd('\r');
            }
            return lines;
        }

        private List<DiffEntry> ComputeDiff(string[] original, string[] modified)
        {
            var result = new List<DiffEntry>();

            // Compute LCS (Longest Common Subsequence)
            var lcs = ComputeLCS(original, modified);

            var i = 0;
            var j = 0;
            var k = 0;

            while (i < original.Length || j < modified.Length)
            {
                if (k < lcs.Count && i < original.Length && j < modified.Length &&
                    original[i] == lcs[k] && modified[j] == lcs[k])
                {
                    // Common line
                    result.Add(new DiffEntry { Type = DiffEntryType.Equal, Line = original[i] });
                    i++;
                    j++;
                    k++;
                }
                else if (j < modified.Length && (k >= lcs.Count || modified[j] != lcs[k]))
                {
                    // Inserted line
                    result.Add(new DiffEntry { Type = DiffEntryType.Insert, Line = modified[j] });
                    j++;
                }
                else if (i < original.Length && (k >= lcs.Count || original[i] != lcs[k]))
                {
                    // Deleted line
                    result.Add(new DiffEntry { Type = DiffEntryType.Delete, Line = original[i] });
                    i++;
                }
            }

            return result;
        }

        private List<string> ComputeLCS(string[] a, string[] b)
        {
            var m = a.Length;
            var n = b.Length;

            // Create DP table
            var dp = new int[m + 1, n + 1];

            for (var i = 1; i <= m; i++)
            {
                for (var j = 1; j <= n; j++)
                {
                    if (a[i - 1] == b[j - 1])
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                    }
                }
            }

            // Backtrack to find LCS
            var lcs = new List<string>();
            var x = m;
            var y = n;

            while (x > 0 && y > 0)
            {
                if (a[x - 1] == b[y - 1])
                {
                    lcs.Add(a[x - 1]);
                    x--;
                    y--;
                }
                else if (dp[x - 1, y] > dp[x, y - 1])
                {
                    x--;
                }
                else
                {
                    y--;
                }
            }

            lcs.Reverse();
            return lcs;
        }

        private enum DiffEntryType { Equal, Insert, Delete }

        private class DiffEntry
        {
            public DiffEntryType Type { get; set; }
            public string Line { get; set; }
        }
    }
}
