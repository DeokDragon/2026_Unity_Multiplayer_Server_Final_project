using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;
using Unity.VisualScripting;

public class ShopManager : MonoBehaviour
{

    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("UI")]
    [SerializeField] Text CoinText;
    [SerializeField] Text MessageText;

    string userKey;

    int currentCoin;
    Dictionary<string, int> inventory = new Dictionary<string, int>();
    Dictionary<string, bool> unitList = new Dictionary<string, bool>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://shinguproject-default-rtdb.asia-southeast1.firebasedatabase.app/"
            );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
        LoadUserData();
    }

    public void LoadUserData()
    {
        userKey = PlayerPrefs.GetString("UserKey");

        if(string.IsNullOrEmpty(userKey) )
        {
            MessageText.text = "로그인 정보가 없습니다.";
            return;
        }

        reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "유저 정보 불러오기 실패";
                });
                return;
            }

            if(task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                currentCoin = int.Parse(snapshot.Child("Coin").Value.ToString());
                string inventoryJson = snapshot.Child("Inventory").Value.ToString();
                string unitListJson = snapshot.Child("UnitList").Value.ToString();

                inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);
                unitList = JsonConvert.DeserializeObject<Dictionary<string, bool>>(unitListJson);

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = "유저 정보 불러오기 완료";
                });
            }
        });
    }

    void RefreshUI()
    {
        CoinText.text = "Coin : " + currentCoin;
    }

    public void OnClickBuyIceCrystal()
    {
        BuyItem("IceCrystal", 150);
    }
    public void OnClickBuyFireScroll()
    {
        BuyItem("FireScroll", 200);
    }
    public void OnClickBuyGoldKey()
    {
        BuyItem("GoldKey", 100);
    }

    public void OnClickBuywarrior()
    {
        BuyUnit("warrior", 200);
    }

    public void OnClickBuyarcher()
    {
        BuyUnit("archer", 300); 
    }

    public void OnClickBuygarder()
    {
        BuyUnit("garder", 400);
    }

    void BuyItem(string itemName, int price)
    {
        if(currentCoin < price)
        {
            MessageText.text = "코인이 부족합니다.";
            return;
        }

        currentCoin -= price;

        if (inventory.ContainsKey(itemName))
        {
            inventory[itemName]++;
        }
        else
        {
            inventory.Add(itemName, 1);
        }

        SaveUserData(itemName);
    }

    void BuyUnit(string unitName, int price)
    {
        if (unitList.ContainsKey(unitName) && unitList[unitName] == true)
        {
            MessageText.text = "이미 보유한 유닛입니다.";
            return;
        }

        if (currentCoin < price)
        {
            MessageText.text = "코인이 부족합니다.";
            return;
        }

        currentCoin -= price;
        unitList[unitName] = true;

        string updatedUnitListJson = JsonConvert.SerializeObject(unitList);

        Dictionary<string, object> updateData = new Dictionary<string, object>();
        updateData["Coin"] = currentCoin;
        updateData["UnitList"] = updatedUnitListJson;

        reference.Child("UserInfo").Child(userKey).UpdateChildrenAsync(updateData).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = unitName + " 구매 완료!";
                });
            }
        });
    }

    void SaveUserData(string boughtItemName)
    {
        string inventoryJson = JsonConvert.SerializeObject(inventory);

        Dictionary<string, object> updateData = new Dictionary<string, object>();

        updateData["Coin"] = currentCoin;
        updateData["Inventory"] = inventoryJson;

        reference.Child("UserInfo").Child(userKey).UpdateChildrenAsync(updateData).ContinueWith(task =>
        {
            if(task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "구매 저장 실패";
                });

                return;
            }

            if (task.IsCompleted)
            {
                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = boughtItemName + "구매 완료";
                });

                
            }
        });
    }
}
