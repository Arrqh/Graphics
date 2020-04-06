﻿namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal struct PassDescriptor
    {
        // Definition
        public string displayName;
        public string referenceName;
        public string lightMode;
        public bool useInPreview;

        // Templates
        public string passTemplatePath;
        public string sharedTemplateDirectory;

        // Port mask
        public BlockFieldDescriptor[] vertexBlocks;
        public BlockFieldDescriptor[] pixelBlocks;

        // Collections
        public StructCollection structs;
        public FieldCollection requiredFields;
        public DependencyCollection fieldDependencies;
        public RenderStateCollection renderStates;
        public PragmaCollection pragmas;
        public DefineCollection defines;
        public KeywordCollection keywords;
        public IncludeCollection includes;

        // Methods
        public bool Equals(PassDescriptor other)
        {
            return referenceName == other.referenceName;
        }
    }
}
