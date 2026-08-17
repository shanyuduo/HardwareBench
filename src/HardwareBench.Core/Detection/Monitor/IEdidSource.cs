namespace HardwareBench.Core.Detection.Monitor;

public interface IEdidSource
{
    IEnumerable<(string InstancePath, byte[] Edid)> ReadAll();
}