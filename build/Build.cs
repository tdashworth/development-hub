using BuildComponents;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild, IStandardTargets, ISolutionTargets, ISpklTargets
{

    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    Target Compile => _ => _.Inherit<IStandardTargets>(x => x.Compile);    
}
