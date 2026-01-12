using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfUndoControls.Abstractions;

namespace WpfUndoControls
{
    /// <summary>
    /// Undo button control with dropdown history list.
    /// Automatically manages the undo stack and provides a dropdown menu for batch undo operations.
    /// Works with any undo/redo framework through the IUndoManager abstraction.
    /// </summary>
    public class UndoButton : UndoRedoButtonBase
    {
        static UndoButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(UndoButton),
                new FrameworkPropertyMetadata(typeof(UndoButton)));
        }

        protected override IEnumerable<IUndoItem> GetStack(IUndoManager manager)
        {
            return manager.UndoStack;
        }

        protected override string GetActionVerb()
        {
            return "放弃";
        }

        protected override string GetCancelText()
        {
            return "取消";
        }

        protected override string GetSingleActionText()
        {
            return "放弃 1 个命令";
        }

        protected override void ExecuteSingle(IUndoManager manager)
        {
            if (manager.CanUndo)
                manager.Undo();
        }

        protected override void ExecuteTo(IUndoManager manager, IUndoItem target)
        {
            manager.UndoTo(target);
        }

        protected override bool CanExecute(IUndoManager manager)
        {
            return manager.CanUndo;
        }

        protected override ICommand GetDefaultCommand()
        {
            return ApplicationCommands.Undo;
        }
    }
}
