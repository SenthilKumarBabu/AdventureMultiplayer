namespace DA_Assets.DM
{

    public readonly struct ResolutionResult
    {
        public bool Found { get; }
        public string DisplayPath { get; }
        public DetectionStrategy MatchedSource { get; }

        public ResolutionResult(bool found, string displayPath, DetectionStrategy matchedSource)
        {
            Found = found;
            DisplayPath = displayPath;
            MatchedSource = matchedSource;
        }

        public static ResolutionResult NotFound { get; } =
            new ResolutionResult(false, null, DetectionStrategy.None);

        public static ResolutionResult Match(string displayPath, DetectionStrategy source) =>
            new ResolutionResult(true, displayPath, source);
    }
}
