using System.Text;

namespace NTComponents.Tests.Form.TnTInputFile;

public class Buffer_Tests {

    [Fact]
    public async Task AppendToFileAsync_WritesOnlyTheReportedBytesAndPreservesExistingContent() {
        var path = Path.Combine(Path.GetTempPath(), $"tnt-input-file-buffer-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(path, "before-", Xunit.TestContext.Current.CancellationToken);
        var buffer = new global::NTComponents.TnTInputFileBuffer(Encoding.UTF8.GetBytes("payload-unused"), 7);

        try {
            await buffer.AppendToFileAsync(new FileInfo(path));

            buffer.BytesRead.Should().Be(7);
            Encoding.UTF8.GetString(buffer.Data).Should().Be("payload-unused");
            (await File.ReadAllTextAsync(path, Xunit.TestContext.Current.CancellationToken)).Should().Be("before-payload");
        }
        finally {
            File.Delete(path);
        }
    }
}
