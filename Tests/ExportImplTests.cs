using DistantWorlds.IDE;
using FluentAssertions;

namespace Tests;

public class ExportImplTests {

    /// <see cref="Exports.GetGameDirectoryImpl"/>
    [Test]
    public unsafe void GetGameDirectoryImpl() {
        var expectedGameDir = Dw2Env.GameDirectory!;

        expectedGameDir.Should().NotBeNullOrWhiteSpace();

        var expectedResult = -(expectedGameDir.Length + 1); // null terminated

        var result = Exports.GetGameDirectoryImpl(null, 0);

        result.Should().Be(expectedResult);

        var needed = -result;

        var buffer = stackalloc char[needed];

        var written = Exports.GetGameDirectoryImpl(buffer, needed);

        written.Should().Be(needed);

        buffer[written - 1].Should().Be('\0');

        var bufferSpan = new ReadOnlySpan<char>(buffer, written).TrimEnd('\0');

        var actualGameDir = new string(bufferSpan);

        actualGameDir.Should().Be(expectedGameDir);
    }

    [Explicit, Test]
    public unsafe void GetUserChosenGameDirectoryImpl() {
        var expectedGameDir = Dw2Env.UserChosenGameDirectory!;

        expectedGameDir.Should().NotBeNullOrWhiteSpace();

        var expectedResult = -(expectedGameDir.Length + 1); // null terminated

        var result = Exports.GetUserChosenGameDirectoryImpl(null, 0);

        result.Should().Be(expectedResult);

        var needed = -result;

        var buffer = stackalloc char[needed];

        var written = Exports.GetUserChosenGameDirectoryImpl(buffer, needed);

        written.Should().Be(needed);

        buffer[written - 1].Should().Be('\0');

        var bufferSpan = new ReadOnlySpan<char>(buffer, written).TrimEnd('\0');

        var actualGameDir = new string(bufferSpan);

        actualGameDir.Should().Be(expectedGameDir);
    }

    [Explicit, Test]
    public void PromptForGameDirectoryTest() {
        Dw2Env.PromptForGameDirectory();
    }

}