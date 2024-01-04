using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Opening
{
    /// <summary>
    /// 押したらシーン遷移するボタン
    /// </summary>
    public class SceneButton : MonoBehaviour
    {
        [SerializeField] private string sceneName;

        /// <summary>
        /// クリックされた際の処理
        /// </summary>
        public void OnClick()
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}