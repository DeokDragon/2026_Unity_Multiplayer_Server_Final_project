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
    [SerializeField] GameObject auctionPanel;

    [Header("Managers")]
    [SerializeField] InventoryManager inventoryManager;
    [SerializeField] Auction auctionManager;



    void Start()
    {
        database = FirebaseDatabase.GetInstance("" +
            "https://shinguproject-default-rtdb.asia-southeast1.firebasedatabase.app/");
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
        userKey = PlayerPrefs.GetString("UserKey");

        CloseAllPanels();
        LoadInitialData();
    }

    void LoadInitialData()
    {
        if (string.IsNullOrEmpty(userKey)) return;

        reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DataSnapshot snapshot = task.Result;
                int currentCoin = int.Parse(snapshot.Child("Coin").Value.ToString());
                int currentScore = int.Parse(snapshot.Child("Score").Value.ToString());

                dispatcher.Enqueue(() =>
                {
                    UpdateGlobalCoin(currentCoin);
                    UpdateGlobalScore(currentScore);
                    ShowMessage("메인 로비 접속 완료");
                });
            }
        });
    }

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

    public void OpenShop()
    {
        CloseAllPanels();
        shopPanel.SetActive(true);
    }
    public void OpenInventory()
    {
        CloseAllPanels();
        inventoryManager.LoadInventory();
        inventoryPanel.SetActive(true);
        
    }
    public void OpenAuction()
    {
        CloseAllPanels();
        auctionManager.LoadAuctionData();
        auctionPanel.SetActive(true);
    }

    public void CloseAllPanels()
    {
        shopPanel.SetActive(false);
        inventoryPanel.SetActive(false);
        auctionPanel.SetActive(false);
    }

    
}