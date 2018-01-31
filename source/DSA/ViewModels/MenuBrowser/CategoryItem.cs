using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using WinRTXamlToolkit.Tools;

namespace DSA.Shell.ViewModels.MenuBrowser
{
    public class CategoryItem : CategoryContentItem
    {
        private readonly List<CategoryContentItem> _allContent;
        private CategoryItem _selectedCategory;
        private RelayCommand<object> _selectionChangedCommand;
        private readonly Action<CategoryItem> _selectedCategoryAction;

        public CategoryItem(
            int level,
            string id,
            string name,
            bool isInternalMode,
            Action<CategoryItem> selectedCategoryAction,
            List<CategoryContentItem> content = null) : base(id, name, isInternalMode)
        {
            Level = level;
            _allContent = content;
            CategoryContent = new ObservableCollection<CategoryContentItem>(content);
            IsInternalMode = isInternalMode;
            _selectedCategoryAction = selectedCategoryAction;
        }

        public override bool IsInternalMode
        {
            get
            {
                return base.IsInternalMode;
            }
            set
            {
                base.IsInternalMode = value;
                _allContent.ForEach(c => c.IsInternalMode = value);
                CategoryContent = new ObservableCollection<CategoryContentItem>(_allContent.Where(ci => ci.IsVisible));
            }
        }

        public int Level
        {
            get;
            private set;
        }

        public string Header
        {
            get
            {
                return Name;
            }
        }

        public ObservableCollection<CategoryContentItem> CategoryContent
        {
            get;
            private set;
        }

        public CategoryItem SelectedCategory
        {
            get { return _selectedCategory; }
            set { Set(ref _selectedCategory, value); }
        }

        public RelayCommand<object> SelectionChangedCommand
        {
            get
            {
                return _selectionChangedCommand ??
                    (_selectionChangedCommand = new RelayCommand<object>(
                        (contentItem) =>
                        {
                            if(contentItem == null)
                            {
                                return;
                            }

                            if(contentItem is MediaItem)
                            {
                                var mediaItem = contentItem as MediaItem;
                                mediaItem.NavigateToMediaCommand.Execute(mediaItem);
                                SelectedCategory = null;
                                return;
                            }

                            if(contentItem is CategoryItem)
                            {
                                _selectedCategoryAction(contentItem as CategoryItem);
                                return;
                            }
                        }));

            }
        }

        public override bool IsVisible
        {
            get
            {
                return Level == 1 || CategoryContent.Any(c => c.IsVisible);
            }
        }
    }
}
