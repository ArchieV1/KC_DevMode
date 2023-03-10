using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using UnityEngine;

namespace KC_DevMode
{
    public class DevMode_Logger : MonoBehaviour
    {
        public static DevMode_Logger Inst;
        /// <summary>
        /// [modName, message]
        /// </summary>
        public List<string[]> eventLog = new List<string[]>();
        public Vector2 scrollPosition;
        
        /// <summary>
        /// List of all built in colours
        /// **DOES NOT CONTAIN gray**
        /// </summary>
        public List<Color> colorList = new List<Color>()
        {
            Color.clear,
            Color.grey,

            Color.black,
            Color.blue,
            Color.green,
            Color.magenta,
            Color.red,
            Color.cyan,
            Color.white,
            Color.yellow
        };

        private float consoleHeight;
        private float consoleWidth;
        private float consoleYpos;
        private float consoleXpos;

        private float logMessageWidth;
        private float logModNameWidth;

        private float ScreenWidth = Screen.width;
        private float ScreenHeight = Screen.height;

        float textHeight = 18f;
        private int maxMessageLength = 125;
        private int maxModNameLength = 25;

        private GUIStyle BaseStyle = new GUIStyle();

        public Dictionary<Color, Texture2D> LogModNameBackgroundTex = new Dictionary<Color, Texture2D>();
        public Dictionary<Color, Texture2D> LogMessageBackgroundTex = new Dictionary<Color, Texture2D>();


        // ~\steamapps\common\Kingdoms and Castles\KingdomsAndCastles_Data\mods\log.txt
        // TODO catch these? would have to work backwards if possible
        // Created by Console.Out.WriteLine() 
        
        // C:\Users\[UserName]\AppData\LocalLow\LionShield\Kingdoms and Castles\Player.log
        // ~/Library/Logs/Unity/Player.log
        // ~/.config/unity3d/CompanyName/ProductName/Player.log
        // Created by: Debug.Log()

        // ~\steamapps\common\Kingdoms and Castles\KingdomsAndCastles_Data\mods\MOD_NAME\output.txt
        // ~\steamapps\workshop\content\569480\MOD_ID\output.txt
        // TODO find workshop mod name?
        // Created by: KCModHelper.Log()

        /// <summary>
        /// Updates consoleHeight/consoleWidth/consoleYpos/consoleXpos in case of screen size changing
        /// </summary>
        public void SetConsoleSizePos()
        {
            consoleHeight = Screen.height * 1/5f;
            consoleWidth  = Screen.width  * 2/4f;
            consoleYpos   = Screen.height * 0f;
            consoleXpos   = Screen.width  * 1/4f;

            const float modNamePerc = 0.15f;
            logModNameWidth = consoleWidth * modNamePerc;
            logMessageWidth = consoleWidth * (1f-modNamePerc);
        }
        
        public Texture2D Texture2DFromColor(Color color, float width, float height)
        {
            return Texture2DFromColor(color, (int) width, (int) height);
        }

        public void Generate_LogModNameBackgroundTex()
        {
            LogModNameBackgroundTex = new Dictionary<Color, Texture2D>();
            foreach (Color color in colorList)
            {
                LogModNameBackgroundTex.Add(color, Texture2DFromColor(color, logModNameWidth, textHeight));
            }
        }

        public void Generate_LogMessageBackgroundTex()
        {
            LogMessageBackgroundTex = new Dictionary<Color, Texture2D>();
            foreach (Color color in colorList)
            {
                LogMessageBackgroundTex.Add(color, Texture2DFromColor(color, logMessageWidth, textHeight));
            }
        }
        
        public static Texture2D Texture2DFromColor(Color color, int width, int height, float opacity=0.5f)
        {
            Texture2D texture2D = new Texture2D(width, height);
            Color[] fillColorArray = texture2D.GetPixels();
                
            for (int i = 0; i < fillColorArray.Length; ++i)
            {
                color.a = opacity;
                fillColorArray[i] = color;
            }

            texture2D.SetPixels(fillColorArray);
            return texture2D;
        }
        
