using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InGame;

public class TotalEval : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI colorText;
    [SerializeField] Image colorCircle;
    // Start is called before the first frame update
    void Start()
    {
        int totalScore = GameManager.score + GameManager.bonus;
        var red = new Color(0.88f, 0.27f, 0.27f);
        var orange = new Color(0.92f, 0.65f, 0.24f);
        var yellow = new Color(0.85f, 0.83f, 0.46f);
        string s;
        if (totalScore < GameManager.bThreahold) {
            colorText.color = yellow;
            colorCircle.color = yellow;
            s = "C";
        }
        else if (totalScore < GameManager.aThreahold) {
            colorText.color = orange;
            colorCircle.color = orange;
            s = "B";
        }
        else {
            colorText.color = red;
            colorCircle.color = red;
            s = "A";
        }
        colorText.text = s;
    }
}