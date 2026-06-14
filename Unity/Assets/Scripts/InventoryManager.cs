using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;
using Unity.VisualScripting;

public class InventoryManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("UI")]
    [SerializeField] Text IceCrystalCountText;
    [SerializeField] Text FireScrollCountText;
    [SerializeField] Text GoldKeyCountText;
    [SerializeField] Text Unit1StatusText;
    [SerializeField] Text Unit2StatusText;
    [SerializeField] Text Unit3StatusText;
    [SerializeField] Text MessageText;

    string userKey;

    Dictionary<string, int> inventory = new Dictionary<string, int>();
    Dictionary<string, bool> unitList = new Dictionary<string, bool>();

    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://shinguproject-default-rtdb.asia-southeast1.firebasedatabase.app/"
            );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
        LoadInventory();
    }

    public void LoadInventory()
    {
        userKey = PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            MessageText.text = "로그인정보가 없습니다.";
            return;
        }

        // 💡 핵심 변경: Child("Inventory")를 지우고 유저 노드 전체를 가져옴
        reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "데이터 불러오기 실패";
                });
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Value == null)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "유저 데이터가 없습니다.";
                    });
                    return;
                }

                if (snapshot.HasChild("Inventory"))
                {
                    string inventoryJson = snapshot.Child("Inventory").Value.ToString();
                    inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);
                }
                if (snapshot.HasChild("UnitList"))
                {
                    string unitListJson = snapshot.Child("UnitList").Value.ToString();
                    unitList = JsonConvert.DeserializeObject<Dictionary<string, bool>>(unitListJson);
                }
                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = "인벤토리 및 유닛 불러오기 완료";
                });
            }
        });
    }

    int GetItemCount(string itemName)
    {
        if(inventory.ContainsKey(itemName))
        {
            return inventory[itemName];
        }
        return 0;
    }
    string GetUnitStatus(string unitName)
    {
        if (unitList.ContainsKey(unitName) && unitList[unitName] == true)
        {
            return "보유 중";
        }
        return "미보유";
    }
    void RefreshUI()
    {
        IceCrystalCountText.text = "IceCrystal : " + GetItemCount("IceCrystal");
        FireScrollCountText.text = "FireScroll : " + GetItemCount("FireScroll");
        GoldKeyCountText.text = "GoldKey : " + GetItemCount("GoldKey");
        Unit1StatusText.text = "unit1 : " + GetUnitStatus("unit1");
        Unit2StatusText.text = "unit2 : " + GetUnitStatus("unit2");
        Unit3StatusText.text = "unit3 : " + GetUnitStatus("unit3");
    }

    void UseItem(string itemName)
    {
        if(!inventory.ContainsKey(itemName))
        {
            MessageText.text = itemName + "아이템이 없습니다.";
            return;
        }

        if (inventory[itemName] <= 0)
        {
            MessageText.text = itemName + "개수가 부족합니다.";
            return;
        }

        inventory[itemName]--;

        SaveInventory(itemName);
    }

    public void OnClickUseIceCrystal()
    {
        UseItem("IceCrystal");
    }

    public void OnClickUseFireScroll()
    {
        UseItem("FireScroll");
    }

    public void OnClickUseGoldKey()
    {
        UseItem("GoldKey");
    }

    void SaveInventory(string userItemname)
    {
        string inventoryJson = JsonConvert.SerializeObject(inventory);

        reference.Child("UserInfo").Child(userKey).Child("Inventory").SetValueAsync(inventoryJson).ContinueWith(task =>
        {
        if (task.IsFaulted)
        {
            dispatcher.Enqueue(() =>
            {
                MessageText.text = "인벤토리 저장 실패";

            });
            return;
        }

        if (task.IsCompleted)
        {
            dispatcher.Enqueue(() =>
            {
                RefreshUI();
                MessageText.text = userItemname + "사용 완료";
            });
            }
        });
    }
}
