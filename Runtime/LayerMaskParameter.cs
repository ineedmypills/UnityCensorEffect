using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
public sealed class LayerMaskParameter : ParameterOverride<LayerMask>
{
    public override void Interp(LayerMask from, LayerMask to, float t)
    {
        value = t < 1 ? from : to;
    }
}
