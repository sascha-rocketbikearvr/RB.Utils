using System;
using System.Reflection;
using UnityEngine;

namespace RB.Utils {

    public static class SerializationUtils {

        [Serializable]
        public class SerializableType {
            [NonSerialized]
            private Type _type;
            [SerializeField]
            private string _fullName;

            public Type Type {
                get {
                    if (_type == null && _fullName != null) {
                        _type = SerializationUtils.TypeForName(_fullName);
                    }
                    return _type;
                }
                set {
                    _type = value;
                    _fullName = _type.FullName;
                }
            }

            public override bool Equals(object obj) {
                if (obj is SerializableType) {
                    return _type.Equals(((SerializableType)obj)._type);
                } else if (obj is Type) {
                    return _type.Equals(obj);
                }
                return false;
            }

            public override int GetHashCode() {
                return _type.GetHashCode();
            }

            public string Name { get { return _type != null ? _type.Name : null; } }

            public SerializableType(Type type) {
                this.Type = type;
            }

            public static implicit operator Type(SerializableType serializableType) {
                return serializableType.Type;
            }

            public static implicit operator SerializableType(Type type) {
                return new SerializableType(type);
            }
        }

        [Serializable]
        public class SerializableMemberInfo {
            [NonSerialized]
            private MemberInfo _memberInfo;
            [SerializeField]
            private SerializableType _type;
            [SerializeField]
            private string _name;
            [SerializeField]
            private MemberTypes _memberType;

            public SerializableMemberInfo(MemberInfo memberInfo) {
                this.MemberInfo = memberInfo;
            }

            public MemberInfo MemberInfo {
                get {
                    if (_memberInfo == null && _name != null && _type != null) {
                        //Debug.LogFormat("Deserializing MemberInfo Type:{0} Member-Name:{1}", _type.Type.Name, _name);
                        switch (_memberType) {
                            case MemberTypes.Field:
                                _memberInfo = _type.Type.GetField(_name);
                                break;
                            case MemberTypes.Property:
                                _memberInfo = _type.Type.GetProperty(_name);
                                break;
                            default:
                                throw new ArgumentException();
                        }
                    }
                    return _memberInfo;
                }
                set {
                    _memberInfo = value;
                    _name = _memberInfo.Name;
                    _type = _memberInfo.DeclaringType;
                    _memberType = _memberInfo.MemberType;
                }
            }

            public FieldInfo FieldInfo {
                get {
                    if (_memberType == MemberTypes.Field) {
                        return (FieldInfo)MemberInfo;
                    }
                    return null;
                }
                set {
                    MemberInfo = value;
                }
            }

            public PropertyInfo PropertyInfo {
                get {
                    if (_memberType == MemberTypes.Property) {
                        return (PropertyInfo)MemberInfo;
                    }
                    return null;
                }
                set {
                    MemberInfo = value;
                }
            }

            public Type MemberType {
                get {
                    switch (_memberType) {
                        case MemberTypes.Field:
                            return FieldInfo.FieldType;
                        case MemberTypes.Property:
                            return PropertyInfo.PropertyType;
                        default:
                            throw new ArgumentException();
                    }
                }
            }

            public object GetValue(object obj) {
                switch (_memberType) {
                    case MemberTypes.Field:
                        return FieldInfo.GetValue(obj);
                    case MemberTypes.Property:
                        return PropertyInfo.GetValue(obj, null);
                    default:
                        throw new ArgumentException();
                }
            }

            public void SetValue(object obj, object value) {
                switch (_memberType) {
                    case MemberTypes.Field:
                        FieldInfo.SetValue(obj, value);
                        break;
                    case MemberTypes.Property:
                        PropertyInfo.SetValue(obj, value, null);
                        break;
                    default:
                        throw new ArgumentException();
                }
            }

            public static implicit operator MemberInfo(SerializableMemberInfo serializableMemberInfo) {
                return serializableMemberInfo.MemberInfo;
            }

            public static implicit operator SerializableMemberInfo(MemberInfo memberInfo) {
                return new SerializableMemberInfo(memberInfo);
            }
        }

        public static Type TypeForName(string TypeName) {
            if (TypeName == null || TypeName.Equals("")) {
                Debug.Log("TypeForName called with null or empty string");
            }
            // Try Type.GetType() first. This will work with types defined
            // by the Mono runtime, in the same assembly as the caller, etc.
            var type = Type.GetType(TypeName);

            // If it worked, then we're done here
            if (type != null)
                return type;

            // If the TypeName is a full name, then we can try loading the defining assembly directly
            if (TypeName.Contains(".")) {

                // Get the name of the assembly (Assumption is that we are using 
                // fully-qualified type names)
                var assemblyName = TypeName.Substring(0, TypeName.IndexOf('.'));

                // Attempt to load the indicated Assembly
                try {
                    var assembly = Assembly.Load(assemblyName);
                    if (assembly != null) {
                        // Ask that assembly to return the proper Type
                        type = assembly.GetType(TypeName);
                        if (type != null) {
                            return type;
                        }
                    }
                } catch (Exception e) {
                    Debug.LogWarning(e);
                }
            }

            // If we still haven't found the proper type, we can enumerate all of the 
            // loaded assemblies and see if any of them define the type
            var currentAssembly = Assembly.GetExecutingAssembly();
            var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
            foreach (var assemblyName in referencedAssemblies) {

                // Load the referenced assembly
                var assembly = Assembly.Load(assemblyName);
                if (assembly != null) {
                    // See if that assembly defines the named type
                    type = assembly.GetType(TypeName);
                    if (type != null)
                        return type;
                }
            }

            // The type just couldn't be found...
            return null;

        }
    }
}
