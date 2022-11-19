Shader "Unlit/TankWater"
{
    Properties
    {
        _WaterColor ("Water Color", Color) = (1.0,1.0,1.0,0.0)
        _FillLevel  ("Fill Level", Range(0.0,1.0)) = 0.5
    }
    SubShader
    {
     Tags {"Queue"="Transparent-1" "IgnoreProjector"="True" "RenderType"="Transparent"}
     LOD 100
        Cull Off
             ZWrite Off
     Blend SrcAlpha OneMinusSrcAlpha 
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 localPos : TEXCOORD1;
            };

            float4 _WaterColor;
            float _FillLevel;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.localPos = (v.vertex.xyz + 0.5);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                
                fixed4 col = _WaterColor;
                if(i.localPos.y > _FillLevel)
                {
                    col.a = 0;
                }
                return col;
            }
            ENDCG
        }
    }
}
