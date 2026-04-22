Shader "Custom/URP/SelectorGlowBox"
{
    Properties
    {
        [Header(Main)]
        _BaseColor ("Base Color", Color) = (0.2, 1.0, 0.5, 1.0)
        _Alpha ("Base Alpha", Range(0, 1)) = 0.25
        _Intensity ("Intensity", Range(0, 10)) = 2.0

        [Header(Height Glow)]
        _HeightMin ("Height Min", Float) = -0.5
        _HeightMax ("Height Max", Float) = 0.5
        _GlowHeight ("Glow Height", Range(0, 1)) = 0.65
        _GlowSoftness ("Glow Softness", Range(0.001, 1)) = 0.2

        [Header(Pulse)]
        _PulseSpeed ("Pulse Speed", Range(0, 20)) = 2.0
        _PulseAmount ("Pulse Amount", Range(0, 2)) = 0.25
        _PulseScale ("Pulse Scale", Range(0, 30)) = 10.0

        [Header(Fresnel Edge)]
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3.0
        _FresnelStrength ("Fresnel Strength", Range(0, 5)) = 1.5

        [Header(Scrolling Overlay)]
        _OverlayStrength ("Overlay Strength", Range(0, 2)) = 0.35
        _OverlaySpeed ("Overlay Speed", Range(0, 10)) = 1.5
        _OverlayScale ("Overlay Scale", Range(0, 50)) = 12.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float3 viewDirWS  : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Alpha;
                float _Intensity;

                float _HeightMin;
                float _HeightMax;
                float _GlowHeight;
                float _GlowSoftness;

                float _PulseSpeed;
                float _PulseAmount;
                float _PulseScale;

                float _FresnelPower;
                float _FresnelStrength;

                float _OverlayStrength;
                float _OverlaySpeed;
                float _OverlayScale;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normalize(normalInputs.normalWS);
                OUT.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                // 1) Нормализованная высота объекта в локальных координатах
                float height01 = saturate((IN.positionOS.y - _HeightMin) / max(0.0001, (_HeightMax - _HeightMin)));

                // 2) Основная высота свечения:
                // всё, что ниже GlowHeight, видно сильнее; выше - мягко затухает
                float glowMask = 1.0 - smoothstep(_GlowHeight - _GlowSoftness, _GlowHeight, height01);

                // 3) Пульсация / перелив по высоте
                float pulse = sin(_Time.y * _PulseSpeed + height01 * _PulseScale) * 0.5 + 0.5;
                float pulseMul = lerp(1.0, pulse, _PulseAmount);

                // 4) Бегущая дополнительная полоска / "живая" поверхность
                float overlay = sin(height01 * _OverlayScale - _Time.y * _OverlaySpeed) * 0.5 + 0.5;
                overlay *= _OverlayStrength * glowMask;

                // 5) Френель по краям
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower) * _FresnelStrength;

                // 6) Итоговая яркость
                float brightness = (glowMask * pulseMul + overlay + fresnel) * _Intensity;

                float3 finalColor = _BaseColor.rgb * brightness;

                // Базовая прозрачность: сильнее снизу/в зоне свечения, но край тоже читается
                float alpha = (_Alpha * glowMask) + (fresnel * 0.35);
                alpha = saturate(alpha);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}