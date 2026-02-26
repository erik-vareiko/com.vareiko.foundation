using System;
using System.Reflection;

namespace Vareiko.Foundation.Tests.TestDoubles
{
    public static class ReflectionTestUtil
    {
        public static void SetPrivateField<T>(object instance, string fieldName, T value)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo field = instance.GetType().GetField(fieldName, flags);
            if (field == null)
            {
                throw new MissingFieldException(instance.GetType().FullName, fieldName);
            }

            field.SetValue(instance, value);
        }
    }
}
