Shader "Horus/Billboard/Enemy Health Bar" 
{    Properties {
        _Fill ("Fill", range(0,1)) = 1
        _TempFill ("Temp Fill", range(0,1)) = 1
        _Color ("Health Color", Color) = (0, 1, 0, 1)
        _TempColor ("Temp Fill Color", Color) = (1, 1, 0, 1)
        _BgColor ("Background Color", Color) = (0, 0, 0, 0.5)
        _BorderColor ("Border Color", Color) = (0, 0, 0, 1)
        _BorderWidth ("Border Width", range(0.01, 0.2)) = 0.1        
        _ShrinkDelay ("Shrink Delay", range(0, 5)) = 0.1
        _ShrinkSpeed ("Shrink Speed", range(0.1, 5)) = 2
        [Toggle] _EnableBillboard ("Enable Billboard", float) = 1
        [Toggle] _EnableShrink ("Enable Shrink", float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest (Coverable)", Float) = 8
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass {
            ZTest [_ZTest]
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "MyLib.cginc"

            struct mesh_data
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };            
            struct Interpolators {
                float2 uv : TEXCOORD0;
                float2 originalUV : TEXCOORD1;  // UV gốc không bị ảnh hưởng bởi fill
                float4 vertex : SV_POSITION;
                // If you need instance data in the fragment shader, uncomment next line
                //UNITY_VERTEX_INPUT_INSTANCE_ID
            };            
            
            fixed4 _Color;
            fixed4 _TempColor;
            fixed4 _BgColor;
            fixed4 _BorderColor;            
            float _BorderWidth;
            float _ShrinkDelay;
            float _ShrinkSpeed;
            float _EnableBillboard;
            float _EnableShrink;
            float _ZTest;

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float, _Fill)
            UNITY_DEFINE_INSTANCED_PROP(float, _TempFill)
            UNITY_DEFINE_INSTANCED_PROP(float, _DamageTime)  // Thời điểm bị damage
            UNITY_DEFINE_INSTANCED_PROP(float, _LastTempFill) 
            UNITY_INSTANCING_BUFFER_END(Props)            
            
            Interpolators vert (mesh_data v) {
                Interpolators o;
                UNITY_SETUP_INSTANCE_ID(v);
                const float fill = UNITY_ACCESS_INSTANCED_PROP(Props, _Fill);                
                
                // Áp dụng billboard transformation tùy theo toggle
                if (_EnableBillboard > 0.5) {
                    o.vertex = billboard(v.vertex, SCALE_X, SCALE_Y);
                } else {
                    o.vertex = UnityObjectToClipPos(v.vertex);
                }
                
                o.originalUV = v.uv;  // Lưu UV gốc cho viền và tempFill
                o.uv = v.uv;
                o.uv.x -= fill;  // UV cho fill chính
                return o;
            }

            fixed4 frag (Interpolators i) : SV_Target {
                // Lấy giá trị TempFill và các thông số auto fade
                float baseTempFill = UNITY_ACCESS_INSTANCED_PROP(Props, _TempFill);
                float damageTime = UNITY_ACCESS_INSTANCED_PROP(Props, _DamageTime);
                float fill = UNITY_ACCESS_INSTANCED_PROP(Props, _Fill);
                float lastTempFill = UNITY_ACCESS_INSTANCED_PROP(Props, _LastTempFill);

                // Tính toán temp fill với hiệu ứng shrink liên tục
                float currentTempFill = baseTempFill;
                
                // Chỉ áp dụng shrink nếu _EnableShrink được bật
                if (_EnableShrink > 0.5) {
                    float timeSinceDamage = _Time.y - damageTime;
                    
                    // Kiểm tra xem có damage mới trong khi đang shrink không
                    if (lastTempFill > fill && lastTempFill > 0) {
                        // Bắt đầu shrink từ vị trí lastTempFill (vị trí temp fill trước đó)
                        float startPoint = max(lastTempFill, fill); // Đảm bảo không nhỏ hơn fill hiện tại
            
                        if (timeSinceDamage > _ShrinkDelay) {
                            float shrinkTime = timeSinceDamage - _ShrinkDelay;
                            float shrinkAmount = shrinkTime * _ShrinkSpeed;
                            currentTempFill = lerp(startPoint, fill, saturate(shrinkAmount));
                        } else {
                            currentTempFill = startPoint;
                        }
                    } else {
                        // Logic bình thường cho damage đầu tiên hoặc khi shrink đã hoàn thành
                        if (timeSinceDamage > _ShrinkDelay) {
                            float shrinkTime = timeSinceDamage - _ShrinkDelay;
                            float shrinkAmount = shrinkTime * _ShrinkSpeed;
                            currentTempFill = lerp(baseTempFill, fill, saturate(shrinkAmount));
                        }
                    }
                } else {
                    // Khi shrink bị tắt, temp fill bằng fill để không hiển thị
                    currentTempFill = fill;
                }
                
                // Sử dụng UV gốc để tính toán viền (không bị ảnh hưởng bởi fill)
                float2 originalUV = i.originalUV;
                  // Tính toán viền với tỷ lệ khác nhau cho X và Y
                // Giả sử thanh máu có tỷ lệ rộng hơn cao, nên viền X cần nhỏ hơn nhiều
                float borderX = _BorderWidth * 0.15;  // Viền trái-phải nhỏ hơn nhiều
                float borderY = _BorderWidth;         // Viền trên-dưới giữ nguyên
                
                // Kiểm tra xem pixel có nằm trong vùng viền không
                bool isInBorder = (originalUV.x < borderX || originalUV.x > (1.0 - borderX) || 
                                   originalUV.y < borderY || originalUV.y > (1.0 - borderY));
                
                // Nếu ở viền, trả về màu viền
                if (isInBorder) {
                    return _BorderColor;
                }
                
                // Sử dụng UV đã được dịch chuyển bởi fill cho logic thanh máu
                // Nhưng cần điều chỉnh để tương ứng với vùng nội dung bên trong viền
                float2 adjustedUV = i.uv;
                
                // Điều chỉnh để chỉ xét vùng bên trong viền (sử dụng borderX)
                float innerWidth = 1.0 - 2.0 * borderX;
                float adjustedX = (adjustedUV.x - borderX) / innerWidth;
                  // Tính toán vị trí cho temp fill với currentTempFill (đã được shrink)
                float2 tempUV = originalUV;
                tempUV.x -= currentTempFill;  // Sử dụng currentTempFill (đã được thu hẹp)
                float tempAdjustedX = (tempUV.x - borderX) / innerWidth;
                
                // Logic thanh máu với 3 lớp: background, temp fill, main fill
                // 1. Nếu trong vùng main fill (màu chính)
                if (adjustedX < 0) {
                    return _Color;
                }
                // 2. Nếu trong vùng temp fill (thanh máu phụ đang thu hẹp)
                else if (tempAdjustedX < 0) {
                    return _TempColor;
                }
                // 3. Còn lại là background
                else {
                    return _BgColor;
                }
            }
            ENDCG
        }
    }
}