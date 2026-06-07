Shader "Custom/Stencil
"
{
    Properties
    {
        [IntRange]_index("Referece", Range(0, 255)) = 0
    }

    SubShader
    {
        Tags 
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Blend Zero One
            ZWrite Off

            Stencil
            {
                Ref[_index]
                Comp Always
                Pass Replace
                // Fail Keep
            }
        }
    }
}
