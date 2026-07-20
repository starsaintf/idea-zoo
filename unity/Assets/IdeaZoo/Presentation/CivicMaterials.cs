using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdeaZoo.Presentation
{
    public enum CivicSurface
    {
        Ink,
        Paper,
        Brass,
        Clay,
        Glass,
        TealGlow,
        Rust,
        Moss
    }

    public static class CivicMaterialLibrary
    {
        private static readonly Dictionary<CivicSurface, Material> Materials = new Dictionary<CivicSurface, Material>();
        private static readonly Dictionary<CivicSurface, Texture2D> Textures = new Dictionary<CivicSurface, Texture2D>();

        public static Material Get(CivicSurface surface)
        {
            Material existing;
            if (Materials.TryGetValue(surface, out existing) && existing != null) return existing;

            // WebGL's build pipeline strips dynamically-found shaders unless they
            // are part of the player. The Resources asset is deliberately the first
            // choice, so the live game never relies on a project-level "always
            // included shaders" setting to create its world at startup.
            var shader = Resources.Load<Shader>("IdeaZooLit");
            if (shader == null) shader = Shader.Find("IdeaZoo/RuntimeLit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Idea Zoo could not load its runtime material shader.");
            var material = new Material(shader) { name = "IZ_" + surface };
            var color = SurfaceColor(surface);

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);

            var texture = BuildTexture(surface);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", surface == CivicSurface.Glass ? 0.88f : surface == CivicSurface.Brass ? 0.56f : 0.22f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", surface == CivicSurface.Brass ? 0.72f : 0f);

            if (surface == CivicSurface.Glass)
            {
                material.renderQueue = 3000;
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (surface == CivicSurface.TealGlow)
            {
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", new Color(0.20f, 1.35f, 1.18f));
                material.EnableKeyword("_EMISSION");
            }

            Materials[surface] = material;
            return material;
        }

        public static Color SurfaceColor(CivicSurface surface)
        {
            switch (surface)
            {
                case CivicSurface.Paper: return new Color(0.78f, 0.72f, 0.60f, 1f);
                case CivicSurface.Brass: return new Color(0.67f, 0.47f, 0.22f, 1f);
                case CivicSurface.Clay: return new Color(0.43f, 0.23f, 0.18f, 1f);
                case CivicSurface.Glass: return new Color(0.22f, 0.70f, 0.68f, 0.46f);
                case CivicSurface.TealGlow: return new Color(0.21f, 0.83f, 0.75f, 1f);
                case CivicSurface.Rust: return new Color(0.56f, 0.20f, 0.15f, 1f);
                case CivicSurface.Moss: return new Color(0.28f, 0.42f, 0.25f, 1f);
                default: return new Color(0.035f, 0.09f, 0.10f, 1f);
            }
        }

        private static Texture2D BuildTexture(CivicSurface surface)
        {
            Texture2D cached;
            if (Textures.TryGetValue(surface, out cached) && cached != null) return cached;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "IZ_" + surface + "_Pattern",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];
            var baseColor = SurfaceColor(surface);
            var seed = 17 + (int)surface * 113;
            var random = new System.Random(seed);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var n = Mathf.PerlinNoise((x + seed) * 0.085f, (y + seed * 0.37f) * 0.085f);
                    var grain = ((float)random.NextDouble() - 0.5f) * 0.08f;
                    var line = 0f;
                    if (surface == CivicSurface.Paper) line = Mathf.Sin(y * 0.82f + Mathf.Sin(x * 0.11f)) * 0.035f;
                    if (surface == CivicSurface.Brass) line = Mathf.Sin((x + y) * 0.18f) * 0.028f;
                    if (surface == CivicSurface.Ink) line = Mathf.PerlinNoise(x * 0.025f, y * 0.13f) * 0.04f;
                    if (surface == CivicSurface.Clay) line = ((x + y * 3) % 11 == 0) ? -0.06f : 0f;
                    var amount = (n - 0.5f) * 0.13f + grain + line;
                    pixels[y * size + x] = new Color(
                        Mathf.Clamp01(baseColor.r + amount),
                        Mathf.Clamp01(baseColor.g + amount),
                        Mathf.Clamp01(baseColor.b + amount),
                        baseColor.a);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, true);
            Textures[surface] = texture;
            return texture;
        }
    }
}
