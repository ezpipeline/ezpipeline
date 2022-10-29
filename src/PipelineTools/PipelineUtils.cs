using System.Text;

namespace PipelineTools;

public static class PipelineUtils
{
    public static Encoding UTF8 = new UTF8Encoding(false);

    public static Stream CreateFile(string fileName)
    {
        return File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
    }

    public static Stream OpenFile(string fileName)
    {
        return File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }
}