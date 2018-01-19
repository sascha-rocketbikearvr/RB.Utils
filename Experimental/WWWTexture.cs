using System;
using System.Collections;
using UnityEngine;

using RSG;
using LiteDB;

namespace RB.Utils.Experimental {
    public class WWWTexture {
        public WWWTexture(string URL) {
            _url = URL;
        }

        private string _url;
        private Texture2D _texture;
        private WWW _www;

        public IPromise<Texture2D> Texture {
            get {
                if (_texture != null) {
                    return Promise<Texture2D>.Resolved(_texture);
                } else if (_www != null) {
                    return PromiseUtils.WrapCoroutine<IEnumerator>(_waitForTexture())
                        .Then<Texture2D>(enumerator => {
                            return _texture;
                        });
                } else {
                    _www = new WWW(_url);
                    return PromiseUtils.WrapCoroutine<WWW>(_www)
                        .Then<Texture2D>(www => {
                            if (www.isDone && www.error == null) {
                                _texture = www.texture;
                            } else {
                                Debug.LogError(www.error);
                                _texture = Texture2D.whiteTexture;
                            }
                            return _texture;
                        });
                }
            }
        }

        private IEnumerator _waitForTexture() {
            while (_texture == null) {
                yield return null;
            }
        }
    }
}
