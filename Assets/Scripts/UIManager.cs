using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인스펙터 바인딩 기반 UI 매니저 (TextMeshPro 지원)
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject titlePanel;
    public GameObject hudPanel;
    public GameObject gameOverPanel;
    public GameObject optionsPanel;

    [Header("HUD Elements")]
    public TMP_Text distanceText;
    public TMP_Text zoneChangeText;

    [Header("Title Elements")]
    public TMP_Text bestDistanceText;
    public Button startButton;
    public Button optionsButton; // 옵션 창 열기 버튼

    [Header("Options Elements")]
    public Slider bgmSlider; // BGM 볼륨 조절 슬라이더
    public Slider sfxSlider; // SFX 볼륨 조절 슬라이더
    public Button optionsCloseButton; // 옵션 창 닫기 버튼

    [Header("Keybind Elements (Dynamic List / No Prefab Asset Required)")]
    public Transform keyContainer;        // 키 아이템들이 나열될 부모 (Layout Group 등)
    public GameObject keyItemTemplate;    // 씬 내 템플릿 오브젝트 (텍스트 + 삭제버튼 포함, 기본 비활성화)
    public Button addKeyButton;           // + 새 키 추가 버튼
    public Button resetKeybindsButton;    // 기본값 초기화 버튼
    public TMP_Text keybindStatusText;    // 상태 안내 문구 ("키를 누르세요...")

    [Header("Game Over Elements")]
    public TMP_Text finalDistanceText;
    public TMP_Text finalBestText;
    public Button retryButton;

    private GameManager gm;
    private float zoneChangeTimer;
    private float titleShowTime;
    private bool isListeningForKey = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStart += ShowHUD;
            gm.OnGameOver += ShowGameOver;
            gm.OnZoneChanged += ShowZoneChange;
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryClicked);
        }

        // 시작 버튼 연결 (타이틀 화면에서만 활성)
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartClicked);
        }

        // 옵션 버튼 연결
        if (optionsButton != null)
        {
            optionsButton.onClick.AddListener(OpenOptions);
        }

        if (optionsCloseButton != null)
        {
            optionsCloseButton.onClick.AddListener(CloseOptions);
        }

        // 슬라이더 초기값 및 리스너 등록
        InitOptionsSliders();

        // 키바인딩 UI 초기화 및 이벤트 연결
        InitKeybindUI();

        ShowTitle();
    }

    private void InitOptionsSliders()
    {
        if (bgmSlider != null)
        {
            float bgmVol = PlayerPrefs.GetFloat("BGMVolume", 1f);
            bgmSlider.value = bgmVol;
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (sfxSlider != null)
        {
            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxSlider.value = sfxVol;
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    private void OnBGMVolumeChanged(float value)
    {
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.SetVolume(value);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.SetVolume(value);
        }
    }

    public void OpenOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }
    }

    public void CloseOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
    }

    // 시작 버튼 클릭 시 게임 시작
    private void OnStartClicked()
    {
        if (gm != null)
        {
            gm.StartGame();
        }
    }

    void OnDestroy()
    {
        if (gm != null)
        {
            gm.OnGameStart -= ShowHUD;
            gm.OnGameOver -= ShowGameOver;
            gm.OnZoneChanged -= ShowZoneChange;
        }

        if (KeybindManager.Instance != null)
        {
            KeybindManager.Instance.OnKeybindsChanged -= RefreshKeybindList;
        }
    }

    void Update()
    {
        CheckKeyInputForBinding();

        if (gm == null) return;


        // HUD: 거리 실시간 표시
        if (gm.CurrentState == GameManager.GameState.Playing)
        {
            if (distanceText != null)
                distanceText.text = Mathf.FloorToInt(gm.Distance) + "m";
        }

        // 구간 전환 표시 페이드
        if (zoneChangeTimer > 0f)
        {
            zoneChangeTimer -= Time.deltaTime;
            if (zoneChangeText != null)
            {
                float alpha = Mathf.Clamp01(zoneChangeTimer / 0.5f);
                zoneChangeText.color = new Color(zoneChangeText.color.r, zoneChangeText.color.g, zoneChangeText.color.b, alpha);
            }
            if (zoneChangeTimer <= 0f && zoneChangeText != null)
                zoneChangeText.gameObject.SetActive(false);
        }
    }

    #region Keybind UI Logic
    private void InitKeybindUI()
    {
        // 씬 내 템플릿은 복제용이므로 원본은 숨김 처리
        if (keyItemTemplate != null)
        {
            keyItemTemplate.SetActive(false);
        }

        if (addKeyButton != null)
        {
            addKeyButton.onClick.AddListener(OnAddKeyClicked);
        }

        if (resetKeybindsButton != null)
        {
            resetKeybindsButton.onClick.AddListener(OnResetKeybindsClicked);
        }

        if (KeybindManager.Instance != null)
        {
            KeybindManager.Instance.OnKeybindsChanged += RefreshKeybindList;
        }

        RefreshKeybindList();
        UpdateKeybindStatus("");
    }

    private void RefreshKeybindList()
    {
        if (keyContainer == null || keyItemTemplate == null) return;

        // 기존에 복제되었던 아이템들 정리 (템플릿 원본 제외)
        for (int i = keyContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = keyContainer.GetChild(i);
            if (child.gameObject != keyItemTemplate)
            {
                Destroy(child.gameObject);
            }
        }

        if (KeybindManager.Instance == null) return;

        var keys = KeybindManager.Instance.BoundKeys;
        for (int i = 0; i < keys.Count; i++)
        {
            KeyCode key = keys[i];

            // 씬의 템플릿 오브젝트를 인스턴스화
            GameObject itemObj = Instantiate(keyItemTemplate, keyContainer);
            itemObj.SetActive(true);

            // 키 이름 표시 (자식 TMP_Text)
            TMP_Text label = itemObj.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = KeybindManager.GetKeyDisplayName(key);
            }

            // 삭제 버튼 연결 (자식 Button)
            Button delBtn = itemObj.GetComponentInChildren<Button>();
            if (delBtn != null)
            {
                // 최소 1개는 조작키가 남아있어야 하므로 1개 이하일 땐 삭제 버튼 비활성화
                delBtn.interactable = keys.Count > 1;
                delBtn.onClick.AddListener(() =>
                {
                    if (KeybindManager.Instance != null)
                    {
                        KeybindManager.Instance.RemoveKey(key);
                    }
                });
            }
        }
    }

    private void OnAddKeyClicked()
    {
        if (isListeningForKey) return;
        isListeningForKey = true;
        UpdateKeybindStatus("등록할 키를 누르세요... (ESC: 취소)");
    }

    private void OnResetKeybindsClicked()
    {
        isListeningForKey = false;
        if (KeybindManager.Instance != null)
        {
            KeybindManager.Instance.ResetToDefaults();
            UpdateKeybindStatus("기본값으로 초기화되었습니다.");
        }
    }

    private void UpdateKeybindStatus(string message)
    {
        if (keybindStatusText != null)
        {
            keybindStatusText.text = message;
        }
    }

    private void CheckKeyInputForBinding()
    {
        if (!isListeningForKey) return;

        foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(k))
            {
                if (k == KeyCode.Escape)
                {
                    isListeningForKey = false;
                    UpdateKeybindStatus("등록이 취소되었습니다.");
                    return;
                }

                if (KeybindManager.Instance != null)
                {
                    bool added = KeybindManager.Instance.AddKey(k);
                    if (added)
                    {
                        UpdateKeybindStatus($"'{KeybindManager.GetKeyDisplayName(k)}' 등록 완료");
                    }
                    else
                    {
                        UpdateKeybindStatus("이미 등록된 키입니다.");
                    }
                }

                isListeningForKey = false;
                break;
            }
        }
    }
    #endregion

            public void ShowTitle()
    {
        if (titlePanel != null) titlePanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        titleShowTime = Time.time;

        if (bestDistanceText != null && gm != null && gm.BestDistance > 0f)
            bestDistanceText.text = "BEST: " + Mathf.FloorToInt(gm.BestDistance) + "m";
        else if (bestDistanceText != null)
            bestDistanceText.text = "";

        // 배경음악 페이드 인 호출
        if (BGMManager.Instance != null)
            BGMManager.Instance.FadeIn();
    }

    void ShowHUD()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        if (distanceText != null) distanceText.text = "0m";
        if (zoneChangeText != null) zoneChangeText.gameObject.SetActive(false);
    }

    void ShowGameOver()
    {
        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        if (finalDistanceText != null)
            finalDistanceText.text = Mathf.FloorToInt(gm.Distance) + "m";
        if (finalBestText != null)
            finalBestText.text = "BEST: " + Mathf.FloorToInt(gm.BestDistance) + "m";
    }

    void ShowZoneChange(string zoneName)
    {
        if (zoneChangeText != null)
        {
            zoneChangeText.gameObject.SetActive(true);
            zoneChangeText.text = "// " + zoneName;
            zoneChangeText.color = Color.white;
            zoneChangeTimer = 2f;
        }
    }

    public void OnRetryClicked()
    {
        if (gm == null) return;
        gm.RetryGame();

        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.ResetVisual();

        var trail = FindAnyObjectByType<TrailController>();
        if (trail != null)
        {
            var tr = trail.GetComponent<TrailRenderer>();
            if (tr != null) tr.Clear();
        }

        ShowTitle();
    }
}