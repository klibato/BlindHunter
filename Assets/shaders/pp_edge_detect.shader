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
    float3 vPositionOs : POSITION < Semantic( PosXyz ); >;
    float2 vTexCoord : TEXCOORD0 < Semantic( LowPrecisionUv ); >;
};
struct PixelInput
{
    float2 uv : TEXCOORD0;
	#if ( PROGRAM == VFX_PROGRAM_VS )
		float4 vPositionPs		: SV_Position;
	#endif
	#if ( ( PROGRAM == VFX_PROGRAM_PS ) )
		float4 vPositionSs		: SV_Position;
	#endif
};
VS
{
    PixelInput MainVs( VertexInput i )
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

    Texture2D g_tColorBuffer < Attribute( "ColorBuffer" ); SrgbRead( true ); >;

    // Helper : récupère la luminance d'un pixel à des coordonnées UV
    float SampleLuminance( float2 uv )
    {
        float4 color = g_tColorBuffer.SampleLevel( g_sBilinearMirror, uv, 0 );
        return dot( color.rgb, float3( 0.299, 0.587, 0.114 ) );
    }

    float4 MainPs( PixelInput i ) : SV_Target0
    {
        float2 vScreenUv = CalculateViewportUv( i.vPositionSs.xy );
        
        // Taille d'un pixel à l'écran (pour échantillonner les voisins)
        float2 texelSize = 1.0 / g_vRenderTargetSize.xy;

        // Échantillonne les 8 voisins + le pixel central
        float tl = SampleLuminance( vScreenUv + float2( -texelSize.x, -texelSize.y ) );
        float t  = SampleLuminance( vScreenUv + float2( 0,            -texelSize.y ) );
        float tr = SampleLuminance( vScreenUv + float2( texelSize.x,  -texelSize.y ) );
        float l  = SampleLuminance( vScreenUv + float2( -texelSize.x, 0 ) );
        float r  = SampleLuminance( vScreenUv + float2( texelSize.x,  0 ) );
        float bl = SampleLuminance( vScreenUv + float2( -texelSize.x, texelSize.y ) );
        float b  = SampleLuminance( vScreenUv + float2( 0,            texelSize.y ) );
        float br = SampleLuminance( vScreenUv + float2( texelSize.x,  texelSize.y ) );

        // Sobel X (changements horizontaux)
        float sobelX = -1.0 * tl + 1.0 * tr
                     + -2.0 * l  + 2.0 * r
                     + -1.0 * bl + 1.0 * br;

        // Sobel Y (changements verticaux)
        float sobelY = -1.0 * tl + -2.0 * t + -1.0 * tr
                     +  1.0 * bl +  2.0 * b +  1.0 * br;

        // Magnitude du gradient
        float edge = sqrt( sobelX * sobelX + sobelY * sobelY );

        // Seuil : on amplifie pour avoir des contours bien visibles
        edge = saturate( edge * 5.0 );

        // Retourne en blanc sur fond noir
        return float4( edge, edge, edge, 1.0 );
    }
}   