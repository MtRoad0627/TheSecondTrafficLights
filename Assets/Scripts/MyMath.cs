using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 数学関数
/// </summary>
public static class MyMath
{
    /// <summary>
    /// ベクトルのｘ軸正からの反時計回りの角度(0-360)を算出
    /// </summary>
    public static float GetAngular(Vector2 vector)
    {
        Quaternion q = Quaternion.FromToRotation(Vector2.right, vector);

        float z = q.eulerAngles.z;

        if (Mathf.Approximately(z, 0f))
        {
            //180度回転の場合、x, y回転とみなされてzが０になっている可能性がある
            if ((q.eulerAngles.x > 90f) || (q.eulerAngles.y > 90f))
            {
                z = 180f;
            }
        }

        return z;
    }

    /// <summary>
    /// ２つのベクトルの角度（時計回り）を算出
    /// </summary>
    public static float GetAngularDifference(Vector2 fromVec, Vector2 toVec)
    {
        Quaternion q = Quaternion.FromToRotation(fromVec, toVec);

        float z = q.eulerAngles.z;

        if (Mathf.Approximately(z, 0f))
        {
            //180度回転の場合、x, y回転とみなされてzが０になっている可能性がある
            if ((q.eulerAngles.x > 90f) || (q.eulerAngles.y > 90f))
            {
                z = 180f;
            }
        }

        return z;
    }

    /// <summary>
    /// 与えられたベクトルに対し、与えられた点が右にあるかを返す
    /// </summary>
    public static bool IsRightFromVector(Vector2 point, Vector2 linePoint, Vector2 lineVector)
    {
        float angularDiference = Quaternion.FromToRotation(lineVector, point - linePoint).eulerAngles.z;

        if (angularDiference < 180f)
        {
            //左にある
            return false;
        }
        else
        {
            //右にある
            return true;
        }
    }

    /// <summary>
    /// 法線ベクトルを求める
    /// </summary>
    public static Vector2 GetPerpendicular(Vector2 vec)
    {
        return new Vector2(vec.y, -vec.x);
    }

    /// <summary>
    /// 角の二等分線ベクトル
    /// </summary>
    public static Vector2 GetBisector(Vector2 vec0, Vector2 vec1)
    {
        Vector2 u0 = vec0.normalized;
        Vector2 u1 = vec1.normalized;

        return (u0 + u1).normalized;
    }

    /// <summary>
    /// 点と直線の距離を求める
    /// </summary>
    public static float GetDistance(Vector2 point, Vector2 linePoint, Vector2 lineVector)
    {
        //pointの相対座標
        Vector2 pointToLineStart = point - linePoint;

        //pointからの正射影点
        float dotProduct = Vector2.Dot(pointToLineStart, lineVector);
        Vector2 projection = linePoint + lineVector * dotProduct;

        //相対座標と正射影点の距離を求めれば良い
        return Vector2.Distance(point, projection);
    }

    /// <summary>
    /// 誤差を許して同一値を返す
    /// </summary>
    public static bool IsSame(float v0, float v1, float threshold)
    {
        return (Mathf.Abs(v0 - v1) <= threshold);
    }

    /// <summary>
    /// 二つのベクトルが平行か（閾値以下）か返す
    /// </summary>
    public static bool IsParallel(Vector2 vec0, Vector2 vec1, float threshold)
    {

        float angle = Vector2.Angle(vec0, vec1);

        if ((angle <= threshold)
            || (Mathf.Abs(angle - 180f) <= threshold)
            || (Mathf.Abs(angle - 360f) <= threshold))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// 極座標から平面座標に変換
    /// </summary>
    public static Vector2 GetPositionFromPolar(Vector2 pole, float radius, float angular)
    {
        return pole + (Vector2)(Quaternion.Euler(0f, 0f, angular) * Vector2.right) * radius;
    }

    /// <summary>
    /// 点が直線上にあるか判定する
    /// </summary>
    public static bool CheckOnLine(Vector3 point, Vector3 linePoint, Vector3 lineVector, float threshold)
    {
        Vector3 difference = point - linePoint;

        //外積の大きさを求める
        float outer = Vector3.Cross(lineVector, difference).magnitude;

        //0なら直線上
        return IsSame(outer, 0f, threshold);
    }

    /// <summary>
    /// 垂線の足を求める
    /// </summary>
    public static Vector2 GetFootOfPerpendicular(Vector2 point, Vector2 linePoint, Vector2 lineVector)
    {
        Vector2 v = point - linePoint;
        float t = Vector2.Dot(v, lineVector) / lineVector.sqrMagnitude;
        Vector2 foot = linePoint + lineVector * t;

        return foot;
    }

    /// <summary>
    /// 二つの線分の交点を求める
    /// </summary>
    public static Vector2 GetIntersection(Vector2 line0Point, Vector2 line0Vector, Vector2 line1Point, Vector2 line1Vector)
    {
        // 外積を求める
        float cross = line0Vector.x * line1Vector.y - line0Vector.y * line1Vector.x;

        // 線分が平行である場合
        if (Mathf.Approximately(cross, 0f))
        {
            Debug.LogError("平行");
            return Vector2.zero;
        }

        // 交点を求める
        float t = ((line1Point.x - line0Point.x) * line1Vector.y - (line1Point.y - line0Point.y) * line1Vector.x) / cross;
        Vector2 intersectionPoint = line0Point + line0Vector * t;

        return intersectionPoint;
    }

    /// <summary>
    /// 速度から角速度を求める
    /// </summary>
    public static float GetAngularSpeed(float velocity, float radius)
    {
        return (180f * velocity) / (Mathf.PI * radius);
    }
}