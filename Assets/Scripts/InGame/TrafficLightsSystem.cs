using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace InGame
{
    /// <summary>
    /// Intersectionに一対一対応
    /// 複数のTrafficLightを取りまとめる
    /// プレイヤーの入力は本クラスが受け取る
    /// </summary>
    public class TrafficLightsSystem : MonoBehaviour
    {
        [Tooltip("対応する信号機")]
        [SerializeField] private TrafficLight[] trafficLights;

        private Dictionary<Road, TrafficLight> correspondingTrafficLight = new Dictionary<Road, TrafficLight>();

        public enum States
        {
            initializing,
            still,
            yellowChanging
        }

        public States state { get; private set; } = States.initializing;

        /// <summary>
        /// 緑色になっている信号機の組み合わせ
        /// </summary>
        public enum GreenPattern
        {
            odd,
            even
        }

        [Tooltip("初期状態で緑信号になるもの")]
        [SerializeField] private GreenPattern initiallyGreen = GreenPattern.even;

        //現在緑になっているパターン
        private GreenPattern currentPattern;

        [Tooltip("黄色信号になっている時間（秒）")]
        [SerializeField] private float yellowTime = 1.5f;

        /// <summary>
        /// 起動済みのTrafficLightを登録。各TrafficLightの初期化処理もする
        /// </summary>
        /// <param name="roads">時計回りに登録すること</param>
        public void RegisterTrafficLights(Road[] roads, Dictionary<Road, int> edges)
        {
            //時計回り順にTrafficLightを起動・登録
            trafficLights = new TrafficLight[roads.Length];
            for(int cnt = 0; cnt < roads.Length; cnt++)
            {
                //TrafficLightを登録
                trafficLights[cnt] = roads[cnt].ActivateTrafficLight(edges[roads[cnt]]);

                //Roadも登録
                correspondingTrafficLight[roads[cnt]] = trafficLights[cnt];
            }

            //並び替え
            trafficLights = RearrangeLight(trafficLights);

            //初期色をセット
            SetInitialLight();

            //初期化完了
            state = States.still;
        }

        /// <summary>
        /// 信号の数にあわせて配列内の信号の順番をずらす
        /// </summary>
        private TrafficLight[] RearrangeLight(TrafficLight[] trafficLights)
        {
            TrafficLight[] newArray = new TrafficLight[trafficLights.Length];

            //n番目の方位角
            float[] angles = new float[trafficLights.Length];
            for(int cnt = 0; cnt < trafficLights.Length; cnt++)
            {
                angles[cnt] = MyMath.GetAngular(trafficLights[cnt].transform.position - transform.position);
            }

            //n -> n+1 の角度差
            float[] angularDifferences = new float[trafficLights.Length];
            for(int cnt = 0; cnt < trafficLights.Length -1; cnt++)
            {
                angularDifferences[cnt] = angles[cnt+1] - angles[cnt];
            }
            angularDifferences[trafficLights.Length - 1] = (angles[0] + 360) - angles[trafficLights.Length - 1];

            //信号数によって場合分け
            switch (trafficLights.Length)
            {
                case 3:
                    //>>一番開いた角のものが同時に緑or赤になるようにする
                    //最大角を探す
                    float angleMax = 0f;
                    int indexMax = -1;
                    for(int cnt = 0; cnt < trafficLights.Length; cnt++)
                    {
                        if (angleMax < angularDifferences[cnt])
                        {
                            angleMax = angularDifferences[cnt];
                            indexMax = cnt;
                        }
                    }

                    //indexMaxを2番目になるようにずらせば良い
                    int addition = 2 - indexMax;
                    for(int oldIndex = 0; oldIndex < trafficLights.Length; oldIndex++)
                    {
                        int newIndex = (oldIndex + addition) % 3;
                        newArray[newIndex] = trafficLights[oldIndex];
                    }

                    break;

                default:
                    newArray = trafficLights;
                    break;
            }

            return newArray;
        }

        /// <summary>
        /// TrafficLightの初期色をセット
        /// 偶数あるいは奇数のTrafficLightを緑にし、残りを赤にする
        /// </summary>
        private void SetInitialLight()
        {
            //信号機の色をセット
            SetLightsStill(initiallyGreen);

            //パターンを記憶
            currentPattern = initiallyGreen;
        }

        /// <summary>
        /// Still状態（緑・赤）のパターンになるように信号機の色をセットする
        /// </summary>
        private void SetLightsStill(GreenPattern greenPattern)
        {
            //緑・赤信号を取得
            TrafficLight[] greens = GetGreenLightsInPattern(greenPattern);
            TrafficLight[] reds = GetRedLightsInPattern(greenPattern);

            //緑信号をセット
            foreach(TrafficLight light in greens)
            {
                light.SetLight(TrafficLight.Color.green);
            }

            //赤信号をセット
            foreach(TrafficLight light in reds)
            {
                light.SetLight(TrafficLight.Color.red);
            }
        }

        /// <summary>
        /// YellowChanging状態（黄色・赤）のパターンになるように信号機の色をセットする
        /// </summary>
        private void SetLightsYellow(GreenPattern greenPattern)
        {
            //黄・赤信号を取得
            //GreenPatternにおける緑を黄色にセットにすればいい
            TrafficLight[] yellows = GetGreenLightsInPattern(greenPattern);
            TrafficLight[] reds = GetRedLightsInPattern(greenPattern);

            //黄信号をセット
            foreach (TrafficLight light in yellows)
            {
                light.SetLight(TrafficLight.Color.yellow);
            }

            //赤信号をセット
            foreach (TrafficLight light in reds)
            {
                light.SetLight(TrafficLight.Color.red);
            }
        }

        /// <summary>
        /// 与えられたパターンで緑信号になる信号を返す
        /// </summary>
        private TrafficLight[] GetGreenLightsInPattern(GreenPattern greenPattern)
        {
            List<TrafficLight> output = new List<TrafficLight>();

            //線形探索
            for (int cnt = 0; cnt < trafficLights.Length; cnt++)
            {
                switch (greenPattern)
                {
                    case GreenPattern.even:
                        if (cnt % 2 == 0)
                        {
                            output.Add(trafficLights[cnt]);
                        }
                        break;

                    case GreenPattern.odd:
                        if (cnt % 2 == 1)
                        {
                            output.Add(trafficLights[cnt]);
                        }
                        break;
                }
            }

            return output.ToArray();
        }

        /// <summary>
        /// 与えられたパターンで赤信号になる信号機を返す
        /// </summary>
        private TrafficLight[] GetRedLightsInPattern(GreenPattern greenPattern)
        {
            //緑信号を取得
            TrafficLight[] green = GetGreenLightsInPattern(greenPattern);

            //余事象（赤）を取得
            List<TrafficLight> output = new List<TrafficLight>();
            foreach(TrafficLight light in trafficLights)
            {
                if (!green.Contains(light))
                {
                    //>>緑信号ではない

                    //登録
                    output.Add(light);
                }
            }

            return output.ToArray();
        }

        /// <summary>
        /// 信号機切り替え
        /// </summary>
        public void ToggleLights()
        {
            //Still状態じゃなければキャンセル
            //黄色信号中に重複を受け付けない
            if (state != States.still)
            {
                return;
            }

            //黄色信号を始める
            StartYellow();

            //信号切り替えを予約
            Invoke(nameof(StartNextPatternLights), yellowTime);
        }

        /// <summary>
        /// 黄色信号が終わり、次のパターンの信号を表示する
        /// </summary>
        public void StartNextPatternLights()
        {
            //ステート上のガード処理
            if (state != States.yellowChanging)
            {
                return;
            }

            //次のパターンへ移る
            currentPattern = GetNextPattern(currentPattern);

            //そのパターンの緑赤信号を表示
            SetLightsStill(currentPattern);

            //緑赤ステートに
            state = States.still;
        }

        /// <summary>
        /// 黄色信号を始める
        /// </summary>
        private void StartYellow()
        {
            //黄色信号ステートに
            state = States.yellowChanging;

            //黄色信号に
            SetLightsYellow(currentPattern);
        }

        /// <summary>
        /// 指定されたパターンの次のパターンを返す
        /// 指定されたのが最後ならば最初のパターンを返す
        /// </summary>
        private GreenPattern GetNextPattern(GreenPattern pattern)
        {
            //引数が何番目かを取得する
            GreenPattern[] allPatterns = Enum.GetValues(typeof(GreenPattern)).Cast<GreenPattern>().ToArray();
            int thisIndex = Array.IndexOf(allPatterns, pattern);

            if (thisIndex < allPatterns.Length - 1)
            {
                return allPatterns[thisIndex + 1];
            }
            else
            {
                return allPatterns[0];
            }
        }
    }
}