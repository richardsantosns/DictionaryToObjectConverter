using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

public class DictionaryToObjectConverter
{
    public T Convert<T>(Dictionary<string, object> dictionary) where T : new()
    {
        T obj = new T();

        foreach (var kvp in dictionary)
        {
            string propertyName = kvp.Key;
            object propertyValue = kvp.Value;

            PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                Type propertyType = propertyInfo.PropertyType;

                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    propertyValue = ConvertList(propertyValue, propertyType);
                }
                else if (propertyValue is Dictionary<string, object> nestedDictionary)
                {
                    propertyValue = ConvertNestedDictionary(nestedDictionary, propertyType);
                }
                else
                {
                    propertyValue = ConvertValue(propertyValue, propertyType);
                }

                propertyInfo.SetValue(obj, propertyValue);
            }
        }

        return obj;
    }

    private object ConvertValue(object value, Type targetType)
    {
        if (value == null)
            return null;

        Type valueType = value.GetType();

        if (targetType.IsAssignableFrom(valueType))
            return value;

        TypeConverter converter = TypeDescriptor.GetConverter(targetType);

        if (converter.CanConvertFrom(valueType))
        {
            return converter.ConvertFrom(value);
        }
        else if (targetType.IsEnum && value is string stringValue)
        {
            return Enum.Parse(targetType, stringValue, ignoreCase: true);
        }
        else if (targetType == typeof(Guid))
        {
            return new Guid(value.ToString());
        }
        else
        {
            try
            {
                return System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException)
            {
                throw new InvalidOperationException($"Cannot convert value '{value}' to type '{targetType}'.");
            }
        }
    }

    private object ConvertNestedDictionary(Dictionary<string, object> nestedDictionary, Type targetType)
    {
        var converterType = typeof(DictionaryToObjectConverter);
        var converterMethod = converterType.GetMethod("Convert", BindingFlags.Public | BindingFlags.Instance);
        var genericMethod = converterMethod.MakeGenericMethod(targetType);
        return genericMethod.Invoke(this, new object[] { nestedDictionary });
    }

    private object ConvertList(object value, Type targetType)
    {
        Type elementType = targetType.GetGenericArguments()[0];
        Type listType = typeof(List<>).MakeGenericType(elementType);
        IList list = (IList)Activator.CreateInstance(listType);

        foreach (var item in (IEnumerable)value)
        {
            object convertedItem;
            if (item is Dictionary<string, object> nestedDictionary)
            {
                // Recursively convert nested dictionaries within the list.
                convertedItem = ConvertNestedDictionary(nestedDictionary, elementType);
            }
            else
            {
                convertedItem = ConvertValue(item, elementType);
            }
            list.Add(convertedItem);
        }

        return list;
    }
}
