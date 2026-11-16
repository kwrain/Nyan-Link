using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Debug-signed APK for physical Android playtests; separate from a store app.</summary>
public static class NyanLinkAndroidBuild
{
    public const string PackageId = "kr.fairstudios.nyanlink.prototype";

    [MenuItem("Nyan Link/Build Android Test APK")]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Install Android Build Support for " + Application.unityVersion);
        string path = Environment.GetEnvironmentVariable("NYANLINK_ANDROID_PATH") ?? "Builds/Android/NyanLink-test.apk";
        string evidence = Environment.GetEnvironmentVariable("NYANLINK_ANDROID_OUTPUT") ?? "output/playtests/2026-11-16-android";
        Directory.CreateDirectory(evidence);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
        var target = NamedBuildTarget.Android;
        var id = PlayerSettings.GetApplicationIdentifier(target);
        var backend = PlayerSettings.GetScriptingBackend(target);
        var architectures = PlayerSettings.Android.targetArchitectures;
        var customKey = PlayerSettings.Android.useCustomKeystore;
        var bundle = EditorUserBuildSettings.buildAppBundle;
        var export = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        var textures = EditorUserBuildSettings.androidBuildSubtarget;
        var defaultApi = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
        var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
        var orientation = PlayerSettings.defaultInterfaceOrientation;
        try
        {
            PlayerSettings.SetApplicationIdentifier(target, PackageId);
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC2;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_NyanLink/Scene/Main.unity", "Assets/_NyanLink/Scene/Game.unity" },
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            File.WriteAllText(Path.Combine(evidence, "build-results.txt"),
                $"unity={Application.unityVersion}\nresult={report.summary.result}\nerrors={report.summary.totalErrors}\nwarnings={report.summary.totalWarnings}\nsize={report.summary.totalSize}\nduration={report.summary.totalTime}\npackage={PackageId}\narchitecture=ARM64\nbackend=IL2CPP\ngraphics=OpenGLES3\noutput={Path.GetFullPath(path)}\n");
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Android APK build failed: " + report.summary.result);
        }
        finally
        {
            PlayerSettings.SetApplicationIdentifier(target, id);
            PlayerSettings.SetScriptingBackend(target, backend);
            PlayerSettings.Android.targetArchitectures = architectures;
            PlayerSettings.Android.useCustomKeystore = customKey;
            PlayerSettings.defaultInterfaceOrientation = orientation;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, apis);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, defaultApi);
            EditorUserBuildSettings.androidBuildSubtarget = textures;
            EditorUserBuildSettings.buildAppBundle = bundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = export;
        }
    }
}
