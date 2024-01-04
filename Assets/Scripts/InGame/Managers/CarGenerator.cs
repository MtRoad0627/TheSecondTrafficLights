using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class CarGenerator : SingletonMonoBehaviour<CarGenerator>
    {
        [Tooltip("車のプレハブ")]
        [SerializeField] private GameObject carPrefab;
        [Tooltip("生成されるCarオブジェクトの親オブジェクト")]
        [SerializeField] private Transform carParent;

        [Tooltip("1秒に出現する車の期待数")]
        [SerializeField] private float expectedCars = 1f;
        [Tooltip("1秒に出現する車の期待数の範囲（片側の範囲の長さ）")]
        [SerializeField] private float expectedCarsRange = 0.2f;

        [Tooltip("スポーンポイント（各OutsideConnectionと繋がる端の車線ごと）固有の生成インターバル")]
        [SerializeField] private float spawnIntervalInPoint = 1f;

        [Tooltip("最後尾の車がこの距離未満だと生成しない")]
        [SerializeField] private float notSpawningDistance = 1f;

        private SpawnPoint[] spawnPoints;

        private bool initialized = false;

        public int spawnedN { get; private set; } = 0;

        private void Update()
        {
            if (initialized 
                && (GameManager.Instance.sequence == GameManager.Sequence.playing))
            {
                AdvanceSpawnPointsTimers();
                SpawnCars();
            }
        }

        /// <summary>
        /// 初期化処理。道路接続後に行われる必要がある
        /// </summary>
        public static void Initialize()
        {
            //スポーン地点を取得
            Instance.spawnPoints = GetSpawnPoints();

            //初期化済みに
            Instance.initialized = true;
        }

        /// <summary>
        /// 各SpawnPointのタイマーを進める
        /// </summary>
        private void AdvanceSpawnPointsTimers()
        {
            foreach(SpawnPoint spawnPoint in spawnPoints)
            {
                spawnPoint.AdvanceTimer();
            }
        }

        /// <summary>
        /// スポーン地点を取得
        /// </summary>
        private static SpawnPoint[] GetSpawnPoints()
        {
            List<SpawnPoint> pointsList = new List<SpawnPoint>();
            
            //全OutsideConnectionから
            foreach(OutsideConnection outsideConnection in FindObjectsOfType<OutsideConnection>())
            {
                //各道路について
                foreach(Road road in outsideConnection.connectedRoads)
                {
                    //このOutsideConnectionのedge番号
                    uint edge = road.GetEdgeID(outsideConnection);

                    //各車線について
                    for (uint lane = 0; lane < road.lanes; lane++)
                    {
                        //位置を取得
                        Vector2 spawnPosition = road.GetStartingPoint(edge, lane);

                        //SpawnPointを登録
                        SpawnPoint newPoint = new SpawnPoint(outsideConnection, road, lane, spawnPosition, Instance.spawnIntervalInPoint);
                        pointsList.Add(newPoint);
                    }
                }
            }

            return pointsList.ToArray();
        }

        /// <summary>
        /// 車を生成する。Update関数から呼ぶこと
        /// </summary>
        private void SpawnCars()
        {
            //これから生成する車数
            int spawningCars = GetSpawningCarsN();

            //使用するSpawnPointの順番を決める
            Queue<SpawnPoint> spawnPointsOrder = new Queue<SpawnPoint>(ShuffleSpawnPoints(spawnPoints));

            //指定された車の数生成する
            for(int carNum = 0; carNum < spawningCars; carNum++)
            {
                //使用可能なSpawnPoint
                SpawnPoint spawnPoint = GetAvailableSpawnPoint(spawnPointsOrder);
                
                //使用可能なspawnPointがなくなったら中止
                if (spawnPoint == null)
                {
                    break;
                }

                //オブジェクト生成
                InstantiateCar(spawnPoint);

                //数える
                spawnedN++;
            }
        }

        /// <summary>
        /// スポーン数を算出
        /// </summary>
        private int GetSpawningCarsN()
        {
            //期待値の範囲
            float expectedMin = expectedCars - expectedCarsRange;
            float expectedMax = expectedCars + expectedCarsRange;

            //このフレームに出すべき車の量を算出
            float generatingCarsF = Random.Range(expectedMin, expectedMax) * Time.deltaTime;

            //>>整数に
            int generatingCarsI = (int)generatingCarsF;

            //端数の処理
            if (Random.value <= generatingCarsF - generatingCarsI)
            {
                generatingCarsI++;
            }

            return generatingCarsI;
        }

        /// <summary>
        /// 使用するSpawnPointの順番を決める
        /// </summary>
        private SpawnPoint[] ShuffleSpawnPoints(SpawnPoint[] spawnPoints)
        {
            SpawnPoint[] _spawnPoints = (SpawnPoint[])spawnPoints.Clone();
            
            //シャッフルする
            for(int cnt = 0; cnt < _spawnPoints.Length; cnt++)
            {
                SpawnPoint temp = _spawnPoints[cnt];
                int randomIndex = Random.Range(0, _spawnPoints.Length);
                _spawnPoints[cnt] = _spawnPoints[randomIndex];
                _spawnPoints[randomIndex] = temp;
            }

            //Queueにする
            return _spawnPoints;
        }

        /// <summary>
        /// 使用可能なSpawnPointを返す
        /// </summary>
        /// <return>使用可能なSpawnPointがなくなったらnull</return>
        private SpawnPoint GetAvailableSpawnPoint(Queue<SpawnPoint> spawnPoints)
        {
            //キューがなくなるまで
            while(spawnPoints.Count > 0)
            {
                //キューの先頭を取り出し
                SpawnPoint checking = spawnPoints.Dequeue();

                //使用可能か確認
                if (CheckSpawnPointAvailable(checking))
                {
                    //>>使用可能
                    
                    //タイマーリセット
                    checking.ResetTimer();

                    //返す
                    return checking;
                }

                //>>使用可能でない⇒次へ
            }

            //ここまで来たら、使用可能なSpawnPointがない
            return null;
        }

        /// <summary>
        /// スポーンポイントが利用可能か確認する
        /// </summary>
        private bool CheckSpawnPointAvailable(SpawnPoint spawnPoint)
        {
            //タイマーについて
            if(spawnPoint.timer < spawnIntervalInPoint)
            {
                //>>タイマーがまだ満了していない
                return false;
            }
            //>>タイマーが満了している

            //最後尾との距離について
            Car tailCar = GetTailCar(spawnPoint);
            if(tailCar != null)
            {
                //>>最後尾が存在
                float distance = Vector2.Distance(tailCar.transform.position, spawnPoint.position);
                if (distance <= notSpawningDistance)
                {
                    //近すぎる
                    return false;
                }
            }

            //可能
            return true;
        }

        /// <summary>
        /// 最後尾の車を取得する
        /// </summary>
        /// <returns>存在しない場合はnull</returns>
        private Car GetTailCar(SpawnPoint spawnPoint)
        {
            //検出ビーム発射
            Road road = spawnPoint.road;
            Vector2 alongVector = road.alongVectors[road.GetEdgeID(spawnPoint.roadJoint)];
            RaycastHit2D[] hitteds = Physics2D.RaycastAll(spawnPoint.position, alongVector, alongVector.magnitude);

            //最も近いものを探す
            float nearestDistance = float.MaxValue;
            Car nearestCar = null;
            foreach(RaycastHit2D hitted in hitteds)
            {
                GameObject opponent = hitted.collider.gameObject;

                //Carオブジェクトのみ
                Car car = opponent.GetComponent<Car>();
                if (car == null)
                {
                    //>>Carでない
                    //飛ばす
                    continue;
                }
                //>>Carである

                //今考えている道路に存在しているか
                if(car.currentRoad != spawnPoint.road)
                {
                    //>>違う道路
                    //飛ばす
                    continue;
                }

                //距離を求める
                float distance = Vector2.Distance(spawnPoint.position, car.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestCar = car;
                }
            }

            return nearestCar;
        }

        /// <summary>
        /// 車オブジェクトを生成
        /// </summary>
        private void InstantiateCar(SpawnPoint spawnPoint)
        {
            //オブジェクト生成
            GameObject carObject = Instantiate(carPrefab, carParent);

            //初期化（位置合わせもCarクラスが担う）
            carObject.GetComponent<Car>().Initialize(spawnPoint.roadJoint, spawnPoint.road, spawnPoint.laneID, null);
        }

        /// <summary>
        /// スポーン地点の管理
        /// </summary>
        private class SpawnPoint
        {
            public RoadJoint roadJoint { get; private set; }
            public Road road { get; private set; }
            public uint laneID { get; private set; }
            public Vector2 position { get; private set; }

            public float timer { get; private set; }

            //コンストラクタ
            public SpawnPoint(RoadJoint roadJoint, Road road, uint laneID, Vector2 position, float timerMax)
            {
                this.roadJoint = roadJoint;
                this.road = road;
                this.laneID = laneID;
                this.position = position;
                this.timer = timerMax;      //最初から使用可能に
            }

            /// <returns>現在のタイマー</returns>
            public float AdvanceTimer()
            {
                timer += Time.deltaTime;
                return timer;
            }

            public void ResetTimer()
            {
                timer = 0f;
            }
        }
    }
}