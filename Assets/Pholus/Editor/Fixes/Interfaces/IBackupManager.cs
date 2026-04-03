using System.Collections.Generic;
using Pholus.Editor.Fixes.Models;

namespace Pholus.Editor.Fixes.Interfaces
{
    /// <summary>
    /// Interface for managing file backups.
    /// Single Responsibility: Only handles backup operations.
    /// </summary>
    public interface IBackupManager
    {
        /// <summary>
        /// Creates a backup of a file.
        /// </summary>
        /// <param name="filePath">Path to the file to back up.</param>
        /// <returns>Path to the backup file.</returns>
        string CreateBackup(string filePath);

        /// <summary>
        /// Restores a file from a backup.
        /// </summary>
        /// <param name="backupPath">Path to the backup file.</param>
        /// <param name="originalPath">Path to restore to.</param>
        /// <returns>True if restoration succeeded.</returns>
        bool RestoreBackup(string backupPath, string originalPath);

        /// <summary>
        /// Cleans up old backups, keeping only the most recent ones.
        /// </summary>
        /// <param name="originalFilePath">Path to the original file.</param>
        /// <param name="keepCount">Number of backups to keep.</param>
        void CleanupOldBackups(string originalFilePath, int keepCount = 10);

        /// <summary>
        /// Gets all backups for a file.
        /// </summary>
        /// <param name="originalFilePath">Path to the original file.</param>
        /// <returns>List of backup info, ordered by timestamp descending.</returns>
        IEnumerable<BackupInfo> GetBackups(string originalFilePath);

        /// <summary>
        /// Gets the backup folder path for a file.
        /// </summary>
        /// <param name="filePath">Path to the original file.</param>
        /// <returns>Path to the backup folder.</returns>
        string GetBackupFolder(string filePath);
    }
}
