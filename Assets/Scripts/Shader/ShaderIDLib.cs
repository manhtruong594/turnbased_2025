using UnityEngine;

public static class ShaderIDLib 
{
    // A
    public static readonly int Appearance = Shader.PropertyToID("_Appearance");

    //D
    public static readonly int DamageTime = Shader.PropertyToID("_DamageTime");

    // F
    public static readonly int Fill = Shader.PropertyToID("_Fill");
    public static readonly int Fill1 = Shader.PropertyToID("_Fill1");
    public static readonly int FresnelContrast = Shader.PropertyToID("_FresnelContrast");


    //L
    public static readonly int LastTempFill = Shader.PropertyToID("_LastTempFill");

    //T
    public static readonly int TempFill = Shader.PropertyToID("_TempFill");

    // S
    public static readonly int ShrinkDelay = Shader.PropertyToID("_ShrinkDelay");
    public static readonly int ShrinkSpeed = Shader.PropertyToID("_ShrinkSpeed");

}
