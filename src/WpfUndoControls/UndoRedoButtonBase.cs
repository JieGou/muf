using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using MonitoredUndo;
using WpfUndoControls.Internal;

namespace WpfUndoControls
{
    /// <summary>
    /// Base class for Undo/Redo buttons with dropdown menu functionality.
    /// Handles all the common logic for managing undo/redo stacks and menu items.
    /// </summary>
    [TemplatePart(Name = PartSplitElement, Type = typeof(UIElement))]
    [TemplatePart(Name = PartPopup, Type = typeof(Popup))]
    [TemplatePart(Name = PartListBox, Type = typeof(ListBox))]
    public abstract class UndoRedoButtonBase : Button
    {
        #region Constants

        private const string PartSplitElement = "PART_SplitElement";
        private const string PartPopup = "PART_Popup";
        private const string PartListBox = "PART_ListBox";

        #endregion

        #region Fields

        // Menu items and commands
        private readonly ObservableCollection<object> _menuItems;
        private readonly ICommand _previewItemCommand;
        private readonly ICommand _executeToItemCommand;
        private readonly ICommand _closePopupCommand;
        private readonly ICommand _mainButtonCommand;
        
        // Template parts
        private UIElement _splitElement;
        private Popup _popup;
        private ListBox _listBox;
        
        // State tracking
        private bool _isMouseOverSplitElement;

        #endregion

        #region Constructors

