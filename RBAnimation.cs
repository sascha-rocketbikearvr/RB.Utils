using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System;

namespace RB.Utils {
    public class RBAnimation {
        public enum Curve { LINEAR, EASE_IN, EASE_OUT, EASE_OUT_BACK, EASE_INOUT, LINEAR_RECIPROCAL, EASE_INOUT_RECIPROCAL, LINEAR_LOGARITHMIC }
        public enum State { INITIALIZED, SCHEDULED, RUNNING, PENDING_COMLETION, FINISHED, CANCELED, TERMINATED }

		private static int __count = 0;

        private State _state = State.INITIALIZED;
        public readonly object target;
        public readonly PropertyInfo property;
        public readonly FieldInfo field;
        public readonly int shaderNameID;
        public readonly int arrayIndex;
        private object _startValue;
        public readonly object endValue;
        private float _startTime;
        public readonly float duration;
        public readonly Curve curve;
        private Action<RBAnimation> _onComplete;
		public readonly int __id;
		public State state {
            get {
                return _state;
            }
        }

        private RBAnimation(object target, PropertyInfo property, FieldInfo field, int shaderNameID, int arrayIndex, object endValue, float duration, Curve curve) {
            this.target = target;
            this.property = property;
            this.field = field;
            this.shaderNameID = shaderNameID;
            this.arrayIndex = arrayIndex;
            this.endValue = endValue;
            this.duration = duration;
            this.curve = curve;
			__id = __count++;
        }

        private static RBAnimation _new(object target, string member, int arrayIndex, object endValue, float delay, float duration, Curve curve, Action onComplete = null) {
            PropertyInfo property = null;
            FieldInfo field = null;
            Type type;
            if (target is Type) {
                type = (Type)target;
                target = null;
            } else {
                type = target.GetType();
            }
            int shaderNameID = -1;
            if (target is Material) {
                shaderNameID = Shader.PropertyToID(member);
            } else {
                property = type.GetProperty(member);
                if (property == null) {
                    field = type.GetField(member);
                }
            }
            return new RBAnimation(target, property, field, shaderNameID, arrayIndex, endValue, duration, curve);
        }

        public static RBAnimation NewAnimation(object target, string member, object endValue, float duration, Curve curve = Curve.EASE_OUT) {
            return _new(target, member, -1, endValue, 0.0f, duration, curve);
        }

        public static RBAnimation NewAnimation(object target, string member, object endValue, float delay, float duration, Curve curve = Curve.EASE_OUT) {
            return _new(target, member, -1, endValue, delay, duration, curve);
        }

        public static RBAnimation NewArrayAnimation(object target, string member, int arrayIndex, object endValue, float duration, Curve curve = Curve.EASE_OUT) {
            return _new(target, member, arrayIndex, endValue, 0.0f, duration, curve);
        }

        public static RBAnimation NewArrayAnimation(object target, string member, int arrayIndex, object endValue, float delay, float duration, Curve curve = Curve.EASE_OUT) {
            return _new(target, member, arrayIndex, endValue, delay, duration, curve);
        }
		 
        public void Start(Action<RBAnimation> onComplete = null, float delay = 0.0f) {
            if (_state != State.INITIALIZED) {
                throw new InvalidOperationException(string.Format("Tried to start animation {0} while in state {1}", target, _state));
            }
            _startTime = Time.time + delay;
            _onComplete = onComplete;
            _state = State.SCHEDULED;
            AnimationManager.Instance._AddAnimation(this);
        }		

        public void Cancel() {
            _state = State.CANCELED;
            AnimationManager.Instance._RemoveAnimation(this);
			if (_onComplete != null) {
				_onComplete(this);
			}
		}

		public void Terminate() {
			_setValue(endValue);
			_state = State.TERMINATED;
			AnimationManager.Instance._RemoveAnimation(this);
			if (_onComplete != null) {
				_onComplete(this);
			}
		}

		public void _complete() {
            if (_state != State.PENDING_COMLETION) {
                throw new InvalidOperationException(string.Format("Tried to complete animation {0} while in state {1}", target, _state));
            }
            _state = State.FINISHED;
			if (_onComplete != null) {
				_onComplete(this);
            }
        }

		private void _setValue(object value) {
			if (arrayIndex < 0) {
				if (shaderNameID >= 0) {
					_MaterialSetValue((Material)target, shaderNameID, value);
				} else if (property != null) {
					property.SetValue(target, value, null);
				} else if (field != null) {
					field.SetValue(target, value);
				}
			} else {
				if (property != null) {
					((IList)property.GetValue(target, null))[arrayIndex] = value;
				} else if (field != null) {
					((IList)field.GetValue(target))[arrayIndex] = value;
				}
			}
		}

        /// <summary>
        /// This method must only be called from the AnimationManager
        /// </summary>
        public void _animate(float time) {
            if (_state != State.SCHEDULED && _state != State.RUNNING) {
                throw new InvalidOperationException(string.Format("Tried to animate {0} while in state {1}", target, _state));
            }
            if (time < _startTime) {
                return;
            }
            if (_state == State.SCHEDULED) {
                if (arrayIndex < 0) {
                    if (shaderNameID >= 0) {
                        _startValue = _MaterialGetValue((Material)target, shaderNameID, endValue);
                    } else {
                        _startValue = (property != null) ? property.GetValue(target, null) : field.GetValue(target);
                    }
                } else {
                    Debug.Assert(shaderNameID < 0);
                    IList array = (IList)((property != null) ? property.GetValue(target, null) : field.GetValue(target));
                    _startValue = array[arrayIndex];
                }
                _state = State.RUNNING;
            }
            float t0 = Math.Min(1.0f, (time - _startTime) / duration);
            float t = _easeFunction(t0, curve);

            _setValue(_fade(_startValue, endValue, t));
            
            if (t0 >= 1.0f) {
                _state = State.PENDING_COMLETION;
            }
        }

