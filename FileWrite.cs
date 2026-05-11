namespace MemberDocument.Send.To.Print.Services.Writer;

public class FileWriterService : IFileWriterService
{
    public void Write(string destinationPath, Stream content)
    {
        // Ensure the stream is at the beginning
        if (content.CanSeek)
            content.Position = 0;

        using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
        {
            content.CopyTo(fileStream);
        }
    }
}
