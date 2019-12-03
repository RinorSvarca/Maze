using System.Collections.Generic;
using System.Numerics;
using Maze.ExtensionMethods;
using Zenseless.Geometry;
using Zenseless.HLGL;
using Maze.Shared;
using Maze.View.Rendering.Instances;
using Maze.View.Rendering.Management;

namespace Maze.View
{
    public class MainView
    {
        private readonly IRenderState _renderState;

        private readonly Dictionary<Enums.EntityType, ITexture2D> _textures = new Dictionary<Enums.EntityType, ITexture2D>();
        private readonly Dictionary<Enums.EntityType, ITexture2D> _normalMaps = new Dictionary<Enums.EntityType, ITexture2D>();
        //private readonly Dictionary<Enums.EntityType, ITexture2D> _heightMaps = new Dictionary<Enums.EntityType, ITexture2D>();
        private readonly List<Enums.EntityType> _disableBackFaceCulling = new List<Enums.EntityType>();
        private readonly Dictionary<Enums.EntityType, DefaultMesh> _meshes = new Dictionary<Enums.EntityType, DefaultMesh>();
        private readonly Dictionary<Enums.EntityType, int> _instanceCounts = new Dictionary<Enums.EntityType, int>();
        private readonly Dictionary<Enums.EntityType, List<Matrix4x4>> _transforms = new Dictionary<Enums.EntityType, List<Matrix4x4>>();

        private readonly RenderInstanceGroup _renderInstanceGroup = new RenderInstanceGroup();
        private readonly Deferred _deferred;
        private readonly DirectionalShadowMapping _directShadowMap;
        private readonly SSAOWithBlur _ssaoWithBlur;
        private readonly EnvironmentMap _environmentMap;
        private readonly Add _addEnvMap;
        private readonly Lighting _lighting;
        private readonly SphereCut _sphereCut;
        private readonly Skybox _skybox;
        private readonly Add _addSkybox;
        private readonly Bloom _bloom;

        private readonly List<LightSource> _lights = new List<LightSource>();

        public bool Bloom { get; set; }

        public MainView(IRenderState renderState, IContentLoader contentLoader)
        {
            _renderState = renderState;
            _renderState.Set(new BackFaceCulling(true));

           var sphere = contentLoader.Load<DefaultMesh>("maze.obj");
            sphere = sphere.SwitchTriangleMeshWinding();

            _meshes.Add(Enums.EntityType.Maze, sphere);
            _meshes.Add(Enums.EntityType.Plane, new TBNMesh(Meshes.CreatePlane(1, 1, 1, 1)));           
            _meshes.Add(Enums.EntityType.Mercury, contentLoader.Load<DefaultMesh>("icosphere.obj"));
            _meshes.Add(Enums.EntityType.Mars, contentLoader.Load<DefaultMesh>("icosphere.obj"));
            


            _disableBackFaceCulling.Add(Enums.EntityType.Mercury);
            _disableBackFaceCulling.Add(Enums.EntityType.Mars);

            _textures.Add(Enums.EntityType.Mercury, contentLoader.Load<ITexture2D>("mercury8.jpg"));
            _textures.Add(Enums.EntityType.Mars, contentLoader.Load<ITexture2D>("mars8.jpg"));
            
            _textures.Add(Enums.EntityType.Plane, contentLoader.Load<ITexture2D>("haumea.jpg"));


            _deferred = _renderInstanceGroup.AddShader<Deferred>(new Deferred(contentLoader, _meshes));
            _directShadowMap = _renderInstanceGroup.AddShader<DirectionalShadowMapping>(new DirectionalShadowMapping(contentLoader, _meshes));
            _ssaoWithBlur = _renderInstanceGroup.AddShader<SSAOWithBlur>(new SSAOWithBlur(contentLoader, 15));
            _environmentMap = _renderInstanceGroup.AddShader<EnvironmentMap>(new EnvironmentMap(1024, contentLoader, _meshes));
            _addEnvMap = _renderInstanceGroup.AddShader<Add>(new Add(contentLoader));
            _lighting = _renderInstanceGroup.AddShader<Lighting>(new Lighting(contentLoader));
            _sphereCut = _renderInstanceGroup.AddShader<SphereCut>(new SphereCut(contentLoader, 100));
            
            _skybox = _renderInstanceGroup.AddShader<Skybox>(new Skybox(contentLoader, 100, "space"));
            _addSkybox = _renderInstanceGroup.AddShader<Add>(new Add(contentLoader));
            _bloom = _renderInstanceGroup.AddShader<Bloom>(new Bloom(contentLoader));

            _lights.Add(new LightSource(Vector3.Zero, Vector3.Normalize(new Vector3(-1f, -1f, 0.6f)), new Vector3(1.0f,0.77f,0.56f)));

            Bloom = true;

        }

        public void Render(List<ViewEntity> entities, float time, ITransformation camera)
        {
            UpdateInstancing(entities);

            var arrTrans = new Dictionary<Enums.EntityType, Matrix4x4[]>();

            foreach (var transform in _transforms)
            {
                arrTrans.Add(transform.Key, transform.Value.ToArray());
            }

            _renderInstanceGroup.UpdateGeometry(arrTrans);

            _deferred.Draw(_renderState, camera, _instanceCounts, _textures, _normalMaps,  _disableBackFaceCulling);

            _directShadowMap.Draw(_renderState, camera, _instanceCounts, _deferred.Depth, _lights[0].Direction, _disableBackFaceCulling);

            _environmentMap.CreateMap(entities[2], _renderState, 0, arrTrans, _instanceCounts, _textures, _normalMaps,  _disableBackFaceCulling, _lights, new Vector3(0.1f), camera);
            _environmentMap.Draw(_renderState, _deferred.Depth);
            _addEnvMap.Draw(_deferred.Color, _environmentMap.Output, 0.5f);

            _lighting.Draw(camera, _addEnvMap.Output, _deferred.Normal, _deferred.Position, _directShadowMap.Output, _lights, new Vector3(0.7f));

            _sphereCut.Draw(camera, _lighting.Output, _deferred.Depth);

            _skybox.Draw(camera);
            _addSkybox.Draw(_skybox.Output, _sphereCut.Output);
            

            if (Bloom)
            {
                _bloom.Draw(_addSkybox.Output);
                _ssaoWithBlur.Draw(_deferred.Depth, _bloom.Output);
            }
            else
            {
                _ssaoWithBlur.Draw(_deferred.Depth, _addSkybox.Output);
            }

            TextureDrawer.Draw(_ssaoWithBlur.Output);
        }

        public void Resize(int width, int height)
        {
            _renderInstanceGroup.UpdateResolution(width, height);
        }

        private void UpdateInstancing(IEnumerable<ViewEntity> entities)
        {
            _transforms.Clear();
            _instanceCounts.Clear();

            foreach (var type in _meshes.Keys)
            {
                _instanceCounts.Add(type, 0);
                _transforms.Add(type, new List<Matrix4x4>());
            }

            foreach (var entity in entities)
            {
                _instanceCounts[entity.Type]++;
                _transforms[entity.Type].Add(entity.Transform);
            }
        }
    }
}
