using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SensorFusion))]
public class SensorFusionInspector : Editor
{
    private SensorFusion _sensorFusion;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        _sensorFusion = target as SensorFusion;
        
        if (GUILayout.Button("Calibrate"))
        {
            _sensorFusion.CalibrateBLEVuforia();
        }
    }
}
