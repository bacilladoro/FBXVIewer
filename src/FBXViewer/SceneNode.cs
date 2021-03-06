using System;
using System.Collections.Generic;
using Assimp;
using ReactiveUI;

namespace FBXViewer
{
    public class SceneNode : BaseNode
    {
        private readonly Node _node;
        private SceneNode? Parent { get; set; }
        private readonly Func<Node, SceneNode> _sceneNodeFactory;
        private readonly SceneContext _context;
        private readonly Func<Mesh, MeshNode> _meshNodeFactory;
        private bool _shouldShow;

        public SceneNode(Node node, Func<Node, SceneNode> sceneNodeFactory, SceneContext context,
            Func<Mesh, MeshNode> meshNodeFactory)
        {
            _node = node;
            _sceneNodeFactory = sceneNodeFactory;
            _context = context;
            _meshNodeFactory = meshNodeFactory;
        }

        public Matrix4x4 Transform => (Parent?.Transform ?? Matrix4x4.Identity) * _node.Transform;
        
        public override bool IsChecked 
        {
            get => _shouldShow;
            set
            {
                this.RaiseAndSetIfChanged(ref _shouldShow, value);
                foreach (var child in GetChildren())
                {
                    child.IsChecked = value;
                }
            }
        }

        public override bool SupportsMultiSelect => _node.HasMeshes;

        public override string? Text => _node.Name;
        public override bool HasChildren => true;
        protected override IEnumerable<INode> CreateChildren()
        {
            foreach (var child in _node.Children)
            {
                var sceneNode = _sceneNodeFactory(child);
                sceneNode.Parent = this;
                yield return sceneNode;
            }

            foreach (var meshIndex in _node.MeshIndices)
            {
                var mesh = _context.GetMeshByIndex(meshIndex);
                if (mesh != null)
                {
                    var meshNode = _meshNodeFactory(mesh);
                    meshNode.IsSubMesh = _node.MeshCount > 1;
                    meshNode.SceneParent = this;
                    yield return meshNode;
                }
            }

            foreach (var property in _node.PrimitiveProperties())
            {
                yield return property;
            }
            
        }
    }
}