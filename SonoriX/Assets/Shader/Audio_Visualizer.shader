Shader "VerticalSlice/Live/AudioVisualizer"
{
    Properties
    {
        _NumBars ("Number Of Bars", Float) = 25.0
        _GapWidth ("Bar Gap Width", Range(0.0, 0.5)) = 0.1
        _SpectrumGain ("Spectrum Gain", Range(1.0, 80.0)) = 18.0
        _BarHeight ("Bar Height", Range(0.0, 1.0)) = 0.336
        _ResponsePower ("Response Power", Range(0.3, 3.0)) = 1.35
        _NoiseFloor ("Noise Floor", Range(0.0, 0.05)) = 0.0
        _FrequencyCurve ("Frequency Curve", Range(0.5, 3.0)) = 1.65
        _FrequencyRange ("Frequency Range", Range(0.1, 1.0)) = 1.0
        _HighFrequencyBoost ("High Frequency Boost", Range(1.0, 8.0)) = 2.4
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            uniform float _AudioSpectrumData[512];
            uniform int _SpectrumLength;

            float _NumBars;
            float _GapWidth;
            float _SpectrumGain;
            float _BarHeight;
            float _ResponsePower;
            float _NoiseFloor;
            float _FrequencyCurve;
            float _FrequencyRange;
            float _HighFrequencyBoost;

            float segmentx(float2 p, float l)
            {
                float hl = 0.5 * l;
                p.x = abs(p.x);
                float d0 = abs(p.y);
                float d1 = length(p - float2(hl, 0.0));
                return p.x > hl ? d1 : d0;
            }

            float SampleSpectrum(float x)
            {
                int spectrumLength = max(_SpectrumLength, 1);
                int arrayIndex = (int)(saturate(x) * (spectrumLength - 1));
                arrayIndex = clamp(arrayIndex, 0, spectrumLength - 1);
                return _AudioSpectrumData[arrayIndex];
            }

            float SmoothSpectrum(float x, float width)
            {
                float w = max(width, 0.001);
                float value = 0.0;
                value += SampleSpectrum(x - w * 2.0) * 0.08;
                value += SampleSpectrum(x - w) * 0.22;
                value += SampleSpectrum(x) * 0.40;
                value += SampleSpectrum(x + w) * 0.22;
                value += SampleSpectrum(x + w * 2.0) * 0.08;
                return value;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv * 2.0 - 1.0;

                float2 roadUv = uv;
                roadUv.y += _Time.y * 0.2666667;

                float2 gridUv = frac(roadUv * float2(8.0, 16.0)) - 0.5;
                float gridDist = min(abs(gridUv.x), abs(gridUv.y));

                float gridLine = smoothstep(0.01, 0.0, gridDist);
                float gridGlow = exp(-30 * gridDist);
                float3 gridCol = float3(0.2, 0.3, 1.0) * (gridLine + gridGlow * 1.2);

                float absX = abs(uv.x);
                float absY = abs(uv.y);

                float minBorder = 0.333;
                float zoneMask = step(minBorder, absX);
                float remappedX = saturate((absX - minBorder) / (1.0 - minBorder));

                float barsPerSide = max(_NumBars, 1.0);
                float st = 1.0 / barsPerSide;
                float barId = min(floor(remappedX / st), barsPerSide - 1.0);

                float linearBand = (barId + 0.5) * st;
                float sampleX = pow(saturate(linearBand), _FrequencyCurve) * _FrequencyRange;
                float fft = SmoothSpectrum(sampleX, st * 0.75);
                fft = max(0.0, fft - _NoiseFloor);
                float gain = _SpectrumGain * lerp(0.8, _HighFrequencyBoost, remappedX);
                fft = 1.0 - exp(-fft * gain);
                fft = pow(saturate(fft), _ResponsePower);
                fft *= _BarHeight;

                float2 barUv = float2(frac(remappedX / st) * st - st * 0.5, absY);
                float d = segmentx(barUv.yx - float2(fft * 0.5, 0.0), fft) - st * (0.5 - _GapWidth);

                float specMask = smoothstep(0.001, 0.0, d);
                float specGlow = exp(-60.0 * max(d, 0.0));

                float3 specCol = lerp(float3(0.3, 0.2, 0.8), float3(1.0, 0.2, 0.0), remappedX)
                    * (specMask + specGlow * 0.6);
                specCol *= zoneMask;

                float3 finalColor = gridCol * 0.2975;
                finalColor += specCol;
                finalColor *= smoothstep(1.3, 0.2, length(uv));
                finalColor *= zoneMask;

                return fixed4(finalColor, zoneMask);
            }
            ENDCG
        }
    }
}
