#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public class GrpcPostBuild
{
  [PostProcessBuild(1)]
  public static void OnPostProcessBuild(BuildTarget target, string path)
  {
    var projectPath = PBXProject.GetPBXProjectPath(path);
    var project = new PBXProject();
    project.ReadFromString(File.ReadAllText(projectPath));
#if UNITY_2019_3_OR_NEWER
    var targetGuid = project.GetUnityFrameworkTargetGuid();
#else
    var targetGuid = project.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif

    // libz.tbd for grpc ios build
    project.AddFrameworkToProject(targetGuid, "libz.tbd", false);

    // libgrpc_csharp_ext missing bitcode. as BITCODE expand binary size to 250MB.
    project.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");

    File.WriteAllText(projectPath, project.WriteToString());
  }
}
#endif
