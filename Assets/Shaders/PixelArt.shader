Shader "Custom/PixelArt"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _PixelDensity("Pixel Density", Range(1, 500)) = 5
    }
        SubShader
        {
            Pass
            {
                CGPROGRAM
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

                sampler2D _MainTex;
                float _PixelDensity;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float2 uv = i.uv;
                    uv.x -= fmod(uv.x, 1.0 / _PixelDensity);
                    uv.y -= fmod(uv.y, 1.0 / _PixelDensity);
                    fixed4 col = tex2D(_MainTex, uv);
                    return col;
                }
                ENDCG
            }
        }
}