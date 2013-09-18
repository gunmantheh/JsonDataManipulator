using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace JsonDataManipulator.Helpers
{
    public static class JsonHelper
    {
        private static bool _isSelected;
        private const ConsoleColor SELECTED = ConsoleColor.Gray;
        
        public static string SpeedToString(this decimal value)
        {
            var suffix = "K";
            if (value >= 1024)
            {
                value = value / 1024;
                suffix = "M"; if (value >= 1024)
                {
                    value = value / 1024;
                    suffix = "G"; if (value >= 1024)
                    {
                        value = value / 1024;
                        suffix = "T";
                    }
                }
            }
           
            string speedToString = String.Format("{0} {1}B/s", value.Round2Dp().ToString(CultureInfo.InvariantCulture).PadLeft(7), suffix);
            return speedToString;
        }

        public static decimal Round2Dp(this decimal value)
        {
            return Math.Round(value, 2);
        }

        public static string SizeToString(this decimal value)
        {
            var suffix = "M";
            if (value >= 1024)
            {
                value = value / 1024;
                suffix = "G";
                if (value >= 1024)
                {
                    value = value / 1024;
                    suffix = "T";
                }
            }
            
            
            string result = String.Format("{0} {1}B", value.Round2Dp().ToString(CultureInfo.InvariantCulture).PadLeft(5), suffix);
            return result;
        }

        public static bool IsSelected(int selected, int position)
        {
            _isSelected = selected == position;
            if (_isSelected)
            {
                Console.BackgroundColor = SELECTED;
            }
            return _isSelected;
        }

        public static string GetEnumDescription<T>(string value)
        {
            Type type = typeof(T);
            var name = Enum.GetNames(type).Where(f => f.Equals(value, StringComparison.CurrentCultureIgnoreCase)).Select(d => d).FirstOrDefault();

            if (name == null)
            {
                return string.Empty;
            }
            var field = type.GetField(name);
            var customAttribute = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return customAttribute.Length > 0 ? ((DescriptionAttribute)customAttribute[0]).Description : name;
        }

        public static string GetDescription<TObjectClass>(TObjectClass value)
        {
            return GetEnumDescription<TObjectClass>(value.ToString());
        }

        public static string Description<T>(this T value) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }
            return GetEnumDescription<T>(value.ToString(CultureInfo.InvariantCulture));
        }
    }
}