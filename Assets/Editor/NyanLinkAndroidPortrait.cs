#if UNITY_ANDROID
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;

/// <summary>Apply to every Android build, including builds made outside the APK helper.</summary>
public sealed class NyanLinkAndroidPortrait : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifest = Path.Combine(path, "src/main/AndroidManifest.xml");
        var document = XDocument.Load(manifest);
        var android = (XNamespace)"http://schemas.android.com/apk/res/android";
        var application = document.Root?.Element("application")
            ?? throw new InvalidOperationException("Android application manifest missing");
        var activities = application.Elements("activity").Where(a =>
            (string)a.Attribute(android + "name") == "com.unity3d.player.UnityPlayerGameActivity" ||
            (string)a.Attribute(android + "name") == "com.unity3d.player.UnityPlayerActivity").ToArray();
        if (activities.Length == 0) throw new InvalidOperationException("Unity game activity missing");
        foreach (var activity in activities)
        {
            activity.SetAttributeValue(android + "screenOrientation", "portrait");
            activity.SetAttributeValue(android + "resizeableActivity", "false");
        }
        // Android's documented opt-outs from manufacturer orientation compatibility overrides.
        // Preserve Unity metadata, launcher intent filters, permissions and third-party activities.
        foreach (string name in new[] {
            "android.window.PROPERTY_COMPAT_ALLOW_ORIENTATION_OVERRIDE",
            "android.window.PROPERTY_COMPAT_ALLOW_DISPLAY_ORIENTATION_OVERRIDE" })
        {
            var property = application.Elements("property").SingleOrDefault(p => (string)p.Attribute(android + "name") == name);
            if (property == null)
            {
                property = new XElement("property", new XAttribute(android + "name", name));
                application.Add(property);
            }
            property.SetAttributeValue(android + "value", "false");
        }
        document.Save(manifest);
    }

    // Verify against the last real Gradle manifest without rebuilding or replacing the installed APK.
    public static void Validate()
    {
        string output = "output/playtests/2026-11-16-android-portrait";
        string fixture = Path.Combine(output, "fixture/src/main");
        Directory.CreateDirectory(fixture);
        string source = "Library/Bee/Android/Prj/IL2CPP/Gradle/unityLibrary/src/main/AndroidManifest.xml";
        var original = XDocument.Load(source);
        var android = (XNamespace)"http://schemas.android.com/apk/res/android";
        original.Root.Element("application").Add(new XElement("activity",
            new XAttribute(android + "name", "validation.UnrelatedActivity"),
            new XAttribute(android + "screenOrientation", "unspecified")));
        string manifest = Path.Combine(fixture, "AndroidManifest.xml");
        original.Save(manifest);
        var processor = new NyanLinkAndroidPortrait();
        processor.OnPostGenerateGradleAndroidProject(Path.Combine(output, "fixture"));
        var result = XDocument.Load(manifest);
        var app = result.Root.Element("application");
        var activity = app.Elements("activity").Single(a => (string)a.Attribute(android + "name") == "com.unity3d.player.UnityPlayerGameActivity");
        if ((string)activity.Attribute(android + "screenOrientation") != "portrait" ||
            (string)activity.Attribute(android + "resizeableActivity") != "false" ||
            app.Elements("property").Count(p => ((string)p.Attribute(android + "name")).StartsWith("android.window.PROPERTY_COMPAT_ALLOW_") && (string)p.Attribute(android + "value") == "false") != 2 ||
            (string)app.Elements("activity").Single(a => (string)a.Attribute(android + "name") == "validation.UnrelatedActivity").Attribute(android + "screenOrientation") != "unspecified" ||
            !original.Root.Elements("uses-permission").Select(e => e.ToString()).SequenceEqual(result.Root.Elements("uses-permission").Select(e => e.ToString())) ||
            !XNode.DeepEquals(original.Root.Element("application").Elements("activity").First().Element("intent-filter"), activity.Element("intent-filter")))
            throw new Exception("Portrait manifest validation failed");
        string first = File.ReadAllText(manifest);
        processor.OnPostGenerateGradleAndroidProject(Path.Combine(output, "fixture"));
        if (first != File.ReadAllText(manifest)) throw new Exception("Manifest update is not idempotent");
        File.WriteAllText(Path.Combine(output, "results.txt"), "PASS actual previous Gradle manifest copied as fixture\nPASS portrait and non-resizable Unity activity\nPASS two manufacturer orientation opt-outs\nPASS unrelated activity and existing permissions/launcher intent preserved\nPASS repeated callback produces identical manifest\nNOT TESTED new APK manifest merge and physical tablet rotation; next build required\n");
    }
}
#endif
