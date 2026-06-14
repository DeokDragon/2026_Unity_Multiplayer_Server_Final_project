using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;

public class Auction : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("UI & Managers")]
    [SerializeField] Text MessageText;
    [SerializeField] MainUIManager mainUIManager;

    [Header("Auction Quantity Texts")]
    [SerializeField] Text IceCrystalAuctionText;
    [SerializeField] Text FireScrollAuctionText;
    [SerializeField] Text GoldKeyAuctionText;

    string userKey;

    // 거래소에 남은 수량을 로컬에 임시 저장하는 딕셔너리
    Dictionary<string, int> auctionInventory = new Dictionary<string, int>();

    void Start()
    {
        database = FirebaseDatabase.GetInstance("" +
            "https://shinguproject-default-rtdb.asia-southeast1.firebasedatabase.app/");
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
        userKey = PlayerPrefs.GetString("UserKey");
    }

    // =======================================================
    // 1. 거래소 열릴 때 전체 수량 불러오기
    // =======================================================
    public void LoadAuctionData()
    {
        // 💡 UserInfo가 아닌 최상위 공용 공간(GlobalAuction)을 읽어옵니다.
        reference.Child("GlobalAuction").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted) return;

            DataSnapshot snapshot = task.Result;

            auctionInventory["IceCrystal"] = snapshot.HasChild("IceCrystal") ? int.Parse(snapshot.Child("IceCrystal").Value.ToString()) : 0;
            auctionInventory["FireScroll"] = snapshot.HasChild("FireScroll") ? int.Parse(snapshot.Child("FireScroll").Value.ToString()) : 0;
            auctionInventory["GoldKey"] = snapshot.HasChild("GoldKey") ? int.Parse(snapshot.Child("GoldKey").Value.ToString()) : 0;

            dispatcher.Enqueue(() =>
            {
                RefreshAuctionUI();
            });
        });
    }

    void RefreshAuctionUI()
    {
        if (IceCrystalAuctionText != null) IceCrystalAuctionText.text = "IceCrystal : " + auctionInventory["IceCrystal"];
        if (FireScrollAuctionText != null) FireScrollAuctionText.text = "FireScroll : " + auctionInventory["FireScroll"];
        if (GoldKeyAuctionText != null) GoldKeyAuctionText.text = "GoldKey : " + auctionInventory["GoldKey"];
    }

    // =======================================================
    // 2. 핵심 통신 로직 (판매 & 구매)
    // =======================================================

    void SellToAuction(string itemName, int price)
    {
        reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted) return;

            DataSnapshot snapshot = task.Result;
            int myCoin = int.Parse(snapshot.Child("Coin").Value.ToString());
            string inventoryJson = snapshot.Child("Inventory").Value.ToString();
            var myInventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);

            if (!myInventory.ContainsKey(itemName) || myInventory[itemName] <= 0)
            {
                dispatcher.Enqueue(() => MessageText.text = "판매할 아이템이 부족합니다.");
                return;
            }

            myCoin += price;
            myInventory[itemName]--;
            string updatedInventoryJson = JsonConvert.SerializeObject(myInventory);

            reference.Child("GlobalAuction").Child(itemName).GetValueAsync().ContinueWith(auctionTask => {
                int currentAuctionQty = 0;

                if (auctionTask.Result.Value != null) currentAuctionQty = int.Parse(auctionTask.Result.Value.ToString());

                int newAuctionQty = currentAuctionQty + 1;

                Dictionary<string, object> updates = new Dictionary<string, object>();
                updates["/UserInfo/" + userKey + "/Coin"] = myCoin;
                updates["/UserInfo/" + userKey + "/Inventory"] = updatedInventoryJson;

                // 💡 공용 거래소 수량을 1개 올립니다.
                updates["/GlobalAuction/" + itemName] = newAuctionQty;

                reference.UpdateChildrenAsync(updates).ContinueWith(updateTask =>
                {
                    if (updateTask.IsCompleted)
                    {
                        dispatcher.Enqueue(() =>
                        {
                            MessageText.text = $"{itemName} 판매 완료! (+{price}원)";
                            auctionInventory[itemName] = newAuctionQty;
                            RefreshAuctionUI();
                            if (mainUIManager != null) mainUIManager.UpdateGlobalCoin(myCoin);
                        });
                    }
                });
            });
        });
    }

    void BuyFromAuction(string itemName, int price)
    {
        reference.Child("GlobalAuction").Child(itemName).GetValueAsync().ContinueWith(auctionTask => {
            int currentAuctionQty = 0;
            if (auctionTask.Result.Value != null) currentAuctionQty = int.Parse(auctionTask.Result.Value.ToString());

            if (currentAuctionQty <= 0)
            {
                dispatcher.Enqueue(() => MessageText.text = "거래소에 남은 수량이 없습니다.");
                return;
            }

            reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
            {
                DataSnapshot snapshot = task.Result;
                int myCoin = int.Parse(snapshot.Child("Coin").Value.ToString());
                string inventoryJson = snapshot.Child("Inventory").Value.ToString();
                var myInventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);

                if (myCoin < price)
                {
                    dispatcher.Enqueue(() => MessageText.text = "코인이 부족합니다.");
                    return;
                }

                myCoin -= price;
                if (myInventory.ContainsKey(itemName)) myInventory[itemName]++;
                else myInventory.Add(itemName, 1);

                string updatedInventoryJson = JsonConvert.SerializeObject(myInventory);
                int newAuctionQty = currentAuctionQty - 1;

                Dictionary<string, object> updates = new Dictionary<string, object>();
                updates["/UserInfo/" + userKey + "/Coin"] = myCoin;
                updates["/UserInfo/" + userKey + "/Inventory"] = updatedInventoryJson;

                // 💡 공용 거래소 수량을 1개 내립니다.
                updates["/GlobalAuction/" + itemName] = newAuctionQty;

                reference.UpdateChildrenAsync(updates).ContinueWith(updateTask =>
                {
                    if (updateTask.IsCompleted)
                    {
                        dispatcher.Enqueue(() =>
                        {
                            MessageText.text = $"{itemName} 구매 완료! (-{price}원)";
                            auctionInventory[itemName] = newAuctionQty;
                            RefreshAuctionUI();
                            if (mainUIManager != null) mainUIManager.UpdateGlobalCoin(myCoin);
                        });
                    }
                });
            });
        });
    }

    // =======================================================
    // 3. 유니티 UI 버튼에 연결할 전용 함수들
    // =======================================================
    public void OnClickSellIceCrystal() { SellToAuction("IceCrystal", 100); }
    public void OnClickBuyIceCrystal() { BuyFromAuction("IceCrystal", 100); }

    public void OnClickSellFireScroll() { SellToAuction("FireScroll", 150); }
    public void OnClickBuyFireScroll() { BuyFromAuction("FireScroll", 150); }

    public void OnClickSellGoldKey() { SellToAuction("GoldKey", 50); }
    public void OnClickBuyGoldKey() { BuyFromAuction("GoldKey", 50); }
}