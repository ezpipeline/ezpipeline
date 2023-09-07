using PipelineTools;
using Xunit;

namespace ezpipeline;

public class PipelineUtilsTests
{
    [Theory]
    [InlineData("C:\\SomeFolder\\..\\", "C:\\")]
    [InlineData("C:\\SomeFolder\\/../", "C:\\")]
    public void ResolvePathWithUpDir(string input, string expected)
    {
        var res = PipelineUtils.ResolvePath(input);
        Assert.Equal(expected, res);
    }
}