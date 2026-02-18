            var validatedPath = ValidatePathWithinDirectory(fs.AppDataDirectory, filesSubDirectory);

            //TODO: This code searches for a string, would me more efficient as a direct search
            await Task.Run(() =>
            {
                foreach (string filePath in Directory.EnumerateFiles(validatedPath, $"*.{fileExtensionNoPeriod}"))
                {
                    var fileNameNoPath = string.Empty;
                    if (filePath != null)
                    {
                        var lastSlash = filePath.LastIndexOf("/");

                        if (lastSlash > -1)
                        {
                            fileNameNoPath = filePath.Substring(lastSlash + 1, filePath.Length - (lastSlash + 1));
                        }

                        if (fileNameNoPath.StartsWith(fileNamePrefixFilter))
                            fileList.Add(File.ReadAllText(filePath));
