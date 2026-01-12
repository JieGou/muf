using System;

namespace WpfUndoControls.Abstractions
{
    /// <summary>
    /// Represents a single undoable/redoable item (e.g., a changeset or command).
    /// This abstraction allows WpfUndoControls to work with any undo/redo implementation.
    /// </summary>
    public interface IUndoItem
    {
        /// <summary>
        /// Gets a human-readable description of this undo item.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Performs the undo operation for this item.
        /// </summary>
        void Undo();

        /// <summary>
        /// Performs the redo operation for this item.
        /// </summary>
        void Redo();
    }
}
