using System;
using Pholus.Editor.Fixes.Models;

namespace Pholus.Editor.Fixes.Interfaces
{
    /// <summary>
    /// Interface for managing undo operations.
    /// Single Responsibility: Only handles undo stack.
    /// </summary>
    public interface IUndoManager
    {
        /// <summary>
        /// Registers an action that can be undone.
        /// </summary>
        /// <param name="action">The undo action to register.</param>
        void RegisterUndo(UndoAction action);

        /// <summary>
        /// Whether there are actions that can be undone.
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// Gets the next action that would be undone without removing it.
        /// </summary>
        UndoAction PeekUndo();

        /// <summary>
        /// Undoes the most recent action.
        /// </summary>
        /// <returns>True if undo succeeded.</returns>
        bool Undo();

        /// <summary>
        /// Clears all undo history.
        /// </summary>
        void Clear();

        /// <summary>
        /// Number of actions in the undo stack.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Event raised when undo stack changes.
        /// </summary>
        event Action OnUndoStackChanged;
    }
}
