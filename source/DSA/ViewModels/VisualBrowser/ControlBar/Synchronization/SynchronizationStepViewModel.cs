using Windows.UI;
using Windows.UI.Xaml.Media;
using GalaSoft.MvvmLight;

namespace DSA.Shell.ViewModels.VisualBrowser.ControlBar.Synchronization
{

    public class SynchronizationStepViewModel : ViewModelBase
    {
        private StepState _state;
        private Brush _leftLineBrush;
        private Brush _rightLineBrush;
        private readonly Brush _inactiveBrush = new SolidColorBrush(Colors.LightGray);
        private readonly Brush _activeBrush = new SolidColorBrush(Colors.DeepSkyBlue);
        private bool _isWaiting;
        private bool _isProgress;
        private bool _isDone;
        private string _name;

        public SynchronizationStepViewModel(string stepName)
        {
            Name = stepName;
            State = StepState.Waiting;
        }

        public StepState State
        {
            get { return _state; }
            set
            {
                Set(ref _state, value);
                ResolveState();
                ResolveLineBrush();
            }
        }

        private void ResolveState()
        {
            IsWaiting = _state == StepState.Waiting;
            IsProgress = _state == StepState.Progress;
            IsDone = _state == StepState.Done;
        }

        private void ResolveLineBrush()
        {
           switch(_state)
            {
                case StepState.Waiting:
                    LeftLineBrush = _inactiveBrush;
                    RightLineBrush = _inactiveBrush;
                    break;
                case StepState.Progress:
                    LeftLineBrush = _activeBrush;
                    RightLineBrush = _inactiveBrush;
                    break;
                case StepState.Done:
                    LeftLineBrush = _activeBrush;
                    RightLineBrush = _activeBrush;
                    break;
            }
        }

        public Brush LeftLineBrush
        {
            get { return _leftLineBrush; }
            set { Set(ref _leftLineBrush, value); }
        }

        public Brush RightLineBrush
        {
            get { return _rightLineBrush; }
            set { Set(ref _rightLineBrush, value); }
        }

        public bool IsWaiting
        {
            get { return _isWaiting; }
            private set { Set(ref _isWaiting, value); }
        }

        public bool IsProgress
        {
            get { return _isProgress; }
            private set { Set(ref _isProgress, value); }
        }

        public bool IsDone
        {
            get { return _isDone; }
            private set { Set(ref _isDone, value); }
        }

        public string Name
        {
            get { return _name; }
            private set { Set(ref _name, value); }
        }
    }

    public enum StepState
    {
        Waiting,
        Progress,
        Done
    }
}