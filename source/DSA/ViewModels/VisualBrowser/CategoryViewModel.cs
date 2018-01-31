using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace DSA.Shell.ViewModels.VisualBrowser
{
    /// <summary>
    /// Category ViewModel
    /// - clean
    /// </summary>
    public class CategoryViewModel : ViewModelBase
    {
        #region Fields and Properties

        private const int ExpandedWidth = 200;
        private const int FoldedWith = 40;

        private bool _isFolded;
        private int _width;

        private RelayCommand<object> _selectionChangedCommand;
        private RelayCommand _selectionCommand;
        private readonly List<CategoryViewModel> _allChildren;
        private string _selectedSubCategoryName;

        public int Level { get; private set; }

        public Action<CategoryViewModel> SelectAction { get; private set; }

        public string Name => Content.Name;

        public CategoryContentViewModel Content { get; private set; }

        public List<CategoryViewModel> AllChildren
        {
            get { return _allChildren; }
        }

        public List<CategoryViewModel> Children
        {
            get { return _allChildren.Where(c => c.IsVisible).ToList(); }
        }

        public bool HasChildren => Children.Any();

        public RelayCommand<object> SelectionChangedCommand
        {
            get
            {
                return _selectionChangedCommand ?? (
                    _selectionChangedCommand = new RelayCommand<object>(
                    (obj) =>
                    {
                        var categoryVm = obj as CategoryViewModel;
                        if (categoryVm != null)
                        {
                            SelectAction(categoryVm);
                            if (categoryVm.HasChildren)
                            {
                                IsFolded = true;
                            }
                        }

                    }));
            }
        }

        public RelayCommand SelectionCommand
        {
            get
            {
                return _selectionCommand ?? (
                    _selectionCommand = new RelayCommand(
                    () =>
                    {
                        SelectAction(this);
                        IsFolded = false;
                    }));
            }
        }

        public int Width
        {
            get { return _width; }
            set { Set(ref _width, value); }
        }

        public bool IsFolded
        {
            get { return _isFolded; }
            set
            {
                Set(ref _isFolded, value);
                Width = value ? FoldedWith : ExpandedWidth;
            }
        }

        public Brush NavigationAreaBackground => Content.NavigationAreaBackground;

        public Color NavigationAreaBackgroundColor => Content.NavigationAreaBackgroundColor;

        public bool IsVisible
        {
            get
            {
                return Content.Media.Any() || Children.Any(c => c.IsVisible);
            }
        }

        public string SelectedSubCategoryName
        {
            get { return _selectedSubCategoryName; }
            set
            {
                Set(ref _selectedSubCategoryName, value);
            }
        }

        #endregion

        #region Constructor

        public CategoryViewModel(
           int level,
           CategoryContentViewModel content,
           List<CategoryViewModel> children,
           Action<CategoryViewModel> selectAction)
        {
            Level = level;
            Content = content;
            _allChildren = children;
            SelectAction = selectAction;
            Width = ExpandedWidth;
        } 

        #endregion
    }
}