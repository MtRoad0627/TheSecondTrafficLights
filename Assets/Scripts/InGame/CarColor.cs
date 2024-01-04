using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    /// <summary>
    /// 車のカラーリング
    /// Carクラスに命じられて動作する。
    /// このクラス自体は色を変えない（継承クラスが色を変える）
    /// </summary>
    public class CarColor : MonoBehaviour
    {
        /// <summary>
        /// 色を更新
        /// </summary>
        /// <param name="happinessRatio">満足度の最大値に対する割合</param>
        public void UpdateColor(float happinessRatio)
        {
            Color color = CalculateColor(happinessRatio);

            SetColor(color);
        }

        /// <summary>
        /// 幸福度を色に変換
        /// </summary>
        protected virtual Color CalculateColor(float happinessRatio)
        {
            //緑→黄→赤

            float b = 0f;
            float r, g;

            if (happinessRatio < 0.5f)
            {
                r = 1f;
                g = happinessRatio / 0.5f;
            }
            else
            {
                g = 1f;
                r = (1f - happinessRatio) / 0.5f;
            }

            return new Color(r, g, b);
        }

        /// <summary>
        /// 実際に色を変える
        /// </summary>
        protected virtual void SetColor(Color color){}
    }
}