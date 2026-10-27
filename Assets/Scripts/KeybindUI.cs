using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 옵션 창 내 키 바인딩 설정 UI 컴포넌트.
/// - 현재 등록된 조작 키 목록 표시
/// - 각 키마다 [X] 버튼으로 즉시 제거 가능 (최소 1개 키 유지)
/// - [+ 키 추가] 버튼을 누른 뒤 원하는 키(키보드/마우스)를 누르면 실시간 등록
/// - [기본값 초기화] 지원 (좌클릭 + 스페이스바)
/// </summary>
public class KeybindUI : MonoBehaviour
{
    [Header("Optional UI References (미할당 시 자동 구성)")]
    public Transform keyListContainer;
    public Button addKeyButton;
    public Button resetButton;
    public TMP_Text statusText;

    private bool isListening = false;
    private float listenStartTime;

    private static readonly KeyCode[] allKeyCodes = (KeyCode[])System.Enum.GetValues(typeof(KeyCode));

    void Start()
    {
        EnsureUIStructure();

        if (KeybindManager.Instance != null)
        {
            KeybindManager.Instance.OnKeybindsChanged += RefreshUI;
        }

        if (addKeyButton != null)
        {
            addKeyButton.onClick.AddListener(StartListening);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
        }

        RefreshUI();
    }

    void OnDestroy()
    {
        if (KeybindManager.Instance != null)
        {
            KeybindManager.Instance.OnKeybindsChanged -= RefreshUI;
        }
    }

    void OnEnable()
    {
        isListening = false;
        RefreshUI();
    }

    void OnDisable()
    {
        isListening = false;
    }

    void Update()
    {
        if (!isListening) return;

        // 시작 직후 클릭(버튼 클릭 자체) 방지용 미세 딜레이 (0.15초)
        if (Time.unscaledTime - listenStartTime < 0.15f) return;

        // ESC 누르면 취소
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopListening();
            if (statusText != null) statusText.text = "키 추가가 취소되었습니다.";
            return;
        }

        // 마우스 버튼 입력 감지
        if (Input.GetMouseButtonDown(0)) { RegisterKey(KeyCode.Mouse0); return; }
        if (Input.GetMouseButtonDown(1)) { RegisterKey(KeyCode.Mouse1); return; }
        if (Input.GetMouseButtonDown(2)) { RegisterKey(KeyCode.Mouse2); return; }

