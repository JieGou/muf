using WpfUndoControls.Abstractions;

namespace MonitoredUndo.WpfIntegration
{
    /// <summary>
    /// Provides IUndoManager instances for MonitoredUndo framework.
    /// Converts root objects to UndoRoot via UndoService, then wraps in an adapter.
    /// </summary>
    public class MonitoredUndoManagerProvider : IUndoManagerProvider
    {
        /// <summary>
        /// Static singleton instance for convenience.
        /// </summary>
        public static readonly MonitoredUndoManagerProvider Instance = new MonitoredUndoManagerProvider();

        public IUndoManager GetUndoManager(object root)
        {
            if (root == null)
                return null;

            var undoRoot = UndoService.Current[root];
            if (undoRoot == null)
                return null;

            return new MonitoredUndoManagerAdapter(undoRoot);
        }
    }
}
