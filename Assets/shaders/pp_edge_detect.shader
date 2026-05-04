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
    #include "common/classes/Depth.hlsl"

    Texture2D g_tColorBuffer < Attribute( "ColorBuffer" ); SrgbRead( true ); >;

    // 8 sources d'écho
    float4 g_Source0 < Attribute("g_Source0"); >;
    float4 g_Source1 < Attribute("g_Source1"); >;
    float4 g_Source2 < Attribute("g_Source2"); >;
    float4 g_Source3 < Attribute("g_Source3"); >;
    float4 g_Source4 < Attribute("g_Source4"); >;
    float4 g_Source5 < Attribute("g_Source5"); >;
    float4 g_Source6 < Attribute("g_Source6"); >;
    float4 g_Source7 < Attribute("g_Source7"); >;

    float4 g_Timing0 < Attribute("g_Timing0"); >;
    float4 g_Timing1 < Attribute("g_Timing1"); >;
    float4 g_Timing2 < Attribute("g_Timing2"); >;
    float4 g_Timing3 < Attribute("g_Timing3"); >;
    float4 g_Timing4 < Attribute("g_Timing4"); >;
    float4 g_Timing5 < Attribute("g_Timing5"); >;
    float4 g_Timing6 < Attribute("g_Timing6"); >;
    float4 g_Timing7 < Attribute("g_Timing7"); >;

    float g_WaveSpeedFactor < Attribute("g_WaveSpeedFactor"); Default(3.0); >;
    float g_WaveThickness < Attribute("g_WaveThickness"); Default(80.0); >;

    float SampleDepth( float2 screenPos )
    {
        return Depth::GetLinear( screenPos );
    }

    // Retourne 1 si le pixel est sur le front d'onde 3D, 0 sinon
    float IsInsideWaveFront( float3 worldPos, float3 sourcePos, float intensity, float age, float lifetime )
    {
        if ( intensity <= 0.001 ) return 0.0;
        if ( age >= lifetime ) return 0.0;

        float distToSource = distance( worldPos, sourcePos );
        float waveSpeed = intensity * g_WaveSpeedFactor;
        float waveRadius = age * waveSpeed;
        float distFromWaveFront = abs( distToSource - waveRadius );

        // Le pixel est dans la sphère si distance au front < épaisseur
        float inWave = 1.0 - smoothstep( 0.0, g_WaveThickness, distFromWaveFront );

        // Atténuation avec l'âge
        float ageFactor = saturate( 1.0 - ( age / lifetime ) );

        return inWave * ageFactor;
    }

    float4 MainPs( PixelInput i ) : SV_Target0
    {
        float2 vScreenUv = CalculateViewportUv( i.vPositionSs.xy );
        float3 worldPos = Depth::GetWorldPosition( i.vPositionSs.xy );
        float2 screenPos = i.vPositionSs.xy;

        // Sobel sur la profondeur (edge detection géométrique)
        float tl = SampleDepth( screenPos + float2( -1, -1 ) );
        float t  = SampleDepth( screenPos + float2(  0, -1 ) );
        float tr = SampleDepth( screenPos + float2(  1, -1 ) );
        float l  = SampleDepth( screenPos + float2( -1,  0 ) );
        float center = SampleDepth( screenPos );
        float r  = SampleDepth( screenPos + float2(  1,  0 ) );
        float bl = SampleDepth( screenPos + float2( -1,  1 ) );
        float b  = SampleDepth( screenPos + float2(  0,  1 ) );
        float br = SampleDepth( screenPos + float2(  1,  1 ) );

        // Skip le ciel / hors map
        if ( center > 5000.0 ) 
        {
            return float4( 0.0, 0.0, 0.0, 1.0 );
        }

        float sobelX = -1.0 * tl + 1.0 * tr
                     + -2.0 * l  + 2.0 * r
                     + -1.0 * bl + 1.0 * br;
        float sobelY = -1.0 * tl + -2.0 * t + -1.0 * tr
                     +  1.0 * bl +  2.0 * b +  1.0 * br;

        float depthEdge = sqrt( sobelX * sobelX + sobelY * sobelY );

        // C'est-il un contour ? (1 ou 0)
        float edge = step( 10.0, depthEdge );

        // Le pixel est-il sur le front d'onde 3D ? (0 à 1)
        float inSphere = 0.0;
        inSphere = max(inSphere, IsInsideWaveFront(worldPos, g_Source0.xyz, g_Source0.w, g_Timing0.x, g_Timing0.y));
        inSphere = max(inSphere, IsInsideWaveFront(worldPos, g_Source1.xyz, g_Source1.w, g_Timing1.x, g_Timing1.y));
        inSphere = max(inSphere, IsInsideWaveFront(worldPos, g_Source2.xyz, g_Source2.w, g_Timing2.x, g_Timing2.y));
        inSphere = max(inSphere, IsInsideWaveFront(worldPos, g_Source3.xyz, g_Source3.w, g_Timing3.x, g_Timing3.y));
        inSphere = max(inSphere, IsInsideWaveFront(worldPos, g_Source4.xyz, g_Source4.w, g_Timing4.x, g_Timing4.y));
        inSphere = max(inSphere, IsInsideWaveFront(worldPos, g_Source5.xyz, g_Source5.w, g_Timing5.x, g_Timing5.y));
        inSphere = max(inSphere, IsInsideWaveFront(worldPos, g_Source6.xyz, g_Source6.w, g_Timing6.x, g_Timing6.y));
        inSphere = max(inSphere, IsInsideWaveFront(worldPos, g_Source7.xyz, g_Source7.w, g_Timing7.x, g_Timing7.y));

        // AND logique : on dessine uniquement les contours qui sont dans la sphère
        float finalColor = edge * inSphere;

        return float4( finalColor, finalColor, finalColor, 1.0 );
    }
}