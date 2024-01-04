using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InGame
{
    /// <summary>
    /// Carが走る経路を決定する
    /// </summary>
    public class Navigator : SingletonMonoBehaviour<Navigator>
    {
        /// <summary>
        /// A*用のノード
        /// </summary>
        private class Node
        {
            public RoadJoint roadJoint;
            public int parent;

            /// <summary>
            /// 手間コスト
            /// </summary>
            public float g;

            /// <summary>
            /// 距離コスト
            /// </summary>
            public float h;

            /// <summary>
            /// 総合コスト
            /// </summary>
            public float f
            {
                get
                {
                    return CalculateF(g, h);
                }
            }

            //コンストラクタ
            public Node(RoadJoint roadJoint)
            {
                this.roadJoint = roadJoint;
            }

            /// <summary>
            /// パラメーターを初期化
            /// </summary>
            public void Initialize()
            {
                g = 1000000f;
                h = 1000000f;
                parent = -1;
            }

            /// <summary>
            /// 2つのNodeが一致するか確認
            /// </summary>
            public bool Equals(Node other)
            {
                if (this.roadJoint == other.roadJoint)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void SetAll(float g = -1, float h = -1)
            {
                if (g >= -0.001f){
                    this.g = g;
                }
                
                if (h >= -0.001f)
                {
                    this.h = h;
                }
            }
        }

        /// <summary>
        /// 隣接行列
        /// </summary>
        private Road[,] adjacency;

        /// <summary>
        /// 隣接行列はこの順
        /// </summary>
        private Node[] nodes;

        /// <summary>
        /// 本クラスのセットアップ処理を行う。
        /// GetRoute()前に行われる必要があり、道路接続の後に呼ばれる必要がある。
        /// </summary>
        public void SetUp()
        {
            //全RoadJoint
            RoadJoint[] roadJoints = FindObjectsOfType<RoadJoint>();

            //nodeに変換
            nodes = new Node[roadJoints.Length];
            for(int cnt = 0; cnt < roadJoints.Length; cnt++)
            {
                nodes[cnt] = new Node(roadJoints[cnt]);
            }

            //隣接行列を作成
            Road[] roads = FindObjectsOfType<Road>();
            adjacency = MakeAdjacencyMatrix(nodes, roads);
        }

        /// <summary>
        /// 到達ルートを返す
        /// </summary>
        public Road[] GetRoute(RoadJoint start, RoadJoint destination)
        {
            //全ノード初期化
            InitializeNodes(nodes);

            //隣接行列上のインデックス
            int startIndex = FindIndex(start, nodes);
            int destinationIndex = FindIndex(destination, nodes);

            nodes[startIndex].SetAll(0, 0);

            Vector2 destinationPosition = nodes[destinationIndex].roadJoint.transform.position;

            HashSet<int> openHash = new HashSet<int>();
            HashSet<int> closedHash = new HashSet<int>();

            openHash.Add(startIndex);

            while (openHash.Count > 0)
            {
                //なんでもいい
                int currentIndex = GetMinOfHash(openHash);

                foreach (int index in openHash)
                {
                    if (nodes[index].f < nodes[currentIndex].f)
                    {
                        currentIndex = index;
                    }
                }

                openHash.Remove(currentIndex);
                closedHash.Add(currentIndex);

                //到着
                if (currentIndex == destinationIndex)
                {
                    List<int> answerRouteNodes = new List<int>();

                    int now = currentIndex;

                    while(now != -1)
                    {
                        answerRouteNodes.Add(now);
                        now = nodes[now].parent;
                    }

                    //反転（始点→終点の順にする）
                    answerRouteNodes.Reverse();

                    return ConvertToRoadRoute(answerRouteNodes).ToArray();
                }

                //子ノードの指定
                List<int> children = new List<int>();

                for(int cnt = 0; cnt < nodes.Length; cnt++)
                {
                    //コストが負（隣接していない）なら無視
                    if (adjacency[currentIndex, cnt] == null){
                        //>>隣接していない
                        //無視する
                        continue;
                    }
                    else
                    {
                        //>>隣接している
                        //子ノードの番号を保存
                        children.Add(cnt);
                    }   
                }

                //コストの計算
                foreach(int child in children)
                {
                    //現在のコストに移動コストを足す
                    float g = nodes[currentIndex].g + GetCostG(currentIndex, child);
                    Vector2 childPosition = nodes[child].roadJoint.transform.position;

                    //移動先ノード（子ノード）と目的地のユークリッド距離を求める
                    float h = Vector2.Distance(destinationPosition, childPosition);

                    float f = CalculateF(g, h);
                    if (nodes[child].f > f)
                    {
                        nodes[child].parent = currentIndex;
                        nodes[child].SetAll(g, h);
                    }
                    else
                    {
                        continue;
                    }

                    //すでにオープン予定でなくればハッシュに加える
                    if (openHash.Contains(child))
                    {
                        continue;
                    }
                    else
                    {
                        openHash.Add(child);
                    }
                }
            }

            //ここまで来たら、ゴールできるルートが無い
            Debug.LogError("到達不可能なルート");

            return null;
        }

        /// <summary>
        /// 隣接行列を作る
        /// </summary>
        private Road[,] MakeAdjacencyMatrix(Node[] nodes, Road[] roads)
        {
            //初期化
            Road[,] adjacency = new Road[nodes.Length, nodes.Length];

            //全道路を登録
            foreach(Road road in roads)
            {
                //接続したRoadJoint
                RoadJoint[] connected = road.connectedJoints;

                //そのIndex
                int index0 = FindIndex(connected[0], nodes);
                int index1 = FindIndex(connected[1], nodes);

                //登録
                adjacency[index0, index1] = road;
                adjacency[index1, index0] = road;
            }

            return adjacency;
        }

        /// <summary>
        /// Roadの手間コスト値を求める。
        /// </summary>
        private float GetCostG(int start, int end)
        {
            Road road = adjacency[start, end];

            return 1f;
        }

        /// <summary>
        /// 配列の中のインデックスを求める
        /// </summary>
        private int FindIndex(Node node, Node[] array)
        {
            //線形探索
            for(int cnt = 0; cnt < array.Length; cnt++)
            {
                if (node == array[cnt])
                {
                    return cnt;
                }
            }

            //見つからなかった
            return -1;
        }

        /// <summary>
        /// 配列の中のインデックスを求める
        /// </summary>
        private int FindIndex(RoadJoint node, Node[] array)
        {
            //線形探索
            for (int cnt = 0; cnt < array.Length; cnt++)
            {
                if (node == array[cnt].roadJoint)
                {
                    return cnt;
                }
            }

            //見つからなかった
            return -1;
        }

        /// <summary>
        /// HashSetから最小の要素を返す
        /// </summary>
        private int GetMinOfHash(HashSet<int> hash)
        {
            int min = int.MaxValue;

            foreach(int column in hash)
            {
                if (min > column)
                {
                    min = column;
                }
            }

            return min;
        }

        /// <summary>
        /// 順路のNodeリストをRoadのリストに変換
        /// </summary>
        /// <param name="nodeRoute"></param>
        /// <returns></returns>
        private List<Road> ConvertToRoadRoute(List<int> nodeRoute)
        {
            List<Road> output = new List<Road>();

            //スタックがなくなるまで
            //最後の一個は終点なので処理に要らない
            for(int cnt = 0; cnt < nodeRoute.Count-1; cnt++)
            {
                //始点
                int startIndex = nodeRoute[cnt];
                //終点
                int endIndex = nodeRoute[cnt + 1];

                //繋がる道
                Road road = adjacency[startIndex, endIndex];

                //登録
                output.Add(road);
            }

            return output;
        }

        /// <summary>
        /// 総合コストを計算
        /// </summary>
        /// <param name="g">手間コスト</param>
        /// <param name="h">距離コスト</param>
        private static float CalculateF(float g, float h)
        {
            return g + h;
        }

        /// <summary>
        /// nodeを初期化する
        /// </summary>
        private void InitializeNodes(Node[] nodes)
        {
            foreach(Node node in nodes)
            {
                node.Initialize();
            }
        }
    }
}