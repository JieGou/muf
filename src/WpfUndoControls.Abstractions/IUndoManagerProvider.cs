using System;

namespace WpfUndoControls.Abstractions
{
    /// <summary>
    /// Provides a way to obtain an IUndoManager from a root object.
    /// This allows WpfUndoControls to remain framework-agnostic while supporting
    /// different undo/redo implementations that use different methods to associate
    /// an UndoManager with a root object.
    /// </summary>
    public interface IUndoManagerProvider
    {
        /// <summary>
        /// Gets an IUndoManager for the specified root object.
        /// </summary>
        /// <param name="root">The root object (e.g., document, view model) that has undo/redo capabilities.</param>
        /// <returns>An IUndoManager instance, or null if the root doesn't support undo/redo.</returns>
        IUndoManager GetUndoManager(object root);
    }
}
