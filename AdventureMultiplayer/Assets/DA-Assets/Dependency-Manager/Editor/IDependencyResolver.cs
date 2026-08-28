namespace DA_Assets.DM
{

    public interface IDependencyResolver
    {

        DetectionStrategy Source { get; }

        ResolutionResult Resolve(DependencyItem item);
    }
}
