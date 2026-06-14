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

    string userKey;

    void Start()
    {
        database = FirebaseDatabase.GetInstance("" +
            "https://shinguproject-default-rtdb.asia-southeast1.firebasedatabase.app/");
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
        userKey = PlayerPrefs.GetString("UserKey");
    }

    // 게임 종료 시 호출할 함수 (예: 버튼 클릭 이벤트에 연결)
    public void OnClickSaveResult()
    {
        if (string.IsNullOrEmpty(userKey)) return;

        // 임의의 획득 점수와 보상 코인 (실제 게임 로직에 맞춰 수정)
        int newScore = Random.Range(10, 500);
        int rewardCoin = 50;

        reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted) return;

            DataSnapshot snapshot = task.Result;
            int currentScore = int.Parse(snapshot.Child("Score").Value.ToString());
            int currentCoin = int.Parse(snapshot.Child("Coin").Value.ToString());

            int finalCoin = currentCoin + rewardCoin;
            int finalScore = currentScore;
            bool isHighScore = false;

            // 핵심 로직: 기존 점수보다 높을 때만 갱신
            if (newScore > currentScore)
            {
                finalScore = newScore;
                isHighScore = true;
            }

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
                            MessageText.text = $"★최고 점수({finalScore}) 갱신★ 보상 코인: {rewardCoin}";
                        else
                            MessageText.text = $"게임 완료. 내 최고 점수: {currentScore} / 보상 코인: {rewardCoin}";
                    });
                }
            });
        });
    }
}