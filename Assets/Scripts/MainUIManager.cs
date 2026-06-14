using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using PimDeWitte.UnityMainThreadDispatcher;

public class MainUIManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;
    string userKey;

    [Header("Persistent UI (고정 UI)")]
    [SerializeField] Text globalCoinText;
    [SerializeField] Text globalScoreText;
    [SerializeField] Text globalMessageText;

    [Header("Panels (팝업 패널)")]
    [SerializeField] GameObject shopPanel;
    [SerializeField] GameObject inventoryPanel;
    [SerializeField] GameObject gameResultPanel;
    [SerializeField] GameObject auctionPanel;

    void Start()
    {
        database = FirebaseDatabase.GetInstance("" +
            "https://shinguproject-default-rtdb.asia-southeast1.firebasedatabase.app/");
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
        userKey = PlayerPrefs.GetString("UserKey");

        // 시작 시 모든 패널 닫기
        CloseAllPanels();

        // 메인 씬 진입 시 초기 코인과 점수 불러오기
        LoadInitialData();
    }

    void LoadInitialData()
    {
        if (string.IsNullOrEmpty(userKey)) return;

        reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() => { ShowMessage("데이터 로드 실패"); });
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                // 1. 코인 안전하게 불러오기
                int currentCoin = 0;
                if (snapshot.HasChild("Coin") && snapshot.Child("Coin").Value != null)
                {
                    currentCoin = int.Parse(snapshot.Child("Coin").Value.ToString());
                }

                // 2. 스코어 안전하게 불러오기 (에러 방지 핵심)
                int currentScore = 0;
                if (snapshot.HasChild("Score") && snapshot.Child("Score").Value != null)
                {
                    currentScore = int.Parse(snapshot.Child("Score").Value.ToString());
                }
                else
                {
                    // Firebase에 Score 노드가 아예 없다면 기본값 0으로 처리
                    currentScore = 0;
                }

                dispatcher.Enqueue(() =>
                {
                    UpdateGlobalCoin(currentCoin);
                    UpdateGlobalScore(currentScore);
                    ShowMessage("메인 로비 접속 완료");
                });
            }
        });
    }

    // --- [고정 UI 업데이트용 전역 함수] ---
    public void UpdateGlobalCoin(int coin)
    {
        globalCoinText.text = "Coin : " + coin;
    }

    public void UpdateGlobalScore(int score)
    {
        globalScoreText.text = "Score : " + score;
    }

    public void ShowMessage(string msg)
    {
        globalMessageText.text = msg;
    }

    // --- [패널 열기/닫기 함수] ---
    // 각 열기 버튼의 OnClick()에 이 함수들을 연결해줘.
    public void OpenShop() { CloseAllPanels(); shopPanel.SetActive(true); }
    public void OpenInventory() { CloseAllPanels(); inventoryPanel.SetActive(true); }
    public void OpenGameResult() { CloseAllPanels(); gameResultPanel.SetActive(true); }
    public void OpenAuction() { CloseAllPanels(); auctionPanel.SetActive(true); }

    // 각 패널 안의 닫기(X) 버튼의 OnClick()에 이 함수를 연결해줘.
    public void CloseAllPanels()
    {
        shopPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        gameResultPanel.SetActive(false);
        auctionPanel.SetActive(false);
    }
}