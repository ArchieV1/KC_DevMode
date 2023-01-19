using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using UnityEngine;

namespace KC_DevMode
{
    public class DevMode : MonoBehaviour
    {
        // https://answers.unity.com/questions/508268/i-want-to-create-an-in-game-log-which-will-print-a.html
        public static KCModHelper Helper;
        public static DevMode inst;
        
        // ~\steamapps\common\Kingdoms and Castles\KingdomsAndCastles_Data\mods\log.txt
        // Created by ModCompiler.Log()
        // Or maybe KCModHelper.LogModEvent()
        // Will finish running before this runs. Can find history?
        
        // Console.Out.WriteLine() definitely writes here
        public List<string> LaunchLogs = new List<string>(); 
        
        // C:\Users\[UserName]\AppData\LocalLow\LionShield\Kingdoms and Castles\Player.log
        // ~/Library/Logs/Unity/Player.log
        // ~/.config/unity3d/CompanyName/ProductName/Player.log
        // Created by: Debug.Log()
        public List<string> RuntimeLogs = new List<string>();
        
        // ~\steamapps\common\Kingdoms and Castles\KingdomsAndCastles_Data\mods\MOD_NAME\output.txt
        // ~\steamapps\workshop\content\569480\MOD_ID\output.txt
        // Created by: KCModHelper.Log()
        public Dictionary<string, List<string>> ModLogs = new Dictionary<string, List<string>>();
        
        // Where does console.writeline come in???

        public List<string> Eventlog = new List<string>();
        private int maxLines = 50;
        private string GuiText = "";
        
        public DevMode()
        {
            inst = this;
        }
        
        public void Start()
        {
            MainMenuMode.topLevelUICanvas;
            string test;
        }

        /// <summary>
        /// Runs before scene loads
        /// Sets up modHelper and patches Harmony
        /// </summary>
        /// <param name="helper">The helper injected by KaC upon compilation</param>
        // ReSharper disable once ParameterHidesMember
        public void Preload(KCModHelper helper)
        {
            Helper = helper;
            var harmony = HarmonyInstance.Create("DevMode");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    
        /// <summary>
        /// Runs after the scene has loaded
        /// </summary>
        /// <param name="helper">The helper injected by KaC upon compilation</param>
        // ReSharper disable once ParameterHidesMember
        public void SceneLoaded(KCModHelper helper)
        {
        }

        /// <summary>
        /// Runs every frame
        /// </summary>
        public void Update()
        {
        }
        public Vector2 scrollPosition;
        
        private void OnGUI()
        {
            float height = Screen.height * 1/5f;
            float width = Screen.width * 1/2f;
            float ypos = Screen.height * 0/3f;
            float xpos = Screen.width * 1/3f;

            GUIStyle baseStyle = new GUIStyle();
            baseStyle.normal.textColor = Color.black;
            baseStyle.onHover.background = Texture2D.grayTexture;

            Rect scrollViewRect = new Rect(xpos, ypos, width, height);
            Rect viewRect = new Rect(0, 0, scrollViewRect.width-16, Eventlog.Count*25f + 50f);
            scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, viewRect, false, true);
            
            for (int x=0; x < Eventlog.Count;  x++)
            {
                string message = Eventlog[x];
                float textHeight = 25f;
                
                GUI.Label(new Rect(4f, textHeight * x, width-4F, textHeight), message, baseStyle);
            }
            
            GUI.EndScrollView();
        }
 
        public void AddEvent(string modName, string msg)
        {
            Eventlog.Add($"[{modName.Substring(0, Math.Min(15, modName.Length)), -15} | {DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}] {msg.Substring(0, Math.Min(40, msg.Length)), -40}");
            
            GuiText = "";
 
            foreach (string logEvent in Eventlog.GetRange(0, Math.Min(maxLines, Eventlog.Count)))
            {
                GuiText += logEvent;
                GuiText += "\n";
            }
        }
    }
    
    [HarmonyPatch(typeof(KCModHelper), nameof(KCModHelper.Log))]
    class Patch_KCModHelper
    {
        static void Postfix(string msg, KCModHelper __instance)
        {
            // Find name of mod somewhere in WorkshopUI
            string modPath = __instance.modPath;
            string modName = modPath.Split('/', '\\').Last();
            DevMode.inst.AddEvent(modName, msg);
        }
    }

    [HarmonyPatch(typeof(Debug), nameof(Debug.Log))]
    class Patch_Debug
    {
        static void Postfix(string msg)
        {
            DevMode.inst.AddEvent("DEBUG", msg);
            // DevMode.inst.RuntimeLogs.Add(msg);
        }
    }
    
    
    public static class Tools{
        /// <summary>
        /// Get value of a private field from an Instance
        /// </summary>
        /// <param name="instance">The instance that contains the private field</param>
        /// <param name="fieldName">The private field name</param>
        /// <param name="fieldIsStatic">Is the field static</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Thrown when fieldName is not found in instance</exception>
        public static object GetPrivateField(object instance, string fieldName, bool fieldIsStatic = false)
        {
            string exceptionString =
                $"{fieldName} does not correspond to a private {(fieldIsStatic ? "static" : "instance")} field in {instance}";
            object result;
            try
            {
                Type type = instance.GetType();

                FieldInfo fieldInfo = fieldIsStatic
                    ? type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)
                    : type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

                if (fieldInfo == null) throw new ArgumentException(exceptionString);

                result = fieldInfo.GetValue(instance);
            }
            catch (Exception e)
            {
                Debug.Log("GetPrivateField failed");
                Debug.Log(e);
                throw new ArgumentException(exceptionString);
            }

            return result;
        }
    }
}