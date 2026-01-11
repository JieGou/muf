using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MonitoredUndo;

namespace WpfUndoControls
{
    /// <summary>
    /// Redo button control with dropdown history list.
    /// Automatically manages the redo stack and provides a dropdown menu for batch redo operations.
    /// </summary>
    public class RedoButton : UndoRedoButtonBase
    {
        static RedoButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RedoButton), 
                new FrameworkPropertyMetadata(typeof(RedoButton)));
        }

        protected override IEnumerable<ChangeSet> GetStack(UndoRoot root)
        {
            return root.RedoStack;
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

        protected override void ExecuteSingle(UndoRoot root)
        {
            if (root.CanRedo)
                root.Redo();
        }

        protected override void ExecuteTo(UndoRoot root, ChangeSet target)
        {
            var stack = root.RedoStack.ToList();
            var index = stack.IndexOf(target);
            if (index >= 0)
            {
                // Redo count = index + 1
                for (int i = 0; i <= index; i++)
                {
                    if (root.CanRedo)
                        root.Redo();
                }
            }
        }

        protected override bool CanExecute(UndoRoot root)
        {
            return root.CanRedo;
        }

        protected override ICommand GetDefaultCommand()
        {
            return ApplicationCommands.Redo;
        }
    }
}
