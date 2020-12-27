using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.ValueInjection.ValueInjectionUtility;

namespace BuildComponents
{
    public interface ISpklTargets : IBaseParametersAndPaths
    {
        // using local executable rather than package due to an issue with spkl location CrmSvcUtil within SDK-style package cache
        [LocalExecutable(".tmp/spkl/spkl.exe")]
        Tool Spkl => TryGetValue(() => Spkl);


        Target CopySpklToTempFolder => _ => _
        .Executes(() =>
        {
            var spklDirectory = ((AbsolutePath)ToolPathResolver.GetPackageExecutable("spkl", "spkl.exe")).Parent;
            var targetSpklDirectory = TemporaryDirectory / "spkl";
            DeleteDirectory(targetSpklDirectory);
            CopyDirectoryRecursively(spklDirectory, targetSpklDirectory);

            var coreToolsDirectory = ((AbsolutePath)ToolPathResolver.GetPackageExecutable("Microsoft.CrmSdk.CoreTools", "CrmSvcUtil.exe")).Parent;
            var targetCoreToolsDirectory = TemporaryDirectory / "coretools";
            DeleteDirectory(targetCoreToolsDirectory);
            CopyDirectoryRecursively(coreToolsDirectory, targetCoreToolsDirectory);
        });

        Target DeployPlugins => _ => _
            .DependsOn(CopySpklToTempFolder)
            .Executes(() =>
            {
                Spkl($"plugins { SolutionDirectory / "spkl.json" }");
            });

        Target DeployWorkflowActivities => _ => _
            .DependsOn(CopySpklToTempFolder)
            .Executes(() =>
            {
                Spkl($"workflow { SolutionDirectory / "spkl.json" }");
            });

        Target GenerateModel => _ => _
            .DependsOn(CopySpklToTempFolder)
            .Executes(() =>
            {
                Spkl($"earlybound { SolutionDirectory / "spkl.json" }");
            });
    }
}
