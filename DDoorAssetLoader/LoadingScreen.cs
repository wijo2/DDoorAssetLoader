using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DDoorAssetLoader
{
    internal static class LoadingScreen
    {
        internal static int totalMaps;
        internal static int mapsDone;
        private static GUIStyle boxStyle;
        private static Texture2D boxTexture;
        public static GUIStyle textStyle;
        private static bool hasInit = false;

        internal static void Init()
        {
            boxTexture = new Texture2D(1,1);
            boxStyle = new GUIStyle(GUI.skin.box);
            textStyle = new GUIStyle(GUI.skin.textArea);
        }

        internal static void OnGUI()
        {
            if (!hasInit) { Init(); }
            boxTexture.SetPixel(0,0,new Color(0,0,0,1));
            boxTexture.Apply();
            boxStyle.normal.background = boxTexture;
            GUI.Box(new Rect(new Vector2(0,0), new Vector2(Screen.width, Screen.height)), "", boxStyle);
            textStyle.border = new RectOffset(0,0,0,0);
            textStyle.normal.background = boxTexture;
            textStyle.fontSize = (int)Mathf.Floor(Screen.width / 16);
            GUI.TextArea(
                    new Rect(new Vector2(Screen.width/4 * 1.1f, Screen.height/4 * 1.1f), 
                    new Vector2(2000,500)), 
                    "Loading Assets", 
                    textStyle
                    );
            GUI.TextArea(
                    new Rect(new Vector2(Screen.width/4 / 1.2f, Screen.height/4 * 2.2f), 
                    new Vector2(2000,500)), 
                    "Working on map " + mapsDone.ToString() + "/" + totalMaps.ToString(), 
                    textStyle
                    );
        }
    }
}
