using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfUndoControls.Abstractions;

namespace WpfUndoControls
{
    /// <summary>
    /// Redo button control with dropdown history list.
    /// Automatically manages the redo stack and provides a dropdown menu for batch redo operations.
    /// Works with any undo/redo framework through the IUndoManager abstraction.
    /// </summary>
    public class RedoButton : UndoRedoButtonBase
    {
        static RedoButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RedoButton), 
                new FrameworkPropertyMetadata(typeof(RedoButton)));
        }

        protected override IEnumerable<IUndoItem> GetStack(IUndoManager manager)
        {
            return manager.RedoStack;
        }

        protected override string GetActionVerb()
        {
            return "重做";
        }

        protected override string GetCancelText()
        {
            return "取消";
        }

        protected override string GetSingleActionText()
        {
            return "重做 1 个命令";
        }

        protected override void ExecuteSingle(IUndoManager manager)
        {
            if (manager.CanRedo)
                manager.Redo();
        }

        protected override void ExecuteTo(IUndoManager manager, IUndoItem target)
        {
            manager.RedoTo(target);
        }

        protected override bool CanExecute(IUndoManager manager)
        {
            return manager.CanRedo;
        }

        protected override ICommand GetDefaultCommand()
        {
            return ApplicationCommands.Redo;
        }
    }
}
