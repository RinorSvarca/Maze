﻿using System;
using System.Collections.Generic;
using System.Numerics;
using Maze.ExtensionMethods;
using Maze.View.Rendering.Management;
using OpenTK.Graphics.OpenGL4;
using Zenseless.Geometry;
using Zenseless.HLGL;
using Zenseless.OpenGL;

namespace Maze.View.Rendering.Instances
{
    struct LightInShader
    {
        public Vector3 Position;
        public float Align1;
        public Vector3 Direction;
        public float Align2;
        public Vector3 Color;
        public float Align3;
    }

    public class Lighting : IUpdateResolution
    {
        public ITexture2D Output => _outputSurface.Texture;

        private readonly IShaderProgram _shader;
        private IRenderSurface _outputSurface;
        private readonly int _lightArraySizeInShader = 8;

        public Lighting(IContentLoader contentLoader)
        {
            _shader = contentLoader.LoadPixelShader("lighting.glsl");
        }

        public void Draw(ITransformation camera, ITexture2D materialColor, ITexture2D normals, ITexture2D position, ITexture2D shadowSurface, List<LightSource> lightSources, Vector3 ambientColor)
        {
            if (lightSources.Count > _lightArraySizeInShader)
            {
                throw new ArgumentException("A maximum of " + _lightArraySizeInShader + " light sources is possible. See shader 'deferredLighting.glsl' for details.");
            }

            _outputSurface.Activate();

            GL.Clear(ClearBufferMask.ColorBufferBit);

            _shader.Activate();

            Matrix4x4.Invert(camera.Matrix, out var invert);
            _shader.Uniform("camPos", invert.Translation / invert.M44);
            _shader.Uniform("hemColorTop", new Vector3(0.0f, 0.0f, .0f));
            _shader.Uniform("hemColorBottom", new Vector3(41.0f / 255.0f, 49.0f / 255.0f, 51.0f / 255.0f));

            _shader.ActivateTexture("materialColor", 0, materialColor);
            _shader.ActivateTexture("normals", 1, normals);
            _shader.ActivateTexture("position", 2, position);
            _shader.ActivateTexture("shadowSurface", 3, shadowSurface);

            var bufferObject = LightSourcesToBufferObject(lightSources);
            var loc = _shader.GetResourceLocation(ShaderResourceType.RWBuffer, "Lights");
            bufferObject.ActivateBind(loc);


            GL.DrawArrays(PrimitiveType.Quads, 0, 4);

            _shader.DeactivateTexture(3, shadowSurface);
            _shader.DeactivateTexture(2, position);
            _shader.DeactivateTexture(1, normals);
            _shader.DeactivateTexture(0, materialColor);

            _shader.Deactivate();

            _outputSurface.Deactivate();
        }

        private BufferObject LightSourcesToBufferObject(List<LightSource> lightSources)
        {
            LightInShader[] lightInShader = new LightInShader[_lightArraySizeInShader];

            for (int i = 0; i < lightSources.Count; i++)
            {
                lightInShader[i].Position = lightSources[i].Position;
                lightInShader[i].Direction = lightSources[i].Direction;
                lightInShader[i].Color = lightSources[i].Color;
            }

            for (int i = lightSources.Count; i < _lightArraySizeInShader; i++)
            {
                lightInShader[i].Position = LightSource.DefaultLightSource.Position;
                lightInShader[i].Direction = LightSource.DefaultLightSource.Direction;
                lightInShader[i].Color = LightSource.DefaultLightSource.Color;
            }

            BufferObject bufferObject = new BufferObject(BufferTarget.ShaderStorageBuffer);
            bufferObject.Set(lightInShader, BufferUsageHint.StaticCopy);

            return bufferObject;
        }

        public void UpdateResolution(int width, int height)
        {
            ((FBO)_outputSurface)?.Dispose();
            _outputSurface = new FBO(Texture2dGL.Create(width, height));

            _shader.Activate();
            _shader.Uniform("iResolution", new Vector2(width, height));
            _shader.Deactivate();
        }
    }
}
