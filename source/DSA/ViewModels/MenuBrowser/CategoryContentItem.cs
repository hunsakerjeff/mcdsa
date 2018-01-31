using GalaSoft.MvvmLight;

namespace DSA.Shell.ViewModels.MenuBrowser
{
    public abstract class CategoryContentItem : ViewModelBase
    {
        private bool _isInternalMode;

        protected CategoryContentItem(
            string id, 
            string name,
            bool isInternalMode)
        {
            _isInternalMode = isInternalMode;
            ID = id;
            Name = name;
        }

        public virtual bool IsInternalMode
        {
            get { return _isInternalMode; }
            set { _isInternalMode = value; }
        }

        public string ID
        {
            private set;
            get;
        }

        public string Name
        {
            private set;
            get;
        }

        public virtual bool IsVisible
        {
            get;
        }
    }
}