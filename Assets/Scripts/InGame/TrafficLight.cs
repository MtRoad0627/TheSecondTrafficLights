using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class TrafficLight : MonoBehaviour
    {
        public enum Color
        {
            green,
            yellow,
            red
        }

        public Color color { get; private set; }

        /// <summary>
        /// 色を切り替え
        /// </summary>
        public void SetLight(Color color)
        {
            this.color = color;
        }
    }
}
