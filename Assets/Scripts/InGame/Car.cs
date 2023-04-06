using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    public class Car : MonoBehaviour
    {
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

        [Header("速度関係")]

        [Tooltip("RoadJointを回る回転速度")]
        [SerializeField] private float angularSpeed = 30f;

        [Tooltip("車線変更時の回転速度")]
        [SerializeField] private float angularSpeedChangingLane = 10f;

        private enum State
        {
            runningRoad,
            runningJoint,
            changingLane
        }

        private State state;

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
        private Vector2 currentAlongRoad;

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
        /// 次のRunningRoadのlaneID
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
        }

        private void Update()
        {
            Run();
            Detect();
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

            //現在位置を道路に開始位置に調整
            AdjustStartingPositionInRoad(road, laneID, edgeID, first);

            //現在の行き先の座標
            Vector2 destinationPoint;

            if(routes.Count > 0)
            {
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
                GetNextLane()
                );

            return true;
        }

        /// <summary>
        /// 次のRoadで走る車線を選ぶ
        /// </summary>
        private uint GetNextLane()
        {
            //TODO
            nextLaneID = 0;
            return nextLaneID;
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
            float advancedDistance = GetSpeedInRoad() * Time.deltaTime;
            //外部（ワールド）的
            transform.position += (Vector3)(currentAlongRoad.normalized * advancedDistance);
            //内部的
            currentDistanceInRoad += advancedDistance;
        }

        /// <summary>
        /// runningRoad時の状況に対応する速度を求める
        /// </summary>
        private float GetSpeedInRoad()
        {
            //前を走っている車を取得
            Car frontCar = GetFrontCarRunningRoad();

            //前を走っている車の速さ
            float frontSpeed;
            //前の車との距離
            float s;
            if (frontCar != null)
            {
                //前を走っている車が存在する
                frontSpeed = frontCar.GetSpeed().magnitude;
                s = Vector2.Distance(frontCar.transform.position, this.transform.position);
            }
            else
            {
                //前の車が存在しない
                frontSpeed = runningRoadV0;
                s = float.MaxValue;
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
            //CheckChangingLaneNecessary()より前に呼ぶ必要がある
            GetNextLane();

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
        /// 車線変更時の、最後の回転移動
        /// </summary>
        private void ChangeLaneRotation(Vector2 targetLanePoint, Vector2 targetLaneVector, CurveRoute curve)
        {
            //角度を移動させる
            if (curveChangingLane.clockwise)
            {
                currentAngle -= GetAngularSpeedInChangingLane() * Time.deltaTime;
            }
            else
            {
                currentAngle += GetAngularSpeedInChangingLane() * Time.deltaTime;
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
            float angularMovement = Mathf.Min(angleMaxChangingLane -angularDifference, GetAngularSpeedInChangingLane() * Time.deltaTime);

            if (shouldTurnRight)
            {
                //右に曲がる場合、正負反転
                angularMovement = -angularMovement;
            }

            //回転
            transform.rotation = transform.rotation * Quaternion.Euler(0, 0, angularMovement);

            //回転後に前進
            transform.position += (Vector3)(front.normalized * GetSpeedInRoad() * Time.deltaTime);
        }

        private float GetAngularSpeedInChangingLane()
        {
            return angularSpeedChangingLane;
        }

        /// <summary>
        /// 目的地に到着して、消えてGameManagerに報告
        /// </summary>
        private void OnArrivedDestination()
        {
            //消える
            Destroy(this.gameObject);
        }

        /// <summary>
        /// Joint回転中の角速度
        /// </summary>
        /// <returns></returns>
        private float GetAngularSpeedInJoint()
        {
            return angularSpeed;
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
        /// 現在の速度を返す
        /// </summary>
        public Vector2 GetSpeed()
        {
            switch (state)
            {
                case State.runningRoad:
                    return front.normalized * currentSpeed;

                default:
                    Debug.LogError("未定義エラー");
                    return Vector2.zero;
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
        /// 同じ道路でrunningRoadしている一番前のCarを取得
        /// </summary>
        private Car GetFrontCarRunningRoad()
        {
            //最も近いものを線形探索
            float nearestDistance = float.MaxValue;
            Car nearestCar = null;
            foreach(Car car in carsDetectedFront)
            {
                //破壊済みなら飛ばす
                if(car == null)
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

            if (nearestCar == null)
            {
                //該当無し
                return null;
            }

            //frontの角度の差が閾値以内なら同じ方向を走っていると言える
            float angleDifference = MyMath.GetAngularDifference(this.front, nearestCar.front);
            if (angleDifference <= runningRoadSameDirectionThreshold)
            {
                //同じ方向を走っている
                return nearestCar;
            }
            else
            {
                return null;
            }
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
            List<Car> output = new List<Car>();

            Vector2 rayStart = rayStartTransform.position;

            foreach(Transform rayEndTransform in rayEndTransforms)
            {
                Vector2 rayEnd = rayEndTransform.position;

                //検出ビームを発射
                RaycastHit2D[] hitteds = Physics2D.RaycastAll(rayStart, rayEnd - rayStart, (rayEnd - rayStart).magnitude);

                //衝突したオブジェクトから車を列挙
                foreach (RaycastHit2D hitted in hitteds)
                {
                    //Carコンポーネントがあるか+自分自身ではないかを確認
                    Car car = hitted.collider.gameObject.GetComponent<Car>();
                    if ((car != null)
                        &&(car != this))
                    {
                        //車である
                        output.Add(car);
                    }
                }
            }

            return output;
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