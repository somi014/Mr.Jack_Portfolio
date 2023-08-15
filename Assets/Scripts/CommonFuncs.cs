using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonFunctions
{
    public static class CommonFuncs
    {
        public static string redReceive = "RedReceive";
        public static string greenReceive = "GreenReceive";
        public static string selectCharReceive = "SelectCharReceive";
        public static string checkRecive = "CheckRecive";

        public static T FindGameObject<T>(this Transform parent, string objName) where T : Component
        {
            GameObject result = null;
            List<T> childList = new List<T>();
            parent.GetComponentsInChildren(true, childList);

            foreach (var child in childList)
            {
                if (child.gameObject.name == objName)
                {
                    result = child.gameObject;
                    break;
                }
            }

            if (result != null)
            {
                return result.GetComponent<T>();
            }
            else
            {
                return null;
            }
        }

        public static GameObject FindGameObject(this Transform parent, string objName)
        {
            GameObject result = null;
            List<Transform> childList = new List<Transform>();
            parent.GetComponentsInChildren<Transform>(true, childList);

            foreach (var child in childList)
            {
                if (child.gameObject.name == objName)
                {
                    result = child.gameObject;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// 애니메이션 커브 총 재생시간 반환
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static float GetPlayTime(this AnimationCurve curve)
        {
            if (curve.length > 0)
            {
                return curve.keys[curve.length - 1].time;
            }
            return 0;
        }

        /// <summary>
        /// 애니메이션 커브 마지막 값을 반환
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static float GetLastValue(this AnimationCurve curve)
        {
            if (curve.length > 0)
            {
                return curve.Evaluate(curve.GetPlayTime());
            }
            return 0;
        }


        /// 현재 스케일 값에서 원하는 스케일 값으로 변경하기
        /// </summary>
        /// <param name="transform">대상</param>
        /// <param name="curve">커브</param>
        /// <param name="target">원하는 스케일 값</param>
        /// <param name="speed">속도</param>
        /// <returns></returns>
        public static IEnumerator IESetScale(this Transform transform, AnimationCurve curve, Vector3 target, float speed = 1)
        {
            float targetTime = curve.GetPlayTime();
            float curTime = 0;
            Vector3 originScale = transform.localScale;
            Vector3 gab = target - originScale;

            while (curTime <= targetTime)
            {
                float scalar = curve.Evaluate(curTime);
                transform.localScale = originScale + gab * scalar;

                yield return null;

                curTime += Time.deltaTime * speed;
            }

            transform.localScale = target;
        }

        /// <summary>
        /// 캔버스 알파 값 on off
        /// </summary>
        /// <param name="_cg"></param>
        /// <param name="_curve"></param>
        /// <param name="_on"></param>
        /// <param name="_speed"></param>
        /// <returns></returns>
        public static IEnumerator IEAlpha(this CanvasGroup cg, AnimationCurve curve, bool on, float speed = 1)
        {
            float targetTime = curve.GetPlayTime();
            float curTime = 0;
            float origin = cg.alpha;

            while (curTime <= targetTime)
            {
                float scalar = curve.Evaluate(curTime);
                if (on == true)
                {
                    cg.alpha = origin + (1 * scalar);
                }
                else
                {
                    cg.alpha = origin - (1 * scalar);
                }

                yield return null;

                curTime += Time.deltaTime * speed;
            }
            cg.alpha = on == true ? 1f : 0f;
        }

        public static IEnumerator IEMove(this Transform trans, AnimationCurve curve, Vector3 target, float speed = 1)
        {
            float targetTime = curve.GetPlayTime();
            float curTime = 0;
            Vector3 originPos = trans.position;
            Vector3 gab = target - originPos;

            while (curTime <= targetTime)
            {
                float scalar = curve.Evaluate(curTime);
                trans.position = Vector3.Lerp(originPos, target, scalar);

                yield return null;

                curTime += Time.deltaTime * speed;
            }

            trans.position = target;

        }

        public static void Shuffle<T>(this System.Random _rand, T[] _array)
        {
            int length = _array.Length;
            while (length > 1)
            {
                int random = _rand.Next(length--);
                T temp = _array[length];
                _array[length] = _array[random];
                _array[random] = temp;
            }
        }
    }
}