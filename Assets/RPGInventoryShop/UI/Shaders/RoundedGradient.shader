Shader "RpgInventoryShop/UI/Rounded Gradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("uGUI compatibility", 2D) = "white" {}
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 position : POSITION;
                float4 tint : COLOR;
                float4 geometry : TEXCOORD0;
                float4 shape : TEXCOORD1;
                float4 fill : TEXCOORD2;
                float4 outline : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float4 tint : COLOR;
                float4 geometry : TEXCOORD0;
                float4 shape : TEXCOORD1;
                float4 fillTop : TEXCOORD2;
                float4 fillBottom : TEXCOORD3;
                float4 border : TEXCOORD4;
                float4 effect : TEXCOORD5;
                #ifdef UNITY_UI_CLIP_RECT
                float2 clipPosition : TEXCOORD6;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            float4 _UIMaskSoftness;
            float _UIVertexColorAlwaysGammaSpace;

            float4 UnpackColor(float packed, float alpha)
            {
                float red = floor(packed / 65536.0);
                float remainder = packed - red * 65536.0;
                float green = floor(remainder / 256.0);
                float blue = remainder - green * 256.0;
                return float4(float3(red, green, blue) / 255.0, alpha);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.position = UnityObjectToClipPos(input.position);
                output.tint = input.tint;
                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                    output.tint.rgb = GammaToLinearSpace(input.tint.rgb);
                output.geometry = input.geometry;
                output.shape = input.shape;
                output.fillTop = UnpackColor(input.fill.x, input.fill.y);
                output.fillBottom = UnpackColor(input.fill.z, input.fill.w);
                output.border = UnpackColor(input.outline.x, input.outline.y);
                output.effect = UnpackColor(input.outline.z, input.outline.w);
                #ifdef UNITY_UI_CLIP_RECT
                output.clipPosition = input.position.xy;
                #endif
                return output;
            }

            float RoundedDistance(float2 localPosition, float2 halfSize, float radius)
            {
                float2 outside = abs(localPosition) - halfSize + radius;
                return length(max(outside, 0.0)) + min(max(outside.x, outside.y), 0.0) - radius;
            }

            float Coverage(float distance, float smoothing)
            {
                float pixel = max(fwidth(distance) * smoothing, 1e-4);
                return smoothing > 0.0 ? 1.0 - smoothstep(-pixel, 0.0, distance) : step(distance, 0.0);
            }

            float3 ToRenderColor(float3 srgb)
            {
                return IsGammaSpace() ? srgb : GammaToLinearSpace(srgb);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 localPosition = input.geometry.xy;
                float2 halfSize = input.geometry.zw;
                float radius = input.shape.x;
                float borderWidth = input.shape.y;
                float smoothing = input.shape.z;
                float effectWidth = input.shape.w;
                float effectAlpha = abs(input.effect.a);
                bool extraInnerHighlight = input.effect.a < 0.0;

                float distance = RoundedDistance(localPosition, halfSize, radius);
                float outer = Coverage(distance, smoothing);
                float2 innerSize = halfSize - borderWidth;
                float innerDistance = RoundedDistance(localPosition, innerSize, max(0.0, radius - borderWidth));
                float inner = borderWidth <= 0.0 ? outer :
                    min(outer, Coverage(innerDistance, smoothing)) * step(1e-4, min(innerSize.x, innerSize.y));

                float fromTop = halfSize.y - localPosition.y;
                float gradient = saturate(fromTop / (halfSize.y * 2.0));
                float4 fill = lerp(input.fillTop, input.fillBottom, gradient);
                if (effectWidth < 0.0)
                {
                    float highlight = 1.0 - smoothstep(0.0, -effectWidth, max(0.0, fromTop));
                    fill.rgb = lerp(fill.rgb, input.effect.rgb, highlight * effectAlpha);
                    if (extraInnerHighlight)
                    {
                        float fromBottom = max(0.0, halfSize.y + localPosition.y);
                        float lowerHighlight = 1.0 - smoothstep(0.0, -effectWidth * 0.75, fromBottom);
                        fill.rgb = lerp(fill.rgb, input.border.rgb, lowerHighlight * effectAlpha * 1.5);
                    }
                }
                else if (effectWidth > 0.0 && extraInnerHighlight)
                {
                    float rim = 1.0 - smoothstep(0.0, effectWidth, max(0.0, -innerDistance));
                    fill.rgb = lerp(fill.rgb, input.effect.rgb, rim * effectAlpha);
                }

                float fillAlpha = inner * fill.a;
                float borderAlpha = (outer - inner) * input.border.a;
                float4 result = float4(
                    ToRenderColor(fill.rgb) * fillAlpha + ToRenderColor(input.border.rgb) * borderAlpha,
                    fillAlpha + borderAlpha);

                if (effectWidth > 0.0)
                {
                    float glow = (1.0 - smoothstep(0.0, effectWidth, max(0.0, distance))) *
                        (1.0 - outer) * effectAlpha;
                    result += float4(ToRenderColor(input.effect.rgb) * glow, glow) * (1.0 - result.a);
                }

                #ifdef UNITY_UI_CLIP_RECT
                float4 edges = float4(input.clipPosition - _ClipRect.xy, _ClipRect.zw - input.clipPosition);
                float4 softness = max(_UIMaskSoftness, float4(_UIMaskSoftnessX, _UIMaskSoftnessY,
                    _UIMaskSoftnessX, _UIMaskSoftnessY));
                float2 pixel = fwidth(input.clipPosition);
                float4 clipEdges = saturate(edges / max(float4(pixel, pixel) * 0.5 + softness * 0.25, 1e-4));
                result *= clipEdges.x * clipEdges.y * clipEdges.z * clipEdges.w;
                #endif

                result *= input.tint.a;
                result.rgb *= input.tint.rgb;
                #ifdef UNITY_UI_ALPHACLIP
                clip(min(result.a, outer) - 0.001);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
