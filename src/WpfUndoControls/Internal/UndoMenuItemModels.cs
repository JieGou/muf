using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WpfUndoControls.Abstractions;

namespace WpfUndoControls.Internal
{
    /// <summary>
    /// Represents a separator in the undo/redo dropdown menu.
    /// </summary>
    internal class UndoListSeparator { }

    /// <summary>
    /// Represents an actionable option in the undo/redo dropdown menu, such as "Undo N commands" or "Cancel".
    /// Used as the final selectable item in the undo/redo menu, providing a summary action or a cancel operation.
    /// </summary>
    internal class UndoListOption : INotifyPropertyChanged
    {
        private string _label;
        private ICommand _command;
        private object _commandParameter;
        private bool _isCancel;

        public string Label
        {
            get => _label;
            set
            {
                if (_label != value)
                {
                    _label = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand Command
        {
            get => _command;
            set
            {
                if (_command != value)
                {
                    _command = value;
                    OnPropertyChanged();
                }
            }
        }

        public object CommandParameter
        {
            get => _commandParameter;
            set
            {
                if (_commandParameter != value)
                {
                    _commandParameter = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand PreviewCommand { get; set; }

        public System.Windows.IInputElement CommandTarget { get; set; }

        public bool IsCancel
        {
            get => _isCancel;
            set
            {
                if (_isCancel != value)
                {
                    _isCancel = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Command to close the popup. This should be set from the UndoRedoButton.
        /// </summary>
        public ICommand ClosePopupCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a single undoable or redoable item in the undo/redo dropdown menu.
    /// Wraps an <see cref="IUndoItem"/> and provides properties for UI interaction, such as highlighting and command binding.
    /// </summary>
    internal class UndoListItem : INotifyPropertyChanged
    {
        private bool _isHighlighted;

        public UndoListItem(IUndoItem undoItem)
        {
            UndoItem = undoItem;
        }

        public IUndoItem UndoItem { get; }

        public string Description => UndoItem?.Description ?? string.Empty;

        public ICommand PreviewCommand { get; set; }
        
        public ICommand ClickCommand { get; set; }

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                if (_isHighlighted != value)
                {
                    _isHighlighted = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
