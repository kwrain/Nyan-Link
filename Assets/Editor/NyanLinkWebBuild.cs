using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Repeatable release build for the /nyan-link/ static hosting directory.</summary>
public static class NyanLinkWebBuild
{
    [MenuItem("Nyan Link/Build Web Test")]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new InvalidOperationException("Install Web Build Support for Unity " + Application.unityVersion);

        string output = Environment.GetEnvironmentVariable("NYANLINK_WEB_PATH") ?? "Builds/Web/nyan-link";
        string evidence = Environment.GetEnvironmentVariable("NYANLINK_WEB_OUTPUT") ?? "output/playtests/2026-11-16-web";
        Directory.CreateDirectory(evidence);
        // Restore the editor's existing settings even if the build fails.
        var template = PlayerSettings.WebGL.template;
        var compression = PlayerSettings.WebGL.compressionFormat;
        var fallback = PlayerSettings.WebGL.decompressionFallback;
        var hashes = PlayerSettings.WebGL.nameFilesAsHashes;
        var caching = PlayerSettings.WebGL.dataCaching;
        var initialMemory = PlayerSettings.WebGL.initialMemorySize;
        var maximumMemory = PlayerSettings.WebGL.maximumMemorySize;
        var threads = PlayerSettings.WebGL.threadsSupport;
        var wasm2023 = PlayerSettings.WebGL.wasm2023;
        var exceptions = PlayerSettings.WebGL.exceptionSupport;
        var textureTarget = EditorUserBuildSettings.webGLBuildSubtarget;
        try
        {
            PlayerSettings.WebGL.template = "PROJECT:NyanLink";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 1024;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.wasm2023 = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.Generic;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_NyanLink/Scene/Main.unity", "Assets/_NyanLink/Scene/Game.unity" },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            File.WriteAllText(Path.Combine(evidence, "build-results.txt"),
                $"unity={Application.unityVersion}\nresult={report.summary.result}\nerrors={report.summary.totalErrors}\nwarnings={report.summary.totalWarnings}\nsize={report.summary.totalSize}\nduration={report.summary.totalTime}\noutput={Path.GetFullPath(output)}\n");
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Web build failed: " + report.summary.result);
        }
        finally
        {
            EditorUserBuildSettings.webGLBuildSubtarget = textureTarget;
            PlayerSettings.WebGL.template = template;
            PlayerSettings.WebGL.compressionFormat = compression;
            PlayerSettings.WebGL.decompressionFallback = fallback;
            PlayerSettings.WebGL.nameFilesAsHashes = hashes;
            PlayerSettings.WebGL.dataCaching = caching;
            PlayerSettings.WebGL.initialMemorySize = initialMemory;
            PlayerSettings.WebGL.maximumMemorySize = maximumMemory;
            PlayerSettings.WebGL.threadsSupport = threads;
            PlayerSettings.WebGL.wasm2023 = wasm2023;
            PlayerSettings.WebGL.exceptionSupport = exceptions;
        }
    }
}
