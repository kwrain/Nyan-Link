using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class NyanLinkSession08Build
{
    public static void PrepareAudio()
    {
        foreach(var path in Directory.GetFiles("Assets/Resources/Audio/Prototype","*.wav"))
        {
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);
            var settings=importer.defaultSampleSettings;
            settings.loadType=UnityEngine.AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=UnityEngine.AudioCompressionFormat.PCM;
            settings.preloadAudioData=true;
            importer.defaultSampleSettings=settings;importer.forceToMono=true;
            importer.SaveAndReimport();
        }
    }
    public static void Build()
    {
        var options=new BuildPlayerOptions {
            scenes=new[]{"Assets/_NyanLink/Scene/Main.unity","Assets/_NyanLink/Scene/Game.unity"},
            locationPathName="Builds/Session08/NyanLink.app",target=BuildTarget.StandaloneOSX,
            options=BuildOptions.Development
        };
        var report=BuildPipeline.BuildPlayer(options);
        File.WriteAllText("output/playtests/2026-11-13-session08/build-results.txt",$"result={report.summary.result}\nerrors={report.summary.totalErrors}\nwarnings={report.summary.totalWarnings}\nsize={report.summary.totalSize}\nduration={report.summary.totalTime}\n");
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Native Player build failed");
    }
}
