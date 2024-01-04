using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using InGame;

namespace Result
{
    /// <summary>
    /// ÅI“I‚È“¾“_‚ð•\Ž¦
    /// </summary>
    public class ResultPoint : MonoBehaviour
    {
        void Start()
        {
            GetComponent<TextMeshProUGUI>().text = GameManager.score.ToString();
        }
    }
}