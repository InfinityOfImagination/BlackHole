using UnityEngine;
using UnityEditor;
using VoidMart.Data;

namespace VoidMart.EditorTools
{
    /// <summary>
    /// Builds the handful of shared materials the whole game renders with.  Props all use one
    /// vertex-coloured clay material, so the entire street batches into very few draw calls.
    /// </summary>
    public static class MaterialFactory
    {
        const int CompareAlways = 8;
        const int CompareEqual = 3;
        const int CompareNotEqual = 6;

        public static void BuildAll(GameConfig config, GameAssets assets)
        {
            var theme = config.theme;

            var clayShader = Shader.Find("VoidMart/ClayLit");
            var maskShader = Shader.Find("VoidMart/HoleMask");
            var pitShader = Shader.Find("VoidMart/PitInterior");
            var skyShader = Shader.Find("VoidMart/SkyGradient");
            var unlitShader = Shader.Find("VoidMart/UnlitVertex");
            var irisShader = Shader.Find("VoidMart/IrisWipe");

            if (clayShader == null)
            {
                Debug.LogError("[VoidMart] VoidMart/ClayLit shader not found - did the shader files import?");
                return;
            }

            // Props, characters, machines: one material for everything vertex-coloured.
            var clay = AssetWriter.WriteMaterial("mat_clay", clayShader);
            ApplyClayDefaults(clay, theme);
            clay.SetFloat("_VertexColorAmount", 1f);
            clay.SetFloat("_StencilRef", 0f);
            clay.SetFloat("_StencilComp", CompareAlways);
            assets.SetMaterial("mat_clay", clay);

            // Street: textured, and refuses to draw where the hole mask wrote its stencil value.
            var street = AssetWriter.WriteMaterial("mat_street", clayShader);
            ApplyClayDefaults(street, theme);
            street.SetFloat("_VertexColorAmount", 0f);
            street.SetFloat("_SpecStrength", 0.04f);
            street.SetFloat("_RimStrength", 0.05f);
            street.SetFloat("_StencilRef", 1f);
            street.SetFloat("_StencilComp", CompareNotEqual);
            assets.SetMaterial("mat_street", street);

            var storeFloor = AssetWriter.WriteMaterial("mat_store_floor", clayShader);
            ApplyClayDefaults(storeFloor, theme);
            storeFloor.SetFloat("_VertexColorAmount", 0f);
            storeFloor.SetFloat("_SpecStrength", 0.1f);
            storeFloor.SetFloat("_StencilRef", 1f);
            storeFloor.SetFloat("_StencilComp", CompareNotEqual);
            assets.SetMaterial("mat_store_floor", storeFloor);

            var storeWall = AssetWriter.WriteMaterial("mat_store_wall", clayShader);
            ApplyClayDefaults(storeWall, theme);
            storeWall.SetFloat("_VertexColorAmount", 0f);
            storeWall.SetFloat("_StencilRef", 0f);
            storeWall.SetFloat("_StencilComp", CompareAlways);
            assets.SetMaterial("mat_store_wall", storeWall);

            if (maskShader != null)
            {
                var mask = AssetWriter.WriteMaterial("mat_hole_mask", maskShader);
                mask.SetFloat("_StencilRef", 1f);
                mask.SetFloat("_Radius", 0.5f);
                assets.SetMaterial("mat_hole_mask", mask);
                assets.holeMaskMaterial = mask;
            }

            if (pitShader != null)
            {
                var pit = AssetWriter.WriteMaterial("mat_pit", pitShader);
                pit.SetColor("_TopColor", theme.voidIndigo);
                pit.SetColor("_DeepColor", theme.voidInk);
                pit.SetColor("_RingColor", theme.neonCyan);
                pit.SetFloat("_StencilRef", 1f);
                pit.SetFloat("_StencilComp", CompareEqual);
                assets.SetMaterial("mat_pit", pit);
                assets.pitInteriorMaterial = pit;
            }

            if (skyShader != null)
            {
                var sky = AssetWriter.WriteMaterial("mat_sky", skyShader);
                sky.SetColor("_TopColor", theme.skyTop);
                sky.SetColor("_HorizonColor", theme.skyHorizon);
                sky.SetColor("_GroundColor", theme.skyGround);
                sky.SetFloat("_HorizonHeight", Mathf.Lerp(-0.25f, 0.25f, theme.skyHorizonHeight));
                sky.SetFloat("_HorizonSharpness", theme.skyHorizonSharpness);
                sky.SetColor("_SunColor", theme.sunColor);
                assets.SetMaterial("mat_sky", sky);
                assets.skyboxMaterial = sky;
            }

            if (unlitShader != null)
            {
                var glow = AssetWriter.WriteMaterial("mat_glow", unlitShader);
                glow.SetColor("_Color", Color.white);
                glow.SetFloat("_Intensity", 1.35f);
                glow.renderQueue = 3000;
                assets.SetMaterial("mat_glow", glow);

                var additive = AssetWriter.WriteMaterial("mat_glow_add", unlitShader);
                additive.SetColor("_Color", Color.white);
                additive.SetFloat("_Intensity", 1.1f);
                additive.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                additive.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                additive.renderQueue = 3100;
                assets.SetMaterial("mat_glow_add", additive);
            }

            if (irisShader != null)
            {
                var iris = AssetWriter.WriteMaterial("mat_iris", irisShader);
                iris.SetColor("_Color", theme.voidInk);
                iris.SetColor("_RingColor", theme.neonCyan);
                iris.SetFloat("_Softness", 8f);
                iris.SetFloat("_RingWidth", 14f);
                assets.SetMaterial("mat_iris", iris);
            }

            var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            if (particleShader != null)
            {
                var particle = AssetWriter.WriteMaterial("mat_particle", particleShader);
                particle.SetColor("_BaseColor", Color.white);
                if (particle.HasProperty("_Surface")) particle.SetFloat("_Surface", 1f);   // transparent
                if (particle.HasProperty("_Blend")) particle.SetFloat("_Blend", 1f);       // additive
                particle.renderQueue = 3200;
                assets.SetMaterial("mat_particle", particle);
            }

            EditorUtility.SetDirty(assets);
        }

