using System;
using RSG;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace RB.Utils {
    public class PromiseUtils: MonoBehaviour {
        public static PromiseUtils instance {
            get {
                return Singleton<PromiseUtils>.SharedInstance;
            }
        }

        private void Awake() {
            Promise.UnhandledException += _UnhandledException;
        }

        private void _UnhandledException(object sender, ExceptionEventArgs e) {
            Debug.LogError(e.Exception);
        }
       
        public static Promise<T> WrapCoroutine<T>(T routine) where T : IEnumerator {
            return new Promise<T>((resolve, reject) => instance.StartCoroutine(WrappedCoroutine(routine, resolve, reject)));
        }

        public static Promise<T> WrapYieldInstruction<T>(T yieldInstruction) where T : YieldInstruction{
            return new Promise<T>((resolve, reject) => instance.StartCoroutine(WrappedYieldInstruction(yieldInstruction, resolve, reject)));
        }

        public static Promise<RBAnimation> WrapAnimation(RBAnimation animation, float delay = 0.0f) {
            return new Promise<RBAnimation>((resolve, reject) => animation.Start(resolve, delay));
        }

        private static IEnumerator WrappedCoroutine<T>(T routine, Action<T> resolve, Action<Exception> reject) where T : IEnumerator {
            yield return routine;
            resolve(routine);
        }

        private static IEnumerator WrappedYieldInstruction<T>(T yieldInstruction, Action<T> resolve, Action<Exception> reject) where T : YieldInstruction {
            yield return yieldInstruction;
            resolve(yieldInstruction);
        }
    }
}