        static UndoRedoButtonBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(UndoRedoButtonBase), 
                new FrameworkPropertyMetadata(typeof(UndoRedoButtonBase)));
        }

        protected UndoRedoButtonBase()
        {
            _menuItems = new ObservableCollection<object>();
            ItemsSource = _menuItems;
            
            _previewItemCommand = new RelayCommand<object>(PreviewItem);
            _executeToItemCommand = new RelayCommand<ChangeSet>(ExecuteToItem);
            _closePopupCommand = new RelayCommand(CloseDropDown);
            _mainButtonCommand = new RelayCommand(ExecuteMainButton, CanExecuteMainButton);
            
            Command = _mainButtonCommand;
        }

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty UndoRootProperty =
            DependencyProperty.Register(
                nameof(UndoRoot),
                typeof(object),
                typeof(UndoRedoButtonBase),
                new PropertyMetadata(null, OnUndoRootChanged));

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(ObservableCollection<object>),
                typeof(UndoRedoButtonBase),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ClosePopupCommandProperty =
            DependencyProperty.Register(
                nameof(ClosePopupCommand),
                typeof(ICommand),
                typeof(UndoRedoButtonBase),
                new PropertyMetadata(null, OnClosePopupCommandChanged));

        public static readonly DependencyProperty ItemContainerStyleProperty =
            DependencyProperty.Register(
                nameof(ItemContainerStyle),
                typeof(Style),
                typeof(UndoRedoButtonBase),
                new PropertyMetadata(null));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(
                nameof(IsDropDownOpen),
                typeof(bool),
                typeof(UndoRedoButtonBase),
                new PropertyMetadata(false, OnIsDropDownOpenChanged));

        /// <summary>
        /// Gets or sets the undo root object that implements ISupportsUndo.
        /// The control will automatically subscribe to stack changes and update the menu.
        /// </summary>
        public object UndoRoot
        {
            get => GetValue(UndoRootProperty);
            set => SetValue(UndoRootProperty, value);
        }

        /// <summary>
        /// Gets the collection of menu items displayed in the dropdown.
        /// This is automatically populated by the control.
        /// </summary>
        public ObservableCollection<object> ItemsSource
        {
            get => (ObservableCollection<object>)GetValue(ItemsSourceProperty);
            private set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the command used to close the popup.
        /// This is typically set by the control template.
        /// </summary>
        public ICommand ClosePopupCommand
        {
            get => (ICommand)GetValue(ClosePopupCommandProperty);
            set => SetValue(ClosePopupCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the style for item containers in the dropdown.
        /// </summary>
        public Style ItemContainerStyle
        {
            get => (Style)GetValue(ItemContainerStyleProperty);
            set => SetValue(ItemContainerStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the dropdown is open.
        /// </summary>
        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        #endregion

        #region Dependency Property Callbacks

        private static void OnUndoRootChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (UndoRedoButtonBase)d;
            button.HandleUndoRootChanged(e.OldValue, e.NewValue);
        }

        private static void OnClosePopupCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (UndoRedoButtonBase)d;
            // ClosePopupCommand is a dependency property, we don't need to assign to the field
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (UndoRedoButtonBase)d;
            button.HandleDropDownOpenChanged((bool)e.NewValue);
        }

        private void HandleUndoRootChanged(object oldValue, object newValue)
        {
            UnsubscribeFromStackEvents(oldValue);
            SubscribeToStackEvents(newValue);
            RefreshMenuItems();
            UpdateEnabledState();
        }

        private void HandleDropDownOpenChanged(bool isOpen)
        {
            if (!isOpen)
            {
                // Dropdown closed - return focus to the button
                Focus();
            }
        }

        #endregion

        #region Abstract Members - Template Method Pattern

        /// <summary>
        /// Gets the stack of ChangeSets (UndoStack or RedoStack).
        /// </summary>
        protected abstract IEnumerable<ChangeSet> GetStack(UndoRoot root);

        /// <summary>
        /// Gets the action verb for this button (e.g., "放弃" or "重做").
        /// </summary>
        protected abstract string GetActionVerb();

        /// <summary>
        /// Gets the cancel text (e.g., "取消").
        /// </summary>
        protected abstract string GetCancelText();

        /// <summary>
        /// Gets the single item action text (e.g., "放弃 1 个命令").
        /// </summary>
        protected abstract string GetSingleActionText();

        /// <summary>
        /// Executes the operation for a single ChangeSet.
        /// </summary>
        protected abstract void ExecuteSingle(UndoRoot root);

        /// <summary>
        /// Executes the operation up to the specified ChangeSet.
        /// </summary>
        protected abstract void ExecuteTo(UndoRoot root, ChangeSet target);

        /// <summary>
        /// Checks if the operation can be executed.
        /// </summary>
        protected abstract bool CanExecute(UndoRoot root);

        /// <summary>
        /// Gets the default command (ApplicationCommands.Undo or Redo).
        /// </summary>
        protected abstract ICommand GetDefaultCommand();

        #endregion

        #region Template Parts Management

        public override void OnApplyTemplate()
        {
            UnsubscribeFromTemplateParts();
            
            base.OnApplyTemplate();

            AttachToTemplateParts();
        }

        private void UnsubscribeFromTemplateParts()
        {
            if (_splitElement != null)
            {
                _splitElement.MouseEnter -= OnSplitElementMouseEnter;
                _splitElement.MouseLeave -= OnSplitElementMouseLeave;
            }

            if (_listBox != null)
            {
                _listBox.SelectionChanged -= OnListBoxSelectionChanged;
                _listBox.PreviewKeyDown -= OnListBoxPreviewKeyDown;
            }

            if (_popup != null)
            {
                _popup.Opened -= OnPopupOpened;
            }
        }

        private void AttachToTemplateParts()
        {
            _splitElement = GetTemplateChild(PartSplitElement) as UIElement;
            _popup = GetTemplateChild(PartPopup) as Popup;
            _listBox = GetTemplateChild(PartListBox) as ListBox;

            if (_splitElement != null)
            {
                _splitElement.MouseEnter += OnSplitElementMouseEnter;
                _splitElement.MouseLeave += OnSplitElementMouseLeave;
            }

            if (_listBox != null)
            {
                _listBox.SelectionChanged += OnListBoxSelectionChanged;
                _listBox.PreviewKeyDown += OnListBoxPreviewKeyDown;
            }

            if (_popup != null)
            {
                _popup.Opened += OnPopupOpened;
            }
        }

        #endregion

        #region Popup Event Handlers

        private void OnPopupOpened(object sender, EventArgs e)
        {
            if (_listBox == null) return;

            _listBox.Focus();
            
            // Select the first UndoListItem if nothing is selected
            if (_listBox.SelectedItem == null && _menuItems.Count > 0)
            {
                var firstItem = _menuItems.FirstOrDefault(item => item is UndoListItem);
                if (firstItem != null)
                {
                    _listBox.SelectedItem = firstItem;
                }
            }
        }

        private void OnSplitElementMouseEnter(object sender, MouseEventArgs e)
        {
            _isMouseOverSplitElement = true;
        }

        private void OnSplitElementMouseLeave(object sender, MouseEventArgs e)
        {
            _isMouseOverSplitElement = false;
        }

        #endregion

        #region ListBox Event Handlers

        private void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] != null)
            {
                PreviewItem(e.AddedItems[0]);
            }
        }

        private void OnListBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    e.Handled = true;
                    if (_listBox.SelectedItem != null)
                    {
                        ExecuteItemCommand(_listBox.SelectedItem);
                    }
                    break;

                case Key.Escape:
                    e.Handled = true;
                    CloseDropDown();
                    break;

                case Key.Down:
                    e.Handled = true;
                    MoveSelection(1);
                    break;

                case Key.Up:
                    e.Handled = true;
                    MoveSelection(-1);
                    break;
            }
        }

        #endregion

        #region Keyboard Navigation

        /// <summary>
        /// Moves the selection in the ListBox by the specified offset.
        /// </summary>
        /// <param name="offset">The number of items to move (positive for down, negative for up)</param>
        private void MoveSelection(int offset)
        {
            if (_listBox == null || _menuItems.Count == 0) return;

            int currentIndex = _listBox.SelectedIndex;
            
            if (currentIndex < 0)
            {
                SelectFirstSelectableItem();
                return;
            }

            int newIndex = FindNextSelectableItem(currentIndex, offset);
            if (newIndex >= 0)
            {
                _listBox.SelectedIndex = newIndex;
                _listBox.ScrollIntoView(_menuItems[newIndex]);
            }
        }

        private void SelectFirstSelectableItem()
        {
            var firstItem = _menuItems.FirstOrDefault(item => item is UndoListItem);
            if (firstItem != null)
            {
                _listBox.SelectedIndex = _menuItems.IndexOf(firstItem);
            }
        }

        private int FindNextSelectableItem(int startIndex, int offset)
        {
            int newIndex = startIndex + offset;

            while (newIndex >= 0 && newIndex < _menuItems.Count)
            {
                // Skip separators
                if (_menuItems[newIndex] is UndoListSeparator)
                {
                    newIndex += offset;
                    continue;
                }

                return newIndex;
            }

            return -1;
        }

        #endregion

        #region Button Click Handling

        protected override void OnClick()
        {
            if (_isMouseOverSplitElement)
            {
                ToggleDropDown();
            }
            else
            {
                ExecuteSingleOperation();
                CloseDropDown();
            }
            
            // Don't call base.OnClick() as we handle everything ourselves
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // When dropdown is open, let ListBox handle Enter and Escape
            if (IsDropDownOpen && (e.Key == Key.Enter || e.Key == Key.Escape))
            {
                e.Handled = false;
                return;
            }
            
            base.OnKeyDown(e);
        }

        #endregion

        #region Command Execution

        /// <summary>
        /// Executes the main button command (single undo/redo).
        /// </summary>
        private void ExecuteMainButton()
        {
            ExecuteSingleOperation();
        }

        /// <summary>
        /// Determines whether the main button command can execute.
        /// </summary>
        private bool CanExecuteMainButton()
        {
            var undoRoot = GetUndoRoot();
            return undoRoot != null && CanExecute(undoRoot);
        }

        private void ExecuteSingleOperation()
        {
            var undoRoot = GetUndoRoot();
            if (undoRoot != null && CanExecute(undoRoot))
            {
                ExecuteSingle(undoRoot);
            }
        }

        private void ExecuteItemCommand(object item)
        {
            if (item == null) return;

            switch (item)
            {
                case UndoListItem undoListItem when undoListItem.ChangeSet != null:
                    ExecuteToItem(undoListItem.ChangeSet);
                    break;

                case UndoListOption option:
                    ExecuteOptionCommand(option);
                    break;
            }
        }

        private void ExecuteOptionCommand(UndoListOption option)
        {
            if (option.IsCancel)
            {
                CloseDropDown();
            }
            else
            {
                ExecuteSingleOperation();
                CloseDropDown();
            }
        }

        private void ExecuteToItem(ChangeSet target)
        {
            var undoRoot = GetUndoRoot();
            if (undoRoot == null) return;

            ExecuteTo(undoRoot, target);
            CloseDropDown();
        }

        #endregion

        #region Stack Event Handling

        private void SubscribeToStackEvents(object root)
        {
            var undoRoot = TryGetUndoRoot(root);
            if (undoRoot == null) return;

            undoRoot.UndoStackChanged += OnStackChanged;
            undoRoot.RedoStackChanged += OnStackChanged;
        }

        private void UnsubscribeFromStackEvents(object root)
        {
            var undoRoot = TryGetUndoRoot(root);
            if (undoRoot == null) return;

            undoRoot.UndoStackChanged -= OnStackChanged;
            undoRoot.RedoStackChanged -= OnStackChanged;
        }

        private void OnStackChanged(object sender, EventArgs e)
        {
            RefreshMenuItems();
            UpdateEnabledState();
        }

        private void UpdateEnabledState()
        {
            if (_mainButtonCommand is RelayCommand relayCommand)
            {
                relayCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Menu Item Management

        /// <summary>
        /// Refreshes the menu items based on the current stack.
        /// </summary>
        protected void RefreshMenuItems()
        {
            _menuItems.Clear();

            var undoRoot = GetUndoRoot();
            if (undoRoot == null) return;

            var stack = GetStack(undoRoot).ToList();
            if (!stack.Any()) return;

            AddStackItems(stack);
            AddSeparator();
            AddActionOption();
        }

        private void AddStackItems(IEnumerable<ChangeSet> stack)
        {
            foreach (var changeSet in stack)
            {
                _menuItems.Add(new UndoListItem(changeSet)
                {
                    PreviewCommand = _previewItemCommand,
                    ClickCommand = _executeToItemCommand
                });
            }
        }

        private void AddSeparator()
        {
            _menuItems.Add(new UndoListSeparator());
        }

        private void AddActionOption()
        {
            _menuItems.Add(new UndoListOption
            {
                Label = GetSingleActionText(),
                Command = GetDefaultCommand(),
                CommandTarget = null,
                PreviewCommand = _previewItemCommand,
                ClosePopupCommand = _closePopupCommand
            });
        }

        #endregion

        #region Preview Logic

        /// <summary>
        /// Handles preview of items when hovering over or selecting them.
        /// Updates highlighting, selection, and the action button text.
        /// </summary>
        private void PreviewItem(object parameter)
        {
            switch (parameter)
            {
                case UndoListItem item:
                    PreviewUndoListItem(item);
                    break;

                case UndoListOption option:
                    PreviewUndoListOption(option);
                    break;
            }
        }

        private void PreviewUndoListItem(UndoListItem item)
        {
            var index = _menuItems.IndexOf(item);
            if (index < 0) return;

            SynchronizeListBoxSelection(item);
            HighlightItemsUpTo(index);
            UpdateActionOptionForItem(item, index);
        }

        private void PreviewUndoListOption(UndoListOption option)
        {
            SynchronizeListBoxSelection(option);
            ClearAllHighlights();
            UpdateActionOptionForCancel(option);
        }

        private void SynchronizeListBoxSelection(object item)
        {
            if (_listBox == null || _listBox.SelectedItem == item) return;

            // Temporarily unsubscribe to avoid recursive calls
            _listBox.SelectionChanged -= OnListBoxSelectionChanged;
            _listBox.SelectedItem = item;
            _listBox.SelectionChanged += OnListBoxSelectionChanged;
        }

        private void HighlightItemsUpTo(int index)
        {
            for (int i = 0; i < _menuItems.Count; i++)
            {
                if (_menuItems[i] is UndoListItem uItem)
                {
                    uItem.IsHighlighted = i <= index;
                }
            }
        }

        private void ClearAllHighlights()
        {
            foreach (var item in _menuItems.OfType<UndoListItem>())
            {
                item.IsHighlighted = false;
            }
        }

        private void UpdateActionOptionForItem(UndoListItem item, int index)
        {
            if (_menuItems.LastOrDefault() is UndoListOption option)
            {
                option.Label = $"{GetActionVerb()} {index + 1} 个命令";
                option.IsCancel = false;
                option.Command = _executeToItemCommand;
                option.CommandParameter = item.ChangeSet;
            }
        }

        private void UpdateActionOptionForCancel(UndoListOption option)
        {
            option.Label = GetCancelText();
            option.IsCancel = true;
            option.Command = option.ClosePopupCommand;
            option.CommandParameter = null;
        }

        #endregion

        #region Helper Methods

        private UndoRoot GetUndoRoot()
        {
            return TryGetUndoRoot(UndoRoot);
        }

        private static UndoRoot TryGetUndoRoot(object root)
        {
            return root != null ? UndoService.Current[root] : null;
        }

        private void ToggleDropDown()
        {
            IsDropDownOpen = !IsDropDownOpen;
        }

        private void CloseDropDown()
        {
            IsDropDownOpen = false;
        }

        #endregion
    }
}
