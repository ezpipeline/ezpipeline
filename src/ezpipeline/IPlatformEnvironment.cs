namespace PipelineTools;

public interface IPlatformEnvironment
{
    void WriteLine(string message);
    void WriteErrorLine(string message);
    PlatformIdentifier GetPlatformId();
    void SetEnvironmentVariable(string envName, string value);
    string? GetEnvironmentVariable(string envName);
}