using DistantWorlds.IDE.Stride;
using FluentAssertions;
using static NUnit.Framework.Assert;

namespace Tests;

public class BundleBuilderTests {

    [Test, Order(0)]
    public void CreateBundleBuilder() {
        var builder = new BundleBuilder("test");
        Pass("No exception thrown during object construction.");
    }
    
    [Test, Order(1)]
    public void SingleFileBundle() {
        var builder = new BundleBuilder("test");
        var path = Path.Combine(Environment.CurrentDirectory, "TestContent.txt");
        builder.TryAdd(path, "TestContent").Should().BeTrue();
        var bundlePath = Path.Combine(Environment.CurrentDirectory, "Test.bundle");
        builder.Build(bundlePath);
        File.Exists(bundlePath);
    }

    

}