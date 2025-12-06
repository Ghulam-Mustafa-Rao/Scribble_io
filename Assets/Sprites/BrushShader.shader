Shader"Custom/RTBrush"
{
    Properties
    {
        _Color("Color", Color) = (0,0,0,1)
        _BrushSize("Brush Size", Float) = 20
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha   // IMPORTANT

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 _Color;
            float _BrushSize;
            float2 _StartUV;
            float2 _EndUV;
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            v2f vert(uint id : SV_VertexID)
            {
                v2f o;
                o.pos = float4((id == 1 || id == 2) ? 3 : -1,
                                           (id == 2 || id == 3) ? 3 : -1,
                                            0, 1);
                o.uv = (o.pos.xy * 0.5f) + 0.5f;
                return o;
            }
            
            float2 ClosestPoint(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                float t = dot(p - a, ab) / dot(ab, ab);
                return a + ab * saturate(t);
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float2 closest = ClosestPoint(i.uv, _StartUV, _EndUV);
                float dist = distance(i.uv, closest);
            
                if (dist < (_BrushSize / 2048.0))
                    return _Color;
            
                return float4(0, 0, 0, 0);
            }

            ENDCG
        }
    }
}
