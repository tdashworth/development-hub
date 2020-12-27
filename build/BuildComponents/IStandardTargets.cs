using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Npm;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace BuildComponents
{
    public interface IStandardTargets : IBaseParametersAndPaths, INukeBuild
    {
        Target Clean => _ => _
            .Before(Restore)
            .Executes(() =>
            {
                SourceDirectory.GlobDirectories("**/bin", "**/obj", "**/dist", "**/out", "**/node_modules").ForEach(DeleteDirectory);
                TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            });

        Target Restore => _ => _
            .Executes(() =>
            {
                SourceDirectory.GlobFiles("**/WebResources/*/package.json").ForEach(packageFile =>
                {
                    NpmTasks.NpmInstall(s => s.SetProcessWorkingDirectory(packageFile.Parent));
                });

                DotNetRestore(s => s
                    .SetProjectFile(Solution));
            });

        Target Compile => _ => _
            .DependsOn(Restore)
            .Executes(() =>
            {
                SourceDirectory.GlobFiles("**/WebResources/*/package.json").ForEach(packageFile =>
                {
                    NpmTasks.NpmRun(s => s
                        .SetProcessWorkingDirectory(packageFile.Parent)
                        .SetCommand("build"));
                });

                DotNetBuild(s => s
                    .SetProjectFile(Solution)
                    .SetConfiguration(SolutionType == SolutionType.Unmanaged ? Configuration.Debug : Configuration.Release)
                    .SetDisableParallel(true)
                    .EnableNoRestore());
            });

        Target CompileTests => _ => _
            .Executes(() =>
            {
                DotNetBuild(s => s
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration.Test));
            });

    }
}
