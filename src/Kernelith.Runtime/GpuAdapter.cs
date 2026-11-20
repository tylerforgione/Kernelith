using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.DirectX.DirectX;   // CreateDXGIFactory2, some DXGI_* consts
using static TerraFX.Interop.Windows.Windows;   // __uuidof<T>()
using static TerraFX.Interop.DirectX.DXGI;    // DXGI_ADAPTER_FLAG.*

namespace Kernelith.Runtime;

public sealed record GpuAdapter(string Name, ulong VramBytes, long DriverVersion, LUID Luid, uint VendorId, uint DeviceId)
{
    public string DriverVersionString =>
        $"{(DriverVersion >> 48) & 0xFFFF}.{(DriverVersion >> 32) & 0xFFFF}." +
        $"{(DriverVersion >> 16) & 0xFFFF}.{DriverVersion & 0xFFFF}";
    public static unsafe IReadOnlyList<GpuAdapter> Enumerate()
    {
        using ComPtr<IDXGIFactory6> factory = default;
        Check.ThrowIfFailed(CreateDXGIFactory2(0, __uuidof<IDXGIFactory6>(), (void**)factory.GetAddressOf()));

        var list = new List<GpuAdapter>();
        for (uint i = 0; ; i++)
        {
            using ComPtr<IDXGIAdapter1> adapter = default;
            HRESULT hr = factory.Get()->EnumAdapterByGpuPreference(
                i, DXGI_GPU_PREFERENCE.DXGI_GPU_PREFERENCE_HIGH_PERFORMANCE, __uuidof<IDXGIAdapter1>(), (void**)adapter.GetAddressOf()
            );
            if (hr == DXGI_ERROR_NOT_FOUND) break; // end of list
            Check.ThrowIfFailed(hr);

            DXGI_ADAPTER_DESC1 desc;
            Check.ThrowIfFailed(adapter.Get()->GetDesc1(&desc));
            if ((desc.Flags & (uint)DXGI_ADAPTER_FLAG.DXGI_ADAPTER_FLAG_SOFTWARE) != 0) continue;

            string name = new string((char*)&desc.Description);

            LARGE_INTEGER umd;
            long driver = adapter.Get()->CheckInterfaceSupport(__uuidof<IDXGIDevice>(), &umd).SUCCEEDED ? umd.QuadPart : 0;

            list.Add(new GpuAdapter(name, desc.DedicatedVideoMemory, driver, desc.AdapterLuid, desc.VendorId, desc.DeviceId));
        }

        return list;
    }

}
