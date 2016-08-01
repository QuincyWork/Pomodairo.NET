using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Pomodairo
{
    public class TaskCompleteConverter :
        IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Brush))
                return null;

            TaskItem task = value as TaskItem;
            if (task == null)
                return null;
            //return task.TaskComplete ? FontStyles.Italic : FontStyles.Normal;
            return task.TaskComplete ? new SolidColorBrush(Color.FromRgb(171,254,254)) : new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
