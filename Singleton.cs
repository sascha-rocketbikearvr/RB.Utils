using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace RB.Utils {
    internal class _Singleton {
        private static GameObject _singletonContainer;
        internal static GameObject SingletonContainer {
            get {
                if (_singletonContainer == null) {
                    _singletonContainer = GameObject.Find("Singletons");
                }
                if (_singletonContainer == null) {
                    _singletonContainer = new GameObject("Singletons");
                    if (!Application.isEditor) {
                        GameObject.DontDestroyOnLoad(_Singleton._singletonContainer);
                    }
                }
                return _singletonContainer;
            }
        }
    }

    public class Singleton<T> {
        private static object _sharedInstance;
        public static T SharedInstance {
            get {
                if (_sharedInstance == null) {
                    Type type = typeof(T);
                    if (typeof(ScriptableObject).IsAssignableFrom(type)) {
                        _sharedInstance = ScriptableObject.CreateInstance(type);
                    } else if (typeof(MonoBehaviour).IsAssignableFrom(type)) {
                        _sharedInstance = _Singleton.SingletonContainer.AddComponent(type);
                    } else {
                        _sharedInstance = Activator.CreateInstance<T>();
                    }
                }
                return (T)_sharedInstance;
            }
			set {
				if (_sharedInstance == null) {
					_sharedInstance = value;
				} else {
					throw new InvalidOperationException(string.Format("Singleton for type {0} has already been set!", typeof(T)));
				}
			}
        }
    }
}
