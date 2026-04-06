using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

public class AnimatorGenerator : EditorWindow
{
    private string jsonContent = "";
    private string outputPath = "Assets/PlayerAnimator.controller";
    private Vector2 scrollPos;

    [MenuItem("Tools/Generate Animator from JSON")]
    public static void ShowWindow()
    {
        GetWindow<AnimatorGenerator>("Animator Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Animator Generator Settings", EditorStyles.boldLabel);

        GUILayout.Label("Output Path:");
        outputPath = EditorGUILayout.TextField(outputPath);

        GUILayout.Label("JSON Data:");
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
        jsonContent = EditorGUILayout.TextArea(jsonContent, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Generate Animator", GUILayout.Height(40)))
        {
            GenerateAnimator();
        }
    }

    private void GenerateAnimator()
    {
        if (string.IsNullOrEmpty(jsonContent))
        {
            EditorUtility.DisplayDialog("Error", "Please paste the JSON content.", "OK");
            return;
        }

        try
        {
            // 1. JSON 파싱
            RootObject root = JsonUtility.FromJson<RootObject>(jsonContent);
            if (root == null || root.AnimatorController == null)
            {
                Debug.LogError("Failed to parse JSON. Check the format.");
                return;
            }

            ControllerData data = root.AnimatorController;

            // 2. 애니메이터 컨트롤러 생성
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(outputPath);

            // 3. 파라미터 추가
            foreach (var param in data.Parameters)
            {
                AnimatorControllerParameterType type = ParseParameterType(param.Type);
                controller.AddParameter(param.Name, type);
            }

            // 4. 레이어 및 스테이트 머신 구성
            // 기본 생성시 Base Layer가 있으므로 첫 번째 레이어를 수정하여 사용
            AnimatorControllerLayer baseLayer = controller.layers[0];
            AnimatorStateMachine sm = baseLayer.stateMachine;

            // 스테이트 이름으로 AnimatorState 객체를 찾기 위한 딕셔너리
            Dictionary<string, AnimatorState> stateMap = new Dictionary<string, AnimatorState>();

            // (1) 스테이트 생성 루프
            // JSON에 정의된 Layers 중 첫번째("Locomotion")만 처리하는 예시입니다.
            if (data.Layers.Count > 0)
            {
                LayerData layerData = data.Layers[0];
                baseLayer.name = layerData.Name;

                // 스테이트 배치 간격 설정
                Vector3 position = new Vector3(250, 0, 0);
                int index = 0;

                foreach (var stateData in layerData.States)
                {
                    AnimatorState newState = sm.AddState(stateData.Name, new Vector3(250, index * 70, 0));

                    // Motion GUID로 에셋 로드 및 할당
                    if (!string.IsNullOrEmpty(stateData.MotionGUID))
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(stateData.MotionGUID);
                        Motion motion = AssetDatabase.LoadAssetAtPath<Motion>(assetPath);
                        if (motion != null)
                        {
                            newState.motion = motion;
                        }
                        else
                        {
                            Debug.LogWarning($"Motion not found for GUID: {stateData.MotionGUID} (State: {stateData.Name})");
                        }
                    }

                    stateMap.Add(stateData.Name, newState);
                    index++;
                }

                // (2) 트랜지션 연결 루프
                foreach (var stateData in layerData.States)
                {
                    if (!stateMap.ContainsKey(stateData.Name)) continue;

                    AnimatorState sourceState = stateMap[stateData.Name];

                    if (stateData.Transitions != null)
                    {
                        foreach (var transData in stateData.Transitions)
                        {
                            if (stateMap.ContainsKey(transData.DestinationState))
                            {
                                AnimatorState destState = stateMap[transData.DestinationState];

                                // 트랜지션 생성
                                AnimatorStateTransition transition = sourceState.AddTransition(destState);

                                // 설정 적용
                                transition.hasExitTime = transData.HasExitTime;
                                transition.exitTime = transData.ExitTime;
                                transition.duration = transData.TransitionDuration;
                                transition.hasFixedDuration = true; // 기본값으로 고정 시간 사용
                                transition.offset = 0;

                                // 조건(Conditions) 적용
                                if (transData.Conditions != null)
                                {
                                    foreach (var cond in transData.Conditions)
                                    {
                                        AnimatorConditionMode mode = ParseConditionMode(cond.Mode);
                                        transition.AddCondition(mode, cond.Threshold, cond.Parameter);
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"Destination State not found: {transData.DestinationState}");
                            }
                        }
                    }
                }
            }

            Debug.Log($"Successfully generated Animator Controller at {outputPath}");
            AssetDatabase.SaveAssets();
            Selection.activeObject = controller;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Generation failed: {e.Message}\n{e.StackTrace}");
        }
    }

    // --- Helper Methods & Enums ---

    private AnimatorControllerParameterType ParseParameterType(string typeStr)
    {
        switch (typeStr)
        {
            case "Float": return AnimatorControllerParameterType.Float;
            case "Int": return AnimatorControllerParameterType.Int;
            case "Bool": return AnimatorControllerParameterType.Bool;
            case "Trigger": return AnimatorControllerParameterType.Trigger;
            default: return AnimatorControllerParameterType.Float;
        }
    }

    private AnimatorConditionMode ParseConditionMode(string modeStr)
    {
        switch (modeStr)
        {
            case "If": return AnimatorConditionMode.If;
            case "IfNot": return AnimatorConditionMode.IfNot;
            case "Greater": return AnimatorConditionMode.Greater;
            case "Less": return AnimatorConditionMode.Less;
            case "Equals": return AnimatorConditionMode.Equals;
            case "NotEqual": return AnimatorConditionMode.NotEqual;
            default: return AnimatorConditionMode.If;
        }
    }
}

// --- Data Structures for JSON ---
// Unity JsonUtility는 중첩 배열 처리를 위해 Serializable 클래스가 필요합니다.

[System.Serializable]
public class RootObject
{
    public ControllerData AnimatorController;
}

[System.Serializable]
public class ControllerData
{
    public string Name;
    public List<ParameterData> Parameters;
    public List<LayerData> Layers;
}

[System.Serializable]
public class ParameterData
{
    public string Name;
    public string Type;
}

[System.Serializable]
public class LayerData
{
    public string Name;
    public string StateMachineName;
    public string StateMachineGUID;
    public List<StateData> States;
}

[System.Serializable]
public class StateData
{
    public string Name;
    public string GUID;
    public string MotionGUID;
    public List<TransitionData> Transitions;
}

[System.Serializable]
public class TransitionData
{
    public string Type; // ComboLink or ReturnLink
    public string DestinationState;
    public string DestinationGUID;
    public bool HasExitTime;
    public float ExitTime;
    public float TransitionDuration;
    public List<ConditionData> Conditions;
}

[System.Serializable]
public class ConditionData
{
    public string Parameter;
    public string Mode;
    public float Threshold;
}