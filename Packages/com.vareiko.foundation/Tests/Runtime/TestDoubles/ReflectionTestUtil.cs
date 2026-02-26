using System;
using System.Reflection;

namespace Vareiko.Foundation.Tests.TestDoubles
{
    public static class ReflectionTestUtil
    {
        public static void SetPrivateField<T>(object instance, string fieldName, T value)
        {
            FieldInfo field = FindField(instance.GetType(), fieldName);
            if (field == null)
            {
                throw new MissingFieldException(instance.GetType().FullName, fieldName);
            }

            field.SetValue(instance, value);
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type current = type;
            while (current != null)
            {
                FieldInfo field = current.GetField(fieldName, flags);
                if (field != null)
                {
                    return field;
                }

                current = current.BaseType;
            }

            return null;
        }
    }
}
