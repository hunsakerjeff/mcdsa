using System;
using Windows.UI.Xaml.Media;
using DSA.Model.Dto;
using DSA.Model.Enums;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace DSA.Shell.ViewModels.VisualBrowser
{
    public class MainButtonViewModel : ViewModelBase
    {
        private readonly ButtonDTO _button;
        private readonly ButtonConfigurationDTO _buttonConfiguration;
        private readonly ImageSource _mainImage;
        private readonly ImageSource _selectedImage;
        private ImageSource _currentImage;
        private Brush _currentTextColor;
        private readonly Action<string> _showCategoryAction;
        private decimal _currentPositionX;
        private decimal _currentPositionY;

        private RelayCommand _selectCategoryCommand;
        private RelayCommand _buttonHoldingCommand;
        private RelayCommand _buttonStopHoldingCommand;

        public MainButtonViewModel(ButtonDTO button, ButtonConfigurationDTO buttonConfiguration, ImageSource mainImage, ImageSource selectedImage, Action<string> showCategoryAction)
        {
            _button = button;
            _buttonConfiguration = buttonConfiguration;
            _mainImage = mainImage;
            _selectedImage = selectedImage;
            CurrentImage = _mainImage;
            CurrentTextColor = TextColor;
            _showCategoryAction = showCategoryAction;
        }

        public RelayCommand SelectCategoryCommand
        {
            get
            {
                return _selectCategoryCommand ?? ( _selectCategoryCommand = new RelayCommand(
                    () =>
                    {
                        CurrentImage = _selectedImage ?? _mainImage;
                        CurrentTextColor = HighlightTextColor;
                        _showCategoryAction(Category);
                    }));
            }
        }

        public RelayCommand ButtonHoldingCommand
        {
            get
            {
                return _buttonHoldingCommand ?? (_buttonHoldingCommand = new RelayCommand(
                    () =>
                    {
                        CurrentImage = _selectedImage ?? _mainImage;
                        CurrentTextColor = HighlightTextColor;
                    }));
            }
        }

        public RelayCommand ButtonStopHoldingCommand
        {
            get
            {
                return _buttonStopHoldingCommand ?? (_buttonStopHoldingCommand = new RelayCommand(
                    () =>
                    {
                        Deselect();
                    }));
            }
        }
        

        public void Deselect()
        {
            CurrentImage = _mainImage;
            CurrentTextColor = TextColor;
        }

        public decimal CurrentPositionX
        {
            get { return _currentPositionX; }
            set
            {
                Set(ref _currentPositionX, value);
                RaisePropertyChanged(nameof(CurrentPositionX));
            }
        }

        public decimal CurrentPositionY
        {
            get { return _currentPositionY; }
            set
            {
                Set(ref _currentPositionY, value);
                RaisePropertyChanged(nameof(CurrentPositionY));
            }
        }

        public ImageSource CurrentImage
        {
            get { return _currentImage; }
            set
            {
                Set(ref _currentImage, value);
                RaisePropertyChanged(nameof(CurrentImage));
            }
        }

        internal void HandleOrientation(PageOrientations orientation)
        {
            switch (orientation)
            {
                case PageOrientations.Landscape:
                case PageOrientations.LandscapeFlipped:
                    CurrentPositionX = _button.PositionXLandscape;
                    CurrentPositionY = _button.PositionYLandscape;
                    break;
                case PageOrientations.Portrait:
                case PageOrientations.PortraitFlipped:
                default:
                    CurrentPositionX = _button.PositionXPortrait;
                    CurrentPositionY = _button.PositionYPortrait;
                    break;
            }
            CurrentImage = _mainImage;
        }

        public Brush CurrentTextColor
        {
            get { return _currentTextColor; }
            set { Set(ref _currentTextColor, value); }
        }

        public string Category => _button.Category;

        public double Opacity => _buttonConfiguration.Opacity;

        private Brush TextColor => new SolidColorBrush(_buttonConfiguration.TextColor);

        private Brush HighlightTextColor => new SolidColorBrush(_buttonConfiguration.HighlightTextColor);
    }
}