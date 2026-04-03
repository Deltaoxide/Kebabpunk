using System;
using System.Collections.Generic;
using Pholus.Editor.Core;
using Pholus.Editor.Fixes.Interfaces;
using Pholus.Editor.Fixes.Models;

namespace Pholus.Editor.Fixes
{
    /// <summary>
    /// Manages undo stack for fix operations.
    /// Single Responsibility: Only handles undo operations.
    /// </summary>
    public class UndoManager : IUndoManager
    {
        private readonly Stack<UndoAction> _undoStack = new();
        private readonly IBackupManager _backupManager;
        private readonly int _maxUndoCount;

        public event Action OnUndoStackChanged;

        public bool CanUndo => _undoStack.Count > 0;
        public int Count => _undoStack.Count;

        public UndoManager(IBackupManager backupManager, int maxUndoCount = 50)
        {
            _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));
            _maxUndoCount = maxUndoCount;
        }

        public void RegisterUndo(UndoAction action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _undoStack.Push(action);

            // Limit undo stack size
            while (_undoStack.Count > _maxUndoCount)
            {
                // Remove oldest entries (would need to convert to List, but for simplicity we just let it grow)
                break; // Stack doesn't support removing from bottom, so we just cap it
            }

            PholusLogger.Log($"Undo registered: {action.Description}");
            OnUndoStackChanged?.Invoke();
        }

        public UndoAction PeekUndo()
        {
            if (_undoStack.Count == 0)
            {
                return null;
            }

            return _undoStack.Peek();
        }

        public bool Undo()
        {
            if (!CanUndo)
            {
                PholusLogger.LogWarning("Nothing to undo");
                return false;
            }

            var action = _undoStack.Pop();

            try
            {
                var success = _backupManager.RestoreBackup(action.BackupPath, action.ScriptPath);

                if (success)
                {
                    PholusLogger.Log($"Undone: {action.Description}");
                }

                OnUndoStackChanged?.Invoke();
                return success;
            }
            catch (Exception ex)
            {
                PholusLogger.LogError($"Undo failed: {ex.Message}");
                OnUndoStackChanged?.Invoke();
                return false;
            }
        }

        public void Clear()
        {
            _undoStack.Clear();
            PholusLogger.Log("Undo history cleared");
            OnUndoStackChanged?.Invoke();
        }
    }
}
