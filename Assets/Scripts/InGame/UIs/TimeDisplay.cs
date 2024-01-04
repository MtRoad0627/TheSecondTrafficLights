using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace InGame.UI
{
    /// <summary>
    /// 残り時間を表示
    /// </summary>
    public class TimeDisplay : MonoBehaviour
    {
        [Tooltip("残り時間がこれ未満になると赤くなる")]
        [SerializeField] private float timeRed = 10f;

        void Update()
        {
            GetComponent<TextMeshProUGUI>().text = MakeText();

            //残り10秒未満になると赤くする
            if(GameManager.Instance.gameTimeLeft < timeRed)
            {
                GetComponent<TextMeshProUGUI>().color = Color.red;
            }
        }

        private string MakeText()
        {
            string output = "";

            int currentTime = (int)GameManager.Instance.gameTimeLeft + 1;

            int minute = currentTime / 60;
            output += minute.ToString();

            output += " : ";

            int second = currentTime % 60;
            if(second < 10)
            {
                //「08」のように表示させる
                output += "0";
            }
            output += second;

            return output;
        }
    }
}