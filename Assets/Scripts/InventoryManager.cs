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
    [SerializeField] Text WarriorStatusText;
    [SerializeField] Text ArcherStatusText;
    [SerializeField] Text GarderStatusText;
    [SerializeField] Text MessageText;

    string userKey;

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
        LoadInventory();
    }

    void LoadInventory()
    {
        userKey = PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            MessageText.text = "로그인정보가 없습니다.";
            return;
        }

        reference.Child("UserInfo").Child(userKey).Child("Inventory").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "인벤토리 불러오기 실패";
                });
                return;
            }

            if(task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if(snapshot.Value == null)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "인벤토리 데이터가 없습니다.";
                    });
                    return;
                }
                string inventoryJson = snapshot.Value.ToString();

                inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = "인벤토리 불러오기 완료";
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
        if (WarriorStatusText != null)
            WarriorStatusText.text = "전사 : " + GetUnitStatus("warrior");

        if (ArcherStatusText != null)
            ArcherStatusText.text = "궁수 : " + GetUnitStatus("archer");

        if (GarderStatusText != null)
            GarderStatusText.text = "가더 : " + GetUnitStatus("garder");
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
