using UnityEngine;
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Puzzle;
using NineMood.Data.Enums;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
#endif
namespace NineMood.UI
{
    // Explicit opt-in local QA in Development Player only. Release builds ignore the flag.
    public class PrototypePlayerSmoke : MonoBehaviour
    {
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
        static string output;
        static bool hadBest;static int originalBest;
        const string BestKey="NyanLink_BestScore";
        readonly List<string> errors=new List<string>();
        readonly List<string> passed=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Configure()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-nyanSmoke");
            if(index<0 || index+1>=args.Length)return;
            hadBest=PlayerPrefs.HasKey(BestKey);originalBest=PlayerPrefs.GetInt(BestKey,0);
            output=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(output);
            SaveManager.TestDirectoryOverride=Path.Combine(output,"isolated-save");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {if(output!=null) {var go=new GameObject("Explicit development smoke");DontDestroyOnLoad(go);go.AddComponent<PrototypePlayerSmoke>();}}
        void Start() {Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Application.logMessageReceived+=Log;StartCoroutine(Guard());}
        void Log(string text,string trace,LogType type)
        {if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(text+"\n"+trace);}
        void Check(bool valid,string text) {if(!valid)throw new Exception(text);passed.Add("PASS "+text);}
        IEnumerator Guard()
        {
            var run=Run();bool more=true;
            while(more)
            {
                object current=null;Exception failure=null;
                try {more=run.MoveNext();if(more)current=run.Current;}catch(Exception e){failure=e;}
                if(failure!=null){errors.Add(failure.ToString());break;}
                if(more)yield return current;
            }
            passed.AddRange(errors.Select(e=>"ERROR "+e));File.WriteAllLines(Path.Combine(output,"results.txt"),passed);
            if(hadBest)PlayerPrefs.SetInt(BestKey,originalBest);else PlayerPrefs.DeleteKey(BestKey);PlayerPrefs.Save();
            Application.Quit(errors.Count==0 ? 0 : 1);
        }
        IEnumerator Run()
        {
            Screen.SetResolution(540,960,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(2);
            Check(SceneManager.GetActiveScene().name=="Main","Native Mac Player starts Main");
            Check(AudioManager.Instance.sfxMatchShort!=null,"Player includes sound resources");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"01-main.png"));yield return new WaitForSecondsRealtime(.2f);
            SceneLoader.LoadGame();yield return new WaitForSecondsRealtime(2);
            var board=FindFirstObjectByType<PuzzleBoardManager>();Check(board!=null && board.GetTileCount()==39,"Player serialized tile references render39 cells");
            GameManager.Instance.StartGame();yield return new WaitForSecondsRealtime(.2f);
            Check(StaminaManager.Instance.MaxStamina==100,"Player runtime stats initialize");
            Check(Shader.Find("NyanLink/LineFlow2D")!=null,"Player includes selection shader");
            var cells=new[]{new Vector3Int(0,0,0),new Vector3Int(1,0,0)};
            foreach(var c in cells)board.SpawnTile(c,TileColor.Red,TileState.Normal);
            var camera=FindObjectsByType<Camera>(FindObjectsSortMode.None).Single(c=>c.name=="Camera_Puzzle");
            foreach(var device in InputSystem.devices.OfType<Mouse>().ToArray()) InputSystem.DisableDevice(device);
            var mouse=InputSystem.AddDevice<Mouse>("PlayerSmokeMouse");mouse.MakeCurrent();
            foreach(var c in cells)
            {
                var state=new MouseState{position=camera.WorldToScreenPoint(board.GetCellCenterWorld(c))};state=state.WithButton(MouseButton.Left,true);InputSystem.QueueStateEvent(mouse,state);yield return new WaitForSecondsRealtime(.08f);
            }
            Check(FindFirstObjectByType<TileInputHandler>().SelectedChain.Count==2,"Native Player input selects adjacent pair");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"02-selected-chain.png"));yield return new WaitForSecondsRealtime(.1f);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=camera.WorldToScreenPoint(board.GetCellCenterWorld(cells[1]))});yield return new WaitForSecondsRealtime(.5f);
            Check(ScoreManager.Instance.CurrentScore==20 && board.GetTileCount()==39,"Player release scores and refills");
            Check(AudioManager.Instance.PlayedSfxCount>0 && FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.isActiveAndEnabled)==1,"Player sound dispatched with single listener");
            Check(File.Exists(Path.Combine(SaveManager.StorageDirectory,"profile-v1.json")),"Player writes isolated profile");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"03-running-board.png"));yield return new WaitForSecondsRealtime(.2f);
            AudioManager.Instance.PlaySFX(AudioManager.Instance.sfxStageClear);yield return new WaitForSecondsRealtime(.06f);
            var samples=new float[256];AudioListener.GetOutputData(samples,0);
            passed.Add("AUDIO sourcePlaying="+AudioManager.Instance.GetComponents<AudioSource>()[0].isPlaying+" listenerVolume="+AudioListener.volume+" peak="+samples.Max(v=>Mathf.Abs(v)));
            InputSystem.RemoveDevice(mouse);Check(errors.Count==0,"Native Player has no runtime errors");
        }
#endif
    }
}
