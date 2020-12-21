using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(CloudLayer))]
    class CloudLayerEditor : VolumeComponentEditor
    {
        readonly GUIContent sunLabel        = new GUIContent("Sun light", "The main directional light, used for lighting and shadow casting.");
        readonly GUIContent shadowTiling    = new GUIContent("Shadow Tiling", "The tiling of the cloud shadows texture. Controlled by the cookie size parameter on the sun light.");

        public override bool hasAdvancedMode => true;

        public SerializedDataParameter cloudMap;
        public SerializedDataParameter[] opacities;

        public SerializedDataParameter rotation;
        public SerializedDataParameter tint;
        public SerializedDataParameter exposure;

        public SerializedDataParameter distortion;
        public SerializedDataParameter scrollDirection;
        public SerializedDataParameter scrollSpeed;
        public SerializedDataParameter flowmap;

        public SerializedDataParameter lighting;
        public SerializedDataParameter steps;
        public SerializedDataParameter thickness;

        public SerializedDataParameter castShadows;

        SerializedDataParameter m_Opacity, m_UpperHemisphereOnly, m_LayerCount;
        SerializedDataParameter m_Resolution, m_ShadowResolution;
        SerializedDataParameter m_ShadowMultiplier, m_ShadowTint;
        //CloudMapParameter[] m_Layers;

        SerializedDataParameter m_cloudMap_LayerA;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<CloudLayer>(serializedObject);

            m_Opacity = Unpack(o.Find(x => x.opacity));
            m_UpperHemisphereOnly = Unpack(o.Find(x => x.upperHemisphereOnly));
            m_LayerCount = Unpack(o.Find(x => x.layers));
            m_Resolution = Unpack(o.Find(x => x.resolution));

            m_ShadowMultiplier = Unpack(o.Find(x => x.shadowMultiplier));
            m_ShadowTint = Unpack(o.Find(x => x.shadowTint));
            m_ShadowResolution = Unpack(o.Find(x => x.shadowResolution));

            cloudMap = Unpack(o.Find(x => x.cloudMap));
            opacities = new SerializedDataParameter[]
            {
                    Unpack(o.Find(x => x.opacityR)),
                    Unpack(o.Find(x => x.opacityG)),
                    Unpack(o.Find(x => x.opacityB)),
                    Unpack(o.Find(x => x.opacityA))
            };

            rotation = Unpack(o.Find(x => x.rotation));
            tint = Unpack(o.Find(x => x.tint));
            exposure = Unpack(o.Find(x => x.exposure));
            distortion = Unpack(o.Find(x => x.distortionMode));
            scrollDirection = Unpack(o.Find(x => x.scrollDirection));
            scrollSpeed = Unpack(o.Find(x => x.scrollSpeed));
            flowmap = Unpack(o.Find(x => x.flowmap));

            lighting = Unpack(o.Find(x => x.lighting));
            steps = Unpack(o.Find(x => x.steps));
            thickness = Unpack(o.Find(x => x.thickness));
            castShadows = Unpack(o.Find(x => x.castShadows));

            //m_Layers = new CloudMapParameter[]
            //{
            //    UnpackCloudMap(o.Find(x => x.layerA)),
            //    UnpackCloudMap(o.Find(x => x.layerB))
            //};
        }

        
        void PropertyField(string label)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);

            PropertyField(cloudMap);
            using (new HDEditorUtils.IndentScope())
            {
                for (int i = 0; i < 4; i++)
                    PropertyField(opacities[i]);
            }

            PropertyField(rotation);
            PropertyField(tint);
            PropertyField(exposure);

            PropertyField(distortion);
            using (new HDEditorUtils.IndentScope())
            {
                PropertyField(scrollDirection);
                PropertyField(scrollSpeed);
                if (distortion.value.intValue == (int)CloudDistortionMode.Flowmap)
                {
                    PropertyField(flowmap);
                }
            }

            PropertyField(lighting);
            using (new HDEditorUtils.IndentScope())
            {
                PropertyField(steps);
                PropertyField(thickness);
            }
            PropertyField(castShadows);
        }
        

        //bool CastShadows => m_Layers[0].castShadows.value.boolValue || (m_LayerCount.value.intValue == (int)CloudMapMode.Double && m_Layers[1].castShadows.value.boolValue);
        bool CastShadows => castShadows.value.boolValue;

        public override void OnInspectorGUI()
        {
            bool prevShadows = CastShadows;

            PropertyField(m_Opacity);
            if (isInAdvancedMode)
                PropertyField(m_UpperHemisphereOnly);
            PropertyField(m_LayerCount);
            if (isInAdvancedMode)
                PropertyField(m_Resolution);

            PropertyField("Layer A");
            //if (m_LayerCount.value.intValue == (int)CloudMapMode.Double)
            //    PropertyField(m_Layers[1], "Layer B");

            Light sun = HDRenderPipeline.currentPipeline?.GetCurrentSunLight();
            if (sun != null && sun.TryGetComponent(out HDAdditionalLightData hdSun))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Cloud Shadows", EditorStyles.miniLabel);

                PropertyField(m_ShadowMultiplier);
                PropertyField(m_ShadowTint);
                if (isInAdvancedMode)
                    PropertyField(m_ShadowResolution);

                bool shadows = CastShadows;
                if (prevShadows && !shadows)
                    sun.cookie = null;
                else if (shadows && sun.cookie == null)
                {
                    Undo.RecordObject(hdSun, "Change cookie size");
                    hdSun.shapeHeight = 500;
                    hdSun.shapeWidth = 500;
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20);
                        EditorGUILayout.ObjectField(sunLabel, sun, typeof(Light), true);
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20);
                        var size = new Vector2(hdSun.shapeWidth, hdSun.shapeHeight);
                        EditorGUILayout.Vector2Field(shadowTiling, size);
                    }
                }
            }
        }
    }
}
