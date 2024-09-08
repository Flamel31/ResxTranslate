namespace ResxTranslate.Models.Translate
{
    public class TranslateResXConfiguration
    {
        /// <summary>
        /// When this is true it ignores all the resources (in the main resx file) that are marked with "SKIP" in the comment. 
        /// -skip -s
        /// </summary>
        public bool SkipComment { get; set; }

        /// <summary>
        /// When this is true it translates all the non-empty resources (in the main resx file), even if there is already a translated entry (in the sub resx file).
        /// -force -f
        /// </summary>
        public bool ForceTranslate { get; set; }

        /// <summary>
        /// Define a IProgress object to keep track of the progress of the translated resx.
        /// </summary>
        public IProgress<string> Progress { get; set; }
    }
}
