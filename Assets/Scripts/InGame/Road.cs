using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InGame
{
    /// <summary>
    /// 道路。
    /// 車がこの上を走る
    /// 初期化処理として、自動的にRoadJointに接続する
    /// </summary>
    public class Road : MonoBehaviour
    {
        [Tooltip("道路の両端、中心の空オブジェクト")]
        [SerializeField] private Transform[] _edges;

        [Tooltip("両端に設置されている信号機。edgeObjectsと同じ順番で登録すること")]
        [SerializeField] private TrafficLight[] _trafficLights;

        [Tooltip("Edge0の開始位置外側（左側）車線からいれること")]
        [SerializeField] private Transform[] _startingPoint0;
        [Tooltip("Edge1の開始位置外側（左側）車線からいれること")]
        [SerializeField] private Transform[] _startingPoint1;

        [Tooltip("道路の角。Edge0、Edge1の順に２ついれること")]
        [SerializeField] private Transform[] _corners;

        /// <summary>
        /// Edge0の開始位置。進行方向左側から順に
        /// </summary>
        public Vector2[] startingPoint0
        {
            get
            {
                Vector2[] output = new Vector2[_startingPoint0.Length];
                for(int cnt = 0; cnt < _startingPoint0.Length; cnt++)
                {
                    output[cnt] = _startingPoint0[cnt].position;
                }

                return output;
            }
        }

        /// <summary>
        /// Edge1の開始位置。進行方向左側から順に
        /// </summary>
        public Vector2[] startingPoint1
        {
            get
            {
                Vector2[] output = new Vector2[_startingPoint1.Length];
                for (int cnt = 0; cnt < _startingPoint1.Length; cnt++)
                {
                    output[cnt] = _startingPoint1[cnt].position;
                }

                return output;
            }
        }

        /// <summary>
        /// 片側車線数
        /// </summary>
        public uint lanes
        {
            get
            {
                return (uint)startingPoint0.Count();
            }
        }

        /// <summary>
        /// 角の座標
        /// </summary>
        public Vector2[] corners
        {
            get
            {
                Vector2[] output = new Vector2[2];
                for(int cnt = 0; cnt < 2; cnt++)
                {
                    output[cnt] = _corners[cnt].position;
                }

                return output;
            }
        }

        public Vector2[] edges
        {
            get
            {
                Vector2[] output = new Vector2[2];
                for (int cnt = 0; cnt < 2; cnt++)
                {
                    output[cnt] = _edges[cnt].position;
                }

                return output;
            }
        }

        /// <summary>
        /// 道沿いベクトル。0番目はedge0⇒edge1、1番目は反対
        /// </summary>
        public Vector2[] alongVectors
        {
            get
            {
                Vector2[] output = new Vector2[2];
                output[0] = edges[1] - edges[0];
                output[1] = -output[0];
                return output;
            }
        } 

        public TrafficLight[] trafficLights
        {
            get
            {
                return _trafficLights;
            }
        }
               

        /// <summary>
        /// 両端に接続しているRoadJoint２つ。順番はedgeに対応。0番目はedge0始点。
        /// </summary>
        public RoadJoint[] connectedJoints { get; private set; } = new RoadJoint[2];

        /// <summary>
        /// 道路に接続済みか
        /// </summary>
        public bool isInitialized { get; private set; } = false;

        private void Start()
        {
            //配置最適化
            OptimizeArrangement();
        }

        /// <summary>
        /// 両端に最近傍のRoadJointを探索し、配置を最適化
        /// </summary>
        private void OptimizeArrangement()
        {
            //接続RoadJointを取得
            GetConnectedJoints();

            //最適化前の両端を結ぶベクトル
            Vector3 originalPath = _edges[0].position - _edges[1].position;
            //最適化後
            Vector3 optimizedPath = connectedJoints[0].transform.position - connectedJoints[1].transform.position;

            //移動
            transform.position = (connectedJoints[0].transform.position + connectedJoints[1].transform.position) / 2;

            //>>拡大あるいは縮小
            //倍率
            float extensionCoef = optimizedPath.magnitude / originalPath.magnitude;
            //横向きに拡大・縮小
            transform.localScale = new Vector3(extensionCoef * transform.localScale.x, transform.localScale.y, transform.localScale.z);

            //回転
            Quaternion rotation = Quaternion.FromToRotation(originalPath, optimizedPath);
            transform.rotation *= rotation;

            //初期化済みに
            isInitialized = true;
        }

        /// <summary>
        /// 両端に最近傍のRoadJointを取得
        /// </summary>
        private void GetConnectedJoints()
        {
            //両端について
            for (int cnt = 0; cnt < 2; cnt++)
            {
                Vector3 edgePosition = _edges[cnt].position;

                //最近傍を探して登録
                connectedJoints[cnt] = GetNearestJoint(edgePosition);

                //RoadJoint側にこちらを登録する
                connectedJoints[cnt].RegisterRoad(this, cnt);
            }
        }

        /// <summary>
        /// 最も近いRoadJointを探す
        /// </summary>
        private RoadJoint GetNearestJoint(Vector3 searchingPoint)
        {
            //全RoadJointを取得
            RoadJoint[] allJoints = FindObjectsOfType<RoadJoint>();

            RoadJoint nearestJoint = null;

            //最近傍を線形探索
            float minDistance = float.MaxValue;
            foreach (RoadJoint joint in allJoints)
            {
                Vector3 difference = searchingPoint - joint.transform.position;
                float distance = difference.magnitude;

                if (distance < minDistance)
                {
                    nearestJoint = joint;
                    minDistance = distance;
                }
            }

            //nullだとおかしい（RoadJointが検知されていない）
            Debug.Assert(nearestJoint != null);

            return nearestJoint;
        }

        /// <summary>
        /// 隠している信号機を起動
        /// </summary>
        /// <param name="side">道のどちらの端の信号機か</param>
        /// <returns>起動した信号機の参照</returns>
        public TrafficLight ActivateTrafficLight(int side)
        {
            //信号機を起動
            trafficLights[side].gameObject.SetActive(true);

            //信号機の参照を返す
            return trafficLights[side];
        }

        /// <summary>
        /// 与えられたroadについてjointと異なる端を返す
        /// </summary>
        public RoadJoint GetDiffrentEdge(RoadJoint joint)
        {
            //jointがroadのどちらかの端のはず
            Debug.Assert(connectedJoints.Contains(joint));

            if (connectedJoints[0] == joint)
            {
                return connectedJoints[1];
            }
            else
            {
                return connectedJoints[0];
            }
        }

        /// <summary>
        /// 得られた方と反対側のEdgeIDを返す
        /// </summary>
        public static uint GetDifferentEdgeID(uint edgeID)
        {
            if(edgeID == 0)
            {
                return 1;
            }else if (edgeID == 1)
            {
                return 0;
            }
            else
            {
                Debug.LogError("Only 0, 1");
                return 0;
            }
        }

        /// <summary>
        /// 与えられた端のedge番号を返す
        /// </summary>
        public uint GetEdgeID(RoadJoint edge)
        {
            if (connectedJoints[0] == edge)
            {
                return 0;
            }
            else if(connectedJoints[1] == edge)
            {
                return 1;
            }
            else
            {
                Debug.LogError("端じゃないRoadJointが渡された");
                return 0;
            }
        }

        /// <summary>
        /// 開始位置を返す
        /// </summary>
        public Vector2 GetStartingPoint(uint edgeID, uint laneID)
        {
            Debug.Assert(edgeID < 2);
            Debug.Assert(laneID < lanes);

            if (edgeID == 0)
            {
                return startingPoint0[laneID];
            }
            else
            {
                return startingPoint1[laneID];
            }
        }

        /// <summary>
        /// 車線左側の始点を返す
        /// </summary>
        public Vector2 GetLeftPoint(uint edgeID, uint laneID)
        {
            //車線左側→車線右側ベクトル
            Vector2 laneWidth = (edges[edgeID] - corners[edgeID]) / lanes;

            //laneIDは左から順
            return corners[edgeID] + (laneWidth * laneID);
        }

        /// <summary>
        /// 車線右側の始点を返す
        /// </summary>
        public Vector2 GetRightPoint(uint edgeID, uint laneID)
        {
            //車線左側→車線右側ベクトル
            Vector2 laneWidth = (edges[edgeID] - corners[edgeID]) / lanes;

            //laneIDは左から順
            return corners[edgeID] + (laneWidth * (laneID+1));
        }
    }
}