Shader "URP/Blueprint Grid (Transparent)"
{
    Properties
    {
        _BaseColor ("Base Color (RGBA = color+opacity)", Color) = (0.10, 0.45, 1.0, 0.35)
        _GridColor ("Grid Color (A = intensity)", Color) = (1,1,1,0.6)

        _CellsPerUnit ("Cells Per World Unit", Float) = 3.0
        _LineWidth ("Line Width (fraction of cell)", Range(0.001,0.2)) = 0.03
        _GridSpeed ("Grid Scroll Speed", Float) = 0.2

        _TriSharpness ("Triplanar Sharpness", Range(1,8)) = 4.0

        _RimStrength ("Rim Strength", Range(0,3)) = 0.6
        _RimPower ("Rim Power", Range(0.5,8)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GridColor;
                float  _CellsPerUnit;
                float  _LineWidth;
                float  _GridSpeed;
                float  _TriSharpness;
                float  _RimStrength;
                float  _RimPower;
            CBUFFER_END

            // distance to nearest integer line along one axis (0 at line)
            float lineDist(float v)
            {
                float f = frac(v);
                return min(f, 1.0 - f);
            }

            // anti-aliased 2D grid (0..1)
            float grid2D(float2 uv, float lineWidth)
            {
                float t = _Time.y * _GridSpeed;
                uv += float2(t, t * 0.5);

                float dx = lineDist(uv.x);
                float dy = lineDist(uv.y);

                float2 fw = max(fwidth(uv), 1e-5);
                float  w  = lineWidth * 0.5;
                float  lx = 1.0 - smoothstep(w, w + fw.x, dx);
                float  ly = 1.0 - smoothstep(w, w + fw.y, dy);

                return saturate(max(lx, ly));
            }

            float3 triWeights(float3 n)
            {
                n = abs(n);
                n = pow(n, _TriSharpness);
                float sum = max(n.x + n.y + n.z, 1e-5);
                return n / sum;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.worldPos    = worldPos;
                OUT.normalWS    = normalize(normalWS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // triplanar UVs in world space
                float s = max(_CellsPerUnit, 1e-4);
                float3 wp = IN.worldPos * s;

                float gx = grid2D(wp.zy, _LineWidth); // X projection (YZ plane)
                float gy = grid2D(wp.xz, _LineWidth); // Y projection (XZ plane)
                float gz = grid2D(wp.xy, _LineWidth); // Z projection (XY plane)

                float3 w = triWeights(IN.normalWS);
                float grid = gx * w.x + gy * w.y + gz * w.z;

                // subtle fresnel
                float3 V = normalize(GetWorldSpaceViewDir(IN.worldPos));
                float  fres = pow(1.0 - saturate(dot(normalize(IN.normalWS), V)), _RimPower) * _RimStrength;

                float3 baseRGB = _BaseColor.rgb;
                float  baseA   = saturate(_BaseColor.a);

                float3 col = baseRGB + _GridColor.rgb * (grid * _GridColor.a);
                col += fres * _GridColor.rgb * 0.35;

                float alpha = saturate(baseA + grid * _GridColor.a * 0.4 + fres * 0.25);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
