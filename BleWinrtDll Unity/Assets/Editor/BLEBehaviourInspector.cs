using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BLEBehaviour))]
public class BLEBehaviourInspector : Editor
{
    private BLEBehaviour _bleBehaviour;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        _bleBehaviour = target as BLEBehaviour;

        if (GUILayout.Button("Connect"))
        {
            _bleBehaviour.StartScanHandler();
        }
        
        if (GUILayout.Button("Disconnect"))
        {
            _bleBehaviour.Disconnect();
        }

        // if (GUILayout.Button("Write"))
        // {
        //     _bleBehaviour.StartWritingHandler();
        // }
        

    }
    

}
