using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    /// <summary>
    /// 信号機。
    /// 各道路が両端に２つずつ、無効化された信号機を持つ。
    /// TrafficLightsSystemを持つIntersectionに接続することで、有効かされる。
    /// </summary>
    public class TrafficLight : MonoBehaviour
    {
        public enum Color
        {
            green,
            yellow,
            red
        }

        public Color color { get; private set; }

        //各信号表示時に表示するSprite
        [System.Serializable]
        private class LightColor
        {
            public Color color;
            public Sprite sprite;
        }

        [Tooltip("各色に対応して表示するスプライト")]
        [SerializeField] private LightColor[] lightColors;

        /// <summary>
        /// 色を切り替え
        /// </summary>
        public void SetLight(Color color)
        {
            //色をセット
            this.color = color;

            //その色を表示
            ShowColor(color);
        }

        /// <summary>
        /// 指定された色を表示する
        /// </summary>
        private void ShowColor(Color color)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

            //対応するスプライトを探す
            foreach(LightColor lightColor in lightColors)
            {
                if(lightColor.color == color)
                {
                    //>>対応するスプライト

                    //表示する
                    spriteRenderer.sprite = lightColor.sprite;

                    return;
                }
            }
        }
    }
}