﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace Minecraft_Launcher_2.Converters
{
    [ValueConversion(typeof(Enum), typeof(int))]
    public class EnumToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int) value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}