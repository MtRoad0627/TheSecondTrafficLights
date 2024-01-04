using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace InGame.UI
{
    /// <summary>
    /// 加点時に、フェードアウトしていくUI。
    /// Animationベースで動く
    /// </summary>
    public class ScoreAdditionUI : MonoBehaviour
    {
        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="scoreAdditional">加点された量</param>
        public void Initialize(int scoreAdditional)
        {
            GetComponent<TextMeshProUGUI>().text = MakeText(scoreAdditional);
        }

        /// <summary>
        /// アニメーションが終わったら、消える
        /// </summary>
        public void OnAnimationFinished()
        {
            Destroy(this.gameObject);
        }

        private string MakeText(int score)
        {
            return "+" + score.ToString();
        }
    }
}