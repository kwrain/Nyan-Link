using System.Collections.Generic;
using UnityEngine;
using NineMood.Core.HexCoordinates;

namespace NineMood.Core.HexCoordinates
{
    /// <summary>
    /// 헥사곤 좌표계 유틸리티 함수들
    /// </summary>
    public static class HexUtils
    {
        /// <summary>
        /// 두 좌표 사이의 직선 경로를 반환 (Bresenham 알고리즘 기반)
        /// </summary>
        public static List<HexCoordinates> GetLine(HexCoordinates from, HexCoordinates to)
        {
            List<HexCoordinates> line = new List<HexCoordinates>();
            int distance = HexCoordinates.Distance(from, to);
            
            for (int i = 0; i <= distance; i++)
            {
                float t = distance > 0 ? (float)i / distance : 0f;
                line.Add(Round(
                    Mathf.Lerp(from.q, to.q, t),
                    Mathf.Lerp(from.r, to.r, t),
                    Mathf.Lerp(from.s, to.s, t)));
            }
            
            return line;
        }

        /// <summary>
        /// 부동소수점 Cube 좌표를 반올림하여 정수 좌표로 변환
        /// </summary>
        private static HexCoordinates Round(float q, float r, float s)
        {
            int qRounded = Mathf.RoundToInt(q);
            int rRounded = Mathf.RoundToInt(r);
            int sRounded = Mathf.RoundToInt(s);

            float qDiff = Mathf.Abs(qRounded - q);
            float rDiff = Mathf.Abs(rRounded - r);
            float sDiff = Mathf.Abs(sRounded - s);

            if (qDiff > rDiff && qDiff > sDiff)
            {
                qRounded = -rRounded - sRounded;
            }
            else if (rDiff > sDiff)
            {
                rRounded = -qRounded - sRounded;
            }
            else
            {
                sRounded = -qRounded - rRounded;
            }

            return new HexCoordinates(qRounded, rRounded, sRounded);
        }

        /// <summary>
        /// 중심 좌표에서 반경 내의 모든 좌표 반환
        /// </summary>
        public static List<HexCoordinates> GetRange(HexCoordinates center, int radius)
        {
            List<HexCoordinates> results = new List<HexCoordinates>();

            for (int q = -radius; q <= radius; q++)
            {
                int r1 = Mathf.Max(-radius, -q - radius);
                int r2 = Mathf.Min(radius, -q + radius);
                for (int r = r1; r <= r2; r++)
                {
                    HexCoordinates hex = new HexCoordinates(q, r);
                    results.Add(center + hex);
                }
            }

            return results;
        }

        /// <summary>
        /// 두 좌표 사이의 맨하탄 거리 (헥사곤 거리)
        /// </summary>
        public static int HexDistance(HexCoordinates a, HexCoordinates b)
        {
            return HexCoordinates.Distance(a, b);
        }

        /// <summary>
        /// 여러 좌표들의 중심점 계산
        /// </summary>
        public static HexCoordinates GetCenter(List<HexCoordinates> coordinates)
        {
            if (coordinates == null || coordinates.Count == 0)
            {
                return new HexCoordinates(0, 0);
            }

            float sumQ = 0f;
            float sumR = 0f;
            float sumS = 0f;

            foreach (var coord in coordinates)
            {
                sumQ += coord.q;
                sumR += coord.r;
                sumS += coord.s;
            }

            float count = coordinates.Count;
            return Round(sumQ / count, sumR / count, sumS / count);
        }
    }
}
