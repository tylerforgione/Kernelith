using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.DirectX.DirectX;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.DirectX.DXGI;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Kernelith.Runtime;

public sealed unsafe class GpuDevice : IDisposable
{
    private ComPtr<ID3D12Device> _device;
    private ComPtr<ID3D12InfoQueue> _infoQueue;

    public GpuAdapter Adapter { get; }
    public bool DebugLayerEnabled { get; }
    internal ID3D12Device* Native => _device.Get();

    public GpuDevice(GpuAdapter adapter, bool enableDebugLayer)
    {
        Adapter = adapter;

        if (enableDebugLayer)
        {
            using ComPtr<ID3D12Debug> debug = default;
            if (D3D12GetDebugInterface(__uuidof<ID3D12Debug>(), (void**)debug.GetAddressOf()).SUCCEEDED)
            {
                debug.Get()->EnableDebugLayer();
                DebugLayerEnabled = true;
            }
        }

        using ComPtr<IDXGIFactory4> factory = default;
        Check.ThrowIfFailed(CreateDXGIFactory2(DebugLayerEnabled ? DXGI_CREATE_FACTORY_DEBUG : 0u, __uuidof<IDXGIFactory4>(), (void**)factory.GetAddressOf()));

        using ComPtr<IDXGIAdapter1> dxgiAdapter = default;
        Check.ThrowIfFailed(factory.Get()->EnumAdapterByLuid(adapter.Luid, __uuidof<IDXGIAdapter1>(), (void**)dxgiAdapter.GetAddressOf()));

        Check.ThrowIfFailed(D3D12CreateDevice((IUnknown*)dxgiAdapter.Get(), D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_0, __uuidof<ID3D12Device>(), (void**)_device.GetAddressOf()));

        if (DebugLayerEnabled)
        {
            _device.Get()->QueryInterface(__uuidof<ID3D12InfoQueue>(), (void**)_infoQueue.GetAddressOf());
            if (Debugger.IsAttached)
            {
                _infoQueue.Get()->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_ERROR, TRUE);
            }
        }
    }

    public void Dispose()
    {
        _device.Dispose();
        _infoQueue.Dispose();
    }

    public string DrainMessages()
    {
        ID3D12InfoQueue* queue = _infoQueue.Get();
        if (queue is null) return string.Empty;

        ulong count = queue->GetNumStoredMessages();
        if (count == 0) return string.Empty;

        var sb = new StringBuilder();

        for (ulong i = 0; i < count; i++)
        {
            nuint size = 0;
            if (queue->GetMessage(i, null, &size).FAILED || size == 0) continue;

            D3D12_MESSAGE* msg = (D3D12_MESSAGE*)NativeMemory.Alloc(size);

            try
            {
                if (queue->GetMessage(i, msg, &size).FAILED) continue;

                string text = msg->pDescription is null ? string.Empty : Marshal.PtrToStringAnsi((nint)msg->pDescription) ?? String.Empty;

                sb.Append('[').Append(msg->Severity).Append(']').Append(msg->Category).Append(" #").Append((int)msg->ID).Append(": ").AppendLine(text);
            }
            finally
            {
                NativeMemory.Free(msg);
            }
        }

        queue->ClearStoredMessages();
        return sb.ToString();
    }
}
