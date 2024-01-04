using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class Car : MonoBehaviour
    {
        private enum State
        {
            runningRoad,
            runningJoint,
            changingLane
        }

        [SerializeField] private State state;

        [Header("runningRoadの速度モデル")]

        [Tooltip("反応遅れ時間T（秒）")]
        [SerializeField] private float runningRoadT = 0.74f;

        [Tooltip("希望速度v0（グローバル座標）")]
        [SerializeField] private float runningRoadV0 = 5f;

        [Tooltip("緩和時間t_1（秒）")]
        [SerializeField] private float runningRoadT1 = 2.45f;

        [Tooltip("緩和時間t_2（秒）")]
        [SerializeField] private float runningRoadT2 = 0.77f;

        [Tooltip("相互作用距離R（グローバル座標）")]
        [SerializeField] private float runningRoadR = 1f;

        [Tooltip("相互作用距離R'（グローバル座標）")]
        [SerializeField] private float runningRoadRp = 20f;

        [Tooltip("停車時の車間距離d")]
        [SerializeField] private float runningRoadD = 0.5f;

        [Tooltip("スポーン時の速度の係数（v0に掛け算する）")]
        [SerializeField] private float spawnedSpeedCoef = 0.75f;

        [Tooltip("この角度以内なら他の車が同じ向きを走っていると判断")]
        [SerializeField] private float runningRoadSameDirectionThreshold = 60f;

        [Tooltip("対向車がこの距離以内に来たら停車する。")]
        [SerializeField] private float runningRoadStopDistanceThreshold = 0.5f;

        [Header("runningJointの速度モデル")]

        [Tooltip("反応遅れ時間T（秒）")]
        [SerializeField] private float runningJointT = 0.74f;

        [Tooltip("希望速度v0（グローバル座標）")]
        [SerializeField] private float runningJointV0 = 2f;

        [Tooltip("緩和時間t_1（秒）")]
        [SerializeField] private float runningJointT1 = 2.45f;

        [Tooltip("緩和時間t_2（秒）")]
        [SerializeField] private float runningJointT2 = 0.77f;

        [Tooltip("相互作用距離R（グローバル座標）")]
        [SerializeField] private float runningJointR = 1f;

        [Tooltip("相互作用距離R'（グローバル座標）")]
        [SerializeField] private float runningJointRp = 20f;

        [Tooltip("停車時の車間距離d")]
        [SerializeField] private float runningJointD = 0.5f;

        [Tooltip("この角度以内なら他の車が同じ向きを走っていると判断")]
        [SerializeField] private float runningJointSameDirectionThreshold = 60f;

        [Tooltip("対向車がこの距離以内に来たら停車する。")]
        [SerializeField] private float runningJointStopDistance = 0.5f;

        [Header("changingLaneの速度モデル")]

        [Tooltip("前の車との距離がこれ未満なら停車")]
        [SerializeField] private float changingLaneStopDistance = 0.5f;

        [Header("速度関係")]

        [Tooltip("車線変更時回頭の回転速度")]
        [SerializeField] private float changingLaneRotationSpeed = 10f;

        [Tooltip("車線変更時回転移動の回転速度")]
        [SerializeField] private float changingLaneAngularSpeed = 10f;

        [Header("スコア・満足度関係")]

        [Tooltip("この間隔で速度を記録する（秒）")]
        [SerializeField] private float saveSpeedInterval = 0.5f;

        [Tooltip("満足度を増減させる間隔（秒）")]
        [SerializeField] private float happinessCalculationInterval = 1f;

        [Tooltip("満足度増減の閾値（希望速度に対する割合）")]
        [SerializeField] private float[] happinessChangeThresholds;

        [Tooltip("満足度の変化量")]
        [SerializeField] private int[] happinessChangements;

        [Tooltip("満足度の初期値")]
        [SerializeField] private int happiness = 80;

        private const int happinessMin = 0;
        private const int happinessMax = 100;

        private float saveSpeedTimer = 0f;
        private List<float> savedSpeeds = new List<float>();
        private float happinessCalculationTimer = 0f;

        public float happinessRatio
        {
            get
            {
                return (float)(happiness - happinessMin) / (float)happinessMax;
            }
        }

        /// <summary>
        /// 生成されたRoadJoint
        /// </summary>
        public RoadJoint spawnPoint { get; private set; }

        /// <summary>
        /// 目的地RoadJoint
        /// </summary>
        public RoadJoint destination { get; private set; }

        /// <summary>
        /// この配列の順に沿って走行。Joint曲がり終えた時点で消費
        /// </summary>
        public Queue<Road> routes { get; private set; }

        ///<summary>
        ///現在走っている道路
        ///</summary>
        public Road currentRoad { get; private set; }

        /// <summary>
        /// 現在走っている道路の車線番号
        /// </summary>
        public uint currentLane { get; private set; } = 0;

        /// <summary>
        /// 前フレームまでの速度
        /// </summary>
        public float currentSpeed { get; private set; }

        /// <summary> 
        /// 現在使ってる道沿いベクトル
        /// </summary>
        public Vector2 currentAlongRoad { get; private set; }

        /// <summary>
        /// 入ってきた方のEdgeID。runningRoad開始時に更新
        /// </summary>
        public uint currentEdgeIDFrom { get; private set; }

        /// <summary>
        /// 現在走ってる道で走行した距離
        /// </summary>
        private float currentDistanceInRoad = 0f;

        /// <summary>
        /// この距離まで到達すればrunningRoad終了
        /// </summary>
        private float targetDistanceInRoad = 0f;

        /// <summary>
        /// 現在のJoint移動軌道。Jointを移動し終えた時点で更新し、次のJointの軌道を代入
        /// </summary>
        private CurveRoute currentCurveRoute;

        /// <summary>
        /// RunningJoint中の回転角
        /// </summary>
        private float currentAngle;

        /// <summary>
        /// 次のRunningRoadのlaneID。runningRoad開始時に更新
        /// </summary>
        private uint nextLaneID;

        /// <summary>
        /// 次のRoadJoint。runningRoad開始時に更新
        /// </summary>
        private RoadJoint nextRoadJoint;

        /// <summary>
        /// 次へ向かう道路が平行なとき。trueならrunningJointではなくchangingLaneに移る。
        /// </summary>
        private bool nextIsParallel;

        /// <summary>
        /// 現在考えるべき対象の信号機
        /// </summary>
        private TrafficLight currentTrafficLight;

        /// <summary>
        /// 車両の正面ベクトル
        /// </summary>
        public Vector2 front
        {
            get
            {
                return transform.right;
            }
        }

        [Header("車線変更")]

        [Tooltip("道路の角度（度）がこれ以下なら平行とみなす")]
        [SerializeField] private float roadsParallelThreshold = 10f;

        [Tooltip("車線変更時、目的の車線までの回転半径がこれ以下になったら回転移動による調整を開始する。")]
        [SerializeField] private float thresholdRadiusChangingLane = 5f;

        [Tooltip("車線変更時、目的の車線との最大角度")]
        [SerializeField] private float angleMaxChangingLane = 10f;

        [Tooltip("車線変更時、道路との角度がこれ以下になったら道路と平行とみなす")]
        [SerializeField] private float parallelThresholdChangingLane = 3f;

        [Header("センシング")]
        [Tooltip("検出ビーム始点")]
        [SerializeField] private Transform detectionRayStart;

        [Tooltip("検出ビーム終点・前")]
        [SerializeField] private Transform[] detectionRayDestinationsFront;

        [Tooltip("検出ビーム終点・左前")]
        [SerializeField] private Transform[] detectionRayDestinationsFrontLeft;

        [Tooltip("検出ビーム終点・右前")]
        [SerializeField] private Transform[] detectionRayDestinationsFrontRight;

        [Tooltip("検出ビーム終点・左")]
        [SerializeField] private Transform[] detectionRayDestinationsLeft;

        [Tooltip("検出ビーム終点・右")]
        [SerializeField] private Transform[] detectionRayDestinationsRight;

        [Header("その他")]
        [Tooltip("同一直線上と判断する外積の閾値")]
        [SerializeField] private float onSameLineThreshold = 0.05f;

        [Tooltip("カラーリング")]
        [SerializeField] private CarColor colorObject;

        //検出された車
        private List<Car> carsDetectedFront = new List<Car>();
        private List<Car> carsDetectedFrontLeft = new List<Car>();
        private List<Car> carsDetectedFrontRight = new List<Car>();
        private List<Car> carsDetectedLeft = new List<Car>();
        private List<Car> carsDetectedRight = new List<Car>();

        /// <summary>
        /// 車線変更でカーブモードに入った
        /// </summary>
        private bool changingLaneRotating = false;

        /// <summary>
        /// 車線変更時の円弧軌道
        /// </summary>
        private CurveRoute curveChangingLane;

        private void Start()
        {
            InitializeSpeed();
            colorObject.UpdateColor(happinessRatio);
        }

        private void Update()
        {
            Run();
            Detect();
            ManageHappiness();
        }

        /// <summary>
        /// スポーン時の初速度を設定
        /// </summary>
        private void InitializeSpeed()
        {
            currentSpeed = runningRoadV0 * spawnedSpeedCoef;
        }

        /// <summary>
        /// 走行。前進も曲がりも兼ねる
        /// </summary>
        private void Run()
        {
            switch (state)
            {
                case State.runningRoad:
                    RunRoad();
                    break;

                case State.runningJoint:
                    RunJoint();
                    break;

                case State.changingLane:
                    ChangeLane();
                    break;
            }
        }

        /// <summary>
        /// 検出ビームを発射して、周囲の物体を検出する。
        /// </summary>
        private void Detect()
        {
            DetectCars();
        }

        /// <summary>
        /// 生成時の初期化処理
        /// </summary>
        public void Initialize(RoadJoint spawnPoint, Road spawnRoad, uint spawnLane, RoadJoint destination = null)
        {
            //生成ポイント
            this.spawnPoint = spawnPoint;

            //目的地
            if (destination == null)
            {
                //こちらで目的地を設定
                this.destination = ChooseDestinationRadomly(this.spawnPoint);
            }
            else
            {
                //目的地が指定されている
                this.destination = destination;
            }

            //初期道路は確定しているので、その次のjointからのルートを得る
            this.routes = GetRoute(spawnRoad.GetDiffrentEdge(spawnPoint), this.destination);

            //走り始める
            StartRunningRoad(spawnRoad, spawnLane, spawnPoint, true);
        }

        /// <summary>
        /// ランダムに目的地を決める
        /// </summary>
        private RoadJoint ChooseDestinationRadomly(RoadJoint spawnPoint)
        {
            //spawnPointとの重複を避けるループ
            RoadJoint destination;
            OutsideConnection[] outsideConnectionsAll = FindObjectsOfType<OutsideConnection>();
            while (true)
            {
                //ランダムに目的地を決める
                destination = outsideConnectionsAll[Random.Range(0, outsideConnectionsAll.Length - 1)];

                //重複チェック
                if (destination != spawnPoint)
                {
                    //>>生成ポイントと目的地が重複していない
                    break;
                }
            }

            return destination;
        }

        /// <summary>
        /// ルートを取得
        /// </summary>
        /// <param name="startingPoint">開始位置</param>
        /// <param name="target">目的地</param>
        /// <returns></returns>
        private Queue<Road> GetRoute(RoadJoint startingPoint, RoadJoint target)
        {
            //Navigatorよりルートを取得
            Road[] routeArray = Navigator.Instance.GetRoute(startingPoint, target);

            //キューに直す
            Queue<Road> routeQueue = new Queue<Road>();
            foreach(Road road in routeArray)
            {
                routeQueue.Enqueue(road);
            }

            return routeQueue;
        }

        /// <summary>
        /// 道路を走り始める
        /// </summary>
        private void StartRunningRoad(Road road, uint laneID, RoadJoint startingJoint, bool first = false)
        {
            uint edgeID = road.GetEdgeID(startingJoint);

            //記憶
            currentRoad = road;
            currentLane = laneID;
            currentDistanceInRoad = 0f;
            currentAlongRoad = road.alongVectors[edgeID];
            nextRoadJoint = road.GetDiffrentEdge(startingJoint);
            currentEdgeIDFrom = edgeID;

            //現在位置を道路に開始位置に調整
            AdjustStartingPositionInRoad(road, laneID, edgeID, first);

            //現在の行き先の座標
            Vector2 destinationPoint;

            if(routes.Count > 0)
            {
                //次に走る車線を取得
                Road[] nextRoads = routes.ToArray();
                if (routes.Count >= 2)
                {
                    //「次の次」が存在
                    nextLaneID = GetNextLane(nextRoads[0], nextRoads[1]);
                }
                else
                {
                    //次の次は終点
                    nextLaneID = GetNextLane(nextRoads[0], null);
                }

                //>>次のJointが存在
                //次のJoint回転を計算
                if (TryGetNextCurveRoute(road.GetDiffrentEdge(startingJoint)))
                {
                    nextIsParallel = false;

                    //次のJoint移動がある場合、回転開始位置までrunningRoad
                    destinationPoint = currentCurveRoute.startingPoint;
                }
                else
                {
                    //>>平行
                    nextIsParallel = true;

                    //Jointまで走る
                    destinationPoint = road.GetDiffrentEdge(startingJoint).transform.position;
                }
            }
            else
            {
                //>>次が終点
                //次のJoint移動がない場合（終点の場合）、Jointまで走る
                destinationPoint = road.GetDiffrentEdge(startingJoint).transform.position;
            }

            //目標走行距離
            targetDistanceInRoad = Vector2.Distance(road.GetStartingPoint(edgeID, laneID), destinationPoint);

            //開始位置時点での走行距離
            currentDistanceInRoad = targetDistanceInRoad - Vector2.Distance(transform.position, destinationPoint);

            //信号機を検知
            currentTrafficLight = DetectTrafficLight(currentRoad, edgeID);

            //ステートを変更
            state = State.runningRoad;
        }

        /// <summary>
        /// RunningRoad開始時の位置調整
        /// </summary>
        private void AdjustStartingPositionInRoad(Road road, uint laneID, uint edgeID, bool first)
        {
            //座標
            if (!MyMath.CheckOnLine(transform.position, road.GetStartingPoint(edgeID, laneID), road.alongVectors[edgeID], onSameLineThreshold))
            {
                //直線状無い場合は垂線の足へ調整
                transform.position = MyMath.GetFootOfPerpendicular(transform.position, road.GetStartingPoint(edgeID, laneID), road.alongVectors[edgeID]);
            }

            //初回の場合、座標を合わせる
            if (first)
            {
                transform.position = road.GetStartingPoint(edgeID, laneID);
            }

            //回転
            transform.rotation = Quaternion.Euler(0,0,GetRotatoinInRoad(road.alongVectors[edgeID]));
        }

        /// <summary>
        /// 次のcurveRouteを取得
        /// </summary>
        /// <returns>平行の場合はfalse</returns>
        private bool TryGetNextCurveRoute(RoadJoint curvingJoint)
        {
            //次のRoad
            Road nextRoad = routes.Peek();

            //次と平行
            if (MyMath.IsParallel(currentRoad.alongVectors[0], nextRoad.alongVectors[0], roadsParallelThreshold))
            {
                return false;
            }

            //取得
            currentCurveRoute = GetCurveRoute(
                curvingJoint,
                currentRoad,
                currentLane,
                nextRoad,
                nextLaneID
                );

            return true;
        }

        /// <summary>
        /// 次のRoadで走る車線を選ぶ
        /// </summary>
        private uint GetNextLane(Road roadSelecting, Road roadNext)
        {
            uint output = 0;

            switch (roadSelecting.lanes)
            {
                case 1:
                    output = 0;
                    break;

                case 2:
                    output = ChooseLaneFrom2(roadSelecting, roadNext);
                    break;

                default:
                    Debug.LogError("未実装エラー");
                    output = 0;
                    break;
            }

            nextLaneID = output;
            return nextLaneID;
        }

        /// <summary>
        /// 2車線から車線を選ぶ
        /// </summary>
        private uint ChooseLaneFrom2(Road selectingRoad, Road nextRoad)
        {
            //共通するRoadJointを探す
            RoadJoint commonJoint = RoadJoint.FindCommonJoint(selectingRoad, nextRoad);

            if(nextRoad == null)
            {
                //>>次が終点のとき
                return (uint)Random.Range(0, 2);
            }

            int fromIndex = commonJoint.connectedRoads.IndexOf(selectingRoad);
            int toIndex = commonJoint.connectedRoads.IndexOf(nextRoad);

            int leftHandIndex = (int)fromIndex - 1;

            switch (commonJoint.connectedRoads.Count)
            {
                case 2:
                    return currentLane;

                case 3:
                    if(leftHandIndex < 0)
                    {
                        leftHandIndex += 3;
                    }

                    if (leftHandIndex == toIndex)
                    {
                        //左手側に行く予定
                        //左側車線へ
                        return 0;
                    }
                    else
                    {
                        //右手側に行く予定
                        //右側車線へ
                        return 1;
                    }

                case 4:
                    if (leftHandIndex < 0)
                    {
                        leftHandIndex += 4;
                    }

                    if (leftHandIndex == toIndex)
                    {

                        //左手側に行く予定
                        //左側車線へ
                        return 0;
                    }
                    else
                    {
                        //真ん中、右手側に行く予定
                        //右側車線へ
                        return 1;
                    }

                default:
                    Debug.LogError("未実装エラー");
                    return 0;
            }
        }

        /// <summary>
        /// runningJointステートに入る
        /// </summary>
        private void StartRunningJoint()
        {
            //位置を調整
            AdjustStartingPositionInJoint();

            //角度情報の初期化
            currentAngle = currentCurveRoute.startingAngle;

            //ステートを変更
            state = State.runningJoint;
        }

        /// <summary>
        /// RunningJoint開始時の位置調整
        /// </summary>
        private void AdjustStartingPositionInJoint()
        {
            //座標
            transform.position = currentCurveRoute.startingPoint;

            //回転
            transform.rotation = GetRotationInJoint(currentCurveRoute.startingAngle, currentCurveRoute.clockwise);
        }

        /// <summary>
        /// runningRoad時の走行処理
        /// </summary>
        private void RunRoad()
        {
            //前進
            AdvanceRoad();
            
            //終端を通り過ぎたか確認
            if (currentDistanceInRoad >= targetDistanceInRoad)
            {
                if(routes.Count > 0)
                {
                    //>>まだ終点まで来ていない
                    if (nextIsParallel)
                    {
                        //車線変更モード
                        StartChangingLane();
                    }
                    else
                    {
                        //Joint回転モードに入る
                        StartRunningJoint();
                    }
                }
                else
                {
                    //>>終点まで来た
                    //到着時処理
                    OnArrivedDestination();
                }
            }
        }

        /// <summary>
        /// runningRoad時の走行処理
        /// </summary>
        private void AdvanceRoad()
        {
            Road nextRoad;
            uint nextEdgeID = 0;
            if (routes.Count > 0)
            {
                nextRoad = routes.Peek();
                nextEdgeID = nextRoad.GetEdgeID(nextRoadJoint);
            }
            else
            {
                nextRoad = null;
            }
            
            float advancedDistance = GetSpeedInRoad(currentRoad, currentEdgeIDFrom, nextRoad, nextEdgeID) * Time.deltaTime;
            //外部（ワールド）的
            transform.position += (Vector3)(currentAlongRoad.normalized * advancedDistance);
            //内部的
            currentDistanceInRoad += advancedDistance;
        }

        /// <summary>
        /// runningRoad時の状況に対応する速度を求める
        /// </summary>
        private float GetSpeedInRoad(Road currentRoad, uint currentEdgeIDFrom, Road nextRoad, uint nextEdgeIDFrom)
        {
            //前を走っている車を取得
            Car frontCar = GetFrontCar();

            //前の車が存在しないときのパラメーター
            float frontSpeed = runningRoadV0;
            float s = float.MaxValue;

            if (frontCar != null)
            {
                float distance = Vector2.Distance(frontCar.transform.position, this.transform.position);

                bool isRelatedRoad = ((frontCar.currentRoad == currentRoad) && (frontCar.currentEdgeIDFrom == currentEdgeIDFrom)) 
                    || ((frontCar.currentRoad == nextRoad) && (frontCar.currentEdgeIDFrom == nextEdgeIDFrom));

                if ((Mathf.Abs(MyMath.GetAngularDifference(frontCar.front, this.front)) > runningRoadSameDirectionThreshold)
                    && (frontCar.state != State.runningRoad)
                    && (distance <= runningRoadStopDistanceThreshold))
                {
                    //>>停車条件：対向車が来ている+閾値より近い+

                    //>>閾値より近い
                    //停車する
                    currentSpeed = 0f;
                    return currentSpeed;
                }
                else if (!((frontCar.state == State.runningRoad) && !isRelatedRoad))
                {
                    //>>通常時：関係ない道路のrunningRoadを除外

                    //前を走っている車が存在する
                    frontSpeed = frontCar.currentSpeed;
                    s = distance;
                }
            }

            //信号機
            if ((currentTrafficLight != null)
                && (currentTrafficLight.color != TrafficLight.Color.green))
            {
                //対象の信号機が存在していて、黄色か赤色

                //距離
                float distanceFromLight = Vector2.Distance(this.transform.position, currentTrafficLight.transform.position);

                if (s >= distanceFromLight)
                {
                    //>>対象との距離より近い
                    //前に停止車両があるとしてすり替え
                    s = distanceFromLight;
                    frontSpeed = 0f;
                }
            }

            //速度計算
            currentSpeed = CalculateGFM(
                    currentSpeed,
                    s,
                    frontSpeed,
                    runningRoadT,
                    runningRoadV0,
                    runningRoadT1,
                    runningRoadT2,
                    runningRoadR,
                    runningRoadRp,
                    runningRoadD
                );

            return currentSpeed;
        }

        /// <summary>
        /// Jointを回る
        /// </summary>
        private void RunJoint()
        {
            //移動
            TurnJoint();

            //終端を通り過ぎたか確認
            if (CheckPassedJoint())
            {
                //通り過ぎた
                StartRunningRoad(routes.Dequeue(), nextLaneID, currentCurveRoute.curvingJoint);
            }
        }

        /// <summary>
        /// RoadJointを回る
        /// </summary>
        private void TurnJoint()
        {
            //時計・反時計回りで正負反転する
            int coef;
            if (currentCurveRoute.clockwise)
            {
                coef = -1;
            }
            else
            {
                coef = 1;
            }

            //回転
            currentAngle += GetAngularSpeedInJoint() * coef * Time.deltaTime;

            //座標
            transform.position = MyMath.GetPositionFromPolar(currentCurveRoute.center, currentCurveRoute.radius, currentAngle);

            //回転
            transform.rotation = GetRotationInJoint(currentAngle, currentCurveRoute.clockwise);
        }

        /// <summary>
        /// RunningJoint中に終端を通り過ぎたか確認
        /// </summary>
        private bool CheckPassedJoint()
        {
            return CheckCircularFinished(currentAngle, currentCurveRoute);
        }

        /// <summary>
        /// 車線変更を開始
        /// </summary>
        private void StartChangingLane()
        {
            //車線変更の必要がないか確認
            if (CheckChangingLaneNecessary())
            {
                StartRunningRoad(routes.Dequeue(), nextLaneID, nextRoadJoint);
                return;
            }

            changingLaneRotating = false;
            curveChangingLane = new CurveRoute();

            state = State.changingLane;
        }

        /// <summary>
        /// 車線変更
        /// </summary>
        private void ChangeLane()
        {
            Road nextRoad = routes.Peek();
            Vector2 nextVector = nextRoad.alongVectors[nextRoad.GetEdgeID(nextRoadJoint)];
            uint nextEdgeID = nextRoad.GetEdgeID(nextRoadJoint);
            Vector2 nextLaneStartingPoint = nextRoad.GetStartingPoint(nextEdgeID, nextLaneID);

            if(!changingLaneRotating)
            {
                TryMakeCurveChangingLane(nextLaneStartingPoint, nextVector);
            }

            if (changingLaneRotating)
            {
                //回転移動
                ChangeLaneRotation(nextLaneStartingPoint, nextVector, curveChangingLane);
            }
            else
            {
                //前進しながら曲がる
                ChangeLaneForward(nextLaneStartingPoint, nextVector);
            }
        }

        /// <summary>
        /// 車線変更が必要か確認する
        /// </summary>
        private bool CheckChangingLaneNecessary()
        {
            Road nextRoad = routes.Peek();
            uint edgeID = nextRoad.GetEdgeID(nextRoadJoint);

            return MyMath.CheckOnLine((Vector2)transform.position,
                (Vector2)nextRoad.GetStartingPoint(edgeID, nextLaneID),
                (Vector2)nextRoad.alongVectors[edgeID],
                onSameLineThreshold);
        }

        /// <summary>
        /// 車線変更時、円弧移動を仮定して、回転移動モードに入るか判断
        /// </summary>
        private bool TryMakeCurveChangingLane(Vector2 nextLaneStartingPoint, Vector2 nextVector)
        {
            //平行ならfalse
            if(MyMath.IsParallel(nextVector, front, parallelThresholdChangingLane))
            {
                return false;
            }

            //>>回転移動すると仮定したときの半径・中心を求める
            //現在の進行方向の法線ベクトル
            Vector2 perpendicularFromAhead = MyMath.GetPerpendicular(front);

            //進行方向と車線方向の角の二等分線ベクトル
            Vector2 bisector = MyMath.GetBisector(-front, nextVector);

            //進行方向と車線の交点
            Vector2 intersection = MyMath.GetIntersection(transform.position, front, nextLaneStartingPoint, nextVector);

            //回転中心の座標
            Vector2 rotationCenter = MyMath.GetIntersection(transform.position, perpendicularFromAhead, intersection, bisector);

            //回転半径
            float radius = Vector2.Distance(rotationCenter, transform.position);

            if (radius <= thresholdRadiusChangingLane)
            {
                //回転移動を開始
                changingLaneRotating = true;

                //>>回転軌道の具体化
                //回転方向を算出
                float angularDiference = MyMath.GetAngularDifference(front, nextVector);
                bool clockwise;
                if (angularDiference < 180f)
                {
                    //反時計回り
                    clockwise = false;
                }
                else
                {
                    //時計回り
                    clockwise = true;
                }

                //カーブの始点・終点を求める
                Vector2 startingPoint = transform.position;
                Vector2 endingPoint = MyMath.GetFootOfPerpendicular(rotationCenter, nextLaneStartingPoint, nextVector);

                //角度を求める
                float startingAngle = MyMath.GetAngular(startingPoint - rotationCenter);
                float endingAngle = MyMath.GetAngular(endingPoint - rotationCenter);

                //軌道の保存
                curveChangingLane.center = rotationCenter;
                curveChangingLane.radius = radius;
                curveChangingLane.clockwise = clockwise;
                curveChangingLane.startingAngle = startingAngle;
                curveChangingLane.endingAngle = endingAngle;

                //現在の角度
                currentAngle = startingAngle;

                return true;
            }

            return false;
        }

        /// <summary>
        /// 車線変更時、前進しながら曲がる
        /// </summary>
        private void ChangeLaneForward(Vector2 targetLanePoint, Vector2 targetLaneVector)
        {
            //曲がる方向を算出
            bool shouldTurnRight = !MyMath.IsRightFromVector(transform.position, targetLanePoint, targetLaneVector);

            //進行方向に対する車線の角度
            float angularDifference = Vector2.Angle(front, targetLaneVector);

            //回転角を算出
            float angularMovement = Mathf.Min(angleMaxChangingLane - angularDifference, changingLaneRotationSpeed* Time.deltaTime);

            if (shouldTurnRight)
            {
                //右に曲がる場合、正負反転
                angularMovement = -angularMovement;
            }

            //回転
            transform.rotation = transform.rotation * Quaternion.Euler(0, 0, angularMovement);

            //回転後に前進
            Road[] roads = routes.ToArray();
            Road currentRoad = roads[0];
            uint currentRoadEdgeIDFrom = currentRoad.GetEdgeID(nextRoadJoint);
            Road nextRoad;
            uint nextRoadEdgeIDFrom = 0;
            if (roads.Length > 1)
            {
                nextRoad = roads[1];
                nextRoadEdgeIDFrom = nextRoad.GetEdgeID(RoadJoint.FindCommonJoint(currentRoad, nextRoad));
            }
            else
            {
                nextRoad = null;
            }
            
            transform.position += (Vector3)(front.normalized * GetSpeedInRoad(currentRoad, currentRoadEdgeIDFrom, nextRoad, nextRoadEdgeIDFrom) * Time.deltaTime);
        }

        /// <summary>
        /// 車線変更時の、最後の回転移動
        /// </summary>
        private void ChangeLaneRotation(Vector2 targetLanePoint, Vector2 targetLaneVector, CurveRoute curve)
        {
            //角度を移動させる
            if (curveChangingLane.clockwise)
            {
                currentAngle -= GetAngularSpeedInChangingLane(curve.radius) * Time.deltaTime;
            }
            else
            {
                currentAngle += GetAngularSpeedInChangingLane(curve.radius) * Time.deltaTime;
            }

            if(CheckCircularFinished(currentAngle, curve)){
                //>>行き過ぎたので車線方向へ戻す

                //座標
                transform.position = MyMath.GetPositionFromPolar(curve.center, curve.radius, curve.endingAngle);

                //回転
                transform.rotation = GetRotationInJoint(curve.endingAngle, curve.clockwise);

                //車線変更を終了
                StartRunningRoad(routes.Dequeue(), nextLaneID, nextRoadJoint);
            }
            else
            {
                //座標
                transform.position = MyMath.GetPositionFromPolar(curve.center, curve.radius, currentAngle);

                //回転
                transform.rotation = GetRotationInJoint(currentAngle, curve.clockwise);
            }
        }

        private float GetAngularSpeedInChangingLane(float radius)
        {
            Car frontCar = GetFrontCar();

            if(frontCar != null)
            {
                //>>前の車が存在

                float distance = Vector2.Distance(frontCar.transform.position, this.transform.position);

                if (distance <= changingLaneStopDistance)
                {
                    //>>近すぎる
                    //停車
                    currentSpeed = 0f;
                    return currentSpeed;
                }
            }
           
            return changingLaneAngularSpeed;
        }

        /// <summary>とき
        /// 目的地に到着して、消えてGameManagerに報告
        /// </summary>
        private void OnArrivedDestination()
        {
            float speedAverage = CalculateAverageSpeed();

            //GameManagerに通達
            GameManager.Instance.OnCarArrived(speedAverage, runningRoadV0);

            //消える
            Destroy(this.gameObject);
        }

        /// <summary>
        /// Joint回転中の角速度
        /// </summary>
        /// <returns></returns>
        private float GetAngularSpeedInJoint()
        {
            //前を走っている車を取得
            Car frontCar = DetectFrontCarInJoint(currentCurveRoute);

            //前の車が存在しないときのパラメーター
            float frontSpeed = runningJointV0;
            float s = float.MaxValue;

            if (frontCar != null)
            {
                float distance = Vector2.Distance(frontCar.transform.position, this.transform.position);

                if ((Mathf.Abs(MyMath.GetAngularDifference(frontCar.front, this.front)) > runningJointSameDirectionThreshold)
                    && (s <= runningJointStopDistance)
                    && !((frontCar.state == State.runningRoad) && (frontCar.currentRoad != this.routes.Peek())))
                {
                    //>>停車条件：対向車が来ている+閾値より近い+

                    //>>閾値より近い
                    //停車する
                    currentSpeed = 0f;
                    return currentSpeed;
                }
                else if(!((frontCar.state == State.runningRoad) && (frontCar.currentRoad != this.routes.Peek())))
                {
                    //>>通常時：他の道路でrunningRoadしているCarは除外

                    //前を走っている車が存在する
                    frontSpeed = frontCar.currentSpeed;
                    s = distance;
                }
            }

            //速度計算
            currentSpeed = CalculateGFM(
                    currentSpeed,
                    s,
                    frontSpeed,
                    runningJointT,
                    runningJointV0,
                    runningJointT1,
                    runningJointT2,
                    runningJointR,
                    runningJointRp,
                    runningJointD
                );

            //角速度に変換して返す
            return MyMath.GetAngularSpeed(currentSpeed, currentCurveRoute.radius);
        }

        /// <summary>
        /// RunRoad時の回転を返す
        /// </summary>
        private float GetRotatoinInRoad(Vector2 alongVector)
        {
            return MyMath.GetAngular(alongVector);
        }

        /// <summary>
        /// 二つの道からカーブ軌道を取得
        /// </summary>
        /// <param name="startingEdgeID">交差点側のedgeID</param>
        /// <param name="endingRoad">交差点側のedgeID</param>
        private CurveRoute GetCurveRoute(
            RoadJoint curvingJoint,
            Road startingRoad, 
            uint startingLaneID,
            Road endingRoad, 
            uint endingLaneID
            )
        {
            CurveRoute output = new CurveRoute();

            output.curvingJoint = curvingJoint;

            //EdgeIDを取得
            uint startingEdgeID = startingRoad.GetEdgeID(curvingJoint);
            uint endingEdgeID = endingRoad.GetEdgeID(curvingJoint);

            //>>時計回りかを取得
            Vector2 startingAlongVector = startingRoad.alongVectors[Road.GetDifferentEdgeID(startingEdgeID)];
            Vector2 endingAlongVector = endingRoad.alongVectors[endingEdgeID];
            float angularDiference = MyMath.GetAngularDifference(startingAlongVector, endingAlongVector);
            if (angularDiference < 180f)
            {
                //反時計回り
                output.clockwise = false;
            }
            else
            {
                //時計回り
                output.clockwise = true;
            }

            //>>中心を取得
            //車線の側面の交点が中心になる

            //考慮する線分
            Vector2 startingRoadSideLinePoint;
            Vector2 startingRoadSideLineVector;
            Vector2 endingRoadSideLinePoint;
            Vector2 endingRoadSideLineVector;

            if (output.clockwise)
            {
                //>>時計回り

                //starting: 車線の右側
                startingRoadSideLinePoint = startingRoad.GetRightPoint(Road.GetDifferentEdgeID(startingEdgeID), startingLaneID);
                startingRoadSideLineVector = startingRoad.alongVectors[Road.GetDifferentEdgeID(startingEdgeID)];

                //ending: 車線の右側
                endingRoadSideLinePoint = endingRoad.GetRightPoint(endingEdgeID, endingLaneID);
                endingRoadSideLineVector = endingRoad.alongVectors[endingEdgeID];
            }
            else
            {
                //>>反時計回り
                
                //starting: 車線の左側
                startingRoadSideLinePoint = startingRoad.GetLeftPoint(Road.GetDifferentEdgeID(startingEdgeID), startingLaneID);
                startingRoadSideLineVector = startingRoad.alongVectors[Road.GetDifferentEdgeID(startingEdgeID)];

                //ending: 車線の左側
                endingRoadSideLinePoint = endingRoad.GetLeftPoint(endingEdgeID, endingLaneID);
                endingRoadSideLineVector = endingRoad.alongVectors[endingEdgeID];
            }

            //交点を求める
            output.center = MyMath.GetIntersection(startingRoadSideLinePoint, startingRoadSideLineVector, endingRoadSideLinePoint, endingRoadSideLineVector);

            //カーブの始点・終点を求める
            Vector2 startingPoint = MyMath.GetFootOfPerpendicular(output.center, startingRoad.GetStartingPoint(Road.GetDifferentEdgeID(startingEdgeID), startingLaneID), startingRoad.alongVectors[Road.GetDifferentEdgeID(startingEdgeID)]);
            Vector2 endingPoint = MyMath.GetFootOfPerpendicular(output.center, endingRoad.GetStartingPoint(endingEdgeID, endingLaneID), endingRoad.alongVectors[endingEdgeID]);

            //半径を求める
            output.radius = Vector2.Distance(startingPoint, output.center);

            //角度を求める
            output.startingAngle = MyMath.GetAngular(startingPoint - output.center);
            output.endingAngle = MyMath.GetAngular(endingPoint - output.center);

            return output;
        }

        /// <summary>
        /// RunningJoint中の回転を取得
        /// </summary>
        private Quaternion GetRotationInJoint(float angleInCurve, bool clockwise)
        {
            float addition;

            //90度足し引きすることで進行方向を向く
            if (clockwise)
            {
                addition = -90f;
            }
            else
            {
                addition = 90f;
            }

            return Quaternion.Euler(0f, 0f, angleInCurve + addition);
        }

        /// <summary>
        /// 回転運動が終了したか確認
        /// </summary>
        private static bool CheckCircularFinished(float currentAngle, CurveRoute curveRoute)
        {
            if (curveRoute.clockwise)
            {
                //時計回り
                if (curveRoute.startingAngle > curveRoute.endingAngle)
                {
                    //x軸正を経過しない
                    return (currentAngle <= curveRoute.endingAngle);
                }
                else
                {
                    //x軸正を経過
                    return (currentAngle <= curveRoute.endingAngle - 360);
                }
            }
            else
            {
                //反時計回り
                if (curveRoute.startingAngle < curveRoute.endingAngle)
                {
                    //x軸正を経過しない
                    return (currentAngle >= curveRoute.endingAngle);
                }
                else
                {
                    //x軸正を経過
                    return (currentAngle >= curveRoute.endingAngle + 360);
                }
            }
        }

        /// <summary>
        /// Generalized Force Modelを計算
        /// </summary>
        private float CalculateGFM(
            float formerSpeed,
            float s,
            float frontSpeed,
            float T,
            float v0,
            float t1,
            float t2,
            float r,
            float rp,
            float d
            )
        {

            float sFunc = d + T * formerSpeed;

            float dv = formerSpeed - frontSpeed;

            float V = v0 * (1f - Mathf.Exp(-(s - sFunc) / r));

            float theta;
            if (dv <= 0)
            {
                theta = 0f;
            }
            else
            {
                theta = 1f;
            }

            float xpp = (V - formerSpeed) / t1 - ((dv * theta) / t2) * Mathf.Exp(-(s - sFunc) / rp);

            float outputSpeed = formerSpeed + xpp * Time.deltaTime;

            if (outputSpeed < 0f)
            {
                outputSpeed = 0f;
            }

            return outputSpeed;
        }

        /// <summary>
        /// 車を検出
        /// </summary>
        private void DetectCars()
        {
            carsDetectedFront = LunchDetectionRayForCars(detectionRayStart, detectionRayDestinationsFront);
            carsDetectedFrontLeft = LunchDetectionRayForCars(detectionRayStart, detectionRayDestinationsFrontLeft);
            carsDetectedFrontRight = LunchDetectionRayForCars(detectionRayStart, detectionRayDestinationsFrontRight);
            carsDetectedLeft = LunchDetectionRayForCars(detectionRayStart, detectionRayDestinationsLeft);
            carsDetectedRight = LunchDetectionRayForCars(detectionRayStart, detectionRayDestinationsRight);
        }

        /// <summary>
        /// 信号機を検出
        /// </summary>
        private TrafficLight DetectTrafficLight(Road road, uint startingEdgeID)
        {
            //始点と反対側の信号機を検出
            TrafficLight trafficLight = road.trafficLights[Road.GetDifferentEdgeID(startingEdgeID)];

            if (trafficLight.enabled)
            {
                //信号機が起動している
                return trafficLight;
            }
            else{
                //信号機が起動されていない
                return null;
            }
        }

        /// <summary>
        /// 検出ビームを発射して、検出された車の配列を返す
        /// </summary>
        private List<Car> LunchDetectionRayForCars(Transform rayStartTransform, Transform[] rayEndTransforms)
        {
            Vector2 rayStart = rayStartTransform.position;
            Vector2[] rayEnds = new Vector2[rayEndTransforms.Length];
            for(int cnt = 0; cnt < rayEndTransforms.Length; cnt++)
            {
                rayEnds[cnt] = rayEndTransforms[cnt].position;
            }

            LunchDetectionRayForCars(rayStart, rayEnds);

            return LunchDetectionRayForCars(rayStart, rayEnds);
        }

        /// <summary>
        /// 検出ビームを発射して、検出された車の配列を返す
        /// </summary>
        private List<Car> LunchDetectionRayForCars(Vector2 rayStart, Vector2[] rayEnds)
        {
            List<Car> output = new List<Car>();

            foreach (Vector2 rayEnd in rayEnds)
            {
                //検出ビームを発射
                output.AddRange(LunchDetectionRayForCars(rayStart, rayEnd));
            }

            return output;
        }

        /// <summary>
        /// 検出ビームを発射して、検出された車の配列を返す
        /// </summary>
        private List<Car> LunchDetectionRayForCars(Vector2 rayStart, Vector2 rayEnd)
        {
            List<Car> output = new List<Car>();
            
            //検出ビームを発射
            RaycastHit2D[] hitteds = Physics2D.RaycastAll(rayStart, rayEnd - rayStart, (rayEnd - rayStart).magnitude);

            //衝突したオブジェクトから車を列挙
            foreach (RaycastHit2D hitted in hitteds)
            {
                //Carコンポーネントがあるか+自分自身ではないかを確認
                Car car = hitted.collider.gameObject.GetComponent<Car>();
                if ((car != null)
                    && (car != this))
                {
                    //車である
                    output.Add(car);
                }
            }

            return output;
        }

        /// <summary>
        /// 同じ道路でrunningRoadしている一つのCarを取得
        /// </summary>
        private Car GetFrontCar()
        {
            //最も近いものを線形探索
            float nearestDistance = float.MaxValue;
            Car nearestCar = null;
            foreach (Car car in carsDetectedFront)
            {
                //破壊済みなら飛ばす
                if (car == null)
                {
                    continue;
                }

                float distance = Vector2.Distance(car.transform.position, this.transform.position);

                if (distance < nearestDistance)
                {
                    nearestCar = car;
                    nearestDistance = distance;
                }
            }

            return nearestCar;
        }

        /// <summary>
        /// runningJoint中の一つ前のCarを取得
        /// </summary>
        private Car GetFrontCarRunningJoint()
        {
            //探索対象を列挙
            List<Car> targets = new List<Car>();
            targets.AddRange(carsDetectedFront);
            if (currentCurveRoute.clockwise)
            {
                //時計回りなら右を見る
                targets.AddRange(carsDetectedFrontRight);
                targets.AddRange(carsDetectedRight);
            }
            else
            {
                //反時計回りなら左を見る
                targets.AddRange(carsDetectedFrontLeft);
                targets.AddRange(carsDetectedLeft);
            }

            //最も近いものを線形探索
            float nearestDistance = float.MaxValue;
            Car nearestCar = null;
            foreach (Car car in targets)
            {
                //破壊済みなら飛ばす
                if (car == null)
                {
                    continue;
                }

                float distance = Vector2.Distance(car.transform.position, this.transform.position);

                if (distance < nearestDistance)
                {
                    nearestCar = car;
                    nearestDistance = distance;
                }
            }

            return nearestCar;
        }

        private Car DetectFrontCarInJoint(CurveRoute curveRoute)
        {
            const float angleUnit = 5f;

            //>>円弧上に検出ビームを出す
            float angle = currentAngle;
            float angleEnd = curveRoute.endingAngle;
            Car target = null;
            Vector2 ending = Vector2.zero;
            while(CheckCircularFinished(angle, curveRoute))
            {
                float rayStartAngle = angle;

                if (curveRoute.clockwise)
                {
                    //時計回り
                    angle -= angleUnit;
                }
                else
                {
                    //反時計回り
                    angle += angleUnit;
                }

                float rayEndAngle = angle;

                Vector2 rayStart = MyMath.GetPositionFromPolar(curveRoute.center, curveRoute.radius, rayStartAngle);
                Vector2 rayEnd = MyMath.GetPositionFromPolar(curveRoute.center, curveRoute.radius, rayEndAngle);

                ending = rayEnd;

                List<Car> hittedCars = LunchDetectionRayForCars(rayStart, rayEnd);
                if (hittedCars.Count > 0)
                {
                    //>>存在
                    //rayStartに最も近いものを探す
                    Car nearestCar = null;
                    float nearestDistance = float.MaxValue;
                    foreach(Car car in hittedCars)
                    {
                        float distance = Vector2.Distance(car.transform.position, rayStart);
                        if ((car.state == State.runningJoint)
                            &&(distance < nearestDistance))
                        {
                            nearestDistance = distance;
                            nearestCar = car;
                        }
                    }

                    if (nearestCar != null)
                    {
                        target = nearestCar;
                        break;
                    }
                }
            }

            //未検出の場合、次の道路の半分も見る
            if(target == null)
            {
                Road nextRoad = routes.Peek();

                Vector2 rayStart = ending;
                Vector2 direction = nextRoad.alongVectors[nextRoad.GetEdgeID(curveRoute.curvingJoint)] / 2f;

                List<Car> cars = LunchDetectionRayForCars(rayStart, rayStart + direction);

                //最も近いものを探す
                Car nearestCar = null;
                float nearestDistance = float.MaxValue;
                foreach(Car car in cars)
                {
                    float distance = Vector2.Distance(car.transform.position, rayStart);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestCar = car;
                    }
                }

                target = nearestCar;
            }

            return target;
        }

        /// <summary>
        /// 満足度の管理をする
        /// </summary>
        private void ManageHappiness()
        {
            //速度の保存
            SaveSpeed();

            //満足度を計算
            CalculateHappiness();

            //色を更新
            colorObject.UpdateColor(happinessRatio);
        }

        /// <summary>
        /// 速度を保存する
        /// </summary>
        private void SaveSpeed()
        {
            //タイマー進める
            saveSpeedTimer += Time.deltaTime;

            if(saveSpeedTimer < saveSpeedInterval)
            {
                //まだタイマーが切れていない
                return;
            }
            //>>タイマーが切れた

            //速度を保存
            savedSpeeds.Add(currentSpeed);

            //タイマーリセット
            saveSpeedTimer = saveSpeedTimer % saveSpeedInterval;
        }

        /// <summary>
        /// 満足度の計算
        /// </summary>
        private void CalculateHappiness()
        {
            //タイマー進める
            happinessCalculationTimer += Time.deltaTime;

            if(happinessCalculationTimer < happinessCalculationInterval)
            {
                //まだタイマーが切れていない
                return;
            }
            //>>タイマーが切れた

            //満足度を増減
            int changement = GetHappinessChangement();
            happiness += changement;

            //範囲管理
            if (happiness < happinessMin)
            {
                happiness = happinessMin;
            }
            else if (happiness > happinessMax)
            {
                happiness = happinessMax;
            }

            //タイマーリセット
            happinessCalculationTimer = happinessCalculationTimer % happinessCalculationInterval;
        }

        /// <summary>
        /// 幸福度の変化量を計算
        /// </summary>
        private int GetHappinessChangement()
        {
            //最高速度に対する割合
            float speedRatio = currentSpeed / runningJointV0;

            //対応する変化量を探す
            int output = happinessChangements[happinessChangeThresholds.Length - 1];
            for(int cnt = 0; cnt < happinessChangeThresholds.Length; cnt++)
            {
                if(speedRatio <= happinessChangeThresholds[cnt])
                {
                    //cntが該当するインデックス
                    output = happinessChangements[cnt];
                    
                    break;
                }
            }

            return output;
        }

        /// <summary>
        /// 平均速度を計算
        /// </summary>
        private float CalculateAverageSpeed()
        {
            float sum = 0f;
            foreach(float speed in savedSpeeds)
            {
                sum += speed;
            }

            return sum / savedSpeeds.Count;
        }

        /// <summary>
        /// Jointを曲がるとき、円弧を描く。その円弧の始点、終点、中心。
        /// </summary>
        private struct CurveRoute
        {
            /// <summary>
            /// 回っているRoadJoint
            /// </summary>
            public RoadJoint curvingJoint;

            /// <summary>
            /// 円弧の中心
            /// </summary>
            public Vector2 center;

            /// <summary>
            /// 円弧半径
            /// </summary>
            public float radius;

            /// <summary>
            /// 始点の角度（ｘ軸から反時計回りに）
            /// </summary>
            public float startingAngle;

            /// <summary>
            /// 終点の角度（ｘ軸から反時計回りに）
            /// </summary>
            public float endingAngle;

            /// <summary>
            /// 時計回りか
            /// </summary>
            public bool clockwise;

            public Vector2 startingPoint
            {
                get
                {
                    return MyMath.GetPositionFromPolar(center, radius, startingAngle);
                }
            }

            public Vector2 endingPoint
            {
                get
                {
                    return MyMath.GetPositionFromPolar(center, radius, endingAngle);
                }
            }
        }
    }
}