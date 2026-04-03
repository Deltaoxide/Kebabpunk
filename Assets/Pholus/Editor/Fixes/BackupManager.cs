using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pholus.Editor.Core;
using Pholus.Editor.Fixes.Interfaces;
using Pholus.Editor.Fixes.Models;

namespace Pholus.Editor.Fixes
{
    /// <summary>
    /// Manages file backups for undo support.
    /// Single Responsibility: Only handles backup operations.
    /// </summary>
    public class BackupManager : IBackupManager
    {
        private const string BackupFolderName = ".pholus-backup";
        private const string BackupExtension = ".bak";
        private const string TimestampFormat = "yyyyMMdd_HHmmss";

        public string CreateBackup(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Cannot backup: file not found: {filePath}");
            }

            var backupFolder = GetBackupFolder(filePath);
            Directory.CreateDirectory(backupFolder);

            var fileName = Path.GetFileName(filePath);
            var timestamp = DateTime.Now.ToString(TimestampFormat);
            var backupName = $"{fileName}.{timestamp}{BackupExtension}";
            var backupPath = Path.Combine(backupFolder, backupName);

            File.Copy(filePath, backupPath, overwrite: true);

            PholusLogger.Log($"Backup created: {backupPath}");

            return backupPath;
        }

        public bool RestoreBackup(string backupPath, string originalPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    PholusLogger.LogError($"Backup not found: {backupPath}");
                    return false;
                }

                File.Copy(backupPath, originalPath, overwrite: true);

                PholusLogger.Log($"Restored from backup: {backupPath}");

                // Refresh Unity's asset database
                UnityEditor.AssetDatabase.Refresh();

                return true;
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Failed to restore backup: {ex.Message}");
                return false;
            }
        }

        public void CleanupOldBackups(string originalFilePath, int keepCount = 10)
        {
            try
            {
                var backups = GetBackups(originalFilePath).ToList();

                if (backups.Count <= keepCount)
                {
                    return;
                }

                // Delete oldest backups
                var toDelete = backups.Skip(keepCount);

                foreach (var backup in toDelete)
                {
                    try
                    {
                        File.Delete(backup.FilePath);
                        PholusLogger.Log($"Deleted old backup: {backup.FilePath}");
                    }
                    catch (Exception ex)
                    {
                        PholusLogger.LogWarning($"Failed to delete backup {backup.FilePath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                PholusLogger.LogWarning($"Backup cleanup failed: {ex.Message}");
            }
        }

        public IEnumerable<BackupInfo> GetBackups(string originalFilePath)
        {
            var backupFolder = GetBackupFolder(originalFilePath);

            if (!Directory.Exists(backupFolder))
            {
                yield break;
            }

            var fileName = Path.GetFileName(originalFilePath);
            var pattern = $"{fileName}.*{BackupExtension}";

            var files = Directory.GetFiles(backupFolder, pattern);

            var backups = files
                .Select(f => new BackupInfo
                {
                    FilePath = f,
                    OriginalPath = originalFilePath,
                    Timestamp = ParseTimestamp(f),
                    Size = new FileInfo(f).Length
                })
                .OrderByDescending(b => b.Timestamp);

            foreach (var backup in backups)
            {
                yield return backup;
            }
        }

        public string GetBackupFolder(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            return Path.Combine(directory, BackupFolderName);
        }

        private DateTime ParseTimestamp(string backupPath)
        {
            try
            {
                var fileName = Path.GetFileName(backupPath);
                // Format: OriginalName.cs.20250104_123456.bak
                var parts = fileName.Split('.');

                if (parts.Length >= 3)
                {
                    var timestampPart = parts[parts.Length - 2]; // The timestamp
                    if (DateTime.TryParseExact(
                        timestampPart,
                        TimestampFormat,
                        null,
                        System.Globalization.DateTimeStyles.None,
                        out var timestamp))
                    {
                        return timestamp;
                    }
                }
            }
            catch
            {
                // Fall through to default
            }

            // Fallback to file modification time
            return File.GetLastWriteTime(backupPath);
        }
    }
}
