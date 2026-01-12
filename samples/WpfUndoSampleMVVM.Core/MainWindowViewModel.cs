using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MonitoredUndo;
using MonitoredUndo.WpfIntegration;
using WpfUndoControls.Abstractions;

namespace WpfUndoSampleMVVM.Core
{
    /// <summary>
    /// The view model for the main window, providing undo/redo functionality and other bound properties.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase, ISupportsUndo
    {
        private CommandBindingCollection _commandBindings = new CommandBindingCollection();

        private ICommand _sliderMouseDownCommand;
        private ICommand _sliderLostMouseCapture;

        /// <summary>
        /// Gets the collection of command bindings for the view.
        /// </summary>
        public CommandBindingCollection RegisterCommandBindings
        {
            get
            {
                return _commandBindings;
            }
        }
        public IUndoManager UndoManager { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel()
        {
            InitialiseCommandBindings();

            UndoManager = MonitoredUndoManagerProvider.Instance.GetUndoManager(this);
        }

        /// <summary>
        /// Gets the command that handles the slider mouse down event.
        /// Used to start a batch of changes.
        /// </summary>
        public ICommand SliderMouseDownCommand
        {
            get
            {
                return _sliderMouseDownCommand ?? (_sliderMouseDownCommand = new RelayCommand<MouseButtonEventArgs>(OnSliderMouseDown));
            }
        }

        /// <summary>
        /// Gets the command that handles the slider lost mouse capture event.
        /// Used to end a batch of changes.
        /// </summary>
        public ICommand SliderLostMouseCapture
        {
            get
            {
                return _sliderLostMouseCapture ?? (_sliderLostMouseCapture = new RelayCommand<MouseEventArgs>(OnSliderLostMouseCapture));
            }
        }

        /// <summary>
        /// Handles the MouseUp or LostMouseCapture event for the slider.
        /// Ends the current batch of changes so they are treated as a single undoable unit.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        private void OnSliderLostMouseCapture(MouseEventArgs e)
        {
            if (!BatchAgeChanges)
                return;

            UndoService.Current[this].EndChangeSetBatch();

            e.Handled = false;
        }

        /// <summary>
        /// Handles the MouseDown event for the slider.
        /// Starts a new batch of changes to group continuous updates (like dragging a slider) into a single undo unit.
        /// </summary>
        /// <param name="e">The mouse button event arguments.</param>
        private void OnSliderMouseDown(MouseButtonEventArgs e)
        {
            if (!BatchAgeChanges)
                return;

            // Start a batch to collect all subsequent undo events (for this root)
            // into a single changeset.
            // 
            // Passing "false" for the last parameter tells the system to keep
            // each individual change that is made. If desired, pass "true" to
            // de-dupe these changes and reduce the memory requirements of the
            // changeset.
            UndoService.Current[this].BeginChangeSetBatch("Age Changed", false);

            e.Handled = false;
        }

        // Below are properties bound to the UI with a XAML binding.
        // NOTE that these properly implement INotifyPropertyChange.
        //  This is critical if the UI is going to stay in sync
        //  with the changes to the data.

        private string _FirstName;
        /// <summary>
        /// Gets or sets the first name.
        /// Changes to this property are recorded in the undo system.
        /// </summary>
        public string FirstName
        {
            get { return _FirstName; }
            set
            {
                if (value == _FirstName)
                    return;

                // Store this change in the Undo system.
                // This uses the "DefaultChangeFactory" to construct the change, but you can 
                // store changes any way you like.
                DefaultChangeFactory.Current.OnChanging(this, nameof(FirstName), _FirstName, value, "First Name Changed");

                _FirstName = value;
                RaisePropertyChanged(nameof(FirstName)); // Tells the UI that this property has changed.
                RaisePropertyChanged(nameof(FullName));  // If FirstName changes, then FullName is also affected.
            }
        }

        private string _LastName;
        /// <summary>
        /// Gets or sets the last name.
        /// Changes to this property are recorded in the undo system.
        /// </summary>
        public string LastName
        {
            get { return _LastName; }
            set
            {
                if (value == _LastName)
                    return;

                // Store this change in the Undo system.
                // This uses the "DefaultChangeFactory" to construct the change, but you can 
                // store changes any way you like.
                DefaultChangeFactory.Current.OnChanging(this, nameof(LastName), _LastName, value, "Last Name Changed");

                _LastName = value;
                RaisePropertyChanged(nameof(LastName));  // Tells the UI that this property changed.
                RaisePropertyChanged(nameof(FullName));  // If LastName changes, then FullName is also affected.
            }
        }

        /// <summary>
        /// Gets the full name, which is a combination of First Name and Last Name.
        /// </summary>
        public string FullName
        {
            get
            {
                return String.Format("{0} {1}", FirstName, LastName);
            }
        }

        private int _Age;
        /// <summary>
        /// Gets or sets the age.
        /// Changes to this property are recorded in the undo system.
        /// </summary>
        public int Age
        {
            get { return _Age; }
            set
            {
                if (value == _Age)
                    return;

                // Store this change in the Undo system.
                // This uses the "DefaultChangeFactory" to construct the change, but you can 
                // store changes any way you like.
                DefaultChangeFactory.Current.OnChanging(this, nameof(Age), _Age, value, "Age Changed");

                _Age = value;
                RaisePropertyChanged(nameof(Age));
            }
        }

        private bool _BatchAgeChanges = true;
        /// <summary>
        /// Gets or sets a value indicating whether age changes should be batched.
        /// If true, continuous changes (like dragging a slider) will be grouped into a single undo step.
        /// </summary>
        public bool BatchAgeChanges
        {
            get { return _BatchAgeChanges; }
            set
            {
                if (value == _BatchAgeChanges)
                    return;

                _BatchAgeChanges = value;
                RaisePropertyChanged(nameof(BatchAgeChanges));
            }
        }

        /// <summary>
        /// Initializes the command bindings for Undo and Redo operations.
        /// Registers these bindings with the CommandManager for this class.
        /// </summary>
        private void InitialiseCommandBindings()
        {
            // create command binding for undo command
            var undoBinding = new CommandBinding(ApplicationCommands.Undo, UndoExecuted, UndoCanExecute);
            var redoBinding = new CommandBinding(ApplicationCommands.Redo, RedoExecuted, RedoCanExecute);

            // register the binding to the class
            CommandManager.RegisterClassCommandBinding(typeof(MainWindowViewModel), undoBinding);
            CommandManager.RegisterClassCommandBinding(typeof(MainWindowViewModel), redoBinding);

            CommandBindings.Add(undoBinding);
            CommandBindings.Add(redoBinding);
        }

        /// <summary>
        /// Executed when the Redo command is invoked.
        /// Calls the UndoService to perform the redo operation.
        /// </summary>
        /// <param name="sender">The command sender.</param>
        /// <param name="e">The execution event arguments.</param>
        private void RedoExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            // A shorthand version of the above call to Undo, except 
            // that this calls Redo.
            UndoService.Current[this].Redo();
        }

        /// <summary>
        /// Determines whether the Redo command can currently execute.
        /// Checks the UndoService to see if a redo is possible.
        /// </summary>
        /// <param name="sender">The command sender.</param>
        /// <param name="e">The can-execute event arguments.</param>
        private void RedoCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            // Tell the UI whether Redo is available.
            e.CanExecute = UndoService.Current[this].CanRedo;
        }

