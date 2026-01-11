using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MonitoredUndo;

namespace WpfUndoControls
{
    /// <summary>
    /// Undo button control with dropdown history list.
    /// Automatically manages the undo stack and provides a dropdown menu for batch undo operations.
    /// </summary>
    public class UndoButton : UndoRedoButtonBase
    {
        static UndoButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(UndoButton),
                new FrameworkPropertyMetadata(typeof(UndoButton)));
        }

        protected override IEnumerable<ChangeSet> GetStack(UndoRoot root)
        {
            return root.UndoStack;
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

        protected override void ExecuteSingle(UndoRoot root)
        {
            if (root.CanUndo)
                root.Undo();
        }

        protected override void ExecuteTo(UndoRoot root, ChangeSet target)
        {
            var stack = root.UndoStack.ToList();
            var index = stack.IndexOf(target);
            if (index >= 0)
            {
                // Undo count = index + 1
                for (int i = 0; i <= index; i++)
                {
                    if (root.CanUndo)
                        root.Undo();
                }
            }
        }

        protected override bool CanExecute(UndoRoot root)
        {
            return root.CanUndo;
        }

        protected override ICommand GetDefaultCommand()
        {
            return ApplicationCommands.Undo;
        }
    }
}
