using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

/// <summary>
/// 플레이어 궤도 반전 조작 키 바인딩 매니저.
/// - 기본 조작: 마우스 좌클릭(Mouse0) + 스페이스바(Space)
/// - 런타임에서 키 추가 / 제거 지원
/// - PlayerPrefs를 통한 영구 저장
/// </summary>
public class KeybindManager : MonoBehaviour
{
    private static KeybindManager instance;
    private static bool isApplicationQuitting = false;

    public static bool HasInstance => instance != null;
    public static KeybindManager Instance
    {
        get
        {
            if (isApplicationQuitting) return null;

            if (instance == null)
            {
                instance = FindAnyObjectByType<KeybindManager>();
                if (instance == null && !isApplicationQuitting)
                {
                    GameObject go = new GameObject("KeybindManager");
                    instance = go.AddComponent<KeybindManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    private const string PREFS_KEY = "Veerscape_Keybinds";
    private readonly List<KeyCode> boundKeys = new List<KeyCode>();

    public IReadOnlyList<KeyCode> BoundKeys => boundKeys;

    /// <summary>키 바인딩 목록이 변경되었을 때 호출되는 이벤트</summary>
    public event Action OnKeybindsChanged;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        LoadKeybinds();
    }

    /// <summary>
    /// 이번 프레임에 등록된 조작 키 중 하나라도 눌렸는지 확인.
    /// 마우스 클릭의 경우 UI 버튼 위 클릭은 무시합니다.
    /// </summary>
    public bool IsActionTriggered()
    {
        for (int i = 0; i < boundKeys.Count; i++)
        {
            KeyCode key = boundKeys[i];
            if (Input.GetKeyDown(key))
            {
                // 마우스 버튼의 경우 UI 클릭 방지
                if (IsMouseButton(key))
                {
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                        continue;
                }
                return true;
            }
        }
        return false;
    }

    public static bool IsMouseButton(KeyCode key)
    {
        return key == KeyCode.Mouse0 || key == KeyCode.Mouse1 || key == KeyCode.Mouse2
            || key == KeyCode.Mouse3 || key == KeyCode.Mouse4 || key == KeyCode.Mouse5 || key == KeyCode.Mouse6;
    }

    /// <summary>
    /// 새 키 바인딩 추가
    /// </summary>
    /// <summary>
    /// 지정된 슬롯(0: 기본키, 1: 보조키)의 키 변경
    /// </summary>
    public void SetKeyAtSlot(int slotIndex, KeyCode newKey)
    {
        if (newKey == KeyCode.None || newKey == KeyCode.Escape) return;

        // 목록 크기가 부족할 경우 확장
        while (boundKeys.Count <= slotIndex)
        {
            boundKeys.Add(KeyCode.None);
        }

        // 다른 슬롯과 키가 겹치면 맞바꾸거나 기존 자리 갱신
        int existingIndex = boundKeys.IndexOf(newKey);
        if (existingIndex >= 0 && existingIndex != slotIndex)
        {
            // 자리 맞바꾸기
            boundKeys[existingIndex] = boundKeys[slotIndex];
        }

        boundKeys[slotIndex] = newKey;
        SaveKeybinds();
    }

    public bool AddKey(KeyCode key)
    {
        if (key == KeyCode.None || key == KeyCode.Escape) return false;

        if (!boundKeys.Contains(key))
        {
            boundKeys.Add(key);
            SaveKeybinds();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 기존 키 바인딩 제거 (최소 1개 키는 유지)
    /// </summary>
    public bool RemoveKey(KeyCode key)
    {
        if (boundKeys.Count <= 1)
        {
            Debug.LogWarning("[KeybindManager] At least one control key must be maintained.");
            return false;
        }

        if (boundKeys.Remove(key))
        {
            SaveKeybinds();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 기본값 (좌클릭 + 스페이스바)으로 리셋
    /// </summary>
    public void ResetToDefaults()
    {
        boundKeys.Clear();
        boundKeys.Add(KeyCode.Mouse0);
        boundKeys.Add(KeyCode.Space);
        SaveKeybinds();
    }

    private void SaveKeybinds()
    {
        List<string> names = new List<string>(boundKeys.Count);
        for (int i = 0; i < boundKeys.Count; i++)
        {
            names.Add(boundKeys[i].ToString());
        }
        PlayerPrefs.SetString(PREFS_KEY, string.Join(",", names));
        PlayerPrefs.Save();

        OnKeybindsChanged?.Invoke();
    }

    private void LoadKeybinds()
    {
        boundKeys.Clear();
        string saved = PlayerPrefs.GetString(PREFS_KEY, "");
        if (!string.IsNullOrEmpty(saved))
        {
            string[] tokens = saved.Split(',');
            foreach (var t in tokens)
            {
                if (Enum.TryParse(t.Trim(), out KeyCode k))
                {
                    if (k != KeyCode.None && !boundKeys.Contains(k))
                    {
                        boundKeys.Add(k);
                    }
                }
            }
        }

        // 기본값: 마우스 좌클릭(Mouse0) + 스페이스바(Space)
        if (boundKeys.Count == 0)
        {
            boundKeys.Add(KeyCode.Mouse0);
            boundKeys.Add(KeyCode.Space);
        }
    }

    /// <summary>
    /// UI에 표시할 친근한 키 이름 반환
    /// </summary>
    public static string GetKeyDisplayName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Mouse0: return "Left Click (Mouse 0)";
            case KeyCode.Mouse1: return "Right Click (Mouse 1)";
            case KeyCode.Mouse2: return "Middle Click (Mouse 2)";
            case KeyCode.Space: return "Space";
            case KeyCode.Return: return "Enter";
            case KeyCode.KeypadEnter: return "Keypad Enter";
            case KeyCode.UpArrow: return "Up Arrow";
            case KeyCode.DownArrow: return "Down Arrow";
            case KeyCode.LeftArrow: return "Left Arrow";
            case KeyCode.RightArrow: return "Right Arrow";
            case KeyCode.LeftShift: return "Left Shift";
            case KeyCode.RightShift: return "Right Shift";
            case KeyCode.LeftControl: return "Left Ctrl";
            case KeyCode.RightControl: return "Right Ctrl";
            case KeyCode.LeftAlt: return "Left Alt";
            case KeyCode.RightAlt: return "Right Alt";
            case KeyCode.Tab: return "Tab";
            default: return key.ToString();
        }
    }
}
