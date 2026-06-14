using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;

public class MarketManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [SerializeField] Text MessageText;
    string userKey;

    void Start()
    {
        database = FirebaseDatabase.GetInstance("주소");
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
        userKey = PlayerPrefs.GetString("UserKey");
    }

    // 1. 내 아이템 판매 등록
    public void RegisterItemToMarket(string itemName, int price, Dictionary<string, int> myInventory)
    {
        if (!myInventory.ContainsKey(itemName) || myInventory[itemName] <= 0)
        {
            MessageText.text = "판매할 아이템이 부족합니다.";
            return;
        }

        // 인벤토리 차감
        myInventory[itemName]--;
        string inventoryJson = JsonConvert.SerializeObject(myInventory);

        string marketKey = reference.Child("Market").Push().Key;

        Dictionary<string, object> marketInfo = new Dictionary<string, object>();
        marketInfo["SellerKey"] = userKey;
        marketInfo["ItemName"] = itemName;
        marketInfo["Price"] = price;

        Dictionary<string, object> updates = new Dictionary<string, object>();
        // 내 인벤토리 업데이트
        updates["/UserInfo/" + userKey + "/Inventory"] = inventoryJson;
        // 거래소에 등록
        updates["/Market/" + marketKey] = marketInfo;

        reference.UpdateChildrenAsync(updates).ContinueWith(task => {
            if (task.IsCompleted) dispatcher.Enqueue(() => MessageText.text = "거래소 등록 완료!");
        });
    }

    // 2. 다른 유저의 아이템 구매
    public void BuyMarketItem(string marketItemKey, string sellerKey, string itemName, int price, int myCoin, Dictionary<string, int> myInventory)
    {
        if (myCoin < price)
        {
            MessageText.text = "코인이 부족합니다.";
            return;
        }

        // 판매자의 현재 코인을 읽어옴
        reference.Child("UserInfo").Child(sellerKey).Child("Coin").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted) return;
            int sellerCoin = int.Parse(task.Result.Value.ToString());

            // 내 코인 감소, 인벤토리 증가
            myCoin -= price;
            if (myInventory.ContainsKey(itemName)) myInventory[itemName]++;
            else myInventory.Add(itemName, 1);

            string buyerInventoryJson = JsonConvert.SerializeObject(myInventory);

            Dictionary<string, object> updates = new Dictionary<string, object>();

            // 구매자(나) 데이터 갱신
            updates["/UserInfo/" + userKey + "/Coin"] = myCoin;
            updates["/UserInfo/" + userKey + "/Inventory"] = buyerInventoryJson;

            // 판매자 코인 증가
            updates["/UserInfo/" + sellerKey + "/Coin"] = sellerCoin + price;

            // 거래소 글 삭제
            updates["/Market/" + marketItemKey] = null;

            reference.UpdateChildrenAsync(updates).ContinueWith(updateTask =>
            {
                if (updateTask.IsCompleted)
                    dispatcher.Enqueue(() => MessageText.text = "거래소 아이템 구매 완료!");
            });
        });
    }
}