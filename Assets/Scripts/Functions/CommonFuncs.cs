using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : class, new()
{
    protected static T instance = null;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new T();
            }
            return instance;
        }
    }
    private void Awake()
    {
        Debug.Log("singleton awake");
        if (instance != null)
        {
            Destroy(this);
        }
        DontDestroyOnLoad(this);
    }
}

namespace CommonFunctions
{
    public static class CommonFuncs
    {
        public static string redReceive = "RedReceive";
        public static string greenReceive = "GreenReceive";
        public static string selectCharReceive = "SelectCharReceive";
        public static string checkRecive = "CheckRecive";

        public static T FindGameObject<T>(this Transform _parent, string _name) where T : Component
        {
            GameObject result = null;
            var childList = new List<T>();
            _parent.GetComponentsInChildren(true, childList);

            foreach (var child in childList)
            {
                if (child.gameObject.name == _name)
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

        public static GameObject FindGameObject(this Transform _parent, string _name)
        {
            GameObject result = null;
            var childList = new List<Transform>();
            _parent.GetComponentsInChildren<Transform>(true, childList);

            foreach (var child in childList)
            {
                if (child.gameObject.name == _name)
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
        public static IEnumerator IEAlpha(this CanvasGroup _cg, AnimationCurve _curve, bool _on, float _speed = 1)
        {
            float targetTime = _curve.GetPlayTime();
            float curTime = 0;
            float origin = _cg.alpha;

            while (curTime <= targetTime)
            {
                float scalar = _curve.Evaluate(curTime);
                if (_on == true)
                {
                    _cg.alpha = origin + (1 * scalar);
                }
                else
                {
                    _cg.alpha = origin - (1 * scalar);
                }

                yield return null;

                curTime += Time.deltaTime * _speed;
            }
            _cg.alpha = _on == true ? 1f : 0f;
        }

        public static IEnumerator IEMove(this Transform _transform, AnimationCurve _curve, Vector3 _target, float _speed = 1)
        {
            float targetTime = _curve.GetPlayTime();
            float curTime = 0;
            Vector3 originPos = _transform.position;
            Vector3 gab = _target - originPos;

            while (curTime <= targetTime)
            {
                float scalar = _curve.Evaluate(curTime);
                _transform.position = Vector3.Lerp(originPos, _target, scalar);

                yield return null;

                curTime += Time.deltaTime * _speed;
            }

            _transform.position = _target;

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
