#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DA_Assets.UCC
{
    public class SpriteDuplicateFinderRequest
    {
        private readonly TaskCompletionSource<List<List<SpriteUsageFinder.UsedSprite>>> _completion = new();
        private readonly Dictionary<string, bool> _defaultSelectionByPath;

        public SpriteDuplicateFinderRequest(List<List<SpriteUsageFinder.UsedSprite>> groups)
        {
            Groups = groups;
            _defaultSelectionByPath = groups
                .SelectMany(g => g)
                .GroupBy(s => s.Path, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Selected, StringComparer.OrdinalIgnoreCase);
        }

        public List<List<SpriteUsageFinder.UsedSprite>> Groups { get; }
        public bool IsResolved => _completion.Task.IsCompleted;
        public Task<List<List<SpriteUsageFinder.UsedSprite>>> Completion => _completion.Task;

        public event Action Resolved;

        public void ResolveCurrent()
        {
            if (_completion.TrySetResult(Groups))
            {
                Resolved?.Invoke();
            }
        }

        public void ResolveDefault()
        {
            foreach (List<SpriteUsageFinder.UsedSprite> group in Groups)
            {
                foreach (SpriteUsageFinder.UsedSprite sprite in group)
                {
                    if (_defaultSelectionByPath.TryGetValue(sprite.Path, out bool selected))
                    {
                        sprite.Selected = selected;
                    }
                }
            }

            ResolveCurrent();
        }

        public void ResolveKeepingAllDuplicates()
        {
            foreach (List<SpriteUsageFinder.UsedSprite> group in Groups)
            {
                foreach (SpriteUsageFinder.UsedSprite sprite in group)
                {
                    sprite.Selected = false;
                }
            }

            ResolveCurrent();
        }
    }
}
#endif