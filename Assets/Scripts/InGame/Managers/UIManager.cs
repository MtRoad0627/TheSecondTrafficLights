using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InGame.UI;

namespace InGame
{
    /// <summary>
    /// UIの統括を行う
    /// </summary>
    public class UIManager : SingletonMonoBehaviour<UIManager>
    {
        [Header("得点表示UI")]
        
        [SerializeField] private ScoreUI scoreUI;

        [Header("加点UI")]

        [Tooltip("プレハブ")]
        [SerializeField] private ScoreAdditionUI scoreAdditionUIPrefab;

        [Header("親")]
        [SerializeField] private Transform scoreAdditionUIParent;

        [Header("時間表示")]

        [SerializeField] private CountDownDisplay countDownDisplay;

        [SerializeField] private TimeDisplay timeDisplay;
        [SerializeField] private Transform timeDisplayParent;

        [SerializeField] private GameObject finishPrefab;
        [SerializeField] private Transform finishUIParent;

        private GameObject currentTimeDisplayObject;

        /// <summary>
        /// 得点更新
        /// </summary>
        public void OnPointsChanged(int changement)
        {
            //更新後の得点を取得
            int currentPoints = GameManager.score;

            //総合得点UIを更新させる
            scoreUI.UpdateScore(currentPoints);

            //加点UI生成
            GenerateAdditionalPoint(changement);
        }

        /// <summary>
        /// 加点UI生成
        /// </summary>
        private void GenerateAdditionalPoint(int additionalPoint)
        {
            //生成
            GameObject ui = Instantiate(scoreAdditionUIPrefab.gameObject, scoreAdditionUIParent);

            //初期化
            ui.GetComponent<ScoreAdditionUI>().Initialize(additionalPoint);
        }

        /// <summary>
        /// カウントダウン終了時処理
        /// </summary>
        public void OnCountDownFinished()
        {
            Destroy(countDownDisplay.gameObject);

            currentTimeDisplayObject = Instantiate(timeDisplay.gameObject, timeDisplayParent);
        }

        /// <summary>
        /// ゲーム終了時処理
        /// </summary>
        public void OnGameFinished()
        {
            Destroy(currentTimeDisplayObject);

            GenerateFinishedUI();
        }

        /// <summary>
        /// 終了時UIを表示
        /// </summary>
        private void GenerateFinishedUI()
        {
            Instantiate(finishPrefab, finishUIParent);
        }
    }
}