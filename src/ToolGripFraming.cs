using System;

namespace DadsFPP;

internal static class ToolGripFraming
{
    internal static void Fit(float x, float y, float z, float fieldOfView, float aspect,
        out float visibleX, out float visibleY, out float visibleZ)
    {
        visibleZ = Math.Max(0.65f, z);
        float halfHeight = visibleZ * (float)Math.Tan(fieldOfView * Math.PI / 360.0);
        float sideLimit = Math.Min(0.3f, halfHeight * aspect * 0.6f);
        visibleX = Math.Max(-sideLimit, Math.Min(sideLimit, x));
        visibleY = Math.Max(-halfHeight * 0.6f, Math.Min(-halfHeight * 0.25f, y));
    }
}
