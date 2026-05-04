HEADER
{
    DevShader = true;
}
MODES
{
    Default();
    Forward();
}
COMMON
{
#include "postprocess/shared.hlsl"
}
struct VertexInput
{
    float3 vPositionOs : POSITION < Semantic(PosXyz);
    > ;
    float2 vTexCoord : TEXCOORD0 < Semantic(LowPrecisionUv);
    > ;
};
struct PixelInput
{
    float2 uv : TEXCOORD0;
#if (PROGRAM == VFX_PROGRAM_VS)
    float4 vPositionPs : SV_Position;
#endif
#if ((PROGRAM == VFX_PROGRAM_PS))
    float4 vPositionSs : SV_Position;
#endif
};
VS
{
    PixelInput MainVs(VertexInput i)
    {
        PixelInput o;

        o.vPositionPs = float4(i.vPositionOs.xy, 0.0f, 1.0f);
        o.uv = i.vTexCoord;
        return o;
    }
}
PS
{
#include "postprocess/common.hlsl"
#include "postprocess/functions.hlsl"

    Texture2D g_tColorBuffer < Attribute("ColorBuffer");
    SrgbRead(true);
    > ;

    float4 MainPs(PixelInput i) : SV_Target0
    {
        // 1. Récupère les UV de l'écran et la couleur originale du pixel
        float2 vScreenUv = CalculateViewportUv(i.vPositionSs.xy);
        float4 color = g_tColorBuffer.SampleLevel(g_sBilinearMirror, vScreenUv, 0);

        // 2. Grayscale (luminance ITU-R BT.601)
        float luminance = dot(color.rgb, float3(0.299, 0.587, 0.114));
        float3 grayColor = float3(luminance, luminance, luminance);

        // 3. Vignette : assombrir les bords
        float2 center = float2(0.5, 0.5);
        float dist = length(vScreenUv - center);
        float vignette = saturate(1.0 - dist * 1.8);

        // 4. Applique la vignette au gris
        grayColor *= vignette;

        return float4(grayColor, color.a);
    }
}
