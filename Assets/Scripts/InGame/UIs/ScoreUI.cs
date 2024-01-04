using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace InGame.UI
{
    /// <summary>
    /// 得点を表示するUI
    /// </summary>
    public class ScoreUI : MonoBehaviour
    {
        private void Start()
        {
            //初期化処理
            UpdateScore(GameManager.score);
        }

        /// <summary>
        /// 表示を更新
        /// </summary>
        public void UpdateScore(int score)
        {
            this.GetComponent<TextMeshProUGUI>().text = MakeText(score);
        }

        /// <summary>
        /// 表示する文字列を作る
        /// </summary>
        private string MakeText(int score)
        {
            return score.ToString() + " pt";
        }
    }
}