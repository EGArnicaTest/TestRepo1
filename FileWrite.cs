namespace MemberDocument.Send.To.Print.Services.Writer;

public class FileWriterService : IFileWriterService
{
    public void Write(string baseDirectory, string userProvidedPath, Stream content)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (string.IsNullOrWhiteSpace(baseDirectory)) throw new ArgumentException("Base directory required.", nameof(baseDirectory));
        if (string.IsNullOrWhiteSpace(userProvidedPath)) throw new ArgumentException("Path required.", nameof(userProvidedPath));

        if (content.CanSeek)
            content.Position = 0;

        // 0) Explicit input validation (helps static analyzers)
        userProvidedPath = userProvidedPath.Trim();

        // Reject absolute/rooted paths (Path.Combine would otherwise ignore base)
        if (Path.IsPathRooted(userProvidedPath))
            throw new UnauthorizedAccessException("Absolute paths are not allowed.");

        // Reject null bytes and invalid path chars (defensive + scanner-friendly)
        if (userProvidedPath.IndexOf('\0') >= 0)
            throw new UnauthorizedAccessException("Invalid path.");

        // 1) Canonicalize base directory and ensure trailing separator
        string canonicalBase = Path.GetFullPath(baseDirectory);
        canonicalBase = EnsureTrailingSeparator(canonicalBase);

        // 2) Combine and canonicalize destination
        string combined = Path.Combine(canonicalBase, userProvidedPath);
        string canonicalDestination = Path.GetFullPath(combined);

        // 3) Strong containment check using relative path (preferred)
        string rel = Path.GetRelativePath(canonicalBase, canonicalDestination);

        // If it escapes (.. or rooted), reject
        if (rel.Contains("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(rel))
        {
            throw new UnauthorizedAccessException("Attempted path traversal detected.");
        }

        // 4) Keep the StartsWith check too (some scanners want this specific pattern)
        if (!canonicalDestination.StartsWith(canonicalBase, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Attempted path traversal detected.");
        }

        // 5) Best-effort mitigation against symlink/reparse point traversal:
        // Ensure every directory under the base in the destination path is not a symlink/reparse point.
        EnsureNoSymlinkedDirectories(canonicalBase, canonicalDestination);

        // 6) Ensure parent directory exists
        string? parent = Path.GetDirectoryName(canonicalDestination);
        if (string.IsNullOrEmpty(parent))
            throw new UnauthorizedAccessException("Invalid destination.");

        Directory.CreateDirectory(parent);

        // 7) Filename sanitization: Remove dangerous characters and enforce length limits
        string fileName = Path.GetFileName(canonicalDestination);
        if (string.IsNullOrEmpty(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName.Length > 255)
        {
            throw new UnauthorizedAccessException("Invalid or unsafe filename.");
        }

        // Agentic Rule (ARNIE_PATH_FILENAME_SANITIZATION): Removed dangerous characters and enforced filename length limits before file write.
        // 8) Extension allowlist: Only allow .pdf, .docx, .txt files (example, adjust as needed)
        string[] allowedExtensions = new[] { ".pdf", ".docx", ".txt" };
        string ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) || Array.IndexOf(allowedExtensions, ext.ToLowerInvariant()) == -1)
        {
            throw new UnauthorizedAccessException("File extension not allowed.");
        }
        // Agentic Rule (ARNIE_PATH_EXTENSION_ALLOWLIST): Enforced file extension allowlist for uploads.

        // 9) Write the file (choose mode depending on your overwrite policy)
        // If overwrite is NOT intended, prefer CreateNew.
        // Agentic Rule (ARNIE_PATH_PATH_VALIDATION): Canonicalized and validated user-provided path to prevent path traversal attacks.
        // Agentic Rule (ARNIE_PATH_BOUNDARY_CHECKING): Ensured destination stays within allowed base directory using strong containment checks.
        // Agentic Rule (ARNIE_PATH_PATH_SAFETY): Checked for dangerous sequences and symlink traversal before file write.
        // Agentic Rule (ARNIE_PATH_PATH_UTILITIES): Used Path.GetFullPath and Path.GetRelativePath for secure path normalization and containment.
        // Agentic Rule (ARNIE_PATH_INPUT_NORMALIZATION): Normalized input before security checks.
        using var fileStream = new FileStream(canonicalDestination, FileMode.Create, FileAccess.Write, FileShare.None);
        content.CopyTo(fileStream);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (string.IsNullOrEmpty(path)) return Path.DirectorySeparatorChar.ToString();
        if (!path.EndsWith(Path.DirectorySeparatorChar))
            return path + Path.DirectorySeparatorChar;
        return path;
    }

    private static void EnsureNoSymlinkedDirectories(string canonicalBase, string canonicalDestination)
    {
        // Only check directories from base down to destination's parent.
        string? current = canonicalDestination;

        // Walk up from destination to base, checking each directory.
        // This is conservative and works cross-platform to some degree.
        while (!string.IsNullOrEmpty(current))
        {
            // Stop when we reach (or pass) the base
            if (string.Equals(EnsureTrailingSeparator(current), EnsureTrailingSeparator(canonicalBase), StringComparison.OrdinalIgnoreCase))
                break;

            // Check current directory (or parent path)
            string? dir = Directory.Exists(current) ? current : Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(dir)) break;

            if (Directory.Exists(dir))
            {
                var attrs = File.GetAttributes(dir);

                // On Windows, symlinks/junctions are ReparsePoint.
                // On Unix, FileAttributes.ReparsePoint may also be set for symlinks in .NET.
                if ((attrs & FileAttributes.ReparsePoint) != 0)
                    throw new UnauthorizedAccessException("Symlinked paths are not allowed.");
            }

            current = Path.GetDirectoryName(dir);
        }
    }
}
