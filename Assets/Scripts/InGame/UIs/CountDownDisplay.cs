using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace InGame.UI
{
    /// <summary>
    /// ƒJƒEƒ“ƒgƒ_ƒEƒ“‚ð•\Ž¦
    /// </summary>
    public class CountDownDisplay : MonoBehaviour
    {
        private void Update()
        {
            GetComponent<TextMeshProUGUI>().text = ((int)GameManager.Instance.countDownTimeLeft + 1).ToString();
        }
    }
}