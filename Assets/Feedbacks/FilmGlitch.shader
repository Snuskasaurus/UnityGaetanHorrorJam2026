// Effet "pellicule abîmée" plein écran pour URP (Unity 6).
// À utiliser avec un Full Screen Pass Renderer Feature.
// Piloté par deux variables globales envoyées par FilmGlitchController :
//   _FilmGlitch : 0..1  intensité générale (tremblement, sauts, rayures, sépia...)
//   _FilmBurn   : 0..1  pellicule qui brûle depuis le centre
Shader "Hidden/FilmGlitch"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "FilmGlitch"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _FilmGlitch;
            float _FilmBurn;

            #define FRAME_RATE 14.0   // cadence du "projecteur" (les défauts changent à ce rythme)

            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float g    = saturate(_FilmGlitch);
                float burn = saturate(_FilmBurn);
                float2 uv  = input.texcoord;

                if (g <= 0.001 && burn <= 0.001)
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);

                float aspect = _ScreenParams.x / _ScreenParams.y;
                float frame  = floor(_Time.y * FRAME_RATE);
                float r1 = hash11(frame);
                float r2 = hash11(frame + 17.3);
                float r3 = hash11(frame + 51.7);

                // 1. Tremblement du cadre (gate weave)
                float2 weave = (float2(r1, r2) - 0.5) * float2(0.004, 0.008) * g;

                // 2. Saut de bobine : l'image glisse verticalement d'un coup
                float jumpChance = g * g * 0.5;
                float jump = (r3 < jumpChance) ? (0.2 + hash11(frame + 3.1) * 0.6) * g : 0.0;

                float2 guv = uv + weave;
                guv.y = frac(guv.y + jump);
                half3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, guv).rgb;

                // Barre noire entre deux images quand ça saute
                if (jump > 0.0)
                {
                    float seam = min(guv.y, 1.0 - guv.y);
                    col *= smoothstep(0.0, 0.035, seam);
                }

                // 3. Sépia / perte de couleur
                float lum = dot(col, float3(0.299, 0.587, 0.114));
                half3 sepia = lum * half3(1.07, 0.95, 0.78);
                col = lerp(col, sepia, saturate(g * 1.5));

                // 4. Scintillement de l'exposition
                col *= 1.0 + (r1 - 0.5) * 0.6 * g;

                // 5. Rayures verticales (claires et sombres), cassées par endroits
                [unroll]
                for (int s = 0; s < 3; s++)
                {
                    float fs = (float)s;
                    if (hash11(frame * 1.3 + fs * 7.1) < g)
                    {
                        float x = hash11(frame + fs * 13.7);
                        float w = 0.0008 + 0.0015 * hash11(fs + frame * 0.7);
                        float scratch = 1.0 - smoothstep(0.0, w, abs(uv.x - x));
                        scratch *= step(0.35, vnoise(float2(fs * 10.0, uv.y * 25.0 + frame)));
                        half3 scratchCol = (s == 1) ? half3(0.05, 0.04, 0.03) : half3(0.92, 0.9, 0.82);
                        col = lerp(col, scratchCol, scratch * 0.75);
                    }
                }

                // 6. Poussière (taches noires et blanches)
                float2 dustUV = uv * float2(aspect, 1.0) * 70.0;
                float d = hash21(floor(dustUV) + frame * 7.0);
                float dust = step(1.0 - 0.006 * g, d);
                half3 dustCol = (hash21(floor(dustUV) + frame) > 0.5) ? half3(0.02, 0.02, 0.02) : half3(0.95, 0.93, 0.86);
                col = lerp(col, dustCol, dust * 0.85);

                // 7. Grain
                float grain = hash21(floor(uv * _ScreenParams.xy * 0.5) + frame * 13.0) - 0.5;
                col += grain * 0.18 * g;

                // 8. Vignette (fenêtre du projecteur)
                float2 vd = uv - 0.5;
                col *= saturate(1.0 - dot(vd, vd) * 2.4 * g);

                // 9. Brûlure de la pellicule depuis le centre
                if (burn > 0.001)
                {
                    float2 bd = (uv - 0.5) * float2(aspect, 1.0);
                    float n = vnoise(bd * 8.0 + _Time.y * 0.6) * 0.25 + vnoise(bd * 25.0) * 0.1;
                    float t = length(bd) + n - burn * 1.5;   // < 0 = déjà brûlé

                    float scorch = 1.0 - smoothstep(0.0, 0.15, t);
                    col *= lerp(1.0, 0.2, scorch * step(0.0, t));          // bord roussi

                    float glow = 1.0 - smoothstep(0.0, 0.03, abs(t));
                    col = lerp(col, half3(1.0, 0.55, 0.15), glow);          // liseré incandescent

                    if (t < -0.03)
                        col = half3(1.0, 0.97, 0.9) * (0.8 + 0.2 * r2);      // trou blanc surexposé
                }

                return half4(saturate(col), 1.0);
            }
            ENDHLSL
        }
    }
}
