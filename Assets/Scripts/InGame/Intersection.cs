using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace InGame
{
    /// <summary>
    /// 交差点。
    /// ３つ以上の道路をつなぐRoadJoint
    /// </summary>
    public class Intersection : RoadJoint
    {
        [Tooltip("対応する信号機システム。nullなら信号機無しの交差点")]
        [SerializeField] private TrafficLightsSystem trafficLightSystem;

        /// <summary>
        /// TrafficLightsSystemに道路、信号機を登録もする
        /// Roadが登録される前のStartに登録すると不具合が発生するので、その後に呼ぶ必要がある
        /// </summary>
        public override void ArrangeRoadsAnticlockwise()
        {
            base.ArrangeRoadsAnticlockwise();

            //信号機が無い場合はキャンセル
            if (trafficLightSystem == null)
            {
                return;
            }

            //TrafficLightsSystemに登録
            trafficLightSystem.RegisterTrafficLights(connectedRoads.ToArray(), edges);
        }
    }
}