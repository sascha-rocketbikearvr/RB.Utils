using System;
using System.Collections;
using UnityEngine;

using RSG;
using LiteDB;

namespace RB.Utils.Experimental {
    public class CachedTexture {
        public static LiteCollection<CachedTexture> Collection { get; set; }

        [BsonIgnore]
        private int _width;
        [BsonIgnore]
        private int _height;
        [BsonIgnore]
        private TextureFormat _format;
        [BsonIgnore]
        private bool _mipmap;
        [BsonIgnore]
        private Texture2D _texture;
        [BsonIgnore]
        private WWW _www;

        public int Id { get; set; }
        public string URL { get; set; }
        public byte[] RawTextureData { get; set; }
        [BsonIgnore]
        public IPromise<Texture2D> Texture {
            get {
                if (_texture != null) {
                    return Promise<Texture2D>.Resolved(_texture);
                } else if (RawTextureData != null) {
                    _texture = new Texture2D(_width, _height, _format, _mipmap);
                    _texture.LoadRawTextureData(RawTextureData);
                    _texture.Apply();
                    return Promise<Texture2D>.Resolved(_texture);
                } else if (_www != null) {
                    return PromiseUtils.WrapCoroutine<IEnumerator>(_waitForTexture())
                        .Then<Texture2D>(enumerator => {
                            return _texture;
                    });
                } else {
                    _www = new WWW(URL);
                    return PromiseUtils.WrapCoroutine<WWW>(_www)
                        .Then<Texture2D>(www => {
                            if (www.isDone && www.error == null) {
                                _texture = www.texture;
                                _width = _texture.width;
                                _height = _texture.height;
                                _format = _texture.format;
                                _mipmap = _texture.mipmapCount > 0;
                                RawTextureData = _texture.GetRawTextureData();
                            } else {
                                Debug.LogError(www.error);
                                _texture = Texture2D.whiteTexture;
                            }
                            Collection.Update(this);
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