        static void ApplyClayDefaults(Material material, ThemeConfig theme)
        {
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_ShadeColor", theme.ambientEquator);
            material.SetColor("_ShadowTint", theme.ambientGround);
            material.SetColor("_RimColor", theme.neonCyan);
            material.SetColor("_EmissionColor", Color.black);
            material.SetFloat("_Wrap", theme.clayWrap);
            material.SetFloat("_ShadeStrength", theme.clayShadeStrength);
            material.SetFloat("_RimStrength", theme.rimStrength);
            material.SetFloat("_RimPower", theme.rimPower);
            material.SetFloat("_SpecStrength", theme.specular);
            material.SetFloat("_Smoothness", 0.5f);
            material.SetFloat("_AmbientBoost", 1f);
        }

        /// <summary>Attaches the generated ground textures once they exist.</summary>
        public static void AssignTextures(GameAssets assets, Texture2D street, Texture2D storeFloor, Texture2D storeWall, CityConfig city)
        {
            var streetMaterial = assets.GetMaterial("mat_street");
            if (streetMaterial != null && street != null)
            {
                streetMaterial.SetTexture("_BaseMap", street);
                streetMaterial.SetTextureScale("_BaseMap", new Vector2(city.blocksX, city.blocksZ));
                EditorUtility.SetDirty(streetMaterial);
            }

            var floorMaterial = assets.GetMaterial("mat_store_floor");
            if (floorMaterial != null && storeFloor != null)
            {
                floorMaterial.SetTexture("_BaseMap", storeFloor);
                floorMaterial.SetTextureScale("_BaseMap", new Vector2(6f, 5f));
                EditorUtility.SetDirty(floorMaterial);
            }

            var wallMaterial = assets.GetMaterial("mat_store_wall");
            if (wallMaterial != null && storeWall != null)
            {
                wallMaterial.SetTexture("_BaseMap", storeWall);
                wallMaterial.SetTextureScale("_BaseMap", new Vector2(8f, 2f));
                EditorUtility.SetDirty(wallMaterial);
            }
        }
    }
}
