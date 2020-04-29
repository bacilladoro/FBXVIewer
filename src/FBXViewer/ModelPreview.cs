using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Assimp;
using MVector3D = System.Windows.Media.Media3D.Vector3D;
using Vector = System.Windows.Vector;

namespace FBXViewer
{
    public class ModelPreview
    {
        private readonly Camera _camera;
        private IDragHandler _dragHandler;
        private Viewport3D _viewPort;
        private PerspectiveCamera _perspectiveCamera;

        public UIElement Element { get; }
        
        private readonly Dictionary<Mesh, ModelVisual3D> _meshes = new Dictionary<Mesh,ModelVisual3D>();

        public ModelPreview()
        {
            _viewPort = new Viewport3D();

            var center = Vector3.Zero;
            

            _perspectiveCamera = new PerspectiveCamera(
                center.AsPoint3D(),
                new Vector3(0, 0, 1).AsMVector3D(), 
                new MVector3D(0, 1, 0), 45);
            
            var lightGroup = new Model3DGroup();
            var light = new PointLight(Colors.Cornsilk, _perspectiveCamera.Position){};
            lightGroup.Children.Add(light);
            _viewPort.Children.Add(new ModelVisual3D{Content = lightGroup});
            _camera = new Camera(_perspectiveCamera, center, light);
            
            _viewPort.Camera = _perspectiveCamera;

            var border = new Border {Background = Brushes.Black};
            border.AddHandler(UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(MouseWheel), true);
            border.AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(MouseMove), true);
            border.AddHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(MouseDown), true);
            border.AddHandler(UIElement.PreviewMouseUpEvent, new MouseButtonEventHandler(MouseUp), true);
            border.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(KeyDown), true);
            border.Child = _viewPort;

            Element = border;
        }

        public void LoadMesh(Mesh mesh)
        {
            UnloadMesh(mesh);
            
            var geometry = new MeshGeometry3D
            {
                Positions = new Point3DCollection(
                    mesh.Vertices.Select(v => new Point3D(v.X, v.Y, v.Z))),
                Normals = new Vector3DCollection(
                    mesh.Normals.Select(n => new MVector3D(n.X, n.Y, n.Z))),
                TriangleIndices = new Int32Collection()
            };
            foreach (var face in mesh.Faces)
            {
                geometry.TriangleIndices.Add(face.Indices[0]);
                geometry.TriangleIndices.Add(face.Indices[1]);
                geometry.TriangleIndices.Add(face.Indices[2]);
                if (face.IndexCount == 4)
                {
                    geometry.TriangleIndices.Add(face.Indices[0]);
                    geometry.TriangleIndices.Add(face.Indices[2]);
                    geometry.TriangleIndices.Add(face.Indices[3]);
                }
                if (face.IndexCount > 4)
                {
                    Debug.WriteLine($"Found {face.IndexCount}gon, only generating quad");
                }
            }

            var geometryModel = new GeometryModel3D
            {
                Material = new MaterialGroup
                {
                    Children = new MaterialCollection
                    {
                        new DiffuseMaterial(Brushes.Pink),
                        // new SpecularMaterial(Brushes.Red, 1)
                    }
                },
                Geometry = geometry,
            };


            var group = new Model3DGroup();
            group.Children.Add(geometryModel);

            var modelVisual = new ModelVisual3D {Content = @group}; 
            _viewPort.Children.Add(modelVisual);
            _meshes[mesh] = modelVisual;
            
            var center = geometry.Bounds.Location.AsVector3() + (geometry.Bounds.Size.AsVector3() / 2);
            var biggestExtent = new[] {geometry.Bounds.SizeX, geometry.Bounds.SizeY, geometry.Bounds.SizeZ}
                .OrderByDescending(s => s).First();
            var cameraOffset = biggestExtent * 2f;
            var cameraPosition = center + new Vector3(0, 0, (float)cameraOffset);
            var lookDir = Vector3.Normalize(center - cameraPosition);
            
            _camera.MoveTo(cameraPosition, lookDir, center);
        }

        public void UnloadMesh(Mesh mesh)
        {
            if (_meshes.TryGetValue(mesh, out var modelVisual3D))
            {
                _viewPort.Children.Remove(modelVisual3D);
                _meshes.Remove(mesh);
            }
        }

        private void KeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine($"Key down {e.Key}");
            if (e.Key == Key.Decimal)
            {
                _camera.Reset();
            }
        }

        private void MouseUp(object sender, MouseEventArgs e)
        {
            _dragHandler = null;
            Element.ReleaseMouseCapture();
        }

        private void MouseDown(object sender, MouseEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift))
            {
                _dragHandler = new PanDragHandler(this, e);
                Element.CaptureMouse();
            }
            else if (e.MiddleButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                _dragHandler = new DollyHandler(this, e);
                Element.CaptureMouse();
            }
            else if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _dragHandler = new OrbitHandler(this, e);
                Element.CaptureMouse();
            }

        }

        private void MouseMove(object sender, MouseEventArgs e)
        {
            _dragHandler?.MouseDrag(e);
        }

        private void MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var delta = e.Delta * 0.25f;
            _camera.Zoom(delta);
        }

        private interface IDragHandler
        {
            void MouseDrag(MouseEventArgs args);
        }

        private abstract class DragHandlerBase : IDragHandler
        {
            protected readonly ModelPreview Outer;
            private Point _pos;
            private DateTime _time;

            protected DragHandlerBase(ModelPreview outer, MouseEventArgs args)
            {
                _pos = args.GetPosition(outer.Element);
                Outer = outer;
                _time = DateTime.Now;
            }

            public void MouseDrag(MouseEventArgs args)
            {
                var timeNow = DateTime.Now;
                var newPos = args.GetPosition(Outer.Element);
                var deltaTime = (timeNow - _time).TotalSeconds;
                var delta = (newPos - _pos) * deltaTime;

                _time = timeNow;
                _pos = newPos;

                DoMouseDrag(delta);

            }

            protected abstract void DoMouseDrag(Vector delta);
        }

        private class PanDragHandler : DragHandlerBase
        {
            public PanDragHandler(ModelPreview outer, MouseEventArgs mouseEventArgs) : base(outer, mouseEventArgs)
            {
            }
            protected override void DoMouseDrag(Vector delta)
            {
                delta *= 50;
                Outer._camera.Pan((float) delta.X, (float) delta.Y);
            }
        }

        private class OrbitHandler : DragHandlerBase
        {
            public OrbitHandler(ModelPreview outer, MouseEventArgs args) : base(outer, args)
            {
            }

            protected override void DoMouseDrag(Vector delta)
            {
                Outer._camera.Orbit(delta);
            }
        }

        private class DollyHandler : DragHandlerBase
        {
            public DollyHandler(ModelPreview outer, MouseEventArgs args) : base(outer, args)
            {
            }

            protected override void DoMouseDrag(Vector delta)
            {
                Outer._camera.Dolly(delta.Y * -15);
            }
        }
    }
}