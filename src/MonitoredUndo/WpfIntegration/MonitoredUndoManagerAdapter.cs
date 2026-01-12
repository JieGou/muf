using System;
using System.Collections.Generic;
using System.Linq;
using WpfUndoControls.Abstractions;

namespace MonitoredUndo.WpfIntegration
{
    /// <summary>
    /// Adapter that wraps a MonitoredUndo.UndoRoot to implement IUndoManager.
    /// This allows WpfUndoControls to work with MonitoredUndo framework.
    /// </summary>
    public class MonitoredUndoManagerAdapter : IUndoManager
    {
        private readonly UndoRoot _undoRoot;

        public MonitoredUndoManagerAdapter(UndoRoot undoRoot)
        {
            _undoRoot = undoRoot ?? throw new ArgumentNullException(nameof(undoRoot));

            _undoRoot.UndoStackChanged += OnUndoRootUndoStackChanged;
            _undoRoot.RedoStackChanged += OnUndoRootRedoStackChanged;
        }

        public IEnumerable<IUndoItem> UndoStack
        {
            get
            {
                return _undoRoot.UndoStack.Select(cs => new MonitoredUndoItemAdapter(cs));
            }
        }

        public IEnumerable<IUndoItem> RedoStack
        {
            get
            {
                return _undoRoot.RedoStack.Select(cs => new MonitoredUndoItemAdapter(cs));
            }
        }

        public bool CanUndo => _undoRoot.CanUndo;

        public bool CanRedo => _undoRoot.CanRedo;

        public void Undo()
        {
            if (_undoRoot.CanUndo)
                _undoRoot.Undo();
        }

        public void UndoTo(IUndoItem targetItem)
        {
            if (targetItem is MonitoredUndoItemAdapter adapter)
            {
                _undoRoot.Undo(adapter.ChangeSet);
            }
            else
            {
                var stack = _undoRoot.UndoStack.ToList();
                var index = -1;

                foreach (var changeSet in stack)
                {
                    index++;
                    if (changeSet.Description == targetItem.Description)
                    {
                        for (int i = 0; i <= index; i++)
                        {
                            if (_undoRoot.CanUndo)
                                _undoRoot.Undo();
                        }
                        break;
                    }
                }
            }
        }

        public void Redo()
        {
            if (_undoRoot.CanRedo)
                _undoRoot.Redo();
        }

        public void RedoTo(IUndoItem targetItem)
        {
            if (targetItem is MonitoredUndoItemAdapter adapter)
            {
                _undoRoot.Redo(adapter.ChangeSet);
            }
            else
            {
                var stack = _undoRoot.RedoStack.ToList();
                var index = -1;

                foreach (var changeSet in stack)
                {
                    index++;
                    if (changeSet.Description == targetItem.Description)
                    {
                        for (int i = 0; i <= index; i++)
                        {
                            if (_undoRoot.CanRedo)
                                _undoRoot.Redo();
                        }
                        break;
                    }
                }
            }
        }

        public event EventHandler UndoStackChanged;
        public event EventHandler RedoStackChanged;

        private void OnUndoRootUndoStackChanged(object sender, EventArgs e)
        {
            UndoStackChanged?.Invoke(this, e);
        }

        private void OnUndoRootRedoStackChanged(object sender, EventArgs e)
        {
            RedoStackChanged?.Invoke(this, e);
        }
    }
}
