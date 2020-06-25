/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// Provides each mesh with the corresponding image as an unlit texture, and blurs the color on disocclusion triangles.
/// </summary>
Shader "COLIBRIVR/Rendering/TexturedPerViewMeshesDT"
{
    Properties
    {
        _ColorData ("Color data", 2DArray) = "white" {}
        _OrthogonalityParameter ("Orthogonality parameter", float) = 0.1
        _TriangleSizeParameter ("Triangle size parameter", float) = 0.1
        _UseDebugColor ("Whether to use the debug color to mark disocclusion triangles", int) = 0
        _MipMapLevel ("The mip map level to use for blurring disocclusion triangles", int) = 0
        [PerRendererData] _SourceCamIndex ("Source camera index", int) = 0
        [PerRendererData] _SourceCamPosXYZ ("Source camera position", Vector) = (0, 0, 0)
    }
    SubShader
    {
/// PASS
        Pass
        {
    /// HEADER
            Name "DrawTexturedProxy"
            CGPROGRAM
            #include "./../CGIncludes/CoreCG.cginc"
            #pragma vertex unlitTex_vert
            #pragma fragment unlitTex_frag
            #pragma geometry unlitTex_geom
            #pragma require geometry
            #pragma require 2darray
    /// ENDHEADER

    /// STRUCTS
            struct unlitTex_vIN
            {
                float4 objectXYZW : POSITION;
                float2 texUV : TEXCOORD0;
            };

            struct unlitTex_v2g
            {
                float4 clipXYZW : SV_POSITION;
                float3 texArrayUVZ : TEXCOORD0;
                float3 worldXYZ : TEXCOORD1;
            };

            struct unlitTex_g2f
            {
                float4 clipXYZW : SV_POSITION;
                float3 texArrayUVZ : TEXCOORD0;
                float isDisocclusionTriangle : TEXCOORD2;
                float4 vertexColor : COLOR;
            };
    /// ENDSTRUCTS

    /// PROPERTIES
            UNITY_DECLARE_TEX2DARRAY(_ColorData);
            UNITY_INSTANCING_BUFFER_START(InstanceProperties)
                UNITY_DEFINE_INSTANCED_PROP(int, _SourceCamIndex)
                UNITY_DEFINE_INSTANCED_PROP(float3, _SourceCamPosXYZ)
            UNITY_INSTANCING_BUFFER_END(InstanceProperties)
            float _OrthogonalityParameter;
            float _TriangleSizeParameter;
            uint _UseDebugColor;
            uint _MipMapLevel;
    /// ENDPROPERTIES

    /// VERTEX
        unlitTex_v2g unlitTex_vert(unlitTex_vIN i)
        {
            unlitTex_v2g o;
            o.clipXYZW = UnityObjectToClipPos(i.objectXYZW);
            uint sourceCamIndex = UNITY_ACCESS_INSTANCED_PROP(InstanceProperties, _SourceCamIndex);
            o.texArrayUVZ = float3(i.texUV, sourceCamIndex);
            o.worldXYZ = mul(unity_ObjectToWorld, i.objectXYZW).xyz;
            return o;
        }
    /// ENDVERTEX

    /// <summary> 
    /// Checks whether a given triangle would create a "rubber sheet" at a disocclusion edge.
    /// If this is the case, one may want to remove the triangle, as it likely connects two objects which should not be connected.   
    /// </summary>
    bool IsTriangleRubberSheet(float3 vertex0, float3 vertex1, float3 vertex2)
    {
        float3 triangleCenter = (vertex0 + vertex1 + vertex2) / 3.0;
        float3 triangleNormal = normalize(cross(vertex2 - vertex0, vertex1 - vertex0));
        float3 normalizedFrameCamToTriangle = normalize(triangleCenter - _SourceCamPosXYZ);
        bool orthogonalityCheck = abs(dot(triangleNormal, normalizedFrameCamToTriangle)) < _OrthogonalityParameter;
        if(orthogonalityCheck)
        {
            float triangleDistance = length(triangleCenter);
            float deltaTriangleDistance = max(max(length(vertex0 - vertex1), length(vertex0 - vertex2)), length(vertex1 - vertex2));
            bool largeTrianglesCheck = (deltaTriangleDistance > _TriangleSizeParameter * triangleDistance);
            if(largeTrianglesCheck)
            {
                return true;
            }
        }
        return false;
    }

    /// GEOMETRY
            [maxvertexcount(3)]
            void unlitTex_geom (triangle unlitTex_v2g i[3], inout TriangleStream<unlitTex_g2f> triangleStream)
            {
                unlitTex_g2f o;
                bool isTriangleRubberSheet = IsTriangleRubberSheet(i[0].worldXYZ, i[1].worldXYZ, i[2].worldXYZ);
                o.isDisocclusionTriangle = isTriangleRubberSheet ? 1 : 0;
                [unroll]
                for(uint iter = 0; iter < 3; iter++)
                {
                    unlitTex_v2g iterVert = i[iter];
                    o.clipXYZW = iterVert.clipXYZW;
                    o.texArrayUVZ = iterVert.texArrayUVZ;
                    o.vertexColor = isTriangleRubberSheet ? fixed4(UNITY_SAMPLE_TEX2DARRAY_LOD(_ColorData, iterVert.texArrayUVZ, _MipMapLevel).rgb, 1) : 0;
                    triangleStream.Append(o);
                }
            }
    /// ENDGEOMETRY

    /// FRAGMENT
            base_fOUT unlitTex_frag (unlitTex_g2f i)
            {
                base_fOUT o;
                if(i.isDisocclusionTriangle > 0)
                    o.color = (_UseDebugColor == 1) ? fixed4(1, 0, 1, 1) : i.vertexColor;
                else
                    o.color = fixed4(UNITY_SAMPLE_TEX2DARRAY(_ColorData, i.texArrayUVZ).rgb, 1);
                return o;
            }
    /// ENDFRAGMENT
            ENDCG
        }
/// ENDPASS
    }
}
