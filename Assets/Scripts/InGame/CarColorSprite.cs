using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    /// <summary>
    /// 車のカラーリング
    /// Carクラスに命じられて動作する。
    /// これはSprite用
    /// </summary>
    public class CarColorSprite : CarColor
    {
        /// <summary>
        /// 実際に色を変える
        /// </summary>
        protected override void SetColor(Color color)
        {
            GetComponent<SpriteRenderer>().color = color;
        }
    }
}