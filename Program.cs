try
{
    // Arguments check
    if (args.Length < 1)
        throw new ArgumentException("You need to provide the path to the main resource file as argument");

    TranslateResXConfiguration configuration = new TranslateResXConfiguration()
    {
        Progress = new Progress<string>(str =>
        {
            Console.WriteLine(str);
        })
    };

    // Opening main resource file
    string filepath = Path.GetRelativePath(Directory.GetCurrentDirectory(), args[0]);
    if (!File.Exists(filepath))
        throw new ArgumentException($"File '{filepath}' does not exist.");

    // Evaluate arguments
    for (int i = 1; i < args.Length; i++)
    {
        if (args[i].Equals("-f") || args[i].Equals("-force"))
            configuration.ForceTranslate = true;
        else if (args[i].Equals("-s") || args[i].Equals("-skip"))
            configuration.SkipComment = true;
        else
            throw new ArgumentException($"Invalid parameter '{args[i]}'.");
    }

    // Try to extract language from file name
    string mainLang = TranslateProvider.GetResXLanguageFromPath(filepath);
    if (string.IsNullOrEmpty(mainLang))
    {
        Console.WriteLine($"Language not found for main resx '{Path.GetFileName(filepath)}'. Using default '{ResXContainer.DefaultLanguage}'.");
        mainLang = ResXContainer.DefaultLanguage;
    }

    ResXContainer mainContainer = new(filepath, mainLang);

    // Find subfiles with other languages
    string directory = Path.GetDirectoryName(filepath);
    string name = Path.GetFileNameWithoutExtension(filepath);
    foreach (string path in Directory.GetFiles(directory, $"{name}.*.resx"))
    {
        string filename = Path.GetFileName(path);
        if (!path.Equals(filepath))
        {
            var subLang = TranslateProvider.GetResXLanguageFromPath(path);
            
            if (string.IsNullOrEmpty(subLang))
            {
                Console.WriteLine($"Skipping file '{filename}', invalid file name (language not found).");
                continue;
            }

            Console.WriteLine($"Scouting file '{filename}' ...");
            ResXContainer subContainer = new(path, subLang);

            TranslateProvider.TranslateResX(mainContainer, subContainer, configuration);
        }
    }

}catch(Exception ex)
{
    Console.WriteLine($"{ex.GetType().Name} : {ex.Message}");
    Console.WriteLine($"Details : {ex.StackTrace}");
}