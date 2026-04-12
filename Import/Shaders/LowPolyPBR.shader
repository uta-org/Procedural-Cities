Shader "Procedural Cities/LowPoly PBR"
{
    Properties
    {
        _Color ("Tint Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Intensity", Range(0, 2)) = 1.0
        _ParallaxMap ("Height Map", 2D) = "white" {}
        _Parallax ("Height Intensity", Range(0, 0.1)) = 0.02
        _OcclusionMap ("Occlusion (R)", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 1.0
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _Glossiness ("Smoothness", Range(0, 1)) = 0.3
        _Tiling ("UV Tiling", Vector) = (1,1,0,0)

        [Toggle(_TRIPLANAR)] _UseTriplanar ("Triplanar Mapping", Float) = 0
        _TriplanarScale ("Triplanar Scale", Float) = 1.0
        _TriplanarSharpness ("Triplanar Blend Sharpness", Range(1, 16)) = 4.0

        [Toggle(_EMISSION)] _UseEmission ("Enable Emission", Float) = 0
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionIntensity ("Emission Intensity", Range(0, 5)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        #pragma shader_feature_local _TRIPLANAR
        #pragma shader_feature_local _EMISSION

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _ParallaxMap;
        sampler2D _OcclusionMap;

        half4 _Color;
        half _BumpScale;
        half _Parallax;
        half _OcclusionStrength;
        half _Metallic;
        half _Glossiness;
        float4 _Tiling;

        half _TriplanarScale;
        half _TriplanarSharpness;

        half4 _EmissionColor;
        half _EmissionIntensity;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            #if defined(_TRIPLANAR)
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
            #endif
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
        }

        // Triplanar sampling helper
        #if defined(_TRIPLANAR)
        fixed4 TriplanarSample(sampler2D tex, float3 wPos, float3 wNorm, float scale)
        {
            float3 blend = pow(abs(wNorm), _TriplanarSharpness);
            blend /= (blend.x + blend.y + blend.z + 0.001);

            float2 uvX = wPos.yz * scale;
            float2 uvY = wPos.xz * scale;
            float2 uvZ = wPos.xy * scale;

            fixed4 cx = tex2D(tex, uvX);
            fixed4 cy = tex2D(tex, uvY);
            fixed4 cz = tex2D(tex, uvZ);

            return cx * blend.x + cy * blend.y + cz * blend.z;
        }

        float3 TriplanarNormal(sampler2D tex, float3 wPos, float3 wNorm, float scale)
        {
            float3 blend = pow(abs(wNorm), _TriplanarSharpness);
            blend /= (blend.x + blend.y + blend.z + 0.001);

            float3 nx = UnpackScaleNormal(tex2D(tex, wPos.yz * scale), _BumpScale);
            float3 ny = UnpackScaleNormal(tex2D(tex, wPos.xz * scale), _BumpScale);
            float3 nz = UnpackScaleNormal(tex2D(tex, wPos.xy * scale), _BumpScale);

            return normalize(nx * blend.x + ny * blend.y + nz * blend.z);
        }
        #endif

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex * _Tiling.xy + _Tiling.zw;

            #if defined(_TRIPLANAR)
                // Triplanar path
                float3 wPos = IN.worldPos * _TriplanarScale;
                float3 wNorm = IN.worldNormal;

                fixed4 albedo = TriplanarSample(_MainTex, wPos, wNorm, _TriplanarScale);
                o.Normal = TriplanarNormal(_BumpMap, wPos, wNorm, _TriplanarScale);
                half occ = TriplanarSample(_OcclusionMap, wPos, wNorm, _TriplanarScale).r;
            #else
                // Parallax offset
                half h = tex2D(_ParallaxMap, uv).r;
                float2 offset = ParallaxOffset(h, _Parallax, IN.viewDir);
                uv += offset;

                fixed4 albedo = tex2D(_MainTex, uv);
                o.Normal = UnpackScaleNormal(tex2D(_BumpMap, uv), _BumpScale);
                half occ = tex2D(_OcclusionMap, uv).r;
            #endif

            o.Albedo = albedo.rgb * _Color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Occlusion = lerp(1, occ, _OcclusionStrength);
            o.Alpha = albedo.a * _Color.a;

            #if defined(_EMISSION)
                o.Emission = _EmissionColor.rgb * _EmissionIntensity;
            #endif
        }
        ENDCG
    }

    SubShader
    {
        // Transparent variant for glass/water
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        half4 _Color;
        half _Metallic;
        half _Glossiness;
        float4 _Tiling;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex * _Tiling.xy + _Tiling.zw;
            fixed4 c = tex2D(_MainTex, uv) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }

    FallBack "Standard"
    CustomEditor "LowPolyPBRShaderGUI"
}
