using GTranslate.Translators;

namespace ResxTranslate.Utility
{
    public class TranslateProvider
    {
        public static string GetResXLanguageFromPath(string path)
        {
            var splittedFilename = Path.GetFileName(path).Split(".");
            return splittedFilename.Length < 3 ? string.Empty : splittedFilename[^2];
        }

        public static void TranslateResX(ResXContainer mainContainer, ResXContainer subContainer, TranslateResXConfiguration configuration)
        {
            // Setup the translator
            var translator = new GoogleTranslator2();

            if (!translator.IsLanguageSupported(mainContainer.Language))
            {
                configuration.Progress?.Report($"Main Language '{mainContainer.Language}' not supported.");
                return;
            }

            if (!translator.IsLanguageSupported(subContainer.Language))
            {
                configuration.Progress?.Report($"Sub Language '{subContainer.Language}' not supported.");
                return;
            }

            // Evaluate differences in the two .resx file
            // Keys that need to be removed from subContainer
            var ToRemove = subContainer.Keys.Except(mainContainer.Keys).ToList();
            foreach (var key in ToRemove)
                subContainer.TryRemove(key, out var removed);
            configuration.Progress?.Report($"Removed {ToRemove.Count} traslations.");

            // Keys that need to be added to subContainer
            var ToAdd = new List<string>();
            foreach (var kvp in mainContainer)
            {
                bool contains = subContainer.TryGetValue(kvp.Key, out var res);

                // If the parameter -force in the configuration is not active and the sub resx file
                // contains a non-empty value for that entry: Skip the current key.
                if (!configuration.ForceTranslate && contains && !string.IsNullOrEmpty(res.Value))
                    continue;

                // If the entry in the main resx file contains the comment SKIP and the parameter -skip
                // in the configuration is active: Skip the current key.
                if (configuration.SkipComment && kvp.Value.Comment.Equals("SKIP"))
                    continue;

                ToAdd.Add(kvp.Key);
            }
            configuration.Progress?.Report($"Found {ToAdd.Count} missing translations.");

            // Translate each value in parallel
            var tasks = new List<Task>();
            foreach (var key in ToAdd)
            {
                tasks.Add(Task.Run(async () =>
                {
                    // Value from the main resx file
                    ResXData data = mainContainer[key];

                    // Ask for the translation
                    var result = await translator.TranslateAsync(data.Value, subContainer.Language, mainContainer.Language);

                    // If valid add it in the sub resx file
                    if (result != null && !string.IsNullOrEmpty(result.Translation))
                    {
                        subContainer[key] = new ResXData
                        {
                            Name = key,
                            Value = result.Translation,
                            Comment = data.Comment,
                            Type = data.Type,
                            Space = data.Space
                        };
                        configuration.Progress?.Report($"Added translation for key '{key}' : '{data.Value}' -> '{result.Translation}'");
                    }
                    else
                    {
                        configuration.Progress?.Report($"Translation failed for key '{key}'");
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());

            // Save changes
            if (ToAdd.Count > 0 || ToRemove.Count > 0)
            {
                subContainer.Save();
            }
        }
    }
}
