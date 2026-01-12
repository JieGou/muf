using System;
using System.Collections.Generic;

namespace WpfUndoControls.Abstractions
{
    /// <summary>
    /// Manages undo and redo operations for a root object or document.
    /// This abstraction allows WpfUndoControls to work with any undo/redo framework.
    /// </summary>
    public interface IUndoManager
    {
        /// <summary>
        /// Gets the collection of items in the undo stack.
        /// </summary>
        IEnumerable<IUndoItem> UndoStack { get; }

        /// <summary>
        /// Gets the collection of items in the redo stack.
        /// </summary>
        IEnumerable<IUndoItem> RedoStack { get; }

        /// <summary>
        /// Gets whether an undo operation can be performed.
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// Gets whether a redo operation can be performed.
        /// </summary>
        bool CanRedo { get; }

        /// <summary>
        /// Performs a single undo operation.
        /// </summary>
        void Undo();

        /// <summary>
        /// Performs undo operations up to and including the specified item.
        /// </summary>
        /// <param name="targetItem">The last item to undo.</param>
        void UndoTo(IUndoItem targetItem);

        /// <summary>
        /// Performs a single redo operation.
        /// </summary>
        void Redo();

        /// <summary>
        /// Performs redo operations up to and including the specified item.
        /// </summary>
        /// <param name="targetItem">The last item to redo.</param>
        void RedoTo(IUndoItem targetItem);

        /// <summary>
        /// Raised when the undo stack changes.
        /// </summary>
        event EventHandler UndoStackChanged;

        /// <summary>
        /// Raised when the redo stack changes.
        /// </summary>
        event EventHandler RedoStackChanged;
    }
}
