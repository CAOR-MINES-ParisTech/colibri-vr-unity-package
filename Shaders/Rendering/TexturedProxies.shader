/// Copyright 2019-2020 MINES ParisTech (PSL University)
/// This work is licensed under the terms of the MIT license, see the LICENSE file.
/// 
/// Author: Grégoire Dupont de Dinechin, gregoire@dinechin.org



/// <summary> 
/// Provides each mesh with the corresponding image as an unlit texture.
/// </summary>
Shader "COLIBRIVR/Rendering/TexturedProxies"
{
    Properties
    {
        _ColorTexArray ("Color texture array", 2DArray) = "white" {}
        [PerRendererData] _SourceCamIndex ("Source camera index", int) = 0
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
            #pragma multi_compile_instancing
    /// ENDHEADER

    /// STRUCTS
            struct unlitTex_vIN
            {
                float4 vertex : POSITION;
                float2 texUV : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct unlitTex_v2f
            {
                float4 vertex : SV_POSITION;
                float3 texArrayUVZ : TEXCOORD0;
            };
    /// ENDSTRUCTS

    /// PROPERTIES
            UNITY_DECLARE_TEX2DARRAY(_ColorTexArray);
            UNITY_INSTANCING_BUFFER_START(InstanceProperties)
                UNITY_DEFINE_INSTANCED_PROP(int, _SourceCamIndex)
            UNITY_INSTANCING_BUFFER_END(InstanceProperties)
    /// ENDPROPERTIES

    /// VERTEX
        unlitTex_v2f unlitTex_vert(unlitTex_vIN v)
        {
            unlitTex_v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            o.vertex = UnityObjectToClipPos(v.vertex);
            uint sourceCamIndex = UNITY_ACCESS_INSTANCED_PROP(InstanceProperties, _SourceCamIndex);
            o.texArrayUVZ = float3(v.texUV, sourceCamIndex);
            return o;
        }
    /// ENDVERTEX

    /// FRAGMENT
            base_fOUT unlitTex_frag (unlitTex_v2f i)
            {
                base_fOUT o;
                o.color = fixed4(UNITY_SAMPLE_TEX2DARRAY(_ColorTexArray, i.texArrayUVZ).rgb, 1);
                return o;
            }
    /// ENDFRAGMENT
            ENDCG
        }
/// ENDPASS
    }
}
