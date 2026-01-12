using WpfUndoControls.Abstractions;

namespace MonitoredUndo.WpfIntegration
{
    /// <summary>
    /// Adapter that wraps a MonitoredUndo.ChangeSet to implement IUndoItem.
    /// This allows WpfUndoControls to work with MonitoredUndo framework.
    /// </summary>
    internal class MonitoredUndoItemAdapter : IUndoItem
    {
        private readonly ChangeSet _changeSet;

        public MonitoredUndoItemAdapter(ChangeSet changeSet)
        {
            _changeSet = changeSet;
        }

        public ChangeSet ChangeSet => _changeSet;

        public string Description => _changeSet?.Description ?? string.Empty;

        public void Undo()
        {
            _changeSet?.Undo();
        }

        public void Redo()
        {
            _changeSet.Redo();
        }
    }
}
