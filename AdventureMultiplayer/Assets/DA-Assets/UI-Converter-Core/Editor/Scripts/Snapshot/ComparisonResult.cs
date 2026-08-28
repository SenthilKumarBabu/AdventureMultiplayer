using System.Collections.Generic;

namespace DA_Assets.UCC.Snapshot
{
    public enum EntryStatus
    {
        Match,
        Diff,
        Missing,
        Extra
    }

    public struct ComparisonReport
    {
        public List<GameObjectEntry> RootEntries;
        public int TotalDeviations;
        public int TotalComponents;
    }

    public struct GameObjectEntry
    {
        public string Name;
        public string RelativePath;
        public List<ComponentEntry> Components;
        public List<GameObjectEntry> Children;
        public int DeviationCount;
        public EntryStatus Status;
        public string FigmaJson;
    }

    public struct ComponentEntry
    {
        public string FileName;
        public EntryStatus Status;
        public string BaselineJson;
        public string SceneJson;
        public int DiffLineCount;
    }
}