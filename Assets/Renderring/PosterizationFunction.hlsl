#ifndef POSTERIZATION_FUNCTION_INCLUDED
#define POSTERIZATION_FUNCTION_INCLUDED

float3 RGBToHSL(float3 c)
{
    float maxC = max(c.r, max(c.g, c.b));
    float minC = min(c.r, min(c.g, c.b));
    float d = maxC - minC;

    float l = (maxC + minC) * 0.5;
    float h = 0.0;
    float s = 0.0;

    if (d > 1e-5)
    {
        s = d / max(1.0 - abs(2.0 * l - 1.0), 1e-5);

        if (maxC == c.r)      h = (c.g - c.b) / d + (c.g < c.b ? 6.0 : 0.0);
        else if (maxC == c.g) h = (c.b - c.r) / d + 2.0;
        else                  h = (c.r - c.g) / d + 4.0;

        h /= 6.0;
    }
    return float3(h, s, l);
}

float3 HSLToRGB(float3 hsl)
{
    float3 hue = saturate(abs(fmod(hsl.x * 6.0 + float3(0.0, 4.0, 2.0), 6.0) - 3.0) - 1.0);
    float chroma = (1.0 - abs(2.0 * hsl.z - 1.0)) * hsl.y;
    return hsl.z + chroma * (hue - 0.5);
}

float Bayer4x4(float2 pixel)
{
    static const float bayer[16] =
    {
         0,  8,  2, 10,
        12,  4, 14,  6,
         3, 11,  1,  9,
        15,  7, 13,  5
    };
    int2 p = int2(fmod(floor(pixel), 4.0));
    return (bayer[p.y * 4 + p.x] + 0.5) / 16.0;
}

float QuantizeDithered(float x, float steps, float offset)
{
    float n = max(steps - 1.0, 1.0);
    return saturate(floor(x * n + offset) / n);
}

//#define LIGHTNESS_STEPS   40.0   
//#define SATURATION_STEPS  256.0   
//#define DITHER_STRENGTH   0.15  
//#define PIXEL_SIZE        10.0   

void PosterizationFunction_float(float3 ColorIn, float2 ScreenPos, float4 Params, out float3 Color)
{
    float LightnessSteps = Params[0];      // main banding, lower = harsher
    float SaturationSteps = Params[1];     // set very high (e.g. 256) to leave saturation alone
    float DitherStrength = Params[2];      // 0 = hard bands, 1 = full ordered dither
    float PixelSize = Params[3];           // must match the pixelation in the graph (1 if none)

    // Quantize in gamma space so dark tones get as many bands as bright ones
    float3 c = pow(saturate(ColorIn), 1.0 / 2.2);

    float3 hsl = RGBToHSL(c);

    float2 pixel = ScreenPos * _ScreenParams.xy / PixelSize;
    float threshold = lerp(0.5, Bayer4x4(pixel), DitherStrength);

    hsl.y = QuantizeDithered(hsl.y, SaturationSteps, 0.5);
    hsl.z = QuantizeDithered(hsl.z, LightnessSteps, threshold);

    Color = pow(saturate(HSLToRGB(hsl)), 2.2);
}


#endif
