﻿using OpenTK.Graphics.OpenGL4;
using Zenseless.HLGL;

namespace Maze.ExtensionMethods
{
    public static class ShaderProgramExtensionMethods
    {
        public static void ActivateTexture(this IShaderProgram shader, string name, int id, ITexture texture)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + id);
            texture.Activate();
            shader.Uniform(name, id);
        }

        public static void DeactivateTexture(this IShaderProgram shader, int id, ITexture texture)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + id);
            texture.Deactivate(); //Don´t use GL.BindTexture(..., 0). The texture target is not known this way and thus is not useable universally.
        }
    }
}