        // 키보드 키 입력 감지
        if (Input.anyKeyDown)
        {
            for (int i = 0; i < allKeyCodes.Length; i++)
            {
                KeyCode k = allKeyCodes[i];
                if (k == KeyCode.None || k == KeyCode.Escape) continue;
                if (KeybindManager.IsMouseButton(k)) continue;

                if (Input.GetKeyDown(k))
                {
                    RegisterKey(k);
                    return;
                }
            }
        }
    }

    private void StartListening()
    {
        isListening = true;
        listenStartTime = Time.unscaledTime;
        if (statusText != null)
        {
            statusText.text = "<color=#FFE853>등록할 키보드 키 또는 마우스 버튼을 누르세요... (ESC: 취소)</color>";
        }
        if (addKeyButton != null)
        {
            addKeyButton.interactable = false;
        }
    }

    private void StopListening()
    {
        isListening = false;
        if (addKeyButton != null)
        {
            addKeyButton.interactable = true;
        }
    }

    private void RegisterKey(KeyCode key)
    {
        StopListening();

        bool added = KeybindManager.Instance.AddKey(key);
        if (statusText != null)
        {
            if (added)
                statusText.text = $"<color=#53FF88>'{KeybindManager.GetKeyDisplayName(key)}' 키가 추가되었습니다!</color>";
            else
                statusText.text = $"<color=#FF7373>'{KeybindManager.GetKeyDisplayName(key)}'는 이미 등록되어 있습니다.</color>";
        }
    }

    private void OnResetClicked()
    {
        StopListening();
        KeybindManager.Instance.ResetToDefaults();
        if (statusText != null)
        {
            statusText.text = "기본 조작(좌클릭 + 스페이스바)으로 초기화되었습니다.";
        }
    }

    /// <summary>
    /// 현재 등록된 키 목록을 칩(Badge) 형태로 갱신
    /// </summary>
    public void RefreshUI()
    {
        if (keyListContainer == null) return;

        // 기존 칩 제거
        for (int i = keyListContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(keyListContainer.GetChild(i).gameObject);
        }

        var keys = KeybindManager.Instance.BoundKeys;
        bool canDelete = keys.Count > 1;

        for (int i = 0; i < keys.Count; i++)
        {
            KeyCode key = keys[i];
            CreateKeyChip(key, canDelete);
        }

        if (!isListening && statusText != null)
        {
            statusText.text = "등록된 키로 궤도를 반전할 수 있습니다. (추가/제거 가능)";
        }
    }

    private void CreateKeyChip(KeyCode key, bool canDelete)
    {
        // 칩 컨테이너
        GameObject chip = new GameObject("KeyChip_" + key.ToString(), typeof(RectTransform));
        chip.transform.SetParent(keyListContainer, false);

        HorizontalLayoutGroup hlg = chip.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 8;
        hlg.padding = new RectOffset(12, 10, 6, 6);

        ContentSizeFitter csf = chip.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image bg = chip.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.22f, 0.32f, 0.85f);

        // 키 이름 텍스트
        GameObject textObj = new GameObject("Label", typeof(RectTransform));
        textObj.transform.SetParent(chip.transform, false);
        TextMeshProUGUI label = textObj.AddComponent<TextMeshProUGUI>();
        label.text = KeybindManager.GetKeyDisplayName(key);
        label.fontSize = 24;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;

        // [X] 삭제 버튼
        GameObject delBtnObj = new GameObject("DeleteBtn", typeof(RectTransform));
        delBtnObj.transform.SetParent(chip.transform, false);
        RectTransform btnRt = delBtnObj.GetComponent<RectTransform>();
        btnRt.sizeDelta = new Vector2(28, 28);

        Image delImg = delBtnObj.AddComponent<Image>();
        delImg.color = canDelete ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.4f, 0.4f, 0.4f, 0.5f);

        Button delBtn = delBtnObj.AddComponent<Button>();
        delBtn.interactable = canDelete;
        delBtn.onClick.AddListener(() =>
        {
            StopListening();
            KeybindManager.Instance.RemoveKey(key);
        });

        GameObject xTextObj = new GameObject("X", typeof(RectTransform));
        xTextObj.transform.SetParent(delBtnObj.transform, false);
        TextMeshProUGUI xLabel = xTextObj.AddComponent<TextMeshProUGUI>();
        xLabel.text = "X";
        xLabel.fontSize = 18;
        xLabel.color = Color.white;
        xLabel.alignment = TextAlignmentOptions.Center;
        RectTransform xRt = xTextObj.GetComponent<RectTransform>();
        xRt.anchorMin = Vector2.zero;
        xRt.anchorMax = Vector2.one;
        xRt.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// 인스펙터 미할당 시 OptionsPanel 내부에 키바인드 영역 UI를 자동 구축
    /// </summary>
    private void EnsureUIStructure()
    {
        if (keyListContainer != null && addKeyButton != null) return;

        // 부모 패널 탐색
        Transform targetParent = transform;

        // 이미 생성된 섹션이 있는지 확인
        Transform existingSection = targetParent.Find("KeybindSection");
        if (existingSection != null)
        {
            keyListContainer = existingSection.Find("KeyList");
            addKeyButton = existingSection.Find("ButtonGroup/AddBtn")?.GetComponent<Button>();
            resetButton = existingSection.Find("ButtonGroup/ResetBtn")?.GetComponent<Button>();
            statusText = existingSection.Find("StatusText")?.GetComponent<TMP_Text>();
            return;
        }

        // 키바인드 섹션 루트 생성
        GameObject sectionObj = new GameObject("KeybindSection", typeof(RectTransform));
        sectionObj.transform.SetParent(targetParent, false);
        RectTransform secRt = sectionObj.GetComponent<RectTransform>();
        secRt.anchoredPosition = new Vector2(0, 0); // OptionsPanel 중앙 하단
        secRt.sizeDelta = new Vector2(620, 180);

        VerticalLayoutGroup vlg = sectionObj.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 8;
        vlg.childAlignment = TextAnchor.MiddleCenter;

        // 1. 헤더 타이틀
        GameObject titleObj = new GameObject("SectionTitle", typeof(RectTransform));
        titleObj.transform.SetParent(sectionObj.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "조작 키 바인딩 (Keybinds)";
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.4f, 0.85f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;

        // 2. 키 칩 목록 컨테이너
        GameObject listObj = new GameObject("KeyList", typeof(RectTransform));
        listObj.transform.SetParent(sectionObj.transform, false);
        RectTransform listRt = listObj.GetComponent<RectTransform>();
        listRt.sizeDelta = new Vector2(580, 48);

        HorizontalLayoutGroup listHlg = listObj.AddComponent<HorizontalLayoutGroup>();
        listHlg.childControlWidth = false;
        listHlg.childControlHeight = false;
        listHlg.childForceExpandWidth = false;
        listHlg.childForceExpandHeight = false;
        listHlg.spacing = 10;
        listHlg.childAlignment = TextAnchor.MiddleCenter;
        keyListContainer = listObj.transform;

        // 3. 버튼 그룹 (+ 키 추가 / 초기화)
        GameObject btnGroup = new GameObject("ButtonGroup", typeof(RectTransform));
        btnGroup.transform.SetParent(sectionObj.transform, false);
        RectTransform bgRt = btnGroup.GetComponent<RectTransform>();
        bgRt.sizeDelta = new Vector2(580, 42);

        HorizontalLayoutGroup bgHlg = btnGroup.AddComponent<HorizontalLayoutGroup>();
        bgHlg.childControlWidth = false;
        bgHlg.childControlHeight = false;
        bgHlg.spacing = 16;
        bgHlg.childAlignment = TextAnchor.MiddleCenter;

        // [+ 키 추가] 버튼
        GameObject addBtnObj = CreateButton(btnGroup.transform, "AddBtn", "+ 키 추가", new Vector2(160, 40), new Color(0.18f, 0.55f, 0.85f));
        addKeyButton = addBtnObj.GetComponent<Button>();

        // [기본값 초기화] 버튼
        GameObject resetBtnObj = CreateButton(btnGroup.transform, "ResetBtn", "기본값 초기화", new Vector2(160, 40), new Color(0.35f, 0.35f, 0.4f));
        resetButton = resetBtnObj.GetComponent<Button>();

        // 4. 상태 및 안내 텍스트
        GameObject statusObj = new GameObject("StatusText", typeof(RectTransform));
        statusObj.transform.SetParent(sectionObj.transform, false);
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 20;
        statusText.color = new Color(0.85f, 0.85f, 0.85f);
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.text = "등록된 키로 궤도를 반전할 수 있습니다. (추가/제거 가능)";
    }

    private GameObject CreateButton(Transform parent, string name, string labelText, Vector2 size, Color bgColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI label = textObj.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = 20;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        return btnObj;
    }
}
