using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using static Nuke.Common.ValueInjection.ValueInjectionUtility;

namespace BuildComponents
{
    public interface IBaseParametersAndPaths : INukeBuild
    {
        [Solution]
        Solution Solution => TryGetValue(() => Solution);

        [Parameter("The solution that is the subject of the build target")]
        string DataverseSolution => TryGetValue(() => DataverseSolution);

        [Parameter("The type of solution")]
        SolutionType SolutionType => TryGetValue(() => SolutionType);

        AbsolutePath SourceDirectory => RootDirectory / "src";
        AbsolutePath TestsDirectory => RootDirectory / "tests";
        AbsolutePath SolutionsDirectory => SourceDirectory / "solutions";
        AbsolutePath SolutionDirectory => SolutionsDirectory / DataverseSolution;
    }
}
