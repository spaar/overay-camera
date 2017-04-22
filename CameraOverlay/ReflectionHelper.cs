using System;
using System.Reflection;
using UnityEngine;

namespace spaar.Mods.CameraOverlay
{
  public static class ReflectionHelper
  {
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static T CallPrivateMethod<T>(this object instance, string methodName, Type[] parameterTypes, object[] parameters)
    {
      var method = instance.GetType().GetMethod(methodName, Flags, null, parameterTypes, null);
      return (T) method.Invoke(instance, parameters);
    }

    public static T GetPrivateField<T>(this object instance, string name)
    {
      var field = instance.GetType().GetField(name, Flags);
      return (T) field.GetValue(instance);
    }

    public static void SetPrivateField<T>(this object instance, string name, T value)
    {
      var field = instance.GetType().GetField(name, Flags);
      field.SetValue(instance, value);
    }
  }
}