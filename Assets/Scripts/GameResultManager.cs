using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections.Generic;

public class GameResultManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("UI")]
    [SerializeField] Text MessageText;
    [SerializeField] MainUIManager mainUIManager;

    string userKey;

    void Start()
    {
        database = FirebaseDatabase.GetInstance("" +
            "https://shinguproject-default-rtdb.asia-southeast1.firebasedatabase.app/");
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
        userKey = PlayerPrefs.GetString("UserKey");
    }

    public void OnClickSaveResult()
    {
        if (string.IsNullOrEmpty(userKey)) return;

        int newScore = Random.Range(10, 500);

        reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted) return;

            DataSnapshot snapshot = task.Result;
            int currentScore = int.Parse(snapshot.Child("Score").Value.ToString());
            int currentCoin = int.Parse(snapshot.Child("Coin").Value.ToString());

            int finalScore = currentScore;
            int rewardCoin = 0;
            bool isHighScore = false;

            if (newScore > currentScore)
            {
                finalScore = newScore;
                rewardCoin = 1000;
                isHighScore = true;
            }
            else
            {
                rewardCoin = 50;
            }

            int finalCoin = currentCoin + rewardCoin;

            Dictionary<string, object> updateData = new Dictionary<string, object>();
            updateData["Coin"] = finalCoin;
            updateData["Score"] = finalScore;

            reference.Child("UserInfo").Child(userKey).UpdateChildrenAsync(updateData).ContinueWith(saveTask =>
            {
                if (saveTask.IsCompleted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        if (isHighScore)
                        {
                            MessageText.text = $"최고 점수({finalScore}) 갱신 보상 코인: {rewardCoin}";
                        }
                        else
                        {
                            MessageText.text = $"게임 완료. 획득 점수: {newScore} (최고점수: {currentScore}) / 보상 코인: {rewardCoin}";
                        }

                        if (mainUIManager != null)
                        {
                            mainUIManager.UpdateGlobalCoin(finalCoin);
                            mainUIManager.UpdateGlobalScore(finalScore);
                        }
                    });
                }
            });
        });
    }
}