        public DevMode_Logger()
        {
            Inst = this;
            BaseStyle.normal.textColor = Color.black; // Do not do background here as could change after game runs
            
            SetConsoleSizePos();
            Generate_LogMessageBackgroundTex();
            Generate_LogModNameBackgroundTex();
        }
        
        /// <summary>
        /// Runs before scene loads
        /// Sets up modHelper and patches Harmony
        /// </summary>
        /// <param name="helper">The helper injected by KaC upon compilation</param>
        // ReSharper disable once ParameterHidesMember
        public void Preload(KCModHelper helper)
        {
            var harmony = HarmonyInstance.Create("DevMode_Logger");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        
        private void OnGUI()
        {
            if (ScreenHeight != Screen.height || ScreenWidth != ScreenWidth)
            {
                SetConsoleSizePos();
                Generate_LogMessageBackgroundTex();
                Generate_LogModNameBackgroundTex();
            }

            Rect scrollViewRect = new Rect(consoleXpos, consoleYpos, consoleWidth, consoleHeight);
            Rect viewRect = new Rect(0, 0, scrollViewRect.width-20f, eventLog.Count*textHeight);
            scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, viewRect, false, true);
            
            for (int x=0; x < eventLog.Count;  x++)
            {
                string modName = eventLog[x][0];
                string message = eventLog[x][1];

                GUIStyle modNameStyle = BaseStyle;
                modNameStyle.normal.background = LogModNameBackgroundTex[Color.grey];

                GUIStyle messageStyle = BaseStyle;
                messageStyle.normal.background = LogMessageBackgroundTex[Color.grey];

                GUI.Label(new Rect(0f, textHeight * x, logModNameWidth, textHeight), modName, BaseStyle);
                GUI.Label(new Rect(logModNameWidth, textHeight * x, logMessageWidth, textHeight), message, BaseStyle);
            }
            
            GUI.EndScrollView();
        }
 
        public void AddEvent(string modName, string msg)
        {
            if (modName.Length > maxModNameLength) modName = modName.Substring(0, maxModNameLength);

            // Split event into multiple if too long
            if (msg.Length > maxMessageLength)
            {
                int numEvents = (int) Math.Ceiling((double) (msg.Length / maxMessageLength)) + 1;
                Debug.Log($"Message length too long {msg.Length} vs {maxMessageLength}. Splitting into {numEvents}");

                for (int i=0; i<numEvents; i++)
                {
                    int amountStringLeft = msg.Length - i * maxMessageLength;
                    
                    string message = msg.Substring(i * maxMessageLength, Math.Min(maxMessageLength, amountStringLeft));
                    eventLog.Add(new []{modName, message});
                }
            }
            else
            {
                eventLog.Add(new []{modName, msg});
            }
        }
    }
    
    [HarmonyPatch(typeof(KCModHelper), nameof(KCModHelper.Log))]
    class Patch_KCModHelper
    {
        static void Postfix(string msg, KCModHelper __instance)
        {
            // TODO Find name of mod somewhere in WorkshopUI?
            string modPath = __instance.modPath;
            string modName = modPath.Split('/', '\\').Last();
            
            DevMode_Logger.Inst.AddEvent(modName, msg);
        }
    }

    [HarmonyPatch(typeof(Debug), nameof(Debug.Log))]
    [HarmonyPatch(new[]{typeof(object)})]
    class Patch_Debug
    {
        static void Postfix(object message)
        {
            DevMode_Logger.Inst.AddEvent("DEBUG", message.ToString());
        }
    }
    
    [HarmonyPatch(typeof(Debug), nameof(Debug.Log))]
    [HarmonyPatch(new[]{typeof(object), typeof(UnityEngine.Object)})]
    class Patch_Debug2
    {
        static void Postfix(object message, UnityEngine.Object context)
        {
            DevMode_Logger.Inst.AddEvent("DEBUG", message.ToString());
        }
    }
}