using System;

namespace Pholus.Editor.Fixes.Models
{
    /// <summary>
    /// Represents an undoable action (fix application).
    /// </summary>
    [Serializable]
    public class UndoAction
    {
        /// <summary>
        /// Path to the script that was modified.
        /// </summary>
        public string ScriptPath { get; set; }

        /// <summary>
        /// Path to the backup file.
        /// </summary>
        public string BackupPath { get; set; }

        /// <summary>
        /// Description of what was fixed.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// When the fix was applied.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Issue type that was fixed.
        /// </summary>
        public string IssueType { get; set; }

        /// <summary>
        /// How long ago this action occurred.
        /// </summary>
        public TimeSpan Age => DateTime.Now - Timestamp;

        /// <summary>
        /// Friendly display of when this occurred.
        /// </summary>
        public string TimeAgo
        {
            get
            {
                var age = Age;
                if (age.TotalSeconds < 60) return "just now";
                if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes}m ago";
                if (age.TotalHours < 24) return $"{(int)age.TotalHours}h ago";
                return $"{(int)age.TotalDays}d ago";
            }
        }

        public UndoAction() { }

        public UndoAction(string scriptPath, string backupPath, string description, string issueType = null)
        {
            ScriptPath = scriptPath;
            BackupPath = backupPath;
            Description = description;
            IssueType = issueType;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Information about a backup file.
    /// </summary>
    [Serializable]
    public class BackupInfo
    {
        /// <summary>
        /// Path to the backup file.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Path to the original file this backs up.
        /// </summary>
        public string OriginalPath { get; set; }

        /// <summary>
        /// When the backup was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Size of the backup in bytes.
        /// </summary>
        public long Size { get; set; }
    }
}
