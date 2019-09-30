﻿using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph.Internal;
using Data.Util;

namespace UnityEditor.ShaderGraph
{
    class PreviewSubShader : ISubShader
    {
        public string GetSubshader(AbstractMaterialNode outputNode, ITarget target, GenerationMode mode, List<string> sourceAssetDependencyPaths = null)
        {
            var subShader = new ShaderStringBuilder();

            subShader.AppendLine("SubShader");
            using(subShader.BlockScope())
            {
                var surfaceTags = ShaderGenerator.BuildMaterialTags(SurfaceType.Opaque);
                var tagsBuilder = new ShaderStringBuilder(0);
                surfaceTags.GetTags(tagsBuilder, null);
                subShader.Concat(tagsBuilder);

                // use standard shader pass generation
                ShaderGenerator result = new ShaderGenerator();
                ShaderGraph.GenerationUtils.GenerateShaderPass(outputNode, target, PreviewTarget.Passes.Preview, mode, result, sourceAssetDependencyPaths,
                    PreviewTarget.fieldDependencies); 
                subShader.AppendLines(result.GetShaderString(0));
            }

            return subShader.ToString();
        }
    }
}
