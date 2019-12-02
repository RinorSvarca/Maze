﻿using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Zenseless.Geometry;
using Zenseless.HLGL;

namespace Zenseless.OpenGL
{
    /// <summary>
    /// Creates a content manager that creates OpenGL object instances, like textures
    /// </summary>
    public static class ContentManagerGL
    {
        /// <summary>
        /// Creates a content manager that creates OpenGL object instances, like textures
        /// </summary>
        /// <param name="resourceAssembly">The resource assembly that contains the resources.</param>
        /// <returns>A content manager instance</returns>
        public static FileContentManager Create(Assembly resourceAssembly)
        {
            var streamLoader = new StreamLoader();
            streamLoader.AddMappings(resourceAssembly);
            streamLoader.AddMappings(Assembly.GetExecutingAssembly());

            var mgr = new FileContentManager(streamLoader);
            mgr.RegisterImporter(ContentImporters.String);
            mgr.RegisterImporter(ContentImporters.ByteBuffer);
            mgr.RegisterImporter(ContentImporters.DefaultMesh);
            mgr.RegisterImporter(BitmapImporter);
            mgr.RegisterImporter(TextureArrayImporter);
            mgr.RegisterImporter(ShaderProgramImporter);
            mgr.RegisterUpdater<Texture2dGL>(Update);
            mgr.RegisterUpdater<ShaderProgramGL>(Update);
            mgr.RegisterUpdater<TextureArray2dGL>(Update);
            return mgr;
        }

        /// <summary>
        /// Gets the shader type from file extension.
        /// </summary>
        /// <param name="extension">The file extension.</param>
        /// <returns>the shader type</returns>
        /// <exception cref="ArgumentException"></exception>
        public static HLGL.ShaderType GetShaderTypeFromExtension(string extension)
        {
            if (mapExtensionToShaderType.TryGetValue(extension.ToLowerInvariant(), out var type))
            {
                return type;
            }
            throw new ArgumentException($"File extension {extension} is not valid for a shader.");
        }

        private static Dictionary<string, HLGL.ShaderType> mapExtensionToShaderType = new Dictionary<string, HLGL.ShaderType>
        {
            { ".comp" , HLGL.ShaderType.ComputeShader },
            { ".frag" , HLGL.ShaderType.FragmentShader },
            { ".glsl" , HLGL.ShaderType.FragmentShader },
            { ".geom" , HLGL.ShaderType.GeometryShader },
            { ".tesc" , HLGL.ShaderType.TessControlShader },
            { ".tese" , HLGL.ShaderType.TessEvaluationShader },
            { ".vert" , HLGL.ShaderType.VertexShader },
        };

        private static void Update(Texture2dGL texture, IEnumerable<NamedStream> resources)
        {
            foreach (var res in resources)
            {
                using (var bitmap = new Bitmap(res.Stream))
                {
                    texture.FromBitmap(bitmap);
                    return;
                }
                //TODO: if multiple textures assume these contain mipmap levels and load them
            }
        }

        private static ITexture2D BitmapImporter(IEnumerable<NamedStream> resources)
        {
            var texture = new Texture2dGL();
            Update(texture, resources);
            return texture;
        }

        private static IShaderProgram ShaderProgramImporter(IEnumerable<NamedStream> resources)
        {
            ShaderProgramGL shaderProgram = new ShaderProgramGL();
            Update(shaderProgram, resources);
            return shaderProgram;
        }

        private static void Update(ShaderProgramGL shaderProgram, IEnumerable<NamedStream> namedStreams)
        {
            var count = namedStreams.Count();
            if (2 > count) return;
            foreach (var res in namedStreams)
            {
                var shaderType = GetShaderTypeFromExtension(Path.GetExtension(res.Name));
                shaderProgram.Compile(ShaderCode(res.Stream), shaderType);
            }
            shaderProgram.Link();
        }

        private static string ShaderCode(Stream stream)
        {
            using (var reader = new StreamReader(stream, true))
            {
                return reader.ReadToEnd();
            }
        }

        private static ITexture2dArray TextureArrayImporter(IEnumerable<NamedStream> resources)
        {
            var texArray = new TextureArray2dGL();
            Update(texArray, resources);
            return texArray;
        }

        private static void Update(TextureArray2dGL texArray, IEnumerable<NamedStream> resources)
        {
            var count = resources.Count();
            if (2 > count) return;

            var bitmaps = from res in resources select new Bitmap(res.Stream);

            var first = bitmaps.First();
            var levels = MathHelper.MipMapLevels(first.Width, first.Height);
            var internalFormat = TextureLoaderDrawing.SelectInternalPixelFormat(first.PixelFormat);

            texArray.SetFormat(first.Width, first.Height, count, levels, (SizedInternalFormat)internalFormat);
            var slice = 0;
            foreach (var bitmap in bitmaps)
            {
                var buffer = bitmap.ToBuffer();
                var pixelFormat = TextureLoaderDrawing.SelectPixelFormat(bitmap.PixelFormat);
                texArray.Load(buffer, slice, pixelFormat, PixelType.UnsignedByte);
                ++slice;
            }
            texArray.Filter = TextureFilterMode.Mipmap;
            texArray.WrapFunction = TextureWrapFunction.ClampToEdge;
        }
        //private static VAO VAOImporter(IEnumerable<NamedStream> resources)
        //{
        //	Obj2Mesh.FromObj()
        //	var texArray = new TextureArray2dGL();
        //	Update(texArray, resources);
        //	return texArray;
        //}
    }
}
