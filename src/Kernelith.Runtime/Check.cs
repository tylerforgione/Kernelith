using System.Runtime.InteropServices;

using TerraFX.Interop.Windows;

namespace Kernelith.Runtime;

internal static class Check
{
    public static void ThrowIfFailed(HRESULT hr, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(hr))] string? expr = null)
    {
        if (hr.FAILED)
        {
            throw new COMException($"{expr} failed with 0x{hr.Value:X8}", hr.Value);
        }
    }
}