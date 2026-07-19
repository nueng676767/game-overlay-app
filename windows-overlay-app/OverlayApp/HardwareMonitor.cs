using LibreHardwareMonitor.Hardware;

namespace GameOverlay;

/// <summary>
/// Reads real sensor values via LibreHardwareMonitorLib (open-source, MPL-2.0),
/// the same engine many free hardware-monitoring apps use. Reading most
/// temperature sensors requires the app to run as Administrator on Windows —
/// see README. Values default to null when a sensor isn't available on the
/// current hardware, so the UI can show "N/A" instead of a fabricated number.
/// </summary>
public sealed class HardwareMonitor : IDisposable
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _visitor = new();

    public double? CpuLoad { get; private set; }
    public double? CpuTemp { get; private set; }
    public double? GpuLoad { get; private set; }
    public double? GpuTemp { get; private set; }
    public double? RamUsedGb { get; private set; }
    public double? RamTotalGb { get; private set; }
    public double? VramUsedGb { get; private set; }

    public HardwareMonitor()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
        };
        _computer.Open();
    }

    public void Refresh()
    {
        _computer.Accept(_visitor);

        CpuLoad = null; CpuTemp = null; GpuLoad = null; GpuTemp = null;
        RamUsedGb = null; RamTotalGb = null; VramUsedGb = null;

        foreach (var hw in _computer.Hardware)
        {
            ReadHardware(hw);
            foreach (var sub in hw.SubHardware)
                ReadHardware(sub);
        }
    }

    private void ReadHardware(IHardware hw)
    {
        switch (hw.HardwareType)
        {
            case HardwareType.Cpu:
                foreach (var s in hw.Sensors)
                {
                    if (s.SensorType == SensorType.Load && s.Name.Contains("Total"))
                        CpuLoad = s.Value;
                    if (s.SensorType == SensorType.Temperature &&
                        (s.Name.Contains("Package") || s.Name.Contains("Average")))
                        CpuTemp = s.Value;
                }
                break;

            case HardwareType.GpuNvidia:
            case HardwareType.GpuAmd:
            case HardwareType.GpuIntel:
                foreach (var s in hw.Sensors)
                {
                    if (s.SensorType == SensorType.Load && s.Name.Contains("Core"))
                        GpuLoad = s.Value;
                    if (s.SensorType == SensorType.Temperature && s.Name.Contains("Core"))
                        GpuTemp = s.Value;
                    if (s.SensorType == SensorType.SmallData && s.Name.Contains("Memory Used"))
                        VramUsedGb = s.Value / 1024.0; // MB -> GB
                }
                break;

            case HardwareType.Memory:
                foreach (var s in hw.Sensors)
                {
                    if (s.SensorType == SensorType.Data && s.Name.Contains("Used"))
                        RamUsedGb = s.Value;
                    if (s.SensorType == SensorType.Data && s.Name.Contains("Available"))
                        RamTotalGb = (RamUsedGb ?? 0) + (s.Value);
                }
                break;
        }
    }

    public void Dispose() => _computer.Close();

    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware) sub.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}