        public override int GetHashCode() {
            int targetHashCode = (target != null) ? target.GetHashCode() : 0;
            if (property != null) {
                return targetHashCode + property.GetHashCode() + arrayIndex.GetHashCode();
            } else if (field != null) {
                return targetHashCode + field.GetHashCode() + arrayIndex.GetHashCode();
            }
            throw new InvalidOperationException();
        }

        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }
            RBAnimation a = (RBAnimation)obj;
            return this.target == a.target && this.property == a.property && this.field == a.field && this.arrayIndex == a.arrayIndex;
        }

        private static float _easeFunction(float t, Curve curve) {
            float v = 2.0f;// for EASE_OUT_BACK overshoot
            switch (curve) {
                case Curve.LINEAR:
                case Curve.LINEAR_LOGARITHMIC:
                case Curve.LINEAR_RECIPROCAL:
                    return t;
                case Curve.EASE_IN:
                    return t * t * t;
                case Curve.EASE_OUT:
                    return 1.0f + (--t) * t * t;
                case Curve.EASE_OUT_BACK:
                    return (t = t - 1.0f) * t * ((v + 1.0f) * t + v) + 1.0f;
                case Curve.EASE_INOUT:
                case Curve.EASE_INOUT_RECIPROCAL:
                    return t < 0.5f ? 4.0f * t * t * t : (t - 1) * (2.0f * t - 2.0f) * (2.0f * t - 2.0f) + 1.0f;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static object _fade(object a, object b, float t, Curve curve) {
            switch (curve) {
                case Curve.LINEAR_RECIPROCAL:
                    return _fadeReciprocal(a, b, _easeFunction(t, curve));
                case Curve.EASE_INOUT_RECIPROCAL:
                    return _fadeReciprocal(a, b, _easeFunction(t, curve));
                case Curve.LINEAR_LOGARITHMIC:
                    return _fadeLogarithmic(a, b, _easeFunction(t, curve));
                default:
                    return _fade(a, b, _easeFunction(t, curve));
            }
        }

        private static object _fade(object a, object b, float t) {
            if (a is float) {
                return (float)a + t * ((float)b - (float)a);
            } else if (a is Vector2) {
                return Vector2.LerpUnclamped((Vector2)a, (Vector2)b, t);
            } else if (a is Vector3) {
                return Vector3.LerpUnclamped((Vector3)a, (Vector3)b, t);
            } else if (a is Vector4) {
                return Vector4.LerpUnclamped((Vector4)a, (Vector4)b, t);
            } else if (a is Quaternion) {
                return Quaternion.LerpUnclamped((Quaternion)a, (Quaternion)b, t);
            } else if (a is Color) {
                return Color.LerpUnclamped((Color)a, (Color)b, t);
            } else if (a is Color32) {
                return Color32.LerpUnclamped((Color32)a, (Color32)b, t);
            } else {
                throw new ArgumentException();
            }
        }

        private static object _fadeReciprocal(object a, object b, float t) {
            if (a is float) {
                return 1.0f / (1.0f / (float)a + t * (1.0f / (float)b - 1.0f / (float)a));
            } else {
                throw new ArgumentException();
            }
        }

        private static object _fadeLogarithmic(object a, object b, float t) {
            if (a is float) {
                return Mathf.Exp(Mathf.Log((float)a) + t * (Mathf.Log((float)b) - Mathf.Log((float)a)));
            } else {
                throw new ArgumentException();
            }
        }

        private static object _MaterialGetValue(Material material, int nameID, object value) {
            if (value is float) {
                return material.GetFloat(nameID);
            } else if (value is Vector2) {
                return material.GetVector(nameID);
            } else if (value is Vector3) {
                return material.GetVector(nameID);
            } else if (value is Vector4) {
                return material.GetVector(nameID);
            } else if (value is Color) {
                return material.GetColor(nameID);
            } else {
                throw new ArgumentException();
            }
        }

        private static void _MaterialSetValue(Material material, int nameID, object value) {
            if (value is float) {
                material.SetFloat(nameID, (float)value);
            } else if (value is Vector2) {
                material.SetVector(nameID, (Vector4)value);
            } else if (value is Vector3) {
                material.SetVector(nameID, (Vector4)value);
            } else if (value is Vector4) {
                material.SetVector(nameID, (Vector4)value);
            } else if (value is Color) {
                material.SetColor(nameID, (Color)value);
            } else {
                throw new ArgumentException();
            }
        }

		public class AnimationManager : MonoBehaviour {

			public static AnimationManager Instance {
				get {
					return Singleton<AnimationManager>.SharedInstance;
				}
			}

			private Dictionary<RBAnimation, RBAnimation> _animations = new Dictionary<RBAnimation, RBAnimation>();

			public void Update() {
				float time = Time.time;
				HashSet<RBAnimation> completed = new HashSet<RBAnimation>();
				foreach (RBAnimation animation in _animations.Keys) {
					animation._animate(time);
					if (animation.state == RBAnimation.State.PENDING_COMLETION) {
						completed.Add(animation);
					}
				}
				foreach (RBAnimation animation in completed) {
					_animations.Remove(animation);
					animation._complete();
				}
			}

			public void _AddAnimation(RBAnimation animation) {
				if (_animations.ContainsKey(animation)) {
					_animations[animation].Cancel();
				}
				_animations[animation] = animation;
			}

			public void _RemoveAnimation(RBAnimation animation) {
				_animations.Remove(animation);
			}
		}
	}
}
