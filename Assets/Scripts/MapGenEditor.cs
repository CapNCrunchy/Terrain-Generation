using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (MapGenerator))]
public class MapGenEditor : Editor
{
    //Custom editor for the map generator
    public override void OnInspectorGUI()
    {
        MapGenerator mapGen = (MapGenerator)target;
        //It will generate the map if the autoUpdate is true or the button is pressed
        if (DrawDefaultInspector())
        {
            if (mapGen.autoUpdate)
            {
                mapGen.GenerateMap();
            }
        }

        if(GUILayout.Button("Generate Map"))
        {
            mapGen.GenerateMap();
        }
    }
}