        /// <summary>
        /// Executed when the Undo command is invoked.
        /// Calls the UndoService to perform the undo operation.
        /// </summary>
        /// <param name="sender">The command sender.</param>
        /// <param name="e">The execution event arguments.</param>
        private void UndoExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            // Get the document root. In this case, we pass in "this", which 
            // implements ISupportsUndo. The ISupportsUndo interface is used
            // by the UndoService to locate the appropriate root node of an 
            // undoable document.
            // In this case, we are treating the window as the root of the undoable
            // document, but in a larger system the root would probably be your
            // domain model.
            var undoRoot = UndoService.Current[this];
            undoRoot.Undo();
        }

        /// <summary>
        /// Determines whether the Undo command can currently execute.
        /// Checks the UndoService to see if an undo is possible.
        /// </summary>
        /// <param name="sender">The command sender.</param>
        /// <param name="e">The can-execute event arguments.</param>
        private void UndoCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            // Tell the UI whether Undo is available.
            e.CanExecute = UndoService.Current[this].CanUndo;
        }

        /// <summary>
        /// Gets the collection of command bindings.
        /// </summary>
        public CommandBindingCollection CommandBindings
        {
            get
            {
                return _commandBindings;
            }
        }

        /// <summary>
        /// Implementation of ISupportsUndo.
        /// Returns the root object of the undo document hierarchy.
        /// </summary>
        /// <returns>The current instance as the root.</returns>
        public object GetUndoRoot()
        {
            return this;
        }
    }
}
