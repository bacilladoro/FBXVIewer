using System;
using System.Collections.Generic;
using System.Linq;
using ReactiveUI;

namespace FBXViewer
{
    public class TreeNodeViewModel
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly INode _node;
        private readonly Func<INode, TreeNodeViewModel> _nodeFactory;
        private readonly List<TreeNodeViewModel> _children = new List<TreeNodeViewModel>();

        private static readonly TreeNodeViewModel Dummy = new TreeNodeViewModel();
        
        public TreeNodeViewModel(MainWindowViewModel mainWindowViewModel, INode node, Func<INode, TreeNodeViewModel> nodeFactory)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _node = node;
            _nodeFactory = nodeFactory;
            if (node.HasChildren)
            {
                _children.Add(Dummy);
            }
        }

        private TreeNodeViewModel()
        {
            
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                _node.IsSelected = value;
                if (value)
                {
                    _mainWindowViewModel.Selected = this;
                }
            }
        }
        public object Preview => _node.GetPreview();
        public string Text => _node.Text;
        public object PreviewThumbnail => _node.GetPreviewThumbnail();

        public bool IsMultiSelect => _node.SupportsMultiSelect;

        public List<TreeNodeViewModel> Children
        {
            get
            {
                if (_children.Any() && _children[0] == Dummy)
                {
                    _children.Clear();
                    _children.AddRange(_node.GetChildren().Select(_nodeFactory));
                }
                return _children;
            }
        }

        public bool IsChecked
        {
            get => _node.ShouldShow;
            set => _node.ShouldShow = value;
        }
    }
}