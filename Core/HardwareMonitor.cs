using LibreHardwareMonitor.Hardware;

namespace TrayBit.Core;

internal sealed class HardwareMonitor : IDisposable
{
    private readonly Computer _computer;
    private float? _cpuTemperature;
    private float? _socTemperature;
    private float? _discreteGpuTemperature;

    public HardwareMonitor()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true
        };
        _computer.Open();
    }

    public float? CpuTemperature => _cpuTemperature;

    public float? SocTemperature => _socTemperature;

    public float? DiscreteGpuTemperature => _discreteGpuTemperature;

    public void Poll()
    {
        _cpuTemperature = null;
        _socTemperature = null;
        _discreteGpuTemperature = null;

        foreach (IHardware hardware in _computer.Hardware)
        {
            switch (hardware.HardwareType)
            {
                case HardwareType.Cpu:
                    hardware.Update();
                    _cpuTemperature = FindCpuTemperature(hardware);
                    break;

                case HardwareType.GpuNvidia:
                    hardware.Update();
                    _discreteGpuTemperature ??= FindGpuTemperature(hardware);
                    break;

                case HardwareType.GpuAmd or HardwareType.GpuIntel:
                    hardware.Update();
                    _socTemperature ??= FindGpuTemperature(hardware);
                    break;
            }
        }
    }

    private static float? FindCpuTemperature(IHardware cpu)
    {
        foreach (ISensor sensor in cpu.Sensors)
        {
            if (sensor.SensorType != SensorType.Temperature
                || sensor.Value is not float value
                || value <= 0f)
            {
                continue;
            }

            if (sensor.Name.Contains("Distance"))
                continue;

            if (sensor.Name.Contains("Package")
                || sensor.Name.Contains("Tctl")
                || sensor.Name.Contains("Tdie")
                || sensor.Name.Contains("Core Average"))
            {
                return value;
            }
        }

        return null;
    }

    private static float? FindGpuTemperature(IHardware gpu)
    {
        foreach (ISensor sensor in gpu.Sensors)
        {
            if (sensor.SensorType == SensorType.Temperature && sensor.Value is float value)
                return value;
        }

        return null;
    }

    public void Dispose() => _computer.Close();
}
