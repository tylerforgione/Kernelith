using Kernelith.Runtime;

foreach (var a in GpuAdapter.Enumerate())
    Console.WriteLine($"{a.Name,-32} {a.VramBytes / (1024 * 1024 * 1024.0),5:F1} GB  " +
                      $"drv {a.DriverVersionString}  vid 0x{a.VendorId:X4} did 0x{a.DeviceId:X4}");